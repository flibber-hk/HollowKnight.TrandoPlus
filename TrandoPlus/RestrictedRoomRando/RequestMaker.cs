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
        public static SceneSelector Selector;

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(250, SelectTransitions);
        }

        private static void SelectTransitions(RequestBuilder rb)
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
            Action<RequestBuilder, SceneSelector> sceneSelectionAction = null;

            if (TrandoPlus.GS.LimitedRoomRando)
            {
                sceneSelectionAction += AddLimitedRoomRandoScenes;
            }

            if (TrandoPlus.GS.RemoveEmptyRooms)
            {
                sceneSelectionAction += AddRoomsWithItems;
            }

            Selector.Run(sceneSelectionAction);
            Selector.Apply();

            if (TrandoPlus.GS.LimitedRoomRando)
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
            while (sel.SelectedSceneCount < TrandoPlus.GS.LimitedRoomRandoFraction * sel.SceneCount - 5)
            {
                List<string> availableScenes = sel.AvailableSceneNames.OrderBy(x => x).ToList();
                sel.SelectScene(rb.rng.Next(availableScenes));
            }
        }
    }
}
