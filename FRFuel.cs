#undef MANUAL_ENGINE_CUTOFF

using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace FRFuel
{
    public class FRFuel : BaseScript
    {

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
        protected Blip[] Airblips;
        protected Blip[] Boatblips;
        protected Pickup[] pickups;

        protected float fuelTankCapacity = 100f;

        protected float fuelAccelerationImpact = 0.0002f;
        protected float fuelTractionImpact = 0.0001f;
        protected float fuelRPMImpact = 0.0005f;

        protected float AirFuelPlaneAccelerationImpact = 0.0002f;
        protected float AirFuelPlaneTractionImpact = 0.0003f;
        protected float AirFuelPlaneRPMImpact = 0.00005f;

        protected float AirFuelHeliAccelerationImpact = 0.0002f;
        protected float AirFuelHeliTractionImpact = 0.0002f;
        protected float AirFuelHeliRPMImpact = 0.0002f;

        protected float BoatFuelAccelerationImpact = 0.00005f;
        protected float BoatFuelTractionImpact = 0.00005f;
        protected float BoatFuelRPMImpact = 0.00005f;

        public float showMarkerInRangeSquared = 250f;

        public HUD hud;
        public Random random = new Random();

        private int currentGasStationIndex;
        private Vehicle lastVehicle;

        protected bool currentVehicleFuelLevelInitialized = false;
        protected bool hudActive = false;
        protected bool refuelAllowed = true;
        protected float addedFuelCapacitor = 0f;

        protected InLoopOutAnimation jerryCanAnimation;

        protected Vehicle LastVehicle { get => lastVehicle; set => lastVehicle = value; }

        protected Config Config { get; set; }
        protected bool showHud = true;

        protected bool initialized = false;
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

            blips = new Blip[GasStations.positions.Length];
            pickups = new Pickup[GasStations.positions.Length];
            Airblips = new Blip[GasStations.AircraftPos.Length];
            Boatblips = new Blip[GasStations.BoatPos.Length];


            Tick += OnTick;

            EntityDecoration.RegisterProperty(fuelLevelPropertyName, DecorationType.Float);
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
                configContent = Function.Call<string>(Hash.LOAD_RESOURCE_FILE, "frfuel", "config.ini");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Config file not found! Error:" + e);
            }

            Config = new Config(configContent);

            showHud = Config.Get("ShowHud", "true") == "true";

#if DEBUG
            Debug.WriteLine($"CreatePickups: {Config.Get("CreatePickups", "true")}");
            Debug.WriteLine($"ShowHud: {Config.Get("ShowHud", "true")}");
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

        public void CreateAirBlips()
        {
            if (Config.Get("CreateBlips", "true") != "true")
            {
                return;
            }

            for (int i = 0; i < GasStations.AircraftPos.Length; i++)
            {
                var blip = World.CreateBlip(GasStations.AircraftPos[i]);
                blip.Sprite = BlipSprite.JerryCan2;
                blip.Color = BlipColor.FranklinGreen;
                blip.Scale = 1f;
                blip.IsShortRange = true;
                blip.Name = "Aircraft Refueling";

                Airblips[i] = blip;
            }
        }

        public void CreateBoatBlips()
        {
            if (Config.Get("CreateBlips", "true") != "true")
            {
                return;
            }

            for (int i = 0; i < GasStations.BoatPos.Length; i++)
            {
                var blip = World.CreateBlip(GasStations.BoatPos[i]);
                blip.Sprite = BlipSprite.Speedboat;
                blip.Color = BlipColor.Blue;
                blip.Scale = 1f;
                blip.IsShortRange = true;
                blip.Name = "Boat Refueling";

                Boatblips[i] = blip;
            }
        }

        /// <summary>
        /// Creates jerry cans pick-ups at the gas stations
        /// </summary>
        public void CreateJerryCanPickUps()
        {
            if (Config.Get("CreatePickups", "true") != "true")
            {
                return;
            }

            int model = 883325847;

            Function.Call(Hash.REQUEST_MODEL, model);

            for (int i = 0; i < GasStations.positions.Length; i++)
            {
                Vector3 p = GasStations.positions[i];

                Pickup pickup = new Pickup(Function.Call<int>(
                  Hash.CREATE_PICKUP,
                  -962731009, // Petrol Can
                  p.X, p.Y, p.Z - 0.5f,
                  8, // Place on the ground
                  true, model
                ));

                pickups[i] = pickup;
            }
        }
        #endregion

        /// <summary>
        /// Returns gas station position that is in range
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rangeSquared"></param>
        /// <returns></returns>
        public int GetGasStationIndexInRange(Vector3 pos, float rangeSquared)
        {
            Ped playerPed = Game.PlayerPed;

            if (playerPed.CurrentVehicle.Model.IsBike || playerPed.CurrentVehicle.Model.IsCar || playerPed.CurrentVehicle.Model.IsQuadbike)
            {
                for (int i = 0; i < blips.Length; i++)
                {
                    Blip blip = blips[i];

                    if (Vector3.DistanceSquared(GasStations.positions[i], pos) < rangeSquared)
                    {
                        return i;
                    }
                }
            }
            else if (playerPed.CurrentVehicle.Model.IsPlane || playerPed.CurrentVehicle.Model.IsHelicopter)
            {
                for (int i = 0; i < Airblips.Length; i++)
                {
                    Blip blip = Airblips[i];

                    if (Vector3.DistanceSquared(GasStations.AircraftPos[i], pos) < rangeSquared)
                    {
                        return i;
                    }
                }
            }
            else if (playerPed.CurrentVehicle.Model.IsBoat)
            {
                for (int i = 0; i < Boatblips.Length; i++)
                {
                    Blip blip = Boatblips[i];

                    if (Vector3.DistanceSquared(GasStations.BoatPos[i], pos) < rangeSquared)
                    {
                        return i;
                    }
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
                var boneIndex = Function.Call<int>(
                    Hash.GET_ENTITY_BONE_INDEX_BY_NAME,
                    vehicle,
                    boneName
                );

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
            Ped playerPed = Game.PlayerPed;
            var fuelTankPos = GetVehicleTankPos(vehicle);
            if (playerPed.CurrentVehicle.Model.IsCar || playerPed.CurrentVehicle.Model.IsQuadbike || playerPed.CurrentVehicle.Model.IsBike)
            {
                foreach (var pump in GasStations.pumps[currentGasStationIndex])
                {
                    if (Vector3.DistanceSquared(pump, fuelTankPos) <= 20f)
                    {
                        return true;
                    }
                }
            }
            else if (playerPed.CurrentVehicle.Model.IsPlane || playerPed.CurrentVehicle.Model.IsHelicopter)
            {
                foreach (var pump in GasStations.AircraftPumps[currentGasStationIndex])
                {
                    if (Vector3.DistanceSquared(pump, fuelTankPos) <= 20f)
                    {
                        return true;
                    }
                }
            }
            else if (playerPed.CurrentVehicle.Model.IsBoat)
            {
                foreach (var pump in GasStations.BoatPumps[currentGasStationIndex])
                {
                    if (Vector3.DistanceSquared(pump, fuelTankPos) <= 35f)
                    {
                        return true;
                    }
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

            Ped playerPed = Game.PlayerPed;

            // Consuming
            if (fuel > 0 && vehicle.IsEngineRunning && (playerPed.CurrentVehicle.Model.IsCar || playerPed.CurrentVehicle.Model.IsBike || playerPed.CurrentVehicle.Model.IsQuadbike))
            {
                float normalizedRPMValue = (float)Math.Pow(vehicle.CurrentRPM, 1.5);

                fuel -= normalizedRPMValue * fuelRPMImpact;
                fuel -= vehicle.Acceleration * fuelAccelerationImpact;
                fuel -= vehicle.MaxTraction * fuelTractionImpact;

                fuel = fuel < 0f ? 0f : fuel;
            }

            if (fuel > 0 && vehicle.IsEngineRunning && playerPed.CurrentVehicle.Model.IsPlane)
            {
                float normalizedRPMValue = (float)Math.Pow(vehicle.CurrentRPM, 1.5);

                fuel -= normalizedRPMValue * AirFuelPlaneRPMImpact;
                fuel -= vehicle.Acceleration * AirFuelPlaneAccelerationImpact;
                fuel -= vehicle.MaxTraction * AirFuelPlaneTractionImpact;

                fuel = fuel < 0f ? 0f : fuel;
            }

            if (fuel > 0 && vehicle.IsEngineRunning && playerPed.CurrentVehicle.Model.IsHelicopter)
            {
                float normalizedRPMValue = (float)Math.Pow(vehicle.CurrentRPM, 1.5);

                fuel -= normalizedRPMValue * AirFuelHeliRPMImpact;
                fuel -= vehicle.Acceleration * AirFuelHeliAccelerationImpact;
                fuel -= vehicle.MaxTraction * AirFuelHeliTractionImpact;

                fuel = fuel < 0f ? 0f : fuel;
            }

            if (fuel > 0 && vehicle.IsEngineRunning && playerPed.CurrentVehicle.Model.IsBoat)
            {
                float normalizedRPMValue = (float)Math.Pow(vehicle.CurrentRPM, 1.5);

                fuel -= normalizedRPMValue * BoatFuelRPMImpact;
                fuel -= vehicle.Acceleration * BoatFuelAccelerationImpact;
                fuel -= vehicle.MaxTraction * BoatFuelTractionImpact;

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
                        
                        Game.DisableControlThisFrame(0, Control.VehicleFlyAttack);
                        Game.DisableControlThisFrame(0, Control.VehicleFlyAttack2);
                    
                        if (Game.IsControlPressed(0, Control.Jump))
                        {
                            if (fuel < fuelTankCapacity)
                            {
                                fuel += 0.1f;
                                addedFuelCapacitor += 0.1f;
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
            if (Game.IsControlJustReleased(0, Control.VehicleHorn) && !Game.IsControlPressed(0, Control.Jump))
            {
                if (vehicle.IsEngineRunning)
                {
                    vehicle.IsDriveable = false;
                    vehicle.IsEngineRunning = false;
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
            if (showHud)
            {
                hud.RenderBar(playerPed.CurrentVehicle.FuelLevel, fuelTankCapacity);
            }

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
                return 100f;
            }
        }

        /// <summary>
        /// Returns vehicle's max fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public float VehicleMaxFuelLevel(Vehicle vehicle)
        {
            return Function.Call<float>(
                (Hash)0x642FC12F,
                vehicle,
                "CHandlingData",
                "fPetrolTankVolume"
            );
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

                int vehicleHandle = Function.Call<int>(Hash.GET_CLOSEST_VEHICLE, pos.X, pos.Y, pos.Z, 3f, 0, 70);
                Vehicle vehicle = new Vehicle(vehicleHandle);

                if (
                  vehicleHandle != 0 &&
                  vehicle.HasDecor(fuelLevelPropertyName) &&
                  !vehicle.Model.IsPlane &&
                  !vehicle.Model.IsHelicopter
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

            hudActive = false;
        }

        protected void PlayHUDAppearSound()
        {
            if (hudActive == false)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CONFIRM_BEEP", "HUD_MINI_GAME_SOUNDSET", 1);
            }
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
                CreateAirBlips();
                CreateBoatBlips();
                CreateJerryCanPickUps();
            }

            hud.ReloadScaleformMovie();

            Ped playerPed = Game.PlayerPed;

            if (
              playerPed.IsInVehicle() &&
              (
                playerPed.CurrentVehicle.Model.IsCar ||
                playerPed.CurrentVehicle.Model.IsBike ||
                playerPed.CurrentVehicle.Model.IsQuadbike ||
                playerPed.CurrentVehicle.Model.IsHelicopter ||
                playerPed.CurrentVehicle.Model.IsPlane ||
                playerPed.CurrentVehicle.Model.IsBoat
              ) &&
              playerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed &&
              playerPed.CurrentVehicle.IsAlive
            )
            {
                Vehicle vehicle = playerPed.CurrentVehicle;

                if (lastVehicle != vehicle)
                {
                    lastVehicle = vehicle;
                    currentVehicleFuelLevelInitialized = false;
                }

                if (!currentVehicleFuelLevelInitialized)
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
