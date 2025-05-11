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
            DataContext = new ScreenViewModel();
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрывает текущее окно
        }

    }


    // чтобы круг был чуть меньше ячейки
    public class InnerCheckerSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is double d) ? d * 0.8 : 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}