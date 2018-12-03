using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace CSL_Mod_Manager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public const int DefaultAppid = 255710;
        
        private DataTable _dt;
        public Database _db;

        // Delegates to be used in placing jobs onto the Dispatcher.
        private delegate void AsyncSelectDirUIDelegate(string progress);

        #region UI

        public MainWindow()
        {
            InitializeComponent();
            InitializeDB();
        }

        /// <summary>
        /// click function for "Select Directory" button
        /// After get the location path, it will drop the current database
        /// and scan the folder to fill the database asynchronously
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectDir(object sender, RoutedEventArgs e)
        {
            var workshopLocation = Utils.Util.SelectDir();
            LoadDB(workshopLocation);
        }

        /// <summary>
        /// DEPRECATED
        /// REASON: Since when the code change the database, it will call RefreshTable. The button is no longer needed.
        /// click function for "Refresh Table" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefreshTable(object sender, RoutedEventArgs e)
        {
            RefreshTable();
        }

        /// <summary>
        /// bind datagridview to new datatable source
        /// When the code change the database, it should call this
        /// </summary>
        /// <param name="search">null to select *, not null to select * where Title like '%{search}%'</param>
        private void RefreshTable(string search = null)
        {
            if (search != null)
            {
                _dt = GetSpecificRowsFromDB(SearchBox.Text);
                DataGridView1.ItemsSource = _dt.AsDataView();
            }
            else
            {
                _dt = GetDB();
                DataGridView1.ItemsSource = _dt.AsDataView();
            }
        }

        /// <summary>
        /// click function for "Download Content and Analyze" button
        /// 1. find everything selected
        /// 2. call the DownloadAndAnalyze asynchronously
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownloadAndAnalyze(object sender, RoutedEventArgs e)
        {
            // https://www.codeproject.com/Questions/119505/Get-Selected-items-in-a-WPF-datagrid
            var count = DataGridView1.SelectedItems.Count;
            var ids = new string[count];
            for (var i = 0; i < count; i++)
            {
                var SelectedRow = (DataRowView)DataGridView1.SelectedItems[i];
                ids[i] = SelectedRow.Row.ItemArray[0].ToString();
            }

            Task.Run(() =>
            {
                DownloadAndAnalyze(ids, Environment.CurrentDirectory);
            });
        }

        /// <summary>
        /// display the picture of mod when selection changed
        /// ONLY DISPLAY THE FIRST SELECTED ITEM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // https://bbs.csdn.net/topics/391924207
            var SelectedRow = (DataRowView)DataGridView1.SelectedItem;
            if (SelectedRow != null)
            {
                var id = SelectedRow.Row.ItemArray[0].ToString();
                var path = $@"{Environment.CurrentDirectory}\Image\{id}.jpg";
                if (File.Exists(path))
                {
                    var image = new BitmapImage(new Uri(path));
                    Preview.Source = image;
                }
                else
                {
                    Preview.Source = null;
                }
            }
        }

        /// <summary>
        /// DEPRECATED
        /// REASON: It is not a main feature and may cause data loss
        /// click function for "Delete Directory" button
        /// 1. find everything selected
        /// 2. delete directory and row in database 
        /// 3. refresh table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Directory_Click(object sender, RoutedEventArgs e)
        {
            // https://www.codeproject.com/Questions/119505/Get-Selected-items-in-a-WPF-datagrid
            var count = DataGridView1.SelectedItems.Count;
            var ids = new string[count];
            for (var i = 0; i < count; i++)
            {
                var SelectedRow = (DataRowView)DataGridView1.SelectedItems[i];
                ids[i] = SelectedRow.Row.ItemArray[0].ToString();
            }
            DeleteDirectory(ids);

            RefreshTable();
        }

        /// <summary>
        /// open the repo on github
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/thekingofcity/CSL-Mod-Manager");
        }

        /// <summary>
        /// Everytime the text of search box changed, it will find the mods that like the text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshTable(SearchBox.Text);
        }

        #endregion

        #region MainLogic
        
        /// <summary>
        /// WILL DROP DATABASE
        /// the initialization of database(drop and scan)
        /// </summary>
        /// <param name="workshopLocation"></param>
        private void LoadDB(string workshopLocation)
        {
            if (Directory.Exists(workshopLocation))
            {
                // https://blog.csdn.net/yl2isoft/article/details/11711833
                Task.Run(() =>
                {
                    RefreshDB(workshopLocation);
                });
            }
        }
   
        /// <summary>
        /// delegate function
        /// update status label
        /// </summary>
        /// <param name="status"></param>
        private void StatusUpdate(string status)
        {
            Status.Content = status;
        }

        /// <summary>
        /// will be used on startup
        /// check if db exist and create or read
        /// </summary>
        private void InitializeDB()
        {
            var fileExist = File.Exists(@"database.sqlite");
            if (!fileExist)
            {
                _db = new Database();
                _db.CreateTable();
                LoadDB(Utils.Util.GetWorkShopDir(DefaultAppid));
            }
            else
            {
                _db = new Database();
                RefreshTable();
            }
        }

        /// <summary>
        /// delete the db file and recreate it
        /// </summary>
        private void OverrideDB()
        {
            var fileExist = File.Exists(@"database.sqlite");
            if (fileExist)
            {
                // full initialize
                // delete db file and recreate
                _db.CloseDB();
                // release the file
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(@"database.sqlite");

                _db = new Database();
                _db.CreateTable();
            }
        }

        /// <summary>
        /// drop and scan the database asynchronously
        /// update UI through delegate function
        /// </summary>
        /// <param name="workshopLocationObj"></param>
        private void RefreshDB(object workshopLocationObj)
        {
            var workshopLocation = (string)workshopLocationObj;
            OverrideDB();  // drop db
            _db.UpdateWorkshopLocation(workshopLocation);
            try
            {
                var di = new DirectoryInfo(workshopLocation);
                var dirs = di.GetDirectories(@"*");
                var ModSize = dirs.Length;
                var index = 0;

                foreach (var diNext in dirs)
                {
                    index++;
                    _db.InsertNewMod(long.Parse(diNext.Name), Utils.Util.GetFilesSize(diNext));

                    var status = $@"{index} of {ModSize}";
                    // update the UI in progress
                    Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new AsyncSelectDirUIDelegate(StatusUpdate),
                        status);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($@"The process failed: {e}");
            }
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(StatusUpdate),
                @"All done.");
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(RefreshTable),
                null);
        }

        /// <summary>
        /// DEPRECATED
        /// REASON: see Delete_Directory_Click for details
        /// </summary>
        /// <param name="ids"></param>
        private void DeleteDirectory(IReadOnlyList<string> ids)
        {
            var l = ids.Count;
            for (var i = 0; i < l; i++)
            {
                var id = ids[i];
                var path = $@"{_db.GetWorkshopLocation()}\{id}";
                if (Directory.Exists(path))
                {
                    Utils.Util.DeleteDir(path);
                    _db.DeleteMod(long.Parse(id));
                }
            }
        }

        /// <summary>
        /// select * from table
        /// and return DataTable for DataGridView source bind
        /// </summary>
        /// <returns></returns>
        private DataTable GetDB()
        {
            var dt = new DataTable();
            _db.GetMods(dt);
            return dt;
        }

        /// <summary>
        /// select * from table where Title like '%{search}%'
        /// and return DataTable for DataGridView source bind
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        private DataTable GetSpecificRowsFromDB(string search)
        {
            var dt = new DataTable();
            _db.GetSpecificMods(dt, search);
            return dt;
        }
        
        /// <summary>
        /// Download the workshop page and update database with mod details
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="localPath"></param>
        private void DownloadAndAnalyze(string[] ids, string localPath)
        {
            // https://www.cnblogs.com/ibeisha/p/threadpool.html
            ThreadPool.SetMinThreads(2, 1);
            ThreadPool.SetMaxThreads(8, 4);

            // https://bbs.csdn.net/topics/220064751
            var wc = new WebClient
            {
                Credentials = CredentialCache.DefaultCredentials
            };

            var l = ids.Length;
            for (var i = 0; i < l; i++)
            {
                var pageUrl = $@"https://steamcommunity.com/sharedfiles/filedetails/?id={ids[i]}&searchtext=";
                var pageData = wc.DownloadData(pageUrl);
                var idHtml = Encoding.UTF8.GetString(pageData);

                // this item might be deleted
                if (idHtml.IndexOf(@"error_ctn", StringComparison.Ordinal) > -1) continue;

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(idHtml);

                var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemTitle\"]");
                var title = titleNode.InnerText;
                var tagsNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopTagsTitle\"]");
                var tags = tagsNode != null ? tagsNode.InnerText : string.Empty;
                var descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"workshopItemDescription\"]");
                var description = descriptionNode.InnerText;

                var highlightPlayerAreaNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"highlight_player_area\"]");
                string screenshots;
                if (highlightPlayerAreaNode.ChildNodes.Count > 5)
                {
                    // multiple screenshots
                    var screenshotsHolderNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@class=\"screenshot_holder\"]");
                    var screenshotsNode = screenshotsHolderNode.FirstChild.NextSibling;
                    screenshots = screenshotsNode.Attributes["onclick"].Value;
                }
                else
                {
                    // single screenshots
                    var screenshotsNode = highlightPlayerAreaNode.FirstChild.NextSibling;
                    if (screenshotsNode.Name == "div")
                    {
                        screenshotsNode = highlightPlayerAreaNode.FirstChild.NextSibling.FirstChild.NextSibling.FirstChild.NextSibling;
                        screenshots = screenshotsNode.Attributes["onclick"].Value;
                    }
                    else
                    {
                        screenshots = screenshotsNode.Attributes["onclick"].Value;
                    }
                }

                const string pattern = @"https://steamuserimages-a.akamaihd.net/ugc/[\w./]+";

                var matchCollection = Regex.Matches(screenshots, pattern);
                screenshots = matchCollection.Count == 1 ? matchCollection[0].Value : string.Empty;

                _db.UpdateMod(ids[i], title, tags, description, screenshots);

                var status = $@"{i} of {l}";
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new AsyncSelectDirUIDelegate(StatusUpdate),
                    status);

                //AsyncPic AP = new AsyncPic();
                //AP.ImgURL = screenshots;
                //AP.fileName = ids[i] + ".jpg";
                //AP.localPath = localPath + "\\Image\\";

                //Thread t = new Thread(new ThreadStart(AP.AsyncDownloadPic));
                //t.Start();

                if (screenshots != string.Empty)
                {
                    DownloadPicArgv dpv;
                    dpv.localPath = $@"{localPath}\Image\";
                    dpv.fileName = $@"{ids[i]}.jpg";
                    dpv.ImgURL = screenshots;
                    ThreadPool.QueueUserWorkItem(Utils.Util.AsyncDownloadPic, dpv);
                }
            }

            wc.Dispose();

            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(StatusUpdate),
                @"All done.");
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new AsyncSelectDirUIDelegate(RefreshTable),
                null);

        }

        #endregion

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Export export = new Export();
            // MultiThread variable access
            // http://www.cnblogs.com/hellohxs/p/9528505.html
            export.getWorkshopLocationHandler = _db.GetWorkshopLocation;
            export.Show();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            Import import = new Import();
            // MultiThread variable access
            // http://www.cnblogs.com/hellohxs/p/9528505.html
            import.getWorkshopLocationHandler = _db.GetWorkshopLocation;
            import.Show();
        }
    }
}
