using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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

        const int WM_DROPFILES = 0x233;

        [DllImport("shell32.dll")]
        static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);

        [DllImport("shell32.dll")]
        static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder filename, uint cch);

        [DllImport("shell32.dll")]
        static extern void DragFinish(IntPtr hDrop);





        bool isShowExtensions = false, isPlaceholderOpenedDoc = false, isDocumentChanged = false;
        string docPath, docName;




        void ChangePlaceholder(bool isOpenedDoc)
        {
            isPlaceholderOpenedDoc = isOpenedDoc;
            ChangePlaceholder();
        }

        void ChangePlaceholder()
        {
            if (isPlaceholderOpenedDoc)
            {
                placeholderMain.Text = "This document is empty";
            }
            else
            {
                placeholderMain.Text = "Type here";
            }
        }

        string TruncateString(string value, int length)
        {
            string result;
            if (value.Length < length)
                length = value.Length;

            int pos1 = value.IndexOf(Environment.NewLine, 0, length);
            if (pos1 > -1)
            {
                result = value.Substring(0, pos1);
            }
            else
            {
                if (value.Length == length)
                {
                    result = value.Substring(0, length);
                }
                else
                {
                    pos1 = value.LastIndexOf(' ', length - 1);
                    int pos2 = value.LastIndexOf('\t', length - 1);
                    if (pos1 > -1 || pos2 > -1)
                        result = value.Substring(0, Math.Max(pos1, pos2));
                    else
                        result = value.Substring(0, length);
                }
            }
            return result.Replace('\t', ' ').Trim();
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

        void OpenFile(string fileName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    txtMain.Text = sr.ReadToEnd();
                }
                docPath = fileName;
                ChangeTitle();
                if (txtMain.Text.Length == 0)
                {
                    ChangePlaceholder(true);
                }
                if (txtMain.Text.Length > 0 && txtMain.Text.Substring(0, 4) == ".LOG")
                {
                    txtMain.Select(txtMain.Text.Length, 0);
                    txtMain.SelectedText = Environment.NewLine + Environment.NewLine + DateTime.Now.ToString("G") + Environment.NewLine;
                    txtMain.Select(txtMain.Text.Length, 0);
                }
                DocumentChanged(false);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                OpenFile(args[1]);
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

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                gridDrop.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                gridDrop.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                gridDrop.Visibility = System.Windows.Visibility.Collapsed;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    OpenFile(files[0]);
                }
            }
        }

        private void txtMain_TextChanged(object sender, TextChangedEventArgs e)
        {
            DocumentChanged(true);

            if (isPlaceholderOpenedDoc)
                ChangePlaceholder(false);

            if (string.IsNullOrEmpty(docPath))
            {
                docName = TruncateString(txtMain.Text, 60);
                ChangeTitle();
                if (txtMain.Text.Length == 0)
                {
                    DocumentChanged(false);
                }
            }
        }

        private void DocumentChanged(bool changed)
        {
            if (isDocumentChanged = changed)
            {
                saveButtonBorder.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                saveButtonBorder.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        void ClickPaste(Object sender, RoutedEventArgs args) { txtMain.Paste(); }
        void ClickCopy(Object sender, RoutedEventArgs args) { txtMain.Copy(); }
        void ClickCut(Object sender, RoutedEventArgs args) { txtMain.Cut(); }
        void ClickSelectAll(Object sender, RoutedEventArgs args) { txtMain.SelectAll(); }
        void ClickClear(Object sender, RoutedEventArgs args) { txtMain.Clear(); }
        void ClickUndo(Object sender, RoutedEventArgs args) { txtMain.Undo(); }
        void ClickRedo(Object sender, RoutedEventArgs args) { txtMain.Redo(); }

        void ClickSelectLine(Object sender, RoutedEventArgs args)
        {
            int lineIndex = txtMain.GetLineIndexFromCharacterIndex(txtMain.CaretIndex);
            int lineStartingCharIndex = txtMain.GetCharacterIndexFromLineIndex(lineIndex);
            int lineLength = txtMain.GetLineLength(lineIndex);
            txtMain.Select(lineStartingCharIndex, lineLength);
        }

        void CxmOpened(Object sender, RoutedEventArgs args)
        {
            // Undo / Redo
            if (txtMain.CanUndo)
                cxmItemUndo.Visibility = System.Windows.Visibility.Visible;
            else
                cxmItemUndo.Visibility = System.Windows.Visibility.Collapsed;

            if (txtMain.CanRedo)
                cxmItemRedo.Visibility = System.Windows.Visibility.Visible;
            else
                cxmItemRedo.Visibility = System.Windows.Visibility.Collapsed;

            // Cut / copy
            if (txtMain.SelectionLength > 0)
                cxmItemCopy.Visibility = cxmItemCut.Visibility = System.Windows.Visibility.Visible;
            else
                cxmItemCopy.Visibility = cxmItemCut.Visibility = System.Windows.Visibility.Collapsed;

            // Paste
            if (Clipboard.ContainsText())
                cxmItemPaste.Visibility = System.Windows.Visibility.Visible;
            else
                cxmItemPaste.Visibility = System.Windows.Visibility.Collapsed;

            if ((txtMain.CanUndo || txtMain.CanRedo) 
                && (cxmItemPaste.Visibility == System.Windows.Visibility.Visible 
                    || cxmItemCopy.Visibility == System.Windows.Visibility.Visible 
                    || cxmItemCut.Visibility == System.Windows.Visibility.Visible))
                cxmUndoSeparator.Visibility = System.Windows.Visibility.Visible;
            else
                cxmUndoSeparator.Visibility = System.Windows.Visibility.Collapsed;

            // Select all
            if (txtMain.Text.Length > 0)
            {
                cxmItemSelectAll.Visibility = System.Windows.Visibility.Visible;
                if (txtMain.CanUndo || txtMain.CanRedo
                    || cxmItemPaste.Visibility == System.Windows.Visibility.Visible
                    || cxmItemCopy.Visibility == System.Windows.Visibility.Visible
                    || cxmItemCut.Visibility == System.Windows.Visibility.Visible)
                    cxmSelectAllSeparator.Visibility = System.Windows.Visibility.Visible;
                else
                    cxmSelectAllSeparator.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                cxmItemSelectAll.Visibility = System.Windows.Visibility.Collapsed;
                cxmSelectAllSeparator.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Line wrap separator
            if (txtMain.CanUndo || txtMain.CanRedo
                ||cxmItemPaste.Visibility == System.Windows.Visibility.Visible
                || cxmItemCopy.Visibility == System.Windows.Visibility.Visible
                || cxmItemCut.Visibility == System.Windows.Visibility.Visible
                || txtMain.Text.Length > 0)
            {
                cxmLineWrapSeparator.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                cxmLineWrapSeparator.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void cxmLineWrap_Click(object sender, RoutedEventArgs e)
        {
            ChangeWrap();
        }

        private void ChangeWrap()
        {
            if (cxmLineWrap.IsChecked)
            {
                txtMain.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                txtMain.TextWrapping = TextWrapping.NoWrap;
            }
        }

        private void InsertTime_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            txtMain.SelectedText = DateTime.Now.ToString("G");
            txtMain.Select(txtMain.Text.Length, 0);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            DragAcceptFiles(hwnd, true);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DROPFILES)
            {
                handled = true;
                return HandleDropFiles(wParam);
            }

            return IntPtr.Zero;
        }

        private IntPtr HandleDropFiles(IntPtr hDrop)
        {
            const int MAX_PATH = 260;

            var count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);

            if (count > 0)
            {
                int size = (int)DragQueryFile(hDrop, 0, null, 0);
                var filename = new StringBuilder(size + 1);
                DragQueryFile(hDrop, 0, filename, MAX_PATH);
                OpenFile(filename.ToString());
            }

            DragFinish(hDrop);

            return IntPtr.Zero;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(docPath))
            {
                SaveAs_Executed(sender, e);
            }
            else
            {
                try
                {
                    System.IO.File.WriteAllText(docPath, txtMain.Text);
                    DocumentChanged(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if (txtFind.IsFocused == false)
            {
                txtMain.Focus();
            }
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = docName; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Plain text documents|*.txt|All files|*.*"; // Filter files by extension
            if (string.IsNullOrEmpty(docPath) == false)
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(docPath);

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                docPath = dlg.FileName;
                try
                {
                    System.IO.File.WriteAllText(docPath, txtMain.Text);
                    DocumentChanged(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            findBorder.Visibility = System.Windows.Visibility.Visible;
            txtFind.Focus();
            txtFind.SelectAll();
        }

        private void txtFind_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) 
            {
                case Key.Enter:
                    FindNext_Executed(null, null);
                    break;
                case Key.Escape:
                    HideFind();
                    break;
            }
        }

        private void HideFind()
        {
            findBorder.Visibility = System.Windows.Visibility.Collapsed;
            txtMain.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HideFind();
        }

        private void FindNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (txtFind.Text.Length > 0)
            {
                int pos = txtMain.Text.IndexOf(txtFind.Text, txtMain.SelectionStart + txtMain.SelectionLength, StringComparison.CurrentCultureIgnoreCase);
                if (pos == -1)
                {
					// beep when repeat search:
					System.Media.SystemSounds.Beep.Play();

					pos = txtMain.Text.IndexOf(txtFind.Text);
                }

                if (pos == -1)
                {
                    // beep when not found:
                    System.Media.SystemSounds.Beep.Play();
                }
                else
                {
                    txtMain.Select(pos, txtFind.Text.Length);
                    txtMain.Focus();
                }
            }
            else
            {
                Find_Executed(sender, e);
            }
        }

		private void txtMain_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape && findBorder.Visibility == System.Windows.Visibility.Visible) {
                HideFind();
            }
		}

		private void FindPrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (txtFind.Text.Length > 0)
            {
                int pos = txtMain.Text.LastIndexOf(txtFind.Text, txtMain.SelectionStart, StringComparison.CurrentCultureIgnoreCase);
                if (pos == -1)
                {
                    // beep when repeat search:
					System.Media.SystemSounds.Beep.Play();

                    pos = txtMain.Text.LastIndexOf(txtFind.Text);
                }

                if (pos == -1)
                {
                    // beep when not found:
                    System.Media.SystemSounds.Beep.Play();
                }
                else
                {
                    txtMain.Select(pos, txtFind.Text.Length);
                }
            }
            else
            {
                Find_Executed(sender, e);
            }
        }

        private void txtMain_LostFocus(object sender, RoutedEventArgs e)
        {
            txtMain.Background = Brushes.WhiteSmoke;
        }

        private void txtMain_GotFocus(object sender, RoutedEventArgs e)
        {
            txtMain.Background = Brushes.White;
        }

    }
}
