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
            matrix = Matrix4x4.Transpose(matrix);
            var outVector = new Vector3(
                matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z + matrix.M14,
                matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z + matrix.M24,
                matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z + matrix.M34
            );

            var w = matrix.M41 * self.X + matrix.M42 * self.Y + matrix.M43 * self.Z + matrix.M44;
            if (w != 1)
            {
                outVector.X /= w;
                outVector.Y /= w;
                outVector.Z /= w;
            }
            return outVector;
        }
    }

    public static class integerBresenham
    {

        public static IEnumerable<Vector2> plotLine(Vector2 start, Vector2 end)
        {
            var components = new float[] { start.X, start.Y, end.X, end.Y };
            var NaN = components.Any(x => float.IsNaN(x));
            if (NaN)
            {
                return new Vector2[0];
            }

            var output = new List<Vector2>();
            var DX = end.X - start.X;
            var DY = end.Y - start.Y;
            var diff = 2 * DY - DX;
            var yIncrement = 1;
            if (DY < 0)
            {
                yIncrement = -1;
                DY = -DY;
            }

            output.Add(new Vector2(start.X, start.Y));

            var j = start.Y;
            for (var i = start.X; i < end.X; i++)
            {
                if (diff < 0)
                {
                    output.Add(new Vector2(i, j));
                    diff = diff + 2 * DY;
                }
                //increment y
                else
                {
                    j = j + yIncrement;
                    output.Add(new Vector2(i, j));
                    diff = diff + 2 * DY - 2 * DX;
                }
            }

            return output;

        }

        /*
                public static naiveLineDrawring(Vector2 start, Vector2 end)
                {
                    var output = new List<Vector2>();
                    var DX = end.X - start.X;
                    var DY = end.Y - start.Y;
                    var m = DX/DY;

                    for (var x = start.X; x <= end.X; x++)
                    {

                        if (x == start.X)
                        {
                            var y = x*m;
                            for(var i =0; i < m; i++){
                                output.Add(new Vector2(x,start.Y))
                            }
                        }
                        else if (x == end.X)
                        {

                        }
                        else
                        {

                        }

                    }

                }

            }
            */

        class Program
        {

            static Vector4 scaleByW(Vector4 vector)
            {
                return Vector4.Multiply(vector, (1 / vector.W));
            }

            static void Main(string[] args)
            {

                Console.WriteLine(args.Length);
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

                if (args.Contains("testnativeinterop")){
                    serialComms.serialUtils.testNativeInterop();
                }

                var width = 1024;
                var height = 768;
                //load our teapot
                var vectors = objLoader.loadVertsFromObjAtPath(new FileInfo(@"./teapot.obj"));
                var tris = objLoader.loadTrisFromObjAtPath(new FileInfo(@"./teapot.obj"));

                Enumerable.Range(-200, 400).Select(i => i / 10F).ToList().ForEach(camy =>
                     {

                         var cameraPos = new Vector3(camy, camy, -5);
                         var target = new Vector3(0, 0, 0);

                         var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
                         //var view = Matrix4x4.CreateWorld(new Vector3(0,1,-20),Vector3.UnitZ,Vector3.UnitY);
                         var proj = Matrix4x4.CreatePerspective(4, 3, 2, 10);

                         Matrix4x4 inverseView;
                         if (!Matrix4x4.Invert(view, out inverseView))
                         {
                             throw new Exception();
                         }

                         //Console.WriteLine(mvp.ToString());
                         //do the projection.
                         var projectedVectors = vectors.Select(vect =>
                                  {
                                      var viewVert = new Vector3(vect.X, vect.Y, vect.Z).ApplyMatrix(view);
                                      //Console.WriteLine(viewVert.ToString());
                                      var projVert = viewVert.ApplyMatrix(proj);
                                      //Console.WriteLine(projVert.ToString());

                                      return projVert;
                                  }).ToList();

                         //Console.WriteLine(view.ToString());
                         //Console.WriteLine(inverseView.ToString());
                         //Console.WriteLine(proj.ToString());

                         using (var image = new Image<Rgba32>(width, height))
                         {
                             int i = 0;
                             var imageCoords = projectedVectors.Select(vect =>
                             {

                                 var distance = Vector3.Distance(
                                     new Vector3(vectors[i].X, vectors[i].Y, vectors[i].Z)
                                 , cameraPos);

                                 if (vect.X >= -1 && vect.X <= 1 && vect.Y <= 1 && vect.Y >= -1)
                                 {
                                     //Console.WriteLine(vect.ToString());
                                     var scaledX = Math.Min(width - 1, (int)((vect.X + 1) * 0.5 * (float)width));
                                     var scaledY = Math.Min(height - 1, (int)((1 - (vect.Y + 1) * 0.5) * (float)height));
                                     var color = 8 / distance;
                                     //Console.WriteLine(color);
                                     image[scaledX, scaledY] = new Rgba32(color, color, color);
                                     return new Vector2(scaledX, scaledY);
                                 }
                                 i++;
                                 //these vectors are offscreen.
                                 return new Vector2(float.NaN, float.NaN);
                             }).ToList();

                             //now draw tris
                             tris.ForEach(tri =>
                             {

                                 //TODO flip x and y based on octants of x and y.

                                 var line1 = plotLine(imageCoords[tri.Item1 - 1], imageCoords[tri.Item2 - 1]);
                                 var line2 = plotLine(imageCoords[tri.Item2 - 1], imageCoords[tri.Item3 - 1]);
                                 var line3 = plotLine(imageCoords[tri.Item3 - 1], imageCoords[tri.Item1 - 1]);

                                 var allpoints = line1.Concat(line2).Concat(line3);
                                 allpoints.ToList().ForEach(pt =>
                                 {
                                     image[(int)pt.X, (int)pt.Y] = new Rgba32(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
                                 });


                             });


                             var stream = File.Create($"./images/testoutput{(camy + 19.90).ToString("N2")}.png");
                             image.SaveAsPng(stream);
                             stream.Dispose();
                         }

                     });
            }

            private static void testsSerial()
            {
                //update the data files.
                testFixedPoint();
                var port = serialComms.serialUtils.openArduinoSerialConnection();
                //read the files back and send them out:
                var vertComponents = File.ReadLines(@"./testVectors.txt");
                vertComponents.ToList().ForEach(line => serialComms.serialUtils.sendData(port, line.ToList().Select(x => x == 1 ? true : false)));

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
                var cameraPos = new Vector3(camy, camy, -5);
                var target = new Vector3(0, 0, 0);

                var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
                var proj = Matrix4x4.CreatePerspective(4, 3, 2, 10);
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

        //load our obj data for our test model
        static class objLoader
        {
            public static List<Vector4> loadVertsFromObjAtPath(FileInfo path)
            {
                var text = File.ReadAllText(path.FullName);
                var lines = text.Split(Environment.NewLine);
                var output = new List<Vector4>();
                foreach (var line in lines)
                {
                    var split = line.Split(" ");
                    if (split.Length < 4 || split.First() == "f")
                    {
                        continue;
                    }
                    var x = float.Parse(split[1]);
                    var y = float.Parse(split[2]);
                    var z = float.Parse(split[3]);

                    output.Add(new Vector4(x, y, z, 1));

                }
                return output;
            }

            public static List<Tuple<int, int, int>> loadTrisFromObjAtPath(FileInfo path)
            {
                var text = File.ReadAllText(path.FullName);
                var lines = text.Split(Environment.NewLine);
                var output = new List<Tuple<int, int, int>>();
                foreach (var line in lines)
                {
                    var split = line.Split(" ");
                    if (split.Length < 4 || split.First() == "v")
                    {
                        continue;
                    }
                    var a = int.Parse(split[1]);
                    var b = int.Parse(split[2]);
                    var c = int.Parse(split[3]);

                    output.Add(Tuple.Create(a, b, c));

                }
                return output;
            }

        }
    }
}
