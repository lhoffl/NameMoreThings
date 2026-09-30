# NameMoreThings

A BepInEx mod for Valheim 1.0. Name boats and doors to see them on the map.

- Look at a boat rudder or a door and press **N** to name it. The keybind can be changed in the config.
- Under the name field are toggles for that boat or door. They only change what you see, and are saved with your character:
  - **Show name above boat** shows the name floating over the mast.
  - **Show on map** shows the name and icon on the map. Boat pins follow the boat. On by default.
- The name shows when you look at the rudder or door. Leaving the name empty removes it.

Names are saved with the world and everyone on the server sees them. Like portal tags, you can't rename a boat or
door inside someone else's ward, but you can still open the menu to change your toggles.

Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Config

The config file is `BepInEx/config/com.lhoffl.NameMoreThings.cfg`. It reloads automatically when you save
it, so you can tune it while the game runs.

| Setting | Default | |
|---|---------|---|
| `Server.MaxNameLength` | 24      | Longest name allowed. Synced from the server. |
| `Server.AllowNamingBoats` / `AllowNamingDoors` | true    | Whether players can name boats / doors. Synced from the server. |
| `Server.MapUpdateInterval` | 2       | Seconds between the server sending far-away boat positions. Synced from the server. |
| `Controls.NameKey` | N       | Key that opens the naming menu. |
| `Names above boats.Enabled` | true    | false hides every name above boats and removes the toggle from the menu. |
| `Names above boats.ShowByDefault` | true    | Show the name above boats you haven't set yourself. |
| `Names above boats.MaxDistance` | 100     | Meters from the camera within which names are shown. They fade out over the last quarter. |
| `Names above boats.FontSize` | 20      | |
| `Names above boats.HeightOffset` | 1       | Meters between the top of the boat and its name. |
| `Map.Enabled` | true    | false hides every pin and removes the toggle from the menu. |
| `Map.ShowOnMinimap` | true    | Also show pins on the small minimap, not just the large map. |
| `Map.ShowBoatsByDefault` / `ShowDoorsByDefault` | true    | Show boats / doors you haven't set yourself. |

## Multiplayer

Install on the server. Optional for clients. Players without the mod can still join, they just can't name
things or see names, labels, or map pins. Players who have it need the same version as the server.
Only the `Server` settings are synced from the server, and are synced live.

## Building

```
dotnet build -c Release
```

The game path comes from the Steam registry keys. BepInEx is looked for in the game folder; if you use a
mod manager, point `BepInExPath` at the profile's `BepInEx` folder with `-p:BepInExPath=...` or
`Environment.props` (the top of `NameMoreThings.csproj` shows how). The build copies `NameMoreThings.dll` to
`BepInEx/plugins/NameMoreThings/`. Pass `-p:DeployToGame=false` to skip that step.
