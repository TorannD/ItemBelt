using System.Collections.Generic;
using Verse.AI;
using System;
using System.Diagnostics;

namespace ItemBelt
{
    public class JobDriver_AddItemToBelt : JobDriver
    {

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            Toil gotoThing = new Toil();
            gotoThing.initAction = delegate
            {
                this.pawn.pather.StartPath(this.TargetThingA, PathEndMode.ClosestTouch);
            };
            gotoThing.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            gotoThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return gotoThing;
            yield return Toils_ItemBelt.TakeToItemBelt(TargetIndex.A, this.job.count);
        }
    }
}
