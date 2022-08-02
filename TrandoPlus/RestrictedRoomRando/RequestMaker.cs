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

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(200, InstantiateSceneSelector);

            RequestBuilder.OnUpdate.Subscribe(250, SelectScenes);
        }

        // Instantiate scene selector early, so that people have the opportunity to add constraints and callbacks to it if necessary.
        private static void InstantiateSceneSelector(RequestBuilder rb)
        {
            Selector = null;

            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }
            if (!RoomRemovalManager.Config.AnySceneRemoval)
            {
                return;
            }

            Selector = new(rb);
        }

        private static void SelectScenes(RequestBuilder rb)
        {
            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }
            if (!RoomRemovalManager.Config.AnySceneRemoval)
            {
                return;
            }

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

            List<string> benchScenes = Utility.GetBenchScenes(rb);            
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
        /// </summary>
        private static string GetSceneForLocation(string locationName, RequestBuilder rb)
        {
            if (rb.TryGetLocationDef(locationName, out LocationDef def) && !string.IsNullOrEmpty(def.SceneName))
            {
                return def.SceneName;
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

                int gen;
                unchecked
                {
                    gen = 163 * rb.gs.Seed + locationName.GetStableHashCode();
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
            TrandoPlus.instance.Log($"{sel.SelectedSceneCount} - {sel.TotalSceneCount} - {RoomRemovalManager.Config.RandomRoomsFraction * sel.TotalSceneCount + 5}");

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
