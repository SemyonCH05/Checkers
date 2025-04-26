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

    public class MainViewModel : INotifyPropertyChanged
    {

        private bool _isGameScreenVisible;
        public bool IsGameScreenVisible
        {
            get => _isGameScreenVisible;
            set
            {
                _isGameScreenVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(IsGameVisible));
            }
        }
        public Visibility IsMenuVisible => IsGameScreenVisible ? Visibility.Collapsed : Visibility.Visible;
        public Visibility IsGameVisible => IsGameScreenVisible ? Visibility.Visible : Visibility.Collapsed;

        // Команды
        public ICommand StartGameCommand { get; }
        public ICommand BackToMenuCommand { get; }

        public MainViewModel()
        {
            IsGameScreenVisible = false; //заменить на false
            StartGameCommand = new RelayCommand(_ => IsGameScreenVisible = true);
            BackToMenuCommand = new RelayCommand(_ => IsGameScreenVisible = false); // новая команда
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


    }
    class CheckerViewModel : INotifyPropertyChanged
    {
        public Checker _checker;
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

        private bool Color => _checker.IsWhite; //цвет шашки 0 - черный 1 - белый

        public Brush Fill => Color ? Brushes.White : Brushes.Black;

        public CheckerViewModel(Checker checker)
        {
            _checker = checker;
            Row = checker.FromX;
            Column = checker.FromY;
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
        public Brush Background => new SolidColorBrush(Color.FromRgb(119, 149, 86));
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

    // ViewModel доски, объединённый с логикой переключения экранов меню/игры/сетевой игры
    class BoardViewModel : INotifyPropertyChanged
    {
        private Board _board; // Модель доски
        private double _cellSize; // Размер клетки

        
        // Коллекция клеток, которую будем отображать в UI
        public ObservableCollection<CellViewModel> Cells { get; set; }

        // Команда нажатия на клетку
        public ICommand CellClickCommand { get; }
        public CellViewModel _selectedCell;

        // Команды переключения экранов
        public ICommand StartGameCommand { get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand StartNetworkGameCommand { get; } // Сетевая игра

        // Выделенная клетка
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

        //public BoardViewModel()
         // Логика отображения экранов
         private bool _isGameScreenVisible;
        private bool _isNetworkGameScreenVisible;

        // Флаг: обычная игра
        public bool IsGameScreenVisible
        {
            get => _isGameScreenVisible;
            set
            {
                _isGameScreenVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(IsGameVisible));
                OnPropertyChanged(nameof(IsNetworkGameVisible));
            }
        }

        //CellClickCommand = new RelayCommand(param => OnCellClick((CellViewModel) param));
        // Флаг: сетевая игра
        public bool IsNetworkGameScreenVisible
        {
            get => _isNetworkGameScreenVisible;
            set
            {
                _isNetworkGameScreenVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(IsGameVisible));
                OnPropertyChanged(nameof(IsNetworkGameVisible));
            }
        }

        // Видимость экранов (для биндинга в XAML)
        public Visibility IsMenuVisible => (!IsGameScreenVisible && !IsNetworkGameScreenVisible) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsGameVisible => (IsGameScreenVisible && !IsNetworkGameScreenVisible) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsNetworkGameVisible => IsNetworkGameScreenVisible ? Visibility.Visible : Visibility.Collapsed;

        // Размер клетки (используется при ресайзе окна)
        public double CellSize
        {
            get => _cellSize;
            set
            {
                if (value != _cellSize)
                {
                    _cellSize = value;
                    OnPropertyChanged();
                }
            }
        }

        // Конструктор
        public BoardViewModel()
        {
            _board = new Board();
            CellSize = 60;

            // Начальное состояние — показываем меню
            IsGameScreenVisible = false;
            IsNetworkGameScreenVisible = false;

            // Команда запуска одиночной игры
            StartGameCommand = new RelayCommand(_ => {
                IsGameScreenVisible = true;
                IsNetworkGameScreenVisible = false;
            });

            // Команда запуска сетевой игры
            StartNetworkGameCommand = new RelayCommand(_ => {
                IsGameScreenVisible = false;
                IsNetworkGameScreenVisible = true;
            });

            // Возврат в меню
            BackToMenuCommand = new RelayCommand(_ => {
                IsGameScreenVisible = false;
                IsNetworkGameScreenVisible = false;
            });

            // Обработка клика по клетке
            CellClickCommand = new RelayCommand(param => OnCellClick((CellViewModel)param));

            // Генерация клеток доски
            Cells = new ObservableCollection<CellViewModel>();
            _selectedCell = null;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0) // Только чёрные клетки
                    {
                        var cell = new CellViewModel(i, j, CellClickCommand)
                        {
                            CellSize = this.CellSize
                        };

                        var checker = _board.Cells[i, j]; // Получаем шашку из модели
                        if (checker != null)
                        {
                            cell.Checker = new CheckerViewModel(checker);
                            
                        }
                        Cells.Add(cell);
                    }
                }
            }
        }   
         // Обновление размера клеток при изменении окна
        public void UpdateCellSize(double newSize)
        {
            foreach (var cell in Cells)
            {
                cell.CellSize = newSize;
            }

        }
 
         // Обработка нажатия на клетку
        private void OnCellClick(CellViewModel cell)
        {
            // какой-то обработчик нажатия на ячейку пока просто показывает координаты клетки
            // Пока просто показываем координаты
            MessageBox.Show(cell.Row.ToString() + " " + cell.Col.ToString());
        }

        // Реализация интерфейса INotifyPropertyChanged
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
