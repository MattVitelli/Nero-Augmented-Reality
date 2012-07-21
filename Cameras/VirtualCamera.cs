using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NeroOS.Cameras
{
    public class VirtualCamera
    {
        bool updateView = true;
        Matrix view;
        Vector3 position = Vector3.Zero;
        Vector3 target = Vector3.Forward;

        bool updateProjection = true;
        Matrix projection;
        float fov = MathHelper.ToRadians(60);
        float aspectRatio = 16.0f/9.0f;
        float nearPlane = 0.0001f;
        float farPlane = 1000;

        Matrix viewProjection;

        public Matrix ViewProjection { get { return viewProjection; } }

        public float AspectRatio { get { return aspectRatio; } set { aspectRatio = value; updateProjection = true; } }

        public float FOV { get { return fov; } set { fov = value; updateProjection = true; } }

        public float FarPlane { get { return farPlane; } set { farPlane = value; updateProjection = true; } }

        public float NearPlane { get { return nearPlane; } set { nearPlane = value; updateProjection = true; } }

        public Vector3 Position { get { return position; } set { position = value; updateView = true; } }

        public Vector3 Target { get { return target; } set { target = value; updateView = true; } }

        public VirtualCamera(Vector3 worldPos, Vector3 target, float fov, float aspectRatio)
        {
            Position = worldPos;
            Target = target;
            FOV = fov;
            AspectRatio = aspectRatio;
        }

        public void Update()
        {
            if (updateView)
            {
                view = Matrix.CreateLookAt(position, position + target, Vector3.Up);
            }

            if (updateProjection)
            {
                projection = Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlane, farPlane);
            }

            if (updateProjection || updateView)
            {
                viewProjection = view * projection;
                updateProjection = false;
                updateView = false;
            }
        }
    }
}
