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
        /// </summary>
        /// <returns>true if weapon can be equipped, false other wise</returns>
        public static bool CanEquipNow()
        {
            var canEquipNow = true;

            canEquipNow &= IsMeleeNotInUse();
            canEquipNow &= IsWeaponIdle();

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
            var isLoaded = PlayerBackpackManager.GetLocalItem(wieldedSlot).IsLoaded;
            var wieldedItem = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedItem;
            return isLoaded && (wieldedItem.FireButton || wieldedItem.FireButtonPressed || 
                                wieldedItem.AimButtonHeld || wieldedItem.AimButtonPressed);
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
    }
}