using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NeroOS.Core
{
    public struct State
    {
        public Vector3 position;
        public Vector3 velocity;
    };

    public struct Derivative
    {
        public Vector3 dx;
        public Vector3 dv;
    };

    public static class PhysicsHelper
    {
        public static Derivative evaluate(State initial, Vector3 acceleration, float dt, Derivative d)
        {
            State state;
            state.position = initial.position + d.dx * dt;
            state.velocity = initial.velocity + d.dv * dt;
            Derivative output;
            output.dx = state.velocity;
            output.dv = acceleration;
            return output;
        }

        public static State Integrate(State oldState, Vector3 accel, float timeDT)
        {
            Derivative orig;
            orig.dx = Vector3.Zero;
            orig.dv = Vector3.Zero;
            Derivative a = evaluate(oldState, accel, 0, orig);
            Derivative b = evaluate(oldState, accel, timeDT * 0.5f, a);
            Derivative c = evaluate(oldState, accel, timeDT * 0.5f, b);
            Derivative d = evaluate(oldState, accel, timeDT, c);

            Vector3 dxdt = 1.0f / 6.0f * (a.dx + 2.0f * (b.dx + c.dx) + d.dx);
            Vector3 dvdt = 1.0f / 6.0f * (a.dv + 2.0f * (b.dv + c.dv) + d.dv);

            State newState;
            newState.position = oldState.position + dxdt * timeDT;
            newState.velocity = oldState.velocity + dvdt * timeDT;
            return newState;
        }
    }
}
