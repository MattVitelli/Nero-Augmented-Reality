using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace NeroOS.Util
{
    public partial class AccelerometerForm : Form
    {
        SerialPort microcontroller;
        bool calibrating = false;
        Timer calibTimer = new Timer();

        double numSamples;
        List<double[]> sensorData = new List<double[]>();

        public AccelerometerForm()
        {
            InitializeComponent();
            calibTimer = new Timer();
            calibTimer.Interval = 2000; // 2 seconds to calibrate data
            calibTimer.Tick +=new EventHandler(OnFinishCalibration);

            microcontroller = new SerialPort();
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    microcontroller.PortName = "COM" + i;
                    microcontroller.Open();
                    microcontroller.DataReceived += new SerialDataReceivedEventHandler(OnMicroControllerDataReceived);
                    break;
                }
                catch { }
            }
        }
        ~AccelerometerForm()
        {
            microcontroller.Close();
        }

        void OnFinishCalibration(object sender, EventArgs e)
        {
            calibrating = false;
            calibTimer.Stop();
            statusLabel.Text = "Status: Calibration Complete";
            double invSamps = 1.0/numSamples;
            double[] avgSensorData = {0, 0, 0, 0, 0, 0};
            double[] varSensorData = { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < sensorData.Count; i++)
            {
                for (int j = 0; j < avgSensorData.Length; j++)
                {
                    avgSensorData[j] += sensorData[i][j];
                }
            }
            for (int i = 0; i < avgSensorData.Length; i++)
            {
                avgSensorData[i] *= invSamps;
            }

            for (int i = 0; i < sensorData.Count; i++)
            {
                for (int j = 0; j < varSensorData.Length; j++)
                {
                    double diff = sensorData[i][j]-avgSensorData[j];
                    varSensorData[j] += diff * diff;
                }
            }
            for (int i = 0; i < varSensorData.Length; i++)
            {
                varSensorData[i] *= invSamps;
            }

            using (FileStream fs = new FileStream("Config/IMU.txt", FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    for (int i = 0; i < avgSensorData.Length; i++)
                    {
                        wr.Write("{0} ", avgSensorData[i]);
                    }
                    wr.Write("/n");
                    for (int i = 0; i < varSensorData.Length; i++)
                    {
                        wr.Write("{0} ", Math.Sqrt(varSensorData[i]));
                    }
                }
            }
        }

        void OnMicroControllerDataReceived(object sender, EventArgs e)
        {
            string[] data = microcontroller.ReadLine().Split(" ".ToCharArray());
            
            int ax, ay, az, gx, gy, gz;
            ax = ay = az = gx = gy = gz = 0;
            if (data.Length >= 7)
            {
                int.TryParse(data[1], out ax);
                int.TryParse(data[2], out ay);
                int.TryParse(data[3], out az);
                int.TryParse(data[4], out gz);
                int.TryParse(data[5], out gx);
                int.TryParse(data[6], out gy);
                if (calibrating)
                {
                    numSamples++;
                    double[] accData = new double[6];
                    
                    accData[0] = ax;
                    accData[1] = ay;
                    accData[2] = az;
                    accData[3] = gz;
                    accData[4] = gx;
                    accData[5] = gy;

                    sensorData.Add(accData);
                }
                ax = Math.Min(ax, 1024);
                ay = Math.Min(ay, 1024);
                az = Math.Min(az, 1024);
                gx = Math.Min(gx, 1024);
                gy = Math.Min(gy, 1024);
                gz = Math.Min(gz, 1024);
            }

            aXBar.Invoke(new MethodInvoker(delegate { aXBar.Value = ax; }));
            aYBar.Invoke(new MethodInvoker(delegate { aYBar.Value = ay; }));
            aZBar.Invoke(new MethodInvoker(delegate { aZBar.Value = az; }));
            gXBar.Invoke(new MethodInvoker(delegate { gXBar.Value = gx; }));
            gYBar.Invoke(new MethodInvoker(delegate { gYBar.Value = gy; }));
            gZBar.Invoke(new MethodInvoker(delegate { gZBar.Value = gz; }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Status: Calibrating...";
            sensorData.Clear();
            numSamples = 0;
            calibrating = true;
            calibTimer.Start();            
        }
    }
}
