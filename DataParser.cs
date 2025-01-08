using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_BSA_Import
{
    public static class DataParser //Assign value to corresponding column
    {
        public static EMPACO_ACOInsights_DataExtraction_Practices ParseFields_DE_Prac(List<string> fields,string filename, string dataset)
        {
            var ot = new EMPACO_ACOInsights_DataExtraction_Practices();
            int i = 0;

            ot.UniqueKey = Convert.ToInt32(fields[i++]);
            ot.Eligibility = fields[i++];
            ot.TIN = fields[i++];
            ot.Practice = fields[i++];
            ot.MemberSource = fields[i++];
            ot.Covid19Rule = fields[i++];
            ot.SAHS = fields[i++];
            ot.Year = fields[i++];
            ot.Category = fields[i++];
            ot.Metric = fields[i++];
            ot.QTR1 = fields[i++];
            ot.QTR2 = fields[i++];
            ot.QTR3 = fields[i++];
            ot.QTR4 = fields[i++];
            ot.AVG = fields[i++];
            ot.WgtAvg = fields[i++];
            ot.YTD = fields[i++];
            ot.Membership = fields[i++];
            ot.DATASET = dataset;
          
            return ot;
        }

        public static EMPACO_ACOInsights_DataExtraction_Providers ParseFields_DE_Prov(List<string> fields, string filename, string dataset)
        {
            var ot = new EMPACO_ACOInsights_DataExtraction_Providers();
            int i = 0;

            ot.UniqueKey = Convert.ToInt32(fields[i++]);
            ot.Eligibility = fields[i++];        
            ot.TIN = fields[i++];
            ot.Practice = fields[i++];
            ot.NPI = fields[i++];
            ot.Provider = fields[i++];          
            ot.MemberSource = fields[i++];
            ot.Covid19Rule = fields[i++];
            ot.SAHS = fields[i++];      
            ot.Year = fields[i++];
            ot.Category = fields[i++];
            ot.Metric = fields[i++];
            ot.QTR1 = fields[i++];
            ot.QTR2 = fields[i++];
            ot.QTR3 = fields[i++];
            ot.QTR4 = fields[i++];
            ot.AVG = fields[i++];
            ot.WeightedAvg = fields[i++];
            ot.YTD = fields[i++];
            ot.Membership = fields[i++];
            ot.DATASET = dataset;
            return ot;
        }
    }

}
