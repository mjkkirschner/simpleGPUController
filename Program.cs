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

    class Program
    {

        static Vector4 scaleByW (Vector4 vector ){
            return Vector4.Multiply(vector,(1/vector.W));
        }

        static void Main(string[] args)
        {
            var width = 640;
            var height = 480;
            //load our teapot
            var vectors = objLoader.loadVertsFromObjAtPath(new FileInfo(@"./teapot.obj"));

            var cameraPos = new Vector3(5, 2, -5);
            var target = new Vector3(5, 0, 10);

            //var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
            var view = Matrix4x4.CreateWorld(new Vector3(3,0,-5),new Vector3(0,0,1),Vector3.UnitY);
            var proj = Matrix4x4.CreatePerspective(4, 3, 1, 500);

            Matrix4x4 inverseView;
            if (!Matrix4x4.Invert(view, out inverseView))
            {
                throw new Exception();
            }

            //Console.WriteLine(mvp.ToString());
            //do the projection.
            var projectedVectors = vectors.SelectMany(vect =>
            {
                var inter = Vector4.Transform(vect,(inverseView));
                var interScal = scaleByW(inter);
                var final = Vector4.Transform(interScal,(proj));
                var scaledfinal = scaleByW(final);

                Console.WriteLine("was" + vect.ToString());
                Console.WriteLine("inter is " + inter.ToString());
                Console.WriteLine("scaledinter is " + interScal.ToString());
                Console.WriteLine("final is " + final.ToString());
                Console.WriteLine("scaledfinal is " + scaledfinal.ToString());
                return new List<Vector4>() { scaledfinal };
            }).ToList();

            Console.WriteLine(view.ToString());
            Console.WriteLine(inverseView.ToString());
            Console.WriteLine(proj.ToString());

            using (var image = new Image<Rgba32>(width, height))
            {
                projectedVectors.ForEach(vect =>
                {
                    if (vect.X >= -1 && vect.X <= 1 && vect.Y <= 1 && vect.Y >= -1)
                    {
                        //Console.WriteLine(vect.ToString());
                        var scaledX = Math.Min(width - 1, (int)((vect.X + 1) * 0.5 * (float)width));
                        var scaledY = Math.Min(height - 1, (int)((1 - (vect.Y + 1) * 0.5) * (float)height));
                        image[scaledX, scaledY] = Rgba32.White;
                    }

                });

                var stream = File.Create(@"./testoutput.png");
                image.SaveAsPng(stream);
                stream.Dispose();
            }


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
                if (split.Length < 4)
                {
                    continue;
                }
                var x = float.Parse(split[1]);
                var y = float.Parse(split[2]);
                var z = float.Parse(split[3]);

                output.Add(new Vector4(x, y, z, 1.0f));

            }
            return output;
        }
    }


}
