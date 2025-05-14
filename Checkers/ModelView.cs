using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Checkers
{
    class CheckerViewModel : INotifyPropertyChanged
    {
        public string ImagePath
        {
            get
            {
                if (Color && IsKing)
                    return "pack://application:,,,/Assets/white_king.png";
                else if (Color && !IsKing)
                    return "pack://application:,,,/Assets/white_checker.png";
                else if (!Color && IsKing)
                    return "pack://application:,,,/Assets/black_king.png";
                else
                    return "pack://application:,,,/Assets/black_checker.png";
            }
        }
        public Checker _checkerModel;
        public int Row { get; private set; }
        public int Column { get; private set; }
        private bool _isKing;
        public bool IsKing
        {
            get => _isKing;
            set
            {
                if (_isKing != value)
                {
                    _isKing = value;
                    OnPropertyChanged(nameof(IsKing));
                    OnPropertyChanged(nameof(ImagePath)); // дамка
                }
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


        private Brush _background = new SolidColorBrush(Color.FromRgb(69, 54, 47));

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
        public ICommand StartSettingsCommand { get; } // Настройки

        // Логика отображения экранов
        private bool _isGameScreenVisible;
        private bool _isNetworkGameScreenVisible;
        private bool _isSettingsScreenVisible;

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
                OnPropertyChanged(nameof(IsSettingsVisible));
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
                OnPropertyChanged(nameof(IsSettingsVisible));
            }
        }

        // Флаг: настройки

        public bool IsSettingsScreenVisible
        {
            get => _isSettingsScreenVisible;
            set
            {
                _isSettingsScreenVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(IsGameVisible));
                OnPropertyChanged(nameof(IsNetworkGameVisible));
                OnPropertyChanged(nameof(IsSettingsVisible));
            }
        }

        // Видимость экранов (для биндинга в XAML)
        public Visibility IsMenuVisible => (!IsGameScreenVisible && !IsNetworkGameScreenVisible) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsGameVisible => (IsGameScreenVisible && !IsNetworkGameScreenVisible) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsNetworkGameVisible => IsNetworkGameScreenVisible ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsSettingsVisible => IsSettingsScreenVisible ? Visibility.Visible : Visibility.Collapsed;

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

            // Команда запуска настроек
            StartSettingsCommand = new RelayCommand(_ => {
                IsGameScreenVisible = false;
                IsNetworkGameScreenVisible = false;
                IsSettingsScreenVisible = true;
            });

            // Возврат в меню
            BackToMenuCommand = new RelayCommand(_ => {
                IsGameScreenVisible = false;
                IsNetworkGameScreenVisible = false;
                IsSettingsScreenVisible = false;
            });
        }

        public void StartAloneGame()
        {
            IsGameScreenVisible = true;
            IsNetworkGameScreenVisible = false;
            BoardViewModel = new BoardViewModel(isNetwork: false, IsClient: false, isSinglePlayer: true);
            BoardViewModel.OnWin = winner =>
            {
                MessageBox.Show($"{winner} победили!");
                IsGameScreenVisible = false;
            };
        }

        public void StartNetworkGame()
        {
            IsGameScreenVisible = false;
            IsNetworkGameScreenVisible = true;
            BoardViewModel = new BoardViewModel();

            BoardViewModel.OnWin = winner =>
            {
                MessageBox.Show($"{winner} победили!");
                IsGameScreenVisible = false;
            };
        }

        public async void StartNetworkCreate()
        {
            BoardViewModel = new BoardViewModel(isNetwork: true, IsClient: false);
            BoardViewModel.server = new Server();
            BoardViewModel.server.OpponentMoved += move =>
            {
                Application.Current.Dispatcher.Invoke(() => BoardViewModel.OnOpponentMoved(move));
            };
            var info = await BoardViewModel.server.CreateServer();
            
            
            MessageBox.Show(info);
            IsGameScreenVisible = true;
            IsNetworkGameScreenVisible = false;

            
            BoardViewModel.OnWin = winner =>
            {
                MessageBox.Show($"{winner} победили!");
                IsGameScreenVisible = false;
            };
        }

        public async void StartNetworkConnect()
        {
            ConnectServer IpWindow = new ConnectServer();

            if (IpWindow.ShowDialog() == true)
                MessageBox.Show($"Ip адрес получен: {IpWindow.IpAdress.ToString()}");
            else
                return;
            BoardViewModel = new BoardViewModel(isNetwork: true, IsClient: true);

            BoardViewModel.client = new Client();
            BoardViewModel.client.OpponentMoved += move =>
            {
                Application.Current.Dispatcher.Invoke(() => BoardViewModel.OnOpponentMoved(move));
            };
            bool flag = await BoardViewModel.client.Connect(IpWindow.IpAdress);
            if (flag)
            {
                IsGameScreenVisible = true;
                IsNetworkGameScreenVisible = false;
                BoardViewModel.OnWin = winner =>
                {
                    MessageBox.Show($"{winner} победили!");
                    IsGameScreenVisible = false;
                };
            }

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

        private readonly bool _isSinglePlayer;
        private MinimaxBot _bot;


        public Action<string>? OnWin;

        private int _player1Score;
        public int Player1Score
        {
            get => _player1Score;
            set
            {
                if (_player1Score != value)
                {
                    _player1Score = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _player2Score;
        public int Player2Score
        {
            get => _player2Score;
            set
            {
                if (_player2Score != value)
                {
                    _player2Score = value;
                    OnPropertyChanged();
                }
            }
        }
        public Server? server;
        public Client? client;


        private Board _board; // Модель доски
        private double _cellSize; // Размер клетки

        
        // Коллекция клеток, которую будем отображать в UI
        public ObservableCollection<CellViewModel> Cells { get; set; }

        // Команда нажатия на клетку
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

        private readonly bool _isClient;
        // Сетевая или нет игра
        bool IsNetWork;

        public void OnOpponentMoved(int[] data)
        {
            UpdateBoard(data);
            IsWhiteTurn = !IsWhiteTurn;
        }


        //Чья очередь ходить
        bool IsWhiteTurn = true;

        // Конструктор
        public BoardViewModel(bool isNetwork = false, bool IsClient = false, bool isSinglePlayer = false)
        {
            IsNetWork = isNetwork;
            _isClient = IsClient;
            _isSinglePlayer = isSinglePlayer;
            if (_isSinglePlayer)
                _bot = new MinimaxBot(aiColor: false /*false = чёрный*/, maxDepth: 6);

            // если сетевая – ходят белые, а затем чёрные (сервер → клиент)
            IsWhiteTurn = isNetwork ? true : true;

            _board = isNetwork
                ? new Board(_isClient) // инвертирует доску для клиента
                : new Board();         // локальная игра
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

        private async Task ReplaceChecker(CellViewModel cell, List<(int Row, int Col)> mypath)
        {
            //if (cell == null)
            //    return;
            //if (SelectedCell == null)
            //    return;
            int row = SelectedCell.Row;
            int col = SelectedCell.Col;
            mypath.RemoveAt(0);

            foreach (var (i, j) in mypath)
            {
                if (SelectedCell.Checker._checkerModel.IsKing)
                {
                    var x = i - row > 0 ? 1 : -1;
                    var y = j - col > 0 ? 1 : -1;
                    int p = 1;
                    var r = row + x * p;
                    var c = col + y * p;
                    
                    while (InBoard(r,c) && _board.Cells[r, c] == null && r != cell.Row && c != cell.Col)
                    {
                        p++;
                        r = row + x * p;
                        c = col + y * p;
                    }
                    if (cell.Row != r || cell.Col != c)
                    {
                        _board.Cells[r, c] = null;
                        Cells[r * 4 + c / 2].Checker = null;
                        row = i;
                        col = j;
                        if (IsWhiteTurn)
                            Player1Score++;
                        else
                            Player2Score++;
                    }
                }
                else if ((Math.Abs(row - i) == 2 || Math.Abs(col - j) == 2) && cell.Row == mypath[mypath.Count - 1].Item1 && cell.Col == mypath[mypath.Count - 1].Item2)
                {
                    var x = i - row > 0 ? i - 1 : i + 1;
                    var y = j - col > 0 ? j - 1 : j + 1;

                    _board.Cells[x, y] = null;
                    Cells[x * 4 + y / 2].Checker = null;
                    row = i;
                    col = j;
                    if (IsWhiteTurn)
                        Player1Score++;
                    else
                        Player2Score++;

                }
            }
            // сам ход 
            if (cell.Row == mypath[mypath.Count - 1].Item1 && cell.Col == mypath[mypath.Count - 1].Item2 || SelectedCell.Checker._checkerModel.IsKing)
            {
                string send = "";
                if (client != null || server != null)
                {
                    int isKing = SelectedCell.Checker._checkerModel.IsKing ? 1 : 0;
                    send += $"{Math.Abs(SelectedCell.Row - 7)} {Math.Abs(SelectedCell.Col - 7)} ";
                    foreach (var (i,j) in mypath)
                    {
                        send += $"{Math.Abs(i - 7)} {Math.Abs(j - 7)} ";
                    }
                    send += $"{Player1Score} {Player2Score} "; // белые черные
                    send += $"{isKing}";
                    
                }
                if (server != null)
                    await server.SendAsync(send);

                else if (client != null)
                    await client.SendAsync(send);


                if (cell.Row != SelectedCell.Row || cell.Col != SelectedCell.Col)
                {
                    _board.Cells[cell.Row, cell.Col] = _board.Cells[SelectedCell.Row, SelectedCell.Col];

                    _board.Cells[SelectedCell.Row, SelectedCell.Col] = null;

                    var cellfromCells = Cells[SelectedCell.Row * 4 + SelectedCell.Col / 2].Checker;
                    var cellfromSelected = SelectedCell.Checker;
                    cell.Checker = SelectedCell.Checker;
                    cell.Checker.Fill = SelectedCell.Checker.Fill;

                    

                    SelectedCell.Checker = null;
                    SelectedCell = null;
                }
                if ((cell.Row == 0 && cell.Checker._checkerModel.IsWhite) || (cell.Row == 7 && !cell.Checker._checkerModel.IsWhite))
                {
                    cell.Checker.IsKing = true;
                 
                    cell.Checker._checkerModel.IsKing = true;

                }
            }

            if (Player1Score >= 12)
            {
                if (client != null)
                {
                    await client.SendAsync("END");
                    client.Disconnect();
                }
                if (server != null)
                {
                    await server.SendAsync("END");
                    await server.Stop();
                }
                OnWin?.Invoke("Белые");
            }
            else if (Player2Score >= 12)
            {
                if (client != null)
                {
                    await client.SendAsync("END");
                    client.Disconnect();
                }
                if (server != null)
                {
                    await server.SendAsync("END");
                    await server.Stop();
                }
                OnWin?.Invoke("Чёрные");
            }

        }

        
        // Обработка нажатия на клетку
        private async void Move(CellViewModel cell)
        {
            
            ResetCellBackgrounds();
            if (IsNetWork)
            {
                if (IsWhiteTurn && _isClient) return;   // белые ходят, но мы клиент (чёрные)
                if (!IsWhiteTurn && !_isClient) return; // чёрные ходят, но мы сервер (белые)
            }
            List<List<(int, int)>> allPaths = new List<List<(int, int)>>();

            // Обработка очередности ходов белые-чёрные
            if (cell.Checker != null && cell.Checker._checkerModel.IsWhite != IsWhiteTurn && client == null && server == null)
            {
                return;
            }

            var forcedChecker = _board.HasForcedChecker(IsWhiteTurn);

            if (forcedChecker.Count > 0)
            {
                if (SelectedCell != null)
                {
                    var pathsFromSelected = _board.GetPath(SelectedCell.Row, SelectedCell.Col);
                    foreach (var path in pathsFromSelected)
                    {
                        if (path[^1].Item1 == cell.Row && path[^1].Item2 == cell.Col)
                        {
                            await ReplaceChecker(cell, path);
                            ResetCellBackgrounds();
                            SelectedCell = null;

                            var newForced = _board.HasForcedChecker(IsWhiteTurn);
                            if (newForced.Count > 0)
                                return;

                            IsWhiteTurn = !IsWhiteTurn;

                            if (_isSinglePlayer && !IsWhiteTurn)
                            {
                                var botMove = await Task.Run(() => _bot.GetMove(_board));

                                var fromCell = Cells[botMove.FromX * 4 + botMove.FromY / 2];
                                SelectedCell = fromCell;
                                var toCell = Cells[botMove.ToX * 4 + botMove.ToY / 2];
                                var botPaths = _board.GetPath(botMove.FromX, botMove.FromY);
                                var botPath = botPaths.First(p => p.Last().Row == botMove.ToX
                                                                && p.Last().Col == botMove.ToY);

                                await ReplaceChecker(toCell, botPath);
                                SelectedCell = null;
                                IsWhiteTurn = !IsWhiteTurn;
                            }

                            return;
                        }
                    }
                }

                if (cell.Checker == null)
                    return;

                var isForced = forcedChecker.Any(fc => fc.Item1 == cell.Row && fc.Item2 == cell.Col);
                if (!isForced)
                {
                    ResetCellBackgrounds();
                    return;
                }

                var thisPaths = _board.GetPath(cell.Row, cell.Col);
                if (thisPaths.All(p => p.Count <= 1))
                {
                    ResetCellBackgrounds();
                    return;
                }

                SelectedCell = cell;
                ResetCellBackgrounds();

                foreach (var path in thisPaths)
                {
                    foreach (var (i, j) in path)
                    {
                        if ((cell.Row != i || cell.Col != j) && !cell.Checker._checkerModel.IsKing)
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                        else if (cell.Checker._checkerModel.IsKing && (cell.Row != i || cell.Col != j))
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    }

                    Cells[path[^1].Item1 * 4 + path[^1].Item2 / 2].Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                }

                return;
            }


            if (cell.Checker != null)
            {
                if (cell.Checker._checkerModel.IsWhite && client != null)
                    return;
                if (!cell.Checker._checkerModel.IsWhite && server != null)
                    return;
                if (SelectedCell != null)
                {
                    var oldpath = _board.GetPath(SelectedCell.Row, SelectedCell.Col);
                    foreach (var path in oldpath)
                    {
                        if (path[path.Count - 1].Row == cell.Row && path[path.Count - 1].Col == cell.Col)
                        {
                            ReplaceChecker(cell, path);
                            foreach (var p in oldpath)
                            {
                                foreach (var (i, j) in p)
                                {
                                    Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(69, 54, 47));
                                }
                            }
                            return;
                        }
                        foreach (var (i, j) in path)
                        {
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(69, 54, 47));
                        }
                    }
                }
                allPaths = _board.GetPath(cell.Row, cell.Col);
                foreach (var path in allPaths)
                {
                    foreach (var (i, j) in path)
                    {
                        if ((cell.Row != i || cell.Col != j) && !cell.Checker._checkerModel.IsKing)
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                        else if (cell.Checker._checkerModel.IsKing && (cell.Row != i || cell.Col != j))
                            Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    }
                    Cells[path[path.Count - 1].Item1 * 4 + path[path.Count - 1].Item2 / 2].Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));

                }
            }
            if (SelectedCell != null && SelectedCell.Checker != null && cell.Checker != null)
            {
                if (forcedChecker.Count > 0) {
                    return;
                }

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

                var oldPaths = _board.GetPath(SelectedCell.Row, SelectedCell.Col);
                foreach (var p in oldPaths)
                    foreach (var (i, j) in p)
                        Cells[i * 4 + j / 2].Background = new SolidColorBrush(Color.FromRgb(119, 149, 86));

                if (!oldPaths.Any(p => p.Last().Row == cell.Row && p.Last().Col == cell.Col))
                {
                    SelectedCell = null;
                    return;
                }

                var myPath = oldPaths.First(p => p.Last().Row == cell.Row && p.Last().Col == cell.Col);
                await ReplaceChecker(cell, myPath);

                IsWhiteTurn = !IsWhiteTurn;

                if (_isSinglePlayer && !IsWhiteTurn)
                {
                    var botMove = await Task.Run(() => _bot.GetMove(_board));

                    var fromCell = Cells[botMove.FromX * 4 + botMove.FromY / 2];
                    SelectedCell = fromCell;

                    var toCell = Cells[botMove.ToX * 4 + botMove.ToY / 2];
                    var botPaths = _board.GetPath(botMove.FromX, botMove.FromY);
                    var botPath = botPaths.First(p => p.Last().Row == botMove.ToX &&
                                                      p.Last().Col == botMove.ToY);

                    await ReplaceChecker(toCell, botPath);
                    SelectedCell = null;

                    IsWhiteTurn = !IsWhiteTurn;
                }

                return;
            }

        }

        // Сброс фона
        private void ResetCellBackgrounds()
        {
            foreach (var c in Cells)
                c.Background = new SolidColorBrush(Color.FromRgb(69, 54, 47));
        }


        public async void UpdateBoard(int[] data)
        {
            bool IsKing = data[data.Length-1] == 1 ? true : false;
            int p1 = data[data.Length-3];
            int p2 = data[data.Length-2];
            Player1Score = p1;
            Player2Score = p2;
            List<(int, int)> steps = new List<(int, int)>();
            for (int i = 0; i < data.Length-3; i+=2)
            {
                steps.Add((data[i], data[i+1]));
            }
            CellViewModel cell = Cells[steps[steps.Count-1].Item1 * 4 + steps[steps.Count - 1].Item2 / 2];
            CellViewModel selectedCell = Cells[steps[0].Item1 * 4 + steps[0].Item2 / 2];
            int row = selectedCell.Row;
            int col = selectedCell.Col;
            steps.RemoveAt(0);
            foreach (var (i, j) in steps)
            {
                if (selectedCell.Checker._checkerModel.IsKing)
                {
                    var x = i - row > 0 ? 1 : -1;
                    var y = j - col > 0 ? 1 : -1;
                    int p = 1;
                    var r = row + x * p;
                    var c = col + y * p;

                    while (InBoard(r, c) && _board.Cells[r, c] == null && r != cell.Row && c != cell.Col)
                    {
                        p++;
                        r = row + x * p;
                        c = col + y * p;
                    }
                    if (cell.Row != r || cell.Col != c)
                    {
                        _board.Cells[r, c] = null;
                        Cells[(r) * 4 + (c) / 2].Checker = null;
                        row = i;
                        col = j;
                    }
                }
                else if ((Math.Abs(row - i) == 2 || Math.Abs(col - j) == 2) && cell.Row == steps[steps.Count - 1].Item1 && cell.Col == steps[steps.Count - 1].Item2)
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
            if (cell.Row == steps[steps.Count - 1].Item1 && cell.Col == steps[steps.Count - 1].Item2 || selectedCell.Checker._checkerModel.IsKing)
            {
                
                if (cell.Row != selectedCell.Row || cell.Col != selectedCell.Col)
                {
                    _board.Cells[cell.Row, cell.Col] = _board.Cells[selectedCell.Row, selectedCell.Col];

                    _board.Cells[selectedCell.Row, selectedCell.Col] = null;

                    Cells[cell.Row * 4 + cell.Col / 2].Checker = Cells[selectedCell.Row * 4 + selectedCell.Col / 2].Checker;
                    Cells[cell.Row * 4 + cell.Col / 2].Checker.Fill = Cells[selectedCell.Row * 4 + selectedCell.Col / 2].Checker.Fill;

                    Cells[selectedCell.Row * 4 + selectedCell.Col / 2].Checker = null;
                    //cell.Checker = selectedCell.Checker;
                    //cell.Checker.Fill = selectedCell.Checker.Fill;
                }
                if ((cell.Row == 0 && !cell.Checker._checkerModel.IsWhite) || (cell.Row == 7 && cell.Checker._checkerModel.IsWhite))
                {
                    //cell.Checker.IsKing = true;
                    //ОСТАВИТЬ ДО ЛУЧШИХ ВРЕМЕН
                    cell.Checker.IsKing = true;

                    cell.Checker._checkerModel.IsKing = true;
                }
                    


            }
            if (Player1Score >= 12)
            {
                if (client != null)
                {
                    await client.SendAsync("END");
                    client.Disconnect();
                }
                if (server != null)
                {
                    await server.SendAsync("END");
                    await server.Stop();
                }
                OnWin?.Invoke("Белые");
            }
            else if (Player2Score >= 12)
            {
                if (client != null)
                {
                    await client.SendAsync("END");
                    client.Disconnect();
                }
                if (server != null)
                {
                    await server.SendAsync("END");
                    await server.Stop();
                }
                OnWin?.Invoke("Чёрные");
            }

        }

        private bool InBoard(int row, int col)
           => row >= 0 && row < 8 && col >= 0 && col < 8;

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
