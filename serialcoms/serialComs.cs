using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using fixedPointMath;

namespace SerialCommunication
{
    public static class serialUtils
    {
        [DllImport("libarduinoComsTest1")]
        public static extern int openSerialPort(string serialPortName, UInt32 baudRate);

        // TODO this surely leaks memory.
        // we need to free the array at the returned pointer.
        [DllImport("libarduinoComsTest1")]
        public static extern System.IntPtr readData(int fd, int numBytesToRead);

        [DllImport("libarduinoComsTest1")]
        public static extern int sendData(int fd, UInt32[] data, int numBytesToWrite);

        [DllImport("libarduinoComsTest1")]
        public static extern int sendDataAsBytes(int fd, byte[] data, int numBytesToWrite);


        public static List<List<T>> Split<T>(IEnumerable<T> source, int subListLength)
        {
            return source.
               Select((x, i) => new { Index = i, Value = x })
               .GroupBy(x => x.Index / subListLength)
               .Select(x => x.Select(v => v.Value).ToList())
               .ToList();
        }

        public static void testNativeInterop()
        {

            //split the vertData into chunks of 200 verts. (600 lines)
            var verts = Split(File.ReadLines(@"./testVectors.txt"), 1200);
            //convert each bitString to a byte array, and then concat all byte arrays into one for each chunk. (200 verts)
            //this should be 200 * 32 bits * 3 components (19200 bits) or 2400 bytes
            var byteArrays = verts.Select(x => x.Select(y => fixedPointMath.fixedPointMath.GetBytes(y)).Aggregate((list1, list2) => list1.Concat(list2).ToArray())).ToList();

            //Console.WriteLine($"byteArray count {byteArrays.Count()}");
            //Console.WriteLine($"byteArray size {byteArrays.ElementAt(0).Length}");

            var fd = openSerialPort("/dev/cu.usbmodem33533101", 230400);
            //blank the screen before sending the vertex data commands
            sendDataAsBytes(fd, new byte[1] { 0b00000011 }, 1);
            System.Threading.Thread.Sleep(1000);
            sendDataAsBytes(fd, new byte[1] { 0b00000000 }, 1);
            var bytesToRead = 128;
            IntPtr ptr;
            var dataResult = new byte[bytesToRead];

            var vertCount = 0;
           // var watch = System.Diagnostics.Stopwatch.StartNew();


            while (vertCount < byteArrays.Count())
            {

                //TODO 
                /* FIONREAD
                 int nread;
                ioctl(Serial, FIONREAD, &nread);
                if (nread > 0) {
                    ...
                }
                  */

                ptr = readData(fd, bytesToRead);
                Marshal.Copy(ptr, dataResult, 0, bytesToRead);
                var stringData = System.Text.Encoding.Default.GetString(dataResult).
                    Split(new string [1] {Environment.NewLine},StringSplitOptions.None).First();

                //Console.WriteLine($"from c and serial port: {String.Join(" , ", stringData)}");
                if (stringData.Contains("entering vertex data mode"))
                {

                    sendDataAsBytes(fd, byteArrays.ElementAt(vertCount), 4800);
                    vertCount = vertCount + 1;
                }

                if (stringData.Contains("entering command mode."))
                {
                   // Debug.WriteLine($"about to send command 0");
                    sendData(fd, new uint[1] { 0 }, 1);
                }
            }
            //clear the incoming stream
            ptr = readData(fd, bytesToRead);
           // Console.WriteLine("clearing the input buffer");

           // watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
           // Console.WriteLine(elapsedMs);
        }

    }
}