namespace TrandoPlus.RestrictedRoomRando
{
    public static class RoomRemovalManager
    {
        public static LimitedRoomRandoConfig Config => TrandoPlus.GS.LimitedRoomRandoConfig;

        public static void Hook()
        {
            RequestMaker.Hook();
            RandomizerMod.Logging.LogManager.AddLogger(new ActiveScenesLogger());
        }
    }
}
