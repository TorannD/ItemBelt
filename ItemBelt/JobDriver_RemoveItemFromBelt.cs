using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse.AI;
using Verse;
using RimWorld;

namespace ItemBelt
{
    public class JobDriver_RemoveItemFromBelt : JobDriver
    {
        private int useDuration = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.useDuration, "useDuration", 0, false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
        }

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            Toil drop = new Toil();
            drop.initAction = delegate
            {
                Pawn actor = drop.actor;              
                CompItemBelt comp = actor.TryGetComp<CompItemBelt>();
                comp.innerContainer.TryDropAll(actor.Position, actor.Map, ThingPlaceMode.Near);
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }
    }    
}
