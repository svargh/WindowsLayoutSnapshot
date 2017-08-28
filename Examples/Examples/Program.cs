using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WindowsDesktop;
using WindowsDesktop.Interop;
namespace Examples
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);           
            VirtualDesktop.someTests();
        }
    }
}
