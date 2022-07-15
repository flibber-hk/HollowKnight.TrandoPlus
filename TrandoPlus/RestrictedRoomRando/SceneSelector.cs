using System;
using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using Modding;
using RandomizerCore.Extensions;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace TrandoPlus.RestrictedRoomRando
{
    public class SceneSelector
    {
        public static readonly Dictionary<string, string> Compressions = new()
        {
            [SceneNames.Crossroads_46b] = SceneNames.Crossroads_46,
            [SceneNames.Crossroads_49b] = SceneNames.Crossroads_49,
            [SceneNames.Ruins2_10b] = SceneNames.Ruins2_10,
            [SceneNames.Abyss_03_b] = SceneNames.Abyss_03,
            [SceneNames.Abyss_03_c] = SceneNames.Abyss_03,
        };

        public static readonly Dictionary<string, string> StagScenes = new()
        {
            // Dirtmouth stag is always present
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

        public static readonly List<(string, string)> Shops = new()
        {
            (LocationNames.Sly, SceneNames.Room_shop),
            (LocationNames.Iselda, SceneNames.Room_mapper),
            (LocationNames.Leg_Eater, SceneNames.Fungus2_26),
            (LocationNames.Salubra, SceneNames.Room_Charm_Shop),
        };


        private readonly RequestBuilder rb;

        /// <summary>
        /// If s -> ts, then if s is present each element of ts must be present.
        /// </summary>
        private readonly Dictionary<string, List<string>> _constraints;

        /// <summary>
        /// The number of scenes to choose from.
        /// </summary>
        private int SceneCount => _sceneTransitionCounts.Count;
        /// <summary>
        /// The list of available (unselected) scenes.
        /// </summary>
        private List<string> _availableSceneNames;
        /// <summary>
        /// A mapping from scene to transition counts, for all (selected or unselected) scenes.
        /// </summary>
        private Dictionary<string, DirectedTransitions> _sceneTransitionCounts;

        private readonly Bucket<string> _oneWayInCounts = new();
        private readonly Bucket<string> _oneWayOutCounts = new();

        private readonly DirectedTransitions _directedTransitionCounts = new();

        private readonly HashSet<string> _selectedScenes = new();
        public HashSet<string> SelectedScenes
        {
            get
            {
                if (_selectedScenes is null)
                {
                    throw new InvalidOperationException("The SceneSelector has not been run!");
                }

                HashSet<string> result = new(_selectedScenes);
                foreach ((string extraScene, string mainScene) in Compressions)
                {
                    if (result.Contains(mainScene))
                    {
                        result.Add(extraScene);
                    }
                }
                return result;
            }
        }

        public string SelectedShop { get; private set; }
        public string SelectedShopScene => Shops.FirstOrDefault(pair => pair.Item1 == SelectedShop).Item2;

        public SceneSelector(RequestBuilder rb)
        {
            this.rb = rb;
            this._constraints = new();
            foreach (RoomConstraint constraint in RoomConstraint.GetConstraints())
            {
                if (!constraint.Applies(rb))
                {
                    continue;
                }

                if (!this._constraints.TryGetValue(constraint.SourceScene, out List<string> targets))
                {
                    targets = this._constraints[constraint.SourceScene] = new();
                }
                targets.Add(constraint.TargetScene);
            }

            GenerateSceneMap();
        }

        /// <summary>
        /// Generate the list of scenes and directed transition counts from the request builder.
        /// </summary>
        private void GenerateSceneMap()
        {
            _availableSceneNames = new();
            _sceneTransitionCounts = new();

            foreach ((string trans, string label, int balance) in EnumerateTransitions(rb))
            {
                if (!rb.TryGetTransitionDef(trans, out TransitionDef def))
                {
                    throw new InvalidOperationException($"Transition {trans} without a transition def!");
                }
                string scene = def.SceneName;
                if (Compressions.ContainsKey(scene))
                {
                    scene = Compressions[scene];
                }

                if (!_availableSceneNames.Contains(scene))
                {
                    _availableSceneNames.Add(scene);
                }

                if (!_sceneTransitionCounts.ContainsKey(scene))
                {
                    _sceneTransitionCounts[scene] = new DirectedTransitions();
                }
                _sceneTransitionCounts[scene].Increment(def, label, balance);

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
        /// Select roughly `fraction` proportion of the available scenes.
        /// </summary>
        /// <param name="fraction">The proportion of available scenes to select.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public HashSet<string> Run(double fraction)
        {
            // Select important scenes, including a shop
            SelectScene(SceneNames.Town);
            SelectScene(SceneNames.Room_Town_Stag_Station);
            SelectScene(SceneNames.Room_temple);
            SelectScene(rb.ctx.StartDef.SceneName);

            (string shop, string shopScene) = rb.rng.Next(Shops);

            SelectScene(shopScene);
            SelectedShop = shop;

            // Select drop entries - ensuring at least one. Make a random mapping
            // from drop entries to exits to ensure that the number of entries
            // is at most the number of exits.
            List<string> oneWayIn = _oneWayInCounts.EnumerateWithMultiplicity().OrderBy(x => x).ToList();
            List<string> oneWayOut = _oneWayOutCounts.EnumerateWithMultiplicity().OrderBy(x => x).ToList();
            rb.rng.PermuteInPlace(oneWayIn);
            rb.rng.PermuteInPlace(oneWayOut);

            foreach ((string source, string target) in Enumerable.Zip(oneWayIn, oneWayOut, (x, y) => (x, y)))
            {
                if (!this._constraints.TryGetValue(source, out List<string> targets))
                {
                    targets = this._constraints[source] = new();
                }
                targets.Add(target);
            }

            rb.rng.PermuteInPlace(oneWayIn);
            int oneWayInCount = 0;
            for (int i = 0; i < oneWayIn.Count; i++)
            {
                if (rb.rng.NextDouble() <= fraction) oneWayInCount++;
            }
            // Need at least one entrance so Fungus2_25 mask shard is reachable
            if (oneWayInCount == 0)
            {
                oneWayInCount = 1;
            }
            for (int i = 0; i < oneWayInCount; i++)
            {
                SelectScene(oneWayIn[i]);
            }
            for (int i = oneWayInCount; i < oneWayIn.Count; i++)
            {
                RemoveScene(oneWayIn[i]);
            }

            // Select enough scenes to reach fraction
            while (_selectedScenes.Count < SceneCount * fraction * 0.75 - 6)
            {
                SelectScene(rb.rng.Next(_availableSceneNames));
            }
            // For the last quarter, attempt to maintain the balance
            while (_selectedScenes.Count < SceneCount * fraction - 6)
            {
                if (_directedTransitionCounts.IsBalanced)
                {
                    SelectScene(rb.rng.Next(_availableSceneNames));
                }
                else
                {
                    List<string> balancingSceneNames = _availableSceneNames
                        .Where(s => _directedTransitionCounts.IsImprovedBy(_sceneTransitionCounts[s], -1))
                        .ToList();
                    if (balancingSceneNames.Count == 0)
                    {
                        throw new InvalidOperationException("No improving scenes found towards end");
                    }
                    SelectScene(rb.rng.Next(balancingSceneNames));
                }
                
            }

            // Remove scenes with an unsatisfied constraint because I don't want to deal with them
            foreach (string scene in _constraints.Where(kvp => kvp.Value.Any(s => _availableSceneNames.Contains(s))).Select(kvp => kvp.Key))
            {
                RemoveScene(scene);
            }

            // TODO - organise transitions by group/label rather than direction
            while (!_directedTransitionCounts.IsBalanced)
            {
                List<string> balancingSceneNames = _availableSceneNames
                    .Where(s => _directedTransitionCounts.IsImprovedBy(_sceneTransitionCounts[s], 1))
                    .ToList();
                if (balancingSceneNames.Count == 0)
                {
                    TrandoPlus.instance.LogError("ERROR");
                    TrandoPlus.instance.LogError($"SELF: {_directedTransitionCounts.Display()}");
                    TrandoPlus.instance.LogError($"");
                    foreach (string s in _availableSceneNames)
                    {
                        TrandoPlus.instance.LogError($"  - {s} - {_sceneTransitionCounts[s].Display()}");
                    }
                    throw new InvalidOperationException("No improving scenes found at end");
                }
                SelectScene(rb.rng.Next(balancingSceneNames));
            }

            return SelectedScenes;
        }

        /// <summary>
        /// Mark the supplied scene as selected.
        /// </summary>
        private void SelectScene(string scene)
        {
            if (!_availableSceneNames.Contains(scene))
            {
                return;
            }

            _availableSceneNames.Remove(scene);
            _selectedScenes.Add(scene);

            _directedTransitionCounts.Add(_sceneTransitionCounts[scene]);

            if (_constraints.TryGetValue(scene, out List<string> targets))
            {
                foreach (string target in targets)
                {
                    SelectScene(target);
                }
            }
        }

        /// <summary>
        /// Mark the supplied scene as unavailable.
        /// </summary>
        private void RemoveScene(string scene)
        {
            _availableSceneNames.Remove(scene);
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
