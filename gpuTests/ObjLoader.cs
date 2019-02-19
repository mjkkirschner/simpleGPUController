//load our obj data for our test model
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

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