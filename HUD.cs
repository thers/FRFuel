using System;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Drawing;
using TinyTween;
using System.Globalization;

namespace FRFuel
{
    public static class HUD
    {
        private static Scaleform buttons = new Scaleform("instructional_buttons");

        private static float fuelBarHeight = 6f;
        private static PointF basePosition = new PointF(0f, 584f);

        private static CitizenFX.Core.UI.Rectangle fuelBarBackdrop;
        private static CitizenFX.Core.UI.Rectangle fuelBarBack;
        private static CitizenFX.Core.UI.Rectangle fuelBar;

        private static Tween<float> fuelBarColorTween = new FloatTween();
        private static bool fuelBarAnimationDir = true;

        private static PointF fuelBarBackdropPosition = new PointF(0f, 584f);
        private static PointF fuelBarPosition = new PointF(fuelBarBackdropPosition.X, fuelBarBackdropPosition.Y + 3f);


        private static Color fuelBarBackdropColor = Color.FromArgb(100, 0, 0, 0);
        private static Color fuelBarBackColor = Color.FromArgb(50, 255, 179, 0);

        private static Color fuelBarNormalColor = Config.GetInstance().FuelBarColor != null ? Color.FromArgb(Int32.Parse(Config.GetInstance().FuelBarColor.Replace("#", ""), NumberStyles.HexNumber)) : Color.FromArgb(150, 255, 179, 0);
        private static Color fuelBarWarningColor = Config.GetInstance().FuelBarLowColor != null ? Color.FromArgb(Int32.Parse(Config.GetInstance().FuelBarLowColor.Replace("#", ""), NumberStyles.HexNumber)) : Color.FromArgb(255, 255, 245, 220);

        private static PointF Position
        {
            set
            {
                fuelBarBackdrop.Position = value;
                fuelBarBack.Position = new PointF(value.X, value.Y + 3f);
                fuelBar.Position = fuelBarBack.Position;
            }
        }

        /// <summary>
        /// Reloads scaleform movie to ensure that it will be rendered
        /// Workaround for bug
        /// Looks safe to span on every tick /shrug
        /// </summary>
        public static void ReloadScaleformMovie()
        {
            buttons = new Scaleform("instructional_buttons");
        }

        /// <summary>
        /// Renders fuel bar
        /// </summary>
        /// <param name="currentFuelLevel"></param>
        /// <param name="maxFuelLevel"></param>
        public static void RenderBar(float currentFuelLevel, float maxFuel)
        {
            var fuelBarSize = new SizeF(GetBarWidth(), fuelBarHeight);
            var fuelBarBackdropSize = new SizeF(GetBarWidth(), 12f);

            fuelBarBackdrop = new CitizenFX.Core.UI.Rectangle(fuelBarBackdropPosition, fuelBarBackdropSize, fuelBarBackdropColor);
            fuelBarBack = new CitizenFX.Core.UI.Rectangle(fuelBarPosition, fuelBarSize, fuelBarBackColor);
            fuelBar = new CitizenFX.Core.UI.Rectangle(fuelBarPosition, fuelBarSize, fuelBarNormalColor);


            float fuelLevelPercentage = (100f / maxFuel) * currentFuelLevel;
            PointF safeZone = GetSafezoneBounds();

            if (API.IsBigmapActive())
            {
                Position = new PointF(basePosition.X + safeZone.X, basePosition.Y - safeZone.Y - 180f);
            }
            else
            {
                Position = new PointF(basePosition.X + safeZone.X, basePosition.Y - safeZone.Y);
            }

            fuelBarSize = new SizeF((GetBarWidth() / 100f) * fuelLevelPercentage, fuelBarHeight);

            fuelBar.Size = fuelBarSize;
            fuelBarBackdrop.Size = fuelBarBackdropSize;
            fuelBarBack.Size = fuelBarSize;

            if (maxFuel > 0 && currentFuelLevel < 9f)
            {
                if (fuelBarColorTween.State == TweenState.Stopped)
                {
                    fuelBarAnimationDir = !fuelBarAnimationDir;

                    fuelBarColorTween.Start(
                      fuelBarAnimationDir ? 100f : 255f,
                      fuelBarAnimationDir ? 255f : 100f,
                      .5f, // seconds
                      ScaleFuncs.QuarticEaseOut
                    );
                }

                fuelBarColorTween.Update(Game.LastFrameTime);

                fuelBar.Color = Color.FromArgb((int)Math.Floor(fuelBarColorTween.CurrentValue), fuelBarWarningColor);
            }
            else
            {
                fuelBar.Color = fuelBarNormalColor;

                if (fuelBarColorTween.State != TweenState.Stopped)
                {
                    fuelBarColorTween.Stop(StopBehavior.ForceComplete);
                }
            }

            fuelBarBackdrop.Draw();
            fuelBarBack.Draw();
            fuelBar.Draw();
        }

        /// <summary>
        /// Returns user-configured screen safe zone offset
        /// </summary>
        /// <returns></returns>
        private static PointF GetSafezoneBounds()
        {
            float t = API.GetSafeZoneSize();
            float w = Screen.Width;
            float h = Screen.Height;

            return new PointF(
              (int)Math.Round((w - (w * t)) / 2 + 1),
              (int)Math.Round((h - (h * t)) / 2 - 2)
            );
        }

        /// <summary>
        /// Change instructions for engine cut off
        /// </summary>
        public static void InstructTurnOffEngine()
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)FRFuel.engineToggleControl, false), "Turn off engine");

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Returns resolution specified bar width
        /// </summary>
        /// <returns></returns>
        private static float GetBarWidth()
        {
            float width;
            double aspect = Screen.AspectRatio;
            bool bigMap = API.IsBigmapActive();

            switch (aspect)
            {
                case (float)1.5: // 3:2
                    width = bigMap ? 336f : 212f;
                    break;
                case (float)1.33333337306976: // 4:3
                    width = bigMap ? 378f : 240f;
                    break;
                case (float)1.66666662693024: // 5:3
                    width = bigMap ? 302f : 191f;
                    break;
                case (float)1.25: // 5:4
                    width = bigMap ? 405f : 255f;
                    break;
                case (float)1.60000002384186: // 16:10
                    width = bigMap ? 316f : 200f;
                    break;
                default:
                    width = bigMap ? 285f : 180f; // 16:9
                    break;
            }
            return width;
        }

        /// <summary>
        /// Change instructions for refueling and engine spin up
        /// </summary>
        public static void InstructRefuelOrTurnOnEngine()
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)Control.Jump, false), "Refuel");
            buttons.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)FRFuel.engineToggleControl, 0), "Turn on engine");

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Change instructions for manual refueling
        /// </summary>
        /// <param name="label"></param>
        public static void InstructManualRefuel(string label)
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)Control.Attack, false), label);

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Renders instruction
        /// </summary>
        public static void RenderInstructions()
        {
            buttons.Render2D();
        }
    }
}
