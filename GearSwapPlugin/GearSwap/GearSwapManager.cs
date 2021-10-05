using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Gear;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    /// <summary>
    /// Subject to GearLoadingObserver
    /// Subscribes to the weapon instance loading event after gear swapping to fix various state miss match issues.
    /// Updates and cleans various equip items by saving states and or updating variables after the instance loads. 
    /// </summary>
    public class GearSwapManager : MonoBehaviour
    {
        // Allows for multiple sentries to be deployed but player will have zero tool ammo
        public static bool PickUpSentryOnToolChange { get; set; } = true;
        
        // Copies tool ammo from deployed sentry if pick up is disabled 
        public static bool CopySentryToolAmmoOnToolChange { get; set; } = false;
        
        private static readonly List<EnemyAgent> PrevEnemiesDetected = new List<EnemyAgent>();
        private static readonly Dictionary<InventorySlot, float> PrevAmmoPercentageBySlot = new Dictionary<InventorySlot, float>();

        public GearSwapManager(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
        }
        
        private void Start()
        {
            GearSwapper.BeforeGearSwap += BeforeGearInstanceUnload;
            GearLoadingObserver.OnGearLoaded += OnGearInstanceLoad;
        }

        private static void OnGearInstanceLoad(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.GearStandard:
                case InventorySlot.GearSpecial:
                    RestoreAmmoPercent(slot);
                    break;
                case InventorySlot.GearClass:
                    RefreshBio();
                    RefreshMineDeployer();
                    if (PickUpSentryOnToolChange)
                    {
                        PickUpSentry();
                    } else if (CopySentryToolAmmoOnToolChange)
                    {
                        CopySentryToolToPlayer();
                    }
                    break;
            }

            UpdateItemUI();
        }

        private static void BeforeGearInstanceUnload(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.GearStandard:
                case InventorySlot.GearSpecial:
                    SavePrevAmmoPercent(slot);
                    break;
                case InventorySlot.GearClass:
                    CleanUpBio();
                    break;
            }
        }

        private static void SavePrevAmmoPercent(InventorySlot slot)
        {
            var localAmmoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            var clipBullets = localAmmoStorage.GetClipAmmoFromSlot(slot);
            var slotAmmoStorage = localAmmoStorage.GetInventorySlotAmmo(slot);
            
            PrevAmmoPercentageBySlot[slot] = (clipBullets + slotAmmoStorage.BulletsInPack) / slotAmmoStorage.BulletsMaxCap;
        }

        private static void RestoreAmmoPercent(InventorySlot slot)
        {
            var localAmmoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            var slotAmmoStorage = localAmmoStorage.GetInventorySlotAmmo(slot);
            // Give an extra one percent to compensate for ost ammo during conversion 
            var totalBullets = (PrevAmmoPercentageBySlot[slot] + 0.01f) * slotAmmoStorage.BulletsMaxCap;

            slotAmmoStorage.AmmoInPack = totalBullets * slotAmmoStorage.CostOfBullet;
            localAmmoStorage.SetClipAmmoInSlot(slot);
            localAmmoStorage.UpdateSlotAmmoUI(slot);
            localAmmoStorage.NeedsSync = true;
        }

        /// <summary>
        /// Clears the enemy list in bio tracker before swapping to another tool.
        /// This works with RefreshBio to avoid the situation where swapping back to bio in future will cause any
        /// enemies already in the list to suffer from the tiny un-taggable blips issue
        /// </summary>
        private static void CleanUpBio()
        {
            var item = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass).Instance;
            if (!(item.TryCast<EnemyScanner>() is null))
            {
                var bio = item.Cast<EnemyScanner>();
                PrevEnemiesDetected.AddRange(bio.m_enemiesDetected.ToArray());
                bio.m_enemiesDetected.Clear();
            }
        }

        /// <summary>
        /// Adds any previous enemies detected back into the detected enemies list in the bio tracker after gear the
        /// instance loads.
        /// This works with CleanUpBio to avoid the situation where swapping back to bio in future will cause any
        /// enemies already in the list to suffer from the tiny un-taggable blips issue
        /// </summary>
        private static void RefreshBio()
        {
            var item = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass).Instance;
            if (item.TryCast<EnemyScanner>() is null) return;
            
            var bio = item.Cast<EnemyScanner>();
            
            foreach (var enemy in PrevEnemiesDetected)
            {
                bio.m_enemiesDetected.Add(enemy);
            }
            PrevEnemiesDetected.Clear();
        }

        /// <summary>
        /// Fixes issue where swapping to mine deployer after using mines causes the player to be in perpetual cool
        /// down.
        /// </summary>
        private static void RefreshMineDeployer()
        {
            var item = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass).Instance;
            if (item.TryCast<MineDeployerFirstPerson>() is null) return;
            
            var deployer = item.Cast<MineDeployerFirstPerson>();
            deployer.ShowItem();
        }

        /// <summary>
        /// Picks up any deployed sentry in order to refund the tool refill inside the sentry
        /// </summary>
        private static void PickUpSentry()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            var toolItem = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass);
            if (toolItem.TryCast<SentryGunFirstPerson>() is null ||
                toolItem.Status != eInventoryItemStatus.Deployed) return;
            
            var sentryInstance =
                (from sentry in FindObjectsOfType<SentryGunInstance>()
                    where sentry.Owner.PlayerName == playerAgent.PlayerName
                    select sentry).FirstOrDefault();

            if (!(sentryInstance is null))
            {
                var interact = sentryInstance.m_interactPickup;
                interact.m_interactionSourceAgent = playerAgent;
                interact.TriggerInteractionAction();
            }
        }

        private static void CopySentryToolToPlayer()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            var sentryInstance = (from sentry in FindObjectsOfType<SentryGunInstance>() where sentry.Owner.PlayerName == playerAgent.PlayerName select sentry).FirstOrDefault();
            if (!(sentryInstance is null))
            {
                PlayerBackpackManager.LocalBackpack.AmmoStorage.ClassAmmo.AddAmmo(sentryInstance.Ammo);
            }
        }

        /// <summary>
        /// Strange way I found to update the entire Weapons UI (not just the ammo counts)
        ///
        /// Note: When switching too many times in only a few frames, wilded item is NONE.
        /// </summary>
        private static void UpdateItemUI()
        {
            var currWielded = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;
            PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(InventorySlot.HackingTool);

            if (currWielded != InventorySlot.None)
            {
                PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(currWielded);    
            }
            else
            {
                PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(InventorySlot.GearMelee);
            }
        }

        private void OnDestroy()
        {
            GearSwapper.BeforeGearSwap -= BeforeGearInstanceUnload;
            GearLoadingObserver.OnGearLoaded -= OnGearInstanceLoad;
        }
    }
}