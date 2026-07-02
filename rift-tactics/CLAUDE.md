# Rift Tactics — Project Brief

Original turn-based mobile RPG. Directional-combo battle system (input High/Low/Left/Right
to chain hits into named "Arts"), inspired in spirit by classic PS1-era combo-input battle
systems but with entirely original world, characters, and mechanics.

Two working prototypes already exist in `reference/` — read them before writing new code,
they contain the actual game logic and should be ported, not redesigned from scratch:

- `reference/rift-tactics-combat.html` — full 2D combat prototype. Single-file vanilla JS.
  Contains the complete data model (heroes, enemies, Arts combo table), the turn/round state
  machine, damage calc, and UI. This is the source of truth for game rules.
- `reference/kael-3d-rig.html` — Three.js proof-of-concept showing one hero (Kael) as a
  hand-built low-poly rig (primitives, not a real model) with idle/attack/guard/hit/death
  animation states driven by a simple time-based state machine. This is the source of truth
  for how animation states should feel, not for the geometry itself (see Asset Plan below).

## Goal

Turn this into a real project: proper file structure, real 3D character models (not
hand-built boxes), skeletal animation instead of hand-tweened rotations, and a build that
can eventually ship as a mobile web app / PWA.

## Recommended stack

- **Vite** + **React** + **TypeScript**
- **@react-three/fiber** (React renderer for Three.js) + **@react-three/drei** (gives you
  `useGLTF`, `useAnimations`, camera controls, etc. for free — much less boilerplate than
  raw Three.js)
- **zustand** or plain React context for game state (turn phase, party, enemies, log) — the
  state shape in the prototype's `state` object is a good starting point
- Physics only if/when hit-reactions need it — **@react-three/cannon** or **@react-three/rapier**.
  Not needed for v1.

## Asset plan (this is the actual blocker on "better models")

Claude can't generate 3D assets. The path to real character models:

1. **Mixamo** (mixamo.com, free with Adobe login) — rigged humanoid base models plus a huge
   animation library (idle, sword attack, hit reaction, death, block). Export as `.glb`/`.fbx`.
   This is the fastest path to real skeletal animation.
2. **Kenney.nl** — free CC0 low-poly asset packs if we want to stay in a stylized-but-polished
   low-poly look rather than realistic humanoids.
3. AI 3D generation (Meshy, Tripo3D, Luma Genie, etc.) if a more custom look is wanted —
   generate externally, export GLB, drop into `/public/models/`.

Once a `.glb` lands in the project, loading + animating it via `@react-three/drei`'s
`useGLTF` + `useAnimations` is straightforward — ask Claude Code to wire it in once you have
a file.

## Data model (port directly from the prototype, don't redesign)

```
Heroes: Kael Ashborn (Blade), Rin Vasker (Twinblade), Mira Thorncall (Spiritcaller)
  each has: hp, maxAp, speed, power, and a "split" object weighting damage per direction
  (this is what makes each hero feel different to play — Kael is balanced, Rin favors
  Left/Right, Mira favors High)

Enemies: Ashclaw Stalker (feral, fast), Husk Sentinel (armored), Wraith of the Hollow (spectral)

Arts (named combos), exact sequence match against ['H','Lo','L','R']:
  H H H          -> Meteor Fall     x2.3
  L R L R        -> Flurry Chain    x2.1
  H Lo H         -> Rising Tempest  x1.85
  Lo Lo          -> Ground Break    x1.4 (chance to stun)
  L R            -> Twin Strike     x1.45
  R L            -> Crosscut        x1.45
  H H            -> Skyfall Cleave  x1.35
```

Full damage calc, turn resolution order (speed-sorted), guard mechanic, and armor reduction
are all implemented in `reference/rift-tactics-combat.html` — copy the logic functions
(`calcHeroDamage`, `matchArt`, the round resolution loop) rather than reimplementing.

Animation states per character (from the 3D prototype): `idle`, `attack`, `guard`, `hit`,
`death`. Keep this state machine shape — it maps cleanly onto Mixamo animation clips once
real models are in.

## Suggested build order

1. Scaffold Vite + React + TS + react-three-fiber project
2. Port the game state/turn logic from the HTML prototype into React state (no 3D yet —
   get the rules working headless/with placeholder boxes first)
3. Get one Mixamo model loading and idling in the scene
4. Wire animation state machine to real animation clips via `useAnimations`
5. Repeat for the other two heroes and three enemies
6. Polish: camera framing per turn, hit VFX, UI overlay for the combo input pad
7. (Later) overworld / exploration layer, PWA packaging for mobile

## Notes on tone/scope

Keep everything original — no reused character names, art, music, or story beats from any
existing commercial game. The directional-combo *mechanic* is the inspiration; everything
else (world, characters, enemies, Art names) is invented for this project and should stay that way.
