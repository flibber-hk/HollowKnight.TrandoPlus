using ItemChanger;
using RandomizerMod.RandomizerData;
using System.Collections.Generic;
using System.Linq;

namespace TrandoPlus.ExtraRandomizedTransitions
{
    public abstract class TransitionSelector
    {
        /// <summary>
        /// Select transitions to randomize. The list does not need to include the vanilla target of randomized transitions (they will be added automatically).
        /// The list may contain already randomized transitions, which will not be randomized again.
        /// </summary>
        public abstract List<TransitionDef> SelectRandomizedTransitions(IReadOnlyCollection<TransitionDef> availableTransitions);

        /// <summary>
        /// Whether this selector should be run
        /// </summary>
        public abstract bool IsEnabled();
    }

    public class DoorRandoTransitionSelector : TransitionSelector
    {
        public override List<TransitionDef> SelectRandomizedTransitions(IReadOnlyCollection<TransitionDef> availableTransitions)
        {
            List<TransitionDef> result = new();

            foreach (TransitionDef def in availableTransitions)
            {
                if (def.Direction == TransitionDirection.Door)
                {
                    result.Add(def);
                }
            }

            return result;
        }

        public override bool IsEnabled() => TrandoPlus.GS.RandomizeDoors;
    }

    public class DropRandoTransitionSelector : TransitionSelector
    {
        public override List<TransitionDef> SelectRandomizedTransitions(IReadOnlyCollection<TransitionDef> availableTransitions)
        {
            List<TransitionDef> result = new();

            foreach (TransitionDef def in availableTransitions)
            {
                if (def.Sides != TransitionSides.Both)
                {
                    result.Add(def);
                }
            }

            return result;
        }

        public override bool IsEnabled() => TrandoPlus.GS.RandomizeDrops;
    }

    public class DeadEndRandoTransitionSelector : TransitionSelector
    {
        public override List<TransitionDef> SelectRandomizedTransitions(IReadOnlyCollection<TransitionDef> availableTransitions)
        {
            List<TransitionDef> result = new();

            Dictionary<string, List<TransitionDef>> transitionsByScene = new();
            foreach (TransitionDef def in availableTransitions)
            {
                string sceneName = def.GetConnectedSceneName();
                if (!transitionsByScene.TryGetValue(sceneName, out List<TransitionDef> transitions))
                {
                    transitions = new();
                    transitionsByScene[sceneName] = transitions;
                }
                transitions.Add(def);
            }

            foreach (List<TransitionDef> transitions in transitionsByScene.Values)
            {
                if (transitions.Count == 1)
                {
                    if (transitions[0].Sides == TransitionSides.Both)
                    {
                        result.Add(transitions[0]);
                    }
                }
            }

            return result;
        }

        public override bool IsEnabled() => TrandoPlus.GS.RandomizeDeadEnds;
    }

    public class HubRandoTransitionSelector : TransitionSelector
    {
        public override List<TransitionDef> SelectRandomizedTransitions(IReadOnlyCollection<TransitionDef> availableTransitions)
        {
            List<TransitionDef> result = new();

            Dictionary<string, List<TransitionDef>> transitionsByScene = new();
            foreach (TransitionDef def in availableTransitions)
            {
                string sceneName = def.GetConnectedSceneName();
                if (!transitionsByScene.TryGetValue(sceneName, out List<TransitionDef> transitions))
                {
                    transitions = new();
                    transitionsByScene[sceneName] = transitions;
                }
                transitions.Add(def);
            }

            foreach (List<TransitionDef> transitions in transitionsByScene.Values)
            {
                if (transitions.Count >= 6)
                {
                    foreach (TransitionDef t in transitions.Where(x => x.Sides == TransitionSides.Both))
                    {
                        result.Add(t);
                    }
                }
            }

            return result;
        }

        public override bool IsEnabled() => TrandoPlus.GS.RandomizeHubs;
    }

    public static class Extensions
    {
        /// <summary>
        /// Scene name for the transition, except returns a distinct scene name for rooms like
        /// Ruins1_24, which are split into disconnected parts.
        /// </summary>
        public static string GetConnectedSceneName(this TransitionDef def)
        {
            string sceneName = def.SceneName;

            switch (def.Name)
            {
                case "Ruins1_24[left1]":
                case "Ruins1_24[right1]":
                case "White_Palace_05[left1]":
                case "White_Palace_05[right1]":
                case "Fungus3_48[right1]":
                case "Fungus3_48[door1]":
                    return sceneName + "-upper";
                
                case "Ruins1_24[left2]":
                case "Ruins1_24[right2]":
                case "White_Palace_05[left2]":
                case "White_Palace_05[right2]":
                case "Fungus3_48[bot1]":
                case "Fungus3_48[right2]":
                case "Ruins1_05[bot1]":
                case "Ruins1_05[bot2]":
                    return sceneName + "-lower";

                case "Hive_01[right2]":
                case "Hive_03[top1]":
                case "Ruins1_18[right2]":
                case "Deepnest_East_07[bot1]":
                case "Abyss_09[right3]":
                case "Ruins2_03[bot2]":
                    return sceneName + "-isolated";
            }

            switch (sceneName)
            {
                case SceneNames.Hive_01:
                case SceneNames.Hive_03:
                case SceneNames.Ruins1_18:
                case SceneNames.Deepnest_East_07:
                case SceneNames.Abyss_09:
                case SceneNames.Ruins2_03:
                case SceneNames.Ruins1_05:
                    return sceneName + "-main";
            }

            return sceneName;
        }
    }
}
