using Verse;

namespace ItemBelt.Settings
{
    public class Variables : ModSettings
    {
        public static Variables Instance;



        public Variables()
        {
            Variables.Instance = this;
        }

        public override void ExposeData()
        {

        }
    }
}
