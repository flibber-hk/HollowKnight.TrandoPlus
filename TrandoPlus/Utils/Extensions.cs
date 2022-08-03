using System;
using System.Collections.Generic;
using System.Linq;
using RandomizerCore.Extensions;

namespace TrandoPlus.Utils
{
    public static class Extensions
    {
        public static void Increment<T>(this Dictionary<T, int> dict, T key, int amount = 1)
        {
            if (!dict.TryGetValue(key, out int value))
            {
                value = 0;
            }
            dict[key] = value + amount;
        }

        
        // Because unity doesn't Enumerable.ToHashSet
        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> e) => new(e);

        /// <summary>
        /// Randomize the order of the elements of toAdd before enqueueing them.
        /// </summary>
        public static void AppendRandomly(this Random rng, Queue<string> current, IEnumerable<string> toAdd)
        {
            List<string> toAddOrdered = toAdd.OrderBy(s => s).ToList();
            rng.PermuteInPlace(toAddOrdered);
            foreach (string element in toAdd)
            {
                current.Enqueue(element);
            }
        }
    }
}
