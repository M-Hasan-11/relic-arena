# Relic Arena — third-party asset credits

## FREE Low Poly Human – RPG Character

- Publisher: **Blink**
- [Unity Asset Store listing](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979)
- Listing checked September 16, 2026: **FREE**, Standard Unity Asset Store EULA, version 1.0.
- Imported from the existing Unity Asset Store cache on this computer. No purchase was made.
- Used in this project: the humanoid character, plate armour, textures and idle/run/attack/roll/death animations included in the pack.
- Adaptations: copied materials converted to URP, custom Animator controller, Blender-made sword attached to the hand, gameplay logic written for Relic Arena.
- Imported source is under `RelicArena/Assets/Blink`; project-specific converted materials and prefabs are separate.

## Particle Pack | Starter Assets

- Publisher: **Unity Technologies**
- [Unity Asset Store listing](https://assetstore.unity.com/packages/vfx/particles/particle-pack-starter-assets-127325)
- Listing checked September 16, 2026: **FREE**, Standard Unity Asset Store EULA.
- Selected textures imported from the existing cached `Particle Pack.unitypackage`. The cache predates the current listing; this project uses adapted textures, not an unmodified current URP prefab.
- Used: `RoundSoftParticle.tif` for sparks and motes, `smokeysteam.tif` for cloud particles. `DustMote.png`, `EnergyEffect.tif`, and `shockwave.tif` are also imported for future effect tuning.
- Adaptations: new URP particle materials and custom Particle System settings.
- Location: `RelicArena/Assets/ThirdParty/UnityParticlePack/Textures`.

The Asset Store assets remain subject to the [Unity Asset Store Terms and EULA](https://unity.com/legal/as-terms). This project does not relicense them. Obtain your own applicable licenses before sharing a source project with people who are not covered by your licenses; do not redistribute the imported assets as a standalone asset pack.

The arena, original guardian/sentinel models, and sword in `ArtSource/RelicArena.blend` were created for this project. No paid assets were added for this update.

## Adobe Mixamo — Realms update

- [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), checked September 17, 2026, describes free access and use of Mixamo characters/animations in games.
- Existing local downloads `Walking.fbx` and `X Bot@Idle.fbx` were copied into `RelicArena/Assets/ThirdParty/Mixamo` as `Walking.fbx` and `Idle.fbx`.
- Both source FBX files contain Mixamo metadata (`Mixamo, Inc.`, `mixamo.com`, and `mixamorig` skeleton names).
- Adaptations: Unity Humanoid import, in-place root settings, looping idle/walk, and a velocity-driven blend tree retargeted to Blink's armored character.
- Combat animations remain from Blink; they are not represented as Mixamo animations. The original Mixamo character meshes are not instantiated in the game.
- Blink and Unity Particle Pack listings were rechecked as free on September 17, 2026. The update reuses these assets for enemy humanoids, hit/roll/boss particles and atmosphere.
- `ArtSource/RealmBackdrops.blend`, its FBX export, the realm sky shader and level cover layouts are original work for this project.
