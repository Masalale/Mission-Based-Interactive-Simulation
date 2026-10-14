# Big Five Heritage Safari

[WebGL build](TODO_WEBGL_URL) | [Android APK](TODO_APK_URL) | [Demo video](TODO_VIDEO_URL)

I built this for the Introduction to Unity module (Course 3152). It is a first-person safari through a Kenyan savanna reserve. I made the player walk around, find the Big Five, and scan each animal from a safe distance. Every scan gives one fact and one job, so the game teaches both wildlife viewing and the careers that come with it.

- Author: Masalale
- Repository: https://github.com/Masalale/Mission-Based-Interactive-Simulation
- Unity 6000.3.23f1, Universal Render Pipeline 17.3.0, Input System 1.20.1

I have the WebGL build ready and I checked it in a desktop browser. I also built the Android APK, but I have not run it on a phone yet. Both links above are still placeholders until I publish them.

## 1. GCGO Statement

- Grand Challenge: Tourism
- Global Opportunity: Job creation
- Mission statement: Wildlife tourism promotes conservation and creates ranger, guide, lodge, and craft jobs. This simulation teaches safe viewing while showing the careers each animal supports.

## 2. Problem Context

**The problem.** Wildlife tourism only works if visitors stay a safe distance from the animals and if the people around the reserves earn from it. When visitors crowd or chase animals, the animals get hurt and the sightings get worse. Jobs like tracker, guide, ranger and driver are what keep a conservancy funded.

**Why it matters.** The World Travel & Tourism Council (WTTC) reports that travel and tourism "supported 1.8 million jobs, equivalent to 8.3% of total employment" in Kenya in 2025, and gave 9.3% of national GDP. [1]

Distance matters for the same reason. The Kenya Wildlife Service visitor guide, as quoted by The Standard on 24 December 2025, tells visitors to keep at least 20 metres when they view wildlife. [2]

My viewing zone is 15 to 18 m and my danger zone is 6 to 8 m. I scaled them down because the reserve is small enough to walk in one run. They teach the idea of a safe distance and a too-close warning. They are not the real Kenya Wildlife Service distance.

**How it connects to the GCGO.** The Tourism challenge is about wildlife viewing that protects the animals it depends on. The Job creation opportunity is about the careers that viewing pays for. Every fact card names one job the animal supports, and the Jobs Supported counter goes up by one for each stamp. The danger zone is the other half of the lesson. Keep your distance and the product stays alive.

The two sources for this section are listed in section 9.

## 3. Simulation Overview

**What it does.** The player walks the reserve in first person and looks for the lion, leopard, elephant, rhino and buffalo. Each animal has a viewing zone. Stand inside the zone, aim at the animal and press Scan. A fact card shows one fact and the job the animal supports. The first scan of each animal gives a passport stamp and one more job. Walking into the danger zone shows a warning and costs 10 points.

**Target users.** Students learning about wildlife tourism, young job seekers who want to know which conservation careers exist, and would-be tourists who want to know how to watch wildlife safely.

**Key interactions.**

- Walk around and look.
- Follow the guide line to the nearest animal that still needs a stamp.
- Stand in the viewing zone, aim, and scan.
- Read the fact and the job behind it.
- Keep out of the danger zone.

**Controls.**

| Action | Desktop | Phone |
| --- | --- | --- |
| Move | WASD | Left on-screen stick |
| Look | Arrow keys, or hold the right mouse button and move the mouse | Right on-screen stick |
| Scan | E, or the gamepad south button | Scan button on the HUD |

The sticks show only on phones and tablets. A gamepad uses the left stick to move, the right stick to look, and the south button to scan.

**How to win.** Stamp all five animals. An animal counts when the player scans it from inside its viewing zone. There is no lose state. When the player closes the last fact card, the end screen shows the stamps, the score, the breaches and the jobs, and offers Replay.

**How the score works.** The score starts at 100 and never goes below 0. Each danger zone breach costs 10 points, and one animal can book a breach only once every 3 seconds. Each first scan adds one job. Scanning the same animal again only reopens the fact card. The top score is 100, so a clean run has zero breaches.

## 4. Unity Mechanics Implemented

### UI

`Assets/Scripts/UIManager.cs` runs every screen: the start screen with Continue and New Safari, the HUD with the crosshair, the passport, the score and the message lines, the fact card, and the end screen with Replay. The Canvas and the panels are laid out in `Assets/Scenes/Safari_Main.unity`. The HUD is built switched off and turned on when a run starts, because the on-screen controls were throwing an error while they were active at load time.

`UIManager` never polls for game state. It listens to the two events on `GameManager` and refreshes when they fire. Without it there is no objective, no feedback after a scan, no start screen and no win.

### Scripting and the New Input System

Input goes only through the New Input System. No script uses the old `Input` class. I used one input asset, `Assets/Input/InputSystem_Actions.inputactions`, and one gameplay action map on it.

| Action | Type | Desktop binding | Gamepad binding |
| --- | --- | --- | --- |
| Move | Value | WASD, 2D Vector | Left stick |
| Look | Value | Arrow keys, 2D Vector | Right stick |
| MouseLook | Value | Mouse delta | none |
| LookHold | Button | Right mouse button | none |
| Interact | Button | E | South button |

The Player object has a `PlayerInput` component with that asset and `Player` as the default map. `AnimalScanner` subscribes to `Player/Interact` in code. The EventSystem uses `InputSystemUIInputModule`, and Active Input Handling in the Player settings is set to the Input System package only.

Sticks and arrow keys give a rate, so `PlayerController` multiplies them by the look speed and by `deltaTime`. The mouse already gives a per-frame delta, so it is multiplied by the sensitivity only.

The on-screen controls are two sticks and the Scan button. The sticks add a virtual gamepad while the mouse is still in use, and `PlayerInput` was switching control schemes on its own and hiding the other bindings, so the sticks dropped out. `PlayerController.Start` turns that switching off and clears the binding mask. Keyboard, mouse and the on-screen gamepad then drive the same action map together.

Without this there is no movement, no scanning and no run state at all.

### Collision

`Assets/Scripts/PlayerController.cs` moves the player with a `CharacterController`, so walking is collision based and the player cannot leave the ground. `Assets/Scripts/Animal.cs`, `Assets/Scripts/ViewingZone.cs` and `Assets/Scripts/DangerZone.cs` handle the animals.

Each animal has a solid `BoxCollider` on the Animal layer, which blocks the player, and two sphere triggers, one for the viewing zone and one for the danger zone. `Animal` copies the two radii out of its data asset at load, so they are 15 m and 6 m for the lion, leopard and buffalo, and 18 m and 8 m for the elephant and rhino.

A trigger does not block anybody, it only sends events. Entering the viewing zone sets `InViewing` and shows the hint. Entering the danger zone shows the warning and books a breach, at most one every 3 seconds per animal. Walking into a solid body plays the bump sound and shows a toast that names what blocked the player. Hits on the floor are ignored, otherwise the ground would bump the player every frame.

Without collision the player walks through the animals, there are no zone events, no breaches and no scan window.

### Raycasting

`Assets/Scripts/AnimalScanner.cs` does the scanning. On Interact it builds a ray from the middle of the screen and casts it 20 m, on the Animal layer only, and with triggers ignored so the zone spheres cannot block the shot.

A miss shows "Nothing in your sights. Aim at an animal." A hit on an animal outside its viewing zone shows "Move into the viewing zone first." A hit on an animal inside its zone is the only way to earn a stamp, and the call only reaches `GameManager.AddStamp`. Scanning again awards nothing, it just plays the call and reopens the card.

Without it there are no scans, no stamps and no way to win.

### Line Renderer

`Assets/Scripts/SafariGuideLine.cs` draws the guide line on a `LineRenderer` in the scene. It has two points: the player, lifted a little off the ground, and the nearest animal that still needs a stamp. It updates in `LateUpdate` so it follows the player, and it picks a new target every time the game state changes. When the passport is full, or on the start screen, it hides itself. The lion stands 50 m from the spawn so the line runs straight ahead at the start of a run.

Without it the player has no idea where to look first.

## 5. Additional Features

### Audio

There are seven clips in `Assets/Audio`. A savanna ambience loops on the Ambience object. `PlayerController` plays a short bump sound when the player walks into something solid. `AnimalScanner` plays the animal call on every valid scan, not only the first one, so the animal answers each time.

### Particles

Two `ParticleSystem` objects, one for dust at the player's feet and one for fireflies over the reserve. No script drives them, all the settings are in the scene. Both are kept small, under 100 particles, so a phone can handle them.

### Saving

`Assets/Scripts/SaveSystem.cs` writes the run as JSON in `PlayerPrefs` using `JsonUtility`. WebGL has no file system, and `PlayerPrefs` works there. `GameManager` saves after every stamp and every breach. Continue reads the save back and puts the run in the same state. New Safari and Replay clear it. I use `PlayerPrefs` and `JsonUtility` because they are the simplest saving that works on both of my platforms.

## 6. Project Structure

```
Assets/
  Audio/            seven clips: one ambience, five calls, one bump
  Editor/           import settings and the build script
  Images/           the title image
  Input/            InputSystem_Actions.inputactions
  Materials/        the ground, the animal, the guide line and the scenery materials
  Models/
    Animals/        the five animal models and their textures
    Generated/      the merged ground, hills and track meshes
    Scenery/        the Kenney trees, rocks and ground cover
  Prefabs/          PF_Animal and one variant per species
  Scenes/           Safari_Main.unity
  ScriptableObjects/ one asset per animal, holding its fact, job and radii
  Scripts/          the ten gameplay scripts below
  ThirdParty/       the licence files for the models, the scenery and the audio
```

`PF_Animal` is the base prefab. Its children are Body, which holds the model and the solid collider, and the two zone triggers. Each species has a variant that overrides the model, the scale and the data asset.

| Script | What it does | Lines |
| --- | --- | --- |
| `Assets/Scripts/PlayerController.cs` | Walking, looking, gravity, bump feedback, spawn reset | 176 |
| `Assets/Scripts/AnimalScanner.cs` | The scan ray, the stamp, the call and the fact card | 107 |
| `Assets/Scripts/Animal.cs` | Per-animal state, zone radii, list of every animal | 73 |
| `Assets/Scripts/AnimalData.cs` | The data asset: fact, job, radii, call clip | 35 |
| `Assets/Scripts/ViewingZone.cs` | The trigger that allows a scan and shows the hint | 55 |
| `Assets/Scripts/DangerZone.cs` | The trigger that warns and books breaches | 51 |
| `Assets/Scripts/SafariGuideLine.cs` | The guide line to the nearest unscanned animal | 96 |
| `Assets/Scripts/GameManager.cs` | Stamps, score, breaches, jobs, win, save and restore | 167 |
| `Assets/Scripts/UIManager.cs` | Every screen, the HUD and the fact card | 392 |
| `Assets/Scripts/SaveSystem.cs` | Saving, loading and clearing the run | 61 |

That is 1,213 lines in total. `Assets/Editor` holds three more: the two import scripts for the models and the scenery, and `Assets/Editor/BuildScript.cs` for the two builds. I used editor scripts to lay out the scene and the prefabs while I built them, and I finished the scene by hand in the Editor afterwards.

## 7. Build Information

**WebGL.** About 61 MB, 1280 x 720, in `Builds/WebGL`. Compression is switched off, because GitHub Pages will not send the header a compressed build needs. To run it, open the link at the top of this file, then press New Safari.

**Android.** An APK, not an app bundle, built with IL2CPP for ARM64, landscape only, about 36 MB, in `Builds/Android/BigFiveHeritageSafari.apk`. I built it but I have not tested it on a phone yet. To run it, copy the APK to the phone, allow installs from that file, and open it. It uses the two sticks and the Scan button.

Both builds use the Mobile quality level, which turns off real-time shadows. The animals use blob shadows instead. Both builds use the one scene, `Assets/Scenes/Safari_Main.unity`.

**Run it in the Editor.** Install Unity 6000.3.23f1 from Unity Hub, add this folder, open `Assets/Scenes/Safari_Main.unity` and press Play. For an APK you also need Android Build Support with the SDK, NDK and OpenJDK.

**Build it again.** Close the Editor first, because Unity locks the project while it is open. From the Editor menu use Build > Build WebGL or Build > Build Android APK. From a command prompt:

```
Unity.exe -batchmode -quit -nographics -projectPath <project path> -executeMethod BuildScript.BuildWebGL -logFile <log path>
```

Swap `BuildScript.BuildWebGL` for `BuildScript.BuildAndroid` for the APK. Each build prints one line that starts with `[Build]` with the result, the size and the error count.

## 8. Reflection

**Problems I hit and fixed.** The full list is in my bug notes, `misc/BUGS.md`. The ones I remember best:

- The fact card came up empty. My `fact` and `jobDescription` fields had `[TextArea]` but no `[SerializeField]`, so Unity never saved them into the assets and the card had nothing to show.
- The animals all faced away from me. I had looked at them through cameras that were on the far side, so I turned every model to point at the approach instead.
- Most of the trees and rocks were placed off the map, because my placement helper mixed local and world space.
- The end screen opened behind the fifth fact card. Now the card shows first and the end screen comes up when I close it.
- The on-screen sticks kept disappearing. PlayerInput was switching control schemes on its own and hiding the other device's bindings.

**WebGL and Android.** GitHub Pages will not serve a compressed build, so I turned compression off. A particle system logged an error every frame and filled the browser console with it, because only the y velocity curve was set. The sticks showed up in desktop browsers as well, because a desktop browser reports a touchscreen, so now they only show on a real mobile platform. On the Android side the build settings are all set, but the APK has still never run on a phone, and that is the first thing I should do next.

**The hardest part.** The input setup. Keyboard, mouse, gamepad and the on-screen sticks all have to drive one action map, and PlayerInput fights that by switching schemes. Working out that I had to stop it switching, and clear the binding mask, was the part that took me longest.

**Next steps.** A real leopard model instead of the jaguar. Walking animals, since the models are static meshes now. The real 20 metre distance from the Kenya Wildlife Service instead of my scaled-down zones. Swahili names and text. And a full run on a phone.

## 9. Credits and Licences

**Models and scenery.**

| Asset | Licence |
| --- | --- |
| Lion, elephant, rhino and buffalo, from Grab3D (grab3d.com) | CC0 1.0. The buffalo is Grab3D's bull model standing in for a Cape buffalo. Licence: `Assets/ThirdParty/Grab3D-LICENSE.txt` |
| Leopard, which is the jaguar model "Jaguar" by Poly by Google, https://poly.pizza/m/4fb-oMr2uUF | CC BY 3.0. Licence: `Assets/ThirdParty/POLY-JAGUAR-LICENSE.txt` |
| Trees, rocks and ground cover, from the Kenney Nature Kit (kenney.nl) | CC0 1.0. Licence: `Assets/ThirdParty/KENNEY-NATURE-KIT-LICENSE.txt` |

Attribution for the jaguar model:

> "Jaguar" by Poly by Google, licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/), via poly.pizza.

I converted the jaguar from GLB to FBX, scaled it and renamed its material. It is not a leopard model. No leopard model under CC0 or CC BY was free to download without an account, so the jaguar stands in, and its rosettes are close enough to read as a leopard in the game.

Tribeca font and title image (`Assets/Images/background_removed.png`). The font has no separate lower-case letters, so all the text in the game shows in capitals.

**Audio.** All seven clips are listed with their sources and processing in `Assets/ThirdParty/AUDIO-LICENCES.txt`.

| File | Sound | Author | Licence |
| --- | --- | --- | --- |
| `Assets/Audio/SFX_Lion.ogg` | Lion roars 1.wav | eardeer | CC BY 4.0 |
| `Assets/Audio/SFX_Leopard.ogg` | Jaguar saw | About Zoos | CC BY 4.0 |
| `Assets/Audio/SFX_Elephant.ogg` | Elephant voice, trumpeting | தகவலுழவன் (Info-farmer) | CC0 1.0 |
| `Assets/Audio/SFX_Rhino.ogg` | white Rhino fighting | geoffvt | CC0 1.0 |
| `Assets/Audio/SFX_Buffalo.ogg` | Buffles-Grognement+amb oiseaux | roubignolle (G de Courtivron) | CC BY 4.0 |
| `Assets/Audio/AMB_Savanna.ogg` | Birds and wind at Ngutuni Lodge | dkaufman | CC0 1.0 |
| `Assets/Audio/SFX_Bump.ogg` | Light Body thud (on clothing) | JonasTisell | CC0 1.0 |

Attribution for the three clips that need it:

> "Lion roars 1.wav" by eardeer, https://freesound.org/people/eardeer/sounds/458903/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Jaguar saw" by About Zoos, https://commons.wikimedia.org/wiki/File:Jaguar_saw.flac, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Buffles-Grognement+amb oiseaux" by roubignolle (G de Courtivron), https://freesound.org/people/roubignolle/sounds/35130/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

Three of the calls are substitutions. The leopard plays the jaguar call, because no leopard call under CC0 or CC BY was found. The elephant call does not name its species and is probably an Asian elephant. The rhino call is a white rhinoceros, used for my black rhino model. The buffalo call still has some birdsong in it, which the filter only reduces.

**Fact card sources.** Elephant status and decline, IUCN press release, 25 March 2021. Black rhino figures, Kenya Wildlife Service figures as reported by WWF-UK, 26 February 2024. Lion figures, Kenya Wildlife Service figures as reported by Anadolu Agency, 16 January 2024. Leopard and buffalo facts, the Wikipedia articles "Leopard" and "African buffalo". The job descriptions are my own general descriptions of typical roles.

**Sources.**

1. World Travel & Tourism Council (WTTC), news release "Kenya Boosts Africa's Tourism Growth, Emerging as a Leading Sustainable Tourism Powerhouse", 15 June 2026, https://wttc.org/news/kenya-boosts-africas-tourism-growth-emerging-as-a-leading-sustainable-tourism-powerhouse
2. Kenya Wildlife Service visitor guide, as quoted by The Standard, 24 December 2025, https://thestandard.ke/business/amp/national/article/2001537234/planning-a-safari-here-is-the-official-kws-guide-for-park-visitors

