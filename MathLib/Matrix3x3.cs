using System;

namespace NeroOS.MathLib
{
    public static partial class CalibMath
    {
        public static double m3Det(double[] A)
        {
           return A[0]*A[4]*A[8]+A[1]*A[5]*A[6]+A[2]*A[3]*A[7]-A[6]*A[4]*A[2]-A[7]*A[5]*A[0]-A[8]*A[3]*A[1];
        }

        public static double m3Trace(double[] A)
        {
	        return A[0]+A[4]+A[8];
        }

        static double TOL = 1.0e-8;

        public static double m3Inv(double[] A, ref double[] Ainv)
        {
           double det = m3Det ( A );
           
           if ( Math.Abs(det)<TOL ) return det;
             
           Ainv[0] =  (A[4]*A[8]-A[7]*A[5])/det;
           Ainv[1] = -(A[1]*A[8]-A[7]*A[2])/det;
           Ainv[2] =  (A[1]*A[5]-A[4]*A[2])/det;

           Ainv[3] = -(A[3]*A[8]-A[6]*A[5])/det;
           Ainv[4] =  (A[0]*A[8]-A[6]*A[2])/det;
           Ainv[5] = -(A[0]*A[5]-A[3]*A[2])/det;

           Ainv[6] =  (A[3]*A[7]-A[6]*A[4])/det;
           Ainv[7] = -(A[0]*A[7]-A[6]*A[1])/det;
           Ainv[8]=   (A[0]*A[4]-A[3]*A[1])/det;

           return det;
        }

        public static void m3MultAB(double[] A, double[] B, ref double[] AB)
        {
           AB[0]=A[0]*B[0]+A[1]*B[3]+A[2]*B[6];
           AB[1]=A[0]*B[1]+A[1]*B[4]+A[2]*B[7];
           AB[2]=A[0]*B[2]+A[1]*B[5]+A[2]*B[8];

           AB[3]=A[3]*B[0]+A[4]*B[3]+A[5]*B[6];
           AB[4]=A[3]*B[1]+A[4]*B[4]+A[5]*B[7];
           AB[5]=A[3]*B[2]+A[4]*B[5]+A[5]*B[8];

           AB[6]=A[6]*B[0]+A[7]*B[3]+A[8]*B[6];
           AB[7]=A[6]*B[1]+A[7]*B[4]+A[8]*B[7];
           AB[8]=A[6]*B[2]+A[7]*B[5]+A[8]*B[8];
        }

        public static void m3MultAb(double[] A, double[] b, ref double[] x)
        {
           x[0]=A[0]*b[0]+A[1]*b[1]+A[2]*b[2];
           x[1]=A[3]*b[0]+A[4]*b[1]+A[5]*b[2];
           x[2]=A[6]*b[0]+A[7]*b[1]+A[8]*b[2];
        }

        public static double m3SolvAxb(double[] A, double[] b, double[] x)
        {
           double[] Ainv={1,0,0, 0,1,0, 0,0,1};
           double det = m3Inv(A,ref Ainv);
           m3MultAb(Ainv,b,ref x);
           return det;
        }

        public static void m3Cross(double[] a, double[] b, ref double[] c)
        {
	        c[0]= a[1]*b[2] - a[2]*b[1];
	        c[1]= a[2]*b[0] - a[0]*b[2];
	        c[2]= a[0]*b[1] - a[1]*b[0];
        }

        public static void m3CopyAB(double[] A, ref double[] B)
        {
            for(int i=0;i<9;i++)
            B[i] = A[i];
        }
    }
}
