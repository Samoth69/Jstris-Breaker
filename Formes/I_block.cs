using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jstris_breaker.Formes
{
    class I_block : Forme
    {
        public override EnumPieceType TypePiece => EnumPieceType.I_block;

        public override List<Point> getShape(int rotation)
        {
            List<Point> l = new List<Point>();
            if (rotation == 1)
            {
                for (int x = 0; x < 3; x++)
                {
                    l.Add(new Point(x, 0));
                }
            }
            return l;
        }

        public I_block()
        {

        }
    }
}
