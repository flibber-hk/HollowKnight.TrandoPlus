using RandomizerCore;
using RandomizerCore.Extensions;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
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
            RequestBuilder.OnUpdate.Subscribe(100, ApplyConstraints);
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
            foreach (TransitionDef tDef in newRandomizedTransitions)
            {
                rb.RemoveFromVanilla(tDef.Name);
            }

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

            AddGroupResolver(rb, -999f, new(oneWayAll), oneWayGroup);
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
                AddGroupResolver(rb, -999f, new(twoWayTransitions), twoWayGroup);                
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
                        ((DefaultGroupPlacementStrategy)horizontalGroup.strategy).ConstraintList.Add(new DefaultGroupPlacementStrategy.Constraint(
                            NotDoorToDoor,
                            Label: "TrandoPlus: Not Door To Door"
                            ));
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

                List<TransitionDef> upDownTransitions = newRandomizedTransitions.Where(d => d.Direction == TransitionDirection.Top || d.Direction == TransitionDirection.Bot).ToList();
                List<TransitionDef> leftRightDoorTransitions = newRandomizedTransitions
                    .Where(d => d.Direction == TransitionDirection.Left || d.Direction == TransitionDirection.Right || d.Direction == TransitionDirection.Door)
                    .ToList();
                
                foreach (TransitionDef def in upDownTransitions)
                {
                    if (def.Direction == TransitionDirection.Top)
                    {
                        verticalGroup.Group2.Add(def.Name);
                    }
                    else if (def.Direction == TransitionDirection.Bot)
                    {
                        verticalGroup.Group1.Add(def.Name);
                    }
                }

                AddGroupResolver(rb, -999f, upDownTransitions.Select(x => x.Name).AsHashSet(), verticalGroup);

                List<string> lefts = leftRightDoorTransitions.Where(d => d.Direction == TransitionDirection.Left).Select(x => x.Name).OrderBy(x => x).ToList();
                List<string> rights = leftRightDoorTransitions.Where(d => d.Direction == TransitionDirection.Right).Select(x => x.Name).OrderBy(x => x).ToList();
                List<string> doors = leftRightDoorTransitions.Where(d => d.Direction == TransitionDirection.Door).Select(x => x.Name).OrderBy(x => x).ToList();

                rb.rng.PermuteInPlace(lefts);
                rb.rng.PermuteInPlace(rights);
                rb.rng.PermuteInPlace(doors);

                if (doors.Count > lefts.Count && !horizontalGroup.Group1.EnumerateDistinct().Any())
                {
                    // Has to be door rando; the unique door-right transition (bretta) has been randomized and no left-right transitions have been
                    foreach (string s in rights)
                    {
                        if (doors.Count < lefts.Count) doors.Add(s);
                        else lefts.Add(s);
                    }

                    horizontalGroup.Group1.AddRange(doors);
                    horizontalGroup.Group2.AddRange(lefts);
                }
                else
                {
                    foreach (string s in doors)
                    {
                        if (lefts.Count < rights.Count) lefts.Add(s);
                        else rights.Add(s);
                    }

                    horizontalGroup.Group1.AddRange(rights);
                    horizontalGroup.Group2.AddRange(lefts);
                }

                AddGroupResolver(rb, -999f, leftRightDoorTransitions.Select(x => x.Name).AsHashSet(), horizontalGroup);
            }
        }

        private static void ApplyConstraints(RequestBuilder rb)
        {
            if (!ShouldRun(rb)) return;

            if (!rb.TryGetStage(RBConsts.MainTransitionStage, out StageBuilder sb)) return;

            Func<string, string, bool> tpconstraint = null;
            string label = null;
            if (TrandoPlus.GS.EnforceTransitionGrouping && !TrandoPlus.GS.AllowInternalNonmatching)
            {
                tpconstraint = (s1, s2) => manager.TransitionGroupConstraint(s1, s2) && manager.InternalGroupConstraint(s1, s2);
                label = "TrandoPlus: ETG+AIN";
            }
            else if (TrandoPlus.GS.EnforceTransitionGrouping)
            {
                tpconstraint = manager.TransitionGroupConstraint;
                label = "TrandoPlus: ETG";
            }
            if (!TrandoPlus.GS.AllowInternalNonmatching)
            {
                tpconstraint = manager.InternalGroupConstraint;
                label = "TrandoPlus: AIN";
            }

            if (tpconstraint == null)
            {
                return;
            }

            foreach (GroupBuilder gb in sb.Groups.ToList())
            {
                if (gb.strategy is DefaultGroupPlacementStrategy dgps)
                {
                    if (gb is SymmetricTransitionGroupBuilder stgb)
                    {
                        ConstraintSplitting.ApplyConstraint(tpconstraint, label + " (Symmetric TGB)", stgb, sb);
                    }
                    else if (gb is SelfDualTransitionGroupBuilder sdtgb)
                    {
                        ConstraintSplitting.ApplyConstraint(tpconstraint, label + " (Self-Dual TGB)",sdtgb, sb);
                    }
                    else
                    {
                        dgps.ConstraintList.Add(new DefaultGroupPlacementStrategy.Constraint(
                            (item, loc) => tpconstraint(item.Name, loc.Name),
                            Label: label + " (Unrecognized TGB)"
                            ));
                    }
                }
            }
        }

        private static void AddGroupResolver(RequestBuilder rb, float priority, HashSet<string> matched, GroupBuilder group)
        {
            rb.OnGetGroupFor.Subscribe(priority, MatchedTryResolveGroup);

            bool MatchedTryResolveGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb)
            {
                if (type == RequestBuilder.ElementType.Transition)
                {
                    if (matched.Contains(item))
                    {
                        gb = group;
                        return true;
                    }
                }
                gb = default;
                return false;
            }
        }
    }
}
