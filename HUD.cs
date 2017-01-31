using System;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Drawing;
using TinyTween;

namespace FRFuel {
  public class HUD {
    protected float fuelBarWidth = 180f;
    protected float fuelBarHeight = 6f;

    protected Color fuelBarColourNormal;
    protected Color fuelBarColourWarning;

    protected Rectangle fuelBarBackdrop;
    protected Rectangle fuelBarBack;
    protected Rectangle fuelBar;

    public Vector3 markerPutDown = new Vector3(0f, 0f, 3f);
    public Vector3 markerDir = new Vector3();
    public Vector3 markerRot = new Vector3();
    public Vector3 markerScale = new Vector3(15f);
    public Color markerColour = Color.FromArgb(50, 255, 179, 0);

    public Text helpTextRefuel = new Text(
      "Hold ~b~Space~w~ to refuel",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextTurnOff = new Text(
      "~b~Horn~w~ to stop engine",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextTurnOn = new Text(
      "~b~Horn~w~ to start engine",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextJerryCan = new Text(
      "Press ~b~Fire~w~ to refuel with jerry can",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    protected Tween<float> fuelBarColorTween = new FloatTween();
    protected bool fuelBarAnimationDir = true;

    public PointF Position {
      set {
        fuelBarBackdrop.Position = value;
        fuelBarBack.Position = new PointF(value.X, value.Y + 3f);
        fuelBar.Position = fuelBarBack.Position;
      }
    }

    protected PointF basePosition = new PointF(0f, 584f);

    public HUD() {
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

    public void RenderBar(float currentFuelLevel, float maxFuelLevel) {
      float fuelLevelPercentage = (100f / maxFuelLevel) * currentFuelLevel;
      PointF safeZone = GetSafezoneBounds();

      Position = new PointF(basePosition.X + safeZone.X, basePosition.Y - safeZone.Y);

      fuelBar.Size = new SizeF(
        (fuelBarWidth / 100f) * fuelLevelPercentage,
        fuelBarHeight
      );

      if (maxFuelLevel > 0 && currentFuelLevel < 9f) {
        if (fuelBarColorTween.State == TweenState.Stopped) {
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
      } else {
        fuelBar.Color = fuelBarColourNormal;

        if (fuelBarColorTween.State != TweenState.Stopped) {
          fuelBarColorTween.Stop(StopBehavior.ForceComplete);
        }
      }

      fuelBarBackdrop.Draw();
      fuelBarBack.Draw();
      fuelBar.Draw();
    }

    public static PointF GetSafezoneBounds() {
      float t = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);

      return new PointF(
        (int) Math.Round((1280 - (1280 * t)) / 2),
        (int) Math.Round((720 - (720 * t)) / 2)
      );
    }

    public void RenderMarker(Vector3 pos) {
      World.DrawMarker(
          MarkerType.VerticalCylinder,
          pos - markerPutDown,
          markerDir,
          markerRot,
          markerScale,
          markerColour
      );
    }

    public static void DrawScaleform() {
      var scaleform = new Scaleform("instructional_buttons");
      scaleform.CallFunction("CLEAR_ALL");
      scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
      scaleform.CallFunction("CREATE_CONTAINER");

      scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.Jump, 0), "t1");
      scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.FrontendCancel, 0), "t2");
      scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.PhoneRight, 0), "");
      scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.PhoneLeft, 0), "t3");
      scaleform.CallFunction("SET_DATA_SLOT", 4, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.FrontendRb, 0), "");
      scaleform.CallFunction("SET_DATA_SLOT", 5, Function.Call<string>((Hash) 0x0499D7B09FC9B407, 2, (int) Control.FrontendLb, 0), "t4");
      scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
      scaleform.Render2D();
    }
  }
}
