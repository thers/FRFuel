using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FRFuel
{
    public static class GasStations
    {
        private static List<GasStation> _instance;

        public static List<GasStation> GetInstance()
        {
            if (_instance == null)
            {
                try
                {
                    string json = API.LoadResourceFile(API.GetCurrentResourceName(), "GasStations.json");
                    _instance = JsonConvert.DeserializeObject<List<GasStation>>(json);
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"[FRFuel] Failed to load config: {ex.Message}");
                }
            }
            return _instance;
        }
    }

    public class GasStation
    {
        public Vector3 Coordinates;
        public List<Vector3> Pumps;
    }
}
