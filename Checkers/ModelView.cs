using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Checkers
{
    class CheckerViewModel : INotifyPropertyChanged
    {
        public Checker _checkerModel;
        public int Row { get; private set; }
        public int Column { get; private set; }
        private bool _isKing;
        public bool IsKing
        {
            get => _isKing;
            set
            {
                _isKing = value;
                OnPropertyChanged("IsKing");
            }
        }

        private bool Color => _checkerModel.IsWhite; //цвет шашки 0 - черный 1 - белый

        private Brush _fill;

        public Brush Fill
        {
            get => _fill;
            set
            {
                _fill = value;
                OnPropertyChanged("Fill");
            }
        }

        public CheckerViewModel(Checker checker)
        {
            _checkerModel = checker;
            Row = checker.FromX;
            Column = checker.FromY;
            _fill = Color ? Brushes.White : Brushes.Black;
        }

       

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class CellViewModel : INotifyPropertyChanged
    {
        public int Row { get; }
        public int Col { get; }
        private Brush _background = new SolidColorBrush(Color.FromRgb(119, 149, 86));

        public Brush Background
        {
            get => _background;
            set
            {
                if (_background != value)
                {
                    _background = value;
                    OnPropertyChanged("Background");
                }
            }
        }
        private CheckerViewModel? _checker;


        public CheckerViewModel Checker
        {
            get => _checker;
            set
            {
                if (_checker != value)
                {
                    _checker = value;
                    OnPropertyChanged("Checker");
                }
            }

        }

        private bool isHighlighted;
        public bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                isHighlighted = value;
                OnPropertyChanged("IsHighlighted");
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public ICommand CellClick { get; }


        private double _cellSize;
        public double CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                OnPropertyChanged("CellSize");
            }
        }

        public CellViewModel(int row, int col, ICommand command)
        {
            Row = row;
            Col = col;
            CellClick = command;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    class BoardViewModel : INotifyPropertyChanged
    {
        public Board _board;
        private double _cellSize;

        public double CellSize
        {
            get => _cellSize;
            set
            {
                if (value != _cellSize)
                {
                    _cellSize = value;
                    OnPropertyChanged("CellSize");
                }
            }
        }
        public ObservableCollection<CellViewModel> Cells { get; set; }
        public ICommand CellClickCommand { get; }
        public CellViewModel _selectedCell;
        public CellViewModel SelectedCell
        {
            get => _selectedCell;
            set
            {
                if (_selectedCell != null) 
                    _selectedCell.IsSelected = false;
                _selectedCell = value;
                if (_selectedCell != null) 
                    _selectedCell.IsSelected = true;
                OnPropertyChanged();
            }
        }

        public BoardViewModel()
        {

            CellClickCommand = new RelayCommand(param => Move((CellViewModel)param));

            _board = new Board();
            CellSize = 60;

            Cells = new ObservableCollection<CellViewModel>();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0)
                    {
                        var cell = new CellViewModel(i, j, CellClickCommand)
                        {
                            CellSize = this.CellSize
                        };

                        var checker = _board.Cells[i, j];
                        if (checker != null)
                        {
                            cell.Checker = new CheckerViewModel(checker);
                        }

                        Cells.Add(cell);
                    }
                }
            }

            _selectedCell = null;
        }

        public void UpdateCellSize(double newSize)
        {
            foreach (var cell in Cells)
            {
                cell.CellSize = newSize;
            }
        }

        private void OnCellClick(CellViewModel cell)
        {
            // какой-то обработчик нажатия на ячейку пока просто показывает координаты клетки
            MessageBox.Show(cell.Row.ToString() + " " + cell.Col.ToString());
            Move(cell);

        }

        private void Move(CellViewModel cell)
        {
            List<(int, int)> AbleMoves = _board.MoveShow(cell.Row, cell.Col);
            foreach (var (i, j) in AbleMoves) {
                Cells[i*4+(j-1)/2].Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            if (cell.Checker != null && SelectedCell == null || cell.Checker == null && SelectedCell != null)
            {
                if (SelectedCell == null) { 
                    SelectedCell = cell;
                    return;
                }
                _board.Cells[cell.Col, cell.Row] = _board.Cells[SelectedCell.Col, SelectedCell.Row];
                _board.Cells[SelectedCell.Col, SelectedCell.Row] = null;
                cell.Checker = SelectedCell.Checker;
                cell.Checker.Fill = SelectedCell.Checker.Fill;
                SelectedCell.Checker = null;
                SelectedCell = null;

            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
