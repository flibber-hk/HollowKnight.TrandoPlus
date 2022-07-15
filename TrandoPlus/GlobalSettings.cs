namespace TrandoPlus
{
    public class GlobalSettings
    {
        public bool RandomizeDoors = false;
        public bool AreaDoorNonInteraction = false;
        public bool RandomizeDrops = false;
        public bool ProhibitAdjacentBenches = false;
        public bool LimitedRoomRando = false;

        [MenuChanger.Attributes.MenuRange(0.15f, 0.65f)]
        public float LimitedRoomRandoFraction = 0.35f;
    }
}
