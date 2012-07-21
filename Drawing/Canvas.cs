using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing
{
    public class RenderElement
    {
        public int StartVertex;

        public int VertexCount;

        public int VertexStride;

        public int PrimitiveCount;

        public Matrix[] Transform;

        public IndexBuffer IndexBuffer;

        public VertexBuffer VertexBuffer;

        public VertexDeclaration VertexDec;
    }

    public enum ImageParameters
    {
        FlipX = 1,
        FlipY = 2,
        RotateCW = 4,
        RotateCCW = 8,
    };

    public class Canvas
    {
        GraphicsDevice device;

        public GraphicsDevice GetDevice() { return device; }

        SortedList<Material, Queue<RenderElement>> Elements = new SortedList<Material, Queue<RenderElement>>();

        Shader generic2DShader;

        public DepthStencilBuffer GetDepthStencil()
        {
            return genericDepthStencil;
        }

        DepthStencilBuffer genericDepthStencil;

        public Canvas(GraphicsDevice device)
        {
            this.device = device;

            CanvasShaderConstants.AuthorShaderConstantFile();
            CanvasVertexDeclarations.Initialize(this);
            CanvasPrimitives.Initialize(this);

            generic2DShader = new Shader();
            generic2DShader.CompileFromFiles(this, "Shaders/Basic2DP.hlsl", "Shaders/Basic2DV.hlsl");
            genericDepthStencil = new DepthStencilBuffer(device, 2048, 2048, DepthFormat.Depth24Stencil8);
        }

        public void SetRenderView(RenderView view)
        {

        }

        public void Render()
        {
            device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            device.RenderState.DepthBufferEnable = true;
            device.RenderState.DepthBufferWriteEnable = true;
            device.RenderState.DepthBufferFunction = CompareFunction.Less;
            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial(this);

                while (Elements[key].Count > 0)
                {
                    RenderElement currElem = Elements[key].Dequeue();
                    if (currElem.VertexDec != device.VertexDeclaration)
                        device.VertexDeclaration = currElem.VertexDec;
                    device.Indices = currElem.IndexBuffer;
                    device.Vertices[0].SetSource(currElem.VertexBuffer, 0, currElem.VertexStride);
                    device.SetVertexShaderConstant(CanvasShaderConstants.VC_WORLD, currElem.Transform);
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, currElem.StartVertex, currElem.VertexCount, 0, currElem.PrimitiveCount);
                }
            }
        }

        public void DrawImage(Texture2D image)
        {
            DrawImage(image, Vector2.One * -1, Vector2.One);
        }

        public void DrawImage(Texture2D image, Vector2 min, Vector2 max)
        {
            DrawImage(image, min, max, 0);
        }

        public void DrawImage(Texture2D image, Vector2 min, Vector2 max, int imageFlag)
        {
            generic2DShader.SetupShader(this);
            device.Textures[0] = image;
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(image.Width, image.Height));
            Vector2 minTC = Vector2.Zero;
            Vector2 maxTC = Vector2.One;
            if ((imageFlag & (int)ImageParameters.FlipX) > 0)
            {
                float temp = minTC.X;
                minTC.X = maxTC.X;
                maxTC.X = temp;
            }
            if ((imageFlag & (int)ImageParameters.FlipY) > 0)
            {
                float temp = minTC.Y;
                minTC.Y = maxTC.Y;
                maxTC.Y = temp;
            }

            CanvasPrimitives.Quad.SetTexCoords(minTC, maxTC);
            CanvasPrimitives.Quad.SetPositions(min, max);
            CanvasPrimitives.Quad.Render(this);
            CanvasPrimitives.Quad.SetTexCoords(Vector2.Zero, Vector2.One);
        }

        public void DrawCube(Vector3 min, Vector3 max)
        {

        }
    }
}
