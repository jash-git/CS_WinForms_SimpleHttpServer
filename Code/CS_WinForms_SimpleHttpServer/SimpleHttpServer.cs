using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

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
                var buffer = new byte[10240];
                var stream = client.GetStream();
                var length = stream.Read(buffer, 0, buffer.Length);
                var incomingMessage = String.Format("Client connected with IP {0}", ((IPEndPoint)client.Client.RemoteEndPoint).Address) + "\n" + Encoding.UTF8.GetString(buffer, 0, length);

                var result = String.Format("{{\"NO\":\"{0:0000}\",\"En_Name\":\"jash.liao\",\"CH_Name\":\"小廖\"}}", NO);//@"{""NO"":""001"",""En_Name"":""jash.liao"",""CH_Name"":""小廖""}";
                NO++;
                stream.Write(
                    Encoding.UTF8.GetBytes(
                        "HTTP/1.0 200 OK" + Environment.NewLine
                        + "Content-Length: " + result.Length + Environment.NewLine
                        + "Content-Type: " + "application / json" + Environment.NewLine
                        + Environment.NewLine
                        + result
                        + Environment.NewLine + Environment.NewLine));
                Console.WriteLine("Incoming message: \n{0}", incomingMessage);
                Console.WriteLine("{0}", result);
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
