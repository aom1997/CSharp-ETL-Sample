using Empire_BSA_Import;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Empire_BSA_Import
{
    partial class Empire_BSAImportService : ServiceBase
    {
        ProgramExecution pe = new ProgramExecution();
        LogManager lm = LogManager.Instance;
        CredentialManager cm = CredentialManager.Instance;



        private System.Timers.Timer timer = new System.Timers.Timer();

        private int skippedExecutions = 0; //number of executions skipped due to thread control
        private bool finishedExecution = false; //semaphore for thread control
        private static readonly int minutesStucked = 180; //number of minutes the program is allowed to be stuck on a thread.


        public Empire_BSAImportService(bool RunAsService)
        {
            if (RunAsService)
            {
                InitializeComponent();
            }
            else
            {
                InitiateExecution();
            }
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            lm.Log("STARTING: " + GetType().Namespace, false, true);

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            //timer.Interval = 3600000; //time in miliseconds (12h) 1000ms*60s*60m*1h = 43200000
            //Derrell change to true when ready to publish
            if (true)
            {
                timer.Interval = 21600000;  //derrell: 6 hours in milliseconds
            }
            else
            {
                timer.Interval = 90000; //Derrell: time is now 1.5 mins change after testing
            }
            //Running program
            this.finishedExecution = false;
            InitiateExecution();
            //Enabling the timer
            timer.Enabled = true;
        }


        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            lm.Log("STOPPING " + GetType().Namespace + " .The thread has been stuck for more than " + minutesStucked + " minutes. The service will be restarted right after (expect confirmation email).", false, true);
            EventLog.WriteEntry("Stopping the service", EventLogEntryType.Warning, 1000);

            foreach (Process proc in Process.GetProcessesByName(GetType().Namespace))
            {
                lm.Log("Killing process: " + proc.ToString());
                proc.Kill();
            }
        }




        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            //Derrell: eliminating download hours           if (finishedExecution && (DateTime.Now.Hour == 8 || DateTime.Now.Hour == 20))
            if (finishedExecution)
            {
                skippedExecutions = 0;
                finishedExecution = false;
                InitiateExecution();
            }
            else if (!finishedExecution)
            {
                skippedExecutions++;
                if (TimeSpan.FromMilliseconds(skippedExecutions * timer.Interval) >= TimeSpan.FromMinutes(minutesStucked))
                    StopService();
                else if (TimeSpan.FromMilliseconds(skippedExecutions * timer.Interval) >= TimeSpan.FromMinutes(5))
                    lm.Log("Previous thread has been running for more than 5 minutes. Number of skipped executions: " + skippedExecutions, true);
                else
                    lm.Log("Previous thread still running, skipping execution. Number of skipped executions: " + skippedExecutions);
            }
            else
            {
                lm.Log("Outside of downloading hours (8am or 8pm). Ignoring action.");
            }

        }




        private void InitiateExecution()
        {
            lm.Log("NEW EXECUTION");
            pe.RunApp();
            this.finishedExecution = true;

        }




        private void StopService()
        {
            ServiceController service = new ServiceController(GetType().Namespace);
            timer.Enabled = false;

            TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }


    }
}
