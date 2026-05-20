# Valendia

Unity 6 prototype for a procedural stylized valley.

## Open The Project

1. Open this folder with Unity `6000.4.7f1` or newer in the Unity 6 line.
2. Let Unity restore packages, including `com.unity.inputsystem`.
3. Open `Assets/Valendia/ValendiaBootstrap.unity` for the lightweight build/runtime entry scene, or use `Valendia > Create Prototype Scene` to regenerate the local visualization scene.
4. For editor visualization/setup, open `Assets/Valendia/ValendiaPrototype.unity`.
5. Press Play.

## Scene Roles

- `Assets/Valendia/ValendiaBootstrap.unity` is the committed, lightweight source scene used for builds. It can look empty in Edit Mode because the world is generated at runtime when Play starts.
- `Assets/Valendia/ValendiaPrototype.unity` is a generated local editor scene for visual inspection and iteration. It contains the generated world, is intentionally ignored by Git, and can be recreated with `Valendia > Create Prototype Scene`.

## Generated Artifacts

The generated prototype scene and preview are intentionally ignored by Git:

- `Assets/Valendia/ValendiaPrototype.unity`
- `Assets/Valendia/Docs/ValendiaPrototypePreview.png`

They are reproducible from the committed generator and editor menu. This keeps the repository small and avoids committing a generated scene of several hundred megabytes.

## Controls

- Keyboard/mouse: WASD or arrows, mouse look, Space jump, Left Shift sprint.
- Gamepad: left stick move, right stick look, south button jump, left stick click or left bumper sprint.

The project uses Unity input handling mode `Both`, with Input System support and legacy Input Manager fallback.

## Documentation

- Project tracking: `Assets/Valendia/Docs/Suivi_Projet_Valendia.md`
- Procedural landscape plan: `Assets/Valendia/Docs/Valendia_Procedural_Landscape_Plan.md`
- Runtime architecture: `Assets/Valendia/Docs/Architecture_Runtime.md`
