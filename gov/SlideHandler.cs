using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using GsxtWebCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Support.UI;


namespace gov
{
    //http://www.cnblogs.com/TankXiao/p/5260707.html 操作弹出窗口
    //http://www.cnblogs.com/lingling99/p/5750266.html
    //http://www.ibm.com/developerworks/cn/java/j-lo-keyboard/ Selenium WebDriver 中鼠标和键盘事件分析及扩展
    public static class SlideHandler
    {
        private static string SlideUseChrome(string companyName)
        {
            const string url = "http://www.gsxt.gov.cn/index.html";

            //InternetExplorerOptions options = new InternetExplorerOptions();
            ////取消浏览器的保护模式 设置为true
            //options.IntroduceInstabilityByIgnoringProtectedModeSettings = true;
            //这里用chrome浏览器 ie浏览器有问题
            var options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25");

            using (var driver = new ChromeDriver(options))
            {
                //设置浏览器大小 设置为最大 元素的X,Y坐标就准了 不然就不准(不知道原因)
                driver.Manage().Window.Maximize();

                var navigation = driver.Navigate();
                navigation.GoToUrl(url);
                //等待时间
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                //等待元素全部加载完成
                wait.Until(ExpectedConditions.ElementExists(By.Id("keyword")));
                var keyWord = driver.FindElement(By.Id("keyword"));
                //keyWord.SendKeys("温州红辣椒电子商务有限公司");
                keyWord.SendKeys(companyName);

                //等待元素全部加载完成
                wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("btn_query")));
                var js = (IJavaScriptExecutor)driver;
                var btnQuery = driver.FindElement(By.Id("btn_query"));
                //经测试，这里要停一下，不然刚得到元素就click可能不会出现滑动块窗口(很坑的地方)
                Thread.Sleep(1000);
                js.ExecuteScript("arguments[0].click();", btnQuery);
                //btnQuery.Click();
                //btnQuery.SendKeys(Keys.Enter);

                //截图加滑动处理
                //因为只有一个弹出窗口，所以直接进到这个里面就好了
                var allWindowsId = driver.WindowHandles;
                if (allWindowsId.Count != 1)
                    throw new Exception("多个弹出窗口。");

                foreach (var windowId in allWindowsId)
                {

                    driver.SwitchTo().Window(windowId);
                }


                //等待元素全部加载完成
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector("div.gt_box")));

                //找到图片
                var imageBox = driver.FindElement(By.CssSelector("div.gt_box"));
                //先休息一会，不然截图不对
                Thread.Sleep(1000);
                //截图得到子图片
                var imageFirst = GetSubImage(driver, imageBox);
                //imageFirst?.Save("c:/test.png");


                var slide = driver.FindElement(By.CssSelector("div.gt_slider_knob.gt_show"));
                var action = new Actions(driver);
                //移到起始位置
                action.ClickAndHold(slide).MoveByOffset(0, 0).Perform();
                //先休息一会，不然截图不对
                Thread.Sleep(1000);
                //再截图得到子图片
                var imageSecond = GetSubImage(driver, imageBox);
                //imageSecond?.Save("c:/test1.png");

                var pass = false;
                var tryTimes = 0;
                //试5次或者pass
                while (tryTimes++<5&&!pass)
                {
                    Console.WriteLine($"第{tryTimes}次。");
                    var left = SlideImageHandler.FindXDiffRectangeOfTwoImage(imageFirst, imageSecond) - 7;
                    Console.WriteLine($"减7后等于:{left}");
                    if (left <= 0)
                        throw new Exception("算出的距离小于等于0");
                    var pointsTrace = SlideImageHandler.GeTracePoints(left);

                    //移动
                    MoveHandler(pointsTrace, action, slide);

                    wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//div[@class='gt_info_text']/span[@class='gt_info_type']")));
                    //找得到元素，但是它不在当前可见的页面上。
                    var infoText = driver.FindElement(By.XPath("//div[@class='gt_info_text']/span[@class='gt_info_type']")).Text;
                    if (infoText.Contains("验证通过"))
                    {
                        pass = true;
                    }
                    //如果判断为非人行为 刷新验证码 重新截图
                    else if (infoText.Contains("再来一次"))
                    {
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("gt_refresh_button")));
                        var refreshButtom = driver.FindElement(By.ClassName("gt_refresh_button"));
                        refreshButtom.Click();
                        //等待元素全部加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector("div.gt_box")));
                        //找到图片
                        imageBox = driver.FindElement(By.CssSelector("div.gt_box"));
                        //先休息一会，不然截图不对
                        Thread.Sleep(1000);
                        //截图得到子图片
                        imageFirst = GetSubImage(driver, imageBox);
                        //imageFirst?.Save("c:/test.png");
                        //移到起始位置
                        action.ClickAndHold(slide).MoveByOffset(0, 0).Perform();
                        //先休息一会，不然截图不对
                        Thread.Sleep(1000);
                        //再截图得到子图片
                        imageSecond = GetSubImage(driver, imageBox);
                        //imageSecond?.Save("c:/test1.png");
                        Console.WriteLine("刷新图片。");
                        pass = false;
                    }
                    else
                    {
                        pass = false;
                    }
                    Console.WriteLine($"pass:{pass}。");
                    //先休息一会，不然截图不对
                    //Thread.Sleep(1000);
                    var imageThird = GetSubImage(driver, imageBox);
                    //imageThird?.Save("c:/test2.png");
                    
                }


                Console.WriteLine(pass ? "验证通过。" : "验证失败。");

                if (!pass)
                {
                    throw new Exception("极速验证码验证失败。");
                }
                //得到页面内容
                return driver.PageSource;


            }
        }






        /// <summary>
        /// SlideWithPhantomJs
        /// </summary>
        /// <param name="companyName"></param>
        private static string SlideUsePhantomJs(string companyName)
        {
            const string url = "http://www.gsxt.gov.cn/index.html";

            var userAgent =
                @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            var options = new PhantomJSOptions();
            options.AddAdditionalCapability(@"phantomjs.page.settings.userAgent", userAgent);

            using (var driver = new PhantomJSDriver(options))
            {
                //设置浏览器大小 设置为最大 元素的X,Y坐标就准了 不然就不准(不知道原因)
                driver.Manage().Window.Maximize();

                var navigation = driver.Navigate();
                navigation.GoToUrl(url);
                //等待时间
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                //等待元素全部加载完成
                wait.Until(ExpectedConditions.ElementExists(By.Id("keyword")));
                var keyWord = driver.FindElement(By.Id("keyword"));
                //keyWord.SendKeys("温州红辣椒电子商务有限公司");
                keyWord.SendKeys(companyName);
                
                //等待元素全部加载完成
                wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("btn_query")));
                var js = (IJavaScriptExecutor)driver;
                var btnQuery = driver.FindElement(By.Id("btn_query"));
                //经测试，这里要停一下，不然刚得到元素就click可能不会出现滑动块窗口(很坑的地方)
                Thread.Sleep(1000);
                js.ExecuteScript("arguments[0].click();", btnQuery);
                //btnQuery.Click();
                //btnQuery.SendKeys(Keys.Enter);

                //截图加滑动处理
                //因为只有一个弹出窗口，所以直接进到这个里面就好了
                var allWindowsId = driver.WindowHandles;
                if (allWindowsId.Count != 1)
                    throw new Exception("多个弹出窗口。");

                foreach (var windowId in allWindowsId)
                {

                    driver.SwitchTo().Window(windowId);
                }


                //等待元素全部加载完成
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector("div.gt_box")));

                //找到图片
                var imageBox = driver.FindElement(By.CssSelector("div.gt_box"));
                //先休息一会，不然截图不对
                Thread.Sleep(1000);
                //截图得到子图片
                var imageFirst = GetSubImage(driver, imageBox);
                //imageFirst?.Save("c:/test.png");


                var slide = driver.FindElement(By.CssSelector("div.gt_slider_knob.gt_show"));
                var action = new Actions(driver);
                //移到起始位置
                action.ClickAndHold(slide).MoveByOffset(0, 0).Perform();
                //先休息一会，不然截图不对
                Thread.Sleep(1000);
                //再截图得到子图片
                var imageSecond = GetSubImage(driver, imageBox);
                //imageSecond?.Save("c:/test1.png");

                var pass = false;
                var tryTimes = 0;
                //试5次或者pass
                while (tryTimes++ < 5 && !pass)
                {
                    Console.WriteLine($"第{tryTimes}次。");
                    var left = SlideImageHandler.FindXDiffRectangeOfTwoImage(imageFirst, imageSecond) - 7;
                    Console.WriteLine($"减7后等于:{left}");
                    if (left <= 0)
                        throw new Exception("算出的距离小于等于0");
                    var pointsTrace = SlideImageHandler.GeTracePoints(left);

                    //移动
                    MoveHandler(pointsTrace, action, slide);

                    wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//div[@class='gt_info_text']/span[@class='gt_info_type']")));
                    //找得到元素，但是它不在当前可见的页面上。
                    var infoText = driver.FindElement(By.XPath("//div[@class='gt_info_text']/span[@class='gt_info_type']")).Text;
                    if (infoText.Contains("验证通过"))
                    {
                        pass = true;
                    }
                    //如果判断为非人行为 刷新验证码 重新截图
                    else if (infoText.Contains("再来一次"))
                    {
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("gt_refresh_button")));
                        var refreshButtom = driver.FindElement(By.ClassName("gt_refresh_button"));
                        refreshButtom.Click();
                        //等待元素全部加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector("div.gt_box")));
                        //找到图片
                        imageBox = driver.FindElement(By.CssSelector("div.gt_box"));
                        //先休息一会，不然截图不对
                        Thread.Sleep(1000);
                        //截图得到子图片
                        imageFirst = GetSubImage(driver, imageBox);
                        //imageFirst?.Save("c:/test.png");
                        //移到起始位置
                        action.ClickAndHold(slide).MoveByOffset(0, 0).Perform();
                        //先休息一会，不然截图不对
                        Thread.Sleep(1000);
                        //再截图得到子图片
                        imageSecond = GetSubImage(driver, imageBox);
                        //imageSecond?.Save("c:/test1.png");
                        Console.WriteLine("刷新图片。");
                        pass = false;
                    }
                    else
                    {
                        pass = false;
                    }
                    Console.WriteLine($"pass:{pass}。");
                    //先休息一会，不然截图不对
                    //Thread.Sleep(1000);
                    var imageThird = GetSubImage(driver, imageBox);
                    //imageThird?.Save("c:/test2.png");

                }


                Console.WriteLine(pass ? "验证通过。" : "验证失败。");
                if (!pass)
                {
                    throw new Exception("极速验证码验证失败。");
                }
                //得到页面内容
                return driver.PageSource;
                
            }
        }

        /// <summary>
        /// GetHtml
        /// </summary>
        /// <param name="companyName"></param>
        /// <returns></returns>
        public static string GetHtml(string companyName)
        {
            //return SlideUsePhantomJs(companyName);
            return SlideUseChrome(companyName);
        }

        /// <summary>
        /// Test
        /// </summary>
        internal static void Test()
        {

            var companyNames = File.ReadAllLines(@"C:\Users\Administrator\Desktop\companyname.txt");
            foreach (var companyName in companyNames)
            {
                //SlideUsePhantomJs(companyName);
                SlideUseChrome(companyName);
            }
        }

        /// <summary>
        /// MoveHandler
        /// </summary>
        /// <param name="pointsTrace"></param>
        /// <param name="action"></param>
        /// <param name="webElement"></param>
        private static void MoveHandler(PointTrace[] pointsTrace, Actions action, IWebElement webElement)
        {

            var preY = 0;

            //鼠标移位置
            var length = pointsTrace.Length;
            for (var i = 0; i < length; i++)
            {
                if (i == 0)
                {
                    action.ClickAndHold(webElement).MoveByOffset(pointsTrace[i].XOffset, pointsTrace[i].YOffset).Perform();
                }
                else if (i < length - 1)
                {
                    action.MoveByOffset(pointsTrace[i].XOffset - pointsTrace[i - 1].XOffset, pointsTrace[i].YOffset - preY).Perform();
                }
                else
                {
                    action.MoveByOffset(pointsTrace[i].XOffset - pointsTrace[i - 1].XOffset, pointsTrace[i].YOffset - preY).Release().Perform();
                }

                //上次的y偏移量，下次要剪掉
                preY = pointsTrace[i].YOffset;


                Thread.Sleep(TimeSpan.FromMilliseconds(pointsTrace[i].SleepTime));

            }

        }

        /// <summary>
        /// BytesToImage
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static Image BytesToImage(byte[] bytes)
        {
            var memoryStream = new MemoryStream(bytes);
            var image = Image.FromStream(memoryStream);
            return image;
        }

        /// <summary>
        /// GetSubImage
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Image GetSubImage(byte[] bytes, int x, int y, int width, int height)
        {
            if (width == 0 || height == 0)
                return null;
            var image = BytesToImage(bytes);
            var bitmap = new Bitmap(image);
            var rectangle = new Rectangle(x, y, width, height);
            var bitmapClone = bitmap.Clone(rectangle, bitmap.PixelFormat);
            return bitmapClone;
        }


        /// <summary>
        /// GetSubImage
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="iWebElement"></param>
        /// <returns></returns>
        private static Image GetSubImage(ChromeDriver driver, IWebElement iWebElement)
        {
            var location = iWebElement.Location;
            var x = location.X;
            var y = location.Y;
            var size = iWebElement.Size;
            var width = size.Width;
            var height = size.Height;
            var screenshot = driver.GetScreenshot();
            //screenshot.SaveAsFile("c:/yuantu.png",ImageFormat.Png);
            var byteArray = screenshot.AsByteArray;
            var image = GetSubImage(byteArray, x, y, width, height);
            return image;
        }

        /// <summary>
        /// GetSubImage
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="iWebElement"></param>
        /// <returns></returns>
        private static Image GetSubImage(PhantomJSDriver driver, IWebElement iWebElement)
        {
            var location = iWebElement.Location;
            var x = location.X;
            var y = location.Y;
            var size = iWebElement.Size;
            var width = size.Width;
            var height = size.Height;
            var screenshot = driver.GetScreenshot();
            //screenshot.SaveAsFile("c:/yuantu.png",ImageFormat.Png);
            var byteArray = screenshot.AsByteArray;
            var image = GetSubImage(byteArray, x, y, width, height);
            return image;
        }


    }
}
