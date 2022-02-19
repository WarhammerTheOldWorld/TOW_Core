﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;
using TOW_Core.Abilities.Crosshairs;
using TOW_Core.Battle.AI.Components;
using TOW_Core.Battle.CrosshairMissionBehavior;
using TOW_Core.Battle.Crosshairs;
using TOW_Core.Items;
using TOW_Core.Utilities;
using TOW_Core.Utilities.Extensions;

namespace TOW_Core.Abilities
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _isAbilityUser;
        private AbilityModeState _currentState;
        private EquipmentIndex _mainHand;
        private EquipmentIndex _offHand;
        private AbilityComponent _abilityComponent;
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("CombatHotKeyCategory");
        private static readonly ActionIndexCache _idleAnimation = ActionIndexCache.Create("act_spellcasting_idle");
        private ParticleSystem[] _psys = null;
        private readonly string _castingStanceParticleName = "psys_spellcasting_stance";
        private SummonedCombatant _defenderSummoningCombatant;
        private SummonedCombatant _attackerSummoningCombatant;
        private readonly float DamagePortionForChargingSpecialMove = 0.25f;
        private CustomCrosshairMissionBehavior _crosshairBehavior; 

        public AbilityModeState CurrentState => _currentState;

        public override void OnFormationUnitsSpawned(Team team)
        {
            if(team.Side == BattleSideEnum.Attacker && _attackerSummoningCombatant == null)
            {
                var culture = team.Leader == null ? team.TeamAgents.FirstOrDefault().Character.Culture : team.Leader.Character.Culture;
                _attackerSummoningCombatant = new SummonedCombatant(team, culture);
            }
            else if (team.Side == BattleSideEnum.Defender && _defenderSummoningCombatant == null)
            {
                var culture = team.Leader == null ? team.TeamAgents.FirstOrDefault().Character.Culture : team.Leader.Character.Culture;
                _defenderSummoningCombatant = new SummonedCombatant(team, culture);
            }
        }

        protected override void OnEndMission()
        {
            BindWeaponKeys();
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
        {
            var comp = affectorAgent.GetComponent<AbilityComponent>();
            if(comp != null)
            {
                if(comp.SpecialMove != null) comp.SpecialMove.AddCharge(damage * DamagePortionForChargingSpecialMove);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (IsAbilityModeAvailableForMainAgent())
            {
                HandleInput();

                UpdateWieldedItems();

                HandleAnimations();
            }
        }

        private void HandleAnimations()
        {
            if(CurrentState != AbilityModeState.Off)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if(CurrentState == AbilityModeState.Idle && action != _idleAnimation)
                {
                    Agent.Main.SetActionChannel(1, _idleAnimation);
                }
            }
        }

        internal void OnCastComplete()
        {
            if(CurrentState == AbilityModeState.Casting) _currentState = AbilityModeState.Idle;
        }

        internal void OnCastStart() 
        {
            if(CurrentState == AbilityModeState.Idle) _currentState = AbilityModeState.Casting;
        }

        private void UpdateWieldedItems()
        {
            if (_currentState == AbilityModeState.Idle && _shouldSheathWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                }
                else if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
                }
                else
                {
                    _shouldSheathWeapon = false;
                }
            }
            if (_currentState == AbilityModeState.Off && _shouldWieldWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != _mainHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_mainHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                else if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) != _offHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                else
                {
                    _shouldWieldWeapon = false;
                }
            }
        }

        private void HandleInput()
        {
            //Turning ability mode on/off
            if (Input.IsKeyPressed(InputKey.Q))
            {
                switch (_currentState)
                {
                    case AbilityModeState.Off:
                        EnableAbilityMode();
                        break;
                    case AbilityModeState.Idle:
                        DisableAbilityMode(false);
                        break;
                    default:
                        break;
                }
            }
            else if (Input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                bool flag = _abilityComponent.CurrentAbility.Crosshair == null ||
                            !_abilityComponent.CurrentAbility.Crosshair.IsVisible ||
                            _currentState != AbilityModeState.Idle ||
                            (_abilityComponent.CurrentAbility.Crosshair.CrosshairType == CrosshairType.Targeted &&
                            ((TargetedCrosshair)_abilityComponent.CurrentAbility.Crosshair).Target == null);
                if (!flag)
                {
                    Agent.Main.CastCurrentAbility();
                }
                if(_abilityComponent.SpecialMove != null && _abilityComponent.SpecialMove.IsUsing) _abilityComponent.StopSpecialMove();
            }
            else if (Input.IsKeyPressed(InputKey.RightMouseButton))
            {
                if (_abilityComponent.SpecialMove != null && _abilityComponent.SpecialMove.IsUsing) _abilityComponent.StopSpecialMove();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollUp) && _currentState != AbilityModeState.Off)
            {
                Agent.Main.SelectNextAbility();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollDown) && _currentState != AbilityModeState.Off)
            {
                Agent.Main.SelectPreviousAbility();
            }
            else if (Input.IsKeyPressed(InputKey.LeftControl) && _abilityComponent != null && _abilityComponent.SpecialMove != null)
            {
                if (_currentState == AbilityModeState.Off && _abilityComponent.SpecialMove.IsCharged && (_crosshairBehavior == null || !(_crosshairBehavior.CurrentCrosshair is SniperScope) || !_crosshairBehavior.CurrentCrosshair.IsVisible))
                {
                    _abilityComponent.SpecialMove.TryCast(Agent.Main);
                }
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (IsCastingMission(Mission))
            {
                if (agent.IsAbilityUser())
                {
                    agent.AddComponent(new AbilityComponent(agent));
                    if (agent.IsAIControlled)
                    {
                        agent.AddComponent(new WizardAIComponent(agent));
                    }
                }
            }
        }

        public static bool IsCastingMission(Mission mission)
        {
            return !mission.IsFriendlyMission && mission.CombatType != Mission.MissionCombatType.ArenaCombat && mission.CombatType != Mission.MissionCombatType.NoCombat;
        }

        private bool IsAbilityModeAvailableForMainAgent()
        {
            return _isAbilityUser &&
                   Agent.Main != null &&
                   Agent.Main.IsActive() &&
                   !ScreenManager.GetMouseVisibility();
        }

        private void EnableAbilityMode()
        {
            _mainHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            _offHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            _shouldSheathWeapon = true;
            _currentState = AbilityModeState.Idle;
            ChangeKeyBindings();
            var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            if (traitcomp != null)
            {
                traitcomp.EnableAllParticles(false);
            }
            EnableCastStanceParticles(true);
        }

        private void DisableAbilityMode(bool isTakingNewWeapon)
        {
            if (isTakingNewWeapon)
            {
                _mainHand = EquipmentIndex.None;
                _offHand = EquipmentIndex.None;
            }
            else
            {
                _shouldWieldWeapon = true;
            }
            _currentState = AbilityModeState.Off;
            ChangeKeyBindings();
            var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            if (traitcomp != null)
            {
                traitcomp.EnableAllParticles(true);
            }
            EnableCastStanceParticles(false);
        }
        private void EnableCastStanceParticles(bool enable)
        {
            if(_psys != null)
            {
                foreach (var psys in _psys)
                {
                    if(psys != null) psys.SetEnable(enable);
                }
            }
        }

        private void ChangeKeyBindings()
        {
            if (_abilityComponent != null && _currentState != AbilityModeState.Off)
            {
                UnbindWeaponKeys();
            }
            else
            {
                BindWeaponKeys();
            }
        }

        private void BindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.MouseScrollUp);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.MouseScrollDown);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Numpad1);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Numpad2);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Numpad3);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Numpad4);
        }

        private void UnbindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Invalid);
        }

        public override void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if(agent == Agent.Main) DisableAbilityMode(true);
        }

        public SummonedCombatant GetSummoningCombatant(Team team)
        {
            if (team.Side == BattleSideEnum.Attacker) return _attackerSummoningCombatant;
            else if (team.Side == BattleSideEnum.Defender) return _defenderSummoningCombatant;
            else return null;
        }

        protected override void OnAgentControllerChanged(Agent agent, Agent.ControllerType oldController)
        {
            if (agent.Controller != Agent.ControllerType.Player || Agent.Main == null || !Agent.Main.IsAbilityUser())
            {
                return;
            }
            _abilityComponent = Agent.Main.GetComponent<AbilityComponent>();
            _crosshairBehavior = Mission.GetMissionBehavior<CustomCrosshairMissionBehavior>();
            if (_abilityComponent != null)
            {
                _psys = new ParticleSystem[2];
                GameEntity entity;
                _psys[0] = TOWParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.HumanMonster.MainHandItemBoneIndex, out entity);
                _psys[1] = TOWParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.HumanMonster.OffHandItemBoneIndex, out entity);
                EnableCastStanceParticles(false);
                _currentState = AbilityModeState.Off;
                _isAbilityUser = true;
            }
        }
    }

    public enum AbilityModeState
    {
        Off,
        Idle,
        Casting
    }
}