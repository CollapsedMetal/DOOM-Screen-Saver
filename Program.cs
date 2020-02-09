using System;
using System.Windows.Forms;

namespace Doom_Screen_Saver {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (args.Length > 0) {
                if (args[0].ToLower().Trim().Substring(0, 2) == "/s") //show
                {
                    //run the screen saver
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    ShowSrc();
                    Application.Run();
                } else if (args[0].ToLower().Trim().Substring(0, 2) == "/p") //preview
                  {
                    //show the screen saver preview
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm(new IntPtr(long.Parse(args[1])))); //args[1] is the handle to the preview window
                } else if (args[0].ToLower().Trim().Substring(0, 2) == "/c") //configure
                  {
                    //inform the user no options can be set in this screen saver
                    MessageBox.Show("This screensaver has no options that you can set",
                        "Blue Screen Saver",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                } else //an argument was passed, but it wasn't /s, /p, or /c, so we don't care wtf it was
                  {
                    //show the screen saver anyway
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    ShowSrc();
                    Application.Run();
                }
            } else //no arguments were passed
              {
                //run the screen saver
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ShowSrc();
                Application.Run();
            }
        }

        //show screen saver
        static void ShowSrc() {          
            MainForm src = new MainForm(Screen.PrimaryScreen.Bounds);
            src.Show();
        }
    }
}
