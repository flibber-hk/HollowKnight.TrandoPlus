using RandomizerCore.Randomization;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrandoPlus.ExtraRandomizedTransitions
{
    internal static class ConstraintSplitting
    {
        /// <summary>
        /// Given a constraint, apply it to the group, splitting into components if necessary.
        /// </summary>
        public static void ApplyConstraint(
            Func<string, string, bool> constraint,
            SymmetricTransitionGroupBuilder stgb,
            StageBuilder stage)
        {
            if (stgb.strategy is not DefaultGroupPlacementStrategy dgps)
            {
                return;
            }

            dgps.Constraints += (item, loc) => constraint(item.Name, loc.Name);

            List<string> Group1 = stgb.Group1.EnumerateWithMultiplicity().ToList();
            List<string> Group2 = stgb.Group2.EnumerateWithMultiplicity().ToList();

            if (!TryMakeEquivalenceClasses(Group1, Group2, constraint, stgb.label, out var classes))
            {
                TrandoPlus.instance.LogDebug($"SYM {stgb.label}: No Equivalence classes");

                return;
            }

            if (classes.Count == 1)
            {
                TrandoPlus.instance.LogDebug($"SYM {stgb.label}: Classes count is 1");

                return;
            }

            if (classes.Count == 0)
            {
                TrandoPlus.instance.LogWarn($"SYM {stgb.label}: Classes count is 0 wtf");

                return;
            }

            // Clear the stgb

            foreach (string s in Group1)
            {
                stgb.Group1.RemoveAll(s);
            }
            foreach (string t in Group2)
            {
                stgb.Group2.RemoveAll(t);
            }
            stgb.Group1.AddRange(classes[0].Item1);
            stgb.Group2.AddRange(classes[0].Item2);

            int counter = 1;
            foreach (var pair in classes.Skip(1))
            {
                SymmetricTransitionGroupBuilder newStgb = new()
                {
                    label = stgb.label + $"_{counter}",
                    reverseLabel = stgb.reverseLabel + $"_{counter}",
                    coupled = stgb.coupled,
                    stageLabel = stgb.stageLabel,
                    strategy = dgps.Clone(),
                    onPermute = stgb.onPermute,
                };
                newStgb.Group1.AddRange(pair.Item1);
                newStgb.Group2.AddRange(pair.Item2);
                counter += 1;
                stage.Add(newStgb);
            }
        }

        /// <summary>
        /// Split the bipartite graph into components induced by the selector.
        /// </summary>
        public static bool TryMakeEquivalenceClasses(
            List<string> Group1,
            List<string> Group2,
            Func<string, string, bool> selector,
            string message,
            out List<(List<string>, List<string>)> classes)
        {
            List<(List<string>, List<string>)> accumulator = new();
            List<string> group1 = new(Group1);
            List<string> group2 = new(Group2);

            while (group1.Count > 0)
            {
                List<string> from1 = new();
                List<string> from2 = new();

                List<string> active1 = new();
                List<string> active2 = new();
                active1.Add(group1[0]);
                group1.RemoveAt(0);

                while (active1.Count > 0)
                {
                    active2 = group2.Where(y => active1.Where(x => selector(x, y)).Any()).ToList();
                    group2 = group2.Except(active2).ToList();
                    from1.AddRange(active1);

                    active1 = group1.Where(x => active2.Where(y => selector(x, y)).Any()).ToList();
                    group1 = group1.Except(active1).ToList();
                    from2.AddRange(active2);
                }

                if (from1.Count != from2.Count)
                {
                    TrandoPlus.instance.LogWarn($"Failed to split group {message} due to imbalanced counts");
                    classes = null;
                    return false;
                }

                accumulator.Add((new(from1), new(from2)));
                from1.Clear();
                from2.Clear();
            }

            classes = accumulator;
            return true;
        }

        /// <summary>
        /// Apply the constraint to the group, splitting up the group if possible.
        /// </summary>
        public static void ApplyConstraint(
            Func<string, string, bool> constraint,
            SelfDualTransitionGroupBuilder sdtgb,
            StageBuilder stage)
        {
            if (sdtgb.strategy is not DefaultGroupPlacementStrategy dgps)
            {
                return;
            }

            dgps.Constraints += (item, loc) => constraint(item.Name, loc.Name);

            List<string> Transitions = sdtgb.Transitions.EnumerateWithMultiplicity().ToList();

            if (!TryMakeBipartition(Transitions, constraint, sdtgb.label, out List<(List<string>, List<string>)> classes))
            {
                TrandoPlus.instance.LogDebug($"SD {sdtgb.label}: No Equivalence classes");

                return;
            }

            if (classes.Count == 0)
            {
                TrandoPlus.instance.LogWarn($"SD {sdtgb.label}: Classes count is 0 wtf");

                return;
            }

            // Clear the sdtgb

            foreach (string s in Transitions)
            {
                sdtgb.Transitions.RemoveAll(s);
            }

            int counter = 0;
            foreach (var pair in classes)
            {
                SymmetricTransitionGroupBuilder stgb = new()
                {
                    label = sdtgb.label + $"_{counter}",
                    reverseLabel = sdtgb.label + $"-rev_{counter}",
                    coupled = sdtgb.coupled,
                    stageLabel = sdtgb.stageLabel,
                    strategy = dgps.Clone(),
                    onPermute = sdtgb.onPermute,
                };
                stgb.Group1.AddRange(pair.Item1);
                stgb.Group2.AddRange(pair.Item2);
                counter += 1;
                stage.Add(stgb);
            }
        }

        /// <summary>
        /// If the graph induced by the selector is bipartite, return a list of partitioned components.
        /// 
        /// The selector should be symmetric (f(x,y) == f(y,x) for all x, y)
        /// </summary>
        public static bool TryMakeBipartition(
            List<string> transitions,
            Func<string, string, bool> selector,
            string message,
            out List<(List<string>, List<string>)> classes)
        {
            classes = new();

            List<string> remaining = new(transitions);
            List<string> active = new();
            List<string> group1 = new();
            List<string> group2 = new();

            while (remaining.Count > 0)
            {
                active.Add(remaining[0]);
                remaining.RemoveAt(0);

                while (active.Count > 0)
                {
                    group1.AddRange(active);
                    active = remaining.Where(x => active.Where(y => selector(x, y)).Any()).ToList();
                    remaining = remaining.Except(active).ToList();

                    group2.AddRange(active);
                    active = remaining.Where(x => active.Where(y => selector(x, y)).Any()).ToList();
                    remaining = remaining.Except(active).ToList();
                }

                foreach (string x in group1)
                {
                    foreach (string y in group1)
                    {
                        if (selector(x, y))
                        {
                            classes = null;
                            TrandoPlus.instance.LogDebug($"SDTGB {message} not bipartite");
                            return false;
                        }
                    }
                }

                foreach (string x in group2)
                {
                    foreach (string y in group2)
                    {
                        if (selector(x, y))
                        {
                            classes = null;
                            TrandoPlus.instance.LogDebug($"SDTGB {message} not bipartite");
                            return false;
                        }
                    }
                }

                if (group1.Count != group2.Count)
                {
                    classes = null;
                    TrandoPlus.instance.LogDebug($"SDTGB {message} unequal counts in component");
                    return false;
                }

                classes.Add((new(group1), new(group2)));
                group1.Clear();
                group2.Clear();
            }

            return true;
        }
    }
}
