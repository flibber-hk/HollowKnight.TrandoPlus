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

            foreach (VanillaDef vDef in new List<VanillaDef>(rb.Vanilla.SelectMany(x => x.Value)))
            {
                if (rb.TryGetTransitionDef(vDef.Item, out TransitionDef tDef) && tDef.Sides == TransitionSides.OneWayOut)
                {
                    DropRandoTransitions.Add(vDef.Item);
                    DropRandoTransitions.Add(vDef.Location);
                    rb.RemoveFromVanilla(vDef);
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
                if (rb.TryGetTransitionDef(trans, out TransitionDef tDef) && tDef.Sides == TransitionSides.OneWayIn)
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
