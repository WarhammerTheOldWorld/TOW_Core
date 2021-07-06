﻿using HarmonyLib;
using MountAndBlade.CampaignBehaviors;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.OptionsStage;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOW_Core.CampaignSupport;
using TOW_Core.CampaignSupport.QuestBattleLocation;
using TOW_Core.Utilities;
using TOW_Core.Utilities.Extensions;

//Need a way to somehow skip loading of vanilla xmls in the following categories:
//Settlements, Clans, Kingdoms, Heroes

namespace TOW_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CampaignPatches
    {
        private static readonly Dictionary<string, string> _typesToForce = new Dictionary<string, string>()
        {
            {"Settlements", "tow_settlements.xml"},
            {"Heroes", "tow_heroes.xml"},
            {"Kingdoms", "tow_kingdoms.xml"},
            {"Factions", "tow_clans.xml"},
        };

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBObjectManager), "LoadXML")]
        public static bool ForceLoadCertainTypes(string id, MBObjectManager __instance)
        {
            if (_typesToForce.ContainsKey(id))
            {
                try
                {
                    var path = System.IO.Path.Combine(BasePath.Name, "Modules/TOW_Core/ModuleData/" + _typesToForce[id]);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    __instance.LoadXml(doc, null);
                    return false;
                }
                catch (Exception)
                {
                    TOWCommon.Log("Error in MBObjectManagerPatch, tried to force load TOW specific xml but failed.", NLog.LogLevel.Error);
                    return true;
                }
            }
            else return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UrbanCharactersCampaignBehavior), "OnNewGameCreated")]
        public static void AfterWandererTemplatesBuilt(ref List<CharacterObject> ____companionTemplates)
        {
            ____companionTemplates = ____companionTemplates.Where(x => x.IsTOWTemplate()).ToList();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UrbanCharactersCampaignBehavior), "OnGameLoaded")]
        public static void AfterWandererTemplatesBuilt2(ref List<CharacterObject> ____companionTemplates)
        {
            ____companionTemplates = ____companionTemplates.Where(x => x.IsTOWTemplate()).ToList();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UrbanCharactersCampaignBehavior), "SpawnUrbanCharactersAtGameStart")]
        public static bool SpawnWanderersAtStart(UrbanCharactersCampaignBehavior __instance, ref List<Hero> ____companions, List<CharacterObject> ____companionTemplates)
        {
            List<CharacterObject> list = ____companionTemplates.Where(x => x.IsTOWTemplate()).ToList();
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsTown)
                {
                    int targetNotableCountForSettlement = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Artisan);
                    for (int i = 0; i < targetNotableCountForSettlement; i++)
                    {
                        HeroCreator.CreateHeroAtOccupation(Occupation.Artisan, settlement);
                    }
                    int targetNotableCountForSettlement2 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Merchant);
                    for (int j = 0; j < targetNotableCountForSettlement2; j++)
                    {
                        HeroCreator.CreateHeroAtOccupation(Occupation.Merchant, settlement);
                    }
                    int targetNotableCountForSettlement3 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.GangLeader);
                    for (int k = 0; k < targetNotableCountForSettlement3; k++)
                    {
                        HeroCreator.CreateHeroAtOccupation(Occupation.GangLeader, settlement);
                    }
                    for(int n = 0; n < list.Count; n++)
                    {
                        ____companions.Add(CreateTowWanderer(settlement, list[n]));
                    }
                }
                else if (settlement.IsVillage)
                {
                    int targetNotableCountForSettlement4 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.RuralNotable);
                    for (int l = 0; l < targetNotableCountForSettlement4; l++)
                    {
                        HeroCreator.CreateHeroAtOccupation(Occupation.RuralNotable, settlement);
                    }
                    int targetNotableCountForSettlement5 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Headman);
                    for (int m = 0; m < targetNotableCountForSettlement5; m++)
                    {
                        HeroCreator.CreateHeroAtOccupation(Occupation.Headman, settlement);
                    }
                }
            }
            return false;
        }

        private static Hero CreateTowWanderer(Settlement settlement, CharacterObject template)
        {
            Hero hero = null;
            if (template != null && settlement != null)
            {
                hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(27));
                Campaign.Current.GetCampaignBehavior<IHeroCreationCampaignBehavior>().DeriveSkillsFromTraits(hero, template);
                List<Equipment> equipments = new List<Equipment>();
                equipments.Add(hero.BattleEquipment);
                equipments.Add(hero.CivilianEquipment);
                ItemModifier @object = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
                ItemModifier object2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
                ItemModifier object3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
                foreach (var equipment in equipments)
                {
                    for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
                    {
                        EquipmentElement equipmentElement = equipment[equipmentIndex];
                        if (equipmentElement.Item != null)
                        {
                            if (equipmentElement.Item.ArmorComponent != null)
                            {
                                equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, @object);
                            }
                            else if (equipmentElement.Item.HorseComponent != null)
                            {
                                equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object3);
                            }
                            else if (equipmentElement.Item.WeaponComponent != null)
                            {
                                equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object2);
                            }
                        }
                    }
                }
            }
            return hero;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Kingdom), "InitialHomeLand", MethodType.Getter)]
        public static void InitialHomeLandFix(ref Settlement __result, Kingdom __instance)
        {
            if (__result == null)
            {
                __result = Settlement.All.FirstOrDefault((Settlement x) => x.IsTown && x.MapFaction == __instance.MapFaction);
                if (__result == null)
                {
                    __result = Settlement.All.FirstOrDefault((Settlement x) => x.IsTown);
                }
            }
        }

        //Ideally this should not need a harmony patch, but somehow removing this on gamestart in submodule.cs still makes it run on NewGameStart event.
        //This behaviour contains hardcoded hero / lord references that are not present because we skip loading the vanilla files.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BackstoryCampaignBehavior), "RegisterEvents")]
        public static bool WhyIsThisNeeded()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Module), "GetInitialStateOptions")]
        public static void MainMenuSkipStoryMode(ref IEnumerable<InitialStateOption> __result)
        {
            List<InitialStateOption> newlist = new List<InitialStateOption>();
            newlist = __result.Where(x => x.Id != "StoryModeNewGame" && x.Id != "SandBoxNewGame").ToList();
            var towOption = new InitialStateOption("TOWNewgame", new TextObject("Enter the Old World"), 3, OnCLick, IsDisabledAndReason);
            newlist.Add(towOption);
            newlist.Sort((x, y) => x.OrderIndex.CompareTo(y.OrderIndex));
            __result = newlist;
        }

        //TODO!!! this is partially responsible for poor campaign performance. When Rob is ready with the map, the distance cache has to be generated with a script from within the scene editor.
        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(DefaultMapDistanceModel), "LoadCacheFromFile")]
        public static bool DisableSettlementCache(ref System.IO.BinaryReader reader)
        {
            reader = null;
            return true;
        }*/

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CampaignOptions), MethodType.Constructor)]
        public static void DisableLifeDeathCycleByDefault()
        {
            CampaignOptions.IsLifeDeathCycleDisabled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterCreationOptionsStageVM), MethodType.Constructor,
            typeof(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation), typeof(Action), typeof(TextObject),
            typeof(Action), typeof(TextObject), typeof(int), typeof(int), typeof(int), typeof(Action<int>))]
        public static void RemoveLifeDeathCycleOptionFromCharacterCreation(CharacterCreationOptionsStageVM __instance)
        {
            var option = __instance.OptionsController.Options.First(o => o.Identifier == "IsLifeDeathCycleEnabled");
            __instance.OptionsController.Options.Remove(option);
            __instance.RefreshValues();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CampaignOptionsVM), MethodType.Constructor, typeof(Action))]
        public static void RemoveLifeDeathCycleFromCampaignMenu(CampaignOptionsVM __instance)
        {
            var option = __instance.OptionsController.Options.First(o => o.Identifier == "IsLifeDeathCycleEnabled");
            __instance.OptionsController.Options.Remove(option);
            __instance.RefreshValues();
        }

        private static void OnCLick()
        {
            MBGameManager.StartNewGame(new TowCampaignGameManager());
        }

        private static (bool, TextObject) IsDisabledAndReason()
        {
            TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.", null);
            return new ValueTuple<bool, TextObject>(Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapScene), "Load")]
        public static bool CustomMapSceneLoad(MapScene __instance, ref Scene ____scene, ref MBAgentRendererSceneController ____agentRendererSceneController)
        {
            Debug.Print("Creating map scene", 0, Debug.DebugColor.White, 17592186044416UL);
            ____scene = Scene.CreateNewScene(false);
            ____scene.SetName("MapScene");
            ____scene.SetClothSimulationState(true);
            ____agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(____scene, 4096);
            ____scene.SetOcclusionMode(true);
            SceneInitializationData initData = new SceneInitializationData(true);
            initData.UsePhysicsMaterials = false;
            initData.EnableFloraPhysics = false;
            initData.UseTerrainMeshBlending = false;
            Debug.Print("reading map scene", 0, Debug.DebugColor.White, 17592186044416UL);
            ____scene.Read("modded_main_map", initData, "");
            TaleWorlds.Engine.Utilities.SetAllocationAlwaysValidScene(____scene);
            ____scene.DisableStaticShadows(true);
            ____scene.InvalidateTerrainPhysicsMaterials();
            MBMapScene.LoadAtmosphereData(____scene);
            __instance.DisableUnwalkableNavigationMeshes();
            MBMapScene.ValidateTerrainSoundIds();
            ____scene.OptimizeScene(true, false);
            Debug.Print("Ticking map scene for first initialization", 0, Debug.DebugColor.White, 17592186044416UL);
            ____scene.Tick(0.1f);
            return false;
        }
    }
}
