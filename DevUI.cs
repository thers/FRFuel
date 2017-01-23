using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using NativeUI;

namespace FRFuel
{
  public class DevUI
  {
    protected MenuPool menuPool;
    protected UIMenu mainMenu;

    protected UIMenuItem position;

    public DevUI()
    {
      menuPool = new MenuPool();
      mainMenu = new UIMenu("FRFuel dev menu", "things");

      position = new UIMenuItem("Position");

      mainMenu.AddItem(position);

      mainMenu.OnItemSelect += (sende, item, index) =>
      {
        if (item == position)
        {
          var pp = Game.PlayerPed.Position;

          BaseScript.TriggerServerEvent("frfuel:dev:savePosition", $"{pp.X};\t{pp.Y};\t{pp.Z}");
          Screen.ShowNotification("Position saved");
        }
      };

      menuPool.Add(mainMenu);
      menuPool.RefreshIndex();
    }

    public void OnTick()
    {
      position.Description = Game.PlayerPed.Position.ToString();

      menuPool.ProcessMenus();

      if (Game.IsControlJustReleased(0, Control.InteractionMenu))
      {
        mainMenu.Visible = !mainMenu.Visible;
      }
    }
  }
}
