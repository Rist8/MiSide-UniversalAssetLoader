
# MiSide-AssetLoader  

This project is based on a fork of the original [MiSide-AssetLoader](https://github.com/CORRUPTOR2037/MiSide-AssetLoader).  
*Этот проект основан на форке оригинального [MiSide-AssetLoader](https://github.com/CORRUPTOR2037/MiSide-AssetLoader).*  

---

**Important:** This mod **will conflict** with ConsoleUnlocker's key bindings.  
*Важно: Этот мод **будет конфликтовать** с биндами ConsoleUnlocker.*  

You have to delete ConsoleUnlocker before using UniversalAssetLoader for full functionality.  
*Необходимо удалить ConsoleUnlocker перед использованием UniversalAssetLoader для полной функциональности.*  

Path to delete ConsoleUnlocker:  
`...\MiSide\BepInEx\plugins\ConsoleUnlocker`  
*Путь для удаления ConsoleUnlocker:  
`...\MiSide\BepInEx\plugins\ConsoleUnlocker`*  

---

### About / О моде

**MiSide-AssetLoader** is a mod for loading assets in real time for the Unity game *MiSide*.  
**MiSide-AssetLoader** — мод для загрузки ресурсов в реальном времени для Unity-игры *MiSide*.

- **Supports custom configurations** for adding new buttons through the **Addons** tab.  
  > Поддерживает пользовательские конфигурации для добавления новых кнопок через вкладку **Addons**.

- **Includes all features** from [MiSide-Console-Unlocker](https://github.com/Rist8/MiSide-Console-Unlocker).  
  > Включает все функции из [MiSide-Console-Unlocker](https://github.com/Rist8/MiSide-Console-Unlocker).

- **Adds a free camera** with greenscreen support (note: greenscreen currently has issues).  
  > Добавляет свободную камеру с поддержкой зеленого экрана *(зелёный экран пока работает некорректно)*.

- The **default CatEars configuration** adds cat ears to all Mitas except Chibi.  
  > Конфигурация CatEars по умолчанию добавляет кошачьи уши всем Митам, кроме Чиби.

For a complete list of features, refer to the `readme.txt` file.  
> Полный список функций доступен в файле `readme.txt`.
---

## Installation / Установка  

### Step 1: Install BepInEx / Установите BepInEx  

1. Download **BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip** (or for x86 systems, use the x86 version).  
   *Скачайте **BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip** (или для систем x86 используйте x86 версию).*  

   [Download link / Прямая ссылка](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)  

2. Extract all files to the MiSide folder (default: `...\SteamLibrary\steamapps\common\MiSide`).  
   *Извлеките все файлы в папку MiSide (по умолчанию: `...\SteamLibrary\steamapps\common\MiSide`).*  

   Example folder structure after extraction:  
   *Пример структуры папок после распаковки:*  
   ![image](https://github.com/user-attachments/assets/bc7d35bf-3b98-499f-8122-410911d545f2)  

3. Launch the game and wait for the main menu. If installed successfully, a **BepInEx console window** will open alongside the game.  
   *Запустите игру и дождитесь загрузки главного меню. Если установка прошла успешно, откроется окно консоли **BepInEx** вместе с игрой.*  

4. Exit the game and proceed to the **Plugin Installation** section.  
   *Закройте игру и перейдите к разделу **Установка плагина**.*  

---

### Step 2: Install the Plugin / Установка плагина  

1. Download the `UniversalAssetLoader.zip` file from [here](https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0).  
   *Скачайте файл `UniversalAssetLoader.zip` [здесь](https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0).*  

2. Extract `OuterFolder\UniversalAssetLoader` to `...\MiSide\BepInEx\plugins`.  
   *Извлеките папку `OuterFolder\UniversalAssetLoader` в `...\MiSide\BepInEx\plugins`.*  

3. Move `assimp.dll` manually to `...\MiSide`.  
   *Переместите файл `assimp.dll` вручную в `...\MiSide`.*  

**Alternative Installation / Альтернативная установка:**  
   - Unpack the archive to any folder.  
   - *Распакуйте архив в любую папку.*  

   - Download and run the `move.bat` script in the extracted `OuterFolder`.  
   - *Скачайте и запустите скрипт `move.bat` в распакованной папке `OuterFolder`.*  

   The script will try to find the MiSide folder and copy all necessary files automatically.  
   *Скрипт попытается найти папку MiSide и автоматически скопировать все нужные файлы.*  

4. Launch the game and check if the **Addons** tab appears in the **Clothes** menu.  
   *Запустите игру и проверьте, появилась ли вкладка **Addons** в меню **Clothes**.*  

![image](https://github.com/user-attachments/assets/b380ff52-5c7d-4ebe-9b85-52eda35ce9fb)  

---

## Additional Screenshots / Дополнительные скриншоты  

### CatEars Addon / Аддон CatEars  
Here are some screenshots of the **CatEars** addon:  
*Вот несколько скриншотов аддона **CatEars**:*  

![image](https://github.com/user-attachments/assets/76c8d3f0-7bbc-484f-bddb-03db69215b1f)  
![image](https://github.com/user-attachments/assets/e6325bbf-fb06-4757-9384-e07ab47d5212)  
![image](https://github.com/user-attachments/assets/f13dd339-d0a9-4ebc-80aa-c2d0dd12bfd9)  
![image](https://github.com/user-attachments/assets/255db69d-2528-4968-8c9d-551ffab0b17e)  
![image](https://github.com/user-attachments/assets/3478a7ba-e0db-4d9d-ab4d-1ad3c49b2192)  
