using RandomizerCore.Randomization;
using RandomizerMod.RC;

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

            foreach (GroupBuilder gb in rb.EnumerateTransitionGroups())
            {
                if (gb.strategy is DefaultGroupPlacementStrategy dgps)
                {
                    dgps.Constraints += Conditions.AdjacentBenchConstraint;
                }
            }
        }
    }
}
