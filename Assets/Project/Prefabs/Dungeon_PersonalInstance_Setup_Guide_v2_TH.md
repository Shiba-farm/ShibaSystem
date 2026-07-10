# คู่มือติดตั้งระบบ Dungeon Personal-Instance — v2 (เริ่มต้นจากศูนย์)

**สถานะ: ฉบับสมบูรณ์ ใช้แทน `Dungeon_PersonalInstance_Setup_Guide.md` ฉบับเดิมทั้งหมด**
อ้างอิงจากการอ่านโค้ดปัจจุบันทั้งหมด (`DungeonManager.cs`, `PlayerDungeonState.cs`, `DungeonEntrance.cs`, `DungeonLadder.cs`, `DungeonDeathHandler.cs`, `PlayerHealth.cs`, `DungeonFloorTransition.cs`, `DungeonInstanceMember.cs`, `NetworkSaveableBehaviour.cs`, `SaveLoadManager.cs`, `DungeonConfigSO.cs`) รวมถึงไฟล์ scene/prefab/asset จริง (`Dungeon.unity`, `MainGame.unity`, `Ladder.prefab`, `Shiba.prefab`, `DefaultNetworkPrefabs.asset`, `DungeonConfig.asset`, `GlobalManagers.prefab`, `InGameNetworkManager.prefab`, `EditorBuildSettings.asset`)

คู่มือนี้เขียนภายใต้สมมติฐานว่า **ยังไม่มีการตั้งค่าอะไรไว้ถูกต้องเลย** และจะอธิบายขั้นตอนตามลำดับการพึ่งพากัน ระหว่างเขียนคู่มือนี้ **ไม่มีการแก้ไขโค้ดหรือ asset ใด ๆ** — เป็นเอกสารอย่างเดียว

---

## 0. มีอะไรเปลี่ยนไปจากคู่มือเดิมบ้าง (อ่านก่อนเป็นอันดับแรก)

ระบบ Dungeon ผ่านการปรับโครงสร้างครั้งใหญ่ที่เรียกว่า "Phase B — Personal Instancing" ทำให้แนวคิดของคู่มือเดิมผิดไปในจุดสำคัญหลายจุด:

1. **DungeonManager ไม่มีพื้นที่ floor เดียวอีกต่อไป** เดิมมี `tilesParent` / `objectsParent` / `navMeshSurface` แค่ชุดเดียว ตอนนี้เปลี่ยนเป็น:
   ```csharp
   public DungeonInstanceSlot[] instances = new DungeonInstanceSlot[MaxSlots]; // MaxSlots = 4
   ```
   โดย `DungeonInstanceSlot { Transform root; Transform tilesParent; Transform objectsParent; NavMeshSurface navMeshSurface; }` พิกัด grid ทุกตำแหน่งจะถูกบวกด้วย `instances[slot].root.position` **ตอนนี้ `instances` ของ DungeonManager ใน scene จริงมีขนาด 0 (ว่างเปล่า)** นี่คือช่องโหว่ที่ใหญ่ที่สุด และเป็นแก่นของ Section 5–6

2. **state ของผู้เล่นแต่ละคน (floor ปัจจุบัน, master seed, instance slot, สถานะอยู่ใน dungeon, ตำแหน่งกลับ) ถูกย้ายออกจาก DungeonManager ไปอยู่ที่ component ใหม่ชื่อ `PlayerDungeonState`** component นี้ **ยังไม่ได้ถูกเพิ่มเข้าไปใน `Shiba.prefab`** — ดู Section 7 ถ้าไม่มี component นี้ ระบบ dungeon จะไม่ทำงานสำหรับผู้เล่นทุกคนเลย

3. **`DungeonReturnData.cs` (static class) เป็นโค้ดที่ตายแล้ว** ถูกแทนที่ด้วย `PlayerDungeonState.ReturnPosition / ReturnRotation / HasReturnPosition` ทั้งหมดแล้ว ไม่มีโค้ดส่วนไหนเรียกใช้ `DungeonReturnData` อีก (มีแค่ comment เก่าที่พูดถึงชื่อนี้) → Section 13

4. **`SpawnPointManager.dungeonSpawnPoint` / `SetDungeonSpawn()` เป็น dead code path** ไม่ถูกเรียกจาก `DungeonEntrance` หรือ `DungeonManager` เลย (ใช้ `PlayerDungeonState.SetReturnPosition` + `TeleportOwnerRpc` แทน) → Section 13. ตัว `SpawnPointManager` เองยังทำงานอยู่ (ใช้สำหรับ spawn ผู้เล่นตอนเชื่อมต่อปกติ)

5. **`DungeonDeathHandler.cs` ถูกเขียนใหม่ทั้งหมด** ไม่ reload scene ตอนตายอีกต่อไป ตอนนี้คือ: fade จอดำ (UI local) → `PlayerDungeonState.RequestExitDungeonServerRpc()` (กระทบเฉพาะผู้เล่นที่ตาย) → `PlayerHealth.Instance.Revive()` → fade กลับเข้า ต้องมี `fadeImage` (Image ใน Canvas) ซึ่งตอนนี้ยังไม่มีที่ให้ชี้ไป → Section 9b

6. **`DungeonEntrance.cs` เป็นไฟล์ใหม่** ยังไม่ถูกวางไว้ใน scene ไหนเลย → Section 8

7. **`DungeonLadder.cs` ถูกเขียนใหม่เป็น `NetworkBehaviour` + `IInteractable` แบบไม่มี field** แต่ `Ladder.prefab` ที่มีอยู่ยังเก็บค่า serialized เก่า (`interactRadius`, `interactKey`, `promptUI`) จากสคริปต์เวอร์ชันเก่า — ค่าเหล่านี้กลายเป็นข้อมูล orphan/ไม่ถูกใช้แล้ว ที่แย่กว่านั้นคือ **`Ladder.prefab` ไม่มี `NetworkObject` และไม่ได้ลงทะเบียนใน `DefaultNetworkPrefabs.asset`** ทำให้ `DungeonManager.SpawnObjectY()`'s `netObj.Spawn(true)` จะ fallback เป็น `Instantiate` แบบไม่ network (พร้อม log warning) และบันไดจะไม่ sync ไปยัง client อื่นที่ไม่ใช่ host → Section 9a

8. **⚠️ เงื่อนไขที่ต้องทำก่อน (BLOCKING PREREQUISITE) — โครงสร้าง "พื้นที่ Dungeon แบบ always-loaded additive" ยังไม่มีอยู่จริง**
   ใน comment หัวไฟล์ของ `DungeonManager.cs` เขียนว่า dungeon ตอนนี้เป็น "พื้นที่ ALWAYS-LOADED additive... อยู่ใน scene เดียวกัน" กับฟาร์ม แต่:
   - ไม่มีสคริปต์ไหนเรียก `SceneManager.LoadScene("Dungeon", LoadSceneMode.Additive)`
   - โค้ดเปลี่ยน scene ที่มีอยู่ตัวเดียว (`SceneTransitionManager.LoadScene` → `NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single)`, ใช้โดย `TransitionTesting.cs`) โหลดแบบ **Single** ซึ่งจะ unload ทุกอย่างที่เหลือ — ตรงข้ามกับแนวคิด "always loaded" โดยสิ้นเชิง
   - ปัจจุบัน `DungeonManager` พร้อม hierarchy ของ tile/object/NavMesh อยู่ใน **`Dungeon.unity`** ซึ่งไม่ได้ถูกโหลดร่วมกับ `MainGame.unity` โดยอะไรเลย

   **คู่มือนี้แก้ปัญหาด้วยวิธีตั้งค่าผ่าน Unity ล้วน ๆ (Option A, แนะนำ, ไม่ต้องแก้โค้ด):** ย้าย GameObject `DungeonManager` (พร้อมลูกของมัน) ออกจาก `Dungeon.unity` ไปไว้ใน `MainGame.unity` ตรง ๆ เพื่อให้มันอยู่ตลอดเวลาที่ฟาร์มถูกโหลด ขั้นตอนอยู่ใน Section 1

   *Option B (ไม่ใช้ในคู่มือนี้ ต้องแก้โค้ด):* เขียน bootstrap script เพื่อโหลด `Dungeon.unity` แบบ additive ครั้งเดียวตอนเริ่มเกมแล้วไม่ unload เลย — กล่าวถึงเพื่อให้ครบเท่านั้น ไม่ได้อยู่ในสโคปนี้เพราะไม่มีการแก้โค้ด

9. **โครงสร้างข้อมูล save พร้อมอยู่แล้ว** `PlayerSaveData` มี `dungeonInDungeon`, `dungeon` (`DungeonSaveData`), `hasDungeonReturnPosition`, `dungeonReturnPosX/Y/Z`, `dungeonReturnRotY` ครบแล้ว และ `PlayerDungeonState.CaptureState/RestoreState` อ่าน/เขียนค่าเหล่านี้แล้ว **ไม่ต้องแก้ไขระบบ save หรือ schema ใด ๆ** — แค่ต้องมี component นี้อยู่บน player (Section 10)

---

## 1. Scene และ Build Settings ที่ต้องมี

### Build Settings ปัจจุบัน (`ProjectSettings/EditorBuildSettings.asset`)

| ลำดับ | Scene | Path | สถานะ |
|---|---|---|---|
| 0 | MainGame | `Assets/Project/Scenes/Main/MainGame.unity` | **Scene หลัก** — scene ที่โหลดตอนเปิดเกม มีโลกฟาร์ม, UI ของเกมทั้งหมด, และ `DebugToHost` ตัวจริงที่เรียก `NetworkManager.StartHost()` |
| 1 | Dungeon | `Assets/Project/Scenes/Dungeon/Dungeon.unity` | ปัจจุบันเป็น **scene ทดสอบแบบเดี่ยว ๆ** — มี `DebugToHost`, `SpawnPointManager`, `SFXSource`, `CurrencyStorage` ของตัวเอง **และ** hierarchy ของ `DungeonManager` ที่ฟีเจอร์นี้ต้องใช้ |
| 2 | RoomShiba | `Assets/Project/Scenes/Main/RoomShiba.unity` | ไม่เกี่ยวกับ dungeon |
| 3 | Bar | `Assets/Project/Scenes/Main/Bar.unity` | ไม่เกี่ยวกับ dungeon |
| 4 | Clinic | `Assets/Project/Scenes/Main/Clinic.unity` | ไม่เกี่ยวกับ dungeon |
| 5 | Shop | `Assets/Project/Scenes/Main/Shop.unity` | ไม่เกี่ยวกับ dungeon |

Scene ทั้ง 6 ถูก enable หมด **MainGame.unity มี `DebugToHost` ของตัวเองอยู่แล้ว** (ยืนยันแล้วในระดับ GameObject) และมีระบบเสียง/`SFX` ของตัวเองด้วย — ดังนั้นของใน `Dungeon.unity` เป็นแค่ scaffolding ของ scene ทดสอบ ไม่ใช่สิ่งที่ต้องย้ายมาด้วย

### สิ่งที่ต้องทำ: ย้าย hierarchy ของ Dungeon เข้าไปใน MainGame.unity

1. เปิด `MainGame.unity` เป็น active scene
2. เปิด `Dungeon.unity` แบบ **additive** เพื่อแก้ไข (ลากเข้า Hierarchy window หรือใช้ `File > Open Scene Additive`) ตอนนี้คุณจะเห็น hierarchy ของทั้งสอง scene ซ้อนกันใน Hierarchy window
3. ในส่วนของ `Dungeon.unity` ใน Hierarchy ให้หา GameObject ชื่อ **`DungeonManager`** ควรมีลูก 2 ตัวคือ **`TilesParent`** และ **`ObjectsParent`** พร้อมกับ component `NavMeshSurface` อยู่บน GameObject `DungeonManager` เอง
4. ลาก GameObject `DungeonManager` (พร้อมลูกทั้ง 2) จากส่วน `Dungeon.unity` ไปไว้ในส่วน `MainGame.unity` ของ Hierarchy การทำแบบนี้จะย้ายทั้งหมด — component, ลูก, และ `NavMeshSurface` — ไปอยู่ใน `MainGame.unity`
5. **ปรับตำแหน่งใหม่** ตำแหน่งโลกปัจจุบันคือ `(106.94, 3.78, 97.05)` ซึ่งถูกตั้งไว้สำหรับ scene ทดสอบ `Dungeon.unity` ที่ว่างเปล่า มีโอกาสสูงที่จะซ้อนทับกับวัตถุในฟาร์มของ `MainGame.unity` ให้ย้าย GameObject `DungeonManager` ทั้งชิ้นไปยังพื้นที่ที่แน่ใจว่าจะไม่ซ้อนกับฟาร์ม — วิธีที่ง่ายและปลอดภัยที่สุดคือ **ใต้ฟาร์มลงไปมาก ๆ** เช่นตั้งตำแหน่งเป็น `(0, -500, 0)` ตำแหน่งนี้จะกลายเป็น origin ของ **Instance Slot 0** (Section 6)
6. Save `MainGame.unity`
7. ปิดหน้าต่าง additive ของ `Dungeon.unity` **โดยไม่ save** (เพื่อให้สำเนา `DungeonManager` เดิมใน scene นั้นยังอยู่เหมือนเดิม — ดู Section 13 สำหรับวิธีจัดการ `Dungeon.unity` ที่เหลือในภายหลัง)

หลังจากนี้ `DungeonManager` จะเป็นส่วนหนึ่งของ `MainGame.unity` และอยู่ตลอดเวลาที่ฟาร์มถูกโหลด — ตอบโจทย์สมมติฐาน "always-loaded" โดยไม่ต้องแก้โค้ดเลย

### Build Settings หลังย้าย

ไม่จำเป็นต้องเปลี่ยน *รายการ* scene (การเปลี่ยน scene แบบ Single ไปยัง Bar/Shop/Clinic/RoomShiba ไม่ได้รับผลกระทบ — เป็นระบบคนละส่วน) `Dungeon.unity` จะเก็บไว้ในรายการ (ไม่ใช้แล้วแต่ไม่เป็นอันตราย) หรือจะลบออกก็ได้ — ดู Section 13

---

## 2. รายการ Prefab และ Component ที่ต้องมี

| Asset | สถานะปัจจุบัน | สิ่งที่ต้องทำ |
|---|---|---|
| `Assets/Project/Prefabs/Characters/Player/Shiba.prefab` (guid `01e1a553da6afb44ab49640da5f079ae`) | มี `NetworkObject`, `PlayerController`, `ClientNetworkTransform`, `ClientNetworkAnimator`, `StatManager`, `PlayerMagnet`, `PlayerHeldItem`, `PlayerItemUser`, `FootstepVFXController`, `Animator`, `CharacterController` แล้ว **ยังไม่มี `PlayerDungeonState`** | เพิ่ม component `PlayerDungeonState` — Section 7 |
| GameObject `DungeonManager` (ปัจจุบันอยู่ใน `Dungeon.unity`) | มี `DungeonManager` (config ✓, tileSize=8 ✓, `instances[]` ว่าง), `NetworkObject` (GlobalObjectIdHash `1271036027`), `NavMeshSurface`, ลูก `TilesParent`/`ObjectsParent` | ย้ายเข้า `MainGame.unity` (Section 1) + ตั้งค่า `instances[4]` (Section 5–6) |
| `Assets/Project/Prefabs/Environment/Dungeon/Ladder.prefab` (guid `f4f1cc8cb043f664ca0da72dceb20c3b`) | เป็น prefab variant ของ `template-floor-layer.fbx` มี `DungeonLadder` (ค่าเก่าตกค้าง) **ไม่มี `NetworkObject`, ยังไม่ยืนยัน `Collider`**, ไม่ได้อยู่ใน `DefaultNetworkPrefabs.asset` | เพิ่ม `NetworkObject` + `Collider` (layer 10), ลงทะเบียนใน `DefaultNetworkPrefabs.asset` — Section 9 |
| DungeonEntrance | **ยังไม่มีอยู่จริงที่ไหนเลย** — `DungeonEntrance.cs` ยังไม่ถูกวางใช้งาน | สร้าง GameObject ใหม่ใน `MainGame.unity` — Section 8 |
| `DungeonFloorTransition` (UI fade + ข้อความ "ชั้น N") | สคริปต์มีอยู่แล้ว **ไม่มี GameObject/Canvas ไหนอ้างถึงมันเลย** | สร้าง Canvas/Image/TMP — Section 9b |
| `PlayerHealth` + `DungeonDeathHandler` | สคริปต์มีอยู่แล้ว ถูกใช้เฉพาะใน `PersistentSystems.prefab` (ใช้โดย `Prototye.unity` ที่ไม่ใช่ scene ใน build) | สร้างตำแหน่งใหม่ที่เข้าถึงได้จาก `MainGame.unity` — Section 9b |
| `Assets/ScriptableObjects/System/DungeonConfig/DungeonConfig.asset` (guid `ba12a4f35e7c42a47a1d08e9b74a90bd`) | `gridWidth=30, gridHeight=30, tileSet=✓, ladderPrefab→Ladder.prefab, rockPrefab=✓, ores=[Ore_Gold], enemies=[Enemy_Slime], objectYOffset=1` | ถูกต้องอยู่แล้ว — ไม่ต้องทำอะไร |
| Ore_Gold / rockPrefab / Enemy_Slime prefabs | มี `NetworkObject` แล้ว, ลงทะเบียนใน `DefaultNetworkPrefabs.asset` แล้ว | ไม่ต้องทำอะไร |
| `Assets/Resources/InGameNetworkManager.prefab` | มี `SaveLoadManager` (in-scene placed `NetworkObject`, hash `518853818`) | โครงสร้างเดิมที่ใช้งานได้อยู่แล้ว — ตรวจสอบเท่านั้น (Section 10) |
| `Assets/Resources/GlobalManagers.prefab` | GameObject root ชื่อ `Canvas` มี `CanvasScaler`, ลูก `GlobalManagers` (NetworkManager, InputHandler), พร้อมลูก UI `FadePanel`/`Loading` | ใช้เป็นที่วาง UI ใหม่ใน Section 9b ได้ (เลือกได้) |

---

## 3. ข้อกำหนดเกี่ยวกับ NetworkObject

| Object | มี NetworkObject หรือยัง? | หมายเหตุ |
|---|---|---|
| `Shiba.prefab` (player) | ✅ มีอยู่แล้ว | `PlayerDungeonState` เป็น `NetworkBehaviour` ธรรมดาที่เพิ่มบน GameObject *เดียวกัน* — ไม่ต้องมี NetworkObject แยก |
| `DungeonManager` | ✅ มีอยู่แล้ว (GlobalObjectIdHash `1271036027`) | เป็น in-scene placed — ย้าย scene ตาม Section 1 ได้เลยโดยอัตโนมัติ |
| `Ladder.prefab` | ❌ **ไม่มี — ต้องเพิ่ม** | จำเป็นเพราะ `SpawnObjectY()` เรียก `go.GetComponent<NetworkObject>().Spawn(true)` ถ้าไม่มีจะกลายเป็น local-only มองไม่เห็นจาก client ที่ไม่ใช่ host |
| `Ore_Gold` / `rockPrefab` / `Enemy_Slime` | ✅ มีอยู่แล้ว | ถูก spawn ผ่าน `SpawnObjectY` ได้ถูกต้องอยู่แล้ว |
| `DungeonEntrance` (object ใหม่ใน scene) | ❌ ต้องเพิ่มตอนสร้าง | `DungeonEntrance : NetworkBehaviour` มี `[Rpc(SendTo.Server...)]` — ต้องมี `NetworkObject` เพื่อ route RPC เนื่องจากเป็น **object ที่วางไว้ใน scene เอง** (ไม่ได้ instantiate จาก prefab ตอน runtime) การเพิ่ม `NetworkObject` จะทำให้มันเป็น **in-scene placed NetworkObject** ซึ่ง NGO จะ spawn ให้อัตโนมัติฝั่ง server **ไม่ต้องลงทะเบียนใน NetworkPrefabsList** |
| `DungeonFloorTransition`, `PlayerHealth`, `DungeonDeathHandler` | ไม่ใช่ `NetworkBehaviour` | เป็น `MonoBehaviour` ธรรมดา — ไม่เกี่ยวกับ `NetworkObject` เป็น UI/state ฝั่ง local ทั้งหมด |

---

## 4. การลงทะเบียน Network Prefabs (`Assets/DefaultNetworkPrefabs.asset`)

asset นี้มี `IsDefault: 1` และถูกอ้างถึงจาก `GlobalManagers.prefab`'s `NetworkManager.NetworkConfig.Prefabs` ปัจจุบันมี 24 รายการ รายการที่ลงทะเบียนแล้วและเกี่ยวข้องกับ dungeon ที่ยืนยันแล้ว:

- `01e1a553da6afb44ab49640da5f079ae` — Shiba/Player
- `e428ef30ee3b57145a90b9342b9cb14a` — rockPrefab
- `2a6335e85cca5c54386de36a777ca685` — Ore_Gold prefab
- `7f038ef649258d2418ab5499b441fd24` — Enemy_Slime prefab

**สิ่งที่ต้องทำ:** หลังจาก Section 9 เพิ่ม `NetworkObject` ให้ `Ladder.prefab` (guid `f4f1cc8cb043f664ca0da72dceb20c3b`) แล้ว ให้เพิ่มเป็นรายการที่ 25 ในลิสต์นี้ ถ้าไม่ทำขั้นนี้ `netObj.Spawn(true)` จะ throw/fail บน client ที่ไม่ใช่ host เพราะ NGO ไม่สามารถ resolve prefab hash ที่ไม่ได้ลงทะเบียนได้

ไม่ต้องลงทะเบียนอะไรเพิ่มอีก — `DungeonEntrance` เป็น in-scene placed (Section 3) ไม่ต้องอยู่ในลิสต์นี้

---

## 5. การตั้งค่า DungeonManager

ตำแหน่งหลังจาก Section 1: `MainGame.unity`, GameObject `DungeonManager`, ตำแหน่ง `(0, -500, 0)` (หรือตำแหน่งที่เลือกใน Section 1)

### Field ที่ถูกต้องอยู่แล้ว (ตรวจสอบเฉย ๆ ไม่ต้องเปลี่ยน)

- `config` → `DungeonConfig.asset` ✓
- `tileSize` → `8` ✓ (หมายความว่า grid 30×30 ของแต่ละ floor จะกินพื้นที่โลก 240×240 หน่วย — สำคัญสำหรับการเว้นระยะใน Section 6)

### Field ที่ต้องตั้งค่า: `instances` (ขนาด 4)

ตอนนี้ array นี้มีขนาด **0** ให้ resize ใน Inspector เป็น **4** แต่ละช่อง `DungeonInstanceSlot` ต้องมี:

- `root` — `Transform` ที่ **ตำแหน่งโลก** ของมันคือ origin ที่พิกัด grid ทุกตัวของ slot นี้จะถูกบวกเข้าไป (ทั้ง `GridToWorld`/`WorldToGrid` ใช้ `origin + slot.root.position`)
- `tilesParent` — `Transform` ลูกที่ใช้เป็นที่เก็บ tile พื้น/ผนัง (จะถูกทำลาย/สร้างใหม่ทุกครั้งที่เปลี่ยน floor)
- `objectsParent` — `Transform` ลูกที่ใช้เก็บ `NetworkObject` แบบ interactive ที่ server spawn (บันได, หิน, แร่, มอนสเตอร์)
- `navMeshSurface` — component `NavMeshSurface` ที่ครอบพื้นที่ของ slot นั้น จะถูก rebake ผ่าน `BuildNavMesh()` ทุกครั้งที่ผู้เล่นใน slot นั้นเปลี่ยน floor

หมายเหตุ: `tilesParent`/`objectsParent` ไม่ต้องอยู่ที่ตำแหน่งใดเป็นพิเศษ — `Instantiate(prefab, worldPos, rot, parent)` กำหนดตำแหน่งโลกตรง ๆ อยู่แล้วแล้วค่อย reparent มีแค่ `root.position` เท่านั้นที่มีผลต่อตำแหน่ง การตั้งค่า `Collect Objects = Children` ของ `navMeshSurface` หมายความว่ามันจะ bake ทุกอย่างที่อยู่ข้างใต้มัน

### Field เก่าที่ตกค้าง — ไม่ต้องสนใจ

component `DungeonManager` ใน scene จริงยังมีค่า serialized เก่าตกค้างอยู่ในระดับบนสุด: `tilesParent: {fileID: 295425184}`, `objectsParent: {fileID: 456832144}`, `navMeshSurface: {fileID: 1575145769}` **field ทั้ง 3 นี้ไม่มีอยู่ใน class `DungeonManager` แล้ว** (ยืนยันจากซอร์สโค้ด — มีแค่ `DungeonInstanceSlot.tilesParent/objectsParent/navMeshSurface` เท่านั้น) Unity จะไม่แสดงค่านี้ใน Inspector และจะตัดออกเองตอน serialize ครั้งถัดไป ไม่ต้องทำอะไรเพิ่ม — ดู Section 13

---

## 6. การตั้งค่า Instance Slot (ทั้ง 4 ช่องของ `DungeonInstanceSlot`)

แต่ละ floor เป็น grid 30×30 ที่ `tileSize = 8` → พื้นที่โลก **240×240 หน่วย** บวกกับผนังรอบนอกอีก 1 tile (ดังนั้นเผื่อไว้ประมาณ 256×256 เพื่อความปลอดภัย) ทั้ง 4 slot root ต้องห่างกันมากพอที่จะไม่ให้ floor 2 อันซ้อนกันทั้งภาพและทางฟิสิกส์

### Layout ที่แนะนำ

จัด 4 slot เป็นตาราง 2×2 ห่างกัน **300 หน่วย** อยู่ใต้ดินทั้งหมดที่ `Y = -500` (ต่อจากตำแหน่ง `DungeonManager` ใน Section 1):

| Slot | ตำแหน่งโลกของ `root` |
|---|---|
| 0 | `(0, -500, 0)` — ใช้ GameObject `DungeonManager` เอง |
| 1 | `(300, -500, 0)` |
| 2 | `(0, -500, 300)` |
| 3 | `(300, -500, 300)` |

### Slot 0 — ใช้ของที่มีอยู่แล้ว

GameObject `DungeonManager` ที่ย้ายมาใน Section 1 มีลูกและ component ที่ต้องใช้อยู่แล้ว:

- `instances[0].root` = `Transform` ของ GameObject `DungeonManager` เอง
- `instances[0].tilesParent` = ลูก `TilesParent` ที่มีอยู่แล้ว
- `instances[0].objectsParent` = ลูก `ObjectsParent` ที่มีอยู่แล้ว
- `instances[0].navMeshSurface` = component `NavMeshSurface` ที่มีอยู่แล้ว

ไม่ต้องสร้าง object ใหม่สำหรับ slot 0 — แค่ลากทั้ง 4 ตัวนี้ไปใส่ใน `instances[0]` ที่ Inspector

### Slot 1–3 — สร้างใหม่

สำหรับแต่ละ slot 1, 2, 3:

1. สร้าง empty GameObject (เช่น `DungeonSlot1_Root`) เป็น sibling ของ `DungeonManager` วางตำแหน่งตามตารางข้างบน
2. ข้างใต้มัน สร้าง empty GameObject ลูก 2 ตัว: `TilesParent` และ `ObjectsParent`
3. เพิ่ม component `NavMeshSurface` (จาก Unity AI Navigation package) ที่ slot root ตั้งค่าให้ตรงกับของ slot 0: **Collect Objects = Children**, **Include Layers = Everything**, **Use Geometry = Render Meshes**, Generate Links = ปิด, Ignore NavMesh Agents = เปิด, Ignore NavMesh Obstacles = เปิด ปล่อย **Nav Mesh Data** ให้ว่างไว้ — `DungeonManager.BakeThenSpawnEnemies()` จะเรียก `navMesh.BuildNavMesh()` ตอน runtime ซึ่งจะสร้าง data asset ให้เองตอนใช้งานครั้งแรก (จะ pre-bake ใน editor ไว้ก่อนเพื่อทดสอบก็ได้)
4. นำ `root` / `tilesParent` / `objectsParent` / `navMeshSurface` ไปใส่ใน `instances[1]` / `[2]` / `[3]` ตามลำดับ

### ทำไมเรื่องนี้สำคัญ

`GetOrAssignSlot()` จะแจก slot 0–3 ให้ผู้เล่นที่เชื่อมต่อพร้อมกันได้สูงสุด 4 คน (มาก่อนได้ก่อน, ปล่อยคืนตอน disconnect ผ่าน `ReleaseSlot`) ผู้เล่นคนที่ 5 ที่เข้ามาพร้อมกันจะขึ้น warning log และถูกบีบเข้า slot 0 (ชนกับคนที่อยู่แล้ว) — `MaxSlots = 4` เป็นค่าคงที่ในโค้ด ไม่สามารถแก้ผ่าน Inspector ได้ ถ้าต้องการรองรับผู้เล่นเข้า dungeon พร้อมกันมากกว่า 4 คน ต้องแก้โค้ด (อยู่นอกสโคปนี้ แต่ควรแจ้งทีมไว้)

---

## 7. การตั้งค่า Player Prefab (`Shiba.prefab`)

**สิ่งที่ต้องทำ: เพิ่ม component `PlayerDungeonState`** เข้าไปที่ root GameObject ของ `Shiba.prefab` (GameObject เดียวกับที่มี `NetworkObject` และ `PlayerController`)

- `PlayerDungeonState` **ไม่มี field ที่ serialize/แสดงใน Inspector เลย** — กด `Add Component > PlayerDungeonState` (หรือลากสคริปต์ลงบน GameObject) ก็จบ ไม่มีค่าให้กรอก
- มันสืบทอดจาก `NetworkSaveableBehaviour` ดังนั้นตอน spawn จะเรียก `SaveLoadManager.Instance?.Register(this)` อัตโนมัติ (server-only) และตอน despawn จะเรียก `Unregister` — รูปแบบเดียวกับที่ `PlayerController` ใช้อยู่แล้ว นี่คือเหตุผลที่ Section 10 (save/load) "ใช้งานได้เลยทันที"
- ลำดับ component เทียบกับ `PlayerController` ฯลฯ ไม่มีผลอะไร — ไม่มี `[RequireComponent]` ที่กำหนดลำดับระหว่างกัน
- `cameraTransform` บน `PlayerController` ที่ไม่ได้ assign ไว้นั้นถูกต้องแล้ว — `PlayerController.OnNetworkSpawn()` จะ self-assign ผ่าน `Camera.main.transform` ถ้าเป็น null **ไม่ใช่ช่องโหว่ ไม่ต้องไปแก้**

หลังจากเพิ่มตัวนี้ตัวเดียว ผู้เล่นทุกคนจะมีครบ: `NetworkObject`, `PlayerController` (เคลื่อนที่/animation/save), `PlayerDungeonState` (floor/seed/instance-slot/return-position + save ของ dungeon) รวมถึง component inventory/combat ที่มีอยู่แล้ว นี่คือ component set ที่ครบสมบูรณ์สำหรับให้ผู้เล่นใช้ dungeon ได้

---

## 8. การตั้งค่า Dungeon Entrance

`DungeonEntrance.cs` (`[RequireComponent(typeof(Collider))]`, `NetworkBehaviour`, `IInteractable`) ยังไม่ถูกวางใน scene ไหนเลย ให้สร้างใหม่ใน `MainGame.unity`:

1. สร้าง empty GameObject ใหม่ใน `MainGame.unity` ที่ตำแหน่งโลกที่ต้องการให้เป็นทางเข้า dungeon (เช่น ใกล้ ๆ prop ถ้ำ/เหมืองในฟาร์ม) ตั้งชื่อว่า `DungeonEntrance`
2. เพิ่ม component `Collider` แบบ concrete (เช่น `BoxCollider` หรือ `SphereCollider`) ขนาดให้พอดีกับรูปทรงทางเข้า `[RequireComponent(typeof(Collider))]` แค่การันตีว่ามี `Collider` *บางแบบ* อยู่หลังจากเพิ่ม `DungeonEntrance.cs` — Unity จะไม่สร้าง collider ให้เองสำหรับ type `Collider` ที่เป็น abstract ดังนั้นต้องเพิ่ม collider แบบ concrete **ก่อน**
3. ตั้ง Layer ของ Collider เป็น **10 ("Interact")** — นี่คือ layer ที่ `InteractController.interactLayer` ใช้สแกนด้วย `Physics.OverlapSphere` เช่นเดียวกับ Door/Workbench ตั้ง `Is Trigger = true` เพื่อไม่ให้กีดขวางผู้เล่นทางฟิสิกส์
4. เพิ่ม component สคริปต์ `DungeonEntrance`
5. เพิ่ม component `NetworkObject` (Section 3 — ทำให้มันเป็น in-scene placed NetworkObject ที่ server spawn ให้อัตโนมัติ ไม่ต้องลงทะเบียน prefab)
6. (ไม่บังคับ) เพิ่มลูกที่เป็น visual (โมเดลปากถ้ำ, decal, particle ฯลฯ) — เป็นแค่ความสวยงาม ไม่จำเป็นสำหรับสคริปต์ `OnDrawGizmosSelected` จะวาด wire sphere สีม่วงแดงรัศมี 1.5 เพื่อช่วยจัดตำแหน่งใน editor

### สรุปการทำงาน (สำหรับใช้ตรวจสอบทีหลัง)

`Interact()` → `RequestEnterDungeonServerRpc()` (`[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]`) → server resolve `PlayerDungeonState` ของผู้เล่นที่เรียก แล้วเรียก `player.SetReturnPosition(currentPos, currentRot)` จากนั้น `DungeonManager.Instance.EnterDungeon(player)` ฟังก์ชันนี้จะ assign/reuse instance slot ของผู้เล่นคนนั้นและ teleport เฉพาะผู้เล่นคนนั้น (`TeleportOwnerRpc`) — ไม่กระทบผู้เล่นคนอื่นเลย

---

## 9. การตั้งค่าบันได (Ladder)

### 9a. การแก้ไข `Ladder.prefab` (จำเป็น)

`Assets/Project/Prefabs/Environment/Dungeon/Ladder.prefab` (guid `f4f1cc8cb043f664ca0da72dceb20c3b`) เป็น `PrefabInstance` variant ของ `Assets/Project/Models/Environment/Dungeon/template-floor-layer.fbx` มีสคริปต์ `DungeonLadder.cs` ที่ถูกต้องติดอยู่แล้ว (ยืนยันจาก script guid `04a8aa45d9581e84d8d561cae1c190e7`) แต่ขาดส่วนที่เกี่ยวกับ networking เปิดใน Prefab Mode แล้ว:

1. **เพิ่ม `Collider`** ให้พอดีกับรูปทรงบันได (เช่น `BoxCollider` หรือ `CapsuleCollider`), `Is Trigger = true`, **Layer 10 ("Interact")** — เหตุผลเดียวกับ Section 8 ตรวจสอบก่อนว่า FBX base มี collider มาให้แล้วหรือยัง ถ้าไม่มีให้เพิ่มเอง
2. **เพิ่ม component `NetworkObject`** นี่คือส่วนที่ขาดและสำคัญที่สุด — ถ้าไม่มี `DungeonManager.SpawnObjectY()` จะ log `Debug.LogWarning($"[DungeonManager] {prefab.name} has no NetworkObject — using plain Instantiate.")` (เช่น `"[DungeonManager] Ladder has no NetworkObject — using plain Instantiate."`) และ fallback เป็น object local ที่ไม่ sync
3. ค่า serialized เก่าของ `DungeonLadder` ที่ตกค้าง (`interactRadius: 20`, `interactKey: 101`, `promptUI: {fileID: 0}`) ปล่อยทิ้งไว้ได้ — `DungeonLadder.cs` เวอร์ชันปัจจุบันไม่มี field เลย ดังนั้นเป็นข้อมูล orphan ที่ไม่มีอันตราย Unity จะตัดออกเองตอน save ครั้งถัดไป (ดู Section 13) ไม่ต้องล้างค่าด้วยมือก็ได้ แต่ถ้าจะล้างก็ทำได้
4. Save prefab

### 9b. ลงทะเบียนใน `DefaultNetworkPrefabs.asset`

เพิ่มรายการใหม่สำหรับ guid `f4f1cc8cb043f664ca0da72dceb20c3b` (Ladder.prefab) ในลิสต์ 24 รายการของ `Assets/DefaultNetworkPrefabs.asset` (→ กลายเป็น 25 รายการ) ดู Section 4

### 9c. `DungeonConfig.asset` — ไม่ต้องเปลี่ยน

`DungeonConfig.ladderPrefab` ชี้ไปที่ `Ladder.prefab` ตัวนี้อยู่แล้ว (ยืนยันจาก stripped GameObject reference fileID `2686620129483127115` ที่ตรงกัน) เนื่องจากเป็นการแก้ไข prefab ไฟล์ *เดิม* (ไม่ได้สร้างใหม่) **ไม่ต้อง re-point ใหม่**

### สรุปการทำงาน (สำหรับใช้ตรวจสอบทีหลัง)

`DungeonLadder.Interact()` → `RequestNextFloorServerRpc()` (`[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]`) → server resolve `PlayerDungeonState` ของผู้เล่นที่เรียก → `DungeonManager.Instance.GoNextFloor(player)` ฟังก์ชันนี้จะเลื่อน floor ของผู้เล่นคนนั้นเท่านั้น UI fade/"ชั้น N" ที่แสดงเฉพาะ host (`DungeonFloorTransition`, ดู 9d) จะเล่นระหว่างการเปลี่ยน floor **เฉพาะตอนที่ผู้เล่นที่เปลี่ยน floor คือ host** (`player.OwnerClientId == NetworkManager.ServerClientId`)

### 9d. UI `DungeonFloorTransition` (จำเป็นสำหรับ fade/ข้อความตอนเปลี่ยน floor)

`DungeonFloorTransition` เป็น singleton `MonoBehaviour` มี `public Image fadePanel; public TextMeshProUGUI floorText;` — ปัจจุบัน **ไม่มี GameObject ไหนติดสคริปต์นี้เลย** ดังนั้น `DungeonFloorTransition.Instance` จะเป็น `null` เสมอ และ `GoNextFloor` จะข้ามขั้น fade/ข้อความไปแบบเงียบ ๆ (มี null-guard อยู่แล้ว ดังนั้นไม่พังแต่จะไม่มี visual feedback)

วิธีตั้งค่า:

1. เพิ่ม `Canvas` (Screen Space – Overlay) ใน `MainGame.unity` ถ้ายังไม่มีตัวที่เหมาะสำหรับ overlay เต็มจอ — root ของ `GlobalManagers.prefab` เป็น `Canvas` ที่มี `CanvasScaler` อยู่แล้วและมี `FadePanel`/`Loading` overlay สำหรับการเปลี่ยน scene อยู่แล้ว จะเพิ่มเป็น sibling ที่นั่น หรือสร้าง canvas แยกใน `MainGame.unity` ก็ได้
2. ใต้ Canvas นั้น สร้าง:
   - `Image` ที่ครอบเต็มจอ alpha 0 ตั้งต้น — นี่คือ `fadePanel`
   - `TextMeshProUGUI` (เช่น จัดกึ่งกลาง, font ขนาดใหญ่) สำหรับข้อความ "ชั้น N" — นี่คือ `floorText`
3. เพิ่มสคริปต์ `DungeonFloorTransition` ลงบน Canvas (หรือ GameObject ลูกที่แยกไว้) และ assign `fadePanel` กับ `floorText` ตามที่สร้างไว้ข้างบน
4. ค่า `fadeDuration` (0.4), `holdDuration` (1.2), `fontSize` (72) ใช้ค่า default ได้เลย ถ้าไม่ต้องการ timing/ขนาดอื่น

UI นี้เป็น local อย่างเดียว (ไม่มี networking) และอ่านผ่าน `DungeonFloorTransition.Instance` — ตราบใดที่มี instance เดียวต่อ client ก็ใช้งานได้

---

## 10. ข้อกำหนดเกี่ยวกับ Save/Load

**ไม่ต้องแก้ไขโค้ดหรือ schema ของระบบ save เลย** ทุกอย่างที่ต้องใช้มีอยู่แล้ว:

- `PlayerSaveData` มี `dungeonInDungeon`, `dungeon` (`DungeonSaveData`), `hasDungeonReturnPosition`, `dungeonReturnPosX/Y/Z`, `dungeonReturnRotY` ครบแล้ว
- `PlayerDungeonState.CaptureState`/`RestoreState` (Section 7) อ่าน/เขียนค่าทั้งหมดนี้แล้ว
- `PlayerDungeonState` สืบทอดจาก `NetworkSaveableBehaviour` ซึ่ง `OnNetworkSpawn`/`OnNetworkDespawn` จะเรียก `SaveLoadManager.Instance?.Register(this)` / `Unregister(this)` อัตโนมัติเมื่อ `IsServer` — เหมือนกับ pattern ที่ `PlayerController` ใช้กับ save data ของตัวเองอยู่แล้ว
- `SaveLoadManager` เองอยู่ใน `Assets/Resources/InGameNetworkManager.prefab` เป็น in-scene-placed `NetworkObject` (hash `518853818`) นี่คือ **โครงสร้างเดิมที่ใช้งานได้อยู่แล้ว** (เป็นวิธีที่ระบบ save ของฟาร์มทำงานอยู่ตอนนี้) — ไม่ใช่งานที่ต้องตั้งค่าใหม่สำหรับ refactor นี้

**สิ่งที่ต้องทำมีแค่ Section 7** (เพิ่ม `PlayerDungeonState` ลงใน `Shiba.prefab`) พอ component นี้อยู่บน player แล้ว การ register/save/restore จะทำงานอัตโนมัติทั้งหมด

**สิ่งที่ต้องตรวจสอบเฉย ๆ:** ยืนยันว่า `NetworkObject` ของ `InGameNetworkManager` มีอยู่จริงและ spawn ตอน runtime ใน `MainGame.unity` (ควรจะเป็นแบบนั้นอยู่แล้ว เพราะระบบ save ของฟาร์มทำงานได้ในปัจจุบัน) — ดู Section 14

ตอน restore, `PlayerDungeonState.RestoreState()` จะเช็ก `playerData.dungeonInDungeon && playerData.dungeon != null` ถ้าเป็นจริงจะเรียก `DungeonManager.Instance?.EnterDungeon(this, playerData.dungeon)` — หมายความว่าผู้เล่นที่ save ไว้ตอนอยู่ใน dungeon จะถูกนำกลับเข้า instance slot (ที่ assign ใหม่) ที่ floor เดิมโดยอัตโนมัติ

---

## 11. ข้อกำหนดด้าน Multiplayer

ทั้งหมดนี้ถูกเขียนไว้ถูกต้องในโค้ดอยู่แล้ว — ลิสต์ไว้เพื่อให้รู้ว่า *อะไรที่ไม่ควรไปแก้* และอะไรที่ต้องตรวจใน Section 14:

- **การแยกข้อมูลของผู้เล่นแต่ละคน** ผ่าน `NetworkVariable<T>` ที่ใช้ `NetworkVariableReadPermission.Owner` / `NetworkVariableWritePermission.Server` บน `PlayerDungeonState` (`netInstanceSlot`, `netMasterSeed`, `netCurrentFloor`, `netInDungeon`) เฉพาะ server และ client ของผู้เล่นคนนั้นเท่านั้นที่เห็นค่านี้
- **การ teleport** ผ่าน `[Rpc(SendTo.Owner)] TeleportOwnerRpc(...)` — เฉพาะ client ของผู้เล่นเป้าหมายเท่านั้นที่ถูกย้าย
- **action ที่ผู้เล่นเป็นคนสั่ง** (`DungeonEntrance.RequestEnterDungeonServerRpc`, `DungeonLadder.RequestNextFloorServerRpc`, `PlayerDungeonState.RequestExitDungeonServerRpc`) เป็น `[Rpc(SendTo.Server, ...)]` resolve ฝั่ง server ผ่าน `rpcParams.Receive.SenderClientId` → `PlayerObject` ของ client นั้น → `PlayerDungeonState` แต่ละ action กระทบเฉพาะผู้เล่นที่เรียกเท่านั้น
- **การแยก instance ขึ้นอยู่กับ Section 6 ทั้งหมด** ถ้า `instances[]` ว่างหรือตั้งค่าผิด `GridToWorld`/`WorldToGrid` จะ fallback เป็น `Vector3.zero` สำหรับทุก slot — หมายความว่า **ทั้ง 4 slot จะซ้อนกันที่ origin โลกเดียวกัน** ทำให้การแยก instance ทางภาพ/ฟิสิกส์พังถึงแม้ data isolation (NetworkVariables, save data) จะยังถูกต้องอยู่ก็ตาม นี่คือเหตุผลที่ Section 6 ไม่ใช่ทางเลือก
- **การ spawn object**: `SpawnObjectY()` ถูก gate ด้วย `IsServer` และเรียก `netObj.Spawn(true)` — prefab ที่ถูก spawn ทุกตัว (บันได, หิน, แร่, มอนสเตอร์) ต้องมี `NetworkObject` ที่ลงทะเบียนแล้ว (Section 3–4) บันไดเป็นช่องโหว่เดียวที่เหลืออยู่ตอนนี้
- **UI เฉพาะ host**: fade/floor-text ของ `DungeonFloorTransition` จะแสดงเฉพาะตอน host เดินทาง dungeon ของตัวเอง (`player.OwnerClientId == NetworkManager.ServerClientId`) ออกแบบมาแบบนี้โดยตั้งใจ — client ที่ไม่ใช่ host ที่เปลี่ยน floor จะไม่เห็น overlay นี้ เห็นแค่ floor ของตัวเองสร้างใหม่ อย่าเข้าใจผิดว่าเป็นบั๊กตอนทดสอบ
- **สถาปัตยกรรมเป็นแบบ host-authoritative** ไม่ใช่ dedicated server — `DebugToHost.Start()` เรียก `NetworkManager.Singleton.StartHost()` โค้ด dungeon ที่ gate ด้วย `IsServer` ทั้งหมดจะรันบนเครื่องที่เป็น host

---

## 12. รายการ Field ใน Inspector ที่ต้อง Assign (เช็คลิสต์)

- [ ] **`DungeonManager` (ใน `MainGame.unity` หลัง Section 1)**
  - [ ] `config` — ปัจจุบันคือ `DungeonConfig.asset` (ตรวจสอบ ไม่ต้องเปลี่ยน)
  - [ ] `tileSize` — ปัจจุบันคือ `8` (ตรวจสอบ ไม่ต้องเปลี่ยน)
  - [ ] `instances` — resize เป็น **4** จากนั้นแต่ละ index 0–3:
    - [ ] `root`
    - [ ] `tilesParent`
    - [ ] `objectsParent`
    - [ ] `navMeshSurface`
- [ ] **`Shiba.prefab`** — เพิ่ม component `PlayerDungeonState` (ไม่มี field ให้กรอก)
- [ ] **`Ladder.prefab`**
  - [ ] เพิ่ม `Collider` (Layer 10 "Interact", Is Trigger ✓)
  - [ ] เพิ่ม `NetworkObject`
  - [ ] (ไม่บังคับ) ล้างค่าเก่าของ `DungeonLadder`: `interactRadius`/`interactKey`/`promptUI`
- [ ] **`Assets/DefaultNetworkPrefabs.asset`** — เพิ่มรายการของ `Ladder.prefab` (guid `f4f1cc8cb043f664ca0da72dceb20c3b`)
- [ ] **GameObject `DungeonEntrance` ใหม่** (ใน `MainGame.unity`)
  - [ ] `Collider` (Layer 10 "Interact", Is Trigger ✓) — เพิ่มก่อนสคริปต์ตาม `[RequireComponent]`
  - [ ] สคริปต์ `DungeonEntrance` (ไม่มี field)
  - [ ] `NetworkObject`
- [ ] **UI `DungeonFloorTransition` ใหม่**
  - [ ] `fadePanel` → `Image` เต็มจอ
  - [ ] `floorText` → `TextMeshProUGUI`
  - [ ] (ไม่บังคับ) `fadeDuration` / `holdDuration` / `fontSize` — ใช้ค่า default ได้
- [ ] **ตำแหน่งใหม่ของ `PlayerHealth` + `DungeonDeathHandler`**
  - [ ] `DungeonDeathHandler.fadeImage` → `Image` เต็มจอ (ใน Canvas)
  - [ ] (ไม่บังคับ) `fadeDuration` / `deathPauseDuration` / `reviveDelay` / `dieAnimTrigger` — ใช้ค่า default ได้ `dieAnimTrigger` ปล่อยว่างไว้ได้ถ้ายังไม่มี animation ตาย

---

## 13. สิ่งที่ล้าสมัย / ควรลบ

| รายการ | ทำไมล้าสมัย | สิ่งที่ควรทำ |
|---|---|---|
| `Assets/Project/ScriptMain/Dungeon/DungeonReturnData.cs` | static class นี้ถูกแทนที่ด้วย `PlayerDungeonState.ReturnPosition/ReturnRotation/HasReturnPosition` ทั้งหมดแล้ว มีแค่ comment เก่าใน `PlayerDungeonState.cs` ที่พูดถึงชื่อนี้ — ไม่มีโค้ดส่วนไหนเรียกใช้จริง | ลบได้เลย |
| field `SpawnPointManager.dungeonSpawnPoint` + method `SetDungeonSpawn()` + branch dungeon-override ใน `GetNextPosition()`/`GetNextRotation()`/`OnDrawGizmosSelected` (gizmo สีฟ้า) | ไม่ถูกเรียกเลย — `DungeonEntrance`/`DungeonManager` ใช้ `PlayerDungeonState.SetReturnPosition`+`TeleportOwnerRpc` แทน | ลบ branch ที่ตายแล้วนี้ได้ ส่วน `SpawnPointManager` เองยังต้องเก็บไว้ (ยังใช้สำหรับ spawn ตอน `DebugToHost` เชื่อมต่อปกติ) |
| ค่า serialized เก่าตกค้างของ `DungeonManager` ระดับบนสุด `tilesParent`/`objectsParent`/`navMeshSurface` | field เหล่านี้ไม่มีอยู่ใน class `DungeonManager` แล้ว (มีแค่ `DungeonInstanceSlot.tilesParent/objectsParent/navMeshSurface`) | ไม่ต้องทำอะไร — Unity จะตัดออกเองตอน save ครั้งถัดไป |
| ค่า serialized เก่าของ `DungeonLadder` ใน `Ladder.prefab` (`interactRadius: 20`, `interactKey: 101`, `promptUI: {fileID: 0}`) | `DungeonLadder.cs` เวอร์ชันปัจจุบันไม่มี field เลย | ไม่ต้องทำอะไร — เป็นงาน optional ระหว่าง Section 9 |
| `Assets/!ShibaFarm/ScenShibaFarm/Prefab/PersistentSystems "1".prefab` และ `"2".prefab` | เป็น duplicate ที่ orphan เต็มตัว ไม่มีอะไรอ้างถึง | เป็นตัวเลือกสำหรับลบ |
| GameObject "Shiba" เก่าใน `PersistentSystems.prefab` (มี `PlayerHealth` + `DungeonDeathHandler` อยู่บน Canvas) | ใช้เฉพาะใน `Prototye.unity` ซึ่ง **ไม่อยู่ใน Build Settings** Section 9b สร้างตำแหน่งใหม่ที่เข้าถึงได้จาก scene ใน build แทน | priority ต่ำ — ทำเครื่องหมายไว้ทำความสะอาด ไม่เร่งด่วนเพราะไม่กระทบ build |
| `Dungeon.unity` (หลังการย้ายใน Section 1) | หลังจากย้าย `DungeonManager`/`TilesParent`/`ObjectsParent`/`NavMeshSurface` เข้า `MainGame.unity` แล้ว ส่วนที่เหลือ (`SpawnPointManager`, `SFXSource`, `CurrencyStorage`, `DebugToHost`) เป็นสำเนาที่ซ้ำกับของที่ `MainGame.unity` มีอยู่แล้ว | ลบออกจาก Build Settings และ/หรือลบไฟล์ scene — ดูขั้นตรวจสอบใน Section 14 ก่อนลบอะไร |
| `C:\Unity\ShibaFarmClaudePro\Dungeon_PersonalInstance_Setup_Guide.md` (คู่มือเดิม) | ถูกแทนที่ด้วยเอกสารนี้ | ใช้อ้างอิงเฉย ๆ |

---

## 14. เช็คลิสต์ตรวจสอบขั้นสุดท้าย

ทำตามลำดับ แต่ละขั้นสมมติว่าขั้นก่อนหน้าผ่านแล้ว

### ตรวจสอบแบบ static / ใน editor (ไม่ต้อง Play mode)

1. **`MainGame.unity` มี `DungeonManager`** โดย `instances` มีขนาด = 4 และ field ย่อยทั้ง 16 ตัว (`root`/`tilesParent`/`objectsParent`/`navMeshSurface` × 4) ถูก assign เป็น reference ที่ไม่ใช่ null ครบ
2. **`Shiba.prefab` มี `PlayerDungeonState`** อยู่คู่กับ `NetworkObject` และ `PlayerController`
3. **`Ladder.prefab` มีทั้ง `NetworkObject` และ `Collider` บน Layer 10** และปรากฏเป็นรายการที่ 25 ใน `Assets/DefaultNetworkPrefabs.asset`
4. **มี GameObject `DungeonEntrance` ใน `MainGame.unity`** ที่มี `Collider` (Layer 10, trigger), สคริปต์ `DungeonEntrance`, และ `NetworkObject`
5. **`DungeonFloorTransition.fadePanel` และ `.floorText` ถูก assign** บน GameObject ที่ติดสคริปต์นี้
6. **`DungeonDeathHandler.fadeImage` ถูก assign** และ `PlayerHealth`/`DungeonDeathHandler` อยู่บน GameObject ที่จะมีอยู่จริงตอน runtime ใน `MainGame.unity` (ไม่ใช่อยู่แค่ใน `Prototye.unity` ที่ไม่ใช้แล้ว)

### ตรวจสอบใน Play mode — ผู้เล่นคนเดียว (host เท่านั้น)

7. เริ่มเกมเป็น host เดินไปที่ `DungeonEntrance` กด interact → ผู้เล่นถูก teleport ไป instance slot 0's floor 1 (ผลลัพธ์จาก `GridToWorld` ควรอยู่ที่ตำแหน่งโลกประมาณ `(0,-500,0) + offset` ไม่ใช่ `(0,0,0)`)
8. tile พื้น, ผนัง, บันได, แร่, หิน, และมอนสเตอร์ ทั้งหมดมองเห็นได้และอยู่ที่ตำแหน่ง grid ที่ถูกต้องภายใน slot 0
9. ใช้บันได → `DungeonFloorTransition` fade จอดำ + ข้อความ "ชั้น 2" + fade กลับเข้า ทำงาน (เฉพาะ host ตามที่ออกแบบไว้) ผู้เล่นจะอยู่ที่ floor 2 ของ slot 0 ส่วน object ของ floor 1 ถูก clear (`ClearFloor`)
10. รับความเสียหายจนตาย (หรือทดสอบตามวิธีของโปรเจกต์) → fade จอดำ → ผู้เล่นออกจาก dungeon และถูก teleport ไปที่ `ReturnPosition` (จุดที่ใช้ `DungeonEntrance`) → HP ถูกฟื้นผ่าน `PlayerHealth.Revive()` → fade กลับเข้า
11. Save เกม, ออกจาก Play mode, เริ่มใหม่, โหลด save เดิม ถ้าผู้เล่น save ไว้ตอนอยู่ใน dungeon ให้ตรวจว่า `RestoreState` นำผู้เล่นกลับเข้า dungeon instance ที่ floor เดิมถูกต้อง (slot ที่ assign ใหม่ — น่าจะเป็น slot 0 อีกครั้งถ้าเล่นคนเดียว)

### ตรวจสอบใน Play mode — multiplayer (2 client ขึ้นไป)

12. ทั้ง host และ client อีก 1 คนใช้ `DungeonEntrance` ตรวจว่าทั้งสองได้ **instance slot คนละช่อง** (เช่น slot 0 และ slot 1) — เช็กผ่าน `DungeonManager._slotAssignments` ใน debugger/log หรือดูง่าย ๆ ว่า floor ของทั้งสองอยู่คนละตำแหน่งโลก (ห่างกัน 300 หน่วยตาม Section 6) และไม่ซ้อนกัน
13. แต่ละผู้เล่นเปลี่ยน floor ผ่านบันไดของตัวเอง — ตรวจว่า **มีแค่ผู้เล่นที่เปลี่ยน floor เท่านั้น** ที่เห็นการเปลี่ยนแปลง (floor ของตัวเองถูกสร้างใหม่ ส่วน floor/object/ตำแหน่งของอีกคนไม่ถูกแตะ)
14. ตรวจว่า overlay fade/floor-text ที่เฉพาะ host แสดง **เฉพาะตอน host เปลี่ยน floor** เท่านั้น ไม่ใช่ตอน client ที่ไม่ใช่ host เปลี่ยน
15. ผู้เล่นคนหนึ่งตายและออกจาก dungeon — ตรวจว่า dungeon session, floor, และ object ของ **ผู้เล่นอีกคน** ไม่ถูกกระทบเลย
16. Disconnect client คนหนึ่งระหว่างอยู่ใน dungeon — ตรวจว่า `ReleaseSlot` ปล่อย slot ของเขาคืน (`OnNetworkDespawn`) เพื่อให้ผู้เล่นคนที่ 3 ที่เข้ามาใหม่ใช้ slot เดิมได้โดยไม่ชนกัน

### ตรวจสอบก่อนลบ (ก่อนลบอะไรใน Section 13)

17. ตรวจว่า `Dungeon.unity` ไม่ถูกอ้างถึงจากสคริปต์ไหนแล้ว (รายชื่อ scene ใน `SceneTransitionManager`, array scene ใน `UIManager`, การเรียก `LoadScene("Dungeon"...)` ใด ๆ) ก่อนลบออกจาก Build Settings หรือลบไฟล์ scene
18. ตรวจว่าการลบ `DungeonReturnData.cs` ไม่ทำให้ compile พัง (grep หาคำว่า `DungeonReturnData` ทั้งโปรเจกต์ — ควรเจอแค่ในไฟล์ของตัวเองเท่านั้น)
