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
        private Dictionary<(int row, int col), Canvas> canvasDict = new Dictionary<(int row, int col), Canvas>();
        private double cellSize = 58;
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0)
                    {
                        var canvas = new Canvas
                        {
                            Background = new SolidColorBrush(Color.FromRgb(119, 149, 86)),
                            Name = $"CheckerCell_{i}_{j}",
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Width = cellSize,
                            Height = cellSize
                        };
                        canvasDict[(i, j)] = canvas;
                        Grid.SetRow(canvas, i);
                        Grid.SetColumn(canvas, j);
                        grid.Children.Add(canvas);

                        int hope = 12;
                    }

                }

            }
        }

        private void ClickMouse(object sender, MouseButtonEventArgs e) // определение положения клетки
        {
            var pos = e.GetPosition((Grid)sender);
            int row = 0;
            int col = 0;
            double accumulatedRow = 0;
            double accumulatedCol = 0;
            foreach (var rowDefinition in grid.RowDefinitions)
            {
                accumulatedRow += rowDefinition.ActualHeight;
                if (accumulatedRow >= pos.Y)
                    break;
                row++;
            }
            foreach (var colDefinition in grid.ColumnDefinitions)
            {
                accumulatedCol += colDefinition.ActualWidth;
                if (accumulatedCol >= pos.X)
                    break;
                col++;
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