using RandomizerCore;
using RandomizerCore.Randomization;
using RandomizerMod.RC;
using System;

namespace TrandoPlus
{
    public static class ConditionManager
    {
        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(120f, PreventAdjacentBenches);
        }

        private static void PreventAdjacentBenches(RequestBuilder rb)
        {
            if (!TrandoPlus.GS.ProhibitAdjacentBenches) return;

            Func<IRandoItem, IRandoLocation, bool> condition = Conditions.GetAdjacentBenchConstraint(rb);

            foreach (GroupBuilder gb in rb.EnumerateTransitionGroups())
            {
                if (gb.strategy is DefaultGroupPlacementStrategy dgps)
                {
                    dgps.Constraints += condition;
                }
            }
        }
    }
}
