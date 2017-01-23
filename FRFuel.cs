using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Drawing;

namespace FRFuel
{
  public class FRFuel: BaseScript
  {
    // REMOVE AT RELEASE
    protected Vehicle defaultCar;
    // REMOVE AT RELEASE

    protected float KPHMultiplier = 3.6f;
    protected float MPHMultiplier = 2.2f;

    protected float fuelTankCapacity = 65f;
    protected float fuel = 65f;
    
    protected float fuelAccelerationImpact = 0.0002f;
    protected float fuelTractionImpact = 0.0001f;
    protected float fuelRPMImpact = 0.0005f;

    protected float fuelBarWidth = 180f;
    protected float fuelBarHeight = 6f;

    protected Color fuelBarColourNormal;
    protected Color fuelBarColourWarning;

    protected Rectangle fuelBarBackdrop;
    protected Rectangle fuelBarBack;
    protected Rectangle fuelBar;

    public FRFuel()
    {
      // REMOVE AT RELEASE
      EventHandlers["playerSpawned"] += new Action<dynamic>(onPlayerSpawned);
      // REMOVE AT RELEASE
      
      Tick += RenderBar;

      var fuelBarBackdropPosition = new PointF(20f, 569f); // Right above the radar HUD
      var fuelBarBackPosition = new PointF(20f, 572f);
      var fuelBarPosition = fuelBarBackPosition;

      var fuelBarBackdropSize = new SizeF(fuelBarWidth, 12f);
      var fuelBarBackSize = new SizeF(fuelBarWidth, fuelBarHeight);
      var fuelBarSize = fuelBarBackSize;

      var fuelBarBackdropColour = Color.FromArgb(100, 0, 0, 0);
      var fuelBarBackColour = Color.FromArgb(50, 255, 179, 0);

      fuelBarColourNormal = Color.FromArgb(100, 255, 179, 0);
      fuelBarColourNormal = Color.FromArgb(255, 255, 179, 0);

      fuelBarBackdrop = new Rectangle(fuelBarBackdropPosition, fuelBarBackdropSize, fuelBarBackdropColour);
      fuelBarBack = new Rectangle(fuelBarBackPosition, fuelBarBackSize, fuelBarBackColour);
      fuelBar = new Rectangle(fuelBarPosition, fuelBarSize, fuelBarColourNormal);
    }

    public void ProcessFuel()
    {
      var playerPed = Game.PlayerPed;

      if (playerPed.IsInVehicle())
      {
        if (fuel > 0)
        {
          var normalizedRPMValue = (float) Math.Pow(playerPed.CurrentVehicle.CurrentRPM, 1.5);

          fuel -= normalizedRPMValue * fuelRPMImpact;
          fuel -= playerPed.CurrentVehicle.Acceleration * fuelAccelerationImpact;
          fuel -= playerPed.CurrentVehicle.MaxTraction * fuelTractionImpact;
        }

        playerPed.CurrentVehicle.FuelLevel = fuel;

        fuelBar.Size = new SizeF(
          (fuelBarWidth / 100f) * ((100f / fuelTankCapacity) * fuel),
          fuelBarHeight
        );
      }
    }

    public async Task RenderBar()
    {
      ProcessFuel();

      var playerPed = Game.PlayerPed;

      if (playerPed.IsInVehicle())
      {
        fuelBarBackdrop.Draw();
        fuelBarBack.Draw();
        fuelBar.Draw();
      }

      // REMOVE AT RELEASE
      else
      {
        if (Game.IsControlJustReleased(0, Control.Phone))
        {
          makeCarForPed();
        }
      }
      // REMOVE AT RELEASE

      await Task.FromResult(0);
    }

    // REMOVE AT RELEASE
    public void onPlayerSpawned(dynamic pos)
    {
      makeCarForPed();
    }

    public async Task makeCarForPed()
    {
      var ped = Game.PlayerPed;

      if (defaultCar == null || !defaultCar.IsAlive)
      {
        defaultCar = await World.CreateVehicle(VehicleHash.T20, ped.Position + new Vector3(1f, 2f, 2f));
        defaultCar.NeedsToBeHotwired = false;
        //defaultCar.Mods.LicensePlateStyle = LicensePlateStyle.NorthYankton;
        //defaultCar.Mods.LicensePlate = "ONII";
        Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, defaultCar.NativeValue, "ONII");
      }
      else
      {
        defaultCar.Position = ped.Position + new Vector3(1f, 2f, 2f);
      }
    }
    // REMOVE AT RELEASE
  }
}
