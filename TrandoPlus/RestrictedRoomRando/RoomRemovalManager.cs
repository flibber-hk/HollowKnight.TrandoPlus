using RandomizerMod.RC;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RoomRemovalManager
    {
        public static LimitedRoomRandoConfig Config => TrandoPlus.GS.LimitedRoomRandoConfig;

        public static void Hook()
        {
            RequestMaker.Hook();
            RandomizerMod.Logging.LogManager.AddLogger(new ActiveScenesLogger());
            RandoController.OnExportCompleted += BeforeGameStart;
        }

        private static void BeforeGameStart(RandoController rc)
        {
            if (Config.RemoveRandomRooms && RandoPlus.RandoPlus.GS.PreferMultiShiny)
            {
                RandoPlus.AreaRestriction.AreaRestriction.PreventMultiChests();
            }
        }
    }
}
