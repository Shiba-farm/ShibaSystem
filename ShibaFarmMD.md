# GEMINI.md — ShibaFarm Project Intelligence

Read this file completely before every session.
This is the single source of truth for architecture, conventions, and design intent.
If anything in this file conflicts with code you find in the project, surface the conflict and ask before resolving it on your own.

---

## 0. First-Time Setup — Scan Before You Code (CRITICAL)

**Context:** This project already has a substantial codebase built by another developer. Do not assume a blank slate.

Every time you start a new session, run this scan protocol before doing anything else:

1. **Read this GEMINI.md in full.**
2. **Scan Assets/_Project/Scripts/ (or equivalent)** — list all existing classes, their namespaces, and apparent responsibilities.
3. **Scan Assets/_Project/ScriptableObjects/ (or equivalent)** — list all SO types found.
4. **Identify existing implementations:** Check how the other developer implemented patterns (Singleton, EventBus, SO data, etc.). **Do not overwrite or duplicate existing systems.**
5. **Summarize what you found in 10–15 lines**, then ask: *"Anything outdated or missing from GEMINI.md I should know before continuing?"*
6. **Only after confirmation:** proceed with the assigned task.

This protocol prevents you from inventing architecture that already exists or breaking patterns the existing developer established.

---

## 1. Project Identity

| Field | Value |
| :--- | :--- |
| **Title** | ShibaFarm |
| **Engine** | Unity 6000.3.11f1 (3D) |
| **Render** | URP |
| **Genre** | Farming Simulation / Life Sim |
| **Camera** | Top-down orthographic (Stardew-style) |
| **Platform** | PC (Windows/Mac) — mobile later |
| **Language** | C# (.NET Standard 2.1) |
| **Unity AI pkg** | None (no Sentis, no ML-Agents) |

### Core Loop
Wake Up → Till / Water / Plant / Harvest → Sell at Market → Buy Seeds/Tools → Sleep → Season Advances

### Social Commentary (Design Intent — Do Not Dilute)
ShibaFarm is a farming game with a quiet critical voice:
* **Labor & exhaustion** — Stamina is finite. Overwork degrades the character visibly.
* **Land ownership** — You start on borrowed land and pay rent to HarvestCorp.
* **Community vs. isolation** — NPCs remember neglect. Relationships decay without attention.
* **Impermanence** — Crops die. Seasons end. Some losses are permanent.

*The slow pace, friction, and consequences are intentional design — not bugs. Do not smooth them out or add shortcuts without explicit discussion.*

---

## 2. Architecture

*After running the scan in Section 0, update this section if the real project structure differs. Mark updated lines with `[UPDATED vYYYY-MM-DD]`.*

### System Map
```text
┌────────────────────────────────────────────────────────┐
│                      GameManager                       │
│         (single entry point, holds system refs)        │
└──────────┬─────────────────────────┬───────────────────┘
           │                         │
   ┌───────▼────────┐       ┌────────▼────────┐
   │ GameStateMachine│       │    EventBus     │
   │ (State Pattern) │       │ (Observer/Pub-Sub)│
   └───────┬────────┘       └────────┬────────┘
           │                         │
  ┌────────▼─────────────────────────▼────────┐
  │                 Systems Layer              │
  │  TimeSystem  FarmSystem  InventorySystem   │
  │  EconomySystem  RelationshipSystem         │
  │  AudioSystem                               │
  └────────┬──────────────────────────────────┘
           │
  ┌────────▼──────────────────────────────────┐
  │          Data Layer (ScriptableObjects)    │
  │   CropData  ItemData  NPCData  SeasonData  │
  └───────────────────────────────────────────┘


```python
markdown_content = """# GEMINI.md — ShibaFarm Project Intelligence

Read this file completely before every session.
This is the single source of truth for architecture, conventions, and design intent.
If anything in this file conflicts with code you find in the project, surface the conflict and ask before resolving it on your own.

---

## 0. First-Time Setup — Scan Before You Code (CRITICAL)

**Context:** This project already has a substantial codebase built by another developer. Do not assume a blank slate.

Every time you start a new session, run this scan protocol before doing anything else:

1. **Read this GEMINI.md in full.**
2. **Scan Assets/_Project/Scripts/ (or equivalent)** — list all existing classes, their namespaces, and apparent responsibilities.
3. **Scan Assets/_Project/ScriptableObjects/ (or equivalent)** — list all SO types found.
4. **Identify existing implementations:** Check how the other developer implemented patterns (Singleton, EventBus, SO data, etc.). **Do not overwrite or duplicate existing systems.**
5. **Summarize what you found in 10–15 lines**, then ask: *"Anything outdated or missing from GEMINI.md I should know before continuing?"*
6. **Only after confirmation:** proceed with the assigned task.

This protocol prevents you from inventing architecture that already exists or breaking patterns the existing developer established.

---

## 1. Project Identity

| Field | Value |
| :--- | :--- |
| **Title** | ShibaFarm |
| **Engine** | Unity 6000.3.11f1 (3D) |
| **Render** | URP |
| **Genre** | Farming Simulation / Life Sim |
| **Camera** | Top-down orthographic (Stardew-style) |
| **Platform** | PC (Windows/Mac) — mobile later |
| **Language** | C# (.NET Standard 2.1) |
| **Unity AI pkg** | None (no Sentis, no ML-Agents) |

### Core Loop
Wake Up → Till / Water / Plant / Harvest → Sell at Market → Buy Seeds/Tools → Sleep → Season Advances

### Social Commentary (Design Intent — Do Not Dilute)
ShibaFarm is a farming game with a quiet critical voice:
* **Labor & exhaustion** — Stamina is finite. Overwork degrades the character visibly.
* **Land ownership** — You start on borrowed land and pay rent to HarvestCorp.
* **Community vs. isolation** — NPCs remember neglect. Relationships decay without attention.
* **Impermanence** — Crops die. Seasons end. Some losses are permanent.

*The slow pace, friction, and consequences are intentional design — not bugs. Do not smooth them out or add shortcuts without explicit discussion.*

---

## 2. Architecture

*After running the scan in Section 0, update this section if the real project structure differs. Mark updated lines with `[UPDATED vYYYY-MM-DD]`.*

### System Map

```

```text
GEMINI.md generated successfully.

```text
┌────────────────────────────────────────────────────────┐
│                      GameManager                       │
│         (single entry point, holds system refs)        │
└──────────┬─────────────────────────┬───────────────────┘
           │                         │
   ┌───────▼────────┐       ┌────────▼────────┐
   │ GameStateMachine│       │    EventBus     │
   │ (State Pattern) │       │ (Observer/Pub-Sub)│
   └───────┬────────┘       └────────┬────────┘
           │                         │
  ┌────────▼─────────────────────────▼────────┐
  │                 Systems Layer              │
  │  TimeSystem  FarmSystem  InventorySystem   │
  │  EconomySystem  RelationshipSystem         │
  │  AudioSystem                               │
  └────────┬──────────────────────────────────┘
           │
  ┌────────▼──────────────────────────────────┐
  │          Data Layer (ScriptableObjects)    │
  │   CropData  ItemData  NPCData  SeasonData  │
  └───────────────────────────────────────────┘

```

### Non-Negotiable Architecture Rules

* **GameManager is not a God Object.** It holds references only. No logic.
* **Systems do not call each other directly.** All cross-system communication goes through EventBus.
* **ScriptableObjects are read-only at runtime.** Mutable state lives in plain C# runtime classes.
* **MonoBehaviours are Views only.** No game logic. Translate events into visuals/audio.
* **No anonymous lambdas on EventBus.** Always use named methods — required for clean unsubscription and stack trace readability.
* **No `FindObjectOfType` at runtime.** Use Inspector injection or GameManager references.
* **No `#region` blocks.**

---

## 3. Design Patterns Reference

### 3.1 State Pattern

Two independent state machines:

1. **GameStateMachine** (game flow): `MainMenu → Loading → Playing → Paused → Sleeping → GameOver`
2. **PlayerStateMachine** (player behavior): `Idle → Moving → UsingTool → Interacting → Exhausted → Sleeping`

Never use `enum` + `switch` for state logic. Always polymorphic state classes inheriting from `IState`.

### 3.2 Observer Pattern — EventBus

* **Publish:** `EventBus.Publish(new CropHarvestedEvent { Crop = cropData, GoldValue = 24 });`
* **Subscribe:** Always in `OnEnable`/`OnDisable` pairs. Named handlers only.
* **Naming:** `[Subject][Verb]Event` — e.g. `CropHarvestedEvent`, `DayEndedEvent`.

### 3.3 Factory Pattern

`CropFactory` and `ItemFactory` create runtime instances from SO blueprints. Never call `Instantiate` for crops/items from gameplay code directly.

### 3.4 Object Pool

`PoolManager` handles particles, floating text, and drop items. Pre-warm pools on scene load.

### 3.5 Command Pattern

Every tool action implements `IFarmCommand`. Undo must be implemented even if unused.
Commands: `TillCommand`, `WaterCommand`, `PlantCommand`, `HarvestCommand`, `FertilizeCommand`.

### 3.6 Repository Pattern — Save/Load

`SaveRepository` is the only class that reads/writes disk. Nothing else touches persistence.

---

## 4. Folder Structure (Canonical Singular Architecture)

```text
Assets/
└── _Project/
    ├── Script/
    │   ├── Core/          — GameManager, GameStateMachine, EventBus, PoolManager
    │   ├── State/
    │   ├── System/        — TimeSystem, FarmSystem, etc.
    │   ├── Data/          — ScriptableObject definitions
    │   ├── Runtime/       — plain C# runtime state (CropInstance, TileState, ...)
    │   ├── Command/       — IFarmCommand + implementations
    │   ├── Factory/
    │   ├── Event/         — GameEvents.cs (all event structs)
    │   ├── View/          — MonoBehaviours: PlayerView, TileView, CropView, HUDView
    │   ├── UI/            
    │   └── Save/          
    ├── ScriptableObject/
    ├── Prefab/
    ├── Art/
    ├── Audio/
    └── Scene/

```

*Note: Folder names use strict singular casing to perfectly match the project design convention. If the actual project uses a different layout due to historical code, log it in Section 0 before creating files.*

---

## 5. Core Data Structures

* **CropData** (`ScriptableObject`): Read-only blueprints (mesh, stages, cost).
* **CropInstance** (Plain C#): Mutable runtime state (`CurrentStage`, `IsWatered`).
* **TileState** (Plain C#): 3D Grid position (Y=0), TileType (`Untilled`, `Tilled`, `Planted`), Crop reference.
* **InventorySlot**: Item data and quantity logic.

---

## 6. Core Events (GameEvents.cs)

* **Time:** `DayStartedEvent`, `DayEndedEvent`, `SeasonChangedEvent`
* **Farming:** `TileTilledEvent`, `TileWateredEvent`, `CropPlantedEvent`, `CropHarvestedEvent`, `CropWitheredEvent`
* **Player:** `PlayerStaminaChangedEvent`, `PlayerExhaustedEvent`, `PlayerGoldChangedEvent`
* **Economy:** `MarketPriceChangedEvent`, `RentDueEvent`
* **Relationship:** `NPCRelationshipChangedEvent`

---

## 7. Systems Summary

| System | Manages | Key Events Published |
| --- | --- | --- |
| **TimeSystem** | Day, season, day-end trigger | `DayStartedEvent`, `SeasonChangedEvent` |
| **FarmSystem** | Grid of TileState, crop growth | `CropWitheredEvent` |
| **InventorySystem** | Slot array | (pull model — UI reads directly) |
| **EconomySystem** | Gold, market prices, rent | `MarketPriceChangedEvent`, `RentDueEvent` |
| **RelationshipSystem** | NPC scores | `NPCRelationshipChangedEvent` |
| **AudioSystem** | Layered ambient + SFX | (listener only) |

*Time advances only on explicit day-end (player sleeps). No `Update()`-based time ticking.*

---

## 8. Upcoming Task Context: 3D Crop Planting System

The immediate next priority for the AI is implementing the crop planting system on 3D terrain.

**Key Design Requirements for this task:**

1. **Preparation:** Scan existing code first. Find out how the other developer handles tile detection (Tilemap? Custom 3D grid? Physics raycast?) before writing any planting logic.
2. **Command Pattern:** Planting uses `PlantCommand` (Command Pattern). Check if it exists before creating it.
3. **3D Grid Interaction:** The tile grid uses a flat XZ plane (Y=0). Raycasting from the camera determines the targeted tile.
4. **Visual Feedback:** Tile selection must highlight the targeted tile before planting.
5. **Validation:** Must check season, equipped tool, `Tilled` state, and seed inventory. All validation happens inside `PlantCommand.Execute()`.
6. **Outcomes:**
* *Success:* Publish `CropPlantedEvent`, deduct seed, transition tile state.
* *Failure:* Publish `PlantFailedEvent` with reason enum for UI feedback.


7. **Separation of Concerns:** `CropView` (MonoBehaviour) subscribes to growth events to update the 3D mesh. It does NOT hold crop logic.

---

## 9. Player Controller Design

`PlayerController` owns `PlayerStateMachine`. It translates input to state transitions **only**.
`Input` → `PlayerStateMachine` → `Active IState` → `IFarmCommand.Execute()` → `Mutates State` → `Publishes Event` → `Views react`.

*Stamina is a punishing mechanic. Do not add regen that negates exhaustion.*

---

## 10. Game Feel Standards

Every player action requires three feedback layers:

1. **Visual** — mesh/material change, particle burst, floating text.
2. **Audio** — distinct SFX per action.
3. **Data** — immediate number update.

*Screen shake is rare (withers, rent demand, storm). Not on every action. The calm is the feel.*

---

## 11. Coding Conventions

* **Naming:** PascalCase for Classes/Methods/Properties. `_camelCase` for private fields. `UPPER_SNAKE_CASE` for constants.
* **Comments:** English only. Explain *why*, not *what*. XML doc (`///`) on public APIs.
* **No TODOs:** Raise the issue explicitly instead of writing `// TODO`.
* **Lambda Policy:** No lambdas for EventBus subscriptions. Named methods only.
* **Async & Threading Policy (UniTask Integration):**
* **CRITICAL:** Explicitly prefer `UniTask` and `UniTaskVoid` over traditional Unity Coroutines or standard .NET Tasks (`System.Threading.Tasks`).
* Coroutines are strictly limited to simple, isolated, timed visual sequences inside a single View component.
* All asynchronous logic, resource loading, asset allocation, save operations, and state machine transitions must utilize `await UniTask`.
* Never use `async void` except for unavoidable Unity-native event callbacks (e.g., UI Button OnClick handlers). Use `async UniTaskVoid` instead for fire-and-forget tasks, and ensure proper error handling via `.Forget()`.


* **Unity Lifecycle:** `Awake` (internal refs), `Start` (request data), `OnEnable`/`OnDisable` (EventBus), `OnDestroy` (unsubscribe).

---

## 12. What You Must Not Do (Strict Rules)

* **DO NOT** overwrite existing systems built by the other developer without asking.
* **DO NOT** use `FindObjectOfType` at runtime.
* **DO NOT** subscribe anonymous lambdas to EventBus.
* **DO NOT** put game logic in MonoBehaviours — Views are dumb.
* **DO NOT** mutate ScriptableObject fields at runtime.
* **DO NOT** add regen/skip mechanics that remove stamina or rent pressure.
* **DO NOT** write `#region` blocks.
* **DO NOT** skip the scan protocol in Section 0.

---

## 13. Checklist Before Writing Any Code

* [ ] Did I complete the scan protocol (Section 0) and review the other dev's work?
* [ ] Does this class have one clear responsibility?
* [ ] Does it communicate via EventBus rather than direct calls?
* [ ] Is mutable state in a plain C# class, not an SO?
* [ ] Is the MonoBehaviour purely presentational?
* [ ] Are all event subscriptions using named methods with matching Unsubscribe?
* [ ] Are all async operations driven by UniTask according to the policy?
* [ ] Are comments in English, explaining *why* not *what*?

> *ShibaFarm — A farm that remembers. A world that doesn't wait.*"""