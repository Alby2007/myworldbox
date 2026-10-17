using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ai.behaviours;

namespace CivMindOverhaul
{
    [HarmonyPatch(typeof(DecisionsLibrary), "init")]
    internal static class PatchDecisionsInit
    {
        private static void Postfix()
        {
            try
            {
                Tuning.Apply();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] tuning failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(City), "setCitizenJob")]
    internal static class PatchSetCitizenJob
    {
        private static readonly FieldInfo _timerWarrior = AccessTools.Field(typeof(City), "_timer_warrior");
        private static readonly MethodInfo _tryToMakeWarrior = AccessTools.Method(typeof(City), "tryToMakeWarrior");
        private static readonly MethodInfo _isProfession = AccessTools.Method(typeof(Actor), "isProfession", new Type[] { typeof(UnitProfession) });
        private static readonly FieldInfo _stats = AccessTools.Field(typeof(BaseSimObject), "stats");

        private static bool Prefix(City __instance, Actor pActor)
        {
            if (!Cfg.AptitudeHiring && !Cfg.EmergencyDraft)
            {
                return true;
            }
            try
            {
                bool danger = Cfg.EmergencyDraft && __instance.isInDanger();
                bool timerOk = danger || _timerWarrior == null || (float)_timerWarrior.GetValue(__instance) <= 0f;
                if (!__instance.isGettingCaptured()
                    && timerOk
                    && IsUnitProfession(pActor)
                    && __instance.getResourcesAmount("gold") > 10
                    && __instance.hasEnoughFoodForArmy()
                    && _tryToMakeWarrior != null
                    && (bool)_tryToMakeWarrior.Invoke(__instance, new object[] { pActor }))
                {
                    return false;
                }
                if (!Cfg.AptitudeHiring)
                {
                    return true;
                }
                if (CheckList(AssetManager.citizen_job_library.list_priority_high, __instance, pActor))
                {
                    return false;
                }
                if (!__instance.hasAnyFood() && CheckList(AssetManager.citizen_job_library.list_priority_high_food, __instance, pActor))
                {
                    return false;
                }
                if (TryAptitudeJob(__instance, pActor))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] setCitizenJob patch failed: " + e.Message);
                return true;
            }
        }

        private static bool IsUnitProfession(Actor pActor)
        {
            return _isProfession != null && (bool)_isProfession.Invoke(pActor, new object[] { UnitProfession.Unit });
        }

        private static bool CheckJob(CitizenJobAsset pJob, City pCity, Actor pActor)
        {
            if (pJob.only_leaders && !pActor.isKing() && !pActor.isCityLeader())
            {
                return false;
            }
            if (pJob.should_be_assigned != null && !pJob.should_be_assigned(pActor))
            {
                return false;
            }
            if (!pCity.jobs.hasJob(pJob))
            {
                return false;
            }
            pCity.jobs.takeJob(pJob);
            pActor.setCitizenJob(pJob);
            return true;
        }

        private static bool CheckList(List<CitizenJobAsset> pList, City pCity, Actor pActor)
        {
            for (int i = 0; i < pList.Count; i++)
            {
                if (CheckJob(pList[i], pCity, pActor))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryAptitudeJob(City pCity, Actor pActor)
        {
            List<CitizenJobAsset> list = AssetManager.citizen_job_library.list_priority_normal;
            CitizenJobAsset best = null;
            float bestScore = -1f;
            for (int i = 0; i < list.Count; i++)
            {
                CitizenJobAsset job = list[i];
                if ((job.ok_for_king || !pActor.isKing()) && (job.ok_for_leader || !pActor.isCityLeader()) && pCity.jobs.hasJob(job))
                {
                    float score = Aptitude(pActor, job);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = job;
                    }
                }
            }
            if (best != null && CheckJob(best, pCity, pActor))
            {
                return true;
            }
            return false;
        }

        private static float Aptitude(Actor pActor, CitizenJobAsset pJob)
        {
            switch (pJob.id)
            {
                case "builder":
                case "road_builder":
                    return Stat(pActor, "construction_speed") + pActor.stewardship * 0.5f;
                case "hunter":
                case "attacker":
                    return pActor.warfare + Stat(pActor, "accuracy") * 0.5f;
                case "farmer":
                case "woodcutter":
                case "miner":
                case "miner_deposit":
                case "gatherer_bushes":
                case "gatherer_herbs":
                case "gatherer_honey":
                    return pActor.stewardship;
                default:
                    return 0.01f;
            }
        }

        private static float Stat(Actor pActor, string pId)
        {
            if (_stats == null)
            {
                return 0f;
            }
            BaseStats s = _stats.GetValue(pActor) as BaseStats;
            if (s == null)
            {
                return 0f;
            }
            return s[pId];
        }
    }

    [HarmonyPatch(typeof(City), "checkIfWarriorStillOk")]
    internal static class PatchWarriorStillOk
    {
        private static bool Prefix(Actor pActor, ref bool __result)
        {
            try
            {
                if (Cfg.EmergencyDraft && pActor.city != null && pActor.city.isInDanger())
                {
                    __result = true;
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Actor), "endJob")]
    internal static class PatchEndJob
    {
        private static readonly MethodInfo _addExperience = AccessTools.Method(typeof(Actor), "addExperience");

        private static void Prefix(Actor __instance, out CitizenJobAsset __state)
        {
            __state = __instance.citizen_job;
        }

        private static void Postfix(Actor __instance, CitizenJobAsset __state)
        {
            try
            {
                if (__state == null || _addExperience == null || Cfg.JobXpAmount <= 0)
                {
                    return;
                }
                _addExperience.Invoke(__instance, new object[] { Cfg.JobXpAmount });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] job xp failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(CityBehCheckCitizenTasks), "execute")]
    internal static class PatchCitizenTasks
    {
        private static readonly MethodInfo _hasBuildingToBuild = AccessTools.Method(typeof(City), "hasBuildingToBuild");
        private static readonly MethodInfo _hasStorage = AccessTools.Method(typeof(City), "hasStorageBuilding");
        private static readonly MethodInfo _hasBuildingType = AccessTools.Method(typeof(City), "hasBuildingType", new Type[]
        {
            typeof(string),
            typeof(bool),
            typeof(TileIsland)
        });

        private static void Postfix(City pCity)
        {
            try
            {
                if (pCity == null || !DebugConfig.isOn(DebugOption.SystemCityTasks) || pCity.status.population_adults <= 0)
                {
                    return;
                }
                CitizenJobs jobs = pCity.jobs;
                if (Cfg.EmergencyDraft && Cfg.DraftAttackerSlots > 0 && pCity.isInDanger())
                {
                    jobs.addToJob(CitizenJobLibrary.attacker, Cfg.DraftAttackerSlots);
                }
                if (Cfg.ExtraBuilders > 0 && Call(_hasBuildingToBuild, pCity))
                {
                    jobs.addToJob(CitizenJobLibrary.builder, Cfg.ExtraBuilders);
                }
                bool storage = Call(_hasStorage, pCity);
                if (Cfg.ExtraFoodJobs > 0 && storage && pCity.getTotalFood() < Cfg.LowFoodThreshold)
                {
                    jobs.addToJob(CitizenJobLibrary.gatherer_bushes, Cfg.ExtraFoodJobs);
                    jobs.addToJob(CitizenJobLibrary.gatherer_herbs, Cfg.ExtraFoodJobs / 2);
                    jobs.addToJob(CitizenJobLibrary.gatherer_honey, Cfg.ExtraFoodJobs / 2);
                    if (Call(_hasBuildingType, pCity, "type_windmill", true, null))
                    {
                        jobs.addToJob(CitizenJobLibrary.farmer, Cfg.ExtraFoodJobs);
                    }
                }
                if (Cfg.ExtraWoodcutters > 0 && storage && pCity.amount_wood <= Cfg.LowWoodThreshold)
                {
                    jobs.addToJob(CitizenJobLibrary.woodcutter, Cfg.ExtraWoodcutters);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] job boost failed: " + e.Message);
            }
        }

        private static bool Call(MethodInfo pMethod, City pCity, params object[] pArgs)
        {
            return pMethod != null && (bool)pMethod.Invoke(pCity, pArgs);
        }
    }

    [HarmonyPatch(typeof(SimGlobals), "init")]
    internal static class PatchSimGlobalsInit
    {
        private static void Postfix()
        {
            try
            {
                SimGlobalAsset m = SimGlobals.m;
                if (m == null)
                {
                    return;
                }
                m.diplomacy_years_war_timeout = (int)Cfg.WarTimeoutYears;
                m.diplomacy_years_war_min_warriors = (int)Cfg.WarMinWarriors;
                m.minimum_years_between_wars = (int)Cfg.WarPeaceYears;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] sim globals tweak failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(City), "isOkToSendArmy")]
    internal static class PatchOkToSendArmy
    {
        private static bool Prefix(City __instance, ref bool __result)
        {
            try
            {
                if (!__instance.hasArmy())
                {
                    __result = false;
                    return false;
                }
                __result = (float)__instance.army.countUnits() / __instance.getMaxWarriors() >= Cfg.MarchFill;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Army), "findCaptain")]
    internal static class PatchFindCaptain
    {
        private static bool Prefix(Army __instance)
        {
            if (!Cfg.MeritCaptains)
            {
                return true;
            }
            try
            {
                if (__instance.isLocked())
                {
                    return false;
                }
                if (__instance.hasCaptain())
                {
                    if (__instance.getCaptain().isKingdomCiv())
                    {
                        return false;
                    }
                    __instance.setCaptain(null);
                }
                if (__instance.units.Count == 0)
                {
                    return false;
                }
                Actor best = null;
                float bestScore = float.MinValue;
                for (int i = 0; i < __instance.units.Count; i++)
                {
                    Actor u = __instance.units[i];
                    if (u.army != __instance || u.isRekt())
                    {
                        continue;
                    }
                    float s = u.warfare * 2f + u.level;
                    if (s > bestScore)
                    {
                        bestScore = s;
                        best = u;
                    }
                }
                if (best != null)
                {
                    __instance.setCaptain(best);
                }
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DiplomacyHelpers), "getWarTarget")]
    internal static class PatchGetWarTarget
    {
        private static bool Prefix(ref Kingdom __result, Kingdom pInitiatorKingdom)
        {
            if (!Cfg.SmartWarTarget)
            {
                return true;
            }
            try
            {
                int own = pInitiatorKingdom.countTotalWarriors();
                if (pInitiatorKingdom.hasAlliance())
                {
                    own = pInitiatorKingdom.getAlliance().countWarriors();
                }
                Kingdom best = null;
                float bestScore = float.MaxValue;
                ListPool<Kingdom> pool = World.world.wars.getNeutralKingdoms(pInitiatorKingdom);
                try
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        Kingdom k = pool[i];
                        if (!k.hasCities() || !k.hasCapital() || k.getAge() < SimGlobals.m.minimum_kingdom_age_for_attack)
                        {
                            continue;
                        }
                        int def = k.hasAlliance() ? k.getAlliance().countWarriors() : k.countTotalWarriors();
                        if (own < def)
                        {
                            continue;
                        }
                        if (!pInitiatorKingdom.capital.reachableFrom(k.capital))
                        {
                            continue;
                        }
                        if (Date.getYearsSince(World.world.diplomacy.getRelation(pInitiatorKingdom, k).data.timestamp_last_war_ended) < SimGlobals.m.minimum_years_between_wars)
                        {
                            continue;
                        }
                        if (pInitiatorKingdom.isOpinionTowardsKingdomGood(k))
                        {
                            continue;
                        }
                        float score = Kingdom.distanceBetweenKingdom(pInitiatorKingdom, k)
                            * (0.4f + (float)def / Math.Max(1, own))
                            * (World.world.wars.hasWars(k) ? 0.6f : 1f);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = k;
                        }
                    }
                }
                finally
                {
                    pool.Dispose();
                }
                __result = best;
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] getWarTarget patch failed: " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CityBehCheckAttackZone), "findTargetCity")]
    internal static class PatchFindTargetCity
    {
        private static readonly FieldInfo _kingdom = AccessTools.Field(typeof(City), "kingdom");
        private static readonly FieldInfo _cityCenter = AccessTools.Field(typeof(City), "city_center");

        private static bool Prefix(City pOurCity, ref City __result)
        {
            if (!Cfg.SmartCityTarget || _kingdom == null || _cityCenter == null)
            {
                return true;
            }
            try
            {
                Kingdom kingdom = _kingdom.GetValue(pOurCity) as Kingdom;
                if (kingdom == null)
                {
                    return true;
                }
                City best = null;
                float bestScore = float.MaxValue;
                float own = kingdom.countTotalWarriors();
                UnityEngine.Vector2 ownCenter = (UnityEngine.Vector2)_cityCenter.GetValue(pOurCity);
                ListPool<City> pool = new ListPool<City>(World.world.cities.Count);
                try
                {
                    World.world.wars.getWarCities(kingdom, pool);
                    for (int i = 0; i < pool.Count; i++)
                    {
                        City c = pool[i];
                        if (!c.reachableFrom(pOurCity))
                        {
                            continue;
                        }
                        float dist = Toolbox.SquaredDistVec2Float((UnityEngine.Vector2)_cityCenter.GetValue(c), ownCenter);
                        float score = dist * (0.5f + c.countWarriors() / Math.Max(5f, own));
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = c;
                        }
                    }
                }
                finally
                {
                    pool.Dispose();
                }
                __result = best;
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] findTargetCity patch failed: " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(War), "update")]
    internal static class PatchWarUpdate
    {
        private static void Postfix(War __instance)
        {
            try
            {
                if (Cfg.WarStalemateYears <= 0f || __instance.hasEnded() || __instance.isTotalWar())
                {
                    return;
                }
                if (__instance.getAge() < Cfg.WarStalemateYears)
                {
                    return;
                }
                if (!Randy.randomChance(0.02f))
                {
                    return;
                }
                Kingdom atk = __instance.getMainAttacker();
                Kingdom def = __instance.getMainDefender();
                if (atk == null || def == null || !atk.isAlive() || !def.isAlive())
                {
                    return;
                }
                if (DiplomacyHelpers.isThereFightBetween(atk, def))
                {
                    return;
                }
                World.world.wars.endWar(__instance, WarWinner.Peace);
                UnityEngine.Debug.Log("[CivMind] stalemate peace: " + __instance.name);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] war update patch failed: " + e.Message);
            }
        }
    }
}
