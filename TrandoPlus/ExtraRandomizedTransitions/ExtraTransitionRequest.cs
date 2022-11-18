using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrandoPlus.Utils;

namespace TrandoPlus.ExtraRandomizedTransitions
{
    public static class ExtraTransitionRequest
    {
        private static TransitionSelectionManager manager;

        public static bool ShouldRun(RequestBuilder rb) => TrandoPlus.GS.IsEnabled() 
            && (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer);

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(-2000, InstantiateManager);
            RequestBuilder.OnUpdate.Subscribe(-750, SelectTransitions);
            RequestBuilder.OnUpdate.Subscribe(100, ApplyConstraint);
        }

        private static void InstantiateManager(RequestBuilder rb)
        {
            manager = null;

            if (!ShouldRun(rb)) return;

            manager = new();
        }

        private static void SelectTransitions(RequestBuilder rb)
        {
            if (!ShouldRun(rb)) return;

            if (!rb.TryGetStage(RBConsts.MainTransitionStage, out StageBuilder sb))
            {
                // Insert stage at the start because it's probably a lot more restricted than the item placements,
                // unless area transitions are already randomized
                sb = rb.InsertStage(0, RBConsts.MainTransitionStage);
            }

            List<TransitionDef> newRandomizedTransitions = manager.GetNewRandomizedTransitions(rb);

            #region Drops
            TransitionGroupBuilder oneWayGroup;
            if (sb.TryGetGroup(RBConsts.OneWayGroup, out GroupBuilder oneWayGroup_prefab))
            {
                oneWayGroup = (TransitionGroupBuilder)oneWayGroup_prefab;
            }
            else
            {
                oneWayGroup = new()
                {
                    label = RBConsts.OneWayGroup,
                    stageLabel = RBConsts.MainTransitionStage,
                    strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy()
                };
                
                sb.Add(oneWayGroup);
            }

            List<string> oneWayInTrans = newRandomizedTransitions.Where(x => x.Sides == TransitionSides.OneWayIn).Select(x => x.Name).ToList();
            List<string> oneWayOutTrans = newRandomizedTransitions.Where(x => x.Sides == TransitionSides.OneWayOut).Select(x => x.Name).ToList();
            HashSet<string> oneWayAll = oneWayInTrans.Concat(oneWayOutTrans).AsHashSet();

            oneWayGroup.Sources.AddRange(oneWayInTrans);
            oneWayGroup.Targets.AddRange(oneWayOutTrans);
            foreach (string oneWay in oneWayAll)
            {
                rb.RemoveFromVanilla(oneWay);
            }

            rb.OnGetGroupFor.Subscribe(-999f, MatchedTryResolveDropGroup);

            bool MatchedTryResolveDropGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb)
            {
                if (type == RequestBuilder.ElementType.Transition)
                {
                    if (oneWayAll.Contains(item))
                    {
                        gb = oneWayGroup;
                        return true;
                    }
                }
                gb = default;
                return false;
            }
            #endregion

            newRandomizedTransitions = newRandomizedTransitions.Where(x => !oneWayAll.Contains(x.Name)).ToList();

            // TODO - assign to groups

            throw new NotImplementedException();
        }

        private static void ApplyConstraint(RequestBuilder rb)
        {
            if (!ShouldRun(rb)) return;
            if (!TrandoPlus.GS.EnforceTransitionGrouping) return;

            if (!rb.TryGetStage(RBConsts.MainTransitionStage, out StageBuilder sb)) return;

            foreach (GroupBuilder gb in sb.Groups)
            {
                if (gb.strategy is DefaultGroupPlacementStrategy dgps)
                {
                    dgps.Constraints += manager.TransitionGroupConstraint;
                }
            }
        }
    }
}
