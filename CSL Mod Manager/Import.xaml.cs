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
    /// Interaction logic for Import.xaml
    /// </summary>
    public partial class Import : Window
    {
        // 0: temporary
        // 1: permanent
        private int ImportMode = 0;
        private string BackupLocation = string.Empty;
        private string SaveLocation = string.Empty;
        private string WorkshopLocation = string.Empty;
        private string BackupWorkshopLocation = string.Empty;
        // list containing saves in backup
        List<string> saves = new List<string>();

        // MultiThread variable access
        // http://www.cnblogs.com/hellohxs/p/9528505.html
        public delegate string GetWorkshopLocationHandler();
        public GetWorkshopLocationHandler getWorkshopLocationHandler;

        public Import()
        {
            InitializeComponent();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            // https://www.cnblogs.com/luluping/archive/2012/07/23/2605777.html
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "CSL Backup files (*.7z)|*.7z|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupLocation = openFileDialog1.FileName;
            }
        }

        private void ExtractBackup(String BackupLocation, String WorkshopLocation_)
        {
            String PromptMsg;
            WorkshopLocation = WorkshopLocation_ + "\\";
            Task.Run(() =>
            {
                using (SevenZipExtractor tmp = new SevenZipExtractor(BackupLocation))
                {
                    tmp.FileExtractionStarted += new EventHandler<FileInfoEventArgs>((s, e) =>
                    {
                        Console.WriteLine(String.Format("[{0}%] {1}",
                            e.PercentDone, e.FileInfo.FileName));
                    });
                    //tmp.ExtractionFinished += new EventHandler((s, e) => { Console.WriteLine("Finished!"); });
                    tmp.ExtractArchive(WorkshopLocation);
                }

                saves.Clear();
                var di = new DirectoryInfo(WorkshopLocation);
                foreach (var fi in di.GetFiles("*.crp"))
                {
                    String SaveFiledestinationPath = SaveLocation + fi.Name;
                    if (File.Exists(fi.FullName) && !File.Exists(SaveFiledestinationPath))
                    {
                        File.Move(fi.FullName, SaveFiledestinationPath);
                        saves.Add(SaveFiledestinationPath);
                    }
                    else
                    {
                        PromptMsg = $"Save {fi.Name} conflict with the save of the same name.\r\nFile is not moved.";
                        MessageBoxResult _ = MessageBox.Show(PromptMsg, "Conflict", MessageBoxButton.OK);
                    }
                }

                //PromptMsg = "Import complete.\r\nPlease click Restore when you want this backup to be restored.";
                PromptMsg = "Import complete.";
                MessageBoxResult result = MessageBox.Show(PromptMsg, "Done", MessageBoxButton.OK);
                
            });
        }

        private void RestoreBackup()
        {

            // remove save
            for (int i = 0; i < saves.Count; i++)
            {
                if (File.Exists(saves[i]))
                {
                    File.Delete(saves[i]);
                }
            }

            WorkshopLocation = WorkshopLocation.Substring(0, WorkshopLocation.Length - 1);
            // delete import workshop
            if (Directory.Exists(WorkshopLocation))
            {
                // true is recursive delete: 
                Directory.Delete(WorkshopLocation, true);
            }

            // restore original workshop
            if (Directory.Exists(BackupWorkshopLocation))
            {
                Directory.Move(BackupWorkshopLocation, WorkshopLocation);
            }

            MessageBoxResult result = MessageBox.Show("Temporary import complete", "Done", MessageBoxButton.OK);
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            // MultiThread variable access
            // http://www.cnblogs.com/hellohxs/p/9528505.html
            WorkshopLocation = getWorkshopLocationHandler();

            if (WorkshopLocation == String.Empty || SaveLocation == String.Empty)
            {
                // something was not selected
                return;
            }

            if (ImportMode == 0)
            {

                String BakWorkshopLocation = WorkshopLocation + "_bak";

                if (Directory.Exists(WorkshopLocation))
                {
                    Directory.Move(WorkshopLocation, BakWorkshopLocation);
                }
                else
                {
                    return;
                }

                BackupWorkshopLocation = BakWorkshopLocation;
                ImportButton.Visibility = Visibility.Hidden;
                RestoreButton.Visibility = Visibility.Visible;
                ExtractBackup(BackupLocation, WorkshopLocation);

            }
            else
            {
                MessageBoxResult result = MessageBox.Show("THIS WILL DELETE ALL YOUR CURRENT WORKSHOP FILES.\r\nSTEAM SHOULD UPDATE WORKSHOP TO SUBSCRIBED AFTER NEXT RESTART", "Warning", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {

                    if (Directory.Exists(WorkshopLocation))
                    {
                        Directory.Delete(WorkshopLocation, true);
                    }
                    else
                    {
                        return;
                    }

                    ExtractBackup(BackupLocation, WorkshopLocation);
                }
            }
        }
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreButton.Visibility = Visibility.Hidden;
            ImportButton.Visibility = Visibility.Visible;
            RestoreBackup();
            return;
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
        }

        private void CustomLocation_Checked(object sender, RoutedEventArgs e)
        {
            SaveLocation = Utils.Util.SelectDir() + "\\";
        }

        private void Permanent_Checked(object sender, RoutedEventArgs e)
        {
            ImportMode = 1;
        }

        private void Temporary_Checked(object sender, RoutedEventArgs e)
        {
            ImportMode = 0;
        }
    }
}
