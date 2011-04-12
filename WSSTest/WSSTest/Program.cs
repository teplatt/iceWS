/**
* Copyright (c)2011 Tracy Platt (te_platt@yahoo.com)
* 
* Dual licensed under the MIT and GPL licenses. 
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WSSTest
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
            Application.Run(new Form1());
        }
    }
}
