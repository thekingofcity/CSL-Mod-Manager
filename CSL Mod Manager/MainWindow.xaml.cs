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

using Microsoft.WindowsAPICodePack.Dialogs;

namespace CSL_Mod_Manager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MainLogic ML;
        DataTable dt;
        public MainWindow()
        {
            InitializeComponent();
            ML = new MainLogic();
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
                // Do something with selected folder string
                ML.RefreshDB(workshopLocation);
            }
        }

        private void BtnRefreshTable(object sender, RoutedEventArgs e)
        {
            dt = ML.GetDB();
            DataGridView1.ItemsSource = dt.AsDataView();
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
            ML.DownloadandAnalyse(ids, Environment.CurrentDirectory);

            dt = ML.GetDB();
            DataGridView1.ItemsSource = dt.AsDataView();
        }

        private void DataGridView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // https://bbs.csdn.net/topics/391924207
            DataRowView SelectedRow = (DataRowView)DataGridView1.SelectedItem;
            if (SelectedRow != null)
            {
                string id = SelectedRow.Row.ItemArray[0].ToString();
                string path = Environment.CurrentDirectory + "\\Image\\" + id + ".jpg";
                if (File.Exists(path) && !AsyncPic.IsFileInUse(path))
                {
                    BitmapImage image = new BitmapImage(new Uri(path));
                    Preview.Source = image;
                }
            }
        }
    }
}
