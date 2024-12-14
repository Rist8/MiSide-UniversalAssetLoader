# MiSide-AssetLoader
This project was originally a fork of the project https://github.com/CORRUPTOR2037/MiSide-AssetLoader
It WILL conflict with ConsoleUnocker's binds, so it is recommended to delete ConsoleUnlocker before using UniversalAssetLoader (you can do it by just deleting "...\MiSide\BepInEx\plugins\ConsoleUnlocker" folder) 

Изначально этот проект был форком проекта https://github.com/CORRUPTOR2037/MiSide-AssetLoader
Он БУДЕТ конфликтовать с привязками ConsoleUnocker, поэтому рекомендуется удалить ConsoleUnlocker перед использованием UniversalAssetLoader (вы можете сделать это, просто удалив папку "...\MiSide\BepInEx\plugins\ConsoleUnlocker")
________________________________________________________________________________________________________________________________________________________



________________________________________________________________________________________________________________________________________________________
Mod for loading assets in real time for a Unity game "MiSide". Supports custom configs to add new buttons with custom functionality (listed in addons_config.txt which is the file for all configs) in «Addon» tab. It also contains in it's code all features of https://github.com/Rist8/MiSide-Console-Unlocker and additional "greenscreen" free camera for main menu (works also inside the game but the greenscreen is not working properly now). By default has CatEars config which adds cat ears to all Mitas except Chibi. Full list of functionalities is available in readme.txt.



Мод для загрузки ресурсов в реальном времени для Unity-игры «MiSide». Поддерживает пользовательские конфигурации для добавления новых кнопок с пользовательскими функциями (перечисленными в addons_config.txt, который является файлом для всех конфигов) на вкладке «Addons». Он также содержит в своем коде все функции https://github.com/Rist8/MiSide-Console-Unlocker и дополнительную свободную камеру с «зеленым экраном» для главного меню (работает также внутри игры, но зеленый экран сейчас не работает должным образом). По умолчанию имеет конфигурацию CatEars, которая добавляет кошачьи уши всем Митам, кроме Чиби. Полный список функций доступен в readme.txt.
________________________________________________________________________________________________________________________________________________________
To use it you should install BepInEx

BepInEx installation guide:
1. download BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip (or BepInEx-Unity.IL2CPP-win-x86-6.0.0-pre.2.zip for x86 systems) from https://github.com/BepInEx/BepInEx/releases
direct link to it
 https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip
2. extract all files to MiSide folder (it is in "...\SteamLibrary\steamapps\common\MiSide" by default)

Game directory should look like this after extraction from archive ![image](https://github.com/user-attachments/assets/bc7d35bf-3b98-499f-8122-410911d545f2)

3. launch the game and wait until the menu screen appears (BepInEx will open the console window in parallel with game window if installed successfully)
4. exit the game and go to Plugin installation guide




Для использования вам необходимо установить BepInEx

Руководство по установке BepInEx:
1. скачайте BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip (или BepInEx-Unity.IL2CPP-win-x86-6.0.0-pre.2.zip для систем x86) отсюда https://github.com/BepInEx/BepInEx/releases
прямая ссылка на его скачивание
 https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip
2. извлеките все файлы в папку MiSide (по умолчанию она находится в "...\SteamLibrary\steamapps\common\MiSide")

Папка с игрой должна выглядеть так после распаковки архива ![image](https://github.com/user-attachments/assets/bc7d35bf-3b98-499f-8122-410911d545f2)

3. запустите игру и дождитесь загрузки экрана меню (BepInEx откроет окно консоли параллельно с окном игры, если установка прошла успешно)
4. выйдите из игры и перейдите к руководству по установке плагина
________________________________________________________________________________________________________________________________________________________



________________________________________________________________________________________________________________________________________________________
Plugin installation guide:

1. download UniversalAssetLoader.zip file from here https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0
2. extract the archive to "...\MiSide\BepInEx\plugins" and move assimp.dll from "...\MiSide\BepInEx\plugins\UniversalAssetLoader" to "...\MiSide " manually.
   OR
   unpack the archive into any folder and execute the move.bat script, which will try to find the MiSide folder and automatically copy all files
3. launch the game and check if the mod works by opening the Сlothes menu and seeing if the «Addons» tab appeared


Руководство по установке плагина:

1. Загрузите файл UniversalAssetLoader.zip отсюда https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0
2. распакуйте архив в "...\MiSide\BepInEx\plugins" и переместите assimp.dll из "...\MiSide\BepInEx\plugins\UniversalAssetLoader" в "...\MiSide" вручную.
   ИЛИ
   распакуйте архив в любую папку и выполнитe скрипт move.bat, который попытается найти папку MiSide и автоматически скопировать все файлы
3. запустите игру и проверьте, работает ли мод, открыв меню Одежда и посмотрев, появилась ли вкладка «Addons»
________________________________________________________________________________________________________________________________________________________
