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
    protected UIMenuItem vehicleId;
    protected UIMenuItem vehicleIdControl;
    protected UIMenuItem netVehicleIdExist;
    protected UIMenuItem netVehicleId;
    protected UIMenuItem netVehicleIdControl;
    protected UIMenuItem decoration;

    public DevUI() {
      menuPool = new MenuPool();
      mainMenu = new UIMenu("FRFuel dev menu", "things");

      isDriver = new UIMenuItem("Is driver");
      isDriver.Enabled = false;

      position = new UIMenuItem("Pos");
      position.Enabled = false;

      vehicleId = new UIMenuItem("Vehicle ID");
      vehicleId.Enabled = false;

      vehicleIdControl = new UIMenuItem("Vehicle ID control");
      vehicleIdControl.Enabled = false;

      netVehicleId = new UIMenuItem("Network vehicle ID");
      netVehicleId.Enabled = false;

      netVehicleIdExist = new UIMenuItem("Network vehicle ID exist");
      netVehicleIdExist.Enabled = false;

      netVehicleIdControl = new UIMenuItem("Network vehicle ID control");
      netVehicleIdControl.Enabled = false;

      decoration = new UIMenuItem("Decoration");
      decoration.Enabled = false;

      mainMenu.AddItem(position);
      mainMenu.AddItem(isDriver);
      mainMenu.AddItem(vehicleId);
      mainMenu.AddItem(vehicleIdControl);
      mainMenu.AddItem(netVehicleId);
      mainMenu.AddItem(netVehicleIdExist);
      mainMenu.AddItem(netVehicleIdControl);
      mainMenu.AddItem(decoration);

      mainMenu.OnItemSelect += (sende, item, index) => {
        if (item == position) {
          var pp = Game.PlayerPed.Position;

          var px = pp.X.ToString().Replace(",", ".");
          var py = pp.Y.ToString().Replace(",", ".");
          var pz = pp.Z.ToString().Replace(",", ".");

          BaseScript.TriggerServerEvent(
            "frfuel:dev:savePosition",
            "spawnpoint 'a_m_y_skater_01' { x = " + px + ", y = " + py + ", z = " + pz + "}"
          );
          Screen.ShowNotification("Position saved");
        }
      };

      menuPool.Add(mainMenu);
      menuPool.RefreshIndex();
    }

    public void OnTick() {
      position.Text = "Pos: " + Game.PlayerPed.Position.ToString();

      if (Game.PlayerPed.IsInVehicle()) {
        Vehicle vehicle = Game.PlayerPed.CurrentVehicle;

        int vehicleNetworkId = Function.Call<int>(Hash.VEH_TO_NET/*NETWORK_GET_NETWORK_ID_FROM_ENTITY*/, vehicle.NativeValue);
        bool isNetworkIdExist = Function.Call<bool>(Hash.NETWORK_DOES_NETWORK_ID_EXIST, vehicleNetworkId);
        bool isControllingVehicle = Function.Call<bool>(Hash.NETWORK_HAS_CONTROL_OF_ENTITY, vehicle.NativeValue);
        bool isControllingNetworkId = Function.Call<bool>(Hash.NETWORK_HAS_CONTROL_OF_NETWORK_ID, vehicleNetworkId);

        if (!isControllingVehicle) {
          //Function.Call(Hash.NETWORK_REQUEST_CONTROL_OF_ENTITY, vehicle.NativeValue);
        }

        if (isNetworkIdExist) {
          //Function.Call(Hash.SET_NETWORK_ID_CAN_MIGRATE, vehicleNetworkId, true);
          //Function.Call(Hash.NETWORK_UNREGISTER_NETWORKED_ENTITY, vehicle.NativeValue);
        }

        //if (!isNetworkIdExist) {
        //
        //}

        isDriver.SetRightLabel(Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == Game.PlayerPed ? "Yes" : "No");
        vehicleId.SetRightLabel(vehicle.NativeValue.ToString());
        vehicleIdControl.SetRightLabel(isControllingVehicle ? "Yes" : "No");
        netVehicleId.SetRightLabel(vehicleNetworkId.ToString());
        netVehicleIdExist.SetRightLabel(isNetworkIdExist ? "Yes" : "No");
        netVehicleIdControl.SetRightLabel(isControllingNetworkId ? "Yes" : "No");

        try {
          decoration.SetRightLabel(EntityDecoration.Get<float>(vehicle, "_Fuel_Level").ToString());
        } catch(EntityDecorationUnregisteredPropertyException e) {
          decoration.SetRightLabel("Unregistered prop");
        }
      } else {
        isDriver.SetRightLabel("ped or not driver");
        vehicleId.SetRightLabel("ped or not driver");
        vehicleIdControl.SetRightLabel("ped or not driver");
        netVehicleId.SetRightLabel("ped or not driver");
        netVehicleIdExist.SetRightLabel("ped or not driver");
        netVehicleIdControl.SetRightLabel("ped or not driver");
        decoration.SetRightLabel("ped or not driver");
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
