#undef MANUAL_ENGINE_CUTOFF

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;

namespace FRFuel
{
    public class FRFuel : BaseScript
    {
        delegate float GetCurrentFuelLevelDelegate();
        delegate void AddFuel(float amount);
        delegate void SetFuel(float amount);

        #region Fields
        public static string fuelLevelPropertyName = "_Fuel_Level";
        public static string manualRefuelAnimDict = "weapon@w_sp_jerrycan";

        public static string[] tankBones = new string[] {
            "petrolcap",
            "petroltank",
            "petroltank_r",
            "petroltank_l",
            "wheel_lr"
        };

        protected Blip[] blips;
        protected List<Pickup> pickups;

        protected float fuelTankCapacity = 65f;

        protected float refuelRate = 1f;
        protected float fuelConsumptionRate = 1f;
        protected float fuelAccelerationImpact = 0.0002f;
        protected float fuelTractionImpact = 0.0001f;
        protected float fuelRPMImpact = 0.0005f;

        public float showMarkerInRangeSquared = 250f;

        public HUD hud;
        public Random random = new Random();

        private int currentGasStationIndex;
        private Vehicle lastVehicle;

        protected bool currentVehicleFuelLevelInitialized = false;
        protected bool hudActive = false;
        protected bool refuelAllowed = true;
        protected bool initVehicles = false;
        protected float addedFuelCapacitor = 0f;

        protected InLoopOutAnimation jerryCanAnimation;

        protected Vehicle LastVehicle { get => lastVehicle; set => lastVehicle = value; }

        protected Config Config { get; set; }
        protected bool showHud = true;
        protected bool showHudWhenEngineOff = true;

        protected bool initialized = false;
        public static Control engineToggleControl = Control.VehicleHorn;

        private string fuelBarNormalHexColor = "FFB300"; // 255, 179, 0
        private string fuelBarWarningHexColor = "FFF5DC"; // 255, 179, 0
        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        public FRFuel()
        {
            hud = new HUD();

            jerryCanAnimation = new InLoopOutAnimation(
              new Animation(manualRefuelAnimDict, "fire_intro"),
              new Animation(manualRefuelAnimDict, "fire"),
              new Animation(manualRefuelAnimDict, "fire_outro")
            );

            EventHandlers["frfuel:refuelAllowed"] += new Action<dynamic>((dynamic toggle) =>
            {
                if (toggle.GetType() == typeof(bool))
                {
                    refuelAllowed = (bool)toggle;
                }
            });

            GasStations.LoadGasStations();

            blips = new Blip[GasStations.positions.Length];
            pickups = new List<Pickup>();

            EntityDecoration.RegisterProperty(fuelLevelPropertyName, DecorationType.Float);

            Exports.Add("addFuel", new AddFuel(ExportsAddFuel));
            Exports.Add("setFuel", new SetFuel(ExportsSetFuel));
            Exports.Add("getCurrentFuelLevel", new GetCurrentFuelLevelDelegate(ExportsGetCurrentFuelLevel));

            Tick += OnTick;
        }

        #region Init
        /// <summary>
        /// Loads configuration from file
        /// </summary>
        protected void LoadConfig()
        {
            string configContent = null;

            try
            {
                configContent = LoadResourceFile(GetCurrentResourceName(), "config.ini");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"An error occurred while loading the config file, error description: {e.Message}.");
            }

            Config = new Config(configContent);

            showHud = Config.Get("ShowHud", "true") == "true";
            showHudWhenEngineOff = Config.Get("ShowHudWhenEngineOff", "true").ToLower() == "true";
            initVehicles = Config.Get("InitVehicleFuel", "false") == "true";

            var fuelConsumptionString = Config.Get("FuelConsumptionRate", "1");
            if (float.TryParse(fuelConsumptionString, out float tmpFuelConsumptionRate))
            {
                fuelConsumptionRate = tmpFuelConsumptionRate;
            }
#if DEBUG
            else
            {
                Debug.WriteLine("Invalid FuelConsumptionRate value. Make sure it is a valid float value, e.g. 1.2");
            }
#endif

            var refuelRateString = Config.Get("RefuelRate", "1");
            if (float.TryParse(refuelRateString, out float tmpRefuelRate))
            {
                refuelRate = tmpRefuelRate;
            }
#if DEBUG
            else
            {
                Debug.WriteLine("Invalid RefuelRate value. Make sure it is a valid float value, e.g. 1.2");
            }
#endif

            // if a valid key is set in the config file, set the control.
            if (int.TryParse(Config.Get("EngineToggleKey", "86"), out int tmpControl))
            {
                engineToggleControl = (Control)tmpControl;
            }

            fuelBarNormalHexColor = Config.Get("FuelBarNormalColor", fuelBarNormalHexColor).Replace("\"", "").Replace("#", "");
            // normal color
            int r = MathUtil.Clamp(int.Parse(fuelBarNormalHexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);
            int g = MathUtil.Clamp(int.Parse(fuelBarNormalHexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);
            int b = MathUtil.Clamp(int.Parse(fuelBarNormalHexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);

            fuelBarWarningHexColor = Config.Get("FuelBarWarningColor", fuelBarNormalHexColor).Replace("\"", "").Replace("#", "");
            // warning color
            int wR = MathUtil.Clamp(int.Parse(fuelBarWarningHexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);
            int wG = MathUtil.Clamp(int.Parse(fuelBarWarningHexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);
            int wB = MathUtil.Clamp(int.Parse(fuelBarWarningHexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber), 0, 255);
            hud.UpdateBarColors(r, g, b, wR, wG, wB);

#if DEBUG
            Debug.WriteLine($"CreatePickups: {Config.Get("CreatePickups", "true")}");
            Debug.WriteLine($"ShowHud: {Config.Get("ShowHud", "true")}");
            Debug.WriteLine($"EngineToggleKey: {Config.Get("EngineToggleKey", "86")}");
            Debug.WriteLine($"FuelConsumptionRate: {Config.Get("FuelConsumptionRate", "1")}");
#endif
        }

        /// <summary>
        /// Creates blips for gas stations
        /// </summary>
        public void CreateBlips()
        {
            if (Config.Get("CreateBlips", "true") != "true")
            {
                return;
            }

            for (int i = 0; i < GasStations.positions.Length; i++)
            {
                var blip = World.CreateBlip(GasStations.positions[i]);
                blip.Sprite = BlipSprite.JerryCan;
                blip.Color = BlipColor.White;
                blip.Scale = 1f;
                blip.IsShortRange = true;
                blip.Name = "Gas Station";

                blips[i] = blip;
            }
        }

        /// <summary>
        /// Gets coordinates for jerry cans within 100f radius
        /// </summary>
        /// <param name="position"></param>
        /// <returns>List of coordinates for pickups</returns>
        public IEnumerable<Vector3> GetNearbyJerryCanPickUpCoordinates(Vector3 position)
        {
            return GasStations.positions.Where(p => p.DistanceToSquared(position) < 100.0f);
        }

        /// <summary>
        /// Automatically adds pickups for nearby jerry cans, and removes when leaving area
        /// </summary>
        public async Task ManageNearbyJerryCanPickUps()
        {
            if (Config.Get("CreatePickups", "true") != "true")
            {
                return;
            }

            Vector3 pos = GetEntityCoords(PlayerPedId(), true);

            int model = 883325847;

            Function.Call(Hash.REQUEST_MODEL, model);

            IEnumerable<Vector3> positions = GetNearbyJerryCanPickUpCoordinates(pos);

            if (positions.Count() == 0 && pickups.Count != 0)
            {
                pickups.ForEach(p => p.Delete());
                pickups.Clear();
            }
            else
            {
                positions.ToList().ForEach(position =>
                {
                    if (!pickups.Any(pickup => position.DistanceToSquared(pickup.Position) < 5f))
                    {
                        // add pickup if one doesn't exist within 5f proximity of it
                        int pickupHandle = CreatePickup(
                            0xc69de3ff, // Petrol Can
                            position.X, position.Y, position.Z - 0.5f,
                            8 | 32, // Place on the ground, local only
                            0,
                            true,
                            (uint)model);

                        Pickup pickup = new Pickup(pickupHandle);

                        pickups.Add(pickup);
                    }
                });
            }
            await Task.FromResult(0);
        }
        #endregion

        /// <summary>
        /// External API for getting current fuel leve;
        /// </summary>
        /// <returns></returns>
        public float ExportsGetCurrentFuelLevel()
        {
            if (!PlayerVehicleViableForFuel())
            {
                return -1f;
            }

            return Game.PlayerPed.CurrentVehicle.FuelLevel;
        }

        /// <summary>
        /// External API for adding fuel
        /// </summary>
        /// <param name="amount"></param>
        public void ExportsAddFuel(float amount)
        {
            if (PlayerVehicleViableForFuel())
            {
                var vehicle = Game.PlayerPed.CurrentVehicle;

                VehicleSetFuelLevel(vehicle, vehicle.FuelLevel + amount);
            }
        }

        /// <summary>
        /// External API for setting fuel level
        /// </summary>
        /// <param name="amount"></param>
        public void ExportsSetFuel(float amount)
        {
            if (PlayerVehicleViableForFuel())
            {
                var vehicle = Game.PlayerPed.CurrentVehicle;

                VehicleSetFuelLevel(vehicle, amount);
            }
        }

        /// <summary>
        /// Returns gas station position that is in range
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rangeSquared"></param>
        /// <returns></returns>
        public int GetGasStationIndexInRange(Vector3 pos, float rangeSquared)
        {
            for (int i = 0; i < blips.Length; i++)
            {
                Blip blip = blips[i];

                if (Vector3.DistanceSquared(GasStations.positions[i], pos) < rangeSquared)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns "adequate" vehicle's petrol tank position
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public Vector3 GetVehicleTankPos(Vehicle vehicle)
        {
            EntityBone bone = null;

            foreach (var boneName in tankBones)
            {
                var boneIndex = GetEntityBoneIndexByName(vehicle.Handle, boneName);

                bone = vehicle.Bones[boneIndex];

                if (bone.IsValid)
                {
                    break;
                }
            }

            if (bone == null)
            {
                return vehicle.Position;
            }

            return bone.Position;
        }

        /// <summary>
        /// Checks if vehicle is within range of activation
        /// for any pump at current gas station
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public bool IsVehicleNearAnyPump(Vehicle vehicle)
        {
            var fuelTankPos = GetVehicleTankPos(vehicle);

            foreach (var pump in GasStations.pumps[currentGasStationIndex])
            {
                if (Vector3.DistanceSquared(pump, fuelTankPos) <= 20f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processes fuel consumption
        /// </summary>
        /// <param name="vehicle"></param>
        public void ConsumeFuel(Vehicle vehicle)
        {
            float fuel = VehicleFuelLevel(vehicle);

            // Consuming
            if (fuel > 0 && vehicle.IsEngineRunning)
            {
                float normalizedRPMValue = (float)Math.Pow(vehicle.CurrentRPM, 1.5);
                float consumedFuel = 0f;

                consumedFuel += normalizedRPMValue * fuelRPMImpact;
                consumedFuel += vehicle.Acceleration * fuelAccelerationImpact;
                consumedFuel += vehicle.MaxTraction * fuelTractionImpact;

                fuel -= consumedFuel * fuelConsumptionRate;
                fuel = fuel < 0f ? 0f : fuel;
            }

#if MANUAL_ENGINE_CUTOFF
            if (fuel == 0f && vehicle.IsEngineRunning)
            {
                vehicle.IsEngineRunning = false;
            }
#endif

            // Refueling at gas station
            if (
              // If we have gas station near us
              currentGasStationIndex != -1 &&
              // And near any pump
              IsVehicleNearAnyPump(vehicle)
            )
            {
#if MANUAL_ENGINE_CUTOFF
                if (vehicle.Speed < 0.1f && fuel != 0)
#endif
                if (vehicle.Speed < 0.1f)
                {
                    ControlEngine(vehicle);
                }

                if (vehicle.IsEngineRunning)
                {
                    hud.InstructTurnOffEngine();
                }
                else
                {
                    hud.InstructRefuelOrTurnOnEngine();

                    if (refuelAllowed)
                    {
                        if (Game.IsControlPressed(0, Control.Jump))
                        {
                            if (fuel < fuelTankCapacity)
                            {
                                float fuelPortion = 0.1f * refuelRate;

                                fuel += fuelPortion;
                                addedFuelCapacitor += fuelPortion;
                                TriggerEvent("frfuel:fuelAddedContinuous", fuelPortion, fuel, fuelTankCapacity);
                            }
                        }

                        if (Game.IsControlJustReleased(0, Control.Jump) && addedFuelCapacitor > 0f)
                        {
                            TriggerEvent("frfuel:fuelAdded", addedFuelCapacitor);
                            TriggerServerEvent("frfuel:fuelAdded", addedFuelCapacitor);
                            addedFuelCapacitor = 0f;
                        }
                    }
                }

                hud.RenderInstructions();
                PlayHUDAppearSound();
                hudActive = true;
            }
            else
            {
#if MANUAL_ENGINE_CUTOFF
                if (fuel != 0f && !vehicle.IsEngineRunning)
                {
                    vehicle.IsEngineRunning = true;
                }
#endif

                hudActive = false;
            }

            VehicleSetFuelLevel(vehicle, fuel);
        }

        /// <summary>
        /// Controls engine
        /// </summary>
        /// <param name="vehicle"></param>
        public void ControlEngine(Vehicle vehicle)
        {
            // Prevent the player from honking the horn whenever trying to toggle the engine.
            if (engineToggleControl == Control.VehicleHorn)
            {
                Game.DisableControlThisFrame(0, Control.VehicleHorn);

                // Also disable the rocket boost control for DLC cars.
                Game.DisableControlThisFrame(0, (Control)351); // INPUT_VEH_ROCKET_BOOST (E on keyboard, L3 on controller)
            }

            if (Game.IsControlJustReleased(0, engineToggleControl) && !Game.IsControlPressed(0, Control.Jump))
            {
                if (vehicle.IsEngineRunning)
                {
                    vehicle.IsDriveable = false;
                    //vehicle.IsEngineRunning = false;
                    API.SetVehicleEngineOn(vehicle.Handle, false, true, true); // temporary fix for when the engine keeps turning back on.
                }
                else
                {
                    vehicle.IsDriveable = true;
                    // FIXME: No neat default behaviour in 1103 :c
                    vehicle.IsEngineRunning = true;
                }
            }
        }


        /// <summary>
        /// Renders fuel bar and marker
        /// </summary>
        /// <param name="playerPed"></param>
        public void RenderUI(Ped playerPed)
        {
            var gasStationIndex = GetGasStationIndexInRange(playerPed.Position, showMarkerInRangeSquared);

            if (gasStationIndex != -1)
            {
                if (gasStationIndex != currentGasStationIndex)
                {
                    // Found gas station in range
                    currentGasStationIndex = gasStationIndex;
                }
            }
            else
            {
                if (currentGasStationIndex != -1)
                {
                    // Lost gas station in range
                    currentGasStationIndex = -1;
                }
            }

            if (showHud && IsHudPreferenceSwitchedOn())
            {
                // If not near any pump.
                if (currentGasStationIndex == -1)
                {
                    // If the engine is on, then display it no matter the config option.
                    if (playerPed.CurrentVehicle.IsEngineRunning)
                    {
                        hud.RenderBar(playerPed.CurrentVehicle.FuelLevel, fuelTankCapacity);
                    }
                    // If the engine is not running, then only display it if ShowHudWhenEngineOff is true.
                    else if (showHudWhenEngineOff)
                    {
                        hud.RenderBar(playerPed.CurrentVehicle.FuelLevel, fuelTankCapacity);
                    }
                }
                // If near a pump, always display the hud bar.
                else
                {
                    hud.RenderBar(playerPed.CurrentVehicle.FuelLevel, fuelTankCapacity);
                }
            }
        }

        /// <summary>
        /// Inits fuel for given vehicle
        /// </summary>
        /// <param name="vehicle"></param>
        public void InitFuel(Vehicle vehicle)
        {
            currentVehicleFuelLevelInitialized = true;

            fuelTankCapacity = VehicleMaxFuelLevel(vehicle);

            if (!vehicle.HasDecor(fuelLevelPropertyName))
            {
                vehicle.SetDecor(
                    fuelLevelPropertyName,
                    RandomizeFuelLevel(fuelTankCapacity)
                );
            }

            vehicle.FuelLevel = vehicle.GetDecor<float>(fuelLevelPropertyName);
        }

        /// <summary>
        /// Returns random fuel level between 1/3 and 3/4 of tank capacity
        /// </summary>
        /// <param name="fuelLevel"></param>
        /// <returns></returns>
        public float RandomizeFuelLevel(float fuelLevel)
        {
            float min = fuelLevel / 3f;
            float max = fuelLevel - (fuelLevel / 4);

            return (float)((random.NextDouble() * (max - min)) + min);
        }

        /// <summary>
        /// Correctly sets vehicle's fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="fuelLevel"></param>
        public void VehicleSetFuelLevel(Vehicle vehicle, float fuelLevel)
        {
            float max = VehicleMaxFuelLevel(vehicle);

            if (fuelLevel > max)
            {
                fuelLevel = max;
            }

            vehicle.FuelLevel = fuelLevel;
            vehicle.SetDecor(fuelLevelPropertyName, fuelLevel);
        }

        /// <summary>
        /// Returns vehicle's current fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public float VehicleFuelLevel(Vehicle vehicle)
        {
            if (vehicle.HasDecor(fuelLevelPropertyName))
            {
                return vehicle.GetDecor<float>(fuelLevelPropertyName);
            }
            else
            {
                return 65f;
            }
        }

        /// <summary>
        /// Returns vehicle's max fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public float VehicleMaxFuelLevel(Vehicle vehicle)
        {
            return GetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fPetrolTankVolume");
        }

        /// <summary>
        /// Handles manual vehicle refueling using Jerry Can
        /// </summary>
        /// <param name="playerPed"></param>
        public void ManualRefuel(Ped playerPed)
        {
            if (playerPed.Weapons.Current.Hash == WeaponHash.PetrolCan)
            {
                Vector3 pos = playerPed.Position;

                if (IsAnyVehicleNearPoint(pos.X, pos.Y, pos.Z, 3f))
                {
                    Vehicle vehicle = World.GetAllVehicles().OrderBy(v => v.Position.DistanceToSquared(pos)).First();

                    if (
                      vehicle != null &&
                      vehicle.Exists() &&
                      vehicle.HasDecor(fuelLevelPropertyName)
                    )
                    {
                        float max = VehicleMaxFuelLevel(vehicle);
                        float current = VehicleFuelLevel(vehicle);

                        if (max - current < 0.5f)
                        {
                            hud.InstructManualRefuel("Fuel tank is full");
                        }
                        else
                        {
                            hud.InstructManualRefuel("Manual refueling");
                        }

                        if (Game.IsControlPressed(0, Control.Attack))
                        {
                            jerryCanAnimation.Magick(playerPed);

                            if (current < max)
                            {
                                if (current + 0.1f >= max)
                                {
                                    VehicleSetFuelLevel(vehicle, max);
                                }
                                else
                                {
                                    VehicleSetFuelLevel(vehicle, current + 0.2f);
                                }
                            }
                        }

                        if (Game.IsControlJustReleased(0, Control.VehicleAttack))
                        {
                            jerryCanAnimation.RewindAndStop(playerPed);
                        }

                        hud.RenderInstructions();
                        PlayHUDAppearSound();
                        hudActive = true;
                        return;
                    }
                }
            }
            hudActive = false;
        }

        protected void PlayHUDAppearSound()
        {
            if (hudActive == false)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CONFIRM_BEEP", "HUD_MINI_GAME_SOUNDSET", 1);
            }
        }

        protected bool PlayerVehicleViableForFuel()
        {
            Ped playerPed = Game.PlayerPed;
            Vehicle vehicle = playerPed.CurrentVehicle;

            return playerPed.IsInVehicle() &&
              (
                vehicle.Model.IsCar ||
                vehicle.Model.IsBike ||
                vehicle.Model.IsQuadbike
              ) &&
              vehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed &&
              vehicle.IsAlive;
        }

        /// <summary>
        /// On tick
        /// </summary>
        /// <returns></returns>
        public async Task OnTick()
        {
            if (!initialized)
            {
                initialized = true;

                LoadConfig();

                CreateBlips();
            }

            await ManageNearbyJerryCanPickUps();

            hud.ReloadScaleformMovie();

            Ped playerPed = Game.PlayerPed;
            Vehicle vehicle = playerPed.CurrentVehicle;

            if (PlayerVehicleViableForFuel())
            {

                if (lastVehicle != vehicle)
                {
                    lastVehicle = vehicle;
                    currentVehicleFuelLevelInitialized = false;
                }

                if (!currentVehicleFuelLevelInitialized && initVehicles)
                {
                    InitFuel(vehicle);
                }

                ConsumeFuel(vehicle);
                RenderUI(playerPed);
            }
            else
            {
                ManualRefuel(playerPed);

                currentVehicleFuelLevelInitialized = false;
            }

            await Task.FromResult(0);
        }
    }
}
