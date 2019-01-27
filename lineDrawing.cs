using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
    }