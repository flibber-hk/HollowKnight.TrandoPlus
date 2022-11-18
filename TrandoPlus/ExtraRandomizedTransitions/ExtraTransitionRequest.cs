using RandomizerCore;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
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

            if (rb.gs.TransitionSettings.TransitionMatching == RandomizerMod.Settings.TransitionSettings.TransitionMatchingSetting.NonmatchingDirections)
            {
                SelfDualTransitionGroupBuilder twoWayGroup;
                if (sb.TryGetGroup(RBConsts.TwoWayGroup, out GroupBuilder twoWayGroup_prefab))
                {
                    twoWayGroup = (SelfDualTransitionGroupBuilder)twoWayGroup_prefab;
                }
                else
                {
                    twoWayGroup = new()
                    {
                        label = RBConsts.TwoWayGroup,
                        stageLabel = RBConsts.MainTransitionStage,
                        coupled = rb.gs.TransitionSettings.Coupled,
                        strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy()
                    };
                    sb.Add(twoWayGroup);
                }

                HashSet<string> twoWayTransitions = new(newRandomizedTransitions.Select(x => x.Name));

                twoWayGroup.Transitions.AddRange(twoWayTransitions);
                foreach (string trans in twoWayTransitions)
                {
                    rb.RemoveFromVanilla(trans);
                }

                rb.OnGetGroupFor.Subscribe(-999f, MatchedTryResolveTwoWayGroup);

                bool MatchedTryResolveTwoWayGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb)
                {
                    if (type == RequestBuilder.ElementType.Transition)
                    {
                        if (twoWayTransitions.Contains(item))
                        {
                            gb = twoWayGroup;
                            return true;
                        }
                    }
                    gb = default;
                    return false;
                }
            }
            else
            {
                SymmetricTransitionGroupBuilder horizontalGroup;
                SymmetricTransitionGroupBuilder verticalGroup;

                if (sb.TryGetGroup(RBConsts.InLeftOutRightGroup, out GroupBuilder horizontalGroup_prefab))
                {
                    horizontalGroup = (SymmetricTransitionGroupBuilder)horizontalGroup_prefab;
                }
                else
                {
                    horizontalGroup = new()
                    {
                        label = RBConsts.InLeftOutRightGroup,
                        reverseLabel = RBConsts.InRightOutLeftGroup,
                        coupled = rb.gs.TransitionSettings.Coupled,
                        stageLabel = RBConsts.MainTransitionStage,
                        strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy()
                    };

                    sb.Add(horizontalGroup);

                    if (rb.gs.TransitionSettings.TransitionMatching == TransitionSettings.TransitionMatchingSetting.MatchingDirectionsAndNoDoorToDoor)
                    {
                        bool NotDoorToDoor(IRandoItem item, IRandoLocation location)
                        {
                            if (!rb.TryGetTransitionDef(item.Name, out TransitionDef t1)
                                || !rb.TryGetTransitionDef(location.Name, out TransitionDef t2))
                            {
                                return true;
                            }

                            return t1.Direction != TransitionDirection.Door || t2.Direction != TransitionDirection.Door;
                        }
                        ((DefaultGroupPlacementStrategy)horizontalGroup.strategy).Constraints += NotDoorToDoor;
                    }
                }

                if (sb.TryGetGroup(RBConsts.InTopOutBotGroup, out GroupBuilder verticalGroup_prefab))
                {
                    verticalGroup = (SymmetricTransitionGroupBuilder)verticalGroup_prefab;
                }
                else
                {
                    verticalGroup = new()
                    {
                        label = RBConsts.InTopOutBotGroup,
                        reverseLabel = RBConsts.InBotOutTopGroup,
                        coupled = rb.gs.TransitionSettings.Coupled,
                        stageLabel = RBConsts.MainTransitionStage,
                        strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy()
                    };

                    sb.Add(verticalGroup);
                }

            }

            // Add transitions
            // Remove from vanilla
            // Handle bretta/non-door matching
            // Group resolver
            // Start location shenaniganry
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
