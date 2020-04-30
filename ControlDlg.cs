using System;
using System.Diagnostics; // for running excutable files
using System.IO;
using System.Runtime.InteropServices; // for Marshal
using System.Timers;
using System.Windows.Forms;
using Tobii.Interaction;

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

    public struct List //저장된 파일의 변수값 20개를 불러오기 위한 중간 변수들
    {
        public float headRoll;
        public float headPitch;
        public float headYaw;

        public float eyeGazeX;
        public float eyeGazeY;

        public float browFurrow;
        public float browRaise;
        public float cheekRaise;
        public float chinRaise;
        public float dimpler;

        public float eyeClosure;
        public float eyeWiden;
        public float jawDrop;
        public float lidTighten;
        public float lipCornerDepressor;

        public float lipPucker;
        public float mouthOpen;
        public float noseWrinkle;
        public float smile;
        public float upperLipRaise;
    }

    struct faceEmotion
    {
        public float Joy;
        public float Sadness;
        public float Anger;
        public float Fear;
        public float Disgust;
        public float Surprise;
        public float Contempt;
        public float maxEmo;
    }

    struct pythonLabel
    {
        public int num;
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
            size_emo = Marshal.SizeOf(curEmo);
            size_label = Marshal.SizeOf(dataLabel);
        }

        static int size_ori;
        static int size_pupil;
        static int size_exp;
        static int size_emo;
        static int size_label;

        static String filepath = null; //browse버튼 클릭 시 파일 경로 설정하는 변수
        static int browse_button_click_count = 0; //처음 목표값을 설정하는가 아닌가의 여부를 판단하기 위함 변수
        static float update = 0; //n분의 1만큼 업데이트할 때 사옹되는 변수

        private static bool dataSendRequested = false;

        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer bTimer; //반복함수 호출을 위한 TIMER
        private static System.Timers.Timer cTimer; //사용자 감정에 상응하는 얼굴표정 반복 호출 Timer


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

        //affectiva에서 받아오는 구조체 변수들
        static facialExpressions expToSend = new facialExpressions();
        static myOrientation oriToSend = new myOrientation();
        static myPupil pupilToSend = new myPupil();
        static faceEmotion curEmo = new faceEmotion(); //affectiva에서 받아오는 이모션 7개+최대 emotion값
        static pythonLabel dataLabel = new pythonLabel();
        static List bList = new List();
        static List cList = new List();
        static List pList = new List(); //위치를 천천히 update하기위해 이전값들을 저장하는 구조체
        //avatar로 보내는 구조체 변수들
        static facialExpressions expToSend_A = new facialExpressions();
        static myOrientation oriToSend_A = new myOrientation();
        static myPupil pupilToSend_A = new myPupil();
        //static string emoAnalysis;

        static string localIP = myNetworks.myNetwork.GetLocalIPAddress();

        private void checkBox_socketFaceSimulator_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_socketFaceSimulator.Checked)
            {
                //myNetworks.myNetwork.StartClient("localhost", 54321);
                string IPAddr = textBox_fSimIP.Text;
                if (!myNetworks.myNetwork.StartClient(IPAddr, 54321))
                {
                    checkBox_socketFaceSimulator.Checked = false;
                }

                //aTimer = new System.Timers.Timer(100);
                aTimer = new System.Timers.Timer(10);
                //aTimer.Elapsed += ATimer_Elapsed;
                aTimer.Elapsed += ATimer_Elapsed_SendDataAll;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }
            else
            {
                try
                {
                    byte[] termination = { 2, 1 };
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
            if (dataSendRequested)
            {
                dataSendRequested = false;

                SendDataAll();
            }
            //throw new NotImplementedException();
        }

        private void ATimer_Elapsed_SendDataAll(object sender, ElapsedEventArgs e)
        {
            if (dataSendRequested)
            {
                set_avatar_response();
                SendDataAll();
            }
            //throw new NotImplementedException();
        }

        private void checkBox_controlBars_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_headRoll.Checked = true;
            checkBox_headPitch.Checked = true;
            checkBox_headYaw.Checked = true;

            checkBox_gazeX.Checked = true;
            checkBox_gazeY.Checked = true;

            checkBox_browRaise.Checked = true;
            checkBox_browFurrow.Checked = true;
            checkBox_cheekRaise.Checked = true;
            checkBox_chinRaise.Checked = true;
            checkBox_dimpler.Checked = true;

            checkBox_eyeClosure.Checked = true;
            checkBox_eyeWiden.Checked = true;
            checkBox_jawDrop.Checked = true;
            checkBox_lidTighten.Checked = true;
            checkBox_lipCornerDepressor.Checked = true;

            checkBox_lipPucker.Checked = true;
            checkBox_mouthOpen.Checked = true;
            checkBox_noseWrinkle.Checked = true;
            checkBox_smile.Checked = true;
            checkBox_upperLipRaise.Checked = true;
        }

        static private void SendDataAll() //수정 필요!
        {
            //if (!ControlDlg.checkBox_socketFaceSimulator.Checked) return;

            byte[] expRawData = Serialize(expToSend_A);
            byte[] pupilRawData = Serialize(pupilToSend_A);
            byte[] oriRawData = Serialize(oriToSend_A);
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

        //머리 움직임 조절 부분
        private void trackBar_headRoll_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headRoll.Text = curTrackBar.Value.ToString();

            if (checkBox_headRoll.Checked)
            {
                //bTimer.Enabled = false; //browse한 후, controlBar를 조작하려고 하니 수정이 안됨을 확인하여 반복 타이머를 멈춤
                oriToSend_A.roll = curTrackBar.Value;
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
                oriToSend_A.roll = intVal;
                dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_headPitch_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headPitch.Text = curTrackBar.Value.ToString();

            if (checkBox_headPitch.Checked)
            {
                //bTimer.Enabled = false;
                oriToSend_A.pitch = curTrackBar.Value;
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
                oriToSend_A.pitch = intVal;
                dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        private void trackBar_headYaw_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_headYaw.Text = curTrackBar.Value.ToString();

            if (checkBox_headYaw.Checked)
            {
                //bTimer.Enabled = false;
                oriToSend_A.yaw = curTrackBar.Value;
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
                oriToSend_A.yaw = intVal;
                dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        //시선 움직임 조절 부분
        private void trackBar_gazeX_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            //UpdateTextBox(curTrackBar.Value.ToString(), textBox_gazeX);
            textBox_gazeX.Text = curTrackBar.Value.ToString();

            if (checkBox_gazeX.Checked)
            {
                //bTimer.Enabled = false;
                pupilToSend_A.leftEyeX = curTrackBar.Value;
                pupilToSend_A.rightEyeX = curTrackBar.Value;
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
                pupilToSend_A.leftEyeX = intVal;
                pupilToSend_A.rightEyeX = intVal;
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
                //bTimer.Enabled = false;
                pupilToSend_A.leftEyeY = curTrackBar.Value;
                pupilToSend_A.rightEyeY = curTrackBar.Value;
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
                pupilToSend_A.leftEyeY = intVal;
                pupilToSend_A.rightEyeY = intVal;
                dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        //affectiva 불러오는 부분
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

                    //set_avatar_response();
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
            byte[] emoData = new byte[size_emo];
            Array.Copy(buffer, (1 + size_ori + size_exp), emoData, 0, size_emo);
            byte[] LabelEmo = new byte[size_label];
            Array.Copy(buffer, (1 + size_ori + size_exp + size_emo), LabelEmo, 0, size_label);

            oriToSend = DeserializeOri(oriData);
            expToSend = DeserializeExpr(exprData);
            curEmo = DeserializeEmo(emoData);
            dataLabel = DeserializeLabel(LabelEmo);

           Console.WriteLine(dataLabel.num);

            //SendDataAll();
            dataSendRequested = true;
        }

        static pythonLabel DeserializeLabel(byte[] data)
        {
            int length = data.Length;
            pythonLabel tempInst = new pythonLabel();
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            tempInst = (pythonLabel)Marshal.PtrToStructure(buffer, typeof(pythonLabel));
            //Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return tempInst;
        }

        //affectiva에서 받아오는 byte단위 정보를 변환하여 다시 struct형 변수에 대입
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

        static faceEmotion DeserializeEmo(byte[] data)
        {
            int length = data.Length;
            faceEmotion tempInst = new faceEmotion();
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            tempInst = (faceEmotion)Marshal.PtrToStructure(buffer, typeof(faceEmotion));
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

        //Form창 띄우기
        private void ControlDlg_Load(object sender, EventArgs e)
        {
            
        }

        //15_expToSend(Scroll-TextChanged)
        private void trackBar_browFurrow_Scroll(object sender, EventArgs e)
        {
            TrackBar curTrackBar = (TrackBar)sender;
            textBox_browFurrow.Text = curTrackBar.Value.ToString();

            if (checkBox_browFurrow.Checked)
            {
                //bTimer.Enabled = false;
                expToSend_A.browFurrow = curTrackBar.Value;
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
                expToSend_A.browFurrow = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.browRaise = curTrackBar.Value;
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
                expToSend_A.browRaise = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.cheekRaise = curTrackBar.Value;
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
                expToSend_A.cheekRaise = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.chinRaise = curTrackBar.Value;
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
                expToSend_A.chinRaise = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.dimpler = curTrackBar.Value;
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
                expToSend_A.dimpler = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.eyeClosure = curTrackBar.Value;
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
                expToSend_A.eyeClosure = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.eyeWiden = curTrackBar.Value;
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
                expToSend_A.eyeWiden = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.jawDrop = curTrackBar.Value;
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
                expToSend_A.jawDrop = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.lidTighten = curTrackBar.Value;
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
                expToSend_A.lidTighten = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.lipCornerDepressor = curTrackBar.Value;
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
                expToSend_A.lipCornerDepressor = intVal;
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

                //bTimer.Enabled = false; 
                expToSend_A.lipPucker = curTrackBar.Value;
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
                expToSend_A.lipPucker = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.mouthOpen = curTrackBar.Value;
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
                expToSend_A.mouthOpen = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.noseWrinkle = curTrackBar.Value;
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
                expToSend_A.noseWrinkle = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.smile = curTrackBar.Value;
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
                expToSend_A.smile = intVal;
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
                //bTimer.Enabled = false;
                expToSend_A.upperLipRaise = curTrackBar.Value;
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
                expToSend_A.upperLipRaise = intVal;
                dataSendRequested = true;
            }
            else
            {
                return;
            }
        }

        //여기까지가 20개의 구성요소 text change하고 scroll bar 변하는 것 구현하는 코드

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Bin file (*.bin)|*.bin|C# file (*.cs)|*.cs";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                //int i;

                // BinaryWriter는 파일스트림을 사용해서 객체를 생성한다
                using (BinaryWriter wr = new BinaryWriter(fs))
                {
                    // 각각의 샘플데이타를 이진파일에 쓴다(20개...ㅎ)
                    wr.Write(oriToSend_A.roll);  // float
                    wr.Write(oriToSend_A.pitch);
                    wr.Write(oriToSend_A.yaw);

                    wr.Write(pupilToSend_A.leftEyeX);
                    wr.Write(pupilToSend_A.leftEyeY);

                    wr.Write(expToSend_A.browFurrow);
                    wr.Write(expToSend_A.browRaise);
                    wr.Write(expToSend_A.cheekRaise);
                    wr.Write(expToSend_A.chinRaise);
                    wr.Write(expToSend_A.dimpler);

                    wr.Write(expToSend_A.eyeClosure);
                    wr.Write(expToSend_A.eyeWiden);
                    wr.Write(expToSend_A.jawDrop);
                    wr.Write(expToSend_A.lidTighten);
                    wr.Write(expToSend_A.lipCornerDepressor);

                    wr.Write(expToSend_A.lipPucker);
                    wr.Write(expToSend_A.mouthOpen);
                    wr.Write(expToSend_A.noseWrinkle);
                    wr.Write(expToSend_A.smile);
                    wr.Write(expToSend_A.upperLipRaise);
                }
            }
        }

        private float smooth_ratio_update(float pre_value, float objective_value, double ratio) //이전위치값, 목표위치값, 비율로 현재위치를 업데이트시켜 줌
        {
            float Rvalue;

            Rvalue = Convert.ToSingle((objective_value * ratio) + (pre_value * (1 - ratio)));

            return Rvalue;
        }

        private void browseLoop(object sender, ElapsedEventArgs e)  //반복호출함수
        {
            oriToSend_A.roll = smooth_ratio_update(pList.headRoll, bList.headRoll, 0.03); //이전위치값, 목표위치값, 비율로 현재위치를 업데이트시켜 줌, 비율 조절 필요
            pList.headRoll = oriToSend_A.roll;
            oriToSend_A.pitch = smooth_ratio_update(pList.headPitch, bList.headPitch, 0.03);
            pList.headPitch = oriToSend_A.pitch;
            oriToSend_A.yaw = smooth_ratio_update(pList.headYaw, bList.headYaw, 0.03);
            pList.headYaw = oriToSend_A.yaw;

            pupilToSend_A.leftEyeX = smooth_ratio_update(pList.eyeGazeX, bList.eyeGazeX, 0.03);
            pupilToSend_A.rightEyeX = smooth_ratio_update(pList.eyeGazeX, bList.eyeGazeX, 0.03);
            pList.eyeGazeX = pupilToSend_A.leftEyeX;
            pupilToSend_A.leftEyeY = smooth_ratio_update(pList.eyeGazeY, bList.eyeGazeY, 0.03);
            pupilToSend_A.rightEyeY = smooth_ratio_update(pList.eyeGazeY, bList.eyeGazeY, 0.03);
            pList.eyeGazeY = pupilToSend_A.leftEyeY;

            expToSend_A.browFurrow = smooth_ratio_update(pList.browFurrow, bList.browFurrow, 0.03);
            pList.browFurrow = expToSend_A.browFurrow;
            expToSend_A.browRaise = smooth_ratio_update(pList.browRaise, bList.browRaise, 0.03);
            pList.browRaise = expToSend_A.browRaise;
            expToSend_A.cheekRaise = smooth_ratio_update(pList.cheekRaise, bList.cheekRaise, 0.03);
            pList.cheekRaise = expToSend_A.cheekRaise;
            expToSend_A.chinRaise = smooth_ratio_update(pList.chinRaise, bList.chinRaise, 0.03);
            pList.chinRaise = expToSend_A.chinRaise;
            expToSend_A.dimpler = smooth_ratio_update(pList.dimpler, bList.dimpler, 0.03);
            pList.dimpler = expToSend_A.dimpler;

            expToSend_A.eyeClosure = smooth_ratio_update(pList.eyeClosure, bList.eyeClosure, 0.03);
            pList.eyeClosure = expToSend_A.eyeClosure;
            expToSend_A.eyeWiden = smooth_ratio_update(pList.eyeWiden, bList.eyeWiden, 0.03);
            pList.eyeWiden = expToSend_A.eyeWiden;
            expToSend_A.jawDrop = smooth_ratio_update(pList.jawDrop, bList.jawDrop, 0.03);
            pList.jawDrop = expToSend_A.jawDrop;
            expToSend_A.lidTighten = smooth_ratio_update(pList.lidTighten, bList.lidTighten, 0.03);
            pList.lidTighten = expToSend_A.lidTighten;
            expToSend_A.lipCornerDepressor = smooth_ratio_update(pList.lipCornerDepressor, bList.lipCornerDepressor, 0.03);
            pList.lipCornerDepressor = expToSend_A.lipCornerDepressor;

            expToSend_A.lipPucker = smooth_ratio_update(pList.lipPucker, bList.lipPucker, 0.03);
            pList.lipPucker = expToSend_A.lipPucker;
            expToSend_A.mouthOpen = smooth_ratio_update(pList.mouthOpen, bList.mouthOpen, 0.03);
            pList.mouthOpen = expToSend_A.mouthOpen;
            expToSend_A.noseWrinkle = smooth_ratio_update(pList.noseWrinkle, bList.noseWrinkle, 0.03);
            pList.noseWrinkle = expToSend_A.noseWrinkle;
            expToSend_A.smile = smooth_ratio_update(pList.smile, bList.smile, 0.03);
            pList.smile = expToSend_A.smile;
            expToSend_A.upperLipRaise = smooth_ratio_update(pList.upperLipRaise, bList.upperLipRaise, 0.03);
            pList.upperLipRaise = expToSend_A.upperLipRaise;

            dataSendRequested = true;
        }

        private void savePreviousList()
        {
            pList.headRoll = Convert.ToSingle(oriToSend_A.roll - 0.01);
            pList.headPitch = Convert.ToSingle(oriToSend_A.pitch - 0.01);
            pList.headYaw = Convert.ToSingle(oriToSend_A.yaw - 0.01);

            pList.eyeGazeX = Convert.ToSingle(pupilToSend_A.leftEyeX - 0.01);
            pList.eyeGazeY = Convert.ToSingle(pupilToSend_A.leftEyeY - 0.01);

            pList.browFurrow = Convert.ToSingle(expToSend_A.browFurrow - 0.01);
            pList.browRaise = Convert.ToSingle(expToSend_A.browRaise - 0.01);
            pList.cheekRaise = Convert.ToSingle(expToSend_A.cheekRaise - 0.01);
            pList.chinRaise = Convert.ToSingle(expToSend_A.chinRaise - 0.01);
            pList.dimpler = Convert.ToSingle(expToSend_A.dimpler - 0.01);

            pList.eyeClosure = Convert.ToSingle(expToSend_A.eyeClosure - 0.01);
            pList.eyeWiden = Convert.ToSingle(expToSend_A.eyeWiden - 0.01);
            pList.jawDrop = Convert.ToSingle(expToSend_A.jawDrop - 0.01);
            pList.lidTighten = Convert.ToSingle(expToSend_A.lidTighten - 0.01);
            pList.lipCornerDepressor = Convert.ToSingle(expToSend_A.lipCornerDepressor - 0.01);

            pList.lipPucker = Convert.ToSingle(expToSend_A.lipPucker - 0.01);
            pList.mouthOpen = Convert.ToSingle(expToSend_A.mouthOpen - 0.01);
            pList.noseWrinkle = Convert.ToSingle(expToSend_A.noseWrinkle - 0.01);
            pList.smile = Convert.ToSingle(expToSend_A.smile - 0.01);
            pList.upperLipRaise = Convert.ToSingle(expToSend_A.upperLipRaise - 0.01);
        }

        private void updateCompareList()
        {
            cList.headRoll = bList.headRoll;
            cList.headPitch = bList.headPitch;
            cList.headYaw = bList.headYaw;

            cList.eyeGazeX = bList.eyeGazeX;
            cList.eyeGazeY = bList.eyeGazeY;

            cList.browFurrow = bList.browFurrow;
            cList.browRaise = bList.browRaise;
            cList.cheekRaise = bList.cheekRaise;
            cList.chinRaise = bList.chinRaise;
            cList.dimpler = bList.dimpler;

            cList.eyeClosure = bList.eyeClosure;
            cList.eyeWiden = bList.eyeWiden;
            cList.jawDrop = bList.jawDrop;
            cList.lidTighten = bList.lidTighten;
            cList.lipCornerDepressor = bList.lipCornerDepressor;

            cList.lipPucker = bList.lipPucker;
            cList.mouthOpen = bList.mouthOpen;
            cList.noseWrinkle = bList.noseWrinkle;
            cList.smile = bList.smile;
            cList.upperLipRaise = bList.upperLipRaise;
        }

        public void browse_expression(int check)
        {
            //표정을 불러올 경우, 순간적으로 바뀌지 않고 천천히 바뀌게 만들고자 하는 함수
            //버튼을 클릭시 읽어오기만 하고 값을 대입하는 것은 여기서 작동하도록 해야 할 것 같음
            //그러면 버튼 클릭시 읽어오는 변수들을 전역으로 변경해야 할 듯...

            bTimer = new System.Timers.Timer(20); //1000 = 1sec 업데이트 속도조절 필요

            //계산(smooth_ratio_update)을 위한 이전위치값 저장 (이전위치값 = 현재위치값-0.01로 설정,눈때문에!)
            savePreviousList();

            if (check == 1) //버튼이 한번 눌렸을 경우(초기 시작)
            {
                //여기서 초기 목표값들을 저장해서 다음 목표값과 비교를 위한 비교군 형성(cList)
                updateCompareList();

                bTimer.Elapsed += browseLoop;
                bTimer.AutoReset = true;
                bTimer.Enabled = true;
            }
            else //browse버튼이 2번 이상 눌렸을 경우(또 불러오는가)
            {
                //if(목표위치값이 바뀌었는가 == true)
                //{
                //현재 위치값에서 목표위치까지 n분할로 나눠주는 함수(리턴값을 돌려줘서 현재 위치값 업데이트 필요) 호출
                //}
                //else 목표위치값이 바뀌지 않는 경우
                //{
                //원래 위치값에서 목표위치까지 n분할로 나눠주는 함수 호출(리턴값을 돌려줘서 현재 위치값 업데이트 필요)
                //}

                if (bList.headRoll != cList.headRoll
                    || bList.headPitch != cList.headPitch
                    || bList.headYaw != cList.headYaw
                    || bList.eyeGazeX != cList.eyeGazeX
                    || bList.eyeGazeY != cList.eyeGazeY
                    || bList.browFurrow != cList.browFurrow
                    || bList.browRaise != cList.browRaise
                    || bList.cheekRaise != cList.cheekRaise
                    || bList.chinRaise != cList.chinRaise
                    || bList.dimpler != cList.dimpler
                    || bList.eyeClosure != cList.eyeClosure
                    || bList.eyeWiden != cList.eyeWiden
                    || bList.jawDrop != cList.jawDrop
                    || bList.lidTighten != cList.lidTighten
                    || bList.lipCornerDepressor != cList.lipCornerDepressor
                    || bList.lipPucker != cList.lipPucker
                    || bList.mouthOpen != cList.mouthOpen
                    || bList.noseWrinkle != cList.noseWrinkle
                    || bList.smile != cList.smile
                    || bList.upperLipRaise != cList.upperLipRaise)
                {
                    updateCompareList();

                    bTimer.Elapsed += browseLoop;
                    bTimer.AutoReset = true;
                    bTimer.Enabled = true;
                }
                else
                {
                    bTimer.Elapsed += browseLoop;
                    bTimer.AutoReset = true;
                    bTimer.Enabled = true;
                }
            }
        }

        public void browse_expression_avatar()
        {
            savePreviousList();

            if (bList.headRoll != cList.headRoll
                || bList.headPitch != cList.headPitch
                || bList.headYaw != cList.headYaw
                || bList.eyeGazeX != cList.eyeGazeX
                || bList.eyeGazeY != cList.eyeGazeY
                || bList.browFurrow != cList.browFurrow
                || bList.browRaise != cList.browRaise
                || bList.cheekRaise != cList.cheekRaise
                || bList.chinRaise != cList.chinRaise
                || bList.dimpler != cList.dimpler
                || bList.eyeClosure != cList.eyeClosure
                || bList.eyeWiden != cList.eyeWiden
                || bList.jawDrop != cList.jawDrop
                || bList.lidTighten != cList.lidTighten
                || bList.lipCornerDepressor != cList.lipCornerDepressor
                || bList.lipPucker != cList.lipPucker
                || bList.mouthOpen != cList.mouthOpen
                || bList.noseWrinkle != cList.noseWrinkle
                || bList.smile != cList.smile
                || bList.upperLipRaise != cList.upperLipRaise)
            {
                updateCompareList();

                bTimer.Elapsed += browseLoop;
                bTimer.AutoReset = true;
                bTimer.Enabled = true;
            }
            else
            {
                bTimer.Elapsed += browseLoop;
                bTimer.AutoReset = true;
                bTimer.Enabled = true;
            }
        }

        public void button_browse_Click(object sender, EventArgs e)
        {
            browse_button_click_count++;

            //String filepath = null;
            openFileDialog1.InitialDirectory = "C:\\";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filepath = openFileDialog1.FileName;

                using (BinaryReader rdr = new BinaryReader(File.Open(filepath, FileMode.Open)))
                {
                    bList.headRoll = rdr.ReadSingle();
                    bList.headPitch = rdr.ReadSingle();
                    bList.headYaw = rdr.ReadSingle();

                    bList.eyeGazeX = rdr.ReadSingle();
                    bList.eyeGazeY = rdr.ReadSingle();

                    bList.browFurrow = rdr.ReadSingle();
                    bList.browRaise = rdr.ReadSingle();
                    bList.cheekRaise = rdr.ReadSingle();
                    bList.chinRaise = rdr.ReadSingle();
                    bList.dimpler = rdr.ReadSingle();

                    bList.eyeClosure = rdr.ReadSingle();
                    bList.eyeWiden = rdr.ReadSingle();
                    bList.jawDrop = rdr.ReadSingle();
                    bList.lidTighten = rdr.ReadSingle();
                    bList.lipCornerDepressor = rdr.ReadSingle();

                    bList.lipPucker = rdr.ReadSingle();
                    bList.mouthOpen = rdr.ReadSingle();
                    bList.noseWrinkle = rdr.ReadSingle();
                    bList.smile = rdr.ReadSingle();
                    bList.upperLipRaise = rdr.ReadSingle();

                    rdr.Close();

                    browse_expression(browse_button_click_count);
                }
            }
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            trackBar_headRoll.Value = 0;
            textBox_headRoll.Text = trackBar_headRoll.Value.ToString();
            oriToSend_A.roll = 0;
            trackBar_headPitch.Value = 0;
            textBox_headPitch.Text = trackBar_headPitch.Value.ToString();
            oriToSend_A.pitch = 0;
            trackBar_headYaw.Value = 0;
            textBox_headYaw.Text = trackBar_headYaw.Value.ToString();
            oriToSend_A.yaw = 0;

            trackBar_gazeX.Value = 0;
            textBox_gazeX.Text = trackBar_gazeX.Value.ToString();
            pupilToSend_A.leftEyeX = 0;
            pupilToSend_A.rightEyeX = 0;
            trackBar_gazeY.Value = 0;
            textBox_gazeY.Text = trackBar_gazeY.Value.ToString();
            pupilToSend_A.leftEyeY = 0;
            pupilToSend_A.rightEyeY = 0;

            trackBar_browFurrow.Value = 0;
            textBox_browFurrow.Text = trackBar_browFurrow.Value.ToString();
            expToSend_A.browFurrow = 0;
            trackBar_browRaise.Value = 0;
            textBox_browRaise.Text = trackBar_browRaise.Value.ToString();
            expToSend_A.browRaise = 0;
            trackBar_cheekRaise.Value = 0;
            textBox_cheekRaise.Text = trackBar_cheekRaise.Value.ToString();
            expToSend_A.cheekRaise = 0;
            trackBar_chinRaise.Value = 0;
            textBox_chinRaise.Text = trackBar_chinRaise.Value.ToString();
            expToSend_A.chinRaise = 0;
            trackBar_dimpler.Value = 0;
            textBox_dimpler.Text = trackBar_dimpler.Value.ToString();
            expToSend_A.dimpler = 0;

            trackBar_eyeClosure.Value = 0;
            textBox_eyeClosure.Text = trackBar_eyeClosure.Value.ToString();
            expToSend_A.eyeClosure = 0;
            trackBar_eyeWiden.Value = 0;
            textBox_eyeWiden.Text = trackBar_eyeWiden.Value.ToString();
            expToSend_A.eyeWiden = 0;
            trackBar_jawDrop.Value = 0;
            textBox_jawDrop.Text = trackBar_jawDrop.Value.ToString();
            expToSend_A.jawDrop = 0;
            trackBar_lidTighten.Value = 0;
            textBox_lidTighten.Text = trackBar_lidTighten.Value.ToString();
            expToSend_A.lidTighten = 0;
            trackBar_lipCornerDepressor.Value = 0;
            textBox_lipCornerDepressor.Text = trackBar_lipCornerDepressor.Value.ToString();
            expToSend_A.lipCornerDepressor = 0;

            trackBar_lipPucker.Value = 0;
            textBox_lipPucker.Text = trackBar_lipPucker.Value.ToString();
            expToSend_A.lipPucker = 0;
            trackBar_mouthOpen.Value = 0;
            textBox_mouthOpen.Text = trackBar_mouthOpen.Value.ToString();
            expToSend_A.mouthOpen = 0;
            trackBar_noseWrinkle.Value = 0;
            textBox_noseWrinkle.Text = trackBar_noseWrinkle.Value.ToString();
            expToSend_A.noseWrinkle = 0;
            trackBar_smile.Value = 0;
            textBox_smile.Text = trackBar_smile.Value.ToString();
            expToSend_A.smile = 0;
            trackBar_upperLipRaise.Value = 0;
            textBox_upperLipRaise.Text = trackBar_upperLipRaise.Value.ToString();
            expToSend_A.upperLipRaise = 0;

            bList.headRoll = 0;
            bList.headPitch = 0;
            bList.headYaw = 0;

            bList.eyeGazeX = 0;
            bList.eyeGazeY = 0;

            bList.browFurrow = 0;
            bList.browRaise = 0;
            bList.cheekRaise = 0;
            bList.chinRaise = 0;
            bList.dimpler = 0;

            bList.eyeClosure = 0;
            bList.eyeWiden = 0;
            bList.jawDrop = 0;
            bList.lidTighten = 0;
            bList.lipCornerDepressor = 0;

            bList.lipPucker = 0;
            bList.mouthOpen = 0;
            bList.noseWrinkle = 0;
            bList.smile = 0;
            bList.upperLipRaise = 0;

            dataSendRequested = true;
        }
        //사용자 얼굴 표정 분석 결과에 따른 아바타의 response 값들 설정

        static private void talking()
        {
            //bList.headRoll = 0;
            //bList.headPitch = 0;
            //bList.headYaw = 0;

            //bList.eyeGazeX = 0;
            //bList.eyeGazeY = 0;

            //bList.browFurrow = 0;
            //bList.browRaise = 0;
            //bList.cheekRaise = Convert.ToSingle(-81);
            //bList.chinRaise = Convert.ToSingle(116);
            //bList.dimpler = Convert.ToSingle(180);

            //bList.eyeClosure = 0;
            //bList.eyeWiden = 0;
            //bList.jawDrop = Convert.ToSingle(95);
            //bList.lidTighten = 0;
            //bList.lipCornerDepressor = Convert.ToSingle(-25);

            //bList.lipPucker = 0;
            //bList.mouthOpen = Convert.ToSingle(124);
            //bList.noseWrinkle = 0;
            //bList.smile = 0;
            //bList.upperLipRaise = Convert.ToSingle(29);
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\talking.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }

        static private void smile()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\smile1.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }

        static private void neutral()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\neutral.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }

        static private void anger()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\angry.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }

        static private void sadness()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\sadness.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }
        static private void Surprise()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\surprise.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }
        static private void Borded()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\Boring.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }
        static private void Curious()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\curious.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }
        static private void bad1()
        {
            using (BinaryReader rdr = new BinaryReader(File.Open(@"C:\Users\SIRLab\Desktop\Face Simulator\FaceController (20190109)\FaceController\bad1.bin", FileMode.Open)))
            {
                bList.headRoll = rdr.ReadSingle();
                bList.headPitch = rdr.ReadSingle();
                bList.headYaw = rdr.ReadSingle();

                bList.eyeGazeX = rdr.ReadSingle();
                bList.eyeGazeY = rdr.ReadSingle();

                bList.browFurrow = rdr.ReadSingle();
                bList.browRaise = rdr.ReadSingle();
                bList.cheekRaise = rdr.ReadSingle();
                bList.chinRaise = rdr.ReadSingle();
                bList.dimpler = rdr.ReadSingle();

                bList.eyeClosure = rdr.ReadSingle();
                bList.eyeWiden = rdr.ReadSingle();
                bList.jawDrop = rdr.ReadSingle();
                bList.lidTighten = rdr.ReadSingle();
                bList.lipCornerDepressor = rdr.ReadSingle();

                bList.lipPucker = rdr.ReadSingle();
                bList.mouthOpen = rdr.ReadSingle();
                bList.noseWrinkle = rdr.ReadSingle();
                bList.smile = rdr.ReadSingle();
                bList.upperLipRaise = rdr.ReadSingle();

                rdr.Close();
            }
        }

        static private void smilec()
        {
            bList.headRoll = 0;
            bList.headPitch = 0;
            bList.headYaw = 0;

            bList.eyeGazeX = 0;
            bList.eyeGazeY = 0;

            bList.browFurrow = Convert.ToSingle(-14);
            bList.browRaise = Convert.ToSingle(4);
            bList.cheekRaise = Convert.ToSingle(88);
            bList.chinRaise = Convert.ToSingle(65);
            bList.dimpler = 0;

            bList.eyeClosure = 0;
            bList.eyeWiden = 0;
            bList.jawDrop = 0;
            bList.lidTighten = 0;
            bList.lipCornerDepressor = 0;

            bList.lipPucker = 0;
            bList.mouthOpen = 0;
            bList.noseWrinkle = 0;
            bList.smile = Convert.ToSingle(132);
            bList.upperLipRaise = 0;
        }

        static private void neutralc()
        {
            bList.headRoll = 0;
            bList.headPitch = 0;
            bList.headYaw = 0;

            bList.eyeGazeX = 0;
            bList.eyeGazeY = 0;

            bList.browFurrow = 0;
            bList.browRaise = Convert.ToSingle(-11);
            bList.cheekRaise = Convert.ToSingle(12);
            bList.chinRaise = 0;
            bList.dimpler = Convert.ToSingle(91);

            bList.eyeClosure = 0;
            bList.eyeWiden = Convert.ToSingle(-78);
            bList.jawDrop = 0;
            bList.lidTighten = Convert.ToSingle(1);
            bList.lipCornerDepressor = Convert.ToSingle(-1);

            bList.lipPucker = 0;
            bList.mouthOpen = Convert.ToSingle(57);
            bList.noseWrinkle = 0;
            bList.smile = 0;
            bList.upperLipRaise = 0;
        }

        static private void badc()
        {
            bList.headRoll = 0;
            bList.headPitch = 0;
            bList.headYaw = 0;

            bList.eyeGazeX = 0;
            bList.eyeGazeY = 0;

            bList.browFurrow = Convert.ToSingle(180);
            bList.browRaise = 0;
            bList.cheekRaise = 0;
            bList.chinRaise = 0;
            bList.dimpler = Convert.ToSingle(91);

            bList.eyeClosure = 0;
            bList.eyeWiden = 0;
            bList.jawDrop = 0;
            bList.lidTighten = 0;
            bList.lipCornerDepressor = 0;

            bList.lipPucker = Convert.ToSingle(-50);
            bList.mouthOpen = Convert.ToSingle(-52);
            bList.noseWrinkle = Convert.ToSingle(4);
            bList.smile = Convert.ToSingle(-113);
            bList.upperLipRaise = 0;
        }

        private void set_avatar_response()
        {

            cTimer = new System.Timers.Timer(20);

            if (dataLabel.num == 1)
            {
                FaceController.zigbeeProgram.zigbeeMain(dataLabel.num);
                //Form2 frm = new Form2();
                //frm.Show();

                savePreviousList();
                smilec();
                updateCompareList();

                cTimer.Elapsed += browseLoop;
                cTimer.AutoReset = true;
                cTimer.Enabled = true;
            }
            else if (dataLabel.num == 2)
            {
                savePreviousList();
                badc();
                updateCompareList();

                cTimer.Elapsed += browseLoop;
                cTimer.AutoReset = true;
                cTimer.Enabled = true;
            }
            //else if (dataLabel.num == 3)
            //{
            //    savePreviousList();
            //    Surprise();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //if (dataLabel.num == 4 && curEmo.maxEmo == curEmo.Joy)
            //{
            //    savePreviousList();
            //    smile();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (dataLabel.num == 4 && curEmo.maxEmo != curEmo.Joy)
            //{
            //    savePreviousList();
            //    neutral();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (dataLabel.num == 5 && curEmo.maxEmo == curEmo.Joy)
            //{
            //    savePreviousList();
            //    neutral();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (dataLabel.num == 5 && curEmo.maxEmo != curEmo.Joy)
            //{
            //    savePreviousList();
            //    bad1();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //if (curEmo.maxEmo == curEmo.Joy)
            //{
            //    savePreviousList();
            //    smilec();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo != curEmo.Joy)
            //{
            //    savePreviousList();
            //    badc();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}


            //if (expToSend.attention >= 80.0 &&
            //    expToSend.browFurrow <= 15.0 &&
            //    expToSend.browRaise <= 15.0 &&
            //    expToSend.cheekRaise <= 15.0 &&
            //    expToSend.chinRaise <= 15.0 &&
            //    expToSend.dimpler <= 15.0 &&
            //    expToSend.eyeClosure <= 15.0 &&
            //    expToSend.eyeWiden <= 15.0 &&
            //    expToSend.innerBrowRaise <= 15.0 &&
            //    expToSend.jawDrop <= 15.0 &&
            //    expToSend.lidTighten <= 15.0 &&
            //    expToSend.lipCornerDepressor <= 15.0 &&
            //    expToSend.lipPress <= 15.0 &&
            //    expToSend.lipPucker <= 15.0 &&
            //    expToSend.lipStretch <= 15.0 &&
            //    expToSend.lipSuck <= 15.0 &&
            //    expToSend.mouthOpen <= 15.0 &&
            //    expToSend.noseWrinkle <= 15.0 &&
            //    expToSend.smile <= 15.0 &&
            //    expToSend.smirk <= 15.0 &&
            //    expToSend.upperLipRaise <= 15.0)
            //{
            //    savePreviousList();
            //    talking();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (expToSend.attention >= 80.0 && expToSend.smile >= 80)
            //{
            //    savePreviousList();
            //    neutral();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo == curEmo.Anger)
            //{
            //    savePreviousList();
            //    anger();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo == curEmo.Sadness)
            //{
            //    savePreviousList();
            //    sadness();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo == curEmo.Surprise)
            //{
            //    savePreviousList();
            //    Surprise();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo == curEmo.Contempt)
            //{
            //    savePreviousList();
            //    Borded();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
            //else if (curEmo.maxEmo == curEmo.Joy)
            //{
            //    savePreviousList();
            //    Curious();
            //    updateCompareList();

            //    cTimer.Elapsed += browseLoop;
            //    cTimer.AutoReset = true;
            //    cTimer.Enabled = true;
            //}
        }

        //private void loop_zigbee()
        //{
        //    //FaceController.zigbeeProgram.zigbeeMain(1);
        //    while (true)
        //    {
        //        FaceController.zigbeeProgram.zigbeeMain(dataLabel.num);
        //    }
        //}

        private void checkBox_zigbee_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox_zigbee.Checked)
            {

                System.Console.WriteLine("hdgfkagsfdglaerg!");

                Console.WriteLine("below" + dataLabel.num);
                //loop_zigbee();
                //FaceController.zigbeeProgram.zigbeeMain(1);
                //FaceController.zigbeeProgram.zigbeeMain(dataLabel.num);


                System.Console.WriteLine("retkejrhgleurglielfgjlejrgljergkjsdfgjlekjfgblejrgerg!");

            }
        }
    }
}
