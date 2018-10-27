using CSL_Mod_Manager.Steam;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace CSL_Mod_Manager.Utils
{
    public static class Util
    {
        public static string SelectDir()
        {
            var res = string.Empty;

            var openFileDialog = new FolderBrowserDialog
            {
                Description = @"Select WorkShop Directory",
                RootFolder = Environment.SpecialFolder.Desktop,
                ShowNewFolderButton = true
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                res = openFileDialog.SelectedPath;
            }

            return res;
        }

        public static bool IsFileInUse(string fileName)
        {
            var inUse = true;

            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);

                inUse = false;
            }
            catch (IOException)
            {
                // ignored
            }
            finally
            {
                fs?.Close();
            }

            return inUse; //true表示正在使用,false没有使用  
        }

        public static long GetFilesSize(DirectoryInfo directoryInfo)
        {
            // https://blog.csdn.net/carlhui/article/details/472330
            long length = 0;
            foreach (var fsi in directoryInfo.GetFileSystemInfos())
            {
                if (fsi is FileInfo info)
                {
                    length += info.Length;
                }
                else
                {
                    directoryInfo = new DirectoryInfo(fsi.FullName);
                    length += GetFilesSize(directoryInfo);
                }
            }

            return length;
        }

        public static void AsyncDownloadPic(object dpv_obj)
        {
            var dpv = (DownloadPicArgv)dpv_obj;
            var wc = new WebClient();
            var filePath = dpv.localPath + dpv.fileName;
            if (File.Exists(filePath))
            {
                return;
            }

            if (Directory.Exists(dpv.localPath) == false)
            {
                Directory.CreateDirectory(dpv.localPath);
            }

            wc.DownloadFile(dpv.ImgURL, filePath);
        }

        public static void DeleteDir(string srcPath)
        {
            // https://www.cnblogs.com/iamlucky/p/5997865.html
            var dir = new DirectoryInfo(srcPath);
            var fileInfo = dir.GetFileSystemInfos(); //返回目录中所有文件和子目录
            foreach (var i in fileInfo)
            {
                if (i is DirectoryInfo) //判断是否文件夹
                {
                    var subDir = new DirectoryInfo(i.FullName);
                    subDir.Delete(true); //删除子目录和文件
                }
                else
                {
                    File.Delete(i.FullName); //删除指定文件
                }
            }
        }

        public static string GetWorkShopDir(int appid)
        {
            foreach (var k in SteamClientHelper.GetAllAppidInWorkshop())
            {
                if (k.Key == appid)
                {
                    return Path.Combine(k.Value, $@"{k.Key}");
                }
            }
            return null;
        }
    }
}
