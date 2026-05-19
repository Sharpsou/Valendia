# Valendia

Unity 6 prototype for a procedural stylized valley.

## Open The Project

1. Open this folder with Unity `6000.4.7f1` or newer in the Unity 6 line.
2. Let Unity restore packages, including `com.unity.inputsystem`.
3. Use `Valendia > Create Prototype Scene`.
4. Open `Assets/Valendia/ValendiaPrototype.unity`.
5. Press Play.

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
