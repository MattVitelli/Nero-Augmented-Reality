using System;
using Microsoft.Xna.Framework;

namespace NeroOS.Core
{
    public class Transform
    {
        Vector3 mPosition;
        Vector3 mRotation;
        Vector3 mScale;

        bool mDirtyMatrix;
        Matrix mWorldMatrix;
        Matrix mObjectMatrix;
        BoundingBox mBounds;

        public Transform()
        {
            mPosition = Vector3.Zero;
            mRotation = Vector3.Zero;
            mScale = Vector3.One;
            mWorldMatrix = Matrix.Identity;
            mObjectMatrix = Matrix.Identity;
            mDirtyMatrix = true;
        }

        public void SetPosition(Vector3 position)
        {
            mPosition = position;
            mDirtyMatrix = true;
        }

        public Vector3 GetPosition()
        {
            return mPosition;
        }

        public void SetRotation(Vector3 rotation)
        {
            mRotation = rotation;
            mDirtyMatrix = true;
        }

        public Vector3 GetRotation()
        {
            return mRotation;
        }

        public void SetScale(Vector3 scale)
        {
            mScale = scale;
            mDirtyMatrix = true;
        }

        public Vector3 GetScale()
        {
            return mScale;
        }

        public Matrix GetTransform()
        {
            if (mDirtyMatrix)
                UpdateMatrix();
            return mWorldMatrix;
        }

        public Matrix GetObjectSpace()
        {
            if (mDirtyMatrix)
                UpdateMatrix();
            return mObjectMatrix;
        }

        public BoundingBox GetBounds()
        {
            return mBounds;
        }

        void UpdateMatrix()
        {
            mWorldMatrix = Matrix.CreateScale(mScale) * Matrix.CreateFromYawPitchRoll(mRotation.Y, mRotation.X, mRotation.Z);
            mWorldMatrix.Translation = mPosition;
            mObjectMatrix = Matrix.Invert(mWorldMatrix);
            mDirtyMatrix = false;
            mBounds.Min = Vector3.Transform(-Vector3.One, mWorldMatrix);
            mBounds.Max = Vector3.Transform(Vector3.One, mWorldMatrix);
        }
    }
}
