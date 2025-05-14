using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Checkers
{

    public class Move
    {
        public int FromX { get; }
        public int FromY { get; }
        public int ToX { get; }
        public int ToY { get; }
        public List<(int Row, int Col)> Path { get; }

        public Move(int fx, int fy, List<(int Row, int Col)> path)
        {
            FromX = fx;
            FromY = fy;
            Path = path;
            var last = path.Last();
            ToX = last.Row;
            ToY = last.Col;
        }
    }
    class NetworkPeer
    {
        NetworkStream _stream;
        StreamReader _reader;
        StreamWriter _writer;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public event Action<string> MessageReceived; // событие сообщение получено 

        public NetworkPeer(NetworkStream stream)
        {
            _stream = stream;
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            _ = Task.Run(() => ListenLoop(_cts.Token));
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var message = await _reader.ReadLineAsync();
                    if (message == null || message == "END")
                    {
                        Stop();
                        break;
                    }
                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ListenLoop упал: " + ex);
            }
        }

        public Task SendAsync(string message)
        {
            return _writer.WriteLineAsync(message);
        }

        public void Stop()
        {
            _cts.Cancel();
            _stream.Close();
        }
    }


    class Server
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888);
        NetworkPeer? _peer;
        

        public event Action<int[]> OpponentMoved;

        public async Task<string> CreateServer()
        {
            tcpListener.Start();
            var endp = (IPEndPoint)tcpListener.LocalEndpoint;
            var localIp = endp.Address;
            var client = await tcpListener.AcceptTcpClientAsync();

            _peer = new NetworkPeer(client.GetStream());
            _peer.MessageReceived += OnMessage;
            return $"Сервер запущен";
        }

        private void OnMessage(string mes)
        {
            var message = Decode(mes);
            OpponentMoved?.Invoke(message);
        }

        public Task SendAsync(string message)
        {
            return _peer!.SendAsync(message);
        }

        public Task Stop()
        {
            _peer?.Stop();
            tcpListener.Stop();
            return Task.CompletedTask;
        }

        private int[] Decode(string send)
        {
            string[] words = send.Split(' ');
            int[] result = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                result[i] = int.Parse(words[i]);
            }
            return result;
        }

       

        
    }

    class Client
    {
        TcpClient _tcpClient = new TcpClient();
        NetworkPeer _peer;

        public event Action<int[]> OpponentMoved;

        public async Task<bool> Connect(string ip)
        {
            try
            {
                await _tcpClient.ConnectAsync(ip, 8888);
            }
            catch (Exception ex)
            {
                return false;
            }
            _peer = new NetworkPeer(_tcpClient.GetStream());
            _peer.MessageReceived += OnMessage;
            return true;
        }

        private void OnMessage(string mes)
        {
            var message = Decode(mes);
            OpponentMoved?.Invoke(message);
        }

        private int[] Decode(string send)
        {
            string[] words = send.Split(' ');
            int[] result = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                result[i] = int.Parse(words[i]);
            }
            return result;
        }

        public Task SendAsync(string mes)
        {
            return _peer.SendAsync(mes);
        }

        public void Disconnect()
        {
            _peer.Stop();
        }

    }
    public class Checker
    {
        public bool IsKing { get; set; }
        public bool IsWhite { get; set; }

        public int FromX { get; set; }
        public int FromY { get; set; }

        public int ToX { get; set; } // нужно для сервера
        public int ToY { get; set; } // нужно для сервера

        // Путь к изображению шашки
        public string ImagePath { get; set; }

        public Checker(bool isWhite, bool isKing, int curX, int curY)
        {
            IsWhite = isWhite;
            IsKing = isKing;
            FromX = curX;
            FromY = curY;

            // Устанавливаем путь к изображению в зависимости от цвета шашки
            ImagePath = isWhite ? "Assets/white_checker.png" : "Assets/black_checker.png";
        }
    }

    public class Board
    {
        public Checker?[,] Cells { get; } = new Checker[8, 8];

        public bool Isclient;

        public Board(bool isClient = false)
        {
            Isclient = isClient;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0)
                    {
                        if (!isClient)
                        {
                            if (i < 3)
                                Cells[i, j] = new Checker(false, false, i, j); // чёрные
                            else if (i > 4)
                                Cells[i, j] = new Checker(true, false, i, j);  // белые
                        }
                        else
                        {
                            if (i < 3)
                                Cells[i, j] = new Checker(true, false, i, j); // белые
                            else if (i > 4)
                                Cells[i, j] = new Checker(false, false, i, j);  // черные
                        }

                    }
                }
            }
        

        }

        public Board Clone()
        {
            var clone = new Board(Isclient);
            // сначала очистим
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    clone.Cells[r, c] = null;
            // скопируем
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var orig = this.Cells[r, c];
                    if (orig != null)
                        clone.Cells[r, c] = new Checker(orig.IsWhite, orig.IsKing, r, c);
                }
            }
            return clone;
        }

        public void ApplyMove(Move move)
        {
            var path = move.Path;
            int row = move.FromX, col = move.FromY;
            var piece = Cells[row, col];
            Cells[row, col] = null;

            foreach (var step in path.Skip(1))
            {
                int nr = step.Row, nc = step.Col;
                if (Math.Abs(nr - row) > 1 || Math.Abs(nc - col) > 1)
                {
                    int midR = (row + nr) / 2;
                    int midC = (col + nc) / 2;
                    Cells[midR, midC] = null;
                }
                row = nr; col = nc;
            }
            piece.FromX = row;
            piece.FromY = col;
            if ((piece.IsWhite && row == 0) || (!piece.IsWhite && row == 7))
                piece.IsKing = true;
            Cells[row, col] = piece;
        }


        public List<List<(int Row, int Col)>> GetPath(int startRow, int startCol)
        {
            var result = new List<List<(int, int)>>();
            if (!InBoard(startRow, startCol) || Cells[startRow, startCol] == null)
                return result;

            var piece = Cells[startRow, startCol]!;
            var origin = (Row: startRow, Col: startCol);

            if (piece.IsKing)
            {
                var backup = Cells[origin.Row, origin.Col];
                Cells[origin.Row, origin.Col] = null;
                FindQueenPaths(origin, new List<(int, int)> { origin }, new HashSet<(int, int)>(), result, piece.IsWhite);
                Cells[origin.Row, origin.Col] = backup;

                if (result.Count == 0)
                    AddQueenSimpleMoves(origin, result);
            }
            else
            {
                var backup = Cells[origin.Row, origin.Col];
                Cells[origin.Row, origin.Col] = null;
                FindPaths(origin, new List<(int, int)> { origin }, result, piece.IsWhite);
                Cells[origin.Row, origin.Col] = backup;

                if (result.Count == 0)
                    AddSimpleMoves(origin, result, piece.IsWhite);
            }

            return result;
        }


        private void AddSimpleMoves((int Row, int Col) origin, List<List<(int, int)>> result, bool isWhite)
        {
            
            int dir = isWhite ? -1 : 1;
            if (Isclient)
                dir = -1;
            var deltas = new[] { (dir, -1), (dir, 1) };
            foreach (var (dr, dc) in deltas)
            {
                var nr = origin.Row + dr;
                var nc = origin.Col + dc;
                if (InBoard(nr, nc) && Cells[nr, nc] == null)
                    result.Add(new List<(int, int)> { origin, (nr, nc) });
            }
        }

        private void AddQueenSimpleMoves((int Row, int Col) origin, List<List<(int, int)>> result)
        {
            var directions = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };
            foreach (var (dr, dc) in directions)
            {
                var path = new List<(int, int)> { origin };
                var r = origin.Row + dr;
                var c = origin.Col + dc;
                while (InBoard(r, c) && Cells[r, c] == null)
                {
                    path.Add((r, c));
                    r += dr; c += dc;
                }
                if (path.Count > 1)
                    result.Add(path);
            }
        }

        private void FindPaths((int Row, int Col) pos, List<(int, int)> current, List<List<(int, int)>> result, bool isWhite)
        {
            bool any = false;
            var deltas = new[] { (2, 2), (2, -2), (-2, 2), (-2, -2) };
            foreach (var (dr, dc) in deltas)
            {
                var mid = (Row: pos.Row + dr / 2, Col: pos.Col + dc / 2);
                var dest = (Row: pos.Row + dr, Col: pos.Col + dc);
                if (InBoard(dest.Row, dest.Col) && Cells[dest.Row, dest.Col] == null
                    && InBoard(mid.Row, mid.Col) && Cells[mid.Row, mid.Col] != null
                    && Cells[mid.Row, mid.Col]!.IsWhite != isWhite)
                {
                    any = true;
                    var captured = Cells[mid.Row, mid.Col];
                    Cells[mid.Row, mid.Col] = null;
                    current.Add(dest);

                    FindPaths(dest, current, result, isWhite);

                    current.RemoveAt(current.Count - 1);
                    Cells[mid.Row, mid.Col] = captured;
                }
            }
            if (!any && current.Count > 1)
                result.Add(new List<(int, int)>(current));
        }

        private void FindQueenPaths((int Row, int Col) pos,List<(int, int)> current,HashSet<(int, int)> capturedSet, List<List<(int, int)>> result, bool isWhite)
        {
            bool any = false;
            var directions = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };
            foreach (var (dr, dc) in directions)
            {
                var r = pos.Row + dr;
                var c = pos.Col + dc;
                while (InBoard(r, c) && Cells[r, c] == null)
                {
                    r += dr; c += dc;
                }
                if (!InBoard(r, c) || Cells[r, c] == null)
                    continue;
                var enemy = (Row: r, Col: c);
                if (Cells[r, c]!.IsWhite == isWhite)
                    continue;
                if (capturedSet.Contains(enemy))
                    continue;
                var captured = Cells[r, c];
                Cells[r, c] = null;
                capturedSet.Add(enemy);
                var jr = r + dr;
                var jc = c + dc;
                while (InBoard(jr, jc) && Cells[jr, jc] == null)
                {
                    any = true;
                    current.Add((jr, jc));
                    FindQueenPaths((jr, jc), current, capturedSet, result, isWhite);
                    current.RemoveAt(current.Count - 1);
                    jr += dr; jc += dc;
                }
                capturedSet.Remove(enemy);
                Cells[r, c] = captured;
            }
            if (!any && current.Count > 1)
                result.Add(new List<(int, int)>(current));
        }

        private bool InBoard(int row, int col)
            => row >= 0 && row < 8 && col >= 0 && col < 8;

        //Поиск шашки, которая должна бить
        public List<(int row, int col)>HasForcedChecker(bool IsWhiteTurn)
        {
            var forced = new List<(int row, int col)>();

            for (int row = 0; row < 8; row++) {
                for (int col = 0; col < 8; col++)
                {
                    var checker = Cells[row, col];

                    if (checker != null && checker.IsWhite == IsWhiteTurn)
                    {
                        var paths = GetPath(row, col);

                        bool hasCapture = paths.Any(path =>
                        path.Count >= 2 &&
                        Enumerable.Range(1, path.Count - 1).Any(i =>
                        Math.Abs(path[i].Row - path[i - 1].Row) > 1));

                        if (hasCapture)
                            forced.Add((row, col));
                    }

                }
                

            }
            return forced;

        }


    }

    public class MinimaxBot
    {
        private readonly bool _aiColor; 
        private readonly int _maxDepth;

        public MinimaxBot(bool aiColor, int maxDepth = 6)
        {
            _aiColor = aiColor;
            _maxDepth = maxDepth;
        }

        public Move GetMove(Board board)
        {
            var forced = board.HasForcedChecker(_aiColor);
            var allMoves = new List<Move>();

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var ch = board.Cells[r, c];
                    if (ch == null || ch.IsWhite != _aiColor) continue;
                    var paths = board.GetPath(r, c);
                    foreach (var p in paths)
                    {
                        if (forced.Count == 0 || forced.Any(f => f.row == r && f.col == c))
                            allMoves.Add(new Move(r, c, p));
                    }
                }

            // минимакс
            int bestVal = int.MinValue;
            Move bestMove = allMoves[0];
            foreach (var mv in allMoves)
            {
                var cloned = board.Clone();
                cloned.ApplyMove(mv);
                int val = Minimax(cloned, _maxDepth - 1, false);
                if (val > bestVal)
                {
                    bestVal = val;
                    bestMove = mv;
                }
            }
            return bestMove;
        }

        private int Minimax(Board board, int depth, bool isMax)
        {
            if (depth == 0 || board.HasForcedChecker(!_aiColor).Concat(board.HasForcedChecker(_aiColor)).Count() == 0)
                return Evaluate(board);

            bool turn = isMax ? _aiColor : !_aiColor;
            var forced = board.HasForcedChecker(turn);
            var moves = new List<Move>();

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var ch = board.Cells[r, c];
                    if (ch == null || ch.IsWhite != turn) continue;
                    foreach (var p in board.GetPath(r, c))
                        if (forced.Count == 0 || forced.Any(f => f.row == r && f.col == c))
                            moves.Add(new Move(r, c, p));
                }

            if (isMax)
            {
                int maxEval = int.MinValue;
                foreach (var mv in moves)
                {
                    var cloned = board.Clone();
                    cloned.ApplyMove(mv);
                    maxEval = Math.Max(maxEval, Minimax(cloned, depth - 1, false));
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var mv in moves)
                {
                    var cloned = board.Clone();
                    cloned.ApplyMove(mv);
                    minEval = Math.Min(minEval, Minimax(cloned, depth - 1, true));
                }
                return minEval;
            }
        }

        private int Evaluate(Board board)
        {
            int score = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var ch = board.Cells[r, c];
                    if (ch == null) continue;
                    int v = ch.IsKing ? 200 : 100;
                    score += (ch.IsWhite == _aiColor) ? v : -v;
                }
            return score;
        }
    }



}