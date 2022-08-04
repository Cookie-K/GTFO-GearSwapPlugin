using GearSwapPlugin.GearSwap;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GearSwapPlugin
{
    [HarmonyPatch]
    public class Entry
    {
        private static GameObject _go;

        [HarmonyPatch(typeof(GameStateManager), "ChangeState", typeof(eGameStateName))]
        private static void Postfix(eGameStateName nextState) => AddGearSwappingComponents(nextState);

        private static void AddGearSwappingComponents(eGameStateName? state = null)
        {
            switch (state)
            {
                case null:
                    return;
                case eGameStateName.StopElevatorRide:
                {
                    GearSwapCore.log.LogMessage("Initializing " + GearSwapCore.NAME);

                    var gameObject = new GameObject(GearSwapCore.AUTHOR + " - " + GearSwapCore.NAME);
                    gameObject.AddComponent<GearLoadingObserver>();
                    gameObject.AddComponent<GearSwapConsistencyManager>();
                    gameObject.AddComponent<GearSwapManager>();

                    Object.DontDestroyOnLoad(gameObject);

                    _go = gameObject;
                    break;
                }
                case eGameStateName.AfterLevel:
                    GearSwapCore.log.LogMessage("Closing " + GearSwapCore.NAME);
                    Object.Destroy(_go);
                    break;
            }
        }
    }
}