using System;
using System.Collections.Generic;
using System.Text;

namespace RedisData
{
    public enum PieceColor
    {
        White, Black
    }
    [System.Serializable]
    public class AlgebraicMove
    {
        public string move;

        public override string ToString()
        {
            return move;
        }
    }
}
