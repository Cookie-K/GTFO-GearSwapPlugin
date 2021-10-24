using System;
using Gear;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    public static class GearEquipValidator
    {
        private const float TimeForWeaponToIdle = 0.1f;
        private static float _timeSinceLastTrigger = TimeForWeaponToIdle;
        
        /// <summary>
        /// Validates if a weapon can be equipped this frame
        /// For a new weapon to be equipped the following conditions must be met:  
        /// 
        /// - Melee must not be in use (in idle state)
        /// - Weapon must not be in fire or aim (there's also slight delay added after weapon not in use to consider weapons at idle to prevent audio bugs)
        /// - Player must not be hacking
        /// - C-Foam can not be spraying
        /// - Player can not be carrying in level item (cell, turbine, etc.)
        /// - Player can not be interacting (pick up item, climb ladder, etc.) 
        /// </summary>
        /// <returns>true if weapon can be equipped, false other wise</returns>
        public static bool CanEquipNow()
        {
            var canEquipNow = true;

            canEquipNow &= IsMeleeNotInUse();
            canEquipNow &= IsWeaponIdle();
            canEquipNow &= IsNotHacking();
            canEquipNow &= IsFoamNotFiring();
            canEquipNow &= IsNotCarryingLevelItem();
            canEquipNow &= IsNotInteracting();

            return canEquipNow;
        }

        private static bool IsMeleeNotInUse()
        {
            var item = PlayerBackpackManager.GetLocalItem(InventorySlot.GearMelee);
            if (!item.IsLoaded) return false;

            var weaponState = item.Instance.Cast<MeleeWeaponFirstPerson>().CurrentStateName;
            return weaponState == eMeleeWeaponState.Idle || weaponState == eMeleeWeaponState.None;
        }

        private static bool IsInAimOrFire()
        {
            var wieldedSlot = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;

            if (wieldedSlot == InventorySlot.GearMelee || wieldedSlot == InventorySlot.GearStandard ||
                wieldedSlot == InventorySlot.GearSpecial || wieldedSlot == InventorySlot.GearClass)
            {
                var wieldedItem = PlayerBackpackManager.GetLocalItem(wieldedSlot);
                if (wieldedItem.IsLoaded && !(wieldedItem.Instance.TryCast<ItemEquippable>() is null))
                {
                    var item = wieldedItem.Instance.Cast<ItemEquippable>();
                    return item.FireButton || item.FireButtonPressed ||
                           item.AimButtonHeld || item.AimButtonPressed;
                }
            }
            return false;
        }

        /// <summary>
        /// Gives the weapons some time to update after use
        /// Resolves issues surrounding gear swapping immediately after the use of a bio tracker or mine deployer 
        /// </summary>
        /// <returns>true if weapons have not been in use for the time defined in TimeForWeaponToIdle, false other wise</returns>
        private static bool IsWeaponIdle()
        {
            if (IsInAimOrFire())
            {
                _timeSinceLastTrigger = TimeForWeaponToIdle;
                return false;
            }

            _timeSinceLastTrigger -= Time.deltaTime;
            return _timeSinceLastTrigger <= 0;
        }

        private static bool IsFoamNotFiring()
        {
            var toolItem = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass);
            var isGlueGun = !(toolItem.Instance.TryCast<GlueGun>() is null);
            return !(isGlueGun && (toolItem.Instance.Cast<GlueGun>().m_maxPressureMet || toolItem.Instance.Cast<GlueGun>().m_firing));
        }

        private static bool IsNotHacking()
        {
            var currWielded = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;
            return !(currWielded == InventorySlot.HackingTool && PlayerBackpackManager.GetLocalItem(InventorySlot.HackingTool).Instance.Cast<HackingTool>().IsBusy);
        }

        private static bool IsNotCarryingLevelItem()
        {
            return PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot != InventorySlot.InLevelCarry;
        }
        
        private static bool IsNotInteracting()
        {
            return !PlayerManager.GetLocalPlayerAgent().Interaction.HasWorldInteraction;
        }
    }
}