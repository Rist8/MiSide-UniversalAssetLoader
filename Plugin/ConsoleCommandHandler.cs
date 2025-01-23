using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using System.Linq;

public class ConsoleCommandHandler
{
    public static List<(string name, string[] args)> assetCommands;

    public static void Initialize()
    {
        ConsoleMain.active = true;
        ConsoleMain.eventEnter = new UnityEvent();
        ConsoleMain.eventEnter.AddListener((UnityAction)(() => { ConsoleEnter(ConsoleMain.codeEnter); }));
    }

    public static void ConsoleEnter(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return;

        string[] parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
            return;

        CapitalizeKeywords(parts);

        UnityEngine.Debug.Log($"[INFO] Console in: {s}");

        assetCommands.RemoveAll(command =>
            command.name == parts[0] && command.args.SequenceEqual(parts.Skip(1)));

        if (!s.StartsWith("-"))
        {
            assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
        }

        if (SceneHandler.synch)
        {
            Plugin.FindMita();
        }
        else
        {
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Plugin.FindMitaCoroutine());
        }

        assetCommands.RemoveAll(command =>
            command.name == parts[0] && command.args.SequenceEqual(parts.Skip(1)));

        if (parts[0] == "greenscreen")
        {
            GreenScreenHandler.HandleGreenScreenCommand(parts);
        }

        Plugin.HandleAddonConfig(s);
    }

    private static void CapitalizeKeywords(string[] parts)
    {
        for (int i = 1; i < parts.Length; ++i)
        {
            switch (parts[i])
            {
                case "mita":
                case "sweater":
                case "attribute":
                case "skirt":
                case "pantyhose":
                case "body":
                case "hair":
                case "head":
                case "shoes":
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                    break;
            }
        }
    }
}
