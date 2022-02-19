﻿using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOW_Core.Battle.TriggeredEffect.Scripts
{
    public class GrenadeScript : ScriptComponentBehavior
    {
        private Agent _shooter;
        private TriggeredEffect _explosion;

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.None;
        }

        protected override void OnRemoved(int removeReason)
        {
            Explode();
        }

        private void Explode()
        {
            _explosion.Trigger(GameEntity.GlobalPosition, Vec3.Zero, _shooter);
            GameEntity.FadeOut(0.5f, true);
        }

        public void SetShooterAgent(Agent shooter)
        {
            _shooter = shooter;
        }

        public void SetTriggeredEffect(TriggeredEffect effect)
        {
            _explosion = effect;
        }
    }
}
