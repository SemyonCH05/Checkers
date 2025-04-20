using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    class Checker 
    {
        public bool IsKing { get; set; }
        public bool IsWhite { get; set; }

        public int FromX { get; set; }
        

        public int FromY {  get; set; }

        public int ToX {  get; set; } // нужно для сервера

        public int ToY { get; set; } // нужно для сервера

        public Checker(bool isWhite, bool isKing, int curX, int curY)
        {
            IsKing = isKing;
            IsWhite = isWhite;
            FromX = curX;
            FromY = curY;
        }

        public void MoveTo(int row, int col)
        {
            FromX = row; 
            FromY = col;
            ToX = 0;
            ToY = 0;

            if ((IsWhite && row == 0) || (!IsWhite && row == 7))
                IsKing = true;
        }
    }

    class Board
    {
        public Checker?[,] Cells { get; } = new Checker[8, 8];

        public Board()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 != 0)
                    {
                        if (i < 3)
                            Cells[i, j] = new Checker(false, false, i, j); // чёрные
                        else if (i > 4)
                            Cells[i, j] = new Checker(true, false, i, j);  // белые
                    }
                }
            }
        }
    }
}
