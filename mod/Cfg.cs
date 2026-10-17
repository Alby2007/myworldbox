using NeoModLoader.api;

namespace CivMindOverhaul
{
    internal static class Cfg
    {
        public static float JobSeekWeight = 2f;
        public static float JobSeekCooldown = 0.4f;
        public static float JobDemandBoost = 1f;
        public static int ExtraBuilders = 2;
        public static int ExtraFoodJobs = 3;
        public static int ExtraWoodcutters = 2;
        public static float LowFoodThreshold = 60f;
        public static float LowWoodThreshold = 15f;
        public static float ExpansionWeight = 1.8f;
        public static float ExpansionCooldown = 0.6f;
        public static float ReproductionWeight = 1.3f;
        public static float SocializeWeight = 1.6f;
        public static bool AptitudeHiring = true;
        public static bool EmergencyDraft = true;
        public static int DraftAttackerSlots = 4;
        public static bool HousingCrisis = true;
        public static float NoHouseReproMult = 0.15f;
        public static int JobXpAmount = 5;
        public static bool MoraleRetreat = true;
        public static float RetreatHealth = 0.35f;
        public static bool TraitMorale = true;
        public static bool SmartWarTarget = true;
        public static bool SmartCityTarget = true;
        public static bool MeritCaptains = true;
        public static float MarchFill = 0.7f;
        public static float WarTimeoutYears = 5f;
        public static float WarMinWarriors = 10f;
        public static float WarPeaceYears = 5f;
        public static float WarStalemateYears = 12f;
        public static float TrainWeight = 1.4f;
        public static float TrainCooldown = 0.6f;
        public static bool WarscoreSurrender = true;
        public static float WarscoreMinDeaths = 20f;
        public static float WarscoreRatio = 3f;
        public static bool FocusFire = true;
        public static float FocusFireChance = 0.15f;
        public static bool HomeField = true;
        public static float HomeFieldMult = 0.8f;
        public static float DesertionWarYears = 8f;
        public static float DesertionChance = 0.005f;
        public static bool Pillage = true;
        public static float PillageChance = 0.02f;
        public static int PillageAmount = 5;
        public static bool Duels = true;
        public static float DuelChance = 0.5f;
        public static float DuelRadius = 8f;
        public static bool MoraleShock = true;
        public static float ShockSeconds = 30f;
        public static float ShockFleeMult = 2.5f;
        public static bool NavalInvasions = true;
        public static float TransportWeight = 1.75f;
        public static float TransportCooldown = 0.5f;
        public static bool BerserkerRage = true;
        public static int RageKills = 3;
        public static float RageWindowSec = 30f;
        public static float RageDurationSec = 20f;
        public static bool MeritSuccession = true;
        public static bool Reparations = true;
        public static float ReparationsPct = 25f;
        public static bool WarHeroes = true;
        public static float HeroMinKills = 3f;
        public static bool BalanceAlliances = true;
        public static bool MoraleAura = true;
        public static float AuraRadius = 12f;
        public static float AuraFleeMult = 0.6f;
        public static bool RallyFlee = true;
        public static bool MilitiaReserve = true;
        public static float ReservePeacetimeRatio = 0.6f;
        public static float RefugeeBoost = 2.5f;
        public static bool MeritLeaders = true;
        public static float LeaderMargin = 5f;
        public static bool Uprisings = true;
        public static float UprisingLoyalty = 30f;
        public static float UprisingRecentYears = 10f;
        public static float UprisingChance = 0.02f;
        public static bool KingPersonality = true;
        public static float PeaceKingDiplomacy = 10f;
        public static float PeaceVetoChance = 0.5f;
        public static float WarlordWarfare = 12f;
        public static float WarlordChance = 0.5f;
        public static float WarlordMinWarriors = 4f;
        public static bool AntiHegemon = true;
        public static float HegemonBonusMult = 0.7f;
        public static bool PretenderWars = true;
        public static float PretenderMargin = 8f;
        public static float PretenderChance = 0.35f;
        public static bool CollapsePressure = true;
        public static float CollapseChance = 0.3f;
        public static float CollapseKingScore = 8f;
        public static bool BorderPressure = true;
        public static float BorderPressureChance = 0.35f;
        public static bool WarTribute = true;
        public static float TributePct = 30f;
        public static float TributeMinGold = 60f;
        public static float TributeChance = 0.05f;
        public static float SupplyWarMult = 0.5f;
        public static bool SiegeStarvation = true;
        public static float SiegeFoodDrain = 2f;
        public static float SiegeDrainChance = 0.3f;
        public static float ReconBuilders = 2f;
        public static float ReconYears = 8f;
        public static float WarExhaustYears = 10f;
        public static float WarExhaustMult = 2f;
        public static bool HolyWars = true;
        public static float HolyWarMult = 0.7f;
        public static bool Assimilation = true;
        public static float AssimilationChance = 0.05f;
        public static float CultureUprisingBonus = 10f;
        public static bool LogTuning = true;

        public static void Load(ModConfig pCfg)
        {
            if (pCfg == null)
            {
                return;
            }
            JobSeekWeight = Slider(pCfg, "Jobs", "JobSeekWeight", JobSeekWeight);
            JobSeekCooldown = Slider(pCfg, "Jobs", "JobSeekCooldown", JobSeekCooldown);
            JobDemandBoost = Slider(pCfg, "Jobs", "JobDemandBoost", JobDemandBoost);
            ExtraBuilders = (int)Slider(pCfg, "Jobs", "ExtraBuilders", ExtraBuilders);
            ExtraFoodJobs = (int)Slider(pCfg, "Jobs", "ExtraFoodJobs", ExtraFoodJobs);
            ExtraWoodcutters = (int)Slider(pCfg, "Jobs", "ExtraWoodcutters", ExtraWoodcutters);
            LowFoodThreshold = Slider(pCfg, "Jobs", "LowFoodThreshold", LowFoodThreshold);
            LowWoodThreshold = Slider(pCfg, "Jobs", "LowWoodThreshold", LowWoodThreshold);
            ExpansionWeight = Slider(pCfg, "Expansion", "ExpansionWeight", ExpansionWeight);
            ExpansionCooldown = Slider(pCfg, "Expansion", "ExpansionCooldown", ExpansionCooldown);
            ReproductionWeight = Slider(pCfg, "Social", "ReproductionWeight", ReproductionWeight);
            SocializeWeight = Slider(pCfg, "Social", "SocializeWeight", SocializeWeight);
            AptitudeHiring = Switch(pCfg, "Experimental", "AptitudeHiring", AptitudeHiring);
            EmergencyDraft = Switch(pCfg, "Experimental", "EmergencyDraft", EmergencyDraft);
            DraftAttackerSlots = (int)Slider(pCfg, "Experimental", "DraftAttackerSlots", DraftAttackerSlots);
            HousingCrisis = Switch(pCfg, "Experimental", "HousingCrisis", HousingCrisis);
            NoHouseReproMult = Slider(pCfg, "Experimental", "NoHouseReproMult", NoHouseReproMult);
            JobXpAmount = (int)Slider(pCfg, "Experimental", "JobXpAmount", JobXpAmount);
            MoraleRetreat = Switch(pCfg, "War", "MoraleRetreat", MoraleRetreat);
            RetreatHealth = Slider(pCfg, "War", "RetreatHealth", RetreatHealth);
            TraitMorale = Switch(pCfg, "War", "TraitMorale", TraitMorale);
            SmartWarTarget = Switch(pCfg, "War", "SmartWarTarget", SmartWarTarget);
            SmartCityTarget = Switch(pCfg, "War", "SmartCityTarget", SmartCityTarget);
            MeritCaptains = Switch(pCfg, "War", "MeritCaptains", MeritCaptains);
            MarchFill = Slider(pCfg, "War", "MarchFill", MarchFill);
            WarTimeoutYears = Slider(pCfg, "War", "WarTimeoutYears", WarTimeoutYears);
            WarMinWarriors = Slider(pCfg, "War", "WarMinWarriors", WarMinWarriors);
            WarPeaceYears = Slider(pCfg, "War", "WarPeaceYears", WarPeaceYears);
            WarStalemateYears = Slider(pCfg, "War", "WarStalemateYears", WarStalemateYears);
            TrainWeight = Slider(pCfg, "War", "TrainWeight", TrainWeight);
            TrainCooldown = Slider(pCfg, "War", "TrainCooldown", TrainCooldown);
            WarscoreSurrender = Switch(pCfg, "WarTactics", "WarscoreSurrender", WarscoreSurrender);
            WarscoreMinDeaths = Slider(pCfg, "WarTactics", "WarscoreMinDeaths", WarscoreMinDeaths);
            WarscoreRatio = Slider(pCfg, "WarTactics", "WarscoreRatio", WarscoreRatio);
            FocusFire = Switch(pCfg, "WarTactics", "FocusFire", FocusFire);
            FocusFireChance = Slider(pCfg, "WarTactics", "FocusFireChance", FocusFireChance);
            HomeField = Switch(pCfg, "WarTactics", "HomeField", HomeField);
            HomeFieldMult = Slider(pCfg, "WarTactics", "HomeFieldMult", HomeFieldMult);
            DesertionWarYears = Slider(pCfg, "WarTactics", "DesertionWarYears", DesertionWarYears);
            DesertionChance = Slider(pCfg, "WarTactics", "DesertionChance", DesertionChance);
            Pillage = Switch(pCfg, "WarTactics", "Pillage", Pillage);
            PillageChance = Slider(pCfg, "WarTactics", "PillageChance", PillageChance);
            PillageAmount = (int)Slider(pCfg, "WarTactics", "PillageAmount", PillageAmount);
            Duels = Switch(pCfg, "WarTactics", "Duels", Duels);
            DuelChance = Slider(pCfg, "WarTactics", "DuelChance", DuelChance);
            DuelRadius = Slider(pCfg, "WarTactics", "DuelRadius", DuelRadius);
            MoraleShock = Switch(pCfg, "WarTactics", "MoraleShock", MoraleShock);
            ShockSeconds = Slider(pCfg, "WarTactics", "ShockSeconds", ShockSeconds);
            ShockFleeMult = Slider(pCfg, "WarTactics", "ShockFleeMult", ShockFleeMult);
            NavalInvasions = Switch(pCfg, "WarTactics", "NavalInvasions", NavalInvasions);
            TransportWeight = Slider(pCfg, "WarTactics", "TransportWeight", TransportWeight);
            TransportCooldown = Slider(pCfg, "WarTactics", "TransportCooldown", TransportCooldown);
            BerserkerRage = Switch(pCfg, "WarTactics", "BerserkerRage", BerserkerRage);
            RageKills = (int)Slider(pCfg, "WarTactics", "RageKills", RageKills);
            RageWindowSec = Slider(pCfg, "WarTactics", "RageWindowSec", RageWindowSec);
            RageDurationSec = Slider(pCfg, "WarTactics", "RageDurationSec", RageDurationSec);
            MoraleAura = Switch(pCfg, "WarTactics", "MoraleAura", MoraleAura);
            AuraRadius = Slider(pCfg, "WarTactics", "AuraRadius", AuraRadius);
            AuraFleeMult = Slider(pCfg, "WarTactics", "AuraFleeMult", AuraFleeMult);
            RallyFlee = Switch(pCfg, "WarTactics", "RallyFlee", RallyFlee);
            MilitiaReserve = Switch(pCfg, "WarTactics", "MilitiaReserve", MilitiaReserve);
            ReservePeacetimeRatio = Slider(pCfg, "WarTactics", "ReservePeacetimeRatio", ReservePeacetimeRatio);
            MeritSuccession = Switch(pCfg, "Politics", "MeritSuccession", MeritSuccession);
            Reparations = Switch(pCfg, "Politics", "Reparations", Reparations);
            ReparationsPct = Slider(pCfg, "Politics", "ReparationsPct", ReparationsPct);
            WarHeroes = Switch(pCfg, "Politics", "WarHeroes", WarHeroes);
            HeroMinKills = Slider(pCfg, "Politics", "HeroMinKills", HeroMinKills);
            BalanceAlliances = Switch(pCfg, "Politics", "BalanceAlliances", BalanceAlliances);
            RefugeeBoost = Slider(pCfg, "HomeFront", "RefugeeBoost", RefugeeBoost);
            MeritLeaders = Switch(pCfg, "HomeFront", "MeritLeaders", MeritLeaders);
            LeaderMargin = Slider(pCfg, "HomeFront", "LeaderMargin", LeaderMargin);
            Uprisings = Switch(pCfg, "HomeFront", "Uprisings", Uprisings);
            UprisingLoyalty = Slider(pCfg, "HomeFront", "UprisingLoyalty", UprisingLoyalty);
            UprisingRecentYears = Slider(pCfg, "HomeFront", "UprisingRecentYears", UprisingRecentYears);
            UprisingChance = Slider(pCfg, "HomeFront", "UprisingChance", UprisingChance);
            WarTribute = Switch(pCfg, "HomeFront", "WarTribute", WarTribute);
            TributePct = Slider(pCfg, "HomeFront", "TributePct", TributePct);
            TributeMinGold = Slider(pCfg, "HomeFront", "TributeMinGold", TributeMinGold);
            TributeChance = Slider(pCfg, "HomeFront", "TributeChance", TributeChance);
            SupplyWarMult = Slider(pCfg, "HomeFront", "SupplyWarMult", SupplyWarMult);
            SiegeStarvation = Switch(pCfg, "HomeFront", "SiegeStarvation", SiegeStarvation);
            SiegeFoodDrain = Slider(pCfg, "HomeFront", "SiegeFoodDrain", SiegeFoodDrain);
            SiegeDrainChance = Slider(pCfg, "HomeFront", "SiegeDrainChance", SiegeDrainChance);
            ReconBuilders = Slider(pCfg, "HomeFront", "ReconBuilders", ReconBuilders);
            ReconYears = Slider(pCfg, "HomeFront", "ReconYears", ReconYears);
            WarExhaustYears = Slider(pCfg, "HomeFront", "WarExhaustYears", WarExhaustYears);
            WarExhaustMult = Slider(pCfg, "HomeFront", "WarExhaustMult", WarExhaustMult);
            KingPersonality = Switch(pCfg, "Politics", "KingPersonality", KingPersonality);
            PeaceKingDiplomacy = Slider(pCfg, "Politics", "PeaceKingDiplomacy", PeaceKingDiplomacy);
            PeaceVetoChance = Slider(pCfg, "Politics", "PeaceVetoChance", PeaceVetoChance);
            WarlordWarfare = Slider(pCfg, "Politics", "WarlordWarfare", WarlordWarfare);
            WarlordChance = Slider(pCfg, "Politics", "WarlordChance", WarlordChance);
            WarlordMinWarriors = Slider(pCfg, "Politics", "WarlordMinWarriors", WarlordMinWarriors);
            AntiHegemon = Switch(pCfg, "Politics", "AntiHegemon", AntiHegemon);
            HegemonBonusMult = Slider(pCfg, "Politics", "HegemonBonusMult", HegemonBonusMult);
            PretenderWars = Switch(pCfg, "Politics", "PretenderWars", PretenderWars);
            PretenderMargin = Slider(pCfg, "Politics", "PretenderMargin", PretenderMargin);
            PretenderChance = Slider(pCfg, "Politics", "PretenderChance", PretenderChance);
            CollapsePressure = Switch(pCfg, "Politics", "CollapsePressure", CollapsePressure);
            CollapseChance = Slider(pCfg, "Politics", "CollapseChance", CollapseChance);
            CollapseKingScore = Slider(pCfg, "Politics", "CollapseKingScore", CollapseKingScore);
            BorderPressure = Switch(pCfg, "WarTactics", "BorderPressure", BorderPressure);
            BorderPressureChance = Slider(pCfg, "WarTactics", "BorderPressureChance", BorderPressureChance);
            HolyWars = Switch(pCfg, "Culture", "HolyWars", HolyWars);
            HolyWarMult = Slider(pCfg, "Culture", "HolyWarMult", HolyWarMult);
            Assimilation = Switch(pCfg, "Culture", "Assimilation", Assimilation);
            AssimilationChance = Slider(pCfg, "Culture", "AssimilationChance", AssimilationChance);
            CultureUprisingBonus = Slider(pCfg, "Culture", "CultureUprisingBonus", CultureUprisingBonus);
            LogTuning = Switch(pCfg, "Debug", "LogTuning", LogTuning);
        }

        private static float Slider(ModConfig pCfg, string pGroup, string pId, float pDefault)
        {
            try
            {
                return pCfg[pGroup][pId].FloatVal;
            }
            catch
            {
                return pDefault;
            }
        }

        private static bool Switch(ModConfig pCfg, string pGroup, string pId, bool pDefault)
        {
            try
            {
                return pCfg[pGroup][pId].BoolVal;
            }
            catch
            {
                return pDefault;
            }
        }
    }
}
