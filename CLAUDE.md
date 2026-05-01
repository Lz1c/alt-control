# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Game concept

> **2026-05-01 v2 收紧版本** — 一个月交作业，单关登山治愈。v1（驱魔师 + 多关 + 傩戏面具）已归档，详见 [`doc/plan/decisions-log.md` 2026-05-01](doc/plan/decisions-log.md)。

A first-person walking-photography game in a single mountain level, **healing tone, Cairn-style aesthetic**. The protagonist is a girl carrying her late friend's camera, climbing the mountain her friend never finished. **Through the viewfinder she can see "影 / 余像" — the mountain's memories of people who walked it before — invisible to the naked eye.** The player learns the camera by reading the friend's notebook (one page per trail segment); the protagonist also doesn't know how to use a camera, so player progression = protagonist progression. The summit goal is to take the photo the friend wanted to take. Every photo is real output: rendered through a custom simulation pipeline and saved to disk; the ending plays back the player's own album.

The challenge is a **hardcore, parameter-driven simulated camera**: ISO / shutter / aperture / focus / metering. Different "影" require different settings — fast wing-strokes need fast shutter, mist-shrouded shapes only resolve under long exposure, etc. **PhotoEvaluator outputs a score but does NOT gate completion** — taking blurry photos still progresses the level (neutral ending); on-target photos unlock the full ending and the friend's notebook full pages. Healing tone = no skill gate, no fail state, no jump scare, no combat.

This is not a cinematic post-processing toy. Exposure, motion blur, and noise are *gameplay inputs*, driven from one source of truth (`CAMCOLCameraSettings`) and applied to both realtime preview and the captured photo.

**Terminology note**: in v2 prose the antagonist concept is called **"影 / 余像"** (shadow / afterimage) instead of "鬼". Internal IDs and file paths still use the `ghost-*` prefix to avoid breaking references — see [`doc/40-ghost-rules.md` § v2 术语](doc/40-ghost-rules.md).

## Design docs (important)

The full game design lives in `doc/` (D&D-rulebook style, Chinese). **When working on gameplay / 影 / level / narrative, read the relevant doc first** — don't invent rules that already have decided answers.

- [`doc/README.md`](doc/README.md) — index of all design sections
- [`doc/00-overview.md`](doc/00-overview.md) — core loop, design pillars (**v2**)
- [`doc/10-camera-rules.md`](doc/10-camera-rules.md) — camera parameter rules, EV formula, photo judgment (**mirrors code numbers; update here when code changes**)
- [`doc/20-office-hub.md`](doc/20-office-hub.md) — ❌ **v1 deprecated** (office hub cut)
- [`doc/30-level-design.md`](doc/30-level-design.md) — level structure template (**v2 single-level**)
- [`doc/40-ghost-rules.md`](doc/40-ghost-rules.md) — 影 rules & templates (Boss / 扰乱 two kinds)
- [`doc/41-ghosts/`](doc/41-ghosts/) — individual 影 cards (v1 cards archived; v2 = 6 new cards)
- [`doc/50-mask-rules.md`](doc/50-mask-rules.md) — ❌ **v1 deprecated** (Nuo mask system cut)
- [`doc/51-masks/`](doc/51-masks/) — ❌ **v1 deprecated** (empty — no mask collection in v2)
- [`doc/60-levels/`](doc/60-levels/) — per-level entries (**v2 = single level: `01-shan.md`**)
- [`doc/70-narrative.md`](doc/70-narrative.md) — story arcs (mostly TBD)
- [`doc/90-reference/nuo-masks.md`](doc/90-reference/nuo-masks.md) — ❌ **v1 deprecated** (Nuo research no longer relevant; kept for history)
- [`doc/plan/`](doc/plan/) — **planning system** (current phase, roadmap, knowledge-lock chain, decisions log, open questions, rules-revisions). Always read [`doc/plan/README.md`](doc/plan/README.md) at the start of a new conversation to know "where we are."

## Design Workflow

**v2 = single level, no chapter slicing.** The vertical-slice cadence collapses to one milestone arc (4 weeks). Workflow is bookkept in `doc/plan/`.

**To know "where we are" right now**: read [`doc/plan/README.md`](doc/plan/README.md), or run `/next-step`.

**To design a new 影**:
1. Use the `ghost-designer` subagent for ideation (returns a markdown card, doesn't write files). Tell it explicitly that v2 is **healing tone, mountain setting, no Nuo masks, no 民俗** — the agent's prompt may still reference v1 conventions.
2. When happy, run `/new-ghost boss|minion <Chinese name>` to materialize the card to `doc/41-ghosts/`
3. ⚠️ **Do not run `/new-mask`** — mask system is deprecated in v2.

**To work on the level**:
1. Read [`doc/60-levels/01-shan.md`](doc/60-levels/01-shan.md) — single level design.
2. Open questions tracked in [`doc/plan/open-questions.md`](doc/plan/open-questions.md).
3. Run `/audit-docs` to confirm everything is filled before locking.
4. Run `/lock-level 01` once docs are complete to freeze for Unity implementation.
5. Implement in Unity (use the `unity-mcp-skill` skill for editor ops).

**To change a rule** (`doc/10-camera-rules.md`, `40-ghost-rules.md`, `30-level-design.md`, or camera code):
1. **Before editing**: open a draft entry in [`doc/plan/rules-revisions.md`](doc/plan/rules-revisions.md). Discuss with the user.
2. After editing: check whether `01-shan.md` is locked; if so, decide (a) accept drift or (b) unlock → retest → relock.
3. Archive the draft entry with actual impact.

## Slash Commands

All in `.claude/commands/`. Listed here so future conversations know they exist.

| Command | v2 status | What it does |
|---|---|---|
| `/new-ghost [boss\|minion] <name>` | ✅ in use | Create a new 影 card in `doc/41-ghosts/`, update indices. **Tell it healing-tone mountain context, not v1 folklore.** |
| `/new-mask <system-slug> <name>` | ❌ deprecated | Mask system cut in v2. Don't run. |
| `/new-level <slug> <name>` | ⚠️ optional | v2 has only 1 level (`01-shan.md`); use only if scope expands. |
| `/audit-docs [--since-rev]` | ✅ in use | Audit doc completion + camera rules vs. code drift |
| `/lock-level <NN>` | ✅ in use | Freeze level for Unity implementation. Snapshots rules-layer git SHAs. |
| `/next-step` | ✅ in use | Read-only: tells the user the next concrete action |

Subagents (also in `.claude/agents/`):
- `ghost-designer` — fuzzy 影 concept → filled card (markdown only, no file write). **Brief it on v2 healing tone explicitly** since the agent prompt still encodes v1 folklore conventions.
- `camera-tuner` — translates gameplay intent ↔ concrete camera/影 numbers
- `level-planner` — ⚠️ less useful in v2 (no multi-level planning); kept for reference

## Doc Status Legend

Every ghost / mask / level md should carry one of these markers in its first-line block (or yaml frontmatter):

| Marker | Meaning | Who can change |
|---|---|---|
| `⚠️ TBD` | Placeholder / not started | Anyone |
| `🟡 In-Progress` | Being designed | Current author |
| `🔒 Locked-for-build` | Frozen, in Unity implementation | Only via `rules-revisions.md` flow |
| `✅ Shipped` | Implemented and verified | Only via `rules-revisions.md` flow |

`/lock-level` writes the `🔒 Locked` marker plus the rules-layer git SHAs the level depends on. `/audit-docs` parses these markers to compute project completeness.

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
