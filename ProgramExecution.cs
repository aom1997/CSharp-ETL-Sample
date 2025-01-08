using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.Data.SqlTypes;
using CsvHelper.Configuration.Attributes;
using System.Data.Entity.Core.Objects;

namespace Empire_BSA_Import
{
    internal class ProgramExecution
    {
        
        public static HashSet<string> destinationfolders;//destination folders
        public static HashSet<string> sourcefolders;//source folders
        public static HashSet<string> errorfolders;//error folders
        public static HashSet<string> source;//source files

        CredentialManager cm = CredentialManager.Instance;
        LogManager lm = LogManager.Instance;

        int mybatchsizes = 100000;//import every 100k rows

        public void RunApp()
        {
            int finishedok;

            while (true)
            {
                try
                {
                    lm.Log("Checking for new files....");
                    destinationfolders = new HashSet<string>(Directory.GetDirectories(cm.destination_path));
                    errorfolders = new HashSet<string>(Directory.GetDirectories(cm.error_path));
                    sourcefolders = new HashSet<string>(Directory.GetDirectories(cm.file_path));
                    Console.WriteLine(sourcefolders.Count + " folders found in directory.");
                    lm.Log(sourcefolders.Count + " folders found in directory.");   

                    //return all the subfolders not been processed before
                    HashSet<string> difference = new HashSet<string>();
                    foreach (string subfolder in sourcefolders)
                    {
                        string subfolderName = subfolder.Substring(subfolder.LastIndexOf(@"\") + 1, subfolder.Length - (subfolder.LastIndexOf(@"\") + 1));
                        bool found = false;

                        foreach (string destination_folder in destinationfolders)
                        {
                            if (destination_folder == cm.destination_path + subfolderName)
                            {
                                found = true;
                                Console.WriteLine(subfolderName + " was found in destination folder. Will not be processed");
                                break;
                            }
                        }

                        foreach (string error_folder in errorfolders)
                        {
                            if (error_folder == cm.error_path + subfolderName)
                            {
                                found = true;
                                Console.WriteLine(subfolderName + " was found in error folder. Will not be processed");
                                break;
                            }
                        }

                        if (found == false)
                        {
                            difference.Add(subfolder);
                        }
                    }
                    Console.WriteLine(difference.Count + " new folders found for processing.");
                    lm.Log(difference.Count + " new folders found for processing.");

                    //process different folders
                    foreach (string diff_subfolder in difference)
                    {
                        finishedok = 0;//check how many files has been processed                    
                        source = new HashSet<string>(Directory.GetFiles(diff_subfolder).Select(f => Path.GetFileName(f)));//source: all the filename in each subfolder
                        cm.dataset = "D" + diff_subfolder.Substring(diff_subfolder.LastIndexOf(@"\") + 3, diff_subfolder.Length - (diff_subfolder.LastIndexOf(@"\") + 3));//dataset is the folder name

                        Console.WriteLine("processing subfolder : " + diff_subfolder);
                        lm.Log("processing subfolder : " + diff_subfolder);
                        Console.WriteLine("dataset value = " + cm.dataset);
                        lm.Log("dataset value = " + cm.dataset);

                        if (source.Count >= 2)
                        {                           
                            foreach (string s in source) //processing each file
                            {
                                string file = diff_subfolder + @"\" + s;
                                Console.WriteLine("Processing "+ file);
                                lm.Log("Processing " + file);
                                if (file.Contains("EACO_ACOInsights_Extract_Practices"))
                                {
                                    int c = chkForDataset(cm.dataset, "EMPACO_ACOInsights_DataExtraction_Practices");
                                    if (c == 0) //dataset not in db
                                    {
                                        Console.WriteLine("Dataset " + cm.dataset + " not in DB, start importing process");
                                        lm.Log("Dataset " + cm.dataset + " not in DB, start importing process");
                                        if (ParseFile_DE_Prac(file))
                                        {
                                            finishedok++;
                                        }
                                    }
                                    else if (c == 1) //dataset in db
                                    {
                                        Console.WriteLine("dataset: " + cm.dataset + " already exists in table: EMPACO_ACOInsights_DataExtraction_Practices - no insertion will be done.");
                                        lm.Log("dataset: " + cm.dataset + " already exists in table: EMPACO_ACOInsights_DataExtraction_Practices - no insertion will be done.");
                                    }
                                    else if (c == -1) //error
                                    {
                                        Console.WriteLine("Error locating dataset in database");
                                        lm.Log("Error locating dataset in database");
                                    }
                                }

                                else if (file.Contains("EACO_ACOInsights_Extract_Providers"))
                                {
                                    int c = chkForDataset(cm.dataset, "EMPACO_ACOInsights_DataExtraction_Providers");
                                    if (c == 0)
                                    {
                                        if (ParseFile_DE_Prov(file))
                                        {
                                            finishedok++;
                                        }
                                    }
                                    else if (c == 1)
                                    {
                                        Console.WriteLine("dataset: " + cm.dataset + " already exists in table: EMPACO_ACOInsights_DataExtraction_Practices - no insertion will be done.");
                                        lm.Log("dataset: " + cm.dataset + " already exists in table: EMPACO_ACOInsights_DataExtraction_Practices - no insertion will be done.");
                                    }
                                    else if (c == -1)
                                    {
                                        Console.WriteLine("Error locating dataset in database");
                                        lm.Log("Error locating dataset in database");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(file + " is not the target file, not been processed");
                                }

                            }
                        }

                        if (finishedok >= 2)
                        {
                            Console.WriteLine("folder processing finished ok, moving to destination folder");
                            copydirectory(diff_subfolder, cm.destination_path);
                            Console.WriteLine("folder " + diff_subfolder + " processing finished ok.  file was moved to destinaiton folder");
                            lm.Log("folder " + diff_subfolder + " processing finished ok.  file was moved to destinaiton folder");
                        }
                        else
                        {
                            copydirectory(diff_subfolder, cm.error_path);
                            Console.WriteLine("folder " + diff_subfolder + " processing finished with errors.  file moved to error folder");
                            lm.Log("folder " + diff_subfolder + " processing finished with errors.  file moved to error folder");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lm.Log("\n Exception in while (true).  No files copied after error.  Error code: " + ex.ToString());
                    Console.WriteLine(ex.ToString());
                }

                Console.WriteLine("Wait for an hour");
                if (true)
                {
                    return;
                }
            }
        }

        public static void copydirectory(string root, string destination)
        {
            string foldername = root.Substring(root.LastIndexOf(@"\"), root.Length - root.LastIndexOf(@"\"));
            Directory.CreateDirectory(destination+foldername);
            foreach (var directory in Directory.GetDirectories(root))
            {
                var newDirectory = Path.Combine(destination + foldername, Path.GetFileName(directory));
                Directory.CreateDirectory(newDirectory);
                copydirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                File.Copy(file, Path.Combine(destination + foldername, Path.GetFileName(file)));
            }
        }

        private int chkForDataset(string dtDataset, string ssqltabletocheck)
        {
            try
            {

                SqlConnection sqlconn = new SqlConnection(ConfigurationManager.ConnectionStrings["temp_Zeming"].ConnectionString);
                sqlconn.Open();
                string sqlck = "select count(*)  FROM [BSA].[DBO].[" + ssqltabletocheck + "] where dataset = '" + dtDataset + "'";
                using (SqlCommand sqlcmd = new SqlCommand(sqlck, sqlconn))
                {
                    sqlcmd.CommandTimeout = 0; //unlimited wait time
                    int mycount = (int)sqlcmd.ExecuteScalar(); //first col firt row value
                    if (mycount > 0)
                    {
                        sqlconn.Close();
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        
            catch (Exception ex)
            {
                return -1;
            }
        }

        private bool UploadToDB<T>(List<T> listToUpload, string sqlDBname)
        {
            int batchSize = 150000;
            try
            {
                for (int i = 0; i < listToUpload.Count; i += batchSize)
                {
                    int toAdd = 0;
                    int pendingToAdd = listToUpload.Count - i;
                    if (pendingToAdd > batchSize)
                        toAdd = batchSize;
                    else
                        toAdd = pendingToAdd;

                    DataTable dt = ListToDataTable.ToDataTable(listToUpload.GetRange(i, toAdd));
                    using (var sqlBulk = new SqlBulkCopy(ConfigurationManager.ConnectionStrings["temp_zeming"].ConnectionString))
                    {
                        sqlBulk.BulkCopyTimeout = 0;
                        sqlBulk.DestinationTableName = "[BSA].[dbo].[" + sqlDBname + "]";
                        sqlBulk.WriteToServer(dt);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("An error ocurred in the UploadToDatabase function." + ex);
                lm.Log("An error ocurred in the UploadToDatabase function." + ex);
                return false;
            }
        }

        private bool ParseFile_DE_Prac(string file)
        {
            CredentialManager cm = CredentialManager.Instance;

            List<EMPACO_ACOInsights_DataExtraction_Practices> DE_Prac = new List<EMPACO_ACOInsights_DataExtraction_Practices>();
            string filename = file.Substring(file.LastIndexOf(@"\") + 1, file.Length - (file.LastIndexOf(@"\") + 1));
            Console.WriteLine("Processing file: " + filename);
            lm.Log("Processing file: " + filename);

            int batch = 0;
            int rownum = 0;
            string[] seperator = { "\",", "\"" };// ", and "

            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();//grt rid of header

                while (!sr.EndOfStream)
                {
                    rownum++;
                    batch++;

                    string entry = sr.ReadLine();
                    List<string> fields = entry.Split(seperator, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (fields.Count == 18)
                    {
                        DE_Prac.Add(DataParser.ParseFields_DE_Prac(fields, filename, cm.dataset));
                    }
                    else
                    {
                        lm.Log("Incorrect number of fields on row: " + rownum + " fieldcount = " + fields.Count);
                    }

                    if (batch >= mybatchsizes)//import every 100,000 rows
                    {
                        lm.Log("Current inserted " + (rownum - batch) + " rows. Now inserting " + DE_Prac.Count + " new rows");//the first batch will show 0, consider using 
                        Console.WriteLine("Current inserted " + (rownum - batch) + " rows. Now inserting " + DE_Prac.Count + " new rows.");
                        batch = 0;

                        bool ok = UploadToDB(DE_Prac, "EMPACO_ACOInsights_DataExtraction_Practices");
                        if (!ok) { return false; }
                        DE_Prac.Clear();
                    }
                }

                if (DE_Prac.Count != 0) // insert raminning data
                {
                    lm.Log("Currently inserted " + rownum + " rows. Now inserting " + DE_Prac.Count + " new rows.");
                    Console.WriteLine("Currently inserted " + rownum + " rows. Now inserting " + DE_Prac.Count + " new rows.");                      
                    bool ok = UploadToDB(DE_Prac, "EMPACO_ACOInsights_DataExtraction_Practices");
                    if (!ok) { return false; }
                    DE_Prac.Clear();
                }

                lm.Log("The file" + filename + "has been processed successfully");
                Console.WriteLine("The file" + filename + "has been processed successfully");      
                return true;
                
            }
        }


        private bool ParseFile_DE_Prov(string file)
        {
            CredentialManager cm = CredentialManager.Instance;

            List<EMPACO_ACOInsights_DataExtraction_Providers> DE_Prov = new List<EMPACO_ACOInsights_DataExtraction_Providers>();
            string filename = file.Substring(file.LastIndexOf(@"\")+1, file.Length - (file.LastIndexOf(@"\") + 1));
            Console.WriteLine("Processing file: " + filename);
            lm.Log("Processing file: " + filename);

            int batch = 0;
            int rownum = 0;
            string[] seperator = { "\",", "\"" };

            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    rownum++;
                    batch++;

                    string entry = sr.ReadLine();
                    List<string> fields = entry.Split(seperator, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (fields.Count == 20)
                    {
                        DE_Prov.Add(DataParser.ParseFields_DE_Prov(fields, filename, cm.dataset));
                    }
                    else
                    {
                        Console.WriteLine("Incorrect number of fields on row: " + rownum + " fieldcount = " + fields.Count);
                        lm.Log("Incorrect number of fields on row: " + rownum + " fieldcount = " + fields.Count);
                    }

                    if (batch >= mybatchsizes)//import every 100,000 rows
                    {
                        lm.Log("Current inserted " + (rownum - batch) + " rows. Now inserting " + DE_Prov.Count + " new rows.");//the first batch will show 0, consider using 
                        Console.WriteLine("Current inserted " + (rownum - batch) + " rows. Now inserting " + DE_Prov.Count + " new rows.");
                        batch = 0;

                        bool ok = UploadToDB(DE_Prov, "EMPACO_ACOInsights_DataExtraction_Providers");
                        if (!ok) { return false; }
                        DE_Prov.Clear();
                    }
                }

                if (DE_Prov.Count != 0) // insert raminning data
                {
                    lm.Log("Currently inserted " + rownum + " rows. Now inserting " + DE_Prov.Count + " new rows.");
                    Console.WriteLine("Currently inserted " + rownum + " rows. Now inserting " + DE_Prov.Count + " new rows.");
                    bool ok = UploadToDB(DE_Prov, "EMPACO_ACOInsights_DataExtraction_Providers");
                    if (!ok) { return false; }
                    DE_Prov.Clear();
                }

                lm.Log("The file" + filename + "has been processed successfully");
                Console.WriteLine("The file" + filename + "has been processed successfully");
                return true;
            }
        }

    }
}

