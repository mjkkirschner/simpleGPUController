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
using SerialCommunication;
using simpleGPUTests;

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

    public static class Vector3Extensions
    {
        public static Vector3 ToVector3(this Vector2 vec)
        {
            return new Vector3(vec.X, vec.Y, 1.0f);
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

            if (args.Contains("testnativeinterop"))
            {
                SerialCommunication.serialUtils.testNativeInterop();
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

                var xs = projectedVectors.Select(x => x.X);
                var ys = projectedVectors.Select(x => x.Y);
                Console.WriteLine($"min x:{xs.Min()}, max x:{xs.Max()}");
                Console.WriteLine($"min y:{ys.Min()}, max y:{ys.Max()}");

                var imageCoords = drawVerts(image, Rgba32.White, projectedVectors).ToList();
                var depthBuffer = Enumerable.Repeat(1.0, width * height).ToArray();
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
                   //     image[(int)pt.X, (int)pt.Y] = new Rgba32(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
                    });

                });


                tris.ForEach(tri =>
                {
                    Console.WriteLine(tri);
                    var A = imageCoords[tri.Item1 - 1];
                    var B = imageCoords[tri.Item2 - 1];
                    var C = imageCoords[tri.Item3 - 1];

                    var AProjectedSpace = projectedVectors[tri.Item1 - 1];
                    var BProjectedSpace = projectedVectors[tri.Item2 - 1];
                    var CProjectedSpace = projectedVectors[tri.Item3 - 1];

                    var verts = new List<Vector2>() { A, B, C };
                    //calculate bounding box and iterate all pixels within.
                    var minx = verts.Select(x => x.X).Min();
                    var miny = verts.Select(x => x.Y).Min();
                    var maxx = verts.Select(x => x.X).Max();
                    var maxy = verts.Select(x => x.Y).Max();

                    Console.WriteLine($"{minx},{miny}    {maxx},{maxy}");

                    Enumerable.Range((int)minx, (int)(maxx - minx)).ToList().ForEach(x =>
                    {
                        Console.WriteLine(x);

                        Enumerable.Range((int)miny, (int)(maxy - miny)).ToList().ForEach(y =>
                        {
                            Console.WriteLine(y);
                            var pixelIsInsideTriangle = Program.pixelIsInsideTriangle(x, y, tri, imageCoords);
                            var bary = baryCoordinates(x, y, tri, imageCoords);
                            var z = bary.X * AProjectedSpace.Z + bary.Y * BProjectedSpace.Z + bary.Z * CProjectedSpace.Z;
                            
                            var AB = Vector3.Subtract(AProjectedSpace,BProjectedSpace);
                            var AC = Vector3.Subtract(AProjectedSpace,CProjectedSpace);
                            var ABXAC = Vector3.Normalize(Vector3.Cross(AB,AC));


                            if (pixelIsInsideTriangle)
                            {
                                if (z < depthBuffer[(x * width + y)])
                                {
                                    
                                    var diffuseCoef = (float)( Math.Max(Vector3.Dot(ABXAC,new Vector3(1.0f,0f,0f)),0));

                                    image[(int)x, (int)y] = new Rgba32(diffuseCoef,diffuseCoef,diffuseCoef);
                                    depthBuffer[(x * width + y)] = z;
                                }

                            }
                        });
                    });

                });

                var stream = File.Create($"./images/testoutput.png");
                image.SaveAsPng(stream);
                stream.Dispose();
            }

        }

        private static Vector3 baryCoordinates(int x, int y, Tuple<int, int, int> triangle, List<Vector2> vectors)
        {

            var p = new Vector2(x, y);
            var a = vectors[triangle.Item1 - 1];
            var b = vectors[triangle.Item2 - 1];
            var c = vectors[triangle.Item3 - 1];

            var ab = Vector2.Subtract(a, b).ToVector3();
            var bc = Vector2.Subtract(b, c).ToVector3();
            var bp = Vector2.Subtract(b, p).ToVector3();
            var cp = Vector2.Subtract(c, p).ToVector3();
            var ac = Vector2.Subtract(a, c).ToVector3();


            var areaABC = Vector3.Cross(ab, bc).Length() * .5f;
            var areaABP = Vector3.Cross(ab, bp).Length() * .5f;
            var areaACP = Vector3.Cross(ac, cp).Length() * .5f;
            var areaBCP = Vector3.Cross(bc, cp).Length() * .5f;

            var bary = new Vector3();
            bary.X = areaBCP / areaABC; // alpha
            bary.Y = areaACP / areaABC; // beta
            bary.Z = 1.0f - bary.X - bary.Y; // gamma

            return bary;
        }

        //TODO make tri class and move to new file - add this function as method there.
        private static bool pixelIsInsideTriangle(int x, int y, Tuple<int, int, int> triangle, List<Vector2> vectors)
        {

            var barycenter = baryCoordinates(x, y, triangle, vectors);
            //only in the triangle if coefs are all positive.
            if (barycenter.X >= 0.0 && barycenter.Y >= 0.0 && barycenter.Z >= 0.0)
            {
                return true;
            }
            return false;
        }


        private static IEnumerable<Vector2> drawVerts(Image<Rgba32> image, Rgba32 color, IEnumerable<Vector3> verts)
        {
            int i = 0;
            return verts.Select(vect =>
            {
                if (vect.X >= -1 && vect.X <= 1 && vect.Y <= 1 && vect.Y >= -1)
                {
                    //Console.WriteLine(vect.ToString());
                    //var scaledX = Math.Min(image.Width - 1, (int)((vect.X + 1) * 0.5 * (float)image.Width));
                    //var scaledY = Math.Min(image.Height - 1, (int)((1 - (vect.Y + 1) * 0.5) * (float)image.Height));

                    int scaledX = (int)((vect.X * (.5 * (float)image.Width)) + (.5 * (float)image.Width));
                    //for some reason to make y scale correctly, we need to invert the values during scaling.
                    int scaledY = (int)((vect.Y * -(.5 * (float)image.Height)) + (.5 * (float)image.Height));

                    //Console.WriteLine(color);
                    image[scaledX, scaledY] = color;
                    return new Vector2(scaledX, scaledY);
                }
                i++;
                //these vectors are offscreen.
                return new Vector2(float.NaN, float.NaN);
            });
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

            File.WriteAllLines("../testVectors.txt", vectorxyzStringComponents);

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
            File.WriteAllText("../testMVP.txt", String.Join("", matrixFixedBits));
        }
    }


}
