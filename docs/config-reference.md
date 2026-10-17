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
| `SocializeWeight` | Socializing urge | 1.6 | 0–4 | Weight × on `socialize_initial_check`, `check_swearing`, `try_to_read` |

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

v0.6 features — loot/tax economy, item production, trade boats.

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
