using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections;
using System.Linq;
using Doom_Screen_Saver.Properties;

namespace Doom_Screen_Saver {

    public partial class MainForm : Form {

        #region Preview API's

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        #endregion

        //Behaviour parameters
        int maxWalkDistance = 50;
        int minWalkDistance = 5;
        int spawnTime = 2500;
        int animationDelay = 60;
        int monsterSpeed = 5;

        Random random = new Random();
        bool IsPreviewMode = false;
        Color colToFadeTo;

        System.Windows.Forms.Timer MainTimer = new System.Windows.Forms.Timer();

        int RightBound = Screen.PrimaryScreen.Bounds.Right;
        int LeftBound = Screen.PrimaryScreen.Bounds.Left;
        int BottomBound = Screen.PrimaryScreen.Bounds.Bottom;

        int RandomIoD = 1;

        private object lockObject = new object();

        public enum Monsters {
            ZOMBIEMAN = 1,
            SHOTGUNGUY = 2,
            MACHINEGUY = 3,
            IMP = 4,
            PINKY = 5,
            HELLKNIGHT = 6,
            CACODEMON = 7
        }

        #region Constructors

        public MainForm() {
            InitializeComponent();
        }

        //This constructor is passed the bounds this form is to show in
        //It is used when in normal mode
        public MainForm(Rectangle Bounds) {
            InitializeComponent();
            this.Bounds = Bounds;
            Cursor.Hide();
        }

        //This constructor is the handle to the select screensaver dialog preview window
        //It is used when in preview mode (/p)
        public MainForm(IntPtr PreviewHandle) {
            InitializeComponent();

            //set the preview window as the parent of this window
            SetParent(this.Handle, PreviewHandle);

            //make this a child window, so when the select screensaver dialog closes, this will also close
            SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));

            //set our window's size to the size of our window's new parent
            Rectangle ParentRect;
            GetClientRect(PreviewHandle, out ParentRect);
            this.Size = ParentRect.Size;

            this.Location = new Point(0, 0);

            IsPreviewMode = true;
        }

        #endregion

        #region GUI

        private void MainForm_Shown(object sender, EventArgs e) {
            if (!IsPreviewMode) {
                this.Refresh();
                Thread.Sleep(1000);
            }

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            ManualResetEvent res = new ManualResetEvent(false);

            this.BackColor = Color.Black;
            //this.TransparencyKey = Color.FromArgb(0, 0, 0, 0); // Transparent bg

            //Set DOOM Logo!
            PictureBox Logo = new PictureBox();
            Logo.Image = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString() == "doom2")
           .Select(x => x.Value as Image)
           .Single();
            Logo.Location = new Point((this.Width / 2) - 400, (this.Height / 2) - 235); //Center Image
            Logo.SizeMode = PictureBoxSizeMode.AutoSize;
            Logo.BackColor = Color.Transparent;
            Controls.Add(Logo);
            Logo.Refresh();

            Thread.Sleep(2000);

            //Fade Doom Logo
            for (int x = 50; x < 100; x++) { 
                Logo.Image = Lighter(Logo.Image, x, colToFadeTo.R, colToFadeTo.G, colToFadeTo.B);
                Logo.Refresh();
                Thread.Sleep(30);
            }

            //Dispose Logo
            Logo.Dispose();
            Controls.Remove(Logo);

            //Start!
            MainTimer.Interval = spawnTime;
            MainTimer.Tick += new EventHandler(timer_Tick);
            MainTimer.Start();
        }

        //Make the magic...
        private void timer_Tick(object sender, EventArgs e) {
            int RandomYStart = random.Next(1, BottomBound);
            Monsters RMonster = GetRandMonster(); //Get random monster to spawn

            var Entity = CreateEntity("PictureBox");
            Entity.SizeMode = PictureBoxSizeMode.AutoSize;
            Entity.BackColor = Color.Transparent;
            Controls.Add(Entity);

            if (RandomIoD == 1) { //Show up from left side of screen
                Entity.Location = new Point(LeftBound - 10, RandomYStart);

                if (random.Next(1, 11) > 8) { // 20% probability to send lost soul flying
                    Task.Factory.StartNew(async () => await LostSoulFly(Entity, 'R'));
                } else {
                    Task.Factory.StartNew(async () => await Walk(Entity, RMonster, 'R')).ContinueWith(async (i) => await GetRandomTask(Entity, RMonster));
                }
                
                RandomIoD = 2;
            } else if (RandomIoD == 2) { //Show up from right side of screen
                Entity.Location = new Point(RightBound + 10, RandomYStart);

                if (random.Next(1, 11) > 8) { // 20% probability to send lost soul flying
                    Task.Factory.StartNew(async () => await LostSoulFly(Entity, 'L'));
                } else {
                    Task.Factory.StartNew(async () => await Walk(Entity, RMonster, 'L')).ContinueWith(async (i) => await GetRandomTask(Entity, RMonster));
                }
                
                RandomIoD = 1;
            }
        }

        //TO-DO: Randomize this...
        public async Task GetRandomTask(PictureBox Entity, Monsters RMonster) {

            var task1 = Walk(Entity, RMonster, 'R');
            var task2 = Walk(Entity, RMonster, 'L');
            var die = Die(Entity, RMonster);

            await Task.WhenAll(task1, task2, die);
        }

        public async Task Rotate(PictureBox Entity, Monsters m, char Direction) {

            List<Image> images = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString().Contains(m.ToString() + (Direction == 'R' ? "_RR" : "_RL")))
           .OrderBy(x => x.Key.ToString())
           .Select(x => x.Value as Image )
           .ToList();

            foreach (Image i in images) {
                try {
                    lock (lockObject) {
                        Entity.Image = i; //Change Image
                        Entity.Refresh();
                    }
                    Thread.Sleep(animationDelay);
                } catch (Exception) { }
            }

            Image Fimage = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString().Contains(m.ToString() + "_FRONT"))
           .Select(x => x.Value as Image)
           .Single();

            //Set Monster Looking Forward
            try {
                lock (lockObject) {
                    Entity.Image = new Bitmap(Fimage);
                    Entity.Refresh();
                }
                Thread.Sleep(random.Next(1000, 4000)); //Kinda "idle" state
            } catch (Exception) { }

        }

        public async Task Die(PictureBox Entity, Monsters m) {

            bool gib = false;

            if (m == Monsters.ZOMBIEMAN || m == Monsters.SHOTGUNGUY || m == Monsters.IMP) { //Too lazy to add MachineGun gib anim
                gib = random.Next(1, 11) > 6; // 40% probability to show gib animation
            }

            List<Image> images = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString().Contains(m.ToString() + (gib == true ? "_G" : "_D")))
           .OrderBy(x => x.Key.ToString())
           .Select(x => x.Value as Image )
           .ToList();

            foreach (Image i in images) {
                try {
                    lock (lockObject) {
                        Entity.Image = i; //Change Image
                        Entity.Refresh();
                    }
                    Thread.Sleep(animationDelay);
                } catch (Exception) { }
            }

            //Dissapear
            try {
                Thread.Sleep(animationDelay * 20);
                for (int x = 50; x < 102; x++) {
                    lock (lockObject) {
                        Entity.Image = Lighter(Entity.Image, x, colToFadeTo.R, colToFadeTo.G, colToFadeTo.B);
                    }
                    Thread.Sleep(animationDelay);
                }
                lock (lockObject) {
                    Entity.SendToBack();
                    Entity.Location = new Point(LeftBound - 150, Entity.Location.Y); //Move away!
                    Entity.Dispose();
                    Controls.Remove(Entity);
                }

            } catch (Exception) {
                lock (lockObject) {
                    Entity.Location = new Point(LeftBound - 150, Entity.Location.Y); //Move away!
                    Entity.Dispose();
                    Controls.Remove(Entity);
                }
            }
        }

        public async Task Walk(PictureBox Entity, Monsters m, char Direction) {

            CheckForIllegalCrossThreadCalls = false; //Shure there's a better way to update GUI from another Thread

            List<Image> images = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString().Contains(m.ToString() + (Direction == 'R' ? "_WR" : "_WL")))
            .OrderBy(x => x.Key.ToString())
           .Select(x => x.Value as Image )
           .ToList();

            Random rnd = new Random();
            int iter = rnd.Next(minWalkDistance, maxWalkDistance); //Walk Random Distance
            for (int x = 0; x < iter; x++) {
                foreach (Image i in images) {
                    try {
                        lock (lockObject) {
                            if (Direction == 'R') {
                                Entity.Location = new Point(Entity.Location.X + monsterSpeed, Entity.Location.Y); //Move Entity Right
                            } else {
                                Entity.Location = new Point(Entity.Location.X - monsterSpeed, Entity.Location.Y); //Move Entity Left
                            }
                            Entity.Image = i; //Change Image
                            Entity.Refresh();
                        }
                        Thread.Sleep(animationDelay);
                    } catch (Exception) { }
                }
            }

            await Rotate(Entity, m, Direction); //Rotate after end walking
        }

        public async Task LostSoulFly(PictureBox Entity, char Direction) {

            CheckForIllegalCrossThreadCalls = false; //Shure there's a better way to update GUI from another Thread

            int screenSize = RightBound - LeftBound;
            int lostSoulSpeed = monsterSpeed * 2;
            int iter = (int)Math.Ceiling((double)screenSize / lostSoulSpeed) + 10;

            Image image = Resources.ResourceManager
           .GetResourceSet(CultureInfo.CurrentCulture, true, true)
           .Cast<DictionaryEntry>()
           .Where(x => x.Value.GetType() == typeof(Bitmap) && x.Key.ToString().Contains("LOSTSOUL" + (Direction == 'R' ? "_FR" : "_FL")))
           .Select(x => x.Value as Image)
           .Single();

            Entity.Image = image;

            for (int x = 0; x < iter; x++) {
                try {
                    lock (lockObject) {
                        if (Direction == 'R') {
                            Entity.Location = new Point(Entity.Location.X + lostSoulSpeed, Entity.Location.Y); //Move Entity Right
                        } else {
                            Entity.Location = new Point(Entity.Location.X - lostSoulSpeed, Entity.Location.Y); //Move Entity Left
                        }
                        Entity.Refresh();
                    }
                    Thread.Sleep(20);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            //Dissapear
            Entity.Dispose();
            Controls.Remove(Entity);        
        }

        #endregion

        public Monsters GetRandMonster() {
            Array values = Enum.GetValues(typeof(Monsters));
            Monsters randomMonster = (Monsters)values.GetValue(random.Next(values.Length));
            return randomMonster;
        }

        public PictureBox CreateEntity(string name) {
            switch (name) {
                case "PictureBox": return new PictureBox();
                default:
                throw new ArgumentException("Unknown class: " + name);
            }
        }

        private Image Lighter(Image imgLight, int level, int nRed, int nGreen, int nBlue) {
            Graphics graphics = Graphics.FromImage(imgLight);
            int conversion = (5 * (level - 50));
            Pen pLight = new Pen(Color.FromArgb(conversion, nRed, nGreen, nBlue), imgLight.Width * 2);
            graphics.DrawLine(pLight, -1, -1, imgLight.Width, imgLight.Height);
            graphics.Save();
            graphics.Dispose();
            return imgLight;
        }

        #region User Input

        private void MainForm_KeyDown(object sender, KeyEventArgs e) {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                MainTimer.Stop();
                MainTimer.Dispose();
                Application.Exit();
            }
        }

        private void MainForm_Click(object sender, EventArgs e) {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                MainTimer.Stop();
                MainTimer.Dispose();
                Application.Exit();
            }
        }

        //start off OriginalLoction with an X and Y of int.MaxValue, because
        //it is impossible for the cursor to be at that position. That way, we
        //know if this variable has been set yet.
        Point OriginalLocation = new Point(int.MaxValue, int.MaxValue);
        private void MainForm_MouseMove(object sender, MouseEventArgs e) {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                //see if originallocat5ion has been set
                if (OriginalLocation.X == int.MaxValue & OriginalLocation.Y == int.MaxValue) {
                    OriginalLocation = e.Location;
                }
                //see if the mouse has moved more than 20 pixels in any direction. If it has, close the application.
                if (Math.Abs(e.X - OriginalLocation.X) > 20 | Math.Abs(e.Y - OriginalLocation.Y) > 20) {
                    MainTimer.Stop();
                    MainTimer.Dispose();
                    Application.Exit();
                }
            }
        }

        #endregion
    }
}
