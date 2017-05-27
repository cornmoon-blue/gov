using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace gov
{
    public static class SlideImageHandler
    {

        /// <summary>
        /// 找到两种图片不相等点的x坐标
        /// </summary>
        /// <param name="imageFirst"></param>
        /// <param name="imageSecond"></param>
        /// <returns></returns>
        public static int FindXDiffRectangeOfTwoImage(Image imageFirst, Image imageSecond)
        {
            try
            {

                var image1 = new Bitmap(imageFirst);
                var image2 = new Bitmap(imageSecond);


                //BufferedImage image1 = ImageIO.read(new File(imageFirst));
                //BufferedImage image2 = ImageIO.read(new File(imageSecond));
                int width1 = image1.Width;
                int height1 = image1.Height;
                int width2 = image2.Width;
                int height2 = image2.Height;

                if (width1 != width2) return -1;
                if (height1 != height2) return -1;

                int left = 0;
                /**
                 * 从左至右扫描
                 */
                bool flag = false;
                for (int i = 60; i < width1; i++)
                {
                    for (int j = 0; j < height1; j++)
                        if (IsPixelNotEqual(image1, image2, i, j))
                        {
                            left = i;
                            flag = true;
                            break;
                        }
                    if (flag) break;
                }

                if (left <= 60)
                {
                    flag = false;
                    //如果left小于等于60 从右至左
                    for (int i = width1; i > 60; i--)
                    {
                        for (int j = 0; j < height1; j++)
                            if (IsPixelNotEqual(image1, image2, i, j))
                            {
                                left = i;
                                flag = true;
                                break;
                            }
                        if (flag)
                        {
                            left -= 45;
                            break;
                        }
                    }

                    Console.WriteLine($"从右往左算出:{left}");
                }
                else
                {
                    Console.WriteLine($"从左往右算出:{left}");
                }

                return left;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return -1;
            }
        }



        /// <summary>
        /// 找到两种图片不相等点的x坐标
        /// </summary>
        /// <param name="imageFirst"></param>
        /// <param name="imageSecond"></param>
        /// <returns></returns>
        public static int FindXDiffRectangeOfTwoImage(string imageFirst, string imageSecond)
        {
            var image1 = new Bitmap(imageFirst);
            var image2 = new Bitmap(imageSecond);
            var left = FindXDiffRectangeOfTwoImage(image1, image2);
            return left;
        }

        /// <summary>
        /// 像素不相等
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private static bool IsPixelNotEqual(Bitmap image1, Bitmap image2, int i, int j)
        {
            var pixel1 = image1.GetPixel(i, j);
            var pixel2 = image2.GetPixel(i, j);

            int[] rgb1 = new int[3];
            rgb1[0] = pixel1.R;
            rgb1[1] = pixel1.G;
            rgb1[2] = pixel1.B;

            int[] rgb2 = new int[3];
            rgb2[0] = pixel2.R;
            rgb2[1] = pixel2.G;
            rgb2[2] = pixel1.B;

            for (int k = 0; k < 3; k++)
                if (Math.Abs(rgb1[k] - rgb2[k]) > 100)//因为背景图会有一些像素差异
                    return true;

            return false;
        }


        /// <summary>
        /// GeTracePoints
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static PointTrace[] GeTracePoints(int distance)
        {
            var random = new Random();

            var curDistance = 0;
            var totalSleepTime = 0D;
            var listX = new List<int>();
            var listY = new List<int>();
            var listSleepTime = new List<double>();

            //random取下界不取上界
            //先加一个初始点
            listX.Add(0);
            listY.Add(random.Next(-2, 3));
            listSleepTime.Add(NextDouble(random, 10, 50));


            //curDistance = curDistance + curDistance + random.Next(1, 5);
            while (Math.Abs(distance - curDistance) > 1)
            {
                //模拟加速的一个过程
                var moveX = curDistance + random.Next(1, 5);
                var moveY = random.Next(-2, 3);
                var sleepTime = NextDouble(random, 10, 50);
                listX.Add(moveX);
                listY.Add(moveY);
                listSleepTime.Add(sleepTime);
                curDistance += moveX;
                totalSleepTime += sleepTime;
                //如果当前的距离大于等于给的距离退出
                if (curDistance >= distance)
                    break;
            }

            //如果移过头了 最后终点加入
            if (curDistance > distance)
            {
                listX.Add(distance);
                listY.Add(random.Next(-2, 3));
                listSleepTime.Add(NextDouble(random, 10, 50));
            }



            //长度
            var length = listSleepTime.Count;
            const int maxTotalSleepTime = 5 * 1000;
            if (totalSleepTime > maxTotalSleepTime)
            {
                //统计时间
                totalSleepTime = 0.0D;
                for (var i = 0; i < length; i++)
                {
                    //按比例缩小时间
                    listSleepTime[i] = listSleepTime[i] * (maxTotalSleepTime / totalSleepTime);
                    totalSleepTime += listSleepTime[i];
                }
            }
            //输出总时间
            Console.WriteLine($"滑块滑动总时间:{totalSleepTime}");


            var tracePoints = new PointTrace[length];
            for (var i = 0; i < length; i++)
            {
                tracePoints[i] = new PointTrace
                {
                    XOffset = listX[i],
                    YOffset = listY[i],
                    SleepTime = listSleepTime[i]
                };
            }

            //输出轨迹值
            Console.WriteLine("输出轨迹值。");
            for (var i = 0; i < length; i++)
            {

                Console.WriteLine($"滑块轨迹;X:{tracePoints[i].XOffset},Y:{tracePoints[i].YOffset},Time:{tracePoints[i].SleepTime}");
            }


            return tracePoints;

        }

        /// <summary>
        /// GeTracePoints
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static PointTrace[] GeTracePoints1(int distance)
        {
            var random = new Random();

            var curDistance = 0;
            var totalSleepTime = 0D;
            var listX = new List<int>();
            var listY = new List<int>();
            var listSleepTime = new List<double>();

            //先加一个初始点
            listX.Add(0);
            listY.Add(random.Next(-2, 2));
            listSleepTime.Add(NextDouble(random, 10, 50));


            //curDistance = curDistance + curDistance + random.Next(1, 5);
            while (Math.Abs(distance - curDistance) > 1)
            {
                var moveX = random.Next(1, 5);
                curDistance += moveX;
                var moveY = random.Next(-2, 2);
                var sleepTime = NextDouble(random, 10, 50);
                listX.Add(curDistance);
                listY.Add(moveY);
                listSleepTime.Add(sleepTime);
                totalSleepTime += sleepTime;
                //如果当前的距离大于等于给的距离退出
                if (curDistance >= distance)
                    break;
            }

            //如果移过头了 最后终点加入
            if (curDistance > distance)
            {
                listX.Add(distance);
                listY.Add(random.Next(-2, 2));
                listSleepTime.Add(NextDouble(random, 10, 50));
            }



            //长度
            var length = listSleepTime.Count;
            const int maxTotalSleepTime = 5 * 1000;
            if (totalSleepTime > maxTotalSleepTime)
            {
                //统计时间
                totalSleepTime = 0.0D;
                for (var i = 0; i < length; i++)
                {
                    //按比例缩小时间
                    listSleepTime[i] = listSleepTime[i] * (maxTotalSleepTime / totalSleepTime);
                    totalSleepTime += listSleepTime[i];
                }
            }
            //输出总时间
            Console.WriteLine($"滑块滑动总时间:{totalSleepTime}");


            var tracePoints = new PointTrace[length];
            for (var i = 0; i < length; i++)
            {
                tracePoints[i] = new PointTrace
                {
                    XOffset = listX[i],
                    YOffset = listY[i],
                    SleepTime = listSleepTime[i]
                };
            }

            //输出轨迹值
            Console.WriteLine("输出轨迹值。");
            for (var i = 0; i < length; i++)
            {

                Console.WriteLine($"滑块轨迹;X:{tracePoints[i].XOffset},Y:{tracePoints[i].YOffset},Time:{tracePoints[i].SleepTime}");
            }


            return tracePoints;

        }

        /// <summary>
        /// 返回double类型的随机数
        /// </summary>
        /// <param name="random"></param>
        /// <param name="minDouble"></param>
        /// <param name="maxDouble"></param>
        /// <returns></returns>
        private static double NextDouble(Random random, double minDouble, double maxDouble)
        {
            if (random != null)
            {
                return random.NextDouble() * (maxDouble - minDouble) + minDouble;
            }
            else
            {
                return 0.0D;
            }
        }
    }

    /// <summary>
    /// 轨迹点
    /// </summary>
    public class PointTrace
    {
        public int XOffset;
        public int YOffset;
        public double SleepTime;
    }
}
