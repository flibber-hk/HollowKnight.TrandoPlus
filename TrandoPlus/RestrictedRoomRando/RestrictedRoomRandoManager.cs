using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrandoPlus.RestrictedRoomRando
{
    public static class RestrictedRoomRandoManager
    {
        public static void Hook()
        {
            RequestMaker.Hook();
            RandomizerMod.Logging.LogManager.AddLogger(new ActiveScenesLogger());
        }
    }
}
