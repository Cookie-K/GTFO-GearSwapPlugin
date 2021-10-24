# Gear Swap API

A bepinex plugin API that allows for swapping gear whilst in an expedition

#### *Note:* this is an API for plugin developers and does not have any effect on game play on its own

## Methods and Properties
```
GearSwapManager#RequestToEquip(GearId id)
GearSwapManager#SwappableGearSlots
GearSwapManager#SetPickUpSentryOnToolChange
```

Everything needed to use this API is exposed via the `GearSwapManager` class
1. `GearSwapManager#RequestToEquip(GearId id)`: Requests to equip the given `GearId` (you can find GearIds through methods like `GearManager#GetAllGearForSlot(InventorySlot slot)`). As the name suggests this does not equip the gear right away. The swap will be postponed until certain conditions are met to avoid a plethora of issues surrounding change of weapons and tools (see **Blocking Operations** below for more details).

2. `GearSwapManager#SwappableGearSlots`: List of `InventorySlot` that this API can swap. Currently it can swap `GearMelee`, `GearStandard`, `GearSpecial`, and `GearClass`.

3. `GearSwapManager#SetPickUpSentryOnToolChange(bool pickUp)`: Sets whether to pick up any sentry guns deployed by the player when tool is swapped (default `false`). If set to `true`, any sentry gun deployed by the player will be picked up and have its tool ammo refunded to the player. Otherwise it will leave the sentry deployed even if different tool is given.

## Blocking Operations
Gear can not be swapped while these conditions are true to avoid issues and annoyances surrounding changing weapons
1. Gear is Aimed or is Firing (counts bio charging and c-foam firing)
2. Player is hacking
3. Melee not in idle

## Things To Note About Gear Swapping
- Ammo is kept consistent by the % value not by the hidden ammo value in player ammo storage
- 1% ammo is given to the player to compensate for precision errors causing players to slowly lose ammo
- Sentry guns hold the player's tool ammo and the player will have 0 tool ammo on them selves
- Deployed sentries can be picked up by the owner to recover tool ammo (even if you no longer have the sentry in your inventory)
- Picking up mines whilst holding another tool will only refund a single clip of tool and not the % amount the mine is worth

### Developed by Cookie_K



