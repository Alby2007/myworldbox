using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ai.behaviours;
using life.taxi;

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
        private static readonly FieldInfo _timerWarrior = Ref.Field(typeof(City), "_timer_warrior");
        private static readonly MethodInfo _tryToMakeWarrior = Ref.Method(typeof(City), "tryToMakeWarrior");
        private static readonly MethodInfo _isProfession = Ref.Method(typeof(Actor), "isProfession", new Type[] { typeof(UnitProfession) });
        private static readonly FieldInfo _stats = Ref.Field(typeof(BaseSimObject), "stats");

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
                    if (danger)
                    {
                        First.Fire("emergency draft");
                    }
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
                First.Fire("aptitude hiring");
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
                case "militia":
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
        private static readonly MethodInfo _isArmyOverLimit = Ref.Method(typeof(City), "isArmyOverLimit");

        private static bool Prefix(City __instance, Actor pActor, ref bool __result)
        {
            try
            {
                if (Cfg.EmergencyDraft && pActor.city != null && pActor.city.isInDanger())
                {
                    __result = true;
                    First.Fire("stay mobilized");
                    return false;
                }
                if (!Cfg.VetRetention || _isArmyOverLimit == null)
                {
                    return true;
                }
                bool flag = true;
                if ((bool)_isArmyOverLimit.Invoke(__instance, null))
                {
                    flag = false;
                }
                else if (!__instance.hasEnoughFoodForArmy())
                {
                    flag = false;
                }
                if (flag)
                {
                    return true;
                }
                ActorData d = Rebel.Data(pActor);
                if (d != null && d.kills >= Cfg.VetKeepKills && Randy.randomChance(Cfg.VetKeepChance))
                {
                    __result = true;
                    First.Fire("veteran retention");
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
        private static readonly MethodInfo _addExperience = Ref.Method(typeof(Actor), "addExperience");

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
                First.Fire("job xp");
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
        private static readonly MethodInfo _hasBuildingToBuild = Ref.Method(typeof(City), "hasBuildingToBuild");
        private static readonly MethodInfo _hasStorage = Ref.Method(typeof(City), "hasStorageBuilding");
        private static readonly MethodInfo _hasBuildingType = Ref.Method(typeof(City), "hasBuildingType", new Type[]
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
                    First.Fire("militia draft slots");
                }
                if (Cfg.ExtraBuilders > 0 && Call(_hasBuildingToBuild, pCity))
                {
                    jobs.addToJob(CitizenJobLibrary.builder, Cfg.ExtraBuilders);
                    First.Fire("extra builders");
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
                    First.Fire("extra food jobs");
                }
                if (Cfg.ExtraWoodcutters > 0 && storage && pCity.amount_wood <= Cfg.LowWoodThreshold)
                {
                    jobs.addToJob(CitizenJobLibrary.woodcutter, Cfg.ExtraWoodcutters);
                    First.Fire("extra woodcutters");
                }
                if (Cfg.ReconBuilders > 0)
                {
                    Kingdom k = Rebel.Of(pCity);
                    double peace;
                    if (k != null && WarFx.PeaceTime.TryGetValue(k.id, out peace) && Date.getYearsSince(peace) < Cfg.ReconYears)
                    {
                        jobs.addToJob(CitizenJobLibrary.builder, (int)Cfg.ReconBuilders);
                        First.Fire("reconstruction surge");
                    }
                }
                if (Cfg.GoldenAge && Cfg.GoldenAgeBuilders > 0)
                {
                    Kingdom k2 = Rebel.Of(pCity);
                    if (k2 != null && Rebel.PeaceYears(k2) >= Cfg.GoldenAgeYears)
                    {
                        jobs.addToJob(CitizenJobLibrary.builder, (int)Cfg.GoldenAgeBuilders);
                        First.Fire("golden age slots");
                    }
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
                First.Fire("war tempo");
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
                if (!__result)
                {
                    First.Fire("march fill gate");
                }
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
                    First.Fire("merit captain");
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
                        if (Cfg.HolyWars && pInitiatorKingdom.religion != null && k.religion != null && pInitiatorKingdom.religion != k.religion)
                        {
                            score *= Cfg.HolyWarMult;
                            First.Fire("holy war");
                        }
                        if (Cfg.RichTargets)
                        {
                            score /= 1f + k.capital.getResourcesAmount("gold") / Math.Max(1f, Cfg.RichTargetGold);
                            First.Fire("rich target");
                        }
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
                First.Fire("smart war target");
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
        private static readonly FieldInfo _kingdom = Ref.Field(typeof(City), "kingdom");
        private static readonly FieldInfo _cityCenter = Ref.Field(typeof(City), "city_center");

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
                First.Fire("smart siege target");
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
        private static readonly HashSet<long> _warscoreDone = new HashSet<long>();
        private static readonly Dictionary<long, float> _sepPeaceNext = new Dictionary<long, float>();

        private static void Postfix(War __instance)
        {
            try
            {
                if (__instance.hasEnded() || __instance.isTotalWar())
                {
                    return;
                }
                if (Cfg.WarscoreSurrender && !_warscoreDone.Contains(__instance.id))
                {
                    int da = __instance.getDeadAttackers();
                    int dd = __instance.getDeadDefenders();
                    int big = Math.Max(da, dd);
                    int small = Math.Min(da, dd);
                    if (big >= Cfg.WarscoreMinDeaths && (small <= 0 || (float)big / small >= Cfg.WarscoreRatio))
                    {
                        Kingdom loser = (da > dd) ? __instance.getMainAttacker() : __instance.getMainDefender();
                        if (loser != null && loser.isAlive())
                        {
                            _warscoreDone.Add(__instance.id);
                            UnityEngine.Debug.Log("[CivMind] warscore surrender: " + loser.name + " (" + big + " vs " + small + " dead)");
                            First.Fire("warscore surrender");
                            __instance.lostWar(loser);
                            return;
                        }
                    }
                }
                float sepNext;
                if (Cfg.SeparatePeace && __instance.getAge() >= Cfg.SepPeaceYears
                    && (!_sepPeaceNext.TryGetValue(__instance.id, out sepNext) || sepNext <= UnityEngine.Time.time))
                {
                    _sepPeaceNext[__instance.id] = UnityEngine.Time.time + 5f;
                    if (Randy.randomChance(Cfg.SepPeaceChance))
                    {
                    Kingdom mainA = __instance.getMainAttacker();
                    Kingdom mainD = __instance.getMainDefender();
                    if (mainA != null && mainD != null)
                    {
                        ListPool<Kingdom> pool = new ListPool<Kingdom>();
                        try
                        {
                            foreach (Kingdom k in __instance.getAttackers())
                            {
                                pool.Add(k);
                            }
                            foreach (Kingdom k2 in __instance.getDefenders())
                            {
                                pool.Add(k2);
                            }
                            for (int i = 0; i < pool.Count; i++)
                            {
                                Kingdom k3 = pool[i];
                                if (k3 == null || k3 == mainA || k3 == mainD || !k3.isAlive())
                                {
                                    continue;
                                }
                                Kingdom main = __instance.isAttacker(k3) ? mainA : mainD;
                                if (main != null && main.isAlive() && k3.countTotalWarriors() < main.countTotalWarriors() * Cfg.SepPeaceShare)
                                {
                                    __instance.leaveWar(k3);
                                    First.Fire("separate peace");
                                    UnityEngine.Debug.Log("[CivMind] separate peace: " + k3.name + " left " + __instance.name);
                                    return;
                                }
                            }
                        }
                        finally
                        {
                            pool.Dispose();
                        }
                    }
                    }
                }
                if (Cfg.WarStalemateYears <= 0f || __instance.getAge() < Cfg.WarStalemateYears)
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
                First.Fire("stalemate peace");
                UnityEngine.Debug.Log("[CivMind] stalemate peace: " + __instance.name);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] war update patch failed: " + e.Message);
            }
        }
    }

    internal static class WarFx
    {
        internal static readonly Dictionary<long, float> Shaken = new Dictionary<long, float>();
        internal static readonly Dictionary<long, int> HeroKills = new Dictionary<long, int>();
        internal static readonly Dictionary<long, double> PeaceTime = new Dictionary<long, double>();
        internal static readonly Dictionary<long, float> Broke = new Dictionary<long, float>();
        internal static readonly Dictionary<long, float> VetArmies = new Dictionary<long, float>();
        internal static readonly Dictionary<long, int> TruceBreak = new Dictionary<long, int>();
        internal static readonly Dictionary<long, TributeDeal> Tribute = new Dictionary<long, TributeDeal>();
        internal static readonly Dictionary<long, float> RetreatUntil = new Dictionary<long, float>();
        internal static readonly Dictionary<long, float> AwaySince = new Dictionary<long, float>();

        internal static bool IsVeteran(long pArmyId)
        {
            float exp;
            return VetArmies.TryGetValue(pArmyId, out exp) && exp > UnityEngine.Time.time;
        }

        internal static bool IsRetreating(long pArmyId)
        {
            float exp;
            return RetreatUntil.TryGetValue(pArmyId, out exp) && exp > UnityEngine.Time.time;
        }

        internal static bool IsExhausted(long pArmyId)
        {
            float since;
            return AwaySince.TryGetValue(pArmyId, out since) && UnityEngine.Time.time - since > Cfg.ExhaustAfterSec;
        }
    }

    internal class TributeDeal
    {
        internal long Winner;
        internal int Expiry;
        internal int LastPaid;
    }

    [HarmonyPatch(typeof(Army), "checkCaptainExistence")]
    internal static class PatchArmyTick
    {
        private static readonly AccessTools.FieldRef<Actor, BaseSimObject> _attackTarget = TryRef<Actor, BaseSimObject>("attack_target");
        private static readonly AccessTools.FieldRef<Actor, bool> _hasAttackTarget = TryRef<Actor, bool>("has_attack_target");
        private static readonly AccessTools.FieldRef<City, Kingdom> _cityKingdom = TryRef<City, Kingdom>("kingdom");
        private static readonly AccessTools.FieldRef<Actor, ActorData> _actorData = TryRef<Actor, ActorData>("data");
        private static readonly AccessTools.FieldRef<Actor, WorldTile> _behTileTarget = TryRef<Actor, WorldTile>("beh_tile_target");
        private static readonly MethodInfo _findEnemies = Ref.Method(typeof(EnemiesFinder), "findEnemiesFrom", new Type[]
        {
            typeof(WorldTile),
            typeof(Kingdom),
            typeof(int)
        });
        private static readonly Dictionary<long, float> _duelPairs = new Dictionary<long, float>();

        private static AccessTools.FieldRef<T, F> TryRef<T, F>(string pName)
        {
            try
            {
                return Ref.FieldRef<T, F>(pName);
            }
            catch
            {
                return null;
            }
        }
        private static float _nextPrune;

        private static void Prefix(Army __instance, out Actor __state)
        {
            __state = __instance.getCaptain();
        }

        private static void Postfix(Army __instance, Actor __state)
        {
            try
            {
                TickMoraleShock(__instance, __state);
                Actor cap = __instance.getCaptain();
                if (cap == null || cap.isRekt() || cap.current_tile == null)
                {
                    return;
                }
                TickRetreat(__instance, cap);
                TickExhaustion(__instance, cap);
                bool retreating = WarFx.IsRetreating(__instance.id);
                if (!retreating)
                {
                    TickFocusFire(__instance, cap);
                    TickDuels(__instance, cap);
                }
                TickPillage(__instance, cap);
                TickDesertion(__instance, cap);
                TickRotation(__instance, cap);
                if (!retreating)
                {
                    TickFormations(__instance, cap);
                    TickRelief(__instance, cap);
                }
                TickVeterancy(__instance, cap);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] army tick failed: " + e.Message);
            }
        }

        private static void TickMoraleShock(Army pArmy, Actor pOldCaptain)
        {
            float now = UnityEngine.Time.time;
            if (now >= _nextPrune)
            {
                _nextPrune = now + 30f;
                Prune(WarFx.Shaken, now);
                Prune(_duelPairs, now);
                Prune(WarFx.RetreatUntil, now);
                Prune(WarFx.VetArmies, now);
                PruneDeadArmies(WarFx.AwaySince);
            }
            if (!Cfg.MoraleShock || pOldCaptain == null || !pOldCaptain.isRekt() || pArmy.isGroupInCityAndHaveLeader())
            {
                return;
            }
            if (WarFx.IsVeteran(pArmy.id) && Randy.randomChance(Cfg.VetShockResist))
            {
                return;
            }
            WarFx.Shaken[pArmy.id] = now + Cfg.ShockSeconds;
            First.Fire("morale shock");
            UnityEngine.Debug.Log("[CivMind] morale shock: " + pArmy.name + " lost " + pOldCaptain.getName());
        }

        private static void Prune(Dictionary<long, float> pDict, float pNow)
        {
            List<long> dead = null;
            foreach (KeyValuePair<long, float> kv in pDict)
            {
                if (kv.Value <= pNow)
                {
                    if (dead == null)
                    {
                        dead = new List<long>();
                    }
                    dead.Add(kv.Key);
                }
            }
            if (dead != null)
            {
                for (int i = 0; i < dead.Count; i++)
                {
                    pDict.Remove(dead[i]);
                }
            }
        }

        private static void PruneDeadArmies(Dictionary<long, float> pDict)
        {
            List<long> dead = null;
            foreach (KeyValuePair<long, float> kv in pDict)
            {
                if (World.world.armies.get(kv.Key) == null)
                {
                    if (dead == null)
                    {
                        dead = new List<long>();
                    }
                    dead.Add(kv.Key);
                }
            }
            if (dead != null)
            {
                for (int i = 0; i < dead.Count; i++)
                {
                    pDict.Remove(dead[i]);
                }
            }
        }

        private static void TickRetreat(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.ArmyRetreat || _findEnemies == null || WarFx.IsRetreating(pArmy.id) || !Randy.randomChance(0.05f))
            {
                return;
            }
            if (pArmy.countUnits() < Cfg.RetreatMinUnits || pCaptain.current_tile == null)
            {
                return;
            }
            Kingdom k = pArmy.getKingdom();
            if (k == null)
            {
                return;
            }
            EnemyFinderData data = _findEnemies.Invoke(null, new object[] { pCaptain.current_tile, k, -1 }) as EnemyFinderData;
            if (data == null || data.isEmpty())
            {
                return;
            }
            int enemies = 0;
            for (int i = 0; i < data.list.Count; i++)
            {
                if (data.list[i] is Actor a && a.isWarrior() && a.isAlive())
                {
                    enemies++;
                }
            }
            if (enemies >= pArmy.countUnits() * Cfg.RetreatRatio)
            {
                WarFx.RetreatUntil[pArmy.id] = UnityEngine.Time.time + Cfg.RetreatSeconds;
                First.Fire("army retreating");
                UnityEngine.Debug.Log("[CivMind] army retreating: " + pArmy.name + " (outmatched " + enemies + " vs " + pArmy.countUnits() + ")");
            }
        }

        private static void TickExhaustion(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.ArmyExhaustion)
            {
                return;
            }
            Kingdom k = pArmy.getKingdom();
            if (k == null || pCaptain.current_zone == null)
            {
                return;
            }
            City zoneCity = pCaptain.current_zone.city;
            if (zoneCity != null && Rebel.Of(zoneCity) == k)
            {
                WarFx.AwaySince.Remove(pArmy.id);
            }
            else if (!WarFx.AwaySince.ContainsKey(pArmy.id))
            {
                WarFx.AwaySince[pArmy.id] = UnityEngine.Time.time;
            }
            else if (WarFx.IsExhausted(pArmy.id))
            {
                First.Fire("exhausted army");
            }
        }

        private static void TickFocusFire(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.FocusFire || _attackTarget == null || _hasAttackTarget == null)
            {
                return;
            }
            if (!(bool)_hasAttackTarget(pCaptain))
            {
                return;
            }
            if (!(_attackTarget(pCaptain) is Actor target) || !target.isAlive())
            {
                return;
            }
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u == pCaptain || u.isRekt() || !u.isArmyGroupWarrior() || u.isInsideSomething())
                {
                    continue;
                }
                if ((bool)_hasAttackTarget(u) && _attackTarget(u) == target)
                {
                    continue;
                }
                if (Randy.randomChance(Cfg.FocusFireChance))
                {
                    u.setAttackTarget(target);
                    First.Fire("focus fire");
                }
            }
        }

        private static void TickDuels(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.Duels || pCaptain.kingdom == null || pCaptain.isInsideSomething() || !Randy.randomChance(0.1f))
            {
                return;
            }
            float now = UnityEngine.Time.time;
            float radius2 = Cfg.DuelRadius * Cfg.DuelRadius;
            foreach (Army other in World.world.armies)
            {
                if (other == pArmy || !other.hasCaptain())
                {
                    continue;
                }
                Actor ocap = other.getCaptain();
                if (ocap == null || ocap.isRekt() || ocap.isInsideSomething() || ocap.kingdom == null)
                {
                    continue;
                }
                if (!ocap.kingdom.isEnemy(pCaptain.kingdom))
                {
                    continue;
                }
                if ((float)Toolbox.SquaredDistTile(pCaptain.current_tile, ocap.current_tile) > radius2)
                {
                    continue;
                }
                long lo = Math.Min(pArmy.id, other.id);
                long hi = Math.Max(pArmy.id, other.id);
                long key = (lo << 32) | (hi & 4294967295L);
                float exp;
                if (_duelPairs.TryGetValue(key, out exp) && exp > now)
                {
                    continue;
                }
                _duelPairs[key] = now + 60f;
                if (Randy.randomChance(Cfg.DuelChance))
                {
                    pCaptain.setAttackTarget(ocap);
                    ocap.setAttackTarget(pCaptain);
                    First.Fire("duel");
                    UnityEngine.Debug.Log("[CivMind] duel: " + pCaptain.getName() + " vs " + ocap.getName());
                }
            }
        }

        private static void TickPillage(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.Pillage || _actorData == null || _cityKingdom == null)
            {
                return;
            }
            TileZone zone = pCaptain.current_zone;
            City zoneCity = ((zone == null) ? null : zone.city);
            Kingdom zoneKingdom = ((zoneCity == null) ? null : (_cityKingdom(zoneCity)));
            bool enemyCity = zoneKingdom != null && pCaptain.kingdom != null && zoneKingdom.isEnemy(pCaptain.kingdom);
            bool homeZone = pCaptain.inOwnCityBorders();
            if (!enemyCity && !homeZone)
            {
                return;
            }
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u.isRekt())
                {
                    continue;
                }
                ActorData d = _actorData(u);
                if (d == null)
                {
                    continue;
                }
                if (homeZone && d.loot > 0 && u.city != null && u.inOwnCityBorders())
                {
                    int loot = d.loot;
                    Kingdom kr = u.kingdom;
                    if (Cfg.LootTax && Cfg.LootTaxPct > 0f && kr != null && kr.hasKing())
                    {
                        int skim = (int)(loot * Cfg.LootTaxPct / 100f);
                        if (skim > 0)
                        {
                            kr.king.addMoney(skim);
                            loot -= skim;
                            First.Fire("loot tax");
                        }
                    }
                    u.city.addResourcesToRandomStockpile("gold", loot);
                    d.loot = 0;
                }
                else if (enemyCity && u.isAlive() && Randy.randomChance(Cfg.PillageChance))
                {
                    int amt = Math.Min(zoneCity.getResourcesAmount("gold"), Randy.randomInt(1, Cfg.PillageAmount + 1));
                    if (amt > 0)
                    {
                        zoneCity.takeResource("gold", amt);
                        d.loot += amt;
                        First.Fire("pillage");
                    }
                }
            }
        }

        private static void TickDesertion(Army pArmy, Actor pCaptain)
        {
            if (Cfg.DesertionWarYears <= 0f || pCaptain.inOwnCityBorders())
            {
                return;
            }
            Kingdom k = pArmy.getKingdom();
            if (k == null)
            {
                return;
            }
            float chance = Cfg.DesertionChance;
            if (Cfg.Wages)
            {
                float broke;
                if (WarFx.Broke.TryGetValue(k.id, out broke) && broke > UnityEngine.Time.time)
                {
                    chance *= Cfg.MutinyDesertionMult;
                }
            }
            if (Cfg.ArmyExhaustion && WarFx.IsExhausted(pArmy.id))
            {
                chance *= Cfg.ExhaustDesertionMult;
            }
            if (chance <= 0f || !Randy.randomChance(chance))
            {
                return;
            }
            foreach (War w in k.getWars())
            {
                if (w.getAge() < Cfg.DesertionWarYears || w.isTotalWar())
                {
                    continue;
                }
                if (pArmy.units.Count > 0)
                {
                    Actor pick = Randy.getRandom(pArmy.units);
                    if (pick != null && pick.isAlive() && pick != pCaptain)
                    {
                        pick.stopBeingWarrior();
                        First.Fire("desertion");
                        UnityEngine.Debug.Log("[CivMind] deserted: " + pick.getName() + " (" + pArmy.name + ")");
                    }
                }
                break;
            }
        }

        private static void TickRotation(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.WoundedRotation || _hasAttackTarget == null || !Randy.randomChance(Cfg.RotateChance))
            {
                return;
            }
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u == null || u.isRekt() || u == pCaptain)
                {
                    continue;
                }
                if (!(bool)_hasAttackTarget(u) && u.getHealthRatio() < Cfg.RotateHealth)
                {
                    u.stopBeingWarrior();
                    First.Fire("wounded rotation");
                    return;
                }
            }
        }

        private static void TickFormations(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.Formations || _behTileTarget == null || _hasAttackTarget == null || _attackTarget == null)
            {
                return;
            }
            float range2 = Cfg.KiteRange * Cfg.KiteRange;
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u == null || u.isRekt() || u.current_tile == null || u.equipment == null || u.isInsideSomething())
                {
                    continue;
                }
                ActorEquipmentSlot slot = u.equipment.getSlot(EquipmentType.Weapon);
                if (slot == null || slot.isEmpty() || slot.getItem() == null || slot.getItem().asset.attack_type != WeaponType.Range)
                {
                    continue;
                }
                if (!(bool)_hasAttackTarget(u))
                {
                    continue;
                }
                BaseSimObject tgt = _attackTarget(u) as BaseSimObject;
                if (tgt == null || tgt.current_tile == null)
                {
                    continue;
                }
                float d2 = Toolbox.SquaredDistTile(u.current_tile, tgt.current_tile);
                if (d2 > range2 || !Randy.randomChance(Cfg.KiteChance))
                {
                    continue;
                }
                WorldTile[] neighbours = u.current_tile.neighboursAll;
                WorldTile bestTile = null;
                float bestDist = d2;
                for (int j = 0; j < neighbours.Length; j++)
                {
                    WorldTile t = neighbours[j];
                    if (t == null || t.is_liquid || !t.isSameIsland(u.current_tile))
                    {
                        continue;
                    }
                    float nd = Toolbox.SquaredDistTile(t, tgt.current_tile);
                    if (nd > bestDist)
                    {
                        bestDist = nd;
                        bestTile = t;
                    }
                }
                if (bestTile != null)
                {
                    _behTileTarget(u) = bestTile;
                    First.Fire("ranged kiting");
                }
            }
        }

        private static void TickRelief(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.SiegeRelief || _behTileTarget == null || !Randy.randomChance(Cfg.ReliefChance))
            {
                return;
            }
            if (_hasAttackTarget != null && (bool)_hasAttackTarget(pCaptain))
            {
                return;
            }
            Kingdom k = pArmy.getKingdom();
            if (k == null || pCaptain.current_tile == null)
            {
                return;
            }
            City needy = null;
            foreach (City c in k.getCities())
            {
                if ((c.isGettingCaptured() || c.isInDanger()) && c.getTile() != null && c.getTile().isSameIsland(pCaptain.current_tile))
                {
                    needy = c;
                    break;
                }
            }
            if (needy == null)
            {
                return;
            }
            WorldTile target = needy.getTile();
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u != null && !u.isRekt())
                {
                    _behTileTarget(u) = target;
                }
            }
            First.Fire("relief march");
            UnityEngine.Debug.Log("[CivMind] relief march: " + pArmy.name + " → " + needy.name);
        }

        private static void TickVeterancy(Army pArmy, Actor pCaptain)
        {
            if (!Cfg.ArmyVeterancy || !Randy.randomChance(0.05f))
            {
                return;
            }
            float now = UnityEngine.Time.time;
            bool wasVet = WarFx.IsVeteran(pArmy.id);
            int kills = 0;
            int n = 0;
            for (int i = 0; i < pArmy.units.Count; i++)
            {
                Actor u = pArmy.units[i];
                if (u == null || u.isRekt())
                {
                    continue;
                }
                ActorData d = _actorData(u);
                if (d != null)
                {
                    kills += d.kills;
                    n++;
                }
            }
            if (n > 0 && (float)kills / n >= Cfg.VetArmyKills)
            {
                WarFx.VetArmies[pArmy.id] = now + 300f;
                if (!wasVet)
                {
                    First.Fire("army veterancy");
                    UnityEngine.Debug.Log("[CivMind] veteran army: " + pArmy.name + " (" + kills + " kills)");
                }
            }
            else if (wasVet)
            {
                WarFx.VetArmies.Remove(pArmy.id);
                UnityEngine.Debug.Log("[CivMind] veteran army disbanded (roster churn): " + pArmy.name);
            }
        }
    }

    [HarmonyPatch(typeof(Actor), "getHit")]
    internal static class PatchGetHit
    {
        private static readonly FieldInfo _stats = Ref.Field(typeof(BaseSimObject), "stats");
        private static readonly MethodInfo _addStatus = Ref.Method(typeof(BaseSimObject), "addStatusEffect", new Type[]
        {
            typeof(string),
            typeof(float),
            typeof(bool)
        });

        private static bool Prefix(Actor __instance, ref float pDamage, bool pFlash, AttackType pAttackType, BaseSimObject pAttacker, bool pSkipIfShake, bool pMetallicWeapon, ref bool pCheckDamageReduction, out bool __state)
        {
            __state = false;
            if (Cfg.HomeField)
            {
                try
                {
                    if (pAttacker != null && __instance.inOwnCityBorders())
                    {
                        pDamage *= Cfg.HomeFieldMult;
                        First.Fire("home field");
                    }
                }
                catch
                {
                }
            }
            try
            {
                if (pAttacker != null)
                {
                    if (Cfg.CombatCrits && _stats != null)
                    {
                        BaseStats dStats = _stats.GetValue(__instance) as BaseStats;
                        float dodge = Cfg.DodgeChance + (dStats == null ? 0f : dStats["accuracy"]) * Cfg.CritDodgeStatScale;
                        if (dodge > 0f && Randy.randomChance(dodge))
                        {
                            pDamage = 0f;
                            First.Fire("dodge");
                            return true;
                        }
                        Actor atk = pAttacker as Actor;
                        if (atk != null)
                        {
                            BaseStats aStats = _stats.GetValue(atk) as BaseStats;
                            float crit = Cfg.CritChance + (aStats == null ? 0f : aStats["accuracy"]) * Cfg.CritDodgeStatScale;
                            if (crit > 0f && Randy.randomChance(crit))
                            {
                                pDamage *= Cfg.CritMult;
                                First.Fire("crit");
                            }
                        }
                    }
                    if (Cfg.ArmorPen && pAttacker is Actor pen && pen.warfare >= Cfg.PenWarfare && Randy.randomChance(Cfg.PenChance))
                    {
                        pCheckDamageReduction = false;
                        First.Fire("armor penetration");
                    }
                    if (Cfg.Stagger && pDamage > 0f)
                    {
                        int maxHp = __instance.getMaxHealth();
                        if (maxHp > 0 && pDamage >= maxHp * Cfg.StaggerDmgPct && Randy.randomChance(Cfg.StaggerChance))
                        {
                            __state = true;
                        }
                    }
                }
            }
            catch
            {
            }
            return true;
        }

        private static void Postfix(Actor __instance, bool __state)
        {
            if (!__state || _addStatus == null || __instance.isRekt())
            {
                return;
            }
            try
            {
                _addStatus.Invoke(__instance, new object[] { "stunned", Cfg.StaggerSeconds, true });
                First.Fire("stagger");
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BaseSimObject), "findEnemyObjectTarget")]
    internal static class PatchFindTarget
    {
        private static readonly MethodInfo _findEnemies = Ref.Method(typeof(EnemiesFinder), "findEnemiesFrom", new Type[]
        {
            typeof(WorldTile),
            typeof(Kingdom),
            typeof(int)
        });
        private static readonly MethodInfo _canAttack = Ref.Method(typeof(BaseSimObject), "canAttackTarget", new Type[]
        {
            typeof(BaseSimObject),
            typeof(bool),
            typeof(bool)
        });

        private static void Postfix(BaseSimObject __instance, bool pAttackBuildings, ref BaseSimObject __result)
        {
            if (!Cfg.FinishWounded || _findEnemies == null || _canAttack == null || __instance == null || __instance.kingdom == null)
            {
                return;
            }
            Actor current = __result as Actor;
            if (current == null || current.getHealthRatio() <= Cfg.FinishHealth)
            {
                return;
            }
            try
            {
                EnemyFinderData data = _findEnemies.Invoke(null, new object[] { __instance.current_tile, __instance.kingdom, -1 }) as EnemyFinderData;
                if (data == null || data.isEmpty())
                {
                    return;
                }
                float range2 = Cfg.FinishRadius * Cfg.FinishRadius;
                Actor best = null;
                float bestDist = float.MaxValue;
                for (int i = 0; i < data.list.Count; i++)
                {
                    Actor a = data.list[i] as Actor;
                    if (a == null || a.isRekt() || a.getHealthRatio() > Cfg.FinishHealth || a.current_tile == null)
                    {
                        continue;
                    }
                    object ok = _canAttack.Invoke(__instance, new object[] { a, true, pAttackBuildings });
                    if (!(ok is bool) || !(bool)ok)
                    {
                        continue;
                    }
                    float d = Toolbox.SquaredDistTile(__instance.current_tile, a.current_tile);
                    if (d <= range2 && d < bestDist)
                    {
                        bestDist = d;
                        best = a;
                    }
                }
                if (best != null)
                {
                    __result = best;
                    First.Fire("finish wounded");
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BehCityActorWarriorTaxiCheck), "execute")]
    internal static class PatchWarriorTaxi
    {
        private static void Postfix(Actor pActor, BehResult __result)
        {
            if (!Cfg.NavalInvasions || __result != BehResult.Stop)
            {
                return;
            }
            try
            {
                Army army = pActor.army;
                if (army == null || !pActor.isArmyGroupWarrior() || !army.hasCaptain())
                {
                    return;
                }
                Actor cap = army.getCaptain();
                if (cap == null || cap.isRekt() || !cap.isInsideSomething() || cap.current_tile == null || cap.current_tile.isSameIsland(pActor.current_tile))
                {
                    return;
                }
                TaxiManager.newRequest(pActor, cap.current_tile);
                First.Fire("naval invasion");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] warrior taxi failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(Actor), "increaseKills")]
    internal static class PatchIncreaseKills
    {
        private class Streak
        {
            public int Count;
            public float Last;
        }

        private static readonly Dictionary<long, Streak> _streaks = new Dictionary<long, Streak>();
        private static readonly MethodInfo _addStatus = Ref.Method(typeof(BaseSimObject), "addStatusEffect", new Type[]
        {
            typeof(string),
            typeof(float),
            typeof(bool)
        });
        private static float _nextPrune;

        private static void Postfix(Actor __instance)
        {
            if (!Cfg.BerserkerRage || _addStatus == null)
            {
                return;
            }
            try
            {
                float now = UnityEngine.Time.time;
                long id = __instance.id;
                if (__instance.kingdom != null && World.world.wars.hasWars(__instance.kingdom))
                {
                    int hc;
                    WarFx.HeroKills.TryGetValue(id, out hc);
                    WarFx.HeroKills[id] = hc + 1;
                }
                Streak s;
                if (!_streaks.TryGetValue(id, out s))
                {
                    s = new Streak();
                    _streaks[id] = s;
                }
                s.Count = ((now - s.Last < Cfg.RageWindowSec) ? (s.Count + 1) : 1);
                s.Last = now;
                if (s.Count >= Cfg.RageKills)
                {
                    s.Count = 0;
                    _addStatus.Invoke(__instance, new object[] { "rage", Cfg.RageDurationSec, true });
                    First.Fire("berserker rage");
                    UnityEngine.Debug.Log("[CivMind] rage: " + __instance.getName());
                }
                if (now >= _nextPrune)
                {
                    _nextPrune = now + 60f;
                    List<long> dead = null;
                    foreach (KeyValuePair<long, Streak> kv in _streaks)
                    {
                        if (now - kv.Value.Last > Cfg.RageWindowSec)
                        {
                            if (dead == null)
                            {
                                dead = new List<long>();
                            }
                            dead.Add(kv.Key);
                        }
                    }
                    if (dead != null)
                    {
                        for (int i = 0; i < dead.Count; i++)
                        {
                            _streaks.Remove(dead[i]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] rage patch failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(WarManager), "endWar")]
    internal static class PatchEndWar
    {
        private static readonly FieldInfo _cityKingdom = Ref.Field(typeof(City), "kingdom");

        private static void Postfix(War pWar, WarWinner pWinner)
        {
            try
            {
                if (pWar == null)
                {
                    return;
                }
                Kingdom atk = pWar.getMainAttacker();
                Kingdom def = pWar.getMainDefender();
                double now = World.world.getCurWorldTime();
                if (atk != null)
                {
                    WarFx.PeaceTime[atk.id] = now;
                }
                if (def != null)
                {
                    WarFx.PeaceTime[def.id] = now;
                }
                if (Cfg.WarHeroes)
                {
                    GrantHero(atk);
                    GrantHero(def);
                }
                Kingdom winner = null;
                Kingdom loser = null;
                if (pWinner == WarWinner.Attackers)
                {
                    winner = atk;
                    loser = def;
                }
                else if (pWinner == WarWinner.Defenders)
                {
                    winner = def;
                    loser = atk;
                }
                if (winner != null && loser != null && winner.isAlive() && loser.isAlive())
                {
                    if (Cfg.TributeInstallments)
                    {
                        int yr = Date.getCurrentYear();
                        WarFx.Tribute[loser.id] = new TributeDeal
                        {
                            Winner = winner.id,
                            Expiry = yr + (int)Cfg.TributeYears,
                            LastPaid = yr
                        };
                        First.Fire("tribute deal");
                        UnityEngine.Debug.Log("[CivMind] tribute deal: " + loser.name + " pays " + winner.name + " for " + (int)Cfg.TributeYears + "y");
                    }
                    if (Cfg.Reparations && _cityKingdom != null && Cfg.ReparationsPct > 0f && winner.hasCapital())
                    {
                        int total = 0;
                        foreach (City city in loser.getCities())
                        {
                            int gold = city.getResourcesAmount("gold");
                            int take = (int)(gold * Cfg.ReparationsPct / 100f);
                            if (take > 0)
                            {
                                city.takeResource("gold", take);
                                total += take;
                            }
                        }
                        if (total > 0)
                        {
                            winner.capital.addResourcesToRandomStockpile("gold", total);
                            First.Fire("reparations");
                            UnityEngine.Debug.Log("[CivMind] reparations: " + total + " gold from " + loser.name + " to " + winner.name);
                        }
                    }
                    if (Cfg.VictoryFireworks && winner.isAlive())
                    {
                        int fired = 0;
                        foreach (City c in winner.getCities())
                        {
                            int n = 0;
                            for (int i = 0; i < c.units.Count && n < Cfg.FestiveMaxPerCity; i++)
                            {
                                Actor u = c.units[i];
                                if (u == null || u.isRekt() || u.isBaby() || !Randy.randomChance(Cfg.FestiveChance))
                                {
                                    continue;
                                }
                                Rebel.AddStatus(u, "festive_spirit", Cfg.FestiveSeconds);
                                n++;
                            }
                            fired += n;
                        }
                        if (fired > 0)
                        {
                            First.Fire("victory fireworks");
                            UnityEngine.Debug.Log("[CivMind] festive: " + winner.name + " celebrates victory (" + fired + " citizens)");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] end war patch failed: " + e.Message);
            }
        }

        private static void GrantHero(Kingdom pKingdom)
        {
            if (pKingdom == null || WarFx.HeroKills.Count == 0)
            {
                return;
            }
            Actor best = null;
            int bestKills = -1;
            foreach (KeyValuePair<long, int> kv in WarFx.HeroKills)
            {
                if (kv.Value <= bestKills)
                {
                    continue;
                }
                Actor a = World.world.units.get(kv.Key);
                if (a == null || a.isRekt() || a.kingdom != pKingdom)
                {
                    continue;
                }
                best = a;
                bestKills = kv.Value;
            }
            if (best != null && bestKills >= Cfg.HeroMinKills)
            {
                best.addTrait("veteran");
                First.Fire("war hero");
                UnityEngine.Debug.Log("[CivMind] war hero: " + best.getName() + " (" + bestKills + " kills)");
            }
            List<long> done = null;
            foreach (KeyValuePair<long, int> kv in WarFx.HeroKills)
            {
                Actor a = World.world.units.get(kv.Key);
                if (a == null || a.isRekt() || a.kingdom == pKingdom)
                {
                    if (done == null)
                    {
                        done = new List<long>();
                    }
                    done.Add(kv.Key);
                }
            }
            if (done != null)
            {
                for (int i = 0; i < done.Count; i++)
                {
                    WarFx.HeroKills.Remove(done[i]);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SuccessionTool), "getKingFromRoyalClan")]
    internal static class PatchSuccessionClan
    {
        private static readonly FieldInfo _stats = Ref.Field(typeof(BaseSimObject), "stats");

        private static void Postfix(ref Actor __result, Kingdom pKingdom, Actor pExcludeActor)
        {
            if (!Cfg.MeritSuccession || __result == null || _stats == null)
            {
                return;
            }
            try
            {
                Clan clan = World.world.clans.get(pKingdom.data.royal_clan_id);
                if (clan == null)
                {
                    return;
                }
                Actor vanilla = __result;
                float bestScore = Score(__result);
                for (int i = 0; i < clan.units.Count; i++)
                {
                    Actor u = clan.units[i];
                    if (u == null || u == pExcludeActor || u == __result || !clan.fitToRule(u, pKingdom))
                    {
                        continue;
                    }
                    float s = Score(u);
                    if (s > bestScore)
                    {
                        bestScore = s;
                        __result = u;
                    }
                }
                if (__result != vanilla)
                {
                    First.Fire("merit succession");
                }
            }
            catch
            {
            }
        }

        internal static float Score(Actor a)
        {
            BaseStats s = _stats.GetValue(a) as BaseStats;
            if (s == null)
            {
                return 0f;
            }
            return s["diplomacy"] + s["stewardship"] + s["warfare"] * 0.5f + a.level;
        }

        internal static float ScoreLeader(Actor a)
        {
            if (_stats == null)
            {
                return 0f;
            }
            BaseStats s = _stats.GetValue(a) as BaseStats;
            if (s == null)
            {
                return 0f;
            }
            return s["stewardship"] + a.level * 0.5f;
        }
    }

    [HarmonyPatch(typeof(SuccessionTool), "getKingFromLeaders")]
    internal static class PatchSuccessionLeaders
    {
        private static void Postfix(ref Actor __result, Kingdom pKingdom)
        {
            if (!Cfg.MeritSuccession || __result == null)
            {
                return;
            }
            try
            {
                Actor vanilla = __result;
                float bestScore = PatchSuccessionClan.Score(__result);
                foreach (City city in pKingdom.getCities())
                {
                    if (!city.hasLeader())
                    {
                        continue;
                    }
                    Actor u = city.leader;
                    if (u == null || u == __result || u.isRekt())
                    {
                        continue;
                    }
                    float s = PatchSuccessionClan.Score(u);
                    if (s > bestScore)
                    {
                        bestScore = s;
                        __result = u;
                    }
                }
                if (__result != vanilla)
                {
                    First.Fire("merit succession");
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(DiplomacyHelpers), "getAllianceTarget")]
    internal static class PatchAllianceTarget
    {
        private static bool Prefix(ref Kingdom __result, Kingdom pKingdomStarter)
        {
            if (!Cfg.BalanceAlliances)
            {
                return true;
            }
            try
            {
                __result = null;
                if (pKingdomStarter.isSupreme())
                {
                    return false;
                }
                Kingdom supreme = null;
                if (Cfg.AntiHegemon)
                {
                    foreach (Kingdom sk in World.world.kingdoms)
                    {
                        if (sk.isSupreme() && sk.isAlive())
                        {
                            supreme = sk;
                            break;
                        }
                    }
                }
                Kingdom best = null;
                float bestDist = float.MaxValue;
                ListPool<Kingdom> pool = World.world.wars.getNeutralKingdoms(pKingdomStarter, pOnlyWithoutWars: true, pOnlyWithoutAlliances: true);
                try
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        Kingdom k = pool[i];
                        if (!k.hasKing() || k.isSupreme() || k.king.hasPlot() || !pKingdomStarter.isOpinionTowardsKingdomGood(k) || k.getRenown() < PlotsLibrary.alliance_create.min_renown_kingdom)
                        {
                            continue;
                        }
                        bool ok = DiplomacyHelpers.areKingdomsClose(k, pKingdomStarter);
                        if (!ok && pKingdomStarter.countCities() <= 2 && k.countCities() <= 2 && !pKingdomStarter.hasNearbyKingdoms() && !k.hasNearbyKingdoms())
                        {
                            ok = true;
                        }
                        if (!ok)
                        {
                            continue;
                        }
                        float dist = Kingdom.distanceBetweenKingdom(pKingdomStarter, k);
                        if (supreme != null && k != supreme && DiplomacyHelpers.areKingdomsClose(k, supreme))
                        {
                            dist *= Cfg.HegemonBonusMult;
                            First.Fire("anti-hegemon");
                        }
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = k;
                        }
                    }
                }
                finally
                {
                    pool.Dispose();
                }
                __result = best;
                First.Fire("regional alliance");
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] alliance target patch failed: " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(BehFindRandomFarTile), "execute")]
    internal static class PatchFlee
    {
        private static readonly AccessTools.FieldRef<Actor, WorldTile> _behTileTarget = TryRef();
        private static readonly FieldInfo _cityZones = Ref.Field(typeof(City), "zones");

        private static AccessTools.FieldRef<Actor, WorldTile> TryRef()
        {
            try
            {
                return Ref.FieldRef<Actor, WorldTile>("beh_tile_target");
            }
            catch
            {
                return null;
            }
        }

        private static bool Prefix(Actor pActor, ref BehResult __result)
        {
            if (!Cfg.RallyFlee || _behTileTarget == null)
            {
                return true;
            }
            try
            {
                Army army = pActor.army;
                if (army == null)
                {
                    return true;
                }
                WorldTile target = null;
                if (army.hasCaptain())
                {
                    Actor cap = army.getCaptain();
                    if (cap != null && !cap.isRekt() && cap != pActor && cap.current_tile != null)
                    {
                        target = cap.current_tile;
                    }
                }
                if (target == null && pActor.city != null && !pActor.city.isInDanger() && _cityZones != null)
                {
                    List<TileZone> z = _cityZones.GetValue(pActor.city) as List<TileZone>;
                    if (z != null && z.Count > 0)
                    {
                        target = z[Randy.randomInt(0, z.Count)].centerTile;
                    }
                }
                if (target == null)
                {
                    return true;
                }
                _behTileTarget(pActor) = target;
                __result = BehResult.Continue;
                First.Fire("rally flee");
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(City), "tryToMakeWarrior")]
    internal static class PatchTryWarrior
    {
        private static bool Prefix(City __instance, Actor pActor, ref bool __result)
        {
            if (!Cfg.MilitiaReserve)
            {
                return true;
            }
            try
            {
                if (!__instance.isInDanger() && __instance.status.warriors_current >= (int)(__instance.status.warrior_slots * Cfg.ReservePeacetimeRatio))
                {
                    __result = false;
                    First.Fire("militia reserve");
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(City), "setLeader")]
    internal static class PatchSetLeader
    {
        private static void Prefix(City __instance, ref Actor pActor, bool pNew)
        {
            if (!Cfg.MeritLeaders || !pNew || pActor == null)
            {
                return;
            }
            try
            {
                float bestScore = PatchSuccessionClan.ScoreLeader(pActor);
                Actor best = pActor;
                for (int i = 0; i < __instance.units.Count; i++)
                {
                    Actor u = __instance.units[i];
                    if (u == null || u == pActor || u.isRekt() || !u.is_profession_citizen || u.isKing() || u.isCityLeader())
                    {
                        continue;
                    }
                    float s = PatchSuccessionClan.ScoreLeader(u);
                    if (s > bestScore)
                    {
                        bestScore = s;
                        best = u;
                    }
                }
                if (bestScore > PatchSuccessionClan.ScoreLeader(pActor) + Cfg.LeaderMargin)
                {
                    pActor = best;
                    First.Fire("merit leader");
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(CityBehCheckLoyalty), "execute")]
    internal static class PatchCheckLoyalty
    {
        private static void Postfix(City pCity)
        {
            try
            {
                if (pCity == null)
                {
                    return;
                }
                Kingdom k = Rebel.Of(pCity);
                if (Cfg.SiegeStarvation && pCity.isInDanger() && pCity.hasStockpiles() && Randy.randomChance(Cfg.SiegeDrainChance))
                {
                    Building stockpile = pCity.getRandomStockpile();
                    ResourceAsset food = stockpile == null ? null : stockpile.resources.getRandomFoodAsset();
                    if (food != null)
                    {
                        pCity.takeResource(food.id, (int)Cfg.SiegeFoodDrain);
                        First.Fire("siege starvation");
                    }
                }
                if (k == null)
                {
                    return;
                }
                if (Cfg.TributeInstallments && pCity.isCapitalCity() && WarFx.Tribute.Count > 0)
                {
                    TributeDeal deal;
                    if (WarFx.Tribute.TryGetValue(k.id, out deal))
                    {
                        int yr = Date.getCurrentYear();
                        if (yr >= deal.Expiry)
                        {
                            WarFx.Tribute.Remove(k.id);
                        }
                        else if (yr > deal.LastPaid)
                        {
                            Kingdom winner = World.world.kingdoms.get(deal.Winner);
                            if (winner == null || !winner.isAlive() || !winner.hasCapital())
                            {
                                WarFx.Tribute.Remove(k.id);
                            }
                            else
                            {
                                int pay = Math.Min((int)Cfg.TributeAnnual, pCity.getResourcesAmount("gold"));
                                if (pay > 0)
                                {
                                    pCity.takeResource("gold", pay);
                                    winner.capital.addResourcesToRandomStockpile("gold", pay);
                                    First.Fire("tribute payment");
                                    UnityEngine.Debug.Log("[CivMind] tribute: " + k.name + " paid " + pay + " gold to " + winner.name);
                                }
                                deal.LastPaid = yr;
                                WarFx.Tribute[k.id] = deal;
                            }
                        }
                    }
                }
                if (Cfg.Wages && k.isAlive() && !k.isNeutral() && pCity.status.warriors_current > 0 && Randy.randomChance(Cfg.WageChance))
                {
                    int cost = Math.Max(1, (int)Math.Ceiling(pCity.status.warriors_current * Cfg.WageRate));
                    if (pCity.getResourcesAmount("gold") >= cost)
                    {
                        pCity.takeResource("gold", cost);
                        First.Fire("soldier wages");
                    }
                    else
                    {
                        WarFx.Broke[k.id] = UnityEngine.Time.time + 120f;
                        First.Fire("unpaid mutiny");
                    }
                }
                double occupiedSince = pCity.data.timestamp_kingdom;
                bool occupied = pCity.data.timestamp_kingdom > pCity.data.created_time + 0.1 && Date.getYearsSince(occupiedSince) < Cfg.UprisingRecentYears;
                if (Cfg.Assimilation && occupied && Randy.randomChance(Cfg.AssimilationChance))
                {
                    if (k.hasCulture() && pCity.culture != k.culture && Rebel.SetCulture != null)
                    {
                        Rebel.SetCulture.Invoke(pCity, new object[] { k.culture });
                        First.Fire("assimilation");
                    }
                    if (k.hasReligion() && pCity.religion != k.religion && Rebel.SetReligion != null)
                    {
                        Rebel.SetReligion.Invoke(pCity, new object[] { k.religion });
                        First.Fire("assimilation");
                    }
                }
                if (!Cfg.Uprisings || Rebel.MakeOwnKingdom == null || Rebel.StartWar == null)
                {
                    return;
                }
                if (Rebel.Revolted.Contains(pCity.id) || pCity.isCapitalCity())
                {
                    return;
                }
                if (!WorldLawLibrary.world_law_rebellions.isEnabled())
                {
                    return;
                }
                if (!occupied || !k.isAlive() || k.isNeutral())
                {
                    return;
                }
                float loyaltyGate = Cfg.UprisingLoyalty;
                if (k.hasCulture() && pCity.culture != null && pCity.culture != k.culture)
                {
                    loyaltyGate += Cfg.CultureUprisingBonus;
                    First.Fire("culture unrest");
                }
                if (pCity.getLoyalty() >= loyaltyGate)
                {
                    return;
                }
                float chance = Cfg.UprisingChance;
                if (World.world.wars.hasWars(k) && Date.getYearsSince(k.data.timestamp_last_war) >= Cfg.WarExhaustYears)
                {
                    chance *= Cfg.WarExhaustMult;
                    First.Fire("war exhaustion");
                }
                if (!Randy.randomChance(chance))
                {
                    return;
                }
                Actor rebel = pCity.leader;
                if (rebel == null || rebel.isRekt())
                {
                    rebel = null;
                    for (int i = 0; i < pCity.units.Count; i++)
                    {
                        Actor u = pCity.units[i];
                        if (u != null && !u.isRekt() && u.isUnitFitToRule() && !u.isKing() && !u.isCityLeader())
                        {
                            rebel = u;
                            break;
                        }
                    }
                }
                if (rebel != null)
                {
                    Rebel.SparkRevolt(pCity, k, rebel);
                    First.Fire("city uprising");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] uprising patch failed: " + e.Message);
            }
        }
    }

    internal static class Rebel
    {
        internal static readonly MethodInfo MakeOwnKingdom = Ref.Method(typeof(City), "makeOwnKingdom", new Type[]
        {
            typeof(Actor),
            typeof(bool),
            typeof(bool)
        });
        internal static readonly MethodInfo StartWar = Ref.Method(typeof(DiplomacyManager), "startWar", new Type[]
        {
            typeof(Kingdom),
            typeof(Kingdom),
            typeof(WarTypeAsset),
            typeof(bool)
        });
        internal static readonly FieldInfo CityKingdom = Ref.Field(typeof(City), "kingdom");
        internal static readonly FieldInfo ActorDataF = Ref.Field(typeof(Actor), "data");
        internal static readonly AccessTools.FieldRef<Actor, ActorData> ActorDataRef = TryDataRef();

        private static AccessTools.FieldRef<Actor, ActorData> TryDataRef()
        {
            try
            {
                return Ref.FieldRef<Actor, ActorData>("data");
            }
            catch
            {
                return null;
            }
        }

        internal static ActorData Data(Actor a)
        {
            if (a == null)
            {
                return null;
            }
            if (ActorDataRef != null)
            {
                return ActorDataRef(a);
            }
            return ActorDataF == null ? null : ActorDataF.GetValue(a) as ActorData;
        }
        internal static readonly MethodInfo SetCulture = Ref.Method(typeof(City), "setCulture", new Type[] { typeof(Culture) });
        internal static readonly MethodInfo SetReligion = Ref.Method(typeof(City), "setReligion", new Type[] { typeof(Religion) });
        internal static readonly MethodInfo AddStatusM = Ref.Method(typeof(BaseSimObject), "addStatusEffect", new Type[]
        {
            typeof(string),
            typeof(float),
            typeof(bool)
        });
        internal static readonly HashSet<long> Revolted = new HashSet<long>();

        internal static void AddStatus(BaseSimObject pObj, string pId, float pSeconds)
        {
            if (AddStatusM != null && pObj != null)
            {
                AddStatusM.Invoke(pObj, new object[] { pId, pSeconds, true });
            }
        }

        internal static Kingdom Of(City pCity)
        {
            return pCity == null || CityKingdom == null ? null : CityKingdom.GetValue(pCity) as Kingdom;
        }

        internal static double PeaceYears(Kingdom k)
        {
            if (k == null || !k.isAlive() || World.world.wars.hasWars(k))
            {
                return 0.0;
            }
            double last = k.data.timestamp_last_war;
            return last >= 0.0 ? Date.getYearsSince(last) : k.getAge();
        }

        internal static Kingdom SparkRevolt(City pCity, Kingdom pOldK, Actor pRebel)
        {
            if (MakeOwnKingdom == null || StartWar == null || pRebel == null || pRebel.isRekt())
            {
                return null;
            }
            Revolted.Add(pCity.id);
            if (pRebel.isCityLeader())
            {
                pCity.removeLeader();
            }
            Kingdom newK = MakeOwnKingdom.Invoke(pCity, new object[] { pRebel, true, false }) as Kingdom;
            if (newK != null)
            {
                StartWar.Invoke(World.world.diplomacy, new object[] { pOldK, newK, WarTypeLibrary.rebellion, true });
                WorldLog.logCityRevolt(pCity);
                UnityEngine.Debug.Log("[CivMind] revolt: " + pCity.name + " breaks from " + pOldK.name + " under " + pRebel.getName());
                ListPool<City> pool = new ListPool<City>();
                try
                {
                    pool.Add(pCity);
                    DiplomacyHelpersRebellion.checkMoreAlignedCities(newK, pOldK, pool, true);
                }
                finally
                {
                    pool.Dispose();
                }
            }
            return newK;
        }

        internal static void FellApart(City pCity, Actor pLeader)
        {
            if (MakeOwnKingdom == null || pLeader == null || pLeader.isRekt())
            {
                return;
            }
            Revolted.Add(pCity.id);
            if (pLeader.isCityLeader())
            {
                pCity.removeLeader();
            }
            if (MakeOwnKingdom.Invoke(pCity, new object[] { pLeader, false, true }) is Kingdom)
            {
                UnityEngine.Debug.Log("[CivMind] province drift: " + pCity.name + " leaves the realm");
            }
        }
    }

    [HarmonyPatch(typeof(DiplomacyHelpers), "isWarNeeded")]
    internal static class PatchIsWarNeeded
    {
        private static void Postfix(Kingdom pKingdom, ref bool __result)
        {
            if (pKingdom == null)
            {
                return;
            }
            try
            {
                Actor king = pKingdom.hasKing() ? pKingdom.king : null;
                if (__result)
                {
                    if (Cfg.KingPersonality && king != null
                        && king.diplomacy >= Cfg.PeaceKingDiplomacy && king.warfare < king.diplomacy
                        && Randy.randomChance(Cfg.PeaceVetoChance))
                    {
                        __result = false;
                        First.Fire("pacifist veto");
                    }
                }
                else
                {
                    if (Cfg.KingPersonality && king != null
                        && king.warfare >= Cfg.WarlordWarfare
                        && Randy.randomChance(Cfg.WarlordChance)
                        && pKingdom.hasCities() && pKingdom.hasCapital()
                        && !World.world.wars.hasWars(pKingdom)
                        && pKingdom.countTotalWarriors() >= Cfg.WarlordMinWarriors
                        && (pKingdom.data.timestamp_last_war == -1.0 || Date.getYearsSince(pKingdom.data.timestamp_last_war) > (double)SimGlobals.m.diplomacy_years_war_timeout))
                    {
                        __result = true;
                        First.Fire("warlord bypass");
                    }
                    else if (Cfg.TwoFronts && pKingdom.isSupreme() && pKingdom.hasEnemies()
                        && World.world.wars.hasWars(pKingdom)
                        && Randy.randomChance(Cfg.TwoFrontChance)
                        && pKingdom.hasCities() && pKingdom.hasCapital()
                        && pKingdom.countTotalWarriors() >= Cfg.WarMinWarriors
                        && (pKingdom.data.timestamp_last_war == -1.0 || Date.getYearsSince(pKingdom.data.timestamp_last_war) > (double)SimGlobals.m.diplomacy_years_war_timeout))
                    {
                        __result = true;
                        First.Fire("two-front war");
                        UnityEngine.Debug.Log("[CivMind] two-front war: " + pKingdom.name + " opens a second front");
                    }
                }
                if (__result && Cfg.WarChest && king != null && pKingdom.hasCapital())
                {
                    ActorData kd = Rebel.Data(king);
                    if (kd != null && kd.money < Cfg.WarChestDrawMax)
                    {
                        int draw = (int)Math.Min(Cfg.WarChestDrawMax - kd.money, pKingdom.capital.getResourcesAmount("gold"));
                        if (draw > 0)
                        {
                            pKingdom.capital.takeResource("gold", draw);
                            king.addMoney(draw);
                            First.Fire("war chest");
                            UnityEngine.Debug.Log("[CivMind] war chest: " + draw + " gold to " + king.getName());
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(Kingdom), "setKing")]
    internal static class PatchSetKing
    {
        private static void Postfix(Kingdom __instance, Actor pActor, bool pFromLoad)
        {
            if (pFromLoad || pActor == null || __instance == null)
            {
                return;
            }
            try
            {
                if (!WorldLawLibrary.world_law_rebellions.isEnabled() || __instance.countCities() <= 1)
                {
                    return;
                }
                float kingScore = PatchSuccessionClan.Score(pActor);
                if (Cfg.PretenderWars && Randy.randomChance(Cfg.PretenderChance))
                {
                    Actor pretender = FindPretender(__instance, pActor, kingScore + Cfg.PretenderMargin);
                    if (pretender != null && pretender.city != null && !pretender.city.isCapitalCity() && Rebel.Of(pretender.city) == __instance)
                    {
                        Rebel.SparkRevolt(pretender.city, __instance, pretender);
                        First.Fire("pretender war");
                        return;
                    }
                }
                if (Cfg.CollapsePressure && kingScore < Cfg.CollapseKingScore && __instance.countCities() > 2 && Randy.randomChance(Cfg.CollapseChance))
                {
                    City worst = null;
                    float worstLoyalty = 40f;
                    foreach (City c in __instance.getCities())
                    {
                        if (c.isCapitalCity() || !c.hasLeader())
                        {
                            continue;
                        }
                        float loyalty = c.getLoyalty();
                        if (loyalty < worstLoyalty)
                        {
                            worstLoyalty = loyalty;
                            worst = c;
                        }
                    }
                    if (worst != null)
                    {
                        Rebel.FellApart(worst, worst.leader);
                        First.Fire("kingdom drift");
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] succession events failed: " + e.Message);
            }
        }

        private static Actor FindPretender(Kingdom pKingdom, Actor pKing, float pMinScore)
        {
            Actor best = null;
            float bestScore = pMinScore;
            foreach (City c in pKingdom.getCities())
            {
                if (c.isCapitalCity() || !c.hasLeader())
                {
                    continue;
                }
                Actor u = c.leader;
                if (u == null || u == pKing || u.isRekt())
                {
                    continue;
                }
                float s = PatchSuccessionClan.Score(u);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = u;
                }
            }
            if (pKingdom.data.royal_clan_id.hasValue())
            {
                Clan clan = World.world.clans.get(pKingdom.data.royal_clan_id);
                if (clan != null)
                {
                    for (int i = 0; i < clan.units.Count; i++)
                    {
                        Actor u = clan.units[i];
                        if (u == null || u == pKing || u.isRekt() || u.city == null || u.city.isCapitalCity())
                        {
                            continue;
                        }
                        float s = PatchSuccessionClan.Score(u);
                        if (s > bestScore)
                        {
                            bestScore = s;
                            best = u;
                        }
                    }
                }
            }
            return best;
        }
    }

    [HarmonyPatch(typeof(PlotsLibrary), "init")]
    internal static class PatchPlotsInit
    {
        private static void Postfix()
        {
            try
            {
                Tuning.WrapPlots();
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BehActorGiveTax), "execute")]
    internal static class PatchGiveTax
    {
        private static void Prefix(Actor pActor, out int __state)
        {
            __state = -1;
            try
            {
                if (pActor != null && pActor.isCityLeader() && pActor.kingdom != null && pActor.kingdom.hasKing() && pActor.kingdom.king != null)
                {
                    __state = pActor.kingdom.king.loot;
                }
            }
            catch
            {
            }
        }

        private static void Postfix(Actor pActor, int __state)
        {
            if ((!Cfg.TaxUnrest && !Cfg.Corruption) || pActor == null || pActor.isRekt())
            {
                return;
            }
            try
            {
                Kingdom k = pActor.kingdom;
                if (k == null || k.isNeutral())
                {
                    return;
                }
                if (Cfg.TaxUnrest && !pActor.isKing() && !pActor.isCityLeader())
                {
                    float rate = k.getTaxRateLocal();
                    if (rate > Cfg.TaxUnrestRate)
                    {
                        int dip = (int)Math.Round((rate - Cfg.TaxUnrestRate) * Cfg.TaxUnrestSeverity);
                        if (dip > 0)
                        {
                            pActor.changeHappiness("paid_tax", -dip);
                            First.Fire("tax unrest");
                        }
                    }
                }
                if (Cfg.Corruption && __state >= 0 && pActor.isCityLeader() && pActor.city != null && k.hasKing() && k.king != null)
                {
                    Actor king = k.king;
                    int gained = king.loot - __state;
                    if (gained > 0 && Randy.randomChance(Cfg.CorruptionChance))
                    {
                        float loyaltyFactor = Math.Max(0f, 1f - pActor.city.getLoyalty() / 100f);
                        int skim = (int)(gained * Cfg.CorruptionPct / 100f * loyaltyFactor);
                        if (skim > 0)
                        {
                            ActorData kd = Rebel.Data(king);
                            if (kd != null)
                            {
                                kd.loot = Math.Max(0, kd.loot - skim);
                                pActor.addMoney(skim);
                                First.Fire("corruption");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] give-tax patch failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(CityBehSupplyKingdomCities), "execute")]
    internal static class PatchSupplyCities
    {
        private static readonly FieldInfo _stats = Ref.Field(typeof(BaseSimObject), "stats");

        private static bool Prefix(City pCity, ref BehResult __result)
        {
            if (!Cfg.FamineRelief || pCity == null)
            {
                return true;
            }
            try
            {
                Kingdom k = Rebel.Of(pCity);
                if (k == null || k.countCities() <= 1 || pCity.data.timer_supply > 0f || !pCity.hasStockpiles() || !pCity.hasCulture())
                {
                    return true;
                }
                Building stockpile = pCity.getRandomStockpile();
                if (stockpile == null)
                {
                    return true;
                }
                ListPool<CityStorageSlot> surplus = new ListPool<CityStorageSlot>();
                try
                {
                    foreach (CityStorageSlot slot in stockpile.resources.getSlots())
                    {
                        if (slot.amount > slot.asset.supply_bound_give)
                        {
                            surplus.Add(slot);
                        }
                    }
                    if (surplus.Count == 0)
                    {
                        return true;
                    }
                    City best = null;
                    CityStorageSlot pick = null;
                    foreach (City c in k.getCities())
                    {
                        if (c == pCity)
                        {
                            continue;
                        }
                        CityStorageSlot need = null;
                        for (int i = 0; i < surplus.Count; i++)
                        {
                            if (c.getResourcesAmount(surplus[i].id) < surplus[i].asset.supply_bound_take)
                            {
                                need = surplus[i];
                                break;
                            }
                        }
                        if (need == null)
                        {
                            continue;
                        }
                        if (best == null || c.getTotalFood() < best.getTotalFood())
                        {
                            best = c;
                            pick = need;
                        }
                    }
                    if (best == null || pick == null)
                    {
                        return true;
                    }
                    pCity.takeResource(pick.id, pick.asset.supply_give);
                    best.addResourcesToRandomStockpile(pick.id, pick.asset.supply_give);
                    float timer = 30f;
                    if (pCity.hasLeader() && _stats != null)
                    {
                        BaseStats s = _stats.GetValue(pCity.leader) as BaseStats;
                        if (s != null)
                        {
                            timer *= s["multiplier_supply_timer"];
                        }
                    }
                    pCity.data.timer_supply = Math.Max(10f, timer);
                    __result = BehResult.Continue;
                    First.Fire("famine relief");
                    return false;
                }
                finally
                {
                    surplus.Dispose();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] famine relief failed: " + e.Message);
                return true;
            }
        }

        private static void Postfix(City pCity)
        {
            if ((!Cfg.WarTribute && Cfg.SupplyWarMult >= 1f) || pCity == null)
            {
                return;
            }
            try
            {
                Kingdom k = Rebel.Of(pCity);
                if (k == null || !World.world.wars.hasWars(k))
                {
                    return;
                }
                if (Cfg.WarTribute && k.hasCapital() && !pCity.isCapitalCity() && Randy.randomChance(Cfg.TributeChance))
                {
                    int gold = pCity.getResourcesAmount("gold");
                    int surplus = gold - (int)Cfg.TributeMinGold;
                    if (surplus > 0)
                    {
                        int send = (int)(surplus * Cfg.TributePct / 100f);
                        if (send > 0)
                        {
                            pCity.takeResource("gold", send);
                            k.capital.addResourcesToRandomStockpile("gold", send);
                            First.Fire("war tribute");
                        }
                    }
                }
                if (Cfg.SupplyWarMult < 1f && pCity.data.timer_supply > 0f)
                {
                    pCity.data.timer_supply *= Cfg.SupplyWarMult;
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(CityBehBorderSteal), "execute")]
    internal static class PatchBorderSteal
    {
        private static readonly MethodInfo _addZone = Ref.Method(typeof(City), "addZone", new Type[] { typeof(TileZone) });
        private static readonly MethodInfo _removeZone = Ref.Method(typeof(City), "removeZone", new Type[] { typeof(TileZone) });

        private static void Postfix(City pCity)
        {
            if (!Cfg.BorderPressure || _addZone == null || _removeZone == null || pCity == null)
            {
                return;
            }
            try
            {
                if (!Randy.randomChance(Cfg.BorderPressureChance) || !WorldLawLibrary.world_law_border_stealing.isEnabled()
                    || pCity.status.population == 0 || pCity.buildings.Count == 0)
                {
                    return;
                }
                Kingdom mine = Rebel.Of(pCity);
                if (mine == null || !World.world.wars.hasWars(mine))
                {
                    return;
                }
                TileZone[] neighbours = pCity.buildings.GetRandom().current_tile.zone.neighbours;
                for (int i = 0; i < neighbours.Length; i++)
                {
                    TileZone z = neighbours[i];
                    if (z.city == null || z.city == pCity || z.hasAnyBuildingsInSet(BuildingList.Civs))
                    {
                        continue;
                    }
                    Kingdom theirs = Rebel.Of(z.city);
                    if (theirs != null && theirs.isEnemy(mine))
                    {
                        _removeZone.Invoke(z.city, new object[] { z });
                        _addZone.Invoke(pCity, new object[] { z });
                        First.Fire("border pressure");
                        return;
                    }
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BehBoatMakeTrade), "execute")]
    internal static class PatchBoatTrade
    {
        private static readonly FieldInfo _behTarget = Ref.Field(typeof(Actor), "beh_building_target");
        private static readonly FieldInfo _cityCenter = Ref.Field(typeof(City), "city_center");

        private static void Postfix(Actor pActor)
        {
            if (!Cfg.TradeIncome || pActor == null || _behTarget == null || _cityCenter == null)
            {
                return;
            }
            try
            {
                City home = pActor.city;
                Building dock = _behTarget.GetValue(pActor) as Building;
                if (home == null || dock == null || dock.current_tile == null)
                {
                    return;
                }
                UnityEngine.Vector2 homeCenter = (UnityEngine.Vector2)_cityCenter.GetValue(home);
                int bonus = (int)Math.Round(Cfg.TradeIncomeBase + Toolbox.Dist(homeCenter.x, homeCenter.y, dock.current_tile.posV.x, dock.current_tile.posV.y) * Cfg.TradeIncomePerTile);
                if (bonus > 0)
                {
                    pActor.addToInventory("gold", bonus);
                    First.Fire("trade income");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] trade income failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(City), "giveItem")]
    internal static class PatchGiveItem
    {
        private static readonly FieldInfo _storageVersion = Ref.Field(typeof(City), "_storage_version");

        private static bool Prefix(Actor pActor, List<long> pItems, City pCity, ref bool __result, out float __state)
        {
            __state = EquippedValue(pActor);
            if (!Cfg.CombinedArms || pItems == null || pItems.Count < 2 || pActor == null || pCity == null)
            {
                return true;
            }
            try
            {
                Army army = pActor.army;
                Kingdom k = Rebel.Of(pCity);
                if (army == null || k == null || !World.world.wars.hasWars(k))
                {
                    return true;
                }
                if (army.countMelee() <= army.countRange() * ((1f - Cfg.RangeShare) / Math.Max(0.01f, Cfg.RangeShare)))
                {
                    return true;
                }
                List<long> ranged = new List<long>();
                for (int i = 0; i < pItems.Count; i++)
                {
                    Item it = World.world.items.get(pItems[i]);
                    if (it != null && it.asset != null && it.asset.attack_type == WeaponType.Range)
                    {
                        ranged.Add(pItems[i]);
                    }
                }
                if (ranged.Count == 0)
                {
                    return true;
                }
                __result = GiveItemFrom(ranged, pActor, pItems, pCity);
                First.Fire("combined arms");
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static bool GiveItemFrom(List<long> pFrom, Actor pActor, List<long> pStorage, City pCity)
        {
            if (pFrom.Count == 0 || !pActor.understandsHowToUseItems())
            {
                return false;
            }
            long random = pFrom.GetRandom();
            Item item = World.world.items.get(random);
            if (item == null)
            {
                return false;
            }
            EquipmentAsset asset = item.getAsset();
            ActorEquipmentSlot slot = pActor.equipment.getSlot(asset.equipment_type);
            if (!slot.isEmpty() && item.getValue() <= slot.getItem().getValue())
            {
                return false;
            }
            Item item2 = null;
            if (!slot.isEmpty())
            {
                item2 = slot.getItem();
                slot.takeAwayItem();
            }
            pStorage.Remove(random);
            pActor.equipment.setItem(item, pActor);
            pActor.setStatsDirty();
            if (item2 != null)
            {
                pCity.data.equipment.addItem(pCity, item2, pStorage);
            }
            if (_storageVersion != null)
            {
                _storageVersion.SetValue(pCity, (int)_storageVersion.GetValue(pCity) + 1);
            }
            return true;
        }

        private static void Postfix(Actor pActor, City pCity, bool __result, float __state)
        {
            if (!Cfg.EquipmentMarket || !__result || pActor == null || pCity == null)
            {
                return;
            }
            try
            {
                if (pCity.isInDanger())
                {
                    return;
                }
                float delta = EquippedValue(pActor) - __state;
                int price = (int)(delta * Cfg.ItemPricePct / 100f);
                if (price <= 0)
                {
                    return;
                }
                ActorData d = Rebel.Data(pActor);
                if (d == null || d.money <= 0)
                {
                    return;
                }
                price = Math.Min(price, d.money);
                if (price <= 0)
                {
                    return;
                }
                d.money -= price;
                pCity.addResourcesToRandomStockpile("gold", price);
                First.Fire("equipment market");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] equipment market failed: " + e.Message);
            }
        }

        private static readonly EquipmentType[] _equipTypes = (EquipmentType[])Enum.GetValues(typeof(EquipmentType));

        private static float EquippedValue(Actor pActor)
        {
            float v = 0f;
            if (pActor == null || pActor.equipment == null)
            {
                return 0f;
            }
            foreach (EquipmentType t in _equipTypes)
            {
                ActorEquipmentSlot s = pActor.equipment.getSlot(t);
                if (s != null && !s.isEmpty() && s.getItem() != null)
                {
                    v += s.getItem().getValue();
                }
            }
            return v;
        }
    }

    [HarmonyPatch(typeof(BehFindNearbyPotentialCivCityToJoin), "getPotentialCityNearby")]
    internal static class PatchCityTarget
    {
        private static readonly FieldInfo _cityCenter = Ref.Field(typeof(City), "city_center");

        private static bool Prefix(WorldTile pFromTile, Actor pActor, ref City __result)
        {
            if (!Cfg.LaborMigration || _cityCenter == null || pFromTile == null || pActor == null)
            {
                return true;
            }
            try
            {
                City best = null;
                float bestScore = float.MinValue;
                float range = SimGlobals.m.nomad_check_far_city_range;
                for (int i = 0; i < World.world.cities.list.Count; i++)
                {
                    City c = World.world.cities.list[i];
                    WorldTile t = c.getTile();
                    if (t == null || c.isNeutral())
                    {
                        continue;
                    }
                    float r = range;
                    Kingdom cKingdom = Rebel.Of(c);
                    if (!pActor.isNomad() && cKingdom != pActor.kingdom)
                    {
                        r *= 3f;
                    }
                    float dist = Toolbox.DistVec3(pFromTile.posV, (UnityEngine.Vector2)_cityCenter.GetValue(c));
                    if (dist > r || !c.isWelcomedToJoin(pActor) || !t.isSameIsland(pFromTile))
                    {
                        continue;
                    }
                    int free = 0;
                    if (c.jobs != null)
                    {
                        foreach (KeyValuePair<CitizenJobAsset, int> kv in c.jobs.jobs)
                        {
                            free += Math.Max(0, kv.Value - c.jobs.countOccupied(kv.Key));
                        }
                    }
                    float score = free * 10f - dist + (cKingdom == pActor.kingdom ? 100f : 0f);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }
                __result = best;
                First.Fire("labor migration");
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] city target patch failed: " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(City), "updateCapture")]
    internal static class PatchUpdateCapture
    {
        private static readonly FieldInfo _captureTicks = Ref.Field(typeof(City), "_capture_ticks");
        private static readonly FieldInfo _capturingUnits = Ref.Field(typeof(City), "_capturing_units");
        private static readonly FieldInfo _beingCapturedBy = Ref.Field(typeof(City), "being_captured_by");

        private static void Postfix(City __instance, float pElapsed)
        {
            if (!Cfg.SiegeMomentum || _captureTicks == null || _capturingUnits == null || _beingCapturedBy == null)
            {
                return;
            }
            try
            {
                Kingdom cap = _beingCapturedBy.GetValue(__instance) as Kingdom;
                if (cap == null)
                {
                    return;
                }
                Dictionary<Kingdom, int> units = _capturingUnits.GetValue(__instance) as Dictionary<Kingdom, int>;
                int cnt;
                if (units == null || !units.TryGetValue(cap, out cnt) || cnt < 2)
                {
                    return;
                }
                cnt = Math.Min(cnt, 10);
                float ticks = (float)_captureTicks.GetValue(__instance);
                _captureTicks.SetValue(__instance, ticks + (cnt - 1) * Cfg.MomentumPerUnit * pElapsed);
                First.Fire("siege momentum");
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(Actor), "joinCity")]
    internal static class PatchMercenary
    {
        private static readonly MethodInfo _tryWarrior = Ref.Method(typeof(City), "tryToMakeWarrior", new Type[] { typeof(Actor) });

        private static void Postfix(Actor __instance, City pCity)
        {
            if (!Cfg.Mercenaries || __instance == null || pCity == null || _tryWarrior == null)
            {
                return;
            }
            try
            {
                Actor a = __instance;
                if (a.asset == null || a.asset.is_boat || !a.isKingdomCiv() || !a.isAlive() || !a.isAdult()
                    || a.isWarrior() || a.isKing() || a.isCityLeader())
                {
                    return;
                }
                if (!Randy.randomChance(Cfg.MercenaryChance) || pCity.getResourcesAmount("gold") < Cfg.MercenaryCost)
                {
                    return;
                }
                if (_tryWarrior.Invoke(pCity, new object[] { a }) is bool ok && ok)
                {
                    pCity.takeResource("gold", (int)Cfg.MercenaryCost);
                    a.addMoney((int)Cfg.MercenaryCost);
                    First.Fire("mercenary");
                    UnityEngine.Debug.Log("[CivMind] mercenary: " + a.getName() + " enlisted in " + pCity.name + " for " + (int)Cfg.MercenaryCost + " gold");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] mercenary patch failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(ai.ActorTool), "getDockTradeTarget", new Type[] { typeof(Actor) })]
    internal static class PatchDockTarget
    {
        private static readonly FieldInfo _dockBuilding = Ref.Field(typeof(BaseBuildingComponent), "building");

        private static void Postfix(Actor pActor, ref Docks __result)
        {
            if (!Cfg.Embargo || __result == null || pActor == null || pActor.kingdom == null || _dockBuilding == null)
            {
                return;
            }
            try
            {
                Kingdom mine = pActor.kingdom;
                Building b = _dockBuilding.GetValue(__result) as Building;
                Kingdom theirs = b == null ? null : Rebel.Of(b.city);
                if (theirs == null || theirs == mine)
                {
                    return;
                }
                if (theirs.isEnemy(mine))
                {
                    __result = null;
                    First.Fire("embargo");
                    return;
                }
                if (!mine.hasAlliance())
                {
                    return;
                }
                Alliance alliance = mine.getAlliance();
                if (alliance == null || alliance.kingdoms_hashset == null)
                {
                    return;
                }
                foreach (Kingdom member in alliance.kingdoms_hashset)
                {
                    if (member != null && member != mine && member.isAlive() && theirs.isEnemy(member))
                    {
                        __result = null;
                        First.Fire("embargo");
                        return;
                    }
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(DiplomacyManager), "startWar", new Type[] { typeof(Kingdom), typeof(Kingdom), typeof(WarTypeAsset), typeof(bool) })]
    internal static class PatchStartWar
    {
        private static void Postfix(object[] __args)
        {
            if (!Cfg.TruceBreak || __args == null || __args.Length < 2)
            {
                return;
            }
            try
            {
                Kingdom att = __args[0] as Kingdom;
                Kingdom def = __args[1] as Kingdom;
                if (att == null || def == null)
                {
                    return;
                }
                DiplomacyRelation rel = World.world.diplomacy.getRelation(att, def);
                if (rel == null || rel.data == null)
                {
                    return;
                }
                if (Date.getYearsSince(rel.data.timestamp_last_war_ended) < SimGlobals.m.minimum_years_between_wars)
                {
                    WarFx.TruceBreak[att.id] = Date.getCurrentYear();
                    First.Fire("truce break");
                    UnityEngine.Debug.Log("[CivMind] truce broken: " + att.name + " → " + def.name);
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(OpinionLibrary), "init")]
    internal static class PatchOpinionInit
    {
        private static void Postfix()
        {
            if (AssetManager.opinion_library == null || !Once.PerInit("opinions"))
            {
                return;
            }
            try
            {
                if (Cfg.BorderFriction)
                {
                    OpinionAsset borders = AssetManager.opinion_library.get("opinion_borders") as OpinionAsset;
                    if (borders != null && borders.calc != null)
                    {
                        OpinionDelegateCalc orig = borders.calc;
                        borders.calc = delegate(Kingdom m, Kingdom t)
                        {
                            int o = orig(m, t);
                            int v = (int)(o * Cfg.BorderFrictionMult);
                            if (v != o)
                            {
                                First.Fire("border friction");
                            }
                            return v;
                        };
                    }
                }
                if (Cfg.TruceBreak && AssetManager.opinion_library.get("opinion_truce_breaker") == null)
                {
                    OpinionAsset tb = new OpinionAsset
                    {
                        id = "opinion_truce_breaker",
                        translation_key = "opinion_truce_breaker",
                        translation_key_negative = "opinion_truce_breaker"
                    };
                    tb.calc = delegate(Kingdom m, Kingdom t)
                    {
                        int year;
                        if (t != null && WarFx.TruceBreak.TryGetValue(t.id, out year) && Date.getCurrentYear() - year < Cfg.TruceBreakYears)
                        {
                            First.Fire("truce-break opinion");
                            return -(int)Cfg.TruceBreakOpinion;
                        }
                        return 0;
                    };
                    AssetManager.opinion_library.add(tb);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] opinion init failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(CombatActionLibrary), "init")]
    internal static class PatchCombatInit
    {
        private static void Postfix()
        {
            if (!Cfg.VetSpecials || AssetManager.combat_action_library == null || !Once.PerInit("combat_actions"))
            {
                return;
            }
            try
            {
                for (int i = 0; i < AssetManager.combat_action_library.list.Count; i++)
                {
                    CombatActionAsset ca = AssetManager.combat_action_library.list[i];
                    if (ca == null || ca.can_do_action == null)
                    {
                        continue;
                    }
                    CombatActionCheckStart orig = ca.can_do_action;
                    ca.can_do_action = delegate(Actor a, BaseSimObject t)
                    {
                        if (orig(a, t))
                        {
                            return true;
                        }
                        ActorData d = Rebel.Data(a);
                        if (d != null && d.kills >= Cfg.VetSpecKills && Randy.randomChance(Cfg.VetSpecBypass))
                        {
                            First.Fire("vet special: " + (ca.id ?? "?"));
                            return true;
                        }
                        return false;
                    };
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[CivMind] combat action wrap failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(BehCityActorCheckAttack), "execute")]
    internal static class PatchCheckAttack
    {
        private static readonly AccessTools.FieldRef<Actor, WorldTile> _behTileTarget = TryRef();

        private static AccessTools.FieldRef<Actor, WorldTile> TryRef()
        {
            try
            {
                return Ref.FieldRef<Actor, WorldTile>("beh_tile_target");
            }
            catch
            {
                return null;
            }
        }

        private static void Postfix(Actor pActor, ref BehResult __result)
        {
            if (__result != BehResult.Continue || !Cfg.ArmyRetreat || _behTileTarget == null || pActor == null || pActor.army == null)
            {
                return;
            }
            try
            {
                if (!WarFx.IsRetreating(pActor.army.id))
                {
                    return;
                }
                City home = pActor.city;
                if (home == null)
                {
                    return;
                }
                WorldTile t = home.getTile();
                if (t != null)
                {
                    _behTileTarget(pActor) = t;
                    First.Fire("retreat redirect");
                }
            }
            catch
            {
            }
        }
    }
}
