using Newtonsoft.Json;

namespace TrandoPlus
{
    public class GlobalSettings
    {
        public bool RandomizeDoors = false;
        public bool AreaDoorNonInteraction = false;
        public bool RandomizeDrops = false;
        public bool ProhibitAdjacentBenches = false;

        public LimitedRoomRandoConfig LimitedRoomRandoConfig = new();
    }

    public class LimitedRoomRandoConfig
    {
        public bool RemoveEmptyRooms = false;
        public bool RemoveRandomRooms = false;

        [JsonIgnore] public bool AnySceneRemoval => RemoveEmptyRooms || RemoveRandomRooms;
        [JsonIgnore] public bool RemoveRandomRoomsPlayable => RemoveRandomRooms && Modding.ModHooks.GetMod("RandoPlus") is not null;

        [MenuChanger.Attributes.MenuRange(0.2f, 0.7f)]
        public float RandomRoomsFraction = 0.4f;
    }
}
