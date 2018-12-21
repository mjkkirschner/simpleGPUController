using System;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
                    j = j + 1;
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
                var width = 640;
                var height = 480;
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
