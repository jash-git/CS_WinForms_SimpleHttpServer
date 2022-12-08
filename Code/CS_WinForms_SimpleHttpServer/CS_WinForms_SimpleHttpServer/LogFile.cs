using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_WinForms_SimpleHttpServer
{
    public class LogFile
    {
        public static void CleanLog(int intDay=30)
        {
            DateTime now = DateTime.Now; // 今天
            String StrLogPath = m_StrSysPath + "Log";

            if (!Directory.Exists(StrLogPath))
            {
                Directory.CreateDirectory(StrLogPath);
            }
            
            DirectoryInfo di = new DirectoryInfo(StrLogPath); // 取得 X:\ 資料夾資訊
            // 從 di 裡找出所有zip檔，且列舉出所有 (今天 - 屬性日期) 超過 N 天的
            //建立日期:p.CreationTime
            //修改日期:p.LastWriteTime
            //存取日期:p.LastAccessTime
            // 用 foreach把上行列舉到的檔案全跑一遍，z 就是每次被列舉到符合條件的zip
            foreach (var z in di.GetFiles("*.*").Where(p => (now - p.LastWriteTime).TotalDays > intDay))
            {
                z.Delete();  // 很好理解，把 z 刪除！
            }
        }

        //public static string m_StrSysPath = System.Windows.Forms.Application.StartupPath;// .net 4X - Forms
        //public static string m_StrSysPath = AppDomain.CurrentDomain.BaseDirectory;// .net 4X - Console
		public static string m_StrSysPath = Directory.GetCurrentDirectory()+"\\";
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();//https://johandorper.com/log/thread-safe-file-writing-csharp
        public static void Write(String StrData, bool blnAutoTime = true)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                String StrLogPath = m_StrSysPath + "Log";
                String StrFileName = StrLogPath + String.Format("\\{0}.log", DateTime.Now.ToString("yyyyMMdd"));

                if (!Directory.Exists(StrLogPath))
                {
                    Directory.CreateDirectory(StrLogPath);
                }

                FileStream fs = new FileStream(StrFileName, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                if (blnAutoTime == true)
                {
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff => ") + StrData);// 寫入文字
                }
                else
                {
                    sw.WriteLine(StrData);// 寫入文字
                }

                sw.Close();// 關閉串流
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }
    }
}
