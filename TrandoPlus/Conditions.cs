using ItemChanger;
using RandomizerCore;
using RandomizerMod.RandomizerData;
using System.Collections.Generic;

namespace TrandoPlus
{
    public static class Conditions
    {
        private static readonly HashSet<string> benchScenes = new()
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
            // SceneNames.GG_Atrium_Roof, // Doesn't even exist
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
            // SceneNames.Room_Tram,
        };

        public static bool IsBenchScene(string sceneName) => benchScenes.Contains(sceneName);

        public static bool AdjacentBenchConstraint(IRandoItem item, IRandoLocation loc)
        {
            if (!Data.IsTransition(item.Name)) return true;
            if (!Conditions.IsBenchScene(Data.GetTransitionDef(item.Name).SceneName)) return true;

            if (!Data.IsTransition(loc.Name)) return true;
            if (!Conditions.IsBenchScene(Data.GetTransitionDef(loc.Name).SceneName)) return true;

            return false;
        }
    }
}
