using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RoomRemovalManager
    {
        public static void Hook()
        {
            RequestMaker.Hook();
            RandomizerMod.Logging.LogManager.AddLogger(new ActiveScenesLogger());
            RandoController.OnExportCompleted += BeforeGameStart;
        }

        private static void BeforeGameStart(RandoController rc)
        {
            if (TrandoPlus.GS.LimitedRoomRando && RandoPlus.RandoPlus.GS.PreferMultiShiny)
            {
                RandoPlus.AreaRestriction.AreaRestriction.PreventMultiChests();
            }
        }
    }
}
