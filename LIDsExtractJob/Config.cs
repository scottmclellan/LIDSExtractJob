using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIDsExtractJob
{
    public static class Config
    {
        public static string LidsUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["LidsUrl"];
            }
        }

        public static string DBName
        {
            get
            {
                return ConfigurationManager.AppSettings["DbName"];
            }
        }

        public static string ReportName { get => ConfigurationManager.AppSettings["ReportName"]; }

        public static string DBConnString { get => ConfigurationManager.ConnectionStrings["DBConnString"].ConnectionString; }
        public static string MailTo { get => ConfigurationManager.AppSettings["MailTo"].ToString(); }
        public static string MailFrom { get => ConfigurationManager.AppSettings["MailFrom"].ToString(); }
        public static string SMTPServer { get => ConfigurationManager.AppSettings["SMTPServer"].ToString(); }
        public static string SMTPUserName { get => ConfigurationManager.AppSettings["SMTPUserName"].ToString(); }
        public static string SMTPPassword { get => ConfigurationManager.AppSettings["SMTPPassword"].ToString(); }

        public static int MaxPageSize { get => int.Parse(ConfigurationManager.AppSettings["MaxPageSize"]?.ToString() ?? "240"); }

    }
}
