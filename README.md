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
