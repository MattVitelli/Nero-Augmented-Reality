using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing
{
    public enum ImageLayer
    {
        Main,
        Glow,
    };
    public class RenderLayer
    {
        public RenderTarget2D RenderTarget;
        public RenderTarget2D GlowTarget;

        public RenderLayer(Canvas canvas, int width, int height)
        {
            RenderTarget = new RenderTarget2D(canvas.GetDevice(), width, height, 1, SurfaceFormat.Color);
            GlowTarget = new RenderTarget2D(canvas.GetDevice(), width, height, 1, SurfaceFormat.Color);
        }

        public Texture2D GetImage()
        {
            return RenderTarget.GetTexture();
        }

        public Texture2D GetImage(ImageLayer layer)
        {
            switch (layer)
            {
                case ImageLayer.Glow:
                    return GlowTarget.GetTexture();
            }

            return GetImage();
        }
    }
}
