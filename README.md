# Big Five Heritage Safari

> Photograph the Big Five without disturbing them, and keep your Respect at 100.

I built this for the Introduction to Unity module (Course 3152). You are a visitor on a Kenyan savanna reserve, guided by Ranger Amani on the radio. Walk up to each animal, step inside the white circle, take a photo, and never cross the red one.

## GCGO

- Grand Challenge: Tourism
- Mission statement: Wildlife tourism only lasts if visitors watch animals without disturbing them. This simulation teaches safe, respectful wildlife viewing through a guided Big Five photo safari.

## Problem

**What.** Wildlife tourism only works if visitors watch animals from a respectful distance. When visitors crowd, chase or startle animals, the animals get hurt and move away, and the sightings tourists came for get worse.

**Why it matters.** Tourism contributed 9.3% of Kenya's GDP in 2025 ([WTTC](https://wttc.org/news/kenya-boosts-africas-tourism-growth-emerging-as-a-leading-sustainable-tourism-powerhouse)). The Kenya Wildlife Service asks visitors to view wildlife from a minimum distance of 20 metres ([KWS](https://kws.go.ke/park/nairobi-national-park)).

**Link to Tourism.** Distance is the part I could put in the game. Stand too close and the animal startles, and the reserve takes Respect off you. My zones are scaled down (15 to 18 m viewing, 6 to 8 m danger) so the reserve fits in one run. They teach the idea of a safe distance, not the real 20 m.

## How it plays

The player walks the reserve in first person and photographs the lion, leopard, elephant, rhino and buffalo. Ranger Amani marks a trail line to the nearest animal that still needs a photo. Each photo opens a fact card with one fact and one safe viewing tip. Photograph all five to finish. The end screen shows your photos, your Respect and your visitor rating.

**Target users.** First-time safari visitors, school students learning about responsible tourism, and trainee guides.

| Rule | What it means |
| :--- | :--- |
| White circle (15 to 18 m) | The ranger's viewing zone. Take photos from inside it |
| Gold circle | The white circle turns gold once that animal is in your passport |
| Red circle (6 to 8 m) | Never enter it. Costs 10 Respect, at most once every 3 seconds per animal |
| Photo | Aim at the animal and press photo. The first photo of each animal gives a passport stamp |
| Respect | Starts at 100. Each disturbance costs 10 |
| Rating | No disturbances: Responsible Visitor. One or two: Careful Visitor. Three or more: Reckless Visitor |

## Controls

| Action | Computer | Phone |
| :--- | :--- | :--- |
| Move | `WASD` | Left on-screen stick |
| Look | Mouse (arrow keys also work) | Right on-screen stick |
| Take photo | Left click or `E` | Photo button |
| Close fact card | `E` or left click | Close button on the card |
| Pause | `Esc` or `P` | `II` button on the HUD |
| Mouse sensitivity | Slider on the pause menu | Not shown |

A gamepad works too: left stick to move, right stick to look, south button to photograph, Start to pause.

## Unity mechanics

| Mechanic | Where in the game |
| :--- | :--- |
| UI | `UIManager` runs the start screen, HUD, fact card and end screen. `PauseMenu` runs the pause menu. Every button is wired in the Inspector with On Click |
| Scripting and New Input System | All input goes through the New Input System (`Assets/Input/InputSystem_Actions.inputactions`). `PlayerController` moves and looks, and `AnimalScanner` takes the photo |
| Collision | A `CharacterController` keeps the player on the ground and out of animals, trees and rocks. The viewing and danger zones are triggers, so they never block |
| Raycasting | `AnimalScanner` casts a ray from the middle of the screen every frame to colour the crosshair, and again on each photo |
| Line Renderer | `SafariGuideLine` draws the trail to the nearest animal. `ZoneRing` draws the white and red circles on the ground |

## Additional features

- Audio: a savanna ambience, an animal call on every photo, a shutter click and a bump sound.
- Particles: dust at the player's feet and fireflies over the reserve.
- Animal reactions: an animal turns to face you, and a startled animal makes a short bluff charge.
- Photo and danger feedback: a white flash on each photo, and a red edge flash and camera shake in the danger zone.
- Pause menu: Esc opens Resume, Restart Safari, How to Play, Main Menu and a mouse sensitivity slider.

## Builds

- [WebGL build](TODO_WEBGL_URL)
- [Android APK](TODO_APK_URL)
- [Demo video](TODO_VIDEO_URL)

Builds are made with File > Build Profiles. WebGL compression is Disabled for GitHub Pages, and the APK is made with "Build App Bundle" unticked.

## Requirements

- Unity `6000.3.23f1` with Unity Hub
- Universal Render Pipeline 17.3.0 and Input System 1.20.1, both in the project

## Getting started

```bash
git clone https://github.com/Masalale/Mission-Based-Interactive-Simulation.git
```

Add the folder in Unity Hub, open `Assets/Scenes/Safari_Main.unity` and press Play.

## Reflection

- **What went well.** The core loop works: walk the reserve, photograph the five animals, and read the rating. The rings and the Respect score show the distance rule in the game instead of only describing it.
- **A bug and its fix.** The on-screen sticks kept disappearing. PlayerInput was switching control schemes on its own and hiding the other device's bindings. `PlayerController.Awake` now turns that switching off and clears the binding mask.
- **Next.** Run the APK on a phone, which I have not done yet. Then a real leopard model in place of the jaguar, and walking animals.

## Credits

| Asset | Source | Licence |
| :--- | :--- | :--- |
| Lion, elephant, rhino, buffalo models | Grab3D (grab3d.com); buffalo is their bull model | CC0 |
| Leopard model | "Jaguar" by Poly by Google, https://poly.pizza/m/4fb-oMr2uUF (a jaguar standing in for the leopard) | CC BY 3.0 |
| Trees, rocks, ground cover | Kenney Nature Kit, kenney.nl | CC0 |
| Ground patches, hills, mountain | Made by me in Unity | n/a |
| Lion, elephant, buffalo calls | Pixabay | Pixabay Content License |
| Leopard call | YouTube | Creative Commons |
| Rhino call | "geoffvt", https://freesound.org/people/geoffvt/sounds/792852/ | CC0 |
| Savanna ambience | "dkaufman", https://freesound.org/people/dkaufman/sounds/743640/ | CC0 |
| Bump sound | "JonasTisell", https://freesound.org/people/JonasTisell/sounds/496187/ | CC0 |
| Camera shutter | Made by me with ffmpeg | n/a |
| Tribeca font (titles) | [1001fonts.com](https://www.1001fonts.com/tribeca-font.html) | Free for commercial use |
| Ubuntu font (body text) | Canonical, fonts.google.com/specimen/Ubuntu | Ubuntu Font Licence 1.0 (licence file next to the font in `Assets/Materials/Fonts`) |

