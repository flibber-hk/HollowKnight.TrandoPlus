# HollowKnight.TrandoPlus

Extension mod for Randomizer 4 that adds alternative ways to enjoy transition rando. 
The following options are included; more may be added in later versions, and some may be removed to move to base Randomizer.

### Door Rando
- Randomize all 35 randomizable transitions which are either a door, or the vanilla target of a door.
- Door rando respects the base "coupled" setting; item rando + uncoupled + door rando will cause door transitions to be randomized and uncoupled.
- With Matched, item + door rando considers left/right transitions to be *unmatched*, so all transitions will connect a door to a non-door.
- The door in White_Palace_11 (guarded by the first Kingsmould) is excluded if Randomization In White Palace is disabled.
- This setting will have no effect in room rando, where all doors are already randomized.
- With one of the area rando variants, door rando adds its transitions to the main area rando group.

The Area Door NonInteraction setting only concerns when door rando is enabled along with one of the area rando variants. In this setting,
the randomizer will attempt to avoid matching door transitions to non-door transitions. Note that this only covers door transitions
added by door rando; for example, in full area rando, the transition Fungus3_44[door1] (overgrown mound entrance) can be considered an
area or a door transition so has no constraint. However, the transition Deepnest_East_06[door1] (the entrance to Oro's hut) is a door
transition but not an area transition, so may not be paired with an area transition - unless that transition is also a door transition.

More precisely, with AreaDoorNonInteraction enabled, any transition added by door rando must be matched with a door (or door target)
transition - though it's not enforced that transitions added by door rando must be matched to other transitions added by door rando.

### Drop Rando
- Randomize all 8 one-way drops.
- In area rando variants, drops are added to the part of the area rando pool used by the area drops.
- This setting will have no effect in room rando, where all drops are already randomized.

### Prohibit adjacent benches
- Rooms containing a bench will not be placed next to each other unless absolutely necessary.
- In area rando, this only considers benches in the room, not benches in the area.
- Rooms containing tram stations are considered bench rooms; other questionable rooms (such as Distant Village Stag, Palace Grounds and Junk Pit) are not.
- This setting has no effect in item rando (where all three White Palace bench rooms are adjacent!)
