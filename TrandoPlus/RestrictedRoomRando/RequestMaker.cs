using System;
using System.Collections.Generic;
using System.Linq;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RequestMaker
    {
        public static SceneSelector Selector { get; set; }

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(200, InstantiateSceneSelector);

            RequestBuilder.OnUpdate.Subscribe(250, SelectTransitions);
        }

        // Instantiate scene selector early, so that people have the opportunity to add constraints and callbacks to it if necessary.
        private static void InstantiateSceneSelector(RequestBuilder rb)
        {
            Selector = null;

            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }
            if (!TrandoPlus.GS.AnySceneRemoval)
            {
                return;
            }

            Selector = new(rb);
        }

        private static void SelectTransitions(RequestBuilder rb)
        {
            if (rb.gs.TransitionSettings.Mode != RandomizerMod.Settings.TransitionSettings.TransitionMode.RoomRandomizer)
            {
                return;
            }
            if (!TrandoPlus.GS.AnySceneRemoval)
            {
                return;
            }

            if (TrandoPlus.GS.RemoveEmptyRooms)
            {
                Selector.OnSceneSelectorRun.Subscribe(-10f, AddRoomsWithItems);
            }

            bool arbitraryScenesRemoved = !TrandoPlus.GS.RemoveEmptyRooms;
            void RecordRemoved(string scene) => arbitraryScenesRemoved = true;

            if (TrandoPlus.GS.LimitedRoomRandoPlayable)
            {
                Selector.OnSceneSelectorRun.Subscribe(0f, (rb, ss) => ss.OnRemoveScene += RecordRemoved);
                Selector.OnSceneSelectorRun.Subscribe(0f, AddLimitedRoomRandoScenes);
                Selector.OnSceneSelectorRun.Subscribe(0f, (rb, ss) => ss.OnRemoveScene -= RecordRemoved);
            }

            Selector.Run();
            Selector.Apply(rb);

            if (TrandoPlus.GS.LimitedRoomRandoPlayable && arbitraryScenesRemoved)
            {
                ApplyPadders(rb);
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
            TrandoPlus.instance.Log($"{sel.SelectedSceneCount} - {sel.TotalSceneCount} - {TrandoPlus.GS.LimitedRoomRandoFraction * sel.TotalSceneCount + 5}");

            while (sel.SelectedSceneCount < TrandoPlus.GS.LimitedRoomRandoFraction * sel.TotalSceneCount - 5)
            {
                List<string> availableScenes = sel.AvailableSceneNames.OrderBy(x => x).ToList();
                sel.SelectScene(rb.rng.Next(availableScenes));
            }

            while (sel.SelectedSceneCount > TrandoPlus.GS.LimitedRoomRandoFraction * sel.TotalSceneCount + 5)
            {
                List<string> selectedScenes = sel.SelectedSceneNames.OrderBy(x => x).ToList();
                sel.RemoveScene(rb.rng.Next(selectedScenes));
            }
        }
    }
}
