using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace CS_WinForms_SimpleHttpServer
{
    public class HttpServer
    {
        public bool blnRun;
        private TcpListener listener;
        private static int NO;

        public HttpServer(int port)
        {
            try
            {
                this.listener = new TcpListener(port);
            }
            catch
            {
                this.listener = null;
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

        //---
        //單純只針對非英數字(中文字)部分轉Unicode編碼
        static string StringToUnicode(string text)
        {
            //https://www.cnblogs.com/sntetwt/p/11218087.html
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

        //---
        //得到用戶IP和PORT ~https://blog.51cto.com/yerik/493795
        public static Socket GetSocket(TcpClient cln)
        {
            Socket s = cln.Client;
            return s;
        }

        public static string GetRemoteIP(TcpClient cln)
        {
            string ip = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[0];
            return ip;
        }

        public static int GetRemotePort(TcpClient cln)
        {
            string temp = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[1];
            int port = Convert.ToInt32(temp);
            return port;
        }
        //---得到用戶IP和PORT

        public void Start()
        {
            NO = 0;
            try
            {
                this.blnRun = true;
                this.listener.Start();
            }
            catch
            {
                this.blnRun = false;
                this.listener = null;
                return;
            }

            while (blnRun)
            {
                TcpClient client;
                try
                {
                    client = this.listener.AcceptTcpClient();
                    //ThreadPool.QueueUserWorkItem(ThreadProc, client);//https://stackoverflow.com/questions/5339782/how-do-i-get-tcplistener-to-accept-multiple-connections-and-work-with-each-one-i
                }
                catch
                {
                    if(blnRun)
                    {
                        LogFile.Write("Server Error & STOP");
                    }
                    else
                    {
                        LogFile.Write("Server STOP");
                    }
                    break;
                }
                byte[] buffer;
                String incomingMessage = "";
                NetworkStream stream = client.GetStream();
                String StrLogData = "";
                String StrInputData = "";
                if (stream.CanRead)
                {
                    buffer = new byte[client.ReceiveBufferSize];
                    int numBytesRead = 0;
                    try
                    {
                        numBytesRead = stream.Read(buffer, 0, (int)client.ReceiveBufferSize);
                    }
                    catch
                    {
                        client = null;
                        continue;
                    }
                    byte[] bytesRead = new byte[numBytesRead];
                    Array.Copy(buffer, bytesRead, numBytesRead);
                    StrLogData = $"IP : {GetRemoteIP(client)}\n";//紀錄連線IP
                    incomingMessage = Encoding.UTF8.GetString(bytesRead, 0, numBytesRead);//HTTP head + data
                    string[] strs = incomingMessage.Split("\n");
                    //---
                    //HTTP head拆解
                    MatchCollection matches = Regex.Matches(strs[0], @"(([\S])*?)\s+");//使用政則表達是用空格作為分隔
                    int i = 0;
                    String StrBuf = "";
                    foreach (Match match in matches)
                    {
                        switch(i)
                        {
                            case 0:
                                StrBuf = $"Type: {match.Groups[1].Value}\n";
                                break;
                            case 1:
                                StrBuf = $"Path: {match.Groups[1].Value}\n";
                                break;
                            case 2:
                                StrBuf = $"Var: {match.Groups[1].Value}\n";
                                break;
                        }
                        i++;
                        StrLogData += StrBuf;
                    }
                    //---HTTP head拆解
                    StrInputData = strs[strs.Length - 1];
                    StrLogData += $"Input: {StrInputData}\n";
                }

                String result = StringToUnicode(String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO));               
                //String result = String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO);
                
                NO++;

                stream.Write(
                    Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK" + Environment.NewLine
                        + "Content-Type: " + "application / json; charset=UTF-8" + Environment.NewLine
                        + "Content-Length: " + Encoding.UTF8.GetBytes(result).Length + Environment.NewLine
                        + Environment.NewLine
                        + result
                        + Environment.NewLine + Environment.NewLine));

                StrLogData += $"result: {result}\n";
                LogFile.Write(StrLogData);
            }
        }
        private static void ThreadProc(object obj)
        {
            var client = (TcpClient)obj;
            byte[] buffer;
            String incomingMessage = "";
            NetworkStream stream = client.GetStream();
            String StrLogData = "";
            String StrInputData = "";
            if (stream.CanRead)
            {
                buffer = new byte[client.ReceiveBufferSize];
                int numBytesRead = 0;
                try
                {
                    numBytesRead = stream.Read(buffer, 0, (int)client.ReceiveBufferSize);
                }
                catch
                {
                    client = null;
                    return;
                }
                byte[] bytesRead = new byte[numBytesRead];
                Array.Copy(buffer, bytesRead, numBytesRead);
                StrLogData = $"IP : {GetRemoteIP(client)}\n";//紀錄連線IP
                incomingMessage = Encoding.UTF8.GetString(bytesRead, 0, numBytesRead);//HTTP head + data
                string[] strs = incomingMessage.Split("\n");
                //---
                //HTTP head拆解
                MatchCollection matches = Regex.Matches(strs[0], @"(([\S])*?)\s+");//使用政則表達是用空格作為分隔
                int i = 0;
                String StrBuf = "";
                foreach (Match match in matches)
                {
                    switch (i)
                    {
                        case 0:
                            StrBuf = $"Type: {match.Groups[1].Value}\n";
                            break;
                        case 1:
                            StrBuf = $"Path: {match.Groups[1].Value}\n";
                            break;
                        case 2:
                            StrBuf = $"Var: {match.Groups[1].Value}\n";
                            break;
                    }
                    i++;
                    StrLogData += StrBuf;
                }
                //---HTTP head拆解
                StrInputData = strs[strs.Length - 1];
                StrLogData += $"Input: {StrInputData}\n";
            }

            String result = StringToUnicode(String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO));
            //String result = String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO);

            NO++;

            stream.Write(
                Encoding.UTF8.GetBytes(
                    "HTTP/1.1 200 OK" + Environment.NewLine
                    + "Content-Type: " + "application / json; charset=UTF-8" + Environment.NewLine
                    + "Content-Length: " + Encoding.UTF8.GetBytes(result).Length + Environment.NewLine
                    + Environment.NewLine
                    + result
                    + Environment.NewLine + Environment.NewLine));

            StrLogData += $"result: {result}\n";
            LogFile.Write(StrLogData);
        }
    }

    public class HttpServerThread
    {
        public static int Port;
        public static HttpServer server=null;
        public static void Run(object arg)
        {
            Port = (int)arg;
            server = null;
            server = new HttpServer(Port);
            server.Start();
        }
        public static void Stop()
        {
            if(server != null)
            {
                server.Stop();
                server = null;
            }
        }
    }
}
