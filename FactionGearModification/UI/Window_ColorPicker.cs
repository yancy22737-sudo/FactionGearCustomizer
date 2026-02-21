using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class Window_ColorPicker : Window
    {
        private Color color;
        private Action<Color> callback;
        private string bufferR, bufferG, bufferB;

        public override Vector2 InitialSize => new Vector2(300f, 200f);

        public Window_ColorPicker(Color initialColor, Action<Color> callback)
        {
            this.color = initialColor;
            this.callback = callback;
            this.bufferR = (color.r * 255).ToString("F0");
            this.bufferG = (color.g * 255).ToString("F0");
            this.bufferB = (color.b * 255).ToString("F0");
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            // Preview
            Rect previewRect = list.GetRect(30f);
            Widgets.DrawBoxSolid(previewRect, color);
            list.Gap();

            // Sliders
            color.r = Widgets.HorizontalSlider(list.GetRect(24f), color.r, 0f, 1f, true, "R: " + (int)(color.r * 255));
            color.g = Widgets.HorizontalSlider(list.GetRect(24f), color.g, 0f, 1f, true, "G: " + (int)(color.g * 255));
            color.b = Widgets.HorizontalSlider(list.GetRect(24f), color.b, 0f, 1f, true, "B: " + (int)(color.b * 255));
            
            list.Gap();

            if (list.ButtonText("Apply"))
            {
                callback(color);
                Close();
            }

            list.End();
        }
    }
}
