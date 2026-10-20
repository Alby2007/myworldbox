# Config Reference

Every user-facing option (NML window → mod → **Mod Configuration**). Sources: `Cfg.cs`,
`default_config.json`, `Locales/en.json`. All numeric options are sliders; `Switch` = toggle.

## Jobs

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `JobSeekWeight` | Job seeking urge | slider | 2.0 | 0–4 | Multiplier on `find_city_job` score (Tuning `JobSeekWeight`) |
| `JobSeekCooldown` | Job seek cooldown (x vanilla) | slider | 0.4 | 0.05–1.0 | `find_city_job` cooldown multiplier |
| `JobDemandBoost` | Open slots attract workers | slider | 1.0 | 0–3 | Added score per open job slot (caps at 4 slots); 0 disables |
| `ExtraBuilders` | Extra builders while building | slider | 2 | 0–6 | Bonus `builder` slots while `hasBuildingToBuild` |
| `ExtraFoodJobs` | Extra food jobs when food low | slider | 3 | 0–8 | Bonus gatherer/farmer slots under `LowFoodThreshold` |
| `ExtraWoodcutters` | Extra woodcutters when wood low | slider | 2 | 0–5 | Bonus `woodcutter` slots under `LowWoodThreshold` |
| `LowFoodThreshold` | Food shortage threshold | slider | 60 | 0–200 | `city.getTotalFood()` level that triggers food-job boost |
| `LowWoodThreshold` | Wood shortage threshold | slider | 15 | 0–80 | `city.amount_wood` level that triggers woodcutter boost |

## Expansion

| Id | Label | Default | Range | Controls |
|---|---|---|---|---|
| `ExpansionWeight` | Expansion urge | 1.8 | 0–4 | Weight × on `claim_land`, `build_civ_city_here`, `check_join_city`, `check_join_empty_nearby_city`, `try_to_start_new_civilization`, `king_check_new_city_foundation` |
| `ExpansionCooldown` | Expansion cooldown (x vanilla) | 0.6 | 0.1–1.0 | Cooldown × on the same set |

## Social

| Id | Label | Default | Range | Controls |
|---|---|---|---|---|
| `ReproductionWeight` | Reproduction urge | 1.3 | 0–4 | Weight × on `find_lover`, `check_lover_city`, `sexual_reproduction_try` (housed units) |
| `SocializeWeight` | Socializing urge | 1.6 | 0–4 | Weight × on `socialize_initial_check`, `check_swearing` |
| `CivReturnWeight` | Stranded civilians seek home | 1.7 | 0–5 | Weight × on `try_to_return_to_home_city` |
| `CivReturnCooldown` | Home-return cooldown | 0.7 | 0.1–2 | Cooldown × on `try_to_return_to_home_city` |
| `BoatTaxiWeight` | Boat rescue priority | 2.3 | 0–5 | Weight × on `boat_transport_check` (rescue vs trade) |
| `VictoryFireworks` | Victory celebrations | switch | on | `festive_spirit` on winner's citizens at `endWar` |
| `FestiveChance` | Citizen festive chance | 0.35 | 0–1 | Per-unit roll for `festive_spirit` |
| `FestiveMaxPerCity` | Max festive citizens | 8 | 0–50 | Per-city cap on celebrations |
| `FestiveSeconds` | Celebration duration | 120 | 10–600 | Status duration in seconds |
| `ReadWeight` | Reading urge | 2.5 | 0–6 | Weight × on `try_to_read` |
| `ReadCooldown` | Reading cooldown | 0.5 | 0.1–2 | Cooldown × on `try_to_read` |
| `ReadMinInt` | Min intelligence to read | 3 | 0–10 | Relaxed gate vs vanilla 5 |
| `Firefighting` | Intensity-scaled firefighting | switch | on | `put_out_fire` weight += fires × `FireWeightPer` |
| `FireWeightPer` | Weight per active fire | 1.0 | 0–5 | Weight bonus per burning zone |
| `FireMaxBonus` | Max fire bonus | 10 | 0–50 | Cap on the fire-intensity bonus |

## Experimental

| Id | Label | Type | Default | Controls |
|---|---|---|---|---|
| `AptitudeHiring` | Aptitude-based hiring | switch | on | Enables aptitude scoring in `PatchSetCitizenJob` |
| `EmergencyDraft` | Emergency militia draft | switch | on | Draft bypass + stay-mobilized + militia slots (features 3–4) |
| `DraftAttackerSlots` | Extra militia slots under attack | slider | 4 (0–8) | `attacker` slots added while `isInDanger` |
| `HousingCrisis` | Housing-gated reproduction | switch | on | Homeless units use `NoHouseReproMult` |
| `NoHouseReproMult` | Homeless reproduction rate | slider | 0.15 (0–1) | Reproduction weight multiplier without `hasHouse()` |
| `JobXpAmount` | XP per completed job | slider | 5 (0–20) | `addExperience` on `endJob`; 0 disables |

## War

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `MoraleRetreat` | Wounded units rout (morale) | switch | on | — | Replaces `run_away` launch check |
| `RetreatHealth` | Rout below health ratio | slider | 0.35 | 0–0.9 | Base rout threshold |
| `TraitMorale` | Traits & warfare affect morale | switch | on | — | pacifist ×1.6 / bloodlust,savage,veteran ×0.5 / +warfare×0.003 |
| `SmartWarTarget` | Smarter war target selection | switch | on | — | Scored `getWarTarget` |
| `SmartCityTarget` | Smarter siege target selection | switch | on | — | Scored `findTargetCity` |
| `MeritCaptains` | Best warrior leads the army | switch | on | — | `warfare*2+level` captain pick |
| `MarchFill` | Army fill required to march | slider | 0.7 | 0.3–1.0 | `isOkToSendArmy` threshold |
| `WarTimeoutYears` | Years between wars (per kingdom) | slider | 5 | 1–20 | `diplomacy_years_war_timeout` |
| `WarMinWarriors` | Min warriors to declare war | slider | 10 | 0–50 | `diplomacy_years_war_min_warriors` |
| `WarPeaceYears` | Years before a rematch | slider | 5 | 0–20 | `minimum_years_between_wars` |
| `WarStalemateYears` | Auto-peace after stalemate (0=off) | slider | 12 | 0–30 | `PatchWarUpdate` age threshold |
| `TrainWeight` | Warrior training urge | slider | 1.4 | 0–4 | `warrior_train_with_dummy` weight × |
| `TrainCooldown` | Training cooldown (x vanilla) | slider | 0.6 | 0.05–1.0 | its cooldown × |

## WarTactics

v0.3 features — attrition systems, duels/morale shock, naval & rage. See
[war-ai.md](war-ai.md) § War Tactics.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `WarscoreSurrender` | Warscore surrender | switch | on | — | Lopsided kill counts force `lostWar` |
| `WarscoreMinDeaths` | Min deaths for surrender | slider | 20 | 5–100 | Death threshold before ratio check |
| `WarscoreRatio` | Kill ratio to force surrender | slider | 3.0 | 1.5–10 | max/min dead ratio needed |
| `FocusFire` | Armies focus captain's target | switch | on | — | Units adopt captain's attack target |
| `FocusFireChance` | Focus fire join chance (per tick) | slider | 0.15 | 0.02–0.5 | Per-unit per-tick convergence roll |
| `HomeField` | Home-field advantage | switch | on | — | Damage reduction in own borders |
| `HomeFieldMult` | Damage taken in own borders | slider | 0.8 | 0.3–1.0 | `pDamage` multiplier for defenders |
| `DesertionWarYears` | Desertion war age (0=off) | slider | 8 | 0–30 | War age enabling desertion |
| `DesertionChance` | Desertion chance (per tick) | slider | 0.005 | 0–0.1 | Gate roll per army tick |
| `Pillage` | Armies pillage enemy cities | switch | on | — | Steal gold into `ActorData.loot` |
| `PillageChance` | Pillage chance (per unit) | slider | 0.02 | 0–0.2 | Per-unit roll while in enemy city |
| `PillageAmount` | Max gold per pillage | slider | 5 | 1–30 | Upper bound per successful roll |
| `Duels` | Champion duels | switch | on | — | Captain-vs-captain encounters |
| `DuelChance` | Duel chance per encounter | slider | 0.5 | 0.05–1.0 | One roll per army pair per 60s |
| `DuelRadius` | Duel trigger radius (tiles) | slider | 8 | 2–20 | Captain proximity required |
| `MoraleShock` | Captain death routs army | switch | on | — | `WarFx.Shaken` → boosted `run_away` |
| `ShockSeconds` | Morale shock duration | slider | 30 | 5–120 | How long the army stays shocked |
| `ShockFleeMult` | Shock flee threshold mult | slider | 2.5 | 1–5 | Rout threshold × while shocked (cap 0.95) |
| `NavalInvasions` | Naval invasions | switch | on | — | Transport retune + follow-captain boarding |
| `TransportWeight` | Transport urge | slider | 1.75 | 0–4 | `check_warrior_transport` weight × |
| `TransportCooldown` | Transport cooldown (x vanilla) | slider | 0.5 | 0.05–1.0 | its cooldown × |
| `BerserkerRage` | Kill-streak rage | switch | on | — | Streak → `rage` status |
| `RageKills` | Kills to trigger rage | slider | 3 | 2–10 | Streak length required |
| `RageWindowSec` | Kill streak window (sec) | slider | 30 | 5–120 | Time between kills to keep streak |
| `RageDurationSec` | Rage duration (sec) | slider | 20 | 5–120 | Applied status duration |
| `MoraleAura` | Commander aura steadies troops | switch | on | — | Living captain nearby lowers rout threshold |
| `AuraRadius` | Aura radius (tiles) | slider | 12 | 4–30 | Distance to captain for the aura |
| `AuraFleeMult` | Aura flee threshold mult | slider | 0.6 | 0.1–1.0 | Rout threshold × with captain near |
| `RallyFlee` | Routed warriors rally home | switch | on | — | Flee toward captain/own city, not random tiles |
| `MilitiaReserve` | Peacetime militia reserve | switch | on | — | Cap peacetime warrior conversions |
| `ReservePeacetimeRatio` | Peacetime army fill ratio | slider | 0.6 | 0.2–0.9 | Max `warriors_current/slots` while safe |
| `BorderPressure` | Armies seize border zones | switch | on | — | Extra `tryStealZone` during war |
| `BorderPressureChance` | Border steal chance | slider | 0.35 | 0–1 | Per-check roll for the extra steal |

## Politics

v0.4 features — succession, reparations, heroes, alliances. See
[war-ai.md](war-ai.md) § Politics.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `MeritSuccession` | Best-stat heir (succession) | switch | on | — | Deterministic heir pick over dice |
| `Reparations` | War reparations | switch | on | — | War loser pays gold to winner |
| `ReparationsPct` | Loser gold seized (%) | slider | 25 | 0–50 | % of each loser city's gold |
| `WarHeroes` | War heroes get veteran trait | switch | on | — | Top wartime killer per side |
| `HeroMinKills` | Min kills for war hero | slider | 3 | 1–20 | Wartime kills required |
| `BalanceAlliances` | Regional alliance blocs | switch | on | — | Nearest-partner alliance picks |
| `KingPersonality` | King personality shapes policy | switch | on | — | `isWarNeeded` veto/bypass by stats |
| `PeaceKingDiplomacy` | Pacifist king diplomacy | slider | 10 | 4–20 | Diplomacy ≥ this + `diplomacy > warfare` = pacifist |
| `PeaceVetoChance` | Pacifist war veto chance | slider | 0.5 | 0–1 | Per-check probability to suppress a war |
| `WarlordWarfare` | Warlord king warfare | slider | 12 | 6–25 | Warfare ≥ this = warlord |
| `WarlordChance` | Warlord war urge | slider | 0.5 | 0–1 | Chance to ignore size/pop war gates |
| `WarlordMinWarriors` | Warlord min warriors | slider | 4 | 1–10 | Even warlords need this many |
| `AntiHegemon` | Anti-hegemon coalitions | switch | on | — | Containment alliances + relaxed `alliance_destroy` |
| `HegemonBonusMult` | Containment ally bonus | slider | 0.7 | 0.3–1.0 | Score × for supreme-adjacent candidates |
| `PretenderWars` | Pretender succession wars | switch | on | — | Strong rivals seize cities on coronation |
| `PretenderMargin` | Pretender score margin | slider | 8 | 0–30 | Score gap needed to revolt |
| `PretenderChance` | Pretender war chance | slider | 0.35 | 0–1 | Per-coronation roll |
| `CollapsePressure` | Weak kings lose provinces | switch | on | — | Peaceful secession of unhappy cities |
| `CollapseChance` | Province drift chance | slider | 0.3 | 0–1 | Per-coronation roll under weak kings |
| `CollapseKingScore` | Weak king threshold | slider | 8 | 0–30 | King merit score below this triggers drift |

## HomeFront

v0.4 features — refugees, leaders, uprisings. See [war-ai.md](war-ai.md) § Home Front.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `RefugeeBoost` | War refugee join urge | slider | 2.5 | 0–6 | Weight × on join-city for homeless at-war civilians; 0 disables |
| `MeritLeaders` | Best-stewardship city leaders | switch | on | — | Stewardship-scored leader picks |
| `LeaderMargin` | Leader swap margin | slider | 5 | 0–20 | Score gap needed to override vanilla pick |
| `Uprisings` | Occupied cities revolt | switch | on | — | Low-loyalty recently-captured cities rebel |
| `UprisingLoyalty` | Uprising loyalty threshold | slider | 30 | 10–60 | Loyalty below this is revolt-eligible |
| `UprisingRecentYears` | Recent conquest window | slider | 10 | 2–30 | Years since capture that count as "occupied" |
| `UprisingChance` | Uprising chance (per check) | slider | 0.02 | 0–0.2 | Per loyalty-check roll (~1%/s at 0.02) |
| `WarTribute` | War tribute to capital | switch | on | — | Cities ship gold to capital during war |
| `TributePct` | Tribute share of surplus (%) | slider | 30 | 5–80 | % of gold above the floor that ships |
| `TributeMinGold` | Gold kept by city (floor) | slider | 60 | 10–300 | Cities never tribute below this |
| `TributeChance` | Tribute chance (per check) | slider | 0.05 | 0–0.5 | Trickle rate (~5% per supply check) |
| `SupplyWarMult` | Supply cooldown during war | slider | 0.5 | 0.2–1.0 | `timer_supply` × — faster aid in wartime |
| `SiegeStarvation` | Besieged cities lose food | switch | on | — | `isInDanger` cities leak food stores |
| `SiegeFoodDrain` | Food drained per tick | slider | 2 | 1–10 | Units of a random food resource |
| `SiegeDrainChance` | Siege drain chance | slider | 0.3 | 0–1 | Per loyalty-check roll while in danger |
| `ReconBuilders` | Post-war extra builders | slider | 2 | 0–8 | Extra `builder` slots during reconstruction |
| `ReconYears` | Reconstruction window (years) | slider | 8 | 1–30 | Years after war-end the surge lasts |
| `WarExhaustYears` | War exhaustion after (years) | slider | 10 | 1–40 | War age that starts boosting uprisings |
| `WarExhaustMult` | Exhaustion uprising mult | slider | 2 | 1–5 | `UprisingChance` × during long wars |

## Culture

v0.5 features — religion-driven wars and occupied-city assimilation.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `HolyWars` | Holy war targeting | switch | on | — | Different-religion targets score better |
| `HolyWarMult` | Foreign-faith target appeal | slider | 0.7 | 0.3–1.5 | Score × for different-religion targets |
| `Assimilation` | Occupied cities assimilate | switch | on | — | Conquered cities adopt culture/religion |
| `AssimilationChance` | Assimilation chance (per check) | slider | 0.05 | 0–0.3 | Per loyalty-check roll while occupied |
| `CultureUprisingBonus` | Foreign culture revolt ease | slider | 10 | 0–30 | Loyalty-gate bonus for foreign-culture cities |

## Economy

v0.6–v0.9 features — loot/tax economy, item production, trade boats, plunder incentives, wages, unrest/corruption, triage logistics, golden ages, mercenaries, embargoes, profiteering.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `WarChest` | Treasury funds war plots | switch | on | — | Capital gold tops up kings' `money` when war-ready |
| `WarChestDrawMax` | War chest draw cap (gold) | slider | 40 | 10–200 | King `money` topped up to this max |
| `TaxWeight` | Tax collection urge | slider | 1.4 | 0.5–3 | `give_tax` weight × |
| `TaxCooldown` | Tax cooldown (x vanilla) | slider | 0.6 | 0.2–1.0 | `give_tax` cooldown × (90s base) |
| `ItemCraftWeight` | Crafting urge | slider | 2.0 | 0.5–4 | `make_items` weight × (0.4 base) |
| `ItemCraftCooldown` | Crafting cooldown (x vanilla) | slider | 0.6 | 0.2–1.0 | `make_items` cooldown × (90s base) |
| `ItemArmWeight` | Arming urge | slider | 1.5 | 0.5–4 | `try_to_take_city_item` weight × |
| `ItemArmCooldown` | Arming cooldown (x vanilla) | slider | 0.5 | 0.1–1.0 | its cooldown × (180s base) |
| `ArmDangerMult` | Danger arming mult | slider | 3.0 | 1–6 | Extra weight × while `isInDanger` |
| `TradeWeight` | Boat trade urge | slider | 1.5 | 0.5–3 | `boat_trading`/`boat_fishing` weight × |
| `TradeWarMult` | Trade during war (x) | slider | 0.3 | 0–1 | `boat_trading` weight × while at war |
| `RichTargets` | Wealthy war targets attract wars | switch | on | — | War-target score ÷ `1 + capitalGold/scale` |
| `RichTargetGold` | Rich target scale (gold) | slider | 100 | 10–500 | Gold divisor — lower = plunder matters more |
| `LootTax` | Crown skims pillage loot | switch | on | — | War dividend skim on loot deposits |
| `LootTaxPct` | War dividend (% of loot) | slider | 20 | 0–80 | % of deposited pillage loot to king `money` |
| `Wages` | Soldier wages | switch | on | — | Periodic city gold drain per warrior |
| `WageChance` | Wage charge chance (per check) | slider | 0.1 | 0–1 | Roll per `CityBehCheckLoyalty` tick |
| `WageRate` | Gold per warrior (wage) | slider | 1.0 | 0–10 | Cost = `warriors_current × WageRate` |
| `MutinyDesertionMult` | Unpaid army desertion (x) | slider | 2.5 | 1–10 | Desertion × while kingdom can't pay |
| `TradeIncome` | Trade runs earn city gold | switch | on | — | `BehBoatMakeTrade` postfix bonus |
| `TradeIncomeBase` | Trade run base gold | slider | 5 | 0–50 | Flat gold per completed run |
| `TradeIncomePerTile` | Trade gold per tile | slider | 0.15 | 0–2 | Distance scaling to foreign dock |
| `EquipmentMarket` | Citizens pay for equipment | switch | on | — | `City.giveItem` charges `money` |
| `ItemPricePct` | Item price (% of value) | slider | 100 | 0–300 | % of equipped-value delta charged |
| `TaxUnrest` | Heavy taxes anger citizens | switch | on | — | Extra happiness dip past threshold |
| `TaxUnrestRate` | Tax unrest threshold (rate) | slider | 0.5 | 0–1 | `getTaxRateLocal()` above this angers citizens |
| `TaxUnrestSeverity` | Tax unhappiness severity | slider | 8 | 0–50 | Happiness dip scale over threshold |
| `Corruption` | Leaders skim royal tribute | switch | on | — | Loyalty-scaled tribute skim to leader `money` |
| `CorruptionPct` | Tribute skim (% of forwarded) | slider | 25 | 0–80 | % of king's tribute pocketed |
| `CorruptionChance` | Corruption chance (per tribute) | slider | 0.5 | 0–1 | Roll per tribute payment |
| `FamineRelief` | Supply runs triage famine | switch | on | — | Kingdom supply → hungriest city first |
| `LaborMigration` | Migrants seek job vacancies | switch | on | — | `getPotentialCityNearby` prefers free job slots |
| `GoldenAge` | Golden age bonus | switch | on | — | Peacetime crafting/build bonus |
| `GoldenAgeYears` | Peace years for golden age | slider | 15 | 1–60 | `Rebel.PeaceYears` threshold |
| `GoldenAgeMult` | Golden age crafting (x) | slider | 1.5 | 1–4 | `make_items` weight × while at peace |
| `GoldenAgeBuilders` | Golden age extra builders | slider | 1 | 0–5 | Extra builder slots during golden age |
| `Mercenaries` | Cities hire nomad mercenaries | switch | on | — | Join-city migrants can be bought into armies |
| `MercenaryChance` | Mercenary enlist chance | slider | 0.35 | 0–1 | Roll per migrant joining |
| `MercenaryCost` | Mercenary signing bonus (gold) | slider | 15 | 0–100 | City gold → migrant `money` on enlist |
| `Embargo` | Alliances embargo enemy docks | switch | on | — | Trade boats skip docks at war with any ally |
| `WarProfiteer` | Neutrals trade more during wars | switch | on | — | Peaceful boats × while wars rage |
| `WarProfiteerMult` | War profiteer trade (x) | slider | 1.75 | 1–4 | `boat_trading` weight × for neutrals |

## Combat

v1.0 combat micro — all rolls inside `Actor.getHit` except finish-wounded (target selection) and veteran specials (action gates).

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `CombatCrits` | Critical hits & dodges | switch | on | — | Master switch for crit/dodge rolls |
| `CritChance` | Base crit chance | slider | 0.08 | 0–0.5 | Attacker crit probability |
| `CritMult` | Crit damage (x) | slider | 2 | 1–5 | Damage × on crit |
| `DodgeChance` | Base dodge chance | slider | 0.06 | 0–0.5 | Defender zero-damage probability |
| `CritDodgeStatScale` | Accuracy bonus to crit/dodge | slider | 0.004 | 0–0.02 | `accuracy` stat × this, added to both rolls |
| `ArmorPen` | Veterans pierce armor | switch | on | — | Master switch for armor penetration |
| `PenWarfare` | Warfare needed to pierce | slider | 8 | 0–25 | Min `warfare` to roll penetration |
| `PenChance` | Armor pierce chance | slider | 0.35 | 0–1 | Sets `pCheckDamageReduction=false` — bypasses ALL reduction |
| `Stagger` | Heavy hits stagger | switch | on | — | Master switch for stagger |
| `StaggerDmgPct` | Stagger damage (% max hp) | slider | 0.25 | 0.05–0.8 | Hit must deal ≥ this fraction of max health |
| `StaggerSeconds` | Stagger duration (s) | slider | 1.5 | 0.5–10 | `stunned` status length |
| `StaggerChance` | Stagger chance | slider | 0.5 | 0–1 | Roll when damage threshold met |
| `FinishWounded` | Focus wounded targets | switch | on | — | Target-swap postfix on `findEnemyObjectTarget` |
| `FinishHealth` | Wounded health threshold | slider | 0.35 | 0.05–0.8 | Enemy health ratio counted as "wounded" |
| `FinishRadius` | Wounded search radius | slider | 12 | 2–40 | Tile radius for the swap search |
| `VetSpecials` | Veteran combat moves | switch | on | — | Wrap `CombatActionAsset.can_do_action` |
| `VetSpecKills` | Kills to unlock specials | slider | 10 | 3–50 | `data.kills` required |
| `VetSpecBypass` | Special bypass chance | slider | 0.5 | 0–1 | Chance a failed `can_do_action` still passes |

## Armies

v1.0 army behavior — all army ticks run inside `Army.checkCaptainExistence`.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `WoundedRotation` | Rotate wounded out | switch | on | — | Idle wounded warriors demobilize |
| `RotateHealth` | Wounded rotation threshold | slider | 0.3 | 0.05–0.8 | Health ratio that triggers rotation |
| `RotateChance` | Rotation chance per tick | slider | 0.3 | 0–1 | Roll per army tick |
| `Formations` | Formations & kiting | switch | on | — | Ranged units step away from near targets |
| `KiteRange` | Kite trigger distance | slider | 7 | 2–20 | Squared-tile trigger for ranged retreat |
| `KiteChance` | Kite chance per tick | slider | 0.4 | 0–1 | Roll per engaged ranged unit |
| `SiegeRelief` | Relief marches | switch | on | — | Idle armies march to threatened cities |
| `ReliefChance` | Relief march chance | slider | 0.5 | 0–1 | Roll per army tick when a city needs relief |
| `ArmyVeterancy` | Army veterancy | switch | on | — | Veteran-army promotion |
| `VetArmyKills` | Avg kills for veteran army | slider | 5 | 1–30 | Mean kills across living units |
| `VetShockResist` | Veteran shock resist | slider | 0.5 | 0–1 | Softens `ShockFleeMult` toward 1 |
| `VetArmyAura` | Veteran aura boost (x) | slider | 1.5 | 1–4 | Divides flee threshold under the captain aura |
| `VetRetention` | Retain veterans | switch | on | — | Veterans survive demobilization checks |
| `VetKeepKills` | Kills for veteran status | slider | 8 | 1–30 | `data.kills` required |
| `VetKeepChance` | Veteran retention chance | slider | 0.7 | 0–1 | Roll when vanilla tries to demobilize |
| `CombinedArms` | Combined-arms arming | switch | on | — | Wartime melee-heavy armies draw ranged gear |
| `RangeShare` | Target ranged share | slider | 0.4 | 0.1–0.9 | Preferred ranged fraction of the army |
| `SiegeMomentum` | Siege momentum | switch | on | — | Extra attackers accelerate capture |
| `MomentumPerUnit` | Capture boost per unit | slider | 0.05 | 0–0.5 | `_capture_ticks` bonus × elapsed per unit past 1st; capped at 10 effective |
| `ArmyRetreat` | Outmatched armies retreat | switch | on | — | `WarFx.RetreatUntil` flag drives `PatchCheckAttack` redirect |
| `RetreatRatio` | Retreat when outnumbered × | slider | 2.5 | 1–6 | Enemy warriors ≥ units × this → retreat |
| `RetreatMinUnits` | Min army size to retreat | slider | 3 | 0–20 | Tiny patrols never retreat |
| `RetreatSeconds` | Retreat duration | slider | 60 | 10–300 | Seconds the flag stays live before re-check |
| `ArmyExhaustion` | Field armies exhaust | switch | on | — | `WarFx.AwaySince` age → `IsExhausted` |
| `ExhaustAfterSec` | Away time before exhaustion | slider | 240 | 30–1200 | Seconds off home zones before effects apply |
| `ExhaustDesertionMult` | Exhaustion desertion × | slider | 3 | 1–8 | Multiplies `TickDesertion` chance while exhausted |
| `ExhaustFleeMult` | Exhaustion rout threshold × | slider | 1.2 | 0–3 | `run_away` threshold × while exhausted (0 disables) |

## Statecraft

v1.0 diplomacy layer — war exits, peace plots, and reputation.

| Id | Label | Type | Default | Range | Controls |
|---|---|---|---|---|---|
| `SeparatePeace` | Separate peace | switch | on | — | Collapsing participants leave wars early |
| `SepPeaceYears` | Min war age for exit | slider | 6 | 1–30 | War must be this old |
| `SepPeaceShare` | Collapse threshold (warrior share) | slider | 0.25 | 0.05–1 | Warrior count below this fraction of the main belligerent |
| `SepPeaceChance` | Exit chance per tick | slider | 0.1 | 0–1 | Roll per `War.update` |
| `PeacePlots` | Retuned peace plots | switch | on | — | `attacker_stop_war` parameter + reuse-loop rewrite |
| `PeacePlotDiplo` | Peace plot min diplomacy | slider | 8 | 0–20 | Replaces vanilla `min_diplomacy` |
| `PeacePlotCost` | Peace plot gold cost | slider | 20 | 0–100 | Replaces vanilla `money_cost` |
| `PeacePlotRatio` | Peace plot dominance ratio | slider | 2 | 1–5 | Warrior dominance required to sue for peace |
| `TwoFronts` | Two-front wars | switch | on | — | Supreme kingdoms can open second fronts |
| `TwoFrontChance` | Second front chance | slider | 0.3 | 0–1 | Roll when the only blocker is an active war |
| `BorderFriction` | Border friction opinion | switch | on | — | Wraps `opinion_borders.calc` |
| `BorderFrictionMult` | Border opinion (x) | slider | 2 | 0–4 | Multiplies the vanilla ±25 border opinion |
| `TruceBreak` | Truce-break reputation | switch | on | — | Marks early re-declarations + opinion penalty |
| `TruceBreakYears` | Reputation lasts (years) | slider | 15 | 1–50 | How long `opinion_truce_breaker` applies |
| `TruceBreakOpinion` | Opinion penalty | slider | 60 | 0–200 | Negative opinion against the breaker |
| `TributeInstallments` | Installment tribute | switch | on | — | Yearly gold from loser capital to winner |
| `TributeYears` | Tribute duration (years) | slider | 10 | 1–50 | Deal length after a decisive win |
| `TributeAnnual` | Tribute per year (gold) | slider | 15 | 0–100 | `min(this, available gold)` per year |

## Debug

| Id | Label | Default | Controls |
|---|---|---|---|
| `LogTuning` | Log tuning to Player.log | on | `Tuning.Log` output — retune lines, missing-decision warnings |

## Notes

- **Missing keys fall back to defaults** — `Cfg.Slider`/`Switch` swallow lookup failures, so an
  outdated `config.json` after an update can't break the mod.
- Config loads once in `OnModLoad`; changes via the UI take effect on the values NML writes —
  some retunes may need a reload to re-apply cleanly (originals are snapshotted at first apply).
- To add an option: `Cfg` field + `Load` line → `default_config.json` entry (group →
  `Id`/`Type`/`FloatVal`+range or `BoolVal`) → label in `Locales/en.json`. Keep ids identical
  across all three files.
