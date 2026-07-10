# UnityShiba — Project File Structure

All game content lives under `Assets/Project/`. Everything outside that folder (third-party assets, plugins, packages) is considered external and should not be modified unless necessary.

---

## Top-Level Layout

```
Assets/Project/
├── Animations/
├── Audio/
├── Editor/
├── Environment/
├── Materials/
├── Models/
├── Prefabs/
├── SaveData/
├── Scenes/
├── ScriptableObjects/
├── ScriptMain/
├── Shaders/
├── Textures/
└── UI/
```

---

## Animations

Holds all Unity Animator Controllers, Animation Clips, and Avatar Masks.

```
Animations/
├── Characters/
│   ├── Animal/          — idle, walk clips for farm animals
│   ├── Enemies/
│   │   └── Slime/       — slime attack, hurt, death clips
│   ├── NPC/             — NPC idle and talk animations
│   └── Player/          — player locomotion, tool use, combat clips
├── Environment/
│   ├── Dungeon/         — dungeon environment animations (doors, traps)
│   └── Farm/
│       ├── Moon/        — moon cycle animation
│       └── Sun/         — sun arc animation for day/night
├── Interaction Object/  — animations for interactable world objects (chests, beds)
└── Items/
    ├── Base/            — generic item animations
    ├── Crop/            — crop growth stage transitions
    ├── Food/            — food item animations
    ├── Other/           — miscellaneous item animations
    └── Tools/           — tool swing/use animations (hoe, axe, etc.)
```

---

## Audio

All sound assets. Separated into music tracks and sound effects.

```
Audio/
├── Music/   — background music tracks per scene/mood (farm day, dungeon, menu)
└── SFX/     — one-shot sound effects (footsteps, item pickup, tool use, UI clicks)
```

---

## Editor

Editor-only scripts. These are excluded from builds automatically by Unity.

```
Editor/
└── BuildWithShaderStrip.cs   — custom build pipeline that strips S_Rock shader variants
                                to avoid the DX11 ps_4_0 sampler limit build error
```

---

## Environment

Scene-level environment data — lighting, terrain, shaders, and post-processing.

```
Environment/
├── Cubemaps/     — reflection cubemaps used by environment materials
├── Lightning/    — lighting data assets and baked lightmap settings
├── Navmesh/      — NavMesh baked data for NPC and enemy pathfinding
├── Shader/
│   └── Water/    — custom water shader graphs
├── Terrain/
│   ├── Data/     — Unity terrain data assets (.asset files)
│   └── Layers/   — terrain layer assets (grass, dirt, sand textures)
└── Volumes/      — URP post-processing Volume profiles (bloom, color grading, etc.)
```

---

## Materials

All Material assets. Mirrors the same category structure as Models and Textures so every material is easy to locate by subject.

```
Materials/
├── Characters/
│   ├── Animal/     — animal character materials
│   ├── Enemies/    — enemy materials
│   ├── NPC/        — NPC materials
│   └── Player/     — player character materials
├── CursurVisual/   — material for the farming tile cursor (green/red grid overlay)
├── Environment/
│   ├── Dungeon/    — dungeon wall, floor, prop materials
│   └── Farm/       — farm terrain, soil, fence materials
├── Items/
│   ├── Base/       — resource/base item materials
│   ├── Crop/       — crop plant materials
│   ├── Food/       — food item materials
│   ├── Other/      — miscellaneous item materials
│   └── Tools/      — tool materials (hoe, axe, watering can)
├── Other/          — materials that don't fit another category
└── VFX/
    └── Footsteps/
        ├── Materials/  — footstep decal materials
        ├── Meshes/     — footstep decal meshes
        └── Prefabs/    — assembled footstep VFX prefabs
```

---

## Models

Raw 3D model files (.fbx, .obj). Same category tree as Materials and Textures.

```
Models/
├── Characters/
│   ├── Animal/         — animal meshes
│   ├── Enemies/
│   │   └── Slime/      — slime enemy mesh + rig
│   ├── NPC/
│   │   └── Backup/     — older NPC model versions kept for reference
│   └── Player/         — player mesh + rig
├── Environment/
│   ├── Dungeon/        — dungeon architecture meshes (walls, floors, pillars)
│   └── Farm/
│       └── Backup/     — older farm prop versions
├── Interaction Object/ — interactive world prop meshes (bed, workbench, sell box)
├── Items/
│   ├── Base/           — raw resource meshes (stone, wood, ore)
│   ├── Crop/           — crop plant meshes per growth stage
│   ├── Food/           — food item meshes
│   ├── Other/          — miscellaneous item meshes
│   └── Tools/
│       └── Backup/     — older tool model versions
└── Other/              — miscellaneous models that don't fit another category
```

---

## Prefabs

Fully assembled, ready-to-use GameObjects. The most referenced folder during scene building.

```
Prefabs/
├── Characters/
│   ├── Animal/             — spawnable farm animals
│   ├── Enemies/
│   │   └── Slime/          — slime enemy prefab with AI + health
│   ├── NPC/                — NPC characters with dialogue + interaction
│   └── Player/             — networked player prefab (PlayerItemUser, StatManager, etc.)
├── Environment/
│   ├── Dungeon/            — dungeon room and prop prefabs
│   └── Farm/
│       ├── Backup/         — older farm prefab versions
│       ├── Decoration/     — decorative farm props (fences, signs, barrels)
│       └── Trees/          — choppable tree prefabs (ChoppableCut_Tree)
├── Interaction Object/     — interactable world objects (bed, workbench, sell box, chest)
├── Items/
│   ├── EquipItem/          — in-hand 3D visuals spawned by PlayerHeldItem
│   │   ├── Base/
│   │   ├── Crop/
│   │   ├── Food/
│   │   ├── Other/
│   │   ├── Pickups/        — floating pickup item visuals
│   │   └── Tools/
│   └── WorldItem/          — items dropped/spawned in the world (WorldItem component)
│       ├── Base/
│       ├── Crop/
│       ├── Food/
│       ├── Other/
│       ├── Pickups/
│       └── Tools/
├── Network/                — NetworkManager, relay, and netcode infrastructure prefabs
├── Other/                  — miscellaneous prefabs (VFX, particles, utility objects)
└── UI/                     — UI canvas prefabs for quick scene placement
```

---

## SaveData

Runtime save files written by the game. Not source-controlled — generated at runtime.

```
SaveData/   — .json or binary save files written by SaveSystem / SaveLoadManager
```

---

## Scenes

All Unity scene files (.unity), organized by game area.

```
Scenes/
├── Dungeon/      — procedurally generated dungeon scene
├── Farm/         — main farm / overworld scene
├── Main/
│   └── MainGame/ — MainGame scene (index 0 in build — first scene loaded)
└── UI/           — standalone UI scenes (main menu, loading screen)
```

---

## ScriptableObjects

All ScriptableObject data assets (.asset files). Split into two groups: per-entity data and system-wide configuration.

```
ScriptableObjects/
├── Characters/
│   ├── Animal/               — animal stat and behaviour data
│   ├── Enemies/
│   │   └── Slime/            — slime enemy config (damage, speed, drops)
│   ├── NPC/                  — NPC dialogue and identity data
│   └── Player/               — player base stats (PlayerDataSO, PerkDataSO)
├── Crafting/                 — crafting recipe ScriptableObjects
├── Items/
│   ├── Base/                 — ItemSO assets for raw resources
│   ├── Crop/                 — SeedItemSO + CropSO assets (Beet, Onion, etc.)
│   ├── Food/                 — FoodItemSO assets
│   ├── Other/                — miscellaneous ItemSO assets
│   └── Tools/                — ToolItemSO assets (Hoe, Axe, etc.)
├── Signals/                  — ScriptableObject event signals shared across systems
│                               (HeldItemSignal, WorldTimeSignal, InventoryDataSignal, etc.)
└── System/
    ├── Crafting/
    │   └── Recipe/           — CraftingRecipeSO assets
    ├── DungeonConfig/        — DungeonConfigSO and DungeonFloorData assets
    ├── FarmHelper/           — FarmHelperSO placement configs
    ├── Fishing/              — FishingZoneSO assets
    └── Stat/                 — ItemStatDataSO assets
```

---

## ScriptMain

All custom game scripts. This is the main code folder for the project. Every system has its own subfolder.

```
ScriptMain/
├── AI/                    — enemy and NPC AI behaviour scripts
├── AnimationController/   — AnimationEventRelay: bridges Animator events to game logic
│                            (OnActionImpact, OnActionAnimationFinished)
├── Bed & Summary/         — sleep/rest system and end-of-day summary UI scripts
├── Camera/                — CameraManager, farm camera follow, Cinemachine helpers
├── Crafting/              — CraftingManager, CraftingRecipeSO, crafting UI scripts
├── Currency/              — CurrencyData, CurrencySignal, CurrencyManager
├── Debt/                  — DebtManager (new networked system), DebtPayUI,
│                            PunishmentPanelUI, NPCDebt interaction, LostItemRowUI
├── Dungeon/               — DungeonGenerator (BSP), DungeonManager, floor transitions,
│                            ore nodes, enemy AI, DungeonConfigSO
├── Editor/                — Editor-only utility scripts (build tools, inspectors)
├── Farming/               — HoeTillingSystem, TileCursor, SoilTile, SoilGridSpawner,
│                            PlantingCursorController, PlantCommand, TilledGroundSystem
├── Interaction & Movement — PlayerController, PlayerItemUser, PlayerHeldItem,
│                            HeldItemSignal, BedInteraction, WorkbenchInteraction,
│                            SellBox, InteractController, PlayerMagnet
├── Inventory/             — InventoryData (networked), InventoryDataRegistry,
│                            InventoryDataSignal, slot UIs, drag-and-drop system
├── Item/                  — ItemSO base class, ToolItemSO, SeedItemSO, FoodItemSO,
│                            ResourceItemSO, WearableItemSO, IUsable, IEquippable,
│                            WorldItem, NetworkItemSpawner
├── Manager/               — GameDataManager, GameSceneBootstrapper, Bootstrapper,
│                            WorldTimeManager, WorldTimeSignal, InGameUIManager
├── Network/               — ClientNetworkTransform, ClientNetworkAnimator
├── NPC/                   — NPC interaction and dialogue management
├── SaveLoad/              — SaveLoadManager, ISaveStorage, LocalFileStorage,
│                            SteamCloudStorage, save/load interfaces
├── Selling/               — ShopManager, SellBox, cart data, selling UI scripts
├── Sound/                 — SoundtrackManager for music transitions
├── Stat/                  — StatManager, NetworkStat, NetworkKnowledgeStat,
│                            PlayerDataSO, PerkDataSO, ItemStatDataSO
├── Time/                  — WorldTimeManager (game clock), TimeOfDaySystem
├── UI/                    — General UI scripts
│   ├── GeneralInfoPanel/  — floating info panel component
│   ├── MainMenu/          — main menu, settings, game mode selection UI
│   └── Prompt/            — InteractPromptUI (contextual interact hints)
├── VFX/                   — FootstepVFXController and other visual effect scripts
└── World/                 — IDamageable interface, RockObject and other world objects
```

---

## Shaders

Custom shader files and Shader Graph assets created specifically for this project (as opposed to third-party shaders which live in their own asset folders).

```
Shaders/   — project-specific .shadergraph and .hlsl files
```

---

## Textures

All texture image assets. Mirrors the same category structure as Models and Materials.

```
Textures/
├── Characters/
│   ├── Animal/
│   ├── Enemies/
│   │   └── Slime/
│   ├── NPC/
│   └── Player/
├── Environment/
│   ├── Dungeon/
│   └── Farm/
├── Interaction Object/
├── Items/
│   ├── Base/
│   ├── Crop/
│   ├── Food/
│   ├── Other/
│   └── Tools/
├── Other/
└── UI/               — textures used in UI elements (backgrounds, frames, icons)
```

---

## UI

UI-specific assets that are not scripts — fonts, icon sprites, and layout sprites.

```
UI/
├── Fonts/       — TMP font assets and font atlases
├── Icons/       — item icons, stat icons, and other in-game icon sprites
├── Other/       — miscellaneous UI assets (loading bars, decorative elements)
└── Sprites/     — UI sprite sheets and individual sprites
    └── Characters/
        ├── Animal/
        ├── Enemies/
        │   └── Slime/
        ├── NPC/
        └── Player/   — player portrait and expression sprites
```

---

## Naming Convention

| Asset Type | Convention | Example |
|---|---|---|
| Scripts | PascalCase | `PlayerItemUser.cs` |
| ScriptableObjects | PascalCase + type suffix | `BeetSeed_SO` |
| Prefabs | PascalCase | `Player_Network` |
| Materials | M_ prefix | `M_Player_Body` |
| Textures | T_ prefix | `T_Player_Diffuse` |
| Animations | A_ prefix | `A_Player_HoeSwing` |
| Scenes | PascalCase | `MainGame` |

---

## Key Systems Cross-Reference

| System | Scripts | ScriptableObjects | Prefabs |
|---|---|---|---|
| Farming | `ScriptMain/Farming/` | `ScriptableObjects/Items/Crop/` | `Prefabs/Environment/Farm/` |
| Inventory | `ScriptMain/Inventory/` | `ScriptableObjects/Signals/` | `Prefabs/UI/` |
| Debt | `ScriptMain/Debt/` | — | `Prefabs/Characters/NPC/` |
| Dungeon | `ScriptMain/Dungeon/` | `ScriptableObjects/System/DungeonConfig/` | `Prefabs/Environment/Dungeon/` |
| Crafting | `ScriptMain/Crafting/` | `ScriptableObjects/System/Crafting/` | `Prefabs/UI/` |
| Time | `ScriptMain/Time/` | `ScriptableObjects/Signals/WorldTimeSignal` | — |
| Items | `ScriptMain/Item/` | `ScriptableObjects/Items/` | `Prefabs/Items/` |
