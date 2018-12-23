using UnityEngine;
using Verse;

namespace ItemBelt.Settings
{ 
    public class Controller : Mod
    {
        public static Controller Instance;

        public override string SettingsCategory()
        {
            return "Item Slot";
        }

        public Controller(ModContentPack content) : base(content)
        {
            Controller.Instance = this;
            Settings.Variables.Instance = base.GetSettings<Settings.Variables>();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            int num = 0;
            float rowHeight = 36f;
            Rect rect1 = new Rect(canvas);
            rect1.width /= 2f;
            num++;
            Rect rowRect = UIHelper.GetRowRect(rect1, rowHeight, num);

        }

        public static class UIHelper
        {
            public static Rect GetRowRect(Rect inRect, float rowHeight, int row)
            {
                float y = rowHeight * (float)row;
                Rect result = new Rect(inRect.x, y, inRect.width, rowHeight);
                return result;
            }
        }
    }
}
