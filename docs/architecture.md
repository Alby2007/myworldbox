# Architecture

How Civ Mind Overhaul is wired: the mod-loading lifecycle, the two mechanisms it uses to change
game behavior, and a complete map of hooks into vanilla code.

## Components

| File | Role |
|---|---|
| `mod/Main.cs` | NML entry point (`BasicMod<ModClass>`). Loads config, applies Harmony patches, kicks off tuning. |
| `mod/Cfg.cs` | Static config class. Binds ~30 `ModConfig` sliders/switches to static fields with fallback defaults. |
| `mod/Patches.cs` | All Harmony patch classes — the control-flow side of the mod. |
| `mod/Tuning.cs` | Asset retuning — the data side. Mutates `DecisionAsset` weights, cooldowns, and delegates. |
| `mod/mod.json` | NML manifest (GUID `devin.civmind`, `targetGameBuild: 719`). |
| `mod/default_config.json` | Declares the in-game config UI (groups → sliders/switches). |
| `mod/Locales/en.json` | Human-readable labels for each config id. |

## Runtime lifecycle

```mermaid
flowchart TD
    A[WorldBox boots] --> B[NML scans game/Mods/*]
    B --> C[Compiles loose .cs files into an assembly]
    C --> D[ModClass.OnModLoad]
    D --> E[Cfg.Load<br/>reads ModConfig UI values]
    D --> F[new Harmony devin.civmind .PatchAll<br/>installs all 9 patch classes]
    D --> G[Tuning.TryApply]
    G -->|decision assets not loaded yet| H[deferred]
    H --> I[DecisionsLibrary.init runs during game init]
    I --> J[PatchDecisionsInit postfix]
    J --> K[Tuning.Apply retunes all DecisionAssets]
    G -->|assets already present| K
    K --> L[Gameplay: patches intercept vanilla calls,<br/>retuned assets bias AI decisions]
```

The deferred path matters: `OnModLoad` fires before `AssetManager.decisions_library` is populated, so
`TryApply` no-ops and the real work happens in the `DecisionsLibrary.init` postfix. Tuning is applied
exactly once per game boot and snapshots originals so it stays idempotent.

## Two mechanisms

The mod changes behavior through two complementary mechanisms — picking the right one is the
core design decision for any new feature.

### 1. Harmony patches (control flow)

Used when a vanilla *method's logic* needs to change. Three shapes appear in `Patches.cs`:

- **Prefix-supplant** — the patch does the whole job and returns `false` to skip vanilla.
  Example: `PatchFindCaptain` replaces "nearest unit" with a merit pick.
- **Prefix-veto / prefix-with-fallback** — the patch handles a special case and either consumes the
  call (`return false`) or falls through to vanilla (`return true`). Example: `PatchSetCitizenJob`
  handles draft/priority/aptitude paths then lets vanilla's round-robin run.
- **Postfix augmentation** — vanilla runs, then the patch adds effects. Example:
  `PatchCitizenTasks` tops up job slots after the city board refills.

Every patch is gated by a `Cfg.*` flag, wraps its body in `try/catch`, and on any exception
falls back to vanilla behavior (prefix → `return true`, postfix → log and swallow). A broken
patch should degrade to vanilla, never crash the game.

### 2. Asset retuning (data mutation)

Used when the right fix is just *numbers*: `DecisionAsset.weight`, `cooldown`, or swapping
`weight_calculate_custom` / `action_check_launch` delegates for richer behavior
(`Tuning.cs`). Originals are snapshotted in `_origWeight`/`_origCooldown`/`_origCustom`
so multipliers apply to vanilla values, not to previously-tuned ones.

```mermaid
flowchart LR
    subgraph Vanilla DecisionAsset
        W[weight]
        C[cooldown]
        CW[weight_calculate_custom]
        CL[action_check_launch]
    end
    T[Tuning.Apply] -->|x multiplier| W
    T -->|x multiplier| C
    T -->|wrap with new delegate| CW
    T -->|replace e.g. run_away morale check| CL
```

Rule of thumb: **retune for "more/less often" and "different score"; patch for "different procedure."**

## Patch map

Every hook point, its mechanism, and its config gate:

| Patch class | Vanilla target | Type | Config gate | Feature |
|---|---|---|---|---|
| `PatchDecisionsInit` | `DecisionsLibrary.init` | postfix | — | Triggers `Tuning.Apply` once assets exist |
| `PatchSetCitizenJob` | `City.setCitizenJob` | prefix | `AptitudeHiring`, `EmergencyDraft` | Draft bypass + aptitude hiring |
| `PatchWarriorStillOk` | `City.checkIfWarriorStillOk` | prefix | `EmergencyDraft` | Stay mobilized in danger |
| `PatchEndJob` | `Actor.endJob` | prefix+postfix | `JobXpAmount > 0` | Job XP (captures `citizen_job` in `__state`) |
| `PatchCitizenTasks` | `CityBehCheckCitizenTasks.execute` | postfix | `Extra*`/`Draft*` options | Pressure staffing + militia slots |
| `PatchSimGlobalsInit` | `SimGlobals.init` | postfix | `War{Timeout,MinWarriors,Peace}Years` | War tempo globals |
| `PatchOkToSendArmy` | `City.isOkToSendArmy` | prefix-supplant | `MarchFill` | March threshold |
| `PatchFindCaptain` | `Army.findCaptain` | prefix-supplant | `MeritCaptains` | Merit-based captain |
| `PatchGetWarTarget` | `DiplomacyHelpers.getWarTarget` | prefix-supplant | `SmartWarTarget` | Scored war targets |
| `PatchFindTargetCity` | `CityBehCheckAttackZone.findTargetCity` | prefix-supplant | `SmartCityTarget` | Scored siege targets |
| `PatchWarUpdate` | `War.update` | postfix | `WarStalemateYears > 0`, `WarscoreSurrender`, `SeparatePeace` | Stalemate auto-peace + warscore surrender + collapsing participants `leaveWar` |
| `PatchArmyTick` | `Army.checkCaptainExistence` | prefix+postfix | `MoraleShock`, `FocusFire`, `Duels`, `Pillage`, `DesertionWarYears`, `WoundedRotation`, `Formations`, `SiegeRelief`, `ArmyVeterancy`, `ArmyRetreat`, `ArmyExhaustion` | Per-army tick dispatcher: shock bookkeeping → retreat/exhaustion → focus fire → duels → pillage → desertion → wounded rotation → kiting → relief marches → veterancy (offensive ticks skipped while `IsRetreating`) |
| `PatchCheckAttack` | `BehCityActorCheckAttack.execute` | postfix (ref `__result`) | `ArmyRetreat` | Redirects `beh_tile_target` to own `city.getTile()` while `WarFx.IsRetreating` — the whole army walks home on vanilla pathing |
| `PatchGetHit` | `Actor.getHit` | prefix (ref `pDamage`, ref `pCheckDamageReduction`, `__state`) + postfix | `HomeField`, `CombatCrits`, `ArmorPen`, `Stagger` | Home-field reduction → dodge → crit → armor pen → `stunned` on heavy hits |
| `PatchWarriorTaxi` | `BehCityActorWarriorTaxiCheck.execute` | postfix | `NavalInvasions` | Follow embarked captain cross-island |
| `PatchIncreaseKills` | `Actor.increaseKills` | postfix | `BerserkerRage` | Kill-streak → `rage` status + war-hero kill tally |
| `PatchEndWar` | `WarManager.endWar` | postfix | `Reparations`, `WarHeroes`, `TributeInstallments`, `VictoryFireworks` | Loser gold → winner capital + hero traits + `PeaceTime` + `Tribute` deal record + `festive_spirit` on winner's citizens |
| `PatchSuccessionClan` | `SuccessionTool.getKingFromRoyalClan` | postfix (ref `__result`) | `MeritSuccession` | Best-stat royal-clan heir |
| `PatchSuccessionLeaders` | `SuccessionTool.getKingFromLeaders` | postfix (ref `__result`) | `MeritSuccession` | Best-stat leader heir |
| `PatchAllianceTarget` | `DiplomacyHelpers.getAllianceTarget` | prefix-supplant | `BalanceAlliances` | Nearest-valid alliance partner |
| `PatchFlee` | `BehFindRandomFarTile.execute` | prefix | `RallyFlee` | Flee toward captain / own city |
| `PatchTryWarrior` | `City.tryToMakeWarrior` | prefix | `MilitiaReserve` | Peacetime warrior fill cap |
| `PatchSetLeader` | `City.setLeader` | prefix (ref `pActor`) | `MeritLeaders` | Stewardship-scored leader swap |
| `PatchCheckLoyalty` | `CityBehCheckLoyalty.execute` | postfix | `Uprisings`, `SiegeStarvation`, `Assimilation`, `Wages`, `TributeInstallments` | Uprising trigger + siege food drain + culture/religion conversion + soldier-wage drain + yearly tribute payment |
| `PatchIsWarNeeded` | `DiplomacyHelpers.isWarNeeded` | postfix (ref `__result`) | `KingPersonality`, `WarChest`, `TwoFronts` | Pacifist veto / warlord bypass / treasury war funding / supreme second fronts |
| `PatchSetKing` | `Kingdom.setKing` | postfix | `PretenderWars`, `CollapsePressure` | Pretender revolt / province drift |
| `PatchPlotsInit` | `PlotsLibrary.init` | postfix | `AntiHegemon`, `PeacePlots` | Wraps `alliance_destroy.check_is_possible` + retunes `attacker_stop_war` (reuse loop preserved) |
| `PatchSupplyCities` | `CityBehSupplyKingdomCities.execute` | prefix+postfix | `WarTribute`, `SupplyWarMult`, `FamineRelief` | Prefix retargets supply to hungriest city; postfix: wartime tribute + faster aid |
| `PatchGiveTax` | `BehActorGiveTax.execute` | prefix+postfix | `TaxUnrest`, `Corruption` | Tax-rate happiness dip + `__state` king-loot diff skim to leader |
| `PatchCityTarget` | `BehFindNearbyPotentialCivCityToJoin.getPotentialCityNearby` (static) | prefix-supplant | `LaborMigration` | Migrants pick cities by open job slots + kingdom + distance |
| `PatchMercenary` | `Actor.joinCity` | postfix | `Mercenaries` | City pays `MercenaryCost` → migrant `money` → `tryToMakeWarrior` |
| `PatchDockTarget` | `ai.ActorTool.getDockTradeTarget` (static) | postfix (ref `__result`) | `Embargo` | Nulls docks whose kingdom is at war with any alliance member |
| `PatchBorderSteal` | `CityBehBorderSteal.execute` | postfix | `BorderPressure` | Extra wartime zone steal |
| `PatchBoatTrade` | `BehBoatMakeTrade.execute` | postfix | `TradeIncome` | Distance-scaled trade gold to boat inventory |
| `PatchGiveItem` | `City.giveItem` (static) | prefix+postfix | `EquipmentMarket`, `CombinedArms` | Wartime melee-heavy armies pick from ranged-only sublist (vanilla body replicated) + `__state` equipped-value diff → charge `money` |
| `PatchFindTarget` | `BaseSimObject.findEnemyObjectTarget` | postfix (ref `__result`) | `FinishWounded` | Rescan enemies → prefer wounded actors |
| `PatchUpdateCapture` | `City.updateCapture` | postfix | `SiegeMomentum` | `_capture_ticks` momentum per extra attacker, capped at 10 |
| `PatchStartWar` | `DiplomacyManager.startWar` | postfix | `TruceBreak` | Stamp `WarFx.TruceBreak` on early re-declaration |
| `PatchOpinionInit` | `OpinionLibrary.init` | postfix | `BorderFriction`, `TruceBreak` | Wrap `opinion_borders.calc` + inject `opinion_truce_breaker` |
| `PatchCombatInit` | `CombatActionLibrary.init` | postfix | `VetSpecials` | Wrap every `can_do_action` — veterans bypass failed checks |
| `PatchMeatSource` | `BehFindMeatSource.execute` | prefix-supplant | `SmartPrey`, `PackHunting`, `DangerousFrontier` | Replicates the meat scan (meat-type filter, attackability, size gate, same island) with wounded/young scoring; pack followers adopt the alpha's `beh_actor_target`; empty scan → starving predators may target civs |
| `PatchWorldBehInit` | `WorldBehaviourLibrary.init` | postfix (`Once.PerInit`) | `DiverseSpawn`, `AnimalSpawnMult`, `BreedingCapMult` | Wraps `unit_spawn.action` with a diversity-aware replication (filters over-cap species before rolling), retunes `interval`, rewrites `animal_breeding_close_units_limit` |

Plus tuning-only features with no dedicated patch class (see `Tuning.Apply`):
`find_city_job` demand weight, expansion/social/reproduction weights, housing-gated
reproduction, `run_away` morale check (also consumes `WarFx.Shaken`/`WarFx.AwaySince`
and applies the commander aura), `warrior_train_with_dummy`, `check_warrior_transport`
retunes, the `check_join_city`/`check_join_empty_nearby_city` refugee weight wraps,
the civilian-taxi retunes (`try_to_return_to_home_city`, `boat_transport_check`),
`try_to_read` retune + relaxed launch gate, `put_out_fire` intensity-scaled weight,
the wildlife retunes (`diet_meat`/`diet_blood`/`diet_meat_insect` cooldown +
hunger-scaled weight, `family_alpha_move`, `family_group_join_or_new_herd`,
`run_away_from_carnivore`),
and the economy retunes: `give_tax`, `make_items`, `try_to_take_city_item` (danger
spike), `boat_trading`/`boat_fishing` (wartime blockade wrap).

`WarFx` (in `Patches.cs`) is a tiny shared-state holder: `Shaken: armyId → Time.time` expiry,
written by `PatchArmyTick` on field captain-death and read by the `run_away` delegate;
`HeroKills: actorId → wartime kill count`, fed by `PatchIncreaseKills` and consumed by
`PatchEndWar`; `PeaceTime: kingdomId → world time of last war end`, read by
`PatchCitizenTasks` for the reconstruction surge; `Broke: kingdomId → Time.time` expiry,
stamped by `PatchCheckLoyalty` when a city can't make payroll and read by `TickDesertion`
for the unpaid-mutiny desertion multiplier; `VetArmies: armyId → Time.time` promotion stamp,
written by `TickVeterancy` and read by `ReworkRetreat` (aura boost + shock resist);
`TruceBreak: kingdomId → currentYear` of the last early re-declaration, written by
`PatchStartWar` and read by `opinion_truce_breaker`; `Tribute: kingdomId → TributeDeal
{Winner, Expiry, LastPaid}`, written by `PatchEndWar` on decisive wins and paid down
yearly by `PatchCheckLoyalty` at the loser's capital; `RetreatUntil: armyId → Time.time`
expiry, written by `TickRetreat` and read by `PatchCheckAttack` + the tick dispatcher
(offensive ticks skipped while live); `AwaySince: armyId → Time.time` departure stamp —
not an expiry, so it is pruned by dead-army lookup rather than the shared `Prune` —
written by `TickExhaustion`, read by `TickDesertion` and the `run_away` delegate via
`IsExhausted`.

`Rebel` (in `Patches.cs`) holds the shared rebellion plumbing — reflected `makeOwnKingdom`,
`startWar`, `City.kingdom`, `setCulture`, `setReligion`, `addStatusEffect` (`AddStatus`
helper) handles plus the once-per-city `Revolted` set — used by `PatchCheckLoyalty`
(occupied uprisings), `PatchSetKing` (pretender wars / province drift),
`PatchBorderSteal` (kingdom lookups), and `PatchEndWar` (victory `festive_spirit`).

## Reflection access

The mod reads `internal`/`private` members via cached `AccessTools` handles
(static readonly `FieldInfo`/`MethodInfo`, resolved once per patch class). Everything goes through
null-checked invocation — if a game update renames a member, the patch degrades gracefully.

`Guard.cs` adds three shared helpers:

- **`Ref`** — wraps every `AccessTools.Field`/`Method`/`FieldRefAccess` lookup and records the
  outcome. `Ref.Report()` (called from `Main` after `PatchAll`) force-runs the static constructors
  of all `[HarmonyPatch]` classes and any type holding `FieldInfo`/`MethodInfo` fields, then logs
  `[CivMind] reflection: N/M targets resolved` — or a `FAILED:` list naming the broken members when
  a game update renames something.
- **`First`** — `First.Fire("label")` logs `[CivMind] first fire: <label>` once per feature at the
  point it actually changes game state. Grepping `first fire:` in `Player.log` shows which features
  are alive without auditing code.
- **`Once`** — `Once.PerInit(key)` returns true exactly once; replaces hand-rolled `_done` flags on
  `Library.init` postfixes that install delegate wraps (a second wrap would stack multipliers).

| Member | Access | Used by |
|---|---|---|
| `City._timer_warrior` | field | `PatchSetCitizenJob` (draft cooldown check) |
| `City.tryToMakeWarrior` | method | `PatchSetCitizenJob` (warrior conversion) |
| `Actor.isProfession` | method | `PatchSetCitizenJob` |
| `BaseSimObject.stats` | field | `PatchSetCitizenJob` (aptitude stats) |
| `Actor.addExperience` | method | `PatchEndJob` |
| `City.hasBuildingToBuild`, `hasStorageBuilding`, `hasBuildingType` | method | `PatchCitizenTasks` |
| `City.kingdom`, `City.city_center` | field | `PatchFindTargetCity`, `PatchArmyTick` (pillage) |
| `Actor.hasWeapon` | method | `Tuning.ReworkRetreat` |
| `Actor.attack_target`, `Actor.has_attack_target` | field | `PatchArmyTick` (focus fire) |
| `Actor.data` | field | `PatchArmyTick` (pillage — `ActorData.loot` set), `PatchIsWarNeeded` (war chest), `PatchGiveItem` (`ActorData.money`) |
| `Actor.beh_building_target` | field | `PatchBoatTrade` (foreign dock for distance) |
| `BaseSimObject.stats` → `BaseStats["multiplier_supply_timer"]` | field/indexer | `PatchSupplyCities` (replicated `updateSupplyTimer`) |
| `BaseBuildingComponent.building` | field | `PatchDockTarget` (dock → city for embargo check) |
| `City.tryToMakeWarrior` | method | `PatchMercenary` (instant enlistment) — also patched by `PatchTryWarrior` |
| `BaseSimObject.addStatusEffect(string,float,bool)` | method | `PatchIncreaseKills`, `PatchGetHit`, `Rebel.AddStatus` (shared — fireworks) |
| `Actor.getHit` | internal override | `PatchGetHit` (patched by name, `ref pDamage`) |
| `City.kingdom` | field | `PatchEndWar`, `PatchCheckLoyalty` |
| `City.zones` | field | `PatchFlee` (rally fallback target) |
| `Actor.beh_tile_target` | field | `PatchFlee`, `PatchCheckAttack` (retreat redirect) |
| `City.makeOwnKingdom` | method | `Rebel` (secession — uprisings, pretenders, drift) |
| `DiplomacyManager.startWar` | method | `Rebel` (rebellion wars) — also patched by `PatchStartWar` (truce-break stamp) |
| `EnemiesFinder.findEnemiesFrom` | static method | `PatchFindTarget` (wounded-target rescan), `PatchArmyTick` (`TickRetreat` outmatched count) |
| `BaseSimObject.canAttackTarget` | method | `PatchFindTarget` (validity filter for swapped targets) |
| `Actor.attack_target`, `Actor.has_attack_target`, `Actor.beh_tile_target` | field | `PatchArmyTick` (focus fire, kiting, relief marches) |
| `City._capture_ticks`, `_capturing_units`, `being_captured_by` | field | `PatchUpdateCapture` (siege momentum) |
| `City._storage_version` | field | `PatchGiveItem` (replicated `giveItem` body bumps it) |
| `Actor.data` | field | `Rebel`/`PatchIsWarNeeded` (`ActorData.money` war chest) |
| `City.setCulture`, `City.setReligion` | method | `PatchCheckLoyalty` (assimilation) |
| `City.addZone`, `City.removeZone` | method | `PatchBorderSteal` (zone grab) |
| `BehFindMeatSource._meat_target_type`, `._check_for_factions` | private field | `PatchMeatSource` (target-type filter, faction check) |
| `BaseSimObject.a` | internal field | `PatchMeatSource` (`beh_actor_target` → `Actor`) |
| `Actor.beh_actor_target` | internal field (FieldRef) | `PatchMeatSource` (scan result + pack-hunt override) |
| `MapBox.islands_calculator`, `.map_chunk_manager` | internal field | `PatchWorldBehInit` (spawn replication) |
| `IslandsCalculator.getRandomIslandGround(bool)` | internal method | `PatchWorldBehInit` |
| `TileIsland.getRandomTile` | internal method | `PatchWorldBehInit` |

See [patching-guide.md](patching-guide.md) for the conventions in detail.
