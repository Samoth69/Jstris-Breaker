using Jstris_breaker.Formes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static Jstris_breaker.Forme;

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
        private Point gridPos = new Point();

        //taille en pixel du tetris
        //const int tailleX = 239;
        //const int tailleY = 477;
        Size tailleGrille = new Size(239, 478);

        //nombre de cases dans le tetris
        const int nbCasesX = 10;
        const int nbCasesY = 20;

        //contient la grille du jeu.
        private List<List<Case>> Grille = new List<List<Case>>();

        public const string colorIblock = "ff0f9bd7";
        public const string colorJblock = "ff2141c6";
        public const string colorLblock = "ffe35b02";
        public const string colorOblock = "ffe39f02";
        public const string colorSblock = "ff59b101";
        public const string colorTblock = "ffaf298a";
        public const string colorZblock = "ffd70f37";

        //tableau couleurs valide pour repérer la grille de jeu
        static private string[] validColors = {
            "ff393939", //gris
            "ff6a6a6a", //pièce en gris
            "ff1e1e1e", //overlay de fin de partie
            "ff232323", //pièce en gris en dessous de overlay de partie (car l'overlay est transparent)
            colorIblock, //couleur I_block
            colorJblock, //couleur J_block
            colorLblock, //couleur L_block
            colorOblock, //couleur O_block
            colorSblock, //couleur S_block
            colorTblock, //couleur T_block
            colorZblock, //couleur Z_block
            "ff999999" //gris en partie
        };

        //couleur où on concidère que c'est une case "rempli", donc on ne peux pas metre un bout de pièce à cette endroit
        static public string[] filledColors =
        {
            colorIblock, //couleur I_block
            colorJblock, //couleur J_block
            colorLblock, //couleur L_block
            colorOblock, //couleur O_block
            colorSblock, //couleur S_block
            colorTblock, //couleur T_block
            colorZblock, //couleur Z_block
            "ff999999" //gris en partie
        };

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
                if (this.isValid)
                {
                    GenCoordsGrille();
                    ReadGrid();
                }
            }   
            else
            {
                this.isValid = false;
                this.width = 0;
                this.height = 0;
            }
        }

        private Bitmap TakeFullProcessScreenShot()
        {
            Rectangle r = new Rectangle(0, 0, width, height);
            Bitmap bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.left, rect.top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            //bmp.Save("out.png", ImageFormat.Png);
            return bmp;
        }
        
        private Bitmap TakeGridScreenShot()
        {
            Rectangle r = new Rectangle(0, 0, tailleGrille.Width, tailleGrille.Height);
            Bitmap bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.left + gridPos.x, rect.top + gridPos.y, 0, 0, tailleGrille, CopyPixelOperation.SourceCopy);
            return bmp;
        }

        //vérifie si une couleur est présente dans le tableau des couleurs valide (voir plus haut le tableau "validColors")
        //entrée: valeur ARGB avec A - Alpha, R - Red, G - Green, B - Blue. toutes ses lettres sont sur deux caractère sous format exadécimal
        //sortie: vrai si dans tableau, faux sinon.
        static public bool CheckValidColor(string ARGB, string[] tb)
        {
            foreach (string s in tb)
            {
                if (string.Equals(s, ARGB, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        //cherche la grille sur la fenêtre
        //renvoie vrai si une grille valide est trouvé
        //renvoie faux sinon.
        private bool findGrid()
        {
            Bitmap b = TakeFullProcessScreenShot();
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
                        

                        if (x + tailleGrille.Width >= b.Width || y + tailleGrille.Height >= b.Height)
                        {
                            continue;
                        }

                        for (int tx = x; tx < x + tailleGrille.Width; tx++)
                        {
                            pts.Add(new Point(tx, y)); //ligne haut
                            pts.Add(new Point(tx, y + tailleGrille.Height)); //ligne bas
                            //b.SetPixel(tx, y, Color.DeepPink);
                            //b.SetPixel(tx, y + tailleGrille.Height, Color.DeepPink);
                        }

                        for (int ty = y; ty < y + tailleGrille.Height; ty++)
                        {
                            pts.Add(new Point(x, ty)); //ligne gauche
                            pts.Add(new Point(x + tailleGrille.Width, ty)); //ligne droite
                            //b.SetPixel(x, ty, Color.DeepPink);
                            //b.SetPixel(x + tailleGrille.Width, ty, Color.DeepPink);
                        }

                        //on vérifie pour chaque point de la liste si la couleur est valide
                        foreach (Point p in pts)
                        {
                            Color testC = b.GetPixel(p.x, p.y);

                            //si jamais la couleur n'est pas valide
                            if (!CheckValidColor(testC.Name, validColors))
                            {
                                //b.SetPixel(p.x, p.y, Color.White);
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

        private void GenCoordsGrille()
        {
            int px = 0;
            int py = 0;

            for (int x = tailleGrille.Width / nbCasesX / 2; x < tailleGrille.Width; x+= tailleGrille.Width / nbCasesX + 1)
            {
                List<Case> l = new List<Case>();
                for (int y = tailleGrille.Height / nbCasesY / 2; y < tailleGrille.Height; y+= tailleGrille.Height / nbCasesY + 1)
                {
                    l.Add(new Case(new Point(x, y), new Point(px, py)));
                    py++;
                }
                Grille.Add(l);
                py = 0;
                px++;
            }
        }

        private void ReadGrid()
        {
            Bitmap b = TakeGridScreenShot();
            foreach (List<Case> l in Grille)
            {
                foreach (Case p in l)
                {
                    Color c = b.GetPixel(p.PixelX, p.PixelY);
                    p.CaseColor = c;
                }
            }
            /*
            foreach (List<Case> l in Grille)
            {
                foreach (Case p in l)
                {
                    if (p.caseStatus == Case.EnumCaseStatus.Filled)
                    {
                        b.SetPixel(p.PixelX, p.PixelY, Color.Red);
                    }
                    else
                    {
                        b.SetPixel(p.PixelX, p.PixelY, Color.Green);
                    }
                }
            }
            b.Save("out.png", ImageFormat.Png);
            */
        }

        private EnumPieceType DeterminerPiece()
        {
            for (int i = 0; i < nbCasesX; i++)
            {
                switch (Grille[i][0].CaseColor.Name)
                {
                    case colorIblock:
                        return EnumPieceType.I_block;
                    case colorJblock:
                        return EnumPieceType.J_block;
                    case colorLblock:
                        return EnumPieceType.L_block;
                    case colorOblock:
                        return EnumPieceType.O_block;
                    case colorSblock:
                        return EnumPieceType.S_block;
                    case colorTblock:
                        return EnumPieceType.T_block;
                    case colorZblock:
                        return EnumPieceType.Z_block;
                }
            }
            return EnumPieceType.none;
        }

        private struct StructBestEmplacement
        {
            public int x; //emplacement de la partie la plus à gauche de la pièce
            public int y;
            public int rotation; //0: N/A, 1:première en partant de la gauche, 4: dernière pièce en partant de la gauche. voir Rotation pièce.png
        }

        private StructBestEmplacement bestEmplacement;

        private int DeterminerMeilleurEmplacement(EnumPieceType pieceAPlacer)
        {
            bestEmplacement.x = -1;
            bestEmplacement.y = -1;
            bestEmplacement.rotation = -1;
            switch (pieceAPlacer)
            {
                case EnumPieceType.I_block:
                    //recherche meilleur emplacement avec la barre horizontal (rotation = 1)
                    for (int i = 0; i < nbCasesX - 3; i++)
                    {
                        int y = 0;
                        while (Grille[i][y].caseStatus == Case.EnumCaseStatus.Empty && Grille[i + 1][y].caseStatus == Case.EnumCaseStatus.Empty && Grille[i + 2][y].caseStatus == Case.EnumCaseStatus.Empty && Grille[i + 3][y].caseStatus == Case.EnumCaseStatus.Empty)
                        {
                            y++;
                        };

                        if (y > bestEmplacement.y)
                        {
                            bestEmplacement.x = i;
                            bestEmplacement.y = y;
                            bestEmplacement.rotation = 1;
                        }
                    }

                    //recherche meilleur emplacement avec la barre verticale (rotation = 2)
                    for (int i = 0; i < nbCasesX; i++)
                    {
                        int y = 0;
                        while (Grille[i][y].caseStatus == Case.EnumCaseStatus.Empty)
                        {
                            y++;
                        };

                        if (y > bestEmplacement.y)
                        {
                            bestEmplacement.x = i;
                            bestEmplacement.y = y;
                            bestEmplacement.rotation = 2;
                        }
                    }
                    break;
                case EnumPieceType.O_block:
                    for (int i = 0; i < nbCasesX - 1; i++)
                    {
                        int y = 0;
                        while (Grille[i][y].caseStatus == Case.EnumCaseStatus.Empty && Grille[i + 1][y].caseStatus == Case.EnumCaseStatus.Empty)
                        {
                            y++;
                        };

                        if (y > bestEmplacement.y)
                        {
                            bestEmplacement.x = i;
                            bestEmplacement.y = y;
                            bestEmplacement.rotation = 0;
                        }
                    }
                    break;
                case EnumPieceType.J_block:
                    for (int r = 1; r <= 4; r++) //rotation: 1 à 4
                    {

                    }
                    break;
            }
            return bestEmplacement.x;
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
