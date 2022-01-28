using System;
using System.Diagnostics;
using System.Threading;
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
                    MessageBox.Show("This screensaver has no options that you can set yet",
                        "Doom Screen Saver",
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
                Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
                ShowSrc();
                Application.Run();
            }
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs t) {
            DialogResult result = DialogResult.Cancel;
            try {
                result = ShowThreadExceptionDialog("Windows Forms Error", t.Exception);
            } catch {
                try {
                    MessageBox.Show("Fatal Windows Forms Error",
                        "Fatal Windows Forms Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
                } finally {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
                Application.Exit();
        }

        private static DialogResult ShowThreadExceptionDialog(string title, Exception e) {
            string errorMsg = "An application error occurred. =(";
            return MessageBox.Show(errorMsg, title, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            try {
                Exception ex = (Exception)e.ExceptionObject;
                string errorMsg = "An application error occurred. =(";

                // Since we can't prevent the app from terminating, log this to the event log.
                if (!EventLog.SourceExists("ThreadException")) {
                    EventLog.CreateEventSource("ThreadException", "Application");
                }

                // Create an EventLog instance and assign its source.
                EventLog myLog = new EventLog();
                myLog.Source = "ThreadException";
                myLog.WriteEntry(errorMsg + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace);
            } catch (Exception exc) {
                try {
                    MessageBox.Show("Fatal Non-UI Error", "Fatal Non-UI Error. Could not write the error to the event log. Reason: " + exc.Message, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                } finally {
                    Application.Exit();
                }
            }
        }

        //show screen saver
        static void ShowSrc() {          
            MainForm src = new MainForm(Screen.PrimaryScreen.Bounds);
            src.Show();
        }
    }
}
