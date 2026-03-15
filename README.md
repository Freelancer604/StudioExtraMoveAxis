# StudioExtraMoveAxis v3.0 (Enhanced Fork)

This is a heavily expanded custom fork of the brilliant original "StudioExtraMoveAxis" plugin created by [ManlyMarco](https://github.com/ManlyMarco/StudioExtraMoveAxis). I've built upon his incredible work to incorporate several quality-of-life additions specifically geared toward making character posing more efficient and intuitive!

**Original plugin:** https://github.com/ManlyMarco/StudioExtraMoveAxis
**Fork by:** [Freelancer604](https://github.com/Freelancer604)

---

## == Expanded Features ==

- **FK Bone Cycling:** Seamlessly switch between the bones of an FK chain by scrolling your mouse wheel while hovering near the rotation gizmo.
- **Multi-Selection Drag-and-Select:** Hold Shift while scrolling your mouse to quickly highlight and group multiple contiguous bones in a chain simultaneously.
- **Intelligent Joint Constraints:** Specific joints (Knees, Elbows, Thumbs, and Fingers) now employ intelligent mathematical Rotation/Gimbal locks. When grabbing the center "free-orb" of the widget on a constrained joint, the plugin now seamlessly converts screen-drag distance directly into safe 1D rotation, completely bypassing the frustrating camera-angle projection limitations of standard studio tools!

---

## Supported Games

- Honey Select 2 (HS2)
- Koikatsu (KK)
- Koikatsu Sunshine (KKS)
- AI-Shoujo (AI)
- PlayHome (PH)

---

## Requirements

- BepInEx v5.1 or newer
- Latest [BepisPlugins](https://github.com/IllusionMods/BepisPlugins) for your game
- Latest [Modding API (KKAPI)](https://github.com/ManlyMarco/KKAPI) for your game

---

## Installation

1. Download the latest release for your game from the [Releases](../../releases) page.
2. Extract the archive into your game folder, merging with the existing `BepInEx` directory.
3. Launch Studio. Load a character, enable FK, then click the **Extra** button in the left toolbar to activate the gizmo.
4. You can adjust settings in the BepInEx plugin configuration menu.

![preview](https://user-images.githubusercontent.com/39247311/103706067-7ae92100-4fac-11eb-8246-76da09b1f67c.PNG)

---

## License

GPL-3.0 — same as the original. See [LICENSE](LICENSE).
