using System;
using System.Collections.Generic;
using NeroOS.MathLib;

namespace NeroOS.Cameras
{
    public class Accelerometer
    {
        double mUpdateTimeMS;
        MATRIX FMatrix; //Contains acceleration estimation
        MATRIX FTMatrix; //Contains transpose of FMatrix
        MATRIX QMatrix; //Composed of GMatrix (based on wikipedia Kalman example)
        MATRIX HMatrix;
        MATRIX HTMatrix;
        MATRIX RMatrix;

        MATRIX[] XNew;
        MATRIX[] PNew;
        MATRIX[] ZEst;

        void UpdatePrediction()
        {
            for(int i = 0; i < XNew.Length; i++)
            {
                MATRIX XOld = XNew[i];
                LinAlg.mtxAB(ref FMatrix.Data, ref XOld.Data, FMatrix.Rows, FMatrix.Cols, XOld.Cols, ref XNew[i].Data);

                MATRIX POld = PNew[i];
                MATRIX PTemp = POld;
                LinAlg.mtxAB(ref FMatrix.Data, ref POld.Data, FMatrix.Rows, FMatrix.Cols, POld.Cols, ref PTemp.Data); //Copy to PTemp
                LinAlg.mtxAB(ref PTemp.Data, ref FTMatrix.Data, PTemp.Rows, PTemp.Cols, FTMatrix.Cols, ref POld.Data);//Copy to POld
                LinAlg.mtxAddMat(ref POld.Data, ref QMatrix.Data, POld.Rows, POld.Cols, ref PNew[i].Data); //Finally, copy to PNew[i]
            }
        }

        void UpdateModel()
        {
            for (int i = 0; i < XNew.Length; i++)
            {
                MATRIX Y = ZEst[i];
                MATRIX HmulX = new MATRIX(HMatrix.Rows, XNew[i].Cols);
                LinAlg.mtxAB(ref HMatrix.Data, ref XNew[i].Data, HMatrix.Rows, HMatrix.Cols, XNew[i].Cols, ref HmulX.Data);
                LinAlg.mtxSubMat(ref ZEst[i].Data, ref HmulX.Data, HmulX.Rows, HmulX.Cols, ref Y.Data); //Measurement residual

                MATRIX HmulP = new MATRIX(HMatrix.Rows, PNew[i].Cols);
                LinAlg.mtxAB(ref HMatrix.Data, ref PNew[i].Data, HMatrix.Rows, HMatrix.Cols, PNew[i].Cols, ref HmulP.Data);
                HmulX = HmulP;
                LinAlg.mtxAB(ref HmulX.Data, ref HTMatrix.Data, HmulX.Rows, HmulX.Cols, HTMatrix.Cols, ref HmulP.Data);

                //S Computation
                MATRIX S = new MATRIX(HmulP.Rows, HmulP.Cols);
                LinAlg.mtxAddMat(ref HmulP.Data, ref RMatrix.Data, HmulP.Rows, HmulP.Cols, ref S.Data); //Covariance residual
                HmulP = S;
                LinAlg.mtxAXI_nxn(ref HmulP.Data, HmulP.Rows, ref S.Data); //Performs matrix inversion (assumes square matrix)


                HmulP = new MATRIX(PNew[i].Rows, HTMatrix.Cols);
                LinAlg.mtxAB(ref PNew[i].Data, ref HTMatrix.Data, PNew[i].Rows, PNew[i].Cols, HTMatrix.Cols, ref HmulP.Data);

                MATRIX K = new MATRIX(HmulP.Rows, S.Cols); //Our kalman
                LinAlg.mtxAB(ref HmulP.Data, ref S.Data, HmulP.Rows, HmulP.Cols, S.Cols, ref K.Data); //Optimal Kalman gain

                HmulP = new MATRIX(K.Rows, Y.Cols);
                LinAlg.mtxAB(ref K.Data, ref Y.Data, K.Rows, K.Cols, Y.Cols, ref HmulP.Data);
                MATRIX tempX = XNew[i];
                LinAlg.mtxAddMat(ref tempX.Data, ref HmulP.Data, tempX.Rows, tempX.Cols, ref XNew[i].Data);//Computes new X param

                HmulP = new MATRIX(K.Rows, HMatrix.Cols);
                LinAlg.mtxAB(ref K.Data, ref HMatrix.Data, K.Rows, K.Cols, HMatrix.Cols, ref HmulP.Data);
                MATRIX identity = new MATRIX(K.Rows, HMatrix.Cols);
                for (int j = 0; j < identity.Rows; j++) identity.SetAt(j, j, 1.0);
                HmulX = new MATRIX(identity.Rows, identity.Cols);
                LinAlg.mtxSubMat(ref identity.Data, ref HmulP.Data, identity.Rows, identity.Cols, ref HmulX.Data);
                MATRIX tempP = PNew[i];
                LinAlg.mtxAB(ref HmulX.Data, ref tempP.Data, HmulX.Rows, HmulX.Cols, tempP.Cols, ref PNew[i].Data); //Compute PNew[i]
            }
        }
    }
}
