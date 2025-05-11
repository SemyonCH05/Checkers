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

        private bool Color; //цвет шашки 0 - черный 1 - белый

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
            Color = _checkerModel.IsWhite;
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
                    OnPropertyChanged();
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

    class ScreenViewModel : INotifyPropertyChanged
    {
        private BoardViewModel _boardViewModel;
        public BoardViewModel BoardViewModel { 
            get => _boardViewModel;
            set
            { 
                _boardViewModel = value; 
                OnPropertyChanged(nameof(BoardViewModel)); 
            } 
        } 

        // Команды переключения экранов
        public ICommand StartGameCommandCreate { get; }
        public ICommand StartGameCommandConnect { get; }

        public ICommand StartGameCommandAlone {  get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand StartNetworkGameCommand { get; } // Сетевая игра

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

        public ScreenViewModel()
        { 

            // Начальное состояние — показываем меню
            IsGameScreenVisible = false;
            IsNetworkGameScreenVisible = false;

            // Команда запуска одиночной игры

            StartGameCommandAlone = new RelayCommand(_ => StartAloneGame());

            // Команды для запуска сетевой игры взависимости от выбора кнопки

            StartGameCommandCreate = new RelayCommand(_ => StartNetworkCreate());

            StartGameCommandConnect = new RelayCommand(_ => StartNetworkConnect());

            // Команда запуска сетевой игры
            StartNetworkGameCommand = new RelayCommand(_ => StartNetworkGame());

            // Возврат в меню
            BackToMenuCommand = new RelayCommand(_ => {
                IsGameScreenVisible = false;
                IsNetworkGameScreenVisible = false;
            });
        }

        public void StartAloneGame()
        {
            IsGameScreenVisible = true;
            IsNetworkGameScreenVisible = false;
            BoardViewModel = new BoardViewModel();
        }

        public void StartNetworkGame()
        {
            IsGameScreenVisible = false;
            IsNetworkGameScreenVisible = true;
        }

        public async void StartNetworkCreate()
        {
            BoardViewModel = new BoardViewModel();
            BoardViewModel.server = new Server();
            BoardViewModel.server.OpponentMoved += move =>
            {
                Application.Current.Dispatcher.Invoke(() => BoardViewModel.OnOpponentMoved(move));
            };
            var info = await BoardViewModel.server.CreateServer();
            MessageBox.Show(info);
            IsGameScreenVisible = true;
            IsNetworkGameScreenVisible = false;
        }

        public async void StartNetworkConnect()
        {
            ConnectServer IpWindow = new ConnectServer();

            if (IpWindow.ShowDialog() == true)
            {
                MessageBox.Show($"Ip адрес получен: {IpWindow.IpAdress.ToString()}");
            }

            BoardViewModel = new BoardViewModel(true);

            BoardViewModel.client = new Client();
            BoardViewModel.client.OpponentMoved += move =>
            {
                Application.Current.Dispatcher.Invoke(() => BoardViewModel.OnOpponentMoved(move));
            }; ;
            await BoardViewModel.client.Connect(IpWindow.IpAdress);


            IsGameScreenVisible = true;
            IsNetworkGameScreenVisible = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // ViewModel доски
    class BoardViewModel : INotifyPropertyChanged
    {
        public Server? server;
        public Client? client;


        private Board _board; // Модель доски
        private double _cellSize; // Размер клетки

        
        // Коллекция клеток, которую будем отображать в UI
        public ObservableCollection<CellViewModel> Cells { get; set; }

        // Команда нажатия на клетку
        public ICommand CellClickCommand { get; }
        public CellViewModel _selectedCell;

        

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

        // Сетевая или нет игра
        bool IsNetWork;

        public void OnOpponentMoved(int[] data)
        {
            UpdateBoard(data);
        }

        // Конструктор
        public BoardViewModel(bool isNetwork = false)
        {
            IsNetWork = isNetwork;
            if (IsNetWork) 
                _board = new Board(true);
            else
                _board = new Board();
            CellSize = 60;

            // Обработка клика по клетке
            CellClickCommand = new RelayCommand(param => Move((CellViewModel)param));

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
        private async void Move(CellViewModel cell)
        {
            List<List<(int, int)>> paths = new List<List<(int, int)>>();

            if (cell.Checker != null)
            {
                if (SelectedCell != null)
                {
                    var oldpath = _board.GetPath(SelectedCell.Row, SelectedCell.Col);
                    foreach (var path in oldpath)
                    {
                        foreach (var (i, j) in path)
                        {
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(119, 149, 86));
                        }
                    }
                }
                paths = _board.GetPath(cell.Row, cell.Col);
                foreach (var path in paths)
                {
                    foreach (var (i, j) in path)
                    {
                        if ((cell.Row != i || cell.Col != j) && !cell.Checker._checkerModel.IsKing)
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                        else if (cell.Checker._checkerModel.IsKing && (cell.Row != i || cell.Col != j))
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    }
                    Cells[path[path.Count - 1].Item1 * 4 + path[path.Count - 1].Item2 / 2].Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    
                }
            }
            if (SelectedCell != null && SelectedCell.Checker != null && cell.Checker != null)
            {
                SelectedCell = cell;
                return;
            }
            if (cell.Checker != null && SelectedCell == null || cell.Checker == null && SelectedCell != null)
            {

                if (SelectedCell == null)
                {
                    SelectedCell = cell;
                    return;
                }

                var oldpaths = _board.GetPath(SelectedCell.Row, SelectedCell.Col);
                foreach (var path in oldpaths)
                {
                    foreach (var (i, j) in path)
                    {
                        Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(119, 149, 86));
                    }
                }
                bool canMove = false;
                List<(int, int)> mypath = new List<(int, int)>();
                foreach (var path in oldpaths)
                {
                    foreach (var (i, j) in path)
                    {
                        if (Cells[i * 4 + j / 2] == cell)
                        {
                            canMove = true;
                            mypath = path;
                            break;
                        }
                    }
                }
                if (canMove)
                {
                    int row = SelectedCell.Row;
                    int col = SelectedCell.Col;
                    foreach (var (i, j) in mypath)
                    {
                        if (SelectedCell.Checker._checkerModel.IsKing)
                        {
                            var x = i - row > 0 ? 1 : - 1;
                            var y = j - col > 0 ? 1 : - 1;
                            int p = 1;
                            var r = row + x * p;
                            var c = col + y * p;
                            if (i == SelectedCell.Row && j == SelectedCell.Col)
                                continue;
                            while (r < 8 && r >= 0 && c < 8 && c >= 0 && _board.Cells[r, c] == null && r != cell.Row && c != cell.Col)
                            {
                                p++;
                                r = row + x * p;
                                c = col + y * p;
                            }
                            if (cell.Row != r || cell.Col != c)
                            {
                                _board.Cells[row + x * p, col + y * p] = null;
                                Cells[(row + x * p) * 4 + (col + y * p) / 2].Checker = null;
                                row = i;
                                col = j;
                            }
                        }
                        else if ((Math.Abs(row - i) == 2 || Math.Abs(col - j) == 2) && cell.Row == mypath[mypath.Count-1].Item1 && cell.Col == mypath[mypath.Count-1].Item2)
                        {
                            var x = i - row > 0 ? i - 1 : i + 1;
                            var y = j - col > 0 ? j - 1 : j + 1;

                            _board.Cells[x, y] = null;
                            Cells[x * 4 + y / 2].Checker = null;
                            row = i;
                            col = j;

                        }
                    }
                    // сам ход 
                    if (cell.Row == mypath[mypath.Count - 1].Item1 && cell.Col == mypath[mypath.Count - 1].Item2 || SelectedCell.Checker._checkerModel.IsKing)
                    {
                        

                        if (server != null)
                        {
                            
                            int isKing = SelectedCell.Checker._checkerModel.IsKing ? 1 : 0;
                            string send = $"{Math.Abs(SelectedCell.Row - 7)} {Math.Abs(SelectedCell.Col - 7)} {Math.Abs(cell.Row - 7)} {Math.Abs(cell.Col - 7)} {isKing}";
                            await server.SendAsync(send);
                        }

                        else if (client != null)
                        {
                            int isKing = SelectedCell.Checker._checkerModel.IsKing ? 1 : 0;
                            string send = $"{Math.Abs(SelectedCell.Row - 7)} {Math.Abs(SelectedCell.Col - 7)} {Math.Abs(cell.Row - 7)} {Math.Abs(cell.Col - 7)} {isKing}";
                            await client.SendAsync(send);
                        }

                        
                        var x = mypath.ToString();
                        _board.Cells[cell.Row, cell.Col] = _board.Cells[SelectedCell.Row, SelectedCell.Col];
                        
                        _board.Cells[SelectedCell.Row, SelectedCell.Col] = null;

                        var cellfromCells = Cells[SelectedCell.Row*4 + SelectedCell.Col/2].Checker;
                        var cellfromSelected = SelectedCell.Checker;
                        cell.Checker = SelectedCell.Checker;
                        cell.Checker.Fill = SelectedCell.Checker.Fill;

                        if (cell.Row == 0 || cell.Row == 7)
                            cell.Checker._checkerModel.IsKing = true;

                        SelectedCell.Checker = null;
                        SelectedCell = null;

                    }
                }
            }

        }

        public void UpdateBoard(int[] data)
        {
            // ДОБАВИТЬ МЕТОД У РИНАТА
            int r1 = data[0];
            int c1 = data[1];
            int r2 = data[2];
            int c2 = data[3];
            bool IsKing = data[4] == 1 ? true : false;

            _board.Cells[r2, c2] = _board.Cells[r1, c1];
            _board.Cells[r1, c1] = null;

            Cells[r2 * 4 + c2 / 2].Checker = Cells[r1 * 4 + c1 / 2].Checker;
            Cells[r2 * 4 + c2 / 2].Checker.Fill = Cells[r1 * 4 + c1 / 2].Checker.Fill;

            Cells[r1 * 4 + c1 / 2].Checker = null;
            //Cells[r1 * 4 + c1 / 2] = null;
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
