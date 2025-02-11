using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using Coffee.UIEffects;
using UnityEngine.UI;

public class SceneHandler
{
    public static string? currentSceneName;
    public static bool synch = false;
    private static int gameObjectCount = 0;
    private static Transform sceneObjectTransform = null;

    public static void OnSceneChanged()
    {
        try
        {
            if (currentSceneName == "SceneMenu")
            {
                synch = true;
                PatchMenuScene();
            }
            else if (currentSceneName == "SceneLoading")
            {
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(SceneLoading());
            }
            else if (currentSceneName == "Scene 12 - Freak")
            {
                GameObject.Find("World/Quest/Quest 1/Trigger EnterCutscene")
                    .GetComponent<Trigger_Event>().eventEnter
                    .AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        UnityEngine.Debug.Log("[INFO] Cutscene triggered, patching new Mitas...");
                        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
                    }));
            }
            else if (currentSceneName == "Scene 6 - BasementFirst")
            {
                GameObject.Find("World/Act/ContinueScene/TriggerEnter")
                    .GetComponent<Trigger_Event>().eventEnter
                    .AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        UnityEngine.Debug.Log("[INFO] Scene continuation triggered, patching new Mitas...");
                        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
                    }));
            }

            UnityEngine.Debug.Log($"[INFO] Scene changed to: {currentSceneName}, synch is {synch}.");
            Plugin.globalAppliedCommands.Clear();
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.PatchAssets());
            if (synch)
            {
                Plugin.FindMita();
                Plugin.FindPlayer();
            }
            else
            {
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindPlayerCoroutine());
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[ERROR] {e}");
        }
    }

    private static void PatchMenuScene()
    {
        sceneObjectTransform = GameObject.Find("MenuGame/Scene").transform;
        gameObjectCount = sceneObjectTransform.childCount;
        UnityEngine.Debug.Log($"[INFO] Patching game scene.");

        var hide_ver = ConsoleCommandHandler.assetCommands
            .FirstOrDefault<(string? name, string[]? args)>(item => item.name == "hide_game_version", (null, null));
        var hide_glowing = ConsoleCommandHandler.assetCommands
            .FirstOrDefault<(string? name, string[]? args)>(item => item.name == "hide_glowing_effect", (null, null));
        var command = ConsoleCommandHandler.assetCommands
            .FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_logo", (null, null));

        if (command.name != null)
        {
            var animators = Reflection.FindObjectsOfType<Animator>(true);
            GameObject gameName = null;
            foreach (var obj in animators)
            {
                if (obj.name == "NameGame")
                {
                    gameName = obj.Cast<Animator>().gameObject;
                    UnityEngine.GameObject.Destroy(obj);
                    break;
                }
            }

            for (int i = 0; i < gameName.transform.childCount; i++)
            {
                var tr = gameName.transform.GetChild(i);
                if (tr.name == "Background")
                {
                    Texture2D tex = AssetLoader.loadedTextures[command.args[0]];
                    if (hide_glowing.name != null)
                        UnityEngine.Object.Destroy(Reflection.GetComponent<UIShiny>(tr));
                    Plugin.logo = Reflection.GetComponent<Image>(tr);
                    Plugin.logo.preserveAspect = true;
                    Plugin.logo.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2.0f);
                    Reflection.GetComponent<RectTransform>(tr).sizeDelta = new Vector2(1600, 400);
                }
                else if (tr.name != "TextVersion")
                {
                    tr.gameObject.SetActive(false);
                }
                else
                {
                    if (hide_ver.name == null)
                    {
                        Color color = Reflection.GetComponent<Text>(tr).color;
                        color.a = 1;
                        Reflection.GetComponent<Text>(tr).color = color;
                    }
                }
            }
        }

        command = ConsoleCommandHandler.assetCommands
            .FirstOrDefault<(string? name, string[]? args)>(item => item.name == "resize_logo", (null, null));
        if (command.name != null)
        {
            Reflection.GetComponent<RectTransform>(Plugin.logo.transform).localScale =
                new Vector3(float.Parse(command.args[0]), float.Parse(command.args[1]), 1);
        }

        command = ConsoleCommandHandler.assetCommands
            .FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_music", (null, null));
        if (command.name != null)
        {
            var musicSources = Reflection.FindObjectsOfType<AudioSource>(true);
            foreach (var source in musicSources)
            {
                if (source.name == "Music")
                {
                    source.clip = AssetLoader.loadedAudio[command.args[0]];
                    source.Play();
                    break;
                }
            }
        }

        ClothesMenuPatcher.Run();

        UnityEngine.Debug.Log($"[INFO] Game scene patching completed.");

        if (Plugin.startup)
        {
            Plugin.ReadActiveAddons();
            Plugin.startup = false;
        }
    }

    public static IEnumerator SceneLoading()
    {
        // Start the scene loading process
        UnityEngine.Debug.Log("Waiting for assets loading...");

        var sceneLoading = Reflection.FindObjectsOfType<SceneLoading>(true)[0];

        bool initialState = sceneLoading.loadReady;

        // Wait until assets are loaded
        while (!Plugin.loaded)
        {
            if (sceneLoading.loadReady)
                initialState = true;
            sceneLoading.loadReady = false;
            sceneLoading.go = false;
            yield return null; // Keep yielding until assets are fully loaded
        }

        sceneLoading.loadReady = initialState;
        sceneLoading.go = initialState;

        UnityEngine.Debug.Log("Assets have been loaded, activating scene...");
    }

    public static void HandleSceneMenu()
    {
        if (Plugin.logo != null)
            Plugin.logo.color = Color.white;

        if (sceneObjectTransform.childCount != gameObjectCount)
        {
            gameObjectCount = sceneObjectTransform.childCount;
            if (sceneObjectTransform.childCount == 6 && !sceneObjectTransform.GetChild(5).gameObject.name.Contains("Particle"))
            {
                if (synch)
                {
                    Plugin.FindMita();
                }
                else
                {
                    UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
                }
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.PatchAssets());
            }
        }
    }
}
