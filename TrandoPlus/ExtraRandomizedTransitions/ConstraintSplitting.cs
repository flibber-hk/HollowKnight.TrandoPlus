using RandomizerCore.Randomization;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrandoPlus.ExtraRandomizedTransitions
{
    internal static class ConstraintSplitting
    {
        public static void ApplyConstraint(
            Func<string, string, bool> constraint,
            SymmetricTransitionGroupBuilder stgb,
            StageBuilder stage
            )
        {
            if (stgb.strategy is not DefaultGroupPlacementStrategy dgps)
            {
                return;
            }

            List<string> Group1 = stgb.Group1.EnumerateWithMultiplicity().ToList();
            List<string> Group2 = stgb.Group2.EnumerateWithMultiplicity().ToList();

            if (!TryMakeEquivalenceClasses(Group1, Group2, constraint, stgb.label, out var classes))
            {
                TrandoPlus.instance.LogDebug($"{stgb.label}: No Equivalence classes");

                return;
            }

            if (classes.Count == 1)
            {
                TrandoPlus.instance.LogDebug($"{stgb.label}: Classes count is 1");

                return;
            }

            if (classes.Count == 0)
            {
                TrandoPlus.instance.LogWarn($"{stgb.label}: Classes count is 0 wtf");

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
                    strategy = dgps,
                    onPermute = stgb.onPermute,
                };
                newStgb.Group1.AddRange(pair.Item1);
                newStgb.Group2.AddRange(pair.Item2);
                counter += 1;
                stage.Add(newStgb);
            }

            dgps.Constraints += (item, loc) => constraint(item.Name, loc.Name);
        }

        public static bool TryMakeEquivalenceClasses(
            List<string> Group1,
            List<string> Group2,
            Func<string, string, bool> selector,
            string message,
            out List<(List<string>, List<string>)> classes
            )
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
    }
}
