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

            foreach (var pair in new List<VanillaRequest>(rb.Vanilla.EnumerateDistinct()))
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

            TransitionGroupBuilder gb;

            if (rb.TryGetStage(RBConsts.MainTransitionStage, out StageBuilder sb))
            {
                gb = (TransitionGroupBuilder)sb.Get(RBConsts.OneWayGroup);
            }
            else if (rb.TryGetStage(Consts.DoorRandoTransitionStage, out sb))
            {
                gb = new TransitionGroupBuilder()
                {
                    label = Consts.DropRandoGroup,
                    stageLabel = Consts.DoorRandoTransitionStage
                };
                sb.Add(gb);
                gb.strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy();
            }
            else
            {
                sb = rb.InsertStage(0, Consts.DropRandoTransitionStage);
                gb = new TransitionGroupBuilder()
                {
                    label = Consts.DropRandoGroup,
                    stageLabel = Consts.DoorRandoTransitionStage
                };
                sb.Add(gb);
                gb.strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy();
            }

            foreach (string trans in DropRandoTransitions)
            {
                if (Data.GetTransitionDef(trans).Sides == TransitionSides.OneWayIn)
                {
                    gb.Sources.Add(trans);
                }
                else
                {
                    gb.Targets.Add(trans);
                }
            }
        }
    }
}
