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
                this.isValid = true;
                this.width = rect.right - rect.left;
                this.height = rect.bottom - rect.top;
                findGrid();
            }   
            else
            {
                this.isValid = false;
                this.width = -1;
                this.height = -1;
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

        private void findGrid()
        {
            Bitmap b = takeFullProcessScreenShot();
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
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

                        for (int tx = x; tx < x + 241; tx++)
                        {
                            Color testC = b.GetPixel(tx, y);
                            if (testC.Name == "ff575757" || testC.Name == "ff6a6a6a" || testC.Name == "ff1e1e1e" || testC.Name == "ff232323")
                            {
                                pts.Add(new Point(tx, y));
                            }
                        }

                        /*
                        Color[] ct = new Color[4];
                        ct[0] = ca;
                        ct[1] = b.GetPixel(x + 241, y);
                        ct[2] = b.GetPixel(x + 241, y + 479);
                        ct[3] = b.GetPixel(x, y + 479);

                        int validPointCounter = 0;

                        //on vérifie si la couleur aux points déterminer au dessus est valide
                        for (int i = 0; i < 3; i++)
                        {
                            //les couleurs valides sont un gris RGB(57,57,57) ou RGB(106,106,106)
                            if ((ct[i].R == 57 && ct[i].G == 57 && ct[i].B == 57) || (ct[i].R == 106 && ct[i].G == 106 && ct[i].B == 106))
                            {
                                validPointCounter++;
                            }
                        }

                        //si les 4 points sont confirmés, on valide la grille
                        if (validPointCounter >= 4)
                        {
                            gridPos.x = x;
                            gridPos.y = y;
                        }
                        */
                    }
                }
            }
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
