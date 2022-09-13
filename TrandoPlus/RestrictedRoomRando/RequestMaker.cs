using System;
using System.Collections.Generic;
using System.Linq;
using ConnectionMetadataInjector;
using ItemChanger;
using ItemChanger.Extensions;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using TrandoPlus.Utils;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RequestMaker
    {
        public static SceneSelector Selector { get; set; }

        public static bool IsActive(RequestBuilder rb)
        {
            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return false;
            }
            if (!RoomRemovalManager.Config.AnySceneRemoval)
            {
                return false;
            }
            return true;
        }

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(200, InstantiateSceneSelector);

            RequestBuilder.OnUpdate.Subscribe(250, SelectScenes);
        }

        // Instantiate scene selector early, so that people have the opportunity to add constraints and callbacks to it if necessary.
        private static void InstantiateSceneSelector(RequestBuilder rb)
        {
            Selector = null;

            if (!IsActive(rb)) return;

            Selector = new(rb);
        }

        private static void SelectScenes(RequestBuilder rb)
        {
            if (!IsActive(rb)) return;

            if (RoomRemovalManager.Config.RemoveEmptyRooms)
            {
                Selector.OnSceneSelectorRun.Subscribe(-10f, AddRoomsWithItems);
            }

            // If remove empty rooms is disabled, then random rooms will have to be removing rather
            // than adding rooms.
            bool arbitraryScenesRemoved = !RoomRemovalManager.Config.RemoveEmptyRooms;
            void RecordRemoved(string scene) => arbitraryScenesRemoved = true;

            if (RoomRemovalManager.Config.RemoveRandomRooms)
            {
                Selector.OnSceneSelectorRun.Subscribe(0f, (rb, ss) => ss.OnRemoveScene += RecordRemoved);
                Selector.OnSceneSelectorRun.Subscribe(0f, AddLimitedRoomRandoScenes);
                Selector.OnSceneSelectorRun.Subscribe(0f, (rb, ss) => ss.OnRemoveScene -= RecordRemoved);
            }

            if (RoomRemovalManager.Config.EnsureBenchRooms)
            {
                Selector.OnSceneSelectorRun.Subscribe(100f, AddBenches);
            }

            Selector.Run();
            Selector.Apply(rb);

            if (arbitraryScenesRemoved)
            {
                ApplyPadders(rb);
            }
        }

        private static void AddBenches(RequestBuilder rb, SceneSelector sel)
        {
            int selectedScenesCount = sel.SelectedSceneCount;
            int totalScenes = sel.TotalSceneCount;

            List<string> benchScenes = Utility.GetBenchScenes(rb).Where(scene => sel.AllSceneNames.Contains(scene)).ToList();            
            int totalBenchScenes = benchScenes.Count;

            while (benchScenes.Where(scene => sel.SelectedSceneNames.Contains(scene)).Count() < totalBenchScenes * selectedScenesCount / totalScenes)
            {
                sel.SelectScene(rb.rng.Next(benchScenes.Where(scene => !sel.SelectedSceneNames.Contains(scene)).OrderBy(s => s).ToList()));
            }
        }

        private static void AddRoomsWithItems(RequestBuilder rb, SceneSelector sel)
        {
            foreach (string locationName in rb.EnumerateItemGroups().SelectMany(gb => gb.Locations.EnumerateDistinct()))
            {
                string scene = GetSceneForLocation(locationName, rb);
                if (!string.IsNullOrEmpty(scene))
                {
                    sel.SelectScene(scene);
                }
            }
        }

        private static readonly MetadataProperty<AbstractLocation, IEnumerable<string>> SceneNamesProperty =
            new("SceneNames", icLoc => icLoc.sceneName?.Yield() ?? Enumerable.Empty<string>());


        /// <summary>
        /// Return a scene containing the given location.
        /// If a location is in multiple scenes, returns one chosen at random in a stable way;
        /// it only depends on the location name and the seed, and does not advance the RequestBuilder's rng.
        /// For journal entry locations - we ensure that we return the least likely scene to be present
        /// for non-respawning enemies (Hornet, Baldur and Kingsmould) and ensure that the entry and note locations
        /// choose the same scene.
        /// </summary>
        private static string GetSceneForLocation(string locationName, RequestBuilder rb)
        {
            if (rb.TryGetLocationDef(locationName, out LocationDef def) && !string.IsNullOrEmpty(def.SceneName))
            {
                return def.SceneName;
            }

            switch (locationName)
            {
                case "Journal_Entry-Hornet":
                case "Hunter's_Notes-Hornet":
                    return SceneNames.Deepnest_East_Hornet;
                case "Journal_Entry-Elder_Baldur":
                case "Hunter's_Notes-Elder_Baldur":
                    return SceneNames.Crossroads_11_alt;
                case "Journal_Entry-Kingsmould":
                case "Hunter's_Notes-Kingsmould":
                    return SceneNames.White_Palace_02;
            }

            AbstractLocation icLoc = Finder.GetLocation(locationName);
            if (icLoc != null)
            {
                List<string> sceneNames = SupplementalMetadata.Of(icLoc).Get(SceneNamesProperty)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                if (sceneNames.Count == 0)
                {
                    return null;
                }

                string seedName = locationName;
                if (seedName.StartsWith("Hunter's_Notes"))
                {
                    seedName = "Journal_Entry" + seedName.Substring("Hunter's_Notes".Length);
                }

                int gen;
                unchecked
                {
                    gen = 163 * rb.gs.Seed + seedName.GetStableHashCode();
                }

                Random rng = new(gen);
                return rng.Next(sceneNames);
            }

            return null;
        }

        private static void ApplyPadders(RequestBuilder rb)
        {
            foreach (ItemGroupBuilder igb in rb.EnumerateItemGroups())
            {
                igb.LocationPadder = RandoPlus.AreaRestriction.AreaLimiterRequest.GetPadder(rb.rng, igb, Selector.SelectedShop);
            }
        }

        private static void AddLimitedRoomRandoScenes(RequestBuilder rb, SceneSelector sel)
        {
            while (sel.SelectedSceneCount < RoomRemovalManager.Config.RandomRoomsFraction * sel.TotalSceneCount - 5)
            {
                List<string> availableScenes = sel.AvailableSceneNames.OrderBy(x => x).ToList();
                sel.SelectScene(rb.rng.Next(availableScenes));
            }

            while (sel.SelectedSceneCount > RoomRemovalManager.Config.RandomRoomsFraction * sel.TotalSceneCount + 5)
            {
                List<string> selectedScenes = sel.SelectedSceneNames.OrderBy(x => x).ToList();
                sel.RemoveScene(rb.rng.Next(selectedScenes));
            }
        }
    }
}
