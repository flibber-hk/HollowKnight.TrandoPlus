using Modding;
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

        public bool IsEnabled()
        {
            if (RandomizeDoors
                || AreaDoorNonInteraction
                || RandomizeDrops
                || ProhibitAdjacentBenches
                )
            {
                return true;
            }

            if (ModHooks.GetMod("RandoPlus") is not null && LimitedRoomRandoConfig.AnySceneRemoval)
            {
                return true;
            }

            return false;
        }
    }

    public class LimitedRoomRandoConfig
    {
        public bool RemoveEmptyRooms = false;
        public bool RemoveRandomRooms = false;
        public bool EnsureBenchRooms = false;

        [JsonIgnore] public bool AnySceneRemoval => RemoveEmptyRooms || RemoveRandomRooms;

        [MenuChanger.Attributes.MenuRange(0.2f, 0.7f)]
        public float RandomRoomsFraction = 0.4f;
    }
}
