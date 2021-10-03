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
                case eGameStateName.InLevel:
                {
                    GearSwapCore.log.LogMessage("Initializing " + GearSwapCore.MODNAME);

                    var gameObject = new GameObject(GearSwapCore.AUTHOR + " - " + GearSwapCore.MODNAME);
                    gameObject.AddComponent<GearLoadingSubject>();
                    gameObject.AddComponent<GearLoadingObserver>();
                    gameObject.AddComponent<GearSwapper>();

                    Object.DontDestroyOnLoad(gameObject);

                    _go = gameObject;
                    break;
                }
                case eGameStateName.AfterLevel:
                    GearSwapCore.log.LogMessage("Closing " + GearSwapCore.MODNAME);
                    Object.Destroy(_go);
                    break;
            }
        }
    }
}