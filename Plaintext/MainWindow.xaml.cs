using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Plaintext
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isShowExtensions = false;
        string docPath, docName;

        [Flags]
        enum SHELLFLAGSTATE : long
        {
            fShowAllObjects = 0x00000001,
            fShowExtensions = 0x00000002,
            fNoConfirmRecycle = 0x00000004,
            fShowSysFiles = 0x00000008,
            fShowCompColor = 0x00000010,
            fDoubleClickInWebView = 0x00000020,
            fDesktopHTML = 0x00000040,
            fWin95Classic = 0x00000080,
            fDontPrettyPath = 0x00000100,
            fShowAttribCol = 0x00000200,
            fMapNetDrvBtn = 0x00000400,
            fShowInfoTip = 0x00000800,
            fHideIcons = 0x00001000,
        }

        const uint SSF_SHOWEXTENSIONS = 0x00000002;

        [DllImport("shell32.dll")]
        static extern void SHGetSettings(out SHELLFLAGSTATE lpsfs, uint dwMask);

        string TruncateString(string value, int length)
        {
            int pos1 = value.IndexOf(Environment.NewLine, 0);
            if (value.Length <= length)
            {
                if (pos1 > -1)
                {
                    return value.Substring(0, pos1).Trim();
                }
                else
                {
                    return value.Trim();
                }
            }
            else
            {
                if (pos1 > -1)
                {
                    return value.Substring(0, pos1).Trim();
                }
                else
                {
                    pos1 = value.LastIndexOf(' ', length);
                    int pos2 = value.LastIndexOf('\t', length);
                    if (pos1 > -1 || pos2 > -1)
                        return value.Substring(0, Math.Max(pos1, pos2)).Trim();
                    else
                        return value.Substring(0, length).Trim();
                }
            }
        }

        /// <summary>
        /// Change the title of the window, especially when the file name or document name has changed
        /// </summary>
        void ChangeTitle()
        {
            if (string.IsNullOrEmpty(docPath) == false)
            {
                if (isShowExtensions)
                {
                    docName = System.IO.Path.GetFileName(docPath);
                }
                else
                {
                    docName = System.IO.Path.GetFileNameWithoutExtension(docPath);
                    if (string.IsNullOrEmpty(docName))
                    {
                        docName = System.IO.Path.GetFileName(docPath);
                    }
                }
            }

            if (string.IsNullOrEmpty(docName.Trim()))
            {
                this.Title = "New document" + " – Snow Plaintext";
            }
            else
            {
                this.Title = docName + " – Snow Plaintext";
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            SHELLFLAGSTATE res;
            SHGetSettings(out res, SSF_SHOWEXTENSIONS);

            if (res == SHELLFLAGSTATE.fShowExtensions)
                isShowExtensions = true;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                docPath = args[1];
                ChangeTitle();
            }

            if (App.createdNew && Properties.Settings.Default.WindowTop != -1)
            {
                this.Top = Properties.Settings.Default.WindowTop;
                this.Left = Properties.Settings.Default.WindowLeft;
            }

            this.Height = Properties.Settings.Default.WindowHeight;
            this.Width = Properties.Settings.Default.WindowWidth;
            if (Properties.Settings.Default.WindowMaximised)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.WindowTop = RestoreBounds.Top;
                Properties.Settings.Default.WindowLeft = RestoreBounds.Left;
                Properties.Settings.Default.WindowHeight = RestoreBounds.Height;
                Properties.Settings.Default.WindowWidth = RestoreBounds.Width;
                Properties.Settings.Default.WindowMaximised = true;
            }
            else
            {
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowLeft = this.Left;
                Properties.Settings.Default.WindowHeight = this.Height;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowMaximised = false;
            }

            Properties.Settings.Default.Save();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            gridDrop.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            gridDrop.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            gridDrop.Visibility = System.Windows.Visibility.Hidden;
        }

        private void txtMain_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(docPath))
            {
                docName = TruncateString(txtMain.Text, 60);
                ChangeTitle();
            }
        }
    }
}
