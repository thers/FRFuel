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
    public static Vector3[] gasStations = {
      new Vector3(-360.8101f, 2858.62f,   43.324f),
      new Vector3(49.41872f,  2778.793f,  58.04395f),
      new Vector3(263.8949f,  2606.463f,  44.98339f),
      new Vector3(1039.958f,  2671.134f,  39.55091f),
      new Vector3(1207.26f,   2660.175f,  37.89996f),
      new Vector3(2539.685f,  2594.192f,  37.94488f),
      new Vector3(2679.858f,  3263.946f,  55.24057f),
      new Vector3(2692.521f,  3269.72f,   55.24056f),
      new Vector3(2692.521f,  3269.72f,   55.24056f),
      new Vector3(2005.055f,  3773.887f,  32.40393f),
      new Vector3(1687.156f,  4929.392f,  42.07809f),
      new Vector3(1701.314f,  6416.028f,  32.76395f),
      new Vector3(154.8158f,  6629.454f,  31.83573f),
      new Vector3(179.8573f,  6602.839f,  31.86817f),
      new Vector3(-94.46199f, 6419.594f,  31.48952f),
      new Vector3(-2554.996f, 2334.402f,  33.07803f),
      new Vector3(-1800.375f, 803.6619f,  138.6512f),
      new Vector3(-1437.622f, -276.7476f, 46.20771f),
      new Vector3(-2096.243f, -320.2867f, 13.16857f),
      new Vector3(-724.6192f, -935.1631f, 19.21386f),
      new Vector3(-526.0198f, -1211.003f, 18.18483f),
      new Vector3(-70.21484f, -1761.792f, 29.53402f),
      new Vector3(265.6484f,  -1261.309f, 29.29294f),
      new Vector3(819.6538f,  -1028.846f, 26.40342f),
      new Vector3(1208.951f,  -1402.567f, 35.22419f),
      new Vector3(1181.381f,  -330.8471f, 69.31651f),
      new Vector3(620.8434f,  269.1009f,  103.0895f),
      new Vector3(2581.321f,  362.0393f,  108.4688f)
    };

    protected Blip[] blips;

    // REMOVE AT RELEASE
    protected Vehicle defaultCar;
    // REMOVE AT RELEASE

    protected float fuelTankCapacity = 65f;
    protected float fuel = 30f;
    
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

    public DevUI devUI;

    public float showMarkerInRangeSquared = 2500f;
    public Blip lastBlipInRange;

    public Vector3 markerPutDown = new Vector3(0f, 0f, 3f);
    public Vector3 markerDir = new Vector3();
    public Vector3 markerRot = new Vector3();
    public Vector3 markerScale = new Vector3(15f);
    public Color markerColour = Color.FromArgb(50, 255, 179, 0);

    public Text helpTextRefuel = new Text(
      "Hold Space to refuel",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextTurnOff = new Text(
      "Press ~3~L to stop engine",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextTurnOn = new Text(
      "Press ~3~L to start engine",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public FRFuel()
    {
      // REMOVE AT RELEASE
      EventHandlers["playerSpawned"] += new Action<dynamic>(onPlayerSpawned);
      // REMOVE AT RELEASE

      blips = new Blip[gasStations.Length];

      devUI = new DevUI();
      
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

      CreateBlips();
    }

    public void CreateBlips()
    {
      for (int i = 0; i < gasStations.Length; i++)
      {
        var blip = World.CreateBlip(gasStations[i]);
        blip.Sprite = BlipSprite.JerryCan;
        blip.Color = BlipColor.White;
        blip.Scale = 1f;
        blip.IsShortRange = true;
        blip.Name = "Gas Station";

        blips[i] = blip;
      }
    }

    public Blip GetBlipInRange(Vector3 pos, float rangeSquared)
    {
      for (int i = 0; i < blips.Length; i++)
      {
        Vector3 blipPos = blips[i].Position;

        if (Vector3.DistanceSquared(blipPos, pos) < rangeSquared)
        {
          return blips[i];
        }
      }

      return null;
    }

    public void ProcessFuel(Vehicle vehicle)
    {
      if (fuel > 0 && vehicle.IsEngineRunning)
      {
        var normalizedRPMValue = (float) Math.Pow(vehicle.CurrentRPM, 1.5);

        fuel -= normalizedRPMValue * fuelRPMImpact;
        fuel -= vehicle.Acceleration * fuelAccelerationImpact;
        fuel -= vehicle.MaxTraction * fuelTractionImpact;
      }

      // If we have a close gas station
      if (lastBlipInRange != null)
      {
        // And ped is in range of 14
        if (Vector3.DistanceSquared(lastBlipInRange.Position, vehicle.Position) <= 80f)
        {
          if (vehicle.IsEngineRunning)
          {
            helpTextTurnOff.Draw();

            if (Game.IsControlJustReleased(0, Control.CinematicSlowMo))
            {
              vehicle.IsEngineRunning = false;
              vehicle.IsDriveable = false;
            }
          }
          else
          {
            if (fuelTankCapacity - fuel < 2f)
            {
              helpTextTurnOn.Draw();
            }
            else
            {
              helpTextRefuel.Draw();
            }

            if (Game.IsControlJustReleased(0, Control.CinematicSlowMo))
            {
              vehicle.IsDriveable = true;
            }

            if (Game.IsControlPressed(0, Control.Jump))
            {
              if (fuel < fuelTankCapacity)
              {
                fuel += 0.1f;
              }

              if (fuel > fuelTankCapacity)
              {
                fuel = fuelTankCapacity;
              }
            }
          }
        }
      }

      vehicle.FuelLevel = fuel;

      fuelBar.Size = new SizeF(
        (fuelBarWidth / 100f) * ((100f / fuelTankCapacity) * fuel),
        fuelBarHeight
      );
    }

    public async Task RenderBar()
    {
      devUI.OnTick();

      var playerPed = Game.PlayerPed;

      if (playerPed.IsInVehicle())
      {
        ProcessFuel(playerPed.CurrentVehicle);

        fuelBarBackdrop.Draw();
        fuelBarBack.Draw();
        fuelBar.Draw();

        Blip blipInRange = GetBlipInRange(playerPed.Position, showMarkerInRangeSquared);

        if (blipInRange != null)
        {
          World.DrawMarker(
            MarkerType.VerticalCylinder,
            blipInRange.Position - markerPutDown,
            markerDir,
            markerRot,
            markerScale,
            markerColour
          );

          if (blipInRange != lastBlipInRange)
          {
            // Found blip in range
            lastBlipInRange = blipInRange;
          }
        }
        else
        {
          if (lastBlipInRange != null)
          {
            // Lost blip in range
            lastBlipInRange = null;
          }
        }
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

      Screen.ShowNotification("Here's your car");
    }
    // REMOVE AT RELEASE
  }
}
