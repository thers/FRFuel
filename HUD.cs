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
      "Press ~b~L~w~ to stop engine",
      new PointF(640f, 690f),
      0.5f,
      Color.FromArgb(255, 255, 255, 255),
      Font.ChaletLondon,
      Alignment.Center,
      false,
      true
    );

    public Text helpTextTurnOn = new Text(
      "Press ~b~L~w~ to start engine",
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

    public HUD() {
      PointF fuelBarBackdropPosition = new PointF(20f, 569f);
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
  }
}
