using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jstris_breaker
{
    abstract class Forme
    {
        //structure qui indique la pièce à laquel on à affaire
        public enum EnumPieceType : ushort
        {
            I_block = 0, //ligne bleu clair de 4 blocks
            J_block = 1, //J blue foncé
            L_block = 2, //L orange
            O_block = 3, //carré de 2 blocks /2 blocks
            S_block = 4, //S vert
            T_block = 5, //T violet
            Z_block = 6, //Z block
            none = 7
        }

        public abstract EnumPieceType TypePiece { get; }

        public abstract List<Point> getShape(int rotation);
    }
}
