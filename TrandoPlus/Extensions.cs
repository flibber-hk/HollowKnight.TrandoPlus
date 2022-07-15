using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrandoPlus
{
    internal static class Extensions
    {
        public static void Increment<T>(this Dictionary<T, int> dict, T key, int amount = 1)
        {
            if (!dict.TryGetValue(key, out int value))
            {
                value = 0;
            }
            dict[key] = value + amount;
        }
    }
}
