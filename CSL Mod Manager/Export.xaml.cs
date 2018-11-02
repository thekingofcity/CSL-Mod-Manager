using System;
using System.Collections.Generic;
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
        public Export()
        {
            InitializeComponent();
        }

        private void SteamCloudSave_Checked(object sender, RoutedEventArgs e)
        {
            var ActiveSteamID = Steam.SteamClientHelper.GetActiveUserSteamId3();
        }
    }
}
