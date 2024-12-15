# MiSide-AssetLoader
This project was originally a fork of the project https://github.com/CORRUPTOR2037/MiSide-AssetLoader
It WILL conflict with ConsoleUnocker's binds, so it is recommended to delete ConsoleUnlocker before using UniversalAssetLoader (you can do it by just deleting "...\MiSide\BepInEx\plugins\ConsoleUnlocker" folder) 

Изначально этот проект был форком проекта https://github.com/CORRUPTOR2037/MiSide-AssetLoader
Он БУДЕТ конфликтовать с биндами ConsoleUnocker, поэтому рекомендуется удалить ConsoleUnlocker перед использованием UniversalAssetLoader (вы можете сделать это, просто удалив папку "...\MiSide\BepInEx\plugins\ConsoleUnlocker")
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

1. download "UniversalAssetLoader.zip" file from here https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0
!!!!
2. extract the folder "OuterFolder\UniversalAssetLoader" from the archive "UniversalAssetLoaderRelease.zip" to "...\MiSide\BepInEx\plugins"
   
    2.1. and move "...\MiSide\BepInEx\plugins\UniversalAssetLoader\assimp.dll" to "...\MiSide" manually.

    OR
   
2. unpack the archive into any folder, download "move.bat" then move it into extracted "OuterFolder" and run the script, which will try to find the MiSide folder and automatically copy all files
!!!!
3. launch the game and check if the mod works by opening the Сlothes menu and seeing if the «Addons» tab appeared

![image](https://github.com/user-attachments/assets/b380ff52-5c7d-4ebe-9b85-52eda35ce9fb)




Руководство по установке плагина:

1. Загрузите файл "UniversalAssetLoader.zip" отсюда https://github.com/Rist8/MiSide-UniversalAssetLoader/releases/tag/Release-0.9.0
!!!!
2. извлеките папку "OuterFolder\UniversalAssetLoader" из архива "UniversalAssetLoaderRelease.zip" в "...\MiSide\BepInEx\plugins"
   
    2.1. и переместите "...\MiSide\BepInEx\plugins\UniversalAssetLoader\assimp.dll" в "...\MiSide" вручную.

    ИЛИ
   
2. распакуйте архив в любую папку и скачайте "move.bat", затем переместите его в распакованную "OuterFolder" и запустите скрипт, который попытается найти папку MiSide и автоматически скопировать все файлы
!!!!
3. запустите игру и проверьте, работает ли мод, открыв меню Одежда и посмотрев, появилась ли вкладка «Addons»

![image](https://github.com/user-attachments/assets/3e4c4c09-e31a-47ad-bff3-634c203f32d9)

________________________________________________________________________________________________________________________________________________________


Some screenshots from cat ears addon/ Несколько скриншотов из аддона на кошачьи уши:

![image](https://github.com/user-attachments/assets/76c8d3f0-7bbc-484f-bddb-03db69215b1f)
![image](https://github.com/user-attachments/assets/e6325bbf-fb06-4757-9384-e07ab47d5212)
![image](https://github.com/user-attachments/assets/f13dd339-d0a9-4ebc-80aa-c2d0dd12bfd9)
![image](https://github.com/user-attachments/assets/255db69d-2528-4968-8c9d-551ffab0b17e)
![image](https://github.com/user-attachments/assets/3478a7ba-e0db-4d9d-ab4d-1ad3c49b2192)





