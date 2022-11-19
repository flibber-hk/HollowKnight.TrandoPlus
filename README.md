# HollowKnight.TrandoPlus

Extension mod for Randomizer 4 that adds alternative ways to enjoy transition rando. 
The following options are included; more may be added in later versions, and some may be removed to move to base Randomizer.

### Extra transitions

The following options are available:
- Randomize all 35 randomizable transitions which are either a door, or the vanilla target of a door.
- Randomize all 107 randomizable transitions which are either the only randomizable transition in their room, or the vanilla target of such a transition. ("Dead Ends")
- Randomize all 8 one-way drops.

Some notes:
- All of the above settings have no effect in Room Rando, where all transitions are already randomized.
- The Enforce Transition Grouping setting attempts to make sure that transitions lead to transitions that are "compatible". For instance,
in area + door rando, the transition between Dirtmouth and Crystal Peaks (an area transition) can not be matched with the transition
between Crossroads and Salubra's shop (a door transition. But the transition between Fog Canyon and Overgrown Mound could be matched with
either of the above, as it is both an area and a door transition.
- The matched and coupled settings are respected. If the only set of extra transitions randomized is door rando, then the matching
will always pair doors with non-doors (unless unmatched is chosen).
- Dead End rando can take a lot of attempts to generate. Switching off Matched can reduce this number. If it is taking a lot of attempts,
it might be worth trying a different seed.

### Prohibit adjacent benches
- Rooms containing a bench will not be placed next to each other unless absolutely necessary.
- In area rando, this only considers benches in the room, not benches in the area.
- Rooms containing tram stations are considered bench rooms; other questionable rooms (such as Distant Village Stag, Palace Grounds and Junk Pit) are not.
- This setting has no effect in item rando (where all three White Palace bench rooms are adjacent!)

### Limited Room Rando

This is a modifier to room rando that causes the number of rooms in Hallownest to be reduced. The following two options are available:
- Remove Empty Rooms: removes almost all rooms without a randomized location.
- Remove Random Rooms: removes (or sometimes adds, if Remove Empty Rooms is active) rooms to ensure that the proportion of rooms available is roughly the provided value.
- Ensure Bench Rooms: Prevents seeds where the proportion of rooms with benches is much lower than it would be in regular room rando.

Some notes:
- The Black Egg Temple room will always be present.
- These settings are only applied if full room rando is enabled in Randomizer (the Coupled and Matched settings can be set to any options though).
- RandoPlus must be installed for these options to appear - none of the settings in RandoPlus need to be active though.
- If any rooms with checks are removed, then the items will be distributed among the remaining locations (so there may be multiple checks per location).
- The PreferMultiShiny setting in RandoPlus is respected.
- With some combinations of settings (for example the default Randomizer pool settings) it may take the randomizer many attempts to generate the placements.
- A log is created in the Randomizer logs folder listing which rooms are present.
- These settings are not compatible with certain vanilla pools (such as vanilla skills or keys).