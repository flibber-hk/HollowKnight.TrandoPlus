using System.Collections.Generic;
using System.Linq;
using Modding;
using RandomizerMod.RandomizerData;

namespace TrandoPlus.RestrictedRoomRando
{
    /// <summary>
    /// Class representing the number of transitions from a room in each direction.
    /// </summary>
    public class DirectedTransitions
    {
        public readonly Dictionary<string, int> Balance = new();

        public void Add(DirectedTransitions other)
        {
            foreach ((string key, int amount) in other.Balance)
            {
                this.Balance.Increment(key, amount);
            }
        }

        public void Increment(TransitionDef def, string label, int balance)
        {
            if (def.Sides != TransitionSides.Both)
            {
                return;
            }

            Balance.Increment(label, balance);
        }

        public bool IsBalanced => Balance.Values.All(x => x == 0);

        public int Distance => Balance.Values.Sum(x => x * x);

        /// <summary>
        /// Returns true if the distance of this would be reduced by at least tol when adding other.
        /// </summary>
        public bool IsImprovedBy(DirectedTransitions other, int tol = 0)
        {
            int thisDistance = this.Distance;

            int nextDistance = 0;
            foreach (string k in Balance.Keys.Concat(other.Balance.Keys).Distinct())
            {
                if (!Balance.TryGetValue(k, out int current)) current = 0;
                if (!other.Balance.TryGetValue(k, out int next)) next = 0;
                nextDistance += (current + next) * (current + next);
            }

            return thisDistance >= nextDistance + tol;
        }

        internal string Display() => "<" + string.Join(", ", Balance.Select(kvp => $"{kvp.Key} :: {kvp.Value}")) + ">";
    }
}
