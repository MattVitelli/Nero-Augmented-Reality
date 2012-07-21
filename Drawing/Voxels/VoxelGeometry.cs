using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing.Voxels
{
    class VoxelGeometry
    {
        public VertexPositionNormal[] verts = null;
        public ushort[] ib = null;
        int PrimitiveCount = 0;

        public RenderElement renderElement;

        public bool CanRender = false;

        public VoxelGeometry()
        {
            InitializeRenderElement();
        }

        void InitializeRenderElement()
        {
            renderElement = new RenderElement();
            renderElement.VertexDec = CanvasVertexDeclarations.PNDec;
            renderElement.VertexStride = VertexPositionNormal.SizeInBytes;
            renderElement.StartVertex = 0;
        }

        void DestroyBuffers()
        {
            if (renderElement.VertexBuffer != null)
                renderElement.VertexBuffer.Dispose();
            renderElement.VertexBuffer = null;

            if (renderElement.IndexBuffer != null)
                renderElement.IndexBuffer.Dispose();
            renderElement.IndexBuffer = null;

            verts = null;
            ib = null;
            PrimitiveCount = 0;

            CanRender = false;
        }

        public void GenerateGeometry(GraphicsDevice device, ref byte[] DensityField, byte IsoValue, int DensityFieldWidth, int DensityFieldHeight, int DensityFieldDepth, int Width, int Height, int Depth, int xOrigin, int yOrigin, int zOrigin, float ratio, Matrix transform)
        {
            DestroyBuffers();

            List<VertexPositionNormal> _vertices = new List<VertexPositionNormal>();
            List<ushort> _indices = new List<ushort>();
            SortedList<int, ushort> _edgeToIndices = new SortedList<int, ushort>();

            int width = DensityFieldWidth;
            int sliceArea = width * DensityFieldHeight;

            byte[] DensityCache = new byte[8];
            Vector3[] VectorCache = new Vector3[8];
            for (int z = zOrigin; z < zOrigin + Depth; z++)
            {
                for (int y = yOrigin; y < yOrigin + Height; y++)
                {

                    int dIdx = z * sliceArea + y * width + xOrigin;

                    VectorCache[0] = new Vector3(xOrigin, y, z) * ratio - Vector3.One;
                    VectorCache[3] = new Vector3(xOrigin, y + 1, z) * ratio - Vector3.One;
                    VectorCache[4] = new Vector3(xOrigin, y, z + 1) * ratio - Vector3.One;
                    VectorCache[7] = new Vector3(xOrigin, y + 1, z + 1) * ratio - Vector3.One;
                    DensityCache[0] = DensityField[dIdx];
                    DensityCache[3] = DensityField[dIdx + width];
                    DensityCache[4] = DensityField[dIdx + sliceArea];
                    DensityCache[7] = DensityField[dIdx + sliceArea + width];

                    for (int x = xOrigin + 1; x <= xOrigin + Width; x++)
                    {
                        dIdx = z * sliceArea + y * width + x;

                        VectorCache[1] = new Vector3(x, y, z) * ratio - Vector3.One;
                        VectorCache[2] = new Vector3(x, y + 1, z) * ratio - Vector3.One;
                        VectorCache[5] = new Vector3(x, y, z + 1) * ratio - Vector3.One;
                        VectorCache[6] = new Vector3(x, y + 1, z + 1) * ratio - Vector3.One;
                        DensityCache[1] = DensityField[dIdx];
                        DensityCache[2] = DensityField[dIdx + width];
                        DensityCache[5] = DensityField[dIdx + sliceArea];
                        DensityCache[6] = DensityField[dIdx + sliceArea + width];
                        /*
                           Determine the index into the edge table which
                           tells us which vertices are inside of the surface
                        */
                        int cubeindex = 0;
                        if (DensityCache[0] > IsoValue) cubeindex |= 1;
                        if (DensityCache[1] > IsoValue) cubeindex |= 2;
                        if (DensityCache[2] > IsoValue) cubeindex |= 4;
                        if (DensityCache[3] > IsoValue) cubeindex |= 8;
                        if (DensityCache[4] > IsoValue) cubeindex |= 16;
                        if (DensityCache[5] > IsoValue) cubeindex |= 32;
                        if (DensityCache[6] > IsoValue) cubeindex |= 64;
                        if (DensityCache[7] > IsoValue) cubeindex |= 128;

                        /* Cube is entirely in/out of the surface */
                        if (cubeindex != 0 && cubeindex != 255)
                        {
                            /*0-r
                            1-r+x
                            2-r+x+y
                            3-r+y
                            4-r+z
                            5-r+x+z
                            6-r+x+y+z
                            7-r+y+z
                            */
                            //Now lets generate some normal vectors!
                            Vector3[] NormalCache = new Vector3[8];
                            NormalCache[0] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y, z);
                            NormalCache[1] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y, z);
                            NormalCache[2] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y + 1, z);
                            NormalCache[3] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y + 1, z);
                            NormalCache[4] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y, z + 1);
                            NormalCache[5] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y, z + 1);
                            NormalCache[6] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y + 1, z + 1);
                            NormalCache[7] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y + 1, z + 1);
                            for (int i = 0; VoxelHelper.NewTriangleTable2[cubeindex, i] != -1; i += 3)
                            {
                                int[] edgeIndices = new int[3];
                                for (int j = 0; j < 3; j++)
                                {
                                    int idx = GetEdgeId(DensityFieldWidth, DensityFieldHeight, x, y, z, VoxelHelper.NewTriangleTable2[cubeindex, i + j]);
                                    edgeIndices[j] = idx;
                               
                                    if (!_edgeToIndices.ContainsKey(idx))
                                    {
                                        _edgeToIndices.Add(idx, (ushort)_vertices.Count);

                                        _vertices.Add(GenerateVertex(VoxelHelper.NewTriangleTable2[cubeindex, i + j], VectorCache, NormalCache, DensityCache, IsoValue));
                                    }
                                    _indices.Add(_edgeToIndices[idx]);
                                }
                                ushort id0 = _indices[_indices.Count - 3];
                                ushort id1 = _indices[_indices.Count - 2];
                                ushort id2 = _indices[_indices.Count - 1];
                                Vector3 v0 = Vector3.Transform(_vertices[id0].Position, transform);
                                Vector3 v1 = Vector3.Transform(_vertices[id1].Position, transform);
                                Vector3 v2 = Vector3.Transform(_vertices[id2].Position, transform);

                                PrimitiveCount++;
                            }
                        }
                        //Swap our caches
                        VectorCache[0] = VectorCache[1];
                        VectorCache[3] = VectorCache[2];
                        VectorCache[4] = VectorCache[5];
                        VectorCache[7] = VectorCache[6];
                        DensityCache[0] = DensityCache[1];
                        DensityCache[3] = DensityCache[2];
                        DensityCache[4] = DensityCache[5];
                        DensityCache[7] = DensityCache[6];
                    }
                }
            }

            verts = _vertices.ToArray();
            ib = _indices.ToArray();
            if (verts.Length > 0)
            {
                renderElement.VertexCount = verts.Length;
                renderElement.VertexBuffer = new VertexBuffer(device, verts.Length * VertexPositionNormal.SizeInBytes, BufferUsage.WriteOnly);
                renderElement.VertexBuffer.SetData<VertexPositionNormal>(verts);
            }
            if (ib.Length > 0)
            {
                renderElement.IndexBuffer = new IndexBuffer(device, ib.Length * sizeof(ushort), BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                renderElement.IndexBuffer.SetData<ushort>(ib);
                renderElement.PrimitiveCount = PrimitiveCount;
            }

            CanRender = (PrimitiveCount > 0);
        }

        int GetEdgeId(int DensityFieldWidth, int DensityFieldHeight, int x, int y, int z, int edgeName)
        {
            switch (edgeName)
            {
                case 0:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z);
                case 1:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z) + 1;
                case 2:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z);
                case 3:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 1;
                case 4:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z + 1);
                case 5:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z + 1) + 1;
                case 6:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z + 1);
                case 7:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z + 1) + 1;
                case 8:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 2;
                case 9:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z) + 2;
                case 10:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y + 1, z) + 2;
                case 11:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z) + 2;
                default:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 3;
            }
        }

        int GetVertexId(int DensityFieldWidth, int DensityFieldHeight, int x, int y, int z)
        {
            return 4 * (DensityFieldWidth * (z * DensityFieldHeight + y) + x); //Vertex centroids
        }

        VertexPositionNormal VertexInterp(byte IsoValue, Vector3 p1, Vector3 p2, Vector3 n1, Vector3 n2, byte valp1, byte valp2)
        {
            float eps = 0.001f;
            float b = valp2 - valp1;
            if (Math.Abs(IsoValue - valp1) < eps || Math.Abs(b) < eps)
                return new VertexPositionNormal(p1, n1);//N1, T1);
            if (Math.Abs(IsoValue - valp2) < eps)
                return new VertexPositionNormal(p2, n2);//N2, T2);

            float mu = (float)(IsoValue - valp1) / b;

            return new VertexPositionNormal(p1 + mu * (p2 - p1), n1 + mu * (n2 - n1));
        }

        VertexPositionNormal GenerateVertex(int edge, Vector3[] vecs, Vector3[] normals, byte[] densities, byte isoValue)
        {
            switch (edge)
            {
                case 0:
                    return VertexInterp(isoValue, vecs[0], vecs[1], normals[0], normals[1], densities[0], densities[1]);
                case 1:
                    return VertexInterp(isoValue, vecs[1], vecs[2], normals[1], normals[2], densities[1], densities[2]);
                case 2:
                    return VertexInterp(isoValue, vecs[2], vecs[3], normals[2], normals[3], densities[2], densities[3]);
                case 3:
                    return VertexInterp(isoValue, vecs[3], vecs[0], normals[3], normals[0], densities[3], densities[0]);
                case 4:
                    return VertexInterp(isoValue, vecs[4], vecs[5], normals[4], normals[5], densities[4], densities[5]);
                case 5:
                    return VertexInterp(isoValue, vecs[5], vecs[6], normals[5], normals[6], densities[5], densities[6]);
                case 6:
                    return VertexInterp(isoValue, vecs[6], vecs[7], normals[6], normals[7], densities[6], densities[7]);
                case 7:
                    return VertexInterp(isoValue, vecs[7], vecs[4], normals[7], normals[4], densities[7], densities[4]);
                case 8:
                    return VertexInterp(isoValue, vecs[0], vecs[4], normals[0], normals[4], densities[0], densities[4]);
                case 9:
                    return VertexInterp(isoValue, vecs[1], vecs[5], normals[1], normals[5], densities[1], densities[5]);
                case 10:
                    return VertexInterp(isoValue, vecs[2], vecs[6], normals[2], normals[6], densities[2], densities[6]);
                case 11:
                    return VertexInterp(isoValue, vecs[3], vecs[7], normals[3], normals[7], densities[3], densities[7]);
                default:
                    return new VertexPositionNormal((vecs[0] + vecs[6]) * 0.5f, Vector3.Lerp(normals[0], normals[6], 0.5f));//Centroid
            }
        }

        Vector3 ComputeNormal(ref byte[] DensityField, int DensityFieldWidth, int DensityFieldHeight, int DensityFieldDepth, int x, int y, int z)
        {
            int sliceArea = DensityFieldWidth * DensityFieldHeight;
            int idx = x + DensityFieldWidth * y + z * sliceArea;
            int x0 = (x - 1 >= 0) ? -1 : 0;
            int x1 = (x + 1 < DensityFieldWidth) ? 1 : 0;
            int y0 = (y - 1 >= 0) ? -DensityFieldWidth : 0;
            int y1 = (y + 1 < DensityFieldHeight) ? DensityFieldWidth : 0;
            int z0 = (z - 1 >= 0) ? -sliceArea : 0;
            int z1 = (z + 1 < DensityFieldDepth) ? sliceArea : 0;

            //Take the negative gradient (hence the x0-x1)
            Vector3 nrm = new Vector3(DensityField[idx + x0] - DensityField[idx + x1], DensityField[idx + y0] - DensityField[idx + y1], DensityField[idx + z0] - DensityField[idx + z1]);

            double magSqr = nrm.X * nrm.X + nrm.Y * nrm.Y + nrm.Z * nrm.Z + 0.0001; //Regularization constant (very important!)
            double invMag = 1.0 / Math.Sqrt(magSqr);
            nrm.X = (float)(nrm.X * invMag);
            nrm.Y = (float)(nrm.Y * invMag);
            nrm.Z = (float)(nrm.Z * invMag);

            return nrm;
        }
    }

    public static class VoxelHelper
    {
        public static int[,] NewTriangleTable = new int[256, 22]
        {
	        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 0
	        { 0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 1
	        { 0,  1,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 2
	        { 1,  8,  3,  9,  8,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 3
	        { 1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 4
	        { 0,  8,  3,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 5
	        { 9,  2, 10,  0,  2,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 6
	        { 3,  9,  8,  3,  2,  9,  2, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 7
	        { 3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 8
	        { 0, 11,  2,  8, 11,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 9
	        { 1,  9,  0,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 10
	        { 2,  8, 11,  2,  1,  8,  1,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 11
	        { 3, 10,  1, 11, 10,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 12
	        { 1, 11, 10,  1,  0, 11,  0,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 13
	        { 0, 10,  9,  0,  3, 10,  3, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 14
	        { 9,  8, 10, 10,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 15
	        { 4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 16
	        { 4,  3,  0,  7,  3,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 17
	        { 0,  1,  9,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 18
	        { 9,  3,  1,  9,  4,  3,  4,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 19
	        { 1,  2, 10,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 20 
	        { 3,  4,  7,  3,  0,  4,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 21
	        { 9,  2, 10,  9,  0,  2,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 22
	        { 2, 10,  9,  4,  7,  2,  2,  7,  3,  4,  2,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 23
	        { 8,  4,  7,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 24
	        { 7,  0,  4,  7, 11,  0, 11,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 25
	        { 9,  0,  1,  8,  4,  7,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 26
	        { 4,  7, 11,  9,  4, 11,  9, 11,  2,  9,  2,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 27
	        //{ 9,  4, 12,  4,  7, 12,  7, 11, 12, 11,  2, 12,  2,  1, 12,  1,  9, 12, -1, -1, -1, -1}, // 27 (Hexagon).
	        { 3, 10,  1,  3, 11, 10,  7,  8,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 28
	        { 1, 11, 10,  7, 11,  1,  1,  0,  4,  7,  1,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 29
	        { 4,  7,  8,  3, 11, 10,  3, 10,  0,  0, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 30
	        { 7, 11, 10,  7, 10,  4,  4, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 31
	        { 9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 32
	        { 9,  5,  4,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 33
	        { 0,  5,  4,  1,  5,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 34
	        { 4,  1,  5,  4,  8,  1,  8,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 35
	        { 1,  2, 10,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 36
	        { 3,  0,  8,  1,  2, 10,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 37
	        {10,  0,  2, 10,  5,  0,  5,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 38
	        { 2, 10,  5,  3,  2,  5,  3,  5,  4,  3,  4,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 39
	        //{10,  5, 12,  5,  4, 12,  4,  8, 12,  8,  3, 12,  3,  2, 12,  2, 10, 12, -1, -1, -1, -1}, // 39 (Hexagon).
	        { 9,  5,  4,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 40
	        { 0, 11,  2,  0,  8, 11,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 41
	        { 0,  5,  4,  0,  1,  5,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 42
	        { 2,  1,  5,  4,  8,  2,  2,  8, 11,  4,  2,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 43
	        {10,  3, 11, 10,  1,  3,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 44
	        { 4,  9,  5,  0,  8, 11,  0, 11,  1,  1, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 45
	        { 5,  4,  0,  3, 11,  5,  5, 11, 10,  3,  5,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 46
	        { 4,  8, 11,  4, 11,  5,  5, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 47
	        { 9,  7,  8,  5,  7,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 48
	        { 0,  7,  3,  0,  9,  7,  9,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 49
	        { 8,  5,  7,  8,  0,  5,  0,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 50
	        { 1,  5,  3,  3,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 51
	        { 9,  7,  8,  9,  5,  7, 10,  1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 52
	        {10,  1,  2,  9,  5,  7,  9,  7,  0,  0,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 53
	        { 8,  0,  2, 10,  5,  8,  8,  5,  7, 10,  8,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 54
	        {10,  5,  7, 10,  7,  2,  2,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 55
	        { 7,  9,  5,  7,  8,  9,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 56
	        { 9,  5,  7, 11,  2,  9,  9,  2,  0, 11,  9,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 57
	        { 2,  3, 11,  0,  1,  5,  0,  5,  8,  8,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 58
	        { 2,  1,  5,  2,  5, 11, 11,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 59
	        { 9,  5,  8,  8,  5,  7, 10,  1,  3, 10,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 60
	        { 9,  5,  7,  9,  7,  0,  7, 11,  0,  0, 11,  1,  1, 11, 10, -1, -1, -1, -1, -1, -1, -1}, // (new) 61
	        { 3, 11, 10,  3, 10,  0, 10,  5,  0,  0,  5,  8,  8,  5,  7, -1, -1, -1, -1, -1, -1, -1}, // (new) 62
	        {11, 10,  5,  7, 11,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 63
	        {10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 64
	        { 0,  8,  3,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 65
	        { 9,  0,  1,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 66
	        { 1,  8,  3,  1,  9,  8,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 67
	        { 1,  6,  5,  2,  6,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 68
	        { 1,  6,  5,  1,  2,  6,  3,  0,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 69
	        { 5,  2,  6,  5,  9,  2,  9,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 70
	        { 5,  9,  8,  3,  2,  5,  5,  2,  6,  3,  5,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 71
	        { 2,  3, 11, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 72
	        {11,  0,  8, 11,  2,  0, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 73
	        { 0,  1,  9,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 74
	        { 5, 10,  6,  1,  9,  8,  1,  8,  2,  2,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 75
	        {11,  1,  3, 11,  6,  1,  6,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 76
	        { 0,  8, 11,  6,  5,  0,  0,  5,  1,  6,  0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 77
	        { 3, 11,  6,  0,  3,  6,  0,  6,  5,  0,  5,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 78
	        //{ 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12, -1, -1, -1, -1}, // 78 (Hexagon).
	        { 5,  9,  8,  5,  8,  6,  6,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 79
	        { 5, 10,  6,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 80
	        { 4,  3,  0,  4,  7,  3,  6,  5, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 81
	        { 1,  9,  0,  5, 10,  6,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 82
	        {10,  6,  5,  4,  7,  3,  4,  3,  9,  9,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 83
	        { 6,  1,  2,  6,  5,  1,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 84
	        { 1,  2,  5,  5,  2,  6,  3,  0,  4,  3,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 85
	        { 8,  4,  7,  9,  0,  2,  9,  2,  5,  5,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 86
	        { 4,  7,  3,  4,  3,  9,  3,  2,  9,  9,  2,  5,  5,  2,  6, -1, -1, -1, -1, -1, -1, -1}, // (new) 87
	        { 3, 11,  2,  7,  8,  4, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 88
	        { 5, 10,  6, 11,  2,  0, 11,  0,  7,  7,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 89
	        { 0,  1,  9,  4,  7,  8,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 90
	        { 9,  2,  1,  9, 11,  2,  9,  4, 11,  7, 11,  4,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1}, // 91
	       //{ 2,  1, 12, 11,  2, 12,  7, 11, 12,  4,  7, 12,  9,  4, 12,  1,  9, 12,  6,  5, 10, -1}, // 91 (Hexagon and triangle).
	        { 8,  4,  7,  6,  5,  1,  6,  1, 11, 11,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 92
	        { 6,  5,  1,  6,  1, 11,  1,  0, 11, 11,  0,  7,  7,  0,  4, -1, -1, -1, -1, -1, -1, -1}, // (new) 93
	        { 0,  5,  9,  0,  6,  5,  0,  3,  6, 11,  6,  3,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1}, // 94 (Hexagon).
	        //{ 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12,  8,  4,  7, -1}, // 94 (Hexagon and triangle).
	        { 6,  5,  9,  6,  9, 11,  4,  7,  9,  7, 11,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 95
	        //{ 6,  5, 12,  6, 12, 11,  4,  7, 12,  7, 11, 12,  5,  9, 12,  4, 12,  9, -1, -1, -1, -1}, // 95 (new folded-hexagon with an additional vertex).
	        {10,  4,  9,  6,  4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 96
	        { 4, 10,  6,  4,  9, 10,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 97
	        { 1,  4,  0,  1, 10,  4, 10,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 98
	        { 8,  3,  1, 10,  6,  8,  8,  6,  4, 10,  8,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 99
	        { 9,  6,  4,  9,  1,  6,  1,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 100
	        { 3,  0,  8,  1,  2,  6,  1,  6,  9,  9,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 101
	        { 0,  2,  4,  4,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 102
	        { 3,  2,  6,  3,  6,  8,  8,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 103
	        {10,  4,  9, 10,  6,  4, 11,  2,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 104
	        { 0,  8,  2,  2,  8, 11,  4,  9, 10,  4, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 105
	        { 3, 11,  2, 10,  6,  4, 10,  4,  1,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 106
	        {10,  6,  4, 10,  4,  1,  4,  8,  1,  1,  8,  2,  2,  8, 11, -1, -1, -1, -1, -1, -1, -1}, // (new) 107
	        { 9,  6,  4, 11,  6,  9,  9,  1,  3, 11,  9,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 108
	        { 0,  8, 11,  0, 11,  1, 11,  6,  1,  1,  6,  9,  9,  6,  4, -1, -1, -1, -1, -1, -1, -1}, // (new) 109
	        {11,  6,  4, 11,  4,  3,  3,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 110
	        { 6,  4,  8, 11,  6,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 111
	        { 6,  9, 10,  6,  7,  9,  7,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 112
	        { 0,  7,  3,  6,  7,  0,  0,  9, 10,  6,  0, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 113
	        {10,  6,  7,  1, 10,  7,  1,  7,  8,  1,  8,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 114
	        //{ 0,  1, 12,  1, 10, 12, 10,  6, 12,  6,  7, 12,  7,  8, 12,  8,  0, 12, -1, -1, -1, -1}, // 114 (Hexagon).
	        { 6,  7,  3,  6,  3, 10, 10,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 115
	        { 1,  2,  6,  7,  8,  1,  1,  8,  9,  7,  1,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 116
	        { 1,  2,  6,  1,  6,  9,  6,  7,  9,  9,  7,  0,  0,  7,  3, -1, -1, -1, -1, -1, -1, -1}, // (new) 117
	        { 8,  0,  2,  8,  2,  7,  7,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 118
	        { 7,  3,  2,  6,  7,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 119
	        { 2,  3, 11,  7,  8,  9,  7,  9,  6,  6,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 120
	        {11,  2,  0, 11,  0,  7,  0,  9,  7,  7,  9,  6,  6,  9, 10, -1, -1, -1, -1, -1, -1, -1}, // (new) 121
	        { 1,  8,  0,  1,  7,  8,  1, 10,  7,  6,  7, 10,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1}, // 122
	        //{ 0,  1, 12,  1, 10, 12, 10,  6, 12,  6,  7, 12,  7,  8, 12,  8,  0, 12,  2,  3, 11, -1}, // 122 (Hexagon and triangle).
	        {11,  2,  1, 11,  1,  7, 10,  6,  1,  6,  7,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 123
	        //{11,  2, 12, 11, 12,  7, 10,  6, 12,  6,  7, 12,  2,  1, 12, 10, 12,  1, -1, -1, -1, -1}, // 123 (new folded-hexagon with an additional vertex).
	        { 7,  8,  9,  7,  9,  6,  9,  1,  6,  6,  1, 11, 11,  1,  3, -1, -1, -1, -1, -1, -1, -1}, // (new) 124
	        { 0,  9,  1, 11,  6,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 125
	        { 7,  8,  0,  7,  0,  6,  3, 11,  0, 11,  6,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 126
	        //{ 7,  8, 12,  7, 12,  6,  3, 11, 12, 11,  6, 12,  8,  0, 12,  3, 12,  0, -1, -1, -1, -1}, // 126 (new folded-hexagon with an additional vertex).
	        { 7, 11,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 127
	        { 7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 128
	        { 3,  0,  8, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 129
	        { 0,  1,  9, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 130
	        { 8,  1,  9,  8,  3,  1, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 131
	        {10,  1,  2,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 132
	        { 1,  2, 10,  3,  0,  8,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 133
	        { 2,  9,  0,  2, 10,  9,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 134
	        { 6, 11,  7,  2, 10,  9,  2,  9,  3,  3,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 135
	        { 7,  2,  3,  6,  2,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 136
	        { 8,  2,  0,  8,  7,  2,  7,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 137
	        { 2,  7,  6,  2,  3,  7,  0,  1,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 138
	        { 1,  6,  2,  7,  6,  1,  1,  9,  8,  7,  1,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 139
	        { 6,  3,  7,  6, 10,  3, 10,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 140
	        {10,  7,  6,  1,  7, 10,  1,  8,  7,  1,  0,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 141
	        //{ 8,  7, 12,  7,  6, 12,  6, 10, 12, 10,  1, 12,  1,  0, 12,  0,  8, 12, -1, -1, -1, -1}, // 141 (Hexagon).
	        { 0,  3,  7,  6, 10,  0,  0, 10,  9,  6,  0,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 142
	        { 6, 10,  9,  6,  9,  7,  7,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 143
	        { 6,  8,  4, 11,  8,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 144
	        {11,  4,  6, 11,  3,  4,  3,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 145
	        { 8,  6, 11,  8,  4,  6,  9,  0,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 146
	        { 9,  4,  6, 11,  3,  9,  9,  3,  1, 11,  9,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 147
	        { 6,  8,  4,  6, 11,  8,  2, 10,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 148
	        { 1,  2, 10,  3,  0,  4,  3,  4, 11, 11,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 149
	        { 4, 11,  8,  4,  6, 11,  0,  2,  9,  2, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 150
	        { 2, 10,  9,  2,  9,  3,  9,  4,  3,  3,  4, 11, 11,  4,  6, -1, -1, -1, -1, -1, -1, -1}, // (new) 151
	        { 3,  6,  2,  3,  8,  6,  8,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 152
	        { 0,  4,  2,  4,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 153
	        { 1,  9,  0,  8,  4,  6,  8,  6,  3,  3,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 154
	        { 9,  4,  6,  9,  6,  1,  1,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 155
	        { 8,  1,  3, 10,  1,  8,  8,  4,  6, 10,  8,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 156
	        { 1,  0,  4,  1,  4, 10, 10,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 157
	        { 8,  4,  6,  8,  6,  3,  6, 10,  3,  3, 10,  0,  0, 10,  9, -1, -1, -1, -1, -1, -1, -1}, // (new) 158
	        {10,  9,  4,  6, 10,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 159
	        { 4,  9,  5,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 160
	        { 0,  8,  3,  4,  9,  5, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 161
	        { 5,  0,  1,  5,  4,  0,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 162
	        {11,  7,  6,  8,  3,  1,  8,  1,  4,  4,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 163
	        { 9,  5,  4, 10,  1,  2,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 164
	        { 6, 11,  7,  1,  2, 10,  0,  8,  3,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 165
	        { 7,  6, 11,  5,  4,  0,  5,  0, 10, 10,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 166
	        { 3,  4,  8,  3,  5,  4,  3,  2,  5, 10,  5,  2, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1}, // 167
	        //{ 3,  2, 12,  8,  3, 12,  4,  8, 12,  5,  4, 12, 10,  5, 12,  2, 10, 12,  7,  6, 11, -1}, // 167 (Hexagon and triangle).
	        { 7,  2,  3,  7,  6,  2,  5,  4,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 168
	        { 9,  5,  4,  7,  6,  2,  7,  2,  8,  8,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 169
	        { 3,  6,  2,  3,  7,  6,  1,  5,  0,  5,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 170
	        { 6,  2,  7,  7,  2,  8,  2,  1,  8,  8,  1,  4,  4,  1,  5, -1, -1, -1, -1, -1, -1, -1}, // (new) 171
	        { 9,  5,  4, 10,  1,  3, 10,  3,  6,  6,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 172
	        { 1,  6, 10,  1,  7,  6,  1,  0,  7,  8,  7,  0,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1}, // 173
	        //{ 1,  0, 12, 10,  1, 12,  6, 10, 12,  7,  6, 12,  8,  7, 12,  0,  8, 12,  5,  4,  9, -1}, // 173 (Hexagon and triangle).
	        { 4,  0,  5,  5,  0, 10,  0,  3, 10, 10,  3,  6,  6,  3,  7, -1, -1, -1, -1, -1, -1, -1}, // (new) 174
	        { 7,  6, 10,  7, 10,  8,  5,  4, 10,  4,  8, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 175
	        //{ 7,  6, 12,  7, 12,  8,  5,  4, 12,  4,  8, 12,  6, 10, 12,  5, 12, 10, -1, -1, -1, -1}, // 175 (new folded-hexagon with an additional vertex).
	        { 5,  8,  9,  5,  6,  8,  6, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 176
	        { 3,  6, 11,  0,  6,  3,  0,  5,  6,  0,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 177
	        //{ 9,  5, 12,  5,  6, 12,  6, 11, 12, 11,  3, 12,  3,  0, 12,  0,  9, 12, -1, -1, -1, -1}, // 177 (Hexagon).
	        { 0, 11,  8,  6, 11,  0,  0,  1,  5,  6,  0,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 178
	        {11,  3,  1, 11,  1,  6,  6,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 179
	        { 1,  2, 10,  6, 11,  8,  6,  8,  5,  5,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 180
	        { 0, 11,  3,  0,  6, 11,  0,  9,  6,  5,  6,  9,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1}, // 181
	        //{ 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12,  1,  2, 10, -1}, // 181 (Hexagon and triangle).
	        { 6, 11,  8,  6,  8,  5,  8,  0,  5,  5,  0, 10, 10,  0,  2, -1, -1, -1, -1, -1, -1, -1}, // (new) 182
	        { 6, 11,  3,  6,  3,  5,  2, 10,  3, 10,  5,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 183
	        //{ 6, 11, 12,  6, 12,  5,  2, 10, 12, 10,  5, 12, 11,  3, 12,  2, 12,  3, -1, -1, -1, -1}, // 183 (new folded-hexagon with an additional vertex).
	        { 5,  8,  9,  3,  8,  5,  5,  6,  2,  3,  5,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 184
	        { 5,  6,  2,  5,  2,  9,  9,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 185
	        { 0,  1,  5,  0,  5,  8,  5,  6,  8,  8,  6,  3,  3,  6,  2, -1, -1, -1, -1, -1, -1, -1}, // (new) 186
	        { 1,  5,  6,  2,  1,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 187
	        { 1,  3, 10, 10,  3,  6,  3,  8,  6,  6,  8,  5,  5,  8,  9, -1, -1, -1, -1, -1, -1, -1}, // (new) 188
	        {10,  1,  0, 10,  0,  6,  9,  5,  0,  5,  6,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 189
	        //{10,  1, 12, 10, 12,  6,  9,  5, 12,  5,  6, 12,  1,  0, 12,  9, 12,  0, -1, -1, -1, -1}, // 189 (new folded-hexagon with an additional vertex).
	        { 0,  3,  8,  5,  6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 190
	        {10,  5,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 191
	        {11,  5, 10,  7,  5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 192
	        {11,  5, 10, 11,  7,  5,  8,  3,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 193
	        { 5, 11,  7,  5, 10, 11,  1,  9,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 194
	        {10,  7,  5, 10, 11,  7,  9,  8,  1,  8,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 195
	        { 2,  5,  1,  2, 11,  5, 11,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 196
	        { 0,  8,  3, 11,  7,  5, 11,  5,  2,  2,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 197
	        { 9,  7,  5, 11,  7,  9,  9,  0,  2, 11,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 198
	        {11,  7,  5, 11,  5,  2,  5,  9,  2,  2,  9,  3,  3,  9,  8, -1, -1, -1, -1, -1, -1, -1}, // (new) 199
	        {10,  7,  5, 10,  2,  7,  2,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 200
	        { 8,  2,  0, 10,  2,  8,  8,  7,  5, 10,  8,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 201
	        { 9,  0,  1,  2,  3,  7,  2,  7, 10, 10,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 202
	        { 1,  9,  8,  1,  8,  2,  8,  7,  2,  2,  7, 10, 10,  7,  5, -1, -1, -1, -1, -1, -1, -1}, // (new) 203
	        { 1,  3,  5,  3,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 204
	        { 8,  7,  5,  8,  5,  0,  0,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 205
	        { 0,  3,  7,  0,  7,  9,  9,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 206
	        { 9,  8,  7,  5,  9,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 207
	        { 4, 11,  8,  4,  5, 11,  5, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 208
	        { 5,  0,  4,  3,  0,  5,  5, 10, 11,  3,  5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 209
	        { 0,  1,  9,  5, 10, 11,  5, 11,  4,  4, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 210
	        { 5, 10, 11,  5, 11,  4, 11,  3,  4,  4,  3,  9,  9,  3,  1, -1, -1, -1, -1, -1, -1, -1}, // (new) 211
	        { 2,  5,  1,  4,  5,  2,  2, 11,  8,  4,  2,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 212
	        { 3,  0,  4,  3,  4, 11,  4,  5, 11, 11,  5,  2,  2,  5,  1, -1, -1, -1, -1, -1, -1, -1}, // (new) 213
	        { 9,  0,  2,  9,  2,  5,  2, 11,  5,  5, 11,  4,  4, 11,  8, -1, -1, -1, -1, -1, -1, -1}, // (new) 214
	        { 9,  4,  5,  2, 11,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 215
	        { 2,  5, 10,  3,  5,  2,  3,  4,  5,  3,  8,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 216
	        //{ 2,  3, 12,  3,  8, 12,  8,  4, 12,  4,  5, 12,  5, 10, 12, 10,  2, 12, -1, -1, -1, -1}, // 216 (Hexagon).
	        {10,  2,  0, 10,  0,  5,  5,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 217
	        { 3, 10,  2,  3,  5, 10,  3,  8,  5,  4,  5,  8,  0,  1,  9, -1, -1, -1, -1, -1, -1, -1}, // 218
	        //{ 2,  3, 12,  3,  8, 12,  8,  4, 12,  4,  5, 12,  5, 10, 12, 10,  2, 12,  0,  1,  9, -1}, // 218 (Hexagon and triangle).
	        { 5, 10,  2,  5,  2,  4,  1,  9,  2,  9,  4,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 219
	        //{ 5, 10, 12,  5, 12,  4,  1,  9, 12,  9,  4, 12, 10,  2, 12,  1, 12,  2, -1, -1, -1, -1}, // 219 (new folded-hexagon with an additional vertex).
	        { 4,  5,  1,  4,  1,  8,  8,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 220
	        { 0,  4,  5,  1,  0,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 221
	        { 8,  4,  5,  8,  5,  3,  9,  0,  5,  0,  3,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 222
	        //{ 8,  4, 12,  8, 12,  3,  9,  0, 12,  0,  3, 12,  4,  5, 12,  9, 12,  5, -1, -1, -1, -1}, // 222 (new folded-hexagon with an additional vertex).
	        { 9,  4,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 223
	        { 7, 10, 11,  7,  4, 10,  4,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 224
	        { 0,  8,  3,  4,  9, 10,  4, 10,  7,  7, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 225
	        { 1, 10, 11,  1, 11,  7,  1,  7,  4,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 226
	        { 8,  3,  1,  8,  1,  4,  1, 10,  4,  4, 10,  7,  7, 10, 11, -1, -1, -1, -1, -1, -1, -1}, // (new) 227
	        { 4, 11,  7,  9, 11,  4,  9,  2, 11,  9,  1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 228
	        //{ 1,  2, 12,  2, 11, 12, 11,  7, 12,  7,  4, 12,  4,  9, 12,  9,  1, 12, -1, -1, -1, -1}, // 228
	        { 9,  7,  4,  9, 11,  7,  9,  1, 11,  2, 11,  1,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1}, // 229
	        //{ 1,  2, 12,  2, 11, 12, 11,  7, 12,  7,  4, 12,  4,  9, 12,  9,  1, 12,  0,  8,  3, -1}, // 229 (Hexagon and triangle).
	        { 7,  4,  0,  7,  0, 11, 11,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 230
	        {11,  7,  4, 11,  4,  2,  8,  3,  4,  3,  2,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 231
	        //{11,  7, 12, 11, 12,  2,  8,  3, 12,  3,  2, 12,  7,  4, 12,  8, 12,  4, -1, -1, -1, -1}, // 231 (new folded-hexagon with an additional vertex).
	        { 2,  9, 10,  2,  7,  4,  2,  3,  7,  4,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 232
	        { 4,  9, 10,  4, 10,  7, 10,  2,  7,  7,  2,  8,  8,  2,  0, -1, -1, -1, -1, -1, -1, -1}, // (new) 233
	        { 2,  3,  7,  2,  7, 10,  7,  4, 10, 10,  4,  1,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1}, // (new) 234
	        { 1, 10,  2,  8,  7,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 235
	        { 9,  1,  3,  9,  3,  4,  4,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 236
	        { 4,  9,  1,  4,  1,  7,  0,  8,  1,  8,  7,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 237
	        //{ 4,  9, 12,  4, 12,  7,  0,  8, 12,  8,  7, 12,  9,  1, 12,  0, 12,  1, -1, -1, -1, -1}, // 237 (new folded-hexagon with an additional vertex).
	        { 4,  0,  3,  7,  4,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 238
	        { 4,  8,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 239
	        { 9, 10,  8, 10, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 240 
	        { 0,  9, 10,  0, 10,  3,  3, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 241
	        { 1, 10, 11,  1, 11,  0,  0, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 242
	        { 3,  1, 10, 11,  3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 243
	        { 2, 11,  8,  2,  8,  1,  1,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 244
	        { 3,  0,  9,  3,  9, 11,  1,  2,  9,  2, 11,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 245
	        //{ 3,  0, 12,  3, 12, 11,  1,  2, 12,  2, 11, 12,  0,  9, 12,  1, 12,  9, -1, -1, -1, -1}, // 245 (new folded-hexagon with an additional vertex).
	        { 0,  2, 11,  8,  0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 246
	        { 3,  2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 247
	        { 3,  8,  9,  3,  9,  2,  2,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 248
	        { 9, 10,  2,  0,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 249
	        { 2,  3,  8,  2,  8, 10,  0,  1,  8,  1, 10,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 250
	        //{ 2,  3, 12,  2, 12, 10,  0,  1, 12,  1, 10, 12,  3,  8, 12,  0, 12,  8, -1, -1, -1, -1}, // 250 (new folded-hexagon with an additional vertex).
	        { 1, 10,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 251
	        { 1,  3,  8,  9,  1,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 252
	        { 0,  9,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 253
	        { 0,  3,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 254
	        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1} // 255
        };

        public static int[,] NewTriangleTable2 = new int[256, 22]
        {
	        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 0
	        { 0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 1
	        { 0,  1,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 2
	        { 1,  8,  3,  9,  8,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 3
	        { 1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 4
	        { 0,  8,  3,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 5
	        { 9,  2, 10,  0,  2,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 6
	        { 3,  9,  8,  3,  2,  9,  2, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 7
	        { 3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 8
	        { 0, 11,  2,  8, 11,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 9
	        { 1,  9,  0,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 10
	        { 2,  8, 11,  2,  1,  8,  1,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 11
	        { 3, 10,  1, 11, 10,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 12
	        { 1, 11, 10,  1,  0, 11,  0,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 13
	        { 0, 10,  9,  0,  3, 10,  3, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 14
	        { 9,  8, 10, 10,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 15
	        { 4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 16
	        { 4,  3,  0,  7,  3,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 17
	        { 0,  1,  9,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 18
	        { 9,  3,  1,  9,  4,  3,  4,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 19
	        { 1,  2, 10,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 20 
	        { 3,  4,  7,  3,  0,  4,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 21
	        { 9,  2, 10,  9,  0,  2,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 22
	        { 2, 10,  9,  4,  7,  2,  2,  7,  3,  4,  2,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 23
	        { 8,  4,  7,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 24
	        { 7,  0,  4,  7, 11,  0, 11,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 25
	        { 9,  0,  1,  8,  4,  7,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 26
	        //{ 4,  7, 11,  9,  4, 11,  9, 11,  2,  9,  2,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 27
	        { 9,  4, 12,  4,  7, 12,  7, 11, 12, 11,  2, 12,  2,  1, 12,  1,  9, 12, -1, -1, -1, -1}, // 27 (Hexagon).
	        { 3, 10,  1,  3, 11, 10,  7,  8,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 28
	        { 1, 11, 10,  7, 11,  1,  1,  0,  4,  7,  1,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 29
	        { 4,  7,  8,  3, 11, 10,  3, 10,  0,  0, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 30
	        { 7, 11, 10,  7, 10,  4,  4, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 31
	        { 9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 32
	        { 9,  5,  4,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 33
	        { 0,  5,  4,  1,  5,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 34
	        { 4,  1,  5,  4,  8,  1,  8,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 35
	        { 1,  2, 10,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 36
	        { 3,  0,  8,  1,  2, 10,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 37
	        {10,  0,  2, 10,  5,  0,  5,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 38
	        //{ 2, 10,  5,  3,  2,  5,  3,  5,  4,  3,  4,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 39
	        {10,  5, 12,  5,  4, 12,  4,  8, 12,  8,  3, 12,  3,  2, 12,  2, 10, 12, -1, -1, -1, -1}, // 39 (Hexagon).
	        { 9,  5,  4,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 40
	        { 0, 11,  2,  0,  8, 11,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 41
	        { 0,  5,  4,  0,  1,  5,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 42
	        { 2,  1,  5,  4,  8,  2,  2,  8, 11,  4,  2,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 43
	        {10,  3, 11, 10,  1,  3,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 44
	        { 4,  9,  5,  0,  8, 11,  0, 11,  1,  1, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 45
	        { 5,  4,  0,  3, 11,  5,  5, 11, 10,  3,  5,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 46
	        { 4,  8, 11,  4, 11,  5,  5, 11, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 47
	        { 9,  7,  8,  5,  7,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 48
	        { 0,  7,  3,  0,  9,  7,  9,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 49
	        { 8,  5,  7,  8,  0,  5,  0,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 50
	        { 1,  5,  3,  3,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 51
	        { 9,  7,  8,  9,  5,  7, 10,  1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 52
	        {10,  1,  2,  9,  5,  7,  9,  7,  0,  0,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 53
	        { 8,  0,  2, 10,  5,  8,  8,  5,  7, 10,  8,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 54
	        {10,  5,  7, 10,  7,  2,  2,  7,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 55
	        { 7,  9,  5,  7,  8,  9,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 56
	        { 9,  5,  7, 11,  2,  9,  9,  2,  0, 11,  9,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 57
	        { 2,  3, 11,  0,  1,  5,  0,  5,  8,  8,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 58
	        { 2,  1,  5,  2,  5, 11, 11,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 59
	        { 9,  5,  8,  8,  5,  7, 10,  1,  3, 10,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 60
	        { 9,  5,  7,  9,  7,  0,  7, 11,  0,  0, 11,  1,  1, 11, 10, -1, -1, -1, -1, -1, -1, -1}, // (new) 61
	        { 3, 11, 10,  3, 10,  0, 10,  5,  0,  0,  5,  8,  8,  5,  7, -1, -1, -1, -1, -1, -1, -1}, // (new) 62
	        {11, 10,  5,  7, 11,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 63
	        {10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 64
	        { 0,  8,  3,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 65
	        { 9,  0,  1,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 66
	        { 1,  8,  3,  1,  9,  8,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 67
	        { 1,  6,  5,  2,  6,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 68
	        { 1,  6,  5,  1,  2,  6,  3,  0,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 69
	        { 5,  2,  6,  5,  9,  2,  9,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 70
	        { 5,  9,  8,  3,  2,  5,  5,  2,  6,  3,  5,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 71
	        { 2,  3, 11, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 72
	        {11,  0,  8, 11,  2,  0, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 73
	        { 0,  1,  9,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 74
	        { 5, 10,  6,  1,  9,  8,  1,  8,  2,  2,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 75
	        {11,  1,  3, 11,  6,  1,  6,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 76
	        { 0,  8, 11,  6,  5,  0,  0,  5,  1,  6,  0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 77
	        //{ 3, 11,  6,  0,  3,  6,  0,  6,  5,  0,  5,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 78
	        { 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12, -1, -1, -1, -1}, // 78 (Hexagon).
	        { 5,  9,  8,  5,  8,  6,  6,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 79
	        { 5, 10,  6,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 80
	        { 4,  3,  0,  4,  7,  3,  6,  5, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 81
	        { 1,  9,  0,  5, 10,  6,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 82
	        {10,  6,  5,  4,  7,  3,  4,  3,  9,  9,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 83
	        { 6,  1,  2,  6,  5,  1,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 84
	        { 1,  2,  5,  5,  2,  6,  3,  0,  4,  3,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 85
	        { 8,  4,  7,  9,  0,  2,  9,  2,  5,  5,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 86
	        { 4,  7,  3,  4,  3,  9,  3,  2,  9,  9,  2,  5,  5,  2,  6, -1, -1, -1, -1, -1, -1, -1}, // (new) 87
	        { 3, 11,  2,  7,  8,  4, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 88
	        { 5, 10,  6, 11,  2,  0, 11,  0,  7,  7,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 89
	        { 0,  1,  9,  4,  7,  8,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 90
	        //{ 9,  2,  1,  9, 11,  2,  9,  4, 11,  7, 11,  4,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1}, // 91
	        { 2,  1, 12, 11,  2, 12,  7, 11, 12,  4,  7, 12,  9,  4, 12,  1,  9, 12,  6,  5, 10, -1}, // 91 (Hexagon and triangle).
	        { 8,  4,  7,  6,  5,  1,  6,  1, 11, 11,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 92
	        { 6,  5,  1,  6,  1, 11,  1,  0, 11, 11,  0,  7,  7,  0,  4, -1, -1, -1, -1, -1, -1, -1}, // (new) 93
	        //{ 0,  5,  9,  0,  6,  5,  0,  3,  6, 11,  6,  3,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1}, // 94 (Hexagon).
	        { 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12,  8,  4,  7, -1}, // 94 (Hexagon and triangle).
	        //{ 6,  5,  9,  6,  9, 11,  4,  7,  9,  7, 11,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 95
	        { 6,  5, 12,  6, 12, 11,  4,  7, 12,  7, 11, 12,  5,  9, 12,  4, 12,  9, -1, -1, -1, -1}, // 95 (new folded-hexagon with an additional vertex).
	        {10,  4,  9,  6,  4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 96
	        { 4, 10,  6,  4,  9, 10,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 97
	        { 1,  4,  0,  1, 10,  4, 10,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 98
	        { 8,  3,  1, 10,  6,  8,  8,  6,  4, 10,  8,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 99
	        { 9,  6,  4,  9,  1,  6,  1,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 100
	        { 3,  0,  8,  1,  2,  6,  1,  6,  9,  9,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 101
	        { 0,  2,  4,  4,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 102
	        { 3,  2,  6,  3,  6,  8,  8,  6,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 103
	        {10,  4,  9, 10,  6,  4, 11,  2,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 104
	        { 0,  8,  2,  2,  8, 11,  4,  9, 10,  4, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 105
	        { 3, 11,  2, 10,  6,  4, 10,  4,  1,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 106
	        {10,  6,  4, 10,  4,  1,  4,  8,  1,  1,  8,  2,  2,  8, 11, -1, -1, -1, -1, -1, -1, -1}, // (new) 107
	        { 9,  6,  4, 11,  6,  9,  9,  1,  3, 11,  9,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 108
	        { 0,  8, 11,  0, 11,  1, 11,  6,  1,  1,  6,  9,  9,  6,  4, -1, -1, -1, -1, -1, -1, -1}, // (new) 109
	        {11,  6,  4, 11,  4,  3,  3,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 110
	        { 6,  4,  8, 11,  6,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 111
	        { 6,  9, 10,  6,  7,  9,  7,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 112
	        { 0,  7,  3,  6,  7,  0,  0,  9, 10,  6,  0, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 113
	        //{10,  6,  7,  1, 10,  7,  1,  7,  8,  1,  8,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 114
	        { 0,  1, 12,  1, 10, 12, 10,  6, 12,  6,  7, 12,  7,  8, 12,  8,  0, 12, -1, -1, -1, -1}, // 114 (Hexagon).
	        { 6,  7,  3,  6,  3, 10, 10,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 115
	        { 1,  2,  6,  7,  8,  1,  1,  8,  9,  7,  1,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 116
	        { 1,  2,  6,  1,  6,  9,  6,  7,  9,  9,  7,  0,  0,  7,  3, -1, -1, -1, -1, -1, -1, -1}, // (new) 117
	        { 8,  0,  2,  8,  2,  7,  7,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 118
	        { 7,  3,  2,  6,  7,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 119
	        { 2,  3, 11,  7,  8,  9,  7,  9,  6,  6,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 120
	        {11,  2,  0, 11,  0,  7,  0,  9,  7,  7,  9,  6,  6,  9, 10, -1, -1, -1, -1, -1, -1, -1}, // (new) 121
	        //{ 1,  8,  0,  1,  7,  8,  1, 10,  7,  6,  7, 10,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1}, // 122
	        { 0,  1, 12,  1, 10, 12, 10,  6, 12,  6,  7, 12,  7,  8, 12,  8,  0, 12,  2,  3, 11, -1}, // 122 (Hexagon and triangle).
	        //{11,  2,  1, 11,  1,  7, 10,  6,  1,  6,  7,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 123
	        {11,  2, 12, 11, 12,  7, 10,  6, 12,  6,  7, 12,  2,  1, 12, 10, 12,  1, -1, -1, -1, -1}, // 123 (new folded-hexagon with an additional vertex).
	        { 7,  8,  9,  7,  9,  6,  9,  1,  6,  6,  1, 11, 11,  1,  3, -1, -1, -1, -1, -1, -1, -1}, // (new) 124
	        { 0,  9,  1, 11,  6,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 125
	        //{ 7,  8,  0,  7,  0,  6,  3, 11,  0, 11,  6,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 126
	        { 7,  8, 12,  7, 12,  6,  3, 11, 12, 11,  6, 12,  8,  0, 12,  3, 12,  0, -1, -1, -1, -1}, // 126 (new folded-hexagon with an additional vertex).
	        { 7, 11,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 127
	        { 7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 128
	        { 3,  0,  8, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 129
	        { 0,  1,  9, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 130
	        { 8,  1,  9,  8,  3,  1, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 131
	        {10,  1,  2,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 132
	        { 1,  2, 10,  3,  0,  8,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 133
	        { 2,  9,  0,  2, 10,  9,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 134
	        { 6, 11,  7,  2, 10,  9,  2,  9,  3,  3,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 135
	        { 7,  2,  3,  6,  2,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 136
	        { 8,  2,  0,  8,  7,  2,  7,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 137
	        { 2,  7,  6,  2,  3,  7,  0,  1,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 138
	        { 1,  6,  2,  7,  6,  1,  1,  9,  8,  7,  1,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 139
	        { 6,  3,  7,  6, 10,  3, 10,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 140
	        //{10,  7,  6,  1,  7, 10,  1,  8,  7,  1,  0,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 141
	        { 8,  7, 12,  7,  6, 12,  6, 10, 12, 10,  1, 12,  1,  0, 12,  0,  8, 12, -1, -1, -1, -1}, // 141 (Hexagon).
	        { 0,  3,  7,  6, 10,  0,  0, 10,  9,  6,  0,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 142
	        { 6, 10,  9,  6,  9,  7,  7,  9,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 143
	        { 6,  8,  4, 11,  8,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 144
	        {11,  4,  6, 11,  3,  4,  3,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 145
	        { 8,  6, 11,  8,  4,  6,  9,  0,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 146
	        { 9,  4,  6, 11,  3,  9,  9,  3,  1, 11,  9,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 147
	        { 6,  8,  4,  6, 11,  8,  2, 10,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 148
	        { 1,  2, 10,  3,  0,  4,  3,  4, 11, 11,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 149
	        { 4, 11,  8,  4,  6, 11,  0,  2,  9,  2, 10,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 150
	        { 2, 10,  9,  2,  9,  3,  9,  4,  3,  3,  4, 11, 11,  4,  6, -1, -1, -1, -1, -1, -1, -1}, // (new) 151
	        { 3,  6,  2,  3,  8,  6,  8,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 152
	        { 0,  4,  2,  4,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 153
	        { 1,  9,  0,  8,  4,  6,  8,  6,  3,  3,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 154
	        { 9,  4,  6,  9,  6,  1,  1,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 155
	        { 8,  1,  3, 10,  1,  8,  8,  4,  6, 10,  8,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 156
	        { 1,  0,  4,  1,  4, 10, 10,  4,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 157
	        { 8,  4,  6,  8,  6,  3,  6, 10,  3,  3, 10,  0,  0, 10,  9, -1, -1, -1, -1, -1, -1, -1}, // (new) 158
	        {10,  9,  4,  6, 10,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 159
	        { 4,  9,  5,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 160
	        { 0,  8,  3,  4,  9,  5, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 161
	        { 5,  0,  1,  5,  4,  0,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 162
	        {11,  7,  6,  8,  3,  1,  8,  1,  4,  4,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 163
	        { 9,  5,  4, 10,  1,  2,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 164
	        { 6, 11,  7,  1,  2, 10,  0,  8,  3,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 165
	        { 7,  6, 11,  5,  4,  0,  5,  0, 10, 10,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 166
	        //{ 3,  4,  8,  3,  5,  4,  3,  2,  5, 10,  5,  2, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1}, // 167
	        { 3,  2, 12,  8,  3, 12,  4,  8, 12,  5,  4, 12, 10,  5, 12,  2, 10, 12,  7,  6, 11, -1}, // 167 (Hexagon and triangle).
	        { 7,  2,  3,  7,  6,  2,  5,  4,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 168
	        { 9,  5,  4,  7,  6,  2,  7,  2,  8,  8,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 169
	        { 3,  6,  2,  3,  7,  6,  1,  5,  0,  5,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 170
	        { 6,  2,  7,  7,  2,  8,  2,  1,  8,  8,  1,  4,  4,  1,  5, -1, -1, -1, -1, -1, -1, -1}, // (new) 171
	        { 9,  5,  4, 10,  1,  3, 10,  3,  6,  6,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 172
	        //{ 1,  6, 10,  1,  7,  6,  1,  0,  7,  8,  7,  0,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1}, // 173
	        { 1,  0, 12, 10,  1, 12,  6, 10, 12,  7,  6, 12,  8,  7, 12,  0,  8, 12,  5,  4,  9, -1}, // 173 (Hexagon and triangle).
	        { 4,  0,  5,  5,  0, 10,  0,  3, 10, 10,  3,  6,  6,  3,  7, -1, -1, -1, -1, -1, -1, -1}, // (new) 174
	        //{ 7,  6, 10,  7, 10,  8,  5,  4, 10,  4,  8, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 175
	        { 7,  6, 12,  7, 12,  8,  5,  4, 12,  4,  8, 12,  6, 10, 12,  5, 12, 10, -1, -1, -1, -1}, // 175 (new folded-hexagon with an additional vertex).
	        { 5,  8,  9,  5,  6,  8,  6, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 176
	        //{ 3,  6, 11,  0,  6,  3,  0,  5,  6,  0,  9,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 177
	        { 9,  5, 12,  5,  6, 12,  6, 11, 12, 11,  3, 12,  3,  0, 12,  0,  9, 12, -1, -1, -1, -1}, // 177 (Hexagon).
	        { 0, 11,  8,  6, 11,  0,  0,  1,  5,  6,  0,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 178
	        {11,  3,  1, 11,  1,  6,  6,  1,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 179
	        { 1,  2, 10,  6, 11,  8,  6,  8,  5,  5,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 180
	        //{ 0, 11,  3,  0,  6, 11,  0,  9,  6,  5,  6,  9,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1}, // 181
	        { 0,  3, 12,  3, 11, 12, 11,  6, 12,  6,  5, 12,  5,  9, 12,  9,  0, 12,  1,  2, 10, -1}, // 181 (Hexagon and triangle).
	        { 6, 11,  8,  6,  8,  5,  8,  0,  5,  5,  0, 10, 10,  0,  2, -1, -1, -1, -1, -1, -1, -1}, // (new) 182
	        //{ 6, 11,  3,  6,  3,  5,  2, 10,  3, 10,  5,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 183
	        { 6, 11, 12,  6, 12,  5,  2, 10, 12, 10,  5, 12, 11,  3, 12,  2, 12,  3, -1, -1, -1, -1}, // 183 (new folded-hexagon with an additional vertex).
	        { 5,  8,  9,  3,  8,  5,  5,  6,  2,  3,  5,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 184
	        { 5,  6,  2,  5,  2,  9,  9,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 185
	        { 0,  1,  5,  0,  5,  8,  5,  6,  8,  8,  6,  3,  3,  6,  2, -1, -1, -1, -1, -1, -1, -1}, // (new) 186
	        { 1,  5,  6,  2,  1,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 187
	        { 1,  3, 10, 10,  3,  6,  3,  8,  6,  6,  8,  5,  5,  8,  9, -1, -1, -1, -1, -1, -1, -1}, // (new) 188
	        //{10,  1,  0, 10,  0,  6,  9,  5,  0,  5,  6,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 189
	        {10,  1, 12, 10, 12,  6,  9,  5, 12,  5,  6, 12,  1,  0, 12,  9, 12,  0, -1, -1, -1, -1}, // 189 (new folded-hexagon with an additional vertex).
	        { 0,  3,  8,  5,  6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 190
	        {10,  5,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 191
	        {11,  5, 10,  7,  5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 192
	        {11,  5, 10, 11,  7,  5,  8,  3,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 193
	        { 5, 11,  7,  5, 10, 11,  1,  9,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 194
	        {10,  7,  5, 10, 11,  7,  9,  8,  1,  8,  3,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 195
	        { 2,  5,  1,  2, 11,  5, 11,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 196
	        { 0,  8,  3, 11,  7,  5, 11,  5,  2,  2,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 197
	        { 9,  7,  5, 11,  7,  9,  9,  0,  2, 11,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 198
	        {11,  7,  5, 11,  5,  2,  5,  9,  2,  2,  9,  3,  3,  9,  8, -1, -1, -1, -1, -1, -1, -1}, // (new) 199
	        {10,  7,  5, 10,  2,  7,  2,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 200
	        { 8,  2,  0, 10,  2,  8,  8,  7,  5, 10,  8,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 201
	        { 9,  0,  1,  2,  3,  7,  2,  7, 10, 10,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 202
	        { 1,  9,  8,  1,  8,  2,  8,  7,  2,  2,  7, 10, 10,  7,  5, -1, -1, -1, -1, -1, -1, -1}, // (new) 203
	        { 1,  3,  5,  3,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 204
	        { 8,  7,  5,  8,  5,  0,  0,  5,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 205
	        { 0,  3,  7,  0,  7,  9,  9,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 206
	        { 9,  8,  7,  5,  9,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 207
	        { 4, 11,  8,  4,  5, 11,  5, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 208
	        { 5,  0,  4,  3,  0,  5,  5, 10, 11,  3,  5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 209
	        { 0,  1,  9,  5, 10, 11,  5, 11,  4,  4, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 210
	        { 5, 10, 11,  5, 11,  4, 11,  3,  4,  4,  3,  9,  9,  3,  1, -1, -1, -1, -1, -1, -1, -1}, // (new) 211
	        { 2,  5,  1,  4,  5,  2,  2, 11,  8,  4,  2,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 212
	        { 3,  0,  4,  3,  4, 11,  4,  5, 11, 11,  5,  2,  2,  5,  1, -1, -1, -1, -1, -1, -1, -1}, // (new) 213
	        { 9,  0,  2,  9,  2,  5,  2, 11,  5,  5, 11,  4,  4, 11,  8, -1, -1, -1, -1, -1, -1, -1}, // (new) 214
	        { 9,  4,  5,  2, 11,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 215
	        //{ 2,  5, 10,  3,  5,  2,  3,  4,  5,  3,  8,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 216
	        { 2,  3, 12,  3,  8, 12,  8,  4, 12,  4,  5, 12,  5, 10, 12, 10,  2, 12, -1, -1, -1, -1}, // 216 (Hexagon).
	        {10,  2,  0, 10,  0,  5,  5,  0,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 217
	        //{ 3, 10,  2,  3,  5, 10,  3,  8,  5,  4,  5,  8,  0,  1,  9, -1, -1, -1, -1, -1, -1, -1}, // 218
	        { 2,  3, 12,  3,  8, 12,  8,  4, 12,  4,  5, 12,  5, 10, 12, 10,  2, 12,  0,  1,  9, -1}, // 218 (Hexagon and triangle).
	        //{ 5, 10,  2,  5,  2,  4,  1,  9,  2,  9,  4,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 219
	        { 5, 10, 12,  5, 12,  4,  1,  9, 12,  9,  4, 12, 10,  2, 12,  1, 12,  2, -1, -1, -1, -1}, // 219 (new folded-hexagon with an additional vertex).
	        { 4,  5,  1,  4,  1,  8,  8,  1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 220
	        { 0,  4,  5,  1,  0,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 221
	        //{ 8,  4,  5,  8,  5,  3,  9,  0,  5,  0,  3,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 222
	        { 8,  4, 12,  8, 12,  3,  9,  0, 12,  0,  3, 12,  4,  5, 12,  9, 12,  5, -1, -1, -1, -1}, // 222 (new folded-hexagon with an additional vertex).
	        { 9,  4,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 223
	        { 7, 10, 11,  7,  4, 10,  4,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 224
	        { 0,  8,  3,  4,  9, 10,  4, 10,  7,  7, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 225
	        { 1, 10, 11,  1, 11,  7,  1,  7,  4,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 226
	        { 8,  3,  1,  8,  1,  4,  1, 10,  4,  4, 10,  7,  7, 10, 11, -1, -1, -1, -1, -1, -1, -1}, // (new) 227
	        //{ 4, 11,  7,  9, 11,  4,  9,  2, 11,  9,  1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 228
	        { 1,  2, 12,  2, 11, 12, 11,  7, 12,  7,  4, 12,  4,  9, 12,  9,  1, 12, -1, -1, -1, -1}, // 228 (Hexagon).
	        //{ 9,  7,  4,  9, 11,  7,  9,  1, 11,  2, 11,  1,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1}, // 229
	        { 1,  2, 12,  2, 11, 12, 11,  7, 12,  7,  4, 12,  4,  9, 12,  9,  1, 12,  0,  8,  3, -1}, // 229 (Hexagon and triangle).
	        { 7,  4,  0,  7,  0, 11, 11,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 230
	        //{11,  7,  4, 11,  4,  2,  8,  3,  4,  3,  2,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 231
	        {11,  7, 12, 11, 12,  2,  8,  3, 12,  3,  2, 12,  7,  4, 12,  8, 12,  4, -1, -1, -1, -1}, // 231 (new folded-hexagon with an additional vertex).
	        { 2,  9, 10,  2,  7,  4,  2,  3,  7,  4,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 232
	        { 4,  9, 10,  4, 10,  7, 10,  2,  7,  7,  2,  8,  8,  2,  0, -1, -1, -1, -1, -1, -1, -1}, // (new) 233
	        { 2,  3,  7,  2,  7, 10,  7,  4, 10, 10,  4,  1,  1,  4,  0, -1, -1, -1, -1, -1, -1, -1}, // (new) 234
	        { 1, 10,  2,  8,  7,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 235
	        { 9,  1,  3,  9,  3,  4,  4,  3,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 236
	        //{ 4,  9,  1,  4,  1,  7,  0,  8,  1,  8,  7,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 237
	        { 4,  9, 12,  4, 12,  7,  0,  8, 12,  8,  7, 12,  9,  1, 12,  0, 12,  1, -1, -1, -1, -1}, // 237 (new folded-hexagon with an additional vertex).
	        { 4,  0,  3,  7,  4,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 238
	        { 4,  8,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 239
	        { 9, 10,  8, 10, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 240 
	        { 0,  9, 10,  0, 10,  3,  3, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 241
	        { 1, 10, 11,  1, 11,  0,  0, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 242
	        { 3,  1, 10, 11,  3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 243
	        { 2, 11,  8,  2,  8,  1,  1,  8,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 244
	        //{ 3,  0,  9,  3,  9, 11,  1,  2,  9,  2, 11,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 245
	        { 3,  0, 12,  3, 12, 11,  1,  2, 12,  2, 11, 12,  0,  9, 12,  1, 12,  9, -1, -1, -1, -1}, // 245 (new folded-hexagon with an additional vertex).
	        { 0,  2, 11,  8,  0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 246
	        { 3,  2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 247
	        { 3,  8,  9,  3,  9,  2,  2,  9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // (new) 248
	        { 9, 10,  2,  0,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 249
	        //{ 2,  3,  8,  2,  8, 10,  0,  1,  8,  1, 10,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 250
	        { 2,  3, 12,  2, 12, 10,  0,  1, 12,  1, 10, 12,  3,  8, 12,  0, 12,  8, -1, -1, -1, -1}, // 250 (new folded-hexagon with an additional vertex).
	        { 1, 10,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 251
	        { 1,  3,  8,  9,  1,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 252
	        { 0,  9,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 253
	        { 0,  3,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}, // 254
	        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1} // 255
        };
    }
}
