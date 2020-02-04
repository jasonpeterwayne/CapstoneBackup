﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tobii.Interaction;
using System.Runtime.InteropServices; // for Marshal
using System.Diagnostics; // for running excutable files
using System.Timers;
using System.IO;

namespace FaceController
{
    [Serializable]
    public struct myOrientation
    {
        public float roll;
        public float pitch;
        public float yaw;
    }

    [Serializable]
    public struct myPupil
    {
        public float leftEyeX;
        public float leftEyeY;
        public float rightEyeX;
        public float rightEyeY;
    }

    // https://developer.affectiva.com/metrics/
    [Serializable]
    public struct facialExpressions
    {
        public float attention;
        public float browFurrow;
        public float browRaise;
        public float cheekRaise;
        public float chinRaise;
        public float dimpler;
        public float eyeClosure;
        public float eyeWiden;
        public float innerBrowRaise;
        public float jawDrop;
        public float lidTighten;
        public float lipCornerDepressor;
        public float lipPress;
        public float lipPucker;
        public float lipStretch;
        public float lipSuck;
        public float mouthOpen;  
        public float noseWrinkle;
        public float smile;
        public float smirk;
        public float upperLipRaise;
    }

    public partial class ControlDlg : Form
    {
        public ControlDlg()
        {
            InitializeComponent();
            textBox_fSimIP.Text = localIP;
            size_ori = Marshal.SizeOf(oriToSend);
            size_pupil = Marshal.SizeOf(pupilToSend);
            size_exp = Marshal.SizeOf(expToSend);
        }

        static int size_ori;
        static int size_pupil;
        static int size_exp;

        private static bool dataSendRequested = false;

        private static System.Timers.Timer aTimer;

        Host host;
        GazePointDataStream gazePointDataStream;

        int eyeXScale = (int)(1920f / 2f);
        int eyeYScale = (int)(1080f / 2f);

        private void checkBox_eyeTracker_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_eyeTracker.Checked)
            {
                host = new Host();
                gazePointDataStream = host.Streams.CreateGazePointDataStream();
                gazePointDataStream.GazePoint((gazePointX, gazePointY, _)
                    => GazeDataReceived(gazePointX, gazePointY));
                textBox_eyeXScale.Text = eyeXScale.ToString();
                textBox_eyeYScale.Text = eyeYScale.ToString();
            }
            else
            {
                host.DisableConnection();
                host = null;
            }
        }

        private void GazeDataReceived(double X, double Y)
        {
            double normalizedX = -40f * (X - (double)eyeXScale) / (double)eyeXScale;
            double normalizedY = 25f * (Y - (double)eyeYScale) / (double)eyeYScale;
            UpdateTextBox(normalizedX.ToString(), textBox_eyeX);
            UpdateTextBox(normalizedY.ToString(), textBox_eyeY);

            // update gaze parameters and update gaze control bar
            if (!checkBox_gazeX.Checked)
            {
                pupilToSend.leftEyeX = (float)normalizedX;
                pupilToSend.rightEyeX = (float)normalizedX;
                //trackBar_gazeX.Value = (int)normalizedX;
                UpdateTrackBar((int)normalizedX, trackBar_gazeX);
                UpdateTextBox(((int)normalizedX).ToString(), textBox_gazeX);
            }

            if (!checkBox_gazeY.Checked)
            {
                pupilToSend.leftEyeY = (float)normalizedY;
                pupilToSend.rightEyeY = (float)normalizedY;
                //trackBar_gazeY.Value = (int)normalizedY;
                UpdateTrackBar((int)normalizedY, trackBar_gazeY);
                UpdateTextBox(((int)normalizedY).ToString(), textBox_gazeY);
            }

            //SendDataAll();
            dataSendRequested = true;
        }

        public void UpdateTrackBar(int value, TrackBar trackbar)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<int, TrackBar>(UpdateTrackBar), new object[] { value, trackbar });
                return;
            }
            trackbar.Value = value;
        }

        public void UpdateTextBox(string value, TextBox textbox)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, TextBox>(UpdateTextBox), new object[] { value, textbox });
                return;
            }
            textbox.Text = value;
        }

        static facialExpressions expToSend = new facialExpressions();
        static myOrientation oriToSend = new myOrientation();
        static myPupil pupilToSend = new myPupil();

        static string localIP = myNetworks.myNetwork.GetLocalIPAddress();

        private void checkBox_socketFaceSimulator_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_socketFaceSimulator.Checked)
            {
                //myNetworks.myNetwork.StartClient("localhost", 54321);
                string IPAddr = textBox_fSimIP.Text;
                if(!myNetworks.myNetwork.StartClient(IPAddr, 54321))
                {
                    checkBox_socketFaceSimulator.Checked = false;
                }

                aTimer = new System.Timers.Timer(100);
                aTimer.Elapsed += ATimer_Elapsed;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }
            else
            {
                try
                {
                    byte[] termination = {2, 1};
                    myNetworks.myNetwork.SendData(termination);
                    myNetworks.myNetwork.CloseClient();
                    aTimer.Enabled = false;
                }
                catch (Exception ee)
                { }
            }
        }

        private void ATimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(dataSendRequested)
            {
                dataSendRequested = false;

                SendDataAll();
            }
            //throw new NotImplementedException();
        }

        private void checkBox_controlBars_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar_headRoll_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headRoll.Text = curTrackBar.Value.ToString();

            if (checkBox_headRoll.Checked)
            {
                oriToSend.roll = curTrackBar.Value;
                //SendDataAll();
                dataSendRequested = true;
            }
        }

        private void textBox_headRoll_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_headRoll.Value = intVal;
                    oriToSend.roll = intVal; 
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        static private void SendDataAll()
        {
            //if (!ControlDlg.checkBox_socketFaceSimulator.Checked) return;

            byte[] expRawData = Serialize(expToSend);
            byte[] pupilRawData = Serialize(pupilToSend);
            byte[] oriRawData = Serialize(oriToSend);
            byte[] data2send = new byte[oriRawData.Length + pupilRawData.Length + expRawData.Length + 1];
            data2send[0] = (byte)(data2send.Length);
            Array.Copy(oriRawData, 0, data2send, 1, oriRawData.Length);
            Array.Copy(pupilRawData, 0, data2send, (1 + oriRawData.Length), pupilRawData.Length);
            Array.Copy(expRawData, 0, data2send, (1 + oriRawData.Length + pupilRawData.Length), expRawData.Length);

            if (!myNetworks.myNetwork.SendData(data2send))
                return;
        }

        static byte[] Serialize(object myStructure)
        {
            int rawsize = Marshal.SizeOf(myStructure);
            byte[] rawdatas = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return rawdatas;
        }

        private void button_eyeXScaleApply_Click(object sender, EventArgs e)
        {
            int tempIntVal;
            if (Int32.TryParse(textBox_eyeXScale.Text, out tempIntVal))
                eyeXScale = tempIntVal;
            else
                textBox_eyeXScale.Text = eyeXScale.ToString();
        }

        private void button_eyeYScaleApply_Click(object sender, EventArgs e)
        {
            int tempIntVal;
            if (Int32.TryParse(textBox_eyeYScale.Text, out tempIntVal))
                eyeYScale = tempIntVal;
            else
                textBox_eyeYScale.Text = eyeYScale.ToString();
        }

        private void trackBar_headPitch_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headPitch.Text = curTrackBar.Value.ToString();

            if (checkBox_headPitch.Checked)
            {
                oriToSend.pitch = curTrackBar.Value;
                //SendDataAll();
                dataSendRequested = true;
            }
        }

        private void textBox_headPitch_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_headPitch.Value = intVal;
                    oriToSend.pitch = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void checkBox_headPitch_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_headRoll_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar_headYaw_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headYaw.Text = curTrackBar.Value.ToString();

            if (checkBox_headYaw.Checked)
            {
                oriToSend.yaw = curTrackBar.Value;
                //SendDataAll();
                dataSendRequested = true;
            }
        }

        private void textBox_headYaw_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_headYaw.Value = intVal;
                    oriToSend.yaw = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_gazeX_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            //UpdateTextBox(curTrackBar.Value.ToString(), textBox_gazeX);
            textBox_gazeX.Text = curTrackBar.Value.ToString();

            if (checkBox_gazeX.Checked)
            {
                pupilToSend.leftEyeX = curTrackBar.Value;
                pupilToSend.rightEyeX = curTrackBar.Value;
                //SendDataAll();
                dataSendRequested = true;
            }
        }

        private void textBox_gazeX_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;

            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_gazeX.Value = intVal;
                    pupilToSend.leftEyeX = intVal;
                    pupilToSend.rightEyeX = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_gazeY_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_gazeY.Text = curTrackBar.Value.ToString();

            if (checkBox_gazeY.Checked)
            {
                pupilToSend.leftEyeY = curTrackBar.Value;
                pupilToSend.rightEyeY = curTrackBar.Value;
                //SendDataAll();
                dataSendRequested = true;
            }
        }

        private void textBox_gazeY_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;

            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_gazeY.Value = intVal;
                    pupilToSend.leftEyeY = intVal;
                    pupilToSend.rightEyeY = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void checkBox_affectiva_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_affectiva.Checked)
            {
                // run sever newtwork
                myNetworks.myNetwork.StartServer(54322);

                string defaultAffectivaDir = Directory.GetCurrentDirectory();//"L:\\GWU(20170905~)\\Programs\\Face Simulator\\Facial Mocap\\affectiva\\csharp-sample-apps-master\\bin\\x64\\Release";
                FolderBrowserDialog affectivaFolderBrowserDialog = new FolderBrowserDialog();
                //affectivaFolderBrowserDialog.RootFolder = defaultAffectivaDir;
                affectivaFolderBrowserDialog.SelectedPath = defaultAffectivaDir;
                if (affectivaFolderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = affectivaFolderBrowserDialog.SelectedPath + "\\csharp-sample-app.exe";
                    string dataPath = affectivaFolderBrowserDialog.SelectedPath + "\\data";

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @filePath;
                    startInfo.Arguments = "-i camera -d \"" + dataPath + "\" -f 1 -m 1";
                    Process.Start(startInfo);
                }
            }
            else
            {
                
            }
        }

        static public void AffectivaReceived(byte[] buffer, int iReadLength)
        {
            // copy orientation and facial expression data
            byte[] oriData = new byte[size_ori];
            Array.Copy(buffer, 1, oriData, 0, size_ori);
            byte[] exprData = new byte[size_exp];
            Array.Copy(buffer, (1 + size_ori), exprData, 0, size_exp);

            oriToSend = DeserializeOri(oriData);
            expToSend = DeserializeExpr(exprData);

            //SendDataAll();
            dataSendRequested = true;
        }

        static facialExpressions DeserializeExpr(byte[] data)
        {
            int length = data.Length;
            facialExpressions tempInst = new facialExpressions();
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            tempInst = (facialExpressions)Marshal.PtrToStructure(buffer, typeof(facialExpressions));
            //Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return tempInst;
        }

        static myOrientation DeserializeOri(byte[] data)
        {
            int length = data.Length;
            myOrientation tempInst = new myOrientation();
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            tempInst = (myOrientation)Marshal.PtrToStructure(buffer, typeof(myOrientation));
            //Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return tempInst;
        }

        static myPupil DeserializePupil(byte[] data)
        {
            int length = data.Length;
            myPupil tempInst = new myPupil();
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            tempInst = (myPupil)Marshal.PtrToStructure(buffer, typeof(myPupil));
            //Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return tempInst;
        }

        private void ControlDlg_Load(object sender, EventArgs e)
        {

        }

        //16_expToSend(Scroll-TextChanged)
        private void trackBar_browFurrow_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_browFurrow.Text = curTrackBar.Value.ToString();

            if (checkBox_browFurrow.Checked)
            {
                expToSend.browFurrow = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_browFurrow_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_browFurrow.Value = intVal;
                    expToSend.browFurrow = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_browRaise_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_browRaise.Text = curTrackBar.Value.ToString();

            if (checkBox_browRaise.Checked)
            {
                expToSend.browRaise = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_browRaise_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_browRaise.Value = intVal;
                    expToSend.browRaise = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_cheekRaise_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_cheekRaise.Text = curTrackBar.Value.ToString();

            if (checkBox_cheekRaise.Checked)
            {
                expToSend.cheekRaise = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_cheekRaise_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_cheekRaise.Value = intVal;
                    expToSend.cheekRaise = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_chinRaise_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_chinRaise.Text = curTrackBar.Value.ToString();

            if (checkBox_chinRaise.Checked)
            {
                expToSend.chinRaise = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_chinRaise_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_chinRaise.Value = intVal;
                    expToSend.chinRaise = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_dimpler_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_dimpler.Text = curTrackBar.Value.ToString();

            if (checkBox_dimpler.Checked)
            {
                expToSend.dimpler = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_dimpler_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_dimpler.Value = intVal;
                    expToSend.dimpler = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_eyeClosure_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_eyeClosure.Text = curTrackBar.Value.ToString();

            if (checkBox_eyeClosure.Checked)
            {
                expToSend.eyeClosure = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_eyeClosure_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_eyeClosure.Value = intVal;
                    expToSend.eyeClosure = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_eyeWiden_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_eyeWiden.Text = curTrackBar.Value.ToString();

            if (checkBox_eyeWiden.Checked)
            {
                expToSend.eyeWiden = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_eyeWiden_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_eyeWiden.Value = intVal;
                    expToSend.eyeWiden = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_jawDrop_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_jawDrop.Text = curTrackBar.Value.ToString();

            if (checkBox_jawDrop.Checked)
            {
                expToSend.jawDrop = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_jawDrop_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_jawDrop.Value = intVal;
                    expToSend.jawDrop = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_lidTighten_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_lidTighten.Text = curTrackBar.Value.ToString();

            if (checkBox_lidTighten.Checked)
            {
                expToSend.lidTighten = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_lidTighten_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_lidTighten.Value = intVal;
                    expToSend.lidTighten = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_lipCornerDepressor_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_lipCornerDepressor.Text = curTrackBar.Value.ToString();

            if (checkBox_lipCornerDepressor.Checked)
            {
                expToSend.lipCornerDepressor = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_lipCornerDepressor_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_lipCornerDepressor.Value = intVal;
                    expToSend.lipCornerDepressor = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_lipPucker_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_lipPucker.Text = curTrackBar.Value.ToString();

            if (checkBox_lipPucker.Checked)
            {
                expToSend.lipPucker = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_lipPucker_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_lipPucker.Value = intVal;
                    expToSend.lipPucker = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_mouthOpen_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_mouthOpen.Text = curTrackBar.Value.ToString();

            if (checkBox_mouthOpen.Checked)
            {
                expToSend.mouthOpen = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_mouthOpen_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_mouthOpen.Value = intVal;
                    expToSend.mouthOpen = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_noseWrinkle_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_noseWrinkle.Text = curTrackBar.Value.ToString();

            if (checkBox_noseWrinkle.Checked)
            {
                expToSend.noseWrinkle = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_noseWrinkle_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_noseWrinkle.Value = intVal;
                    expToSend.noseWrinkle = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_smile_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_smile.Text = curTrackBar.Value.ToString();

            if (checkBox_smile.Checked)
            {
                expToSend.smile = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_smile_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_smile.Value = intVal;
                    expToSend.smile = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_upperLipRaise_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_upperLipRaise.Text = curTrackBar.Value.ToString();

            if (checkBox_upperLipRaise.Checked)
            {
                expToSend.upperLipRaise = curTrackBar.Value;
                //SendDataAll(); //있어도 잘 돌아가고 없어도 잘 돌아가는데...?
                dataSendRequested = true;
            }
        }

        private void textBox_upperLipRaise_TextChanged(object sender, EventArgs e)
        {
            TextBox curTextBox = (TextBox)sender;
            string text = curTextBox.Text;
            int intVal;
            if (Int32.TryParse(text, out intVal))
            {
                if ((intVal <= 180) && (intVal >= -180))
                    trackBar_upperLipRaise.Value = intVal;
                    expToSend.upperLipRaise = intVal;
                    dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            FileStream fs = File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\test.bin", FileMode.Create);
            int i;

            // BinaryWriter는 파일스트림을 사용해서 객체를 생성한다
            using (BinaryWriter wr = new BinaryWriter(fs))
            {
                // 각각의 샘플데이타를 이진파일에 쓴다(20개...ㅎ)
                wr.Write(oriToSend.roll);  // float
                wr.Write(oriToSend.pitch);
                wr.Write(oriToSend.yaw);

                wr.Write(pupilToSend.leftEyeX);
                wr.Write(pupilToSend.leftEyeY);

                wr.Write(expToSend.browFurrow);
                wr.Write(expToSend.browRaise);
                wr.Write(expToSend.cheekRaise);
                wr.Write(expToSend.chinRaise);
                wr.Write(expToSend.dimpler);

                wr.Write(expToSend.eyeClosure);
                wr.Write(expToSend.eyeWiden);
                wr.Write(expToSend.jawDrop);
                wr.Write(expToSend.lidTighten);
                wr.Write(expToSend.lipCornerDepressor);

                wr.Write(expToSend.lipPucker);
                wr.Write(expToSend.mouthOpen);
                wr.Write(expToSend.noseWrinkle);
                wr.Write(expToSend.smile);
                wr.Write(expToSend.upperLipRaise);
            }
        }

        private void button_browse_Click(object sender, EventArgs e)
        {
            BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\test.bin", FileMode.Open));
            {
                // 각 데이타 타입별로 다양한 Read 메서드를 사용한다
                float headRoll = rdr.ReadSingle();
                float headPitch = rdr.ReadSingle();
                float headYaw = rdr.ReadSingle();

                float eyeGazeX= rdr.ReadSingle();
                float eyeGazeY = rdr.ReadSingle();

                float browFurrow = rdr.ReadSingle();
                float browRaise = rdr.ReadSingle();
                float cheekRaise = rdr.ReadSingle();
                float chinRaise = rdr.ReadSingle();
                float dimper = rdr.ReadSingle();

                float eyeClosure = rdr.ReadSingle();
                float eyeWiden = rdr.ReadSingle();
                float jawDrop = rdr.ReadSingle();
                float lidTighten = rdr.ReadSingle();
                float lipCornerDepressor = rdr.ReadSingle();

                float lipPucker = rdr.ReadSingle();
                float mouthOpen = rdr.ReadSingle();
                float noseWrinkle = rdr.ReadSingle();
                float smile = rdr.ReadSingle();
                float upperLipRaise = rdr.ReadSingle();

                oriToSend.roll = headRoll;
                oriToSend.pitch = headPitch;
                oriToSend.yaw = headYaw;

                pupilToSend.leftEyeX = eyeGazeX;
                pupilToSend.rightEyeX = eyeGazeX;
                pupilToSend.leftEyeY = eyeGazeY;
                pupilToSend.rightEyeY = eyeGazeY;

                expToSend.browFurrow = browFurrow;
                expToSend.browRaise = browRaise;
                expToSend.cheekRaise = cheekRaise;
                expToSend.chinRaise = chinRaise;
                expToSend.dimpler = dimper;

                expToSend.eyeClosure = eyeClosure;
                expToSend.eyeWiden = eyeWiden;
                expToSend.jawDrop = jawDrop;
                expToSend.lidTighten = lidTighten;
                expToSend.lipCornerDepressor = lipCornerDepressor;

                expToSend.lipPucker = lipPucker;
                expToSend.mouthOpen = mouthOpen;
                expToSend.noseWrinkle = noseWrinkle;
                expToSend.smile = smile;
                expToSend.upperLipRaise = upperLipRaise;

                dataSendRequested = true;
            }
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            trackBar_headRoll.Value = 0;
            textBox_headRoll.Text = trackBar_headRoll.Value.ToString();
            oriToSend.roll = 0;
            trackBar_headPitch.Value = 0;
            textBox_headPitch.Text = trackBar_headPitch.Value.ToString();
            oriToSend.pitch = 0;
            trackBar_headYaw.Value = 0;
            textBox_headYaw.Text = trackBar_headYaw.Value.ToString();
            oriToSend.yaw = 0;

            trackBar_gazeX.Value = 0;
            textBox_gazeX.Text = trackBar_gazeX.Value.ToString();
            pupilToSend.leftEyeX = 0;
            pupilToSend.rightEyeX = 0;
            trackBar_gazeY.Value = 0;
            textBox_gazeY.Text = trackBar_gazeY.Value.ToString();
            pupilToSend.leftEyeY = 0;
            pupilToSend.rightEyeY = 0;

            trackBar_browFurrow.Value = 0;
            textBox_browFurrow.Text = trackBar_browFurrow.Value.ToString();
            expToSend.browFurrow = 0;
            trackBar_browRaise.Value = 0;
            textBox_browRaise.Text = trackBar_browRaise.Value.ToString();
            expToSend.browRaise = 0;
            trackBar_cheekRaise.Value = 0;
            textBox_cheekRaise.Text = trackBar_cheekRaise.Value.ToString();
            expToSend.cheekRaise = 0;
            trackBar_chinRaise.Value = 0;
            textBox_chinRaise.Text = trackBar_chinRaise.Value.ToString();
            expToSend.chinRaise = 0;
            trackBar_dimpler.Value = 0;
            textBox_dimpler.Text = trackBar_dimpler.Value.ToString();
            expToSend.dimpler = 0;

            trackBar_eyeClosure.Value = 0;
            textBox_eyeClosure.Text = trackBar_eyeClosure.Value.ToString();
            expToSend.eyeClosure = 0;
            trackBar_eyeWiden.Value = 0;
            textBox_eyeWiden.Text = trackBar_eyeWiden.Value.ToString();
            expToSend.eyeWiden = 0;
            trackBar_jawDrop.Value = 0;
            textBox_jawDrop.Text = trackBar_jawDrop.Value.ToString();
            expToSend.jawDrop = 0;
            trackBar_lidTighten.Value = 0;
            textBox_lidTighten.Text = trackBar_lidTighten.Value.ToString();
            expToSend.lidTighten = 0;
            trackBar_lipCornerDepressor.Value = 0;
            textBox_lipCornerDepressor.Text = trackBar_lipCornerDepressor.Value.ToString();
            expToSend.lipCornerDepressor = 0;

            trackBar_lipPucker.Value = 0;
            textBox_lipPucker.Text = trackBar_lipPucker.Value.ToString();
            expToSend.lipPucker = 0;
            trackBar_mouthOpen.Value = 0;
            textBox_mouthOpen.Text = trackBar_mouthOpen.Value.ToString();
            expToSend.mouthOpen = 0;
            trackBar_noseWrinkle.Value = 0;
            textBox_noseWrinkle.Text = trackBar_noseWrinkle.Value.ToString();
            expToSend.noseWrinkle = 0;
            trackBar_smile.Value = 0;
            textBox_smile.Text = trackBar_smile.Value.ToString();
            expToSend.smile = 0;
            trackBar_upperLipRaise.Value = 0;
            textBox_upperLipRaise.Text = trackBar_upperLipRaise.Value.ToString();
            expToSend.upperLipRaise = 0;

            dataSendRequested = true;
        }
    }
}
