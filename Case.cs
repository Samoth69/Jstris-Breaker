using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jstris_breaker
{
    class Case
    {
        private Point pixelPos; //position en pixel de la case par rapport au haut à droite de la fenêtre
        private Point pointPos; //position de la case dans la grille (1, 2, 3...)
        private EnumCaseStatus caseStatus_ = EnumCaseStatus.Empty;

        //indique si la case est libre ou non
        public enum EnumCaseStatus : ushort
        {
            Empty = 0,
            Filled = 1
        }

        public Case(Point pixelPos, Point pointPos)
        {
            this.pixelPos = pixelPos;
            this.pointPos = pointPos;
        }

        public int PixelX 
        {
            get
            {
                return pixelPos.x;
            }
        }

        public int PixelY
        {
            get
            {
                return pixelPos.y;
            }
        }

        public int PointX
        {
            get
            {
                return pointPos.x;
            }
        }

        public int PointY
        {
            get
            {
                return pointPos.y;
            }
        }

        public EnumCaseStatus caseStatus
        {
            get
            {
                return caseStatus_;
            }
            set
            {
                this.caseStatus_ = value;
            }
        }
    }
}
