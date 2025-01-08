using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_BSA_Import
{
    public sealed class CredentialManager
    {
        public string log_path {  get; set; }
        public string destination_path { get; set; }
        public string error_path { get; set; }
        public string file_path { get; set; }
        public string dataset { get; set; }
        
        private static readonly CredentialManager instance = new CredentialManager();

        static CredentialManager() { }

        private CredentialManager()
        {   
            this.dataset = DateTime.Now.ToString("yyyyMMdd");
            using (FileStream fileStream = new FileStream(@"C:\Users\ZemingLiu\source\repos\Empire_BSA_Import\Publish\Empire_BSA_Upload.conf", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                log_path = reader.ReadLine();
                destination_path = reader.ReadLine();
                error_path = reader.ReadLine();
                file_path = reader.ReadLine();
            }

        }

        public static CredentialManager Instance
        {
            get { return instance; }
        }
    }
}
