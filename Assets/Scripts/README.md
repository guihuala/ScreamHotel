# Scream Hotel — Unity Skeleton

- Unity 2022+ (URP), C#
- Includes DataManager (SO + JSON override), core systems, domain POCOs, and example configs.
- Import this folder into a new Unity project (or copy `Assets` into your project).

## Structure
- `Assets/Scripts/Core`: entry `Game.cs`, `EventBus.cs`
- `Assets/Scripts/Domain`: pure logic POCOs
- `Assets/Scripts/Data`: ScriptableObjects, DataManager, runtime `ConfigDatabase`
- `Assets/Scripts/Systems`: Assignment, Execution, Build, Day/Progression
- `Assets/Scripts/UI`: simple button stub
- `Assets/Resources/Configs`: default ScriptableObjects (created via Editor menu)
- `Assets/StreamingAssets/Configs`: JSON overrides (examples included)

## Quick Start
1) Open the project in Unity.
2) Create an empty scene, add a GameObject with `Game` and `DataManager` components.
3) (Optional) Menu: **ScreamHotel → Create Sample Configs** to generate sample assets under `Resources/Configs`.
4) Press Play. Use `UIButtonStartNight` to advance states (add to a UI Button and wire references).
