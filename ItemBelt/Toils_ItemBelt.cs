using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace ItemBelt
{
    public class Toils_ItemBelt
    {
        public static void ErrorCheck(Pawn pawn, Thing haulThing)
        {
            if (!haulThing.Spawned)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which isn't spawned."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (haulThing.stackCount == 0)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which had stackcount 0."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (pawn.jobs.curJob.count <= 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Invalid count: ",
                    pawn.jobs.curJob.count,
                    ", setting to 1. Job was ",
                    pawn.jobs.curJob
                }));
                pawn.jobs.curJob.count = 1;
            }
        }

        public static Toil TakeToItemBelt(TargetIndex ind, int count)
        {
            return Toils_ItemBelt.TakeToItemBelt(ind, () => count);
        }

        public static Toil TakeToItemBelt(TargetIndex ind, Func<int> countGetter)
        {
            Toil takeThing = new Toil();
            takeThing.initAction = delegate
            {
                Pawn actor = takeThing.actor;
                Thing thing = actor.CurJob.GetTarget(ind).Thing;
                Toils_ItemBelt.ErrorCheck(actor, thing);
                int num = Mathf.Min(countGetter(), thing.stackCount);
                if (num <= 0)
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
                else
                {
                    CompItemBelt comp = actor.GetComp<CompItemBelt>();
                    if(comp.innerContainer == null)
                    {
                        comp.Initialize(comp.props);
                    }
                    if (comp != null)
                    {
                        if (comp.innerContainer.Count > 0)
                        {
                            if (comp.innerContainer[0].def != thing.def)
                            {
                                comp.innerContainer.TryDropAll(actor.Position, actor.Map, ThingPlaceMode.Near);
                            }
                        }
                        comp.innerContainer.TryAdd(thing.SplitOff(num), true);
                        if (thing.def.ingestible != null && thing.def.ingestible.preferability <= FoodPreferability.RawTasty)
                        {
                            actor.mindState.lastInventoryRawFoodUseTick = Find.TickManager.TicksGame;
                        }
                        thing.def.soundPickup.PlayOneShot(new TargetInfo(actor.Position, actor.Map, false));
                    }
                    else
                    {
                        Log.Message("comp was null");
                    }
                }
            };
            return takeThing;
        }

        public static Toil PickupIngestible(TargetIndex ind, Pawn eater)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(ind).Thing;
                if (curJob.count <= 0)
                {
                    Log.Error("Tried to do PickupIngestible toil with job.maxNumToCarry = " + curJob.count);
                    actor.jobs.EndCurrentJob(JobCondition.Errored, true);
                    return;
                }
                int count = Mathf.Min(thing.stackCount, curJob.count);
                actor.carryTracker.TryStartCarry(thing, count, true);
                if (thing != actor.carryTracker.CarriedThing && actor.Map.reservationManager.ReservedBy(thing, actor, curJob))
                {
                    actor.Map.reservationManager.Release(thing, actor, curJob);
                }
                actor.jobs.curJob.targetA = actor.carryTracker.CarriedThing;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        public static Toil ChewIngestible(Pawn chewer, float durationMultiplier, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd = TargetIndex.None)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing thing = actor.CurJob.GetTarget(ingestibleInd).Thing;
                if (!thing.IngestibleNow)
                {
                    chewer.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                actor.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);
                if (thing.Spawned)
                {
                    thing.Map.physicalInteractionReservationManager.Reserve(chewer, actor.CurJob, thing);
                }
            };
            toil.tickAction = delegate
            {
                if (chewer != toil.actor)
                {
                    toil.actor.rotationTracker.FaceCell(chewer.Position);
                }
                else
                {
                    Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
                    if (thing != null && thing.Spawned)
                    {
                        toil.actor.rotationTracker.FaceCell(thing.Position);
                    }
                    else if (eatSurfaceInd != TargetIndex.None && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid)
                    {
                        toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(eatSurfaceInd).Cell);
                    }
                }
                toil.actor.GainComfortFromCellIfPossible();
            };
            toil.WithProgressBar(ingestibleInd, delegate
            {
                Pawn actor = toil.actor;
                Thing thing = actor.CurJob.GetTarget(ingestibleInd).Thing;
                if (thing == null)
                {
                    return 1f;
                }
                return 1f - (float)toil.actor.jobs.curDriver.ticksLeftThisToil / Mathf.Round((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);
            }, false, -0.5f);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.FailOnDestroyedOrNull(ingestibleInd);
            toil.AddFinishAction(delegate
            {
                if (chewer == null)
                {
                    return;
                }
                if (chewer.CurJob == null)
                {
                    return;
                }
                Thing thing = chewer.CurJob.GetTarget(ingestibleInd).Thing;
                if (thing == null)
                {
                    return;
                }
                if (chewer.Map.physicalInteractionReservationManager.IsReservedBy(chewer, thing))
                {
                    chewer.Map.physicalInteractionReservationManager.Release(chewer, toil.actor.CurJob, thing);
                }
            });
            toil.handlingFacing = true;
            Toils_ItemBelt.AddIngestionEffects(toil, chewer, ingestibleInd, eatSurfaceInd);
            return toil;
        }

        public static Toil AddIngestionEffects(Toil toil, Pawn chewer, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd)
        {
            toil.WithEffect(delegate
            {
                LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                if (!target.HasThing)
                {
                    return null;
                }
                EffecterDef result = target.Thing.def.ingestible.ingestEffect;
                if (chewer.RaceProps.intelligence < Intelligence.ToolUser && target.Thing.def.ingestible.ingestEffectEat != null)
                {
                    result = target.Thing.def.ingestible.ingestEffectEat;
                }
                return result;
            }, delegate
            {
                if (!toil.actor.CurJob.GetTarget(ingestibleInd).HasThing)
                {
                    return null;
                }
                Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
                if (chewer != toil.actor)
                {
                    return chewer;
                }
                if (eatSurfaceInd != TargetIndex.None && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid)
                {
                    return toil.actor.CurJob.GetTarget(eatSurfaceInd);
                }
                return thing;
            });
            toil.PlaySustainerOrSound(delegate
            {
                if (!chewer.RaceProps.Humanlike)
                {
                    return null;
                }
                LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                if (!target.HasThing)
                {
                    return null;
                }
                return target.Thing.def.ingestible.ingestSound;
            });
            return toil;
        }

        public static Toil FinalizeIngest(Pawn ingester, TargetIndex ingestibleInd)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(ingestibleInd).Thing;
                if (ingester.needs.mood != null && thing.def.IsNutritionGivingIngestible && thing.def.ingestible.chairSearchRadius > 10f)
                {
                    if (!(ingester.Position + ingester.Rotation.FacingCell).HasEatSurface(actor.Map) && ingester.GetPosture() == PawnPosture.Standing)
                    {
                        ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.AteWithoutTable, null);
                    }
                    Room room = ingester.GetRoom(RegionType.Set_Passable);
                    if (room != null)
                    {
                        int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
                        if (ThoughtDefOf.AteInImpressiveDiningRoom.stages[scoreStageIndex] != null)
                        {
                            ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(ThoughtDefOf.AteInImpressiveDiningRoom, scoreStageIndex), null);
                        }
                    }
                }
                float num = ingester.needs.food.NutritionWanted;
                if (curJob.overeat)
                {
                    num = Mathf.Max(num, 0.75f);
                }
                float num2 = thing.Ingested(ingester, num);
                if (!ingester.Dead)
                {
                    ingester.needs.food.CurLevel += num2;
                }
                ingester.records.AddTo(RecordDefOf.NutritionEaten, num2);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

    }
}
