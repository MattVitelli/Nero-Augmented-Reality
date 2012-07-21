using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NeroOS.Core;
using NeroOS.Drawing;
namespace NeroOS.Sim
{
    public abstract class IProgram
    {
        protected Transform transform = new Transform();

        public virtual void OnCreate() 
        {
        }

        public virtual void OnDestroy() 
        { 
        
        }

        public virtual void OnPause() 
        { 
        
        }
        
        public virtual void OnResume() 
        { 
        
        }

        public virtual bool InteractCollision(BoundingSphere collisionPoint)
        {
            return (transform.GetBounds().Contains(collisionPoint) != ContainmentType.Disjoint);
        }

        public virtual void OnUpdate(float elapsedTime) { }
        public virtual void OnRender(Canvas canvas) { }
        public virtual void OnCollision() { }
        public virtual void OnInteract(BoundingSphere[] collisionPoints) { }

        public Transform GetTransform()
        {
            return transform;
        }
    }
}
