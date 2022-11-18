using RandomizerCore.Randomization;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            throw new NotImplementedException();
        }

        private static void ApplyConstraint(RequestBuilder rb)
        {
            if (!ShouldRun(rb)) return;
            if (!TrandoPlus.GS.EnforceTransitionGrouping) return;

            foreach (GroupBuilder gb in rb.EnumerateTransitionGroups())
            {
                if (gb.strategy is DefaultGroupPlacementStrategy dgps)
                {
                    dgps.Constraints += manager.TransitionGroupConstraint;
                }
            }
        }
    }
}
