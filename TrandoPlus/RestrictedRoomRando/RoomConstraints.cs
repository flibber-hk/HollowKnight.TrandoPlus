using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using RandomizerMod.RC;
using TrandoPlus.Utils;

namespace TrandoPlus.RestrictedRoomRando
{
    /// <summary>
    /// Class encapsulating a requirement for a scene to be present if another is.
    /// </summary>
    internal static class RoomConstraints
    {
        public static IEnumerable<(string Source, string Target)> GetConstraints(RequestBuilder rb)
        {
            // Warps
            yield return (SceneNames.Abyss_05, SceneNames.White_Palace_11);
            yield return (SceneNames.White_Palace_11, SceneNames.Abyss_05);
            yield return (SceneNames.White_Palace_09, SceneNames.Abyss_05);
            yield return (SceneNames.White_Palace_03_hub, SceneNames.Abyss_05);
            yield return (SceneNames.White_Palace_20, SceneNames.White_Palace_06);
            yield return (SceneNames.Abyss_08, SceneNames.Abyss_06_Core);
            yield return (SceneNames.RestingGrounds_04, SceneNames.RestingGrounds_07);
            yield return (SceneNames.Room_Mansion, SceneNames.RestingGrounds_12);
            yield return (SceneNames.Room_Colosseum_01, SceneNames.Room_Colosseum_02);

            // Trams / Elevators
            yield return (SceneNames.Crossroads_46b, SceneNames.Crossroads_46);
            yield return (SceneNames.Crossroads_46, SceneNames.Crossroads_46b);
            yield return (SceneNames.Crossroads_49b, SceneNames.Crossroads_49);
            yield return (SceneNames.Crossroads_49, SceneNames.Crossroads_49b);
            yield return (SceneNames.Ruins2_10b, SceneNames.Ruins2_10);
            yield return (SceneNames.Ruins2_10, SceneNames.Ruins2_10b);
            yield return (SceneNames.Abyss_03, SceneNames.Abyss_03_b);
            yield return (SceneNames.Abyss_03_b, SceneNames.Abyss_03_c);
            yield return (SceneNames.Abyss_03_c, SceneNames.Abyss_03);

            // Required for standard checks to be achievable
            yield return (SceneNames.Room_shop, SceneNames.Room_ruinhouse);
            yield return (SceneNames.Room_Mansion, SceneNames.Fungus3_49);
            yield return (SceneNames.Mines_32, SceneNames.Mines_18);
            yield return (SceneNames.Fungus2_31, SceneNames.Fungus2_15);

            // Logically required for some checks
            yield return (SceneNames.Abyss_04, SceneNames.Room_Colosseum_01);
            yield return (SceneNames.Abyss_04, SceneNames.Ruins1_05b);
            yield return (SceneNames.Abyss_04, SceneNames.Ruins1_27);
            yield return (SceneNames.Room_nailmaster_03, SceneNames.Ruins1_05b);
            yield return (SceneNames.Room_nailmaster_03, SceneNames.Ruins1_27);
            yield return (SceneNames.Grimm_Divine, SceneNames.Ruins1_05b);
            yield return (SceneNames.Grimm_Divine, SceneNames.Ruins1_27);

            // Stags to Dirtmouth Stag
            yield return (SceneNames.Crossroads_47, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Fungus1_16_alt, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Fungus2_02, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Fungus3_40, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Ruins1_29, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Ruins2_08, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.RestingGrounds_09, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Deepnest_09, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Abyss_22, SceneNames.Room_Town_Stag_Station);
            yield return (SceneNames.Cliffs_03, SceneNames.Room_Town_Stag_Station);

            // Various warps to Dirtmouth
            yield return (SceneNames.Room_shop, SceneNames.Town);
            yield return (SceneNames.Grimm_Main_Tent, SceneNames.Town);

            HashSet<string> randomizedLocations = rb.EnumerateItemGroups().SelectMany(gb => gb.Locations.EnumerateDistinct()).AsHashSet();
            HashSet<string> randomizedItems = rb.EnumerateItemGroups().SelectMany(gb => gb.Items.EnumerateDistinct()).AsHashSet();

            // Required for conditional checks to be achievable
            if (randomizedLocations.Contains(LocationNames.Boss_Essence_Grey_Prince_Zote))
            {
                yield return (SceneNames.Room_Bretta, SceneNames.Deepnest_33);
                yield return (SceneNames.Room_Bretta, SceneNames.Room_Colosseum_01);
                yield return (SceneNames.Room_Bretta, SceneNames.Fungus2_23);
            }
            if (randomizedLocations.Contains(LocationNames.Boss_Essence_White_Defender))
            {
                yield return (SceneNames.Waterways_15, SceneNames.Waterways_05);
            }

            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Ancient_Basin))
            {
                yield return (SceneNames.Abyss_02, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Brumm))
            {
                yield return (SceneNames.Room_spider_small, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Fungal_Core))
            {
                yield return (SceneNames.Fungus2_30, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Hive))
            {
                yield return (SceneNames.Hive_03, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Kings_Pass))
            {
                yield return (SceneNames.Tutorial_01, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Kingdoms_Edge))
            {
                yield return (SceneNames.Deepnest_East_03, SceneNames.Grimm_Main_Tent);
            }
            if (randomizedLocations.Contains(LocationNames.Grimmkin_Flame_Resting_Grounds))
            {
                yield return (SceneNames.RestingGrounds_06, SceneNames.Grimm_Main_Tent);
            }

            if (!randomizedItems.Contains(ItemNames.Grimmchild1) 
                && !randomizedItems.Contains(ItemNames.Grimmchild2) 
                && !randomizedItems.Contains("Grimmchild"))
            {
                yield return (SceneNames.Grimm_Main_Tent, SceneNames.Cliffs_06);
            }
        }
    }
}
