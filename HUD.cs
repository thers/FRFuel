using System;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Drawing;
using TinyTween;

namespace FRFuel
{
    public class HUD
    {
        protected Scaleform buttons = new Scaleform("instructional_buttons");

        protected float fuelBarWidth = 180f;
        protected float fuelBarHeight = 6f;

        protected Color fuelBarColourNormal;
        protected Color fuelBarColourWarning;

        protected Rectangle fuelBarBackdrop;
        protected Rectangle fuelBarBack;
        protected Rectangle fuelBar;

        protected Tween<float> fuelBarColorTween = new FloatTween();
        protected bool fuelBarAnimationDir = true;
        protected PointF basePosition = new PointF(0f, 584f);

        public PointF Position
        {
            set
            {
                fuelBarBackdrop.Position = value;
                fuelBarBack.Position = new PointF(value.X, value.Y + 3f);
                fuelBar.Position = fuelBarBack.Position;
            }
        }

        public HUD()
        {
            PointF fuelBarBackdropPosition = basePosition;
            PointF fuelBarBackPosition = new PointF(fuelBarBackdropPosition.X, fuelBarBackdropPosition.Y + 3f);
            PointF fuelBarPosition = fuelBarBackPosition;

            SizeF fuelBarBackdropSize = new SizeF(fuelBarWidth, 12f);
            SizeF fuelBarBackSize = new SizeF(fuelBarWidth, fuelBarHeight);
            SizeF fuelBarSize = fuelBarBackSize;

            Color fuelBarBackdropColour = Color.FromArgb(100, 0, 0, 0);
            Color fuelBarBackColour = Color.FromArgb(50, 255, 179, 0);

            fuelBarColourNormal = Color.FromArgb(150, 255, 179, 0);
            fuelBarColourWarning = Color.FromArgb(255, 255, 245, 220);

            fuelBarBackdrop = new Rectangle(fuelBarBackdropPosition, fuelBarBackdropSize, fuelBarBackdropColour);
            fuelBarBack = new Rectangle(fuelBarBackPosition, fuelBarBackSize, fuelBarBackColour);
            fuelBar = new Rectangle(fuelBarPosition, fuelBarSize, fuelBarColourNormal);
        }

        /// <summary>
        /// Reloads scaleform movie to ensure that it will be rendered
        /// Workaround for bug
        /// Looks safe to span on every tick /shrug
        /// </summary>
        public void ReloadScaleformMovie()
        {
            buttons = new Scaleform("instructional_buttons");
        }

        /// <summary>
        /// Renders fuel bar
        /// </summary>
        /// <param name="currentFuelLevel"></param>
        /// <param name="maxFuelLevel"></param>
        public void RenderBar(float currentFuelLevel, float maxFuelLevel)
        {
            float fuelLevelPercentage = (100f / maxFuelLevel) * currentFuelLevel;
            PointF safeZone = GetSafezoneBounds();

            Position = new PointF(basePosition.X + safeZone.X, basePosition.Y - safeZone.Y);

            fuelBar.Size = new SizeF(
              (fuelBarWidth / 100f) * fuelLevelPercentage,
              fuelBarHeight
            );

            if (maxFuelLevel > 0 && currentFuelLevel < 9f)
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

                fuelBar.Color = Color.FromArgb((int) Math.Floor(fuelBarColorTween.CurrentValue), fuelBarColourWarning);
            }
            else
            {
                fuelBar.Color = fuelBarColourNormal;

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
        public static PointF GetSafezoneBounds()
        {
            float t = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);

            return new PointF(
              (int) Math.Round((1280 - (1280 * t)) / 2),
              (int) Math.Round((720 - (720 * t)) / 2)
            );
        }

        /// <summary>
        /// Change instructions for engine cut off
        /// </summary>
        public void InstructTurnOffEngine()
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.VehicleHorn, 0), "Turn off engine");

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Change instructions for refueling and engine spin up
        /// </summary>
        public void InstructRefuelOrTurnOnEngine()
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.Jump, 0), "Refuel");
            buttons.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.VehicleHorn, 0), "Turn on engine");

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Change instructions for manual refueling
        /// </summary>
        /// <param name="label"></param>
        public void InstructManualRefuel(string label)
        {
            buttons.CallFunction("CLEAR_ALL");
            buttons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            buttons.CallFunction("CREATE_CONTAINER");

            buttons.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.Attack, 0), label);

            buttons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        /// <summary>
        /// Renders instruction
        /// </summary>
        public void RenderInstructions()
        {
            buttons.Render2D();
        }
    }
}
