using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NeroOS.Sim;
using NeroOS.Drawing;


namespace NeroOS.Apps
{
    class GlowFingerApp : IProgram
    {
        Shader glowShader;
        bool shadersLoaded = false;
        Vector4 color = new Vector4(0.9804f, 0.9939f, 0.4884f, 1.0f);
        BoundingSphere[] collisionPoints;

        public override bool InteractCollision(BoundingSphere collisionPoint)
        {
            return true;
        }

        void LoadShaders(Canvas canvas)
        {
            shadersLoaded = true;
            glowShader = new Shader();
            glowShader.CompileFromFiles(canvas, "Shaders/GlowP.hlsl", "Shaders/CubeV.hlsl");
        }

        public override void OnInteract(BoundingSphere[] collisionPoints)
        {
            base.OnInteract(collisionPoints);
            this.collisionPoints = collisionPoints;
        }

        public override void OnRender(Canvas canvas)
        {
            base.OnRender(canvas);

            if (!shadersLoaded)
            {
                LoadShaders(canvas);
            }
            if (collisionPoints == null)
                return;

            glowShader.SetupShader(canvas);
            GraphicsDevice device = canvas.GetDevice();
            device.SetPixelShaderConstant(0, color);

            for (int i = 0; i < collisionPoints.Length; i++)
            {
                Matrix transform = Matrix.CreateScale(collisionPoints[i].Radius);
                transform.Translation = collisionPoints[i].Center;
                device.SetVertexShaderConstant(CanvasShaderConstants.VC_WORLD, transform);
                CanvasPrimitives.Sphere.Render(canvas);
            }
        }
    }
}
