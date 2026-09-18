# Verification — Realms v3

Final Windows x64 build: **Succeeded, 0 errors**. Unity-reported build size: **117,405,932 bytes**. The standalone verifier exited successfully with **67 / 67 checks passed**.

## Runtime coverage

- Real humanoid bone movement in both attack states, roll and casting; avatar and Adobe Mixamo controller connections.
- Keyboard-driven movement, velocity-based locomotion blending, delayed sword impact, dodge immunity, damage and shockwave cooldown.
- Ground contact, arena boundary, cover penetration/climbing during sliding, and projectiles intercepted by cover.
- Ranged AI projectile damage and distance keeping; boss slam damage, enraging and damage reception.
- Pause/resume, all three blessings, healing, all nine encounters, each level's boss, all three level transitions, 45-enemy campaign victory, defeat and restart.

The first pass exposed unusable poses in two supplied animation clips; they were replaced with the verified swing and combat idle. A repeated cylinder collision check prompted replacing the primitive's capsule collider with a mesh collider matching the visible cover and checking clearance throughout movement. These corrections are present in the final tested build.

## Rendering and performance

Final standalone title, gameplay and three boss screens were inspected. No gameplay exceptions or failed checks were logged. Direct3D still logs an unavailable info-queue interface (`0x80004002`), without preventing rendering or test completion.

The final 120-frame combat sample measured **16.68 ms mean / approximately 60.0 FPS**, with **16.70 ms 95th-percentile frame time**, in a 1440×900 window on Intel Graphics. The 3D render scale is 85% on Intel; UI remains at window resolution. Earlier short samples varied from approximately 32 to 60 FPS before the final batching optimization. This is a short sample, not a sustained or worst-case hardware benchmark.

## Evidence

All final evidence is in `Windows-v3/Verification`:

- `smoke-results.txt`: 67 checks.
- `performance.txt`: frame sample, resolution and GPU.
- `player.log`: standalone runtime log.
- `title.png`, `gameplay.png`, `upgrade.png`.
- `level-1-boss.png`, `level-2-boss.png`, `level-3-boss.png`.

The source-kit builder writes its result to `Builds/build-result.txt`. Earlier v1/v2 evidence and builds remain available separately.

## Limits

Tests use injected keyboard input and direct gameplay calls. They do not replace a human difficulty/balance review, verify every boss attack combination, or establish sustained performance on other hardware. AI uses local steering around authored cover rather than a general navigation mesh. Settings persistence and every possible animation interruption have not been exhaustively tested.

