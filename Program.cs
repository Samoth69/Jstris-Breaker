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
    class Program
    {
        static List<Screen> procList = new List<Screen>();

        const UInt32 WM_KEYDOWN = 0x0100;
        const int VK_F5 = 0x74;
        const int VK_DOWN = 0x28;

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        static void Main(string[] args)
        {
            //*******************************************
            //Détection et optention du PID de la fenêtre
            //*******************************************

            Process[] prl = Process.GetProcessesByName("PaintDotNet");
            Console.WriteLine("searching valid firefox process...");
            foreach (Process p in prl)
            {
                Screen s = new Screen(p);
                if (s.IsValid)
                {
                    procList.Add(s);
                }
            }
            Console.WriteLine("Done");

            Console.WriteLine("Found the following valid process:");
            foreach (Screen p in procList)
            {
                Console.WriteLine("PID: {0}", p.PID);
            }
            Console.WriteLine("Press a key to start");
            Console.ReadKey();

            //****************************************************
            //Recherche de l'emplacement du plateau sur la fenêtre
            //****************************************************


        }
        /*
        static void SendKey(int key)
        {
            foreach (Process p in procList)
            {
                PostMessage(p.MainWindowHandle, WM_KEYDOWN, key, 0);
            }
        }
        */
    }
}
