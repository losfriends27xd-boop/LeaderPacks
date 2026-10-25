LEADER PACKS — Ages of Conflict (prototype)

This project is a prototype BepInEx plugin. It is not yet a finished mod and has not been compiled/tested in this environment.

FEATURES IN THIS PROTOTYPE
- Press F6 to open/close a Leader Packs panel.
- Browse starter name lists grouped into historical packs.
- Search the names (for example, Simón Bolívar or Napoleón Bonaparte).
- Draw a 256x256 portrait with a mouse and choose brush size/color.
- Save portraits as PNG.
- Save a country -> leader mapping.

IMPORTANT LIMITATION
This prototype saves portraits and mapping data. It does not yet hook into Ages of Conflict's country-selection interface, so the game's built-in country UI will not automatically show the portrait. That requires a later game-specific integration and testing.

BUILD IT INTO ONE DLL
1. Install the .NET SDK on Windows if it is not already installed.
2. Extract this ZIP.
3. Double-click build.bat.
4. If successful, the single plugin file will be:
   bin\Release\net472\LeaderPacks.dll
5. Copy ONLY LeaderPacks.dll to:
   <Ages of Conflict folder>\BepInEx\plugins\
6. Start the game and press F6.

The game libraries package is based on the Ages of Conflict BepInEx modding setup guide.
If the build fails, take a screenshot of the console error and share it so the project can be corrected.
