using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;



namespace myNetworks
{
    class myNetwork
    {
        static TcpListener listener;
        // currently we are accepting only one client
        //const int LIMIT = 5; //5 concurrent clients


        static TcpClient client;
        static Stream s;
        static StreamReader sr;
        static StreamWriter sw;
        static BinaryReader bsr;
        static BinaryWriter bsw;

        public static void StartServer(int portNum)
        {
            // Socket Server
            //listener = new TcpListener(54321);
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), portNum);
            listener.Start();
            //Console.WriteLine("Server mounted, listening to port 55555");
            Thread t = new Thread(new ThreadStart(Service));
            t.IsBackground = true;
            t.Start();
        }

        public static void StartClient(string hostIP, int portNum)
        {
            client = new TcpClient(hostIP, portNum);
            if (!client.Connected) return;
            s = client.GetStream();
            sr = new StreamReader(s);
            sw = new StreamWriter(s);
            bsr = new BinaryReader(s);
            bsw = new BinaryWriter(s);
            sw.AutoFlush = true;
        }

        public static void CloseClient()
        {
            if (!client.Connected) return;
            sr.Close();
            sw.Close();
            bsr.Close();
            bsw.Close();
            s.Close();
            client.Close();
        }

        public static bool SendData(string data)
        {
            //if (!client.Connected)
            //{
            //    System.Console.WriteLine("Server connection interrupted.");
            //return false;
            //}
            try
            {
                sw.WriteLine(data); // no need to add a code for flush because we set the autoflush parameter by true
                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Server connection interrupted.");
                return false;
            }
            //return true;
        }

        public static void SendData(char[] buffer) // note that char is 2Byte
        {
            if (!client.Connected) return;
            sw.Write(buffer);
            sr.Read();
        }

        public static bool SendData(byte[] buffer) // for 1Byte array sending
        {
            if (!client.Connected) return false;

            try
            {
                bsw.Write(buffer);
                bsw.Flush();
                //bsr.Read();
                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Server connection interrupted.");
                return false;
            }
        }

        public static void Service()
        {
            while (true)
            {
                Socket soc = listener.AcceptSocket();
                //soc.SetSocketOption(SocketOptionLevel.Socket,
                //        SocketOptionName.ReceiveTimeout,10000);
                //Console.WriteLine("Connected: {0}", soc.RemoteEndPoint);
                try
                {
                    Stream ss = new NetworkStream(soc);
                    BinaryReader bssr = new BinaryReader(ss);
                    BinaryWriter bssw = new BinaryWriter(ss);
                    while (true)
                    {
                        //string name = sr.ReadLine();
                        byte[] buffer = new byte[512];
                        //buffer = bssr.ReadBytes(512);
                        int iReadLength = bssr.Read(buffer, 0, 512);

                        if (iReadLength > 0)
                        {

                        }
                        bssw.Write(buffer, 0, iReadLength);
                        bssw.Flush();
                        //sw.WriteLine(name);
                        //Console.WriteLine("Received: " + name);
                        //if (name == "" || name == null) break;
                    }
                    ss.Close();
                    break;
                }
                catch (Exception e)
                {
#if LOG
                        Console.WriteLine(e.Message);
#endif
                }
#if LOG
                        Console.WriteLine("Disconnected: {0}", soc.RemoteEndPoint);
#endif
                soc.Close();
            }
        }
    }
}
