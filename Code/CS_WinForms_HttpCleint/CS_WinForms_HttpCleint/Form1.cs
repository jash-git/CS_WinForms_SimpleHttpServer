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
                tcpClient.Connect("127.0.0.1", 8001);
            }
            catch
            {
                MessageBox.Show("?L?k?s?u");
                return;
            }

            using (NetworkStream networkStream = tcpClient.GetStream())
            {

                // request line
                networkStream.Write(Encoding.UTF8.GetBytes($"{comboBox1.SelectedItem.ToString()} /orderno HTTP/1.0\r\n"));

                String StrInput = (comboBox1.SelectedItem.ToString()=="POST") ? @"<methodCall><methodName>sycgsti.launcher</methodName><params><param><value><string>amFzaGxpYW8=</string></value></param></params></methodCall>":"";
                // headers
                networkStream.Write(Encoding.UTF8.GetBytes("Host: 127.0.0.1\r\n"));
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

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }
    }
}