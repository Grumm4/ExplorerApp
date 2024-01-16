using System;
using System.Collections.Generic;
using System.IO;

//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExplorerApp
{
    internal class Model
    {
        //public Image Image { get; set; }
        //void Foo()
        //{
            
        //}

        public BitmapSource Image { get; set; }
        //public string Image { get; set; }
        public string Path { get; set; }
        public string Label { get; set; }
        public string DateOfChange { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public FileAttributes Attributes;
    }
}
