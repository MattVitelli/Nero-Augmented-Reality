using System;
using DirectShowLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using NeroOS.Drawing;

namespace NeroOS.Cameras
{
    public class CameraDevice : ISampleGrabberCB, IDisposable
    {
        private IAMCameraControl m_CameraControl = null;
        private IAMVideoProcAmp m_VideoControl = null;

        /// <summary> Dimensions of the image, calculated once in constructor for perf. </summary>
        private int m_videoWidth;
        private int m_videoHeight;
        private int m_stride;

        private int deviceNumber;

        IGraphBuilder graphBuilder;
        Texture2D camTextureA;
        Texture2D camTextureB;
        bool swapA = true;
        
        /// <summary> buffer for bitmap data.  Always release by caller</summary>
        byte[] bgrData;

        public Texture2D GetCameraImage()
        {
            return (swapA) ? camTextureB : camTextureA;
        }

        public IAMCameraControl Camera
        {
            get { return m_CameraControl; }
        }

        public IAMVideoProcAmp Video
        {
            get { return m_VideoControl; }
        }

        // Zero based device index and device params and output window
        public CameraDevice(Canvas iCanvas, int iDeviceNum, int iWidth, int iHeight, short iBPP)
        {
            DsDevice[] capDevices;
            // Get the collection of video devices
            capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            if (iDeviceNum + 1 > capDevices.Length)
            {
                throw new Exception("No video capture devices found at that index!");
            }

            try
            {
                deviceNumber = iDeviceNum;
                InitDevice(capDevices[iDeviceNum], iWidth, iHeight);
                camTextureA = new Texture2D(iCanvas.GetDevice(), Width, Height, 1, TextureUsage.None, SurfaceFormat.Color);
                camTextureB = new Texture2D(iCanvas.GetDevice(), Width, Height, 1, TextureUsage.None, SurfaceFormat.Color);
                bgrData = new byte[Stride * Height];
            }
            catch
            {
                Dispose();
                throw;
            }
            
        }

        public void Dispose()
        {
            CloseInterfaces();
        }

        ~CameraDevice()
        {
            Dispose();
        }

        public int Width
        {
            get
            {
                return m_videoWidth;
            }
        }

        public int Height
        {
            get
            {
                return m_videoHeight;
            }
        }

        public int Stride
        {
            get
            {
                return m_stride;
            }
        }

        private void SaveSizeInfo(ISampleGrabber sampGrabber)
        {
            int hr;

            // Get the media type from the SampleGrabber
            AMMediaType media = new AMMediaType();

            hr = sampGrabber.GetConnectedMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
            {
                throw new NotSupportedException("Unknown Grabber Media Format");
            }

            // Grab the size info
            VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
            m_videoWidth = videoInfoHeader.BmiHeader.Width;
            m_videoHeight = videoInfoHeader.BmiHeader.Height;
            m_stride = m_videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);

            DsUtils.FreeAMMediaType(media);
            media = null;
        }

        private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
        {
            int hr;
            AMMediaType media = new AMMediaType();

            // Set the media type to Video/RBG24
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;

            hr = sampGrabber.SetMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
            media = null;

            // Configure the samplegrabber
            hr = sampGrabber.SetCallback(this, 1);
            DsError.ThrowExceptionForHR(hr);
        }

        public void Play()
        {
            
            IMediaControl mediaCtrl = (IMediaControl)graphBuilder;
            FilterState state;
            mediaCtrl.GetState(1000, out state);
            if(state == FilterState.Paused || state == FilterState.Stopped)
                mediaCtrl.Run();   
        }

        public void Pause()
        {

            IMediaControl mediaCtrl = (IMediaControl)graphBuilder;
            FilterState state;
            mediaCtrl.GetState(1000, out state);
            if (state == FilterState.Running)
                mediaCtrl.Pause();
        }

        public void Stop()
        {

            IMediaControl mediaCtrl = (IMediaControl)graphBuilder;
            if (mediaCtrl != null)
            {
                FilterState state;
                mediaCtrl.GetState(1000, out state);
                if (state == FilterState.Paused || state == FilterState.Running)
                    mediaCtrl.Stop();
            }
        }

        public void InitDevice(DsDevice device, int iWidth, int iHeight)
        {
            int hr;
            object camDevice;
            Guid iid = typeof(IBaseFilter).GUID;
            device.Mon.BindToObject(null, null, ref iid, out camDevice);
            IBaseFilter camFilter = camDevice as IBaseFilter;
            m_CameraControl = camFilter as IAMCameraControl;
            m_VideoControl = camFilter as IAMVideoProcAmp;
            ISampleGrabber sampGrabber = null;

            graphBuilder = (IGraphBuilder)new FilterGraph();

            //Create the Capture Graph Builder
            ICaptureGraphBuilder2 captureGraphBuilder = null;
            captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

            // Attach the filter graph to the capture graph
            hr = captureGraphBuilder.SetFiltergraph(this.graphBuilder);
            DsError.ThrowExceptionForHR(hr);

            //Add the Video input device to the graph
            hr = graphBuilder.AddFilter(camFilter, "WebCam" + deviceNumber);
            DsError.ThrowExceptionForHR(hr);

            // Configure the sample grabber
            sampGrabber = new SampleGrabber() as ISampleGrabber;
            ConfigureSampleGrabber(sampGrabber);
            IBaseFilter sampGrabberBF = sampGrabber as IBaseFilter;

            //Add the Video compressor filter to the graph
            hr = graphBuilder.AddFilter(sampGrabberBF, "SampleGrabber" + deviceNumber);
            DsError.ThrowExceptionForHR(hr);

            IBaseFilter nullRender = new NullRenderer() as IBaseFilter;
            graphBuilder.AddFilter(nullRender, "NullRenderer" + deviceNumber);
            InitResolution(captureGraphBuilder, camFilter, iWidth, iHeight);

            hr = captureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, camDevice, sampGrabberBF, nullRender);
            DsError.ThrowExceptionForHR(hr);


            SaveSizeInfo(sampGrabber);

            Marshal.ReleaseComObject(sampGrabber);
            Marshal.ReleaseComObject(captureGraphBuilder);
        }

        void InitResolution(ICaptureGraphBuilder2 capGraph, IBaseFilter capFilter, int targetWidth, int targetHeight)
        {
            object o;
            capGraph.FindInterface(PinCategory.Capture, MediaType.Video, capFilter, typeof(IAMStreamConfig).GUID, out o);

            AMMediaType media = null;
            IAMStreamConfig videoStreamConfig = o as IAMStreamConfig;
            IntPtr ptr;
            int iC = 0, iS = 0;

            videoStreamConfig.GetNumberOfCapabilities(out iC, out iS);
            ptr = Marshal.AllocCoTaskMem(iS);
            int bestDWidth = 999999;
            int bestDHeight = 999999;
            int streamID = 0;
            for (int i = 0; i < iC; i++)
            {
                videoStreamConfig.GetStreamCaps(i, out media, ptr);
                VideoInfoHeader v;
                v = new VideoInfoHeader();
                Marshal.PtrToStructure(media.formatPtr, v);
                int dW = Math.Abs(targetWidth - v.BmiHeader.Width);
                int dH = Math.Abs(targetHeight - v.BmiHeader.Height);
                if (dW < bestDWidth && dH < bestDHeight)
                {
                    streamID = i;
                    bestDWidth = dW;
                    bestDHeight = dH;
                }
            }

            videoStreamConfig.GetStreamCaps(streamID, out media, ptr);
            int hr = videoStreamConfig.SetFormat(media);
            Marshal.FreeCoTaskMem(ptr);

            DsError.ThrowExceptionForHR(hr);
            DsUtils.FreeAMMediaType(media);
            media = null;
        }

        private void CloseInterfaces()
        {
            try
            {
                if (graphBuilder != null)
                {
                    IMediaControl mediaCtrl = graphBuilder as IMediaControl;
                    mediaCtrl.Stop();
                }
            }
            catch {}
            if (graphBuilder != null)
            {
                Marshal.ReleaseComObject(graphBuilder);
                graphBuilder = null;
            }
            if (m_VideoControl != null)
            {
                Marshal.ReleaseComObject(m_VideoControl);
                m_VideoControl = null;
            }
            if (m_CameraControl != null)
            {
                Marshal.ReleaseComObject(m_CameraControl);
                m_CameraControl = null;
            }
            bgrData = null;

        }

        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            Marshal.ReleaseComObject(pSample);
            return 0;
        }

        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            if (deviceNumber == 1)
            {
                int j = 0;
            }
            Marshal.Copy(pBuffer, bgrData, 0, BufferLen);
            Color[] colorData = new Color[BufferLen / 3];
            int colIdx = colorData.Length - 1;
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = new Microsoft.Xna.Framework.Graphics.Color(bgrData[3 * i + 2], bgrData[3 * i + 1], bgrData[3 * i]);
            }
            if (swapA)
            {
                camTextureA.SetData<Microsoft.Xna.Framework.Graphics.Color>(colorData);
            }
            else
            {
                camTextureB.SetData<Microsoft.Xna.Framework.Graphics.Color>(colorData);
            }
            swapA = !swapA;

            return 0;
        }
    }
}
