using Empire_BSA_Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Empire_BSA_Import
{
    class Program
    {

        static void Main(string[] args)
        {
            //update username and pwd from connection string in app.config [temp.zeming]
            //update path at: Publish\Empire_BSA_Upload.conf
            //search for publish and change all false ifs to true  -done
            //change to true when ready to publish - done
            bool RunAsService = true;
            if (RunAsService)
            {
                ServiceBase[] ServiceToRun;
                ServiceToRun = new ServiceBase[]
                {
                    new Empire_BSAImportService(RunAsService)

                };
                ServiceBase.Run(ServiceToRun);
            }
            else
            {
                new Empire_BSAImportService(RunAsService);
            }
        }
    }
}

