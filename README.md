# StudioExtraMoveAxis — Enhanced Fork (v2.0)

An expanded fork of ManlyMarco's [StudioExtraMoveAxis](https://github.com/ManlyMarco/StudioExtraMoveAxis), the Studio plugin that adds an extra move/rotate/scale gizmo in the bottom-right corner (toggled with the **Extra** button on the left toolbar). This fork keeps all of the original behaviour and adds several posing tools on top of it.

- **Original plugin:** https://github.com/ManlyMarco/StudioExtraMoveAxis
- **Fork by:** [Freelancer604](https://github.com/Freelancer604)

## Added features

- **Right-click drag reverse FK (counter-rotation).** Right-click and drag a bone to rotate it while its child bones counter-rotate to hold their pose, so downstream limbs stay put instead of swinging along. Constrained joints (knees, elbows, thumbs, fingers) are locked to their natural axis, and hand drags split the counter-rotation across the finger bones.
- **FK bone cycling.** Scroll the mouse wheel while hovering near the rotation gizmo to step through the bones of an FK chain without reselecting.
- **Multi-selection.** Hold Shift while scrolling to grow the selection across contiguous bones in a chain and pose them together.
- **Gimbal-safe 1D rotation.** On hinge joints, dragging the widget's centre orb converts screen-drag distance directly into a single-axis rotation, avoiding the camera-angle projection and gimbal-lock problems of the default handles.

## Supported games

Honey Select 2 (HS2), Koikatsu (KK), Koikatsu Sunshine (KKS), AI-Shoujo (AI), PlayHome (PH), and Honey Select 1 (HS1).

> The HS1 project targets the IPA framework and references its game assemblies locally, so it builds on its own rather than as part of the main solution. The other five games build from `StudioExtraMoveAxis.sln` and restore their dependencies from NuGet.

## Requirements

- BepInEx v5.1 or newer (HS1 uses IPA)
- Latest [BepisPlugins](https://github.com/IllusionMods/BepisPlugins) for your game
- Latest [Modding API (KKAPI)](https://github.com/ManlyMarco/KKAPI) for your game

## Installation

1. Download the latest release for your game from the [Releases](../../releases) page.
2. Extract the archive into your game folder, merging with the existing `BepInEx` directory.
3. Launch Studio, create or load a character, enable FK, then click the **Extra** button in the left toolbar to show the gizmo.
4. Settings for each game are under the BepInEx plugin configuration menu.

![preview](https://user-images.githubusercontent.com/39247311/103706067-7ae92100-4fac-11eb-8246-76da09b1f67c.PNG)

## License

GPL-3.0, same as the original. See [LICENSE](LICENSE).

## ExtraMoveAxis PLUS source update — 2026-10-10

This fork contains Freelancer604's modified ExtraMoveAxis PLUS source for HS2, KK, KKS, AI-Shoujo, PlayHome and HS1. It remains licensed under GNU GPL version 3; see [LICENSE](LICENSE). Original author credits and notices are retained. The upstream project is [ManlyMarco/StudioExtraMoveAxis](https://github.com/ManlyMarco/StudioExtraMoveAxis); its v3.0 numbering is independent of this fork's 2.0 DLL identity.

The October 2026 update adds one Studio undo/redo action per rotation gesture, including right-drag counterrotation of parents and children, Shift roll, constrained FK movement, middle-click reset and deferred hand/finger rotations. It also preserves Drag-Doll's existing integration contracts. Earlier HS1 scroll/toe handling and KK thumb-axis fixes are included.

Use the source commit linked beside a particular binary download to identify that download's source. This fork includes the complete ExtraMoveAxis project source and build inputs; Drag-Doll's own IK movement implementation is a separate project.

### Build from source

On Windows, install Visual Studio Build Tools with C#/.NET desktop build tools and the .NET Framework targeting packs required by the projects (3.5 and 4.6.2), plus NuGet CLI.

1. Clone this repository.
2. Run `nuget restore StudioExtraMoveAxis.sln` from the repository root.
3. Run `MSBuild StudioExtraMoveAxis.sln /t:Build /p:Configuration=Release /p:SkipDeploy=true` for PH, KK, AI, HS2 and KKS. Outputs are written to `bin/`.
4. HS1 is a separate legacy IPA project. Supply its references from your own Honey Select Studio installation in `HS1_StudioExtraMoveAxis/`: `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `IllusionPlugin.dll`, `UnityEngine.dll`, `UnityEngine.UI.dll`, `UnityEngine.UI.Translation.dll`, and the installed compatible `0Harmony.dll`. Then build `HS1_StudioExtraMoveAxis/HS1_StudioExtraMoveAxis.csproj` with the same Release options. Game assemblies are excluded from this repository.

### Regression checks

Run each script in a separate PowerShell 7 process:

- `pwsh -NoProfile -File tests/UndoRegression.ps1`
- `pwsh -NoProfile -File tests/HS1-ScrollRegression.ps1`
- `pwsh -NoProfile -File tests/KK-ThumbRegression.ps1`

These execute source code against small engine doubles. They cover grouped history, repeated undo/redo, deferred fingers, native release cleanup, interrupted gestures and existing scroll/thumb behavior. They do not replace live Studio testing. AI/PlayHome runtime testing requires those games; local reference-assembly builds establish API compatibility only.
