# Civ Mind Overhaul

A gameplay-overhaul mod for [WorldBox](https://store.steampowered.com/app/1206560/WorldBox__God_Simulator) that makes
civilization and combat AI noticeably smarter — citizens seek work where it's needed, cities staff up under pressure,
armies rout when beaten, and wars actually end.

**Game version:** WorldBox ~0.51.x / build 719 · **Loader:** [NeoModLoader](https://github.com/NeoModLoader/NeoModLoader) 1.2.0.1

## What it does

### Citizen & city AI
- **Demand-driven job seeking** — idle units hunt for jobs harder when the city has open slots, scaled by hunger
- **Pressure staffing** — cities add extra builder/food/woodcutter job slots during shortages and construction
- **Emergency militia draft** — cities under attack bypass the warrior cooldown and open militia slots
- **Aptitude hiring** — workers are matched to jobs by `warfare`/`stewardship`/`construction_speed`/`accuracy`
- **Housing-gated reproduction** — homeless units reproduce at a reduced rate
- **Job XP** — completing a citizen job grants experience

### Combat & war AI
- **Morale retreat** — units rout below a health threshold (vanilla flees only while *healthy*); traits and
  `warfare` shift the threshold
- **Merit captains** — the best warrior leads the army, not the nearest
- **Smarter war & siege targets** — kingdoms and cities pick targets weighing distance and defender strength
- **Stalemate peace** — wars with no fighting auto-end after a configurable age
- **Warscore surrender** — lopsided kill counts force the losing side to surrender
- **War tempo tuning** — war timeout, minimum warriors, and rematch cooldown are configurable
- **Training retune** — warriors use training dummies more often

### War tactics (v0.3)
- **Focus fire** — army members converge on the captain's target
- **Home-field advantage** — defenders inside own city borders take reduced damage
- **Desertion** — armies stuck in long wars abroad bleed deserters
- **Pillage** — soldiers loot enemy city gold and haul it home to their stockpiles
- **Champion duels** — enemy captains near each other duel (one roll per encounter)
- **Morale shock** — kill the captain in the field and his army routs
- **Naval invasions** — warriors pile into transport boats for overseas assaults, following the captain
- **Berserker rage** — kill streaks grant a short `rage` status (double damage)

Every feature is toggleable/tunable in-game: **NML window → mod → Mod Configuration** (~30 options).
See [docs/config-reference.md](docs/config-reference.md) for the full list.

## Install

1. Install NeoModLoader into `worldbox_Data/StreamingAssets/mods/`
2. Copy `mod/` into `<game>/Mods/CivMindOverhaul/` (folder must contain `mod.json` + the `.cs` files — NML compiles
   source at startup, no prebuilt DLL needed)
3. Enable **Experimental Mode** in game settings (required or no mods load)
4. Launch — check `Player.log` for `[CivMind]` lines

## Development

- `mod/` — mod source (junctioned to the live `Mods/` folder in the dev environment; edits are live)
- `build/` — `dotnet build` in `build/` typechecks the mod against real game DLLs (netstandard2.1)
- `game-api/` — curated decompiled classes for reference; `decompiled/` holds a full `Assembly-CSharp` decompile
  (both stay local — not in this repo)

## Documentation

| Doc | Contents |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Runtime lifecycle, module map, patch map |
| [docs/vanilla-ai-primer.md](docs/vanilla-ai-primer.md) | How WorldBox's utility AI, job board, and war pipeline work |
| [docs/citizen-ai.md](docs/citizen-ai.md) | Citizen/city features in depth |
| [docs/war-ai.md](docs/war-ai.md) | Combat/war features in depth |
| [docs/patching-guide.md](docs/patching-guide.md) | Modding conventions, reflection patterns, how to add features |
| [docs/config-reference.md](docs/config-reference.md) | Every config option and what it controls |

## License / disclaimers

Fan-made mod, not affiliated with Maxim Karpenko or WorldBox. Game code is not distributed here — all patches are
applied at runtime via Harmony.
