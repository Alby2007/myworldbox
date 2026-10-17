using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CivMindOverhaul
{
    internal static class Tuning
    {
        private static readonly MethodInfo _hasWeapon = AccessTools.Method(typeof(Actor), "hasWeapon");

        private static readonly Dictionary<string, float> _origWeight = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> _origCooldown = new Dictionary<string, int>();
        private static readonly Dictionary<string, DecisionActionWeight> _origCustom = new Dictionary<string, DecisionActionWeight>();
        private static PlotCheckerDelegate _origAllianceDestroyCheck;

        public static void TryApply()
        {
            try
            {
                if (AssetManager.decisions_library == null || AssetManager.decisions_library.list == null || AssetManager.decisions_library.list.Count == 0)
                {
                    return;
                }
                Apply();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CivMind] tuning deferred: " + e.Message);
            }
        }

        public static void Apply()
        {
            Retune("find_city_job", Cfg.JobSeekWeight, Cfg.JobSeekCooldown, pDemandAware: true);

            string[] expansion = {
                "claim_land",
                "build_civ_city_here",
                "check_join_city",
                "check_join_empty_nearby_city",
                "try_to_start_new_civilization",
                "king_check_new_city_foundation"
            };
            for (int i = 0; i < expansion.Length; i++)
            {
                Retune(expansion[i], Cfg.ExpansionWeight, Cfg.ExpansionCooldown);
            }

            Retune("find_lover", Cfg.ReproductionWeight, 1f);
            Retune("check_lover_city", Cfg.ReproductionWeight, 1f);
            if (Cfg.HousingCrisis)
            {
                RetuneHousing("sexual_reproduction_try");
            }
            else
            {
                Retune("sexual_reproduction_try", Cfg.ReproductionWeight, 1f);
            }

            Retune("socialize_initial_check", Cfg.SocializeWeight, 1f);
            Retune("check_swearing", Cfg.SocializeWeight, 1f);
            Retune("try_to_read", Cfg.SocializeWeight, 1f);

            if (Cfg.RefugeeBoost > 0f)
            {
                RetuneRefugee("check_join_city");
                RetuneRefugee("check_join_empty_nearby_city");
            }

            Retune("warrior_train_with_dummy", Cfg.TrainWeight, Cfg.TrainCooldown);
            if (Cfg.NavalInvasions)
            {
                Retune("check_warrior_transport", Cfg.TransportWeight, Cfg.TransportCooldown);
            }
            Retune("give_tax", Cfg.TaxWeight, Cfg.TaxCooldown);
            RetuneMult("make_items", Cfg.ItemCraftWeight, Cfg.ItemCraftCooldown, GoldenAgeMult);
            RetuneMult("try_to_take_city_item", Cfg.ItemArmWeight, Cfg.ItemArmCooldown, DangerArmMult);
            RetuneMult("boat_trading", Cfg.TradeWeight, 1f, BlockadeMult);
            Retune("boat_fishing", Cfg.TradeWeight, 1f);
            if (Cfg.MoraleRetreat)
            {
                ReworkRetreat("run_away");
            }
            WrapPlots();
        }

        private static float DangerArmMult(Actor a)
        {
            return a.city != null && a.city.isInDanger() ? Cfg.ArmDangerMult : 1f;
        }

        private static float GoldenAgeMult(Actor a)
        {
            if (!Cfg.GoldenAge)
            {
                return 1f;
            }
            try
            {
                Kingdom k = a.kingdom;
                if (k == null)
                {
                    k = Rebel.Of(a.city);
                }
                if (k != null && Rebel.PeaceYears(k) >= Cfg.GoldenAgeYears)
                {
                    return Cfg.GoldenAgeMult;
                }
            }
            catch
            {
            }
            return 1f;
        }

        private static float BlockadeMult(Actor a)
        {
            Kingdom k = a.kingdom;
            if (k == null)
            {
                k = Rebel.Of(a.city);
            }
            if (k == null)
            {
                return 1f;
            }
            try
            {
                if (World.world.wars.hasWars(k))
                {
                    return Cfg.TradeWarMult;
                }
            }
            catch
            {
            }
            return 1f;
        }

        private static void RetuneMult(string pId, float pWeightMult, float pCooldownMult, Func<Actor, float> pMult)
        {
            DecisionAsset d = Find(pId);
            if (d == null)
            {
                Log("missing decision: " + pId);
                return;
            }
            if (!_origWeight.ContainsKey(pId))
            {
                _origWeight[pId] = d.weight;
                _origCooldown[pId] = d.cooldown;
                _origCustom[pId] = d.weight_calculate_custom;
            }
            DecisionActionWeight orig = _origCustom[pId];
            float w = _origWeight[pId] * pWeightMult;
            if (orig == null)
            {
                d.has_weight_custom = true;
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return w * pMult(a);
                };
            }
            else
            {
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return orig(a) * w * pMult(a);
                };
            }
            d.cooldown = (int)(_origCooldown[pId] * pCooldownMult);
            Log($"retuned {pId}: weight x{pWeightMult} (+situational), cooldown x{pCooldownMult}");
        }

        private static void Retune(string pId, float pWeightMult, float pCooldownMult, bool pDemandAware = false)
        {
            DecisionAsset d = Find(pId);
            if (d == null)
            {
                Log("missing decision: " + pId);
                return;
            }

            if (!_origWeight.ContainsKey(pId))
            {
                _origWeight[pId] = d.weight;
                _origCooldown[pId] = d.cooldown;
                _origCustom[pId] = d.weight_calculate_custom;
            }

            if (pDemandAware)
            {
                d.has_weight_custom = true;
                d.weight_calculate_custom = JobSeekWeight;
            }
            else if (_origCustom[pId] != null)
            {
                DecisionActionWeight orig = _origCustom[pId];
                float m = pWeightMult;
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return orig(a) * m;
                };
            }
            else
            {
                d.weight = _origWeight[pId] * pWeightMult;
            }
            d.cooldown = Math.Max(1, (int)(_origCooldown[pId] * pCooldownMult));
            Log($"retuned {pId}: cooldown {_origCooldown[pId]} -> {d.cooldown}");
        }

        private static void RetuneHousing(string pId)
        {
            DecisionAsset d = Find(pId);
            if (d == null)
            {
                Log("missing decision: " + pId);
                return;
            }
            if (!_origWeight.ContainsKey(pId))
            {
                _origWeight[pId] = d.weight;
                _origCooldown[pId] = d.cooldown;
                _origCustom[pId] = d.weight_calculate_custom;
            }
            DecisionActionWeight orig = _origCustom[pId];
            if (orig == null)
            {
                float w = _origWeight[pId];
                d.has_weight_custom = true;
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return a.hasHouse() ? w * Cfg.ReproductionWeight : w * Cfg.NoHouseReproMult;
                };
            }
            else
            {
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return a.hasHouse() ? orig(a) * Cfg.ReproductionWeight : orig(a) * Cfg.NoHouseReproMult;
                };
            }
            d.cooldown = _origCooldown[pId];
            Log($"retuned {pId}: housing-gated");
        }

        private static void RetuneRefugee(string pId)
        {
            DecisionAsset d = Find(pId);
            if (d == null)
            {
                Log("missing decision: " + pId);
                return;
            }
            if (!_origWeight.ContainsKey(pId))
            {
                _origWeight[pId] = d.weight;
                _origCooldown[pId] = d.cooldown;
                _origCustom[pId] = d.weight_calculate_custom;
            }
            DecisionActionWeight orig = _origCustom[pId];
            float w = _origWeight[pId];
            if (orig == null)
            {
                d.has_weight_custom = true;
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return w * RefugeeMult(a);
                };
            }
            else
            {
                d.weight_calculate_custom = delegate(Actor a)
                {
                    return orig(a) * RefugeeMult(a);
                };
            }
            d.cooldown = _origCooldown[pId];
            Log($"retuned {pId}: refugee-boosted");
        }

        private static float RefugeeMult(Actor a)
        {
            if (a.city != null || a.kingdom == null)
            {
                return 1f;
            }
            try
            {
                if (World.world.wars.hasWars(a.kingdom))
                {
                    return Cfg.RefugeeBoost;
                }
            }
            catch
            {
            }
            return 1f;
        }

        internal static void WrapPlots()
        {
            if (!Cfg.AntiHegemon || _origAllianceDestroyCheck != null)
            {
                return;
            }
            PlotAsset destroy = AssetManager.plots_library == null ? null : AssetManager.plots_library.get("alliance_destroy") as PlotAsset;
            if (destroy == null || destroy.check_is_possible == null)
            {
                return;
            }
            _origAllianceDestroyCheck = destroy.check_is_possible;
            PlotCheckerDelegate orig = _origAllianceDestroyCheck;
            destroy.check_is_possible = delegate(Actor a)
            {
                if (orig(a))
                {
                    return true;
                }
                try
                {
                    Kingdom k = a.kingdom;
                    if (k == null || !k.hasAlliance() || k.hasEnemies())
                    {
                        return false;
                    }
                    Alliance al = k.getAlliance();
                    if (al == null || al.isForcedType() || al.getAge() < 10)
                    {
                        return false;
                    }
                    if (Date.getYearsSince(al.data.timestamp_member_joined) < 10.0)
                    {
                        return false;
                    }
                    foreach (Kingdom m in al.kingdoms_hashset)
                    {
                        if (m.isSupreme())
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                }
                return false;
            };
            Log("wrapped alliance_destroy: members may break hegemon blocs");
        }

        private static void ReworkRetreat(string pId)
        {
            DecisionAsset d = Find(pId);
            if (d == null)
            {
                Log("missing decision: " + pId);
                return;
            }
            d.action_check_launch = delegate(Actor a)
            {
                if (!a.isFighting())
                {
                    return false;
                }
                bool armed = _hasWeapon != null && (bool)_hasWeapon.Invoke(a, null);
                float t = armed ? Cfg.RetreatHealth : Math.Max(Cfg.RetreatHealth, 0.85f);
                if (Cfg.TraitMorale)
                {
                    if (a.hasTrait("pacifist"))
                    {
                        t *= 1.6f;
                    }
                    else if (a.hasTrait("bloodlust") || a.hasTrait("savage") || a.hasTrait("veteran"))
                    {
                        t *= 0.5f;
                    }
                    t += a.warfare * 0.003f;
                }
                if (Cfg.MoraleAura && a.army != null && a.army.hasCaptain())
                {
                    Actor cap = a.army.getCaptain();
                    if (cap != null && !cap.isRekt() && cap.current_tile != null && (float)Toolbox.SquaredDistTile(a.current_tile, cap.current_tile) <= Cfg.AuraRadius * Cfg.AuraRadius)
                    {
                        t *= Cfg.AuraFleeMult;
                    }
                }
                if (Cfg.MoraleShock && a.army != null)
                {
                    float shockExp;
                    if (WarFx.Shaken.TryGetValue(a.army.id, out shockExp) && shockExp > Time.time)
                    {
                        t = Math.Min(0.95f, t * Cfg.ShockFleeMult);
                    }
                }
                return a.getHealthRatio() < t;
            };
            Log($"reworked {pId}: rout when health < {Cfg.RetreatHealth} (vanilla: only while > 0.2)");
        }

        private static float JobSeekWeight(Actor pActor)
        {
            float num = pActor.isStarving() ? 0.3f : (pActor.isHungry() ? 1f : 2f);
            num *= Cfg.JobSeekWeight;
            if (Cfg.JobDemandBoost > 0f && pActor.city != null)
            {
                int free = 0;
                CitizenJobs jobs = pActor.city.jobs;
                foreach (KeyValuePair<CitizenJobAsset, int> kv in jobs.jobs)
                {
                    int open = kv.Value - jobs.countOccupied(kv.Key);
                    if (open > 0)
                    {
                        free += open;
                    }
                }
                if (free > 0)
                {
                    num += Math.Min(free, 4) * Cfg.JobDemandBoost;
                }
            }
            return num;
        }

        private static DecisionAsset Find(string pId)
        {
            try
            {
                return AssetManager.decisions_library.get(pId);
            }
            catch
            {
                return null;
            }
        }

        private static void Log(string pMsg)
        {
            if (Cfg.LogTuning)
            {
                Debug.Log("[CivMind] " + pMsg);
            }
        }
    }
}
