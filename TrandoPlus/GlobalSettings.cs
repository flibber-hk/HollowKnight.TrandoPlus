namespace TrandoPlus
{
    public class GlobalSettings
    {
        public bool RandomizeDoors = false;
        public bool AreaDoorNonInteraction = false;
        public bool RandomizeDrops = false;
        public bool ProhibitAdjacentBenches = false;

        public bool RemoveEmptyRooms = false;
        public bool LimitedRoomRando = false;
        
        [MenuChanger.Attributes.MenuRange(0.2f, 0.7f)]
        public float LimitedRoomRandoFraction = 0.4f;
    }
}
