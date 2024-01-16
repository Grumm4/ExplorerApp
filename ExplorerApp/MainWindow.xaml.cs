using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Reflection;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ExplorerApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        //internal async Task GetDirectoriesAndFiles(string path)
        //{
        //    List<DirectoryInfo> dirs = await Task.Run(() => new DirectoryInfo(path).GetDirectories("*", SearchOption.TopDirectoryOnly).ToList());
        //    List<FileInfo> files = await Task.Run(() => new DirectoryInfo(path).GetFiles("*", SearchOption.TopDirectoryOnly).ToList());


        //    List<string> strings = new List<string>();
        //    strings.AddRange(from d in dirs select d.FullName);
        //    strings.AddRange(from f in files select f.FullName);

        //    List<Model> list = new List<Model>();

        //    foreach (var item in strings)
        //    {
        //        FileInfo fileInfo = new FileInfo(item);

        //        Model model = new Model
        //        {
        //            Image = GetIcons(fileInfo),
        //            Label = fileInfo.Name,
        //            Path = fileInfo.FullName,
        //            DateOfChange = fileInfo.LastAccessTime.ToShortDateString(),
        //            Size = Directory.Exists(item) ? String.Empty : Math.Ceiling(Convert.ToDouble(fileInfo.Length)/1024).ToString() + " КБ",
        //            Type = Directory.Exists(item) ? "Папка" : "Файл"
        //        };
        //        list.Add(model);
        //    }

        //    TbPath.Text = path;
        //    dgExplorer.ItemsSource = list;

        //    ItemsCount.Content = "Элементов: " + strings.Count;
        //}

        internal async Task GetDirectoriesAndFiles(string path)
        {
            string[] directories = await Task.Run(() => Directory.GetDirectories(path));
            string[] files = await Task.Run(() => Directory.GetFiles(path));

            List<Model> list = new List<Model>();

            foreach (var dir in directories)
            {
                Model model = CreateModelFromPath(dir, true);
                list.Add(model);
            }

            foreach (var file in files)
            {
                Model model = CreateModelFromPath(file, false);
                list.Add(model);
            }

            TbPath.Text = path;
            dgExplorer.ItemsSource = list;

            ItemsCount.Content = "Элементов: " + list.Count;
        }

        private Model CreateModelFromPath(string path, bool isDirectory)
        {
            FileSystemInfo info;
            if (isDirectory)
            {
                info = new DirectoryInfo(path);
            }
            else
            {
                info = new FileInfo(path);
            }

            Model model = new Model
            {
                Image = GetIcons(info),
                Label = info.Name,
                Path = info.FullName,
                DateOfChange = info.LastAccessTime.ToShortDateString(),
                Size = (info is FileInfo) ? Math.Ceiling(Convert.ToDouble(((FileInfo)info).Length) / 1024).ToString() + " КБ" : String.Empty,
                Type = (info is DirectoryInfo) ? "Папка" : "Файл"
            };
            return model;
        }

        ImageSource GetIcons(FileSystemInfo fileInfo)
        {
            if (File.Exists(fileInfo.FullName))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(fileInfo.FullName);
                if (icon.Width > 0 && icon.Height > 0)
                {
                    var bitmap = icon.ToBitmap();
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    return bitmapSource;
                }
                return null;
            }
            else
            {
                return (ImageSource)new ImageSourceConverter().ConvertFromString("..\\..\\Resources\\file.png");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var drivesPage = new Drives(); // Создание экземпляра страницы Drives.xaml
            mainFrame.Navigate(drivesPage); // Навигация к странице Drives.xaml во фрейме
        }

        private async void DgExplorer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string cellValue = String.Empty;

            if (Directory.Exists(((Model)dgExplorer.SelectedItem).Path))
                cellValue = ((Model)dgExplorer.SelectedItem).Path;

            else if (File.Exists(((Model)dgExplorer.SelectedItem).Path))
            {
                var selectedRow = ((Model)dgExplorer.SelectedItem).Path;

                ProcessStartInfo startInfo = new ProcessStartInfo(selectedRow)
                {
                    FileName = selectedRow,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return;
            }

            //cellValue = ((Model)dgExplorer.SelectedItem).Path;

            await GetDirectoriesAndFiles(cellValue);
        }

        private async void BackBt_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = TbPath.Text.Split('\\').ToList();
            if (list.Count == 1)
            {
                Drives drives = new Drives();
                drives.Show();
                this.Close();
                return;
            }
            else
            {
                list.RemoveAt(list.Count - 1);
                await GetDirectoriesAndFiles(string.Join("\\", list));
            }
        }
    }
}
