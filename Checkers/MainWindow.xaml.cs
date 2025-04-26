using System.Globalization;
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

namespace Checkers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        BoardViewModel _boardViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _boardViewModel = new BoardViewModel();
            DataContext = _boardViewModel;
        }
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            MenuScreen.Visibility = Visibility.Collapsed;
            GameScreen.Visibility = Visibility.Visible;
        }


        private void Size(object sender, SizeChangedEventArgs e)
        {
            var grid = (Grid)sender;
            double cellSize = Math.Min(grid.ActualWidth / 8, grid.ActualHeight / 8);
            _boardViewModel.UpdateCellSize(cellSize);
        }
    }

        private void GridSizeChanged(object sender, SizeChangedEventArgs e) // изменение размера всего при изменении размера окна
        {
            var gridd = (Grid)sender;
            double cellSize = Math.Min(gridd.ActualWidth / gridd.ColumnDefinitions.Count, gridd.ActualHeight / gridd.RowDefinitions.Count);

            foreach (var row in grid.RowDefinitions)
            {
                row.Height = new GridLength(cellSize);
            }

            foreach (var column in grid.ColumnDefinitions)
            {
                column.Width = new GridLength(cellSize);
            }

            foreach (var canv in canvasDict.Values)
            {
                canv.Width = cellSize;
                canv.Height = cellSize;
            }
        }
    }
}