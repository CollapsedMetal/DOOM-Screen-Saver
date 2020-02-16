﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;

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

        string MainDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
        int maxWalkDistance = 50;
        int spawnTime = 2500;
        int animationDelay = 100;
        int monsterSpeed = 5;
        Random random = new Random();
        bool IsPreviewMode = false;
        Color colToFadeTo;
        System.Windows.Forms.Timer MainTimer = new System.Windows.Forms.Timer();
        int RightBound = Screen.PrimaryScreen.Bounds.Right;
        int LeftBound = Screen.PrimaryScreen.Bounds.Left;
        int BottomBound = Screen.PrimaryScreen.Bounds.Bottom;
        int RandomIoD = 1;

        public enum Monsters {
            ZombieMan = 1,
            ShotgunGuy = 2,
            MachineGuy = 3,
            Imp = 4,
            Pinky = 5
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

            //this.BackColor = Color.Black;
            this.TransparencyKey = Color.FromArgb(0, 0, 0, 0); // Transparent bg

            MainTimer.Interval = spawnTime;
            MainTimer.Tick += new System.EventHandler(timer_Tick);
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
                    Task.Factory.StartNew(() => LostSoulFly(Entity, 'R'));
                } else {
                    Task.Factory.StartNew(() => Walk(Entity, RMonster, 'R')).ContinueWith(async (i) => await GetRandomTask(Entity, RMonster));
                }
                
                RandomIoD = 2;
            } else if (RandomIoD == 2) { //Show up from right side of screen
                Entity.Location = new Point(RightBound + 10, RandomYStart);

                if (random.Next(1, 11) > 8) { // 20% probability to send lost soul flying
                    Task.Factory.StartNew(() => LostSoulFly(Entity, 'L'));
                } else {
                    Task.Factory.StartNew(() => Walk(Entity, RMonster, 'L')).ContinueWith(async (i) => await GetRandomTask(Entity, RMonster));
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

        private object lockObjectR = new object();
        public async Task Rotate(PictureBox Entity, Monsters m, char Direction) {

            string path = MainDirectory + "\\Resources\\" + m;
            List<Image> images = new List<Image>();
            DirectoryInfo d = new DirectoryInfo(path);
            foreach (var file in d.GetFiles("RR*.png")) { //Load Animation Images
                Bitmap image = new Bitmap(file.FullName);
                if (Direction == 'R')
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                images.Add(image);
            }

            foreach (Image i in images) {
                try {
                    lock (lockObjectR) {
                        Entity.Image = i; //Change Image
                        Entity.Refresh();
                    }
                    Thread.Sleep(animationDelay);
                } catch (Exception) { }
            }

            //Set Monster Looking Forward
            try {
                lock (lockObjectR) {
                    Entity.Image = new Bitmap(path + "\\FRONT.png");
                    Entity.Refresh();
                }
                Thread.Sleep(random.Next(500, 4000)); //Kinda "idle" state
            } catch (Exception) { }

        }

        public async Task Die(PictureBox Entity, Monsters m) {

            bool gib = false;

            if (m == Monsters.ZombieMan || m == Monsters.ShotgunGuy || m == Monsters.Imp) { //Too lazy to add MachineGun gib anim
                gib = random.Next(1, 11) > 6; // 40% probability to show gib animation
            }

            string path = MainDirectory + "\\Resources\\" + m;
            List<Image> images = new List<Image>();
            DirectoryInfo d = new DirectoryInfo(path);
            foreach (var file in (gib == true ? d.GetFiles("G*.png") : d.GetFiles("D*.png"))) { //Load Animation Images
                Bitmap image = new Bitmap(file.FullName);
                images.Add(image);
            }

            foreach (Image i in images) {
                try {
                    lock (lockObjectR) {
                        Entity.Image = i; //Change Image
                        Entity.Refresh();
                    }
                    Thread.Sleep(animationDelay);
                } catch (Exception) { }
            }

            //Dissapear
            try {
                Thread.Sleep(animationDelay * 10);
                for (int x = 50; x < 102; x++) {
                    Entity.Image = Lighter(Entity.Image, x, colToFadeTo.R, colToFadeTo.G, colToFadeTo.B);
                    Thread.Sleep(animationDelay);
                }
                Entity.SendToBack();
                Entity.Location = new Point(LeftBound - 150, Entity.Location.Y); //Move away!
                Entity.Dispose();
                Controls.Remove(Entity);
            } catch (Exception) {
                Entity.Location = new Point(LeftBound - 150, Entity.Location.Y); //Move away!
                Entity.Dispose();
                Controls.Remove(Entity);
            }

        }

        private object lockObject = new object();
        public async Task Walk(PictureBox Entity, Monsters m, char Direction) {

            CheckForIllegalCrossThreadCalls = false; //Shure there's a better way to update GUI from another Thread

            string path = MainDirectory + "\\Resources\\" + m;
            List<Image> images = new List<Image>();
            DirectoryInfo d = new DirectoryInfo(path);
            foreach (var file in d.GetFiles("WL*.png")) { //Load Animation Images
                Bitmap image = new Bitmap(file.FullName);
                if (Direction == 'R')
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX); //Not Shure if this is the right way...
                                                                      //image.RotateFlip(RotateFlipType.Rotate180FlipY);
                images.Add(image);
            }

            Random rnd = new Random();
            int iter = rnd.Next(1, maxWalkDistance); //Walk Random Distance
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


        private object lockObjectL = new object();
        public async Task LostSoulFly(PictureBox Entity, char Direction) {

            CheckForIllegalCrossThreadCalls = false; //Shure there's a better way to update GUI from another Thread

            int screenSize = RightBound - LeftBound;
            int lostSoulSpeed = monsterSpeed * 2;
            int iter = (int)Math.Ceiling((double)screenSize / lostSoulSpeed) + 10;

            Image image = Image.FromFile(MainDirectory + "\\Resources\\LostSoul\\M2.png"); //Change Image
            if (Direction == 'R')
                image.RotateFlip(RotateFlipType.RotateNoneFlipX); //Not Shure if this is the right way...

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
