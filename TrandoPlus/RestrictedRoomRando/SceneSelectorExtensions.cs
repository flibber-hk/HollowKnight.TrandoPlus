﻿using System;
using System.Collections.Generic;
using System.Linq;
using ConnectionMetadataInjector;
using ItemChanger;
using ItemChanger.Extensions;
using Modding;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class SceneSelectorExtensions
    {
        /// <summary>
        /// Apply the list of selected scenes to the request builder, by removing transitions. Does not mutate the selector.
        /// </summary>
        public static void Apply(this SceneSelector selector, RequestBuilder rb)
        {
            // Remove transitions from a missing scene
            foreach (string trans in selector.AllTransitionNames)
            {
                if (rb.TryGetTransitionDef(trans, out TransitionDef def) && !selector.SelectedSceneNames.Contains(def.SceneName))
                {
                    rb.RemoveTransitionByName(trans);
                }
            }

            // Make sure that one way drops are balanced
            int dropEntries = selector.AllTransitionNames
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && selector.SelectedSceneNames.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayIn)
                .Count();
            List<string> dropExits = selector.AllTransitionNames
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && selector.SelectedSceneNames.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayOut)
                .OrderBy(t => t)
                .ToList();
            while (dropExits.Count > dropEntries)
            {
                string removedTransition = rb.rng.Next(dropExits.Where(x => !x.StartsWith(SceneNames.Fungus2_25)).ToList());
                dropExits.Remove(removedTransition);
                rb.RemoveTransitionByName(removedTransition);
            }

            // Remove locations
            rb.RemoveLocationsWhere(l => !CanAppear(l, rb, selector));

            // Remove stag items if their targets do not exist
            foreach ((string item, string scene) in SceneSelector.StagScenes)
            {
                if (!selector.SelectedSceneNames.Contains(scene))
                {
                    rb.RemoveItemByName(item);
                }
            }

            // Remove bench items if their targets do not exist
            if (ModHooks.GetMod("BenchRando") is Mod) RemoveBenches(rb, selector.SelectedSceneNames);
            // Remove levers that unlock stags if necessary
            if (ModHooks.GetMod("RandomizableLevers") is Mod) RemoveLevers(rb, selector.SelectedSceneNames);
        }

        private static readonly MetadataProperty<AbstractLocation, IEnumerable<string>> SceneNamesProperty = 
            new("SceneNames", icLoc => icLoc.sceneName?.Yield() ?? Enumerable.Empty<string>());

        public static bool CanAppear(string loc, RequestBuilder rb, SceneSelector sel)
        {
            if (rb.TryGetLocationDef(loc, out LocationDef def) && !string.IsNullOrEmpty(def.SceneName))
            {
                return sel.SelectedSceneNames.Contains(def.SceneName);
            }

            AbstractLocation icLoc = Finder.GetLocation(loc);

            
            switch (loc)
            {
                // For these enemies, their note location requires colo access
                case "Hunter's_Notes-Gruz_Mother":
                case "Hunter's_Notes-Vengefly_King":
                    return sel.SelectedSceneNames.Contains(SceneNames.Room_Colosseum_01);
                // For these non-respawning enemies, if any of their scenes is missing we will
                // derandomize the hunter's note location.
                case "Hunter's_Notes-Hornet":
                case "Hunter's_Notes-Elder_Baldur":
                case "Hunter's_Notes-Kingsmould":
                case "Hunter's_Notes-Crystal_Guardian":
                case "Hunter's_Notes-Bluggsac":
                    if (SupplementalMetadata.Of(icLoc).Get(SceneNamesProperty).Any(x => !sel.SelectedSceneNames.Contains(x)))
                    {
                        return false;
                    }
                    break;
            }

            if (icLoc != null)
            {
                return SupplementalMetadata.Of(icLoc).Get(SceneNamesProperty).Any(x => sel.SelectedSceneNames.Contains(x));
            }

            return false;
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

            // TODO - remove all useless levers

            if (!selectedScenes.Contains(SceneNames.RestingGrounds_09))
            {
                rb.RemoveItemByName(RandomizableLevers.LeverNames.Lever_Resting_Grounds_Stag);
            }
            if (!selectedScenes.Contains(SceneNames.Room_Town_Stag_Station))
            {
                rb.RemoveItemByName(RandomizableLevers.LeverNames.Switch_Dirtmouth_Stag);
            }
        }
    }
}
