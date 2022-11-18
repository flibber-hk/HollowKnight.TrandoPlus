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
    public class TransitionSelectionManager
    {
        public List<TransitionSelector> Selectors = new()
        {
            new DoorRandoTransitionSelector(),
            new DropRandoTransitionSelector(),
            new DeadEndRandoTransitionSelector(),
        };

        // Used for the constraint
        private List<HashSet<string>> GroupedRandomizedTransitions = new();

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
                    if (rb.TryGetTransitionDef(def.VanillaTarget, out TransitionDef target))
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
