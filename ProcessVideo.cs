using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace csharp_sample_app
{
    public partial class ProcessVideo : Form, Affdex.ProcessStatusListener, Affdex.ImageListener
    {
        [Serializable]
        struct myFeaturePoint
        {
            public int id;
            public Single x; // Single == float
            public Single y;
        }

        [Serializable]
        struct myOrientation
        {
            public float roll;
            public float pitch;
            public float yaw;
        }

        [Serializable]
        struct facialExpressions
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

        //struct faceEmotion
        //{
        //    public float Joy;
        //    public float Sadness;
        //    public float Anger;
        //    public float Fear;
        //    public float Disgust;
        //    public float Surprise;
        //    public float Contempt;
        //    public float maxEmo;
        //}
        // expressions
        // refere following links
        // http://affectiva.github.io/developerportal/pages/platforms/v3_1/cpp/classdocs/affdex-native/structaffdex_1_1_expressions.html
        // http://www.affectiva.com/wp-content/uploads/2017/03/McDuff_2016_Affdex.pdf

        // https://knowledge.affectiva.com/v3.2/docs/facial-landmarks-1
        static int num_featurePoints = 34;
        myFeaturePoint[] myFeaturePoints = new myFeaturePoint[num_featurePoints];

        public ProcessVideo(Affdex.Detector detector)
        {
            System.Console.WriteLine("Starting Interface...");
            this.detector = detector;
            detector.setImageListener(this);
            detector.setProcessStatusListener(this);
            InitializeComponent();
            rwLock = new ReaderWriterLock();
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            //Line added by Hifza
            //Affdex log contains full log from last session of emotion game
            //Affdex results are sent to emotion game code to process data in chunks
            //System.IO.File.WriteAllText(@"C:\Users\artmed\Desktop\sangwonlee\Affdex_Outputs\Affdex_Log.txt", String.Empty);
            //System.IO.File.WriteAllText(@"C:\Users\artmed\Documents\sangwonlee\Affdex_Outputs\Affdex_Results.txt", String.Empty);

        }
        int empty_flag = 1; //필요?

        public void onImageCapture(Affdex.Frame frame)
        {
            frame.Dispose();

        }

        int myFlag = 0;

        public void onImageResults(Dictionary<int, Affdex.Face> faces, Affdex.Frame frame)
        {
            process_fps = 1.0f / (frame.getTimestamp() - process_last_timestamp);
            process_last_timestamp = frame.getTimestamp();
            // System.Console.WriteLine(" pfps: {0}", process_fps.ToString());

            byte[] pixels = frame.getBGRByteArray();
            this.img = new Bitmap(frame.getWidth(), frame.getHeight(), PixelFormat.Format24bppRgb);
            var bounds = new Rectangle(0, 0, frame.getWidth(), frame.getHeight());
            BitmapData bmpData = img.LockBits(bounds, ImageLockMode.WriteOnly, img.PixelFormat);
            IntPtr ptr = bmpData.Scan0;


            int data_x = 0;
            int ptr_x = 0;
            int row_bytes = frame.getWidth() * 3;

            // The bitmap requires bitmap data to be byte aligned.
            // http://stackoverflow.com/questions/20743134/converting-opencv-image-to-gdi-bitmap-doesnt-work-depends-on-image-size

            for (int y = 0; y < frame.getHeight(); y++)
            {
                Marshal.Copy(pixels, data_x, ptr + ptr_x, row_bytes);
                data_x += row_bytes;
                ptr_x += bmpData.Stride;
            }
            img.UnlockBits(bmpData);

            this.faces = faces;
            string[] emotionArr = new string[7];
            float[] indexArr = new float[7];

            if (myFlag == 0)
            {
                //Hifza
                //initialize socket for data transfer to java server
                //myNetworks.myNetwork.StartClient("192.168.123.2", 55555);
                myNetworks.myNetwork.StartClient("localhost", 54322);
                myFlag = 1;
            }

            foreach (KeyValuePair<int, Affdex.Face> pair in faces)
            {
                Affdex.Face face = pair.Value;
                if (face != null)
                {
                    int a = 0;
                    foreach (PropertyInfo prop in typeof(Affdex.Emotions).GetProperties())
                    {
                        float value = (float)prop.GetValue(face.Emotions, null);

                        string output = string.Format("{0}: {1}", prop.Name, value);
                        if (prop.Name != "Engagement" && prop.Name != "Valence")
                        {
                            emotionArr[a] = prop.Name;
                            indexArr[a] = value;
                            a = a + 1;
                        }
                        output = frame.getTimestamp().ToString() + " " + output;

                        //System.IO.File.AppendAllText(@"C:\Users\artmed\Documents\sangwonlee\Affdex_Outputs\Affdex_Results.txt", DateTime.Now.ToString("hh.mm.ss.ffffff") + " " + output + " " + Environment.NewLine);
                        //System.Console.WriteLine(output);
                    }
                    float maxValue = indexArr.Max();
                    int maxIndex = indexArr.ToList().IndexOf(maxValue);
                    string maxEmotion = emotionArr[maxIndex];


                    //determine arousal/valence values from for emotion name
                    string[] emoArr = new string[18];
                    double[] valArr = new double[18];
                    double[] arArr = new double[18];
                    //string type으로 데이터 통신이 안될수도 있으니까 일단 숫자(어레이 넘버!)로 보내보자!

                    //emoArr[0] = "Neutral/Default"; valArr[0] = 5; arArr[0] = 5;
                    // emoArr[1] = "Excited"; valArr[1] = 11; arArr[1] = 20;
                    emoArr[0] = "Joy"; valArr[0] = 20; arArr[0] = 15;
                    //emoArr[3] = "Curious"; valArr[3] = 8; arArr[3] = 8;
                    //emoArr[4] = "Sleepy"; valArr[4] = 5; arArr[4] = -20;
                    //emoArr[5] = "Tired"; valArr[5] = 1; arArr[5] = -19;
                    //emoArr[6] = "Gloomy/Crying"; valArr[6] = -17; arArr[6] = -11;
                    emoArr[1] = "Sadness"; valArr[1] = -17; arArr[1] = -6;
                    //emoArr[8] = "Dizzy/Distressed"; valArr[8] = -17; arArr[8] = 6;
                    //emoArr[9] = "Frustrated"; valArr[9] = -18; arArr[9] = 14;
                    emoArr[2] = "Anger"; valArr[2] = -13; arArr[2] = 16;
                    emoArr[3] = "Fear"; valArr[3] = -15; arArr[3] = 20;
                    //emoArr[12] = "Celebrating"; valArr[12] = 20; arArr[12] = 20;
                    //emoArr[13] = "Wanting"; valArr[13] = 10; arArr[13] = 17;
                    //emoArr[14] = "Bored"; valArr[14] = -15; arArr[14] = -18;
                    emoArr[4] = "Disgust"; valArr[4] = -13; arArr[4] = 13;
                    //emoArr[16] = "Unhappy"; valArr[16] = -19; arArr[16] = -3;
                    //emoArr[17] = "Nervous/Tense"; valArr[17] = -10; arArr[17] = 15;
                    emoArr[5] = "Surprise"; valArr[5] = 0; arArr[5] = 13;
                    emoArr[6] = "Contempt"; valArr[6] = -13; arArr[6] = 6;

                    //double emotion2 = 0; 
                    double valence = 0; double arousal = 0;
                    for (int i = 0; i < a; i++)
                    {
                        if (maxEmotion == emoArr[i])
                        {
                            //emotion2 = i;
                            valence = valArr[i];
                            arousal = arArr[i];
                            break;
                        }
                    }

                    string sendStr1 = "A " + maxEmotion + " " + arousal.ToString() + " " + valence.ToString();// + "\n";
                    //string sendStr1 = maxEmotion;
                    string sendStr2 = maxEmotion + " " + maxValue.ToString();
                    //System.Console.WriteLine("\n"+sendStr1+ "\n");
                    //flag prevents repeated socket creation

                    //faceEmotion curEmo = new faceEmotion();
                    //for (int i = 0; i < a; i++)
                    //{
                    //    if (emotionArr[i] == "Joy") curEmo.Joy = indexArr[i];
                    //    else if (emotionArr[i] == "Sadness") curEmo.Sadness = indexArr[i];
                    //    else if (emotionArr[i] == "Anger") curEmo.Anger = indexArr[i];
                    //    else if (emotionArr[i] == "Fear") curEmo.Fear = indexArr[i];
                    //    else if (emotionArr[i] == "Disgust") curEmo.Disgust = indexArr[i];
                    //    else if (emotionArr[i] == "Surprise") curEmo.Surprise = indexArr[i];
                    //    else if (emotionArr[i] == "Contempt") curEmo.Contempt = indexArr[i];
                    //}
                    //curEmo.maxEmo = maxValue;

                    facialExpressions curExprs;
                    curExprs.attention = face.Expressions.Attention;
                    curExprs.browFurrow = face.Expressions.BrowFurrow;
                    curExprs.browRaise = face.Expressions.BrowRaise;
                    curExprs.cheekRaise = face.Expressions.CheekRaise;
                    curExprs.chinRaise = face.Expressions.ChinRaise;
                    curExprs.dimpler = face.Expressions.Dimpler;
                    curExprs.eyeClosure = face.Expressions.EyeClosure;
                    curExprs.eyeWiden = face.Expressions.EyeWiden;
                    curExprs.innerBrowRaise = face.Expressions.InnerBrowRaise;
                    curExprs.jawDrop = face.Expressions.JawDrop;
                    curExprs.lidTighten = face.Expressions.LidTighten;
                    curExprs.lipCornerDepressor = face.Expressions.LipCornerDepressor;
                    curExprs.lipPress = face.Expressions.LipPress;
                    curExprs.lipPucker = face.Expressions.LipPucker;
                    curExprs.lipStretch = face.Expressions.LipStretch;
                    curExprs.lipSuck = face.Expressions.LipSuck;
                    curExprs.mouthOpen = face.Expressions.MouthOpen;
                    curExprs.noseWrinkle = face.Expressions.NoseWrinkle;
                    curExprs.smile = face.Expressions.Smile;
                    curExprs.smirk = face.Expressions.Smirk;
                    curExprs.upperLipRaise = face.Expressions.UpperLipRaise;

                    string tempOut = string.Format("{0}    {1}  {2} {3}",
                        curExprs.cheekRaise, curExprs.smile, curExprs.lipSuck, curExprs.chinRaise);
                    System.Console.WriteLine(tempOut + "\n");

                    byte[] expRawdDta = Serialize(curExprs);

                    myOrientation tempOrientation;
                    tempOrientation.roll = face.Measurements.Orientation.Roll;
                    tempOrientation.pitch = face.Measurements.Orientation.Pitch;
                    tempOrientation.yaw = face.Measurements.Orientation.Yaw;

                    byte[] oriRawdata = Serialize(tempOrientation);
                    //Serialize(tempOrientation, data2send);

                    //얼굴 감정 분석 결과를 보내는 코드
                    //byte[] emoRawdata = Serialize(curEmo);

                    //byte[] data2send = new byte[expRawdDta.Length + oriRawdata.Length + 1];
                    byte[] data2send = new byte[expRawdDta.Length + oriRawdata.Length + 1];

                    data2send[0] = (byte)(data2send.Length);
                    Array.Copy(oriRawdata, 0, data2send, 1, oriRawdata.Length);
                    Array.Copy(expRawdDta, 0, data2send, (1 + oriRawdata.Length), expRawdDta.Length);
                    //Array.Copy(emoRawdata, 0, data2send, (1 + oriRawdata.Length + expRawdDta.Length), emoRawdata.Length);

                    //Hifza
                    //send data to java server through socket
                    if (myFlag == 1)
                    {
                        if (!myNetworks.myNetwork.SendData(data2send))
                        {
                            //myNetworks.myNetwork.CloseClient();
                            //this.Invalidate();
                            //frame.Dispose();
                            //Environment.Exit(0);
                            try       //try에서 에러가 날 경우, catch가 실행되면서 문구만 나타나게 되는데 이에 대한 종료가 필요한 것이 아닌가?
                            {
                                this.Close();
                                //this.Invalidate();
                                //detector.stop();
                                //frame.Dispose();

                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine("Closing.");
                                Console.WriteLine("\nMessage ---\n{0}", ex.Message);
                            }
                        }
                    }


                    //added by Hifza: to output expressions and emojis in addition to the emotions output in the code above
                    //output expressions
                    //foreach (PropertyInfo prop in typeof(Affdex.Expressions).GetProperties())
                    //{
                    //    float value = (float)prop.GetValue(face.Expressions, null);
                    //    string output = string.Format("{0}: {1}", prop.Name, value);
                    //    //System.Console.WriteLine(output);

                    //    //System.IO.File.AppendAllText(@"C:\Users\artmed\Desktop\sangwonlee\Affdex_Outputs\Affdex_Log.txt",
                    //    //DateTime.Now.ToString("hh.mm.ss.ffffff") + " " + output + " " + Environment.NewLine);

                    //    System.IO.File.AppendAllText(@"C:\Users\artmed\Documents\sangwonlee\Affdex_Outputs\Affdex_Results.txt",
                    //   DateTime.Now.ToString("hh.mm.ss.ffffff") + " " + output + " " + Environment.NewLine);
                    //    System.Console.WriteLine(output);
                    //    //Hifza
                    //    //send data to java server through socket
                    //    //myNetworks.myNetwork.SendData(output);
                    //}
                    //output emojis
                    //foreach (PropertyInfo prop in typeof(Affdex.Emojis).GetProperties())
                    //{
                    //    float value = (float)prop.GetValue(face.Emojis, null);
                    //    string output = string.Format("{0}: {1}", prop.Name, value);
                    //    //System.Console.WriteLine(output);

                    //    //System.IO.File.AppendAllText(@"C:\Users\artmed\Desktop\sangwonlee\Affdex_Outputs\Affdex_Log.txt",
                    //    //DateTime.Now.ToString("hh.mm.ss.ffffff") + " " + output + " " + Environment.NewLine);

                    //    System.IO.File.AppendAllText(@"C:\Users\artmed\Documents\sangwonlee\Affdex_Outputs\Affdex_Results.txt",
                    //   DateTime.Now.ToString("hh.mm.ss.ffffff") + " " + output + " " + Environment.NewLine);
                    //    System.Console.WriteLine(output);
                    //    //Hifza
                    //    //send data to java server through socket
                    //    //myNetworks.myNetwork.SendData(output);
                    //}
                    // System.Console.WriteLine(" ");
                    //System.IO.File.AppendAllText(@"C:\Users\artmed\Desktop\sangwonlee\Affdex_Outputs\Affdex_Log.txt", Environment.NewLine);
                    //System.IO.File.AppendAllText(@"C:\Users\artmed\Documents\sangwonlee\Affdex_Outputs\Affdex_Results.txt", Environment.NewLine);
                }
            }

            this.Invalidate();
            frame.Dispose();
        }

        //private byte[] ObjectToByteArray(myOrientation obj)
        //{
        //    //if (obj == null)
        //    //    return null;
        //    BinaryFormatter bf = new BinaryFormatter();
        //    MemoryStream ms = new MemoryStream();
        //    bf.Serialize(ms, obj);
        //    return ms.ToArray();
        //}

        //private byte[] ObjectToByteArray(object obj) 주석처리해도 작동됨을 확인
        //{
        //    //if (obj == null)
        //    //    return null;
        //    BinaryFormatter bf = new BinaryFormatter();
        //    MemoryStream ms = new MemoryStream();
        //    bf.Serialize(ms, obj);
        //    return ms.ToArray();
        //}
        private myOrientation ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            myOrientation obj = (myOrientation)binForm.Deserialize(memStream);
            return obj;
        }


        static void Serialize(myOrientation myStructure, byte[] raw)
        {
            int rawsize = Marshal.SizeOf(myStructure);
            if (raw.Length != rawsize) return;
            byte[] rawdatas = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            Array.Copy(rawdatas, raw, rawsize);
        }

        //static byte[] Serialize(myOrientation myStructure)
        //{
        //    int rawsize = Marshal.SizeOf(myStructure);
        //    byte[] rawdatas = new byte[rawsize];
        //    GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
        //    IntPtr buffer = handle.AddrOfPinnedObject();
        //    Marshal.StructureToPtr(myStructure, buffer, false);
        //    handle.Free();
        //    return rawdatas;
        //}

        static byte[] Serialize(object myStructure) //roll, pitch, yaw가 담긴 struct를 어레이형 변수에 저장하고 이에 대한 객체 주소를 할당하여 버퍼에 반환 후 저장, myNetwork와 통신하기 위한 부분
        {
            int rawsize = Marshal.SizeOf(myStructure);
            byte[] rawdatas = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            Marshal.StructureToPtr(myStructure, buffer, false);
            handle.Free();
            return rawdatas;
        }

        private void DrawResults(Graphics g, Dictionary<int, Affdex.Face> faces)
        {
            Pen whitePen = new Pen(Color.OrangeRed);
            Pen redPen = new Pen(Color.DarkRed);
            Pen bluePen = new Pen(Color.DarkBlue);
            Font aFont = new Font(FontFamily.GenericSerif, 8, FontStyle.Bold);
            float radius = 2;
            int spacing = 10;
            int left_margin = 30;

            foreach (KeyValuePair<int, Affdex.Face> pair in faces)
            {
                Affdex.Face face = pair.Value;
                foreach (Affdex.FeaturePoint fp in face.FeaturePoints)
                {
                    g.DrawCircle(whitePen, fp.X, fp.Y, radius);
                }

                Affdex.FeaturePoint tl = minPoint(face.FeaturePoints);
                Affdex.FeaturePoint br = maxPoint(face.FeaturePoints);

                int padding = (int)tl.Y;

                //g.DrawString(String.Format("ID: {0}", pair.Key), aFont, whitePen.Brush, new PointF(br.X, padding += spacing));
                //g.DrawString("APPEARANCE", aFont, bluePen.Brush, new PointF(br.X, padding += (spacing * 2)));
                //g.DrawString(face.Appearance.Gender.ToString(), aFont, whitePen.Brush, new PointF(br.X, padding += spacing));
                //g.DrawString(face.Appearance.Age.ToString(), aFont, whitePen.Brush, new PointF(br.X, padding += spacing));
                //g.DrawString(face.Appearance.Ethnicity.ToString(), aFont, whitePen.Brush, new PointF(br.X, padding += spacing));
                //g.DrawString("Glasses: " + face.Appearance.Glasses.ToString(), aFont, whitePen.Brush, new PointF(br.X, padding += spacing));

                //g.DrawString("EMOJIs", aFont, bluePen.Brush, new PointF(br.X, padding += (spacing * 2)));
                //g.DrawString("DominantEmoji: " + face.Emojis.dominantEmoji.ToString(), aFont,
                //             (face.Emojis.dominantEmoji != Affdex.Emoji.Unknown) ? whitePen.Brush : redPen.Brush,
                //             new PointF(br.X, padding += spacing));

                //foreach (String emojiName in Enum.GetNames(typeof(Affdex.Emoji)))
                //{
                //    PropertyInfo prop = face.Emojis.GetType().GetProperty(emojiName.ToLower());
                //    if (prop != null)
                //    {
                //        float value = (float)prop.GetValue(face.Emojis, null);
                //        string c = String.Format("{0}: {1:0.00}", emojiName, value);
                //        g.DrawString(c, aFont, (value > 50) ? whitePen.Brush : redPen.Brush, new PointF(br.X, padding += spacing));
                //    }
                //}

                g.DrawString("EXPRESSIONS", aFont, bluePen.Brush, new PointF(br.X, padding += (spacing * 2)));
                foreach (PropertyInfo prop in typeof(Affdex.Expressions).GetProperties())
                {
                    float value = (float)prop.GetValue(face.Expressions, null);
                    String c = String.Format("{0}: {1:0.00}", prop.Name, value);
                    g.DrawString(c, aFont, (value > 50) ? whitePen.Brush : redPen.Brush, new PointF(br.X, padding += spacing));
                }

                //g.DrawString("EMOTIONS", aFont, bluePen.Brush, new PointF(br.X, padding += (spacing * 2)));

                //foreach (PropertyInfo prop in typeof(Affdex.Emotions).GetProperties())
                //{
                //    float value = (float)prop.GetValue(face.Emotions, null);
                //    String c = String.Format("{0}: {1:0.00}", prop.Name, value);
                //    g.DrawString(c, aFont, (value > 50) ? whitePen.Brush : redPen.Brush, new PointF(br.X, padding += spacing));
                //}

            }
        }

        public void onProcessingException(Affdex.AffdexException A_0)
        {
            System.Console.WriteLine("Encountered an exception while processing {0}", A_0.ToString());
        }

        public void onProcessingFinished()
        {
            System.Console.WriteLine("Processing finished successfully");
        }

        Affdex.FeaturePoint minPoint(Affdex.FeaturePoint[] points)
        {
            Affdex.FeaturePoint ret = points[0];
            foreach (Affdex.FeaturePoint point in points)
            {
                if (point.X < ret.X) ret.X = point.X;
                if (point.Y < ret.Y) ret.Y = point.Y;
            }
            return ret;
        }

        Affdex.FeaturePoint maxPoint(Affdex.FeaturePoint[] points)
        {
            Affdex.FeaturePoint ret = points[0];
            foreach (Affdex.FeaturePoint point in points)
            {
                if (point.X > ret.X) ret.X = point.X;
                if (point.Y > ret.Y) ret.Y = point.Y;
            }
            return ret;
        }

        [HandleProcessCorruptedStateExceptions]
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                // rwLock.AcquireReaderLock(Timeout.Infinite);
                if (img != null)
                {
                    this.Width = img.Width;
                    this.Height = img.Height;
                    e.Graphics.DrawImage((Image)img, new Point(0, 0));
                }

                if (faces != null) DrawResults(e.Graphics, faces);


                e.Graphics.Flush();
            }
            catch (System.AccessViolationException exp)
            {
                System.Console.WriteLine("Encountered AccessViolationException.");
            }
        }

        private float process_last_timestamp = -1.0f;
        private float process_fps = -1.0f;

        private Bitmap img { get; set; }
        private Dictionary<int, Affdex.Face> faces { get; set; }
        private Affdex.Detector detector { get; set; }
        private ReaderWriterLock rwLock { get; set; }

        private void ProcessVideo_Load(object sender, EventArgs e)
        {

        }
    }
}

public static class GraphicsExtensions
{
    public static void DrawCircle(this Graphics g, Pen pen,
                                  float centerX, float centerY, float radius)
    {
        g.DrawEllipse(pen, centerX - radius, centerY - radius,
                      radius + radius, radius + radius);
    }
}
