using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;

namespace CSL_Mod_Manager
{
    class MainLogic
    {
        private database.Database db;

        public MainLogic()
        {
            db = new database.Database();
        }

        private void InitializeDB()
        {
            if (File.Exists(@"database.sqlite"))
            {
                db.CloseDB();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(@"database.sqlite");
                db = new database.Database();
            }
            db.CreateTable();
        }

        public void RefreshDB(string workshopLocation)
        {
            InitializeDB();  // drop db
            //string workshopLocation = db.GetWorkshopLocation();
            try
            {
                DirectoryInfo di = new DirectoryInfo(workshopLocation);

                // Get only subdirectories that contain the letter "p."
                DirectoryInfo[] dirs = di.GetDirectories("*");

                foreach (DirectoryInfo diNext in dirs)
                {
                    db.InsertNewMod(long.Parse(diNext.Name));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            //db.InsertNewMod();
        }
        public DataTable GetDB()
        {
            DataTable dt = new DataTable();
            db.GetMods(dt);
            return dt;
        }

        public void DownloadandAnalyse(string[] ids, string localPath)
        {
            // https://bbs.csdn.net/topics/220064751
            WebClient wc = new WebClient();
            wc.Credentials = CredentialCache.DefaultCredentials;

            int l = ids.Length;
            for (int i = 0; i < l; i++)
            {
                string PageUrl = String.Format("https://steamcommunity.com/sharedfiles/filedetails/?id={0}&searchtext=", ids[i]);
                Byte[] pageData = wc.DownloadData(PageUrl);
                string idHtml = Encoding.UTF8.GetString(pageData);

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(idHtml);

                HtmlNode TitleNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemTitle\"]");
                string Title = TitleNode.InnerText;
                HtmlNode TagsNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopTagsTitle\"]");
                string Tags;
                if (TagsNode != null) { Tags = TagsNode.InnerText; } else { Tags = ""; }
                HtmlNode DescriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemDescription\"]");
                string Description = DescriptionNode.InnerText;
                HtmlNode Screenshot_holderNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"screenshot_holder\"]");
                HtmlNode ScreenshotNode = Screenshot_holderNode.FirstChild.NextSibling;
                string Screenshot = ScreenshotNode.Attributes["onclick"].Value;
                string pattern = @"https://steamuserimages-a.akamaihd.net/ugc/[\w./]+";

                MatchCollection matchcollection = Regex.Matches(Screenshot, pattern);
                if (matchcollection.Count == 1)
                {
                    Screenshot = matchcollection[0].Value;
                }
                else
                {
                    Screenshot = "";
                }
                //foreach (Match match in Regex.Matches(Screenshot, pattern))
                //    Console.WriteLine(match.Value);

                db.UpdateMod(ids[i], Title, Tags, Description, Screenshot);

                AsyncPic AP = new AsyncPic();
                AP.ImgURL = Screenshot;
                AP.fileName = ids[i] + ".jpg";
                AP.localPath = localPath + "\\Image\\";

                Thread t = new Thread(new ThreadStart(AP.AsyncDownloadPic));
                t.Start();
            }

            wc.Dispose();

        }

    }

    class AsyncPic
    {
        // https://www.cnblogs.com/chongyao/p/6484905.html
        // http://www.cnblogs.com/downmoon/articles/1217269.html
        public string fileName, ImgURL, localPath;

        public void AsyncDownloadPic()
        {
            WebClient wc = new System.Net.WebClient();
            string filePath = this.localPath + this.fileName;
            if (File.Exists(filePath)) { return; }
            if (Directory.Exists(this.localPath) == false) { Directory.CreateDirectory(this.localPath); }
            wc.DownloadFile(this.ImgURL, filePath);
        }

        public static bool IsFileInUse(string fileName)
        {
            bool inUse = true;

            FileStream fs = null;
            try
            {

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,

                FileShare.None);

                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)

                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }
    }
}
