using Harmony;
using System.Reflection;
using RimWorld;
using System;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemBelt
{
    [StaticConstructorOnStartup]
    internal class Main
    {

        static Main()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("rimworld.torann.ItemBelt");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", null)]
        public static class FloatMenuMakerMap_Patch
        {

            public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                IntVec3 c = IntVec3.FromVector3(clickPos);
                CompItemBelt comp = pawn.TryGetComp<CompItemBelt>();
                if (comp != null)
                {
                    if (c.GetThingList(pawn.Map).Count == 0 && !pawn.Drafted)
                    {
                        if (comp.innerContainer != null && comp.innerContainer.Count > 0)
                        {
                            if (!pawn.CanReach(c, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("IB_CannotDrop".Translate(new object[]
                                {
                                   comp.innerContainer[0].Label
                                }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("IB_DropItemBeltAll".Translate(new object[]
                                {
                                comp.innerContainer.ContentsString
                                }), delegate
                                {
                                    Job job = new Job(ItemBeltDefOf.RemoveItemFromBelt, c);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, c, "ReservedBy"));
                            }

                        }
                    }
                    foreach (Thing current in c.GetThingList(pawn.Map))
                    {
                        Thing t = current;

                        if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                        {                            
                            Thing item = c.GetFirstItem(pawn.Map);
                            if (item != null && item.def.EverHaulable)
                            {
                                if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                                {
                                    opts.Add(new FloatMenuOption("CannotPickUp".Translate(new object[]
                                    {
                                    item.Label
                                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                }
                                else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, 1))
                                {
                                    opts.Add(new FloatMenuOption("CannotPickUp".Translate(new object[]
                                    {
                                    item.Label
                                    }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                }
                                else if (item.stackCount == 1)
                                {
                                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("IB_AddToItemBelt".Translate(new object[]
                                    {
                                    item.Label
                                    }), delegate
                                    {
                                        item.SetForbidden(false, false);
                                        Job job = new Job(ItemBeltDefOf.AddItemToBelt, item);
                                        job.count = 1;
                                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, item, "ReservedBy"));
                                }
                                else
                                {
                                    if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, item.stackCount))
                                    {
                                        opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(new object[]
                                        {
                                        item.Label
                                        }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                    }
                                    else
                                    {
                                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("IB_AddAllToBelt".Translate(new object[]
                                        {
                                        item.Label
                                        }), delegate
                                        {
                                            item.SetForbidden(false, false);
                                            Job job = new Job(ItemBeltDefOf.AddItemToBelt, item);
                                            job.count = item.stackCount;
                                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                        }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, item, "ReservedBy"));
                                    }
                                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("IB_AddSomeToBelt".Translate(new object[]
                                    {
                                    item.Label
                                    }), delegate
                                    {
                                        int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item), item.stackCount);
                                        Dialog_Slider window = new Dialog_Slider("PickUpCount".Translate(new object[]
                                        {
                                        item.LabelShort
                                        }), 1, to, delegate (int count)
                                        {
                                            item.SetForbidden(false, false);
                                            Job job = new Job(ItemBeltDefOf.AddItemToBelt, item);
                                            job.count = count;
                                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                        }, -2147483648);
                                        Find.WindowStack.Add(window);
                                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, item, "ReservedBy"));
                                }
                            }
                            
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MassUtility), "InventoryMass", null)]
        public class MassUtility_Patch
        {
            public static void Postfix(Pawn p, ref float __result)
            {
                float num = 0f;
                if (p.def.HasComp(typeof(CompItemBelt)))
                {
                    CompItemBelt comp = p.GetComp<CompItemBelt>();
                    if (comp.innerContainer != null && comp.innerContainer.Count > 0)
                    { 
                        for (int i = 0; i < comp.innerContainer.Count; i++)
                        {
                            Thing thing = comp.innerContainer[i];
                            num += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass, true);
                        }
                        __result += num;
                    }
                }
            }
        }
                


        [HarmonyPatch(typeof(Pawn), "GetGizmos", null)]
        public class Pawn_DraftController_GetGizmos_Patch
        {
            public static void Postfix(ref IEnumerable<Gizmo> __result, ref Pawn __instance)
            {
                Pawn pawn = __instance;
                bool flag = __instance != null || __instance.Faction.Equals(Faction.OfPlayer);
                if (flag)
                {
                    bool flag2 = __result == null || !__result.Any<Gizmo>();
                    if (!flag2)
                    {
                        CompItemBelt itembelt = __instance.TryGetComp<CompItemBelt>();
                        bool flag3 = itembelt == null;
                        if (!flag3)
                        {
                            List<Gizmo> list = __result.ToList<Gizmo>();
                            if (Find.Selector.SelectedObjects.Count < 2 && itembelt.innerContainer != null)
                            {
                                for (int i = 0; i < itembelt.innerContainer.Count; i++)
                                {
                                    Command_Action item = new Command_Action
                                    {
                                        defaultLabel = itembelt.innerContainer[i].Label,
                                        defaultDesc = itembelt.innerContainer[i].GetDescription(),
                                        icon = itembelt.innerContainer[i].def.uiIcon,                                        

                                        action = delegate
                                        {
                                            Thing thing = itembelt.innerContainer[0].SplitOff(1);
                                            GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Direct, null);
                                            //Job job = new Job(JobDefOf.Ingest, thing);    
                                            Job job = new Job(ItemBeltDefOf.UseItemFromBelt, thing);
                                            job.count = 1;
                                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                        }
                                    };                                    
                                    list.Add(item);
                                }
                                __result = list;
                            }
                        }
                    }
                }
            }
        }
    }
}
