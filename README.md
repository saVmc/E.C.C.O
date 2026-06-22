# E.C.C.O.
### Entity Containment Control Operations

NOTE THIS README WAS GENERATED WITH THE HELP OF AI

A top-down 2D roguelike shooter. Survive endless waves of hostile geometric entities, level up, unlock weapons, and build ability combinations to push as deep as you can.

---

## How to Play (from the GitHub Release)

1. Go to the [Releases page](https://github.com/saVmc/E.C.C.O/releases) and download the latest `.zip`.
2. Extract the `.zip` to any folder on your PC.
3. Inside the extracted folder, run **`E.C.C.O.exe`**.
4. No installation required. The game is standalone.

> **Windows only.** If Windows SmartScreen blocks the exe, click **More info** then **Run anyway**.

---

## Controls

| Action | Key / Input |
|---|---|
| Move | `W A S D` or Arrow Keys |
| Sprint | Hold `Left Shift` (1.5x speed) |
| Aim | Mouse cursor |
| Fire | `Space` or `Left Mouse Button` |
| Reload | `R` |
| Ability Slot 1 | `Q` |
| Ability Slot 2 | `E` |
| Ability Slot 3 | `C` |
| Ability Slot 4 | `F` |
| Skip / Confirm (menus) | `Enter` or `Escape` |

---

## Game Loop

**Intro > Tutorial (first run) > Difficulty Select > Arena**

- Enemies arrive in numbered waves. Each wave is larger and spawns faster than the last.
- Kill enemies to collect **EXP orbs**. Fill the bar to level up.
- On **level up**, choose one upgrade: a new weapon tier or a new ability tier.
- You have **4 ability slots**; choose which abilities to equip as you find them.
- Every **5 waves** a **boss** spawns before the normal wave begins. Bosses drop an ability pickup.
- There is no time limit. Survive as long as possible.

### Difficulty Modes

| Mode | Enemy Health | Enemy Speed | Unlock Condition |
|---|---|---|---|
| Easy | x1.0 | x1.0 | Always available |
| Medium | x1.5 | x1.15 | Clear Wave 10 on Easy |
| Hard | x2.2 | x1.35 | Clear Wave 10 on Medium |

### Wave Scaling

- Base wave size: **10 enemies**, growing by **+4 per wave**
- Spawn interval: starts at **2.5 s**, reduced by **0.1 s per player level** and **0.05 s per wave** (floor: 0.22 s)
- Live enemy cap: **8 + (2 x level)**, max 100
- 10 second rest between waves

### Enemy Scaling (per player level)

- Health: **+14%**
- Speed: **+2.5%**
- EXP value: **+8%**

---

## Weapons

Weapons are selected at game start and upgraded via level up choices. Each weapon has **5 star tiers (★1 to ★5)** that stack new mechanics on top of the base behaviour.

**Unlock note:** The **Zarkinator** unlocks permanently after clearing Wave 6 on any difficulty.

### Core Mechanics Available Across Weapons

| Mechanic | Effect |
|---|---|
| Burst fire | Rapid follow up shots after the first |
| Triple shot | Three projectiles per trigger pull |
| Double barrel / Echo | Second delayed shot offset from the first |
| Piercing | Projectile passes through enemies |
| Ricochet | Bounces off walls and enemies |
| Explosive | Area of effect damage on impact |
| Burn / Napalm | Damage over time on hit |
| Chain kill | Projectiles scatter from killed enemies |
| Shockwave on kill | Area knockback and damage when an enemy dies |
| Executioner | Instant kill below a health threshold |
| Ammo on kill | Bullets refunded from kills |
| Suppressive fire | Nearby enemies slowed while firing |
| Infinite magazine | No reload; ammo restores on movement |
| Enemy marking | Hit enemies take increased damage |
| Speed boost on fire | Short movement speed surge per shot |

### Weapon Profiles

**Pistol:** Reliable single shot sidearm. Upgrades move into executioner and chain kill territory.

**Shotgun:** 5 pellets, 30° spread, 3 round magazine. Upgrades progressively tighten spread, add crowd control, and end with a devastating knockback finish.

| Star | Upgrade name | What it adds |
|---|---|---|
| ★1 | Breacher Shells | Pellets pierce 1 enemy; pellets 10% faster; spread tightened 10° |
| ★2 | Concussion Load | Each pellet slows hit enemies 60% for 2 s; spread tightened 5° more; +1 dmg |
| ★3 | Adrenaline Pump | Each shot grants a 50% movement speed boost for 1.2 s; +1 dmg |
| ★4 | War Drum | +5 magazine; 40% faster reload; 15% faster fire; +1 dmg |
| ★5 | Blastback | Every pellet physically launches enemies away (heavy knockback); bullet trails; +2 dmg |

**Bulldog:** Burst fire pistol. ★3 adds an echo burst where a delayed copy of the full burst fires after the first. ★5 reaches maximum burst count with infinite ammo mechanics.

**Minigun:** High fire rate, no reload. Trades accuracy for volume. Late tiers add suppression and burn.

**Flamethrower:** Short range continuous napalm. Upgrades increase range, ignite radius, and add wildfire spread between enemies.

**Zarkinator:** Unlocked at Wave 6. Special projectile behaviour with a custom fire pattern. Full upgrade path rewards accurate aim.

---

## Abilities

Abilities drop from bosses and level up choices. You can hold **up to 4** at a time. Each ability has **6 star levels (★0 to ★5)**. Higher stars are chosen by picking the same ability again from a level up menu.

---

### Momentum (Dash)

**Hotbar key:** Assigned slot | **Role:** Mobility / Escape

Dash in your current movement direction. At higher stars you phase through enemies and leave a damaging trail behind you.

| Star | Changes |
|---|---|
| ★0 | Short dash (4 units) |
| ★1 | Longer dash (5 units) |
| ★2 | Dash distance 6 units; x2 speed boost for 1 s after landing |
| ★3 | Phase through enemies during the dash |
| ★4 | Leaves a fire trail that damages enemies |
| ★5 | Maximum dash (8 units), phase, fire trail, 2 s speed boost |

---

### Chupa Sword (Lunge)

**Hotbar key:** Assigned slot | **Role:** Melee / Burst damage

Lock onto the nearest enemy and lunge at them with a blade strike. High single target damage, especially lethal at high stars.

- Base damage: **8** | Lunge range: **12 units** | Lunge speed: **20 u/s**

| Star | Changes |
|---|---|
| ★0 | Basic lunge (8 dmg) |
| ★1 | 13 dmg; pierces through multiple enemies in a line |
| ★2 | 17 dmg; afterimage pulses (35% dmg) persist for 2.2 s |
| ★3 | 21 dmg; kills chain to nearby enemies; cooldown resets on kill |
| ★4 | 26 dmg; execute enemies below 35% health instantly |
| ★5 | 34 dmg; **cinematic finisher**: slow motion cutscene, camera work, shockwaves, and a full slash animation |

---

### Sentry (Turret)

**Hotbar key:** Assigned slot | **Role:** Sustained damage / Zoning

Deploy a stationary auto turret 1.5 units in front of you. It targets the nearest enemy automatically. Press the key again to recall and redeploy it.

- Cooldown (retrieved normally): **15 s** | Cooldown (destroyed): **45 s**
- Detection radius: **6 units** | Base damage: **5** | Base fire interval: **0.8 s**

| Star | Changes |
|---|---|
| ★0 | Single barrel, basic turret |
| ★1 | 22 HP; fire interval 0.65 s; deploying briefly boosts your movement speed |
| ★2 | Dual barrel firing two simultaneous shots at a 28° spread |
| ★3 | Projectiles never expire; ricochet (1 bounce); stun pulse every 3 s (radius 3, stuns for 1 s) |
| ★4 | Triple ricochet bounces |
| ★5 | **Overclock mode:** 3 fire directions, 5 ricochets, 50 HP, 0.35 s fire interval, stun every 1.5 s, fire rate surge on deploy plus camera shake |

---

### Forcefield (Energy Shield)

**Hotbar key:** Assigned slot | **Role:** Defense / Nova burst

Activate a personal energy shield that absorbs incoming damage. When it expires or breaks, it erupts in a nova whose damage scales with how much it absorbed.

- Base shield HP: **10** | Base duration: **6 s** | Base nova damage: **8** | Nova radius: **3.5 units**

| Star | Changes |
|---|---|
| ★0 | 10 HP, 6 s, 8 dmg nova |
| ★1 | 15 HP, 7 s, 12 dmg base nova |
| ★2 | 20 HP; nova damage scales harder with absorbed hits |
| ★3 | 25 HP, 8 s; nova **stuns** all enemies it hits for 1.8 s |
| ★4 | 30 HP; press the ability key again to **detonate early** for a stronger nova |
| ★5 | 40 HP; shield **refreshes to 50% HP once** when broken; massive nova with cinematic void tear and bloom visuals |

---

### Lure Bomb (Grenade)

**Hotbar key:** Assigned slot | **Role:** Crowd control / Area damage

Throw a bomb to the mouse cursor position (max range 12 units). It lures nearby enemies toward it, then detonates.

- Lure radius: **5 units** | Lure duration: **1.5 s** | Blast radius: **4.5 units** | Base damage: **28**

| Star | Changes |
|---|---|
| ★0 | Lure then blast |
| ★1 | Lure radius 7 units, lure duration 2 s |
| ★2 | Leaves a burn zone after the explosion (2.5 s; 4 dmg every 0.5 s) |
| ★3 | Fires 3 fragment grenades outward after the main blast |
| ★4 | Blast radius 5.5, 42 dmg, 5 fragments |
| ★5 | **The Duck Cinematic**: a rubber duck orbits the bomb with sparkles while enemies are lured (1.5 s), camera zooms in as it shakes (0.7 s), hard freeze and detonation, then camera pulls back. 55 dmg, radius 6, 1.75x vulnerability on survivors, 1.2 s stun |

---

### Singularity Rift (Black Hole)

**Hotbar key:** Assigned slot | **Role:** Crowd control / Scaling burst

Open a gravitational rift at the cursor that pulls enemies inward, then collapses in an explosion. Damage scales with the number of enemies consumed.

- Pull radius: **7 units** | Pull force: **6 u/s** | Duration: **4.5 s** | Collapse radius: **3 units**

| Star | Changes |
|---|---|
| ★0 | Basic pull; collapses on expiry |
| ★1 | Pull force 8 u/s |
| ★2 | Duration 5.5 s, radius 8.5 units, stronger collapse |
| ★3 | **Orbital phase:** at 30 to 72% of duration, enemies orbit the rift; pull force 10 |
| ★4 | Absorbs incoming player damage to charge the collapse (1.5 HP/s near centre); ricochet projectiles on collapse; pull force 12 |
| ★5 | **Event Horizon Cinematic**: camera pulls back as space darkens, fracture lines spread, full rift activates with spaghettification and 0.15x slow motion, hard freeze and impact flash, multi-wave detonation with echo pulses and expanding rings, then camera return. Pull force 14; absorbs enemies for healing (+2 HP per enemy); massive AoE with debris trails |

---

## Progression and Records

Your best run per difficulty is saved automatically:
- Highest wave reached
- Weapon used
- Time survived

Medium and Hard unlock permanently once their required wave is cleared once, across any session.

---

## Tips

- The **Tutorial** can be skipped on repeat playthroughs by pressing `Escape` at any terminal prompt.
- Boss ability pickups despawn if left too long; collect them before clearing the remaining wave enemies.
- **Forcefield** nova scales off absorbed damage. Let it tank big hits rather than activating it as a pre-emptive buff.
- **Singularity Rift ★4+** can sustain you through heavy waves by converting incoming damage into collapse power while you stand near the rift centre.
- **Sentry** placement is always directly in front of your facing direction. Reposition before deploying in corridors.
- **Chupa Sword ★3** cooldown resets on kill, letting you chain lunges across grouped enemies without waiting.
