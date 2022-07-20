using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemChanger;
using Modding;
using RandomizerCore.Extensions;
using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using TrandoPlus.RestrictedRoomRando;

namespace TrandoPlus
{
    public static class RemoveEmptyRooms
    {
        private static HashSet<string> RemovedScenes = new();

        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(240, RemoveTransitions);
            LogManager.AddLogger(new RemovedScenesLogger());
        }

        private static void RemoveTransitions(RequestBuilder rb)
        {
            RemovedScenes.Clear();

            if (!TrandoPlus.GS.RemoveEmptyRooms) return;
            if (rb.gs.TransitionSettings.Mode != TransitionSettings.TransitionMode.RoomRandomizer
                || rb.gs.TransitionSettings.TransitionMatching != TransitionSettings.TransitionMatchingSetting.NonmatchingDirections)
            {
                return;
            }

            HashSet<string> locationScenes = new(rb.EnumerateItemGroups()
                .SelectMany(gb => gb.Locations.EnumerateDistinct())
                .Select(loc => rb.TryGetLocationDef(loc, out LocationDef def) ? def?.SceneName : default)
                .Where(scene => !string.IsNullOrEmpty(scene)));

            if (locationScenes.Count == 0)
            {
                return;
            }

            HashSet<string> allTransitions = SceneSelector.GetTransitions(rb);

            HashSet<string> allScenes = new(allTransitions
                .Select(trans => rb.TryGetTransitionDef(trans, out TransitionDef def) ? def?.SceneName : null)
                .Where(scene => !string.IsNullOrEmpty(scene)));

            // Remove scenes with no item
            foreach (string scene in allScenes)
            {
                if (!locationScenes.Contains(scene))
                {
                    RemovedScenes.Add(scene);
                }
            }

            // Add starter scene, black egg, dirtmouth stag, dirtmouth
            RemovedScenes.Remove(SceneNames.Room_temple);
            RemovedScenes.Remove(SceneNames.Town);
            RemovedScenes.Remove(SceneNames.Room_Town_Stag_Station);
            RemovedScenes.Remove(rb.ctx.StartDef.SceneName);

            // Add back in scenes according to constraints
            List<(string Source, string Target)> constraints = RoomConstraint.GetConstraints()
                .Where(c => c.Applies(rb))
                // Select only constraints requiring a scene that is absent
                .Where(c => RemovedScenes.Contains(c.TargetScene))
                .Select(c => (c.SourceScene, c.TargetScene))
                .ToList();

            while (true)
            {
                bool removed = false;
                foreach ((string Source, string Target) in constraints)
                {
                    if (!RemovedScenes.Contains(Source) && RemovedScenes.Contains(Target))
                    {
                        removed = true;
                        RemovedScenes.Remove(Target);
                    }
                }
                if (!removed)
                {
                    break;
                }
            }

            // Number of missing drops must be at least the number of missing falls
            int missingDropEntries = allTransitions
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && RemovedScenes.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayIn)
                .Count();

            // All missing falls
            List<string> missingDropExits = allTransitions
                .Where(t => rb.TryGetTransitionDef(t, out TransitionDef def) && RemovedScenes.Contains(def.SceneName) && def.Sides == TransitionSides.OneWayOut)
                .OrderBy(t => t)
                .ToList();

            while (missingDropExits.Count > missingDropEntries)
            {
                string returnedTransition = rb.rng.Next(missingDropExits);
                missingDropExits.Remove(returnedTransition);

                string scene = rb.TryGetTransitionDef(returnedTransition, out TransitionDef def) ? def.SceneName : "";

                RemovedScenes.Remove(scene);
            }

            foreach (string trans in allTransitions)
            {
                if (rb.TryGetTransitionDef(trans, out TransitionDef def) && RemovedScenes.Contains(def.SceneName))
                {
                    rb.RemoveTransitionByName(trans);
                }
            }
        }

        public class RemovedScenesLogger : RandoLogger
        {
            public override void Log(LogArguments args)
            {
                if (!RemovedScenes.Any()) return;

                StringBuilder sb = new();
                sb.AppendLine("Removed scenes for limited room rando:");
                sb.AppendLine();
                foreach (string scene in RemovedScenes)
                {
                    sb.AppendLine($" - {scene}");
                }
                sb.AppendLine();
                sb.AppendLine($"Total scenes removed: {RemovedScenes.Count}");

                LogManager.Write(sb.ToString(), "RemovedRoomRandoScenes.txt");
            }
        }
    }
}
