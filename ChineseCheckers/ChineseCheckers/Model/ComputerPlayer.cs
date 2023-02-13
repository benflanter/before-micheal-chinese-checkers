using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;


namespace ChineseCheckers.Model
{
    public class ComputerPlayer : Player
    {
        public Graph graph;
        private int outsidePieces;
        private Dictionary<int, Piece> Destinations; // A dictinary of locations in the destination base that are not occupied by rival pieces
        private Dictionary<int, Piece> Base; // A dictinary of locations in the origin base that are occupied by friendly pieces

        private int[,] weights =
        {
            { 0,0,0,0,0,0,0,0,0,0,0,0,100,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,100,0,100,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,100,0,100,0,100,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,100,0,100,0,100,0,100,0,0,0,0,0,0,0,0,0 },
            { -100,0,-60,0,-50,0,75,0,85,0,90,0,90,0,90,0,85,0,75,0,-50,0,-60,0,-100 },
            { 0,-50,0,-30,0,55,0,75,0,80,0,80,0,80,0,80,0,75,0,55,0,-30,0,-50,0 },
            { 0,0,-25,0,-5,0,55,0,65,0,70,0,70,0,70,0,65,0,55,0,-5,0,-25,0,0 },
            { 0,0,0,-15,0,-10,0,50,0,55,0,60,0,60,0,55,0,50,0,-10,0,-15,0,0,0 },
            { 0,0,0,0,-5,0,15,0,40,0,50,0,50,0,40,0,40,0,15,0,-5,0,0,0,0 },
            { 0,0,0,-5,0,10,0,30,0,40,0,40,0,40,0,40,0,30,0,10,0,-5,0,0,0 },
            { 0,0,-10,0,1,0,8,0,25,0,30,0,30,0,30,0,25,0,8,0,1,0,-10,0,0 },
            { 0,1,0,3,0,3,0,15,0,20,0,20,0,20,0,20,0,15,0,3,0,3,0,1,0 },
            { 1,0,1,0,1,0,5,0,10,0,10,0,10,0,10,0,10,0,5,0,1,0,1,0,1 },
            { 0,0,0,0,0,0,0,0,0,5,0,5,0,5,0,5,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,3,0,3,0,3,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,2,0,2,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0 },
        };

        public ComputerPlayer(bool side, Board board) : base(side, board)
        {
            this.outsidePieces = 0;
        }
        private Move ChooseMove()
        {
            List<Move> moves = GetMoves();
            int index = Heuristic(moves);
            return moves[index];
        }

        private int GiveStateOfGame()
        {
            int state = 3, countEnd = 0, countMid = 0;
            this.outsidePieces = 0;
            foreach (Piece piece in pieces.Values)
            {
                if (Board.initmat[piece.row, piece.col] != 2)
                    outsidePieces++;
                if (!(Base.Count == 0)) // if origin base is not empty
                {
                    state = 1;
                    break;
                }
                else
                {
                    if (piece.row > Board.HEIGHT / 2)
                        countMid++;
                    else if (piece.row < Board.HEIGHT / 2)
                        countEnd++;
                    if (countMid > pieces.Count / 2)
                    {
                        state = 2;
                        break;
                    }
                    if (countEnd > pieces.Count / 2)
                    {
                        state = 3;
                        break;
                    }
                }
            }
            return state;         
        }
        private Dictionary<int, Piece> GetDestinations()
        {
            Dictionary<int, Piece> Destinations = new Dictionary<int, Piece>();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 9; j < 16; j++)
                {
                    if (Board.initmat[i, j] == 2)
                    {
                        if (board.getPiece(i, j) == null)
                            Destinations.Add(i * Board.WIDTH + j, null);
                    }
                }
            }
            return Destinations;
        } // returns a dictinary of locations in the destination base that are not occupied by rival pieces

        private Dictionary<int, Piece> GetBase()
        {
            Dictionary<int, Piece> Base = new Dictionary<int, Piece>();
            for (int i = Board.HEIGHT - 4; i < Board.HEIGHT; i++)
            {
                for (int j = 9; j < 16; j++)
                {
                    if (Board.initmat[i, j] == 3)
                    {
                        if (getPiece(i, j) != null)
                            Base.Add(i * Board.WIDTH + j, null);
                    }
                }
            }
            return Base;
        } // returns a dictinary of locations in the origin base that are occupied by friendly pieces

        private int GetSMoves(List<Move> moves)
        {
            int index = -1;
            foreach (var move in moves)
            {
                index++;
                Piece piece = move.GetOrigin();
                if (piece.row - 4 == move.GetRow() && piece.col == move.GetCol())
                    return index;
            }
            return -1;
        } // returns the index of an S shaped move

        private int LongestJump(List<Move> moves)
        {
            double longest = 0;
            int currentIndex = -1;
            int index = -1;
            foreach (var move in moves)
            {
                currentIndex++;
                Piece piece = move.GetOrigin();
                int x = (int)Math.Pow(Math.Abs(piece.row - move.GetRow()), 2);
                int y = (int)Math.Pow(Math.Abs(piece.col - move.GetCol()), 2);
                double length = Math.Sqrt(x + y);
                if (longest == 0)
                {
                    longest = length;
                    index = currentIndex;
                }
                else if (piece.row > move.GetRow())
                {
                    if (length > longest)
                    {
                        longest = length;
                        index = currentIndex;
                    }
                }
            }
            return index;
        } // returns the index of the move with the longest distance bitween the origin piece location and the destiantion location

        private bool PieceEscaped(Move move)
        {
            Piece piece = move.GetOrigin();
            int key = move.GetRow() * Board.WIDTH + move.GetCol();
            return Base.ContainsKey(key) && !Base.ContainsKey(move.GetRow() * Board.WIDTH + move.GetCol());
        } // returns a boolean value if a move will make it so a piece will leave its base

        private int GetMoveWeightStart(Move move, int key)
        {
            int moveRow = move.GetRow(), moveCol = move.GetCol();
            int weight = weights[moveRow, moveCol];
            Piece piece = move.GetOrigin();
            if (PieceEscaped(move)) // check if a move will result in the removal of a piece from the origin base 
                weight += (piece.row - moveRow) * 10;
            if(piece.row - 4 == moveRow && piece.col == moveCol) // check Smove
                weight += 5;
            if (piece.row * Board.WIDTH + piece.col == key && moveRow < piece.row) // check rearmost
                weight += 5;
            return weight;
        } // returns a weight for a move during the start of the game

        private int GetMoveWeightMiddle(Move move, int key)
        {
            int moveRow = move.GetRow(), moveCol = move.GetCol();
            int weight = weights[moveRow, moveCol];
            Piece piece = move.GetOrigin();
            if (piece.row - 4 == moveRow && piece.col == moveCol) // if SMove
                weight += 10;
            if (piece.row * Board.WIDTH + piece.col == key && moveRow < piece.row) // if rearmost
                weight += (piece.row - moveRow) * 10;
            if (piece.row > Board.HEIGHT / 2)
                weight += 40;
            if (piece.row < Board.HEIGHT / 4 && moveRow > piece.row) // prevent loops near dest base
                weight -= (moveRow - piece.row) * 10;
            int x = (int)Math.Pow(Math.Abs(piece.row - moveRow), 2);
            int y = (int)Math.Pow(Math.Abs(piece.col - moveCol), 2);
            int length = (int)Math.Sqrt(x + y);
            if (!Destinations.ContainsKey(moveRow * Board.WIDTH + moveCol) && (piece.row * Board.WIDTH + piece.col) !=key)
            {
                int deviation = 0;
                foreach (KeyValuePair<int, Piece> p in pieces)
                {
                    int row = p.Value.row, col = p.Value.col;
                    int xDev = (int)Math.Pow(Math.Abs(row - piece.row), 2);
                    int yDev = (int)Math.Pow(Math.Abs(col - piece.col), 2);
                    int dev = (int)Math.Sqrt(xDev + yDev);
                    deviation += dev;
                }
                weight += (length - deviation / 5) * 5;
            }
            else
                weight += 40 * (4 - moveRow);

            return weight;
        }//  returns a weight for a move during the middle of the game
        private int GetMoveWeightEnd(Move move, int key)
        {
            int moveRow = move.GetRow(), moveCol = move.GetCol();
            int weight = weights[moveRow, moveCol];
            Piece piece = move.GetOrigin();
            if (piece.row * Board.WIDTH + piece.col == key && moveRow < piece.row) // if rearmost
                weight += (piece.row - moveRow) * 70;
            if (Board.initmat[piece.row, piece.col] == 2) // check if you can compress the pieces in the dest base
            {
                if (moveRow < piece.row)
                    weight += (piece.row - moveRow) * 5;
                else
                    weight = 0;
            }
            else if (Destinations.ContainsKey(moveRow * Board.WIDTH + moveCol)) // check if you can move a piece to dest
                weight += 70 * (4 - moveRow);
            return weight;
        } //  returns a weight for a move during the middle of the game
        private int Heuristic(List<Move> moves)
        {
            int index = -1;
            int BestWeight = 0;
            int rearMostKey = GetRearMostPiece();
            Destinations = GetDestinations();
            Base = GetBase();
            int state = GiveStateOfGame();
            switch (state)
            {
                case 1:
                    foreach (var move in moves)
                    {
                        Piece piece = move.GetOrigin();
                        if ((getPiece(13, 9) != null || getPiece(13, 15) != null) && move.GetRow() < piece.row)
                        {
                            if ((piece.row == 13 && piece.col == 9) || (piece.row == 13 && piece.col == 15))
                            {
                                index = moves.IndexOf(move);
                                break;
                            }
                        } // check if you can move forward the pieces in locations: (13,9) or (13,15)
                        else if(Base.ContainsKey(piece.row * Board.WIDTH + piece.col))
                        {
                            int weight = GetMoveWeightStart(move, rearMostKey);
                            if (weight > BestWeight)
                            {
                                BestWeight = weight;
                                index = moves.IndexOf(move);
                            }
                        } // if a move's piece is in the origin base,
                          // calculate it's weight and save the index of the move with the greatest weight
                    }
                    index = (index != -1) ? index : GetSMoves(moves);
                    index = (index != -1) ? index : LongestJump(moves);
                    // if non of the above were met, choose an SMove or the longest jump
                    break;
                case 2:
                    index = LongestJump(moves);
                    index = (index != -1) ? index : 0;
                    foreach (var move in moves)
                    {
                        Piece piece = move.GetOrigin();
                        if (Board.initmat[piece.row, piece.col] == 2)
                            continue;
                        else
                        {
                            int weight = GetMoveWeightMiddle(move, rearMostKey);
                            if (weight > BestWeight)
                            {
                                BestWeight = weight;
                                index = moves.IndexOf(move);
                            } // calculate it's weight and save the index of the move with the greatest weight
                        }
                    }
                    break;
                case 3:
                    if (outsidePieces == 1)
                    {
                        index = OnePiece(moves);
                    }
                    else
                    {
                        foreach (var move in moves)
                        {
                            Piece piece = move.GetOrigin();
                            int moveRow = move.GetRow(), moveCol = move.GetCol();
                            int key = moveRow * Board.WIDTH + moveCol;
                            if (Board.initmat[piece.row, piece.col] == 2)
                            {
                                if(moveRow < piece.row && Destinations.ContainsKey(key))
                                {
                                    index = moves.IndexOf(move);
                                    break;
                                }
                            }
                            else if (Destinations.ContainsKey(key))
                            {
                                index = moves.IndexOf(move);
                                break;
                            }
                            else
                            {
                                int weight = GetMoveWeightEnd(move, rearMostKey);
                                if (weight > BestWeight)
                                {
                                    BestWeight = weight;
                                    index = moves.IndexOf(move);
                                } // calculate it's weight and save the index of the move with the greatest weight
                            }
                        }
                    }
                    break;
            }
            return index; 
        }

        private int OnePiece(List<Move> moves)
        {
            Piece piece;
            int index = -1;
            foreach (var move in moves)
            {
                Piece originPiece = move.GetOrigin();
                if (Board.initmat[originPiece.row, originPiece.col] != 2)
                {
                    piece = originPiece;
                }
                int moveRow = move.GetRow(), moveCol = move.GetCol();
                if (Destinations.ContainsKey(moveRow * Board.WIDTH + moveCol))
                {
                    index = moves.IndexOf(move);
                    break;
                }
            }
            if (index ==-1)
            {
                int bestweight = 0;
                //implement weights for single piece
            }
            return index;
        }

        public void MakeMove()
        {
            Move move = ChooseMove();
            if (move != null)
            {
                addPiece(move.GetRow(), move.GetCol(), this.side);
                removePiece(move.GetOrigin());
            }
        }
    }
}
