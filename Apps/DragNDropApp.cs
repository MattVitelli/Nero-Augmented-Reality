using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NeroOS.Sim;
using NeroOS.Drawing;


namespace NeroOS.Apps
{
    public class DragNDropApp : IProgram
    {
        Shader cubeShader;
        bool shadersLoaded = false;
        Vector4 color = Vector4.One;
        Texture2D baseTexture;

        Vector3 rotVec = Vector3.Zero;
        bool isTouching = false;

        public override void OnCreate()
        {
            base.OnCreate();

            transform.SetPosition(Vector3.Forward * 0.5f);
            transform.SetScale(Vector3.One * 0.05f);
        }

        public override void OnUpdate(float elapsedTime)
        {
            base.OnUpdate(elapsedTime);
            rotVec = transform.GetRotation();
            rotVec.X += elapsedTime * 0.75f;
            rotVec.Y += elapsedTime * 0.25f;
            rotVec.Z += elapsedTime * 0.5f;
            if (rotVec.X >= MathHelper.TwoPi)
                rotVec.X -= MathHelper.TwoPi;
            if (rotVec.Y >= MathHelper.TwoPi)
                rotVec.Y -= MathHelper.TwoPi;
            if (rotVec.Z >= MathHelper.TwoPi)
                rotVec.Z -= MathHelper.TwoPi;
            transform.SetRotation(rotVec);

            color = Vector4.One;
            isTouching = false;
        }

        public override void OnInteract(BoundingSphere[] collisionPoints)
        {
            base.OnInteract(collisionPoints);
            isTouching = true;
            color = new Vector4(1, 0, 0, 1);
            if(collisionPoints.Length == 1)
            {
                transform.SetPosition(collisionPoints[0].Center);
            }
        }

        void LoadShaders(Canvas canvas)
        {
            shadersLoaded = true;
            cubeShader = new Shader();
            cubeShader.CompileFromFiles(canvas, "Shaders/CubeP.hlsl", "Shaders/CubeV.hlsl");
            baseTexture = Texture2D.FromFile(canvas.GetDevice(), "Data/Textures/smiley.png");
        }

        public override void OnRender(Canvas canvas)
        {
            base.OnRender(canvas);

            if (!shadersLoaded)
            {
                LoadShaders(canvas);
            }

            cubeShader.SetupShader(canvas);
            GraphicsDevice device = canvas.GetDevice();
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_WORLD, transform.GetTransform());
            device.Textures[0] = baseTexture;
            device.SetPixelShaderConstant(0, color);
                        
            CanvasPrimitives.Cube.Render(canvas);
        }

    }
}
