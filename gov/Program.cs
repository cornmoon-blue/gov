using System.Globalization;
using System.Threading;
using X.CommLib.Miscellaneous;

namespace gov
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using GsxtWebCore;

    using X.CommLib.Logs;
    using X.CommLib.Threader;

    /// <summary>
    /// 主程序
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {



            var tSessionDefender = new TerminalSessionDefender();
            tSessionDefender.OnLogginEvent += WriteLog;
            tSessionDefender.Start();


            /*tSessionDefender.Stop();
            tSessionDefender.OnLogginEvent -= WriteLog;*/


            // 启用日志记录
            X.CommLib.Logs.LogHelper.StartLogSystem();


            var govCrawler = new GovCrawler();
            govCrawler.OnLogginEvent += WriteLog;

            //由于安全原因 此处不给出工作环境
            var govDbHelperRenwu = new GovDbHelper("host", "tableName", "userName", "password");
            var govDbHelperResult = new GovDbHelper("host", "tableName", "userName", "password");


            ////优先
            //RunTask(govCrawler, govDbHelperRenwu, "renwu_gongshang_x315_vip_attr", govDbHelperResult, "yuanshi_gongshang_x315_vip_attr");
            ////其次 继续 不退出
            //RunTask(govCrawler, govDbHelperRenwu,  "renwu_gongshang_x315_attr", govDbHelperResult, "yuanshi_gongshang_x315_attr", true);

            RunVipTaskUntilFinish(govCrawler, govDbHelperRenwu, "renwu_gongshang_x315_vip_attr", govDbHelperResult, "yuanshi_gongshang_x315_vip_attr");

            RunTaskAndCheckVipTask(govCrawler, govDbHelperRenwu, "renwu_gongshang_x315_vip_attr", "renwu_gongshang_x315_attr",
                                   govDbHelperResult, "yuanshi_gongshang_x315_vip_attr", "yuanshi_gongshang_x315_attr");


        }


        /// <summary>
        /// RunVipTask
        /// </summary>
        /// <param name="govCrawler"></param>
        /// <param name="govDbHelperRenwu"></param>
        /// <param name="renwuTableName"></param>
        /// <param name="govDbHelperResult"></param>
        /// <param name="resultTableName"></param>
        private static void RunVipTaskUntilFinish(GovCrawler govCrawler, GovDbHelper govDbHelperRenwu, string renwuTableName, GovDbHelper govDbHelperResult, string resultTableName)
        {
            var hasTask = true;
            while (hasTask)
            {
                hasTask = HasTask(govCrawler, govDbHelperRenwu, renwuTableName, govDbHelperResult, resultTableName);
            }
        }


        /// <summary>
        /// RunTaskAndCheckVipTask
        /// </summary>
        /// <param name="govCrawler"></param>
        /// <param name="govDbHelperRenwu"></param>
        /// <param name="renwuVipTableName"></param>
        /// <param name="renwuTableName"></param>
        /// <param name="govDbHelperResult"></param>
        /// <param name="resultVipTableName"></param>
        /// <param name="resultTableName"></param>
        private static void RunTaskAndCheckVipTask(GovCrawler govCrawler, GovDbHelper govDbHelperRenwu, string renwuVipTableName, string renwuTableName, GovDbHelper govDbHelperResult, string resultVipTableName, string resultTableName)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                //跑一个普通任务
                HasTask(govCrawler, govDbHelperRenwu, renwuTableName, govDbHelperResult, resultTableName);
                if ((DateTime.Now - startTime).Minutes <= 30) continue;
                //跑vip任务
                RunVipTaskUntilFinish(govCrawler, govDbHelperRenwu, renwuVipTableName, govDbHelperResult, resultVipTableName);
                //更新开始时间
                startTime = DateTime.Now;
            }
        }



        /// <summary>
        /// HasTask
        /// </summary>
        /// <param name="govCrawler"></param>
        /// <param name="govDbHelperRenwu"></param>
        /// <param name="renwuTableName"></param>
        /// <param name="govDbHelperResult"></param>
        /// <param name="resultTableName"></param>
        private static bool HasTask(GovCrawler govCrawler, GovDbHelper govDbHelperRenwu, string renwuTableName, GovDbHelper govDbHelperResult, string resultTableName)
        {
            try
            {
                //先取任务为1的
                var dic =
                    govDbHelperRenwu.GetSelectDicBySqlWithLock(
                        $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue = 1 LIMIT 3", renwuTableName);
                var valueList = dic["companyname"];

                //为空 取任务为6的 尝试次数小于等于5的
                if (valueList.Count == 0)
                {
                    //重试次数30次
                    dic =
                        govDbHelperRenwu.GetSelectDicBySqlWithLock(
                        $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue = 6 AND taskTryNumber<30 LIMIT 3", renwuTableName, false);
                    valueList = dic["companyname"];
                }


                //为空 取任务为100 时间大于半小时的
                if (valueList.Count == 0)
                {
                    dic =
                        govDbHelperRenwu.GetSelectDicBySqlWithLock(
                            $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue=100 AND TIMESTAMPDIFF(MINUTE,DispatchTime,NOW())>30 LIMIT 3", renwuTableName, false);
                    valueList = dic["companyname"];
                    if (valueList.Count == 0)
                    {
                        return false;
                    }
                }

                foreach (var value in valueList)
                {
                    var companyName = value.ToString();
                    //公司信息是否存在
                    var infoExist = true;
                    var companyInfoDic = new Dictionary<string, string>();
                    try
                    {

                        try
                        {
                            companyInfoDic = govCrawler.GetCompanyInfoDicByKeyWord(companyName);
                        }
                        catch (CompanyNotFoundException)
                        {
                            companyInfoDic.Add("companyName", companyName);

                            // 台州市黄岩南陌商贸有限公司
                            companyInfoDic.Add("notice", "公司信息不存在。");
                            // 公司信息不存在
                            infoExist = false;
                        }

                        //加入采集时间
                        companyInfoDic.Add("InfoGatherDate", DateTime.Now.ToString(CultureInfo.CurrentCulture));

                        // 插入结果表
                        govDbHelperResult.InsertTableWithDic(companyInfoDic, resultTableName);
                        //更新任务表状态
                        govDbHelperRenwu.UpdateTable(companyName, renwuTableName, infoExist);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"异常信息：{e.Message}");

                        // 更新任务表状态
                        govDbHelperRenwu.UpdateTable(companyName, renwuTableName, false);
                    }

                    Console.WriteLine($"companyname:{value}");
                    foreach (var info in companyInfoDic)
                    {
                        Console.WriteLine($"{info.Key}:{info.Value}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"出现异常：{e.Message}");
                Console.WriteLine("休息1分钟");
                Thread.Sleep(1000 * 60);
            }

            return true;

        }


        /// <summary>
        /// RunTask
        /// </summary>
        /// <param name="govCrawler"></param>
        /// <param name="govDbHelperRenwu"></param>
        /// <param name="renwuTableName"></param>
        /// <param name="govDbHelperResult"></param>
        /// <param name="resultTableName"></param>
        /// <param name="isContinue"></param>
        private static void RunTask(GovCrawler govCrawler, GovDbHelper govDbHelperRenwu, string renwuTableName, GovDbHelper govDbHelperResult, string resultTableName, bool isContinue = false)
        {
            while (true)
            {
                try
                {
                    //先取任务为1的
                    var dic =
                        govDbHelperRenwu.GetSelectDicBySqlWithLock(
                            $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue = 1 LIMIT 3", renwuTableName);
                    var valueList = dic["companyname"];

                    //为空 取任务为6的 尝试次数小于等于5的
                    if (valueList.Count == 0)
                    {
                        //重试次数30次
                        dic =
                            govDbHelperRenwu.GetSelectDicBySqlWithLock(
                            $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue = 6 AND taskTryNumber<30 LIMIT 3", renwuTableName, false);
                        valueList = dic["companyname"];
                    }


                    //为空 取任务为100 时间大于半小时的
                    if (valueList.Count == 0)
                    {
                        dic =
                            govDbHelperRenwu.GetSelectDicBySqlWithLock(
                                $"SELECT companyname FROM {renwuTableName} WHERE TaskStatue=100 AND TIMESTAMPDIFF(MINUTE,DispatchTime,NOW())>30 LIMIT 3", renwuTableName, false);
                        valueList = dic["companyname"];
                        if (valueList.Count == 0)
                        {
                            if (isContinue)
                            {
                                //休息50s
                                Console.WriteLine("休息50s");
                                Thread.Sleep(1000 * 50);
                                continue;
                            }
                            return;
                        }
                    }

                    foreach (var value in valueList)
                    {
                        var companyName = value.ToString();

                        var companyInfoDic = new Dictionary<string, string>();
                        try
                        {

                            try
                            {
                                companyInfoDic = govCrawler.GetCompanyInfoDicByKeyWord(companyName);
                            }
                            catch (CompanyNotFoundException)
                            {
                                companyInfoDic.Add("companyName", companyName);

                                // 台州市黄岩南陌商贸有限公司
                                companyInfoDic.Add("notice", "公司信息不存在。");
                            }

                            //加入采集时间
                            companyInfoDic.Add("InfoGatherDate", DateTime.Now.ToString(CultureInfo.CurrentCulture));

                            // 插入结果表
                            govDbHelperResult.InsertTableWithDic(companyInfoDic, resultTableName);

                            // 更新任务表状态
                            govDbHelperRenwu.UpdateTable(companyName, renwuTableName, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"异常信息：{e.Message}");

                            // 更新任务表状态
                            govDbHelperRenwu.UpdateTable(companyName, renwuTableName, false);
                        }

                        Console.WriteLine($"companyname:{value}");
                        foreach (var info in companyInfoDic)
                        {
                            Console.WriteLine($"{info.Key}:{info.Value}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"出现异常：{e.Message}");
                    Console.WriteLine("休息1分钟");
                    Thread.Sleep(1000 * 60);
                }
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="LoggingEventArgs"/> instance containing the event data.</param>
        private static void WriteLog(object sender, LoggingEventArgs e)
        {
            LogHelper.WriteLog(sender, e);
            LogHelper.OutputLog(sender, e);
        }
    }
}