# Relic Arena: Realms

A free Windows third-person action arena built with Unity and Blender. Fight through three realms, nine encounters, and three bosses.

- **Download:** https://github.com/M-Hasan-11/relic-arena/releases/latest
- **Website:** https://relic-arena.vercel.app
- **Controls:** WASD move, mouse look, left click/J sword, Space dodge, Q shockwave, Escape pause.

## Play on Windows

Download `RelicArena-v0.3.0-Windows-x64.zip` from Releases, extract the entire ZIP, and open `RelicArena.exe`. Keep its data folders and DLLs together. No Unity installation or account is needed. This prototype is not code-signed.

## Repository contents

- `game/`: Unity scripts, original meshes/shader, render settings and project settings.
- `ArtSource/`: editable original Blender scenes and regeneration scripts.
- `site/`: static download website, served directly by Vercel.
- `ASSET_CREDITS.md`: third-party sources and attribution.
- `verification/`: final runtime check results and short performance sample.

This is a **source kit**, not a self-contained Unity clone. Third-party Asset Store and Mixamo source assets are intentionally omitted. Compiled assets are included in the playable Windows release.

## Build the game

1. Open `game` with Unity **6000.5.3f1** and let its packages resolve.
2. Obtain/import Blink's **FREE Low Poly Human – RPG Character**, preserving its default `Assets/Blink` paths.
3. Obtain Unity's **Particle Pack**. The build uses `RoundSoftParticle.tif` and `smokeysteam.tif` from the legacy pack; place your licensed copies in `Assets/ThirdParty/UnityParticlePack/Textures`.
4. Obtain Adobe Mixamo **Idle** and **Walking** humanoid FBX animations and place them as `Assets/ThirdParty/Mixamo/Idle.fbx` and `Walking.fbx`. The authored build used existing X Bot downloads, retargeted to Blink's humanoid.
5. Select **Relic Arena → Rebuild Scene**, then **Relic Arena → Build Windows**. The scene generator replaces the generated scene; keep manual edits in a separate scene.
6. The executable is written to `Builds/Windows/RelicArena.exe`. Run `powershell -File Verify-Game.ps1` to exercise the standalone checks.

See `ASSET_CREDITS.md` for official asset links. Do not commit your downloaded third-party source assets.

## Website

Serve `site/` with any static HTTP server, for example `python -m http.server 4173 --directory site`. Vercel serves this directory with no build step. Update `site/release.json` and the download links when publishing a new release. The Windows ZIP is hosted in GitHub Releases rather than in Git history.

## Status and rights

Playable prototype. The final v3 build passed 67 runtime checks. Short frame samples do not establish sustained performance on every PC; see `VERIFICATION.md`.

Original project code and artwork remain copyright their respective author; no blanket open-source license is granted by this repository. Third-party assets keep their own licenses. The compiled game is provided as a free download.
