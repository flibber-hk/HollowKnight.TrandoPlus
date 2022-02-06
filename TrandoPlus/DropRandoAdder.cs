using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Collections.Generic;
using System.Linq;

namespace TrandoPlus
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

            if (!TrandoPlus.GS.RandomizeDrops) return;

            foreach (VanillaDef def in new List<VanillaDef>(rb.Vanilla.SelectMany(x => x.Value)))
            {
                if (Data.IsTransition(def.Item) && Data.GetTransitionDef(def.Item).Sides == TransitionSides.OneWayOut)
                {
                    DropRandoTransitions.Add(def.Item);
                    DropRandoTransitions.Add(def.Location);
                    rb.RemoveFromVanilla(def);
                }
            }
        }

        private static void AddDropsToRandoPool(RequestBuilder rb)
        {
            if (!TrandoPlus.GS.RandomizeDrops) return;

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
