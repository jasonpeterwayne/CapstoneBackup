using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ROBOTIS;

namespace FaceController
{
    class zigbeeProgram
    {
        // Defulat setting
        public const int DEFAULT_PORTNUM = 3; // COM3
        public const int TIMEOUT_TIME = 1000; // msec
        static int emotion = 0;
        static int TxData, RxData;
        static int i;
        static int labelNum;
        //public static void zigbeeMain(int num)
        public static void zigbeeMain(int label)
        {
            labelNum = label;
            Console.WriteLine("Label" + labelNum);

            //if (labelNum == 1)
            //{
            //    Form2 frm = new Form2();
            //    frm.Show();
            //}
            //int TxData, RxData;
            //int i;
            //Form2 frm = new Form2();
            //frm.Show();
            //Form2 frm = new Form2();
            //frm.Show();

            //if (zigbee.zgb_initialize(DEFAULT_PORTNUM) == 0)
            //{
            //    System.Console.WriteLine("Succeed to open Zig2Serial!");
            //}

            //Open device
            try
            {
                if (zigbee.zgb_initialize(DEFAULT_PORTNUM) != 0)
                {
                    System.Console.WriteLine("Succeed to open Zig2Serial!");
                    
                }
                //else
                //{
                //    int ik = 0;
                //}
            }
            catch (Exception e)
            {
                //Form2 frm = new Form2();
                //frm.Show();
                //Console.WriteLine("Succeed to open Zig2Serial!");
                System.Console.WriteLine("Failed to open Zig2Serial!");
                System.Console.WriteLine("Press any key to terminate...");
                //System.Console.ReadKey(true);
                //Form2 frm = new Form2();
                //frm.Show();
                //return;
            }

            Thread t = new Thread(new ThreadStart(Service));
            t.IsBackground = true;
            t.Start(); 
        }

        public static void Service()
        {
            while (true)
            {
                Console.WriteLine("Press any key to continue!(press ESC to quit)");
                //if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                //break;

                // Wait user's input
                //Console.Write("Input number to transmit: ");
                //TxData = int.Parse(Console.ReadLine());
                //if (labelNum == 1) break;

                TxData = labelNum;// num;
                Console.WriteLine("input :" + TxData);

                // Transmit data
                if (zigbee.zgb_tx_data(TxData) == 0)
                    Console.WriteLine("Failed to transmit");


                for (i = 0; i < TIMEOUT_TIME; i++)
                {
                    // Verify data recieved
                    if (zigbee.zgb_rx_check() == 1)
                    {
                        // Get data verified
                        RxData = zigbee.zgb_rx_data();
                        Console.WriteLine("1Recieved: {0:d}", RxData);
                        break;
                    }

                    if (zigbee.zgb_rx_check() == 2)
                    {
                        // Get data verified
                        RxData = zigbee.zgb_rx_data();
                        Console.WriteLine("1Recieved: {0:d}", RxData);
                        break;
                    }


                    Thread.Sleep(1);
                }

                if (i == TIMEOUT_TIME)
                    Console.WriteLine("Timeout: Failed to recieve");
            }

            // Close device
            zigbee.zgb_terminate();
            Console.WriteLine("Press any key to terminate...");
            //Console.ReadKey(true);
        }
    }
}
