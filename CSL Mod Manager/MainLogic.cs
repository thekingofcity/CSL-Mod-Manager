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
            db.UpdateWorkshopLocation(workshopLocation);
            try
            {
                DirectoryInfo di = new DirectoryInfo(workshopLocation);

                // Get only subdirectories that contain the letter "p."
                DirectoryInfo[] dirs = di.GetDirectories("*");

                foreach (DirectoryInfo diNext in dirs)
                {
                    db.InsertNewMod(long.Parse(diNext.Name), GetFilesSize(diNext));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            //db.InsertNewMod();
        }

        private static long GetFilesSize(DirectoryInfo directoryInfo)
        // https://blog.csdn.net/carlhui/article/details/472330
        {
            long length = 0;
            foreach (FileSystemInfo fsi in directoryInfo.GetFileSystemInfos())
            {
                if (fsi is FileInfo)
                {
                    length += ((FileInfo)fsi).Length;
                }
                else
                {
                    directoryInfo = new DirectoryInfo(fsi.FullName);
                    length += GetFilesSize(directoryInfo);
                }
            }
            return length;
        }

        public void DeleteDirectory(string[] ids)
        {
            int l = ids.Length;
            string id;
            for (int i = 0; i < l; i++)
            {
                id = ids[i];
                string path = db.GetWorkshopLocation() + "\\" + id;
                if (Directory.Exists(path))
                {
                    DelectDir(path);
                    db.DeleteMod(long.Parse(id));
                }
            }
        }

        private static void DelectDir(string srcPath)
        // https://www.cnblogs.com/iamlucky/p/5997865.html
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public DataTable GetDB()
        {
            DataTable dt = new DataTable();
            db.GetMods(dt);
            return dt;
        }

        public DataTable GetSpecificRowsFromDB(string search)
        {
            DataTable dt = new DataTable();
            db.GetSpecificMods(dt, search);
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

                // this item might be deleted
                if (idHtml.IndexOf("error_ctn") > -1) continue;

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(idHtml);

                HtmlNode TitleNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemTitle\"]");
                string Title = TitleNode.InnerText;
                HtmlNode TagsNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopTagsTitle\"]");
                string Tags;
                if (TagsNode != null) { Tags = TagsNode.InnerText; } else { Tags = ""; }
                HtmlNode DescriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemDescription\"]");
                string Description = DescriptionNode.InnerText;

                HtmlNode highlight_player_areaNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"highlight_player_area\"]");
                string Screenshot;
                if (highlight_player_areaNode.ChildNodes.Count > 5)
                {
                    // multiple screenshots
                    HtmlNode Screenshot_holderNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"screenshot_holder\"]");
                    HtmlNode ScreenshotNode = Screenshot_holderNode.FirstChild.NextSibling;
                    Screenshot = ScreenshotNode.Attributes["onclick"].Value;
                }
                else
                {
                    // single screenshot
                    HtmlNode ScreenshotNode = highlight_player_areaNode.FirstChild.NextSibling;
                    if (ScreenshotNode.Name == "div")
                    {
                        ScreenshotNode = highlight_player_areaNode.FirstChild.NextSibling.FirstChild.NextSibling.FirstChild.NextSibling;
                        Screenshot = ScreenshotNode.Attributes["onclick"].Value;
                    }
                    else
                    {
                        Screenshot = ScreenshotNode.Attributes["onclick"].Value;
                    }
                }

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

                //AsyncPic AP = new AsyncPic();
                //AP.ImgURL = Screenshot;
                //AP.fileName = ids[i] + ".jpg";
                //AP.localPath = localPath + "\\Image\\";

                //Thread t = new Thread(new ThreadStart(AP.AsyncDownloadPic));
                //t.Start();

                if (Screenshot != "")
                {
                    DownloadPicArgv dpv;
                    dpv.localPath = localPath + "\\Image\\";
                    dpv.fileName = ids[i] + ".jpg";
                    dpv.ImgURL = Screenshot;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncDownloadPic), dpv);
                }
            }

            wc.Dispose();

        }
        
        struct DownloadPicArgv {
            public string localPath;
            public string fileName;
            public string ImgURL;
        }

        static public void AsyncDownloadPic(object dpv_obj)
        {
            DownloadPicArgv dpv = (DownloadPicArgv)dpv_obj;
            WebClient wc = new System.Net.WebClient();
            string filePath = dpv.localPath + dpv.fileName;
            if (File.Exists(filePath)) { return; }
            if (Directory.Exists(dpv.localPath) == false) { Directory.CreateDirectory(dpv.localPath); }
            wc.DownloadFile(dpv.ImgURL, filePath);
        }

        public bool IsFileInUse(string fileName)
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
