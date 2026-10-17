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
