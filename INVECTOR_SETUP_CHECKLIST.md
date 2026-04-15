# Invector Setup Checklist

This project already includes the Invector Third Person Controller package content under `Assets/Invector-3rdPersonController`.

## What is already verified

- Unity version is `2022.3.62f3`, which is newer than the package requirement `2022.3.12f1 LTS`.
- The package content is present:
  - `Basic Locomotion`
  - `Melee Combat`
  - `ItemManager`
  - Demo scenes, prefabs, scripts, and documentation
- No extra Unity package dependency was found for the base install:
  - No `Cinemachine` requirement
  - No `Input System` package requirement
  - No `AI Navigation` package requirement for the base controller
- The project still uses the legacy `InputManager`, and the required axes are present.

## Build Settings updated

The following scenes are now included in `ProjectSettings/EditorBuildSettings.asset` so you can validate the package directly in Unity:

1. `Assets/Scenes/SampleScene.unity`
2. `Assets/Invector-3rdPersonController/Basic Locomotion/DemoScenes/Invector_BasicLocomotion.unity`
3. `Assets/Invector-3rdPersonController/Melee Combat/DemoScenes/Invector_MeleeCombat.unity`

## Recommended validation flow in Unity

1. Open `Invector_BasicLocomotion.unity`.
2. Enter Play Mode and verify:
   - Character movement
   - Jump
   - Third-person camera rotation
   - HUD visibility
3. Open `Invector_MeleeCombat.unity`.
4. Enter Play Mode and verify:
   - Attack
   - Lock-on
   - Hit reactions
   - Weapon display and equip flow

## Prefabs to use in your own scene

- Basic third-person:
  - `Assets/Invector-3rdPersonController/Basic Locomotion/Prefabs/vBasicController.prefab`
  - `Assets/Invector-3rdPersonController/Basic Locomotion/Prefabs/vBasicController_Template.prefab`
- Melee:
  - `Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Player/vMeleeController_Inventory.prefab`
  - `Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Player/vMeleeController_NoInventory.prefab`
  - `Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Player/vMeleeController_Template.prefab`

## Important note about AI

`Simple Melee AI` uses NavMesh. If you add those enemies to your own scene, bake the NavMesh first or the AI may fail with a missing NavMesh warning.

## Conclusion

The package appears to be installed correctly. The remaining work is scene integration and runtime validation inside the Unity Editor, not installing additional required base packages.
