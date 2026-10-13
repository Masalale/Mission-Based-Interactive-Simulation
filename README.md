# Big Five Heritage Safari

[WebGL build](TODO_WEBGL_URL) | [Android APK](TODO_APK_URL) | [Demo video](TODO_VIDEO_URL)

A first-person safari through a Kenyan savanna reserve, built in Unity for the Introduction to Unity module (Course 3152).

- Author: Masalale
- Repository: https://github.com/Masalale/Mission-Based-Interactive-Simulation
- Unity 6000.3.23f1, Universal Render Pipeline (URP) 17.3.0, Input System 1.20.1

The WebGL and Android builds are not finished yet, so the links above are placeholders.

## 1. GCGO Statement

- **Grand Challenge:** Tourism
- **Global Opportunity:** Job creation

**Mission statement:** Wildlife tourism promotes conservation and creates ranger, guide, lodge, and craft jobs. This simulation teaches safe viewing while showing the careers each animal supports.

## 2. Problem Context

**The problem.** Wildlife tourism only lasts if visitors keep a safe distance from animals and if the communities around the reserves share in the income. When visitors crowd or chase animals, the animals are harmed and the sightings get worse. Visible jobs such as trackers, guides, rangers and drivers keep conservancies funded.

**Why it matters.** The World Travel & Tourism Council (WTTC) reports that travel and tourism "supported 1.8 million jobs, equivalent to 8.3% of total employment" in Kenya in 2025, and contributed 9.3% of national GDP. [1]

Safe distance matters for the same reason. The Kenya Wildlife Service visitor guide, as quoted by The Standard on 24 December 2025, tells visitors to keep a minimum distance of 20 metres when viewing wildlife. [2]

The simulation does not use that rule. Its viewing zone is 15 to 18 m and its danger zone is 6 to 8 m. These radii are scaled down for a small play area. They teach the idea of a safe distance and a too-close warning. They are not the real Kenya Wildlife Service distance.

**How it connects to the GCGO.** The Tourism challenge is about wildlife viewing that protects the animals it depends on. The Job creation opportunity is about the careers that come from that viewing. Each fact card names a job the animal supports, and the Jobs Supported counter goes up as the player collects stamps. The danger zone shows the other half of the idea: keeping your distance keeps the product alive.

Sources for this section are listed in section 9.

## 3. Simulation Overview

**What it does.** The player walks a savanna reserve in first person and looks for the Big Five: lion, leopard, elephant, rhino and buffalo. Each animal has a viewing zone. Stand inside the zone, aim at the animal and press Scan. The fact card shows one fact and one job the animal supports. The first scan of each animal gives a passport stamp and +1 Jobs Supported. Entering an animal's danger zone shows a warning and costs 10 points.

**Target users.** Students learning about wildlife tourism, young job seekers who want to know which conservation careers exist, and would-be tourists who want to know how to watch wildlife safely.

**Key interactions.**

- Walk and look around the reserve.
- Follow the guide line to the nearest animal that has no stamp yet.
- Enter a viewing zone, aim at the animal and press Scan.
- Read the fact card and the job it supports.
- Keep out of the danger zone.

**Controls.**

| Action | Type | Desktop | Mobile (touch) |
| --- | --- | --- | --- |
| Move | Vector2 | WASD | Left on-screen stick |
| Look | Vector2 | Arrow keys | Right on-screen stick |
| MouseLook | Vector2 | Mouse movement, only while LookHold is held | None |
| LookHold | Button | Right mouse button | None |
| Interact | Button | E key, or click the Scan button | On-screen Scan button |

A gamepad uses the left stick for Move, the right stick for Look and the south button for Interact.

**How to win.** Stamp all five animals (5 of 5 stamps). Each animal is stamped when the player scans it from inside its viewing zone. There is no fail state. The end screen shows the stamps, score, breaches and jobs, and it offers Replay.

**How the score works.** The score starts at 100 and never drops below 0. Each breach of a danger zone costs 10 points. An animal can book a breach at most once every 3 seconds. Each first scan adds 1 to Jobs Supported. A repeat scan only reopens the fact card. The top score is 100, so a perfect run has zero breaches.

## 4. Unity Mechanics Implemented

Each mechanic names its scripts, what it does in this game, and what breaks if it is removed. That consequence is the deletion test from the build spec.

### 4.1 UI

**Scripts:** `UIManager.cs`. The Canvas is built by `SafariUIBuilder.cs` in the Editor and saved in `Safari_Main.unity`.

**What it does:**

- A start screen with Continue and New Safari buttons. Continue is only enabled when a save exists.
- A HUD with a crosshair, a five-slot passport, a Jobs Supported counter, a score, a stamps counter, a hint line, a warning line, and short toast messages.
- A fact card with the animal name, habitat, status, the fact, the job role and the job description, and a close button.
- An end screen with a summary and a Replay button.

`UIManager` does not poll. It subscribes to `GameManager.OnStateChanged` and `GameManager.OnWin` in `OnEnable`, and it unsubscribes in `OnDisable`. Every runtime change to the screen text and panels goes through `UIManager`.

**Delete it:** no objective, no feedback after a scan, no start or end screen, no win.

### 4.2 Scripting and the New Input System

**Scripts:** all ten scripts in `Assets/Scripts`. Input is read only through the New Input System (`UnityEngine.InputSystem`). No script in the project calls the legacy `Input` class.

**Input setup.**

- One input asset: `Assets/Input/InputSystem_Actions.inputactions`.
- One action map, `Player`, with five actions: `Move` (Value), `Look` (Value), `MouseLook` (Value), `LookHold` (Button) and `Interact` (Button).
- The bindings are:

| Action | Bindings |
| --- | --- |
| Move | WASD (2D Vector composite), `<Gamepad>/leftStick` |
| Look | Arrow keys (2D Vector composite), `<Gamepad>/rightStick` |
| MouseLook | `<Mouse>/delta` |
| LookHold | `<Mouse>/rightButton` |
| Interact | `<Keyboard>/e`, `<Gamepad>/buttonSouth` |

- The `Player` object has a `PlayerInput` component. Its Actions field is the asset above and its Default Action Map is `Player`. Notifications are sent as C# events. `AnimalScanner` subscribes to `Interact.performed` in code.
- The `EventSystem` uses `InputSystemUIInputModule`. No `StandaloneInputModule` is in the scene, and the Validate step checks for one.
- Project setting: Active Input Handling is set to the Input System Package only (`activeInputHandler: 1`).
- On-screen controls: an `OnScreenStick` bound to `<Gamepad>/leftStick` (Move), an `OnScreenStick` bound to `<Gamepad>/rightStick` (Look), and an `OnScreenButton` bound to `<Gamepad>/buttonSouth` (Interact). The Scan button stays on the HUD. `UIManager` shows the two sticks only when `Touchscreen.current` is not null.

**Look and move rules.** `PlayerController.Look` multiplies stick and arrow look by `lookSpeed` (120 degrees per second) and by `Time.deltaTime`. Mouse delta is multiplied by `mouseSensitivity` (0.1) and not by `deltaTime`. Yaw turns the player body. Pitch is clamped to -80 to 80 degrees and turns only the camera. No script locks the cursor.

**The control-scheme fix.** `PlayerInput` switches control schemes automatically. The on-screen sticks add a virtual gamepad while the mouse is also in use. The automatic switch can then mask the bindings of the other device, and the sticks drop out. `PlayerController.Start` turns automatic switching off (`neverAutoSwitchControlSchemes = true`) and clears `bindingMask` and `devices` on the action asset. Keyboard, mouse and the on-screen gamepad then drive the one action map together. `PlayerController` has `[DefaultExecutionOrder(-50)]`, so its `Start` runs before `UIManager.Start`, which disables the player until a run begins. I found this in code review before any device testing.

**Delete it:** no movement, no scanning, no state.

### 4.3 Collision

**Scripts:** `PlayerController.cs`, `Animal.cs`, `ViewingZone.cs`, `DangerZone.cs`.

- **Player body.** The player has a `CharacterController` (height 1.8 m, radius 0.4 m, centre at 0.9 m). It has no Rigidbody.
- **Solid animal bodies.** Each animal's body has a `BoxCollider` on the Animal layer (layer 6). The builder sizes it to 85% of the model bounds. A solid collider blocks the `CharacterController`.
- **Bump feedback.** `PlayerController.OnControllerColliderHit` ignores hits on the ground (`hit.normal.y > 0.5f`). For any other solid hit it plays the bump sound and raises `Bumped`. `UIManager` then shows a toast: "Blocked by the <species>" for an animal, or "Blocked by a rock or tree" for anything else. It allows one bump per second.
- **Trigger zones.** Each `PF_Animal` has two `SphereCollider` triggers, one for `ViewingZone` and one for `DangerZone`. `Animal.Awake` copies their radii from `AnimalData`.
  - `ViewingZone` sets `Animal.InViewing` on enter and exit, for objects tagged `Player`. It shows the hint "Aim at the <species> and press Scan".
  - `DangerZone` shows the warning "Too close, back away" on enter. It books a breach through `GameManager.RegisterBreach`, with a 3 second cooldown (`breachCooldown`) per animal. It hides the warning on exit.

A trigger does not block movement. It only fires events. The solid body blocks the player, and the trigger zones decide whether scanning is allowed and whether a breach counts.

**Delete it:** the player walks through animals, and there are no zone events, no breaches and no scan window.

### 4.4 Raycasting

**Script:** `AnimalScanner.cs`.

- On Interact, `OnInteract` builds a ray from the centre of the screen with `scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))`.
- It calls `Physics.Raycast(ray, out hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore)`. `maxDistance` is 20 m. `animalMask` is set in the scene to layer 6 (value 64). `QueryTriggerInteraction.Ignore` means the zone triggers never block the ray.
- A miss shows "Nothing in your sights. Aim at an animal." A hit on an animal that is not in its viewing zone shows "Move into the viewing zone first."
- A hit on an animal in its viewing zone is the only way to earn a stamp. On the first scan, `AnimalScanner` calls `GameManager.Instance.AddStamp(animal)`. That is the only call site in the project. It then plays the animal's call clip.
- A repeat scan does not award anything. It only reopens the fact card, which says "Already in your passport".

**Delete it:** no scans, no stamps, and the game cannot be won.

### 4.5 Line Renderer

**Script:** `SafariGuideLine.cs`, on an object with a `LineRenderer` (two world-space points, material `M_GuideLine`, URP Unlit).

- The start point is the player position plus 0.3 m up. The end point is the `GuidePosition` of the nearest unscanned animal, which is the base of its model, plus 0.3 m up.
- The line is updated in `LateUpdate`, so it follows the player every frame.
- It retargets on `GameManager.OnStampAdded` and `GameManager.OnStateChanged`. When every animal has a stamp, there is no target and the line is hidden. It is also hidden on the start screen.

**Delete it:** no guidance, so the hunt has no direction.

## 5. Additional Features

Three features beyond the module, all in the scene and the scripts.

### 5.1 Audio

- **Savanna ambience.** An `AudioSource` on the `Ambience` object (under `Systems`) plays `AMB_Savanna.ogg`. Loop is on, Play On Awake is on, and the volume is 0.35.
- **Animal call on first scan.** `AnimalScanner` calls `callSource.PlayOneShot(animal.Data.CallClip)` on a first scan only. The clip comes from the `callClip` field of each `SO_*` asset. The leopard has no call clip, so it plays nothing.
- **Bump sound.** `PlayerController` calls `bumpSource.PlayOneShot(bumpClip)` with `SFX_Bump.ogg`, once per bump cooldown.

The six clips are in `Assets/Audio`. Their licences are in section 9.

### 5.2 Particle systems

Two `ParticleSystem` objects, both built by `SafariSceneBuilder` with the `M_Particle` material. No script drives them. Their behaviour comes entirely from the settings stored in the scene.

- **Dust.** A child of the Player. A circle-shaped emitter, lifetime 3 to 5 s, maximum 60 particles.
- **Fireflies.** A child of `Systems`. A box-shaped emitter, lifetime 6 to 9 s, maximum 80 particles.

Each system is capped well under 100 particles, which keeps the cost low on phones.

### 5.3 Data persistence

**Scripts:** `SaveSystem.cs` (static, plus the `SaveData` class) and `GameManager.cs`.

- `SaveData` holds a list of scanned animal ids (`scannedIds`), the `score` and the `breaches`. `SaveSystem` writes it with `JsonUtility.ToJson` as a single string under the PlayerPrefs key `SafariSave`.
- `GameManager.AddStamp` and `GameManager.RegisterBreach` call `Save()` after every change. `SaveSystem.Save` then calls `PlayerPrefs.Save()`, so WebGL flushes the data.
- **Continue** calls `GameManager.Restore()`. It reads the save, marks the matching animals as scanned and sets the stamps, jobs, score and breaches directly. It does not call `AddStamp`, so no stamp is awarded twice and `OnStampAdded` is not raised. Jobs are not stored: they are set to the number of restored stamps. If the save shows 5 of 5, the end screen opens.
- **New Safari** and **Replay** call `GameManager.ResetRun()`, which clears the save.
- The Continue button is only interactable when `SaveSystem.HasSave` is true.

The project never uses file IO, because WebGL has no writable filesystem.

## 6. Project Structure

### Folder tree

```
Assets/
  Audio/              AMB_Savanna.ogg, SFX_Bump.ogg, SFX_Lion.ogg, SFX_Elephant.ogg, SFX_Rhino.ogg, SFX_Buffalo.ogg
  Editor/             Safari*.cs build tools, SceneryImportSettings.cs
  Images/             background_removed.png (title logo)
  Input/              InputSystem_Actions.inputactions
  Materials/          M_Ground, M_GuideLine, M_Leopard, M_Particle, M_Track
  Models/
    Animals/          SM_Lion.fbx, SM_Elephant.fbx, SM_Rhino.fbx, SM_Buffalo.fbx
    Scenery/          Kenney Nature Kit pieces, in sub-folders
  Prefabs/            PF_Animal, PF_Animal_Lion, PF_Animal_Leopard, PF_Animal_Elephant, PF_Animal_Rhino, PF_Animal_Buffalo
  Scenes/             Safari_Main.unity
  ScriptableObjects/  SO_Lion, SO_Leopard, SO_Elephant, SO_Rhino, SO_Buffalo
  Scripts/            ten gameplay scripts (table below)
  Settings/           URP render pipeline assets and volume profiles
  ThirdParty/         licence files for Grab3D, Kenney and the audio clips
```

### Script table

Line counts are from `wc -l` on 13 Oct 2026.

| File | Purpose | Lines |
| --- | --- | --- |
| `PlayerController.cs` | First-person move and look, gravity, bump feedback | 192 |
| `AnimalScanner.cs` | Interact: centre-screen raycast, stamps, call sound, fact card | 104 |
| `Animal.cs` | Per-animal runtime state, zone radii, static list of all animals | 96 |
| `AnimalData.cs` | ScriptableObject: fact, job, radii, call clip | 59 |
| `ViewingZone.cs` | Trigger that allows scanning and shows the hint | 54 |
| `DangerZone.cs` | Trigger that shows the warning and books breaches, with a cooldown | 53 |
| `SafariGuideLine.cs` | LineRenderer guide to the nearest unscanned animal | 108 |
| `GameManager.cs` | Run state (stamps, score, breaches, jobs), events, win, save and restore | 213 |
| `UIManager.cs` | Every screen, the HUD, the fact card; listens to GameManager | 413 |
| `SaveSystem.cs` | PlayerPrefs and JsonUtility save, load and clear | 77 |

Total: 1,369 lines.

### Prefabs

- **`PF_Animal`** (base). The root has `Animal.cs` and the `AnimalData` slot. Its children are `Body` (the model and a solid body collider on the Animal layer), `ViewingZone` (a `SphereCollider` trigger with `ViewingZone.cs`) and `DangerZone` (a `SphereCollider` trigger with `DangerZone.cs`).
- **Five variants:** `PF_Animal_Lion`, `PF_Animal_Leopard`, `PF_Animal_Elephant`, `PF_Animal_Rhino` and `PF_Animal_Buffalo`. Each one overrides the model, the scale and the `AnimalData`.
- The leopard variant uses the lion model at leopard scale, with the `M_Leopard` material. See section 9.

### ScriptableObjects

`SO_Lion`, `SO_Leopard`, `SO_Elephant`, `SO_Rhino` and `SO_Buffalo` are `AnimalData` assets. Each holds the species name, habitat, status, fact, job role, job description, the two zone radii and the call clip. The viewing and danger radii are 15 m and 6 m for lion, leopard and buffalo, and 18 m and 8 m for elephant and rhino.

### Editor tooling

The scene, prefabs and data assets are generated by editor scripts in `Assets/Editor`, so the scene can be rebuilt the same way each time. They are exposed in the Editor under the `Safari` menu:

- `Safari > 1 Project Setup` (`SafariProjectSetup`): layers, player settings, import settings and the URP assets.
- `Safari > 2 Build Assets` (`SafariAssetBuilder`): shared materials, the five `AnimalData` assets, `PF_Animal` and its five variants.
- `Safari > 3 Build Scene` (`SafariSceneBuilder`): rebuilds `Safari_Main` with a fixed random seed, so two runs give the same scatter.
- `Safari > 0 Build All` (`SafariBuilder.BuildAll`): runs the three steps above in order and saves.
- `Safari > 9 Validate` (`SafariBuilder.Validate`): audits the saved scene and prefabs, and prints one PASS or FAIL line per check.

`BuildAll` and `Validate` both end by calling `EditorApplication.Exit`, so they close the Editor when they finish. Run them from the command line, or save your work first.

## 7. Build Information

**Links**

| Build | Link |
| --- | --- |
| WebGL (desktop and phone browser) | TODO_WEBGL_URL |
| Android APK | TODO_APK_URL |
| Demo video | TODO_VIDEO_URL |

**Versions.** Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.1.

**Build settings in the project.**

- WebGL: compression format is Disabled and decompression fallback is off. GitHub Pages cannot send the `Content-Encoding` header that compressed builds need. Output folder: `Builds/WebGL`.
- Android: IL2CPP scripting backend, ARM64 only, landscape orientation (auto-rotate left and right, portrait off), built as an APK rather than an AAB. Output: `Builds/Android/BigFiveHeritageSafari.apk`.
- Build Settings contains one scene: `Assets/Scenes/Safari_Main.unity`.

**Run the project in the Editor.**

1. Install Unity 6000.3.23f1 with Unity Hub. For an APK build, also install Android Build Support (SDK, NDK and OpenJDK).
2. Add the project folder in Unity Hub and open it.
3. Open `Assets/Scenes/Safari_Main.unity` and press Play.
4. On the start screen, press New Safari. Press Continue if you have a saved run.

**Run on a desktop browser.** Open the WebGL link (TODO_WEBGL_URL). Move with WASD, hold the right mouse button to look, press E or click the Scan button.

**Run on an Android phone.** Install the APK from the GitHub Release (TODO_APK_URL) and allow installs from that source. The game runs in landscape. Use the two on-screen sticks to move and look, and the Scan button to scan. A phone browser can also open the WebGL link (TODO_WEBGL_URL). That path has not been tested yet.

**Build from the command line.** Close the Editor first, because Unity locks the project while it is open:

```
Unity.exe -batchmode -quit -nographics -projectPath <project path> -executeMethod Safari.EditorTools.SafariBuild.BuildWebGL -logFile <log path>
```

Use `BuildAndroid` in place of `BuildWebGL` for the APK. The same methods are available from the `Safari > 7 Build WebGL` and `Safari > 8 Build Android` menu items.

## 8. Reflection

**Real bugs.** The full entries, with date, symptom, cause and fix, are in `BUGS.md`. In short:

- The fact card showed no fact and no job description. The two fields had no `[SerializeField]`, so Unity never saved them. The builder now reads them back and fails if they are empty.
- The bump sound fired every frame, because the ground counts as a hit. The controller now ignores upward-facing normals and has a one-second cooldown.
- The on-screen sticks could drop out, because automatic control-scheme switching hid the other device's bindings. I found this in code review before device testing.
- An Editor build crashed with a `MissingReferenceException`, because a material was created while a prefab was open. Shared materials are now created first.
- All scenery was reported missing, because the folder name was joined twice in the path.
- Two tools set the animal `Bake Axis Conversion` flag in opposite directions. Only one owner is left.
- Validation skipped array fields, because it looked properties up by `name` instead of `propertyPath`.
- A design note: a method named `Reset` would have been run by Unity as an editor message, so it was renamed `ResetState`.

**Hardest feature.** For me the hardest part was the editor build pipeline. Five of the eight `BUGS.md` entries came from the scene, asset and import code, not from gameplay. The fixes taught me to give each setting one owner, to read values back from an asset after writing it, and to walk the real property paths when validating.

**WebGL and Android challenges.** TODO_REFLECTION_PLATFORM

**Next steps.**

- Animated, rigged animals with walk cycles. The current models are static meshes with no rigs.
- A real leopard model, to replace the lion re-skin.
- More species, with Swahili names and text.
- Real distances that follow the Kenya Wildlife Service guidance, in place of the scaled-down radii.

## 9. Credits and Licences

### Third-party assets

| Asset | Source | Licence | Notes |
| --- | --- | --- | --- |
| Lion, elephant, rhino and buffalo meshes (`SM_Lion`, `SM_Elephant`, `SM_Rhino`, `SM_Buffalo`) | Grab3D (grab3d.com) | CC0 1.0 | The buffalo is Grab3D's Low Poly Bull, standing in for a Cape buffalo. |
| Leopard | Re-skin of the Grab3D lion mesh (`SM_Lion`) at leopard scale, with `M_Leopard` | CC0 1.0 (Grab3D) | Not a leopard model. Coat texture `Assets/Models/Animals/Textures/T_LeopardCoat.png` is a procedurally generated spotted texture. |
| Scenery: trees, rocks and ground cover | Kenney Nature Kit 2.1 (kenney.nl) | CC0 1.0 | Credit to Kenney is appreciated but not required. |
| Title logo (`Assets/Images/background_removed.png`) | TODO_LOGO_CREDIT | Not yet recorded | |

### Audio

The six clips are listed in `Assets/ThirdParty/AUDIO-LICENCES.txt`. The details are below.

| File | Sound | Author and source | Licence |
| --- | --- | --- | --- |
| `SFX_Lion.ogg` | Lion roar ("the roaring of a lion in captivity") | User Info-farmer (தகவலுழவன்), Wikimedia Commons, 2010 (https://commons.wikimedia.org/wiki/File:Lion_raring-sound1TamilNadu178.ogg) | Public domain, PD-self. No attribution required. |
| `SFX_Elephant.ogg` | "Elephant sound", a field recording of an elephant in a zoo | ikbenraar, Freesound (https://freesound.org/people/ikbenraar/sounds/819668/) | CC0 1.0. No attribution required. |
| `SFX_Buffalo.ogg` | "Buffles-Grognement+amb oiseaux" (some buffaloes in Tanzania with birds in the background) | roubignolle (G de Courtivron), Freesound (https://freesound.org/people/roubignolle/sounds/35130/) | CC BY 4.0. Attribution required, see below. |
| `SFX_Rhino.ogg` | "Indian rhinoceros (Rhinoceros unicornis)", a recording of an Indian rhinoceros | Breviceps, Freesound (https://freesound.org/people/Breviceps/sounds/535810/) | CC0 1.0. No attribution required. |
| `AMB_Savanna.ogg` | "Birds and wind at Ngutuni Lodge", near Voi, Kenya | dkaufman, Freesound (https://freesound.org/people/dkaufman/sounds/743640/) | CC0 1.0. No attribution required. Edited: trimmed, loudness-normalised and re-encoded. |
| `SFX_Bump.ogg` | "Light Body thud (on clothing)" | JonasTisell, Freesound (https://freesound.org/people/JonasTisell/sounds/496187/) | CC0 1.0. No attribution required. Edited: trimmed and re-encoded. |

**Required attribution (CC BY 4.0), for `SFX_Buffalo.ogg`:**

> "Buffles-Grognement+amb oiseaux" by roubignolle (G de Courtivron), https://freesound.org/people/roubignolle/sounds/35130/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

**Substitution note.** No black rhinoceros recording under CC0 or CC BY was found on Wikimedia Commons, Freesound or OpenGameArt. `SFX_Rhino.ogg` is an Indian rhinoceros recording, used as a stand-in for the black rhino.

**No call clip for the leopard.** The leopard has no call clip, so it has no audio file.

