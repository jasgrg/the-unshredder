using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Drawing;

namespace imgUnshred
{
   class Program
   {
       private static IEvaluator evaluator;

      static void Main(string[] args)
      {
         if (args.Count() != 1)
         {
            Console.WriteLine("Usage: imgUnshred <image_file_name>");
            return;
         }
         evaluator = new EudlideanDistanceLab ();

         var file_name = args[0];
         if (!System.IO.File.Exists(file_name))
         {
            Console.WriteLine(file_name + " does not exist.");
            return;
         }

         var bmp = new Bitmap(file_name);

         var numShreds = bmp.Width / 32;

         var segments = GetSegments(bmp, numShreds, 32);
          BestMatchWins(segments);
      }

       private static void BestMatchWins(List<Bitmap> shreds)
       {
           while (shreds.Count > 1)
           {
               var currentBestScore = -1d;
               var bestLeft = -1;
               var bestRight = -1;
               for (int i = 0; i < shreds.Count; i++)
               {
                   for (int j = 0; j < shreds.Count; j++)
                   {
                       if (i == j) continue;
                       var score = Evaluate(shreds[i], shreds[j]);
                       if (score > currentBestScore)
                       {
                           currentBestScore = score;
                           bestLeft = i;
                           bestRight = j;
                       }
                   }
               }
               CombineShreds(shreds, bestLeft, bestRight);
           }

           shreds[0].Save(@"C:\temp\unshredded.png", ImageFormat.Png);
       }

      private static double Evaluate(Bitmap left, Bitmap right)
      {
          double score = 0;
          for (int i = 0; i <left.Height; i++)
          {
              Color pixelLeft = left.GetPixel(left.Width - 1, i);
              Color pixelRight = right.GetPixel(0, i);
              score += evaluator.Evaluate(pixelLeft, pixelRight); 
          }
          return score ;
      }

       private static List<Bitmap> GetSegments(Bitmap bmp, int segments, int segmentWidth)
      {
          var lst = new List<Bitmap>();
          for (int curSegment = 0; curSegment < segments; curSegment++)
          {
              int left = curSegment*segmentWidth;
              int top = 0;
              int width = segmentWidth;
              int height = bmp.Height;

              Rectangle r = new Rectangle(left, top, width, height);
              Bitmap b = bmp.Clone(r, bmp.PixelFormat);
              lst.Add(b);
          }
          return lst;

      }

       private static void CombineShreds(List<Bitmap> list, int left, int right )
       {
            Bitmap combinedBMP = new Bitmap(list[left].Width + list[right].Width, list[left].Height);
            Graphics g = Graphics.FromImage(combinedBMP);
            g.DrawImage(list[left], new Rectangle(0, 0, list[left].Width, list[left].Height), 0, 0, list[left].Width, list[left].Height, GraphicsUnit.Pixel);
            g.DrawImage(list[right], new Rectangle(list[left].Width, 0, list[right].Width, list[right].Height), 0, 0, list[right].Width, list[right].Height, GraphicsUnit.Pixel);
            list[left] = combinedBMP;
            list.RemoveAt(right);
       }

 
       private struct LAB
       {
           public double L;
           public double a;
           public double b;
       }
       private struct XYZ
       {
           public double x;
           public double y;
           public double z;
       }

       private interface IEvaluator
       {
           double Evaluate(Color c1, Color c2);
       }

       private class EudlideanDistanceLab : IEvaluator
       {
           public double Evaluate(Color c1, Color c2)
           {
               LAB lab1 = RGBToLAB(c1);
               LAB lab2 = RGBToLAB(c2);
               var distance = Math.Sqrt(Math.Pow(lab1.L - lab2.L, 2) +
                                Math.Pow(lab1.a - lab2.a, 2) +
                                Math.Pow(lab1.b - lab2.b, 2));
               if (distance == 0)
                   return 1;
               return 1 / distance;
           }

           private static LAB RGBToLAB(Color c)
           {
               return XYZToLAB(RGBToXYZ(c));
           }

           private static XYZ RGBToXYZ(Color color)
           {
               double alpha = 0.055d;
               double r = color.R / 255d;
               double g = color.G / 255d;
               double b = color.B / 255d;

               double Rlinear = (r <= 0.04045d) ? r / 12.92d : Math.Pow((r + alpha) / (1d + alpha), 2.4d);
               double Glinear = (g <= 0.04045d) ? g / 12.92d : Math.Pow((g + alpha) / (1d + alpha), 2.4d);
               double Blinear = (b <= 0.04045d) ? b / 12.92d : Math.Pow((b + alpha) / (1d + alpha), 2.4d);

               Rlinear *= 100d;
               Glinear *= 100d;
               Blinear *= 100d;

               XYZ xyz = new XYZ();
               xyz.x = (.4124d * Rlinear) + (.3576d * Glinear) + (.1805d * Blinear);
               xyz.y = (.2126d * Rlinear) + (.7152d * Glinear) + (.0722d * Blinear);
               xyz.z = (.0193d * Rlinear) + (.1192d * Glinear) + (.9505d * Blinear);
               return xyz;

           }

           private static LAB XYZToLAB(XYZ xyz)
           {
               double whiteX = 95.047d;
               double whiteY = 100.000d;
               double whiteZ = 108.883d;

               double x = xyz.x / whiteX;
               double y = xyz.y / whiteY;
               double z = xyz.z / whiteZ;

               // (6/29)^3 = 0.008856

               x = (x > 0.008856d) ? Math.Pow(x, 1d / 3d) : (7.787037d * x) + (4d / 29d);
               y = (y > 0.008856d) ? Math.Pow(y, 1d / 3d) : (7.787037d * y) + (4d / 29d);
               z = (z > 0.008856d) ? Math.Pow(z, 1d / 3d) : (7.787037d * z) + (4d / 29d);

               LAB lab = new LAB();
               lab.L = 116d * y - 16d;
               lab.a = 500d * (x - y);
               lab.b = 200d * (y - z);
               return lab;
           }

       }

       private class EuclideanDistance : IEvaluator
       {
           public double Evaluate(Color c1, Color c2)
           {
               var distance = (Math.Sqrt(Math.Pow(c1.R - c2.R, 2) +
                                    Math.Pow(c1.G - c2.R, 2) +
                                    Math.Pow(c1.B - c2.B, 2)));
               if(distance == 0){return 1;}
               return 1 / distance;                    
           }
       }

        private class ExactMatch : IEvaluator
        {
            public double Evaluate(Color c1, Color c2)
            {
                if (c1.Equals(c2)) { return 1;  }
                return 0;
            }
        }
   }

}
