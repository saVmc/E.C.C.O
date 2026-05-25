# E.C.C.O – Object-Oriented Programming Project
## Assessment Documentation

---

## DESIGN OF CHARACTERS AND ENVIRONMENT

### Characters (Entities that interact with environment)
- **Player**: Primary character controlled by user; records and replays actions
- **Echo (Ghost)**: Recorded playback entity; replays exact player actions
- **Enemy**: AI-controlled antagonist; responds to player and echo presence
- **NPC**: Non-player character; provides puzzle hints or objectives

### Environment (Static and Interactive Elements)
- **Level/Room**: Container for game state; holds tiles, objects, and entities
- **Tile**: Walkable grid cell; stores collision data and properties
- **Obstacle**: Static blocking element (wall, pit, hazard)
- **Door**: Conditional passage; requires key or state trigger
- **Puzzle Element**: Interactive object (pressure plate, switch, lock)
- **Collectible**: Item for inventory (key, potion, narrative item)

---

## Define Characters as Objects

### Object Hierarchy (Parent → Child Classes)

```
Entity (Parent)
├── Player
│   ├── Attributes: position, velocity, isRecording, recordedActions
│   ├── Methods: move(), recordAction(), playback(), interact()
│
├── Echo (extends Player)
│   ├── Attributes: position, recordedActions, playbackTime, isPlaying
│   ├── Methods: playback(), syncWithPlayer()
│
├── Enemy
│   ├── Attributes: position, velocity, AI state, detection range
│   ├── Methods: patrol(), chase(), attack(), react()
│
└── NPC
    ├── Attributes: position, dialogue, questState
    ├── Methods: interact(), giveHint()

Environment (Parent)
├── Tile
│   ├── Attributes: gridPosition, walkable, properties
│   ├── Methods: getTile(), isWalkable()
│
├── Obstacle
│   ├── Attributes: position, sprite, collisionBox
│   ├── Methods: collideWith()
│
├── Door (extends Obstacle)
│   ├── Attributes: isLocked, requiredKey, state
│   ├── Methods: unlock(), open(), close()
│
└── PuzzleElement
    ├── Attributes: state, targetEffect, triggerType
    ├── Methods: activate(), setState(), checkCompletion()
```

---

## Identify Characteristics (Attributes) that can be Inherited from Parent Class

### Entity Base Class
```
Entity:
  - position: Vector2 (x, y in world space)
  - sprite: SpriteRenderer (visual representation)
  - collider: Collider2D (collision boundary)
  - layer: int (sorting layer for rendering)
  - isActive: bool (alive/enabled state)
  - velocity: Vector2 (current movement vector)

Methods:
  - move(direction: Vector2)
  - getPosition(): Vector2
  - setActive(bool)
  - destroy()
```

### Collectible Base Class
```
Collectible (extends Entity):
  - itemType: enum (Key, Potion, Artifact, etc.)
  - collected: bool (picked up state)
  - collectionEffect: Action (what happens when collected)
  - spriteWhenCollected: Sprite (visual after pickup)

Methods:
  - onCollect(collector: Entity)
  - applyEffect(target: Entity)
  - isCollectable(): bool
```

### Enemy Base Class
```
Enemy (extends Entity):
  - health: int (hit points)
  - maxHealth: int (maximum HP)
  - speed: float (movement speed)
  - detectionRange: float (sight distance)
  - aiState: enum (Idle, Patrol, Chase, Attack)
  - target: Entity (current target)
  - patrolPoints: List<Vector2> (waypoints)

Methods:
  - patrol()
  - chase(target: Entity)
  - attack(target: Entity)
  - takeDamage(amount: int)
  - updateAI()
  - canSeeTarget(): bool
```

---

## Interactions Between Objects

### Player ↔ Environment
- Player moves through tiles (collision check)
- Player opens doors (state change on door)
- Player activates puzzle elements (trigger event)
- Player collects items (inventory add, object removal)
- Player records own actions (internal to Player class)

### Echo ↔ Environment
- Echo follows exact player path (uses recorded actions)
- Echo interacts with same elements as player
- Echo can simultaneously exist with player (dual character puzzle)
- Echo does NOT generate new recordings

### Player/Echo ↔ Enemy
- Enemy detects player/echo (line-of-sight check)
- Enemy chases when detected (pathfinding)
- Enemy attacks/harms player/echo (health system)
- Player avoids or uses echo as distraction

### Player/Echo ↔ Puzzle
- Stepping on pressure plate (triggers door open)
- Standing on plate + echo on other plate = unlock
- Sequential action triggers (player moves, echo replays, combined effect)

---

## RESEARCH: Wumpus to OOP Paradigm

### The Genesis of Wumpus (Procedural to OOP)

**Original Wumpus (1970s – Procedural BASIC)**
- Single player in cave (text-based grid)
- Hunt dangerous Wumpus creature
- Avoid pits and "super bats"
- Fire arrows to kill Wumpus
- Procedural: Linear flow, global variables, room state in arrays

**Procedural Elements in Original:**
```
- Room array: room[N] = state of each room
- Player position: INTEGER x, y (global)
- Wumpus location: INTEGER wx, wy (global)
- Hazards checked in linear if-statements
- Shooting logic: DO WHILE arrow moves
```

### OOP Translation for E.C.C.O

| Aspect | Procedural Wumpus | OOP E.C.C.O |
|--------|-------------------|-----------|
| **Entity Representation** | Global arrays (rooms, enemies) | Player, Enemy, Entity classes |
| **State Management** | Global variables (position, health) | Object attributes (encapsulated) |
| **Behavior** | Function calls (move(), shoot()) | Methods tied to objects (player.move()) |
| **Interactions** | Linear if-checks in main loop | Polymorphic method calls (entity.interact()) |
| **Extensibility** | Hard to add new creature types | Inherit from Entity base class |
| **Replayability** | Not inherent in procedural | Echo class stores and replays recorded actions |
| **Modularity** | Monolithic code | Separate classes for Player, Echo, Enemy, etc. |

**Key OOP Advantage for E.C.C.O:**
- Procedural: Hard-coded enemy behavior, static puzzle logic
- OOP: Enemy inherits from Entity, can override AI methods; Puzzle elements inherit from Interactable

---

## SUCCESS CRITERIA (5 Minimum)

1. **Modularity & Extensibility**
   - Each class has single responsibility (Player handles movement, not rendering)
   - Easy to add new entity types without modifying existing code
   - Evidence: No God Objects; clear separation between Player, Echo, Enemy

2. **Recording & Replay Accuracy**
   - Echo reproduces exact player movement and actions frame-for-frame
   - No drift or desynchronization over time
   - Evidence: Position tolerance < 0.01 units; action timestamp matches ±1 frame

3. **Puzzle Complexity & Replayability**
   - At least 3 distinct puzzle types (timed, spatial, sequential)
   - Puzzles solvable using player + echo cooperation
   - Evidence: Multiple solution paths, repeatable without softlock

4. **Code Efficiency & Performance**
   - No significant frame-rate drops with player + echo active
   - O(n) collision checks per frame (not O(n²))
   - Evidence: Maintains 60 FPS with 2 characters + 5+ entities

5. **Polish & User Experience**
   - Smooth animations (walk, idle, attack) with pixel-perfect rendering
   - Clear visual feedback (echo color distinct, damage indicators)
   - Intuitive controls (WASD movement, clear interaction prompts)
   - Evidence: No jittering, clear UI/UX, responsive input

6. **OOP Principles Demonstrated**
   - Inheritance: Entity base class used by Player, Enemy, Echo
   - Polymorphism: interact() method overridden per class
   - Encapsulation: Private attributes, public getter/setter methods
   - Abstraction: Abstract methods (movementBehavior) in base class

---

## RECORDING SYSTEM ARCHITECTURE (Phase 2)

### Overview: Event-Driven Recording & Playback

The recording system is built with **loose coupling** using events. Each class has a single responsibility:

```
RecordingDevice → (unlock event) → RecordingManager
                                       ├→ PlayerActionRecorder (listens)
                                       ├→ RecordingVisualsManager (listens)
                                       └→ RecordingInputHandler (listens)
                                             └→ GhostPlayer (spawned on stop)
```

### Core Classes

#### 1. **RecordingDevice**
- **Responsibility:** Represents the unlockable power; stores configuration
- **Attributes:**
  - `isUnlocked: bool` — permanently grants recording ability
  - `maxRecordingTime: float` — time limit per session (e.g., 10s)
  - `recordingTintColor: Color` — overlay tint (e.g., cyan @ 0.3 alpha)
- **Methods:**
  - `Unlock()` — called during tutorial; fires `OnDeviceUnlocked`
  - `IsUnlocked()` — checks if device is usable
  - `GetMaxRecordingTime()` — returns time limit
  - `GetRecordingTintColor()` — returns tint for overlay

#### 2. **RecordingManager** (Singleton)
- **Responsibility:** Orchestrates recording lifecycle; manages timer countdown
- **Attributes:**
  - `isRecording: bool` — current state
  - `recordingTimeRemaining: float` — counts down each frame
  - `device: RecordingDevice` — reference to device config
  - `actionRecorder: PlayerActionRecorder` — reference to recorder
- **Methods:**
  - `StartRecording()` — validates device, starts recorder, fires events
  - `StopRecording()` — stops recorder, fires events
  - `GetTimeRemaining()` — for UI display
  - `Update()` — decrements timer; auto-stops at zero
- **Events:**
  - `OnRecordingStarted` — fired when recording begins
  - `OnRecordingStopped` — fired when recording ends
  - `OnTimeUpdated(float)` — fired each frame with time remaining
  - `OnTimeExpired` — fired when time limit reached

#### 3. **RecordingVisualsManager**
- **Responsibility:** Pure visuals (overlay, timer UI, ghost color)
- **Attributes:**
  - `overlayCanvasGroup: CanvasGroup` — fades tint in/out
  - `timerText: Text` — displays remaining time
  - `ghostTintColor: Color` — derived from projectile color (matches bullet)
- **Methods:**
  - `UpdateOverlay(bool)` — fade overlay alpha smoothly
  - `UpdateTimer(float)` — format and display countdown
  - `GetGhostTintColor()` — returns ghost tint for playback
- **Listeners:** Subscribes to RecordingManager events

#### 4. **GhostPlayer**
- **Responsibility:** Plays back recorded actions with ghost visuals
- **Attributes:**
  - `recordedActions: List<PlayerAction>` — action sequence to replay
  - `isPlaying: bool` — playback state
  - `spriteRenderer: SpriteRenderer` — tinted to ghost color
- **Methods:**
  - `PlayRecording(List<PlayerAction>)` — starts playback
  - `Update()` — interpolates position, applies animation each frame
  - `ApplyGhostColor()` — tints sprite on spawn
- **Data Structure Used:** `PlayerAction` (from PlayerActionRecorder)
  - `timestamp: float` — when action occurred
  - `position: Vector2` — player position
  - `movementDirection: Vector2` — input direction
  - `isSprinting: bool` — sprint state

#### 5. **RecordingInputHandler**
- **Responsibility:** Captures input; spawns ghosts on recording stop
- **Attributes:**
  - `recordToggleKey: KeyCode` — e.g., KeyCode.R
  - `ghostPlayerPrefab: GhostPlayer` — prefab to instantiate
- **Methods:**
  - `Update()` — polls for record toggle input
  - `SpawnGhost()` — instantiates ghost with recorded action list
- **Listeners:** Checks RecordingManager.IsRecording each frame

### OOP Design Principles Applied

| Principle | Implementation |
|-----------|-----------------|
| **Single Responsibility** | Device (config), Manager (state), Visuals (UI), Ghost (playback), Input (triggers) |
| **Loose Coupling** | Event-driven; classes don't reference each other directly |
| **Encapsulation** | Private attributes; public getter methods; events for notification |
| **Inheritance** | GhostPlayer inherits MonoBehaviour; can be extended for variant ghost types |
| **Polymorphism** | Same animator/sprite logic reused for ghost playback |
| **DRY (Don't Repeat Yourself)** | PlayerActionRecorder reused; no duplication of action capture logic |

### Data Flow: Record & Playback Sequence

```
1. Tutorial → Device.Unlock() → fires OnDeviceUnlocked
   
2. Player presses R → RecordingInputHandler.Update()
   → RecordingDevice.RequestStartRecording()
   → RecordingManager.StartRecording()
   ├─ Fires OnRecordingStarted event
   ├─ PlayerActionRecorder.StartRecording() [listens]
   └─ RecordingVisualsManager overlay fade in [listens]
   
3. During recording:
   ├─ Player moves → PlayerMovement fires OnMovementInput
   ├─ PlayerActionRecorder captures in list
   └─ RecordingManager.Update() counts down timer
       └─ OnTimeUpdated → RecordingVisualsManager updates timer text
   
4. Player presses R again OR time expires
   → RecordingManager.StopRecording()
   ├─ Fires OnRecordingStopped event
   ├─ PlayerActionRecorder.StopRecording() [listens]
   ├─ RecordingVisualsManager overlay fade out [listens]
   └─ Action list saved
   
5. RecordingInputHandler detects stop → SpawnGhost()
   → GhostPlayer.PlayRecording(actions)
   ├─ Applies ghost tint color from RecordingVisualsManager
   └─ Playback begins:
       ├─ Interpolate position each frame
       ├─ Apply animation (walk/idle) from stored direction
       ├─ Flip sprite based on movement
       └─ Destroy ghost when playback complete
```

### Why This Design Satisfies Assessment Rubric

| Rubric Item | Evidence |
|-----------|----------|
| **Classes & Inheritance** | 5 classes (Device, Manager, Visuals, Ghost, Input); GhostPlayer inherits MonoBehaviour |
| **Attributes & Methods** | Each class has public/private attributes; methods with clear purposes documented |
| **Object Interaction** | Event-driven; RecordingManager coordinates; classes communicate via events |
| **Extensibility** | Easy to add: time upgrades, multiple ghosts, puzzle-specific recordings, new recording types |
| **Documentation** | Inline XML comments; class diagrams; data structures defined; OOP principles mapped |

---

## SYSTEM DIAGRAMS

### 1. Data Flow Diagram (Level 1 – Context)

```
    User Input (Keyboard)
           ↓
    ┌─────────────────────┐
    │    Game Engine      │
    │   (E.C.C.O Loop)    │
    └─────────────────────┘
      ↙        ↓        ↘
  Render   Logic    Audio
     ↓       ↓        ↓
  Screen  Physics  Speaker
  (Sprite)(Collision)(SFX)
    ↑
    └── Recorded Actions Database
         (Echo Playback)
```

### 2. Structure Chart (Hierarchical Decomposition)

```
                    E.C.C.O Game
                    /    |    \
              Input   Update   Render
              /         |  \      \
         Keyboard   Physics Collision Graphics
                      /  |  \       |
                  Position Health Inventory
```

### 3. Data Dictionary

| Data Element | Type | Purpose | Owner |
|--------------|------|---------|-------|
| `position` | Vector2 | World location | Entity |
| `recordedActions` | List\<Action\> | Playback sequence | Recordable (Player/Echo) |
| `isRecording` | bool | Recording state flag | PlayerMovement |
| `velocity` | Vector2 | Current movement vector | Entity |
| `health` | int | Hit points | Character (Player/Enemy) |
| `interactableObjects` | List\<Interactable\> | Nearby puzzle/door list | Level/Room |
| `puzzleState` | enum | Locked/Unlocked/Triggered | PuzzleElement |
| `recordingTime` | float | Elapsed time during record | PlayerActionRecorder |

### 4. Class Diagram (Simplified)

```
                    ┌─────────────┐
                    │   Entity    │
                    ├─────────────┤
                    │ -position   │
                    │ -sprite     │
                    │ -collider   │
                    ├─────────────┤
                    │ +move()     │
                    │ +render()   │
                    └──────┬──────┘
                          /|\
            ┌─────────────┼──────────────┐
            │             │              │
      ┌─────▼─────┐ ┌────▼────┐  ┌──────▼──────┐
      │   Player  │ │  Enemy  │  │    Echo     │
      ├───────────┤ ├─────────┤  ├─────────────┤
      │-velocity  │ │-aiState │  │-playbackIdx │
      │-recording │ │-aggro   │  │-isPlaying   │
      ├───────────┤ ├─────────┤  ├─────────────┤
      │+interact()│ │+patrol()│  │+syncToFrame│
      │+record()  │ │+chase() │  │+playback()  │
      └───────────┘ └─────────┘  └─────────────┘
            │
            └─────┬──────────────────────────┐
                  │ uses                     │
            ┌─────▼──────────────┐  ┌────────▼────────┐
            │ PlayerMovement     │  │ PlayerRecorder  │
            ├────────────────────┤  ├─────────────────┤
            │ -moveInput         │  │ -recordedAction │
            │ -isSprinting       │  │ -recordingTime  │
            ├────────────────────┤  ├─────────────────┤
            │ +handleInput()     │  │ +startRecord()  │
            │ +updatePhysics()   │  │ +stopRecord()   │
            └────────────────────┘  │ +getActions()   │
                                    └─────────────────┘
```

---

## SHOOTING RECORDING SYSTEM (Extended Recording)

### Overview: Recording Shooting Actions

Beyond movement and sprinting, the system now records **shooting events** with precise timing and direction. This enables ghosts to replay exact projectile attacks, opening up puzzle possibilities (synchronized fire, ricochets, etc.).

### PlayerAction Struct: Complete Data Model

```csharp
public struct PlayerAction
{
    // Movement Data
    public float timestamp;           // Time since recording started (seconds)
    public Vector2 position;          // Player world position
    public Vector2 movementDirection; // Movement input (-1 to 1 per axis)
    public bool isSprinting;          // Sprint state
    
    // Shooting Data (NEW)
    public bool didShoot;             // Whether player fired this frame
    public Vector2 shootDirection;    // Direction of shot (normalized)
}
```

### Recording Pipeline: Shooting Integration

#### 1. **PlayerShooter** (Input Detection)
- **New Field:** `private bool firedThisFrame = false;`
- **Firing Logic:**
  - `Update()` resets `firedThisFrame = false` at frame start
  - `Fire()` sets `firedThisFrame = true` and spawns projectile
  - Fires `OnShotFired` event for external listeners
- **Query Method:** `FiredThisFrame()` returns whether shot occurred this frame
- **OOP Design:** Single Responsibility — only handles shooting mechanics; doesn't manage recording state

#### 2. **PlayerActionRecorder** (Event-Driven Capture)
- **Event Subscriptions:**
  - `PlayerMovement.OnMovementInput` → updates movement direction
  - `PlayerMovement.OnSprintToggled` → updates sprint flag
  - `PlayerShooter.OnShotFired` → marks shot for next frame
- **Recording Logic (Update):**
  ```csharp
  void Update()
  {
      if (!isRecording) return;
      
      // Check if shot this frame
      bool shotThisFrame = playerShooter.FiredThisFrame();
      
      // Create action snapshot
      PlayerAction action = new PlayerAction
      {
          timestamp = Time.time - recordingStartTime,
          position = playerMovement.GetPosition(),
          movementDirection = currentMovementDirection,
          isSprinting = currentIsSprinting,
          didShoot = shotThisFrame,
          shootDirection = shotThisFrame 
              ? playerMovement.GetFacingDirection() 
              : Vector2.zero
      };
      
      recordedActions.Add(action);
  }
  ```
- **OOP Design:** Encapsulation — internal list protected; only exposes `GetRecordedActions()` copy

#### 3. **GhostPlayer** (Playback Simulation)
- **New Components:**
  - `[SerializeField] Projectile projectilePrefab` — same prefab as player uses
  - `private HashSet<int> firedAtIndices` — prevents duplicate shots
- **Replay Logic (Update):**
  ```csharp
  void Update()
  {
      // ... position interpolation & animation ...
      
      // Replay shots when recorded
      if (currentAction.didShoot && !firedAtIndices.Contains(currentActionIndex))
      {
          ReplayShot(currentAction);
          firedAtIndices.Add(currentActionIndex);
      }
  }
  
  private void ReplayShot(PlayerAction action)
  {
      // Spawn projectile at ghost position with recorded direction
      Projectile projectile = Instantiate(
          projectilePrefab, 
          transform.position, 
          Quaternion.identity
      );
      
      // Apply ghost tint color for visual sync
      SpriteRenderer sprite = projectile.GetComponent<SpriteRenderer>();
      sprite.color = spriteRenderer.color;
      
      // Initialize with recorded direction
      projectile.Initialize(
          action.shootDirection,
          projectileSpeed,
          projectileLifetime,
          projectileDamage,
          gameObject
      );
  }
  ```
- **OOP Design:** Polymorphism — GhostPlayer mimics PlayerShooter behavior without inheriting it

### Data Flow: Shooting Record & Playback

```
Player Input (Space/Mouse)
    ↓
PlayerShooter.Update() checks for fire input
    ↓
PlayerShooter.Fire():
  ├─ firedThisFrame = true
  ├─ Spawns projectile (player version)
  ├─ Fires OnShotFired event
  └─ Sets nextFireTime cooldown
    ↓
PlayerActionRecorder listens via OnShotFired
    ↓
PlayerActionRecorder.Update() records:
  ├─ Reads playerShooter.FiredThisFrame()
  ├─ Captures shootDirection = playerMovement.GetFacingDirection()
  └─ Appends PlayerAction { didShoot=true, shootDirection=vector } to list
    ↓
Recording stops (R key or time expires)
    ↓
RecordingInputHandler spawns GhostPlayer with recorded actions
    ↓
GhostPlayer.Update() replays:
  ├─ Reads recordedActions[i].didShoot flag
  ├─ If true, calls ReplayShot() with recorded shootDirection
  ├─ Spawns ghost projectile with same tint color
  └─ Both projectiles visible simultaneously (puzzle mechanic)
```

### OOP Principles Demonstrated

| Principle | Evidence in Shooting System |
|-----------|------------------------------|
| **Single Responsibility** | PlayerShooter (fire), PlayerActionRecorder (record), GhostPlayer (replay) — each class does one thing |
| **Dependency Injection** | PlayerActionRecorder receives PlayerShooter reference via GetComponent() in Awake() |
| **Event-Driven Decoupling** | PlayerShooter doesn't know about recording; fires event; PlayerActionRecorder subscribes independently |
| **Encapsulation** | `firedThisFrame` is private; only exposed via `FiredThisFrame()` getter method |
| **Polymorphism** | Both Player and Ghost can spawn projectiles; same data model (PlayerAction) drives both |
| **Code Reuse** | PlayerActionRecorder struct used identically for all action types (movement, sprint, shooting) |

### Extension Points (Future Features)

This architecture enables natural extensions:
1. **Interaction Recording** — Add `bool didInteract` + `int interactionType` to PlayerAction
2. **Projectile Types** — Store `int projectileTypeID` in action; ghost spawns correct type
3. **Multi-Ghost Coordination** — Multiple ghosts with different recordings can fire simultaneously
4. **Puzzle Mechanics** — Enemy only vulnerable when player + ghost both shoot same target
5. **Damage Recording** — Track health changes; ghosts reflect damage state during replay

---

## Next Steps (Code Evidence)

- **PlayerMovement.cs**: Implement `recordedActions` list; store Vector2 + timestamp
- **PlayerActionRecorder.cs** ✅: Event-driven recording with shooting support (IMPLEMENTED)
- **PlayerShooter.cs** ✅: FiredThisFrame() query method (IMPLEMENTED)
- **GhostPlayer.cs** ✅: Shooting replay with projectile spawning (IMPLEMENTED)
- **AnimationDriver.cs**: Already tracks movement state for Animation parameters
- **Enemy.cs** (TODO): Patrol → Chase → Attack state machine
- **PuzzleElement.cs** (TODO): Base class for doors, pressure plates, etc.

---

**Note:** This documentation is a living draft. Code examples will be embedded as the project develops.

