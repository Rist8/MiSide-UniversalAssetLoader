# MiSide-AssetLoader  

This project is based on a fork of the original [MiSide-AssetLoader](https://github.com/CORRUPTOR2037/MiSide-AssetLoader).  
> Этот проект основан на форке оригинального [MiSide-AssetLoader](https://github.com/CORRUPTOR2037/MiSide-AssetLoader).

---

**Important:** This mod **doesn't have** ConsoleUnlocker's features in it no more, install ConsoleUnlocker in order to use them.  
> Важно: Этот мод больше **не содержит** в себе возможностей ConsoleUnlocker, установите ConsoleUnlocker чтобы их использовать.

---

### About / О моде

**MiSide-AssetLoader** is a mod for loading assets in real time for the Unity game *MiSide*.  
> **MiSide-AssetLoader** — мод для загрузки ресурсов в реальном времени для Unity-игры *MiSide*.

- **Supports custom configurations** for adding new buttons through the **Addons** tab.  
  > Поддерживает пользовательские конфигурации для добавления новых кнопок через вкладку **Addons**.

- **Adds a free camera** with greenscreen support on f10 (note: greenscreen currently has issues).
  > Добавляет свободную камеру с поддержкой зеленого экрана на f10 *(зелёный экран пока работает неполноценно)*.

- By default has **CatEars** config, which adds cat ears to all Mitas except Chibi.  
  > По умолчанию имеет **CatEars** конфиг, который добавляет кошачьи уши всем Митам, кроме Чиби.

For a complete list of features, refer to the `readme.txt` file.  
> Полный список функций доступен в файле `readme.txt`.

---

### Installation Guide / Руководство по установке

#### **BepInEx Installation**

1. **Download** BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip (or x86 for 32-bit systems) from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).  
   > **Скачайте** BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip (или версию для x86 для 32-битных систем) с [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).

   **Direct download link:**  
   > Прямая ссылка для скачивания:  

   https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip  

2. **Extract all files** to the MiSide folder (default path: `...\SteamLibrary\steamapps\common\MiSide`).  
   > **Извлеките все файлы** в папку MiSide (по умолчанию: `...\SteamLibrary\steamapps\common\MiSide`).

   Your game directory should look like this after extraction:  
   > Папка с игрой должна выглядеть так после распаковки:

   ![image](https://github.com/user-attachments/assets/bc7d35bf-3b98-499f-8122-410911d545f2)

3. **Launch the game** and wait until the main menu appears. If BepInEx is installed successfully, a console window will open alongside the game window.  
   > **Запустите игру** и дождитесь появления главного меню. Если BepInEx установлен успешно, откроется окно консоли параллельно с игрой.

4. **Exit the game** and proceed to the plugin installation guide.  
   > **Выйдите из игры** и перейдите к руководству по установке плагина.

---

#### **Plugin Installation**

1. **Download** `UniversalAssetLoaderRelease-0.11.0.zip` from the [releases page](https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.11.0).  
   > **Скачайте** `UniversalAssetLoaderRelease-0.11.0.zip` с [страницы релизов](https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.11.0).

2. **Open** the `OuterFolder` inside the downloaded archive, and **extract** the `UniversalAssetLoader` folder into the `...\MiSide\BepInEx\plugins` directory of your game (so that `plugins\UniversalAssetLoader` path exists).
   **Locate** the `assimp.dll` file in `plugins\UniversalAssetLoader` and move it to the root folder of your game (where the game executable is located).
   > **Откройте** папку OuterFolder внутри загруженного архива и **распакуйте** папку UniversalAssetLoader в каталог `...\MiSide\BepInEx\plugins` вашей игры (так чтобы появился путь `plugins\UniversalAssetLoader`).
   > **Найдите** файл `assimp.dll` в `plugins\UniversalAssetLoader` и переместите его в корневую папку вашей игры (где находится исполняемый файл игры).

   **OR / ИЛИ**

   Unpack the archive into any folder, download `move.bat`, move it to the extracted `OuterFolder`, and run the script. This will automatically locate the MiSide folder and copy all files.  
   > Распакуйте архив в любую папку, скачайте `move.bat`, переместите его в распакованную папку `OuterFolder` и запустите скрипт. Скрипт автоматически найдет папку MiSide и скопирует все файлы.

3. **Launch the game** and check if the mod works by opening the **Clothes** menu and looking for the **Addons** tab.  
   > **Запустите игру** и проверьте, работает ли мод, открыв меню **Clothes** и найдя вкладку **Addons**.

   ![image](https://github.com/user-attachments/assets/b380ff52-5c7d-4ebe-9b85-52eda35ce9fb)

---

### Screenshots / Скриншоты

Here are some screenshots of the **CatEars** addon:  
> Вот несколько скриншотов аддона **CatEars**:

![image](https://github.com/user-attachments/assets/76c8d3f0-7bbc-484f-bddb-03db69215b1f)  
![image](https://github.com/user-attachments/assets/e6325bbf-fb06-4757-9384-e07ab47d5212)  
![image](https://github.com/user-attachments/assets/f13dd339-d0a9-4ebc-80aa-c2d0dd12bfd9)  
![image](https://github.com/user-attachments/assets/255db69d-2528-4968-8c9d-551ffab0b17e)  
