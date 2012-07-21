using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NeroOS.Drawing;
using NeroOS.Core;
using NeroOS.MathLib;
using NeroOS.Util;

namespace NeroOS.Cameras
{
    public enum RenderModes
    {
        Normal,
        Matrix,
        Edges,
        Trippy,
    };

    public class StereoRig
    {
        bool camerasInitialized;
        bool swapCameras = true;
        CameraDevice cameraA;
        CameraDevice cameraB;

        State physicsState = new State();

        //public RenderTarget2D renderTargetA;
        //public RenderTarget2D renderTargetB;

        public RenderLayer renderLayerA;
        public RenderLayer renderLayerB;

        public VirtualCamera virtualCameraA;
        public VirtualCamera virtualCameraB;

        Vector3 cameraARotation = Vector3.Zero;

        SerialPort microcontroller;
        double[] sensorData;
        double[] sensorPredictData;
        double[] sensorPredictVar;
        double[] sensorVar;
        double[] sensorMean;
        double[] sensorCalibScales;
        AccelerometerForm accelForm;

        RenderModes mode = RenderModes.Normal;

        /* Rotation between camera frames
         * r0   r3  r6
         * r1   r4  r7
         * r2   r5  r8
         */
        double[] RotationMatrix;

        /* Translation between camera frames
         * T0
         * T1
         * T2
         */
        double[] TranslationVector;
        
        /* Distortion parameters for each camera
         * D0 - Radial Distortion Coeff 1
         * D1 - Radial Distortion Coeff 2
         * D2 - Tangential Distortion Coeff 1
         * D3 - Tangential Distortion Coeff 2
         */
        double[] DistortParamsA;
        double[] DistortParamsB;

        /* Camera parameters for each camera
         * P0 - Principle Point X
         * P1 - Principle Point Y
         * F2 - Focal Length X
         * F3 - Focal Length Y
         */
        double[] CameraParamsA;
        double[] CameraParamsB;

        Shader undistortShader;
        Shader matrixShader;
        Shader glowShader;
        Shader edgeShader;
        Texture2D unicodeTable;
        int unicodeTableRows = 23;
        int unicodeTableColumns = 32;

        Texture2D cameraAImage;
        Texture2D cameraBImage;
        RenderTarget2D renderTarget;
        RenderTarget2D[] glowTarget;
        
        bool shadersInitialized;

        RenderTarget2D[] dsChain; //Down-Sample Chain for finger recognition
        Shader downsampleMaxLumShader;
        Shader biasLumShader;
        Color[] floodFillData;
        static int FINGERTHRESHOLD = 200;
        static float MINPIXELBOUNDS = 0.0025f;

        Shader redShader;
        Shader render3D;

        BoundingRect[] fingersA;
        BoundingRect[] fingersB;
        BoundingRect[] fingerPairs;
        Color[] fingerColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan, Color.CornflowerBlue };

        BoundingSphere[] detectionPoints;

        Matrix rotationMatrix = Matrix.Identity;

        public StereoRig()
        {
            RotationMatrix = new double[9];
            TranslationVector = new double[3];
            DistortParamsA = new double[4];
            DistortParamsB = new double[4];
            CameraParamsA = new double[4];
            CameraParamsB = new double[4];

            shadersInitialized = false;
            camerasInitialized = false;

            LoadMicroController();
            virtualCameraA = new VirtualCamera(Vector3.Zero, Vector3.Forward, MathHelper.ToRadians(60), 16.0f / 9.0f);
            virtualCameraB = new VirtualCamera(Vector3.Zero, Vector3.Forward, MathHelper.ToRadians(60), 16.0f / 9.0f);
        }
        ~StereoRig()
        {
            if(microcontroller.IsOpen)
                microcontroller.Close();
        }

        void LoadMicroController()
        {
            /*
            accelForm = new AccelerometerForm();
            accelForm.Show();
            return;
            */
            microcontroller = new SerialPort();
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    microcontroller.PortName = "COM" + i;
                    microcontroller.BaudRate = 115200;
                    microcontroller.Open();
                    microcontroller.DataReceived += new SerialDataReceivedEventHandler(OnMicroControllerDataReceived);
                    LoadAccelerometerData();
                    break;
                }
                catch { }
            }
            
        }

        void LoadAccelerometerData()
        {
            using (FileStream fs = new FileStream("Config/IMU.txt", FileMode.Open))
            {
                using (StreamReader rd = new StreamReader(fs))
                {
                    string[] meanData = rd.ReadLine().Split(' ');
                    string[] varData = rd.ReadLine().Split(' ');
                    string[] scaleData = rd.ReadLine().Split(' ');
                    sensorMean = new double[meanData.Length];
                    sensorVar = new double[varData.Length];
                    sensorCalibScales = new double[scaleData.Length];
                    for (int i = 0; i < meanData.Length; i++)
                    {
                        sensorMean[i] = double.Parse(meanData[i]);
                        sensorVar[i] = double.Parse(varData[i]);
                        sensorCalibScales[i] = double.Parse(scaleData[i]);
                        sensorVar[i] *= sensorCalibScales[i];
                    }
                    sensorData = new double[sensorMean.Length];
                    sensorPredictData = new double[sensorMean.Length];
                    sensorPredictVar = new double[sensorMean.Length];
                    
                    for (int i = 0; i < sensorMean.Length; i++)
                    {
                        sensorData[i] = 0.0;
                        sensorPredictData[i] = sensorMean[i];
                        sensorPredictVar[i] = sensorVar[i];
                    }
                    Console.WriteLine("Loaded accelerometer data!");
                }
            }
        }

        double KalmanFilter(double observed, ref double predicted, ref double predictedVar, double var, double gain)
        {
            double kalman = predictedVar / (predictedVar + var);
            double corrected = (1.0-gain)*observed + gain*predicted;
            corrected += kalman * (observed - predicted);
            double correctedVar = predictedVar * (1.0 - kalman);
            predicted = corrected;
            predictedVar = correctedVar;

            return corrected;
        }

        void OnMicroControllerDataReceived(object sender, EventArgs e)
        {
            string[] data = microcontroller.ReadLine().Split(',', '\n', '\r');
            if (data.Length == 8)
            {
                
                double elapsedTime = .03;
                if(data[0] != string.Empty)
                    elapsedTime = double.Parse(data[0])/1000.0;

                double[] rawValues = new double[sensorData.Length];
                for (int i = 0; i < sensorData.Length; i++)
                {
                    rawValues[i] = double.Parse(data[i + 1]);
                }

                rawValues[5] += rotationMatrix.Up.X;
                rawValues[3] += rotationMatrix.Up.Y;
                rawValues[4] += rotationMatrix.Up.Z;
                
                for (int i = 0; i < sensorData.Length; i++)
                {
                    double observedData = (rawValues[i] - sensorMean[i]) * sensorCalibScales[i];
                    sensorData[i] = KalmanFilter(observedData * elapsedTime, ref sensorPredictData[i], ref sensorPredictVar[i], sensorVar[i], 0.96);
                }
                
                /*
                 *  Sensor data in form of:
                 *  s0-aX   s1-aY   s2-aZ
                 *  s3-gz   s4-gx   s5-gy
                 */
                //AZ=CX AY=CZ AX=CY GZ=CRX GX=CRZ GY=-CRY 
                cameraARotation.X += (float)sensorData[2];
                cameraARotation.Y += (float)sensorData[0];
                cameraARotation.Z += (float)sensorData[1];

                Vector3 acceleration = Vector3.Zero;// rotationMatrix.Right * (float)sensorData[5] + rotationMatrix.Up * (float)sensorData[3] + rotationMatrix.Forward * (float)sensorData[4];
                //Vector3 acceleration = new Vector3((float)sensorData[5], (float)sensorData[3], (float)sensorData[4]);

                physicsState = PhysicsHelper.Integrate(physicsState, acceleration, (float)elapsedTime);
                Console.WriteLine("Accelerometer (XYZ): {0} {1} {2}", data[6], data[4], data[5]);
                //Console.WriteLine("Accelerometer (XYZ): {0} {1} {2}", rawValues[5], rawValues[3], rawValues[4]);
                Console.WriteLine("Up Vector: {0}", rotationMatrix.Up);
                Console.WriteLine("Rotation Vector: {0}", cameraARotation);
                //Console.WriteLine("Kalman (XYZ): {0} {1} {2}", sensorData[5], sensorData[3], sensorData[4]);
                //Console.WriteLine("Gyro (XYZ): {0} {1} {2}", sensorData[4], sensorData[5], sensorData[3]);
            }
        }

        void ReadMatrixData(StreamReader reader, ref double[] data)
        {
            string[] values = reader.ReadLine().Split(" ".ToCharArray());
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = double.Parse(values[i]);
            }
            
        }

        void ReadIntrinsics(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        char space = ' ';
                        string[] data = reader.ReadLine().Split(space);
                        switch (data[0])
                        {
                            case "M1:": //Left cam
                                double[] M1 = new double[9];
                                ReadMatrixData(reader, ref M1);
                                CameraParamsB = new double[] { M1[2], M1[5], M1[0], M1[4] };
                                break;
                            case "D1:": //Left cam
                                double[] D1 = new double[8];
                                ReadMatrixData(reader, ref D1);
                                DistortParamsB = new double[] { D1[0], D1[1], D1[2], D1[3] };
                                break;
                            case "M2:": //Right cam
                                double[] M2 = new double[9];
                                ReadMatrixData(reader, ref M2);
                                CameraParamsA = new double[] { M2[2], M2[5], M2[0], M2[4] };
                                break;
                            case "D2:": //Right cam
                                double[] D2 = new double[8];
                                ReadMatrixData(reader, ref D2);
                                DistortParamsA = new double[] { D2[0], D2[1], D2[2], D2[3] };
                                //DistortParamsA = new double[] { D2[0], D2[1], D2[6], D2[7] };
                                break;
                        }
                    }
                }
            }
        }

        void ReadExtrinsics(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        char space = ' ';
                        string[] data = reader.ReadLine().Split(space);
                        switch (data[0])
                        {
                            case "R:": //Rotation matrix
                                ReadMatrixData(reader, ref RotationMatrix);
                                break;
                            case "T:": //Translation matrix
                                ReadMatrixData(reader, ref TranslationVector);
                                TranslationVector[0] /= 1000.0;
                                TranslationVector[1] /= 1000.0;
                                TranslationVector[2] /= 1000.0;
                                break;
                        }
                    }
                }
            }
        }

        public void LoadCalibrationFile(string intrinsicFilename, string extrinsicFilename)
        {
            ReadIntrinsics(intrinsicFilename);
            ReadExtrinsics(extrinsicFilename);
            Log.GetInstance().WriteLine("Rotation Matrix: ");
            for(int i = 0; i < 3; i++)
            {
                Log.GetInstance().WriteLine("{0} {1} {2}", RotationMatrix[i * 3], RotationMatrix[i * 3 + 1], RotationMatrix[i * 3 + 2]);
            }
            Log.GetInstance().WriteLine("Translation Vector: ");
            Log.GetInstance().WriteLine("{0} {1} {2}", TranslationVector[0], TranslationVector[1], TranslationVector[2]);
            Log.GetInstance().WriteLine("Distortion Parameters (A and B): ");
            Log.GetInstance().WriteLine("{0} {1} {2} {3}", DistortParamsA[0], DistortParamsA[1], DistortParamsA[2], DistortParamsA[3]);
            Log.GetInstance().WriteLine("{0} {1} {2} {3}", DistortParamsB[0], DistortParamsB[1], DistortParamsB[2], DistortParamsB[3]);
            Log.GetInstance().WriteLine("Camera Parameters (A and B): ");
            Log.GetInstance().WriteLine("{0} {1} {2} {3}", CameraParamsA[0], CameraParamsA[1], CameraParamsA[2], CameraParamsA[3]);
            Log.GetInstance().WriteLine("{0} {1} {2} {3}", CameraParamsB[0], CameraParamsB[1], CameraParamsB[2], CameraParamsB[3]);
            
        }

        public void ReverseMatrix()
        {
            double[] prevRotMat = RotationMatrix;
            RotationMatrix[1] = prevRotMat[3];
            RotationMatrix[2] = prevRotMat[6];
            RotationMatrix[3] = prevRotMat[1];
            RotationMatrix[5] = prevRotMat[7];
            RotationMatrix[6] = prevRotMat[2];
            RotationMatrix[7] = prevRotMat[5];

            double[] tempTransVec = TranslationVector;
            TranslationVector[0] = tempTransVec[0] * -RotationMatrix[0] + tempTransVec[1] * -RotationMatrix[3] + tempTransVec[2] * -RotationMatrix[6];
            TranslationVector[1] = tempTransVec[0] * -RotationMatrix[1] + tempTransVec[1] * -RotationMatrix[4] + tempTransVec[2] * -RotationMatrix[7];
            TranslationVector[2] = tempTransVec[0] * -RotationMatrix[2] + tempTransVec[1] * -RotationMatrix[5] + tempTransVec[2] * -RotationMatrix[8];
            
            double[] tempParams = DistortParamsA;
            DistortParamsA = DistortParamsB;
            DistortParamsB = tempParams;
        }

        void SetupCameraParameters(CameraDevice camera)
        {
            int curr;
            int max;
            int min;
            int def;
            DirectShowLib.CameraControlFlags flag;
            
            camera.Camera.GetRange(DirectShowLib.CameraControlProperty.Exposure, out min, out max, out curr, out def, out flag);
            camera.Camera.Set(DirectShowLib.CameraControlProperty.Exposure, min+curr*2, DirectShowLib.CameraControlFlags.Manual);
            
            camera.Camera.GetRange(DirectShowLib.CameraControlProperty.Focus, out min, out max, out curr, out def, out flag);
            camera.Camera.Set(DirectShowLib.CameraControlProperty.Focus, min, DirectShowLib.CameraControlFlags.Manual);

            DirectShowLib.VideoProcAmpFlags vidFlag;
            camera.Video.GetRange(DirectShowLib.VideoProcAmpProperty.WhiteBalance, out min, out max, out curr, out def, out vidFlag);
            camera.Video.Set(DirectShowLib.VideoProcAmpProperty.WhiteBalance, min, DirectShowLib.VideoProcAmpFlags.Manual);
        }

        void InitializeCameras(Canvas canvas)
        {
            camerasInitialized = true;
            cameraA = new CameraDevice(canvas, 0, 640, 480, 24);
            cameraA.Play();
            SetupCameraParameters(cameraA);
            cameraB = new CameraDevice(canvas, 1, 640, 480, 24);
            cameraB.Play();
            SetupCameraParameters(cameraB);
            GraphicsDevice device = canvas.GetDevice();
            int width = device.PresentationParameters.BackBufferWidth/2;
            int height = device.PresentationParameters.BackBufferHeight;
            renderLayerA = new RenderLayer(canvas, width, height);
            renderLayerB = new RenderLayer(canvas, width, height);
        }

        void InitializeShaders(Canvas canvas)
        {
            shadersInitialized = true;
            undistortShader = new Shader();
            undistortShader.CompileFromFiles(canvas, "Shaders/UndistortP.hlsl", "Shaders/Basic2DV.hlsl");
            matrixShader = new Shader();
            matrixShader.CompileFromFiles(canvas, "Shaders/MatrixP.hlsl", "Shaders/Basic2DV.hlsl");

            edgeShader = new Shader();
            edgeShader.CompileFromFiles(canvas, "Shaders/EdgesP.hlsl", "Shaders/Basic2DV.hlsl");

            downsampleMaxLumShader = new Shader();
            downsampleMaxLumShader.CompileFromFiles(canvas, "Shaders/DownsampleMaxLumP.hlsl", "Shaders/Basic2DV.hlsl");

            biasLumShader = new Shader();
            biasLumShader.CompileFromFiles(canvas, "Shaders/BiasLum.hlsl", "Shaders/Basic2DV.hlsl");

            unicodeTable = Texture2D.FromFile(canvas.GetDevice(), "Data/Textures/japaneseTable.png");
            renderTarget = new RenderTarget2D(canvas.GetDevice(), cameraA.Width*2, cameraA.Height, 1, SurfaceFormat.Color);

            BuildDSChain(canvas);
            redShader = new Shader();
            redShader.CompileFromFiles(canvas, "Shaders/RedP.hlsl", "Shaders/Basic2DV.hlsl");

            render3D = new Shader();
            render3D.CompileFromFiles(canvas, "Shaders/RedP.hlsl", "Shaders/RedV.hlsl");

            glowTarget = new RenderTarget2D[2];
            glowTarget[0] = new RenderTarget2D(canvas.GetDevice(), renderTarget.Width / 4, renderTarget.Height / 4, 1, SurfaceFormat.Color);
            glowTarget[1] = new RenderTarget2D(canvas.GetDevice(), glowTarget[0].Width, glowTarget[0].Height, 1, glowTarget[0].Format);
        }

        void BuildDSChain(Canvas canvas)
        {
            dsChain = new RenderTarget2D[4];
            int dsChainRes = Math.Min(renderTarget.Width, renderTarget.Height);
            for (int i = 0; i < dsChain.Length; i++)
            {
                dsChain[i] = new RenderTarget2D(canvas.GetDevice(), dsChainRes, dsChainRes, 1, SurfaceFormat.Color);
                dsChainRes /= 2;
            }
        }

        void UndistortCameraImages(Canvas canvas)
        {
            GraphicsDevice device = canvas.GetDevice();

            undistortShader.SetupShader(canvas);
            device.Textures[0] = cameraA.GetCameraImage();
            device.SetPixelShaderConstant(0, new Vector4((float)DistortParamsA[0], 
                (float)DistortParamsA[1], (float)DistortParamsA[2], (float)DistortParamsA[3]));
            device.SetPixelShaderConstant(1, new Vector4((float)CameraParamsA[0],
                (float)CameraParamsA[1], (float)CameraParamsA[2], (float)CameraParamsA[3]) 
                / new Vector4(cameraA.Width, cameraA.Height, cameraA.Width, cameraA.Height));

            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, renderTarget);
            CanvasPrimitives.Quad.Render(canvas);
            device.SetRenderTarget(0, null);
            device.DepthStencilBuffer = dsOld;

            cameraAImage = renderTarget.GetTexture();
                        
        }

        void RenderMatrixStyle(Canvas canvas)
        {
            GraphicsDevice device = canvas.GetDevice();

            matrixShader.SetupShader(canvas);
            device.Textures[0] = cameraA.GetCameraImage();
            device.Textures[1] = unicodeTable;
            //device.Textures[2] = noiseTexture;
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_INVTEXRES, Vector2.Zero);
            device.SetPixelShaderConstant(0, new Vector4(1.0f / (float)unicodeTable.Width, 1.0f / (float)unicodeTable.Height,
                (float)unicodeTableRows / (float)unicodeTable.Width, (float)unicodeTableColumns / (float)unicodeTable.Height));
            device.SetPixelShaderConstant(1, new Vector4(0, 1.0f, 0, 1.0f));

            
            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, renderTarget);
            CanvasPrimitives.Quad.Render(canvas);
            device.SetRenderTarget(0, null);
            device.DepthStencilBuffer = dsOld;

            cameraAImage = renderTarget.GetTexture();

        }

        void RenderEdgesStyle(Canvas canvas)
        {
            GraphicsDevice device = canvas.GetDevice();
            edgeShader.SetupShader(canvas);
            device.Textures[0] = renderTarget.GetTexture();
            //device.Textures[2] = noiseTexture;
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_INVTEXRES, Vector2.Zero);
            device.SetPixelShaderConstant(0, Vector2.One / new Vector2(renderTarget.Width, renderTarget.Height));

            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, renderTarget);
            CanvasPrimitives.Quad.SetPositions(Vector2.One * -1, Vector2.One);
            CanvasPrimitives.Quad.Render(canvas);
            device.SetRenderTarget(0, null);
            device.DepthStencilBuffer = dsOld;
        }

        BoundingRect[] DetectFingersFromImage(Canvas canvas, Texture2D image)
        {
            
            GraphicsDevice device = canvas.GetDevice();
            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, dsChain[0]);
            canvas.DrawImage(image, Vector2.One * -1, Vector2.One, (int)ImageParameters.FlipY);
            device.SetRenderTarget(0, null);
            
            downsampleMaxLumShader.SetupShader(canvas);
            for(int i = 1; i < dsChain.Length; i++)
            {
                device.Textures[0] = dsChain[i-1].GetTexture();
                Vector2 invRes = Vector2.One / new Vector2(dsChain[i-1].Width, dsChain[i-1].Height);
                device.SetVertexShaderConstant(CanvasShaderConstants.VC_INVTEXRES, invRes);
                device.SetPixelShaderConstant(0, invRes);
                device.SetRenderTarget(0, dsChain[i]);
                CanvasPrimitives.Quad.Render(canvas);
                device.SetRenderTarget(0, null);
            }

            biasLumShader.SetupShader(canvas);
            int idx = dsChain.Length - 1;
            device.Textures[0] = dsChain[idx].GetTexture();
            Vector2 iRes = Vector2.One / new Vector2(dsChain[idx].Width, dsChain[idx].Height);
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_INVTEXRES, iRes);
            device.SetRenderTarget(0, dsChain[idx]);
            CanvasPrimitives.Quad.Render(canvas);
            device.SetRenderTarget(0, null);

            device.DepthStencilBuffer = dsOld;

            return FloodFillBounds(dsChain[idx].GetTexture());

        }

        struct FloodFillStruct
        {
            public SortedList<int, int> visitedPixels;
            public BoundingRect rect;

            public FloodFillStruct(int arg)
            {
                visitedPixels = new SortedList<int, int>();
                rect = new BoundingRect();
                rect.Min = Vector2.One * float.PositiveInfinity;
                rect.Max = Vector2.One * float.NegativeInfinity;
            }
        }

        void RecursiveFill(ref FloodFillStruct floodFill, int index, int width, int height)
        {
            if(index >= floodFillData.Length || floodFillData[index].R <= FINGERTHRESHOLD || floodFill.visitedPixels.ContainsKey(index) )
                return;

            floodFill.visitedPixels.Add(index, index);
            int xP = (index % width);
            int yP = (index / width);
            Vector2 posVec = new Vector2((float)xP / (float)width, (float)yP / (float)height) * 2.0f - Vector2.One;
            posVec.Y *= -1; //Invert yvec
            floodFill.rect.Min = Vector2.Min(floodFill.rect.Min, posVec);
            floodFill.rect.Max = Vector2.Max(floodFill.rect.Max, posVec);
            if (xP - 1 >= 0)
            {
                RecursiveFill(ref floodFill, index - 1, width, height);
            }
            if (xP + 1 < width)
            {
                RecursiveFill(ref floodFill, index + 1, width, height);
            }
            if (yP - 1 >= 0)
            {
                RecursiveFill(ref floodFill, index - width, width, height);
            }
            if (yP + 1 < height)
            {
                RecursiveFill(ref floodFill, index + width, width, height);
            }
        }

        BoundingRect[] FloodFillBounds(Texture2D image)
        {
            if (floodFillData == null)
            {
                floodFillData = new Color[image.Width * image.Height];                      
            }

            image.GetData<Color>(floodFillData);

            List<FloodFillStruct> floodFills = new List<FloodFillStruct>();

            for (int i = 0; i < floodFillData.Length; i++)
            {
                if (floodFillData[i].R > FINGERTHRESHOLD)
                {
                    bool process = true;
                    for (int j = 0; j < floodFills.Count; j++)
                    {
                        if (floodFills[j].visitedPixels.ContainsKey(i))
                        {
                            process = false;
                            break;
                        }
                    }

                    if (process)
                    {
                        FloodFillStruct fill = new FloodFillStruct(0);
                        RecursiveFill(ref fill, i, image.Width, image.Height);
                        floodFills.Add(fill);
                    }

                }
            }

            List<BoundingRect> rects = new List<BoundingRect>();
            int detectArea = (int)((float)(image.Width * image.Height) * MINPIXELBOUNDS);

            for (int i = 0; i < floodFills.Count; i++)
            {
                if (floodFills[i].visitedPixels.Count >= detectArea)
                    rects.Add(floodFills[i].rect);
            }

            return rects.ToArray();
        }

        Vector4[] ComputeBoundsDistances(BoundingRect[] bounds)
        {
            Vector4[] dists = new Vector4[bounds.Length];
            for (int i = 0; i < dists.Length; i++)
            {
                dists[i] = new Vector4(bounds[i].Min.X + 1.0f, bounds[i].Min.Y + 1.0f,
                    1.0f - bounds[i].Max.X, 1.0f - bounds[i].Max.Y);
                if (i - 1 >= 0)
                {
                    dists[i].X = dists[i - 1].Z;
                    dists[i].Y = dists[i - 1].W;
                }
                if (i + 1 < bounds.Length)
                {
                    dists[i].Z = Math.Abs(bounds[i + 1].Min.X - bounds[i].Max.X);
                    dists[i].W = Math.Abs(bounds[i + 1].Min.Y - bounds[i].Max.Y);
                }
            }
            return dists;
        }

        BoundingRect[] DetectFingerPairs()
        {
            Vector4[] distsA = ComputeBoundsDistances(fingersA);
            Vector4[] distsB = ComputeBoundsDistances(fingersB);
            List<BoundingRect> fingers = new List<BoundingRect>();
            //List<BoundingRect> unmatchedBFingers = new List<BoundingRect>();
            //unmatchedBFingers.AddRange(fingersB);
            for (int i = 0; i < fingersA.Length; i++)
            {

                if (distsB.Length == 0)
                    break;
                
                int bestMatchIndex = 0;
                float residError = Vector4.DistanceSquared(distsA[i], distsB[bestMatchIndex])
                    + Vector2.DistanceSquared(fingersA[i].Min, fingersB[bestMatchIndex].Min)
                    + Vector2.DistanceSquared(fingersA[i].Max, fingersB[bestMatchIndex].Max);
                for (int j = 0; j < fingersB.Length; j++)
                {
                    float testError = Vector4.DistanceSquared(distsA[i], distsB[j])
                        + Vector2.DistanceSquared(fingersA[i].Min, fingersB[j].Min)
                        + Vector2.DistanceSquared(fingersA[i].Max, fingersB[j].Max);
                    if (testError < residError)
                    {
                        residError = testError;
                        bestMatchIndex = j;
                    }
                }
                fingers.Add(fingersA[i]);
                fingers.Add(fingersB[bestMatchIndex]);
            }

            return fingers.ToArray();
            
        }

        double RayRayIntersection(ref double[] o1, ref double[] o2, ref double[] d1, ref double[] d2)
        {
            double[] cp = new double[3];
            cp[0] = d1[1] * d2[2] - d1[2] * d2[1];
            cp[1] = d1[2] * d2[0] - d1[0] * d2[2];
            cp[2] = d1[0] * d2[1] - d1[1] * d2[0];
            double magSqr = cp[0] * cp[0] + cp[1] * cp[1] + cp[2] * cp[2];
            if (magSqr <= 0.000001)
                return 0;

            double[] diff = new double[3];
            diff[0] = o2[0] - o1[0];
            diff[1] = o2[1] - o1[1];
            diff[2] = o2[2] - o1[2];
            
            
            return (diff[0] * d2[1] * cp[2] + d2[0] * cp[1] * diff[2] + cp[0] * diff[1] * d2[2]
                - diff[2] * d2[1] * cp[0] - d2[2] * cp[1] * diff[0] - cp[2] * diff[1] * d2[0]) / magSqr;
        }

        void ComputeDetectionPoints()
        {
            detectionPoints = new BoundingSphere[fingerPairs.Length / 2];
            double[] rO1 = {0, 0, 0};
            double[] rD1 = {0, 0, 1};
            double[] rD2 = {0, 0, 1};
            double[] rD2Temp = rD2;
            for (int i = 0; i < detectionPoints.Length; i++)
            {
                int idx = i*2;

                rD1[0] = 0.5 * (fingerPairs[idx].Max.X + fingerPairs[idx].Min.X);
                rD1[1] = 0.5 * (fingerPairs[idx].Max.Y + fingerPairs[idx].Min.Y);
                rD1[2] = -1;
                double iMag1 = 1.0/Math.Sqrt(rD1[0]*rD1[0]+rD1[1]*rD1[1]+rD1[2]*rD1[2]);
                rD1[0] *= iMag1;
                rD1[1] *= iMag1;
                rD1[2] *= iMag1;

                rD2Temp[0] = 0.5 * (fingerPairs[idx + 1].Max.X + fingerPairs[idx + 1].Min.X);
                rD2Temp[1] = 0.5 * (fingerPairs[idx + 1].Max.Y + fingerPairs[idx + 1].Min.Y);
                rD2Temp[2] = -1;
                double iMag2 = 1.0/Math.Sqrt(rD2Temp[0]*rD2Temp[0]+rD2Temp[1]*rD2Temp[1]+rD2Temp[2]*rD2Temp[2]);
                rD2Temp[0] *= iMag2;
                rD2Temp[1] *= iMag2;
                rD2Temp[2] *= iMag2;
                
                rD2[0] = rD2Temp[0] * RotationMatrix[0] + rD2Temp[1] * RotationMatrix[3] + rD2Temp[2] * RotationMatrix[6];
                rD2[1] = rD2Temp[0] * RotationMatrix[1] + rD2Temp[1] * RotationMatrix[4] + rD2Temp[2] * RotationMatrix[7];
                rD2[2] = rD2Temp[0] * RotationMatrix[2] + rD2Temp[1] * RotationMatrix[5] + rD2Temp[2] * RotationMatrix[8];

                detectionPoints[i].Center = new Vector3((float)rD1[0], (float)rD1[1], (float)rD1[2])
                    * (float)RayRayIntersection(ref rO1, ref TranslationVector, ref rD1, ref rD2);
                detectionPoints[i].Radius = 0.080f;
            }
        }

        void DetectFingers(Canvas canvas)
        {
            BoundingRect[] drawRects = GetCameraDrawBoxes();
            fingersA = DetectFingersFromImage(canvas, cameraA.GetCameraImage());
            fingersB = DetectFingersFromImage(canvas, cameraB.GetCameraImage());
            fingerPairs = DetectFingerPairs();
            ComputeDetectionPoints();
        }

        public Vector3 RotateVector(Vector3 srcVec)
        {
            Vector3 vec = new Vector3();
            vec.X = (float)(RotationMatrix[0] * srcVec.X + RotationMatrix[3] * srcVec.Y + RotationMatrix[6] * srcVec.Z);
            vec.Y = (float)(RotationMatrix[1] * srcVec.X + RotationMatrix[4] * srcVec.Y + RotationMatrix[7] * srcVec.Z);
            vec.Z = (float)(RotationMatrix[2] * srcVec.X + RotationMatrix[5] * srcVec.Y + RotationMatrix[8] * srcVec.Z);

            return vec;
        }

        public void Update()
        {
            if (cameraARotation.X >= MathHelper.TwoPi)
                cameraARotation.X -= MathHelper.TwoPi;
            if (cameraARotation.X < 0.0f)
                cameraARotation.X += MathHelper.TwoPi;
            if (cameraARotation.Y >= MathHelper.TwoPi)
                cameraARotation.Y -= MathHelper.TwoPi;
            if (cameraARotation.Y < 0.0f)
                cameraARotation.Y += MathHelper.TwoPi;
            if (cameraARotation.Z >= MathHelper.TwoPi)
                cameraARotation.Z -= MathHelper.TwoPi;
            if (cameraARotation.Z < 0.0f)
                cameraARotation.Z += MathHelper.TwoPi;

            rotationMatrix = Matrix.CreateFromYawPitchRoll(cameraARotation.Y, cameraARotation.X, cameraARotation.Z);
            virtualCameraA.Target = Vector3.TransformNormal(Vector3.Forward, rotationMatrix);
            virtualCameraA.Position = physicsState.position;

            virtualCameraB.Position = new Vector3((float)TranslationVector[0],
                (float)TranslationVector[1], (float)TranslationVector[2]) + virtualCameraA.Position;
            virtualCameraB.Target = RotateVector(virtualCameraA.Target);

            virtualCameraA.Update();
            virtualCameraB.Update();
        }

        public BoundingSphere[] GetDetectionPoints()
        {
            return detectionPoints;
        }

        //Returns boxes of a and b respectively
        BoundingRect[] GetCameraDrawBoxes()
        {
            BoundingRect[] rects = new BoundingRect[2];
            rects[0].Min = new Vector2(0, -1.0f);
            rects[0].Max = Vector2.One;
            rects[1].Min = Vector2.One * -1;
            rects[1].Max = new Vector2(0, 1.0f);
            if (swapCameras)
            {
                Vector2 temp = rects[1].Min;
                rects[1].Min = rects[0].Min;
                rects[0].Min = temp;
                temp = rects[1].Max;
                rects[1].Max = rects[0].Max;
                rects[0].Max = temp;
            }
            return rects;
        }

        public void CompositeFinalImage(Canvas canvas)
        {
            BoundingRect[] drawRects = GetCameraDrawBoxes();

            GraphicsDevice device = canvas.GetDevice();
            DepthStencilBuffer dsOld = device.DepthStencilBuffer;
            device.DepthStencilBuffer = canvas.GetDepthStencil();
            device.SetRenderTarget(0, renderTarget);

            canvas.DrawImage(cameraAImage, drawRects[0].Min, drawRects[0].Max, (int)(ImageParameters.FlipY));
            canvas.DrawImage(cameraBImage, drawRects[1].Min, drawRects[1].Max, (int)(ImageParameters.FlipY));

            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.SourceBlend = Blend.SourceAlpha;
            device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            canvas.DrawImage(renderLayerA.GetImage(), drawRects[0].Min, drawRects[0].Max);
            canvas.DrawImage(renderLayerB.GetImage(), drawRects[1].Min, drawRects[1].Max);
            
            device.SetRenderTarget(0, null);
            device.DepthStencilBuffer = dsOld;

            //RenderMatrixStyle(canvas);
            //RenderEdgesStyle(canvas);
            //CanvasPrimitives.Quad.SetPositions(Vector2.One * -2, Vector2.One);
            
            device.RenderState.DepthBufferEnable = false;
            device.RenderState.DepthBufferWriteEnable = false;
            canvas.DrawImage(renderTarget.GetTexture());
            /*
            CanvasPrimitives.Quad.SetPositions(Vector2.One * -1, Vector2.One);
            device.RenderState.DepthBufferEnable = true;
            device.RenderState.DepthBufferWriteEnable = true;
            render3D.SetupShader(canvas);
            Matrix scaleMat = Matrix.CreateScale(0.05f);
            device.SetVertexShaderConstant(CanvasShaderConstants.VC_MODELVIEW, virtualCameraA.ViewProjection);
            for (int i = 0; i < detectionPoints.Length; i++)
            {
                device.SetPixelShaderConstant(0, fingerColors[i % fingerColors.Length].ToVector4());
                scaleMat.Translation = detectionPoints[i].Center;
                device.SetVertexShaderConstant(CanvasShaderConstants.VC_WORLD, scaleMat);
                CanvasPrimitives.Quad.Render(canvas);
            }
            */

        }
        
        public void RenderCameraRig(Canvas canvas)
        {
            if (!camerasInitialized)
            {
                InitializeCameras(canvas);
            }

            if (!shadersInitialized)
            {
                InitializeShaders(canvas);
            }
            
            cameraAImage = cameraA.GetCameraImage();
            cameraBImage = cameraB.GetCameraImage();

            DetectFingers(canvas);

            //canvas.DrawImage(dsChain[dsChain.Length-1].GetTexture());

            /*
            
            redShader.SetupShader(canvas);
            for (int i = 0; i < fingerPairs.Length / 2; i++)
            {
                device.SetPixelShaderConstant(0, fingerColors[i % fingerColors.Length].ToVector4());
                CanvasPrimitives.Quad.SetPositions(fingerPairs[i*2].Min, fingerPairs[i*2].Max);
                CanvasPrimitives.Quad.Render(canvas);
                CanvasPrimitives.Quad.SetPositions(fingerPairs[i * 2 + 1].Min, fingerPairs[i * 2 + 1].Max);
                CanvasPrimitives.Quad.Render(canvas);
            }
            
            */

            
        }

    }
}
