# Character source

- Original file: `RPGCharacterSprites32x32.png` (384 x 672 px, 32 px grid, 12 columns x 21 rows).
- Source page: https://opengameart.org/content/32x32-rpg-character-sprites
- Author: Eldiran. The page marks the asset CC0.
- Secondary sheet: `RPGSoldier32x32.png` from the same page.
- Enemy candidate: `spritesheet-goblin-32x32-alpha.png` from https://opengameart.org/content/goblin-monster ; author ImogiaGames, page marks it CC0. It has a genuine alpha channel.
- Visual row candidates (zero-based, image origin top-left): row 1 grey armor/guard, row 2 brown clothes/archer, row 4 blue armor/knight, row 6 white robe/healer, row 11 purple robe/mage. The source does not label these exact classes; these are visual descriptions only.
- Each main-sheet cell is 32 x 32 px. The main sheet background is opaque magenta RGB(255,0,255), alpha 255 throughout; Unity slicing does not remove it.
- For Unity Sprite rects (origin bottom-left), `y = 672 - (row + 1) * 32`: candidate first cells are row 4 `(0,512,32,32)`, row 11 `(0,288,32,32)`, row 6 `(0,448,32,32)`, row 1 `(0,608,32,32)`, row 2 `(0,576,32,32)`.
- Approximate layout: columns 0-3 one facing/pose group, 4-7 opposite-facing group, 8-10 side-facing frames; column 11 is usually empty or contains equipment. Review the sheet before choosing animation frames.
- The untouched original PNGs are preserved. Use color-key handling for the main sheet; the Goblin candidate has transparent pixels already.
