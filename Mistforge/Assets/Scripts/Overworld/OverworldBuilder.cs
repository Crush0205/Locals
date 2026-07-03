using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace Mistforge
{
    /// Builds an overworld map (Rimwatch or the Hushfields) from an ASCII
    /// layout at runtime using placeholder tiles, and routes tile events
    /// (forge, rest, gates, encounter grass). Once real tilesets land, replace
    /// the runtime painting with authored Tile Palette tilemaps and keep the
    /// event routing.
    ///
    /// Glyphs — town: '#' wall, '.' ground, 'S' spawn, 'W' well (blocks),
    /// 'F' forge door, 'R' rest/inn door, 'E' gate to the Hushfields.
    /// Field: '#' treeline, '.' safe path, ',' encounter grass, 't' dead tree,
    /// 's' standing stones, 'o' mist pool (decor), 'T' gate back to town.
    public class OverworldBuilder : MonoBehaviour
    {
        public enum CellEvent { None, Forge, Rest, GateToField, GateToTown }

        public MapKind map = MapKind.Town;

        static readonly string[] TownRows =
        {
            "####################",
            "#......####........#",
            "#......####........#",
            "#......#R##..####..#",
            "#............####..#",
            "#............#F##..#",
            "#..................#",
            "#........W.........#",
            "#........S.........#",
            "#..##.........##...#",
            "#..##.........##...E",
            "#..................#",
            "#..................#",
            "####################",
        };

        static readonly string[] FieldRows =
        {
            "########################",
            "#,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,t,,,,,,,,,,,,oo,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,ss,,,,,,,,,,,,,,,,,,#",
            "#,,ss,,,,,,,,..,,,,,,,,#",
            "#,,,,,,,,....,,,,,,,,,,#",
            "T......................#",
            "#,,,,....,,,,,,,,......#",
            "#,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,tt,,,,,,#",
            "#,,,oo,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,ss,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,#",
            "########################",
        };

        readonly HashSet<Vector3Int> blocked = new HashSet<Vector3Int>();
        readonly HashSet<Vector3Int> grass = new HashSet<Vector3Int>();
        readonly Dictionary<Vector3Int, CellEvent> events = new Dictionary<Vector3Int, CellEvent>();

        int width;
        int height;
        Vector3Int defaultSpawn;
        Vector3Int gateCell;
        EncounterTrigger encounterTrigger;
        ForgeUI forgeUI;

        void Awake()
        {
            var gm = GameManager.Ensure();
            string[] rows = map == MapKind.Town ? TownRows : FieldRows;
            height = rows.Length;
            width = rows[0].Length;

            BuildTilemap(rows);
            var camera = BuildCamera();

            if (map == MapKind.Field)
                encounterTrigger = gameObject.AddComponent<EncounterTrigger>();
            if (map == MapKind.Town)
            {
                var forgeGO = new GameObject("ForgeUI");
                forgeUI = forgeGO.AddComponent<ForgeUI>();
            }

            var player = SpawnPlayer(gm);
            camera.GetComponent<CameraFollow>().target = player.transform;

            MistDrift.Scatter(transform, new Rect(0, 0, width, height), map == MapKind.Field ? 5 : 3);
        }

        void BuildTilemap(string[] rows)
        {
            var gridGO = new GameObject("Grid");
            gridGO.AddComponent<Grid>();
            var mapGO = new GameObject("Tilemap");
            mapGO.transform.SetParent(gridGO.transform, false);
            var tilemap = mapGO.AddComponent<Tilemap>();
            var renderer = mapGO.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;

            var tiles = new Dictionary<char, TileBase>();
            for (int r = 0; r < height; r++)
            {
                int y = height - 1 - r;
                for (int x = 0; x < width; x++)
                {
                    char glyph = rows[r][x];
                    var cell = new Vector3Int(x, y, 0);
                    tilemap.SetTile(cell, TileFor(glyph, tiles));
                    Classify(glyph, cell);
                }
            }
        }

        void Classify(char glyph, Vector3Int cell)
        {
            switch (glyph)
            {
                case '#':
                case 'W':
                case 't':
                case 's':
                    blocked.Add(cell);
                    break;
                case ',':
                    grass.Add(cell);
                    break;
                case 'S':
                    defaultSpawn = cell;
                    break;
                case 'F':
                    events[cell] = CellEvent.Forge;
                    break;
                case 'R':
                    events[cell] = CellEvent.Rest;
                    break;
                case 'E':
                    events[cell] = CellEvent.GateToField;
                    gateCell = cell;
                    break;
                case 'T':
                    events[cell] = CellEvent.GateToTown;
                    gateCell = cell;
                    break;
            }
        }

        TileBase TileFor(char glyph, Dictionary<char, TileBase> cache)
        {
            if (cache.TryGetValue(glyph, out var cached)) return cached;

            Color color;
            bool speckle = false;
            bool town = map == MapKind.Town;
            switch (glyph)
            {
                case '#': color = town ? new Color(0.28f, 0.26f, 0.30f) : new Color(0.20f, 0.30f, 0.22f); break;
                case 'W': color = new Color(0.40f, 0.45f, 0.55f); break;
                // Warm gold/amber reserved for interactables (§3.5).
                case 'F': color = new Color(0.85f, 0.60f, 0.20f); break;
                case 'R': color = new Color(0.75f, 0.55f, 0.35f); break;
                case 'E':
                case 'T': color = new Color(0.70f, 0.62f, 0.30f); break;
                case ',': color = new Color(0.32f, 0.44f, 0.28f); speckle = true; break;
                case 't': color = new Color(0.35f, 0.28f, 0.22f); break;
                case 's': color = new Color(0.50f, 0.50f, 0.52f); break;
                case 'o': color = new Color(0.60f, 0.68f, 0.72f); break;
                default: // '.', 'S' — walkable ground
                    color = town ? new Color(0.45f, 0.50f, 0.38f) : new Color(0.55f, 0.47f, 0.35f);
                    speckle = town;
                    break;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = PlaceholderArt.Centered(16, 16, color, border: false, speckle: speckle);
            tile.colliderType = Tile.ColliderType.None;
            cache[glyph] = tile;
            return tile;
        }

        Camera BuildCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(width / 2f, height / 2f, -10f);

            // §2: pixel-perfect, 320x180 reference, PPU 16, integer scaling.
            var pixelPerfect = camGO.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 16;
            pixelPerfect.refResolutionX = 320;
            pixelPerfect.refResolutionY = 180;
            pixelPerfect.upscaleRT = true;
            pixelPerfect.pixelSnapping = true;

            var follow = camGO.AddComponent<CameraFollow>();
            follow.bounds = new Rect(0, 0, width, height);
            return cam;
        }

        PlayerController SpawnPlayer(GameManager gm)
        {
            Vector3Int spawn = defaultSpawn;
            string sceneName = SceneManager.GetActiveScene().name;

            if (gm.hasPendingCellSpawn && gm.pendingSpawnScene == sceneName)
            {
                spawn = gm.pendingSpawnCell;   // returning from battle
                gm.hasPendingCellSpawn = false;
            }
            else if (gm.spawnAtGate)
            {
                spawn = gateCell;              // arriving through a gate
                gm.spawnAtGate = false;
            }
            else if (map == MapKind.Field)
            {
                spawn = gateCell;
            }

            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>();
            var kade = gm.db.Character("kade");
            // 16x24 overworld frame (§3.1).
            sr.sprite = PlaceholderArt.Solid(16, 24, kade != null ? kade.placeholderColor : new Color(0.85f, 0.4f, 0.3f));
            sr.sortingOrder = 10;
            var player = go.AddComponent<PlayerController>();
            player.Init(this, spawn);
            return player;
        }

        public bool IsBlocked(Vector3Int cell)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height) return true;
            return blocked.Contains(cell);
        }

        public void OnPlayerArrived(Vector3Int cell, PlayerController player)
        {
            if (events.TryGetValue(cell, out var ev))
            {
                switch (ev)
                {
                    case CellEvent.Forge:
                        forgeUI.Open();
                        return;
                    case CellEvent.Rest:
                        GameManager.I.HealParty();
                        FloatingText.Spawn(player.transform.position + Vector3.up * 2f,
                            "The party rests. HP restored!", new Color(0.6f, 0.95f, 0.6f), 6f);
                        return;
                    case CellEvent.GateToField:
                        SceneFlow.Ensure().TravelThroughGate(SceneFlow.FieldScene);
                        return;
                    case CellEvent.GateToTown:
                        SceneFlow.Ensure().TravelThroughGate(SceneFlow.TownScene);
                        return;
                }
            }

            if (map == MapKind.Field && grass.Contains(cell))
                encounterTrigger.TryStartEncounter(SceneManager.GetActiveScene().name, cell);
        }
    }
}
