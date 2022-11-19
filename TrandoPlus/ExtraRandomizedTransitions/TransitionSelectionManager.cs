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
            new HubRandoTransitionSelector(),
        };

        private IEnumerable<TransitionSelector> Selectors => selectors.Where(x => x.IsEnabled());

        // Used for the constraint
        private List<HashSet<string>> GroupedRandomizedTransitions = new();
        private HashSet<string> AllTransitions => GroupedRandomizedTransitions.SelectMany(x => x).AsHashSet();

        /// <summary>
        /// Constraint to be added to the transition groups.
        /// 
        /// Explanation: This will only return false if item and loc are both transitions that have
        /// been randomized, but not in the same collection.
        /// </summary>
        public bool TransitionGroupConstraint(IRandoItem item, IRandoLocation loc)
        {
            foreach (HashSet<string> group in GroupedRandomizedTransitions)
            {
                if (group.Contains(item.Name) && group.Contains(loc.Name))
                {
                    return true;
                }
            }

            HashSet<string> allTransitions = AllTransitions;
            if (!allTransitions.Contains(item.Name) || !allTransitions.Contains(loc.Name))
            {
                return true;
            }

            return false;
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
            CollectTransitions(rb, out List<TransitionDef> vanilla, out List<TransitionDef> alreadyRandomized);
            IReadOnlyCollection<TransitionDef> availableTransitions = vanilla.Concat(alreadyRandomized).ToList().AsReadOnly();
            GroupedRandomizedTransitions.Add(alreadyRandomized.Select(x => x.Name).AsHashSet());

            HashSet<TransitionDef> newTransitions = new();

            foreach (TransitionSelector selector in Selectors)
            {
                List<TransitionDef> selectedTransitions = selector.SelectRandomizedTransitions(availableTransitions);
                HashSet<TransitionDef> allSelectedTransitions = new(selectedTransitions);
                foreach (TransitionDef def in selectedTransitions)
                {
                    // def.VanillaTarget is null if it's a OneWayOut transition - hope that in this case, the
                    // selector has selected the source transition
                    if (def.VanillaTarget != null && rb.TryGetTransitionDef(def.VanillaTarget, out TransitionDef target))
                    {
                        allSelectedTransitions.Add(target);
                    }
                }

                GroupedRandomizedTransitions.Add(allSelectedTransitions.Select(x => x.Name).AsHashSet());
                newTransitions.UnionWith(allSelectedTransitions);
            }

            return newTransitions.Where(x => !alreadyRandomized.Contains(x)).ToList();
        }
    }
}
