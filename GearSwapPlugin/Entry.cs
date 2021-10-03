using GearSwapPlugin.GearSwap;
using HarmonyLib;
using UnityEngine;

namespace GearSwapPlugin
{
    [HarmonyPatch]
    public class Entry
    {
        private static GameObject _go;

        [HarmonyPatch(typeof(GameStateManager), "ChangeState", typeof(eGameStateName))]
        public static void Postfix(eGameStateName nextState) => StartCheatMenu(nextState);

        private static void StartCheatMenu(eGameStateName? state = null)
        {
            switch (state)
            {
                case null:
                    return;
                case eGameStateName.StopElevatorRide:
                {
                    GearSwapCore.log.LogMessage("Initializing " + GearSwapCore.NAME);

                    var gameObject = new GameObject(GearSwapCore.AUTHOR + " - " + GearSwapCore.NAME);
                    gameObject.AddComponent<GearLoadingSubject>();
                    gameObject.AddComponent<GearLoadingObserver>();
                    gameObject.AddComponent<GearSwapper>();

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