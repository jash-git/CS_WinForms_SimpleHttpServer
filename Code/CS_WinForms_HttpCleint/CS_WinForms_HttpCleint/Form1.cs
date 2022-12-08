using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace CS_WinForms_HttpCleint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TcpClient tcpClient=new TcpClient();
            try
            {
                tcpClient.Connect("192.168.1.108", 8001);
            }
            catch
            {
                MessageBox.Show("無法連線");
                return;
            }

            using (NetworkStream networkStream = tcpClient.GetStream())
            {

                // request line
                networkStream.Write(Encoding.UTF8.GetBytes("POST /test/post.php HTTP/1.0\r\n"));

                String StrInput = @"<methodCall><methodName>sycgsti.launcher</methodName><params><param><value><string>amFzaGxpYW8=</string></value></param></params></methodCall>";
                // headers
                networkStream.Write(Encoding.UTF8.GetBytes("Host: 192.168.1.108\r\n"));
                networkStream.Write(Encoding.UTF8.GetBytes("Content-Type: text/xml\r\n"));
                networkStream.Write(Encoding.UTF8.GetBytes($"Content-Length: {Encoding.UTF8.GetBytes(StrInput).Length}\r\n"));
                networkStream.Write(Encoding.UTF8.GetBytes("\r\n"));

                // body
                networkStream.Write(Encoding.UTF8.GetBytes($"{StrInput}\r\n"));

                if (networkStream.CanRead)
                {
                    byte[] buffer;
                    buffer = new byte[tcpClient.ReceiveBufferSize];
                    int numBytesRead = networkStream.Read(buffer, 0, (int)tcpClient.ReceiveBufferSize);
                    byte[] bytesRead = new byte[numBytesRead];
                    Array.Copy(buffer, bytesRead, numBytesRead);
                    richTextBox1.Text = Encoding.UTF8.GetString(bytesRead, 0, numBytesRead);
                }
            }
        }
    }
}