# BlockFlow

A mobile puzzle game inspired by *Color Block Jam*, built in Unity 2022.3 LTS. Drag colored blocks to matching colored grinders at the board's edges to clear the grid before the timer runs out. Built as a case study around **clean architecture, SOLID principles, mobile performance, and game feel**.

---

## Table of Contents

1. [Gameplay](#gameplay)
2. [Tech Stack](#tech-stack)
3. [Architecture](#architecture)
   - [Assembly Layout](#assembly-layout)
   - [Scene Flow](#scene-flow)
   - [DI Composition](#di-composition)
   - [Event Bus](#event-bus)
   - [Model / View Separation](#model--view-separation)
   - [Design Patterns](#design-patterns)
4. [Subsystems](#subsystems)
   - [Grid & Movement](#grid--movement)
   - [Blocks](#blocks)
   - [Grinders](#grinders)
   - [Level Data](#level-data)
   - [Input](#input)
   - [Feedback (Audio / Haptics / Juice)](#feedback-audio--haptics--juice)
   - [UI (UI Toolkit)](#ui-ui-toolkit)
   - [Progression & Scene Flow](#progression--scene-flow)
5. [Mobile Optimization](#mobile-optimization)
6. [Build-Time Asset Rules](#build-time-asset-rules)
7. [Project Structure](#project-structure)
8. [Setup & Building](#setup--building)
9. [Editor Tooling](#editor-tooling)
10. [Testing](#testing)
11. [Controls](#controls)
12. [Credits](#credits)

---

## Gameplay

- **5 hand-crafted levels** on 4×4, 5×5, and 6×6 grids, authored as JSON.
- **10 block shapes** — Cube, Line2/3, L, T, Z, Plus, and variants — defined as ScriptableObjects with cell-offset patterns.
- **Axis-locked blocks** can only move horizontally or vertically; the view shows an arrow indicator.
- **Iced blocks** hide their color behind a frost overlay; a global grind counter melts ice progressively until the color is revealed.
- **Grinders** are color-matched exit gates on the board's edges. Drag a block into a matching grinder to consume its cells one row at a time with a "grinding" slide animation.
- **Timer countdown** per level. Clear all blocks to win; run out of time (or hit an unwinnable state) to lose.
- **Level progression** persists via PlayerPrefs. The LevelMap scene is the app entry point; Gameplay is loaded additively on Play and unloaded on win/lose/home.

---

## Tech Stack

| Area | Choice |
|---|---|
| Engine | Unity **2022.3.62f3** LTS |
| Render Pipeline | Universal RP (Performant tier: no shadows, no MSAA, no HDR) |
| DI | **VContainer** 1.17 |
| Async | **UniTask** — zero-alloc async/await |
| Tweening | **PrimeTween** — struct-based, no GC |
| Serialization | **Newtonsoft.Json** for level JSON |
| Input | **New Input System** — `Pointer.current`, `InputSystemUIInputModule` |
| UI | **UI Toolkit** (UXML + USS) for HUD, popups, level map |
| Audio Routing | **AudioMixer** asset with SFX / Music groups + exposed volume params |

---

## Architecture

### Assembly Layout

```
BlockFlow.Core       Foundation: DI root, event bus, input abstraction, pooling, scene loader, prefab loader
BlockFlow.Data       ScriptableObjects, grid primitives, level JSON schema + validator, feel configs, audio library
BlockFlow.Gameplay   Grid/block models, movement strategies, drag input, grinders, level runner, feedback services
BlockFlow.UI         HUD, pause/outcome popups, level map, safe-area fitter
BlockFlow.Editor     Menu commands: Create Definitions, Reload Level, Optimize Audio Imports
BlockFlow.Tests      EditMode unit tests
```

Dependencies flow strictly one way: **Core ← Data ← Gameplay ← UI**. No circular references; UI depends on Gameplay only at the DI seam.

### Scene Flow

```
App Launch ──► LevelMap.unity  (build index 0, always loaded)
                   │
                   ▼  player taps Play
                Additive load Gameplay.unity
                   │
                   ▼  win / lose / home button
                Unload Gameplay.unity ─► back on LevelMap
```

- `SceneFlowManager` owns the load/unload orchestration.
- `LevelMapLifetimeScope` registers only what the map needs (`LevelProgressionService`, `LevelCatalog`).
- `GameplayLifetimeScope` builds when the Gameplay scene loads and disposes on unload — no manual teardown.
- A **single EventSystem + InputSystemUIInputModule** lives in the LevelMap scene to avoid the duplicates that additive loading would otherwise introduce.
- Fullscreen UIDocument roots are set to `PickingMode.Ignore` so higher-sort panels never swallow clicks intended for lower-sort popups (a real bug encountered with multiple popups sharing a PanelSettings).

### DI Composition

- `ProjectLifetimeScope` — survives all scenes, registers cross-scene services (`IPrefabLoader`, `ISaveRepository`, `HapticsService`).
- `LevelMapLifetimeScope` — only the map's needs.
- `GameplayLifetimeScope` — the composition root for the entire gameplay loop. Every service is registered here, including lifecycle-managed entry points (`RegisterEntryPoint`) for things like `LevelRunner`, `DragController`, `WinConditionEvaluator`, `AudioFeedbackRouter`.

Zero `FindObjectOfType` calls in runtime code; every dependency is constructor-injected.

### Event Bus

`IEventBus` is a typed publish-subscribe mechanism with `IDisposable` subscriptions:

```csharp
subs.Add(bus.Subscribe<BlockGroundEvent>(OnBlockGrounded));
subs.Add(bus.Subscribe<LevelEndedEvent>(OnLevelEnded));
```

All cross-system communication goes through events. The `TimerService` doesn't know the HUD exists; the `GrinderService` doesn't know about audio or haptics. Each listener is independently registered on the bus. This is how the architecture stays decoupled despite having ~20 services.

Event categories (see `Gameplay/Events/`):
- **Drag events** — `BlockDragStartedEvent`, `BlockDragEndedEvent`
- **Block lifecycle** — `BlockGroundEvent`, `BlockDestroyedEvent`, `BlockIceMeltedEvent`
- **Level lifecycle** — `LevelStartedEvent`, `LevelEndedEvent`, `LevelOutcomeEvent`
- **Timer** — `TimerTickEvent`
- **UI requests** — `PauseRequestedEvent`, `RetryRequestedEvent`

### Model / View Separation

`GridModel` and `BlockModel` are **pure C#** — no MonoBehaviour, no UnityEngine types beyond simple structs. They hold the authoritative simulation state:
- Cell occupancy
- Block placement, step moves, slides with wall/block collision
- Multi-cell block support via cell-offset patterns

Views (`BlockView`, `GrinderView`, `GroundTileView`, `WallView`) are dumb renderers: they observe model state and sync their Transform/Material. No Update loops — views only mutate on explicit sync calls triggered by events.

This split is what makes `GridModelTests` and `MovementStrategyTests` trivially runnable in EditMode without a scene.

### Design Patterns

- **Strategy Pattern** — `IMovementStrategy` is implemented by `FreeMovementStrategy` and `SingleAxisMovementStrategy` (horizontal/vertical variants). The factory chooses one per block based on `BlockAxisLock`.
- **Factory + Object Pool** — `PrefabPool` wraps Unity's `ObjectPool<T>`. `BlockViewFactory` and `GrinderViewFactory` pool per prefab variant.
- **Service Locator via DI** — VContainer resolves services at construction time; no static singletons in gameplay code.
- **Typed event bus** (described above) replaces direct references and C# events on MonoBehaviours.
- **ScriptableObject catalogs** (`BlockDefinitionCatalog`, `GrinderDefinitionCatalog`, `LevelCatalog`) provide runtime lookup without hardcoded paths.
- **Configs as SOs** — `IceFeelConfig`, `ShatterFeelConfig`, `GrinderFeelConfig`, `PopupAnimationConfig` let designers tune feel without code changes.

---

## Subsystems

### Grid & Movement

- `GridModel` — 2D array of `BlockId`, with placement, removal, step, and slide operations. Slide stops on wall or block collision and returns the final cell.
- `GridMath` — cell↔world conversions, CCW rotation, direction math.
- `CellSpace` — XZ-plane convention (Y is up; grid sits flat).
- `GridBuilder` — instantiates ground tiles and boundary walls from a `GridSize`.
- `GridPicker` — raycasts pointer world rays against the grid plane and snaps to cells.
- `BlockModel` maintains its anchor cell and a pattern of relative cell offsets. Multi-cell blocks slide as rigid shapes.

### Blocks

- `BlockDefinition` (SO) — shape, color, default axis lock, mesh prefab.
- `BlockModelFactory` produces `BlockModel` + `IMovementStrategy` pair.
- `BlockView` renders the mesh, tints it via `MaterialPropertyBlock` (no per-block material instances → GPU instancing works), and owns an `IceOverlayController` for the frost overlay + remaining-grinds text.
- `BlockShatterEffect` spawns a tinted grind-particle prefab at the grinder contact edge, orients the cone along the slide direction, and scales with grinder width.

### Grinders

- `GrinderDefinition` (SO) — color, width (1 or 2 cells).
- `GrinderPlacement` computes world position, rotation, and depth offset for an edge-anchored grinder.
- `GrinderService` is the sole authority on "can this block be consumed by this grinder" and owns the consumption tween: the block slides into the grinder, cells clip against a shader-driven clip plane, and the grind particle plays for the duration.
- `GrinderGeometry` caches the clip-plane math so the slide distance always pushes the block past the cutoff.

### Level Data

- **JSON schema** (`Assets/_Project/Levels/level_XX.json`):
  ```json
  {
    "gridSize": 5,
    "timeLimitSeconds": 60,
    "blocks": [ { "shape": "L", "color": "Red", "cell": [1,2], "axisLock": "None" } ],
    "grinders": [ { "edge": "Right", "cell": 1, "color": "Red", "width": 1 } ]
  }
  ```
- `LevelJsonSerializer` parses into `LevelPayload`.
- `LevelValidator` runs at load and rejects malformed levels (out-of-range cells, unknown shapes/colors, overlapping placements, unreachable grinders by color).
- `LevelLoader` + `LevelBuilder` construct the runtime scene from the payload.
- `LevelRunner` owns the per-level lifecycle: Start → gameplay → win/lose → teardown.

### Input

- `UnityPointerInputService` wraps `Pointer.current` into a `PointerState` stream (`PointerPhase.Began / Moved / Ended / Cancelled`).
- `DragController` turns pointer input into block drags:
  - Picks the block under the pointer on `Began`.
  - Tracks finger delta along the block's allowed axes (free or single-axis).
  - Clamps to wall/block collisions via `GridModel` previews.
  - Snaps to the nearest cell on `Ended`.
  - Auto-consumes into adjacent matching grinders (without publishing `BlockDragEndedEvent`, which would fight the consume tween).
- `BlockInputLock` pauses drag input during ice-reveal animations and grinder consumption.

### Feedback (Audio / Haptics / Juice)

- **AudioMixer** (`GameAudioMixer.mixer`) with `SFX` and `Music` groups; exposed `SfxVol` / `MusicVol` parameters driven by `AudioService` / `MusicService`. Bus-level mute uses `SetFloat(exposedName, -80f)`.
- `AudioService` plays SFX with a **per-key cooldown** (prevents rapid-fire sounds from piling up in one frame).
- `MusicService` handles track swaps with fade; survives additive Gameplay-over-Map scene switches.
- `AudioFeedbackRouter` subscribes to gameplay events and triggers SFX keys (drag start/end, grind, ice melt, win, lose).
- `HapticsService` is a thin wrapper around platform vibration; user-toggleable.
- `BlockJuiceService` adds squash/stretch/bounce tweens on block grounding and grinder entry.

### UI (UI Toolkit)

All runtime UI is UXML/USS:

- **`LevelMap.uxml`** — vertical scroll of level nodes; current level highlighted green; scroll is locked to the bottom (current) node and input is blocked so players can't pan away from it.
- **`GameplayHud.uxml`** — level badge, timer, pause button.
- **`PausePopup.uxml`** — slide-down settings panel with Sound / Music / Haptic toggles and a Home button. Custom toggle skin uses parent `justify-content` (not `align-self`) so the dot actually moves between left/right on the main axis.
- **`LevelOutcomePopup.uxml`** — win/lose result, Next/Retry/Home buttons, stars.
- **`UIToolkitSafeArea`** applies the device's safe area to UIDocument roots for notched devices.

PanelSettings are configured with **Scale Mode: Scale With Screen Size**, **Reference Resolution: 1080×1920**, **Screen Match Mode: Match Width Or Height**, **Match: 1** — UI scales to height, so it stays readable across phone (9:20, 9:18), standard (16:9), and iPad (4:3) aspects.

### Progression & Scene Flow

- `LevelProgressionService` persists the current level index to PlayerPrefs, with `ReloadFromDisk()` for cross-scene refresh.
- `StarCalculator` maps clear-time percentage to a 1–3 star rating.
- `GameFlowController` orchestrates level start → outcome → next/retry across the scene-load seam.
- `ILevelStartupStrategy` decouples "how do I know which level to load" from `LevelRunner`, enabling future test/debug strategies.

---

## Mobile Optimization

- **URP-Performant**: no real-time shadows, MSAA off, HDR off, post-processing minimal.
- **Shader stripping** aggressively enabled in URP Global Settings (Strip Unused Variants) + `GraphicsSettings` (Lightmap Modes off, Fog Modes off). Shader variant count dropped ~80k → ~9k.
- **GPU Instancing** via `MaterialPropertyBlock` tinting — all blocks share one material.
- **Object pooling** for block / grinder / particle views.
- **Zero-allocation hot paths**: UniTask instead of coroutines; struct events; no LINQ in Update-path code.
- **No physics**: all collisions are grid-logical integer math.
- **No Update loops on views** — views sync only when models publish change events.
- **Audio import optimizer** (menu: `BlockFlow → Audio → Optimize Import Settings`) normalizes all clips: Force To Mono, 22050 Hz, Vorbis, appropriate load type by length, iOS/Android overrides. Music clips stream.
- **Target 60 FPS** with VSync off.

---

## Build-Time Asset Rules

URP strips unreferenced shaders and `Resources.Load` only resolves assets under a `Resources/` folder at runtime. The following must stay in sync with any code that references them:

**Runtime-loaded prefabs** — must live under `Assets/_Project/Resources/`:
- `GrindParticleEffect.prefab` — loaded by `BlockShatterEffect`.
- `IceShatterEffect.prefab` — loaded by `IceMeltService` via `IPrefabLoader`.

**Always Included Shaders** — any shader referenced only by `Shader.Find` must be added to `ProjectSettings/GraphicsSettings.asset → m_AlwaysIncludedShaders`:
- `BlockFlow/BlockClipPlane` — used by `BlockView` for the grinder clip cutoff.
- `BlockFlow/AlwaysOnTop` — used by `IceOverlayController` for the remaining-grinds text.
- `Universal Render Pipeline/Particles/Unlit` — runtime fallback in `BlockShatterEffect` when a particle renderer has no authored material.

Adding a new `Shader.Find` or `Resources.Load` target without updating these two places will fail silently in builds (editor-only fallback via `AssetDatabase.LoadAssetAtPath` masks the error during development).

---

## Project Structure

```
Assets/_Project/
├── Scripts/
│   ├── Core/              DI, event bus, input service, pooling, bootstrap, scene flow
│   ├── Data/              ScriptableObjects, grid primitives, level schema, audio library, feel configs
│   ├── Gameplay/
│   │   ├── Blocks/        Models, view, movement strategies, shatter effect, ice overlay
│   │   ├── Grid/          Grid model, grid builder, camera fitter, grid picker, ground/wall views
│   │   ├── Grinders/      Model, view, placement, geometry, service
│   │   ├── Input/         Pointer service, drag controller, input lock
│   │   ├── Level/         Loader, builder, runner, progression, flow controller, star calculator
│   │   ├── Feedback/      Audio, haptics, juice
│   │   ├── Goals/         Win / lose condition evaluators
│   │   ├── State/         GamePhase, GameStateService
│   │   ├── Timer/         CountdownTimer
│   │   ├── Events/        All IEventBus event structs
│   │   ├── DI/            GameplayLifetimeScope
│   │   └── Bootstrap/     GameplayBootstrapper
│   ├── UI/
│   │   ├── Screens/       LevelMapScreen, GameplayHudView, PausePopupView, LevelOutcomePopupView, UIToolkitSafeArea
│   │   └── DI/            LevelMapLifetimeScope
│   └── Editor/            BlockFlowDefinitionCreator, BlockFlowMenu, AudioImportOptimizer
├── Resources/             Runtime-loaded prefabs (GrindParticleEffect, IceShatterEffect)
├── Levels/                level_01.json … level_05.json
├── Scenes/                LevelMap.unity, Gameplay.unity
├── Prefabs/Gameplay/      Authored prefabs not loaded via Resources
├── UI/
│   ├── UXML/              LevelMap, GameplayHud, PausePopup, LevelOutcomePopup
│   └── USS/               Matching stylesheets
├── ScriptableObjects/     AudioLibrary, GameAudioMixer, block/grinder definitions, color palette, feel configs
├── Art/                   Meshes, shaders, textures
├── Tools/                 LevelEditor.html (drag-and-drop level designer that exports JSON)
└── Tests/EditMode/        GridModelTests, MovementStrategyTests
```

---

## Setup & Building

1. Open the project in **Unity 2022.3.62f3** (or any 2022.3.x LTS).
2. Let the package manager resolve (`Packages/manifest.json`).
3. If the block/grinder definition assets are missing or out of date, run:
   ```
   BlockFlow → Create All Definitions
   ```
4. (Optional) After adding new audio clips:
   ```
   BlockFlow → Audio → Optimize Import Settings
   ```
5. Make sure `LevelMap.unity` is at **build index 0** and `Gameplay.unity` is at index 1 (File → Build Settings).
6. Press Play from `LevelMap.unity`.

### Mobile Build

- Target platform: Android or iOS.
- Scripting backend: **IL2CPP**.
- Active Input Handling: **Input System Package (New)**.
- Default Orientation: **Portrait**.
- First build will be slow due to shader variant compilation (~3 min clean). Incremental builds are fast because shader cache is reused.

---

## Editor Tooling

| Menu | What it does |
|---|---|
| `BlockFlow → Create All Definitions` | Regenerates `BlockDefinition` and `GrinderDefinition` SOs from FBX meshes. |
| `BlockFlow → Reload Current Level` (Ctrl+Shift+R) | Hot-reloads the active level in play mode for rapid iteration. |
| `BlockFlow → Audio → Optimize Import Settings` | Walks `Assets/_Project` audio clips and applies mobile-friendly import settings. |
| `Assets/_Project/Tools/LevelEditor.html` | Standalone drag-and-drop level authoring tool that exports compatible level JSON. |

### Level Editor (`Assets/_Project/Tools/LevelEditor.html`)

A single-file, zero-dependency level authoring tool. Open it in any modern browser — no server, no build step, no installs. It produces JSON files that drop straight into `Assets/_Project/Levels/`.

**Architecture**

Internally the editor mirrors the game's runtime model so what you see in the editor is what the game loads:

```
State (pure model) ─► Commands (apply/undo) ─► History (undo/redo stack) ─► Renderer (DOM)
                                                                                   ▲
                                                                           Tools / Selection
```

Every mutation is a Command with matching `apply()` / `undo()` — so Ctrl+Z reverses any action. The same `SHAPES` offsets, `COLORS` set, and `EDGES` enum that the Unity side uses are duplicated here, and the validator runs the same rules as `LevelValidator.cs` so invalid levels are caught before export.

**Workflow**

1. Open `Assets/_Project/Tools/LevelEditor.html` in a browser.
2. Set **Level ID** (e.g. `level_06`), **Grid Size** (4×4 / 5×5 / 6×6), and **Time Limit**.
3. Pick a **Tool**, **Color**, and (for Block tool) a **Shape**.
4. Click cells to place blocks; click edge slots to place grinders.
5. Watch the **Issues** panel on the right — export is disabled while any **ERR** is present.
6. Click **Export** to download `<levelId>.json`, or **Copy** to put it on the clipboard.
7. Drop the `.json` file into `Assets/_Project/Levels/` and add it to `LevelCatalog`.

**Tools**

| Tool | Shortcut | Action |
|---|---|---|
| Block | **B** | Click a cell to place the selected shape at that origin. |
| Grinder | **G** | Click an edge slot to place a grinder with the selected color and width. Clicking an existing grinder removes it. |
| Eraser | **E** | Click a block cell to remove the block; click a grinder or edge slot to remove the grinder. |
| Select | **S** | Click to select a block or grinder; drag a block to a new cell; edit properties in the right sidebar. |

**Color Palette** — 6 colors matching the game's palette (Red, Blue, Green, Yellow, Purple, Orange). Keys **1**–**6** pick colors directly.

**Block Shapes** — all 10 shapes the game supports (Cube S, Cube 2×2, Line 2, Line 3, L Small, L Large, L Mirror, T, Z, Plus), rendered as mini-previews in the shape picker.

**Grinder Width** — 1, 2, or 3 cells, picked in the Grinder sidebar.

**Block Properties** (Select tool → click a block) — change color, toggle **Axis Lock** (None / Horizontal / Vertical), set **Ice count** (0–5). Visual indicators: an axis arrow appears on axis-locked blocks; an ice badge shows the remaining-grind count.

**Other shortcuts**

| Key | Action |
|---|---|
| Ctrl+Z | Undo |
| Ctrl+Shift+Z / Ctrl+Y | Redo |
| Ctrl+S | Export JSON |
| Ctrl+O | Import JSON |
| Esc | Clear selection / close popover |
| Del / Backspace | Delete selected block or grinder |
| ? | Toggle keyboard-shortcut help panel |
| Drop `.json` anywhere | Open that level file |

**Validation rules (mirrors `LevelValidator.cs`)**

- Grid size must be 4×4 – 6×6.
- Time limit must be > 0.
- Every block must fit entirely within the grid.
- Blocks cannot overlap each other.
- Every color with blocks on the board must have at least one matching grinder.
- Grinder width must be 1–3 and must not exceed its edge length.
- Grinder position + width must stay within the edge.

Errors block export; warnings don't. Click any issue row to jump to the cell it references.

**Safety nets**

- **Autosave** — state serializes to `localStorage` every 2s. On reload, you're prompted to restore the last session.
- **Import round-trips** — drop any existing `level_XX.json` back in and the editor reproduces it exactly, preserving unknown fields so future schema additions survive a round trip.
- **Grid-resize guard** — shrinking the grid detects items that would fall outside the new bounds and asks before dropping them.

**Exported JSON** matches the shape `LevelLoader` consumes:

```json
{
  "id": "level_06",
  "gridSize": { "x": 5, "y": 5 },
  "timeLimit": 60,
  "walls": [],
  "grinders": [
    { "edge": "Right", "position": 2, "width": 1, "color": "Red" }
  ],
  "blocks": [
    {
      "shape": "L_S",
      "origin": { "x": 1, "y": 2 },
      "rotation": 0,
      "color": "Red",
      "modifiers": { "iced": 0, "axisLock": "None" }
    }
  ]
}
```

---

## Testing

Open **Window → General → Test Runner → EditMode → Run All**.

- **GridModelTests** — placement, removal, step moves, slides, wall collisions, multi-cell blocks
- **MovementStrategyTests** — free movement, single-axis filtering (horizontal/vertical)

Tests run on pure C# models with no scene dependency.

---

## Controls

- **Drag** any block — slides along the grid, stops on wall or other block.
- **Release** adjacent to a matching grinder — block is consumed with a grinding animation.
- **Pause button** (top-right) — opens settings popup with Sound / Music / Haptic toggles and Home.

---

## Credits

- **Midas Games** — case study brief.
- **3D models** — provided by Midas (free case-study pack).
- **Epic Toon FX** — particle effects (asset store).
- **PrimeTween, VContainer, UniTask** — open-source packages credited in `Packages/manifest.json`.
- **Audio** — free SFX from public-domain sources.
