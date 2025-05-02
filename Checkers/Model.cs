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
            while (!ct.IsCancellationRequested)
            {
                var message = await _reader.ReadLineAsync();
                if (message == null || message == "END")
                    break;
                MessageReceived?.Invoke(message);
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
            return $"Сервер запущен на {localIp}:{endp.Port}";
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
            int[] result = new int[5];
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

        public async Task Connect(string ip)
        {
            await _tcpClient.ConnectAsync(ip, 8888);
            _peer = new NetworkPeer(_tcpClient.GetStream());
            _peer.MessageReceived += OnMessage;
        }

        private void OnMessage(string mes)
        {
            var message = Decode(mes);
            OpponentMoved?.Invoke(message);
        }

        private int[] Decode(string send)
        {
            string[] words = send.Split(' ');
            int[] result = new int[5];
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
    class Checker
    {
        public bool IsKing { get; set; }
        public bool IsWhite { get; set; }

        public int FromX { get; set; }


        public int FromY { get; set; }

        public int ToX { get; set; } // нужно для сервера

        public int ToY { get; set; } // нужно для сервера

        public Checker(bool isWhite, bool isKing, int curX, int curY)
        {
            IsKing = isKing;
            IsWhite = isWhite;
            FromX = curX;
            FromY = curY;
        }

       
    }

    class Board
    {
        public Checker?[,] Cells { get; } = new Checker[8, 8];

        public bool IsNetwork;

        public Board(bool isNetwork = false)
        {
            IsNetwork = isNetwork;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0)
                    {
                        if (!IsNetwork)
                        {
                            if (i < 3)
                                Cells[i, j] = new Checker(false, false, i, j); // чёрные
                            else if (i > 4)
                                Cells[i, j] = new Checker(true, false, i, j);  // белые
                        }
                        else
                        {
                            if (i < 3)
                                Cells[i, j] = new Checker(true, false, i, j); // чёрные
                            else if (i > 4)
                                Cells[i, j] = new Checker(false, false, i, j);  // белые
                        }

                    }
                }
            }

        }

        public List<List<(int,int)>> GetPath(int startRow, int startCol)
        {
            List<List<(int, int)>> paths = new List<List<(int, int)>>();
            List<(int, int)> initialPath = new List<(int, int)> { (startRow, startCol) };
            if (Cells[startRow, startCol].IsKing)
            {
                return GetPathQuenn(startRow, startCol);
            }
            DFS(startRow, startCol, initialPath, paths); // поиск прыжков

            if (paths.Count == 0)
            {
                if (!IsNetwork)
                {
                    if (startRow + 1 < 8 && startCol - 1 >= 0 && Cells[startRow + 1, startCol - 1] == null && !Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow + 1, startCol - 1) });
                    if (startRow + 1 < 8 && startCol + 1 < 8 && Cells[startRow + 1, startCol + 1] == null && !Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow + 1, startCol + 1) });
                    if (startRow - 1 >= 0 && startCol - 1 >= 0 && Cells[startRow - 1, startCol - 1] == null && Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow - 1, startCol - 1) });
                    if (startRow - 1 >= 0 && startCol + 1 < 8 && Cells[startRow - 1, startCol + 1] == null && Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow - 1, startCol + 1) });
                }
                else
                {
                    if (startRow + 1 < 8 && startCol - 1 >= 0 && Cells[startRow + 1, startCol - 1] == null && Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow + 1, startCol - 1) });
                    if (startRow + 1 < 8 && startCol + 1 < 8 && Cells[startRow + 1, startCol + 1] == null && Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow + 1, startCol + 1) });
                    if (startRow - 1 >= 0 && startCol - 1 >= 0 && Cells[startRow - 1, startCol - 1] == null && !Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow - 1, startCol - 1) });
                    if (startRow - 1 >= 0 && startCol + 1 < 8 && Cells[startRow - 1, startCol + 1] == null && !Cells[startRow, startCol].IsWhite)
                        paths.Add(new List<(int, int)> { (startRow, startCol), (startRow - 1, startCol + 1) });
                }
            } 

            return paths;
        }

        private void DFS(int row, int col, List<(int, int)> path, List<List<(int, int)>> result)
        {
            bool foundJump = false;
            List<(int, int)> jumpingCells = new List<(int, int)>
            {
                (row+2, col-2),
                (row +2, col+2),
                (row -2, col-2),
                (row -2, col+2),    
            };

            foreach (var (i,j) in jumpingCells)
            {
                if (i < 8 && i >= 0 && j < 8 && j >= 0 && Cells[i, j] == null)
                {
                    var x = i - row > 0 ? i - 1 : i + 1;
                    var y = j - col > 0 ? j - 1 : j + 1;
                    if (Cells[x, y] != null && Cells[x,y].IsWhite != Cells[path[0].Item1, path[0].Item2].IsWhite && !path.Contains((i,j))) { 
                        foundJump = true;
                        var newPath = new List<(int, int)>(path)
                        {
                            (i, j)
                        };
                        DFS(i, j, newPath, result);
                    }
                }
            }

            if (!foundJump && path.Count > 1)
            {
                result.Add(path);
            }
        }

        private List<List<(int, int)>> GetPathQuenn(int startRow, int startCol)
        {
            List<List<(int, int)>> paths = new List<List<(int, int)>>();
            List<(int, int)> initialPath = new List<(int, int)> { (startRow, startCol) };
            DFSQuenn(startRow, startCol, initialPath, paths);
            if (paths.Count == 0)
            {
                initialPath.Clear();
                int r = 0;
                int c = 0;
                while (startRow + 1 + r < 8 && startCol - 1 - c >= 0 && Cells[startRow + 1 + r, startCol - 1 - c] == null)
                {
                    initialPath.Add((startRow+1+r, startCol-1-c));
                    r++;
                    c++;
                }
                if (initialPath.Count != 0) 
                    paths.Add(initialPath);
                initialPath = new List<(int, int)>();
                r = 0; c = 0;
                while (startRow + 1 + r < 8 && startCol + 1 + c < 8 && Cells[startRow + 1 + r, startCol + 1 + c] == null)
                {
                    initialPath.Add((startRow+1+r, startCol+1+c));
                    r++;
                    c++;
                }
                if (initialPath.Count != 0)
                    paths.Add(initialPath);
                initialPath = new List<(int, int)>();
                r = 0; c = 0;
                while (startRow - 1 - r >= 0 && startCol - 1 - c>= 0 && Cells[startRow - 1 -r, startCol - 1 - c] == null)
                {
                    initialPath.Add((startRow - 1 - r, startCol - 1 - c));
                    r++;
                    c++;
                }
                if (initialPath.Count != 0)
                    paths.Add(initialPath);
                initialPath = new List<(int, int)>();
                r = 0; c = 0;
                while (startRow - 1 - r >= 0 && startCol + 1 + c < 8 && Cells[startRow - 1 - r, startCol + 1 + c] == null)
                {
                    initialPath.Add((startRow - 1 - r, startCol + 1 + c));
                    r++;
                    c++;
                }
                if (initialPath.Count != 0)
                    paths.Add(initialPath);
                initialPath = new List<(int, int)>();
            }
            return paths;
        }
        private void DFSQuenn(int row, int col, List<(int, int)> currentPath, List<List<(int, int)>> paths)
        {
            bool anyCapture = false;
            var directions = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };

            foreach (var (dr, dc) in directions)
            {
                int r = row + dr;
                int c = col + dc;
                while (InBoard(r, c) && Cells[r, c] == null)
                {
                    r += dr; 
                    c += dc;
                }
                if (InBoard(r, c) && Cells[r, c] != null && Cells[r, c].IsWhite != Cells[currentPath[0].Item1, currentPath[0].Item2].IsWhite)
                {
                    int jr = r + dr, jc = c + dc;
                    while (InBoard(jr, jc) && Cells[jr, jc] == null)
                    {
                        anyCapture = true;
                        var captured = Cells[r, c];
                        Cells[r, c] = null;
                        var newPath = new List<(int, int)>(currentPath)
                        {
                            (jr, jc)
                        };
                        DFSQuenn(jr, jc, newPath, paths);
                        Cells[r, c] = captured;
                        jr += dr; 
                        jc += dc;
                    }
                }
            }

            if (!anyCapture && currentPath.Count > 1)
            {
                paths.Add(currentPath);
            }
        }

        private bool InBoard(int row, int col)
            => row >= 0 && row < 8 && col >= 0 && col < 8;

        
    }
}