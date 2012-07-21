using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeroOS.MathLib
{
    public class KalmanFilter
    {
        double percentvar = 0.05;
        double gain = 0.8;
        public double[] ComputeKalman(double[] variables, int iterations)
        {   
            int dimension = variables.Length;
            double[] filteredData = new double[dimension];
		    double[] noisevar = new double[dimension];
		    double[] average = new double[dimension];
		    double[] predicted = new double[dimension];
		    double[] predictedvar = new double[dimension];
		    double[] observed = new double[dimension];
		    double[] kalman = new double[dimension];
		    double[] corrected = new double[dimension];
		    double[] correctedvar = new double[dimension];

            for (int i = 0; i < dimension; ++i) noisevar[i] = percentvar;
            predicted = variables;
            predictedvar = noisevar;
            observed = variables;
            for(int i = 0; i < iterations; i++)
            {
                
                for (int k = 0; k < kalman.Length; ++k)
                {
                    /*calculate the KalmanGain*/
                    kalman[k] = predictedvar[k] / (predictedvar[k] + noisevar[k]);
                }
                for (int k = 0; k < corrected.Length; ++k)
                {
                    /*allow for the gain*/
                    corrected[k] = gain * predicted[k] + (1.0 - gain) * observed[k]
                    + kalman[k] * (observed[k] - predicted[k]);
                }
                for (int k = 0; k < correctedvar.Length; ++k)
                {
                    correctedvar[k] = predictedvar[k] * (1.0 - kalman[k]);
                }
                predictedvar = correctedvar;
                predicted = corrected;
            }
            return corrected;
        }

        public KalmanFilter()
		{
			pX = &Xa;
			pXprev = &Xb;
			pP = &Pa;
			pPprev = &Pb;

			Xa.zero();
			Xb.zero();

			Z.zero();

			F.zero();
			F[0][1] = 1.0f;
			F[1][2] = -1.0f;

			Phi.identity();// Depends on time
			PhiTranspose.identity();// Depends on time

			G.zero();
			G[1][0] = 1.0f;
			G[2][1] = 1.0f;
			GTranspose.zero();
			GTranspose[0][1] = 1.0f;
			GTranspose[1][2] = 1.0f;

			H.zero();
			H[0][0] = 1.0f;
			HTranspose.zero();
			HTranspose[0][0] = 1.0f;

			K.zero();// gain
            // The above will not change for our filter. For a general filter they will.

            // These are the intial covariance estimates, feel free to muck with them.
			Pa.zero();
			Pa[0][0] =  0.05f * 0.05f;
			Pa[1][1] =  0.5f * 0.5f;
			Pa[2][2] =  10.0f * 10.0f;
			Pb = Pa;

            // The following will change to appropriate values for noise.
			timePropNoiseVector[0] = 0.01f ;// m / s^2
			timePropNoiseVector[1] = 0.10f;// m / s^2 / s
			measurmentNoiseVector[0] = 0.005f;// =1/2 cm;
		}

		double getCurrentStateBestEstimate(int stateIndex )
		{
			if( stateIndex < 0 || stateIndex > pX->numElements )
			{
				return pX->getElement(0);
			}

			return pX->getElement(stateIndex);
		}

		void setCurrentStateBestEstimate(int stateIndex, double value )
		{
			pX->setElement(stateIndex, value);
		}

		double getCurrentStateUncertainty(int stateIndex )
		{
			if( stateIndex < 0 || stateIndex * stateIndex >= pP->getNumElements() )
			{
				return pP->getElement(0,0);
			}

			return Math.Sqrt( pP->getElement(stateIndex,stateIndex) );
		}


		double getTimePropUncertainty(int index )
		{
			return ( index > timePropNoiseVector.numDimensions ) ? 0.0 : timePropNoiseVector[index];
		}

		double getMeasurementPropUncertainty(int index )
		{
			return ( index > measurmentNoiseVector.numDimensions ) ? 0.0 : measurmentNoiseVector[index];
		}

		public void updateTime(float t, float measuredAccel )// t is the time to propagate
		{
            // Variable time propigation
	        // Basic equations to simulate
	        // xnew = Phi * xold
	        // Pnew = Phi * Pold * PhiT + Int [Phi*G*Q*GT*PhiT] * dt 
			timeUpdateSetPrevious();
            // Setup the Phi and PhiTranspose
			//Phi.identity();// Depends on time, overkill for this example. Only need to rewrite the same elements
			Phi[0][1] = t;
			Phi[0][2] = -t * t * 0.5f;
			Phi[1][2] = -t;

			//PhiTranspose.identity();// Depends on time, overkill for this example. Only need to rewrite the same elements
			PhiTranspose[1][0] = t;
			PhiTranspose[2][0] = -t * t * 0.5f;
			PhiTranspose[2][1] = -t;

			TVectorN<3,float>& X = *pX;
			TVectorN<3,float>& Xprev = *pXprev;

	        // Update the state vector.
			Phi.transform(Xprev, ref X);// xnew = Phi * xold

			X[0] += measuredAccel * t * t * 0.5f;// These will not be here for an eerror state Kalman Filter
			X[1] += measuredAccel * t;// These will not be here for an eerror state Kalman Filter

			TMatrixMxN<3,3,float>& P = *pP;// Points to Current Covariance matrix
			TMatrixMxN<3,3,float>& Pprev = *pPprev;// Points to Previous Covariance matrix

	        // Now update teh covariance matrix. Yay lots of temps.
			TMatrixMxN<3,3,float> temp33;
			temp33.mul(Phi,Pprev);
			P.mul(temp33,PhiTranspose);

            // Since Q and G are independent of time can make into one easy vector the is integrated by hand
			const float Q11 = timePropNoiseVector[0] * timePropNoiseVector[0];
			const float Q22 = timePropNoiseVector[1] * timePropNoiseVector[1];
            //const float Q12 = 0.0f * timePropNoiseVector[0] * timePropNoiseVector[1]; 0.0f means tehy are uncorrelated
			temp33[0][0] = ( Q11 / 3.0f + Q22 * t * t / 20.0f )* t * t * t;
			temp33[0][1] = ( Q11 * 0.5f + Q22 * t * t / 8.0f )* t * t;
			temp33[0][2] = -Q22* t * t * t / 6.0f;
			temp33[1][0] = temp33[0][1];
			temp33[1][1] = ( Q11 + Q22 * t * t  / 3.0f )* t;
			temp33[1][2] = -Q22 * t * t * 0.5f;
			temp33[2][0] = temp33[0][2];
			temp33[2][1] = temp33[1][2];
			temp33[2][2] = Q22 * t;

			P += temp33;
		}

		void updateMeasurement(float[] measurement )
		{
			measurementUpdateSetPrevious();
		    // K = P * HT * [H * P * HT + noiseMeasurement]^-1 (Involves matrix inverse in general, for us not)
		    // x+ = x + K * [ measurement - H * x ]
		    // P+ = ( 1 - K * H ) * P = P - K*H*P
			TVectorN<3, float>& X = *pX;
			TVectorN<3, float>& Xprev = *pXprev;

			TMatrixMxN<3,3,float>& P = *pP;// Points to Current Covariance matrix
			TMatrixMxN<3,3,float>& Pprev = *pPprev;// Points to Previous Covariance matrix

			TMatrixMxN<3,3,float> temp33;
			TMatrixMxN<3,1,float> temp31;
			TMatrixMxN<1,3,float> temp13;
			TMatrixMxN<1,1,float> temp11;

			temp13.mul(H,Pprev);
			temp11.mul(temp13,HTranspose);

			float temp = temp11[0][0] + measurmentNoiseVector[0];

			temp31.mul(Pprev,HTranspose);
			temp31 /= temp;// This is K.
			temp31.getColumn(0,K);

			temp13.mul(H,Pprev);
            //temp31.setColumn(0,K);
			temp33.mul(temp31,temp13);
			P = Pprev - temp33;

			temp31.setColumn(0,Xprev);
			temp11.mul(H,temp31);
			X = K * (measurement - temp11[0][0] );
			X += Xprev;
		}

		void timeUpdateSetPrevious()
		{
			if( pX == &Xa )
			{
				pX = &Xb;
				pXprev = &Xa;
			}
			else
			{
				pX = &Xa;
				pXprev = &Xb;
			}

			if( pP == &Pa )
			{
				pP = &Pb;
				pPprev = &Pa;
			}
			else
			{
				pP = &Pa;
				pPprev = &Pb;
			}
		}

		void measurementUpdateSetPrevious()
		{
			if( pX == &Xa )
			{
				pX = &Xb;
				pXprev = &Xa;
			}
			else
			{
				pX = &Xa;
				pXprev = &Xb;
			}

			if( pP == &Pa )
			{
				pP = &Pb;
				pPprev = &Pa;
			}
			else
			{
				pP = &Pa;
				pPprev = &Pb;
			}
		}
    }
}
