using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FRFuel
{

    public static class GasStations
    {
        public static Vector3[] positions;
        public static Vector3[][] pumps;

        private static bool AreGasStationsLoaded = false;

        /// <summary>
        /// Loads the 'positions' and 'pumps' arrays with the data from 'GasStations.json'.
        /// Could use some improvements in the future, but it works for now.
        /// </summary>
        public static void LoadGasStations()
        {
            if (!AreGasStationsLoaded)
            {
                // load the GasStations.json file.
                string jsonString = LoadResourceFile(GetCurrentResourceName(), "GasStations.json");
                if (string.IsNullOrEmpty(jsonString))
                {
                    // Do not continue if the file is empty or it's null.
                    Debug.WriteLine("[FRFuel] An error occurred while loading the gas stations file.");
                    return;
                }

                // Convert the json into an object.
                Newtonsoft.Json.Linq.JObject jsonData = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                int i = 0;

                Newtonsoft.Json.Linq.JArray gasStations = (Newtonsoft.Json.Linq.JArray)jsonData["GasStations"];

                // Initialize the 'positions' and 'pumps' Vector3 Arrays.
                positions = new Vector3[gasStations.Count];
                pumps = new Vector3[gasStations.Count][];

                // Go through every gas station in the json data, and create a location entry for it.
                // Then go through all the pumps for that gas station and add all the pump vector3's to the pumps Array.
                foreach (var gasStation in gasStations)
                {
                    Vector3 location = new Vector3(
                        float.Parse(gasStation["coordinates"]["X"].ToString()),
                        float.Parse(gasStation["coordinates"]["Y"].ToString()),
                        float.Parse(gasStation["coordinates"]["Z"].ToString())
                        );

                    positions[i] = location;

                    List<Vector3> tempPumps = new List<Vector3>();
                    foreach (var pump in gasStation["pumps"])
                    {
                        Vector3 pumpLocation = new Vector3(
                            float.Parse(pump["X"].ToString()),
                            float.Parse(pump["Y"].ToString()),
                            float.Parse(pump["Z"].ToString())
                            );
                        tempPumps.Add(pumpLocation);
                    }
                    pumps[i] = new Vector3[tempPumps.Count];
                    for (var pump = 0; pump < tempPumps.Count; pump++)
                    {
                        pumps[i][pump] = tempPumps[pump];
                    }

                    tempPumps = null;

                    i++;
                }

                // Prevent this function from being accidentally called twice for whatever reason.
                AreGasStationsLoaded = true;
            }
        }
    }
}
