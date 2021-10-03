using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Gear;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    public class GearLoadingObserver : MonoBehaviour
    {
        private static readonly List<EnemyAgent> PrevEnemiesDetected = new List<EnemyAgent>();
        private static readonly Dictionary<InventorySlot, float> PrevClipAmmoBySlot = new Dictionary<InventorySlot, float>();
        private static readonly List<InventorySlot> GearSlots = new List<InventorySlot> { InventorySlot.GearMelee, InventorySlot.GearStandard, InventorySlot.GearSpecial, InventorySlot.GearClass };

        public GearLoadingObserver(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
        }
        
        private void Start()
        {
            GearSwapper.BeforeGearSwap += CleanUpBio;
            GearSwapper.BeforeGearSwap += SavePrevClipAmmo;
            
            GearLoadingSubject.AfterSwapOnGearLoaded += FillClipAmmo;
            GearLoadingSubject.AfterSwapOnGearLoaded += RefreshBio;
            GearLoadingSubject.AfterSwapOnGearLoaded += PickUpSentry;
            GearLoadingSubject.AfterSwapOnGearLoaded += UpdateItemUI;
        }


        private static void SavePrevClipAmmo(InventorySlot slot)
        {
            if (slot != InventorySlot.GearStandard && slot != InventorySlot.GearSpecial) return;
            
            var localAmmoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            PrevClipAmmoBySlot[slot] = localAmmoStorage.GetClipAmmoFromSlot(slot) *
                                       localAmmoStorage.GetInventorySlotAmmo(slot).CostOfBullet;
        }

        private static void FillClipAmmo(InventorySlot slot)
        {
            if (slot != InventorySlot.GearStandard && slot != InventorySlot.GearSpecial) return;
            
            var localAmmoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            var slotAmmo = localAmmoStorage.GetInventorySlotAmmo(slot);

            if (slotAmmo.IsFull)
            {
                localAmmoStorage.SetClipAmmoInSlot(slot);
                slotAmmo.AddAmmo(PrevClipAmmoBySlot[slot]);
            }
            else
            {
                slotAmmo.AddAmmo(PrevClipAmmoBySlot[slot]);    
                localAmmoStorage.SetClipAmmoInSlot(slot);
            }
            
            localAmmoStorage.UpdateSlotAmmoUI(slot);
            localAmmoStorage.NeedsSync = true;
        }

        // Clears the enemies list in bio for tiny blips bugs when switching to and or from bio  
        private static void CleanUpBio(InventorySlot slot)
        {
            if (slot != InventorySlot.GearClass) return;
            
            var item = PlayerBackpackManager.GetLocalItem(InventorySlot.GearClass).Instance;
            if (!(item.TryCast<EnemyScanner>() is null))
            {
                var bio = item.Cast<EnemyScanner>();
                PrevEnemiesDetected.AddRange(bio.m_enemiesDetected.ToArray());
                bio.m_enemiesDetected.Clear();
            }
        }

        // Re adds the enemies list in bio for tiny blips bugs when switching to and or from bio
        private static void RefreshBio(InventorySlot slot)
        {
            var item = PlayerBackpackManager.GetLocalItem(slot).Instance;
            if (item.TryCast<EnemyScanner>() is null) return;
            
            var bio = item.Cast<EnemyScanner>();
            
            foreach (var enemy in PrevEnemiesDetected)
            {
                bio.m_enemiesDetected.Add(enemy);
            }
            PrevEnemiesDetected.Clear();
        }

        // Pick up any deployed sentry after switching weapons
        // Picking up before switching causes player to be stuck wielding nothing 
        private static void PickUpSentry(InventorySlot slot)
        {
            if (slot != InventorySlot.GearClass) return;
            
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            var sentryInstance = (from sentry in FindObjectsOfType<SentryGunInstance>() where sentry.Owner.PlayerName == playerAgent.PlayerName select sentry).FirstOrDefault();
            if (!(sentryInstance is null))
            {
                var interact = sentryInstance.m_interactPickup;
                interact.m_interactionSourceAgent = playerAgent;
                interact.TriggerInteractionAction();
            }
        }

        private static void UpdateItemUI(InventorySlot slot)
        {
            var currWielded = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;
            PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(InventorySlot.HackingTool);
            PlayerManager.GetLocalPlayerAgent().Sync.WantsToWieldSlot(currWielded);
        }

        private void OnDestroy()
        {
            GearLoadingSubject.AfterSwapOnGearLoaded -= FillClipAmmo;
            GearLoadingSubject.AfterSwapOnGearLoaded -= RefreshBio;
            GearLoadingSubject.AfterSwapOnGearLoaded -= PickUpSentry;
            GearLoadingSubject.AfterSwapOnGearLoaded -= UpdateItemUI;
            
            GearSwapper.BeforeGearSwap -= CleanUpBio;
            GearSwapper.BeforeGearSwap -= SavePrevClipAmmo;
        }
    }
}