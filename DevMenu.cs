using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using NativeUI;
using System.Drawing;
using System.Security;
using CitizenFX.Core.Native;

namespace FRFuel.Dev
{
    public class DevMenu
    {
        protected MenuPool menuPool;
        protected UIMenu mainMenu;

        protected UIMenuItem position;
        protected UIMenuItem deleteVehicle;

        protected UIMenuItem vehicleModelId;
        protected UIMenuItem vehicleFuelTank;
        protected UIMenuItem knownVehicle;

        protected UIMenuItem netVehicleId;
        protected UIMenuItem netVehicleIdControl;
        protected UIMenuItem decoration;

        protected Text txt = new Text("", new PointF(600f, 100f), .5f);

        public DevMenu()
        {
            menuPool = new MenuPool();
            mainMenu = new UIMenu("FRFuel dev menu", "things");

            position = new UIMenuItem("Pos");
            position.Enabled = false;

            deleteVehicle = new UIMenuItem("Delete last vehicle");

            vehicleModelId = new UIMenuItem("Vehicle model ID");
            vehicleModelId.Enabled = false;

            knownVehicle = new UIMenuItem("Is known vehicle");
            knownVehicle.Enabled = false;

            vehicleFuelTank = new UIMenuItem("Vehicle fuel tank");

            mainMenu.AddItem(position);
            mainMenu.AddItem(deleteVehicle);
            mainMenu.AddItem(knownVehicle);
            mainMenu.AddItem(vehicleModelId);
            mainMenu.AddItem(vehicleFuelTank);

            mainMenu.OnItemSelect += (sende, item, index) =>
            {
                if (item == vehicleFuelTank && Game.PlayerPed.IsInVehicle())
                {
                    BaseScript.TriggerServerEvent(
                      "frfuel:dev:saveFuel",
                      "{" + Game.PlayerPed.CurrentVehicle.Model.Hash.ToString() + ", " + Game.PlayerPed.CurrentVehicle.FuelLevel.ToString() + "f},"
                    );
                    Screen.ShowNotification("Fuel to model saved");
                }

                if (item == deleteVehicle)
                {
                    if (Game.PlayerPed.LastVehicle != null)
                    {
                        Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, Game.PlayerPed.LastVehicle, false, false);
                        Game.PlayerPed.LastVehicle.Delete();
                    }
                }
            };

            menuPool.Add(mainMenu);
            menuPool.RefreshIndex();
        }

        public void OnTick()
        {
            position.Text = "Pos: " + Game.PlayerPed.Position.ToString();

            if (Game.PlayerPed.IsInVehicle())
            {
                Vehicle vehicle = Game.PlayerPed.CurrentVehicle;

                knownVehicle.SetRightLabel(VehiclesPetrolTanks.dict.ContainsKey(vehicle.Model.Hash) ? "Yes" : "No");
                vehicleModelId.SetRightLabel(vehicle.DisplayName.ToString());
                vehicleFuelTank.SetRightLabel(vehicle.FuelLevel.ToString());
            }
            else
            {
                vehicleModelId.SetRightLabel("ped or not driver");
                vehicleFuelTank.SetRightLabel("ped or not driver");
            }

            mainMenu.MouseControlsEnabled = false;
            menuPool.MouseEdgeEnabled = false;
            menuPool.ControlDisablingEnabled = false;
            menuPool.ProcessMenus();

            if (Game.IsControlJustReleased(0, Control.InteractionMenu))
            {
                mainMenu.Visible = !mainMenu.Visible;
            }
        }
    }
}
