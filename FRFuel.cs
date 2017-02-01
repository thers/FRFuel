using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using System.Drawing;

namespace FRFuel {
  public class FRFuel : BaseScript {

    #region Fields
    public static string fuelLevelPropertyName = "_Fuel_Level";
    public static string manualRefuelAnimDict = "weapon@w_sp_jerrycan";

    protected Blip[] blips;
    protected Pickup[] pickups;

    protected float fuelTankCapacity = 65f;

    protected float fuelAccelerationImpact = 0.0002f;
    protected float fuelTractionImpact = 0.0001f;
    protected float fuelRPMImpact = 0.0005f;

    protected Random random = new Random();

    public HUD hud;

#if DEBUG
    public Dev.DevMenu menu;
#endif

    public float showMarkerInRangeSquared = 2500f;
    public Blip currentGasStation;

    protected Vehicle lastVehicle;
    protected bool currentVehicleFuelLevelInitialized = false;
    protected bool hudActive = false;

    protected InLoopOutAnimation jerryCanAnimation;
    #endregion

    public FRFuel() {
#if DEBUG
      menu = new Dev.DevMenu();
#endif
      hud = new HUD();

      jerryCanAnimation = new InLoopOutAnimation(
        new Animation(manualRefuelAnimDict, "fire_intro"),
        new Animation(manualRefuelAnimDict, "fire"),
        new Animation(manualRefuelAnimDict, "fire_outro")
      );

      EventHandlers["onClientMapStart"] += new Action<dynamic>((dynamic res) => {
        CreateBlips();
        CreateJerryCanPickUps();
      });

      blips = new Blip[GasStations.positions.Length];
      pickups = new Pickup[GasStations.positions.Length];

      CreateBlips();
      CreateJerryCanPickUps();

      Tick += OnTick;

      EntityDecoration.RegisterProperty(fuelLevelPropertyName, DecorationType.Float);
    }

    #region Init
    /// <summary>
    /// Creates blips for gas stations
    /// </summary>
    public void CreateBlips() {
      for (int i = 0; i < GasStations.positions.Length; i++) {
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
    /// Creates jerry cans pick-ups at the gas stations
    /// </summary>
    public void CreateJerryCanPickUps() {
      int model = 883325847;

      Function.Call(Hash.REQUEST_MODEL, model);

      for (int i = 0; i < GasStations.positions.Length; i++) {
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
    /// Returns blip within given squared range
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="rangeSquared"></param>
    /// <returns></returns>
    public Blip GetBlipInRange(Vector3 pos, float rangeSquared) {
      for (int i = 0; i < blips.Length; i++) {
        Blip blip = blips[i];

        if (Vector3.DistanceSquared(blip.Position, pos) < rangeSquared) {
          return blip;
        }
      }

      return null;
    }

    /// <summary>
    /// Processes fuel consumption
    /// </summary>
    /// <param name="vehicle"></param>
    public void ConsumeFuel(Vehicle vehicle) {
      float fuel = vehicle.FuelLevel;

      // Consuming
      if (fuel > 0 && vehicle.IsEngineRunning) {
        float normalizedRPMValue = (float) Math.Pow(vehicle.CurrentRPM, 1.5);

        fuel -= normalizedRPMValue * fuelRPMImpact;
        fuel -= vehicle.Acceleration * fuelAccelerationImpact;
        fuel -= vehicle.MaxTraction * fuelTractionImpact;
      }

      // Refueling at gas station
      if (
        // If we have gas station near us
        currentGasStation != null &&
        // And ped is in range of sqrt(80) to it
        Vector3.DistanceSquared(currentGasStation.Position, vehicle.Position) <= 80f
      ) {
        if (vehicle.Speed < 0.1f) {
          ControlEngine(vehicle);
        }

        if (vehicle.IsEngineRunning) {
          hud.InstructTurnOffEngine();
        } else {
          hud.InstructRefuelOrTurnOnEngine();

          if (Game.IsControlPressed(0, Control.Jump)) {
            if (fuel < fuelTankCapacity) {
              fuel += 0.1f;
            }
          }
        }

        hud.RenderInstructions();
        PlayHUDAppearSound();
        hudActive = true;
      } else {
        hudActive = false;
      }

      VehicleSetFuelLevel(vehicle, fuel);
    }

    /// <summary>
    /// Controls engine
    /// </summary>
    /// <param name="vehicle"></param>
    public void ControlEngine(Vehicle vehicle) {
      if (Game.IsControlJustReleased(0, Control.VehicleHorn)) {
        if (vehicle.IsEngineRunning) {
          vehicle.IsDriveable = false;
          vehicle.IsEngineRunning = false;
        } else {
          vehicle.IsDriveable = true;
        }
      }
    }


    /// <summary>
    /// Renders fuel bar and marker
    /// </summary>
    /// <param name="playerPed"></param>
    public void RenderUI(Ped playerPed) {
      hud.RenderBar(playerPed.CurrentVehicle.FuelLevel, fuelTankCapacity);

      Blip blipInRange = GetBlipInRange(playerPed.Position, showMarkerInRangeSquared);

      if (blipInRange != null) {
        hud.RenderMarker(blipInRange.Position);

        if (blipInRange != currentGasStation) {
          // Found blip in range
          currentGasStation = blipInRange;
        }
      } else {
        if (currentGasStation != null) {
          // Lost blip in range
          currentGasStation = null;
        }
      }
    }

    /// <summary>
    /// Inits fuel for given vehicle
    /// </summary>
    /// <param name="vehicle"></param>
    public void InitFuel(Vehicle vehicle) {
      currentVehicleFuelLevelInitialized = true;

      if (VehiclesPetrolTanks.Has(vehicle)) {
        fuelTankCapacity = VehiclesPetrolTanks.Get(vehicle);
      } else {
        fuelTankCapacity = 65f;
      }

      if (!EntityDecoration.ExistOn(vehicle, fuelLevelPropertyName)) {
        EntityDecoration.Set(
          vehicle,
          fuelLevelPropertyName,
          RandomizeFuelLevel(fuelTankCapacity)
        );
      }

      vehicle.FuelLevel = EntityDecoration.Get<float>(vehicle, fuelLevelPropertyName);
    }

    /// <summary>
    /// Returns random fuel level between 1/3 and 3/4 of tank capacity
    /// </summary>
    /// <param name="fuelLevel"></param>
    /// <returns></returns>
    public float RandomizeFuelLevel(float fuelLevel) {
      float min = fuelLevel / 3f;
      float max = fuelLevel - (fuelLevel / 4);

      return (float) ((random.NextDouble() * (max - min)) + min);
    }

    /// <summary>
    /// Correctly sets vehicle's fuel level
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="fuelLevel"></param>
    public void VehicleSetFuelLevel(Vehicle vehicle, float fuelLevel) {
      float max = VehicleMaxFuelLevel(vehicle);

      if (fuelLevel > max) {
        fuelLevel = max;
      }

      vehicle.FuelLevel = fuelLevel;
      EntityDecoration.Set(vehicle, fuelLevelPropertyName, fuelLevel);
    }

    /// <summary>
    /// Returns vehicle's current fuel level
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public float VehicleFuelLevel(Vehicle vehicle) {
      if (EntityDecoration.ExistOn(vehicle, fuelLevelPropertyName)) {
        return EntityDecoration.Get<float>(vehicle, fuelLevelPropertyName);
      } else {
        return 65f;
      }
    }

    /// <summary>
    /// Returns vehicle's max fuel level
    /// </summary>
    /// <param name="vehicle"></param>
    /// <returns></returns>
    public float VehicleMaxFuelLevel(Vehicle vehicle) {
      if (VehiclesPetrolTanks.Has(vehicle)) {
        return VehiclesPetrolTanks.Get(vehicle);
      } else {
        return 65f;
      }
    }

    /// <summary>
    /// Handles manual vehicle refueling using Jerry Can
    /// </summary>
    /// <param name="playerPed"></param>
    public void ManualRefuel(Ped playerPed) {
      if (playerPed.Weapons.Current.Hash == WeaponHash.PetrolCan) {
        Vector3 pos = playerPed.Position;

        int vehicleHandle = Function.Call<int>(Hash.GET_CLOSEST_VEHICLE, pos.X, pos.Y, pos.Z, 3f, 0, 70);
        Vehicle vehicle = new Vehicle(vehicleHandle);

        if (
          vehicleHandle != 0 &&
          EntityDecoration.ExistOn(vehicle, fuelLevelPropertyName)
        ) {
          float max = VehicleMaxFuelLevel(vehicle);
          float current = VehicleFuelLevel(vehicle);

          if (max - current < 0.5f) {
            hud.InstructManualRefuel("Fuel tank is full");
          } else {
            hud.InstructManualRefuel("Manual refueling");
          }

          if (Game.IsControlPressed(0, Control.Attack)) {
            jerryCanAnimation.Magick(playerPed);

            if (current < max) {
              if (current + 0.1f >= max) {
                VehicleSetFuelLevel(vehicle, max);
              } else {
                VehicleSetFuelLevel(vehicle, current + 0.2f);
              }
            }
          }

          if (Game.IsControlJustReleased(0, Control.VehicleAttack)) {
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

    protected void PlayHUDAppearSound() {
      if (hudActive == false) {
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CONFIRM_BEEP", "HUD_MINI_GAME_SOUNDSET", 1);
      }
    }

    /// <summary>
    /// On tick
    /// </summary>
    /// <returns></returns>
    public async Task OnTick() {
      hud.ReloadScaleformMovie();

#if DEBUG
      menu.OnTick();
#endif

      Ped playerPed = Game.PlayerPed;

      if (
        playerPed.IsInVehicle() &&
        (
          playerPed.CurrentVehicle.Model.IsCar ||
          playerPed.CurrentVehicle.Model.IsBike ||
          playerPed.CurrentVehicle.Model.IsQuadbike
        ) &&
        playerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed &&
        playerPed.CurrentVehicle.IsAlive
      ) {
        Vehicle vehicle = playerPed.CurrentVehicle;
        
        if (lastVehicle != vehicle) {
          lastVehicle = vehicle;
          currentVehicleFuelLevelInitialized = false;
        }

        if (!currentVehicleFuelLevelInitialized) {
          InitFuel(vehicle);
        }

        ConsumeFuel(vehicle);
        RenderUI(playerPed);

      } else {
        ManualRefuel(playerPed);

        currentVehicleFuelLevelInitialized = false;
      }

      await Task.FromResult(0);
    }
  }
}
