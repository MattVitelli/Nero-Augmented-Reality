using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing
{
    public class RenderView
    {
        BoundingFrustum frustum;
        Vector3 position;
        float farPlane;
        float nearPlane;

        Matrix projection;
        Matrix view;
        Matrix viewProjection;
        Matrix viewLocal;
        Matrix viewProjectionLocal;
        Matrix inverseViewProjection;
        Matrix inverseViewProjectionLocal;

        public Queue<RenderElement> Elements;

        public bool Processed; //Sets state for completed vs incompleted rendering

        bool dirtyMatrix;

        public RenderView(Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane)
        {
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.position = position;
            this.view = view;
            this.projection = projection;
            dirtyMatrix = true;
            Elements = new Queue<RenderElement>();
            Processed = true;

        }

        void ComputeMatrix()
        {
            if (!dirtyMatrix)
                return;

            dirtyMatrix = false;
            viewProjection = view * projection;
            viewLocal = view;
            viewLocal.Translation = Vector3.Zero;
            viewProjectionLocal = viewLocal * projection;
            frustum = new BoundingFrustum(viewProjection);
            inverseViewProjection = Matrix.Invert(viewProjection);
            inverseViewProjectionLocal = Matrix.Invert(viewProjectionLocal);

        }

        public void SetView(Matrix view)
        {
            this.view = view;
            dirtyMatrix = true;
        }

        public Matrix GetView()
        {
            return view;
        }

        public void SetProjection(Matrix projection)
        {
            this.projection = projection;
            dirtyMatrix = true;
        }

        public Matrix GetProjection()
        {
            return projection;
        }

        public Matrix GetViewProjection()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return viewProjection;
        }

        public Matrix GetViewProjectionLocal()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return viewProjectionLocal;
        }

        public Matrix GetInverseViewProjection()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return inverseViewProjection;
        }

        public Matrix GetInverseViewProjectionLocal()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return inverseViewProjectionLocal;
        }

        public BoundingFrustum GetFrustum()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return frustum;
        }

        public void SetPosition(Vector3 position)
        {
            this.position = position;
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public Vector4 GetEyePosShader()
        {
            return new Vector4(position, farPlane);
        }

        public static RenderView Identity = new RenderView(Matrix.Identity, Matrix.Identity, Vector3.Zero, 0.001f, 1000);

    }
}
