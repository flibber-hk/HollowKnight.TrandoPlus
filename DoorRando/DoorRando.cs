using Modding;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Collections.Generic;
using System.Linq;
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

            RandomizerMod.RC.RequestBuilder.OnUpdate.Subscribe(-750f, CaptureRandomizedDoorTransitions);
            RandomizerMod.RC.RequestBuilder.OnUpdate.Subscribe(-750f, SetDoorRandoForItemRando);
            RandomizerMod.RC.RequestBuilder.OnUpdate.Subscribe(-750f, SetDoorRandoForAreaRando);
        }

        private void CaptureRandomizedDoorTransitions(RequestBuilder rb)
        {
            Transitions.Clear();

            if (!GS.RandomizeDoors) return;

            foreach (var pair in new List<VanillaRequest>(rb.Vanilla.EnumerateDistinct()))
            {
                if ((Data.IsTransition(pair.Item) && Data.GetTransitionDef(pair.Item).Direction == TransitionDirection.Door)
                    || (Data.IsTransition(pair.Location) && Data.GetTransitionDef(pair.Location).Direction == TransitionDirection.Door))
                {
                    Transitions.Add(pair.Item);
                    Transitions.Add(pair.Location);
                    rb.Vanilla.RemoveAll(pair);
                }
            }
        }

        private void SetDoorRandoForAreaRando(RequestBuilder rb)
        {
            if (!GS.RandomizeDoors) return;

            TransitionSettings ts = rb.gs.TransitionSettings;
            if (ts.Mode != TransitionSettings.TransitionMode.AreaRandomizer)
            {
                return;
            }

            StageBuilder sb = rb.Stages.First(x => x.label == RBConsts.MainTransitionStage);

            GroupBuilder builder = null;

            if (ts.TransitionMatching == TransitionSettings.TransitionMatchingSetting.NonmatchingDirections)
            {
                builder = sb.Get(RBConsts.TwoWayGroup);

                ((SelfDualTransitionGroupBuilder)builder).Transitions.AddRange(Transitions);
            }
            else
            {
                builder = sb.Get(RBConsts.InLeftOutRightGroup);

                List<string> lefts = Transitions.Where(x => Data.GetTransitionDef(x).Direction == TransitionDirection.Left).ToList();
                List<string> rights = Transitions.Where(x => Data.GetTransitionDef(x).Direction == TransitionDirection.Right).ToList();
                List<string> doors = Transitions.Where(x => Data.GetTransitionDef(x).Direction == TransitionDirection.Door).ToList();
                rb.rng.PermuteInPlace(doors);

                foreach (string doorTrans in doors)
                {
                    if (lefts.Count > rights.Count)
                    {
                        rights.Add(doorTrans);
                    }
                    else
                    {
                        lefts.Add(doorTrans);
                    }
                }

                ((SymmetricTransitionGroupBuilder)builder).Group1.AddRange(rights);
                ((SymmetricTransitionGroupBuilder)builder).Group2.AddRange(lefts);
            }

            bool MatchedTryResolveGroup(RequestBuilder rb, string item, ElementType type, out GroupBuilder gb)
            {
                if ((type == ElementType.Transition || Data.IsTransition(item))
                    && (Transitions.Contains(item)))
                {
                    gb = builder;
                    return true;
                }
                gb = default;
                return false;
            }
            OnGetGroupFor.Subscribe(-1000f, MatchedTryResolveGroup);
        }

        private void SetDoorRandoForItemRando(RequestBuilder rb)
        {
            if (!GS.RandomizeDoors) return;

            TransitionSettings ts = rb.gs.TransitionSettings;
            if (ts.Mode != TransitionSettings.TransitionMode.None)
            {
                return;
            }

            // Insert stage at the start because it's a lot more restricted than the item placements
            // Treat matched as Door <--> Non-Door because that's what matched means in this context
            StageBuilder sb = rb.InsertStage(0, "Door Rando Transition Stage");

            GroupBuilder builder = null;

            if (ts.TransitionMatching == TransitionSettings.TransitionMatchingSetting.NonmatchingDirections)
            {
                builder = new SelfDualTransitionGroupBuilder()
                {
                    label = $"Door Rando Group",
                    stageLabel = RBConsts.MainTransitionStage,
                    coupled = ts.Coupled,
                };

                ((SelfDualTransitionGroupBuilder)builder).Transitions.AddRange(Transitions);
            }
            else
            {
                builder = new SymmetricTransitionGroupBuilder()
                {
                    label = $"Forward Door Rando",
                    reverseLabel = $"Reverse Door Rando",
                    coupled = ts.Coupled,
                    stageLabel = "Door Rando Transition Stage"
                };

                List<string> nonDoors = Transitions.Where(x => Data.GetTransitionDef(x).Direction != TransitionDirection.Door).ToList();
                List<string> doors = Transitions.Where(x => Data.GetTransitionDef(x).Direction == TransitionDirection.Door).ToList();

                ((SymmetricTransitionGroupBuilder)builder).Group1.AddRange(doors);
                ((SymmetricTransitionGroupBuilder)builder).Group2.AddRange(nonDoors);
            }

            builder.strategy = rb.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy();
            sb.Add(builder);

            bool MatchedTryResolveGroup(RequestBuilder rb, string item, ElementType type, out GroupBuilder gb)
            {
                if ((type == ElementType.Transition || Data.IsTransition(item))
                    && (Transitions.Contains(item)))
                {
                    gb = builder;
                    return true;
                }
                gb = default;
                return false;
            }
            OnGetGroupFor.Subscribe(-1000f, MatchedTryResolveGroup);
        }
    }
}