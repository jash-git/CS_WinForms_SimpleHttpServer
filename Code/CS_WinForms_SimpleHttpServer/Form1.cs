using System;
using System.Diagnostics;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace CS_WinForms_SimpleHttpServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "Ver : " + FileVersionInfo.GetVersionInfo(Assembly.
                GetExecutingAssembly().Location).FileVersion + string.Format("Version {0}", FileVersionInfo.
                GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString()); ;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(HttpServerThread.Run);
            t.IsBackground = true;
            t.Start(8001);
            Thread.Sleep(1000);
            if(HttpServerThread.server!=null)
            {
                MessageBox.Show("OK");
            }
            else
            {
                MessageBox.Show("Fail");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            HttpServerThread.Stop();
            if (HttpServerThread.server == null)
            {
                MessageBox.Show("OK");
            }
            else
            {
                MessageBox.Show("Fail");
            }
        }
    }
}