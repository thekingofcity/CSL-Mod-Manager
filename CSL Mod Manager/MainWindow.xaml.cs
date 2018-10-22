using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace CSL_Mod_Manager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // MainLogic ML;
        DataTable dt;
        private database.Database db;

        // Delegates to be used in placking jobs onto the Dispatcher.
        private delegate void AsyncSelectDirUIDelegate(string progress);

        public MainWindow()
        {
            InitializeComponent();
            // ML = new MainLogic();
            InitializeDB(false);
            db = new database.Database();
            RefreshTable();
        }

        private void BtnSelectDir(object sender, RoutedEventArgs e)
        {
            // https://stackoverflow.com/questions/4007882/select-folder-dialog-wpf/17712949#17712949
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select WorkShop Directonary";
            dlg.IsFolderPicker = true;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var workshopLocation = dlg.FileName;

                // https://blog.csdn.net/yl2isoft/article/details/11711833
                Thread newThread = new Thread(new ParameterizedThreadStart(RefreshDB));
                newThread.Start(workshopLocation);
                
                //RefreshDB(workshopLocation);
                //RefreshTable();
            }
        }

        private void BtnRefreshTable(object sender, RoutedEventArgs e)
        {
            RefreshTable();
        }

        private void RefreshTable(string search=null)
        {
            if (search != null)
            {
                dt = GetSpecificRowsFromDB(SeachBox.Text);
                DataGridView1.ItemsSource = dt.AsDataView();
            }
            else
            {
                dt = GetDB();
                DataGridView1.ItemsSource = dt.AsDataView();
            }
        }

        public struct DownloadandAnalyseArgs
        {
            public string[] ids;
            public string localPath;
        }

        private void BtnDownloadandAnalyse(object sender, RoutedEventArgs e)
        {
            // https://www.codeproject.com/Questions/119505/Get-Selected-items-in-a-WPF-datagrid
            int count = DataGridView1.SelectedItems.Count;
            string[] ids = new string[count];
            for (int i = 0; i < count; i++)
            {
                DataRowView SelectedRow = (DataRowView)DataGridView1.SelectedItems[i];
                ids[i] = SelectedRow.Row.ItemArray[0].ToString();
            }

            DownloadandAnalyseArgs args;
            args.ids = ids;
            args.localPath = Environment.CurrentDirectory;
            // https://blog.csdn.net/yl2isoft/article/details/11711833
            Thread newThread = new Thread(new ParameterizedThreadStart(DownloadandAnalyse));
            newThread.Start(args);

            // DownloadandAnalyse(ids, Environment.CurrentDirectory);

            // RefreshTable();
        }

        private void DataGridView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // https://bbs.csdn.net/topics/391924207
            DataRowView SelectedRow = (DataRowView)DataGridView1.SelectedItem;
            if (SelectedRow != null)
            {
                string id = SelectedRow.Row.ItemArray[0].ToString();
                string path = Environment.CurrentDirectory + "\\Image\\" + id + ".jpg";
                if (File.Exists(path) && !IsFileInUse(path))
                {
                    BitmapImage image = new BitmapImage(new Uri(path));
                    Preview.Source = image;
                }
                else
                {
                    Preview.Source = null;
                }
            }
        }

        private void Delete_Directory_Click(object sender, RoutedEventArgs e)
        {
            // https://www.codeproject.com/Questions/119505/Get-Selected-items-in-a-WPF-datagrid
            int count = DataGridView1.SelectedItems.Count;
            string[] ids = new string[count];
            for (int i = 0; i < count; i++)
            {
                DataRowView SelectedRow = (DataRowView)DataGridView1.SelectedItems[i];
                ids[i] = SelectedRow.Row.ItemArray[0].ToString();
            }
            DeleteDirectory(ids);

            RefreshTable();
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/thekingofcity/CSL-Mod-Manager");
        }

        private void SeachBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This func will be invoked before db initialize
            if (SeachBox.Text == "Search Here") return;

            RefreshTable(SeachBox.Text);
        }
        
        private void StatusUpdate(string status)
        {
            Status.Content = status;
        }

        // UI
        // ----------------
        // MainLogic

        
        private void InitializeDB(Boolean overrideDB)
        {
            Boolean fileExist = File.Exists(@"database.sqlite");
            if (overrideDB && fileExist)
            {
                // full initialize
                // delete db file and recreate
                db.CloseDB();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(@"database.sqlite");

                db = new database.Database();
                db.CreateTable();
            }
            else if (!overrideDB && !fileExist)
            {
                // run for the first time
                // create db file and initialize

                db = new database.Database();
                db.CreateTable();
            }
        }

        public void RefreshDB(object workshopLocationObj)
        {
            string workshopLocation = (String)workshopLocationObj;
            InitializeDB(true);  // drop db
            db.UpdateWorkshopLocation(workshopLocation);
            try
            {
                DirectoryInfo di = new DirectoryInfo(workshopLocation);

                // Get only subdirectories that contain the letter "p."
                DirectoryInfo[] dirs = di.GetDirectories("*");

                int ModSize = dirs.Length;
                int index = 0;
                string status;

                foreach (DirectoryInfo diNext in dirs)
                {
                    index++;
                    db.InsertNewMod(long.Parse(diNext.Name), GetFilesSize(diNext));

                    status = index.ToString() + " of " + ModSize.ToString();
                    this.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new AsyncSelectDirUIDelegate(StatusUpdate),
                        status);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(StatusUpdate),
                "All done.");
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(RefreshTable),
                null);
        }

        private static long GetFilesSize(DirectoryInfo directoryInfo)
        {
            // https://blog.csdn.net/carlhui/article/details/472330
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
        {
            // https://www.cnblogs.com/iamlucky/p/5997865.html
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

        public void DownloadandAnalyse(object argsObj)
        {
            DownloadandAnalyseArgs args = (DownloadandAnalyseArgs)argsObj;
            string[] ids = args.ids;
            string localPath = args.localPath;

            // https://www.cnblogs.com/ibeisha/p/threadpool.html
            ThreadPool.SetMinThreads(2, 1);
            ThreadPool.SetMaxThreads(8, 4);

            // https://bbs.csdn.net/topics/220064751
            WebClient wc = new WebClient();
            wc.Credentials = CredentialCache.DefaultCredentials;

            int l = ids.Length;
            string status, lstr = l.ToString();
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

                status = i.ToString() + " of " + lstr;
                this.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new AsyncSelectDirUIDelegate(StatusUpdate),
                    status);

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

            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(StatusUpdate),
                "All done.");
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(RefreshTable),
                null);

        }

        struct DownloadPicArgv
        {
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
