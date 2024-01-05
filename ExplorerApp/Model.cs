using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExplorerApp
{
    internal class Model
    {
        //public Image Image { get; set; }
        //void Foo()
        //{
            
        //}
        public ImageSource Image { get; set; }
        public string Path { get; set; }
        public string Label { get; set; }
        public string DateOfChange { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
    }
}
