using System.IO.Compression;
using System.Net;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Drawing;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClothesMenuPatcher{
    public static void Run()
    {
        try
        {
            CreateMenuTab();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error while creating menu tab\n" + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    private static GameObject _addonButtonPrefab;
    private static Dictionary<string, GameObject> addonButtons = new Dictionary<string, GameObject>();

    private static void CreateMenuTab(){
        var clothesMenu = Reflection.FindObjectsOfType<MenuClothes>()[0].gameObject;

        var tabs = new GameObject("Tabs");
        var rect = tabs.AddComponent<RectTransform>();
        rect.SetParent(clothesMenu.transform);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition3D = new Vector3(200, 200, 0);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(500, 64);

        var clothesButton = new GameObject("ClothesTabButton");
        rect = clothesButton.AddComponent<RectTransform>();
        rect.SetParent(tabs.transform);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition3D = new Vector3(0, 0, 0);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(200, 64);
        var text = clothesMenu.transform.Find("Text");
        text.SetParent(rect);
        text.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, 0, 0);
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 64);
        UnityEngine.Object.Destroy(text.GetComponent<UI_Colors>());
        var button1 = clothesButton.AddComponent<UnityEngine.UI.Button>();
        button1.targetGraphic = button1.gameObject.AddComponent<UnityEngine.UI.Image>();
        button1.targetGraphic.color = new UnityEngine.Color(1,1,1, 0.005f);

        var addonsButton = GameObject.Instantiate(clothesButton, tabs.transform);
        addonsButton.name = "AddonsTabButton";
        rect = addonsButton.GetComponent<RectTransform>();
        rect.anchoredPosition3D = new Vector3(220, 0, 0);
        rect.sizeDelta = new Vector2(200, 64);
        rect.localScale = Vector3.one;
        text = rect.Find("Text");
        UnityEngine.Object.Destroy(text.GetComponent<Localization_UIText>());
        text.GetComponent<UnityEngine.UI.Text>().text = "Addons";
        var button2 = addonsButton.GetComponent<UnityEngine.UI.Button>();

        var addonsList = clothesMenu.transform.parent.Find("Location OptionsChange").GetComponentInChildren<ScrollRect>().gameObject;
        addonsList = GameObject.Instantiate(addonsList, clothesMenu.transform);
        addonsList.gameObject.name = "AddonsList";
        rect = addonsList.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition3D = new Vector3(0, 125, 0);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(530, 300);
        var content = addonsList.GetComponentInChildren<MenuScrolac>().transform;
        UnityEngine.Object.Destroy(content.GetComponent<MenuScrolac>());
        UnityEngine.Object.Destroy(content.Find("Change").gameObject);
        UnityEngine.Object.Destroy(content.Find("ChangeTarget").gameObject);
        _addonButtonPrefab = content.GetChild(2).gameObject;
        _addonButtonPrefab.GetComponent<RectTransform>().Find("Text").GetComponent<UnityEngine.UI.Text>().text = "Default";
        _addonButtonPrefab.SetActive(false);
        CreateAddonButtons();
        //var button3 = _addonButtonPrefab.AddComponent<UnityEngine.UI.Button>();
        //button3.onClick.AddListener((UnityAction)LogOnClick);


        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        addonsList.SetActive(false);
        
        void ShowClothesTab(){
            button1.interactable = false;
            button2.interactable = true;
            for (int i = 0; i < clothesMenu.transform.childCount; i++){
                var cloth = clothesMenu.transform.GetChild(i);
                if (cloth.name.StartsWith("CaseCloth"))
                    cloth.gameObject.SetActive(true);
            }
            addonsList.SetActive(false);
        }

        void ShowAddonsTab(){
            button1.interactable = true;
            button2.interactable = false;
            for (int i = 0; i < clothesMenu.transform.childCount; i++){
                var cloth = clothesMenu.transform.GetChild(i);
                if (cloth.name.StartsWith("CaseCloth"))
                    cloth.gameObject.SetActive(false);
            }
            addonsList.SetActive(true);
        }

        button1.onClick.AddListener((UnityAction) ShowClothesTab);
        button2.onClick.AddListener((UnityAction) ShowAddonsTab);

        var uiColors = tabs.AddComponent<UI_Colors>();
        uiColors.ui_images = new();
        uiColors.ui_imagesColor = new();
        uiColors.ui_text = new();
        uiColors.ui_textColor = new();

        var ml = clothesMenu.GetComponent<MenuLocation>();
        ml.objects.Clear();
        ml.objects.Add(tabs.GetComponent<RectTransform>());
    }
    public static void LogOnClick(string name){
        bool active = !Plugin.Active[name];
        if (Plugin.currentSceneName == "SceneMenu")
        {
            Debug.Log("clicked: " + name);
            addonButtons[name].GetComponent<RectTransform>().Find("Text").GetComponent<Text>().text = name + ((!active) ? "" : "(*)");
        }
        MitaClothesResource clothes =
            Reflection.FindObjectsOfType<MenuClothes>()[0].resourceClothes.GetComponent<MitaClothesResource>();

        Dictionary<string, DataClothMita> clothesDict = new Dictionary<string, DataClothMita>();
        foreach (var cloth in clothes.clothes)
            clothesDict[cloth.fileSave] = cloth;


        Debug.Log(name + " is active:" + active);
        Plugin.Active[name] = active;
        string filePath = PluginInfo.AssetsFolder + "/addons_config.txt";
        try{
            using (StreamReader sr = new StreamReader(filePath)){
                string line, currentName = "";
                while ((line = sr.ReadLine()) != null){
                    // Ignore empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;
                    if (line.StartsWith("*")){
                        currentName = line.Substring(1);
                        continue;
                    }
                    if (currentName == name){
                        if (line == "trailer") {
                            if (!active)
                                GlobalGame.trailer = false;
                            else
                                GlobalGame.trailer = true;
                            continue;
                        }
                        if (line == "halloween"){
                            if (!active)
                                GlobalGame.halloween = false;
                            else
                                GlobalGame.halloween = true;
                            continue;
                        }if (line == "christmas"){
                            if (!active)
                                GlobalGame.christmas = false;
                            else
                                GlobalGame.christmas = true;
                            continue;
                        }
                        if (line.StartsWith("$")){
                            Plugin.ConsoleEnter(line.Substring(1));
                            continue;
                        }
                        if (!active){
                            if (!line.StartsWith("-")){
                                string[] parts1 = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                Plugin.assetCommands.RemoveAll(command =>
                                    command.name == parts1[0] && Enumerable.SequenceEqual(command.args, parts1.Skip(1).ToArray()));
                                continue;
                            }
                            line = line.Substring(1);
                        } else if (line.StartsWith("-")){
                            string[] parts1 = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Plugin.assetCommands.RemoveAll(command =>
                                command.name == parts1[0] && Enumerable.SequenceEqual(command.args, parts1.Skip(1).ToArray()));
                            continue;
                        }
                        // Split line on commands with arguments list
                        string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        Plugin.assetCommands.RemoveAll(command =>
                            command.name == parts[0] && Enumerable.SequenceEqual(command.args,parts.Skip(1).ToArray()));
                        Plugin.assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
                    }
                }
                Plugin.FindMita();
            }
        }
        catch (Exception e){
            Console.WriteLine("Error: " + e.Message);
        }
    }
    static void CreateAddonButtons() {
        string filePath = PluginInfo.AssetsFolder + "/addons_config.txt";
        try{
            using (StreamReader sr = new StreamReader(filePath)){
                string line;
                while ((line = sr.ReadLine()) != null){
                    if (!line.StartsWith("*"))
                        continue;
                    addonButtons[line.Substring(1)] = GameObject.Instantiate(_addonButtonPrefab, _addonButtonPrefab.transform.parent);
                    addonButtons[line.Substring(1)].SetActive(true);
                    var button = addonButtons[line.Substring(1)].AddComponent<UnityEngine.UI.Button>();

                    string line1 = line.Substring(1);
                    button.onClick.AddListener((UnityAction)(() => { LogOnClick(line1); }));
                    if(!Plugin.Active.ContainsKey(line1))
                        Plugin.Active.Add(line1, false);
                    if (line1 == "TrailerMode")
                        Plugin.Active[line1] = GlobalGame.trailer;
                    if (line1 == "Halloween")
                        Plugin.Active[line1] = GlobalGame.halloween;
                    if (line1 == "Christmas")
                        Plugin.Active[line1] = GlobalGame.christmas;

                    var rect = addonButtons[line.Substring(1)].GetComponent<RectTransform>();
                    rect.anchoredPosition3D += new Vector3(0, 40 * (addonButtons.Count - 1), 0);
                    addonButtons[line.Substring(1)].GetComponent<RectTransform>().Find("Text").GetComponent<UnityEngine.UI.Text>().text = line.Substring(1) + ((!Plugin.Active[line1]) ? "" : "(*)");
                }
            }
        }
        catch (Exception e){
            Console.WriteLine("Error: " + e.Message);
        }
    }
}