﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOW_Core.CampaignSupport.QuestBattleLocation;
using TOW_Core.Utilities;

namespace TOW_Core.CampaignSupport.ChaosRaidingParty
{
    public class ChaosRaidingPartyCampaignBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, HourlyTickPartyAI);
        }

        private void HourlyTickPartyAI(MobileParty party, PartyThinkParams partyThinkParams)
        {
            if (party.PartyComponent is ChaosRaidingPartyComponent)
            {
                var component = (ChaosRaidingPartyComponent) party.PartyComponent;
                
                if (component.Patrol)
                {
                    PatrolBehavior(partyThinkParams, component);
                }

                if (!component.Patrol)
                {
                    RaiderBehavior(party, partyThinkParams, component);
                }

                party.Ai.SetDoNotMakeNewDecisions(false);
            }
        }

        private static void RaiderBehavior(MobileParty party, PartyThinkParams partyThinkParams, ChaosRaidingPartyComponent component)
        {
            if (party.TargetSettlement.IsRaided)
            {
                AIBehaviorTuple key = new AIBehaviorTuple(party.TargetSettlement, AiBehavior.RaidSettlement);
                partyThinkParams.AIBehaviorScores[key] = 0f;
                var find = FindAllBelongingToSettlement("Averheim").FindAll(settlementF => !settlementF.IsRaided);
                if (find.Count > 0)
                {
                    key = new AIBehaviorTuple(find.GetRandomElement(), AiBehavior.RaidSettlement);
                    partyThinkParams.AIBehaviorScores[key] = 0f;
                }
                else
                {
                    key = new AIBehaviorTuple(component.Portal, AiBehavior.GoToSettlement);
                    partyThinkParams.AIBehaviorScores[key] = 10f;
                }
            }

            if (party.TargetSettlement != component.Portal)
            {
                AIBehaviorTuple key = new AIBehaviorTuple(party.TargetSettlement, AiBehavior.RaidSettlement);
                partyThinkParams.AIBehaviorScores[key] = 10f;
            }
        }

        private static void PatrolBehavior(PartyThinkParams partyThinkParams, ChaosRaidingPartyComponent component)
        {
            AIBehaviorTuple key = new AIBehaviorTuple(component.Portal, AiBehavior.PatrolAroundPoint);
            partyThinkParams.AIBehaviorScores[key] = 10f;
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.Name.ToString() != "Chaos Portal") return; //TODO: Is there a better way to do this?
            var questBattleComponent = settlement.GetComponent<QuestBattleComponent>();
            SpawnNewParties(settlement, questBattleComponent);
        }

        private static void SpawnNewParties(Settlement settlement, QuestBattleComponent questBattleComponent)
        {
            if (questBattleComponent != null)
            {
                if (questBattleComponent.RaidingParties.Count < 5)
                {
                    var find = FindAllBelongingToSettlement("Averheim").GetRandomElement();
                    var chaosRaidingParty = ChaosRaidingPartyComponent.CreateChaosRaidingParty("chaos_clan_1_party_" + questBattleComponent.RaidingParties.Count + 1, settlement, questBattleComponent, 30);
                    chaosRaidingParty.Ai.SetAIState(AIState.Raiding);
                    chaosRaidingParty.SetMoveRaidSettlement(find);
                    FactionManager.DeclareWar(chaosRaidingParty.Party.MapFaction, Clan.PlayerClan);
                    TOWCommon.Say("Raiding " + find.Name);
                }

                if (questBattleComponent.PatrolParties.Count < 2)
                {
                    var chaosRaidingParty = ChaosRaidingPartyComponent.CreateChaosPatrolParty("chaos_clan_1_patrol_" + questBattleComponent.PatrolParties.Count + 1, settlement, questBattleComponent, 120);
                    chaosRaidingParty.Ai.SetAIState(AIState.PatrollingAroundLocation);
                    chaosRaidingParty.SetMovePatrolAroundSettlement(settlement);
                    TOWCommon.Say("Patrolling around " + settlement.Name);
                }
            }
        }

        private static List<Settlement> FindAllBelongingToSettlement(string settlementName)
        {
            return Campaign.Current.Settlements.ToList().FindAll(settlementF => settlementF.IsVillage && settlementF.Village.Bound.Name.ToString() == settlementName);
        }
    }
}