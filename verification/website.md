# Website and release verification

Verified September 17, 2026.

- Production: https://relic-arena.vercel.app
- Vercel deployment: dpl_FcsMnGmQrkpZ1cjT7SP27QGCnBqZ, target production, status READY.
- Anonymous HTTP GET returned 200 with the game landing page; hosted release.json matched v0.3.0.
- Downloaded the public GitHub release without authentication: 45,782,994 bytes, 192 ZIP entries, valid CRC, executable present.
- SHA-256: d9532b939e98a1f3c0241802f7200e40d6d90b6e313b0d95e8a6879391d48bb7.
- In-app browser: inspected desktop 1440x1000 and mobile 390x844; no horizontal overflow. Verified realm tabs by click and keyboard, FAQ expansion, checksum copy, and correct download links. No warning/error console entries during checks.
- Gameplay verification remains the packaged v3 build's 67 passing runtime checks; these website checks do not claim additional gameplay testing.

## Visual review

Authored direction: deep green background, cream serif headline, mint action buttons, real gameplay imagery, three selectable realm previews, and clear installation steps. Desktop and mobile screenshots were visually reviewed against this direction. Hierarchy, spacing, image visibility, responsive stacking and primary action were confirmed. An attempted generated concept was unavailable because the image-generation account limit was reached; no generated concept comparison is claimed.

## Deploy again

From site/, run `vercel link --project relic-arena --scope mahedi-hasans-projects-6d5e504d`, then `vercel deploy --prod`. The project is deployed through the CLI; Git pushes alone are not configured to trigger deployment.
