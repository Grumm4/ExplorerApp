using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace ExplorerApp
{
    /// <summary>
    /// Логика взаимодействия для Drives.xaml
    /// </summary>
    public partial class Drives : Window
    {
        public Drives()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetDrives();
        }

        void GetDrives()
        {
            List<DrivesModel> driveCards = new List<DrivesModel>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                var model = new DrivesModel
                {
                    Image = (ImageSource)new ImageSourceConverter().ConvertFromString("..\\..\\Resources\\driveIcon.png"),
                    VolumeLabel = string.IsNullOrEmpty(driveInfo.VolumeLabel) ? "Local drive" : driveInfo.VolumeLabel,
                    VolumeCharacter = driveInfo.Name,
                    AvailableFreeSpace = Math.Round(Convert.ToDouble(driveInfo.AvailableFreeSpace) / 1024 / 1024 / 1024, 1).ToString(),
                    TotalSpace = Math.Round(Convert.ToDouble(driveInfo.TotalSize) / 1024 / 1024 / 1024, 1).ToString()
                };

                model.CardLabelString = model.VolumeLabel + " (" + model.VolumeCharacter + ")";
                model.CardSpaceString = model.AvailableFreeSpace + " доступно из " + model.TotalSpace; 

                driveCards.Add(model);
            }

            dgDrives.ItemsSource = driveCards;
        }

        private async void DgDrives_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string cellValue;

            if ((DrivesModel)dgDrives.SelectedItem == null)
                return;
            cellValue = ((DrivesModel)dgDrives.SelectedItem).VolumeCharacter;

            List<string> strings = cellValue.Split('\\').ToList();
            strings.RemoveAt(strings.Count - 1);
            string character = string.Join("\\", strings);

            MainWindow window = new MainWindow();
            window.Left = this.Left;
            window.Top = this.Top;
            window.Show();
            this.Close();
            await window.GetDirectoriesAndFiles(character);
        }
    }
}
