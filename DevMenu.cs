using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using NativeUI;
using System.Drawing;
using CitizenFX.Core.Native;

namespace FRFuel.Dev
{
    public class DevMenu
    {
        protected MenuPool menuPool;
        protected UIMenu mainMenu;

        protected UIMenuItem position;
        protected UIMenuItem teleport;
        protected UIMenuItem deleteVehicle;

        protected UIMenuItem vehicleModelId;
        protected UIMenuItem vehicleFuelTank;
        protected UIMenuItem drainFuelTank;
        protected UIMenuItem knownVehicle;

        protected UIMenuItem netVehicleId;
        protected UIMenuItem netVehicleIdControl;
        protected UIMenuItem decoration;

        protected Text txt = new Text("", new PointF(600f, 100f), .5f);
        protected int gasStation = 0;

        public DevMenu()
        {
            var stations = new List<dynamic> { };

            for (int i = 0; i < GasStations.positions.Length; i++)
            {
                stations.Add(i);
            }

            menuPool = new MenuPool();
            mainMenu = new UIMenu("FRFuel dev menu", "things");

            position = new UIMenuItem("Pos");
            position.Enabled = true;

            teleport = new UIMenuListItem("Gas stations", stations, 0);
            teleport.Enabled = true;

            deleteVehicle = new UIMenuItem("Delete last vehicle");

            vehicleModelId = new UIMenuItem("Vehicle model ID");
            vehicleModelId.Enabled = false;

            knownVehicle = new UIMenuItem("Is known vehicle");
            knownVehicle.Enabled = false;

            vehicleFuelTank = new UIMenuItem("Vehicle fuel tank");
            drainFuelTank = new UIMenuItem("Almost drain fuel tank");

            mainMenu.AddItem(position);
            mainMenu.AddItem(teleport);
            mainMenu.AddItem(deleteVehicle);
            mainMenu.AddItem(knownVehicle);
            mainMenu.AddItem(vehicleModelId);
            mainMenu.AddItem(vehicleFuelTank);
            mainMenu.AddItem(drainFuelTank);

            mainMenu.OnItemSelect += (sende, item, index) =>
            {
                if (item == position)
                {
                    var p = Game.PlayerPed.Position;

                    BaseScript.TriggerServerEvent(
                      "frfuel:dev:saveFuel",
                      $"new Vector3({p.X.ToString().Replace(",", ".")}f,\t{p.Y.ToString().Replace(",", ".")}f,\t{p.Z.ToString().Replace(",", ".")}f),"
                    );
                    Screen.ShowNotification("Position saved");
                }

                if (item == vehicleFuelTank && Game.PlayerPed.IsInVehicle())
                {
                    BaseScript.TriggerServerEvent(
                      "frfuel:dev:saveFuel",
                      "{" + Game.PlayerPed.CurrentVehicle.Model.Hash.ToString() + ", " + Game.PlayerPed.CurrentVehicle.FuelLevel.ToString() + "f},"
                    );
                    Screen.ShowNotification("Fuel to model saved");
                }

                if (item == drainFuelTank && Game.PlayerPed.IsInVehicle())
                {
                    Game.PlayerPed.CurrentVehicle.SetDecor(FRFuel.fuelLevelPropertyName, .5f);
                }

                if (item == deleteVehicle)
                {
                    if (Game.PlayerPed.LastVehicle != null)
                    {
                        Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, Game.PlayerPed.LastVehicle, false, false);
                        Game.PlayerPed.LastVehicle.Delete();
                    }
                }

                if (item == teleport)
                {
                    Game.PlayerPed.Position = GasStations.positions[gasStation];
                }
            };

            mainMenu.OnListChange += (sender, item, index) =>
            {
                if (item == teleport)
                {
                    gasStation = index;
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
