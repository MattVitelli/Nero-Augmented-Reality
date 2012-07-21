using System;

namespace NeroOS.MathLib
{
    public struct MATRIX
    {
        public int Rows;
        public int Cols;
        public double[] Data;
        public MATRIX(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Data = new double[Rows * Cols];
        }

        public void SetAt(int i, int j, double value)
        {
            Data[i + j * Rows] = value;
        }

    }
    public static partial class LinAlg
    {
        static double EPS = 1e-10;

        public static double SQR(double x)
        {
            return (x * x);
        }

        public static void swap(ref double a, ref double b) 
        {
           double tmp=a;
           a=b;
           b=tmp;
        }

        static double SIGN(double a,double b)
        { 
            return ((b) >= 0.0) ? Math.Abs(a) : -Math.Abs(a);
        }


        /* lib functions */
        public static int mtxGaussAxb(double[] a, int n, double[] b) 
        {
         int    i, j, k;
         int    imax;             /* pivot line */
         double amax, rmax; 

         for (j=0; j<n-1; j++) {  /*  Loop in the columns of [a] */
          rmax = 0.0f;
          imax = j;
          for (i=j; i<n; i++) {   /* Loop to find the best ration a[i-1)*n-1+j]/a[i-1)*n-1+k] */
           amax = 0.0f;
           for (k=j; k<n; k++)    /* Loop to find largest element of line i */
           if (Math.Abs(a[i*n+k]) > amax)
            amax = Math.Abs(a[i*n+k]);
           if (amax < EPS)        /* Check if all elements are null */
            return 0;             /* no solution */
           else if ((Math.Abs(a[i*n+j]) > rmax*amax) && (Math.Abs(a[i*n+j]) >= EPS)) { /* teste current line */
            rmax = Math.Abs(a[i*n+j]) / amax;
            imax = i;             /* find pivot line */
           }
          }
          if (Math.Abs(a[imax*n+j])<EPS) {         /* Check if pivot is null */
           for (k=imax+1; k<n; k++)          /* Search for a line with a no-null pivot */
            if (Math.Abs(a[k*n+j]) < EPS)
             imax = k;                       /* exchange line j with k */
           if (Math.Abs(a[imax*n+j]) < EPS)
            return 0;                        /* no unique soluition */
          }
          if (imax != j) {                   /* Exchange j by line imax */
           for (k=j; k<n; k++)
            swap(ref a[imax*n+k], ref a[j*n+k]);
           swap(ref b[imax], ref b[j]);
          }
          for (i=j+1; i<n; i++) {            /* Clear elements under the diagonal */
           double aux = a[i*n+j] / a[j*n+j];
           for (k=j+1; k<n; k++)             /* Transforms the rest of the elements of the line */
            a[i*n+k] -= aux * a[j*n+k];
           b[i] -= aux * b[j];
          }
         }
         if (Math.Abs(a[(n-1)*n+n-1]) <= EPS)        /* Check the unicity of the solution */
          return 0;                          /* no solution */
         else {
          b[n-1] /= a[(n-1)*n+n-1];          /* back substitution */
          for (i=n-2; i>=0; i--) {           
           for (j=i+1; j<n; j++)
            b[i] -= a[i*n+j] * b[j];
           b[i] /= a[i*n+i];
          }
         }
         return 1;     /* solution ok */                      
        }

        public static int mtxSVD(ref double[] a, int m, int n, ref double[] u, ref double[] d, ref double[] v, ref double[] tmp)
        {
           int flag,i,its,j,jj,k,l=0,nm=0;
           double anorm,c,f,g,h,s,scale,x,y,z;

           for(i=0;i<m;i++)
              for(j=0;j<n;j++)
                 u[i*n+j]=a[i*n+j];

           g=scale=anorm=0.0;
           for (i=0;i<n;i++) {
              l=i+2;
              tmp[i]=scale*g;
              g=s=scale=0.0;
              if (i < m) {
                 for (k=i;k<m;k++) scale +=  Math.Abs(u[k*n+i]);
                 if (scale != 0.0) {
                    for (k=i;k<m;k++) {
                       u[k*n+i] /= scale;
                       s += u[k*n+i]*u[k*n+i];
                    }
                    f=u[i*n+i];
                    g = -SIGN(Math.Sqrt(s),f);
                    h=f*g-s;
                    u[i*n+i]=f-g;
                    for (j=l-1;j<n;j++) {
                       for (s=0.0,k=i;k<m;k++) s += u[k*n+i]*u[k*n+j];
                       f=s/h;
                       for (k=i;k<m;k++) u[k*n+j] += f*u[k*n+i];
                    }
                    for (k=i;k<m;k++) u[k*n+i] *= scale;
                 }
              }
              d[i]=scale *g;
              g=s=scale=0.0;
              if (i+1 <= m && i+1 != n) {
                 for (k=l-1;k<n;k++) scale += Math.Abs(u[i*n+k]);
                 if (scale!=0.0) {
                    for (k=l-1;k<n;k++) {
                       u[i*n+k] /= scale;
                       s += u[i*n+k]*u[i*n+k];
                    }
                    f=u[i*n+l-1];
                    g = -SIGN(Math.Sqrt(s),f);
                    h=f*g-s;
                    u[i*n+l-1]=f-g;
                    for (k=l-1;k<n;k++) tmp[k]=u[i*n+k]/h;
                    for (j=l-1;j<m;j++) {
                       for (s=0.0,k=l-1;k<n;k++) s += u[j*n+k]*u[i*n+k];
                       for (k=l-1;k<n;k++) u[j*n+k] += s*tmp[k];
                    }
                   for (k=l-1;k<n;k++) u[i*n+k] *= scale;
                 }
              }
              anorm=(anorm>(Math.Abs(d[i])+Math.Abs(tmp[i]))?anorm:(Math.Abs(d[i])+Math.Abs(tmp[i])));
           }
           for (i=n-1;i>=0;i--) {
              if (i < n-1) {
                 if (g!=0.0) {
                   for (j=l;j<n;j++)
                       v[j*n+i]=(u[i*n+j]/u[i*n+l])/g;
                    for (j=l;j<n;j++) {
                       for (s=0.0,k=l;k<n;k++) s += u[i*n+k]*v[k*n+j];
                       for (k=l;k<n;k++) v[k*n+j] += s*v[k*n+i];
                    }
                 }
                 for (j=l;j<n;j++) v[i*n+j]=v[j*n+i]=0.0;
              }
              v[i*n+i]=1.0;
              g=tmp[i];
              l=i;
           }
           for (i=(m<n?m:n)-1;i>=0;i--) {
              l=i+1;
              g=d[i];
              for (j=l;j<n;j++) u[i*n+j]=0.0;
              if (g != 0.0) {
                 g=1.0/g;
                 for (j=l;j<n;j++) {
                    for (s=0.0,k=l;k<m;k++) s += u[k*n+i]*u[k*n+j];
                    f=(s/u[i*n+i])*g;
                    for (k=i;k<m;k++) u[k*n+j] += f*u[k*n+i];
                 }
                 for (j=i;j<m;j++) u[j*n+i] *= g;
              } else for (j=i;j<m;j++) u[j*n+i]=0.0;
              ++u[i*n+i];
           }
           for (k=n-1;k>=0;k--) {
              for (its=0;its<30;its++) {
                 flag=1;
                 for (l=k;l>=0;l--) {
                    nm=l-1;
                    if ((Math.Abs(tmp[l])+anorm) == anorm) {
                       flag=0;
                       break;
                    }
                    if ((Math.Abs(d[nm])+anorm) == anorm) break;
                 }
                 if (flag>0) 
                 {
                    c=0.0;
                    s=1.0;
                    for (i=l;i<k+1;i++) {
                       f=s*tmp[i];
                       tmp[i]=c*tmp[i];
                       if ((Math.Abs(f)+anorm) == anorm) break;
                       g=d[i];
                       h=Math.Sqrt(f*f+g*g);
                       d[i]=h;
                       h=1.0/h;
                       c=g*h;
                       s = -f*h;
                       for (j=0;j<m;j++) {
                          y=u[j*n+nm];
                          z=u[j*n+i];
                          u[j*n+nm]=y*c+z*s;
                          u[j*n+i]=z*c-y*s;
                       }
                    }
                 }
                 z=d[k];
                 if (l == k) {
                    if (z < 0.0) {
                       d[k] = -z;
                       for (j=0;j<n;j++) v[j*n+k] = -v[j*n+k];
                    }
                    break;
                 }
                 if (its == 49) return 0;
                 x=d[l];
                 nm=k-1;
                 y=d[nm];
                 g=tmp[nm];
                 h=tmp[k];
                 f=((y-z)*(y+z)+(g-h)*(g+h))/(2.0f*h*y);
                 g=Math.Sqrt(f*f+1.0);
                 f=((x-z)*(x+z)+h*((y/(f+SIGN(g,f)))-h))/x;
                 c=s=1.0;
                 for (j=l;j<=nm;j++) {
                    i=j+1;
                    g=tmp[i];
                    y=d[i];
                    h=s*g;
                    g=c*g;
                    z=Math.Sqrt(f*f+h*h);
                    tmp[j]=z;
                    c=f/z;
                    s=h/z;
                    f=x*c+g*s;
                    g = g*c-x*s;
                    h=y*s;
                    y *= c;
                    for (jj=0;jj<n;jj++) {
                       x=v[jj*n+j];
                       z=v[jj*n+i];
                       v[jj*n+j]=x*c+z*s;
                       v[jj*n+i]=z*c-x*s;
                    }
                    z=Math.Sqrt(f*f+h*h);
                    d[j]=z;
                    if (z>0) {
                       z=1.0f/z;
                       c=f*z;
                       s=h*z;
                    }
                    f=c*g+s*y;
                    x=c*y-s*g; 
                    for (jj=0;jj<m;jj++) {
                       y=u[jj*n+j];
                       z=u[jj*n+i];
                       u[jj*n+j]=y*c+z*s;
                       u[jj*n+i]=z*c-y*s;
                    }
                 }
                 tmp[l]=0.0;
                 tmp[k]=f;
                 d[k]=x;
              }
           }
           return 1;
        }

        /* 
         * Add the tensor product of the vector {v} (i.e., {v}{v}T) 
         * to the matrix [A].  
         * [A]+={v}{v}T
         *
         */
        public static void mtxAddMatVecTensor(ref double[] a, ref double[] v, int n)
        {
	        int i,j;
           for (i=0;i<n;i++) {
              for (j=0;j<n;j++) { 
			        a[i*n+j]+=v[i]*v[j];
              }
           }
        }

        /* {x}=[A]{b}    Dimensions: [A]=mxn, {b}=n and {x}=m. --modificado*/
        public static void mtxAb(ref double[] a, ref double[] b, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++) {
              x[i]=0.0;
              for (j=0;j<n;j++)
                 x[i]+=a[i*n+j]*b[j];
           }
        }

        /* {x}=[A]T{b}    Dimensions: [A]=mxn, {b}=m and {x}=n. */
        public static void mtxAtb(ref double[] a, ref double[] b, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<n;i++) {
              x[i]=0.0;
              for (j=0;j<m;j++)
                 x[i]+=a[j*n+i]*b[j];
           }
        }


        /* [X]=[A-1)*n-1+B]    Dimensions: [A]=mxp, [B]=pxn and [X]=mxn. */
        public static void mtxAB(ref double[] a, ref double[] b, int m, int p, int n, ref double[] x)
        {
           int i,j,k;
           for (i=0;i<m;i++) {
              for (j=0;j<n;j++) {
                 x[i*n+j]=0.0;
                 for (k=0;k<p;k++)
                        x[i*n+j]+=a[i*p+k]*b[k*n+j];
              }
           }
        }

        /* [X]=[A-1)*n-1+B]T    Dimensions: [A]=mxp, [B]=nxp and [X]=mxn. */
        public static void mtxABt(ref double[] a, ref double[] b, int m, int p, int n, ref double[] x)
        {
           int i,j,k;
           for (i=0;i<m;i++) {
              for (j=0;j<n;j++) {
                 x[i*n+j]=0.0;
                 for (k=0;k<p;k++)
                        x[i*n+j]+=a[i*p+k]*b[j*p+k];
              }
           }
        }

        /*  [X]=[A]T[B]    Dimensions: [A]=mxp, [B]=mxn and [X]=pxn. */
        public static void mtxAtB(ref double[] a, ref double[] b, int m, int p, int n, ref double[] x)
        {
           int i,j,k;
           for (i=0;i<p;i++) {
              for (j=0;j<n;j++) {
                 x[i*n+j]=0.0;
                 for (k=0;k<m;k++)
                        x[i*n+j]+=a[k*p+i]*b[k*n+j];
              }
           }
        }

        /*  [X]=[A]+[B]    Dimensions: [A]=mxn, [B]=mxn and [X]=mxn. */
        public static void mtxAddMat(ref double[] a, ref double[] b, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++)
              for (j=0;j<n;j++)
                 x[i*n+j]=a[i*n+j]+b[i*n+j];
        }

        /*  [X]=[A]-[B]    Dimensions: [A]=mxn, [B]=mxn and [X]=mxn. */
        public static void mtxSubMat(ref double[] a, ref double[] b, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++)
              for (j=0;j<n;j++)
                 x[i*n+j]=a[i*n+j]-b[i*n+j];
        }


        /*  [X]=s[A]    Dimensions: [A]=mxn and [X]=mxn. */
        public static void mtxScaMat(ref double[] a, double s, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++)
              for (j=0;j<n;j++)
                 x[i*n+j]=s*a[i*n+j];
        }


        /*  [X]=[A]T    Dimensions: [A]=mxn and [X]=nxm. */
        public static void mtxAt(ref double[] a, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++)
              for (j=0;j<n;j++)
                 x[j*m+i]=a[i*n+j];
        }

        /*  [X]=[A]    Dimensions: [A]=mxn and [X]=mxn. */
        public static void mtxMatCopy(ref double[] a, int m, int n, ref double[] x)
        {
           int i,j;
           for (i=0;i<m;i++) 
              for (j=0;j<n;j++) 
                 x[i*n+j]=a[i*n+j];
        }

        /*  {x}={v}+{u}  Dimensions: {v}=m, {u}=m and {x}=m. */
        public static void mtxAddVec(ref double[] u, ref double[] v, int m, ref double[] x)
        {
           int i;
           for (i=0;i<m;i++) x[i]=u[i]+v[i];
        }

        /*  {x}={v}-{u}  Dimensions: {v}=m, {u}=m and {x}=m. */
        public static void mtxSubVec(ref double[] u, ref double[] v, int m, ref double[] x)
        {
           int i;
           for (i=0;i<m;i++) x[i]=u[i]-v[i];
        }

        /*  {x}=s{u}  Dimensions: {u}=m and {x}=m. */
        public static void mtxScaVec(ref double[] u, int m, double s, ref double[] x)
        {
           int i;
           for (i=0;i<m;i++) x[i]=s*u[i];
        }

        /*  s={v}.{u}  Dimensions: {v}=m and  {u}=m. */
        public static double mtxDotProd(ref double[] u, ref double[] v, int m)
        {
           double tmp=0;
           int i;
           for (i=0;i<m;i++) 
              tmp+=u[i]*v[i];
           return tmp;
        }

        public static double mtxNormalizeVector(int n, ref double[] v)
        {
           double norm=mtxDotProd(ref v,ref v,n);
           int i;
           if (norm>EPS) {
              norm = Math.Sqrt(norm);
              for (i=0;i<n;i++) v[i]/=norm;
           }
           return norm;
        }

        public static void mtxCol(ref double[] A, int col, int m, int n, ref double[] x)
        {
	        int i;
	        for (i=0;i<m;i++){
		        x[i]=A[i*n+col];
	        }
        }

        /* {x}={v} Dimensions: {v}={x}=m. */
        public static void mtxVecCopy(ref double[] v, int m, ref double[] x)
        {
         int i;
         for(i=0;i<m;i++) x[i]=v[i];
        }

        public static int mtxDecompLU(ref double[] a, int n, ref int[] rp, ref double d, ref double[] scale)
        {
            int i, imax, j, k;
            double max, tmp, sum;

            //rp--;
            //scale--;
            
            d=1.0;                   /* No row interchanges (yet). */
            for (i=0;i<n;i++) 
            {     /* Loop over rows to get the implicit scaling */
                max=0.0;          
                for (j=0;j<n;j++)
                   max = (max>Math.Abs(a[i*n+j]))?max:Math.Abs(a[i*n+j]);
                if (max == 0.0) 
                    return 0;
                /* Largest nonzero largest element. */
                scale[i+1]=1.0/max;    /* Save the scaling. */
            }
            for (j=1;j<=n;j++) {  /* This is the loop over columns of Crout's method */
                for (i=1;i<j;i++) {  /* Sum form of a triangular matrix except for i=j */
                    sum=a[(i-1)*n-1+j];     
                    for (k=1;k<i;k++) sum -= a[(i-1)*n-1+k]*a[(k-1)*n-1+j];
                    a[(i-1)*n-1+j]=sum;
                }
                max=0.0;  /* Initialize for the search for largest pivot element. */
                imax = 0;        /* Set default value for imax */
                for (i=j;i<=n;i++) {  /* This is i=j of previous sum and i=j+1...N */
                    sum=a[(i-1)*n-1+j];      /* of the rest of the sum */
                    for (k=1;k<j;k++)
                        sum -= a[(i-1)*n-1+k]*a[(k-1)*n-1+j];
                    a[(i-1)*n-1+j]=sum;
                    if ( (tmp=scale[i]*Math.Abs(sum)) >= max) {
                /* Is the figure of merit for the pivot better than the best so far? */
                        max=tmp;
                        imax=i;
                    }
                }
                if (j != imax) 
                {          /* Do we need to interchange rows? */
                    for (k=1;k<=n;k++) {  /* Yes, do so... */
                        tmp=a[(imax-1)*n-1+k];
                        a[(imax-1)*n-1+k]=a[(j-1)*n-1+k];
                        a[(j-1)*n-1+k]=tmp;
                    }
                    d = -(d);           /* ...and change the parity of d */
                    scale[imax]=scale[j];       /* Also interchange the scale factor */
                }
                rp[j]=imax;
                if (a[(j-1)*n-1+j] == 0.0) a[(j-1)*n-1+j]=EPS;
                /* If the pivot element is zero the matrix is singular (at least */
                /* to the precision of the algorithm). For some applications on */
                /* singular matrices, it is desiderable to substitute EPS for zero */
                if (j != n) {           /* Now, finally divide by pivot element */
                    tmp=1.0/(a[(j-1)*n-1+j]);
                    for ( i=j+1 ; i<=n ; i++ ) a[(i-1)*n-1+j] *= tmp;
                }
            }   /* Go back for the next column in the reduction. */

            return 1;
        }

        public static void mtxBackSubLU(ref double[] a, int n, ref int[] rp, ref double[] b)
        {
            int i, ii=0, ip, j;
            double sum;
            //b--;
            //rp--;

            for (i=1;i<=n;i++) 
            {  
                ip=rp[i];     
                sum=b[ip];  
                b[ip]=b[i]; 
                if (ii>0)  
                    for ( j=ii ; j<=i-1 ; j++ ) sum -= a[(i-1)*n-1+j]*b[j];
                else if (sum>0)      
                    ii = i; 
                b[i]=sum;
            }
            for (i=n-1;i>=0;i--) {   /* Backsubstitution */
                sum=b[i+1];
                for (j=i+1;j<n;j++) sum -= a[i*n+j]*b[j+1];
                b[i+1]=sum/a[i*n+i];  
                if (Math.Abs(b[i])<EPS) b[i] = 0.0;   
            }
        }

        public static double mtxDetLU(ref double[] a, double d, int n)
        {
           double det=d;
           int i;
           for (i=0;i<n;i++)
              det*=a[i*n+i];
           return det;
        }

        /*
         * Finds a non trivial solution for the system Ax=0
         * A mxn, m>n and rank(A)< n (better if rank(A)= n-1).
         *
         * The return value indicates the error. He is the ratio from the smallest  
         * to the largest singular value of A. If rank(A) < n
         * this value should be zero (~=EPS for the implementation).
         */
        public static double mtxSVDAx0(ref double[] a, int m, int n, ref double[] x, ref double[] u, ref double[] d, ref double[] v, ref double[] tmp)
        {
	        double wmax,wmin,wmin2;
	        int i,j,jmin=0;
        		
           /* perform decomposition */
	        mtxSVD(ref a,m,n,ref u,ref d,ref v,ref tmp);

	        /* test if A has a non-trivial solution */
	        wmax=d[0]; wmin = d[0];
	        for (j=1;j<n;j++){
	            if (d[j] < wmin) { wmin=d[j]; jmin =j; }
		         if (d[j] > wmax) wmax=d[j];
	        }

           /* test for the second smallest singular value */
           wmin2=wmax;
	        for (j=0;j<n;j++){
              if (j==jmin) continue;
	           if (d[j] < wmin2) wmin2=d[j];
	        }

	        /* copy the column of V correspondent to the smallest singular value of A */
	        for (i=0;i<n;i++)
                x[i] = v[i*n+jmin];

	        return (wmin/wmax);
        }



        static double TINY = 1.0e-20;

        public static void mtxSVDbacksub(ref double[] u, ref double[] d, ref double[] v, int m, int n, ref double[] b, ref double[] x, ref double[] tmp)
        {
	        int j,i;
	        double s;

	        for (j=0;j<n;j++) {
		        s=0.0;
		        if (d[j]<TINY) {  
			        for (i=0;i<m;i++) s += u[i*n+j]*b[i]; /* computes [U]T{b} */
			        s /= d[j];   /* multiply by [d]-1 */
		        }
		        tmp[j]=s;
	        }
	        for (i=0;i<n;i++) {
		        s=0.0;
		        for (j=0;j<n;j++) s += v[i*n+j]*tmp[j];  /* computes [V]{tmp} */
		        x[i]=s;
	        }
        }

        /*
         * Finds a solution for the system Ax=b
         * A mxn, m>n.
         * 
         */
        public static void mtxSVDAxb(ref double[] a, int m, int n, ref double[] x, ref double[] b, ref double[] u, ref double[] d, ref double[] v, ref double[] tmp)
        {
	        double wmax,wmin;
	        int j;
        		
           /* perform decomposition */
	        mtxSVD(ref a,m,n,ref u,ref d,ref v,ref tmp);

	        /* find the larger single value */
	        wmax=d[0];
	        for (j=1;j<n;j++)
              if (d[j] > wmax) wmax = d[j];

           /* cutoff small values */
           wmin=(1.0e-6)*wmax;
           for (j=0;j<n;j++)
              if (d[j]<wmin) d[j]=0;

	        /* backsubstitution */
           mtxSVDbacksub(ref u,ref d,ref v,m,n,ref b,ref x,ref tmp);
        }


        public static void mtxLUdcmp(ref double[,] a, int n, ref int[] indx, ref double d)
        {
	        int i,imax=0,j,k;
	        double big,dum,sum,temp;
	        double[] vv;
            vv = new double[n + 1];
        	
	        d=1.0;
	        for (i=1;i<=n;i++) {
		        big=0.0;
		        for (j=1;j<=n;j++)
			        if ((temp=(double)Math.Abs(a[i,j])) > big) big=temp;
		        //if (big == 0.0) printf("\nSingular matrix in routine mtxLUDCMP");
		        vv[i]=(double)1.0/big;
	        }
	        for (j=1;j<=n;j++) {
		        for (i=1;i<j;i++) {
			        sum=a[i,j];
			        for (k=1;k<i;k++) sum -= a[i,k]*a[k,j];
			        a[i,j]=sum;
		        }
		        big=0.0;
		        for (i=j;i<=n;i++) {
			        sum=a[i,j];
			        for (k=1;k<j;k++)
				        sum -= a[i,k]*a[k,j];
			        a[i,j]=sum;
			        if ( (dum=(double)(vv[i]*Math.Abs(sum))) >= big) {
				        big=dum;
				        imax=i;
			        }
		        }
		        if (j != imax) {
			        for (k=1;k<=n;k++) {
				        dum=a[imax,k];
				        a[imax,k]=a[j,k];
				        a[j,k]=dum;
			        }
			        d = -(d);
			        vv[imax]=vv[j];
		        }
		        indx[j]=imax;
		        if (a[j,j] == 0.0f) a[j,j]=TINY;
		        if (j != n) {
			        dum=(double)1.0/(a[j,j]);
			        for (i=j+1;i<=n;i++) a[i,j] *= dum;
		        }
	        }
        }

        public static void mtxLUbksb(ref double[,] a, int n, ref int[] indx, ref double[] b)
        {
	        int i,ii=0,ip,j;
	        double sum;

	        for (i=1;i<=n;i++) {
		        ip=indx[i];
		        sum=b[ip];
		        b[ip]=b[i];
		        if (ii>0)
			        for (j=ii;j<=i-1;j++) sum -= a[i,j]*b[j];
		        else if (sum>0) ii=i;
		        b[i]=sum;
	        }
	        for (i=n;i>=1;i--) {
		        sum=b[i];
		        for (j=i+1;j<=n;j++) sum -= a[i,j]*b[j];
		        b[i]=sum/a[i,i];
	        }
        }

        /*
        * Computes the nxn matrix [X] such that [A][X]=[I]
        * , then [X] is the inverse of [A].  
        *(uses LU decomposition).
        */
        public static void mtxAXI_nxn(ref double[] a, int n, ref double[] x)
        {
	        int i,j;
            double[,] lu = new double[n + 1, n + 1];
            double[,] y = new double[n + 1, n + 1];
            double[] col = new double[n + 1];
            int[] indx = new int[n+1];
            double d = 0;
	        /* Copy the original matrix inside lu */
	        for (i=1;i<=n;i++){
		        for (j=1;j<=n;j++) {
			        lu[i,j]=a[(i-1)*n + (j-1)];
		        }
	        }

	        /* LU decomposition */
	        mtxLUdcmp(ref lu,n,ref indx,ref d);

	        for(j=1;j<=n;j++) { //Find inverse by columns.
		        for(i=1;i<=n;i++) 
			        col[i]=0.0;
		        col[j]=1.0;
		        mtxLUbksb(ref lu,n,ref indx,ref col);
		        for(i=1;i<=n;i++) 
			        y[i,j]=col[i];
	        }

	        for (i=1;i<=n;i++)
		        for (j=1;j<=n;j++) 
			        x[(i-1)*n + (j-1)]= y[i,j];        	
	        return;
        }

        /*
        * Computes the mxn matrix [X] such that [A][X]=[I]
        * , then [X] is the inverse of [A].  
        */
        public static void mtxAxI_mxn(ref double[] A, int m, int n, ref double[] X)
        {

	        double[] At = new double[n*m];
            double[] AtA = new double[n * n];
            double[] invAtA = new double[n * n];
            double[] invAtA_At = new double[n * m];
        	
	        mtxAt(ref A, m, n, ref At);
	        mtxAB(ref At,ref A,n,m,n,ref AtA);
	        mtxAXI_nxn(ref AtA,n,ref invAtA);
	        mtxAB(ref invAtA,ref At,n,n,m,ref X);
        		
	        return;
        }
        /*
        * Get a solution x for the system Ax=b
        *										[A]x =				b
        *								[At][A]x =	[At]b
        *	 inv([At][A])	[At][A]x =	inv([At][A])[At]b	
        *										[I]x =  inv([At][A])[At]b	
        */
        public static void mtxAx_b(ref double[] A, int m, int n, ref double[] b, ref double[] x)
        {
            double[] At = new double[n*m];
            double[] AtA = new double[m * m];
            double[] invAtA = new double[n * n];
            double[] invAtA_At = new double[n * m];
        	  
	        mtxAt(ref A, m, n, ref At);
	        mtxAB(ref At,ref A,n,m,n,ref AtA);
	        mtxAXI_nxn(ref AtA,n,ref invAtA);
	        mtxAB(ref invAtA,ref At,n,n,m,ref invAtA_At);
	        mtxAb(ref invAtA_At,ref b,n,m,ref x);

	        return;
        }

    }
}
