using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using Coffee.UIEffects;
using UnityEngine.UI;
using HarmonyLib;

// Hook all in-game loaded assets so users wont have to press f5

// The video games minigame (milk and penguin)
[HarmonyPatch(typeof(MinigamesTelevisionController), "StartGame")]
class TelevisionGamePatch
{
    static void Prefix(MinigamesTelevisionController __instance)
    {
        Plugin.PatchAssetsSync();
    }
}

//  Racing minigame, Manekin minigame, and sukubus
[HarmonyPatch(typeof(MinigamesAutomate), "StartGame")]
class GameAutomatePatch
{
    static void Postfix(MinigamesTelevisionController __instance)
    {
        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
        Plugin.PatchAssetsSync();
    }
}