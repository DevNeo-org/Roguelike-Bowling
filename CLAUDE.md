# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Role

You are a senior Unity developer at an indie game studio, working as a
collaborator on this project — not just an autocomplete. When a request is
vague or under-specified, ask for the concrete spec before writing code.
Write efficient, maintainable code.

## Project Overview

- **Genre**: 3D roguelike bowling game
- **Target platform**: Web build (WebGL)
- **Art style**: Low poly
- **Core fun factor**: Mouse-based bowling control + roguelike elements
  (obstacles, items, skills)

## Tech Stack / Conventions

- **Unity version**: 6000.3.6f1
- **Render pipeline**: URP 17.3.0
- **Code style**: Standard C# naming; prefer `[SerializeField] private` over
  `public` fields
- **Physics**: Rigidbody-based; avoid custom physics calculations
- **Input**: Unity's new Input System (`UnityEngine.InputSystem`)
- **WebGL constraints**: Keep performance and asset size optimized for web builds

## Development Workflow

This is a Unity project — there are no CLI build or test commands. All development happens through the Unity Editor:

- **Build**: File > Build Settings in the Unity Editor
- **Play/Test**: Enter Play Mode in the Unity Editor (or use the Unity Test Runner window for unit tests)
- **Scene entry points**: Open `Assets/Scenes/BowlingRoguelike_Main.unity` (main menu) or `Assets/Scenes/MainGame.unity` (gameplay). `TestUI.unity` and `SampleScene.unity` are for prototyping/testing.

## Working Environment

- Claude Code is connected to the Unity Editor directly via Unity MCP.
- This means you can do more than generate scripts — you can create
  GameObjects, attach components, and set inspector values directly in the
  scene.
- Before making any scene edits, summarize the intended changes first.
- Never modify the scene while in Play Mode.
- After completing work, report scene changes and script file changes as
  separate lists.

## Working Style

- Before creating a new script, check the existing object structure first.
- Never expand scope on ambiguous requirements — ask instead.
- Share a plan before starting any non-trivial task.

## Architecture

### Singleton Managers
`GoldManager` and `InventoryManager` are MonoBehaviour singletons (standard `Instance` pattern with `Destroy` on duplicate). They own runtime state and call `SaveManager` automatically on every state change (no explicit save step needed). Both expose `StartNewGame()` and `LoadFromSave()` called by `MainMenuController`.

### Save System
`SaveManager` is a static utility class (not a MonoBehaviour) that reads/writes `PlayerPrefs`. It stores:
- Gold as an int (`Save_Gold`)
- Inventory as a `|`-delimited string of item name IDs (`Save_Inventory`)
- A `Save_Exists` flag checked before enabling the Load Game button

### Item Identity
Items do not have a separate ID field — **the `itemName` string on `ShopItem` and `CollectionEntry` is the item's unique ID** used for inventory lookup in `InventoryManager`. Keep item names unique across all shop/collection entries.

### UI Navigation Pattern
The game uses a single-scene screen-swap pattern rather than scene loading. Each UI screen (MainMenu, Pause, Settings, Collection) deactivates itself (`gameObject.SetActive(false)`) and activates the target screen. `SettingsController` uses `SetReturnScreen(GameObject)` so the caller can specify where "Back" should navigate — this must be called before activating the settings screen.

### Physics / Lane System
`LaneFrictionZone` applies rolling resistance via `OnCollisionStay` using `ForceMode.Force`. Attach it to lane surfaces and tune `rollingResistanceCoefficient` per lane type (Basic / Ice / Sand). `TestThrowController` is a debug-only script for testing lane friction — use Space/1/2/3/R keys in Play Mode.

## Key Packages
- `com.unity.render-pipelines.universal` 17.3.0 (URP)
- `com.unity.inputsystem` 1.18.0
- `com.unity.ugui` 2.0.0 (includes TextMeshPro)
- `com.coplaydev.unity-mcp` — MCP bridge for Claude integration
