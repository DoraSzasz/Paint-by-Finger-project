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
    /// Interaction logic for ColorChooser.xaml
    /// </summary>
    public partial class ColorChooser : UserControl
    {
        public ColorChooser()
        {
            InitializeComponent();
        }

        public Brush SelectedBrush
        {
            get;
            set;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IList<BrushNamePair> brushes = DataContext as IList<BrushNamePair>;
            if (brushes == null)
            {
                return;
            }
            BrushConverter bc = new BrushConverter();
            // compute the number of rows and columns
            int noColumns = 4;
            int noRows = (int)Math.Ceiling( (double)brushes.Count() / noColumns);
            
            

            gdRoot.Children.Clear();

            for (int i = 0; i < noColumns; i++)
            {
                gdRoot.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < noRows; i++)
            {
                gdRoot.RowDefinitions.Add(new RowDefinition());
            }

            for (int index = 0; index < brushes.Count(); index++)
            {
                int c = index % noColumns;
                int r = index / noColumns;
                BrushDisplay bd = new BrushDisplay();
                bd.DataContext = brushes[index];
                Grid.SetRow(bd, r);
                Grid.SetColumn(bd, c);
                bd.MouseDown += new MouseButtonEventHandler(bd_MouseDown);
                gdRoot.Children.Add(bd);
            }
        }

        void bd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BrushDisplay bd = sender as BrushDisplay;
            this.SelectedBrush = (bd.DataContext as BrushNamePair).Brush;
        }
    }
}
