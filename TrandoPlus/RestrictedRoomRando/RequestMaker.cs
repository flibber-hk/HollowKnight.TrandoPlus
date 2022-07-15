using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemChanger;
using Modding;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RequestMaker
    {
        internal static SceneSelector selector;

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(-250, SelectTransitions);
            RequestBuilder.OnUpdate.Subscribe(250, ModifyItemsAndLocations);
        }

        private static void SelectTransitions(RequestBuilder rb)
        {
            selector = null;

            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }
            if (!TrandoPlus.GS.LimitedRoomRando)
            {
                return;
            }

            selector = new(rb);

            HashSet<string> selectedScenes = selector.Run(TrandoPlus.GS.LimitedRoomRandoFraction);

            HashSet<string> allTransitions = SceneSelector.GetTransitions(rb);

            foreach (string t in allTransitions)
            {
                if (!rb.TryGetTransitionDef(t, out TransitionDef def) || !selectedScenes.Contains(def.SceneName))
                {
                    rb.RemoveTransitionByName(t);
                }
            }

            // Fix vertical drops
            int dropEntries = allTransitions
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && selectedScenes.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayIn)
                .Count();

            List<string> dropExits = allTransitions
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && selectedScenes.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayOut)
                .OrderBy(t => t)
                .ToList();

            while (dropExits.Count > dropEntries)
            {
                string removedTransition = rb.rng.Next(dropExits.Where(x => !x.StartsWith("Fungus2_25")).ToList());
                dropExits.Remove(removedTransition);
                rb.RemoveTransitionByName(removedTransition);
            }
        }

        private static void ModifyItemsAndLocations(RequestBuilder rb)
        {
            if (selector is null) return;

            HashSet<string> selectedScenes = selector.SelectedScenes;
            foreach (string scene in selectedScenes)
            {
                TrandoPlus.instance.Log($" - SELECTED {scene}");
            }
            TrandoPlus.instance.Log($" = START {rb.ctx.StartDef.SceneName}");

            rb.RemoveLocationsWhere(l => !rb.TryGetLocationDef(l, out LocationDef def)
                || def.SceneName == null
                || !selectedScenes.Contains(def.SceneName));

            foreach ((string item, string scene) in SceneSelector.StagScenes)
            {
                if (!selectedScenes.Contains(scene))
                {
                    rb.RemoveItemByName(scene);
                }
            }

            if (ModHooks.GetMod("BenchRando") is Mod) RemoveBenches(rb, selectedScenes);
            if (ModHooks.GetMod("Randomizable Levers") is Mod) RemoveLevers(rb, selectedScenes);

            foreach (ItemGroupBuilder igb in rb.EnumerateItemGroups())
            {
                igb.LocationPadder = RandoPlus.AreaRestriction.AreaLimiterRequest.GetPadder(rb.rng, igb, selector.SelectedShop);
            }
        }

        private static void RemoveBenches(RequestBuilder rb, HashSet<string> selectedScenes)
        {
            if (!BenchRando.Rando.RandoInterop.IsEnabled())
            {
                return;
            }

            foreach (string bench in BenchRando.Rando.RandoInterop.LS.Benches)
            {
                string scene = BenchRando.BRData.BenchLookup[bench].SceneName;
                if (!selectedScenes.Contains(scene))
                {
                    rb.RemoveItemByName(bench);
                }
            }
        }

        private static void RemoveLevers(RequestBuilder rb, HashSet<string> selectedScenes)
        {
            if (!RandomizableLevers.RandomizableLevers.GS.RandoSettings.Any)
            {
                return;
            }

            // Removing useless levers would require some manual work for some of them, so
            // I probably won't. But the RG stag lever has to go
            if (!selectedScenes.Contains(SceneNames.RestingGrounds_09))
            {
                rb.RemoveItemByName(RandomizableLevers.LeverNames.Lever_Resting_Grounds_Stag);
            }
        }
    }
}
