using System;
using ItemChanger;
using Modding;
using RandomizerCore;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.Linq;

namespace TrandoPlus
{
    public static class Conditions
    {
        private class BenchConstraintHolder
        {
            private static readonly HashSet<string> vanillaBenchScenes = new()
            {
                SceneNames.Town,
                SceneNames.Room_nailmaster,
                SceneNames.Crossroads_30,
                SceneNames.Crossroads_47,
                SceneNames.Crossroads_04,
                SceneNames.Crossroads_ShamanTemple,
                // SceneNames.Room_Final_Boss_Atrium,
                SceneNames.Fungus1_01b,
                SceneNames.Fungus1_37,
                SceneNames.Fungus1_31,
                SceneNames.Fungus1_16_alt,
                SceneNames.Room_Slug_Shrine,
                SceneNames.Fungus1_15,
                SceneNames.Fungus3_archive,
                SceneNames.Fungus2_02,
                SceneNames.Fungus2_26,
                SceneNames.Fungus2_13,
                SceneNames.Fungus2_31,
                SceneNames.Ruins1_02,
                SceneNames.Ruins1_31,
                SceneNames.Ruins1_29,
                SceneNames.Ruins1_18,
                SceneNames.Ruins2_08,
                SceneNames.Ruins_Bathhouse,
                SceneNames.Waterways_02,
                // SceneNames.GG_Atrium,
                // SceneNames.GG_Atrium_Roof,
                // SceneNames.GG_Workshop,
                SceneNames.Deepnest_30,
                SceneNames.Deepnest_14,
                SceneNames.Deepnest_Spider_Town,
                SceneNames.Abyss_18,
                SceneNames.Abyss_22,
                SceneNames.Deepnest_East_06,
                SceneNames.Deepnest_East_13,
                SceneNames.Room_Colosseum_02,
                SceneNames.Hive_01,
                SceneNames.Mines_29,
                SceneNames.Mines_18,
                SceneNames.RestingGrounds_09,
                SceneNames.RestingGrounds_12,
                SceneNames.Fungus1_24,
                SceneNames.Fungus3_50,
                SceneNames.Fungus3_40,
                SceneNames.White_Palace_01,
                SceneNames.White_Palace_03_hub,
                SceneNames.White_Palace_06,

                // SceneNames.Room_Tram_RG,
                SceneNames.Crossroads_46,
                SceneNames.Crossroads_46b,
                // SceneNames.Room_Tram,
                SceneNames.Abyss_03,
                SceneNames.Abyss_03_b,
                SceneNames.Abyss_03_c,
            };

            private readonly RequestBuilder rb;
            private readonly HashSet<string> benchScenes;

            public BenchConstraintHolder(RequestBuilder rb, IEnumerable<string> benchScenes)
            {
                this.rb = rb;
                this.benchScenes = new(benchScenes);
            }

            public static BenchConstraintHolder Default(RequestBuilder rb)
                => new(rb, vanillaBenchScenes);

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
            BenchConstraintHolder bch = null;

            if (ModHooks.GetMod("BenchRando") is Mod)
            {
                if (TryGetBenchRandoList(rb, out List<string> benchRandoScenes))
                {
                    bch = new BenchConstraintHolder(rb, benchRandoScenes);
                }
            }

            bch ??= BenchConstraintHolder.Default(rb);

            return bch.Constraint;
        }

        // Safe to use bench rando code in this method
        private static bool TryGetBenchRandoList(RequestBuilder rb, out List<string> benchRandoScenes)
        {
            if (!BenchRando.Rando.RandoInterop.IsEnabled())
            {
                benchRandoScenes = null;
                return false;
            }

            benchRandoScenes = BenchRando.Rando.RandoInterop.LS.Benches
                .Select(benchName => BenchRando.BRData.BenchLookup[benchName].SceneName)
                .ToList();
            return true;
        }
    }
}
