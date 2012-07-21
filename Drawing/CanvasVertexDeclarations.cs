using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing
{

    public struct VertexPositionNormal
    {
        public Vector3 Position;
        public Vector3 Normal;

        public static int SizeInBytes = (6) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*3, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
         };
        public VertexPositionNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }

    public static class CanvasVertexDeclarations
    {
        public static VertexDeclaration PTDec;

        public static VertexDeclaration PNDec;

        public static void Initialize(Canvas canvas)
        {
            GraphicsDevice device = canvas.GetDevice();
            PTDec = new VertexDeclaration(device, VertexPositionTexture.VertexElements);
            PNDec = new VertexDeclaration(device, VertexPositionNormal.VertexElements);
        }
    }
}
