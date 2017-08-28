using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsLayoutSnapshot {
    static class Program {
        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            bool use2ndIcon = args.Length == 1 && args[0].Equals("/Use2ndIcon");

        Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayIconForm(use2ndIcon));
        }
    }
}
