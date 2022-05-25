﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HiSql.UnitTest
{
    class Program
    {
        public delegate string MethodCaller(string firstName, string lastName);
        static void Main(string[] args)
        {
            /*
            Console.WriteLine("1:" + Thread.CurrentThread.ManagedThreadId);
            MethodCaller method = new MethodCaller(GetFullName);
            //Task task = Task.Run(() => MethodCaller(GetFullName)) ;
            //IAsyncResult result = method.BeginInvoke("m", "n",null,null);
            var workTask = Task.Run(() => method.Invoke("m", "n" ));
            bool flag = workTask.Wait(2000,new CancellationToken(true));
            //bool flag = result.AsyncWaitHandle.WaitOne(2000, true);//请教WaitOne的第二个参数是什么作用？            
            if (flag)
            {
                Console.WriteLine("time in");
            }
            else
            {
                Console.WriteLine("time out");
            }

            string fullName = workTask.Result;


            Console.WriteLine(fullName);
            Console.WriteLine("2:" + Thread.CurrentThread.ManagedThreadId);
            */

            /*
            dynamic o1 = new { UserName = "tansar", Age = 33 };
            ExpandoObject o2 = new ExpandoObject();
            dynamic o3 = (dynamic)o2;
            o3.UserName = "tansar";
            o3.Age = 33;

            TDynamic t1 = new TDynamic();
            t1["UserName"] = "tansar";
            t1["Age"] = 33;
            dynamic o4 = (ExpandoObject)t1;
            Console.WriteLine(t1["Age"]);

            Console.WriteLine(o4.Age);

            //DataConvert.ToDynamic(new TDynamic(new { UserName = "tansar", Age = 33 }).ToDynamic());
            DataConvert.ToDynamic(t1.ToDynamic());
            */

            StockThread();
            // HiSqlClient sqlcient = Demo_Init.GetSqlClient();

            // Console.WriteLine($"数据库连接id"+sqlcient.Context.ConnectedId);

            //Demo_Update.Init(sqlcient);
            //Demo_Query.Init(sqlcient);

            //Demo_Delete.Init(sqlcient);
            //Demo_Insert.Init(sqlcient);
            //DemoCodeFirst.Init(sqlcient);
            //Demo_Snro.Init(sqlcient);
            // Demo_DbCode.Init(sqlcient);

            //Demo_Cache.Init(sqlcient);
            //SnowId();


            Console.ReadLine();
        }
        static Object _lockerNextId = new Object();
        static void StockThread()
        {
            HiSql.Global.RedisOn = true;//开启redis缓存
            HiSql.Global.RedisOptions = new RedisOptions { Host = "127.0.0.1", PassWord = "", Port = 6379, CacheRegion = "HRM", Database = 2 };

            HiSqlClient sqlClient = Demo_Init.GetSqlClient();
            sqlClient.CodeFirst.Truncate("H_Stock");
            sqlClient.CodeFirst.Truncate("H_Order");

            sqlClient.Modi("H_Stock", new List<object> {
                new { Batch="9000112112",Material="ST0021",Location="A001",st_kc=2000},
                new { Batch="8000252241",Material="ST0080",Location="A001",st_kc=1000},
                new { Batch="7000252241",Material="ST0026",Location="A001",st_kc=1500}

            }).ExecCommand();

            string[] grp_arr1 = new string[] { "9000112112" };
            string[] grp_arr2 = new string[] { "8000252241", "9000112112" };
            string[] grp_arr3 = new string[] { "8000252241", "9000112112", "7000252241" };

            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
           
            Random random = new Random();

            HiSql.Snowflake.SnowType = SnowType.IdWorker;
            Hashtable hashtable = new Hashtable();
            int count  = 0;
            Stopwatch stopwatch1 = Stopwatch.StartNew();
            stopwatch1.Start();
            Parallel.For(0, 100, (index, y) =>
            {
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {

                            string key = HiSql.Snowflake.NextId().ToString();
                            //Thread.Sleep(random.Next(10,99));
                            // Console.WriteLine(key);
                            //
                            lock (_lockerNextId)
                            {
                                hashtable.Add(key, index + "");
                            }


                            //hashtable.Add(key, index + "");
                            // dic.Add(key, index+"");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("---------- " + ex.Message);
                            //throw ex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("---------- " + ex.Message);
                    //throw ex;
                }


            });

            Console.WriteLine(hashtable.Count + " 耗时：" + stopwatch1.ElapsedMilliseconds);
            return;

            Parallel.For(0, 20, (index, y) =>
            {

                int grpidx = index % 3;

                string[] grparr = grp_arr2;
                //if (grpidx == 0)
                //    grparr = grp_arr1;
                //else if (grpidx == 1)
                //    grparr = grp_arr2;
                //else
                //    grparr = grp_arr3;

                //Thread.Sleep(random.Next(10) * 200);

                var radom = new Random();

                bool _flag = true;

                HiSqlClient _sqlClient = Demo_Init.GetSqlClient();

                var rtn = HiSql.Lock.LockOnExecute(grparr, () =>
                {

                    HiSql.Snowflake.SnowType = SnowType.IdWorker;
                    Int64 orderid = HiSql.Snowflake.NextId() + radom.Next(1000, 9999);
                    Console.WriteLine($"时间：{stopwatch.ElapsedMilliseconds} 加锁成功-{index}, 雪花ID{orderid}");

                    _sqlClient.BeginTran(IsolationLevel.ReadUncommitted);

                    DataTable dt = _sqlClient.HiSql($"select Batch,Material,Location,st_kc from H_Stock  where  Batch in ({grparr.ToSqlIn()}) and st_kc>0").ToTable();

                    if (dt.Rows.Count > 0)
                    {
                        List<object> lstorder = new List<object>();
                        string _shop = "4301";
                        _sqlClient.BeginTran();
                        foreach (string n in grparr)
                        {
                            int s = random.Next(10);
                            int v = _sqlClient.Update("H_Stock", new { st_kc = $"`st_kc`-{s}" }).Where($"Batch='{n}' and st_kc>={s}").ExecCommand();
                            Console.WriteLine($"结果:{v}");
                            if (v == 0)
                            {
                                _flag = false;
                                Console.WriteLine($"批次:[{n}]扣减[{s}]失败");
                                _sqlClient.RollBackTran();
                                break;
                            }
                            else
                            {
                                DataRow _drow = dt.AsEnumerable().Where(s => s.Field<string>("Batch").Equals(n)).FirstOrDefault();
                                if (_drow != null)
                                {
                                    lstorder.Add(
                                        new
                                        {
                                            OrderId = orderid,
                                            Batch = _drow["Batch"].ToString(),
                                            Material = _drow["Material"].ToString(),
                                            Shop = _shop,
                                            Location = _drow["Location"].ToString(),
                                            SalesNum = s,
                                        }

                                        );


                                }
                                else
                                {
                                    _flag = false;
                                    Console.WriteLine($"批次:[{n}]扣减[{s}]失败 未找到库存");
                                    _sqlClient.RollBackTran();
                                    break;

                                }

                            }
                        }
                        if (_flag)
                        {
                            //生成订单
                            if (lstorder.Count > 0)
                                _sqlClient.Insert("H_Order", lstorder).ExecCommand();
                            _sqlClient.CommitTran();
                        }

                    }
                    else
                        Console.WriteLine($"库存不足...");



                    Console.WriteLine($"时间：{stopwatch.ElapsedMilliseconds}  业务执行完成-{index}");


                }, new LckInfo
                {
                    UName = "tanar-" + index,
                    Ip = "127.0.0.1"


                }, 60, 30);
                if (!rtn.Item1)
                {
                    Console.WriteLine($"时间：{stopwatch.ElapsedMilliseconds} 对象-{index}" + rtn.Item2);
                }
                //Console.WriteLine($" {index}线程Id:{Thread.CurrentThread.ManagedThreadId}\t{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

                Thread.Sleep(200);

            });
        }
        static void SnowId()
        {
            //IdWorker idWorker = new IdWorker(0, IdWorker.TimeTick(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            //IdSnow snowflake = new IdSnow(0, IdSnow.TimeTick(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            List<long> lst = new List<long>();



            Snowflake.SnowType = SnowType.IdSnow;
            Snowflake.WorkerId = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 10000; i++)
            {
                lst.Add(Snowflake.NextId());
                //Console.WriteLine(idWorker.NextId());
                //Console.WriteLine(snowflake.NextId());

                //Console.WriteLine(Snowflake.NextId());
            }
            sw.Stop();

            Console.WriteLine($"耗时：{sw.Elapsed}秒");


        }

        public static void ToAnonymous(dynamic o)
        {

            //var ostr = JsonConvert.SerializeObject(o);

            //dynamic json = Newtonsoft.Json.Linq.JToken.Parse(ostr) as dynamic;


            Type type = o.GetType();
            dynamic x = new { UserName = "tansar", Age = 33 };
            dynamic dyn = (dynamic)o;

            Console.WriteLine($"UserName:{dyn.UserName},Age:{dyn.Age}");
            //object o1=Activator.CreateInstance(type, true);

            //if (o1 != null)
            //{ 

            //}

        }
        public static string GetFullName(string firstName, string lastName)
        {
            Console.WriteLine("3:" + Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(6000);
            Console.WriteLine("3:" + Thread.CurrentThread.ManagedThreadId);
            return firstName + lastName;
        }
    }
}
