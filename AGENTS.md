# WorldBox modding workspace

## Environment
- Game: `C:\Program Files (x86)\Steam\steamapps\common\worldbox` (Steam app 1206560, ~0.51.x / build 719, Unity 2022.3.60f1 Mono)
- Mod loader: NeoModLoader (NML) 1.2.0.1 installed at `worldbox_Data\StreamingAssets\mods\NeoModLoader.dll`
- Harmony is embedded in NML (`0Harmony.dll` resource, namespace `HarmonyLib`) — usable in mod source at runtime; for offline builds reference `build\0Harmony.dll`
- NML mods live in `<game>\Mods\<ModName>\` with `mod.json` + loose `.cs` files (NML compiles source at startup — no prebuilt dll needed)
- Experimental Mode must be ON in game settings or no mods load

## Layout
- `mod\` — the mod source (junctioned to `<game>\Mods\CivMindOverhaul`, so edits are live)
- `build\build.csproj` — offline typecheck: `cd build && dotnet build` (references real game dlls; netstandard2.1)
- `decompiled\` — full `Assembly-CSharp.dll` decompile via `ilspycmd -p -o decompiled <dll>` (tool: `dotnet tool install -g ilspycmd --version 9.1.0.7988`)
- `game-api\` — curated decompiled classes + `types.txt` (full type list)
- `docs\` — architecture docs + Mermaid diagrams (start at `docs\README.md`)

## Civ/jobs AI architecture (0.51.2)
- Unit AI = utility decisions: `UtilityBasedDecisionSystem.useOn(Actor)` scores `DecisionAsset`s (`weight` or `weight_calculate_custom`, softmax `e^w` weighted random), filtered by `DecisionChecks`, cooldowns, and `NeuroLayer` priority tiers (`critical` forces priority-only mode; `isAbleToSkipPriorityLevels` → 80% chance to bypass)
- Decision lists: `DecisionsLibrary` (`list_only_civ`, `list_only_city`, `list_only_children`, `list_only_animal`, `list_others`) + per-`ActorAsset` + per-actor arrays
- Professions: `ProfessionAsset` (Nothing/Unit/Warrior/King/Leader) each carrying extra decision ids (`t.addDecision`)
- City job board: `CityBehCheckCitizenTasks` (city task `do_checks`) refills `city.jobs` (`CitizenJobs` slots vs `occupied`) gated by `DebugConfig.isOn(DebugOption.SystemCityTasks)` and `population_adults`
- Worker side: `find_city_job` decision → `BehCityActorFindNewJob` → `City.setCitizenJob` (warrior conversion → `list_priority_high` → `list_priority_high_food` if starving → round-robin `list_priority_normal`)
- `city.jobs.addToJob` is public — postfix on `CityBehCheckCitizenTasks.execute` can top up slots safely
- Army cap = `status.warrior_slots` synced from `countCurrentJobs(attacker)`; conversions gated by `isArmyFull`/`checkCanMakeWarrior`/`tryToMakeWarrior` (private), `_timer_warrior` re-draft cooldown (15s convert / 30s demobilize)
- Warrior stand-down: `checkIfWarriorStillOk` demobilizes when `isArmyOverLimit` or `!hasEnoughFoodForArmy`
- Aptitude stats: `a.warfare`/`stewardship`/`intelligence`/`diplomacy` public int props; `stats` field is internal on `BaseSimObject` → reflect once, indexer `stats["construction_speed"]` is public
- `CitizenJobAsset.should_be_assigned` (public delegate `CitizenJobCondition`) = per-job assignment filter hook, no patch needed
- Job end: `end_job` task → `BehEndJob` → `Actor.endJob()` (clears `citizen_job` — capture it in a Harmony prefix `__state` before postfix); `addExperience` is internal

## Mod features (v0.1)

1. Demand-aware `find_city_job` weight + cooldown; retuned expansion/social decisions (Tuning.cs, `DecisionsLibrary.init` postfix)
2. Extra job slots under shortage + militia `attacker` slots when `isInDanger` (PatchCitizenTasks)
3. Aptitude-based hiring + emergency draft bypassing `_timer_warrior` (PatchSetCitizenJob prefix, mirrors vanilla flow: warrior conversion → priority lists → aptitude pick → vanilla fallback)
4. Stay-mobilized during danger (PatchWarriorStillOk)
5. Housing-gated reproduction — homeless × NoHouseReproMult (Tuning.cs)
6. Job XP on `endJob` (PatchEndJob)

## Combat/war AI architecture (0.51.2)
- Wars are **plots**: `new_war` PlotAsset (king lvl≥3, warfare≥6, renown≥50, 20g) → `DiplomacyHelpers.isWarNeeded` gates (war timeout, >10 warriors, <4 cities needs 60% pop) → `getWarTarget` (nearest valid kingdom: own warriors ≥ defender, reachable, peace elapsed, bad opinion) → `DiplomacyManager.startWar` → `WarTypeAsset` (normal/spite/inspire/rebellion/whisper_of_war; `total_war`, `alliance_join`, `can_end_with_plot` flags)
- Siege: `CityBehCheckAttackZone` → `findTargetCity` = nearest reachable war city; `isOkToSendArmy` = army fill ≥ 0.7; `Army.findCaptain` = nearest/random unit (no merit); leader→`warrior_army_leader_move_to_attack_target`, followers→`warrior_army_follow_leader` (unique decisions → `list_others` → evaluated for ALL non-boat actors)
- Naval invasions via `check_warrior_transport`/`BehCityActorWarriorTaxiCheck`; attackers focus `type_watch_tower` buildings (BehCityActorCheckAttack)
- `War.update` (every frame via WarManager): ends only on main-side death / 0 cities after 10y — **no stalemate peace**; `eventFriendship`/`eventSpite` plots can end/start wars
- `SimGlobals.m` = `public static SimGlobalAsset` — `diplomacy_years_war_timeout`(5), `diplomacy_years_war_min_warriors`(10), `minimum_years_between_wars`(5), `minimum_kingdom_age_for_attack`(5) — pure data tweaks, patch `SimGlobals.init` postfix
- `run_away` decision: `isFighting() && healthRatio > 0.2` — vanilla flees ONLY while healthy, wounded fight to death
- Training: `warrior_train_with_dummy` (needs `type_training_dummies`) → `BehActorTryToAddRandomCombatSkill` 15% chance of `pot_traits_combat` trait
- `ListPool<T>` — struct pool; dispose in `finally` (no `using` decls for old-compiler safety); iterate `pool[i]`

## Mod features (v0.2 — combat & war)

7. Morale retreat — `run_away` launch check replaced: flee when `health < RetreatHealth` (pacifist ×1.6, bloodlust/savage/veteran ×0.5, +warfare×0.003) (Tuning.cs)
8. Merit captains — `Army.findCaptain` prefix-supplant: max `warfare*2 + level` (PatchFindCaptain)
9. Smart war targets — `DiplomacyHelpers.getWarTarget` prefix-supplant: same filters, score = dist × (0.4 + def/own) × (hasWars ? 0.6) (PatchGetWarTarget)
10. Smart siege targets — `findTargetCity` prefix-supplant: dist × (0.5 + defenders/own) (PatchFindTargetCity; `City.kingdom`/`city_center` are internal → reflected)
11. `isOkToSendArmy` fill threshold → Cfg.MarchFill (PatchOkToSendArmy)
12. Stalemate peace — `War.update` postfix: age ≥ WarStalemateYears && no fighting → `endWar(Peace)`, throttled by `Randy.randomChance(0.02)` (PatchWarUpdate)
13. War tempo — `SimGlobals.init` postfix applies timeout/min-warriors/peace-years (PatchSimGlobalsInit)
14. Training retune — `warrior_train_with_dummy` weight/cooldown (Tuning.cs)

## Mod features (v0.3 — war tactics, 3 packs sharing `PatchArmyTick` on `Army.checkCaptainExistence` postfix; dispatch order: shock bookkeeping → focus fire → duels → pillage → desertion)

15. Warscore surrender — `War.update` postfix block before stalemate: `max(deadA,deadD) >= WarscoreMinDeaths` && ratio >= `WarscoreRatio` → `war.lostWar(loser)` (main side; once-per-war via `_warscoreDone` — `lostWar`→`update()` reenters postfix)
16. Focus fire — army units roll `FocusFireChance`/tick to `setAttackTarget` captain's live target (internal `attack_target`/`has_attack_target` → reflected)
17. Home-field — `Actor.getHit` prefix (7 params): defender `inOwnCityBorders()` + attacker != null → `pDamage *= HomeFieldMult`
18. Desertion — old non-total war (`>= DesertionWarYears`) + captain NOT inOwnCityBorders → random unit `stopBeingWarrior()`
19. Pillage — captain in enemy-zone city → per-unit roll `PillageChance` steals `min(stockpile, 1..PillageAmount)` gold → `ActorData.loot`; units deposit at own city via `addResourcesToRandomStockpile` (`Actor.data` internal → reflected)
20. Champion duels — scan `World.world.armies` (IEnumerable) for enemy captains within `DuelRadius`; pair dedup `_duelPairs` key `(min<<32)|(max&0xffffffff)` w/ 60s expiry; pass `DuelChance` → mutual `setAttackTarget`
21. Morale shock — `__state` old captain rekt && `!isGroupInCityAndHaveLeader()` → `WarFx.Shaken[army.id] = Time.time + ShockSeconds`; `run_away` delegate multiplies rout threshold `× ShockFleeMult` (cap 0.95)
22. Naval invasions — `check_warrior_transport` retune (`TaxiManager.newRequest` auto-merges same-kingdom/same-island) + `BehCityActorWarriorTaxiCheck.execute` postfix: on `BehResult.Stop`, follow embarked captain cross-island
23. Berserker rage — `Actor.increaseKills` postfix: streak within `RageWindowSec` >= `RageKills` → `addStatusEffect("rage", RageDurationSec)` (reflected; `rage` = multiplier_damage 1.0, affects_mind, default 300s)

## Access gotchas
- `internal` (use `AccessTools` reflection): `City.hasBuildingToBuild`, `hasStorageBuilding`, `hasBuildingType`, `City.kingdom`, `City.city_center`, `City._timer_warrior`, `City.tryToMakeWarrior`, `Actor.isProfession`, `Actor.addExperience`, `Actor.data`, `Actor.attack_target`, `Actor.has_attack_target`, `Actor.getHit` (internal override, 7 params), `BaseSimObject.stats`, `BaseSimObject.addStatusEffect(string,float,bool)` — note the StatusAsset overload is separate
- `private` but patchable by name: `Actor.increaseKills`, `Army.findCaptain`, `CityBehCheckAttackZone.findTargetCity`
- `hasCity()` is internal on Actor — use `a.city != null`
- `Kingdom` warrior check is `hasEnemies()`/`wars.hasWars(k)` — no `hasWars()` on Kingdom
- `isKingdomCiv()` is public on `BaseSimObject`; `Army.findCaptain`, `CityBehCheckAttackZone.findTargetCity` are private (patchable by name)
- public & handy: `NanoObject.id`, `MetaObject.units`, `Actor.army`, `Actor.loot` (get-only — set via reflected `data`→`ActorData.loot`), `BaseSimObject.kingdom`/`current_tile`/`current_zone`, `Army.getKingdom()`, `Kingdom.getWars()`, `War.getDeadAttackers/Defenders`/`lostWar`, `TaxiManager.newRequest`, `World.world.armies` (IEnumerable)
- free per-army-per-tick hook: postfix `Army.checkCaptainExistence` (called by `ArmyManager.update`); capture old captain in prefix `__state` to detect captain death before vanilla replaces it
- Docs baseline: WorldBox 0.51.2 build 719 / NML 1.2.0.1 (lous12.github.io/worldbox-modding-docs, worldboxopenmods.gitbook.io/mod-tutorial-en)

## Verify
- Launch game → NML button in main tab; mod logs `[CivMind]` lines to `%LocalLow%\mkarpenko\WorldBox\Player.log`
- Mod config UI: NML window → mod → "Mod Configuration" (uses `default_config.json`)
