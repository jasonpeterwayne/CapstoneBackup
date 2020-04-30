using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myNetworks4
{
    class myNetwork4P
    {
        static int num;
        static int Port;
        public static void Main(int portNum)
        {

            // server 소켓을 생성한다.
            Port = portNum;
            Thread t = new Thread(new ThreadStart(Service));
            t.IsBackground = true;
            t.Start();
        }

        public static void Service()
        {
            int TxData;
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // ip는 로컬이고 포트는 9999로 listen 대기한다.
                server.Bind(new IPEndPoint(IPAddress.Any, Port));
                server.Listen(20);
                Console.WriteLine("Server Start... Listen port 9999...");
                try
                {
                    while (true)
                    {
                        // 다중 접속을 허용하기 위해 Threadpool를 이용한 멀티 쓰레드 환경을 만들었다.
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            Socket client = (Socket)c;
                            try
                            {
                                // 무한 루프로 메시지를 대기한다.
                                while (true)
                                {
                                    // 처음에 데이터 길이를 받기 위한 4byte를 선언한다.
                                    var data = new byte[4];
                                    // python에서 little 엔디언으로 값이 온다. big엔디언과 little엔디언은 배열의 순서가 반대이므로 reverse한다.
                                    client.Receive(data, 4, SocketFlags.None);
                                    Array.Reverse(data);
                                    // 데이터의 길이만큼 byte 배열을 생성한다.
                                    data = new byte[BitConverter.ToInt32(data, 0)];
                                    // 데이터를 수신한다.
                                    client.Receive(data, data.Length, SocketFlags.None);
                                    // byte를 UTF8인코딩으로 string 형식으로 변환한다.
                                    var msg = Encoding.UTF8.GetString(data);
                                    if (msg == "intro")
                                    {
                                        //num = 1;
                                    }
                                    else if (msg == "extro")
                                    {
                                        //num = 2;
                                    }
                                    else if (msg == "sympathy")
                                    {
                                        //num = 3;
                                    }
                                    else if (msg == "good")
                                    {
                                        num = 1;
                                    }
                                    else if (msg == "bad")
                                    {
                                        num = 2;
                                    }
                                    TxData = num;
                                    // 데이터를 콘솔에 출력한다.
                                    Console.WriteLine(msg);
                                    // 메시지에 echo를 문자를 붙힌다.
                                    //msg = "C# server echo : " + msg;
                                    // 데이터를 UTF8인코딩으로 byte형식으로 변환한다.
                                    data = Encoding.UTF8.GetBytes(msg);
                                    // 데이터 길이를 클라이언트로 전송한다.
                                    client.Send(BitConverter.GetBytes(data.Length));
                                    // 데이터를 전송한다.
                                    client.Send(data, data.Length, SocketFlags.None);
                                }
                            }
                            catch (Exception)
                            {
                                // Exception이 발생하면 (예기치 못한 접속 종료) client socket을 닫는다.
                                client.Close();
                            }
                            // server로 client가 접속이 되면 ThreadPool에 Thread가 생성됩니다.
                        }, server.Accept());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Press any key...");
            Console.ReadLine();
        }

        public static int pythonLabel()
        {
            //Console.WriteLine(num);
            return num;
        }
    }
}
