using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NeroOS.Drawing.Voxels;

namespace NeroOS.Drawing
{
    public class ScreenAlignedQuad
    {
        VertexPositionTexture[] verts = null;
        short[] ib = null;

        public ScreenAlignedQuad()
        {
            verts = new VertexPositionTexture[]
                        {
                            new VertexPositionTexture(
                                new Vector3(1,-1,0),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,0),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,0),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,0),
                                new Vector2(1,0))
                        };

            ib = new short[] { 0, 1, 2, 2, 3, 0 };
        }

        ~ScreenAlignedQuad()
        {
            verts = null;
            ib = null;
        }

        public void SetPositions(Vector2 min, Vector2 max)
        {
            verts[0].Position.X = max.X;
            verts[0].Position.Y = min.Y;
            verts[1].Position.X = min.X;
            verts[1].Position.Y = min.Y;
            verts[2].Position.X = min.X;
            verts[2].Position.Y = max.Y;
            verts[3].Position.X = max.X;
            verts[3].Position.Y = max.Y;
        }

        public void SetTexCoords(Vector2 min, Vector2 max)
        {
            verts[0].TextureCoordinate.X = max.X;
            verts[0].TextureCoordinate.Y = max.Y;
            verts[1].TextureCoordinate.X = min.X;
            verts[1].TextureCoordinate.Y = max.Y;
            verts[2].TextureCoordinate.X = min.X;
            verts[2].TextureCoordinate.Y = min.Y;
            verts[3].TextureCoordinate.X = max.X;
            verts[3].TextureCoordinate.Y = min.Y;
        }

        public void Render(Canvas canvas)
        {
            canvas.GetDevice().VertexDeclaration = CanvasVertexDeclarations.PTDec;
            canvas.GetDevice().DrawUserIndexedPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, verts, 0, 4, ib, 0, 2);
        }
    }

    public class RenderCube
    {
        VertexPositionTexture[] verts = null;
        short[] ib = null;

        public RenderCube()
        {
            verts = new VertexPositionTexture[]
                        {
                            new VertexPositionTexture(
                                new Vector3(1,-1,1),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,1),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,1),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,1),
                                new Vector2(1,0)),
                            new VertexPositionTexture(
                                new Vector3(1,-1,-1),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,-1),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,-1),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,-1),
                                new Vector2(1,0))
                        };

            ib = new short[] { 0, 1, 2, 2, 3, 0, 6, 5, 4, 4, 7, 6, 
                                   3, 2, 6, 6, 7, 3, 5, 1, 0, 0, 4, 5, 
                                   6, 2, 1, 1, 5, 6, 0, 3, 7, 7, 4, 0};
        }

        ~RenderCube()
        {
            verts = null;
            ib = null;
        }

        public void Render(Canvas canvas)
        {
            canvas.GetDevice().VertexDeclaration = CanvasVertexDeclarations.PTDec;
            canvas.GetDevice().DrawUserIndexedPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, verts, 0, 8, ib, 0, 12);
        }
    }

    public class RenderSphere
    {
        VoxelGeometry Geometry;

        public RenderSphere(GraphicsDevice device, int resolution)
        {
            GenerateGeometry(device, resolution);
        }

        void GenerateGeometry(GraphicsDevice device, int DensityFieldSize)
        {
            byte[] DensityField;
            byte IsoValue = 127;

            DensityField = new byte[DensityFieldSize * DensityFieldSize * DensityFieldSize];
            Vector3 center = Vector3.One * DensityFieldSize * 0.5f;
            Vector3 minPos = center;
            Vector3 maxPos = center;

            float radius = DensityFieldSize / 2;

            for (int x = 0; x < DensityFieldSize; x++)
            {
                for (int y = 1; y < (DensityFieldSize - 1); y++)
                {
                    for (int z = 0; z < DensityFieldSize; z++)
                    {
                        Vector3 pos = new Vector3(x, y, z);

                        float density = MathHelper.Clamp(1.0f - (pos - center).Length() / radius, 0, 1);
                        if (density > 0.0f)
                        {
                            pos = (pos / DensityFieldSize) * 2.0f - Vector3.One;
                            minPos = Vector3.Min(pos, minPos);
                            maxPos = Vector3.Max(pos, maxPos);
                        }
                        DensityField[x + (y + z * DensityFieldSize) * DensityFieldSize] = (byte)(density * 255.0f);
                    }
                }
            }

            Geometry = new VoxelGeometry();
            Geometry.GenerateGeometry(device, ref DensityField, IsoValue, DensityFieldSize, DensityFieldSize, DensityFieldSize, DensityFieldSize - 1, DensityFieldSize - 1, DensityFieldSize - 1, 0, 0, 0, 2.0f / (float)(DensityFieldSize - 1), Matrix.Identity);
        }

        public void Render(Canvas canvas)
        {
            GraphicsDevice device = canvas.GetDevice();
            device.VertexDeclaration = Geometry.renderElement.VertexDec;
            device.Indices = Geometry.renderElement.IndexBuffer;
            device.Vertices[0].SetSource(Geometry.renderElement.VertexBuffer, 0, Geometry.renderElement.VertexStride);
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, Geometry.renderElement.StartVertex, Geometry.renderElement.VertexCount, 0, Geometry.renderElement.PrimitiveCount);
        }
    }

    public static class CanvasPrimitives
    {
        public static ScreenAlignedQuad Quad;
        public static RenderCube Cube;
        public static RenderSphere Sphere;

        public static void Initialize(Canvas canvas)
        {
            Quad = new ScreenAlignedQuad();
            Cube = new RenderCube();
            Sphere = new RenderSphere(canvas.GetDevice(), 17);
        }
    }
}
