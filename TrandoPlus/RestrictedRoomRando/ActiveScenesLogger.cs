using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerMod.Logging;

namespace TrandoPlus.RestrictedRoomRando
{
    public class ActiveScenesLogger : RandoLogger
    {
        public override void Log(LogArguments args)
        {
            if (RequestMaker.Selector is null) return;

            HashSet<string> selectedScenes = RequestMaker.Selector.SelectedSceneNames;

            StringBuilder sb = new();
            sb.AppendLine($"Available scenes for limited room rando with seed {args.gs.Seed}:");
            sb.AppendLine();
            foreach (string scene in selectedScenes
#if !DEBUG
                // In debug mode, showing the scenes in the order they were added may be helpful
                .OrderBy(x => x)
#endif
                )
            {
                sb.AppendLine($" - {scene}");
            }
            sb.AppendLine();
            sb.AppendLine($"Total selected scenes: {selectedScenes.Count}/{RequestMaker.Selector.TotalSceneCount}.");
            sb.AppendLine($"Total selected transitions: {RequestMaker.Selector.SelectedTransitionCount}/{RequestMaker.Selector.TransitionCount}.");


            LogManager.Write(sb.ToString(), "SelectedScenesSpoiler.txt");
        }
    }
}
