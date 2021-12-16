﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace TOW_Core.Utilities.Extensions
{
    public static class MissionExtensions
    {
        public static void RemoveMissionBehaviourIfNotNull(this Mission mission, MissionBehavior behavior) 
        {
            if(behavior != null)
            {
                mission.RemoveMissionBehavior(behavior);
            }
        }
    }
}
