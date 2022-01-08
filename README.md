# HollowKnight.TrandoPlus

Extension mod for Randomizer 4 that adds alternative ways to enjoy transition rando. 
The following options are included; more may be added in later versions, and some may be removed to move to base Randomizer.

### Door Rando
- Randomize all 35 randomizable transitions which are either a door, or the vanilla target of a door.
- Door rando respects the base "coupled" setting; item rando + uncoupled + door rando will cause door transitions to be randomized and uncoupled.
- In area rando variants, doors and door targets are added to the main area rando pool.
- With Matched, item + door rando considers left/right transitions to be *unmatched*, so all transitions will connect a door to a non-door.
- The door in White_Palace_11 (guarded by the first Kingsmould) is excluded if Randomization In White Palace is disabled.
- This setting will have no effect in room rando.

### Drop Rando
- Randomize all 8 one-way drops.
- In area rando variants, drops are added to the part of the area rando pool used by the area drops.
- This setting will have no effect in room rando.

### Prohibit adjacent benches
- Rooms containing a bench will not be placed next to each other unless absolutely necessary.
- In area rando, this only considers benches in the room, not benches in the area.
- Rooms containing tram stations are considered bench rooms; other questionable rooms (such as Distant Village Stag, Palace Grounds and Junk Pit) are not.
- This setting has no effect in item rando (where all three White Palace rooms are adjacent!)
