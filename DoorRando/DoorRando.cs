using System;
using System.Collections.Generic;
using System.Linq;
using Modding;
using RandomizerCore;
using RandomizerCore.Extensions;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using UnityEngine;
using static RandomizerMod.RC.RequestBuilder;

namespace DoorRando
{
    public class DoorRando : Mod, IGlobalSettings<GlobalSettings>
    {
        public static readonly HashSet<string> Transitions = new();

        internal static DoorRando instance;

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings s) => GS = s;
        public GlobalSettings OnSaveGlobal() => GS;

        public DoorRando() : base(null)
        {
            instance = this;
        }
        
        public override string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();;
        }
        
        public override void Initialize()
        {
            Log("Initializing Mod...");

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += MenuHolder.OnExitMenu;
            RandomizerMod.Menu.RandomizerMenuAPI.AddMenuPage(MenuHolder.ConstructMenu, MenuHolder.TryGetMenuButton);

            RandomizerMod.RC.RequestBuilder.OnUpdate.Subscribe(-750f, SetDoorRando);
        }

        private void SetDoorRando(RequestBuilder rb)
        {
            Transitions.Clear();
            if (!GS.RandomizeDoors) return;

            TransitionSettings ts = rb.gs.TransitionSettings;

            if (ts.Mode != TransitionSettings.TransitionMode.None)
            {
                LogError("Cannot door rando if base transition rando has been selected!");
                return;
            }    

            foreach (var pair in new List<VanillaRequest>(rb.Vanilla.EnumerateDistinct()))
            {
                if (pair.Item.Contains("[door") || pair.Location.Contains("[door"))
                {
                    Transitions.Add(pair.Item);
                    Transitions.Add(pair.Location);
                    rb.Vanilla.RemoveAll(pair);
                }
            }

            // For easiness (pairing door with door is deadly, because most non-doors are isolated) we ignore matched setting
            StageBuilder sb = rb.AddStage("Door Rando Transition Stage");

            List<string> nonDoors = Transitions.Where(x => !x.Contains("[door")).ToList();
            List<string> doors = Transitions.Where(x => x.Contains("[door")).ToList();

            string startNonDoor = null;
            string start = rb.gs.StartLocationSettings.StartLocation;
            string startTrans = Data.GetStartDef(start).Transition;
            if (nonDoors.Contains(startTrans))
            {
                startNonDoor = Data.GetTransitionDef(startTrans).VanillaTarget;
            }

            List<(string door, string nonDoor)> pairs = ManualRandomizer.GetPairs(doors, nonDoors, startNonDoor, rb.rng);

            for (int i = 0; i < pairs.Count; i++)
            {
                SymmetricTransitionGroupBuilder builder = new()
                {
                    label = $"Forward {i}",
                    reverseLabel = $"Reverse {i}",
                    coupled = true,
                    stageLabel = "Door Rando Transition Stage"
                };

                builder.Group2.Add(pairs[i].door);
                builder.Group1.Add(pairs[i].nonDoor);

                builder.strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy();

                sb.Add(builder);

                bool MatchedTryResolveGroup(RequestBuilder rb, string item, ElementType type, out GroupBuilder gb)
                {
                    if ((type == ElementType.Transition || Data.IsTransition(item))
                        && (item == pairs[i].door || item == pairs[i].nonDoor))
                    {
                        gb = builder;
                        return true;
                    }
                    gb = default;
                    return false;
                }
                OnGetGroupFor.Subscribe(-1000f, MatchedTryResolveGroup);
            }

            // Cursed
            List<StageBuilder> _stages = ReflectionHelper.GetField<RequestBuilder, List<StageBuilder>>(rb, "_stages");
            StageBuilder mig = _stages[0];
            _stages.RemoveAt(0);
            _stages.Add(mig);
        }
    }
}