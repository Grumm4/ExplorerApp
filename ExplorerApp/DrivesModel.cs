using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;

namespace ExplorerApp
{
    internal class DrivesModel
    {
        public ImageSource Image {  get; set; }
        public string VolumeLabel { get; set; }
        public string VolumeCharacter { get; set; }
        
        public string AvailableFreeSpace { get; set; }
        public string TotalSpace { get; set; }

        public string CardLabelString { get; set; }
        public string CardSpaceString { get; set; }
    }
}
