using System;
using System.Collections.Generic;
using ItemChanger;
using RandomizerMod.RC;
using BossEssenceSetting = RandomizerMod.Settings.LongLocationSettings.BossEssenceSetting;

namespace TrandoPlus.RestrictedRoomRando
{
    /// <summary>
    /// Class encapsulating a requirement for a scene to be present if another is.
    /// </summary>
    public class RoomConstraint
    {
        /// <summary>
        /// The scene which is added.
        /// </summary>
        public string SourceScene { get; init; }
        
        /// <summary>
        /// The scene which must be present if the source is.
        /// </summary>
        public string TargetScene { get; init; }

        /// <summary>
        /// Return false if the constraint should be ignored.
        /// (An delegate because I'm too lazy to do it properly)
        /// </summary>
        public Func<RequestBuilder, bool> Applies { get; init; }

        public RoomConstraint(string SourceScene, string TargetScene) : this(SourceScene, TargetScene, null) { }
        public RoomConstraint(string SourceScene, string TargetScene, Func<RequestBuilder, bool> Applies)
        {
            this.SourceScene = SourceScene;
            this.TargetScene = TargetScene;

            if (Applies is not null)
            {
                this.Applies = Applies;
            }
            else
            {
                this.Applies = rb => true;
            }
        }


        public static IEnumerable<RoomConstraint> GetConstraints()
        {
            // Warps
            yield return new(SceneNames.Abyss_05, SceneNames.White_Palace_11);
            yield return new(SceneNames.White_Palace_11, SceneNames.Abyss_05);
            yield return new(SceneNames.White_Palace_09, SceneNames.Abyss_05);
            yield return new(SceneNames.White_Palace_03_hub, SceneNames.Abyss_05);
            yield return new(SceneNames.White_Palace_20, SceneNames.White_Palace_06);
            yield return new(SceneNames.Abyss_08, SceneNames.Abyss_06_Core);
            yield return new(SceneNames.RestingGrounds_04, SceneNames.RestingGrounds_07);
            yield return new(SceneNames.Room_Mansion, SceneNames.RestingGrounds_12);
            yield return new(SceneNames.Room_Colosseum_01, SceneNames.Room_Colosseum_02);

            // Trams / Elevators
            yield return new(SceneNames.Crossroads_46b, SceneNames.Crossroads_46);
            yield return new(SceneNames.Crossroads_46, SceneNames.Crossroads_46b);
            yield return new(SceneNames.Crossroads_49b, SceneNames.Crossroads_49);
            yield return new(SceneNames.Crossroads_49, SceneNames.Crossroads_49b);
            yield return new(SceneNames.Ruins2_10b, SceneNames.Ruins2_10);
            yield return new(SceneNames.Ruins2_10, SceneNames.Ruins2_10b);
            yield return new(SceneNames.Abyss_03, SceneNames.Abyss_03_b);
            yield return new(SceneNames.Abyss_03_b, SceneNames.Abyss_03_c);
            yield return new(SceneNames.Abyss_03_c, SceneNames.Abyss_03);

            // Required for standard checks to be achievable
            yield return new(SceneNames.Room_shop, SceneNames.Room_ruinhouse);
            yield return new(SceneNames.Room_Mansion, SceneNames.Fungus3_49);
            yield return new(SceneNames.Mines_32, SceneNames.Mines_18);
            yield return new(SceneNames.Fungus2_31, SceneNames.Fungus2_15);

            // Required for conditional checks to be achievable
            yield return new(SceneNames.Room_Bretta, SceneNames.Deepnest_33, HardDreamBosses);
            yield return new(SceneNames.Room_Bretta, SceneNames.Room_Colosseum_01, HardDreamBosses);
            yield return new(SceneNames.Room_Bretta, SceneNames.Fungus2_23, HardDreamBosses);
            yield return new(SceneNames.Waterways_15, SceneNames.Waterways_05, HardDreamBosses);
            yield return new(SceneNames.Abyss_02, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Room_spider_small, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Fungus2_30, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Hive_03, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Tutorial_01, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Deepnest_East_03, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.RestingGrounds_06, SceneNames.Grimm_Main_Tent, rb => rb.gs.PoolSettings.GrimmkinFlames);
            yield return new(SceneNames.Grimm_Main_Tent, SceneNames.Cliffs_06, rb => !rb.gs.PoolSettings.Charms);

            // Logically required for some checks
            yield return new(SceneNames.Abyss_04, SceneNames.Room_Colosseum_01);
            yield return new(SceneNames.Abyss_04, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Abyss_04, SceneNames.Ruins1_27);
            yield return new(SceneNames.Room_nailmaster_03, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Room_nailmaster_03, SceneNames.Ruins1_27);
            yield return new(SceneNames.Grimm_Divine, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Grimm_Divine, SceneNames.Ruins1_27);

            // Stags to Dirtmouth Stag
            yield return new(SceneNames.Crossroads_47, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Fungus1_16_alt, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Fungus2_02, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Fungus3_40, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Ruins1_29, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Ruins2_08, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.RestingGrounds_09, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Deepnest_09, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Abyss_22, SceneNames.Room_Town_Stag_Station);
            yield return new(SceneNames.Cliffs_03, SceneNames.Room_Town_Stag_Station);

            // Various warps to Dirtmouth
            yield return new(SceneNames.Room_shop, SceneNames.Town);
            yield return new(SceneNames.Grimm_Main_Tent, SceneNames.Town);

         }

        private static bool HardDreamBosses(RequestBuilder rb)
            => rb.gs.PoolSettings.BossEssence
                && (rb.gs.LongLocationSettings.BossEssenceRando == BossEssenceSetting.ExcludeAllDreamWarriors
                    || rb.gs.LongLocationSettings.BossEssenceRando == BossEssenceSetting.All);
    }
}
