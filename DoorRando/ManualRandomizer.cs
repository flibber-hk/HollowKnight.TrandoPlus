using System;
using System.Collections.Generic;
using System.Linq;
using ItemChanger;
using Modding;
using RandomizerCore;
using RandomizerCore.Extensions;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace DoorRando
{
    /// <summary>
    /// Cursed class that creates a manual pairing of doors to non-doors
    /// </summary>
    public static class ManualRandomizer
    {
        public static List<(string door, string nonDoor)> GetPairs(List<string> doors, List<string> nonDoors, string startNonDoor, Random rng)
        {
            List<(string door, string nonDoor)> pairs = new();

            // Match pleasure house
            if (startNonDoor != null)
            {
                doors.Remove($"{SceneNames.Ruins_Bathhouse}[door1]");
                nonDoors.Remove(startNonDoor);
                pairs.Add(($"{SceneNames.Ruins_Bathhouse}[door1]", startNonDoor));
            }
            else
            {
                List<string> sources = new()
                {
                    "Room_Town_Stag_Station[left1]",
                    "Ruins_Elevator[left1]",
                    "Ruins_Elevator[left2]",
                    "Ruins_House_03[left2]",
                    "Ruins_House_03[left2]"
                };
                string target = rng.Next(sources);
                doors.Remove($"{SceneNames.Ruins_Bathhouse}[door1]");
                nonDoors.Remove(target);
                pairs.Add(($"{SceneNames.Ruins_Bathhouse}[door1]", target));
            }

            rng.PermuteInPlace(doors);
            rng.PermuteInPlace(nonDoors);

            // They need to be able to unlock sly
            while (doors.IndexOf("Town[door_sly]") == nonDoors.IndexOf("Room_ruinhouse[left1]"))
            {
                rng.PermuteInPlace(doors);
                rng.PermuteInPlace(nonDoors);
            }

            foreach (var pair in doors.Zip(nonDoors, (a, b) => (a, b)))
            {
                pairs.Add(pair);
            }

            return pairs;
        }

    }
}
