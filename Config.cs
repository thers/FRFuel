using Newtonsoft.Json;
using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.IO;

namespace FRFuel
{
    class Config
    {
        private static Config _instance;

        public static Config GetInstance()
        {
            if (_instance == null)
            {
                try
                {
                    string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
                    _instance = JsonConvert.DeserializeObject<Config>(json);
                }

                catch (Exception ex)
                {
                    _instance = new Config();
                    Debug.WriteLine($"[FRFuel] Failed to load config: {ex.Message}");
                }
            }
            return _instance;
        }

        //Defaults
        public bool ShowBlips { get; set; } = true;
        public bool CreatePickups { get; set; } = true;
        public bool ShowHud { get; set; } = true;
        public bool ShowHudWhenEngineOff { get; set; } = true;
        public float? FuelTankCapacityOverride { get; set; } = null;
        public float FuelConsumptionRate { get; set; } = 1f;
        public float FuelAccelerationImpact { get; set; } = 0.0002f;
        public float FuelTractionImpact { get; set; } = 0.0001f;
        public float FuelRPMImpact { get; set; } = 0.0005f;
        public float FuelSpeedImpact { get; set; } = 0.0002f;
        public float RefuelRate { get; set; } = 1f;
        public bool AllowManualRefills { get; set; } = false;
        public int EngineToggleKey { get; set; } = 86;
        public string FuelBarColor { get; set; }
        public string FuelBarLowColor { get; set; }
    }
}
