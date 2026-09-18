# Relic Arena — Realms (v3)

A Windows third-person action game prototype built with Unity 6000.5.3f1, Blender 5.1.1, Adobe Mixamo animations and free Unity Asset Store assets.

## Play

Launch `Windows-v3/RelicArena.exe` and press Enter. Builds v1 and v2 remain in `Windows` and `Windows-v2`.

| Control | Action |
| --- | --- |
| WASD | Camera-relative movement |
| Mouse / wheel | Look / camera distance |
| Left mouse / J | Sword swings; hold to repeat |
| Space | Dodge; brief invulnerability |
| Q | Shockwave |
| Escape | Pause and settings |
| 1 / 2 / 3 | Choose a blessing after an encounter |
| M | Mute / unmute |
| R | Restart after defeat or victory |

## Campaign

Three arena levels use different skies, lighting, cover layouts and crystal scenery. Each has two regular encounters followed by a boss encounter. There are nine encounters and 45 enemies in total, including three bosses.

| Level | Setting | Boss |
| --- | --- | --- |
| Azure Sanctuary | Blue mountain sky, crystal cover | The Gate Warden: slam and charge |
| Ember Citadel | Warm dusk, additional barricades | Cinder Tyrant: slam, charge and projectile fan |
| Astral Summit | Starry sky, outer crystal pillars | The Astral King: stronger attacks and larger entourage |

Bosses become enraged below half health. Move out of red circles before a slam and dodge sideways from charge lanes. Their attacks cannot be permanently interrupted by sword hits. Ranged knights move to maintain distance and briefly predict movement before locking their aim. Melee knights approach on alternating flanks, separate from allies, and limit simultaneous attack windups.

Solid cover blocks movement, sword hits and projectiles. Dodge does not pass through it. Health is restored on entering a new level; clearing an encounter heals 15. Every third kill drops a healing relic. Choose a damage, vitality or cooldown blessing between encounters. Preferences and personal best are saved locally; campaign progress is not saved.

## Animation and art

The player uses a humanoid blend between Adobe Mixamo idle/walk and Blink run animation. Combat, roll, casting, hit and death clips come from the free Blink character pack. Attack damage waits for the swing impact; dodge movement and animation have separate durations. Original Blender models provide the temple and large sentinel bosses; `ArtSource/RealmBackdrops.blend` contains the new floating mountains, citadel silhouettes and celestial ring.

`ASSET_CREDITS.md` lists asset sources. No paid assets were purchased. Mixamo clips were imported from existing downloads on this computer.

## Edit and build

Open `RelicArena` in Unity Hub with 6000.5.3f1, then open `Assets/Scenes/RelicArena.unity`.

- `Guardian.cs`: player combat, input and animation.
- `ArenaMotor.cs`: swept CharacterController movement, gravity, boundary and obstacle steering.
- `Sentinel.cs`: enemy and boss behavior.
- `Campaign.cs`: level activation, attack coordination and boss HUD.
- `RelicProgression.cs`: upgrades, preferences, projectiles and effects.
- `RelicGame.cs`: session, encounters, camera and UI.
- `CampaignVerification.cs`: standalone runtime regression checks.
- `RealmsBuilder.cs`: humanoid retargeting, blend controller, collision scenery and environments.

Use **Relic Arena → Build Windows** to build v3. **Rebuild Scene** regenerates the scene; save an alternate scene before using it if you want to preserve manual edits. Blender scripts `build_art.py` and `build_backdrops.py` reproduce the original meshes and export FBX; they recreate their Blender scenes.

## Verify

Run `powershell -File .\Verify-Game.ps1`. A visible game window runs checks and saves reports/screenshots in `Windows-v3/Verification`. See `VERIFICATION.md` for the latest actual results.

This remains a prototype: no gamepad, campaign save, navigation mesh or multiplayer. AI uses local steering around the authored arena cover. Performance and combat balance still benefit from human playtesting on the target machine.

## Rendering

The game targets 60 FPS. On Intel graphics, the 3D scene uses 85% render scale; the HUD stays at window resolution. MSAA uses two samples and shadow distance is limited to 40 metres. Fixed temple, background and cover meshes are marked for build-time static batching. Actual frame rates depend on the machine and workload; the verification report records the measured sample rather than guaranteeing 60 FPS.
