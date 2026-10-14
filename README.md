# Big Five Heritage Safari

[WebGL build](TODO_WEBGL_URL) | [Android APK](TODO_APK_URL) | [Demo video](TODO_VIDEO_URL)

A first-person safari through a Kenyan savanna reserve, built in Unity for the Introduction to Unity module (Course 3152).

- Author: Masalale
- Repository: https://github.com/Masalale/Mission-Based-Interactive-Simulation
- Unity 6000.3.23f1, Universal Render Pipeline (URP) 17.3.0, Input System 1.20.1

The WebGL build has been run locally in a desktop browser, but its public link is still a placeholder. No Android build has been made yet, so the APK link is a placeholder too.

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

Sources for this section are listed in section 10.

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

The on-screen sticks appear only on mobile platforms. The Scan button is on the HUD on every platform.

**How to win.** Stamp all five animals (5 of 5 stamps). Each animal is stamped when the player scans it from inside its viewing zone. There is no fail state. The end screen opens after the player closes the last fact card. It shows the stamps, score, breaches and jobs, and it offers Replay.

**How the score works.** The score starts at 100 and never drops below 0. Each breach of a danger zone costs 10 points. An animal can book a breach at most once every 3 seconds. Each first scan adds 1 to Jobs Supported. A repeat scan only reopens the fact card. The top score is 100, so a perfect run has zero breaches.

## 4. Unity Mechanics Implemented

Each mechanic names its scripts, what it does in this game, and what breaks if it is removed. That consequence is the deletion test from the build spec.

### 4.1 UI

**Scripts:** `UIManager.cs`. The Canvas and its panels are in `Safari_Main.unity`. They were first laid out by editor tooling that is no longer in the project (see section 6).

**What it does:**

- A start screen with Continue and New Safari buttons. Continue is only enabled when a save exists.
- A HUD with a crosshair, a five-slot passport, a Jobs Supported counter, a score, a stamps counter, a hint line, a warning line, and short toast messages. The HUD is built inactive and is shown when a run starts.
- A fact card with the animal name, habitat, status, the fact, the job role and the job description, and a close button.
- An end screen with a summary and a Replay button. It opens when the player closes the last fact card of a won run.

`UIManager` does not poll for game state. It subscribes to `GameManager.OnStateChanged` and `GameManager.OnWin` in `OnEnable`, and it unsubscribes in `OnDisable`. Every runtime change to the screen text and panels goes through `UIManager`.

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
- The `EventSystem` uses `InputSystemUIInputModule`. No `StandaloneInputModule` is in the scene.
- Project setting: Active Input Handling is set to the Input System Package only (`activeInputHandler: 1`).
- On-screen controls: an `OnScreenStick` bound to `<Gamepad>/leftStick` (Move), an `OnScreenStick` bound to `<Gamepad>/rightStick` (Look), and an `OnScreenButton` bound to `<Gamepad>/buttonSouth` (Interact). The two sticks sit in their own `MobileControls` panel. `UIManager` shows that panel only when `Application.isMobilePlatform` is true, so a desktop browser does not show the sticks. The Scan button stays on the HUD on desktop and mobile.

**Look and move rules.** `PlayerController.Look` multiplies stick and arrow look by `lookSpeed` (120 degrees per second) and by `Time.deltaTime`. Mouse delta is multiplied by `mouseSensitivity` (0.1) and not by `deltaTime`. Yaw turns the player body. Pitch is clamped to -80 to 80 degrees and turns only the camera. No script locks the cursor.

**The control-scheme fix.** `PlayerInput` switches control schemes automatically. The on-screen sticks add a virtual gamepad while the mouse is also in use. The automatic switch can then mask the bindings of the other device, and the sticks drop out. `PlayerController.Start` turns automatic switching off (`neverAutoSwitchControlSchemes = true`) and clears `bindingMask` and `devices` on the action asset. Keyboard, mouse and the on-screen gamepad then drive the one action map together. `PlayerController` has `[DefaultExecutionOrder(-50)]`, so its `Start` runs before `UIManager.Start`, which disables the player until a run begins. I found this in code review before any device testing.

**Delete it:** no movement, no scanning, no state.

### 4.3 Collision

**Scripts:** `PlayerController.cs`, `Animal.cs`, `ViewingZone.cs`, `DangerZone.cs`.

- **Player body.** The player has a `CharacterController` (height 1.8 m, radius 0.4 m, centre at 0.9 m). It has no Rigidbody.
- **Solid animal bodies.** Each animal's body has a `BoxCollider` on the Animal layer (layer 6). The collider is sized to 85% of the model bounds. A solid collider blocks the `CharacterController`.
- **Bump feedback.** `PlayerController.OnControllerColliderHit` ignores hits on the ground (`hit.normal.y > 0.5f`). For any other solid hit it plays the bump sound and raises `Bumped`. `UIManager` then shows a toast: "Blocked by the <species>" for an animal, or "Blocked by a rock or tree" for anything else. It allows one bump per second.
- **Trigger zones.** Each `PF_Animal` has two `SphereCollider` triggers, one for `ViewingZone` and one for `DangerZone`. `Animal.Awake` copies their radii from `AnimalData`.
  - `ViewingZone` sets `Animal.InViewing` on enter and exit, for objects tagged `Player`. It shows the hint "Aim at the <species> and press Scan".
  - `DangerZone` shows the warning "Too close, back away" on enter. It books a breach through `GameManager.RegisterBreach`, with a 3 second cooldown (`breachCooldown`) per animal. It hides the warning on exit.

A trigger does not block movement. It only fires events. The solid body blocks the player, and the trigger zones decide whether scanning is allowed and whether a breach counts.

**Delete it:** the player walks through animals, and there are no zone events, no breaches and no scan window.

### 4.4 Raycasting

**Script:** `AnimalScanner.cs`.

- On Interact, `OnInteract` builds a ray from the centre of the screen with `scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))`.
- It calls `Physics.Raycast(ray, out hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore)`. `maxDistance` is 20 m. `animalMask` is set to the Animal layer (layer 6, value 64) in the scene. `QueryTriggerInteraction.Ignore` means the zone triggers never block the ray.
- A miss shows "Nothing in your sights. Aim at an animal." A hit on an animal that is not in its viewing zone shows "Move into the viewing zone first."
- A hit on an animal in its viewing zone is the only way to earn a stamp. On the first scan, `AnimalScanner` calls `GameManager.Instance.AddStamp(animal)`. That is the only call site in the project. Every valid scan then plays the animal's call clip.
- A repeat scan does not award anything. It plays the call again and reopens the fact card, which says "Already in your passport".

**Delete it:** no scans, no stamps, and the game cannot be won.

### 4.5 Line Renderer

**Script:** `SafariGuideLine.cs`, on an object with a `LineRenderer` (two world-space points, material `M_GuideLine`, URP Unlit).

- The start point is the player position plus 0.3 m up. The end point is the `GuidePosition` of the nearest unscanned animal, which is the base of its model, plus 0.3 m up.
- At the start of a run the line points toward the lion, which is the nearest animal at spawn.
- The line is updated in `LateUpdate`, so it follows the player every frame.
- It retargets on `GameManager.OnStampAdded` and `GameManager.OnStateChanged`. When every animal has a stamp, there is no target and the line is hidden. It is also hidden on the start screen.

**Delete it:** no guidance, so the hunt has no direction.

## 5. Additional Features

Three features beyond the module, all in the scene and the scripts.

### 5.1 Audio

There are seven clips in `Assets/Audio`: a savanna ambience, a bump sound, and five animal calls (lion, leopard, elephant, rhino and buffalo).

- **Savanna ambience.** An `AudioSource` on the `Ambience` object (under `Systems`) plays `AMB_Savanna.ogg`. Loop is on, Play On Awake is on, and the volume is 0.35.
- **Animal call on every scan.** `AnimalScanner` calls `callSource.PlayOneShot(animal.Data.CallClip)` on every valid scan, first or repeat. The clip comes from the `callClip` field of each `SO_*` asset. The leopard plays a jaguar "saw" call, `SFX_Leopard.ogg`, because no leopard call under an accepted licence was found.
- **Bump sound.** `PlayerController` calls `bumpSource.PlayOneShot(bumpClip)` with `SFX_Bump.ogg`, once per bump cooldown.
- **Import settings.** The call and bump clips (`SFX_*`) use Decompress On Load and are preloaded. The ambience (`AMB_*`) is compressed in memory and is also preloaded. It is not streamed, because streaming is unreliable on WebGL.

Every clip is credited in section 10. The licence file (`Assets/ThirdParty/AUDIO-LICENCES.txt`) records that the sounds were checked by measurement and spectrogram, not by ear. Sound in the WebGL build has not been checked by ear (see `BUGS.md`, entry 19).

### 5.2 Particle systems

Two `ParticleSystem` objects, both using the `M_Particle` material. No script drives them. Their behaviour comes entirely from the settings stored in the scene.

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

Two levels under `Assets/`, from the project on 14 October 2026:

```
Assets/
  Audio/              AMB_Savanna.ogg, SFX_Buffalo.ogg, SFX_Bump.ogg, SFX_Elephant.ogg, SFX_Leopard.ogg, SFX_Lion.ogg, SFX_Rhino.ogg
  Editor/             AnimalImportSettings.cs, BuildScript.cs, SceneryImportSettings.cs
  Images/             background_removed.png (title logo)
  Input/              InputSystem_Actions.inputactions
  Materials/          26 materials (M_Ground, M_GuideLine, M_Lion, M_Track and others)
    Fonts/            Tribeca.ttf, the UI font, and its TextMesh Pro asset
  Models/
    Animals/          SM_Buffalo.fbx, SM_Elephant.fbx, SM_Leopard_Jaguar.fbx, SM_Lion.fbx, SM_Rhino.fbx
      Textures/       T_Buffalo_Palette.png, T_Elephant_Palette.png, T_Leopard_Jaguar.png, T_Lion_Palette.png, T_Rhino_Palette.png
    Generated/        17 merged ground, landform and mountain meshes, used by the scene
    Scenery/          Kenney Nature Kit pieces, in sub-folders Ground, Materials (14 shared materials), Rocks, Scatter and Trees
  Prefabs/            PF_Animal, PF_Animal_Lion, PF_Animal_Leopard, PF_Animal_Elephant, PF_Animal_Rhino, PF_Animal_Buffalo
  Resources/          PerformanceTestRunInfo.json, PerformanceTestRunSettings.json (Unity Test Framework files)
  Scenes/             Safari_Main.unity
  ScriptableObjects/  SO_Lion, SO_Leopard, SO_Elephant, SO_Rhino, SO_Buffalo
  Scripts/            ten gameplay scripts (table below)
  Settings/           URP render pipeline assets and volume profiles
  TextMesh Pro/       TextMesh Pro essentials, imported for the UI text
  ThirdParty/         licence files for Grab3D, Kenney, the jaguar and the audio clips
  UI/                 empty folder
```

**Scene.** The reserve is a 250 m square savanna. Open grassland fills the middle. Woodland clumps are placed around it, and a dense bush belt runs along the edge. Each animal has a habitat behind it: a rock outcrop for the lion, a large acacia with boulders and a log for the leopard, a waterhole for the elephant, scrub and stumps for the rhino, and tall grass and a mud pool for the buffalo. A dirt track runs from the spawn to each animal. Distant hills and one mountain landmark are in the view, and the mountain is to the north. A warm procedural sky and fog complete the view.

**Scenery.** All scenery pieces come from the Kenney Nature Kit. Only part of the pack is placed in the scene, and the rest stays in the project for further scene work. The pieces are recoloured to a dry savanna palette. The colours are set on the pack's shared materials, so the scenery uses no textures.

**Scene totals** (measured before the animals were enlarged; scale does not change triangle counts): 126,075 triangles and 1,265 renderers.

### Script table

Line counts are from `wc -l` on 14 Oct 2026.

| File | Purpose | Lines |
| --- | --- | --- |
| `PlayerController.cs` | First-person move and look, gravity, bump feedback, spawn reset | 168 |
| `AnimalScanner.cs` | Interact: centre-screen raycast, stamps, call sound, fact card | 98 |
| `Animal.cs` | Per-animal runtime state, zone radii, static list of all animals | 76 |
| `AnimalData.cs` | ScriptableObject: fact, job, radii, call clip | 33 |
| `ViewingZone.cs` | Trigger that allows scanning and shows the hint | 50 |
| `DangerZone.cs` | Trigger that shows the warning and books breaches, with a cooldown | 48 |
| `SafariGuideLine.cs` | LineRenderer guide to the nearest unscanned animal | 101 |
| `GameManager.cs` | Run state (stamps, score, breaches, jobs), events, win, save and restore | 163 |
| `UIManager.cs` | Every screen, the HUD, the fact card; listens to GameManager | 385 |
| `SaveSystem.cs` | PlayerPrefs and JsonUtility save, load and clear | 57 |

Total: 1,179 lines.

### Editor scripts

The Editor folder has three scripts. Line counts are from `wc -l` on 14 Oct 2026.

| File | Purpose | Lines |
| --- | --- | --- |
| `AnimalImportSettings.cs` | Import settings for the animal models under `Assets/Models/Animals` (an `AssetPostprocessor`) | 44 |
| `BuildScript.cs` | WebGL and Android builds, from the Build menu or the command line | 65 |
| `SceneryImportSettings.cs` | Import settings for the Kenney scenery, and the shared materials (an `AssetPostprocessor`) | 115 |

Total: 224 lines.

### Editor tooling

The scene, prefabs and data assets were laid out during development with editor scripts: a scene generator, a static validator, a Play-mode smoke test and a screenshot tool. They are not part of the submitted project. The submitted `Assets/Editor` folder has three small scripts, listed above: `AnimalImportSettings.cs` and `SceneryImportSettings.cs` (import settings for the models) and `BuildScript.cs` (WebGL and Android builds). The scene is now edited by hand in the Editor, like any Unity scene. Verification recorded on 14 October 2026: static validation 87 checks passed, Play-mode test 74 checks passed.

### Prefabs

- **`PF_Animal`** (base). The root has `Animal.cs` and the `AnimalData` slot. Its children are `Body` (the model and a solid body collider on the Animal layer), `ViewingZone` (a `SphereCollider` trigger with `ViewingZone.cs`) and `DangerZone` (a `SphereCollider` trigger with `DangerZone.cs`). Its zone radii are the base values, 15 and 6 m.
- **Five variants:** `PF_Animal_Lion`, `PF_Animal_Leopard`, `PF_Animal_Elephant`, `PF_Animal_Rhino` and `PF_Animal_Buffalo`. Each one overrides the model, the scale and the `AnimalData`.
- The leopard variant uses the jaguar model (`SM_Leopard_Jaguar.fbx`) at leopard scale, with the `M_LeopardJaguar` material. See section 10.

### ScriptableObjects

`SO_Lion`, `SO_Leopard`, `SO_Elephant`, `SO_Rhino` and `SO_Buffalo` are `AnimalData` assets. Each holds the species name, habitat, status, fact, job role, job description, the two zone radii and the call clip. The viewing and danger radii are 15 m and 6 m for lion, leopard and buffalo, and 18 m and 8 m for elephant and rhino.

### Animal models

| Animal | Model file | Triangles |
| --- | --- | --- |
| Lion | `SM_Lion.fbx` | 2,998 |
| Leopard | `SM_Leopard_Jaguar.fbx` (a jaguar) | 1,020 |
| Elephant | `SM_Elephant.fbx` | 3,000 |
| Rhino | `SM_Rhino.fbx` | 2,998 |
| Buffalo | `SM_Buffalo.fbx` | 2,999 |

Measured before the animals were enlarged. Scale does not change triangle counts.

## 7. Deviations from the plan

The plan is `misc/BUILD-SPEC.md`. This table lists what differs from it, and why.

| Area | What differs from the plan | Why |
| --- | --- | --- |
| Leopard model and call | A jaguar model (`SM_Leopard_Jaguar.fbx`) and a jaguar "saw" call stand in for the leopard | No leopard model or call under CC0 or CC BY could be downloaded without an account. |
| Buffalo model | Grab3D's bull model (Low Poly Bull) stands in for the buffalo | The plan allowed a bull used as the buffalo. It is not a Cape buffalo model. |
| Animal size and angle | Lion, rhino and buffalo 1.3x, leopard 1.35x, elephant 1.12x; animals turned to a three-quarter view | So the animals read at scanning distance. The viewing and danger radii keep the plan's values. |
| Bake Axis Conversion | Off for the animal FBX files | The Blender exports already carry the axis conversion, and baking it again turns the animals to face -Z. |
| Lion position | The lion stands 50 m from the spawn; the others are about 60 m | So the guide line leads straight ahead at the start. |
| On-screen sticks | Sticks show only when `Application.isMobilePlatform` is true; the Scan button is always visible | A touchscreen flag also reports true in desktop browsers (BUGS 16). |
| Animal call | The call plays on every valid scan, not only the first | The animal answers each scan with its call, so the player gets feedback every time. |
| Zone radii | The prefab's serialized zone radii are the base values (15 and 6); `Animal.Awake` sets each animal's real radii from its AnimalData (elephant and rhino 18 and 8) | Each animal's radii come from its own data asset. |
| Reset method | `Animal.ResetState` instead of `Reset` | Unity treats `Reset()` as an Editor message (BUGS 8). |
| Script size | 1,179 lines, against the plan's estimate of about 550 | Mostly Inspector fields, headers and the null guards the plan asks for. |
| UI font | Tribeca is used for all UI text, and it is caps-only | One font for the whole UI (commit dbaa191). |
| Audio clips | Seven clips: four CC0 and three CC BY 4.0 (lion, leopard, buffalo), credited in section 10 | CC BY 4.0 needs only a credit, and section 10 gives it. |
| Android | Not built yet | No APK has been built, so the phone path is untested. |

## 8. Build Information

**Links**

| Build | Link |
| --- | --- |
| WebGL (desktop browser) | TODO_WEBGL_URL |
| Android APK | TODO_APK_URL |
| Demo video | TODO_VIDEO_URL |

**Versions.** Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.1.

**Build settings in the project.**

- WebGL: compression format is Disabled and decompression fallback is off. GitHub Pages cannot send the `Content-Encoding` header that compressed builds need. `BuildScript.BuildWebGL` sets the canvas to 1280 x 720 and writes the build to `Builds/WebGL`.
- Android: IL2CPP scripting backend, ARM64 only, landscape orientation (auto-rotate left and right, portrait off). `BuildScript.BuildAndroid` builds an APK, not an Android App Bundle. The file is named `BigFiveHeritageSafari.apk` and goes in an `Android` sub-folder of the project's `Builds` folder. No APK has been built yet.
- Quality: WebGL and Android both use the "Mobile" quality level. It has real-time shadows disabled and MSAA off. The animals use blob shadows instead.
- Build Settings contains one scene: `Assets/Scenes/Safari_Main.unity`.

**Build status.**

- WebGL: built and run locally in a desktop browser, where the start screen, walking, scanning and the fact card were checked. Sound in the browser has not been checked by ear.
- WebGL build (14 Oct 2026, `BuildScript.BuildWebGL`): result Succeeded, 0 errors, 65,828,444 bytes (65.8 MB, or 62.8 MiB). Canvas 1280 x 720, compression disabled.
- WebGL speed: the heavier scene measured about 128 frames per second in a desktop browser on the author's PC. This was not a phone test.
- Android: not built yet. The phone path is untested.

**Mobile note.** The animals use blob shadows, not real-time shadows. Real-time shadows are off on the Mobile quality level used by WebGL and Android.

**Run the project in the Editor.**

1. Install Unity 6000.3.23f1 with Unity Hub. For an APK build, also install Android Build Support (SDK, NDK and OpenJDK).
2. Add the project folder in Unity Hub and open it.
3. Open `Assets/Scenes/Safari_Main.unity` and press Play.
4. On the start screen, press New Safari. Press Continue if you have a saved run.

**Run on a desktop browser.** Open the WebGL link (TODO_WEBGL_URL). Move with WASD, hold the right mouse button to look, press E or click the Scan button. The on-screen sticks do not appear on desktop.

**Run on an Android phone.** There is no Android build yet, so there is no APK to install. The APK link above (TODO_APK_URL) is a placeholder. When an APK exists, it is built for landscape and uses the two on-screen sticks and the Scan button. A phone browser can also open the WebGL link (TODO_WEBGL_URL). Neither phone path has been tested.

**Build from the command line.** Close the Editor first, because Unity locks the project while it is open:

```
Unity.exe -batchmode -quit -nographics -projectPath <project path> -executeMethod BuildScript.BuildWebGL -logFile <log path>
```

Use `BuildScript.BuildAndroid` in place of `BuildWebGL` for the APK. The same methods are in the Editor menu as Build > Build WebGL and Build > Build Android APK. Each build prints one line that starts with `[Build]`, with the result, the size in bytes and the error count.

## 9. Reflection

**Real bugs.** The full entries, with date, symptom, cause and fix, are in `BUGS.md`. In short:

- The fact card showed no fact and no job description, because the two fields had no `[SerializeField]` (entry 1).
- The bump sound fired every frame, because the ground counts as a hit (entry 2).
- The on-screen sticks could drop out, because automatic control-scheme switching hid the other device's bindings. I found this in code review before device testing (entry 3).
- An Editor build crashed while saving prefab variants, because a material was created while a prefab was open (entry 4).
- All scenery was reported missing, because the folder name was joined twice in the path (entry 5).
- Two tools owned the animal Bake Axis Conversion flag and fought over it (entry 6).
- Validation skipped array fields, because it looked properties up by name instead of `propertyPath` (entry 7).
- A design note: a method named `Reset` would have been run by Unity as an editor message, so it was renamed `ResetState` (entry 8).
- The guide line never followed the player, because it was wired to an empty Player group at the origin instead of the object with the `CharacterController` (entry 9).
- The four Grab3D animals rendered white, because their palette PNG was never added to the project (entry 10).
- Most of the scenery was placed off the map, because the placement helper mixed local and world space (entry 11).
- The scene failed to load with `InvalidOperationException: Invalid user`, because the on-screen controls were active before `PlayerInput` had a user (entry 12).
- The win screen was hidden under the fifth fact card (entry 13).
- A particle system logged an error every frame, found in the browser console (entry 14).
- The guide line led out of view at the start, because the lion was 60.0 m ahead while the elephant and rhino were 59.8 m behind (entry 15).
- On-screen sticks appeared in a desktop browser, because `Touchscreen.current` is not null there (entry 16).
- Animals faced away from the player, because the review cameras were on the far side of each animal (entry 17).
- Some fact card sentences went beyond their cited sources and were rewritten (entry 18).
- Known issue, not fixed: the WebGL console logs a "sound which is not loaded yet" warning. It does not come from project code (entry 19).
- Track and ground patches were invisible after merging, and the hills and mountain rendered inside-out, because the generated meshes had reversed triangle winding (entry 20).
- Scenery never matched the savanna palette, because the embedded Kenney materials were not matched by name to the project's shared materials (entry 21).
- The fact card was hard to read, and its Continue button hung below the card (entry 22).
- The "walk into the elephant" test failed after the animals were enlarged, because the test started the player a fixed 3 m from the elephant, which left 0.56 m of walking room. The collider itself was working (entry 23).

**Hardest feature (draft for the author to confirm).** For me the hardest part was the editor build pipeline. Many of the bugs in `BUGS.md` came from the scene, asset and import code, not from gameplay (entries 4 to 7, 9 to 11, 14, 15 and 17). The fixes taught me to give each setting one owner, to read values back from an asset after writing it, and to walk the real property paths when validating.

**WebGL and Android challenges.** WebGL: compression is disabled so GitHub Pages can serve the files. A particle error was logged every frame and flooded the browser console (BUGS 14). On-screen sticks appeared in a desktop browser (BUGS 16), so they now show only on mobile platforms. The browser console also logs a message about sound length before audio is loaded. It does not come from project code and it is still open (BUGS 19). The heavier scene measured about 128 frames per second in a desktop browser on the author's PC. Android: the settings are in place (IL2CPP, ARM64, landscape, APK), but no APK has been built or run on a phone yet.

**Next steps.**

- Animated, rigged animals with walk cycles. The current models are static meshes with no rigs.
- A real leopard model, to replace the jaguar stand-in.
- Check the sound in the WebGL build by ear, and find a leopard call under an acceptable licence (the leopard now plays a jaguar call).
- Build the Android APK and play a full run on a phone.
- More species, with Swahili names and text.
- Real distances that follow the Kenya Wildlife Service guidance, in place of the scaled-down radii.

## 10. Credits and Licences

### Third-party assets

| Asset | Source | Licence | Notes |
| --- | --- | --- | --- |
| Lion, elephant, rhino and buffalo meshes (`SM_Lion`, `SM_Elephant`, `SM_Rhino`, `SM_Buffalo`) | Grab3D (grab3d.com) | CC0 1.0 | The buffalo is Grab3D's Low Poly Bull, standing in for a Cape buffalo. The palette textures (`T_Lion_Palette.png`, `T_Elephant_Palette.png`, `T_Rhino_Palette.png`, `T_Buffalo_Palette.png`) are Grab3D's own, extracted from its GLB downloads, and are used through `M_Lion`, `M_Elephant`, `M_Rhino` and `M_Buffalo`. Licence file: `Assets/ThirdParty/Grab3D-LICENSE.txt`. |
| Leopard, as a jaguar model (`SM_Leopard_Jaguar.fbx`, `T_Leopard_Jaguar.png`, `M_LeopardJaguar`). Source: "Jaguar" by Poly by Google, https://poly.pizza/m/4fb-oMr2uUF | Poly by Google, via poly.pizza | CC BY 3.0 | This is a jaguar, not a leopard. It stands in for the leopard. Changes: converted from GLB to FBX in Blender 5.2, scaled, placed so the feet rest on the ground at the origin, base colour texture downscaled from 2048 to 1024 pixels, material renamed. Licence file: `Assets/ThirdParty/POLY-JAGUAR-LICENSE.txt`. |
| Scenery: trees, rocks and ground cover | Kenney Nature Kit 2.1 (kenney.nl) | CC0 1.0 | Credit to Kenney is appreciated but not required. Licence file: `Assets/ThirdParty/KENNEY-NATURE-KIT-LICENSE.txt`. |
| Font: Tribeca (`Assets/Materials/Fonts/Tribeca.ttf`) | Not recorded in the project | TODO_FONT_CREDIT | Used for all UI text. It is caps-only. The TextMesh Pro essentials are in `Assets/TextMesh Pro`. |
| Title logo (`Assets/Images/background_removed.png`) | TODO_LOGO_CREDIT | Not yet recorded | |

**Required attribution (CC BY 3.0), for the jaguar model:**

> "Jaguar" by Poly by Google, licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/), via poly.pizza.

### Audio

The seven clips and their licences are recorded in `Assets/ThirdParty/AUDIO-LICENCES.txt`. That file is the source for this table. Four clips are CC0 1.0 and need no credit. Three clips are CC BY 4.0 and must be credited.

| File | Sound (title on the source) | Author | Licence | Source | Notes |
| --- | --- | --- | --- | --- | --- |
| `SFX_Lion.ogg` | "Lion roars 1.wav" (male lions roaring, recorded at Ukutula Game Reserve, South Africa) | eardeer | CC BY 4.0 | https://freesound.org/people/eardeer/sounds/458903/ | A 4.5 s excerpt, mono and loudness-normalised. A steady tone near 4.8 kHz (recording noise) is in the source; it is low in the excerpt. |
| `SFX_Leopard.ogg` | "Jaguar saw" (a jaguar, not a leopard) | About Zoos | CC BY 4.0 | https://commons.wikimedia.org/wiki/File:Jaguar_saw.flac | Substitution: no leopard call under an accepted licence was found. A 1.39 s call, raised 9 dB and peak-limited. |
| `SFX_Elephant.ogg` | "Elephant voice - trumpeting" | தகவலுழவன் (Info-farmer), Wikimedia Commons | CC0 1.0 | https://commons.wikimedia.org/wiki/File:Elephant_voice_-_trumpeting.ogg | The page does not name the species. The uploader's other recording is from Tamil Nadu, India, so this is probably an Asian elephant, not an African elephant. A 1.44 s clip. |
| `SFX_Rhino.ogg` | "white Rhino   fighting" (a white rhinoceros, Umfolozi game reserve) | geoffvt | CC0 1.0 | https://freesound.org/people/geoffvt/sounds/792852/ | Substitution: this is a white rhinoceros, used for the black rhino model. The page says "fighting", so some sounds may be contact sounds. A 3.5 s excerpt, lowered 2 dB. |
| `SFX_Buffalo.ogg` | "Buffles-Grognement+amb oiseaux" (African buffalo grunts, Tanzania) | roubignolle (G de Courtivron) | CC BY 4.0 | https://freesound.org/people/roubignolle/sounds/35130/ | Birds are in the source. A 2.5 kHz low-pass reduces them but does not remove them. A 3.5 s excerpt. |
| `AMB_Savanna.ogg` | "Birds and wind at Ngutuni Lodge" (near Voi, Kenya) | dkaufman | CC0 1.0 | https://freesound.org/people/dkaufman/sounds/743640/ | Loudness-normalised and faded in and out. A 22.2 s clip. |
| `SFX_Bump.ogg` | "Light Body thud (on clothing)" | JonasTisell | CC0 1.0 | https://freesound.org/people/JonasTisell/sounds/496187/ | Silence trimmed and re-encoded. A 0.25 s clip. |

**Required attribution (CC BY 4.0), for the three clips that need it:**

> "Lion roars 1.wav" by eardeer, https://freesound.org/people/eardeer/sounds/458903/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Jaguar saw" by About Zoos, https://commons.wikimedia.org/wiki/File:Jaguar_saw.flac, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

> "Buffles-Grognement+amb oiseaux" by roubignolle (G de Courtivron), https://freesound.org/people/roubignolle/sounds/35130/, licensed under CC BY 4.0, https://creativecommons.org/licenses/by/4.0/

**Substitution note (rhino).** The rhino model is a black rhinoceros. The sound is a white rhinoceros recording, used as a stand-in for the black rhino.

**Substitution note (leopard).** No leopard call under CC0 or CC BY was found. The leopard plays a jaguar "saw" call, and its model is a jaguar too.

