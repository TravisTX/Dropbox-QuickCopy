using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using System.Threading;
using Microsoft.Win32;

namespace DropBoxQuickCopy
{
    public class Program
    {
        private NotifyIcon appIcon = new NotifyIcon();
        private ContextMenu sysTrayMenu = new ContextMenu();
        private MenuItem menuExitApp = new MenuItem("E&xit");
        private MenuItem menuAutoStart = new MenuItem("Autostart");

        #region props
        
        string userid = "14612097";
        
        public string PublicPath
        {
            get
            {
                if (_publicPath == null)
                {
                    StreamReader sr = new StreamReader(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Dropbox\host.db"));

                    // read a line of text
                    string ignore = sr.ReadLine();
                    string encodedpath = sr.ReadLine();
                    sr.Close();
                    byte[] decodedBytes = Convert.FromBase64String(encodedpath);
                    string decodedPath = Encoding.UTF8.GetString(decodedBytes);
                    _publicPath = Path.Combine(decodedPath, "Public");
                }
                return _publicPath;
            }
        }
        private string _publicPath = null;

        public bool AutoStart
        {
            get
            {
                if (!_autoStart.HasValue)
                {
                    RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    _autoStart = (rkApp.GetValue("DropBoxQuickCopy") != null);
                }
                return _autoStart.Value;
            }
            set
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (value)
                    rkApp.SetValue("DropBoxQuickCopy", Application.ExecutablePath.ToString());
                else
                    rkApp.DeleteValue("DropBoxQuickCopy", false);

                _autoStart = value;
            }
        }
        private bool? _autoStart = null;

        
        #endregion


        [STAThread]
        static void Main()
        {
            Program program = new Program();
            program.Start();
            Application.Run();
        }

        public void Start()
        {
            //Icon ico = new Icon(@"C:\Projects\misc\DropBoxQuickCopy\DropBoxQuickCopy\DropBoxQuickCopy.ico");

            appIcon.Icon = Resource1.DropBoxQuickCopy;
            appIcon.Text = "My .NET Application";

            sysTrayMenu.MenuItems.Add(menuExitApp);
            appIcon.ContextMenu = sysTrayMenu;
            appIcon.Visible = true;

            sysTrayMenu.Popup += new EventHandler(sysTrayMenu_Popup);
            menuExitApp.Click += new EventHandler(menuExitApp_Click);
            menuAutoStart.Click += new EventHandler(menuAutoStart_Click);
        }

        void sysTrayMenu_Popup(object sender, EventArgs e)
        {
            sysTrayMenu.MenuItems.Clear();
            menuAutoStart.Checked = this.AutoStart;
            sysTrayMenu.MenuItems.Add(menuAutoStart);
            sysTrayMenu.MenuItems.Add(menuExitApp);
            sysTrayMenu.MenuItems.Add("-");

            DirectoryInfo di = new DirectoryInfo(this.PublicPath);
            FileSystemInfo[] files = di.GetFileSystemInfos("*.*", SearchOption.AllDirectories);
            IEnumerable<FileSystemInfo> orderedFiles = files.OrderByDescending(f => f.LastWriteTime);

            int count = 0;
            foreach (FileSystemInfo fsi in orderedFiles)
            {
                if ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory) continue;
                if (count++ >= 5) break;
                string shortpath = fsi.FullName.Substring(PublicPath.Length + 1);
                string url = String.Format("http://dl.dropbox.com/u/{0}/{1}"
                    , userid
                    , Uri.EscapeDataString(shortpath).Replace("%5C", "/")
                    );
                

                MenuItem menuItem = new MenuItem(String.Format("&{0}. {1}", count, shortpath));
                menuItem.Tag = url;
                
                menuItem.Click +=new EventHandler(menuItem_Click);
                sysTrayMenu.MenuItems.Add(menuItem);
            }
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(((MenuItem)sender).Tag.ToString());
            appIcon.BalloonTipText="URL copied to clipboard";
            appIcon.ShowBalloonTip(5000);
        }

        void menuExitApp_Click(object sender, EventArgs e)
        {
            appIcon.Visible = false;
            Application.Exit();
        }

        void menuAutoStart_Click(object sender, EventArgs e)
        {
            this.AutoStart = !this.AutoStart;
            if (this.AutoStart)
                appIcon.BalloonTipText = "DropBoxQuickCopy will auto start with Windows";
            else
                appIcon.BalloonTipText = "DropBoxQuickCopy will no longer auto start with Windows";

            appIcon.ShowBalloonTip(5000);
        }
    }
}
