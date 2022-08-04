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
    public class GearSwapConsistencyManager : MonoBehaviour
    {
        internal static bool PickUpSentryOnToolChange { get; set; } = true;

        private static readonly List<EnemyAgent> PrevEnemiesDetected = new ();
        private static readonly Dictionary<InventorySlot, float> PrevAmmoPercentageBySlot = new ();
        
        private void Start()
        {
            GearSwapManager.BeforeGearSwap += BeforeGearInstanceUnload;
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
                    break;
            }

            UpdateItemUI();
            PlayerManager.GetLocalPlayerAgent().Sync.SyncInventory(true);
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
                    if (PickUpSentryOnToolChange)
                    {
                        PickUpSentry();
                    }
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
            // Give an extra one percent to compensate for lost ammo during conversion (unless if full)
            var totalBullets = (PrevAmmoPercentageBySlot[slot] + (slotAmmoStorage.IsFull ? 0f : 0.01f)) * slotAmmoStorage.BulletsMaxCap;

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
        /// Picks up any deployed sentry in order to refund the tool refill inside the sentry
        /// </summary>
        private static void PickUpSentry()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            var toolItem = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass);

            if (toolItem.Instance is null || toolItem.Instance.TryCast<SentryGunFirstPerson>() is null || toolItem.Status != eInventoryItemStatus.Deployed) return;
            
            var sentryInstance =
                (from sentry in FindObjectsOfType<SentryGunInstance>()
                    where sentry.Owner.PlayerName == playerAgent.PlayerName
                    select sentry).FirstOrDefault();
            
            if (!(sentryInstance is null))
            {
                var interact = sentryInstance.m_interactPickup;
                interact.TriggerInteractionAction(playerAgent);
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
            var tempWield = currWielded == InventorySlot.HackingTool ? InventorySlot.GearMelee : InventorySlot.HackingTool;
            PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(tempWield);
            PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(currWielded != InventorySlot.None ? currWielded : InventorySlot.GearMelee);
        }

        private void OnDestroy()
        {
            GearSwapManager.BeforeGearSwap -= BeforeGearInstanceUnload;
            GearLoadingObserver.OnGearLoaded -= OnGearInstanceLoad;
        }
    }
}