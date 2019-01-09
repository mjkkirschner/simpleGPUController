
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

namespace serialComms
{

    public static class serialUtils
    {
        [DllImport("libarduinoComsTest1")]
        private static extern int main(int argc, Char[] args);


        public static SerialPort openArduinoSerialConnection()
        {
            var portName = string.Empty;
            //find a portname that contains "Arduino";
            foreach (var s in SerialPort.GetPortNames())
            {
                Console.WriteLine(" {0}", s);
                if (s.ToLower().Contains("arduino"))
                {
                    portName = s;
                }
            }
            if (string.IsNullOrEmpty(portName))
            {
                throw new Exception("could not find ardunio port");
            }


            var serial = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            serial.Open();
            serial.DataReceived += (o, e) =>
            {
                Console.WriteLine($"received some data from arduino:{serial.ReadChar()}");
            };

            return serial;
        }

        public static void sendData(SerialPort serial, IEnumerable<bool> data)
        {
            var asString = String.Join("", data.Select(x => x.ToString()));
            serial.WriteLine(asString);
        }

        public static void openArduinoSerialPortDirect()
        {
            var files = new System.IO.DirectoryInfo("/dev/").GetFiles().Where(x => x.FullName.ToLower().Contains("usbmodem14501")).ToList();
            Console.WriteLine(files.Count);
            Console.WriteLine(String.Join(",", files.Select(x => x.FullName)));
        }

        public static void testNativeInterop()
        {
            main(0, null);
        }

        //

    }





}