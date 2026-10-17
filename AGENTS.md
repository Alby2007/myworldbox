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
- Wars are **plots**: `new_war` PlotAsset (king lvl≥3, warfare≥6, renown≥50, 20g) → `DiplomacyHelpers.isWarNeeded` gates (war timeout, >10 warriors, <4 cities needs 60% pop, **already blocks declarations while at war** — no two-front wars) → `getWarTarget` (nearest valid kingdom: own warriors ≥ defender, reachable, peace elapsed, bad opinion) → `DiplomacyManager.startWar` → `WarTypeAsset` (normal/spite/inspire/rebellion/whisper_of_war; `total_war`, `alliance_join`, `can_end_with_plot` flags)
- Succession: `SuccessionTool.getKingFromLeaders`/`getKingFromRoyalClan` pick heir via `ActorTool.attributeDice` (random); `CityBehCheckLeader` picks leaders the same dice way → `City.setLeader(Actor,bool)`
- Alliances: `DiplomacyHelpers.getAllianceTarget` filters (supreme/king/no-plot/good opinion/renown ≥ `alliance_create.min_renown_kingdom`/proximity-or-2-city-isolation) then `LoopRandom` picks randomly
- Rebellion: `DiplomacyHelpersRebellion.startRebellion` requires a non-null `Plot` (dereferences `pPlot.units`) — to trigger manually, replicate: `City.makeOwnKingdom` (internal) + `DiplomacyManager.startWar` (internal, `WarTypeLibrary.rebellion`) + `checkMoreAlignedCities` (public) + `WorldLog.logCityRevolt`; `CityBehCheckLoyalty.execute` only recalcs loyalty (~2s cadence) = clean periodic hook
- Conquest detection: `CityData.timestamp_kingdom` is set on founding AND on kingdom change — a city is "occupied" only when `timestamp_kingdom > created_time` (both on `data`)
- Succession chaos: `KingdomBehCheckKing` — no clan heir + leaders empty → `checkKingdomChaos` splits ALL non-capital cities (`pFellApart:true`, no war); `shattered_crown` culture → splits among clan; `unbroken_chain` culture OR rebellions-law-off → falls back to `getKingFromLeaders`
- City food = `Σ storage.resources` slots where `ResourceAsset.food == true` — `Building.resources.getRandomFoodAsset()` picks one, `City.takeResource(id, n)` drains it
- `alliance_destroy` vanilla gates: alliance ≥10y + member ≥10y + initiator supreme-or-2nd-best + alliance power > 2× all others + contains supreme — very strict
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

## Mod features (v0.4 — politics, battlefield polish, home front)

24. Merit succession — `SuccessionTool.getKingFromLeaders`/`getKingFromRoyalClan` postfixes rescore `__result`: max `diplomacy + stewardship + warfare*0.5 + level` (PatchSuccessionClan/Leaders; shared `Score()` reflects `BaseSimObject.stats`)
25. War reparations — `WarManager.endWar` postfix: `WarWinner.Attackers/Defenders` → loser cities pay `ReparationsPct`% of `gold` (`takeResource`) → `winner.capital.addResourcesToRandomStockpile`; skipped on `Peace`/no capital (PatchEndWar)
26. War heroes — `PatchIncreaseKills` also feeds `WarFx.HeroKills` (only while `wars.hasWars(killer.kingdom)`); at `endWar` each main side's top killer with ≥ `HeroMinKills` → `addTrait("veteran")`; per-kingdom entries removed after award
27. Balance-of-power alliances — `DiplomacyHelpers.getAllianceTarget` prefix-supplant: same vanilla filters (supreme/king/plot/opinion/renown/proximity-or-isolated) then argmin `Kingdom.distanceBetweenKingdom` → regional blocs; `ListPool` disposed in `finally` (PatchAllianceTarget)
28. Commander aura — `run_away` delegate: living captain within `AuraRadius` → rout threshold `× AuraFleeMult` (applied BEFORE shock so the 0.95 cap stays safe) (Tuning.cs)
29. Rally flee — `BehFindRandomFarTile` prefix: army units route to captain's `current_tile` else random own-city zone center; reflected `Actor.beh_tile_target`/`City.zones`; non-army/missing-target → vanilla (PatchFlee)
30. Militia reserve — `City.tryToMakeWarrior` prefix: peacetime blocks conversion once `warriors_current >= warrior_slots × ReservePeacetimeRatio`; `isInDanger` bypasses cap (PatchTryWarrior)
31. War refugees — `check_join_city`/`check_join_empty_nearby_city` weight wrap: homeless (`city == null`) + kingdom at war → `× RefugeeBoost`; composes with vanilla custom weights (Tuning.RetuneRefugee)
32. Merit leaders — `City.setLeader` prefix `ref pActor` swap: scans `city.units` for `is_profession_citizen` non-king/leader, `stewardship + level*0.5` must beat vanilla pick by `LeaderMargin` (PatchSetLeader)
33. Occupied-city uprisings — `CityBehCheckLoyalty` postfix (every ~2s/city): non-capital + loyalty < `UprisingLoyalty` + `timestamp_kingdom` within `UprisingRecentYears` + `world_law_rebellions` + `randomChance(UprisingChance)` → `makeOwnKingdom(rebel)` + `startWar(oldK, newK, rebellion)` (both internal → reflected) + `logCityRevolt` + `checkMoreAlignedCities`; `_uprisingDone` once-per-city (PatchCheckLoyalty)
34. `WarFx.PeaceTime` records `endWar` timestamp per kingdom (bookkeeping for future reconstruction surge)

## Mod features (v0.5 — realpolitik, economy, culture)

35. King personality — `DiplomacyHelpers.isWarNeeded` postfix `ref __result`: pacifist kings (diplomacy ≥ PeaceKingDiplomacy, > warfare) veto via `PeaceVetoChance`; warlords (warfare ≥ WarlordWarfare) bypass warrior-min + 60%-pop gates (re-checks structural gates only) via `WarlordChance`
36. Anti-hegemon — `PatchAllianceTarget`: find supreme kingdom (`World.world.kingdoms` foreach), candidates `areKingdomsClose(candidate, supreme)` get `dist × HegemonBonusMult` (containment blocs); `Tuning.WrapPlots` relaxes `alliance_destroy.check_is_possible` — drops supreme/2nd-best-initiator + 2×-power-dominance gates, keeps "alliance contains supreme" + 10y age; hooked via `PatchPlotsInit` on `PlotsLibrary.init` (asset via `AssetManager.plots_library.get("alliance_destroy")` — NOT a static field unlike `alliance_create`)
37. Pretender wars — `Kingdom.setKing` postfix (`!pFromLoad`): best non-capital city leader / royal-clan member scoring > `king + PretenderMargin` secedes via `Rebel.SparkRevolt` (`PretenderChance`)
38. Collapse pressure — same hook: weak king (`Score < CollapseKingScore`) + >2 cities → lowest-loyalty non-capital (<40) province secedes via `Rebel.FellApart` (`makeOwnKingdom(leader, pRebellion:false, pFellApart:true)` — no war)
39. War tribute — `CityBehSupplyKingdomCities` postfix: at war + non-capital + `TributeChance` → `TributePct`% of gold above `TributeMinGold` to capital; `timer_supply ×= SupplyWarMult` during war (faster aid runs)
40. Siege starvation — `PatchCheckLoyalty`: `isInDanger` + `SiegeDrainChance` → `getRandomStockpile().resources.getRandomFoodAsset()` → `takeResource` (`Building.resources` is PUBLIC `=> data.resources`)
41. Border pressure — `CityBehBorderSteal` postfix: wartime + `BorderPressureChance` → one extra zone steal, replicates `tryStealZone` via reflected `addZone`/`removeZone`
42. Reconstruction surge — `PatchCitizenTasks`: kingdom's `WarFx.PeaceTime` within `ReconYears` → +`ReconBuilders` builder slots
43. Holy wars — `PatchGetWarTarget` loop: `religion != null && differs` → `score × HolyWarMult`
44. Assimilation — `PatchCheckLoyalty`: occupied + `AssimilationChance` → `setCulture`/`setReligion` (internal → `Rebel.SetCulture/SetReligion` reflect)
45. War exhaustion — `PatchCheckLoyalty`: at war ≥ `WarExhaustYears` (via `k.data.timestamp_last_war`) → `UprisingChance × WarExhaustMult`
46. Foreign-culture unrest — `PatchCheckLoyalty`: `city.culture != kingdom.culture` → loyalty gate `+CultureUprisingBonus` (revolt easier)
47. `Rebel` shared helper (Patches.cs): reflected `makeOwnKingdom`/`startWar`/`City.kingdom`/`setCulture`/`setReligion`, `Revolted` once-per-city set, `SparkRevolt` (rebellion war + aligned cities + `logCityRevolt`), `FellApart` (peaceful secession), `Of(city)` kingdom lookup

## Access gotchas
- `internal` (use `AccessTools` reflection): `City.hasBuildingToBuild`, `hasStorageBuilding`, `hasBuildingType`, `City.kingdom`, `City.city_center`, `City.zones`, `City.storages`, `City._timer_warrior`, `City.tryToMakeWarrior`, `City.makeOwnKingdom`, `City.addZone`/`removeZone`, `City.setCulture`/`setReligion`, `DiplomacyManager.startWar`, `Actor.beh_tile_target`, `Actor.isProfession`, `Actor.addExperience`, `Actor.data`, `Actor.attack_target`, `Actor.has_attack_target`, `Actor.getHit` (internal override, 7 params), `BaseSimObject.stats`, `BaseSimObject.addStatusEffect(string,float,bool)` — note the StatusAsset overload is separate
- `private` but patchable by name: `Actor.increaseKills`, `Army.findCaptain`, `CityBehCheckAttackZone.findTargetCity`
- `hasCity()` is internal on Actor — use `a.city != null`
- `Kingdom` warrior check is `hasEnemies()`/`wars.hasWars(k)` — no `hasWars()` on Kingdom
- `isKingdomCiv()` is public on `BaseSimObject`; `Army.findCaptain`, `CityBehCheckAttackZone.findTargetCity` are private (patchable by name)
- public & handy: `NanoObject.id`, `MetaObject.units`, `Actor.army`, `Actor.loot` (get-only — set via reflected `data`→`ActorData.loot`), `BaseSimObject.kingdom`/`current_tile`/`current_zone`, `Army.getKingdom()`, `Kingdom.getWars()`/`getCities()`/`getRenown()`/`hasCapital()`/`capital`, `War.getDeadAttackers/Defenders`/`lostWar`/`getMainAttacker`/`getMainDefender`, `WarWinner` enum, `TaxiManager.newRequest`, `World.world.armies`/`units`/`clans`/`wars` (managers; `.get(long id)`), `City.leader`/`setLeader`/`removeLeader`/`getLoyalty`/`units`/`status`, `CityData.timestamp_kingdom`, `SuccessionTool.getKingFromLeaders/getKingFromRoyalClan` (static), `DiplomacyHelpers.getAllianceTarget/areKingdomsClose`, `Kingdom.distanceBetweenKingdom`, `PlotsLibrary.alliance_create.min_renown_kingdom`, `WorldLawLibrary.world_law_rebellions.isEnabled()`, `WarTypeLibrary.rebellion`, `DiplomacyHelpersRebellion.checkMoreAlignedCities`, `WorldLog.logCityRevolt`, `Date.getYearsSince`, `TileZone.centerTile`/`city`/`neighbours`/`hasAnyBuildingsInSet`, `Toolbox.SquaredDistTile`, `World.world.kingdoms` (IEnumerable), `Alliance.kingdoms_hashset`/`getAge()`/`isForcedType()`, `Kingdom.king`/`hasKing`/`isSupreme`/`religion`/`culture`/`countTotalWarriors`, `Kingdom.setKing(Actor,bool pFromLoad)`, `City.getRandomStockpile`/`hasStockpiles`/`takeResource`/`getResourcesAmount`, `Building.resources` (public prop → `getRandomFoodAsset`), `CityData.timer_supply`/`timestamp_kingdom`/`created_time`, `PlotAsset.check_is_possible` (wrappable public delegate field; plots assets via `AssetManager.plots_library.get("id")`)
- free per-army-per-tick hook: postfix `Army.checkCaptainExistence` (called by `ArmyManager.update`); capture old captain in prefix `__state` to detect captain death before vanilla replaces it
- Docs baseline: WorldBox 0.51.2 build 719 / NML 1.2.0.1 (lous12.github.io/worldbox-modding-docs, worldboxopenmods.gitbook.io/mod-tutorial-en)

## Verify
- Launch game → NML button in main tab; mod logs `[CivMind]` lines to `%LocalLow%\mkarpenko\WorldBox\Player.log`
- Mod config UI: NML window → mod → "Mod Configuration" (uses `default_config.json`)
