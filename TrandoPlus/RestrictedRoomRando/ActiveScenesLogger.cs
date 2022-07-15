using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomizerMod.Logging;

namespace TrandoPlus.RestrictedRoomRando
{
    public class ActiveScenesLogger : RandoLogger
    {
        public override void Log(LogArguments args)
        {
            if (RequestMaker.selector is null) return;

            HashSet<string> selectedScenes = RequestMaker.selector.SelectedScenes;

            StringBuilder sb = new();
            sb.AppendLine("Active scenes for limited room rando:");
            sb.AppendLine();
            foreach (string scene in selectedScenes)
            {
                sb.AppendLine($" - {scene}");
            }

            LogManager.Write(sb.ToString(), "LimitedRoomRandoScenes.txt");
        }
    }
}
