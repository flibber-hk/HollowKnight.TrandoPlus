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

            // Logically required for some checks
            yield return new(SceneNames.Abyss_04, SceneNames.Room_Colosseum_01);
            yield return new(SceneNames.Abyss_04, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Abyss_04, SceneNames.Ruins1_27);
            yield return new(SceneNames.Room_nailmaster_03, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Room_nailmaster_03, SceneNames.Ruins1_27);
            yield return new(SceneNames.Grimm_Divine, SceneNames.Ruins1_05b);
            yield return new(SceneNames.Grimm_Divine, SceneNames.Ruins1_27);

            // Ruins1_23[top1] needs to be reachable in uncoupled room rando; without precise movement (coming from above), this requires Ruins1_30 so the floor can be broken.
            // Remove this constraint if the logic for Ruins1_23[top1] is modified to include Ruins1_23[top1] + LEFTCLAW + RIGHTSUPERDASH + WINGS (for example).
            yield return new(SceneNames.Ruins1_23, SceneNames.Ruins1_30, rb => !rb.gs.SkipSettings.PreciseMovement);
        }

        private static bool HardDreamBosses(RequestBuilder rb)
            => rb.gs.PoolSettings.BossEssence
                && (rb.gs.LongLocationSettings.BossEssenceRando == BossEssenceSetting.ExcludeAllDreamWarriors
                    || rb.gs.LongLocationSettings.BossEssenceRando == BossEssenceSetting.All);
    }
}
