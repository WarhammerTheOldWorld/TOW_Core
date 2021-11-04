﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.MountAndBlade;
using TOW_Core.Utilities.Extensions;
using TOW_Core.Utilities;

namespace TOW_Core.Battle.StatusEffects
{
    public class StatusEffectMissionLogic : MissionLogic
    {
        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman)
            {
                StatusEffectComponent effectComponent = new StatusEffectComponent(agent);
                agent.AddComponent(effectComponent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            for (var num = 0; num < Mission.Current.Agents.Count; num++)
            {
                var agent = Mission.Current.Agents[num];
                if (agent != null && agent.IsActive() && agent.Health > 0f)
                {
                    if (agent.GetComponent<StatusEffectComponent>() != null)
                    {
                        var comp = agent.GetComponent<StatusEffectComponent>();
                        comp.OnTick(dt);
                    }
                }
            }
        }
    }
}