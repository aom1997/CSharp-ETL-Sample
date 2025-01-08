using Empire_BSA_Import;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Empire_BSA_Import
{
    /* LogManager Class has dependencies on 
          * 1. The main service (requires the delay in seconds and the lastLogSentTime variables) 
          * 2. The SendMail class to be able to send the emails
          * 
          * TO USE THE LOGMANAGER CLASS: LogManager.Instance.Log(errormessage,[booleanError])
          */

    public sealed class LogManager
    {
        /* SINGLETON */
        /* Creating singleton patter so only instances of LogManager can be obtained */
        private static readonly int delayBetweenEmails = 3600; // In seconds
        private static DateTime lastLogSentTime = DateTime.Now.AddSeconds(0 - delayBetweenEmails);

        private static readonly LogManager instance = new LogManager();
        static LogManager() { }
        private LogManager() { }
        public static LogManager Instance
        {
            get { return instance; }
        }

        /* Only the class itself is allowed to update log pathing */
        private readonly static string logPath = CredentialManager.Instance.log_path + typeof(LogManager).Namespace + @"_Logfiles\";
        private readonly string extension = @".log";
        private string fileName;

        public void Log(string message, bool error = false, bool enforce = false)
        {
            CreateLogPath();
            CreateLogFile();

            if (error)
                message = "ERROR: " + message;
            if (enforce)
                message = "PRIORITY ALERT: " + message;

            WriteToFile(message);

            if ((error && DateTime.Compare(DateTime.Now, lastLogSentTime.AddSeconds(delayBetweenEmails)) >= 0) || enforce)
            {
                if (error)
                    lastLogSentTime = DateTime.Now;
                SendMail sm = new SendMail();
                ////derrell change to true when ready to publish 
                if (true)
                    sm.SendErrorMail(message);
            }

        }

        /* If it doesnt exist we create the path for the log*/
        private void CreateLogPath()
        {
            System.IO.Directory.CreateDirectory(logPath);
        }

        /* We create a new file everyday */
        private void CreateLogFile()
        {
            string fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + typeof(LogManager).Namespace + "_logfile";

            if (this.GetFileName() != fileName)
                this.SetFileName(fileName);

            if (!File.Exists(GetFullLogPath()))
            {
                using (StreamWriter sw = File.CreateText(GetFullLogPath()))
                {
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + " - LOG FILE CREATED ");
                }
            }
        }

        private void WriteToFile(string message)
        {
            using (StreamWriter sw = File.AppendText(GetFullLogPath()))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + " - " + message);
            }
        }


        public string GetFullLogPath()
        {
            return (logPath + this.GetFileName() + extension);
        }

        /* NOT USED 
        public string DuplicateLog()
        {
            string dupPath = logPath + GetFileName() + "_v2" + extension;
            if (File.Exists(dupPath))
                RemoveLogFile(dupPath);
            File.Copy(GetFullLogPath(), dupPath);
            return dupPath;
        }

        public void RemoveLogFile(string path)
        {
            File.Delete(path);
        }
        */

        private string GetFileName()
        {
            return fileName;
        }

        private void SetFileName(string fileName)
        {
            this.fileName = fileName;
        }

        /*
        public int GetDelayBetweenEmails()
        {
            return delayBetweenEmails;
        }
        */
    }
}
