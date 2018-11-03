using SevenZip;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CSL_Mod_Manager
{
    /// <summary>
    /// Export.xaml 的交互逻辑
    /// </summary>
    public partial class Export : Window
    {
        private string SaveLocation = string.Empty;
        private string ExportFileName = "export.7z";

        // MultiThread variable access
        // http://www.cnblogs.com/hellohxs/p/9528505.html
        public delegate string GetWorkshopLocationHandler();
        public GetWorkshopLocationHandler getWorkshopLocationHandler;

        public Export()
        {
            InitializeComponent();
        }

        private void ScanSaveFolder(string SaveLocation)
        {

            List<string> items = new List<string>();

            var di = new DirectoryInfo(SaveLocation);
            foreach (var fi in di.GetFiles("*.crp"))
            {
                var ListBoxItem = $@"{fi.Name}    {fi.LastWriteTime}";
                Console.WriteLine(ListBoxItem);
                items.Add($@"{fi.Name}    {fi.LastWriteTime}");
            }

            SaveListBox.ItemsSource = items;
        }

        private void SteamCloudSave_Checked(object sender, RoutedEventArgs e)
        {
            var ActiveSteamID = Steam.SteamClientHelper.GetActiveUserSteamId3();
            if (ActiveSteamID == 0)
            {
                MessageBoxResult result = MessageBox.Show("No Active Steam User Found", "Error", MessageBoxButton.OK);
                return;
            }

            var SteamPath = Steam.SteamClientHelper.GetPathFromRegistry();
            if (SteamPath == string.Empty)
            {
                MessageBoxResult result = MessageBox.Show("No Steam Location Found", "Error", MessageBoxButton.OK);
                return;
            }

            SaveLocation = $@"{SteamPath}\userdata\{ActiveSteamID}\{MainWindow.DefaultAppid}\remote\";
            ScanSaveFolder(SaveLocation);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var SaveCount = SaveListBox.SelectedItems.Count;
            if (SaveCount == 0) {
                MessageBoxResult result = MessageBox.Show("Please select save location first", "Error", MessageBoxButton.OK);
                return;
            }

            var SavePaths = new string[SaveCount];
            for (int i = 0; i < SaveCount; i++)
            {
                var SavePath = SaveListBox.SelectedItems[i].ToString();
                SavePath = System.Text.RegularExpressions.Regex.Split(SavePath, "    ")[0];
                SavePaths[i] = SaveLocation + SavePath;
            }

            Task.Run(() =>
            {
                // usage of SevenZipCpmpressor
                // https://www.cnblogs.com/gdouzz/p/7090710.html
                // http://cache.baiducontent.com/c?m=9d78d513d98216eb0fb1837f7d5e8c240e55f022668490576b93d3169c3e1d070527f4ba543f0d548d98297001d8181dbcac2172405f77f1869bcb0c8efdc1357cc8616f2142d15c44845ffc901864dc279159e9ab1be5b0&p=8b2a975694934ea452fcd6281b5d&newp=8a64c91d85cc43ff57e69e6f550c92695d0fc20e39ddc44324b9d71fd325001c1b69e7bf24271104d8ce766300a44c5ceff63478341766dada9fca458ae7c4&user=baidu&fm=sc&query=sevenzipsharp&qid=bd98ed4500048d09&p1=6
                var compressor = new SevenZipCompressor();
                compressor.ArchiveFormat = OutArchiveFormat.SevenZip;
                compressor.CompressionLevel = CompressionLevel.None;

                // MultiThread variable access
                // http://www.cnblogs.com/hellohxs/p/9528505.html
                var WorkshopLocation = getWorkshopLocationHandler();

                compressor.CompressDirectory(WorkshopLocation, ExportFileName);

                // append files to 7z
                // https://stackoverflow.com/questions/31292707/c-sharp-sevenzipsharp-adding-folder-to-new-existing-archive
                compressor.CompressionMode = CompressionMode.Append;
                compressor.CompressFiles(ExportFileName, SavePaths);
            });
        }

        private void CustomLocation_Checked(object sender, RoutedEventArgs e)
        {
            var SaveLocation = Utils.Util.SelectDir();

        }
    }
}
