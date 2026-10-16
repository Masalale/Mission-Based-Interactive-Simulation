# Big Five Heritage Safari

> Photograph Kenya's Big Five without disturbing them, and keep your Respect at 100.

You are a visitor on a Kenyan savanna reserve, guided by Ranger Amani on the radio. Walk up to each animal, step inside the white circle, take a photo, and never cross the red one.

## GCGO

- **Grand Challenge:** Tourism
- **Mission:** Wildlife tourism only lasts if visitors watch animals without disturbing them. This simulation teaches safe, respectful wildlife viewing through a guided photo safari of Kenya's Big Five.

## Problem

- Visitors who crowd, chase or startle animals drive them away, and the sightings tourists came for get worse.
- Tourism was 9.3% of Kenya's GDP in 2025 ([WTTC](https://wttc.org/news/kenya-boosts-africas-tourism-growth-emerging-as-a-leading-sustainable-tourism-powerhouse)), and the Kenya Wildlife Service asks visitors to keep at least 20 m from wildlife ([KWS](https://kws.go.ke/park/nairobi-national-park)).
- The game turns that distance rule into play. The zones are scaled down (15 to 18 m viewing, 6 to 8 m danger) so the reserve fits in one run.

## How it plays

- Walk the reserve in first person and photograph Kenya's Big Five: lion (Simba), leopard (Chui), elephant (Tembo), rhino (Kifaru) and buffalo (Nyati).
- A trail line leads to the nearest animal still missing from your passport.
- Each photo opens a fact card: your approach (calm or disturbed), where in Kenya to see the animal, its warning signs, and a do and a don't.
- Photograph all five to see your Respect and visitor rating.
- **Target users:** first-time safari visitors, school students and trainee guides.

| Rule | What it means |
| :--- | :--- |
| White circle | Safe distance. Take photos from inside it |
| Red circle | Too close. The animal charges and you lose 10 Respect |
| Gold circle | That animal is already in your passport |
| Rating | 0 disturbances: Responsible. 1 to 2: Careful. 3 or more: Reckless |

## Controls

| Action | Computer | Phone |
| :--- | :--- | :--- |
| Move | `WASD` | Left stick |
| Look | Mouse | Right stick |
| Photo | Left click or `E` | Photo button |
| Pause | `Esc` | `II` button |

## Unity mechanics

- **UI:** start screen, HUD, fact card, end screen and pause menu (`UIManager`, `PauseMenu`). Buttons are wired with On Click in the Inspector.
- **Scripting and New Input System:** all input goes through `InputSystem_Actions` and PlayerInput. `PlayerController` moves, `AnimalScanner` takes the photo.
- **Collision:** a `CharacterController` keeps the player out of animals, trees and rocks. The viewing and danger zones are triggers.
- **Raycasting:** a ray from the screen centre colours the crosshair and takes the photo.
- **Line Renderer:** the trail line (`SafariGuideLine`) and the circles on the ground (`ZoneRing`).

## Additional features

- Audio: ambience, animal calls, shutter and bump sounds
- Particles: dust and fireflies
- Animal reactions: animals turn to face you and bluff-charge
- Photo flash, red danger flash and camera shake
- Pause menu with mouse sensitivity

## Builds

- [WebGL build](TODO_WEBGL_URL)
- [Android APK](TODO_APK_URL)
- [Demo video](TODO_VIDEO_URL)

## Requirements

- Unity `6000.3.23f1` and Unity Hub

## Getting started

```bash
git clone https://github.com/Masalale/Mission-Based-Interactive-Simulation.git
```

Add the folder in Unity Hub, open `Assets/Scenes/Safari_Main.unity` and press Play.

## Reflection

- **Went well:** the rings and the Respect score show the distance rule instead of just describing it.
- **Bug:** the phone sticks kept disappearing because PlayerInput switched control schemes on its own. I turned the switching off in `PlayerController.Awake`.
- **Next:** a real leopard model and animals that walk.

## Credits

| Asset | Source | Licence |
| :--- | :--- | :--- |
| Lion, elephant, rhino, buffalo models | Grab3D (grab3d.com) | CC0 |
| Leopard model | "Jaguar" by Poly by Google, [poly.pizza](https://poly.pizza/m/4fb-oMr2uUF) | CC BY 3.0 |
| Trees, rocks, ground cover | Kenney Nature Kit, kenney.nl | CC0 |
| Lion, elephant, buffalo calls | Pixabay | Pixabay Content License |
| Leopard call | YouTube | Creative Commons |
| Rhino call | geoffvt, [freesound](https://freesound.org/people/geoffvt/sounds/792852/) | CC0 |
| Savanna ambience | dkaufman, [freesound](https://freesound.org/people/dkaufman/sounds/743640/) | CC0 |
| Bump sound | JonasTisell, [freesound](https://freesound.org/people/JonasTisell/sounds/496187/) | CC0 |
| Tribeca font | [1001fonts.com](https://www.1001fonts.com/tribeca-font.html) | Free for commercial use |
| Ubuntu font | Canonical, [Google Fonts](https://fonts.google.com/specimen/Ubuntu) | Ubuntu Font Licence 1.0 |
