using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace fixedPointMath
{

    public static class fixedPointMath
    {

        public static string ToBitString(this BitArray bits)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < bits.Count; i++)
            {
                char c = bits[i] ? '1' : '0';
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static BitArray floatToFixedPoint(float value, int n, int q)
        {
            Console.WriteLine($"value:{value}");
            var sign = Math.Sign(value);
            value = Math.Abs(value);
            var shifted = (int)(value * Math.Pow(2, q));
            Console.WriteLine($"shifted: {shifted}");
            var binaryString = Convert.ToString(shifted, 2).PadLeft(n, '0');
            Console.WriteLine($"firstBinary: {binaryString}");
            //lets cut this string down to n bits total
            var nSizeString = binaryString.ToCharArray(binaryString.Length - (n), n);
            //set the sign bit.
            if (sign < 0)
            {
                nSizeString[0] = '1';
            }
            else
            {
                nSizeString[0] = '0';
            }


            Console.WriteLine($"nlength string: {String.Join("", nSizeString)}");
            var bitarray = new BitArray(nSizeString.ToList().Select(x => x == '1').ToArray());
            Console.WriteLine($"bitArray: {bitarray.ToBitString()}");
            return bitarray;
        }

        ///
        ///<summary>This method converts a bit string to a byte array.</summary>
        ///  
        public static byte[] GetBytes(string bitString)
        {
            var byteValue = Enumerable.Range(0, bitString.Length / 8).
                Select(pos => Convert.ToByte(
                    bitString.Substring(pos * 8, 8),
                    2)
                ).ToArray();

            Debug.WriteLine($"string {bitString} becomes byte { String.Join(",", byteValue.Select(x => Convert.ToString(x,2).PadLeft(8, '0')))}");
            return byteValue;
        }
    }

}