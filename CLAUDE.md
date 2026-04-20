# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Game concept

A first-person photo-puzzle/horror game set in Chinese folk / ethnic-minority folklore. The player hunts ghosts wearing 傩戏 (Nuo opera) masks by **physically aiming a camera model and taking photos**. The core challenge is a **hardcore, parameter-driven simulated camera**: different ghosts require different settings — e.g. fast-moving ones need a fast shutter to avoid motion blur, dim scenes need high ISO but tolerate noise, etc. Photos are real output: rendered through a custom simulation pipeline and saved to disk.

This is not a cinematic post-processing toy. The whole point is that exposure, motion blur, and noise are *gameplay inputs*, driven from one source of truth (`CAMCOLCameraSettings`) and applied to both realtime preview and the captured photo.

## Unity setup

- **Editor**: Unity **2022.3.62f3** (LTS). Do not upgrade casually; Invector and several asset packs depend on 2022.3.
- **Render pipeline**: URP 14.0.12. Renderer assets live in `Assets/_Project/Settings/` (`URP-Balanced`, `URP-Performant`, `URP-HighFidelity`).
- **Input**: legacy `InputManager` only (the Input System package is **not** installed — every script uses `Input.GetKey*`/`KeyCode`). If you add Input System, you must refactor every camera and FPC script.
- **Post effects**: SC Post Effects (`Assets/SC Post Effects/`) and URP built-in effects via a Volume + VolumeProfile (`Assets/_Project/Settings/SampleSceneProfile.asset`).
- **Shader space**: linear / gamma helpers are used explicitly in metering (`Mathf.GammaToLinearSpace`). Keep linear-space color when adding new metering/noise math.
- **Unity MCP** (`com.coplaydev.unity-mcp`) is a project package. Claude drives the editor through MCP tools — **use the `unity-mcp-skill` skill** for Unity operations (create GameObjects, edit scripts, run tests, check `read_console` after script changes).

## Commands

There is **no CLI build or test pipeline**. Everything is driven from the Unity Editor.

- **Open the project**: Unity Hub → open `D:/Unity/Project/alt-control` with 2022.3.62f3.
- **Primary working scene**: `Assets/_Project/Scenes/lzcwork.unity`.
- **Tests**: Unity Test Framework is installed but no tests exist yet. When added, run via `Window → General → Test Runner` (or MCP `run_tests`).
- **Play mode keys** (all legacy InputManager, defined in script fields — edit there to rebind):
  - `WASD` + `Left Shift` — walk / sprint (`Proto_FPC/.../Movement.cs`)
  - `M` — meter scene (`CAMMeteringBase.meteringKey`)
  - `O` — half-press shutter: auto-meter + auto-focus + show HUD (`CAMPhotoCapture.halfPressKey`)
  - `P` — full shutter: waits exposure duration, captures, saves PNG (`CAMPhotoCapture.captureKey`)
- **Captured photos** save to `<ProjectRoot>/photo/photo_<timestamp>.png` (the folder is git-ignored).
- **Editor utility**: `Tools → Convert Proto_FPC Materials to URP` (menu item in `Assets/_Project/Scripts/Editor/ConvertProtoFPCMaterialsToURP.cs`). Standard → URP/Lit bulk conversion for the Proto_FPC asset pack.

## Architecture — the simulated camera

Everything lives in `Assets/_Project/Scripts/Camera/`. Two naming prefixes:

- **`CAMCOL*`** = *Camera Controller* pieces that live on the scene-level camera rig and apply settings continuously (realtime preview). Typically read from `CAMCOLCameraSettings`.
- **`CAM*`** (no `COL`) = photo-capture subsystems (metering, focus, motion-blur subject tagging, capture orchestrator, HUD).

The system is **data-oriented around one MonoBehaviour (`CAMCOLCameraSettings`)**. Every other script holds a `[SerializeField] CAMCOLCameraSettings settings;` reference and reacts to its fields. Do not duplicate ISO/shutter/aperture state anywhere else.

### Data flow

```
CAMCOLCameraSettings  (iso, shutterSpeed, aperture, expComp — with clamped limits)
        │
        ├──► CAMPhysicalCameraApplier  → Camera.usePhysicalProperties + iso/shutter/aperture (for DoF)
        │
        ├──► CAMCOLExposureApplier     → EV100 math → Volume.ColorAdjustments.postExposure
        │      ▲                           (if CAMMetering has a reading, adds a conservative auto offset)
        │      │
        │   CAMMeteringBase (abstract)  — center weighted / log-average luminance
        │      ├── CAMMetering          — log-average
        │      └── CAMEvaluativeMetering— zoned (N×M) with center + contrast + focus-zone bias
        │
        ├──► CAMCOLIsoController        → realtime FilmGrain override (URP Volume)
        │                                + post-capture noise shader "Hidden/Simulated Camera/Photo Processing"
        │                                + CPU fallback if shader missing
        │
        └──► CAMCOLMotionBlurController → post-capture motion blur (camera delta + per-subject)
                                          CAMMotionBlurSubject components tag moving objects (ghosts!)
```

### Capture pipeline (CAMPhotoCapture.CapturePhotoRoutine)

1. **Half-press** (`O`): runs `metering.MeterCenterOnce()` and `focusController.FocusCenterOnce()` once. The overlay HUD reads `IsHalfPressActive`.
2. **Full-press** (`P`): snapshot camera pose + subject snapshots → **wait `ShutterSpeed` seconds** (the "open shutter" window; this is why slow shutters physically feel slow) → resnapshot → compute motion vectors → `targetCamera.Render()` into RT → motion-blur shader pass → ISO noise shader pass → `ReadPixels` into `Texture2D` → `EncodeToPNG` → save.
3. Motion vectors are per-subject *and* camera-global. Subjects opt in by adding `CAMMotionBlurSubject` (it auto-registers into a static list and tracks smoothed velocity in `LateUpdate`).
4. If shaders fail to load (`Shader.Find(...) == null`), both ISO and motion-blur classes have CPU fallbacks. Warnings are logged once.
5. An alternative accumulator (`CAMExposureAccumulator` + `SimulatedExposureAccumulation.shader`) exists but is **not currently wired into CAMPhotoCapture** — it accumulates N frames across the shutter window. Use it if you want true long-exposure light trails instead of a single-frame motion-vector blur.

### Gameplay implication

To make a ghost "requires fast shutter", put it on a `CAMMotionBlurSubject` with a fast-moving `Rigidbody`/transform — its motion over `settings.ShutterSpeed` seconds determines photo blur. A ghost that "needs high ISO" lives in a dim zone; the metered scene brightness drives `ColorAdjustments.postExposure`, and cranking ISO to compensate will raise `CAMCOLIsoController` noise past the `isoNoiseThreshold` → grainy photo.

When you add ghost detection / puzzle logic: read from the saved `Texture2D` at the end of `CapturePhotoRoutine`, or inspect the component state at the moment of capture (ghost visibility, on-screen rect, blur magnitude) before the coroutine completes.

## Project layout

- `Assets/_Project/` — **all first-party work**. New scripts, scenes, prefabs, and settings go here. Do not edit third-party asset folders.
  - `Scripts/Camera/` — the camera simulation (described above).
  - `Scripts/Editor/` — editor utilities (URP converter, etc.).
  - `Scenes/lzcwork.unity` — main working scene.
  - `Settings/` — URP renderer assets + default VolumeProfile.
- `Assets/Proto_FPC/` — third-party first-person controller ("Prototype FPC"). Player rig prefab at `FPC_Prefab/Prototype_FPC.prefab`. `FPC_Resources/Scripts/FPC/Dependencies.cs` is the shared-reference hub; `Movement.cs`, `Jump.cs`, `Sway.cs`, `GrabThrow.cs`, `Inspect.cs` read from it. Do not edit these unless you are deliberately forking the controller.
- `Assets/FlatKit/`, `Assets/JC_LP_Cars/`, `Assets/LowPolyFantasyVillage/`, `Assets/LowPolyMegaBundle/`, `Assets/SC Post Effects/` — asset-store bundles (art + post fx). Treat as read-only unless migrating shaders.
- `photo/` — captured photo output. Git-ignored.
- `Packages/manifest.json` — package dependencies including `com.coplaydev.unity-mcp`.
- `ProjectSettings/EditorBuildSettings.asset` — **currently references `Assets/Invector-3rdPersonController/...` demo scenes that do not exist** in the repo. Those entries are stale; if you start a build, remove them or add the Invector package first (see `INVECTOR_SETUP_CHECKLIST.md`, which is itself outdated).

## Conventions to preserve

- **One camera-settings source**. New camera behavior pulls from `CAMCOLCameraSettings`; never add a second source of ISO/shutter/aperture. If you need a new gameplay parameter (white balance, flash power, focal length), add it there and let applier components read it.
- **OnValidate-driven clamping**. Every camera script clamps its serialized fields in `OnValidate` and re-applies. Follow this pattern — designers tweak values in the Inspector during Play Mode.
- **EnsureReferences + one-time warnings**. Camera scripts auto-resolve missing refs from the same GameObject / `Camera.main`, and log a missing-dependency warning **once** (guarded by a `warned*` bool). Keep this pattern — noisy repeated logs make the console useless.
- **Shader + CPU fallback**. Capture-path shaders have CPU fallbacks because `Shader.Find` can fail in stripped builds. Preserve this when adding new capture passes; alternatively, mark required shaders as "always included" in Graphics Settings.
- **Legacy Input only**. Until/unless the Input System package is added, stick to `Input.GetKey*` and `KeyCode` serialized fields.
- **Script placement**. First-party scripts go under `Assets/_Project/Scripts/<System>/`. Editor scripts go under `Assets/_Project/Scripts/<System>/Editor/` (or the top-level `Assets/_Project/Scripts/Editor/` for project-wide tools).

## After editing scripts with MCP

Always call `read_console` after `create_script` / `manage_script` edits to confirm compilation succeeded before using new types. Poll `editor_state.isCompiling` if needed. New components are only usable after domain reload completes.

## Known stale state

- `INVECTOR_SETUP_CHECKLIST.md` describes Invector 3rd-person content at `Assets/Invector-3rdPersonController/` — that folder is not currently in the working tree, but `EditorBuildSettings.asset` still references two of its demo scenes. Treat the checklist as historical unless Invector is re-added.
- Recent reorg commits moved user content into `Assets/_Project/`; some prefabs in third-party folders may still carry GUID references that predate the move.
