#undef MANUAL_ENGINE_CUTOFF

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace FRFuel
{
    public class FRFuel : BaseScript
    {
        delegate float GetCurrentFuelLevelDelegate();
        delegate void AddFuel(float amount);
        delegate void SetFuel(float amount);

        #region Fields
        private static string fuelLevelPropertyName = "_Fuel_Level";

        private static List<Pickup> _jerryCans = new List<Pickup>();
        private static string manualRefuelAnimDict = "weapon@w_sp_jerrycan";
        private static InLoopOutAnimation _jerryCanAnimation = new InLoopOutAnimation(
               new Animation(manualRefuelAnimDict, "fire_intro"),
               new Animation(manualRefuelAnimDict, "fire"),
               new Animation(manualRefuelAnimDict, "fire_outro")
             );

        private static string[] tankBones = new string[] {
            "petrolcap",
            "petroltank",
            "petroltank_r",
            "petroltank_l",
            "wheel_lr"
        };

        private static Random _random = new Random();
        private static bool hudActive = false;

        private static float addedFuelCapacitor = 0f;
        private bool refuelAllowed = true;

        public static Control engineToggleControl = Control.VehicleHorn;
        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        public FRFuel()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);

            EventHandlers["frfuel:refuelAllowed"] += new Action<dynamic>((dynamic toggle) =>
            {
                if (toggle.GetType() == typeof(bool))
                {
                    refuelAllowed = (bool)toggle;
                }
            });

            EntityDecoration.RegisterProperty(fuelLevelPropertyName, DecorationType.Float);

            Exports.Add("addFuel", new AddFuel(ExportsAddFuel));
            Exports.Add("setFuel", new SetFuel(ExportsSetFuel));
            Exports.Add("getCurrentFuelLevel", new GetCurrentFuelLevelDelegate(ExportsGetCurrentFuelLevel));


            PeriodicFuelCheck(); //dont await
            Tick += OnTick;
        }

        public static void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;

            if (Config.GetInstance().ShowBlips)
            {
                foreach (var station in GasStations.GetInstance())
                {

                    var blip = World.CreateBlip(station.Coordinates);
                    blip.Sprite = BlipSprite.JerryCan;
                    blip.Color = BlipColor.White;
                    blip.Scale = .75f;
                    blip.IsShortRange = true;
                    blip.Name = "Gas Station";
                }
            }

            if (Config.GetInstance().CreatePickups)
                SpawnJerryCansPeriodically(); //dont await
        }

        static async Task SpawnJerryCansPeriodically()
        {
            foreach (var station in GasStations.GetInstance())
            {
                _jerryCans.Add(new Pickup(API.CreatePickup(0xc69de3ff, station.Coordinates.X, station.Coordinates.Y, station.Coordinates.Z - 0.5f,
                        8 | 32, // Place on the ground, local only
                        0,
                        true,
                        883325847)));
            }

            while (true)
            {
                var replaceCans = _jerryCans.Where(p => p.IsCollected).ToList();
                _jerryCans.RemoveAll(x => replaceCans.Any(a => a.Handle == x.Handle));

                foreach (var pickup in replaceCans)
                {
                    var pos = pickup.Position;
                    pickup.Delete();
                    _jerryCans.Add(new Pickup(API.CreatePickup(0xc69de3ff, pos.X, pos.Y, pos.Z - 0.5f, 8 | 32, 0, true, 883325847)));
                }

                await BaseScript.Delay(1000 * 60 * 10); //Every 10 minutes
            }
        }

        /// <summary>
        /// On tick
        /// </summary>
        /// <returns></returns>
        public async Task OnTick()
        {
            ShowFuelBar();
            RefuelCheck();
            if (!Config.GetInstance().AllowManualRefills)
            {
                ManualRefuelCheck();
            }


            await Task.FromResult(0);
        }

        /// <summary>
        /// Processes fuel consumption
        /// </summary>
        /// <param name="vehicle"></param>
        public async Task PeriodicFuelCheck()
        {
            while (true)
            {
                if (Game.PlayerPed.IsInVehicle() && (Game.PlayerPed.CurrentVehicle.Model.IsCar || Game.PlayerPed.CurrentVehicle.Model.IsBike || Game.PlayerPed.CurrentVehicle.Model.IsQuadbike)
                    && Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == Game.PlayerPed && Game.PlayerPed.CurrentVehicle.IsAlive)
                {
                    if (!Game.PlayerPed.CurrentVehicle.HasDecor(fuelLevelPropertyName)) //init fuel
                    {
                        var _vehicleMaxFuel = GetVehicleFuelCapacity(Game.PlayerPed.CurrentVehicle);
                        SetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle, RandomizeFuelLevel(_vehicleMaxFuel));
                    }

                    var _vehicleFuel = GetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle);

                    if (Game.PlayerPed.CurrentVehicle.IsEngineRunning && _vehicleFuel > 0f)//Consume Fuel (Algorithm may need tweeked due to removal of tightly coupling fuel usage to frame rates)
                    {
                        float normalizedRPMValue = (float)Math.Pow(Game.PlayerPed.CurrentVehicle.CurrentRPM, 1.5);
                        float consumedFuel = 0f;

                        consumedFuel += normalizedRPMValue * Config.GetInstance().FuelRPMImpact;
                        consumedFuel += Game.PlayerPed.CurrentVehicle.Acceleration * Config.GetInstance().FuelAccelerationImpact;
                        consumedFuel += Game.PlayerPed.CurrentVehicle.MaxTraction * Config.GetInstance().FuelTractionImpact;
                        consumedFuel += Game.PlayerPed.CurrentVehicle.Speed * Config.GetInstance().FuelSpeedImpact;
                        consumedFuel *= Config.GetInstance().FuelConsumptionRate;

                        _vehicleFuel = Math.Max(0f, _vehicleFuel - consumedFuel);
                        SetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle, _vehicleFuel);
                    }
                }

                await BaseScript.Delay(500);//Check fuel usage every .5 seconds
            }
        }

        /// <summary>
        /// Checks for refuel ability
        /// </summary>
        /// <param name="vehicle"></param>
        public void RefuelCheck()
        {
            if (Game.PlayerPed.IsInVehicle() && refuelAllowed)
            {
                bool stationNear = GasStations.GetInstance().Any(x => x.Pumps.Any(p => Vector3.DistanceSquared(p, GetVehicleTankPos(Game.PlayerPed.CurrentVehicle)) <= 20f));
                // Refueling at gas station
                if (stationNear)
                {
                    if (Game.PlayerPed.CurrentVehicle.Speed < 0.1f)
                    {
                        ControlEngine(Game.PlayerPed.CurrentVehicle);
                    }

                    if (Game.PlayerPed.CurrentVehicle.IsEngineRunning)
                    {
                        HUD.InstructTurnOffEngine();
                    }
                    else
                    {
                        HUD.InstructRefuelOrTurnOnEngine();
                        if (Game.IsControlPressed(0, Control.Jump))
                        {
                            var _vehicleFuel = GetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle);
                            if (_vehicleFuel < GetVehicleFuelCapacity(Game.PlayerPed.CurrentVehicle))
                            {
                                float fuelPortion = 0.1f * Config.GetInstance().RefuelRate;

                                _vehicleFuel += fuelPortion;
                                addedFuelCapacitor += fuelPortion;
                                SetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle, _vehicleFuel);
                            }
                        }

                        if (Game.IsControlJustReleased(0, Control.Jump) && addedFuelCapacitor > 0f)
                        {
                            TriggerEvent("frfuel:fuelAdded", addedFuelCapacitor);
                            TriggerServerEvent("frfuel:fuelAdded", addedFuelCapacitor);
                            addedFuelCapacitor = 0f;
                        }
                    }

                    HUD.RenderInstructions();
                    PlayHUDAppearSound();
                    hudActive = true;
                }
                else
                {
#if MANUAL_ENGINE_CUTOFF
                if (_vehicleFuel != 0f && !Game.PlayerPed.CurrentVehicle.IsEngineRunning)
                {
                    Game.PlayerPed.CurrentVehicle.IsEngineRunning = true;
                }
#endif

                    hudActive = false;
                }
            }
        }

        /// <summary>
        /// Handles manual vehicle refueling using Jerry Can
        /// </summary>
        /// <param name="playerPed"></param>
        public void ManualRefuelCheck()
        {
            if (Game.PlayerPed.Weapons.Current.Hash != WeaponHash.PetrolCan)
                return;

            Vector3 pos = Game.PlayerPed.Position;

            if (API.IsAnyVehicleNearPoint(pos.X, pos.Y, pos.Z, 3f))
            {
                Vehicle vehicle = World.GetAllVehicles().OrderBy(v => v.Position.DistanceToSquared(pos)).First(); //this is an expensive check..

                if (vehicle != null && vehicle.Exists() && vehicle.HasDecor(fuelLevelPropertyName))
                {
                    var _vehicleFuel = GetVehicleFuelLevel(vehicle);
                    var _vehicleMaxFuel = GetVehicleFuelCapacity(vehicle);

                    if (_vehicleFuel >= _vehicleMaxFuel)
                        HUD.InstructManualRefuel("Fuel tank is full");
                    else
                        HUD.InstructManualRefuel("Manual refueling");

                    if (Game.IsControlPressed(0, Control.Attack))
                    {
                        _jerryCanAnimation.Magick(Game.PlayerPed);

                        if (_vehicleFuel < _vehicleMaxFuel)
                        {
                            _vehicleFuel += 0.2f;
                            SetVehicleFuelLevel(vehicle, _vehicleFuel);
                        }
                    }

                    if (Game.IsControlJustReleased(0, Control.VehicleAttack))
                    {
                        _jerryCanAnimation.RewindAndStop(Game.PlayerPed);
                    }

                    HUD.RenderInstructions();
                    PlayHUDAppearSound();
                    hudActive = true;
                    return;
                }

                hudActive = false;
            }

        }

        #region Helpers

        /// <summary>
        /// Returns "adequate" vehicle's petrol tank position
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public Vector3 GetVehicleTankPos(Vehicle vehicle)
        {
            foreach (var boneName in tankBones)
            {
                var boneFound = vehicle.Bones.FirstOrDefault(b => b.Index == API.GetEntityBoneIndexByName(vehicle.Handle, boneName));
                if (boneFound != null && boneFound.IsValid)
                    return boneFound.Position;
            }

            return vehicle.Position;
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

            return (float)((_random.NextDouble() * (max - min)) + min);
        }

        /// <summary>
        /// Returns vehicle's current fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public float GetVehicleFuelLevel(Vehicle vehicle)
        {
            if (vehicle.HasDecor(fuelLevelPropertyName))
            {
                return vehicle.GetDecor<float>(fuelLevelPropertyName);
            }
            else
            {
                return GetVehicleFuelCapacity(vehicle);
            }
        }

        /// <summary>
        /// Returns vehicle's current fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public float GetVehicleFuelCapacity(Vehicle vehicle)
        {
            return Config.GetInstance().FuelTankCapacityOverride ?? API.GetVehicleHandlingFloat(vehicle.Handle, "CHandlingData", "fPetrolTankVolume");
        }

        /// <summary>
        /// Correctly sets vehicle's fuel level
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="fuelLevel"></param>
        public void SetVehicleFuelLevel(Vehicle vehicle, float fuel)
        {
            vehicle.SetDecor(fuelLevelPropertyName, Math.Min(fuel, GetVehicleFuelCapacity(vehicle)));
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

        protected void ShowFuelBar()
        {
            if (Config.GetInstance().ShowHud && API.IsHudPreferenceSwitchedOn() && Game.PlayerPed.IsInVehicle())
            {
                if (!Config.GetInstance().ShowHudWhenEngineOff && !Game.PlayerPed.CurrentVehicle.IsEngineRunning)
                    return;

                HUD.ReloadScaleformMovie();
                HUD.RenderBar(GetVehicleFuelLevel(Game.PlayerPed.CurrentVehicle), GetVehicleFuelCapacity(Game.PlayerPed.CurrentVehicle));
            }
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
        #endregion Helpers

        #region Exports
        /// <summary>
        /// External API for getting current fuel leve;
        /// </summary>
        /// <returns></returns>
        public float ExportsGetCurrentFuelLevel()
        {
            return PlayerVehicleViableForFuel() ? Game.PlayerPed.CurrentVehicle.FuelLevel : 1f;
        }

        /// <summary>
        /// External API for adding fuel
        /// </summary>
        /// <param name="amount"></param>
        public void ExportsAddFuel(float amount)
        {
            if (PlayerVehicleViableForFuel())
            {
                Game.PlayerPed.CurrentVehicle?.SetDecor(fuelLevelPropertyName, Math.Min(Game.PlayerPed.CurrentVehicle.FuelLevel + amount, GetVehicleFuelCapacity(Game.PlayerPed.CurrentVehicle)));
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
                Game.PlayerPed.CurrentVehicle?.SetDecor(fuelLevelPropertyName, Math.Min(amount, GetVehicleFuelCapacity(Game.PlayerPed.CurrentVehicle)));
            }
        }
        #endregion Exports
    }
}
