﻿using System;
using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using Modding;
using RandomizerCore.Extensions;
using RandomizerMod;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using TrandoPlus.Utils;

namespace TrandoPlus.RestrictedRoomRando
{
    public class SceneSelector
    {
        /// <summary>
        /// Event invoked while the selector is running.
        /// </summary>
        public PriorityEvent<Action<RequestBuilder, SceneSelector>> OnSceneSelectorRun;
        private PriorityEvent<Action<RequestBuilder, SceneSelector>>.IPriorityEventOwner _onSceneSelectorRunOwner;

        /// <summary>
        /// A mapping stag name -> scene name
        /// </summary>
        public static readonly Dictionary<string, string> StagScenes = new()
        {
            [ItemNames.Dirtmouth_Stag] = SceneNames.Room_Town_Stag_Station,
            [ItemNames.Crossroads_Stag] = SceneNames.Crossroads_47,
            [ItemNames.Greenpath_Stag] = SceneNames.Fungus1_16_alt,
            [ItemNames.Queens_Station_Stag] = SceneNames.Fungus2_02,
            [ItemNames.Queens_Gardens_Stag] = SceneNames.Fungus3_40,
            [ItemNames.City_Storerooms_Stag] = SceneNames.Ruins1_29,
            [ItemNames.Kings_Station_Stag] = SceneNames.Ruins2_08,
            [ItemNames.Resting_Grounds_Stag] = SceneNames.RestingGrounds_09,
            [ItemNames.Distant_Village_Stag] = SceneNames.Deepnest_09,
            [ItemNames.Hidden_Station_Stag] = SceneNames.Abyss_22,
            [ItemNames.Stag_Nest_Stag] = SceneNames.Cliffs_03,
        };

        public static readonly List<(string shopName, string shopScene)> Shops = new()
        {
            (LocationNames.Sly, SceneNames.Room_shop),
            (LocationNames.Iselda, SceneNames.Room_mapper),
            (LocationNames.Leg_Eater, SceneNames.Fungus2_26),
            (LocationNames.Salubra, SceneNames.Room_Charm_Shop),
        };


        private readonly RequestBuilder rb;

        /// <summary>
        /// If s -> ts, then if s is present each element of ts must be present.
        /// We write this as a hashset with fast source lookup because removing scenes
        /// is unlikely, and I don't want to deal with multiple synced dictionaries.
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> Constraints;


        /// <summary>
        /// All of the scenes that the selector is aware of.
        /// </summary>
        public HashSet<string> AllSceneNames => SceneTransitionBalance.Keys.AsHashSet();

        /// <summary>
        /// All of the transition names the selector is aware of.
        /// </summary>
        public readonly HashSet<string> AllTransitionNames = new();

        /// <summary>
        /// A mapping from scene name to signed number of transitions per direction.
        /// </summary>
        public readonly Dictionary<string, DirectedTransitions> SceneTransitionBalance = new();

        /// <summary>
        /// A mapping from scene name to total transitions for that scene.
        /// </summary>
        public readonly Dictionary<string, int> TransitionsPerScene = new();

        /// <summary>
        /// The number of scenes the selector is aware of.
        /// </summary>
        public int TotalSceneCount => AllSceneNames.Count;

        /// <summary>
        /// The total number of transitions the selector is aware of.
        /// </summary>
        public int TransitionCount => TransitionsPerScene.Values.Sum();


        /// <summary>
        /// The selected scene names.
        /// </summary>
        public readonly HashSet<string> SelectedSceneNames = new();

        /// <summary>
        /// The not-yet-selected scene names.
        /// </summary>
        public HashSet<string> AvailableSceneNames => AllSceneNames.Except(SelectedSceneNames).AsHashSet();

        /// <summary>
        /// The total number of selected scenes.
        /// </summary>
        public int SelectedSceneCount => SelectedSceneNames.Count();

        /// <summary>
        /// The total number of transitions among selected scenes.
        /// </summary>
        public int SelectedTransitionCount => SelectedSceneNames.Select(x => TransitionsPerScene[x]).Sum();

        /// <summary>
        /// The signed number of transitions in each direction over all selected scenes.
        /// This quantity must be zero for each direction for the seed.
        /// </summary>
        public DirectedTransitions CurrentTransitionBalance
        {
            get
            {
                DirectedTransitions dt = new();
                foreach (string sceneName in SelectedSceneNames)
                {
                    dt.Add(SceneTransitionBalance[sceneName]);
                }

                return dt;
            }
        }


        /// <summary>
        /// There must always be at least one shop - this is the shop that is selected.
        /// </summary>
        public string SelectedShop { get; private set; }
        public string SelectedShopScene => Shops.FirstOrDefault(pair => pair.Item1 == SelectedShop).Item2;

        // Special handling for one ways
        private readonly Bucket<string> _oneWayInCounts = new();
        private readonly Bucket<string> _oneWayOutCounts = new();


        public SceneSelector(RequestBuilder rb)
        {
            OnSceneSelectorRun = new(out _onSceneSelectorRunOwner);

            this.rb = rb;
            this.Constraints = GenerateConstraints(rb);

            GenerateSceneInfo();
        }

        private static Dictionary<string, HashSet<string>> GenerateConstraints(RequestBuilder rb)
        {
            Dictionary<string, HashSet<string>> constraints = new();

            foreach ((string Source, string Target) in RoomConstraints.GetConstraints(rb))
            {
                if (!constraints.TryGetValue(Source, out HashSet<string> targets))
                {
                    targets = constraints[Source] = new();
                }
                targets.Add(Target);
            }

            return constraints;
        }

        /// <summary>
        /// Ensure that if the source scene is selected, then so is the target.
        /// </summary>
        public void AddConstraint(string source, string target)
        {
            if (SelectedSceneNames.Contains(source))
            {
                SelectScene(target);
            }

            if (!Constraints.TryGetValue(source, out HashSet<string> targets))
            {
                targets = Constraints[source] = new();
            }
            targets.Add(target);
        }

        /// <summary>
        /// Generate the list of scenes and directed transition counts from the request builder.
        /// </summary>
        private void GenerateSceneInfo()
        {
            foreach ((string trans, string label, int balance) in EnumerateTransitions(rb))
            {
                if (!rb.TryGetTransitionDef(trans, out TransitionDef def))
                {
                    throw new InvalidOperationException($"Transition {trans} without a transition def!");
                }
                string scene = def.SceneName;

                AllTransitionNames.Add(trans);
                TransitionsPerScene.Increment(scene);

                if (!SceneTransitionBalance.ContainsKey(scene))
                {
                    SceneTransitionBalance[scene] = new DirectedTransitions();
                }
                SceneTransitionBalance[scene].Increment(def, label, balance);

                if (def.Sides == TransitionSides.OneWayIn)
                {
                    _oneWayInCounts.Increment(scene, 1);
                }
                else if (def.Sides == TransitionSides.OneWayOut)
                {
                    _oneWayOutCounts.Increment(scene, 1);
                }
            }
        }

        /// <summary>
        /// Event invoked whenever a scene that was not previously selected becomes selected.
        /// </summary>
        public event Action<string> OnSelectScene;
        /// <summary>
        /// Event invoked whenever a previously selected scene is removed.
        /// </summary>
        public event Action<string> OnRemoveScene;

        /// <summary>
        /// Mark the supplied scene as selected.
        /// </summary>
        public void SelectScene(string scene)
        {
            if (SelectedSceneNames.Contains(scene))
            {
                return;
            }
            if (!AllSceneNames.Contains(scene))
            {
                throw new ArgumentException($"{nameof(SelectScene)}: Scene {scene} not recognised!");
            }

            SelectedSceneNames.Add(scene);
            OnSelectScene?.Invoke(scene);

            if (Constraints.TryGetValue(scene, out HashSet<string> targets))
            {
                foreach (string target in targets)
                {
                    SelectScene(target);
                }
            }
        }

        /// <summary>
        /// Deselect the given scene, if it was already selected.
        /// </summary>
        public void RemoveScene(string scene)
        {
            if (!SelectedSceneNames.Contains(scene))
            {
                return;
            }
            if (!AllSceneNames.Contains(scene))
            {
                throw new ArgumentException($"{nameof(RemoveScene)}: Scene {scene} not recognised!");
            }

            SelectedSceneNames.Remove(scene);
            OnRemoveScene?.Invoke(scene);

            foreach ((string source, HashSet<string> targets) in Constraints)
            {
                if (targets.Contains(scene))
                {
                    RemoveScene(source);
                }
            }
        }

        /// <summary>
        /// Initialize the Scene Selector by adding some constraints.
        /// </summary>
        protected virtual void Initialize()
        {
            // Add a randomly selected scene constraining stag nest stag, to prevent the unlikely scenario where they
            // select all 9 stag scenes but not stag nest itself.
            List<string> normalStagScenes = StagScenes
                .Where(kvp => kvp.Value != SceneNames.Cliffs_03 && kvp.Value != SceneNames.Room_Town_Stag_Station)
                .Select(kvp => kvp.Value)
                .OrderBy(x => x)
                .ToList();
            string constrainingStagScene = rb.rng.Next(normalStagScenes);
            AddConstraint(constrainingStagScene, SceneNames.Cliffs_03);

            // Create a uniformly selected mapping to ensure that the OneWayIn and OneWayOut counts are valid
            // This need not correspond to the real placements.
            List<string> oneWayIn = _oneWayInCounts.EnumerateWithMultiplicity().OrderBy(x => x).ToList();
            List<string> oneWayOut = _oneWayOutCounts.EnumerateWithMultiplicity().OrderBy(x => x).ToList();
            rb.rng.PermuteInPlace(oneWayIn);
            rb.rng.PermuteInPlace(oneWayOut);

            foreach ((string source, string target) in Enumerable.Zip(oneWayIn, oneWayOut, (x, y) => (x, y)))
            {
                AddConstraint(source, target);
                if (target == SceneNames.Fungus2_25)
                {
                    // Need at least one scene capable of giving access to Mask_Shard-Deepnest
                    AddConstraint(target, source);
                }
            }
        }

        /// <summary>
        /// Close the scene selector to ensure a seed that is likely to generate successfully by adding scenes.
        /// </summary>
        protected virtual void Close()
        {
            AddRequiredScenes();
            AddGrubScenes();
            AddEssenceScenes();
            AddHubs();
            BalanceTransitions();
        }

        private void AddRequiredScenes()
        {
            // Require Black Egg temple for True Ending
            SelectScene(SceneNames.Room_temple);
            // Require the start scene for obvious reasons
            SelectScene(rb.ctx.StartDef.SceneName);
            // Require at least one shop
            (string shop, string shopScene) = rb.rng.Next(Shops);
            SelectScene(shopScene);
            SelectedShop = shop;
        }

        /// <summary>
        /// Run the selector to generate a list of scenes.
        /// </summary>
        public void Run()
        {
            Initialize();
            foreach (Action<RequestBuilder, SceneSelector> toInvoke in _onSceneSelectorRunOwner.GetSubscribers())
            {
                toInvoke?.Invoke(rb, this);
            }
            Close();

            // Add a log message to inform modlog readers that this is going on - is likely this will come up because
            // it can take many attempts to generate the seed.
            TrandoPlus.instance.Log($"SceneSelector closed: {SelectedSceneCount} scenes selected, {SelectedTransitionCount} transitions selected.");
        }


        /// <summary>
        /// Make sure there are enough scenes with grubs if grubs are not randomized.
        /// </summary>
        private void AddGrubScenes()
        {
            if (rb.gs.PoolSettings.Grubs) return;

            int requiredGrubCount = rb.gs.CostSettings.MaximumGrubCost + rb.gs.CostSettings.GrubTolerance;

            List<string> availableGrubScenes = Finder.GetFullLocationList()
                .Where(kvp => kvp.Key.StartsWith("Grub"))
                .Select(kvp => kvp.Value.sceneName)
                .Where(scene => !SelectedSceneNames.Contains(scene))
                .OrderBy(scene => scene)
                .ToList();

            void Remove(string sceneName) => availableGrubScenes.Remove(sceneName);

            OnSelectScene += Remove;
            while (46 - availableGrubScenes.Count < requiredGrubCount)
            {
                string selectedGrubScene = rb.rng.Next(availableGrubScenes);
                SelectScene(selectedGrubScene);
            }
            OnSelectScene -= Remove;
        }

        /// <summary>
        /// Make sure there is enough reachable essence. Not necessary with default settings because
        /// White Defender, Marmu, Failed Champion and Soul Tyrant are available.
        /// </summary>
        private void AddEssenceScenes()
        {
            Dictionary<string, (int essence, string sceneName)> essenceData = new();
            int currentEssence = 0;

            int targetEssence = rb.gs.CostSettings.MaximumEssenceCost + rb.gs.CostSettings.EssenceTolerance;
            HashSet<string> allItems = rb.EnumerateItemGroups().SelectMany(igb => igb.Items.EnumerateDistinct()).AsHashSet();

            foreach (string itemName in Finder.GetFullItemList().Keys.Where(x => x.StartsWith("Boss_Essence-") || x.StartsWith("Whispering_Root-")))
            {
                // WD and GPZ require multiple scenes, so we exclude them
                if (itemName == ItemNames.Boss_Essence_White_Defender || itemName == ItemNames.Boss_Essence_Grey_Prince_Zote)
                {
                    continue;
                }

                int essence = ((ItemChanger.Items.EssenceItem)Finder.GetItem(itemName)).amount;
                string rawSceneName = Finder.GetLocation(itemName).sceneName;
                string sceneName = ItemChanger.Util.SceneUtil.TryGetSuperScene(rawSceneName, out string super) ? super : rawSceneName;

                // If the item is randomized, it will always be reachable
                if (allItems.Contains(itemName) || SelectedSceneNames.Contains(sceneName))
                {
                    currentEssence += essence;
                }
                else
                {
                    essenceData[itemName] = (essence, sceneName);
                }
            }

            void AddEssenceScene(string selectedScene)
            {
                List<string> toRemove = new();
                foreach ((string item, (int essence, string scene)) in essenceData)
                {
                    if (scene == selectedScene)
                    {
                        currentEssence += essence;
                        toRemove.Add(item);
                    }
                }

                foreach (string item in toRemove)
                {
                    essenceData.Remove(item);
                }
            }

            OnSelectScene += AddEssenceScene;
            
            while (currentEssence < targetEssence)
            {
                Bucket<string> weightedScenes = new();
                foreach ((string item, (int essence, string scene)) in essenceData.OrderBy(kvp => kvp.Key))
                {
                    weightedScenes.Increment(scene, essence);
                }

                SelectScene(weightedScenes.ToWeightedArray().Next(rb.rng));
            }
            
            OnSelectScene -= AddEssenceScene;
        }


        /// <summary>
        /// Add enough hubs to ensure that the world isn't saturated with one-transition scenes.
        /// This is not necessary in uncoupled mode.
        /// Explanation of the choice of ratio:
        /// - Need to have T >= 2R - 2 for connectivity (each edge encompasses two transitions)
        /// - Transition count balancing needs to happen after adding hubs, so we increase to give some leeway.
        /// </summary>
        private void AddHubs()
        {
            if (!rb.gs.TransitionSettings.Coupled) return;

            Queue<string> availableHubScenes = new();

            for (int i = 5; i >= 3; i--)
            {
                IEnumerable<string> newScenes = TransitionsPerScene
                    .Where(kvp => !SelectedSceneNames.Contains(kvp.Key))
                    .Where(kvp => kvp.Value >= 5)
                    .Select(kvp => kvp.Key);
                rb.rng.AppendRandomly(availableHubScenes, newScenes);
            }

            while (SelectedTransitionCount < 2.15f * SelectedSceneCount)
            {
                string selectedHub = availableHubScenes.Dequeue();
                SelectScene(selectedHub);

                if (availableHubScenes.Count == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Ensure that there are the same number of transitions in each direction.
        /// </summary>
        private void BalanceTransitions()
        {
            DirectedTransitions currentBalance = CurrentTransitionBalance;

            void ReBalance(string scene) => currentBalance.Add(SceneTransitionBalance[scene]);

            OnSelectScene += ReBalance;

            while (!currentBalance.IsBalanced)
            {
                List<string> balancingSceneNames = AvailableSceneNames
                    .Where(s => CurrentTransitionBalance.IsImprovedBy(SceneTransitionBalance[s], 1))
                    .OrderBy(x => x)
                    .ToList();
                if (balancingSceneNames.Count == 0)
                {
                    SelectScene(rb.rng.Next(AvailableSceneNames.OrderBy(x => x).ToList()));
                }
                else
                {
                    SelectScene(rb.rng.Next(balancingSceneNames));
                }
            }

            OnSelectScene -= ReBalance;
        }

        public static HashSet<string> GetTransitions(RequestBuilder rb)
        {
            return new HashSet<string>(EnumerateTransitions(rb).Select(tuple => tuple.trans));
        }

        public static IEnumerable<(string trans, string label, int balance)> EnumerateTransitions(RequestBuilder rb)
        {
            foreach (TransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<TransitionGroupBuilder>())
            {
                foreach (string t in tgb.Sources.EnumerateDistinct())
                {
                    yield return (t, $"{tgb.label}:{tgb.stageLabel}", 1);
                }
                foreach (string t in tgb.Targets.EnumerateDistinct())
                {
                    yield return (t, $"{tgb.label}:{tgb.stageLabel}", -1);
                }
            }
            foreach (SymmetricTransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<SymmetricTransitionGroupBuilder>())
            {
                foreach (string t in tgb.Group1.EnumerateDistinct())
                {
                    yield return (t, $"{tgb.label}:{tgb.stageLabel}", 1);
                }
                foreach (string t in tgb.Group2.EnumerateDistinct())
                {
                    yield return (t, $"{tgb.label}:{tgb.stageLabel}", -1);
                }
            }
            foreach (SelfDualTransitionGroupBuilder tgb in rb.EnumerateTransitionGroups().OfType<SelfDualTransitionGroupBuilder>())
            {
                foreach (string t in tgb.Transitions.EnumerateDistinct())
                {
                    yield return (t, $"{tgb.label}:{tgb.stageLabel}", 0);
                }
            }
        }
    }
}
