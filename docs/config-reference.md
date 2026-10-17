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
