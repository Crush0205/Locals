# Mistforge — Unity Vertical Slice

2D top-down JRPG vertical slice: one town (Rimwatch), one field (The Hushfields) with
random encounters, a full directional-combo battle scene, and a Forge crafting system
with rarity tiers and the Emberwake set. Built per the Mistforge handoff spec; all IP
is original.

## Getting started

1. **Open the project** in Unity **2022.3 LTS or newer** (this folder — `Mistforge/` —
   is the project root). Required packages (2D Tilemap, 2D Pixel Perfect, Input System,
   TextMeshPro, URP, uGUI) are pinned in `Packages/manifest.json` and install
   automatically on first open.
   - If Unity asks *"Enable the new Input System backends?"* — answer **Yes** (or pick
     "Both"). The input layer compiles against whichever handler is active, so either
     choice works.
2. Run the menu item **Mistforge → Generate All (Data + Scenes)**. This:
   - imports TMP Essential Resources if missing,
   - creates every ScriptableObject from the spec's §8 data tables under
     `Assets/ScriptableObjects/`, plus the `GameDatabase` in `Assets/Resources/`,
   - creates and saves `Boot`, `Town_Rimwatch`, `Field_Hushfields`, and `Battle`
     scenes and registers them in Build Settings.
   The generator is idempotent — re-run it any time after tweaking values in code.
3. Open `Assets/Scenes/Boot.unity` and press **Play**.

## Controls

| Context | Input |
|---|---|
| Overworld movement | WASD / Arrows (grid-locked, one tile per step, cardinal only) |
| Battle: queue combo inputs | WASD / Arrows or the on-screen L/U/D/R buttons (max 5) |
| Battle: confirm attack / Art | Enter / Space or the Attack button |
| Battle: Focus (guard, +16 AP) | F or the Focus button |
| Battle: clear queue | Backspace / C |
| Battle: change target | Click an enemy |
| Forge: close | Esc or the Close button |

Walk onto the **amber forge doorway** to open the Forge, the **inn doorway** to fully
heal, and the **gold gate tiles** to travel between Rimwatch and the Hushfields.
Stepping in tall grass rolls a ~16% encounter chance (Inspector-tunable on the field's
`EncounterTrigger`).

## Placeholder art

Everything renders with runtime-generated bordered color blocks at the exact frame
sizes from the sprite spec (§3): 16×24 overworld characters, 32×48 battle characters,
32×32 / 40×40 enemies, 16×16 tiles, PPU 16, pixel-perfect camera at 320×180. Swapping
in real sheets means: import the sheets, add Animator controllers with the spec's
parameters (`Direction` int, `IsMoving` bool — `PlayerController` already drives them
and flips X for left), and replace the `PlaceholderArt` sprites / runtime tile painting
in `OverworldBuilder` with authored Tile Palette tilemaps. Milestone 4 in the build
order (§10).

## Where things live (spec § → code)

| Spec | Implementation |
|---|---|
| §2 project setup | `Packages/manifest.json`, pixel-perfect cameras built in `OverworldBuilder`/`BattleManager` |
| §4 overworld, player controller | `Scripts/Overworld/OverworldBuilder.cs` (ASCII maps + tile events), `PlayerController.cs`, `EncounterTrigger.cs`, `CameraFollow.cs`, `MistDrift.cs` |
| §5 scene transitions | `Scripts/Core/SceneFlow.cs` (+ `EncounterData`, `ScreenFader`), state cache on `GameManager` |
| §6 battle | `Scripts/Battle/BattleManager.cs` (phase loop §6.4, victory/defeat §6.5), `ComboInput.cs` (combo trail + live Art highlight §6.2), `ArtMatcher.cs`, `TurnController.cs` (enemy AI), `BattleActor.cs` (bars, flash, lunge), `CameraShake.cs`, `HitPause.cs`, `FloatingText.cs` (damage numbers) |
| §7 Forge | `Scripts/UI/ForgeUI.cs` — in-town overlay Canvas with materials, recipes, paperdoll, set-bonus readout |
| §8 data tables | ScriptableObject classes in `Scripts/Data/`, values authored by `Assets/Editor/MistforgeGenerator.cs` into `Assets/ScriptableObjects/` |
| §9 formulas | Normal/Art damage + AP gains in `BattleManager`, enemy damage in `TurnController`, drops in `BattleManager.Victory()`, item math in `Items/ItemGenerator.cs` |

## Systems notes

- **Rarity quality boost:** each rarity has a `weightPerBoost` (Common −22, Rare +14,
  Set +8, floor 4). Base weights 52/29/15/4 become 30/29/29/12 at boost 1 — the POC's
  `rollRarity(qualityBoost)` curve. Boost comes from the highest material tier in a
  recipe, +2 if you toggle **Infuse Gleaming Core** in the Forge, or from Stone Cub's
  +1 on battle drops.
- **Emberwake set:** 2pc +15% Art Power; 3pc +20% AP Gain and attacks have a 25%
  chance to ignite (3 damage at the enemy's turn start, 2 turns).
- **Defeat is soft-fail:** party revives in town at 40% HP, −20% gold.
- **Battles start at 12 AP** (`BattleManager.startingAP`); HP persists between fights;
  the inn heals to full.
- The Battle scene can be played standalone in the editor — it spawns a debug
  Wisp + Hound encounter when no overworld hand-off exists.

## Open questions from the handoff (§11) — decisions taken

- **Grid-locked vs. smooth movement:** grid-locked, per the doc's recommendation.
  The animator contract (`Direction`/`IsMoving`) is unaffected if this changes.
- **Forge scene vs. overlay:** overlay Canvas in town (no scene load), per the doc's
  recommendation. There is no `Forge.unity`.
- **Battle backgrounds:** currently a flat backdrop using the field palette; the
  recommended "reuse tileset assets at battle scale" approach slots into
  `BattleManager.BuildBackdrop()` once real tilesets exist.

## Not in this slice (spec marks these optional / later milestones)

Background townsfolk NPCs, Cinemachine, save system (the inn is wired as the save
point location), SFX pass, shader-based transition wipe (simple fade implemented),
and real sprite sheets / animation clips (milestone 4).
