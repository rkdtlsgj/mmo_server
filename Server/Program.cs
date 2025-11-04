using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using SharedDB;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{

    // 1. Recv 쓰레드풀
    // 2. GameRoomManager 단일 쓰레드
    // 3. DB 단일 쓰레드
    // 4. Send 단일 쓰레드
    class Program
    {
        static Listener _listener = new Listener();

        static void GameLogicTask()
        {
            while (true)
            {
                GameLogic.Instance.Update();
                Thread.Sleep(0);
            }
        }

        static void DbTask()
        {
            while (true)
            {
                DbTransaction.Instance.Flush();
                Thread.Sleep(0);
                //일감을 모두 끝나고나왔으면 잠시 실행권을 넘기는 느낌
            }
            
        }

        static void NetworkTask()
        {
            while (true)
            {
                List<ClientSession> sessions =  SessionManager.Instance.GetSessions();
                foreach (ClientSession session in sessions)
                {
                    session.FlushSend();
                }
                Thread.Sleep(0);
            }
        }

        static void StartServerInfoTask()
        {
            var t = new System.Timers.Timer();
            t.AutoReset = true;
            t.Elapsed += new System.Timers.ElapsedEventHandler((s, e) =>
            {
                using (SharedDbContext shared = new SharedDbContext())
                {
                    ServerDb serverDb = shared.Servers.Where(s => s.Name == Name).FirstOrDefault();
                    if (serverDb != null)
                    {
                        serverDb.IpAddress = IpAddress;
                        serverDb.Port = Port;
                        serverDb.BusyScore = SessionManager.Instance.GetBusyScore();
                        shared.SaveChangesEx();
                    }
                    else
                    {
                        serverDb = new ServerDb()
                        {
                            Name = Program.Name,
                            IpAddress = Program.IpAddress,
                            Port = Program.Port,
                            BusyScore = SessionManager.Instance.GetBusyScore()
                        };
                        shared.Servers.Add(serverDb);
                        shared.SaveChangesEx();
                    }
                }
            });

            t.Interval = 10 * 1000;
            t.Start();
        }


        public static string Name { get; } = "춘식서버";
        public static int Port { get; } = 8000;
        public static string IpAddress { get; set; }

        static void Main(string[] args)
        { 
            ConfigManager.LoadConfig();
            DataManager.LoadData();

            GameLogic.Instance.Push(() =>
            {
                GameRoom room = GameLogic.Instance.Add(1);
            });
            
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, Port);

            IpAddress = ipAddr.ToString();

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");


            StartServerInfoTask();

            {
                Thread t = new Thread(DbTask);
                t.Name = "DB";
                t.Start();
            }

            {

                Thread t = new Thread(NetworkTask);
                t.Name = "Network Send";
                t.Start();
            }

            Thread.CurrentThread.Name = "GameLogic";
            GameLogicTask();
        }
    }
}
