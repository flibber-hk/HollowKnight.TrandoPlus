using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Collections.Generic;

namespace DoorRando
{
    public static class DropRandoAdder
    {
        public static readonly HashSet<string> DropRandoTransitions = new();

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(-745f, CaptureRandomizedDrops);
            RequestBuilder.OnUpdate.Subscribe(-745f, AddDropsToRandoPool);
        }

        private static void CaptureRandomizedDrops(RequestBuilder rb)
        {
            DropRandoTransitions.Clear();

            if (!DoorRando.GS.RandomizeDrops) return;

            foreach (VanillaRequest pair in new List<VanillaRequest>(rb.Vanilla.EnumerateDistinct()))
            {
                if (Data.IsTransition(pair.Item) && Data.GetTransitionDef(pair.Item).Sides == TransitionSides.OneWayOut)
                {
                    DropRandoTransitions.Add(pair.Item);
                    DropRandoTransitions.Add(pair.Location);
                    rb.Vanilla.RemoveAll(pair);
                }
            }
        }

        private static void AddDropsToRandoPool(RequestBuilder rb)
        {
            if (!DoorRando.GS.RandomizeDrops) return;

            if (rb.gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }

            TransitionGroupBuilder tgb;

            if (rb.TryGetStage(RBConsts.MainTransitionStage, out StageBuilder sb))
            {
                tgb = (TransitionGroupBuilder)sb.Get(RBConsts.OneWayGroup);
            }
            else
            {
                if (!rb.TryGetStage(Consts.DoorRandoTransitionStage, out sb))
                {
                    sb = rb.InsertStage(0, Consts.DoorRandoTransitionStage);
                }
                tgb = new TransitionGroupBuilder()
                {
                    label = Consts.DropRandoGroup,
                    stageLabel = Consts.DoorRandoTransitionStage
                };
                sb.Add(tgb);
                tgb.strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy();
            }

            foreach (string trans in DropRandoTransitions)
            {
                if (Data.GetTransitionDef(trans).Sides == TransitionSides.OneWayIn)
                {
                    tgb.Sources.Add(trans);
                }
                else
                {
                    tgb.Targets.Add(trans);
                }
            }

            rb.OnGetGroupFor.Subscribe(-999f, MatchedTryResolveGroup);

            bool MatchedTryResolveGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb)
            {
                if (type == RequestBuilder.ElementType.Transition)
                {
                    if (DropRandoTransitions.Contains(item))
                    {
                        gb = tgb;
                        return true;
                    }
                }
                gb = default;
                return false;
            }

        }
    }
}
