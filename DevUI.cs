using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using NativeUI;

namespace FRFuel {
  public class DevUI {
    protected MenuPool menuPool;
    protected UIMenu mainMenu;

    protected UIMenuItem position;
    protected UIMenuItem isDriver;
    protected UIMenuItem vehicleModelId;
    protected UIMenuItem vehicleFuelTank;
    protected UIMenuItem knownVehicle;
    protected UIMenuItem netVehicleId;
    protected UIMenuItem netVehicleIdControl;
    protected UIMenuItem decoration;

    public DevUI() {
      menuPool = new MenuPool();
      mainMenu = new UIMenu("FRFuel dev menu", "things");

      position = new UIMenuItem("Pos");
      position.Enabled = false;

      vehicleModelId = new UIMenuItem("Vehicle model ID");
      vehicleModelId.Enabled = false;

      knownVehicle = new UIMenuItem("Is known vehicle");
      knownVehicle.Enabled = false;

      vehicleFuelTank = new UIMenuItem("Vehicle fuel tank");

      mainMenu.AddItem(position);
      mainMenu.AddItem(knownVehicle);
      mainMenu.AddItem(vehicleModelId);
      mainMenu.AddItem(vehicleFuelTank);

      mainMenu.OnItemSelect += (sende, item, index) => {
        if (item == vehicleFuelTank && Game.PlayerPed.IsInVehicle()) {
          BaseScript.TriggerServerEvent(
            "frfuel:dev:saveFuel",
            "{" + Game.PlayerPed.CurrentVehicle.Model.Hash.ToString() + ", " + Game.PlayerPed.CurrentVehicle.FuelLevel.ToString() + "f},"
          );
          Screen.ShowNotification("Fuel to model saved");
        }
      };

      menuPool.Add(mainMenu);
      menuPool.RefreshIndex();
    }

    public void OnTick() {
      position.Text = "Pos: " + Game.PlayerPed.Position.ToString();

      if (Game.PlayerPed.IsInVehicle()) {
        Vehicle vehicle = Game.PlayerPed.CurrentVehicle;

        knownVehicle.SetRightLabel(VehiclesPetrolTanks.dict.ContainsKey(vehicle.Model.Hash) ? "Yes" : "No");
        vehicleModelId.SetRightLabel(vehicle.DisplayName.ToString());
        vehicleFuelTank.SetRightLabel(vehicle.FuelLevel.ToString());
      } else {
        vehicleModelId.SetRightLabel("ped or not driver");
        vehicleFuelTank.SetRightLabel("ped or not driver");
      }

      mainMenu.MouseControlsEnabled = false;
      menuPool.MouseEdgeEnabled = false;
      menuPool.ControlDisablingEnabled = false;
      menuPool.ProcessMenus();

      if (Game.IsControlJustReleased(0, Control.InteractionMenu)) {
        mainMenu.Visible = !mainMenu.Visible;
      }
    }
  }
}
