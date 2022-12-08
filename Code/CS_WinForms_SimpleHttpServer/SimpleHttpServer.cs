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

namespace CS_WinForms_SimpleHttpServer
{
    public class HttpServer
    {
        public bool blnRun;
        private TcpListener listener;
        private int NO;

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
        //得到用戶IP和PORT ~https://blog.51cto.com/yerik/493795
        Socket GetSocket(TcpClient cln)
        {
            Socket s = cln.Client;
            return s;
        }

        string GetRemoteIP(TcpClient cln)
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

        public void Start()
        {
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
                }
                catch
                {
                    break;
                }

                byte[] buffer;
                String incomingMessage = "";
                NetworkStream stream = client.GetStream();
                if (stream.CanRead)
                {
                    buffer = new byte[client.ReceiveBufferSize];
                    int numBytesRead = stream.Read(buffer, 0, (int)client.ReceiveBufferSize);
                    byte[] bytesRead = new byte[numBytesRead];
                    Array.Copy(buffer, bytesRead, numBytesRead);
                    incomingMessage = GetRemoteIP(client)+"\n" + Encoding.UTF8.GetString(bytesRead, 0, numBytesRead);
                }

                String result = String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO);//@"{""NO"":""001"",""En_Name"":""jash.liao"",""CH_Name"":""小廖""}";
                NO++;

                stream.Write(
                    Encoding.UTF8.GetBytes(
                        "HTTP/1.0 200 OK" + Environment.NewLine
                        + "Content-Length: " + result.Length + Environment.NewLine
                        + "Content-Type: " + "application / json" + Environment.NewLine
                        + Environment.NewLine
                        + result
                        + Environment.NewLine + Environment.NewLine));
                
                LogFile.Write(incomingMessage + ";" + result);
            }
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
