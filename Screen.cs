using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jstris_breaker
{
    //objet créer pour chaque Process qu'on shouaite controller.
    class Screen
    {
        //cette structure va contenir la position du point en haut à gauche et en bas à droite de la fenêtre.
        //on fait appel à des fonctions native de windows qui écrit les données dans un pointeur.
        //on à besoin de dire au compilateur que les variables de la structure doivent être "à la suite" dans la mémoire de l'ordinateur.
        //https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rect
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        //renvoie la position du point en haut à gauche et en bas à droite.
        //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        //isvalid: vrai si les dimensions de la fenêtre ne sont pas nul. faux sinon
        private bool isValid;

        //contient les dimensions de la fenêtre. si elle n'as pas de dimension, elles auront pour valeurs -1.
        private int width, height;

        //contient le pid de la fenêtre
        private int pid;

        //contient l'emplacement du point en haut à gauche et en bas à droite de la fenêtre.
        //voir la déclaration de la structure RECT pour en savoir plus.
        private RECT rect = new RECT();

        //contient l'emplacement du point en haut à gauche de la grille
        private Point gridPos;

        //constructeur
        public Screen(Process p)
        {
            GetWindowRect(p.MainWindowHandle, ref rect);

            this.pid = p.Id;

            if (rect.bottom != 0 && rect.left != 0 && rect.right != 0 && rect.top != 0)
            {
                this.width = rect.right - rect.left;
                this.height = rect.bottom - rect.top;
                this.isValid = findGrid();
            }   
            else
            {
                this.isValid = false;
                this.width = 0;
                this.height = 0;
            }
        }

        private Bitmap takeFullProcessScreenShot()
        {
            Rectangle r = new Rectangle(0, 0, width, height);
            Bitmap bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.left, rect.top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            //bmp.Save("out.png", ImageFormat.Png);
            return bmp;
        }

        //cherche la grille sur la fenêtre
        //renvoie vrai si une grille valide est trouvé
        //renvoie faux sinon.
        private bool findGrid()
        {
            Bitmap b = takeFullProcessScreenShot();
            bool found = false; //deviens vrai si une grille valide est trouvé

            //on utilise un hashset pour des raisons de performance (on dois vérifier l'existance d'objet dans une liste qui ne fait que grandir)
            HashSet<Point> ptBlacklist = new HashSet<Point>(); //contient la liste des points déjà tester et invalide

            for (int x = 0; x < b.Width && !found; x++)
            {
                for (int y = 0; y < b.Height && !found; y++)
                {
                    if (ptBlacklist.Contains(new Point(x, y)))
                    {
                        //continue;
                    } 
                    else
                    {
                        ptBlacklist.Add(new Point(x, y));
                    }

                    Color ca = b.GetPixel(x, y);
                    //on recherche la couleur du cadre du tetris (ici, un gris avec pour valeur RGB 57,57,57)
                    if (ca.R == 57 && ca.G == 57 && ca.B == 57)
                    {
                        //si c'est le bon point gris, on devrait être en haut à gauche de la grille.
                        //on vérifie si c'est le bonne endroit car la taille de la grille est toujours fixe, peut l'importe la taille de la
                        //fenêtre du navigateur internet.
                        //pour ce faire, on va essayer de suivre le contour du tetris. et pour chaque pixel rencontrer, on vérifie si il est
                        //parmis les couleurs valide couleur.

                        List<Point> pts = new List<Point>();

                        bool invalid = false; //deviens vrai si le point n'est pas valide.
                        const int tailleX = 239;
                        const int tailleY = 477;

                        if (x + tailleX >= b.Width || y + tailleY >= b.Height)
                        {
                            continue;
                        }

                        for (int tx = x; tx < x + tailleX; tx++)
                        {
                            pts.Add(new Point(tx, y)); //ligne haut
                            pts.Add(new Point(tx, y + tailleY)); //ligne bas
                        }

                        for (int ty = y; ty < y + tailleY; ty++)
                        {
                            pts.Add(new Point(x, ty)); //ligne gauche
                            pts.Add(new Point(x + tailleX, ty)); //ligne droite
                        }

                        //on vérifie pour chaque point de la liste si la couleur est valide
                        foreach (Point p in pts)
                        {
                            Color testC = b.GetPixel(p.x, p.y);

                            //si jamais la couleur n'est pas valide
                            if (!(testC.Name == "ff393939" || testC.Name == "ff6a6a6a" || testC.Name == "ff1e1e1e" || testC.Name == "ff232323"))
                            {
                                invalid = true;
                                break;
                            }
                        }

                        //si la liste ci-dessus n'est pas valide, on ajoute tous les points à la blacklist pour éviter de vérifier deux fois le même pixel
                        //sinon, on quitte la boucle.
                        if (invalid)
                        {
                            ptBlacklist.UnionWith(pts);
                        }
                        else
                        {
                            found = true;
                            gridPos.x = x;
                            gridPos.y = y;
                            //foreach (Point p in pts)
                            //{
                            //    b.SetPixel(p.x, p.y, Color.Red);
                            //}
                        }
                    }
                }
            }
            //b.Save("test.png", ImageFormat.Png); 
            return found;
        }

        //si les dimensions du process sont différente de 0, on concidère la fenêtre comme "valide"
        public bool IsValid
        {
            get {
                return this.isValid;
            }
            
        }

        //renvoie la largeur de la fenêtre
        public int Width
        {
            get
            {
                return width;
            }
        }

        //renvoie la hauteur de la fenêtre
        public int Height
        {
            get
            {
                return height;
            }
        }

        //renvoie le PID de la fenêtre
        public int PID
        {
            get
            {
                return pid;
            }
        }
    }
}
