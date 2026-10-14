# Big Five Heritage Safari

[WebGL build](TODO_WEBGL_URL) | [Android APK](TODO_APK_URL) | [Demo video](TODO_VIDEO_URL)

I built this for the Introduction to Unity module (Course 3152). It is a first-person walking safari through a Kenyan savanna reserve. Ranger Amani is my guide on the radio. He marks a trail line to each animal, I walk to it, step inside the white circle he has drawn on the ground and take a photo, without ever crossing the red circle. Five photos fill my Safari Passport and the safari is complete. Every disturbance costs me Respect, so the whole game is really about how close I dare to get.

- Author: Masalale
- Repository: https://github.com/Masalale/Mission-Based-Interactive-Simulation
- Unity 6000.3.23f1, Universal Render Pipeline 17.3.0, Input System 1.20.1

I have the WebGL build ready and I checked it in a desktop browser. I also built the Android APK, but I have not run it on a phone yet. Both links above are still placeholders until I publish them.

## 1. GCGO Statement

- Grand Challenge: Tourism
- Mission statement: Wildlife tourism only lasts if visitors watch animals without disturbing them. This simulation teaches safe, respectful wildlife viewing through a guided Big Five photo safari.

## 2. Problem Context

**The problem.** Wildlife tourism only works if visitors watch animals from a respectful distance. When visitors crowd, chase or startle animals, the animals get hurt and move away, and the sightings the tourists came for get worse. A reserve that is driven to be too busy about it loses the thing it is selling.

**Why it matters.** The World Travel & Tourism Council (WTTC) says tourism contributed 9.3% of Kenya's GDP in 2025. [1]

Distance is the part I could put in the game. The Kenya Wildlife Service visitor guidelines tell visitors to remain in their vehicle except at designated areas, to move slowly and quietly because animals can react dangerously if startled, and to view wildlife from a minimum distance of 20 metres. [2]

My viewing zone is 15 to 18 m and my danger zone is 6 to 8 m. I scaled them down because the reserve is small enough to walk in one run. They teach the idea of a safe distance and a too-close warning. They are not the real Kenya Wildlife Service distance.

**How it connects to the GCGO.** The Tourism challenge is about wildlife viewing that protects the animals it depends on. Every fact card ends with a ranger's safe viewing tip taken from a real visitor guideline, and the score is not points but the visitor's Respect. Stand too close and the animal startles, calls, and the reserve takes 10 Respect off you. Leave it alone and you keep all 100.

The two sources for this section are listed in section 9.

## 3. Simulation Overview

**What it does.** The player walks the reserve in first person and looks for the lion, leopard, elephant, rhino and buffalo. Each animal has a viewing zone and a danger zone, drawn on the ground as a white ranger's circle and a red circle. Stand inside the white circle, aim at the animal and press the photo button. A white flash, a shutter click, and a fact card show one fact and one safe viewing tip. The first photo of each animal gives a passport stamp. Step into the red circle and Ranger Amani reads the animal's warning line, the animal startles, the screen flashes red and the camera shakes, and that is one disturbance.

**Target users.** First-time safari visitors who want to know how to watch wildlife safely, school students learning about responsible tourism, and trainee guides who need the distance rules in one line.

**Key interactions.**

- Walk around and look, with the mouse always looking on a desktop.
- Follow the trail line Ranger Amani marks to the nearest animal that still needs a photo.
- Step inside the white ranger's circle, aim at the animal and take your photo.
- Read the fact and the ranger's tip behind it, and close the card with Interact.
- Read the crosshair before you press anything: green means a good photo, amber means get closer.
- Never cross the red circle.

**Controls.**

| Action | Desktop | Phone |
| --- | --- | --- |
| Move | WASD | Left on-screen stick |
| Look | Mouse, always, no button held. Arrow keys also work | Right on-screen stick |
| Photo | Left click, or E, or the gamepad south button | Photo button on the HUD |
| Pause | Esc, or P, or the gamepad start button, or the II button on the HUD | II button on the HUD |
| Close a fact card | Interact, so the mouse stays locked | Close button on the card |
| Mouse sensitivity | Slider on the pause menu, saved in PlayerPrefs | not shown, a phone has no mouse |

The sticks show only on phones and tablets. A gamepad uses the left stick to move, the right stick to look, and the south button to take a photo. The pause button is on the HUD on every platform, so a phone can pause too.

**How to win.** Photograph all five animals. An animal counts when the player takes a photo of it from inside its ranger's circle. There is no lose state. When the player closes the last fact card, the end screen shows the photos, the Respect score, the disturbances and a visitor rating, and offers Replay.

**How the score works.** The score is called Respect and it starts at 100. Each disturbance costs 10, and one animal can only book one disturbance every 3 seconds. Photographing the same animal again only reopens the fact card and costs nothing. The top Respect score is 100, so a clean run is a run with zero disturbances. The end screen rates the visitor from the disturbances: none is a Responsible Visitor, one or two is a Careful Visitor, and three or more is a Reckless Visitor.

## 4. Unity Mechanics Implemented

### UI

`Assets/Scripts/UIManager.cs` runs every screen: the start screen with Continue and New Safari, the HUD with the crosshair, the passport, the Respect score and the message lines, the fact card, and the end screen with Replay. `Assets/Scripts/PauseMenu.cs` runs the pause panel. The Canvas and every panel are laid out in `Assets/Scenes/Safari_Main.unity`. The HUD is built switched off and turned on when a run starts, because the on-screen controls were throwing an error while they were active at load time.

`UIManager` never polls for game state. It listens to the two events on `GameManager` and refreshes when they fire. Without it there is no objective, no feedback after a photo, no start screen and no win.

The fact card shows the title, habitat, status, fact and then the ranger's tip, which comes from the same `AnimalData` asset as the warning line. The close button reads "Close (E)" on a desktop and "Close" on a phone.

### Scripting and the New Input System

Input goes only through the New Input System. No script uses the old `Input` class. I used one input asset, `Assets/Input/InputSystem_Actions.inputactions`, and one gameplay action map on it.

| Action | Type | Desktop binding | Gamepad binding |
| --- | --- | --- | --- |
| Move | Value | WASD, 2D Vector | Left stick |
| Look | Value | Arrow keys, 2D Vector | Right stick |
| MouseLook | Value | Mouse delta | none |
| Interact | Button | E, left mouse button | South button |
| Pause | Button | Esc, P | Start button |

The Player object has a `PlayerInput` component with that asset and `Player` as the default map. `AnimalScanner` subscribes to `Player/Interact` and `PauseMenu` subscribes to `Player/Pause` in code. The EventSystem uses `InputSystemUIInputModule`, and Active Input Handling in the Player settings is set to the Input System package only.

Sticks and arrow keys give a rate, so `PlayerController` multiplies them by the look speed and by `deltaTime`. The mouse already gives a per-frame delta, so it is multiplied by the saved sensitivity only. `LookHold` is gone: on a desktop the cursor is locked and hidden while the safari runs, so the mouse always looks and there is no button to hold.

The on-screen controls are two sticks and the Photo button. The sticks add a virtual gamepad while the mouse is still in use, and `PlayerInput` was switching control schemes on its own and hiding the other bindings, so the sticks dropped out. `PlayerController.Start` turns that switching off and clears the binding mask. Keyboard, mouse and the on-screen gamepad then drive the same action map together.

Without this there is no movement, no photos and no run state at all.

### Collision

`Assets/Scripts/PlayerController.cs` moves the player with a `CharacterController`, so walking is collision based and the player cannot leave the ground. `Assets/Scripts/Animal.cs`, `Assets/Scripts/ViewingZone.cs` and `Assets/Scripts/DangerZone.cs` handle the animals.

Each animal has a solid `BoxCollider` on the Animal layer, which blocks the player, and two sphere triggers, one for the viewing zone and one for the danger zone. `Animal` copies the two radii out of its data asset at load, so they are 15 m and 6 m for the lion, leopard and buffalo, and 18 m and 8 m for the elephant and rhino.

A trigger does not block anybody, it only sends events. Entering the viewing zone sets `InViewing`, shows Ranger Amani's hint, and makes the animal turn slowly to look at me. Entering the danger zone shows the animal's own warning line, plays its call, makes it do a short bluff charge, flashes the screen red, shakes the camera, and books a breach at most once every 3 seconds. Walking into a solid body plays the bump sound and shows a toast that names what blocked the player. Hits on the floor are ignored, otherwise the ground would bump the player every frame.

Without collision the player walks through the animals, there are no zone events, no disturbances and no photo window.

### Raycasting

`Assets/Scripts/AnimalScanner.cs` does the photography, and it casts two rays every frame from the middle of the screen.

The first ray is the crosshair ray. `AnimalScanner.Update` casts it every frame while the safari runs, on the Animal layer only and with triggers ignored. It scores nothing. It only colours the crosshair: green with the animal's name when the animal is in its own viewing zone, amber with "Get closer" when the animal is outside it, and plain white when the ray hits nothing. The player can see what the photo will do before pressing the button.

The second ray is the photo ray, on the same line, cast once when Interact is pressed, up to 20 m. A miss shows "Nothing in frame. Aim at an animal." A hit on an animal outside its ranger's circle shows "Too far for a good photo. Step inside the ranger's circle." A hit on an animal inside its circle is the only way to earn a stamp, and only `AnimalScanner` ever calls `GameManager.AddStamp`. A photo of an animal that is already in the passport awards nothing, it plays the shutter and the call again and reopens the card.

`Interact` also closes an open fact card, so on a desktop the cursor never has to leave the game. A mouse press is ignored when the cursor is not locked, because that press was a click on the UI.

Without the raycast there are no photos, no stamps and no way to win.

### Line Renderer

`Assets/Scripts/SafariGuideLine.cs` draws the trail line on a `LineRenderer` in the scene. It has two points: the player, lifted a little off the ground, and the nearest animal that still needs a photo. It updates in `LateUpdate` so it follows the player, and it picks a new target every time the game state changes. When the passport is full, or on the start screen, it hides itself. The lion stands 50 m from the spawn so the line runs straight ahead at the start of a run.

`Assets/Scripts/ZoneRing.cs` is the second use of the same component. Each animal gets a `ZoneRing`, which builds two flat circles on the ground in `Start`: 64 points each, 0.12 m wide, 0.05 m above the ground, with a white loop at the viewing radius and a red loop at the danger radius, both from the same `AnimalData` radii the triggers use. The circles fade in over the last 35 m as I walk towards them, so a distant animal does not clutter the savanna, and the white circle turns gold once the animal is in the passport. Both use a transparent unlit material, `Assets/Materials/M_ZoneRing.mat`, so the colour gradient can fade.

Without the line renderer the player has no idea where to look first, and without the circles the two radii are invisible numbers.

### Pause menu

`Assets/Scripts/PauseMenu.cs` listens to `Player/Pause` and to the II button on the HUD. It opens only while a safari is running and never over the fact card or the end screen. Pausing sets `GameManager.IsPaused`, `Time.timeScale = 0` and `AudioListener.pause = true`, frees the cursor and shows the panel. Resume reverses all of it. Restart Safari is exactly what Replay does. Main Menu keeps the save, so Continue Safari is still there when the player comes back. The mouse sensitivity slider writes `MouseSensitivity` to PlayerPrefs, and `PlayerController` reads the same key at startup, so the setting survives a restart.

On a desktop the browser drops pointer lock when Escape is pressed. `PlayerController` notices a locked cursor that has gone free while the safari is running and opens the pause menu itself, so the player is never left with no control.

Without the pause menu there is no way to change the mouse sensitivity, restart a run or go back to the start screen once a safari has begun.

## 5. Additional Features

### Audio

There are eight clips in `Assets/Audio`. A savanna ambience loops on the Ambience object. `PlayerController` plays a short bump sound when the player walks into something solid. `AnimalScanner` plays the shutter click and then the animal call on every photo, not only the first one, so the animal answers each time. `DangerZone` plays the animal call from its own position when the player is too close. The shutter click is self-made with ffmpeg from filtered noise, so there is no licence to track.

### Particles

Two `ParticleSystem` objects, one for dust at the player's feet and one for fireflies over the reserve. No script drives them, all the settings are in the scene. Both are kept small, under 100 particles, so a phone can handle them.

### Animal reactions

`Assets/Scripts/Animal.cs` does two things the player can see. While I am inside the viewing zone and the animal is not startled, it turns its yaw towards me at about 25 degrees a second, so the animal looks like it has noticed me. `Animal.Startle` is a short bluff charge: a 1.2 m lunge over 0.15 s, a pause of 0.3 s, and a walk back over 0.6 s, with a clamp that stops the lunge two metres short of the player so it can never reach them.

### Photo and danger feedback

A photo fires a white full screen image from alpha 0.7 down to 0 over about 0.25 s, and a disturbance fires a red full screen image over about 0.6 s. Both are `Image` objects in the HUD with `raycastTarget` off, and both fade in `UIManager.Update` instead of a coroutine, so a hidden flash costs nothing. A disturbance also shakes the camera: `PlayerController.Shake(0.3f)` puts a small random offset on the camera local position for a third of a second and then puts it back where it was.

### Saving

`Assets/Scripts/SaveSystem.cs` writes the run as JSON in `PlayerPrefs` using `JsonUtility`. WebGL has no file system, and `PlayerPrefs` works there. `GameManager` saves after every stamp and every disturbance. Continue reads the save back and puts the run in the same state. New Safari, Replay and Restart Safari clear it. I use `PlayerPrefs` and `JsonUtility` because they are the simplest saving that works on both of my platforms.

## 6. Project Structure

```
Assets/
  Audio/            eight clips: one ambience, five calls, one bump, one shutter
  Editor/           import settings and the build script
  Images/           the title image
  Input/            InputSystem_Actions.inputactions
  Materials/        the ground, the animal, the guide line, the zone ring and the scenery materials
  Models/
    Animals/        the five animal models and their textures
    Generated/      the merged ground, hills and track meshes
    Scenery/        the Kenney trees, rocks and ground cover
  Prefabs/          PF_Animal and one variant per species
  Scenes/           Safari_Main.unity
  ScriptableObjects/ one asset per animal, holding its fact, its safe viewing tip, its warning line and its radii
  Scripts/          the twelve gameplay scripts below
  ThirdParty/       the licence files for the models, the scenery and the audio
```

`PF_Animal` is the base prefab. Its children are Body, which holds the model and the solid collider, the two zone triggers and the guide point. `ZoneRing` is on the root, and the five species variants inherit it. Each variant overrides the model, the scale and the data asset.

| Script | What it does | Lines |
| --- | --- | --- |
| `Assets/Scripts/UIManager.cs` | Every screen, the HUD, the crosshair, the flashes and the fact card | 560 |
| `Assets/Scripts/PlayerController.cs` | Walking, always-on mouse look, gravity, bump feedback, shake, spawn reset | 296 |
| `Assets/Scripts/GameManager.cs` | Stamps, Respect, disturbances, win, pause, save and restore | 189 |
| `Assets/Scripts/AnimalScanner.cs` | The crosshair ray, the photo ray, the stamp, the shutter and the fact card | 171 |
| `Assets/Scripts/Animal.cs` | Per-animal state, zone radii, turning towards the player, the bluff charge | 159 |
| `Assets/Scripts/PauseMenu.cs` | The pause panel, the pause button and the mouse sensitivity | 162 |
| `Assets/Scripts/ZoneRing.cs` | The white ranger's circle and the red circle on the ground | 97 |
| `Assets/Scripts/SafariGuideLine.cs` | The trail line to the nearest animal that still needs a photo | 96 |
| `Assets/Scripts/DangerZone.cs` | The trigger that warns, startles the animal and books disturbances | 87 |
| `Assets/Scripts/SaveSystem.cs` | Saving, loading and clearing the run | 61 |
| `Assets/Scripts/ViewingZone.cs` | The trigger that allows a photo and shows the ranger's hint | 56 |
| `Assets/Scripts/AnimalData.cs` | The data asset: fact, safe viewing tip, warning line, radii, call clip | 34 |

That is 1,968 lines in total. `Assets/Editor` holds three more: the two import scripts for the models and the scenery, and `Assets/Editor/BuildScript.cs` for the two builds. I used editor scripts to lay out the scene and the prefabs while I built them, and I finished the scene by hand in the Editor afterwards.

## 7. Build Information

**WebGL.** About 61 MB, 1280 x 720, in `Builds/WebGL`. Compression is switched off, because GitHub Pages will not send the header a compressed build needs. To run it, open the link at the top of this file, then press New Safari.

**Android.** An APK, not an app bundle, built with IL2CPP for ARM64, landscape only, about 36 MB, in `Builds/Android/BigFiveHeritageSafari.apk`. I built it but I have not tested it on a phone yet. To run it, copy the APK to the phone, allow installs from that file, and open it. It uses the two sticks, the Photo button and the II pause button.

Both builds use the Mobile quality level, which turns off real-time shadows. The animals use blob shadows instead. Both builds use the one scene, `Assets/Scenes/Safari_Main.unity`.

**Run it in the Editor.** Install Unity 6000.3.23f1 from Unity Hub, add this folder, open `Assets/Scenes/Safari_Main.unity` and press Play. For an APK you also need Android Build Support with the SDK, NDK and OpenJDK.

**Build it again.** Close the Editor first, because Unity locks the project while it is open. From the Editor menu use Build > Build WebGL or Build > Build Android APK. From a command prompt:

```
Unity.exe -batchmode -quit -nographics -projectPath <project path> -executeMethod BuildScript.BuildWebGL -logFile <log path>
```

Swap `BuildScript.BuildWebGL` for `BuildScript.BuildAndroid` for the APK. Each build prints one line that starts with `[Build]` with the result, the size and the error count.

## 8. Reflection

**Problems I hit and fixed.** The full list is in my bug notes, `misc/BUGS.md`. The ones I remember best:

- The fact card came up empty. My long text fields had `[TextArea]` but no `[SerializeField]`, so Unity never saved them into the assets and the card had nothing to show.
- The animals all faced away from me. I had looked at them through cameras that were on the far side, so I turned every model to point at the approach instead.
- Most of the trees and rocks were placed off the map, because my placement helper mixed local and world space.
- The end screen opened behind the fifth fact card. Now the card shows first and the end screen comes up when I close it.
- The on-screen sticks kept disappearing. PlayerInput was switching control schemes on its own and hiding the other device's bindings.
- The circles on the ground were invisible at first, because the guide line material is opaque and the fade in the colour gradient did nothing. The rings needed their own transparent unlit material.

**WebGL and Android.** GitHub Pages will not serve a compressed build, so I turned compression off. A particle system logged an error every frame and filled the browser console with it, because only the y velocity curve was set. The sticks showed up in desktop browsers as well, because a desktop browser reports a touchscreen, so now they only show on a real mobile platform. On the Android side the build settings are all set, but the APK has still never run on a phone, and that is the first thing I should do next.

**The hardest part.** The input setup. Keyboard, mouse, gamepad and the on-screen sticks all have to drive one action map, and PlayerInput fights that by switching schemes. Working out that I had to stop it switching, and clear the binding mask, was the part that took me longest. Desktop mouse look was the second hard part, because the cursor lock, the UI clicks and the fact card all have to agree about when a click is a photo.

**Next steps.** A real leopard model instead of the jaguar. Walking animals, since the models are static meshes now. The real 20 metre distance from the Kenya Wildlife Service instead of my scaled-down zones. A slow jog, which I left out because it needed more code than it was worth. Swahili names and text. And a full run on a phone.

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

**Audio.** All eight clips are listed with their sources and processing in `Assets/ThirdParty/AUDIO-LICENCES.txt`.

| File | Sound | Author | Licence |
| --- | --- | --- | --- |
| `Assets/Audio/SFX_Lion.ogg` | Lion roars 1.wav | eardeer | CC BY 4.0 |
| `Assets/Audio/SFX_Leopard.ogg` | Jaguar saw | About Zoos | CC BY 4.0 |
| `Assets/Audio/SFX_Elephant.ogg` | Elephant voice, trumpeting | தகவலுழவன் (Info-farmer) | CC0 1.0 |
| `Assets/Audio/SFX_Rhino.ogg` | white Rhino fighting | geoffvt | CC0 1.0 |
| `Assets/Audio/SFX_Buffalo.ogg` | Buffles-Grognement+amb oiseaux | roubignolle (G de Courtivron) | CC BY 4.0 |
| `Assets/Audio/AMB_Savanna.ogg` | Birds and wind at Ngutuni Lodge | dkaufman | CC0 1.0 |
| `Assets/Audio/SFX_Bump.ogg` | Light Body thud (on clothing) | JonasTisell | CC0 1.0 |
| `Assets/Audio/SFX_Shutter.wav` | self-made two click shutter, made with ffmpeg | me | CC0 1.0 |

Attribution for the three clips that need it:

> "Lion roars 1.wav" by eardeer, https://freesound.org/people/eardeer/sounds/458903/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Jaguar saw" by About Zoos, https://commons.wikimedia.org/wiki/File:Jaguar_saw.flac, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Buffles-Grognement+amb oiseaux" by roubignolle (G de Courtivron), https://freesound.org/people/roubignolle/sounds/35130/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

Three of the calls are substitutions. The leopard plays the jaguar call, because no leopard call under CC0 or CC BY was found. The elephant call does not name its species and is probably an Asian elephant. The rhino call is a white rhinoceros, used for my black rhino model. The buffalo call still has some birdsong in it, which the filter only reduces. The shutter click has no source page at all, because I generated it from ffmpeg noise on this machine.

**Fact card sources.** Elephant status and decline, IUCN press release, 25 March 2021. Black rhino figures, Kenya Wildlife Service figures as reported by WWF-UK, 26 February 2024. Lion figures, Kenya Wildlife Service figures as reported by Anadolu Agency, 16 January 2024. Leopard and buffalo facts, the Wikipedia articles "Leopard" and "African buffalo".

**Ranger's tip sources.** One line per species, each written as general safe viewing guidance rather than a claim about one animal.

- Lion: Kenya Wildlife Service safety guidelines, "Remain in your vehicle unless in designated areas" and "Move slowly and quietly", https://kws.go.ke/park/nairobi-national-park
- Leopard: SafariLingo safari guide, "a good view is never worth crowding a predator", "prevent a leopard from reaching a tree" and "avoid surrounding animals in a tight ring of vehicles", https://safarilingo.com/en/guide/what-to-do-if-you-see-a-predator-kill
- Elephant: Discover Wildlife (BBC Wildlife Magazine), "holding its ears out for a prolonged period to show its size", "your viewing position should not separate elephant family members" and "if you see any of these signs, back away slowly", https://www.discoverwildlife.com/animal-facts/mammals/stay-safe-around-elephants
- Black rhino: Etosha National Park, "both species of rhino have very poor eyesight" and "don't make any loud noises or sudden movements", https://www.etoshanationalpark.org/news/how-not-to-get-charged-by-a-rhino
- Buffalo: National Geographic, "one of the big five ... the most dangerous animals to hunt in Africa" and "grumpy and unpredictable", https://www.nationalgeographic.com/animals/mammals/facts/african-buffalo

**Sources.**

1. World Travel & Tourism Council (WTTC), news release "Kenya Boosts Africa's Tourism Growth, Emerging as a Leading Sustainable Tourism Powerhouse", 15 June 2026, https://wttc.org/news/kenya-boosts-africas-tourism-growth-emerging-as-a-leading-sustainable-tourism-powerhouse
2. Kenya Wildlife Service, safety guidelines for visitors, https://kws.go.ke/park/nairobi-national-park

