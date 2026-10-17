# Patching Guide

Conventions used throughout `Patches.cs`/`Tuning.cs`, the access gotchas discovered so far,
and a checklist for adding a feature.

## Patch anatomy

```csharp
[HarmonyPatch(typeof(City), "setCitizenJob")]          // target: type + method name (string — works for private methods)
internal static class PatchSetCitizenJob
{
    // reflected handles cached once per class — never per-call
    private static readonly FieldInfo _timerWarrior =
        AccessTools.Field(typeof(City), "_timer_warrior");

    private static bool Prefix(City __instance, Actor pActor)   // __instance = this; params by name
    {
        if (!Cfg.SomeFlag) return true;                 // feature off → vanilla
        try
        {
            /* ... */
            return false;                               // consumed — skip vanilla
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[CivMind] ...: " + e.Message);
            return true;                                // degrade to vanilla on failure
        }
    }
}
```

Rules every patch follows:

1. **Config gate first** — every feature is behind a `Cfg.*` switch (or `> 0` check for numerics).
2. **try/catch the body** — on exception, prefix returns `true` (vanilla) / postfix just logs.
   A failed patch degrades; it never crashes the sim.
3. **Supplant = prefix `return false`** — set `__result` (for non-void) yourself.
4. **`__state` for prefix→postfix handoff** — e.g. `PatchEndJob` captures `citizen_job` before
   vanilla clears it.
5. **Hot paths stay cheap** — `War.update` runs every frame: throttle with
   `Randy.randomChance(0.02f)` and do zero reflection in the loop.
6. **Log prefix is `[CivMind]`** — greppable in `Player.log`.

## Tuning conventions

- Always snapshot before mutating: `_origWeight/_origCooldown/_origCustom` dictionaries keep
  vanilla values so retunes apply to *originals*, and re-application stays idempotent.
- Wrap, don't replace: when `weight_calculate_custom` exists, compose `orig(a) * mult`
  instead of stomping it.
- `Retune(...)` for plain weight/cooldown; dedicated helpers (`RetuneHousing`,
  `ReworkRetreat`) when a delegate needs logic.
- `Find()` returns null on missing ids → log `"missing decision: <id>"` and continue. A game
  update renaming a decision shouldn't kill the whole pass.

## Access gotchas (build 719)

**Internal/private — reflect via `AccessTools`:**

| Member | Notes |
|---|---|
| `City._timer_warrior` | 15s convert / 30s demobilize cooldown |
| `City.tryToMakeWarrior` | private; warrior conversion incl. cap checks |
| `City.hasBuildingToBuild`, `hasStorageBuilding`, `hasBuildingType(string,bool,TileIsland)` | used by pressure staffing |
| `City.kingdom`, `City.city_center` | internal fields |
| `Actor.isProfession(UnitProfession)` | internal |
| `Actor.addExperience` | internal |
| `Actor.hasWeapon` | internal |
| `BaseSimObject.stats` | internal field; the `BaseStats` indexer `stats["id"]` is public once you have it |

**Public — call directly (don't reflect what isn't needed):**
`Actor.city` (use `a.city != null`, NOT `hasCity()` — that's internal) · `isKingdomCiv()` on
`BaseSimObject` · `city.jobs.addToJob` · `CitizenJobAsset.should_be_assigned` (public delegate —
assignment-filter hook, no patch needed) · `a.warfare/stewardship/intelligence/diplomacy` ·
`SimGlobals.m` · `Army.findCaptain`, `CityBehCheckAttackZone.findTargetCity` are private but
patchable by name.

**Traps:** `Kingdom` has no `hasWars()` — use `hasEnemies()` or `World.world.wars.hasWars(k)`.
`hasCity()` is internal on Actor.

## `ListPool<T>` etiquette

Struct pool — leaks if not disposed:

```csharp
ListPool<Kingdom> pool = World.world.wars.getNeutralKingdoms(k);
try {
    for (int i = 0; i < pool.Count; i++) { var x = pool[i]; /* ... */ }
} finally {
    pool.Dispose();
}
```

No `using` declarations — the NML compiler is older; use try/finally.

## Adding a new feature — checklist

1. **Pick the mechanism** — retune for weights/cooldowns/scores (`Tuning.cs`), patch for
   procedural changes (`Patches.cs`).
2. **Find the hook** — search `decompiled/` for the vanilla method; confirm signature and
   accessibility (see table above).
3. **Config** — add fields to `Cfg.cs` + load line, slider/switch in `default_config.json`,
   label in `Locales/en.json`. Group it under an existing tab unless it needs a new one.
4. **Write the patch/tuning** following the rules above; mirror vanilla structure where the
   goal is "same flow, better choice."
5. **Update [config-reference.md](config-reference.md)** and the feature list in `AGENTS.md`.
6. **Verify** — `cd build && dotnet build` for the typecheck, then in-game: watch
   `Player.log` for `[CivMind]` lines (keep `LogTuning` on while iterating).

## Build & debug

- `build/build.csproj` compiles `mod/**/*.cs` against the real game DLLs (`netstandard2.1`).
  It exists only to typecheck — the game runs the loose source via NML.
- NML compiles mods at startup; a compile error shows in its mod list and `Player.log`.
- Harmony is embedded in NML (`HarmonyLib` namespace); offline builds reference `build/0Harmony.dll`.
- Experimental Mode must be ON in game settings or nothing loads.
