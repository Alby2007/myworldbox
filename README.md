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

### Politics & home front (v0.4)
- **Merit succession** — kings are succeeded by the best-stat heir, not a dice roll
- **War reparations** — the losing side's cities pay a share of their gold to the victor
- **War heroes** — each side's top wartime killer earns the `veteran` trait
- **Regional alliances** — kingdoms ally with their nearest valid partner, forming blocs
- **Commander aura** — troops hold the line longer while their captain stands nearby
- **Rally flee** — routed warriors fall back toward their captain or home city
- **Militia reserve** — cities keep a garrison at home instead of marching at full strength
- **War refugees** — displaced civilians actively seek shelter in friendly cities
- **Merit leaders** — cities are run by their best stewards
- **Occupied-city uprisings** — unhappy recently-conquered cities can revolt into a rebellion war

### Realpolitik, economy & culture (v0.5)
- **King personality** — pacifist kings veto wars; warlord kings declare with smaller armies
- **Pretender wars** — a clearly-better rival seizes their city in a succession war
- **Collapse pressure** — weak kings watch low-loyalty provinces drift away peacefully
- **Anti-hegemon coalitions** — neighbours of the supreme kingdom ally to contain it
- **War tribute & supply surge** — cities ship gold to the capital and resupply faster in wartime
- **Siege starvation** — besieged cities slowly bleed stockpiled food
- **Border pressure** — wartime cities grab empty frontier zones more aggressively
- **Reconstruction surge** — post-war cities get bonus builders for a few years
- **Holy wars** — different-religion kingdoms make juicier war targets
- **Assimilation** — conquered cities gradually adopt the victor's culture and faith
- **War exhaustion** — long wars make occupied (and unhappy) cities likelier to revolt

### Economy & logistics (v0.6–v0.8)
- **War chest** — the capital treasury funds kings who are ready for war but short on cash
- **Tax chain retune** — citizens→leaders→king tribute flows faster, filling war coffers
- **Item economy** — crafting and arming are far more common; drafted militia grabs weapons in danger
- **Trade boats & blockades** — docks trade harder in peace, sea trade collapses in war
- **Rich-target wars** — kingdoms pick on whoever's hoarding gold; wars of plunder
- **War dividend** — the crown skims a cut of every loot deposit; conquest funds kings
- **Soldier wages & mutiny** — standing armies drain city gold; broke kingdoms' armies desert
- **Trade income** — completed boat runs pay gold scaled by distance; docks earn real revenue
- **Equipment market** — citizens pay `money` for gear; armorer markets fund city stockpiles
- **Tax→unrest** — heavy local tax rates push citizens toward revolt
- **Corruption** — disloyal city leaders skim the tribute meant for the king
- **Famine relief** — kingdom supply runs go to the hungriest city, not a random one
- **Labor migration** — homeless migrants pick cities with open jobs
- **Golden age** — kingdoms at peace for years craft faster and get bonus builders
- **Mercenaries** — cities pay gold signing bonuses to arm migrant drifters
- **Embargoes** — alliance members' boats boycott any ally's enemy docks
- **War profiteers** — neutral dock cities trade harder while others bleed

### Combat depth & statecraft (v1.0)

- **Combat micro** — crits and accuracy-scaled dodges, veteran armor penetration, heavy-hit staggers
- **Finish wounded** — armies preferentially execute wounded enemies in reach
- **Wounded rotation** — idle bloodied warriors rotate home to heal
- **Veteran specials** — battle-hardened soldiers bypass combat-action gates
- **Formations & kiting** — ranged units skirmish away from closing melee
- **Relief marches** — idle armies march to relieve besieged cities
- **Army veterancy** — veteran armies resist morale shock and rally harder
- **Veteran retention** — high-kill soldiers survive demobilization
- **Combined arms** — melee-heavy armies preferentially draw ranged gear
- **Siege momentum** — mass assaults fill capture bars faster
- **Separate peace** — collapsing allies bail out of lost wars
- **Peace plots** — `attacker_stop_war` retuned, no duplicate-plot spam
- **Two-front wars** — supreme kingdoms can open a second front
- **Border friction** — neighbor opinion penalty amplified
- **Truce-break reputation** — early re-declarations cost global opinion
- **Installment tribute** — losers pay the victor yearly for a decade

### Life & attrition (v1.1)

- **Army retreats** — outmatched armies march home instead of dying in place (redirects vanilla attack orders)
- **Field exhaustion** — armies away from home zones too long suffer desertion and rout easier
- **Civilian rescue** — stranded citizens re-request boats sooner; transport boats outrank trade runs
- **Victory celebrations** — winning a decisive war sets off `festive_spirit` fireworks in the victor's cities
- **Education retune** — citizens read more often and with a relaxed intelligence gate (books → stats → leaders)
- **Intensity-scaled firefighting** — `put_out_fire` weight scales with active fires; infernos get swarmed

Every feature is toggleable/tunable in-game: **NML window → mod → Mod Configuration** (~180 options).
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
