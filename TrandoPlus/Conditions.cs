using System;
using ItemChanger;
using Modding;
using RandomizerCore;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.Linq;
using TrandoPlus.Utils;

namespace TrandoPlus
{
    public static class Conditions
    {
        private class BenchConstraintHolder
        {
            private readonly RequestBuilder rb;
            private readonly HashSet<string> benchScenes;

            public BenchConstraintHolder(RequestBuilder rb, IEnumerable<string> benchScenes)
            {
                this.rb = rb;
                this.benchScenes = new(benchScenes);
            }

            public static BenchConstraintHolder Get(RequestBuilder rb)
                => new(rb, Utility.GetBenchScenes(rb));

            public bool Constraint(IRandoItem item, IRandoLocation loc)
            {
                if (!rb.TryGetTransitionDef(item.Name, out TransitionDef itemTrans)) return true;
                if (!benchScenes.Contains(itemTrans.SceneName)) return true;

                if (!rb.TryGetTransitionDef(loc.Name, out TransitionDef locTrans)) return true;
                if (!benchScenes.Contains(locTrans.SceneName)) return true;

                return false;
            }
        }

        public static Func<IRandoItem, IRandoLocation, bool> GetAdjacentBenchConstraint(RequestBuilder rb)
        {
            return BenchConstraintHolder.Get(rb).Constraint;
        }
    }
}
