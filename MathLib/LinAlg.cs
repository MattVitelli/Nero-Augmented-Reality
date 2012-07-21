using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeroOS.MathLib
{
    public static partial class LinAlg
    {
        public static double Dot(ref double[] one, ref double[] two)
        {
            double result = 0;
            for (int i = 0; i < one.Length; i++)
            {
                result += one[i] * two[i];
            }
            return result;
        }

        public static double[] Multiply(ref double[] one, ref double[] two)
        {
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] * two[i];
            }
            return result;
        }

        public static double[] Multiply(ref double[] one, double scalar)
        {
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] * scalar;
            }
            return result;
        }

        public static double[] Divide(ref double[] one, ref double[] divisor)
        {
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] / divisor[i];
            }
            return result;
        }

        public static double[] Divide(ref double[] one, double scalar)
        {
            double invScalar = 1.0 / scalar;
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] * invScalar;
            }
            return result;
        }

        public static double[] Add(ref double[] one, ref double[] two)
        {
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] + two[i];
            }
            return result;
        }

        public static double[] Subtract(ref double[] one, ref double[] two)
        {
            double[] result = new double[one.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = one[i] + two[i];
            }
            return result;
        }
    }
}
