using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using NeroOS.Cameras;
using NeroOS.Drawing;
using NeroOS.Apps;
using NeroOS.Sim;

namespace NeroOS
{
    public class NeroPlatform : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        Canvas canvas;
        StereoRig cameraRig;
        KeyboardState lastKeyState;
        List<IProgram> apps;
        
        public NeroPlatform()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            canvas = new Canvas(this.GraphicsDevice);
            cameraRig = new StereoRig();
            cameraRig.LoadCalibrationFile("Config/intrinsics.yml", "Config/extrinsics.yml");
            apps = new List<IProgram>();
            apps.Add(new GlowFingerApp());
            apps.Add(new DragNDropApp());

            for (int i = 0; i < apps.Count; i++)
            {
                apps[i].OnCreate();
            }

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            cameraRig.Update();

            BoundingSphere[] detectedPoints = cameraRig.GetDetectionPoints();
            for (int i = 0; i < apps.Count; i++)
            {
                apps[i].OnUpdate(elapsedTime);
                if (detectedPoints != null)
                {
                    //List<BoundingSphere> intersectedPoints = new List<BoundingSphere>();
                    for (int j = 0; j < detectedPoints.Length; j++)
                    {
                        if (apps[i].InteractCollision(detectedPoints[j]))
                        {
                            //intersectedPoints.Add(detectedPoints[j]);
                            apps[i].OnInteract(detectedPoints);
                            break;
                        }
                    }
                    //if(intersectedPoints.Count > 0)
                    //    apps[i].OnInteract(intersectedPoints.ToArray());
                }
            }

            KeyboardState currKeyState = Keyboard.GetState();
            if(currKeyState.IsKeyDown(Keys.Space) && lastKeyState.IsKeyUp(Keys.Space))
            {
                Console.WriteLine("Reversing matrix!");
                cameraRig.ReverseMatrix();
            }
            lastKeyState = currKeyState;
            base.Update(gameTime);
        }

        void RenderApps(VirtualCamera camera, RenderLayer renderLayer)
        {
            GraphicsDevice device = canvas.GetDevice();
            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, renderLayer.RenderTarget);
            device.SetRenderTarget(1, renderLayer.GlowTarget);
            device.Clear(Color.TransparentBlack);
            device.RenderState.DepthBufferEnable = true;
            device.RenderState.DepthBufferWriteEnable = true;
            device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_MODELVIEW, camera.ViewProjection);
            
            for (int i = 0; i < apps.Count; i++)
            {
                apps[i].OnRender(canvas);
            }

            device.SetRenderTarget(0, null);
            device.SetRenderTarget(1, null);
            device.DepthStencilBuffer = dsOld;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            cameraRig.RenderCameraRig(canvas);

            RenderApps(cameraRig.virtualCameraA, cameraRig.renderLayerA);
            RenderApps(cameraRig.virtualCameraB, cameraRig.renderLayerB);

            cameraRig.CompositeFinalImage(canvas);

            base.Draw(gameTime);
        }
    }
}
