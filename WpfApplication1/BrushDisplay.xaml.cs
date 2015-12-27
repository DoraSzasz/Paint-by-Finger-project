using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for BrushDisplay.xaml
    /// </summary>
    public partial class BrushDisplay : UserControl
    {
        public BrushDisplay()
        {
            InitializeComponent();
        }
    }

    public class BrushDisplayConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BrushNamePair b = value as BrushNamePair;
            if (b != null)
            {
                return b.Name;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class BrushNamePair
    {
        public string Name
        { get; set; }

        public Brush Brush
        {
            get;
            set;
        }

        public BrushNamePair(string Name, Brush Brush)
        {
            this.Name = Name;
            this.Brush = Brush;
        }
    }
}
