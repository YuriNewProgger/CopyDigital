using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CopyDigital
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VistaFolderBrowserDialog DialogCopyFrom { get; set; }
        private VistaFolderBrowserDialog DialogCopyTo { get; set; }

        private List<string> ListFiles { get; set; }
        private List<string> ListDirectories { get; set; }

        private List<Helper> Helpers { get; set; }

        private long totalCopied = 0;
        private long total = 0;


        public MainWindow()
        {
            InitializeComponent();

            DialogCopyFrom = new VistaFolderBrowserDialog();
            DialogCopyTo = new VistaFolderBrowserDialog();

            ListFiles = new List<string>();
            ListDirectories = new List<string>();

            Helpers = new List<Helper>()
            {
                new Helper(FileNameForBarOne, BarOne, AmountBytesBarOne, false),
                new Helper(FileNameForBarTwo, BarTwo, AmountBytesBarTwo, false),
                new Helper(FileNameForBarThree, BarThree, AmountBytesBarThree, false),
                new Helper(FileNameForBarFour, BarFour, AmountBytesBarFour, false)
            };
        }

        private void Button_Click_Close(object sender, RoutedEventArgs e) => Close();

        private void Button_Click_Maximize(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Buy a license to use all functions!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        private void Button_Click_Minimize(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Minimized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Button_Click_CopyFrom(object sender, RoutedEventArgs e)
        {
            if (DialogCopyFrom.ShowDialog() == false)
                return;

            textBoxCopyFrom.Text = DialogCopyFrom.SelectedPath;
        }

        private void Button_Click_CopyTo(object sender, RoutedEventArgs e)
        {
            if (DialogCopyTo.ShowDialog() == false)
                return;

            textBoxCopyTo.Text = DialogCopyTo.SelectedPath;
        }

        private async void Button_Click_CopyStart(object sender, RoutedEventArgs e)
        {
            try
            {
                GetListFoldersAndFiels(DialogCopyFrom.SelectedPath);

                foreach(var item in ListFiles)
                {
                    if (CheckRules(item))
                        throw new Exception("Нет прав доступа.");
                }

                await Task.Run(() => CopyFaiels(Helpers));
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Clear(textBoxCopyTo.Text);
            }
        }

        private void GetListFoldersAndFiels(string folder)
        {
            foreach (var item in Directory.GetFiles(folder))
            {
                total += new FileInfo(item).Length;
                ListFiles.Add(item);
            }

            foreach (var item in Directory.GetDirectories(folder))
            {
                ListDirectories.Add(item);
                GetListFoldersAndFiels(item);
            }
        }

        private void CopyFaiels(List<Helper> helpers)
        {
            for (int i = 0; i < ListDirectories.Count; i++)
            {
                var item = DialogCopyTo.SelectedPath + ListDirectories[i].Remove(0, DialogCopyFrom.SelectedPath.Length);
                if (!Directory.Exists(item))
                    Directory.CreateDirectory(item);
            }

            if (ListFiles.Count == 0)
                return;

            for (int i = 0; i < ListFiles.Count; i++)
            {
                Helper helper = null;
                while ((helper = Helpers.Where(q => q.IsBusy == false).FirstOrDefault()) == null) { }
                helper.IsBusy = true;
                helper.Path = ListFiles[i];

                Task.Run(() => ReadWriteFile(helper));
            }
        }

        private void ReadWriteFile(Helper helper)
        {
            Dispatcher.Invoke((Action)(() => helper.FileName.Text = Path.GetFileName(helper.Path)));

            string directory = DialogCopyTo.SelectedPath + helper.Path.Remove(0, DialogCopyFrom.SelectedPath.Length);

            long totalFile = new FileInfo(helper.Path).Length;
            long totalWriten = 0;
            byte[] buffer = new byte[4096];

            using (Stream reader = File.OpenRead(helper.Path))
            {
                using (Stream writer = File.Create(directory))
                {
                    while (true)
                    {
                        int read = reader.Read(buffer, 0, buffer.Length);

                        if (read == 0)
                            break;

                        writer.Write(buffer, 0, read);
                        totalCopied += read;
                        totalWriten += read;

                        Dispatcher.Invoke((Action)(() => helper.Bar.Value = (int)(100 * totalWriten / totalFile)));

                        Dispatcher.Invoke((Action)(() => helper.AmountCopied.Text = $"{totalWriten / (1024 * 1024)} MB"));

                        Dispatcher.Invoke((Action)(() => totalResult.Value = 100 * totalCopied / total));
                    }
                }
            }
            Dispatcher.Invoke((Action)(() => helper.FileName.Text = ""));
            Dispatcher.Invoke((Action)(() => helper.Bar.Value = 0));
            Dispatcher.Invoke((Action)(() => helper.AmountCopied.Text = ""));
            helper.IsBusy = false;
        }

        private void Clear(string folder)
        {
            foreach (var item in Directory.GetFiles(folder))
                File.Delete(item);


            foreach (var item in Directory.GetDirectories(folder))
                Directory.Delete(item);
        }

        private bool CheckRules(string file)
        {
            using(FileStream stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            {
                FileSecurity security = stream.GetAccessControl();
                
                foreach(FileSystemAccessRule item in security.GetAccessRules(true, true, typeof(NTAccount)))
                    return item.AccessControlType == AccessControlType.Allow ? true : false;
            }


            return false;
        }
    }
}
