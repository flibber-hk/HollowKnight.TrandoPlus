using System;
using System.Collections.Generic;
using System.Linq;
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
                if (rb.TryGetLocationDef(locationName, out LocationDef def))
                {
                    if (!string.IsNullOrEmpty(def.SceneName))
                    {
                        sel.SelectScene(def.SceneName);
                    }
                }
            }
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
