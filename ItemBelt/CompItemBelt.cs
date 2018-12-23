using System;
using Verse;
using System.Collections.Generic;


namespace ItemBelt
{
    public class CompItemBelt : ThingComp
    {

        public ThingOwner<Thing> innerContainer = new ThingOwner<Thing>();

        private bool initialize = true;
        //public List<Thing> innerContainer = new List<Thing>();

        private Pawn pawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Pawn pawn = this.parent as Pawn;
            if (initialize)
            {
                this.innerContainer = new ThingOwner<Thing>();
                this.initialize = false;
            }

        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<ThingOwner<Thing>>(ref this.innerContainer, "innerContainer", new object[0]);
            Scribe_Values.Look<bool>(ref this.initialize, "initailize", true, false);
            //Scribe_Collections.Look<Thing>(ref this.innerContainer, "innerContainer", LookMode.Deep);

        }
    }
}
