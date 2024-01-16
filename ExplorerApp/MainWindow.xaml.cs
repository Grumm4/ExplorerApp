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
namespace ExplorerApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //XmlSerializer serializer;
        private Dictionary<string, BitmapSource> imageCache = new Dictionary<string, BitmapSource>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("imageCache.json"))
            {
                return;
            }
            // Чтение JSON из файла
            var jsonString = File.ReadAllText("imageCache.json"); 

            // Десериализация из JSON
            var deserializedDictBytes = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(jsonString);

            // Преобразование byte[] обратно в ImageSource
            imageCache = deserializedDictBytes.ToDictionary(pair => pair.Key, pair => BytesToImageSource(pair.Value));
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
            BitmapImage bitmapImage = ConvertToBitmapImage(imageSource);

            if (bitmapImage == null)
            {
                throw new ArgumentException("ImageSource должен быть типа BitmapImage");
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public BitmapSource BytesToImageSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            BitmapImage image = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                ms.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        //string ImageToBase64(BitmapSource bitmap)
        //{
        //    var encoder = new PngBitmapEncoder();
        //    var frame = BitmapFrame.Create(bitmap);
        //    encoder.Frames.Add(frame);
        //    using (var stream = new MemoryStream())
        //    {
        //        encoder.Save(stream);
        //        return Convert.ToBase64String(stream.ToArray());
        //    }
        //}

        //BitmapSource Base64ToImage(string base64)
        //{
        //    byte[] bytes = Convert.FromBase64String(base64);
        //    using (var stream = new MemoryStream(bytes))
        //    {
        //        bytes.BeginInit();
        //        source.StreamSource = stream;
        //        source.CacheOption = BitmapCacheOption.OnLoad;    // not a mistake - see below
        //        source.EndInit();
        //    }
        //}

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
        //            //Image = GetIcons(fileInfo),
        //            Label = fileInfo.Name,
        //            Path = fileInfo.FullName,
        //            DateOfChange = fileInfo.LastAccessTime.ToShortDateString(),
        //            Size = Directory.Exists(item) ? String.Empty : Math.Ceiling(Convert.ToDouble(fileInfo.Length) / 1024).ToString() + " КБ",
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
            var directories = await Task.Run(() => Directory.GetDirectories(path).ToList());
            directories.RemoveAll(dir => (File.GetAttributes(dir) & FileAttributes.Hidden) == FileAttributes.Hidden);

            var files = await Task.Run(() => Directory.GetFiles(path).ToList());
            files.RemoveAll(dir => (File.GetAttributes(dir) & FileAttributes.Hidden) == FileAttributes.Hidden);

            List<Model> list = new List<Model>();

            foreach (var dir in directories)
            {
                Model model = await CreateModelFromPath(dir, true);
                if ((model.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }
                else
                {
                    list.Add(model);
                }
            }

            foreach (var file in files)
            {
                Model model = await CreateModelFromPath(file, false);
                if ((model.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }
                else
                {
                    list.Add(model);
                }
            }

            TbPath.Text = path;
            dgExplorer.ItemsSource = list;

            ItemsCount.Content = imageCache.Count.ToString();//"Элементов: " + list.Count;
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
                Size = (info is FileInfo) ? Math.Ceiling(Convert.ToDouble(((FileInfo)info).Length) / 1024).ToString() + " КБ" : String.Empty,
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
                    BitmapSource imageSource = (BitmapSource)new ImageSourceConverter().ConvertFromString("..\\..\\Resources\\file.png");
                    
                    //SerializableImage img = new SerializableImage { Image = imageSource };

                    //imageCache.Add(fileInfo.FullName, imageSource);
                    return imageSource;
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
            string cellValue = String.Empty;

            if (Directory.Exists(((Model)dgExplorer.SelectedItem).Path))
                cellValue = ((Model)dgExplorer.SelectedItem).Path;

            else if (File.Exists(((Model)dgExplorer.SelectedItem).Path))
            {
                var selectedRow = ((Model)dgExplorer.SelectedItem).Path;

                ProcessStartInfo startInfo = new ProcessStartInfo(selectedRow)
                {
                    FileName = selectedRow,
                    UseShellExecute = false
                };
                //исполняемыфй или нет
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
