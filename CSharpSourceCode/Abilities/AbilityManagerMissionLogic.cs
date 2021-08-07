﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TOW_Core.Utilities.Extensions;

namespace TOW_Core.Abilities
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        public AbilityManagerMissionLogic() { }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission.CurrentState == Mission.State.Continuing && Agent.Main != null && Agent.Main.IsAbilityUser())
            {
                if (Input.IsKeyPressed(InputKey.Q))
                {
                    Agent.Main.CastCurrentAbility();
                }
                if (Input.IsKeyPressed(InputKey.T))
                {
                    Agent.Main.SelectNextAbility();
                }
            }
        }
    }
}
