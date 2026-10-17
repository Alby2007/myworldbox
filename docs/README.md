# Civ Mind Overhaul — Documentation

These docs describe how the mod works and the WorldBox systems it hooks into.
Target reader: someone (human or agent) modifying the mod.

## Reading order

1. **[architecture.md](architecture.md)** — the 10,000-ft view: how the mod loads, the two mechanisms it uses
   (Harmony patches vs asset retuning), and a map of every patch to its vanilla target.
2. **[vanilla-ai-primer.md](vanilla-ai-primer.md)** — how WorldBox's AI actually works: utility-based decisions,
   neuro-layer priorities, the city job board, professions, and the war/diplomacy pipeline. Read this before
   touching any patch.
3. **[citizen-ai.md](citizen-ai.md)** — deep dive on the citizen/city features (features 1–6).
4. **[war-ai.md](war-ai.md)** — deep dive on the combat/war features (features 7–14).
5. **[patching-guide.md](patching-guide.md)** — code conventions: patch anatomy, reflection access patterns,
   fallback style, and a checklist for adding a new feature.
6. **[config-reference.md](config-reference.md)** — every user-facing option, its default, range, and which
   code path reads it.

## Quick map

```
WorldBox (Assembly-CSharp)
├── UtilityBasedDecisionSystem ──► DecisionAssets (weights/cooldowns) ◄── Tuning.cs retunes these
├── City jobs (CitizenJobs) ◄── PatchCitizenTasks tops up slots
├── City.setCitizenJob ◄── PatchSetCitizenJob (draft + aptitude hiring)
├── DiplomacyHelpers.getWarTarget ◄── PatchGetWarTarget
├── CityBehCheckAttackZone.findTargetCity ◄── PatchFindTargetCity
├── Army.findCaptain ◄── PatchFindCaptain
├── City.isOkToSendArmy ◄── PatchOkToSendArmy
├── City.checkIfWarriorStillOk ◄── PatchWarriorStillOk
├── Actor.endJob ◄── PatchEndJob (XP)
├── War.update ◄── PatchWarUpdate (stalemate peace)
└── SimGlobals.init ◄── PatchSimGlobalsInit (war tempo)
```

Source of truth for gotchas and discoveries: [../AGENTS.md](../AGENTS.md).
