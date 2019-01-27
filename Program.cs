using System;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using fixedPointMath;

namespace simpleGPUTests
{

    static class extensionMethods
    {
        public static Vector3 ApplyMatrix(this Vector3 self, Matrix4x4 matrix)
        {
            
            var w = matrix.M41 * self.X + matrix.M42 * self.Y + matrix.M43 * self.Z + matrix.M44;

            //matrix = Matrix4x4.Transpose(matrix);
            var outVector = new Vector3(
                matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z + matrix.M14,
                matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z + matrix.M24,
                matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z + matrix.M34
            );

            if (w != 1)
            {
                outVector.X /= w;
                outVector.Y /= w;
                outVector.Z /= w;
            }
            return outVector;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine($"args length: {args.Length}");
            if (args.Contains("testfixedpoint"))
            {
                testFixedPoint();
                return;
            }

            if (args.Contains("testmatrixorder"))
            {
                testMatrixMultiplyOrder();
                return;
            }

            if (args.Contains("testserial"))
            {
                testsSerial();
                return;
            }

            if (args.Contains("testnativeinterop"))
            {
                serialComms.serialUtils.testNativeInterop();
                return;
            }

            var width = 1024;
            var height = 768;
            //load our teapot
            var vectors = objLoader.loadVertsFromObjAtPath(new FileInfo(@"./teapot.obj"));
            var tris = objLoader.loadTrisFromObjAtPath(new FileInfo(@"./teapot.obj"));

            var camy = 5;
            var cameraPos = new Vector3(camy, camy, 5);
            var target = new Vector3(0, 0, 0);

            var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
            //var view = Matrix4x4.CreateWorld(new Vector3(0,1,-20),Vector3.UnitZ,Vector3.UnitY);
            var proj = Matrix4x4.CreatePerspective(4, 3, 2, 10);
            
            //do the projection.
            var MVP = Matrix4x4.Multiply(view, proj);
            MVP = Matrix4x4.Transpose(MVP);
            var projectedVectors = vectors.Select(vect =>
                       {
                           return new Vector3(vect.X, vect.Y, vect.Z).ApplyMatrix(MVP);
                       }).ToList();


            using (var image = new Image<Rgba32>(width, height))
            {

                var imageCoords = drawVerts(image, Rgba32.White, projectedVectors).ToList();
                //now draw tris
                tris.ForEach(tri =>
                {

                    //TODO flip x and y based on octants of x and y.
                    var line1 = integerBresenham.plotLine(imageCoords[tri.Item1 - 1], imageCoords[tri.Item2 - 1]);
                    var line2 = integerBresenham.plotLine(imageCoords[tri.Item2 - 1], imageCoords[tri.Item3 - 1]);
                    var line3 = integerBresenham.plotLine(imageCoords[tri.Item3 - 1], imageCoords[tri.Item1 - 1]);

                    var allpoints = line1.Concat(line2).Concat(line3);
                    allpoints.ToList().ForEach(pt =>
                    {
                        image[(int)pt.X, (int)pt.Y] = new Rgba32(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
                    });

                });


                var stream = File.Create($"./images/testoutput.png");
                image.SaveAsPng(stream);
                stream.Dispose();
            }

        }


        private static IEnumerable<Vector2> drawVerts(Image<Rgba32> image, Rgba32 color, IEnumerable<Vector3> verts)
        {
            int i = 0;
            return verts.Select(vect =>
            {
                if (vect.X >= -1 && vect.X <= 1 && vect.Y <= 1 && vect.Y >= -1)
                {
                    //Console.WriteLine(vect.ToString());
                    var scaledX = Math.Min(image.Width - 1, (int)((vect.X + 1) * 0.5 * (float)image.Width));
                    var scaledY = Math.Min(image.Height - 1, (int)((1 - (vect.Y + 1) * 0.5) * (float)image.Height));
                    //Console.WriteLine(color);
                    image[scaledX, scaledY] = color;
                    return new Vector2(scaledX, scaledY);
                }
                i++;
                //these vectors are offscreen.
                return new Vector2(float.NaN, float.NaN);
            });
        }

        private static void testsSerial()
        {
            //update the data files.
            testFixedPoint();
            //var port = serialComms.serialUtils.openArduinoSerialConnection();
            //read the files back and send them out:
            var vertComponents = File.ReadLines(@"./testVectors.txt");
            //vertComponents.ToList().ForEach(line => serialComms.serialUtils.sendData(port, line.ToList().Select(x => x == 1 ? true : false)));

            serialComms.serialUtils.openArduinoSerialPortDirect();

        }

        private static void testMatrixMultiplyOrder()
        {
            var width = 640;
            var height = 480;


            //first lets generate projected verts by multiplying them against multiple matricies:

            //load our teapot
            var vectors = objLoader.loadVertsFromObjAtPath(new FileInfo(@"./teapot.obj"));
            var tris = objLoader.loadTrisFromObjAtPath(new FileInfo(@"./teapot.obj"));


            var cameraPos = new Vector3(5, 5, -5);
            var target = new Vector3(0, 0, 0);

            var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
            var proj = Matrix4x4.CreatePerspective(4, 3, 2, 10);

            var projectedVectors1 = vectors.Select(vect =>
         {
             var viewVert = new Vector3(vect.X, vect.Y, vect.Z).ApplyMatrix(view);
             var projVert = viewVert.ApplyMatrix(proj);
             return projVert;
         }).ToList();

            //then lets project verts using a single matrix which has been pre multipled.
            var MVP = Matrix4x4.Multiply(view, proj);
            var projectedVectors2 = vectors.Select(vect =>
                       {
                           return new Vector3(vect.X, vect.Y, vect.Z).ApplyMatrix(MVP);
                       }).ToList();
            //now compare every vector to make sure projection is the same.

            var equalMask = projectedVectors1.Select((x, i) =>
            {
                var result = x.ToString("F2") == projectedVectors2[i].ToString("F2");
                if (result == false)
                {
                    Console.WriteLine($"index:{i} originalVector{x}, new vector:{projectedVectors2[i]}");

                }
                return result;
            }
            ).ToList();
            if (equalMask.Any(x => x == false))
            {
                throw new Exception("projected vectors were not the same");
            }
            Console.WriteLine("vectors were the same");

            projectedVectors2.Select((vect, i) =>
            {
                //now lets generate a report about the resulting pixel postions so we can use this as a test case in hardware:
                var scaledX = Math.Min(width - 1, (int)((vect.X + 1) * 0.5 * (float)width));
                var scaledY = Math.Min(height - 1, (int)((1 - (vect.Y + 1) * 0.5) * (float)height));
                Console.WriteLine($"index:{i} originalVert:{vectors[i]} projectedTo{vect}, pixel coords:{scaledX},{scaledY}");
                return 0;
            }).ToList();
        }

        private static void testFixedPoint()
        {
            var Q = 16;
            var N = 32;

            var vectors = objLoader.loadVertsFromObjAtPath(new FileInfo(@"./teapot.obj"));
            var vectorxyzStringComponents = vectors.Select(x =>
             {
                 var xstring = fixedPointMath.fixedPointMath.floatToFixedPoint(x.X, N, Q);
                 var ystring = fixedPointMath.fixedPointMath.floatToFixedPoint(x.Y, N, Q);
                 var zstring = fixedPointMath.fixedPointMath.floatToFixedPoint(x.Z, N, Q);
                 var wstring = fixedPointMath.fixedPointMath.floatToFixedPoint(x.W, N, Q);


                 Console.WriteLine($"vector {x} becomes {(xstring.ToBitString())} {(ystring.ToBitString())} {(zstring.ToBitString())} {(wstring.ToBitString())} ");
                 return $"{(xstring.ToBitString())}\n{(ystring.ToBitString())}\n{(zstring.ToBitString())}";
             });

            File.WriteAllLines("./testVectors.txt", vectorxyzStringComponents);

            var camy = 5;
            var cameraPos = new Vector3(camy, camy, 5);
            var target = new Vector3(0, 0, 0);

            var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
            var proj = Matrix4x4.CreatePerspective(4.0f, 3.0f, 2.0f, 10.0f);
            var MVP = Matrix4x4.Multiply(view, proj);

            Console.WriteLine(MVP);
            //TODO A HAIL MARY TEST:
            MVP = Matrix4x4.Transpose(MVP);
            //now do the same for the MVP matrix.
            var matrixVectors = new float[16] { MVP.M11, MVP.M12, MVP.M13, MVP.M14,
                                                    MVP.M21, MVP.M22, MVP.M23, MVP.M24,
                                                    MVP.M31, MVP.M32, MVP.M33, MVP.M34,
                                                    MVP.M41, MVP.M42, MVP.M43, MVP.M44};
            var matrixFixedBits = matrixVectors.Select(x =>
            {
                var bits = fixedPointMath.fixedPointMath.floatToFixedPoint(x, N, Q);
                Console.WriteLine($"matrixEntry {x} becomes {bits.ToBitString()}");
                return $"{(bits.ToBitString())}";
            });
            File.WriteAllText("./testMVP.txt", String.Join("", matrixFixedBits));
        }
    }


}
