using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
namespace ExplorerApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //XmlSerializer serializer;
        private Dictionary<string, BitmapSource> imageCache = new Dictionary<string, BitmapSource>();
        //ResourceDictionary dictionary = new ResourceDictionary();
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //dictionary.Source = new Uri("App.xaml", UriKind.RelativeOrAbsolute);

            if (!File.Exists("imageCache.json"))
            {
                return;
            }
            // Чтение JSON из файла
            var jsonString = File.ReadAllText("imageCache.json"); 

            // Десериализация из JSON
            var deserializedDictBytes = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(jsonString);

            // Преобразование byte[] обратно в BitmapSource
            imageCache = deserializedDictBytes.ToDictionary(pair => pair.Key, pair => BytesToBitmapSource(pair.Value));

            Button bt = new Button();
            Style myStyle = (Style)Application.Current.Resources["CustomButtonStyle"];
            bt.Style = myStyle;
            bt.Tag = "D:";
            bt.Click += MenuButtonClick;
            bt.Content = "Локальный диск " + bt.Tag + "/";
            PanelQuickAccess.Children.Add(bt);

        }
        private async void MenuButtonClick(object sender, RoutedEventArgs e)
        {
            var dir = (sender as Button).Tag;

            await GetDirectoriesAndFiles(dir.ToString());
        }


        private async void HiddenBt_Click(object sender, RoutedEventArgs e)
        {
            if ((string)(sender as Button).Content == (string)App.Current.FindResource("ShowHidden"))
            {
                HiddenBt.Content = (string)App.Current.FindResource("NotShowHidden");
            }
            else
            {
                HiddenBt.Content = (string)App.Current.FindResource("ShowHidden");
            }
            
            await GetDirectoriesAndFiles(TbPath.Text);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Преобразование ImageSource в byte[]
            var dictBytes = imageCache.ToDictionary(pair => pair.Key, pair => ImageSourceToBytes(pair.Value));

            // Сериализация в JSON
            var jsonText = JsonSerializer.Serialize(dictBytes);

            // Сохранение JSON в файл
            //File.WriteAllText("imageCache.json", jsonString);
            File.WriteAllText("imageCache.json", jsonText);
        }

        private BitmapImage ConvertToBitmapImage(ImageSource imageSource)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder(); // Выберите соответствующий энкодер в зависимости от формата изображения
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));
                encoder.Save(memoryStream);

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }

        public byte[] ImageSourceToBytes(ImageSource imageSource)
        {
            BitmapImage bitmapImage = ConvertToBitmapImage(imageSource) ?? throw new ArgumentException("ImageSource должен быть типа BitmapImage");

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public BitmapSource BytesToBitmapSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            BitmapImage source = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                ms.Position = 0;
                source.BeginInit();
                source.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.UriSource = null;
                source.StreamSource = ms;
                source.EndInit();
            }
            source.Freeze();
            return source;
        }

        internal async Task GetDirectoriesAndFiles(string path)
        {
            var directories = await Task.Run(() => Directory.GetDirectories(path+"\\").ToList());
            var files = await Task.Run(() => Directory.GetFiles(path+"//").ToList());
            
            if ((string)HiddenBt.Content != (string)App.Current.FindResource("ShowHidden"))
            {
                directories.RemoveAll(dir => (File.GetAttributes(dir) & FileAttributes.Hidden) == FileAttributes.Hidden);
                files.RemoveAll(dir => (File.GetAttributes(dir) & FileAttributes.Hidden) == FileAttributes.Hidden);
            }

            List<Model> list = new List<Model>();

            foreach (var dir in directories)
            {
                Model model = await CreateModelFromPath(dir, true);
                list.Add(model);
            }

            foreach (var file in files)
            {
                Model model = await CreateModelFromPath(file, false);
                list.Add(model);
            }

            TbPath.Text = path;
            dgExplorer.ItemsSource = list;

            ItemsCount.Content = $"Элементов: {list.Count}"; //"Элементов: " + list.Count;
            LabelItemsCount.Content = imageCache.Count.ToString();

            
            dgExplorer.ScrollIntoView(0);

            //Button bt = new Button();
            //bt.Name = "C:"
            //PanelQuickAccess.Children.Add();
        }

        private async Task<Model> CreateModelFromPath(string path, bool isDirectory)
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
                Image = await GetIconsAsync(info),
                Label = info.Name,
                Path = info.FullName,
                DateOfChange = info.LastAccessTime.ToShortDateString(),
                Size = (info is FileInfo) ? Math.Ceiling(Convert.ToDouble(((FileInfo)info).Length) / 1024).ToString() + " КБ" : string.Empty,
                Type = (info is DirectoryInfo) ? "Папка" : "Файл",
                Attributes = info.Attributes
            };
            
            return model;
        }

        //ОЧЕНЬ ДОЛГА БИМ БИМ БАМ БАМ
        async Task <BitmapSource> GetIconsAsync(FileSystemInfo fileInfo)
        {
            return await Application.Current.Dispatcher.InvokeAsync<BitmapSource>(() =>
            {

                if (File.Exists(fileInfo.FullName))
                {
                    if (imageCache.ContainsKey(fileInfo.FullName))
                    {
                        return imageCache[fileInfo.FullName];
                    }

                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(fileInfo.FullName);
                    if (icon.Width > 0 && icon.Height > 0)
                    {
                        var bitmap = icon.ToBitmap();
                        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        imageCache.Add (fileInfo.FullName, bitmapSource);
                        return bitmapSource;
                    }
                    return null;
                }
                else
                {
                    BitmapSource bitmapSource = (BitmapSource)new ImageSourceConverter().ConvertFromString("..\\..\\Resources\\directory.png");
                    return bitmapSource;
                }
            }).Task;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var drivesPage = new Drives(); // Создание экземпляра страницы Drives.xaml
            mainFrame.Navigate(drivesPage); // Навигация к странице Drives.xaml во фрейме
        }

        private async void DgExplorer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string cellValue = string.Empty;
            if (sender is ExplorerApp.MainWindow)
            {   
                cellValue = TbPath.Text;   
            }
            else if((Model) dgExplorer.SelectedItem == null)
            {
                return;
            }
            else
            {
                var selectedItem = ((Model)dgExplorer.SelectedItem).Path;

                if (!string.IsNullOrEmpty(selectedItem))
                {
                    if (Directory.Exists(selectedItem))
                        cellValue = ((Model)dgExplorer.SelectedItem).Path;

                    else if (File.Exists(((Model)dgExplorer.SelectedItem).Path))
                    {
                        var selectedRow = ((Model)dgExplorer.SelectedItem).Path;

                        //ProcessStartInfo startInfo = new ProcessStartInfo(selectedRow)
                        //{
                        //    FileName = selectedRow,
                        //    UseShellExecute = false
                        //};
                        Process.Start(selectedRow);
                        return;
                    }
                    
                }
            }
            await GetDirectoriesAndFiles(cellValue);
        }

        private async void BackBt_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = TbPath.Text.Split('\\').ToList();
            if (list.Count == 1)
            {
                Drives drives = new Drives();
                drives.Left = this.Left;
                drives.Top = this.Top;
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
        private void FindBt_Click(object sender, RoutedEventArgs e)
        {
            DgExplorer_MouseDoubleClick(this, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
        }

        private void TbPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DgExplorer_MouseDoubleClick(this, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        }

        public bool IsMenuOpen { get; set; } = true;
        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation();
            if (IsMenuOpen)
            {
                animation.To = 0;
                ToggleMenu.Content = (string)App.Current.FindResource("OpenMenu");
            }
            else
            {
                animation.To = 150;
                ToggleMenu.Content = (string)App.Current.FindResource("CloseMenu");
            }
            animation.Duration = TimeSpan.FromMilliseconds(500);

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, MenuPanel);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Grid.WidthProperty));

            storyboard.Begin();
            IsMenuOpen = !IsMenuOpen;
        }
    }
}
