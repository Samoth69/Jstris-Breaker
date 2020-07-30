using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jstris_breaker
{
    class Point
    {
        public int x, y;
        
        public Point()
        {
            this.x = 0;
            this.y = 0;
        }

        public Point(int x, int y)
        {
            if (x < 0)
            {
                this.x = 0;
            }
            else
            {
                this.x = x;
            }
            
            if (y < 0)
            {
                this.y = 0;
            }
            else
            {
                this.y = y;
            }
        }

        public override bool Equals(object obj)
        {
            //vérifie si l'objet est du bon type
            if (obj is Point)
            {
                Point p = (Point)obj;
                
                if (p.x == this.x && p.y == this.y)
                {
                    return true;
                } 
                else
                {
                    return false;
                }
            } 
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return int.Parse(x.ToString() + y.ToString());
        }
    }
}
