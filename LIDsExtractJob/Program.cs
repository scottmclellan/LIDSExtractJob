using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LIDsExtractJob.Extensions;
using System.Net;
using System.Net.Mail;
using System.Data.SQLite;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace LIDsExtractJob
{
    class Program
    {
        static void Main(string[] args)
        {

            var currentItems = GetCurrentItems();

            Console.WriteLine($"{currentItems.Count} Items found.");

            var savedItems = GetSavedItems();

            Console.WriteLine($"{savedItems.Count} Items previously saved.");

            var deletedItems = savedItems.Except(currentItems, new ItemComparer()).ToList();

            Console.WriteLine($"{deletedItems.Count} Items to be deleted.");

            var newItems = currentItems.Except(savedItems, new ItemComparer()).ToList();

            Console.WriteLine($"{newItems.Count} Items to be added.");

            var updatedItems = currentItems.Where(x =>
            {
                var match = savedItems.FirstOrDefault(y => y.Id == x.Id);

                return (match != null && match.Price != x.Price);
            }).ToList();

            Console.WriteLine($"{updatedItems.Count} Items to be updated.");

            AddItems(newItems.ToArray());
            DeleteItems(deletedItems.ToArray());
            UpdateItems(updatedItems.ToArray());


            if (newItems.Any() || updatedItems.Any())
            {
                var ItemReport = GetEmailBody(newItems, updatedItems);

                SaveReport(ItemReport);
                //SendEmail(newItemReport);
            }
        }       

        public static List<Item> GetCurrentItems()
        {

            Uri lidsUri = new Uri(Config.LidsUrl);

            var web = new CustomWebClient();

            var html = web.DownloadString(lidsUri);

            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(html);

            //get number of products
            var numberOfProductsElement = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'pagination-bar-result-number')]");

            var numberOfProducts = Regex.Replace(numberOfProductsElement.InnerText, "[^0-9]", ""); 

            var products = htmlDoc.DocumentNode.SelectNodes("//li[contains(@class, 'product-item')]");           

            return products.Select(product =>
            {
                var idElement = product.SelectSingleNode(".//a[@class='js-reference-item']");
                var priceElement = product.SelectSingleNode(".//span[@class='price']");
                var oldPriceElement = product.SelectSingleNode(".//span[@class='oldprice']");
                var imageElement = product.SelectSingleNode(".//img[@class='image-medium']"); 
                var detailPathElement = product.SelectSingleNode(".//a[@class='thumb']");

                var id = idElement.Attributes["data-productcode"].Value.ToInt();
                var price = priceElement.InnerText.ToDecimalFromCurrency();
                var oldPrice = oldPriceElement == null ? price : oldPriceElement.InnerText.ToDecimalFromCurrency();
                var imageUrl = "https:" + imageElement.Attributes["src"].Value;
                var name =  imageElement.Attributes["alt"].Value;
                var detailPath = "https://" + lidsUri.Host + detailPathElement.Attributes["href"].Value;

                return new Item()
                {
                    Id = id,
                    Price = price,
                    OldPrice = oldPrice,
                    ImageUrl = imageUrl,
                    Name = name,
                    DetailPath = detailPath
                };
            }).ToList();                     
        }

        public static List<Item> GetSavedItems()
        {
            List<Item> Items = new List<Item>();

            if (!File.Exists(Config.DBName))
            {
                SQLiteConnection.CreateFile(Config.DBName);
            }

            using (SQLiteConnection conn = new SQLiteConnection(Config.DBConnString))
            {
                conn.Open();

                #region Create Table
                if (!TableExists(conn, "Items"))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand("create table Items (id int, name varchar(200), price real, oldprice real, imageurl varchar(200), detailpath varchar(200), createddate varchar(50), modifieddate varchar(50), deleteddate varchar(50))", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                #endregion

                string sql = "SELECT id, name, price, oldprice, imageurl, detailpath, createddate, modifieddate FROM Items WHERE deleteddate is null;";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                            Items.Add(new Item()
                            {
                                Id = reader["id"].ToString().ToInt(),
                                Name = reader["name"].ToString(),
                                Price = reader["price"].ToString().ToDecimal(),
                                OldPrice = reader["oldprice"].ToString().ToDecimal(),
                                ImageUrl = reader["imageurl"].ToString(),
                                DetailPath = reader["detailpath"].ToString(),
                                CreatedDate = reader["createddate"].ToString().ToDateTime(),
                                ModifiedDate = reader["modifieddate"].ToString().ToDateTime()
                            });
                }
            }

            return Items;
        }

        public static void AddItems(params Item[] newItems)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Config.DBConnString))
            {
                conn.Open();

                foreach (var Item in newItems)
                {
                    string sql = $"INSERT INTO Items (id, name, price, oldprice, imageurl, detailpath, createddate) VALUES ({Item.Id}, '{Item.Name.SQLPrep()}',{Item.Price},{Item.OldPrice},'{Item.ImageUrl.SQLPrep()}','{Item.DetailPath.SQLPrep()}', '{DateTime.Now.ToSQLFormat()}');";

                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }

            }
        }

        public static void DeleteItems(params Item[] deleteItems)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Config.DBConnString))
            {
                conn.Open();

                foreach (var Item in deleteItems)
                {
                    string sql = $"UPDATE Items SET deleteddate = '{DateTime.Now.ToSQLFormat()}' WHERE id = {Item.Id};";

                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }

            }
        }

        public static void UpdateItems(params Item[] updatedItems)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Config.DBConnString))
            {
                conn.Open();

                foreach (var Item in updatedItems)
                {
                    string sql = $"UPDATE Items SET name = '{Item.Name.SQLPrep()}', price = {Item.Price}, oldprice = {Item.OldPrice}, imageurl = '{Item.ImageUrl.SQLPrep()}', detailpath = '{Item.DetailPath.SQLPrep()}', modifieddate = '{DateTime.Now.ToSQLFormat()}' WHERE id = {Item.Id};";

                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }

            }
        }

        public static bool TableExists(SQLiteConnection conn, string tableName)
        {
            string sql = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString() == tableName) return true;
                    }
                }
            }

            return false;
        }

        public static string GetEmailBody(IEnumerable<Item> newItems, IEnumerable<Item> updatedItems)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");

            sb.AppendLine("<style>");
            sb.AppendLine(".title { font-family: Helvetica; font-size:25px; font-weight:bold;}");
            sb.AppendLine(".header { font-family: Helvetica; font-size:20px; font-weight:bold; }");
            sb.AppendLine(".detail { font-family: Helvetica; font-size:15px; vertical-align: text-top;}");
            sb.AppendLine("tr { border-bottom: thick black; }");
            sb.AppendLine("</style>");

            sb.AppendLine("</head>");
            sb.AppendLine("<table>");

            if (newItems.Any())
            {
                sb.AppendLine("<tr class=\"header\"><td colspan=\"4\">NEW</td></tr>");
                sb.AppendLine("<tr class=\"header\"><td>Product Code</td><td>Name</td><td>Price</td><td>Details</td></tr>");

                foreach (var Item in newItems.OrderBy(x => x.Price))
                {
                    sb.AppendLine($"<tr class=\"detail\"><td>{Item.Id}</td><td>{Item.Name}</td><td>{Item.Price:c2} ({Item.OldPrice:c2})</td><td><a href=\"{Item.DetailPath}\"><img src=\"{Item.ImageUrl}\" /></a></td></tr>");
                }
            }

            if (updatedItems.Any())
            {
                sb.AppendLine("<tr class=\"header\"><td colspan=\"4\">UPDATED</td></tr>");

                foreach (var Item in updatedItems.OrderBy(x => x.Price))
                {
                    sb.AppendLine($"<tr class=\"detail\"><td>{Item.Id}</td><td>{Item.Name}</td><td>{Item.Price:c2} ({Item.OldPrice:c2})</td><td><a href=\"{Item.DetailPath}\"><img src=\"{Item.ImageUrl}\" /></a></td></tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static void SendEmail(string body)
        {
            MailMessage mailMsg = new MailMessage();
            mailMsg.To.Add(new MailAddress(Config.MailTo));
            // From
            MailAddress mailAddress = new MailAddress(Config.MailFrom);
            mailMsg.From = mailAddress;

            //Content
            mailMsg.Subject = "New Clearance Items!";
            mailMsg.Body = body;

            //SmtpClient
            SmtpClient smtpConnection = new SmtpClient(Config.SMTPServer, 587);
            smtpConnection.Credentials = new System.Net.NetworkCredential(Config.SMTPUserName, Config.SMTPPassword);
            smtpConnection.UseDefaultCredentials = false;

            smtpConnection.EnableSsl = true;
            smtpConnection.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpConnection.Send(mailMsg);
        }

        public static void SaveReport(string reportBody)
        {
            if (File.Exists(Config.ReportName))
            {
                File.Delete(Config.ReportName);
            }

            File.WriteAllText(Config.ReportName, reportBody);

            Process.Start(Config.ReportName);

        }

    }
}
