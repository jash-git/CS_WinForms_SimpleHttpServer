using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.IO;
using static System.Net.WebRequestMethods;

namespace CS_WinForms_SimpleHttpServer
{
    /***********************************************************************
     *  HttpServer 類別
     *
     *  實作所有 Http Server 基本服務框架(將TcpListener 封裝成簡易 Http class)
     *  
     *  函數: 
     *      HttpServer - 建構子
     *      Start - 底動監聽並在被存取時回應對應資訊
     *      Stop - 停止監聽動作
     *      HttpHeadPase - Http Head 分析
     *      HttpBodyCreate - 產生對應回應資料(呼叫對應 HttpAPI)
     ***********************************************************************/
    public class HttpServer
    {
        public bool blnRun;
        private TcpListener listener;
        private int intport;
        
        public HttpServer(int port)
        {
            HttpAPI.Init();
            intport = port;
            try
            {
                //IPAddress IPAddr = IPAddress.Parse("0.0.0.0");//所有介面監聽
                this.listener = new TcpListener(IPAddress.Any,port);//new TcpListener(IPAddr,port);
            }
            catch (Exception ex)
            {
                this.listener = null;
                LogFile.Write($"HttpServer Constructor Error:{ex.ToString()}");
            }
        }

        public void Stop()
        {
            blnRun = false;
            if (this.listener != null)
            {
                blnRun = false;
                this.listener.Stop();
                this.listener = null;
            }
        }

        public void Start()
        {
            try
            {              
                this.blnRun = true;
                HttpServerThread.State = 1;
                this.listener.Start();//啟動監聽
                LogFile.Write($"Server Start(Listen port:{intport})");
            }
            catch (Exception ex)
            {
                this.blnRun = false;
                HttpServerThread.State = 1;
                this.listener = null;
                LogFile.Write($"Server Start(Listen port:{intport}) Error:{ex.ToString()}");
                return;
            }

            while (blnRun)
            {
                TcpClient client;
                try
                {
                    client = this.listener.AcceptTcpClient();//接受連線請求
                    //ThreadPool.SetMinThreads(2, 2);//參考: https://test.nizarium.com/httpserver/
                    HttpClientResponse(client);//ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);//處理連線請求
                }
                catch (Exception ex)
                {
                    if (blnRun)
                    {
                        LogFile.Write($"Server Error:{ex.ToString()}");
                    }
                    else
                    {
                        LogFile.Write($"Server STOP");
                        break;
                    }
                }
            }
        }

        /****************************************************************************
         * ClientThread
         * 
         * 功能: 處理連線請求(實際API命令分析和對應資訊回饋)(ThreadPool Mode)，直接呼叫 HttpClientResponse
        ****************************************************************************/
        public void ClientThread(Object StateInfo)
        {
            TcpClient client = (TcpClient)StateInfo;
            HttpClientResponse(client);
        }

        /****************************************************************************
         * HttpClientResponse
         * 
         * 功能: 處理連線請求(實際API命令分析和對應資訊回饋)
         * 
         * 輸入:
         *      client - 連線元件
         * 
         * 輸出:
         *     回復命令結果(true/false)        
         *****************************************************************************/
        public bool HttpClientResponse(TcpClient client)
        {
            bool blnResult = false;
            String StrType = "";//存放輸入資訊拆結果
            String StrPath = "";
            String StrVar = "";
            String StrInput = "";
            String StrLogData = "";//Log資訊占存

            if (client == null)
            {
                LogFile.Write("Get TcpClient Object Error");
                return blnResult;
            }

            byte[] buffer;
            String incomingMessage = "";
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                LogFile.Write($"Get TcpClient's NetworkStream Error:{ex.ToString()}");
                return blnResult;
            }
            
            if (stream == null)
            {
                LogFile.Write("Get TcpClient Error:NetworkStream NULL");
                return blnResult;
            }


            if (stream.CanRead)
            {
                //---
                //抓取此次連線所有輸入資訊
                buffer = new byte[client.ReceiveBufferSize];
                int numBytesRead = 0;
                try
                {
                    numBytesRead = stream.Read(buffer, 0, (int)client.ReceiveBufferSize);
                }
                catch (Exception ex)
                {
                    LogFile.Write($"TcpClient's NetworkStream Read Error:{ex.ToString()}");
                    return blnResult;
                }

                byte[] bytesRead = new byte[numBytesRead];
                Array.Copy(buffer, bytesRead, numBytesRead);
                incomingMessage = Encoding.UTF8.GetString(bytesRead, 0, numBytesRead);//HTTP head + data
                string[] strs = incomingMessage.Split("\n");
                if((strs==null) || (strs.Length<1))
                {
                    LogFile.Write("TcpClient's NetworkStream Data Error");
                    return blnResult;
                }
                //---抓取此次連線所有輸入資訊

                StrLogData = $"IP : {GetRemoteIP(client)}\n";//紀錄連線IP

                //---
                //拆解API輸入參數
                if (HttpHeadPase(strs[0], ref StrType, ref StrPath, ref StrVar))
                {
                    StrLogData += $"Type: {StrType}\n";
                    StrLogData += $"Path: {StrPath}\n";
                    StrLogData += $"Var: {StrVar}\n";
                }
                else
                {
                    LogFile.Write("Http Head Pase Error");
                    return blnResult;
                }

                StrInput = strs[strs.Length - 1];//Input
                //---拆解API輸入參數

                StrLogData += $"Input: {StrInput}\n";

                //---
                //產生API回應對應內容
                String StrResult = "";
                String HTTPStatusCode = "";
                if(HttpBodyCreate(StrType, StrPath,StrInput, ref StrResult))
                {
                    HTTPStatusCode = "HTTP/1.1 200 OK";
                }
                else
                {
                    HTTPStatusCode = "HTTP/1.1 403 Forbidden";
                }
                //---產生API回應對應內容

                //---
                //輸出API回應對應內容
                stream.Write(
                    Encoding.UTF8.GetBytes(
                        HTTPStatusCode + Environment.NewLine
                        + "Content-Type: " + "application / json; charset=UTF-8" + Environment.NewLine
                        + "Content-Length: " + Encoding.UTF8.GetBytes(StrResult).Length + Environment.NewLine
                        + Environment.NewLine
                        + StrResult
                        + Environment.NewLine + Environment.NewLine));
                //---產生API回應對應內容

                StrLogData += $"result: {StrResult}\n";
                LogFile.Write(StrLogData);

                blnResult = true ;

            }//if (stream.CanRead)
            else
            {
                LogFile.Write("TcpClient Stream Access Error");
                return blnResult;
            }

            return blnResult;
        }

        /****************************************************************************
         * HttpHeadPase
         * 
         * 功能:
         *     拆解命令格式
         *     
         * 輸入:
         *     StrData 輸入字串
         *     
         * 輸出:
         *     回復命令結果(true/false)
         *     StrType - HTTP類型:get/post/delete/put
         *     StrPath - 呼叫API名稱
         *     StrVar - HTTP版本:1.0/1.1
         ****************************************************************************/
        public bool HttpHeadPase(String StrData,ref String StrType, ref String StrPath, ref String StrVar)
        {
            bool blnResult = false;
            if((StrData!=null)&&(StrData.Length>0))
            {
                //---
                //HTTP head拆解
                MatchCollection matches = Regex.Matches(StrData, @"(([\S])*?)\s+");//使用正則表達是用空格作為分隔
                int i = 0;
                String StrBuf = "";
                foreach (Match match in matches)
                {
                    switch (i)
                    {
                        case 0:
                            StrType = $"{match.Groups[1].Value}".ToLower();
                            break;
                        case 1:
                            StrPath = $"{match.Groups[1].Value}".ToLower();
                            break;
                        case 2:
                            StrVar = $"{match.Groups[1].Value}".ToLower();
                            break;
                    }
                    i++;
                }
                //---HTTP head拆解
                blnResult = true;
            }
            else
            {
                blnResult = false;
            }

            return blnResult;
        }

        /****************************************************************************
         * HttpBodyCreate
         * 
         * 功能:
         *     產生API對應實際的回應資料
         *     
         * 輸入:
         *     StrType - HTTP類型:get/post/delete/put
         *     StrPath - 呼叫API名稱
         *     StrVar - HTTP版本:1.0/1.1
         *     StrInput - 呼叫對應API的輸入參數    
         *     
         * 輸出:
         *     回復命令結果(true/false)
         *     StrResult - 實際回應字串內容
         ****************************************************************************/
        public static bool HttpBodyCreate(String StrType, String StrPath, String StrInput, ref String StrResult)
        {
            bool blnResult = false;
            if((StrType.Length>0) && (StrPath.Length>0))
            {
                switch(StrPath)
                {
                    case "/orderno":
                        blnResult = HttpAPI.orderno(StrType, ref StrResult);
                        break;
                    default:
                        blnResult = false;
                        break;
                }

            }
            return blnResult;
        }

        //---
        //得到用戶IP和PORT
        //參考: https://blog.51cto.com/yerik/493795
        public Socket GetSocket(TcpClient cln)
        {
            Socket s = cln.Client;
            return s;
        }

        public string GetRemoteIP(TcpClient cln)
        {
            string ip = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[0];
            return ip;
        }

        public int GetRemotePort(TcpClient cln)
        {
            string temp = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[1];
            int port = Convert.ToInt32(temp);
            return port;
        }
        //---得到用戶IP和PORT
    }

    /***********************************************************************
     *  HttpServerThread 類別
     *
     *  啟動/停止 HttpServer 並且載啟動不要咬住UI操作
     *  
     *  函數: Start() 啟動服務
     *        Stop() 停止服務
     *        Iint() 啟動服務變數初始化
     ***********************************************************************/
    public class HttpServerThread
    {
        private static HttpServer Server = null;
        private static int Port;
        private static Thread t;
        public static int State;//紀錄 HttpServer 運行狀態

        /***********************************************************************
         *  函數:Start()
         *  
         *  功能: 啟動HTTP服務
         *  
         *  輸入: 
         *      port 該HTTP服務監聽埠
         *      
         *  輸出:
         *      回復命令結果(true/false)
         ***********************************************************************/
        public static bool Start(int port)
        {
            if(t!=null)
            {
                t = null;
            }
            if(Server != null)
            {
                Server.Stop();
                Server = null;
            }

            State = 0;
            bool blnResult = false;
            t = new Thread(Init);
            t.IsBackground = true;//確保該執行序會在主程序關閉時跟著結束， 參考: https://learn.microsoft.com/zh-tw/dotnet/api/system.threading.thread.isbackground?view=net-7.0
            t.Start(port);

            do
            {
                Thread.Sleep(10);//10ms 等待HttpServer啟動
                if (Server != null)
                {
                    blnResult = Server.blnRun;//取得HttpServer狀態
                }
            } while (State == 0);//HttpServer狀態有效性防呆

            return blnResult;
        }

        public static void Init(object arg)//Run(object arg)
        {
            Port = (int)arg;
            Server = null;
            Server = new HttpServer(Port);
            Server.Start();
        }

        /***********************************************************************
         *  函數:Stop()
         *  
         *  功能: 停止HTTP服務
         *  
         *  輸入: 
         *      無
         *      
         *  輸出:
         *      回復命令結果(true/false)
         ***********************************************************************/
        public static bool Stop()
        {
            if(Server != null)
            {
                Server.Stop();
                Server = null;
            }
            return true;
        }
    }

    /***********************************************************************
     *  HttpAPI 類別
     *
     *  實作所有對應API資料處理功能
     *  
     *  函數: Init() 初始化相關變數
     ***********************************************************************/
    public class HttpAPI
    {
        private static int NO;
        public static void Init()
        {
            NO = 0;
        }

        //---
        //單純只針對非英數字(中文字)部分轉Unicode編碼
        private static string StringToUnicode(string text)
        {
            //參考: https://www.cnblogs.com/sntetwt/p/11218087.html
            string result = "";
            for (int i = 0; i < text.Length; i++)
            {
                if ((int)text[i] > 32 && (int)text[i] < 127)
                {
                    result += text[i].ToString();
                }
                else
                    result += string.Format("\\u{0:x4}", (int)text[i]);
            }
            return result;
        }
        //---單純只針對非英數字(中文字)部分轉Unicode編碼

        public static bool orderno(String StrType, ref String StrResult)
        {
            bool blnResult = false;
            if (StrType == "get")
            {
                blnResult = true;
                StrResult = StringToUnicode(String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO));
                NO++;
            }
            return blnResult;
        }
    }
}
