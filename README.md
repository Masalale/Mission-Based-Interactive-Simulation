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

The on-screen sticks appear only on mobile platforms. The Scan button is on the HUD on every platform.

**How to win.** Stamp all five animals (5 of 5 stamps). Each animal is stamped when the player scans it from inside its viewing zone. There is no fail state. The end screen opens after the player closes the last fact card. It shows the stamps, score, breaches and jobs, and it offers Replay.

**How the score works.** The score starts at 100 and never drops below 0. Each breach of a danger zone costs 10 points. An animal can book a breach at most once every 3 seconds. Each first scan adds 1 to Jobs Supported. A repeat scan only reopens the fact card. The top score is 100, so a perfect run has zero breaches.

## 4. Unity Mechanics Implemented

Each mechanic names its scripts, what it does in this game, and what breaks if it is removed. That consequence is the deletion test from the build spec.

### 4.1 UI

**Scripts:** `UIManager.cs`. The Canvas is built by `SafariUIBuilder.cs` in the Editor and saved in `Safari_Main.unity`.

**What it does:**

- A start screen with Continue and New Safari buttons. Continue is only enabled when a save exists.
- A HUD with a crosshair, a five-slot passport, a Jobs Supported counter, a score, a stamps counter, a hint line, a warning line, and short toast messages. The HUD is built inactive and is shown when a run starts.
- A fact card with the animal name, habitat, status, the fact, the job role and the job description, and a close button.
- An end screen with a summary and a Replay button. It opens when the player closes the last fact card of a won run.

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
- On-screen controls: an `OnScreenStick` bound to `<Gamepad>/leftStick` (Move), an `OnScreenStick` bound to `<Gamepad>/rightStick` (Look), and an `OnScreenButton` bound to `<Gamepad>/buttonSouth` (Interact). The two sticks sit in their own `MobileControls` panel. `UIManager` shows that panel only when `Application.isMobilePlatform` is true, so a desktop browser does not show the sticks. The Scan button stays on the HUD on desktop and mobile.

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
- It calls `Physics.Raycast(ray, out hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore)`. `maxDistance` is 20 m. `animalMask` is set to layer 6 (value 64) by the scene builder. `QueryTriggerInteraction.Ignore` means the zone triggers never block the ray.
- A miss shows "Nothing in your sights. Aim at an animal." A hit on an animal that is not in its viewing zone shows "Move into the viewing zone first."
- A hit on an animal in its viewing zone is the only way to earn a stamp. On the first scan, `AnimalScanner` calls `GameManager.Instance.AddStamp(animal)`. That is the only call site in the project. It then plays the animal's call clip.
- A repeat scan does not award anything. It only reopens the fact card, which says "Already in your passport".

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

Five clips are used for animal calls and the bump sound: four calls (lion, elephant, rhino and buffalo) and `SFX_Bump.ogg`. One clip, `AMB_Savanna.ogg`, is the savanna ambience.

- **Savanna ambience.** An `AudioSource` on the `Ambience` object (under `Systems`) plays `AMB_Savanna.ogg`. Loop is on, Play On Awake is on, and the volume is 0.35.
- **Animal call on first scan.** `AnimalScanner` calls `callSource.PlayOneShot(animal.Data.CallClip)` on a first scan only. The clip comes from the `callClip` field of each `SO_*` asset. The leopard has no call clip, because no leopard recording under an acceptable licence was found, so the leopard plays nothing. The rhino call is an Indian rhinoceros recording used as a stand-in.
- **Bump sound.** `PlayerController` calls `bumpSource.PlayOneShot(bumpClip)` with `SFX_Bump.ogg`, once per bump cooldown.
- **Import settings.** `SafariProjectSetup` sets them. The call and bump clips (`SFX_*`) use Decompress On Load and are preloaded. The ambience (`AMB_*`) is compressed in memory and is also preloaded. It is not streamed, because streaming is unreliable on WebGL.

The six clips are in `Assets/Audio`. Their licences are in section 9. Sound in the WebGL build has not been checked by ear (see `BUGS.md`, entry 19).

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
  Audio/              AMB_Savanna.ogg, SFX_Buffalo.ogg, SFX_Bump.ogg, SFX_Elephant.ogg, SFX_Lion.ogg, SFX_Rhino.ogg
  Editor/             13 editor scripts (table below)
  Images/             background_removed.png (title logo)
  Input/              InputSystem_Actions.inputactions
  Materials/          M_Buffalo, M_Elephant, M_Ground, M_GuideLine, M_LeopardJaguar, M_Lion, M_Particle, M_Rhino, M_Track
    Fonts/            Tribeca.ttf, the UI font, and its TextMesh Pro asset
  Models/
    Animals/          SM_Buffalo.fbx, SM_Elephant.fbx, SM_Leopard_Jaguar.fbx, SM_Lion.fbx, SM_Rhino.fbx
      Textures/       T_Buffalo_Palette.png, T_Elephant_Palette.png, T_Leopard_Jaguar.png, T_Lion_Palette.png, T_Rhino_Palette.png
    Scenery/          Kenney Nature Kit pieces, in sub-folders Ground, Materials, Rocks, Scatter and Trees
    Generated/        Merged ground and landform meshes, made by the editor tooling (SafariScenery.cs)
  Prefabs/            PF_Animal, PF_Animal_Lion, PF_Animal_Leopard, PF_Animal_Elephant, PF_Animal_Rhino, PF_Animal_Buffalo
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

**Scene totals:** 2,614 GameObjects, 1,265 renderers and 126,075 triangles.

### Script table

Line counts are from `wc -l` on 13 Oct 2026.

| File | Purpose | Lines |
| --- | --- | --- |
| `PlayerController.cs` | First-person move and look, gravity, bump feedback, spawn reset | 168 |
| `AnimalScanner.cs` | Interact: centre-screen raycast, stamps, call sound, fact card | 99 |
| `Animal.cs` | Per-animal runtime state, zone radii, static list of all animals | 73 |
| `AnimalData.cs` | ScriptableObject: fact, job, radii, call clip | 33 |
| `ViewingZone.cs` | Trigger that allows scanning and shows the hint | 44 |
| `DangerZone.cs` | Trigger that shows the warning and books breaches, with a cooldown | 45 |
| `SafariGuideLine.cs` | LineRenderer guide to the nearest unscanned animal | 101 |
| `GameManager.cs` | Run state (stamps, score, breaches, jobs), events, win, save and restore | 163 |
| `UIManager.cs` | Every screen, the HUD, the fact card; listens to GameManager | 379 |
| `SaveSystem.cs` | PlayerPrefs and JsonUtility save, load and clear | 57 |

Total: 1,162 lines.

### Editor scripts

The Editor folder holds the build tools, the verification tools and two import postprocessors. Line counts are from `wc -l` on 13 Oct 2026. The Editor is still changing, so these counts are a snapshot.

| File | Purpose | Lines |
| --- | --- | --- |
| `AnimalImportSettings.cs` | Import settings for the animal models under `Assets/Models/Animals` (an `AssetPostprocessor`) | 44 |
| `SafariAssetBuilder.cs` | Shared materials, the five `AnimalData` assets, `PF_Animal` and its five variants | 1,055 |
| `SafariBuild.cs` | WebGL and Android player builds, run by hand | 127 |
| `SafariBuilder.cs` | `BuildAll` and the read-only `Validate` audit | 1,322 |
| `SafariLog.cs` | Log lines with the `[Safari]` prefix, and a failure count for the exit code | 202 |
| `SafariModelOrient.cs` | Works out how to turn each animal model to face the approach, by rotating its Model child | 257 |
| `SafariProjectSetup.cs` | Layers, player settings, import settings and URP assets, and `SafariPaths` | 565 |
| `SafariSceneBuilder.cs` | Rebuilds `Safari_Main` with a fixed random seed | 763 |
| `SafariScenery.cs` | Scenery layout: zones, habitats, track and ground, hills and the mountain, and the merged meshes in `Assets/Models/Generated` | 1,609 |
| `SafariShots.cs` | Renders review screenshots | 836 |
| `SafariSmokeTest.cs` | Play-mode smoke test | 806 |
| `SafariUIBuilder.cs` | Canvas, HUD, start screen, fact card and end screen | 700 |
| `SceneryImportSettings.cs` | Import settings for the Kenney scenery (an `AssetPostprocessor`) | 116 |

Total: 8,402 lines.

### Prefabs

- **`PF_Animal`** (base). The root has `Animal.cs` and the `AnimalData` slot. Its children are `Body` (the model and a solid body collider on the Animal layer), `ViewingZone` (a `SphereCollider` trigger with `ViewingZone.cs`) and `DangerZone` (a `SphereCollider` trigger with `DangerZone.cs`).
- **Five variants:** `PF_Animal_Lion`, `PF_Animal_Leopard`, `PF_Animal_Elephant`, `PF_Animal_Rhino` and `PF_Animal_Buffalo`. Each one overrides the model, the scale and the `AnimalData`.
- The leopard variant uses the jaguar model (`SM_Leopard_Jaguar.fbx`) at leopard scale, with the `M_LeopardJaguar` material. See section 9.

### ScriptableObjects

`SO_Lion`, `SO_Leopard`, `SO_Elephant`, `SO_Rhino` and `SO_Buffalo` are `AnimalData` assets. Each holds the species name, habitat, status, fact, job role, job description, the two zone radii and the call clip. The viewing and danger radii are 15 m and 6 m for lion, leopard and buffalo, and 18 m and 8 m for elephant and rhino.

### Editor tooling

The scene, prefabs and data assets are generated by editor scripts in `Assets/Editor`, so the scene can be rebuilt the same way each time. They are exposed in the Editor under the `Safari` menu:

- `Safari > 1 Project Setup` (`SafariProjectSetup`): layers, player settings, import settings and the URP assets.
- `Safari > 2 Build Assets` (`SafariAssetBuilder`): shared materials, the five `AnimalData` assets, `PF_Animal` and its five variants.
- `Safari > 3 Build Scene` (`SafariSceneBuilder`): rebuilds `Safari_Main` with a fixed random seed, so two runs give the same scatter.
- `Safari > 0 Build All` (`SafariBuilder.BuildAll`): runs the three steps above in order and saves.
- `Safari > 4 Screenshots` (`SafariShots`): renders review screenshots of the built reserve to `Logs/shots`. It needs a graphics device, so it runs without `-nographics`.
- `Safari > 9 Validate` (`SafariBuilder.Validate`): audits the saved scene and prefabs, and prints one PASS or FAIL line per check.

`BuildAll` and `Validate` both end by calling `EditorApplication.Exit`, so they close the Editor when they finish. Run them from the command line, or save your work first.

The scene is now edited by hand in the Editor. Some UI text uses the Tribeca font, which was set by hand. `SafariBuilder.BuildAll` regenerates the scene and would discard these hand edits, so do not run it without care.

`SafariBuilder.Validate` is a read-only check of the generated scene and prefabs, and the last run reported Validate 87 checks passing, Play-mode smoke test 74 checks passing (13 October 2026). `SafariSmokeTest` is an Editor-driven Play-mode run that walks, scans, breaches, wins, saves and replays through the real Input System, and it logs one PASS or FAIL line per check. The smoke test has no menu item. It is started from the command line with `-executeMethod`. Its last run reported Validate 87 checks passing, Play-mode smoke test 74 checks passing (13 October 2026).

## 7. Build Information

**Links**

| Build | Link |
| --- | --- |
| WebGL (desktop browser) | TODO_WEBGL_URL |
| Android APK | TODO_APK_URL |
| Demo video | TODO_VIDEO_URL |

**Versions.** Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.1.

**Build settings in the project.**

- WebGL: compression format is Disabled and decompression fallback is off. GitHub Pages cannot send the `Content-Encoding` header that compressed builds need. `SafariBuild.BuildWebGL` sets the canvas to 1280 x 720. Output folder: `Builds/WebGL`.
- Android: IL2CPP scripting backend, ARM64 only, landscape orientation (auto-rotate left and right, portrait off), built as an APK rather than an AAB. The APK goes to the project's Builds folder, at the path set in `SafariBuild.cs`. No APK has been built yet.
- Build Settings contains one scene: `Assets/Scenes/Safari_Main.unity`.

**Build status.**

- WebGL: built and run locally in a desktop browser, where the start screen, walking, scanning and the fact card were checked. Sound in the browser has not been checked by ear.
- WebGL size and speed: the build is 62 MB at 1280 x 720. It ran at about 128 frames per second in a desktop browser on the author's PC. This was not a phone test.
- Android: not built yet. The phone path is untested.

**Mobile note.** The animals use blob shadows, not real-time shadows. Real-time shadows are off on the low quality level used by WebGL and Android.

**Run the project in the Editor.**

1. Install Unity 6000.3.23f1 with Unity Hub. For an APK build, also install Android Build Support (SDK, NDK and OpenJDK).
2. Add the project folder in Unity Hub and open it.
3. Open `Assets/Scenes/Safari_Main.unity` and press Play.
4. On the start screen, press New Safari. Press Continue if you have a saved run.

**Run on a desktop browser.** Open the WebGL link (TODO_WEBGL_URL). Move with WASD, hold the right mouse button to look, press E or click the Scan button. The on-screen sticks do not appear on desktop.

**Run on an Android phone.** There is no Android build yet, so there is no APK to install. The APK link above (TODO_APK_URL) is a placeholder. When an APK exists, it is built for landscape and uses the two on-screen sticks and the Scan button. A phone browser can also open the WebGL link (TODO_WEBGL_URL). Neither phone path has been tested.

**Build from the command line.** Close the Editor first, because Unity locks the project while it is open:

```
Unity.exe -batchmode -quit -nographics -projectPath <project path> -executeMethod Safari.EditorTools.SafariBuild.BuildWebGL -logFile <log path>
```

Use `BuildAndroid` in place of `BuildWebGL` for the APK. The same methods are available from the `Safari > 7 Build WebGL` and `Safari > 8 Build Android` menu items.

## 8. Reflection

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

**Hardest feature (draft for the author to confirm).** For me the hardest part was the editor build pipeline. Many of the bugs in `BUGS.md` came from the scene, asset and import code, not from gameplay (entries 4 to 7, 9 to 11, 14, 15 and 17). The fixes taught me to give each setting one owner, to read values back from an asset after writing it, and to walk the real property paths when validating.

**WebGL and Android challenges.** TODO_REFLECTION_PLATFORM

**Next steps.**

- Animated, rigged animals with walk cycles. The current models are static meshes with no rigs.
- A real leopard model, to replace the jaguar stand-in.
- Check the sound in the WebGL build by ear, and find a leopard call under an acceptable licence.
- More species, with Swahili names and text.
- Real distances that follow the Kenya Wildlife Service guidance, in place of the scaled-down radii.

## 9. Credits and Licences

### Third-party assets

| Asset | Source | Licence | Notes |
| --- | --- | --- | --- |
| Lion, elephant, rhino and buffalo meshes (`SM_Lion`, `SM_Elephant`, `SM_Rhino`, `SM_Buffalo`) | Grab3D (grab3d.com) | CC0 1.0 | The buffalo is Grab3D's Low Poly Bull, standing in for a Cape buffalo. The palette textures (`T_Lion_Palette.png`, `T_Elephant_Palette.png`, `T_Rhino_Palette.png`, `T_Buffalo_Palette.png`) are Grab3D's own, extracted from its GLB downloads, and are used through `M_Lion`, `M_Elephant`, `M_Rhino` and `M_Buffalo`. |
| Leopard, as a jaguar model (`SM_Leopard_Jaguar.fbx`, `T_Leopard_Jaguar.png`, `M_LeopardJaguar`). Source: "Jaguar" by Poly by Google, https://poly.pizza/m/4fb-oMr2uUF | Poly by Google, via poly.pizza | CC BY 3.0 | This is a jaguar, not a leopard. It stands in for the leopard. Changes: converted from GLB to FBX in Blender 5.2, scaled, placed so the feet rest on the ground at the origin, base colour texture downscaled from 2048 to 1024 pixels, material renamed. Licence file: `Assets/ThirdParty/POLY-JAGUAR-LICENSE.txt`. |
| Scenery: trees, rocks and ground cover | Kenney Nature Kit 2.1 (kenney.nl) | CC0 1.0 | Credit to Kenney is appreciated but not required. |
| Font: Tribeca (`Assets/Materials/Fonts/Tribeca.ttf`) | Not recorded in the project | TODO_FONT_CREDIT | Set by hand on several UI texts in the Editor. The TextMesh Pro essentials are in `Assets/TextMesh Pro`. |
| Title logo (`Assets/Images/background_removed.png`) | TODO_LOGO_CREDIT | Not yet recorded | |

**Required attribution (CC BY 3.0), for the jaguar model:**

> "Jaguar" by Poly by Google, licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/), via poly.pizza.

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

**Substitution note (rhino).** No black rhinoceros recording under CC0 or CC BY was found on Wikimedia Commons, Freesound or OpenGameArt. `SFX_Rhino.ogg` is an Indian rhinoceros recording, used as a stand-in for the black rhino.

**Substitution note (leopard).** No leopard model under CC0 or CC BY could be downloaded without an account. The jaguar model stands in for the leopard, and it is not a leopard model.

**No call clip for the leopard.** The leopard has no call clip, so it has no audio file.

