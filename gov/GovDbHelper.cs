using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace gov
{
    /// <summary>
    /// GovDbHelper
    /// </summary>
    class GovDbHelper : MySqlHelper
    {

        public GovDbHelper()
        {
        }


        /// <summary>
        /// 基类构造函数
        /// </summary>
        /// <param name="dbServer"></param>
        /// <param name="dbName"></param>
        /// <param name="dbUserName"></param>
        /// <param name="dbPassWord"></param>
        public GovDbHelper(string dbServer, string dbName, string dbUserName, string dbPassWord)
                     : base(dbServer, dbName, dbUserName, dbPassWord)
        {

        }


        /// <summary>
        /// GetSelectDicBySqlWithLock
        /// </summary>
        /// <param name="sqlSelect"></param>
        /// <param name="reset"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Dictionary<string, List<object>> GetSelectDicBySqlWithLock(string sqlSelect, bool reset = true, int timeout = 10)
        {
            //当执行RELEASE_LOCK()函数时、或者执行一个新的GET_LOCK()函数时，或者线程终止时，之前加上的锁都会自动解除。
            //GET_LOCK('one',20)加上一个名为one的锁，然后再通过GET_LOCK('two',20)创建two锁，那么，one锁就自动解除了
            //所以这个名字必须一样
            const string lockName = "TK1.renwu_gongshang_x315_attr";
            Dictionary<string, List<object>> dic;

            using (var mySqlConnection = base.GetMySqlConnection())
            {
                //打开连接
                mySqlConnection.Open();
                //加锁
                if (!base.GetLock(mySqlConnection, lockName))
                    throw new Exception("加锁失败。");

                //执行select sql语句 并把结果存入到dic中
                var mySqlCommand = base.GetMySqlCommand(sqlSelect, mySqlConnection);
                dic = GetSelectDic(mySqlCommand);
                //取出keyword列表
                var keyWordList = dic["companyname"];
                //主机名
                var hostName = Dns.GetHostName();
                foreach (var keyWord in keyWordList)
                {
                    //更新状态
                    string sqlUpdate;


                    //更新尝试次数
                    if (reset)
                    {
                        sqlUpdate =
                            $"UPDATE renwu_gongshang_x315_attr SET TaskStatue = 100,DispatchTime = NOW(),StationId=@hostName,taskTryNumber = 1 WHERE companyname = @keyWord;";


                    }
                    else
                    {
                        sqlUpdate =
                            $"UPDATE renwu_gongshang_x315_attr SET TaskStatue = 100,DispatchTime = NOW(),StationId=@hostName,taskTryNumber = taskTryNumber+1 WHERE companyname = @keyWord;";
                    }




                    var mySqlCommandUpdate = base.GetMySqlCommand(sqlUpdate, mySqlConnection);

                    //加入参数
                    mySqlCommandUpdate.Parameters.AddWithValue("@hostName", hostName);
                    mySqlCommandUpdate.Parameters.AddWithValue("@keyWord", keyWord);


                    UpdateTable(mySqlCommandUpdate);

                }

                //解锁
                if (!base.GetReleaseLock(mySqlConnection, lockName))
                    throw new Exception("解锁失败。");
                //关闭连接
                mySqlConnection.Close();

            }

            return dic;
        }


        /// <summary>
        /// 根据是否成功更新表字段
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="isSuccess"></param>
        public void UpdateTable(string keyWord, bool isSuccess)
        {

            using (var mySqlConnection = base.GetMySqlConnection())
            {
                //打开连接
                mySqlConnection.Open();
                string sqlUpdate;
                if (isSuccess)
                {
                    sqlUpdate = $"UPDATE renwu_gongshang_x315_attr SET TaskStatue = 5 WHERE companyname = @keyWord;";
                }
                else
                {
                    sqlUpdate = $"UPDATE renwu_gongshang_x315_attr SET TaskStatue = 6 WHERE companyname = @keyWord;";
                }
                var mySqlCommandUpdate = base.GetMySqlCommand(sqlUpdate, mySqlConnection);

                //加入参数
                mySqlCommandUpdate.Parameters.AddWithValue("@keyWord", keyWord);

                UpdateTable(mySqlCommandUpdate);
                //关闭连接
                mySqlConnection.Close();
            }
        }


        /// <summary>
        /// 根据是否成功更新表字段
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="tableName"></param>
        /// <param name="isSuccess"></param>
        public void UpdateTable(string keyWord, string tableName, bool isSuccess)
        {

            using (var mySqlConnection = base.GetMySqlConnection())
            {
                //打开连接
                mySqlConnection.Open();
                string sqlUpdate;
                if (isSuccess)
                {
                    sqlUpdate = $"UPDATE {tableName} SET TaskStatue = 5 WHERE companyname = @keyWord;";
                }
                else
                {
                    sqlUpdate = $"UPDATE {tableName} SET TaskStatue = 6 WHERE companyname = @keyWord;";
                }
                var mySqlCommandUpdate = base.GetMySqlCommand(sqlUpdate, mySqlConnection);

                //加入参数
                mySqlCommandUpdate.Parameters.AddWithValue("@keyWord", keyWord);

                UpdateTable(mySqlCommandUpdate);
                //关闭连接
                mySqlConnection.Close();
            }
        }



        /// <summary>
        /// GetSelectDicBySqlWithLock
        /// </summary>
        /// <param name="sqlSelect"></param>
        /// <param name="tableName"></param>
        /// <param name="reset"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Dictionary<string, List<object>> GetSelectDicBySqlWithLock(string sqlSelect, string tableName, bool reset = true, int timeout = 10)
        {
            //当执行RELEASE_LOCK()函数时、或者执行一个新的GET_LOCK()函数时，或者线程终止时，之前加上的锁都会自动解除。
            //GET_LOCK('one',20)加上一个名为one的锁，然后再通过GET_LOCK('two',20)创建two锁，那么，one锁就自动解除了
            //所以这个名字必须一样
            const string lockName = "TK1.renwu_gongshang_x315_attr";
            Dictionary<string, List<object>> dic;

            using (var mySqlConnection = base.GetMySqlConnection())
            {
                //打开连接
                mySqlConnection.Open();
                //加锁
                if (!base.GetLock(mySqlConnection, lockName))
                    throw new Exception("加锁失败。");

                //执行select sql语句 并把结果存入到dic中
                var mySqlCommand = base.GetMySqlCommand(sqlSelect, mySqlConnection);
                dic = GetSelectDic(mySqlCommand);
                //取出keyword列表
                var keyWordList = dic["companyname"];
                //主机名
                var hostName = Dns.GetHostName();
                foreach (var keyWord in keyWordList)
                {
                    //更新状态
                    string sqlUpdate;


                    //更新尝试次数
                    if (reset)
                    {
                        sqlUpdate =
                            $"UPDATE {tableName} SET TaskStatue = 100,DispatchTime = NOW(),StationId=@hostName,taskTryNumber = 1 WHERE companyname = @keyWord;";

                    }
                    else
                    {
                        sqlUpdate =
                            $"UPDATE {tableName} SET TaskStatue = 100,DispatchTime = NOW(),StationId=@hostName,taskTryNumber = taskTryNumber+1 WHERE companyname = @keyWord;";
                    }

                    var mySqlCommandUpdate = base.GetMySqlCommand(sqlUpdate, mySqlConnection);

                    //加入参数
                    mySqlCommandUpdate.Parameters.AddWithValue("@hostName", hostName);
                    mySqlCommandUpdate.Parameters.AddWithValue("@keyWord", keyWord);

                    UpdateTable(mySqlCommandUpdate);

                }

                //解锁
                if (!base.GetReleaseLock(mySqlConnection, lockName))
                    throw new Exception("解锁失败。");
                //关闭连接
                mySqlConnection.Close();

            }

            return dic;
        }


    }
}
