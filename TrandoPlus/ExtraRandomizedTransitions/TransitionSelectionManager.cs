using RandomizerCore;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.Linq;
using TrandoPlus.Utils;

namespace TrandoPlus.ExtraRandomizedTransitions
{
    public class TransitionSelectionManager
    {
        private List<TransitionSelector> selectors = new()
        {
            new DoorRandoTransitionSelector(),
            new DropRandoTransitionSelector(),
            new DeadEndRandoTransitionSelector(),
        };

        private IEnumerable<TransitionSelector> Selectors => selectors.Where(x => x.IsEnabled());

        // Used for the inter-group constraint
        private List<HashSet<string>> GroupedRandomizedTransitions = new();

        // Used for the intra-group constraint
        private List<HashSet<string>> InternalGroupedTransitions = new();

        private HashSet<string> AllTransitions => GroupedRandomizedTransitions.SelectMany(x => x).AsHashSet();

        /// <summary>
        /// Constraint to be added to the transition groups.
        /// 
        /// Explanation: This will only return false if item and loc are both transitions that have
        /// been randomized, but not in the same collection.
        /// </summary>
        public bool TransitionGroupConstraint(string itemName, string locName)
        {
            foreach (HashSet<string> group in GroupedRandomizedTransitions)
            {
                if (group.Contains(itemName) && group.Contains(locName))
                {
                    return true;
                }
            }

            HashSet<string> allTransitions = AllTransitions;
            if (!allTransitions.Contains(itemName) || !allTransitions.Contains(locName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Constraint to be added to the transition groups.
        /// 
        /// Explanation: This will only return false if there is a selector which explicitly declares item and loc but neither
        /// of their targets, or declares the vanilla targets of item and loc but neither item or loc themselves.
        /// 
        /// For example, in dead end rando will return false unless one is a dead end and the other is the target of a dead end.
        /// </summary>
        public bool InternalGroupConstraint(string itemName, string locName)
        {
            foreach (HashSet<string> group in InternalGroupedTransitions)
            {
                if (group.Contains(itemName) && group.Contains(locName))
                {
                    return false;
                }
            }

            return true;
        }

        public static void CollectTransitions(RequestBuilder rb, out List<TransitionDef> vanilla, out List<TransitionDef> alreadyRandomized)
        {
            HashSet<TransitionDef> vanillaSet = new();
            HashSet<string> alreadyRandomizedNames = new();

            foreach (VanillaDef def in rb.Vanilla.SelectMany(x => x.Value))
            {
                if (rb.TryGetTransitionDef(def.Item, out TransitionDef iDef))
                {
                    vanillaSet.Add(iDef);
                }
                if (rb.TryGetTransitionDef(def.Location, out TransitionDef lDef))
                {
                    vanillaSet.Add(lDef);
                }
            }

            vanilla = vanillaSet.ToList();

            foreach (TransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<TransitionGroupBuilder>())
            {
                foreach (string t in tgb.Sources.EnumerateDistinct())
                {
                    alreadyRandomizedNames.Add(t);
                }
                foreach (string t in tgb.Targets.EnumerateDistinct())
                {
                    alreadyRandomizedNames.Add(t);
                }
            }
            foreach (SymmetricTransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<SymmetricTransitionGroupBuilder>())
            {
                foreach (string t in tgb.Group1.EnumerateDistinct())
                {
                    alreadyRandomizedNames.Add(t);
                }
                foreach (string t in tgb.Group2.EnumerateDistinct())
                {
                    alreadyRandomizedNames.Add(t);
                }
            }
            foreach (SelfDualTransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<SelfDualTransitionGroupBuilder>())
            {
                foreach (string t in tgb.Transitions.EnumerateDistinct())
                {
                    alreadyRandomizedNames.Add(t);
                }
            }

            alreadyRandomized = new();

            foreach (string t in alreadyRandomizedNames)
            {
                if (rb.TryGetTransitionDef(t, out TransitionDef def))
                {
                    alreadyRandomized.Add(def);
                }
            }
        }

        public List<TransitionDef> GetNewRandomizedTransitions(RequestBuilder rb)
        {
            GroupedRandomizedTransitions.Clear();
            InternalGroupedTransitions.Clear();

            CollectTransitions(rb, out List<TransitionDef> vanilla, out List<TransitionDef> alreadyRandomized);
            IReadOnlyCollection<TransitionDef> availableTransitions = vanilla.Concat(alreadyRandomized).ToList().AsReadOnly();
            GroupedRandomizedTransitions.Add(alreadyRandomized.Select(x => x.Name).AsHashSet());

            HashSet<TransitionDef> newTransitions = new();

            foreach (TransitionSelector selector in Selectors)
            {
                List<TransitionDef> selectedTransitions = selector.SelectRandomizedTransitions(availableTransitions);

                HashSet<TransitionDef> vanillaTargets = new();
                foreach (TransitionDef def in selectedTransitions)
                {
                    // def.VanillaTarget is null if it's a OneWayOut transition - hope that in this case, the
                    // selector has selected the source transition
                    if (def.VanillaTarget != null && rb.TryGetTransitionDef(def.VanillaTarget, out TransitionDef target))
                    {
                        vanillaTargets.Add(target);
                    }
                }

                HashSet<TransitionDef> allSelectedTransitions = selectedTransitions.Union(vanillaTargets).AsHashSet();

                GroupedRandomizedTransitions.Add(allSelectedTransitions.Select(x => x.Name).AsHashSet());
                if (selector.ProvidesInternalConstraint())
                {
                    HashSet<string> declared = selectedTransitions.Where(x => !vanillaTargets.Contains(x)).Select(x => x.Name).AsHashSet();
                    HashSet<string> matched = vanillaTargets.Where(x => !selectedTransitions.Contains(x)).Select(x => x.Name).AsHashSet();

                    InternalGroupedTransitions.Add(declared);
                    InternalGroupedTransitions.Add(matched);
                }

                newTransitions.UnionWith(allSelectedTransitions);
            }

            return newTransitions.Where(x => !alreadyRandomized.Contains(x)).ToList();
        }
    }
}
