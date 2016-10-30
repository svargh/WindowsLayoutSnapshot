using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsLayoutSnapshot {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.Out.WriteLine("Press any key to continue!sxxxs");
            Console.ReadKey();
            Application.Run(new TrayIconForm());            
        }
    }
}
