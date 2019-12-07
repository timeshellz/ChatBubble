using ImageProcessor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace ChatBubble.Client
{
    public partial class Form1 : Form
    {
        public delegate void ShowPanelDelegate(int panelCategory, Panel panelName);
        public delegate void CleanPanelDelegate(Panel panelName);

        public delegate void OpenPanelDelegate();
        public delegate void OpenTabDelegate(MainPage.TabType tabType, string tabArgument = "");

        public static System.Timers.Timer connectedCheckTimer = new System.Timers.Timer(5000);

        public static Font titleFont = new Font("Verdana", 10, FontStyle.Regular);
        public static Font hatFont = new Font("Verdana", 9, FontStyle.Regular);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams paramSet = base.CreateParams;
                paramSet.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return paramSet;
            }
        }

        public Form1()
        {           
            InitializeComponent();

            LoadingPage loadingPage = new LoadingPage();
            loadingPage.ActiveForm = this;

            loadingPage.Size = new Size(903, 480);
            loadingPage.Location = new Point(0, 0);
            this.Controls.Add(loadingPage);
            loadingPage.BringToFront();

            loadingPage.OpenLoadingPage();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Form1.connectedCheckTimer.Stop();

                NetComponents.BreakBind(false);
            }
            catch
            {

            }
        }
                                                 
        public class MainPage : Panel
        {
            delegate void TriggerNotificationDelegate(NotificationType type, string content);

            public enum TabType { MainPage, Friends, Dialogues, ActiveDialogue, Search, Settings, LogOut }
            public enum NotificationType { NewFriend, NewMessage }

            static Size mainPageSize { get; set; } = new Size(900, 480);
            static Point mainPageLocation { get; set; } = new Point(0, 0);

            public static List<Panel> TabHistory;

            public static Point tabLocation = new Point(60, 7);
            public static Size tabSize = new Size(840, 473);

            System.Timers.Timer notificationDetectionTimer = new System.Timers.Timer(200);
                
            public MainPage()
            {
                Size = mainPageSize;
                Location = mainPageLocation;
                BackgroundImage = Properties.Resources.mainPanelBGImage;               
            }

            public void OpenMainPage()
            {              
                CreateButtons();

                notificationDetectionTimer.Elapsed += new ElapsedEventHandler(TryDetectNotification);
                notificationDetectionTimer.Start();

                BringToFront();
            }

            public void CloseMainPage()
            {
                notificationDetectionTimer.Stop();
                notificationDetectionTimer.Dispose();

                Dispose();
            }

            public void ClearAll()
            {
                foreach(Control panel in Controls.OfType<Control>())
                {
                    if (panel.GetType() != typeof(MenuButton) && panel.GetType() != typeof(PictureBox))
                    {
                        Controls.Remove(panel);
                        panel.Dispose();
                    }
                }
            }

            public void TabClose(Panel tab)
            {
                Controls.Remove(tab);
                tab.Dispose();
                TabHistory.RemoveAt(TabHistory.Count - 1);

                if (TabHistory.Count > 0)
                {
                    TabHistory[TabHistory.Count - 1].Enabled = true;
                }
            }

            private void CreateButtons()
            {
                Button menuButton;

                foreach(TabType tabType in Enum.GetValues(typeof(TabType)))
                {
                    if (tabType != TabType.ActiveDialogue)
                    {
                        menuButton = new MenuButton(this, this, tabType);

                        Controls.Add(menuButton);
                    }
                }
            }

            private void TryDetectNotification(object sender, ElapsedEventArgs e)
            {
                if(!NetComponents.receivedMessagesCollection.IsEmpty)
                {
                    TriggerNotificationDelegate notificationDelegate = new TriggerNotificationDelegate(TriggerNotifications);

                    string lastMessage;
                    NetComponents.receivedMessagesCollection.TryDequeue(out lastMessage);

                    Invoke(notificationDelegate, NotificationType.NewMessage, lastMessage);
                }
            }

            public static void ShowUserProfile(object sender, EventArgs eventArgs)
            {
                PictureBox userThumbnail = (PictureBox)sender;
                MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                string userID = userThumbnail.Name.Substring(userThumbnail.Name.IndexOf('_') + 1);

                mainPage.OpenNewTab(TabType.MainPage, userID);               
            }

            public static void ShowUserDialogue(object sender, EventArgs eventArgs)
            {
                Control requestingControl = (Control)sender;
                MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                string chatID = requestingControl.Name.Substring(requestingControl.Name.IndexOf('_') + 1);

                mainPage.OpenNewTab(TabType.ActiveDialogue, chatID);
            }

            public void OpenNewTab(TabType tabType, string tabArgument = "")
            {
                if (Controls.OfType < Panel>().Count() == 0)
                {
                    TabHistory = new List<Panel>();
                }                  

                switch (tabType)
                {
                    case TabType.MainPage:
                        {
                            MainTab mainTab = new MainTab(tabArgument);

                            mainTab.Size = tabSize;
                            mainTab.Location = tabLocation;
                            mainTab.BackgroundImage = null;
                            mainTab.Name = tabType.ToString();

                            TabHistory.Add(mainTab);

                            Controls.Add(mainTab);
                            mainTab.BringToFront();
                            break;
                        }
                    case TabType.Friends:
                        {
                            FriendsTab friendsTab = new FriendsTab();

                            friendsTab.Size = tabSize;
                            friendsTab.Location = tabLocation;
                            friendsTab.BackgroundImage = null;
                            friendsTab.Name = tabType.ToString();

                            TabHistory.Add(friendsTab);

                            Controls.Add(friendsTab);
                            friendsTab.BringToFront();
                            break;
                        }
                    case TabType.Dialogues:
                        {
                            DialoguesTab dialoguesTab = new DialoguesTab();

                            dialoguesTab.Size = tabSize;
                            dialoguesTab.Location = tabLocation;
                            dialoguesTab.BackgroundImage = null;
                            dialoguesTab.Name = tabType.ToString();

                            TabHistory.Add(dialoguesTab);

                            Controls.Add(dialoguesTab);
                            dialoguesTab.BringToFront();
                            break;
                        }
                    case TabType.ActiveDialogue:
                        {
                            ActiveDialogueTab activeDialogueTab = new ActiveDialogueTab(tabArgument);

                            activeDialogueTab.Size = tabSize;
                            activeDialogueTab.Location = tabLocation;
                            activeDialogueTab.BackgroundImage = null;
                            activeDialogueTab.Name = tabType.ToString();

                            TabHistory.Add(activeDialogueTab);

                            Controls.Add(activeDialogueTab);
                            activeDialogueTab.BringToFront();
                            break;
                        }
                    case TabType.Search:
                        {
                            SearchTab searchTab = new SearchTab();

                            searchTab.Size = tabSize;
                            searchTab.Location = tabLocation;
                            searchTab.BackgroundImage = null;
                            searchTab.Name = tabType.ToString();

                            TabHistory.Add(searchTab);

                            Controls.Add(searchTab);
                            searchTab.BringToFront();
                            break;
                        }
                    case TabType.Settings:
                        {
                            SettingsTab settingsTab = new SettingsTab();

                            settingsTab.Size = tabSize;
                            settingsTab.Location = tabLocation;
                            settingsTab.BackgroundImage = null;
                            settingsTab.Name = tabType.ToString();

                            TabHistory.Add(settingsTab);

                            Controls.Add(settingsTab);
                            settingsTab.BringToFront();
                            break;
                        }
                    case TabType.LogOut:
                        {
                            LogOutTab logOutTab = new LogOutTab();
                            Controls.Add(logOutTab);

                            logOutTab.LogOut(true);
                            break;
                        }
                }

                if(TabHistory.Count > 1)
                {
                    TabHistory[TabHistory.Count - 2].Enabled = false;
                }
            }

            public void LogOut(bool sendTCPResetRequest)
            {
                Form1.connectedCheckTimer.Stop();

                if (sendTCPResetRequest == true)
                {
                    NetComponents.ClientRequestArbitrary("[log_out_log_out]", "", false);
                }

                NetComponents.BreakBind(true);

                FileIOStreamer fileIO = new FileIOStreamer();
                fileIO.WriteToFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt", "invalid_", true);

                Form1 currentForm = (Form1)Application.OpenForms[0];
                MainPage mainPage = currentForm.Controls.OfType<MainPage>().First();

                mainPage.CloseMainPage();

                LoadingPage loadingPage = new LoadingPage();
                loadingPage.ActiveForm = currentForm;

                loadingPage.Size = new Size(903, 480);
                loadingPage.Location = new Point(0, 0);
                currentForm.Controls.Add(loadingPage);
                loadingPage.BringToFront();

                loadingPage.OpenLoadingPage();              
            }

            public void TriggerNotifications(NotificationType notificationType, string notificationContent)
            {      
                switch(notificationType)
                {
                    case NotificationType.NewMessage:
                        string[] pendingMessages = notificationContent.Split(new string[] { "msg=" }, StringSplitOptions.RemoveEmptyEntries);
                        int totalMessages = pendingMessages.Length;

                        string[] pendingMessageSubstrings = pendingMessages[pendingMessages.Length - 1].Split(new string[] { "sender=", "time=", "message=" }, StringSplitOptions.RemoveEmptyEntries);
                        //[0] = sender id, [1] - message time, [2] = message text

                        string messageSenderData = NetComponents.ClientRequestArbitrary("[get_user_summar]", "reqid=" + pendingMessageSubstrings[0], true, true);
                        string[] messageDataSubstrings = messageSenderData.Split(new string[] { "id=", "login=", "name=", "status=", "main=", "bubscore=" }, StringSplitOptions.RemoveEmptyEntries);

                        Notification notification = new Notification(notificationType);

                        notification.notificationContentTitle = messageDataSubstrings[2];
                        notification.notificationContentSubtitle = pendingMessageSubstrings[2];
                        notification.notificationCounter = totalMessages;

                        Controls.Add(notification);
                        notification.BringToFront();
                        
                        break;
                }              
            }

            private class Notification : DoubleBufferPanel //Class for notification panels that appear on top of the screen
            {
                Size notificationSize { get; set; } = new Size(600, 70);
                Point notificationLocation { get; set; } = new Point(180, -70);

                NotificationType currentNotificationType;

                public string notificationContentTitle = "empty";
                public string notificationContentSubtitle = "empty";
                public int notificationCounter = 0;

                delegate void NotificationLocationUpdateDelegate(Point newLocation);
                delegate void NotificationDisposeDelegate();

                int notificationLiveTimeMsDefault = 500;
                int notificationLiveTimeCurrent;
                bool notificationPresent = false;

                GraphicsPath borderPath = new GraphicsPath();
                Pen pen = new Pen(Color.FromArgb(255, 141, 179, 16), 3.5f);

                public Notification(NotificationType notificationType)
                {
                    Size = notificationSize;
                    Location = notificationLocation;

                    GetRegion();

                    currentNotificationType = notificationType; 

                    this.HandleCreated += new EventHandler(PrepareNotification);
                }

                void GetRegion()
                {
                    int cornerRadius = 25;
                    int borderMargin = 1;

                    borderPath.StartFigure();

                    borderPath.AddArc(new Rectangle(borderMargin, borderMargin, cornerRadius, cornerRadius), 180, 90);
                    borderPath.AddLine(cornerRadius + borderMargin, borderMargin, Width - cornerRadius - borderMargin, borderMargin);

                    borderPath.AddArc(new Rectangle(Width - cornerRadius - borderMargin, borderMargin, cornerRadius, cornerRadius), 270, 90);
                    borderPath.AddLine(Width - borderMargin, cornerRadius + borderMargin, Width - borderMargin, Height - cornerRadius - borderMargin);

                    borderPath.AddArc(new Rectangle(Width - cornerRadius - borderMargin, Height - cornerRadius - borderMargin, cornerRadius, cornerRadius), 0, 90);
                    borderPath.AddLine(Width - cornerRadius - borderMargin, Height - borderMargin, cornerRadius + borderMargin, Height - borderMargin);

                    borderPath.AddArc(new Rectangle(borderMargin, Height - cornerRadius - borderMargin, cornerRadius, cornerRadius), 90, 90);
                    borderPath.AddLine(borderMargin, Height - cornerRadius + borderMargin, borderMargin, cornerRadius + borderMargin);

                    borderPath.CloseFigure();

                    GraphicsPath regionPath = new GraphicsPath();

                    regionPath.StartFigure();

                    regionPath.AddArc(new Rectangle(0, 0, cornerRadius, cornerRadius), 180, 90);
                    regionPath.AddLine(0, 0, Width, 0);

                    regionPath.AddArc(new Rectangle(Width - cornerRadius, 0, cornerRadius, cornerRadius), 270, 90);
                    regionPath.AddLine(Width, cornerRadius, Width, Height - cornerRadius);

                    regionPath.AddArc(new Rectangle(Width - cornerRadius, Height - cornerRadius, cornerRadius, cornerRadius), 0, 90);
                    regionPath.AddLine(Width - cornerRadius, Height, cornerRadius, Height);

                    regionPath.AddArc(new Rectangle(0, Height - cornerRadius, cornerRadius, cornerRadius), 90, 90);
                    regionPath.AddLine(0, Height - cornerRadius, 0, cornerRadius);

                    regionPath.CloseFigure();

                    Region = new Region(regionPath);
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    e.Graphics.DrawPath(pen, borderPath);
                   
                    base.OnPaint(e);
                }              

                private void PrepareNotification(object sender, EventArgs eventArgs)
                {
                    System.Timers.Timer animationTimer = new System.Timers.Timer(1);
                    animationTimer.Elapsed += new ElapsedEventHandler(OnAnimationTimerTick);

                    PictureBox notificationThumbnail = new PictureBox();                    
                    Label notificationLabelTitle = new Label();
                    Label notificationLabelSubtitle = new Label();
                    Button dismissNotification = new Button();

                    notificationThumbnail.Size = new Size(Height - 12, Height - 12);
                    notificationThumbnail.Location = new Point(6, 6);

                    System.IO.MemoryStream profilePictureStream = new System.IO.MemoryStream();

                    Bitmap thumbnailImage = new Bitmap(Properties.Resources.PlaceholderProfilePicture, notificationThumbnail.Size);
                    ImageFactory imageEditor = new ImageFactory();
                    imageEditor.Load(thumbnailImage);
                    imageEditor.Format(new ImageProcessor.Imaging.Formats.PngFormat());
                    imageEditor.RoundedCorners(notificationThumbnail.Height / 2);
                    imageEditor.BackgroundColor(Color.Transparent);
                    imageEditor.Save(profilePictureStream);

                    notificationThumbnail.BackgroundImage = Image.FromStream(profilePictureStream);
                    notificationThumbnail.BackColor = Color.Transparent;          

                    notificationLabelTitle.AutoSize = false;
                    notificationLabelTitle.Size = new Size(Width - notificationThumbnail.Width - 30 - Width / 4, 22);
                    notificationLabelTitle.Location = new Point(notificationThumbnail.Location.X + notificationThumbnail.Width + 3, notificationThumbnail.Location.Y);
                    notificationLabelTitle.Font = new Font("Verdana", 10, FontStyle.Regular);

                    dismissNotification.Size = new Size(24, 24);
                    dismissNotification.Location = new Point(Width - 24 - 6, notificationLabelTitle.Location.Y - 1);
                    dismissNotification.FlatStyle = FlatStyle.Flat;
                    dismissNotification.FlatAppearance.BorderSize = 0;
                    dismissNotification.BackgroundImage = Properties.Resources.removeFriendButton;
                    
                    notificationLabelSubtitle.AutoSize = false;
                    notificationLabelSubtitle.Size = new Size(Width - notificationThumbnail.Width - 20, notificationThumbnail.Height - notificationLabelTitle.Height);
                    notificationLabelSubtitle.Location = new Point(notificationThumbnail.Location.X + notificationThumbnail.Width + 6, notificationThumbnail.Location.Y + notificationLabelTitle.Height);
                    notificationLabelSubtitle.Font = new Font("Verdana", 7, FontStyle.Regular);
                    notificationLabelSubtitle.TextAlign = ContentAlignment.MiddleLeft;

                    switch (currentNotificationType)
                    {
                        case NotificationType.NewFriend:
                            break;
                        case NotificationType.NewMessage:                           
                            notificationLabelTitle.Text = notificationContentTitle;
                            notificationLabelSubtitle.Text = notificationContentSubtitle;

                            if (notificationCounter - 1 > 0)
                            {
                                Label otherMessagesCounter = new Label();

                                otherMessagesCounter.AutoSize = true;
                                otherMessagesCounter.Size = new Size(Width / 4, notificationLabelTitle.Height);
                                otherMessagesCounter.Location = new Point(dismissNotification.Location.X - otherMessagesCounter.Width - 20, notificationLabelTitle.Location.Y + 2);
                                otherMessagesCounter.Font = new Font("Verdana", 8, FontStyle.Italic);
                                otherMessagesCounter.ForeColor = Color.Gray;

                                if((notificationCounter - 1) % 10 == 1)
                                {
                                    otherMessagesCounter.Text = "And " + (notificationCounter - 1) + " other message";
                                }
                                else
                                {
                                    otherMessagesCounter.Text = "And " + (notificationCounter - 1) + " other messages";
                                }
                                Controls.Add(otherMessagesCounter);
                            }

                            break;
                    }

                    Controls.Add(notificationThumbnail);
                    Controls.Add(notificationLabelTitle);                   
                    Controls.Add(notificationLabelSubtitle);
                    Controls.Add(dismissNotification);

                    dismissNotification.MouseEnter += new EventHandler(OnDismissNotificationMouseEnter);
                    dismissNotification.MouseLeave += new EventHandler(OnDismissNotificationMouseLeave);
                    dismissNotification.MouseUp += new MouseEventHandler(OnDismissNotificationMouseUp);

                    notificationLiveTimeCurrent = notificationLiveTimeMsDefault;
                    animationTimer.Start();
                }

                private void OnAnimationTimerTick(object sender, ElapsedEventArgs eventArgs)
                {
                    notificationLiveTimeCurrent--;

                    switch(notificationPresent)
                    {
                        case false:
                            if (notificationLocation.Y < 4 + notificationLocation.Y % 6 && Parent != null && Parent.IsHandleCreated == true)
                            {
                                notificationLocation = new Point(notificationLocation.X, notificationLocation.Y + 6);

                                NotificationLocationUpdateDelegate updateDelegate = new NotificationLocationUpdateDelegate(UpdateLocation);
                                Invoke(updateDelegate, notificationLocation);
                            }
                            else
                            {
                                notificationPresent = true;                          
                            }
                            break;
                        case true:
                            if (notificationLiveTimeCurrent <= 0)
                            {
                                if (notificationLocation.Y >= 4 - notificationSize.Height - notificationSize.Height % 8 && Parent != null && Parent.IsHandleCreated == true)
                                {
                                    notificationLocation = new Point(notificationLocation.X, notificationLocation.Y - 8);

                                    NotificationLocationUpdateDelegate updateDelegate = new NotificationLocationUpdateDelegate(UpdateLocation);

                                    if (this.InvokeRequired == true)
                                    {
                                        Invoke(updateDelegate, notificationLocation);
                                    }
                                }
                                else
                                {
                                    notificationPresent = false;

                                    System.Timers.Timer timer = (System.Timers.Timer)sender;
                                    timer.Stop();
                                }
                            }
                            break;
                    }
                }

                private void UpdateLocation(Point newLocation)
                {
                    Location = newLocation;
                    Invalidate(false);
                }

                private void OnDismissNotificationMouseEnter(object sender, EventArgs eventArgs)
                {
                    Button button = (Button)sender;
                    button.BackgroundImage = Properties.Resources.removeFriendButtonHover;
                }

                private void OnDismissNotificationMouseLeave(object sender, EventArgs eventArgs)
                {
                    Button button = (Button)sender;
                    button.BackgroundImage = Properties.Resources.removeFriendButton;
                }

                private void OnDismissNotificationMouseUp(object sender, EventArgs eventArgs)
                {
                    Button button = (Button)sender;
                    button.BackgroundImage = Properties.Resources.cancelEditButtonClick;

                    notificationLiveTimeCurrent = 0;
                }
            }

            private class TabHatImage : PictureBox
            {
                PictureBox bubblePictureBox = new PictureBox();
                Label tabNameLabel = new Label();

                Font font = new Font("Verdana", 14, FontStyle.Regular);

                SolidBrush SolidBrush = new SolidBrush(Color.FromArgb(255, 93, 143, 215));
                SolidBrush WhiteBrush = new SolidBrush(Color.White);

                GraphicsPath GraphicsPath;
                GraphicsPath AuxillaryPath;

                TabType ParentTabType { get; set; }

                public TabHatImage(TabType parentTabType, string tabName = "Unnamed Tab")
                {
                    Size = tabSize;
                    Location = new Point(0, 0);
                    ParentTabType = parentTabType;

                    GetRegion();

                    tabNameLabel.Location = new Point(46, 5);
                    tabNameLabel.AutoSize = true;
                    tabNameLabel.Font = font;
                    tabNameLabel.ForeColor = Color.White;
                    tabNameLabel.BackColor = Color.Transparent;
                    tabNameLabel.Text = tabName;

                    if (ParentTabType != TabType.ActiveDialogue)
                    {
                        bubblePictureBox.Location = new Point(6, 7);
                        bubblePictureBox.Size = new Size(31, 29);
                        bubblePictureBox.Image = Properties.Resources.bubblesImage;
                        Controls.Add(bubblePictureBox);
                    }

                    Controls.Add(tabNameLabel);
                }

                void GetRegion()
                {
                    GraphicsPath = new GraphicsPath();

                    GraphicsPath.StartFigure();
                    GraphicsPath.AddLine(0, 0, this.Width, 0);
                    GraphicsPath.AddLine(this.Width, 0, this.Width, 39);
                    GraphicsPath.AddLine(this.Width, 39, 53, 39);
                    GraphicsPath.AddArc(new RectangleF(0, 39f, 91, 91), 270, -90);
                    GraphicsPath.AddLine(0, 91, 0, 0);
                    GraphicsPath.CloseFigure();
                   
                    Region = new Region(GraphicsPath);

                    if(ParentTabType == TabType.ActiveDialogue)
                    {
                        AuxillaryPath = new GraphicsPath();

                        AuxillaryPath.StartFigure();
                        AuxillaryPath.AddEllipse(new RectangleF(0.25f, 25.25f, 23.125f, 23.125f));
                        AuxillaryPath.CloseFigure();
                    }
                }

                protected override void OnPaint(PaintEventArgs pe)
                {                  
                    pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    pe.Graphics.FillPath(SolidBrush, GraphicsPath);

                    base.OnPaint(pe);

                    if (ParentTabType == TabType.ActiveDialogue)
                    {
                        pe.Graphics.FillPath(WhiteBrush, AuxillaryPath);
                    }
                }
            }

            private class MainTab : MainPage   //Tab classes encompass Tab-specific controls and functionality
            {                                  //Tab controls are added inside tab constructors 
                ProfileInfoPanel profileInfoPanel;
                public string UserPageID { get; private set; }

                public MainTab(string userID = "")
                {
                    UserPageID = userID;

                    profileInfoPanel = new ProfileInfoPanel();

                    if(UserPageID != "")
                    {
                        profileInfoPanel.userID = UserPageID;
                    }
                    Controls.Add(profileInfoPanel);
                }

                private class ProfileInfoPanel : DoubleBufferPanel
                {
                    Point ProfileInfoPanelLocation { get; set; } = new Point(0, 0);
                    Size ProfileInfoPanelSize { get; set; } = tabSize;

                    public string userID { get; set; } = "self";

                    Font titleFont = new Font("Verdana", 28, FontStyle.Regular);
                    Font subtitleFont = new Font("Verdana", 12, FontStyle.Italic);
                    Font scoreFont = new Font("Verdana", 8, FontStyle.Regular);
                    Font generalFont = new Font("Verdana", 10, FontStyle.Regular);

                    delegate void ProfileInfoAddDelegate(PictureBox profilePicture, Label name, Label username, Label bubScore, Label statusLabel, Label summaryLabel);
                    delegate void ProfileEditingControlsAddDelegate(Button editButton, Button editButtonConfirm, Button editButtonCancel);
                    delegate void ProfileUserInteractionControlsAddDelegate(Button returnButton, Button textButton, Button friendshipControlButton);

                    public ProfileInfoPanel()
                    {
                        Size = ProfileInfoPanelSize;
                        Location = ProfileInfoPanelLocation;
                        BackgroundImage = Properties.Resources.mainTabBackground;

                        HorizontalScroll.Maximum = 0;
                        AutoScroll = false;
                        VerticalScroll.Visible = false;
                        AutoScroll = true;

                        this.HandleCreated += new EventHandler(ShowProfileInfo);
                        this.MouseWheel += new MouseEventHandler(OnScrollMW);
                        this.Scroll += new ScrollEventHandler(OnScroll);
                    }

                    private const int WM_HSCROLL = 0x114;
                    private const int WM_VSCROLL = 0x115;

                    protected override void WndProc(ref Message m)
                    {
                        if ((m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                        && (((int)m.WParam & 0xFFFF) == 5))
                        {
                            // Change SB_THUMBTRACK to SB_THUMBPOSITION
                            m.WParam = (IntPtr)(((int)m.WParam & ~0xFFFF) | 4);
                        }
                        base.WndProc(ref m);
                    }

                    private void OnScrollMW(object sender, MouseEventArgs e)
                    {
                        this.Invalidate();

                        OnScroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, 120));
                    }

                    private void OnScroll(object sender, ScrollEventArgs e)
                    {
                        if (e.Type == ScrollEventType.First)
                        {
                            LockWindowUpdate(this.Handle);
                        }
                        else
                        {
                            LockWindowUpdate(IntPtr.Zero);
                            this.Update();
                            if (e.Type != ScrollEventType.Last) LockWindowUpdate(this.Handle);
                        }
                    }

                    [DllImport("user32.dll", SetLastError = true)]
                    private static extern bool LockWindowUpdate(IntPtr hWnd);

                    private void ShowProfileInfo(object sender, EventArgs eventArgs)
                    {
                        string[] profileInfoSplitstrings = { "id=", "login=", "name=", "status=", "main=", "bubscore=" };
                        string profileInfoString = NetComponents.ClientRequestArbitrary("[get_user_summar]", "reqid=" + userID, true, true);

                        try
                        {
                            profileInfoString = profileInfoString.Substring(0, profileInfoString.IndexOf('\0'));
                        }
                        catch
                        {

                        }

                        if (profileInfoString == "database__error__")
                        {
                            return;
                        }

                        string[] allProfileData = profileInfoString.Split(profileInfoSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                        //[0] = id, [1] = login, [2] = name, [3] = status summary, [4] = main summary, [5] = bubscore

                        for(int i = 0; i < allProfileData.Length; i++)
                        {
                            allProfileData[i] = allProfileData[i].Replace("[eqlsgn]", "=");
                        }

                        PictureBox profilePicture = new PictureBox();
                        Label nameLabel = new Label();
                        Label usernameLabel = new Label();
                        Label bubScoreLabel = new Label();
                        Label statusLabel = new Label();
                        Label summaryLabel = new Label();                     

                        System.IO.MemoryStream profilePictureStream = new System.IO.MemoryStream();

                        profilePicture.Size = new Size(200, 200);
                        profilePicture.Location = new Point(12, 12);
                        
                        Bitmap thumbnailImage = new Bitmap(Properties.Resources.PlaceholderProfilePicture, profilePicture.Size);
                        ImageFactory imageEditor = new ImageFactory();
                        imageEditor.Load(thumbnailImage);
                        imageEditor.Format(new ImageProcessor.Imaging.Formats.PngFormat());
                        imageEditor.RoundedCorners(profilePicture.Height / 2);
                        imageEditor.BackgroundColor(Color.Transparent);
                        imageEditor.Save(profilePictureStream);

                        profilePicture.BackgroundImage = Image.FromStream(profilePictureStream);
                        profilePicture.BackColor = Color.Transparent;
                        profilePicture.Name = "userthumb_" + allProfileData[0];

                        nameLabel.AutoSize = true;
                        nameLabel.Location = new Point(profilePicture.Width + 10, 6);
                        nameLabel.Font = titleFont;
                        nameLabel.Text = allProfileData[2];
                        nameLabel.ForeColor = Color.White;
                        nameLabel.BackColor = Color.Transparent;

                        usernameLabel.AutoSize = true;
                        usernameLabel.Location = new Point(nameLabel.Location.X + 10, nameLabel.Location.Y + nameLabel.Height + 34);
                        usernameLabel.Font = subtitleFont;
                        usernameLabel.ForeColor = Color.Gray;
                        usernameLabel.Text = allProfileData[1];
                        usernameLabel.ForeColor = Color.White;
                        usernameLabel.BackColor = Color.Transparent;

                        bubScoreLabel.Text = allProfileData[5];
                        bubScoreLabel.Width = 100;
                        bubScoreLabel.TextAlign = ContentAlignment.MiddleCenter;
                        bubScoreLabel.Font = scoreFont;
                        bubScoreLabel.Location = new Point(746, 55);
                        bubScoreLabel.ForeColor = Color.White;
                        bubScoreLabel.BackColor = Color.Transparent;

                        statusLabel.Width = Parent.Width - profilePicture.Width - 62;
                        statusLabel.Height = 25;
                        statusLabel.Location = new Point(usernameLabel.Location.X, usernameLabel.Location.Y + usernameLabel.Height + 21);
                        statusLabel.Font = subtitleFont;
                        statusLabel.Text = allProfileData[3];
                        statusLabel.BackColor = Color.Transparent;
                        statusLabel.Name = "statusLabel";

                        summaryLabel.Size = new Size(Parent.Width - profilePicture.Width - 62, 81);
                        summaryLabel.Location = new Point(statusLabel.Location.X, statusLabel.Location.Y + statusLabel.Height + 8);
                        summaryLabel.Font = generalFont;
                        summaryLabel.Text = allProfileData[4];
                        summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
                        summaryLabel.Name = "summaryLabel";                
                        summaryLabel.BackColor = Color.Transparent;

                        ProfileInfoAddDelegate profileInfoAddDelegate = new ProfileInfoAddDelegate(AddProfileInfo);
                        Invoke(profileInfoAddDelegate, profilePicture, nameLabel, usernameLabel, bubScoreLabel, statusLabel, summaryLabel);

                        if (userID == "self")
                        {
                            Button editDescriptionButton = new Button();
                            editDescriptionButton.MouseEnter += new EventHandler(EditControlOnMouseEnter);
                            editDescriptionButton.MouseLeave += new EventHandler(EditControlOnMouseLeave);
                            editDescriptionButton.MouseUp += new MouseEventHandler(EditProfileDescription);
                            
                            Button editDescriptionButtonConfirm = new Button();
                            editDescriptionButtonConfirm.MouseEnter += new EventHandler(EditControlOnMouseEnter);
                            editDescriptionButtonConfirm.MouseLeave += new EventHandler(EditControlOnMouseLeave);

                            Button editDescriptionButtonCancel = new Button();
                            editDescriptionButtonCancel.MouseEnter += new EventHandler(EditControlOnMouseEnter);
                            editDescriptionButtonCancel.MouseLeave += new EventHandler(EditControlOnMouseLeave);

                            editDescriptionButton.Size = new Size(32, 32);
                            editDescriptionButton.Location = new Point(Parent.Width - editDescriptionButton.Width - 8, statusLabel.Location.Y - 3);
                            editDescriptionButton.FlatStyle = FlatStyle.Flat;
                            editDescriptionButton.BackgroundImage = Properties.Resources.editDescriptionButtonIdle;
                            editDescriptionButton.Name = "buttonEdit";                          
                            editDescriptionButton.FlatAppearance.BorderSize = 0;

                            editDescriptionButtonConfirm.Size = new Size(24, 24);
                            editDescriptionButtonConfirm.Location = new Point(editDescriptionButton.Location.X + 4, statusLabel.Location.Y + editDescriptionButton.Height + 1);
                            editDescriptionButtonConfirm.FlatStyle = FlatStyle.Flat;
                            editDescriptionButtonConfirm.BackgroundImage = Properties.Resources.confirmEditButtonIdle;
                            editDescriptionButtonConfirm.Name = "buttonConfirm";                            
                            editDescriptionButtonConfirm.FlatAppearance.BorderSize = 0;
                            editDescriptionButtonConfirm.Visible = false;
                            editDescriptionButtonConfirm.Enabled = false;

                            editDescriptionButtonCancel.Size = editDescriptionButtonConfirm.Size;
                            editDescriptionButtonCancel.Location = new Point(editDescriptionButton.Location.X + 4, editDescriptionButton.Location.Y + 4);
                            editDescriptionButtonCancel.FlatStyle = FlatStyle.Flat;
                            editDescriptionButtonCancel.BackgroundImage = Properties.Resources.cancelEditButtonIdle;
                            editDescriptionButtonCancel.Name = "buttonCancel";                            
                            editDescriptionButtonCancel.FlatAppearance.BorderSize = 0;
                            editDescriptionButtonCancel.Visible = false;
                            editDescriptionButtonCancel.Enabled = false;

                            ProfileEditingControlsAddDelegate profileEditingControlsD = new ProfileEditingControlsAddDelegate(AddProfileEditingControls);
                            Invoke(profileEditingControlsD, editDescriptionButton, editDescriptionButtonCancel, editDescriptionButtonConfirm);
                        }             
                        else
                        {
                            Button returnToLastPageButton = new Button();
                            returnToLastPageButton.Click += new EventHandler(GoToLastTab);

                            Button sendMessageButton = new Button();

                            Button addRemoveFriendButton = new Button();

                            returnToLastPageButton.Size = new Size(40, 40);
                            returnToLastPageButton.Location = new Point(12, 12);
                            returnToLastPageButton.BackColor = Color.Transparent;
                            returnToLastPageButton.Text = "<";
                            returnToLastPageButton.FlatStyle = FlatStyle.Flat;
                            returnToLastPageButton.FlatAppearance.BorderSize = 0;

                            sendMessageButton.Size = new Size(40, 40);
                            sendMessageButton.Location = new Point(profilePicture.Location.X, profilePicture.Location.Y + profilePicture.Height - sendMessageButton.Height);
                            sendMessageButton.BackColor = Color.Transparent;
                            sendMessageButton.Text = "#";
                            sendMessageButton.FlatStyle = FlatStyle.Flat;
                            sendMessageButton.FlatAppearance.BorderSize = 0;

                            addRemoveFriendButton.Size = sendMessageButton.Size;
                            addRemoveFriendButton.Location = new Point(sendMessageButton.Location.X + profilePicture.Width - addRemoveFriendButton.Width, sendMessageButton.Location.Y);
                            addRemoveFriendButton.BackColor = Color.Transparent;
                            addRemoveFriendButton.Text = "+";
                            addRemoveFriendButton.FlatStyle = FlatStyle.Flat;
                            addRemoveFriendButton.FlatAppearance.BorderSize = 0;

                            ProfileUserInteractionControlsAddDelegate profileUserInteractionD = new ProfileUserInteractionControlsAddDelegate(AddUserInteractionControls);
                            Invoke(profileUserInteractionD, returnToLastPageButton, sendMessageButton, addRemoveFriendButton);
                        }
                    }

                    private void AddProfileInfo(PictureBox profilePicture, Label nameLabel, Label usernameLabel, Label bubScoreLabel, Label statusLabel, Label summaryLabel)
                    {
                        Controls.Add(profilePicture);
                        Controls.Add(nameLabel);
                        Controls.Add(usernameLabel);
                        Controls.Add(bubScoreLabel);
                        Controls.Add(statusLabel);
                        Controls.Add(summaryLabel);         
                    }

                    private void AddProfileEditingControls(Button editDescriptionButton, Button editDescriptionButtonConfirm, Button editDescriptionButtonCancel)
                    {
                        Controls.Add(editDescriptionButton);
                        Controls.Add(editDescriptionButtonConfirm);
                        Controls.Add(editDescriptionButtonCancel);
                        editDescriptionButton.BringToFront();
                    }

                    private void AddUserInteractionControls(Button returnButton, Button sendMessageButton, Button addRemoveButton)
                    {
                        Controls.Add(returnButton);
                        Controls.Add(sendMessageButton);
                        Controls.Add(addRemoveButton);

                        returnButton.BringToFront();
                        sendMessageButton.BringToFront();
                        addRemoveButton.BringToFront();
                    }

                    private void EditProfileDescription(object sender, EventArgs eventArgs)
                    {
                        TextBox statusTextBox = new TextBox();
                        TextBox summaryTextBox = new TextBox();

                        Label[] controlLabels = this.Controls.OfType<Label>().ToArray();
                        Button[] controlButtons = this.Controls.OfType<Button>().ToArray();

                        foreach (Label label in controlLabels)
                        {
                            if(label.Name == "statusLabel")
                            {
                                statusTextBox.Location = new Point(label.Location.X, label.Location.Y - 3);
                                statusTextBox.Size = label.Size;
                                statusTextBox.Text = label.Text;
                                statusTextBox.Font = label.Font;
                                statusTextBox.ForeColor = label.ForeColor;
                                statusTextBox.MaxLength = 50;
                                statusTextBox.Name = "statusTextBox";
                            }
                            if(label.Name == "summaryLabel")
                            {
                                summaryTextBox.Location = new Point(label.Location.X, label.Location.Y - 5);
                                summaryTextBox.Size = label.Size;
                                summaryTextBox.Height = label.Height + 5;
                                summaryTextBox.Text = label.Text;
                                summaryTextBox.Font = label.Font;
                                summaryTextBox.ForeColor = label.ForeColor;
                                summaryTextBox.Multiline = true;
                                summaryTextBox.MaxLength = 220;
                                summaryTextBox.Name = "summaryTextBox";
                            }
                        }

                        for(int i = 0; i < controlButtons.Length; i++)
                        {
                            if(controlButtons[i].Name == "buttonEdit")
                            {
                                controlButtons[i].Visible = false;
                                controlButtons[i].Enabled = false;

                                controlButtons[i].MouseUp -= EditProfileDescription;
                            }
                            if(controlButtons[i].Name == "buttonCancel")
                            {
                                controlButtons[i].Visible = true;
                                controlButtons[i].Enabled = true;

                                controlButtons[i].MouseUp += CancelEdit;
                            }
                            if(controlButtons[i].Name == "buttonConfirm")
                            {
                                controlButtons[i].Visible = true;
                                controlButtons[i].Enabled = true;

                                controlButtons[i].MouseUp += ConfirmDescription;
                            }
                        }

                        this.Controls.Add(statusTextBox);
                        this.Controls.Add(summaryTextBox);

                        statusTextBox.BringToFront();
                        summaryTextBox.BringToFront();
                    }

                    private void CancelEdit(object sender, EventArgs eventArgs)
                    {
                        TextBox[] controlTextBoxes = this.Controls.OfType<TextBox>().ToArray();
                        Button[] controlButtons = this.Controls.OfType<Button>().ToArray();

                        foreach (TextBox textBox in controlTextBoxes)
                        {
                            textBox.Dispose();
                        }

                        for (int i = 0; i < controlButtons.Length; i++)
                        {
                            if (controlButtons[i].Name == "buttonEdit")
                            {
                                controlButtons[i].Visible = true;
                                controlButtons[i].Enabled = true;

                                controlButtons[i].Click += EditProfileDescription;
                            }
                            if (controlButtons[i].Name == "buttonCancel")
                            {
                                controlButtons[i].Visible = false;
                                controlButtons[i].Enabled = false;

                                controlButtons[i].Click -= CancelEdit;
                            }
                            if (controlButtons[i].Name == "buttonConfirm")
                            {
                                controlButtons[i].Visible = false;
                                controlButtons[i].Enabled = false;

                                controlButtons[i].Click -= ConfirmDescription;
                            }
                        }
                    }

                    private void ConfirmDescription(object sender, EventArgs eventArgs)
                    {
                        TextBox[] controlTextBoxes = this.Controls.OfType<TextBox>().ToArray();
                        string descriptionChangeRequest = "newsummary=";

                        foreach (TextBox textBox in controlTextBoxes)
                        {
                            if (textBox.Name == "statusTextBox")
                            {
                                descriptionChangeRequest += "\nstatus=" + textBox.Text;
                            }          
                        }
                        foreach (TextBox textBox in controlTextBoxes)
                        {
                            if (textBox.Name == "summaryTextBox")
                            {
                                descriptionChangeRequest += "\nmain=" + textBox.Text + "\n";
                            }
                        }
                        NetComponents.ClientRequestArbitrary("[edt_user_summar]", descriptionChangeRequest, true, true);

                        ProfileInfoPanel profileInfoPanel = new ProfileInfoPanel();
                        this.Parent.Controls.Add(profileInfoPanel);
                        this.Dispose();
                    }

                    private void GoToLastTab(object sender, EventArgs eventArgs)
                    {
                        MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                        mainPage.TabClose(TabHistory[TabHistory.Count - 1]);
                    }

                    private void EditControlOnMouseEnter(object sender, EventArgs eventArgs)
                    {
                        Button button = (Button)sender;

                        switch (button.Name)
                        {
                            case "buttonEdit":
                                {
                                    button.BackgroundImage = Properties.Resources.editDescriptionButtonHover;
                                    break;
                                }
                            case "buttonCancel":
                                {
                                    button.BackgroundImage = Properties.Resources.cancelEditButtonHover;
                                    break;
                                }
                            case "buttonConfirm":
                                {
                                    button.BackgroundImage = Properties.Resources.confirmEditButtonHover;
                                    break;
                                }
                        }
                    }

                    private void EditControlOnMouseLeave(object sender, EventArgs eventArgs)
                    {
                        Button button = (Button)sender;

                        switch (button.Name)
                        {
                            case "buttonEdit":
                                {
                                    button.BackgroundImage = Properties.Resources.editDescriptionButtonIdle;
                                    break;
                                }
                            case "buttonCancel":
                                {
                                    button.BackgroundImage = Properties.Resources.cancelEditButtonIdle;
                                    break;
                                }
                            case "buttonConfirm":
                                {
                                    button.BackgroundImage = Properties.Resources.confirmEditButtonIdle;
                                    break;
                                }
                        }
                    }
                }

            }
            private class FriendsTab : MainPage
            {
                TabHatImage tabHatPictureBox;
                FriendPanel friendPanel;
              
                public FriendsTab()
                {
                    Name = "Friends";

                    friendPanel = new FriendPanel();
                    tabHatPictureBox = new TabHatImage(TabType.Friends, Name);                  

                    Font = hatFont;
                    
                    Controls.Add(friendPanel);
                    Controls.Add(tabHatPictureBox);
                    tabHatPictureBox.BringToFront();
                }              

                private class FriendPanel : DoubleBufferPanel
                {                  
                    Point FriendPanelLocation { get; set; } = new Point(7, 29);
                    Size FriendPanelSize { get; set; } = new Size(833, 444);

                    Font titleFont = new Font("Verdana", 12, FontStyle.Regular);
                    Font subtitleFont = new Font("Verdana", 9, FontStyle.Italic);

                    int pageNumber { get; set; } = 0;
                    string[,] allFriendsDataByPage { get; set; }

                    delegate void FriendBoxAddDelegate(DoubleBufferPanel friendBox, Control removeFriendButton, Control friendThumbnail, Control friendName, Control friendUsername);
                    delegate void PageControlsAddDelegate(Button pageControlButton);

                    public FriendPanel()
                    {
                        Size = FriendPanelSize;
                        Location = FriendPanelLocation;

                        this.HandleCreated += new EventHandler(GetFriendList);
                        this.HandleCreated += new EventHandler(ShowFriendList);
                    }

                    void GetFriendList(object sender, EventArgs eventArgs)
                    {
                        string friendListResultString = NetComponents.ClientRequestArbitrary("[get_friends_lst]", "", true, true);

                        try
                        {
                            friendListResultString = friendListResultString.Substring(0, friendListResultString.IndexOf('\0'));
                        }
                        catch
                        {

                        }

                        if (friendListResultString == "database__error__")      //TO DO: Output an error message here
                        {
                            allFriendsDataByPage = null;
                            return;
                        }

                        string[] allFriendsData = friendListResultString.Split(new string[] { "user=" }, StringSplitOptions.RemoveEmptyEntries);
                        int isLastPageFilled = 0;

                        if(allFriendsData.Length % 12 > 0)
                        {
                            isLastPageFilled = 1;
                        }

                        allFriendsDataByPage = new string[12, allFriendsData.Length / 12 + isLastPageFilled];

                        int currentFriend = 0;
                        for(int pageNum = 0; pageNum < allFriendsDataByPage.GetLength(1); pageNum++)
                        {
                            for(int friendNum = 0; friendNum < 12; friendNum++)
                            {
                                if (currentFriend == allFriendsData.Length)
                                {
                                    break;
                                }

                                allFriendsDataByPage[friendNum, pageNum] = allFriendsData[currentFriend];
                                
                                currentFriend++;                          
                            }
                        }
                    }

                    void ShowFriendList(object sender, EventArgs eventArgs)
                    {
                        string[] friendListSplitstrings = { "id=", "login=", "name="};
                        
                        List<DoubleBufferPanel> friendBoxList = new List<DoubleBufferPanel>();

                        Button previousPageButton = new Button();
                        Button nextPageButton = new Button();

                        PageControlsAddDelegate previousPageAddControlDelegate = new PageControlsAddDelegate(AddPageControl);
                        PageControlsAddDelegate nextPageAddControlDelegate = new PageControlsAddDelegate(AddPageControl);

                        if (allFriendsDataByPage == null || allFriendsDataByPage.Length == 0)
                        {
                            return;
                        }

                        if (pageNumber + 1 > allFriendsDataByPage.GetLength(1))
                        {
                            pageNumber = allFriendsDataByPage.GetLength(1) - 1;
                        }

                        if (pageNumber + 1 > 1)
                        {
                            previousPageButton.Size = new Size(30, 30);
                            previousPageButton.Location = new Point(FriendPanelSize.Width - previousPageButton.Width * 2 - 8, 0);
                            previousPageButton.Text = "<";

                            Invoke(previousPageAddControlDelegate, previousPageButton);
                            previousPageButton.Click += new EventHandler(ShowPreviousPage);               
                        }
                        if (allFriendsDataByPage.GetLength(1) != pageNumber + 1)
                        {
                            nextPageButton.Size = new Size(30, 30);
                            nextPageButton.Location = new Point(FriendPanelSize.Width - nextPageButton.Width - 7, 0);
                            nextPageButton.Text = ">";

                            Invoke(nextPageAddControlDelegate, nextPageButton);
                            nextPageButton.Click += new EventHandler(ShowNextPage);
                        }

                        int friendBoxRow = 0;
                        int friendBoxColumn = 0;
                        for (int i = 0; i < 12;i++)
                        {
                            System.IO.MemoryStream thumbnailStream = new System.IO.MemoryStream();

                            Button removeFriendButton = new Button();
                            PictureBox friendThumbnail = new PictureBox();
                            Label friendUsername = new Label();
                            Label friendName = new Label();
                            WaterMarkTextBox friendSearchQuery = new WaterMarkTextBox();                                                                                                                                                               

                            if(allFriendsDataByPage[i, pageNumber] == null)
                            {
                                break;
                            }

                            string[] friendData = allFriendsDataByPage[i, pageNumber].Split(friendListSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                            //[0] = id, [1] = login, [1] = name                                                    

                            friendBoxList.Add(new DoubleBufferPanel());
                            friendBoxList[i].Size = new Size((FriendPanelSize.Width - 6) / 6, (FriendPanelSize.Height - 30) / 2);
                            friendBoxList[i].Name = "friend_" + friendData[0];
                           
                            removeFriendButton.Size = new Size(24, 24);
                            removeFriendButton.Location = new Point(friendBoxList[i].Size.Width - removeFriendButton.Width, 0);
                            removeFriendButton.BackgroundImage = Properties.Resources.removeFriendButton;
                            removeFriendButton.FlatStyle = FlatStyle.Flat;
                            removeFriendButton.FlatAppearance.BorderSize = 0;

                            friendThumbnail.Size = new Size(friendBoxList[i].Width - 18, friendBoxList[i].Width - 18);
                            friendThumbnail.Location = new Point(0, 0);
                            friendThumbnail.Name = "userthumb_" + friendData[0];

                            Bitmap thumbnailImage = new Bitmap(Properties.Resources.PlaceholderProfilePicture, friendThumbnail.Size);
                            ImageFactory imageEditor = new ImageFactory();
                            imageEditor.Load(thumbnailImage);
                            imageEditor.RoundedCorners(friendThumbnail.Height / 2);
                            imageEditor.BackgroundColor(Color.White);
                            imageEditor.Save(thumbnailStream);
                        
                            friendThumbnail.BackgroundImage = Image.FromStream(thumbnailStream);

                            friendName.AutoSize = true;
                            friendName.Font = titleFont;
                            friendName.Location = new Point(3, friendThumbnail.Height + 3);
                            friendName.Text = friendData[2];

                            friendUsername.AutoSize = true;
                            friendUsername.Font = subtitleFont;
                            friendUsername.ForeColor = Color.Gray;
                            friendUsername.Location = new Point(6, friendName.Height + friendName.Location.Y + 3);
                            friendUsername.Text = friendData[1];          
                          
                            if(friendBoxList.Count >= 1)
                            {
                                if(friendBoxColumn == 6)
                                {
                                    friendBoxRow++;
                                    friendBoxColumn = 0;
                                }
                                if(friendBoxColumn < 7)
                                {
                                    friendBoxList[i].Location = new Point(6 + friendBoxList[i].Size.Width*friendBoxColumn, friendBoxList[i].Size.Height*friendBoxRow + 30);
                                    friendBoxColumn++;
                                }
                            }

                            friendThumbnail.Click += new EventHandler(ShowUserProfile);

                            removeFriendButton.MouseEnter += new EventHandler(RemoveFriendMouseEnter);
                            removeFriendButton.MouseLeave += new EventHandler(RemoveFriendMouseLeave);
                            removeFriendButton.MouseUp += new MouseEventHandler(RemoveFriendMouseUp);


                            FriendBoxAddDelegate friendBoxAddDelegate = new FriendBoxAddDelegate(AddFriendBox);
                            Invoke(friendBoxAddDelegate, friendBoxList[i], removeFriendButton, friendThumbnail, friendUsername, friendName);
                        }

                    }

                    void AddFriendBox(DoubleBufferPanel friendBox, Control removeFriendButton, Control friendThumbnail, Control friendName, Control friendUsername)
                    {
                        Controls.Add(friendBox);
                        friendBox.Controls.Add(removeFriendButton);
                        friendBox.Controls.Add(friendThumbnail);
                        friendBox.Controls.Add(friendName);
                        friendBox.Controls.Add(friendUsername);
                        friendBox.BringToFront();

                        friendBox.Visible = true;
                    }

                    void AddPageControl(Control pageControl)
                    {
                        Controls.Add(pageControl);
                    }

                    void RemoveFriendMouseEnter(object sender, EventArgs eventArgs)
                    {
                        Button button = (Button)sender;

                        button.BackgroundImage = Properties.Resources.removeFriendButtonHover;
                    }

                    void RemoveFriendMouseLeave(object sender, EventArgs eventArgs)
                    {
                        Button button = (Button)sender;

                        button.BackgroundImage = Properties.Resources.removeFriendButton;
                    }

                    void RemoveFriendMouseUp(object sender, EventArgs eventArgs)
                    {
                        Button button = (Button)sender;

                        button.BackgroundImage = Properties.Resources.removeFriendButtonClick;

                        button.MouseEnter -= RemoveFriendMouseEnter;
                        button.MouseLeave -= RemoveFriendMouseLeave;
                        button.MouseUp -= RemoveFriendMouseUp;

                        string requestID = button.Parent.Name.Substring(button.Parent.Name.IndexOf('_') + 1);

                        //TO DO: Move this logic into another thread

                        NetComponents.ClientRequestArbitrary("[rem_friend_rem_]", "fid=" + requestID, true, true);

                        button.Parent.Parent.Controls.Clear();

                        GetFriendList(button.Parent.Parent, eventArgs);
                        ShowFriendList(button.Parent.Parent, eventArgs);          
                    }

                    void ShowNextPage(object sender, EventArgs eventArgs)
                    {
                        pageNumber++;
                        Controls.Clear();
                        ShowFriendList(this, eventArgs);
                    }

                    void ShowPreviousPage(object sender, EventArgs eventArgs)
                    {
                        pageNumber--;
                        Controls.Clear();
                        ShowFriendList(this, eventArgs);
                    }                   
                }
            }
            private class DialoguesTab : MainPage
            {
                TabHatImage tabHatImage;
                TextBox dialogueSearchQueryTextBox;
                DialogueListPanel dialogueListPanel;

                public DialoguesTab()
                {
                    Name = "Dialogues";

                    tabHatImage = new TabHatImage(TabType.Dialogues, Name);
                    dialogueListPanel = new DialogueListPanel();

                    dialogueSearchQueryTextBox = new TextBox();
                    dialogueSearchQueryTextBox.Size = new Size(626, 26);
                    dialogueSearchQueryTextBox.Location = new Point(200, 6);
                    dialogueSearchQueryTextBox.Font = hatFont;
                                
                    Controls.Add(dialogueListPanel);
                    //Controls.Add(dialogueSearchQueryTextBox);
                    Controls.Add(tabHatImage);

                    tabHatImage.BringToFront();
                    //dialogueSearchQueryTextBox.BringToFront();
                }

                private class DialogueListPanel : Panel
                {                   
                    Point DialogueListPanelLocation { get; set; } = new Point(6, 48);
                    Size DialogueListPanelSize { get; set; } = new Size(tabSize.Width - 6, tabSize.Height - 48);

                    Font titleFont = new Font("Verdana", 12, FontStyle.Regular);
                    Font subtitleFont = new Font("Verdana", 9, FontStyle.Italic);

                    List<string> currentDialoguesList = new List<string>();

                    delegate void ControlCreationDelegate(DoubleBufferPanel dialogueBox, Control thumbnail, Control title, Control subtitle, Button button);

                    public DialogueListPanel()
                    {
                        DoubleBuffered = true;

                        Location = DialogueListPanelLocation;
                        Size = DialogueListPanelSize;

                        HorizontalScroll.Maximum = 0;
                        AutoScroll = false;
                        VerticalScroll.Visible = false;
                        AutoScroll = true; 

                        this.HandleCreated += new EventHandler(GetCurrentDialogues);
                    }

                    void GetCurrentDialogues(object sender, EventArgs eventArgs)
                    {
                        FileIOStreamer fileIO = new FileIOStreamer();

                        string[] dialogueFilenameArray = fileIO.GetDirectoryFiles(FileIOStreamer.defaultLocalUserDialoguesDirectory, false, false);

                        string[] senderDataSplitstrings = new string[] {"id=", "login=", "name=",
                            "status=", "main=", "bubscore=" };

                        for (int i = 0; i < dialogueFilenameArray.Length; i++)
                        {
                            string senderID = dialogueFilenameArray[i].Substring(dialogueFilenameArray[i].IndexOf('=') + 1);
                            string senderData = NetComponents.ClientRequestArbitrary("[get_user_summar]", "reqid=" + senderID, true, true);
                            string dialogueContent = fileIO.ReadFromFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + dialogueFilenameArray[i] + ".txt");

                            if (!String.IsNullOrEmpty(dialogueContent))
                            {
                                string[] dialogueMessages = dialogueContent.Split(new string[] { "message==", "==message" }, StringSplitOptions.RemoveEmptyEntries);
                                string[] senderDataSubstrings = senderData.Split(senderDataSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                                string[] lastMessageEntrySubstrings = dialogueMessages.Last().Split(new string[] { "time=", "status=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                                //[0] - time, [1] - message status, [2] - last message content

                                currentDialoguesList.Add("id=" + senderDataSubstrings[0] + "name= " + senderDataSubstrings[2] +
                                    "time=" + lastMessageEntrySubstrings[0] +
                                    "status=" + lastMessageEntrySubstrings[1] +
                                    "lastmsg=" + lastMessageEntrySubstrings[2]);
                            }
                        }

                        DisplayCurrentDialogues(this, eventArgs);
                    }

                    void DisplayCurrentDialogues(object sender, EventArgs eventArgs)
                    {
                        Controls.Clear();

                        List<DoubleBufferPanel> dialogueBoxesList = new List<DoubleBufferPanel>();
                                           
                        for(int i = 0; i < currentDialoguesList.Count; i++)
                        {
                            string[] dialogueDataSubstrings = 
                                currentDialoguesList[i].Split(new string[] { "id=", "name=", "time=", "status=", "lastmsg=" }, StringSplitOptions.RemoveEmptyEntries);
                            //[0] - id, [1] - name, [2] - time, [3] - status, [4] - last message content


                            Label dialogueTitleLabel = new Label();
                            Label dialogueSubtitleLabel = new Label();
                            PictureBox dialogueThumbnail = new PictureBox();
                            Button closeDialogueButton = new Button();

                            System.IO.MemoryStream thumbnailStream = new System.IO.MemoryStream();

                            dialogueBoxesList.Add(new DoubleBufferPanel());
                            dialogueBoxesList[i].Size = new Size(DialogueListPanelSize.Width - 6, 60);
                            dialogueBoxesList[i].Name = "chatid_" + dialogueDataSubstrings[0];
                            dialogueBoxesList[i].Visible = true;
                            dialogueBoxesList[i].BackgroundImageLayout = ImageLayout.Tile;

                            dialogueThumbnail.Height = dialogueBoxesList[i].Height - 6;
                            dialogueThumbnail.Width = dialogueThumbnail.Height;
                            dialogueThumbnail.Name = "userthumb_" + dialogueDataSubstrings[0];
                            dialogueThumbnail.Location = new Point(0, 0);
                            Bitmap thumbnailImage = new Bitmap(Properties.Resources.PlaceholderProfilePicture,
                                                            dialogueThumbnail.Height, dialogueThumbnail.Width);

                            ImageFactory imageEditor = new ImageFactory();
                            imageEditor.Load(thumbnailImage);
                            imageEditor.RoundedCorners(dialogueThumbnail.Height / 2);
                            imageEditor.BackgroundColor(Color.White);
                            imageEditor.Save(thumbnailStream);

                            dialogueTitleLabel.AutoSize = true;
                            dialogueTitleLabel.Location = new Point(dialogueThumbnail.Location.X + dialogueThumbnail.Width, dialogueThumbnail.Location.Y);
                            dialogueTitleLabel.Font = titleFont;

                            dialogueSubtitleLabel.AutoSize = true;
                            dialogueSubtitleLabel.Location = new Point(dialogueTitleLabel.Location.X + 9, dialogueTitleLabel.Height + dialogueTitleLabel.Location.Y + 3);
                            dialogueSubtitleLabel.Font = subtitleFont;
                            dialogueSubtitleLabel.ForeColor = Color.Gray;

                            closeDialogueButton.Size = new Size(24, 24);
                            closeDialogueButton.Location = new Point(dialogueBoxesList[i].Width - closeDialogueButton.Width - 6, 3);
                            closeDialogueButton.FlatAppearance.BorderSize = 0;
                            closeDialogueButton.FlatStyle = FlatStyle.Flat;
                            closeDialogueButton.BackgroundImage = Properties.Resources.removeFriendButton;

                            //The statement below arranges every dialogue box one after another on y axis
                            if (i >= 1)
                            {
                                dialogueBoxesList[i].Location = new Point(dialogueBoxesList[i - 1].Location.X,
                                                                    dialogueBoxesList[i - 1].Height + dialogueBoxesList[i - 1].Location.Y);
                            }
                            else
                            {
                                dialogueBoxesList[i].Location = new Point(6, 0);
                            }

                            dialogueThumbnail.Image = Image.FromStream(thumbnailStream);

                            dialogueThumbnail.Click += new EventHandler(ShowUserProfile);
                            dialogueBoxesList[i].Click += new EventHandler(ShowUserDialogue);
                            closeDialogueButton.Click += new EventHandler(RemoveDialogue);

                            dialogueTitleLabel.Text = dialogueDataSubstrings[1]; //Name is written in title
                            dialogueSubtitleLabel.Text = dialogueDataSubstrings[4]; //Last message text is written in subtitle

                            ControlCreationDelegate controlCreationDelegate = new ControlCreationDelegate(AddDialogueBox);
                            BeginInvoke(controlCreationDelegate, dialogueBoxesList[i], dialogueThumbnail,
                                        dialogueTitleLabel, dialogueSubtitleLabel, closeDialogueButton);
                        }
                    }

                    void AddDialogueBox(DoubleBufferPanel dialogueBox, Control thumbnail, Control title, Control subtitle, Button button)
                    {
                        Controls.Add(dialogueBox);
                        dialogueBox.Controls.Add(thumbnail);
                        dialogueBox.Controls.Add(title);
                        dialogueBox.Controls.Add(subtitle);
                        dialogueBox.Controls.Add(button);
                        button.BringToFront();
                    }

                    void RemoveDialogue(object sender, EventArgs eventArgs)
                    {
                        FileIOStreamer fileIO = new FileIOStreamer();
                        Button button = (Button)sender;

                        string currentChatID = button.Parent.Name.Substring(button.Parent.Name.IndexOf('_') + 1);

                        string[] dialogueFilenameArray = fileIO.GetDirectoryFiles(FileIOStreamer.defaultLocalUserDialoguesDirectory, false, false);

                        for (int i = 0; i < dialogueFilenameArray.Length; i++)
                        {
                            if(dialogueFilenameArray[i] == "chatid=" + currentChatID)
                            {
                                fileIO.RemoveFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + dialogueFilenameArray[i] + ".txt");
                                currentDialoguesList.RemoveAt(i);
                            }
                        }

                        GetCurrentDialogues(Parent, eventArgs);
                    }
                }

            }

            private class ActiveDialogueTab : MainPage
            {
                TabHatImage tabHatImage;
                ChatInputPanel chatInputPanel;
                ChatMessagesPanel chatMessagesPanel;
                LastTabButton lastTabButton;

                static int chatInputPanelHeight { get; set; } = 39;

                static string ChatID { get; set; }

                public ActiveDialogueTab(string id)
                {
                    Name = "ActiveDialogue";

                    ChatID = id;
                    string recepientData = NetComponents.ClientRequestArbitrary("[get_user_summar]", "reqid=" + ChatID, true, true);
                    string chatName = recepientData.Split(new string[] {
                        "id=", "login=", "name=", "status=", "main=", "bubscore=" }, StringSplitOptions.RemoveEmptyEntries)[2];

                    chatMessagesPanel = new ChatMessagesPanel();
                    tabHatImage = new TabHatImage(TabType.ActiveDialogue, chatName);
                    lastTabButton = new LastTabButton();
                    
                    lastTabButton.MouseUp += new MouseEventHandler(GoToLastTab);

                    HandleCreated += new EventHandler(AddControls);
                }

                void AddControls(object sender, EventArgs e)
                {
                    chatInputPanel = new ChatInputPanel((Panel)sender);
                    Controls.Add(chatInputPanel);
                    Controls.Add(tabHatImage);
                    Controls.Add(chatMessagesPanel);
                    Controls.Add(lastTabButton);
                    lastTabButton.BringToFront();
                }

                void GoToLastTab(object sender, EventArgs e)
                {
                    MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                    mainPage.TabClose(TabHistory[TabHistory.Count - 1]);
                }

                private class LastTabButton : Button
                {
                    GraphicsPath RegionPath;

                    public LastTabButton()
                    {
                        Size = new Size(22, 22);
                        Location = new Point(0, 25);
                        FlatAppearance.BorderSize = 0;
                        FlatStyle = FlatStyle.Flat;
                        Image = Properties.Resources.returnButtonIdle;
                        ImageAlign = ContentAlignment.MiddleCenter;

                        MouseEnter += new EventHandler(OnMouseEnter);
                        MouseLeave += new EventHandler(OnMouseLeave);
                        MouseDown += new MouseEventHandler(OnMouseDown);
                        MouseUp += new MouseEventHandler(OnMouseUp);

                        RegionPath = new GraphicsPath();

                        RegionPath.StartFigure();
                        RegionPath.AddEllipse(0, 0, Width, Height);
                        RegionPath.CloseFigure();

                        Region = new Region(RegionPath);
                    }

                    void OnMouseEnter(object sender, EventArgs e)
                    {
                        Image = Properties.Resources.returnButtonHover;
                    }

                    void OnMouseLeave(object sender, EventArgs e)
                    {
                        Image = Properties.Resources.returnButtonIdle;
                    }

                    void OnMouseDown(object sender, MouseEventArgs e)
                    {
                        Image = Properties.Resources.returnButtonClick;
                    }

                    void OnMouseUp(object sender, MouseEventArgs e)
                    {
                        Image = Properties.Resources.returnButtonIdle;                      
                    }
                }

                private class ChatMessagesPanel : DoubleBufferPanel
                {
                    List<MessageBox> messageBoxList;
                    List<MessageTimeLabel> messageTimeLabelList;
                    List<MessageDecorBox> messageDecorBoxList;

                    enum MessageType { Read, Unread, Self };

                    private const int WM_HSCROLL = 0x114;
                    private const int WM_VSCROLL = 0x115;

                    protected override void WndProc(ref Message m)
                    {
                        if ((m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                        && (((int)m.WParam & 0xFFFF) == 5))
                        {
                            // Change SB_THUMBTRACK to SB_THUMBPOSITION
                            m.WParam = (IntPtr)(((int)m.WParam & ~0xFFFF) | 4);
                        }
                        base.WndProc(ref m);
                    }

                    private void OnScrollMW(object sender, MouseEventArgs e)
                    {
                        this.Invalidate();

                        OnScroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, 120));
                    }

                    private void OnScroll(object sender, ScrollEventArgs e)
                    {
                        if (e.Type == ScrollEventType.First)
                        {
                            LockWindowUpdate(this.Handle);
                        }
                        else
                        {
                            LockWindowUpdate(IntPtr.Zero);
                            this.Update();
                            if (e.Type != ScrollEventType.Last) LockWindowUpdate(this.Handle);
                        }
                    }

                    [DllImport("user32.dll", SetLastError = true)]
                    private static extern bool LockWindowUpdate(IntPtr hWnd);

                    public ChatMessagesPanel()
                    {
                        messageBoxList = new List<MessageBox>();
                        messageTimeLabelList = new List<MessageTimeLabel>();
                        messageDecorBoxList = new List<MessageDecorBox>();

                        Location = new Point(0, 39);
                        Size = new Size(tabSize.Width + 15, tabSize.Height - chatInputPanelHeight - 39);

                        HorizontalScroll.Maximum = 0;
                        AutoScroll = false;
                        VerticalScroll.Visible = false;
                        AutoScrollMargin = new Size(0, 6);
                        AutoScroll = true;

                        this.HandleCreated += new EventHandler(GetMessages);
                    }

                    public void GetMessages(object sender, EventArgs eventArgs)
                    {
                        FileIOStreamer fileIO = new FileIOStreamer();                     

                        string dialogueData = fileIO.ReadFromFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + ChatID + ".txt");
                        string[] dialogueMessages = dialogueData.Split(new string[] { "message==", "==message" }, StringSplitOptions.RemoveEmptyEntries);

                        string[] messageSplitstrings = new string[] { "time=", "content=", "status=" };

                        Array.Sort(dialogueMessages, MessageSortByTime);

                        int lastMessageIndex = messageBoxList.Count;

                        for (int i = lastMessageIndex; i < dialogueMessages.Length; i++)
                        {
                            string[] messageData = dialogueMessages[i].Split(messageSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                            //[0] - message time, [1] - message status, [2] - message content

                            messageBoxList.Insert(0, new MessageBox(messageData[0], messageData[1], messageData[2]));
                            
                            if (messageBoxList[0].MessageType == MessageType.Self)
                            {
                                messageBoxList[0].Left = tabSize.Width - messageBoxList[0].Width - 60;
                            }
                            else
                            {
                                messageBoxList[0].Left = 40;
                            }

                            messageTimeLabelList.Insert(0, new MessageTimeLabel(messageBoxList[0]));
                            messageDecorBoxList.Insert(0, new MessageDecorBox(messageBoxList[0]));
                        }

                        ShowMessages();
                    }

                    int MessageSortByTime(string message1, string message2)
                    {
                        string[] messageSubstrings1 = message1.Split(new string[] { "time=", "content=", "status=" }, StringSplitOptions.RemoveEmptyEntries);
                        string[] messageSubstrings2 = message2.Split(new string[] { "time=", "content=", "status=" }, StringSplitOptions.RemoveEmptyEntries);

                        DateTime dateTime1 = DateTime.Parse(messageSubstrings1[0]).ToUniversalTime();
                        DateTime dateTime2 = DateTime.Parse(messageSubstrings2[0]).ToUniversalTime();

                        return (dateTime1.CompareTo(dateTime2));
                    }

                    void ShowMessages()
                    {
                        int totalMessagesHeight = 0;

                        Controls.Clear();

                        foreach(MessageBox message in messageBoxList)
                        {
                            totalMessagesHeight += message.Height + 6;
                        }

                        for(int i = 0; i < messageBoxList.Count; i++)
                        {
                            if(i == 0)
                            {
                                if (totalMessagesHeight <= Height)
                                {
                                    messageBoxList[i].Top = Height - messageBoxList[i].Height - 6;
                                }
                                else
                                {
                                    messageBoxList[i].Top = totalMessagesHeight - messageBoxList[i].Height;                                 
                                }
                            }
                            else
                            {
                                messageBoxList[i].Top = messageBoxList[i - 1].Top - 6 - messageBoxList[i].Height;
                            }

                            messageTimeLabelList[i].Location = new Point(tabSize.Width - 33 - messageTimeLabelList[i].Width / 2, 
                                messageBoxList[i].Top + messageBoxList[i].Height / 2 - messageTimeLabelList[i].Height / 2);

                            messageDecorBoxList[i].Location = new Point(0, messageBoxList[i].Location.Y);
                            messageDecorBoxList[i].GetRegion();

                            Controls.Add(messageBoxList[i]);
                            Controls.Add(messageTimeLabelList[i]);

                            if (messageBoxList[i].MessageType != MessageType.Self)
                            {
                                Controls.Add(messageDecorBoxList[i]);
                            }                      
                        }

                        if (messageBoxList.Count() > 0)
                        {
                            ScrollControlIntoView(messageBoxList[0]);
                        }
                    }

                    private class MessageTimeLabel : Label
                    {
                        public MessageTimeLabel(MessageBox message)
                        {
                            Font = new Font("Verdana", 6, FontStyle.Italic);
                            BackColor = Color.Transparent;
                            ForeColor = Color.Gray;
                            Text = message.UniversalMessageTime.ToLocalTime().ToShortTimeString();
                            Size = TextRenderer.MeasureText(Text, Font);
                            Location = new Point(tabSize.Width - 33 - Width / 2, message.Top + message.Height / 2 - Height / 2);
                        }
                    }

                    private class MessageDecorBox : PictureBox
                    {
                        private enum DecorType { Left, Right }
                        private enum DecorTier { Small, Medium, Big }

                        GraphicsPath FillPath;
                        GraphicsPath DrawPathWhite;
                        GraphicsPath DrawPathBlue;

                        SolidBrush SolidBrush;
                        Pen PenWhite;
                        Pen PenBlue;

                        DecorType MessageDecorType { get; set; }
                        DecorTier MessageDecorTier { get; set; }

                        public MessageDecorBox(MessageBox message)
                        {
                            Location = new Point(0, message.Location.Y);
                            Size = new Size(message.Location.X, message.Height);
                            MessageDecorType = DecorType.Left;

                            if (message.Height <= 40) MessageDecorTier = DecorTier.Small;
                            else if (message.Height <= 60) MessageDecorTier = DecorTier.Medium;
                            else MessageDecorTier = DecorTier.Big;

                            GetRegion();
                        }

                        public void GetRegion()
                        {
                            FillPath = new GraphicsPath();

                            FillPath.StartFigure();
                            FillPath.AddEllipse(new Rectangle(2, 9, 10, 10));
                            FillPath.AddEllipse(new Rectangle(15, 3, 20, 20));

                            if (MessageDecorTier != DecorTier.Small)
                            {
                                DrawPathWhite = new GraphicsPath();
                                DrawPathBlue = new GraphicsPath();

                                DrawPathWhite.StartFigure();
                                DrawPathBlue.AddEllipse(new Rectangle(10, 14, 24, 24));
                                DrawPathWhite.AddEllipse(new Rectangle(14, 2, 22, 22));
                                DrawPathWhite.CloseFigure();

                                if(MessageDecorTier == DecorTier.Medium)
                                {
                                    Top += 4;
                                }

                                if (MessageDecorTier == DecorTier.Big)
                                {
                                    FillPath.AddEllipse(new Rectangle(25, 41, 9, 9));
                                }
                            }
                            else
                            {
                                Top += 6;
                                Size = new Size(Width, Height - 6);
                            }

                            FillPath.CloseFigure();
                        }

                        protected override void OnPaint(PaintEventArgs pe)
                        {
                            SolidBrush = new SolidBrush(Color.FromArgb(255, 93, 143, 217));
                            PenWhite = new Pen(Color.White, 2);
                            PenBlue = new Pen(Color.FromArgb(255, 93, 143, 217), 2);

                            pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                            pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                            if (MessageDecorType == DecorType.Left)
                            {
                                if (MessageDecorTier != DecorTier.Small)
                                {
                                    pe.Graphics.DrawPath(PenBlue, DrawPathBlue);
                                    pe.Graphics.DrawPath(PenWhite, DrawPathWhite);
                                    pe.Graphics.FillPath(SolidBrush, FillPath);
                                }
                                else
                                {
                                    pe.Graphics.FillPath(SolidBrush, FillPath);
                                }
                            }           

                            base.OnPaint(pe);
                        }
                    }

                    private class MessageBox : DoubleBufferPanel
                    {
                        //PictureBox bubblesPictureBox;
                        MessageBackground messageBoxBackground;
                        Label messageLabel;

                        public MessageType MessageType { get; set; }

                        public DateTime UniversalMessageTime { get; set; }

                        public MessageBox(string messageTime, string messageStatus, string messageContent)
                        {
                            messageLabel = new Label();
                            messageLabel.Location = new Point(10, 10);
                            messageLabel.Font = new Font("Verdana", 10, FontStyle.Regular);
                            messageLabel.BackColor = Color.Transparent;
                            messageLabel.ForeColor = Color.White;
                            messageLabel.Text = ContentWordWrap(messageContent);
                            messageLabel.Size = TextRenderer.MeasureText(messageLabel.Text, messageLabel.Font);

                            UniversalMessageTime = DateTime.Parse(messageTime);

                            switch (messageStatus)
                            {
                                case ("unread"):
                                    MessageType = MessageType.Unread;
                                    messageLabel.ForeColor = Color.Black;
                                    break;
                                case ("read"):
                                    MessageType = MessageType.Read;
                                    messageLabel.ForeColor = Color.White;
                                    break;
                                case ("sent"):
                                    MessageType = MessageType.Self;
                                    messageLabel.ForeColor = Color.White;
                                    break;
                            }
                           

                            Size = new Size(messageLabel.Width + 20, messageLabel.Height + 20);

                            messageBoxBackground = new MessageBackground();
                            messageBoxBackground.Size = Size;
                            messageBoxBackground.MessageType = MessageType;
                            messageBoxBackground.GetRegion();

                            Controls.Add(messageBoxBackground);
                            messageBoxBackground.Controls.Add(messageLabel);                          
                            messageLabel.BringToFront();
                        }


                        string ContentWordWrap(string content)
                        {
                            int lineLength;

                            if(content.Length < 150)
                            {
                                lineLength = 30;
                            }
                            else if(content.Length < 300)
                            {
                                lineLength = 40;
                            }
                            else
                            {
                                lineLength = 55;
                            }

                            for(int i = 0; i < content.Length; i++)
                            {
                                int indexOfPreviousLineChange = content.LastIndexOf("\n");

                                if(indexOfPreviousLineChange == i - lineLength)
                                {
                                    int indexOfNextSpace = i + content.Substring(i).IndexOf(' ');
                                    int indexOfPreviousSpace = content.Substring(0, i).LastIndexOf(' ');

                                    if (content.Substring(i).IndexOf(' ') < 10 && content.Substring(i).Length >= 10)
                                    {                                      
                                        content = content.Insert(indexOfNextSpace, "\n");
                                        content = content.Remove(indexOfNextSpace + 1, 1);
                                    }
                                    else
                                    {                                      
                                        content = content.Insert(indexOfPreviousSpace, "\n");
                                        content = content.Remove(indexOfPreviousSpace + 1, 1);
                                    }
                                }
                            }

                            return content;
                        }

                        private class MessageBackground : PictureBox
                        {
                            GraphicsPath path;
                            SolidBrush SolidBrush { get; set; }
                            Pen Pen { get; set; }
                          
                            int cornerRadius { get; set; } = 20;
                            int borderMargin { get; set; } = 4;

                            public MessageType MessageType { get; set; }

                            public void GetRegion()
                            {
                                path = new GraphicsPath();

                                path.StartFigure();

                                path.AddArc(new Rectangle(borderMargin, borderMargin, cornerRadius, cornerRadius), 180, 90);
                                path.AddLine(cornerRadius + borderMargin, borderMargin, Width - cornerRadius - borderMargin, borderMargin);

                                path.AddArc(new Rectangle(Width - cornerRadius - borderMargin, borderMargin, cornerRadius, cornerRadius), 270, 90);
                                path.AddLine(Width - borderMargin, cornerRadius + borderMargin, Width - borderMargin, Height - cornerRadius - borderMargin);

                                path.AddArc(new Rectangle(Width - cornerRadius - borderMargin, Height - cornerRadius - borderMargin, cornerRadius, cornerRadius), 0, 90);
                                path.AddLine(Width - cornerRadius - borderMargin, Height - borderMargin, cornerRadius + borderMargin, Height - borderMargin);

                                path.AddArc(new Rectangle(borderMargin, Height - cornerRadius - borderMargin, cornerRadius, cornerRadius), 90, 90);
                                path.AddLine(borderMargin, Height - cornerRadius + borderMargin, borderMargin, cornerRadius + borderMargin);

                                path.CloseFigure();

                                switch(MessageType)
                                {
                                    case MessageType.Self:
                                        SolidBrush = new SolidBrush(Color.FromArgb(255, 93, 143, 217));
                                        break;
                                    case MessageType.Unread:
                                        Pen = new Pen(Color.FromArgb(255, 93, 143, 217), 4);
                                        break;
                                }
                            }

                            protected override void OnPaint(PaintEventArgs pe)
                            {
                                pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                                pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                                pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                                if (MessageType == MessageType.Self)
                                {
                                    pe.Graphics.FillPath(SolidBrush, path);
                                }
                                else
                                {
                                    pe.Graphics.DrawPath(Pen, path);

                                }
                                

                                base.OnPaint(pe);
                            }
                        }
                    }
                }

                private class ChatInputPanel : DoubleBufferPanel
                {
                    BackgroundPictureBox textBoxBackground;
                    WaterMarkRichTextBox messageTextBox;
                    SendMessageButton sendMessageButton;
                    System.Timers.Timer multilineTimer;

                    ActiveDialogueTab DialoguePanelParent { get; set; }

                    static int TextBoxHeightLines { get; set; } = 1;
                    static int TextBoxHeight;

                    delegate void MultilineCheckDelegate();

                    

                    public ChatInputPanel(Panel parentPanel) //Constructs using parent panel reference due to the way Send Button class is structured
                    {                                        //Posssibly need to mess around with input panel region to add button directly to it
                        Height = chatInputPanelHeight;
                        Width = tabSize.Width;
                        Location = new Point(0, tabSize.Height - Height);


                        DialoguePanelParent = (ActiveDialogueTab)parentPanel;

                        textBoxBackground = new BackgroundPictureBox();
                        textBoxBackground.Width = this.Width;
                        textBoxBackground.Height = this.Height;

                        messageTextBox = new WaterMarkRichTextBox();
                        messageTextBox.Width = this.Width - 70;                       
                        messageTextBox.Multiline = true;
                        messageTextBox.WordWrap = true;
                        messageTextBox.ScrollBars = RichTextBoxScrollBars.None;
                        messageTextBox.Location = new Point(2, 8);
                        messageTextBox.BorderStyle = BorderStyle.None;
                        messageTextBox.Watermark = "Type your message here...";
                        messageTextBox.Font = new Font("Verdana", 10, FontStyle.Regular);
                        messageTextBox.Height = messageTextBox.Font.Height;

                        TextBoxHeight = messageTextBox.Font.Height * TextBoxHeightLines;

                        sendMessageButton = new SendMessageButton();
                        
                        textBoxBackground.GetRegion();
                        Controls.Add(textBoxBackground);
                        Controls.Add(messageTextBox);
                        parentPanel.Controls.Add(sendMessageButton);
                        messageTextBox.BringToFront();
                        sendMessageButton.BringToFront();

                        multilineTimer = new System.Timers.Timer(50);
                        multilineTimer.Elapsed += new ElapsedEventHandler(CheckMultiline);
                        multilineTimer.Start();

                        sendMessageButton.MouseUp += new MouseEventHandler(SendMessage);

                        messageTextBox.KeyDown += new KeyEventHandler(MessageTextBoxOnKeyDown);
                    }

                    void MessageTextBoxOnKeyDown(object sender, KeyEventArgs e)
                    {
                        if(e.KeyCode == Keys.Enter)
                        {
                            SendMessage(this, e);
                        }
                    }

                    public void SendMessage(object sender, EventArgs e)
                    {
                        if (messageTextBox.Text != messageTextBox.Watermark)
                        {
                            NetComponents.ClientSendMessage(ChatID, messageTextBox.Text);
                            DialoguePanelParent.chatMessagesPanel.GetMessages(DialoguePanelParent.chatMessagesPanel, e);
                            
                            messageTextBox.Text = "";
                        }
                    }

                    void CheckMultiline(object sender, ElapsedEventArgs eventArgs)
                    {
                        MultilineCheckDelegate multilineCheck = new MultilineCheckDelegate(GetTextBoxHeight);

                        if (InvokeRequired)
                        {
                            try
                            {
                                Invoke(multilineCheck);
                            }
                            catch
                            {
                                multilineTimer.Stop();
                                multilineTimer.Dispose();
                            }
                        }
                    }

                    void GetTextBoxHeight()
                    {
                        int textWidthPixels = TextRenderer.MeasureText(messageTextBox.Text, messageTextBox.Font).Width;                     

                        if (textWidthPixels / 769 + 1 != TextBoxHeightLines)
                        {
                            TextBoxHeightLines = textWidthPixels / 769 + 1;
                            int oldSelection = messageTextBox.SelectionStart;

                            if (TextBoxHeightLines + 1 < 6)
                            {
                                messageTextBox.ScrollBars = RichTextBoxScrollBars.None;
                                messageTextBox.SelectionStart = oldSelection;

                                messageTextBox.Height = messageTextBox.Font.Height * TextBoxHeightLines;

                                Location = new Point(Location.X, tabSize.Height - messageTextBox.Height - 18);
                                Height = 18 + messageTextBox.Height;

                                chatInputPanelHeight = Height;

                                TextBoxHeight = messageTextBox.Height;

                                textBoxBackground.Height = Height;
                                textBoxBackground.GetRegion();
                            }
                            else
                            {
                                messageTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
                                messageTextBox.SelectionStart = oldSelection;
                            }                          
                        }
                        
                    }

                    private class BackgroundPictureBox : PictureBox
                    {
                        GraphicsPath path;
                        GraphicsPath buttonBackgroundPath;
                        SolidBrush solidBrush = new SolidBrush(Color.FromArgb(255, 93, 143, 215));
                        SolidBrush whiteBrush = new SolidBrush(Color.White);

                        public void GetRegion()
                        {
                            path = new GraphicsPath();
                            buttonBackgroundPath = new GraphicsPath();

                            buttonBackgroundPath.StartFigure();
                            buttonBackgroundPath.AddEllipse(new Rectangle(Right - 60, Bottom - 59, 70, 70));
                            buttonBackgroundPath.CloseFigure();

                            path.StartFigure();
                            path.AddRectangle(new Rectangle(0, 0, Width, Height));

                            // '
                            path.AddLine(new Point(7, 5), new Point(Right - 73, 5));
                            path.AddArc(new Rectangle(Right - 73, 5, 8, 8), 270, 90);
                            // ,
                            path.AddLine(new Point(Right - 65, 13), new Point(Right - 65, 5 + TextBoxHeight));
                            path.AddArc(new Rectangle(Right - 73, 5 + TextBoxHeight, 8, 8), 0, 90);
                            //,
                            path.AddLine(new Point(Right - 73, 13 + TextBoxHeight), new Point(7, 13 + TextBoxHeight));
                            path.AddArc(new Rectangle(0, 5 + TextBoxHeight, 8, 8), 90, 90);
                            //'
                            path.AddLine(new Point(0, 5 + TextBoxHeight), new Point(0, 13));
                            path.AddArc(new Rectangle(0, 5, 8, 8), 180, 90);

                            path.CloseFigure();
                        }

                        protected override void OnPaint(PaintEventArgs pe)
                        {
                            pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                            pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;                          

                            pe.Graphics.FillPath(solidBrush, path);
                            pe.Graphics.FillPath(whiteBrush, buttonBackgroundPath);

                            base.OnPaint(pe);                          
                        }
                    }

                    private class SendMessageButton : Button
                    {
                        GraphicsPath regionPath;

                        public SendMessageButton()
                        {
                            Location = new Point(779, 413);
                            Size = new Size(70, 70);
                            FlatStyle = FlatStyle.Flat;
                            FlatAppearance.BorderSize = 0;
                            GetRegion();
                        }

                        public void GetRegion()
                        {
                            regionPath = new GraphicsPath();

                            regionPath.StartFigure();
                            regionPath.AddEllipse(new Rectangle(1, 1, Width - 1, Height - 1));
                            regionPath.CloseFigure();

                            Region = new Region(regionPath);

                            Image = Properties.Resources.sendButtonIdle;

                            MouseDown += new MouseEventHandler(OnMouseDown);
                            MouseUp += new MouseEventHandler(OnMouseUp);
                            MouseEnter += new EventHandler(OnMouseEnter);
                            MouseLeave += new EventHandler(OnMouseLeave);
                        }

                        void OnMouseEnter(object sender, EventArgs e)
                        {
                            Image = Properties.Resources.sendButtonHover;
                        }

                        void OnMouseLeave(object sender, EventArgs e)
                        {
                            Image = Properties.Resources.sendButtonIdle;
                        }

                        void OnMouseDown(object sender, MouseEventArgs e)
                        {
                            Image = Properties.Resources.sendButtonClick;
                        }

                        void OnMouseUp(object sender, EventArgs e)
                        {
                            Image = Properties.Resources.sendButtonHover;
                        }
                    }
                }                
            }

            private class SearchTab : MainPage
            {
                ConcurrentQueue<string> queryQueue;
                ConcurrentQueue<string> searchStateQueue;

                TextBox searchQueryTextBox;
                SearchResultsPanel searchResultsPanel;
                TabHatImage tabHatImage;

                volatile bool searchServiceRunning;

                public SearchTab()     
                {                  
                    Name = "Search";

                    tabHatImage = new TabHatImage(TabType.Search, "");
              
                    searchResultsPanel = new SearchResultsPanel();
                    searchQueryTextBox = new TextBox();
                    searchQueryTextBox.Size = new Size(tabSize.Width - 62, 26);
                    searchQueryTextBox.Location = new Point(56, 6);
                    searchQueryTextBox.Font = hatFont;

                    Controls.Add(tabHatImage);
                    Controls.Add(searchQueryTextBox);
                    Controls.Add(searchResultsPanel);

                    tabHatImage.BringToFront();
                    searchQueryTextBox.BringToFront();

                    searchQueryTextBox.TextChanged += new EventHandler(OnSearchQueryChanged);

                    queryQueue = new ConcurrentQueue<string>();
                    searchStateQueue = new ConcurrentQueue<string>();

                    searchServiceRunning = true;
                    searchStateQueue.Enqueue("wait_search");

                    Thread searchManagerThread = new Thread(SearchManager);
                    searchManagerThread.Start();

                    Disposed += new EventHandler(OnTabChanged);
                }

                async void OnSearchQueryChanged(object sender, EventArgs eventArgs)
                {
                    await Task.Run(() =>
                    {
                        if (queryQueue.Count != 0)
                        {
                            string result;
                            queryQueue.TryDequeue(out result);
                        }
                        //SEND QUERY THROUGH COLLECTION HERE
                        queryQueue.Enqueue(searchQueryTextBox.Text);
                        searchStateQueue.Enqueue("do_search");
                    });
                }

                void OnTabChanged(object sender, EventArgs eventArgs)
                {
                    searchServiceRunning = false;
                }

                void SearchManager()
                {
                    //SearchResultsPanel searchResultsPanel = (SearchResultsPanel)parentPanel;

                    while (searchServiceRunning == true)
                    {
                        string signal;
                        searchStateQueue.TryDequeue(out signal);

                        if (signal == "do_search")
                        {
                            string searchQueryString;
                            queryQueue.TryDequeue(out searchQueryString);

                            string searchResultString = NetComponents.ClientRequestArbitrary("[searchs_request]", searchQueryString, true);

                            try
                            {
                                searchResultString = searchResultString.Substring(0, searchResultString.IndexOf('\0'));
                            }
                            catch
                            {

                            }

                            if (searchResultString != "=no_match=")
                            {
                                string[] searchResultUserdata = searchResultString.Split(new string[1] { "user=" }, StringSplitOptions.RemoveEmptyEntries);
                                List<string> searchResultsList = new List<string>(searchResultUserdata);

                                SearchResultsPanel.SearchRefreshDelegate controlDisplay = new SearchResultsPanel.SearchRefreshDelegate(searchResultsPanel.DisplaySearchResults);
                                searchResultsPanel.BeginInvoke(controlDisplay, searchResultsList, SearchResultsPanel.SearchCategory.People);
                            }
                            else //This happens if the query textbox is empty after being erased
                            {
                                SearchResultsPanel.ControlRemovalDelegate controlRemoval = new SearchResultsPanel.ControlRemovalDelegate(searchResultsPanel.RefreshSearchResults);
                                searchResultsPanel.BeginInvoke(controlRemoval, true);
                            }

                            searchStateQueue.Enqueue("do_wait");
                        }
                        else
                        {
                            searchStateQueue.Enqueue("do_wait");
                        }
                    }
                }

                private class SearchResultsPanel : Panel
                {
                    public enum SearchCategory { All, People, Groups };
                    delegate void ControlCreationDelegate(DoubleBufferPanel searchResultBox, Control thumbnail, Control title, Control subtitle, Control button1, Control button2);
                    public delegate void SearchRefreshDelegate(List<string> searchList, SearchCategory searchCategory);
                    public delegate void ControlRemovalDelegate(bool switchCondition);

                    int searchResultCount = 0;

                    Font titleFont = new Font("Verdana", 12, FontStyle.Regular);
                    Font subtitleFont = new Font("Verdana", 9, FontStyle.Italic);

                    Point SearchResultPanelLocation { get; set; } = new Point(6, 38);
                    Size SearchResultPanelSize { get; set; } = new Size(tabSize.Width - 6 + 15, tabSize.Height - 38);

                    public SearchResultsPanel()
                    {
                        DoubleBuffered = true;

                        Location = SearchResultPanelLocation;
                        Size = SearchResultPanelSize;                       

                        HorizontalScroll.Maximum = 0;
                        AutoScroll = false;
                        VerticalScroll.Visible = false;
                        AutoScroll = true;
                        BackgroundImage = Properties.Resources.searchPanelBackground;
                    }

                    public void DisplaySearchResults(List<string> searchResultsList, SearchCategory searchCategory)
                    {
                        string[] searchResultSplitStrings = new string[3] { "id=", "login=", "name=" };
                        List<DoubleBufferPanel> searchResultBoxesList = new List<DoubleBufferPanel>();

                        //Refreshes results if there are more or less boxes screen than new results
                        if (searchResultCount != searchResultsList.Count())
                        {
                            ControlRemovalDelegate controlRemoval = new ControlRemovalDelegate(RefreshSearchResults);
                            BeginInvoke(controlRemoval, false);
                        }

                        if (searchResultsList.Count() == 0)
                        {
                            BackgroundImage = Properties.Resources.searchPanelBackground;
                        }
                        else
                        {
                            BackgroundImage = null;
                        }

                        for (int i = 0; i < searchResultsList.Count; i++)
                        {
                            string[] searchResultSubstrings = searchResultsList[i].Split(searchResultSplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);
                            //For searchResultSubstrings, [0] = id, [1] = username, [2] = name

                            if(searchResultSubstrings[0] == "server_closed")
                            {
                                break;
                            }

                            System.IO.MemoryStream thumbnailStream = new System.IO.MemoryStream();

                            Label searchResultTitleLabel = new Label();
                            Label searchResultSubtitleLabel = new Label();
                            PictureBox searchResultThumbnail = new PictureBox();
                            Button searchResultSendMessageButton = new Button();
                            Button searchResultSendFriendRequestButton = new Button();

                            searchResultBoxesList.Add(new DoubleBufferPanel());
                            searchResultBoxesList[i].Height = 80;
                            searchResultBoxesList[i].Width = SearchResultPanelSize.Width - 6;
                            searchResultBoxesList[i].Name = "searchResult_" + searchResultSubstrings[0];
                            searchResultBoxesList[i].Visible = false;
                            searchResultBoxesList[i].BackgroundImageLayout = ImageLayout.Tile;

                            searchResultThumbnail.Height = searchResultBoxesList[i].Height - 6;
                            searchResultThumbnail.Width = searchResultThumbnail.Height;
                            searchResultThumbnail.Name = "userthumb_" + searchResultSubstrings[0];
                            searchResultThumbnail.Location = new Point(0, 0);
                            Bitmap thumbnailImage = new Bitmap(Properties.Resources.PlaceholderProfilePicture,
                                                            searchResultThumbnail.Height, searchResultThumbnail.Width);

                            ImageFactory imageEditor = new ImageFactory();
                            imageEditor.Load(thumbnailImage);
                            imageEditor.RoundedCorners(searchResultThumbnail.Height / 2);
                            imageEditor.BackgroundColor(Color.White);
                            imageEditor.Save(thumbnailStream);

                            searchResultTitleLabel.AutoSize = true;
                            searchResultTitleLabel.Location = new Point(searchResultThumbnail.Location.X + searchResultThumbnail.Width, searchResultThumbnail.Location.Y + 12);
                            searchResultTitleLabel.Font = titleFont;

                            searchResultSubtitleLabel.AutoSize = true;
                            searchResultSubtitleLabel.Location = new Point(searchResultTitleLabel.Location.X + 2, searchResultTitleLabel.Height + searchResultTitleLabel.Location.Y + 3);
                            searchResultSubtitleLabel.Font = subtitleFont;
                            searchResultSubtitleLabel.ForeColor = Color.Gray;

                            //Sets category-specific conditions for result generation
                            if (searchCategory == SearchCategory.People)
                            {
                                searchResultSendMessageButton.FlatStyle = FlatStyle.Flat;
                                searchResultSendMessageButton.FlatAppearance.BorderSize = 0;
                                searchResultSendMessageButton.Width = 24;
                                searchResultSendMessageButton.Height = 24;
                                searchResultSendMessageButton.Location = new Point(searchResultBoxesList[i].Width - searchResultSendMessageButton.Width - 39 -
                                                SystemInformation.VerticalScrollBarWidth, searchResultBoxesList[i].Height / 2 - searchResultSendMessageButton.Height / 2);
                                searchResultSendMessageButton.BackgroundImage = Properties.Resources.searchResultSendMessage;
                                searchResultSendMessageButton.Name = "chatStart_" + searchResultSubstrings[0];

                                    //TO DO: Pause thread when new tab gets opened
                                    //TO DO: Add textures to this button
                                searchResultSendMessageButton.Click += new EventHandler(ShowUserDialogue);

                                searchResultSendFriendRequestButton.FlatStyle = FlatStyle.Flat;
                                searchResultSendFriendRequestButton.FlatAppearance.BorderSize = 0;
                                searchResultSendFriendRequestButton.Width = 24;
                                searchResultSendFriendRequestButton.Height = 24;
                                searchResultSendFriendRequestButton.Location = new Point(searchResultSendMessageButton.Location.X + searchResultSendMessageButton.Width + 9,
                                                                                        searchResultSendMessageButton.Location.Y);
                                searchResultSendFriendRequestButton.BackgroundImage = Properties.Resources.searchResultSendFriendRequest;

                                searchResultSendFriendRequestButton.MouseUp += new MouseEventHandler(AddFriendButtonMouseUp);
                                searchResultSendFriendRequestButton.MouseEnter += new EventHandler(AddFriendButtonMouseEnter);
                                searchResultSendFriendRequestButton.MouseLeave += new EventHandler(AddFriendButtonMouseLeave);
                            }

                            //The statement below arranges every result box one after another on y axis
                            if (i >= 1)
                            {
                                searchResultBoxesList[i].Location = new Point(searchResultBoxesList[i - 1].Location.X,
                                                                    searchResultBoxesList[i - 1].Height + searchResultBoxesList[i - 1].Location.Y);
                            }
                            else
                            {
                                searchResultBoxesList[i].Location = new Point(6, 10);
                            }

                            searchResultThumbnail.Image = Image.FromStream(thumbnailStream);
                            searchResultThumbnail.Click += new EventHandler(ShowUserProfile);

                            searchResultTitleLabel.Text = searchResultSubstrings[2]; //Name is written in title
                            searchResultSubtitleLabel.Text = searchResultSubstrings[1]; //Username is written in subtitle in smaller font
                          
                            ControlCreationDelegate controlCreationDelegate = new ControlCreationDelegate(AddSearchResultBox);
                            BeginInvoke(controlCreationDelegate, searchResultBoxesList[i], searchResultThumbnail,
                                    searchResultTitleLabel, searchResultSubtitleLabel, searchResultSendMessageButton, searchResultSendFriendRequestButton);

                            searchResultCount++;
                        }
                    }

                    public void AddSearchResultBox(DoubleBufferPanel searchResultBox, Control thumbnail, Control title, Control subtitle, Control button1, Control button2)
                    {
                        Controls.Add(searchResultBox);
                        searchResultBox.Controls.Add(thumbnail);
                        searchResultBox.Controls.Add(title);
                        searchResultBox.Controls.Add(subtitle);
                        searchResultBox.Controls.Add(button1);
                        searchResultBox.Controls.Add(button2);
                        thumbnail.BringToFront();

                        searchResultBox.Visible = true;
                    }

                    public void RefreshSearchResults(bool noMatch)
                    {
                        this.Controls.Clear();

                        if (noMatch == true)
                        {
                            BackgroundImage = Properties.Resources.searchPanelBackground;
                        }
                        searchResultCount = 0;
                    }

                    public void AddFriendButtonMouseEnter(object sender, EventArgs eventArgs)
                    {
                        Button addFriendButton = (Button)sender;

                        addFriendButton.BackgroundImage = Properties.Resources.searchResultSendFriendRequestHover;
                    }

                    public void AddFriendButtonMouseLeave(object sender, EventArgs eventArgs)
                    {
                        Button addFriendButton = (Button)sender;

                        addFriendButton.BackgroundImage = Properties.Resources.searchResultSendFriendRequest;
                    }

                    public void AddFriendButtonMouseUp(object sender, EventArgs eventArgs)
                    {
                        Button addFriendButton = (Button)sender;

                        addFriendButton.BackgroundImage = Properties.Resources.searchResultSendFriendRequestClick;
                        addFriendButton.MouseLeave -= AddFriendButtonMouseLeave;
                        addFriendButton.MouseEnter -= AddFriendButtonMouseEnter;
                        addFriendButton.MouseUp -= AddFriendButtonMouseUp;

                        string requestID = addFriendButton.Parent.Name.Substring(addFriendButton.Parent.Name.IndexOf('_') + 1);

                        //TO DO: Move this logic into another thread

                        NetComponents.ClientRequestArbitrary("[add_friend_add_]", "addid=" + requestID, true, true);
                    }
                }
            }
            private class SettingsTab : MainPage
            {
                TabHatImage tabHatImage;

                public SettingsTab()
                {
                    Name = "Settings";

                    tabHatImage = new TabHatImage(TabType.Search, Name);

                    Controls.Add(tabHatImage);
                }               
            }
            private class LogOutTab : MainPage
            {



                public LogOutTab()
                {
                    //tabHatImage.Image = Properties.Resources.tabBackgroundLogOutHat;
                    //this.Controls.Add(tabHatImage);

                    //tabNameLabel.Text = "Log Out";
                    //this.Controls.Add(tabNameLabel);
                }              
            }

            private class MenuButton : Button   //This class encompasses all the buttons to the left of the tabs
            {
                Size buttonSize = new Size(60, 60);
                TabType thisTabType;

                Bitmap backgroundImageIdle;
                Bitmap backgroundImageOnHover;
                Bitmap backgroundImageOnClick;
               

                public MenuButton(Panel parentMainPage, MainPage instanceMainPage, TabType tabType)
                {
                    FlatStyle = FlatStyle.Flat;
                    FlatAppearance.BorderSize = 0;
                    TextAlign = ContentAlignment.MiddleLeft;
                    Font = titleFont;
                    thisTabType = tabType;

                    int buttonY;                   

                    if(parentMainPage.Controls.OfType<MenuButton>().Count() > 0)
                    {               
                        buttonY = parentMainPage.Controls.OfType<MenuButton>().Last().Location.Y + parentMainPage.Controls.OfType<MenuButton>().Last().Height - 1;
                    }
                    else
                    {
                        buttonSize.Height = 97;
                        buttonY = 6;
                    }

                    Size = buttonSize;
                    Location = new Point(0, buttonY);

                    this.MouseEnter += new EventHandler(OnMouseEnter);
                    this.MouseLeave += new EventHandler(OnMouseLeave);
                    this.MouseDown += new MouseEventHandler(OnMouseDown);
                    this.MouseUp += new MouseEventHandler(OnMouseUp);

                    switch (tabType)
                    {
                        case TabType.MainPage:
                            {
                                backgroundImageIdle = Properties.Resources.homeButtonIdle;
                                backgroundImageOnHover = Properties.Resources.homeButtonHover;
                                backgroundImageOnClick = Properties.Resources.homeButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Main Page";
                                break;
                            }
                        case TabType.Friends:
                            {
                                backgroundImageIdle = Properties.Resources.friendsButtonIdle;
                                backgroundImageOnHover = Properties.Resources.friendsButtonHover;
                                backgroundImageOnClick = Properties.Resources.friendsButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Friends";
                                break;
                            }
                        case TabType.Dialogues:
                            {
                                backgroundImageIdle = Properties.Resources.dialoguesButtonIdle;
                                backgroundImageOnHover = Properties.Resources.dialoguesButtonHover;
                                backgroundImageOnClick = Properties.Resources.dialoguesButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Dialogues";
                                break;
                            }
                        case TabType.Search:
                            {
                                backgroundImageIdle = Properties.Resources.searchButtonIdle;
                                backgroundImageOnHover = Properties.Resources.searchButtonHover;
                                backgroundImageOnClick = Properties.Resources.searchButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Search";
                                break;
                            }
                        case TabType.Settings:
                            {
                                backgroundImageIdle = Properties.Resources.settingsButtonIdle;
                                backgroundImageOnHover = Properties.Resources.settingsButtonHover;
                                backgroundImageOnClick = Properties.Resources.settingsButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Settings";
                                break;
                            }
                        case TabType.LogOut:
                            {
                                backgroundImageIdle = Properties.Resources.logOutButtonIdle;
                                backgroundImageOnHover = Properties.Resources.logOutButtonHover;
                                backgroundImageOnClick = Properties.Resources.logOutButtonClick;

                                BackgroundImage = backgroundImageIdle;
                                //Text = "Log Out";
                                break;
                            }
                    }

                    void OnMouseEnter(object sender, EventArgs eventArgs)
                    {
                        BackgroundImage = backgroundImageOnHover;
                    }

                    void OnMouseLeave(object sender, EventArgs eventArgs)
                    {                      
                        BackgroundImage = backgroundImageIdle;
                    }

                    void OnMouseDown(object sender, EventArgs eventArgs)
                    {
                        BackgroundImage = backgroundImageOnClick;
                    }

                    void OnMouseUp(object sender, EventArgs eventArgs)
                    {
                        BackgroundImage = backgroundImageIdle;

                        instanceMainPage.ClearAll();
                        instanceMainPage.OpenNewTab(tabType);
                    }

                }
            }

        }

        public class FrontDoorPage : Panel
        {
            delegate void PanelAnimationDelegate(Point point);

            EventArgs eventArgs = new EventArgs();
            event EventHandler PanelShown;
            event EventHandler PanelHidden;

            char[] restrictedSymbols = new char[1] { '=', };
            string[] serverReplySplitStrings = new string[2] { "id=", "hash=" };

            public enum CredentialsPanelType { Login, Registration }

            Size credentialsPanelSize = new Size(320, 480);
            Point credentialsPanelLocation = new Point(-320, 0);

            public FrontDoorPage()
            {
                DoubleBuffered = true;

                Size = new Size(900, 480);
                Location = new Point(0, 0);
                BackgroundImage = Properties.Resources.frontDoorPanelBackground;
            }

            public void PrepareCredentialPanels()
            {
                LoginPanel loginPanel = new LoginPanel(this);
                loginPanel.Size = credentialsPanelSize;
                loginPanel.Location = credentialsPanelLocation;
                loginPanel.BackgroundImage = Properties.Resources.loginPanelBackground;
                this.Controls.Add(loginPanel);

                RegistrationPanel registrationPanel = new RegistrationPanel(this);
                registrationPanel.Size = credentialsPanelSize;
                registrationPanel.Location = credentialsPanelLocation;
                registrationPanel.BackgroundImage = Properties.Resources.registrationPanelBackground;
                this.Controls.Add(registrationPanel);

                loginPanel.PanelShown(this, eventArgs);

                //registrationPanel.loginPanelReferenceObj = loginPanel;
                loginPanel.registrationPanelReferenceObj = registrationPanel;
                registrationPanel.loginPanelReferenceObj = loginPanel;
            }

            bool ContainsRestrictedSymbols(string loginPasswordCombination)
            {
                foreach (char symbol in restrictedSymbols)
                {
                    if (loginPasswordCombination.Contains(symbol))
                    {
                        return (true);
                    }
                }

                return (false);
            }            

            class LoginPanel : FrontDoorPage
            {
                Label statusMessage = new Label();
                PictureBox loginTextBoxBackgroundPicture = new PictureBox();
                PictureBox passwordTextBoxBackgroundPicture = new PictureBox();
                WaterMarkTextBox loginTextBox = new WaterMarkTextBox();
                WaterMarkTextBox passwordTextBox = new WaterMarkTextBox();
                Button loginButton = new Button();
                Button noAccountButton = new Button();


                //This property is used to pass object reference of another panel from enclosing class
                //This is done primarily to be able to switch panels on event fire
                public RegistrationPanel registrationPanelReferenceObj { get; set; }

                public LoginPanel(FrontDoorPage frontDoorPage)
                {                  
                    loginTextBox.Size = new Size(292, 30);
                    loginTextBox.Location = new Point(14, 250);
                    loginTextBox.BorderStyle = BorderStyle.None;
                    loginTextBox.Watermark = "Login";
                    this.Controls.Add(loginTextBox);

                    loginTextBoxBackgroundPicture.Size = new Size(308, 34);
                    loginTextBoxBackgroundPicture.Location = new Point(loginTextBox.Location.X - 8, loginTextBox.Location.Y - 6);
                    loginTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(loginTextBoxBackgroundPicture);                    
                    loginTextBox.BringToFront();

                    statusMessage.AutoSize = false;
                    statusMessage.TextAlign = ContentAlignment.MiddleCenter;
                    statusMessage.Size = new Size(308, 28);
                    statusMessage.Location = new Point(6, loginTextBox.Location.Y - statusMessage.Size.Height - 6);                   
                    statusMessage.Font = titleFont;
                    statusMessage.ForeColor = Color.Red;
                    this.Controls.Add(statusMessage);                  

                    passwordTextBox.Size = loginTextBox.Size;
                    passwordTextBox.Location = new Point(14, loginTextBox.Location.Y + 42);
                    passwordTextBox.BorderStyle = BorderStyle.None;
                    passwordTextBox.Watermark = "Password";
                    passwordTextBox.PasswordChar = '•';
                    this.Controls.Add(passwordTextBox);

                    passwordTextBoxBackgroundPicture.Size = new Size(308, 34);
                    passwordTextBoxBackgroundPicture.Location = new Point(passwordTextBox.Location.X - 8, passwordTextBox.Location.Y - 6);
                    passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(passwordTextBoxBackgroundPicture);
                    passwordTextBox.BringToFront();

                    loginButton.Size = new Size(308, 59);
                    loginButton.Location = new Point(6, passwordTextBox.Location.Y + 36);
                    loginButton.FlatStyle = FlatStyle.Flat;
                    loginButton.FlatAppearance.BorderSize = 0;
                    loginButton.BackgroundImage = Properties.Resources.frontDoorButton;
                    loginButton.Font = titleFont;
                    loginButton.TextAlign = ContentAlignment.MiddleCenter;
                    loginButton.ForeColor = Color.White;
                    //loginButton.ForeColor = Color.FromArgb(255, 78, 122, 183);
                    loginButton.Text = "Log In";
                    this.Controls.Add(loginButton);

                    noAccountButton.Size = new Size(308, 59);
                    noAccountButton.Location = new Point(6, loginButton.Location.Y + 67);
                    noAccountButton.FlatStyle = FlatStyle.Flat;
                    noAccountButton.FlatAppearance.BorderSize = 0;
                    noAccountButton.BackgroundImage = Properties.Resources.frontDoorButton;
                    noAccountButton.Font = titleFont;
                    noAccountButton.TextAlign = ContentAlignment.MiddleCenter;
                    noAccountButton.ForeColor = Color.White;
                    //noAccountButton.ForeColor = Color.FromArgb(255, 78, 122, 183);
                    noAccountButton.Text = "Don't have an account?\n Sign Up!";
                    this.Controls.Add(noAccountButton);

                    PanelShown += new EventHandler(PanelShow);
                    PanelHidden += new EventHandler(PanelHide);

                    loginButton.MouseDown += new MouseEventHandler(OnFrontDoorButtonMouseDown);
                    loginButton.MouseUp += new MouseEventHandler(OnFrontDoorButtonMouseUp);
                    noAccountButton.MouseDown += new MouseEventHandler(OnFrontDoorButtonMouseDown);
                    noAccountButton.MouseUp += new MouseEventHandler(OnFrontDoorButtonMouseUp);

                    loginButton.MouseEnter += new EventHandler(OnFrontDoorMouseEnter);
                    loginButton.MouseLeave += new EventHandler(OnFrontDoorButtonMouseLeft);
                    noAccountButton.MouseEnter += new EventHandler(OnFrontDoorMouseEnter);
                    noAccountButton.MouseLeave += new EventHandler(OnFrontDoorButtonMouseLeft);

                    foreach(WaterMarkTextBox textBox in this.Controls.OfType<WaterMarkTextBox>())
                    {
                        textBox.Click += new EventHandler(OnRefreshErrors);
                    }

                    loginButton.Click += new EventHandler(LogInAttempt);
                    noAccountButton.Click += new EventHandler(OnNoAccountClick);                                  
                }

                void OnFrontDoorButtonMouseDown(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;

                    thisButton.ForeColor = Color.White;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonClick;
                }

                void OnFrontDoorButtonMouseUp(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonHover;
                }

                void OnFrontDoorMouseEnter(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonHover;
                }

                void OnFrontDoorButtonMouseLeft(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButton;
                }

                void OnNoAccountClick(object sender, EventArgs eventArgs)
                {
                    PanelHide(this, eventArgs);
                    Thread.Sleep(200);
                    registrationPanelReferenceObj.PanelShow(registrationPanelReferenceObj, eventArgs);

                    OnRefreshErrors(this, eventArgs);
                }

                void OnRefreshErrors(object sender, EventArgs eventArgs)
                {
                    loginTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;

                    statusMessage.Text = "";
                    statusMessage.ForeColor = Color.FromArgb(255, 255, 98, 78);
                }
                public void LogInAttempt(object sender, EventArgs eventArgs)
                {
                    if(loginTextBox.watermarkApplied == true || passwordTextBox.watermarkApplied == true)
                    {
                        return;
                    }

                    string login = loginTextBox.Text;
                    string password = passwordTextBox.Text;

                    if (ContainsRestrictedSymbols(login))
                    {
                        statusMessage.Text = "Login contains invalid characters!";
                        loginTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }
                    if (ContainsRestrictedSymbols(password))
                    {
                        statusMessage.Text = "Password contains unacceptable characters!";
                        passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }

                    if (login == "" || password == "")
                    {
                        return;
                    }

                    string serverReply;
                    serverReply = NetComponents.LogPasRequestClientside(login, password);

                    serverReply = serverReply.Substring(0, serverReply.IndexOf('\0'));
                    string[] serverReplySubstrings = serverReply.Split(serverReplySplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);

                    statusMessage.Text = LogInHandler(serverReply);

                    if(statusMessage.Text == "Wrong login or password.")
                    {
                        loginTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                    }
                }              
            }

            public string LogInHandler(string serverReply)
            {
                //For serverReplySubstrings, index 0 is server flag, index 1 is ID, index 2 is hash
                string[] serverReplySubstrings = serverReply.Split(serverReplySplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);

                if (serverReplySubstrings[0] == "login_success")
                {
                    //Set local user directory for logged in user
                    FileIOStreamer.SetLocalUserDirectory(serverReplySubstrings[1]);

                    //Create a cookie in local client directory to keep user logged in
                    FileIOStreamer fileIO = new FileIOStreamer();

                    fileIO.ClearFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");
                    fileIO.WriteToFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt", "id=" +
                        serverReplySubstrings[1] + "confirmation=" + serverReplySubstrings[2], true);

                    MainPage mainPage = new MainPage();
                    Application.OpenForms[0].Controls.Add(mainPage);
                    mainPage.OpenMainPage();
                    mainPage.OpenNewTab(MainPage.TabType.MainPage);

                    NetComponents.ClientPendingMessageManager();

                    Thread messageReceiverThread = new Thread(NetComponents.ClientServerFlagListener);
                    messageReceiverThread.Start();                                      

                    this.Dispose();

                    return ("login_success");
                }
                else if (serverReply == "login_failure")
                {                   
                    return ("Wrong login or password.");
                }
                else
                {
                    return ("Login service unavailable.\nPlease try again later.");

                }
            }

            private class RegistrationPanel : FrontDoorPage
            {
                Label statusMessage = new Label();
                PictureBox firstNameTextBoxBackgroundPicture = new PictureBox();
                PictureBox usernameTextBoxBackgroundPicture = new PictureBox();
                PictureBox passwordTextBoxBackgroundPicture = new PictureBox();
                PictureBox repeatPasswordTextBoxBackgroundPicture = new PictureBox();
                WaterMarkTextBox firstNameTextBox = new WaterMarkTextBox();
                WaterMarkTextBox usernameTextBox = new WaterMarkTextBox();
                WaterMarkTextBox passwordTextBox = new WaterMarkTextBox();
                WaterMarkTextBox repeatPasswordTextBox = new WaterMarkTextBox();
                Button signUpButton = new Button();
                Button goBackButton = new Button();

                //This property is used to pass object reference of another panel from enclosing class
                //This is done primarily to be able to switch panels on event fire
                public LoginPanel loginPanelReferenceObj { get; set; }

                public RegistrationPanel(FrontDoorPage frontDoorPage)
                {
                    
                    //First Name Textbox
                    firstNameTextBox.Size = new Size(292, 30);
                    firstNameTextBox.Location = new Point(14, 250);
                    firstNameTextBox.BorderStyle = BorderStyle.None;
                    firstNameTextBox.Watermark = "First Name";
                    firstNameTextBox.TabIndex = 0;
                    this.Controls.Add(firstNameTextBox);

                    firstNameTextBoxBackgroundPicture.Size = new Size(308, 34);
                    firstNameTextBoxBackgroundPicture.Location = new Point(firstNameTextBox.Location.X - 8, firstNameTextBox.Location.Y - 6);
                    firstNameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(firstNameTextBoxBackgroundPicture);
                    firstNameTextBox.BringToFront();

                    //Status Message
                    statusMessage.AutoSize = false;
                    statusMessage.TextAlign = ContentAlignment.MiddleCenter;
                    statusMessage.Size = new Size(308, 28);
                    statusMessage.Location = new Point(6, firstNameTextBox.Location.Y - statusMessage.Size.Height - 7);
                    statusMessage.Font = hatFont;
                    statusMessage.ForeColor = Color.FromArgb(255, 255, 98, 78);
                    this.Controls.Add(statusMessage);

                    //Username Textbox
                    usernameTextBox.Size = firstNameTextBox.Size;
                    usernameTextBox.Location = new Point(14, firstNameTextBox.Location.Y + 42);
                    usernameTextBox.BorderStyle = BorderStyle.None;
                    usernameTextBox.Watermark = "Username";
                    usernameTextBox.TabIndex = 2;
                    this.Controls.Add(usernameTextBox);

                    usernameTextBoxBackgroundPicture.Size = new Size(308, 34);
                    usernameTextBoxBackgroundPicture.Location = new Point(usernameTextBox.Location.X - 8, usernameTextBox.Location.Y - 6);
                    usernameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(usernameTextBoxBackgroundPicture);
                    usernameTextBox.BringToFront();

                    //Password Textbox
                    passwordTextBox.Size = firstNameTextBox.Size;
                    passwordTextBox.Location = new Point(14, usernameTextBox.Location.Y + 42);
                    passwordTextBox.BorderStyle = BorderStyle.None;
                    passwordTextBox.Watermark = "Password";
                    passwordTextBox.PasswordChar = '•';
                    passwordTextBox.TabIndex = 3;
                    this.Controls.Add(passwordTextBox);

                    passwordTextBoxBackgroundPicture.Size = new Size(308, 34);
                    passwordTextBoxBackgroundPicture.Location = new Point(passwordTextBox.Location.X - 8, passwordTextBox.Location.Y - 6);
                    passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(passwordTextBoxBackgroundPicture);
                    usernameTextBox.BringToFront();

                    //Repeat Password Textbox
                    repeatPasswordTextBox.Size = firstNameTextBox.Size;
                    repeatPasswordTextBox.Location = new Point(14, passwordTextBox.Location.Y + 42);
                    repeatPasswordTextBox.BorderStyle = BorderStyle.None;
                    repeatPasswordTextBox.Watermark = "Repeat Password";
                    repeatPasswordTextBox.PasswordChar = '•';
                    repeatPasswordTextBox.TabIndex = 4;
                    this.Controls.Add(repeatPasswordTextBox);

                    repeatPasswordTextBoxBackgroundPicture.Size = new Size(308, 34);
                    repeatPasswordTextBoxBackgroundPicture.Location = new Point(repeatPasswordTextBox.Location.X - 8, repeatPasswordTextBox.Location.Y - 6);
                    repeatPasswordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    this.Controls.Add(repeatPasswordTextBoxBackgroundPicture);
                    usernameTextBox.BringToFront();

                    //SignUp Button
                    signUpButton.Size = new Size(152, 48);
                    signUpButton.Location = new Point(6 + 155, repeatPasswordTextBox.Location.Y + 36);
                    signUpButton.FlatStyle = FlatStyle.Flat;
                    signUpButton.FlatAppearance.BorderSize = 0;
                    signUpButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrow;
                    signUpButton.Font = titleFont;
                    signUpButton.TextAlign = ContentAlignment.MiddleCenter;
                    signUpButton.ForeColor = Color.White;
                    //signUpButton.ForeColor = Color.FromArgb(255, 78, 122, 183);
                    signUpButton.Text = "Sign Up";
                    signUpButton.TabIndex = 5;
                    this.Controls.Add(signUpButton);

                    //Go Back Button
                    goBackButton.Size = new Size(152, 48);
                    goBackButton.Location = new Point(6, repeatPasswordTextBox.Location.Y + 36);
                    goBackButton.FlatStyle = FlatStyle.Flat;
                    goBackButton.FlatAppearance.BorderSize = 0;
                    goBackButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrow;
                    goBackButton.Font = titleFont;
                    goBackButton.TextAlign = ContentAlignment.MiddleCenter;
                    goBackButton.ForeColor = Color.White;
                    //goBackButton.ForeColor = Color.FromArgb(255, 78, 122, 183);
                    goBackButton.Text = "Go Back";
                    goBackButton.TabIndex = 6;
                    this.Controls.Add(goBackButton);

                    PanelShown += new EventHandler(PanelShow);
                    PanelHidden += new EventHandler(PanelHide);

                    signUpButton.MouseDown += new MouseEventHandler(OnFrontDoorButtonMouseDown);
                    signUpButton.MouseUp += new MouseEventHandler(OnFrontDoorButtonMouseUp);
                    goBackButton.MouseDown += new MouseEventHandler(OnFrontDoorButtonMouseDown);
                    goBackButton.MouseUp += new MouseEventHandler(OnFrontDoorButtonMouseUp);

                    signUpButton.MouseEnter += new EventHandler(OnFrontDoorMouseEnter);
                    signUpButton.MouseLeave += new EventHandler(OnFrontDoorButtonMouseLeft);
                    goBackButton.MouseEnter += new EventHandler(OnFrontDoorMouseEnter);
                    goBackButton.MouseLeave += new EventHandler(OnFrontDoorButtonMouseLeft);

                    foreach(WaterMarkTextBox textBox in this.Controls.OfType<WaterMarkTextBox>())
                    {
                        textBox.Click += new EventHandler(OnRefreshErrors);
                    }

                    signUpButton.Click += new EventHandler(SignUpAttempt);
                    goBackButton.Click += new EventHandler(OnGoBackClick);
                }

                void OnGoBackClick(object sender, EventArgs eventArgs)
                {
                    PanelHide(this, eventArgs);
                    Thread.Sleep(200);
                    loginPanelReferenceObj.PanelShow(loginPanelReferenceObj, eventArgs);

                    OnRefreshErrors(this, eventArgs);
                }

                void OnFrontDoorButtonMouseDown(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;

                    thisButton.ForeColor = Color.White;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrowClick;
                }

                void OnFrontDoorButtonMouseUp(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrowHover;
                }

                void OnFrontDoorMouseEnter(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrowHover;
                }

                void OnFrontDoorButtonMouseLeft(object sender, EventArgs eventArgs)
                {
                    Button thisButton = (Button)sender;
                    thisButton.BackgroundImage = Properties.Resources.frontDoorButtonNarrow;
                }

                void OnRefreshErrors(object sender, EventArgs eventArgs)
                {
                    firstNameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    usernameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;
                    repeatPasswordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorder;

                    statusMessage.Text = "";
                    statusMessage.ForeColor = Color.FromArgb(255, 255, 98, 78);
                }

                public void SignUpAttempt(object sender, EventArgs eventArgs)
                {
                    string name = firstNameTextBox.Text;
                    string login = usernameTextBox.Text;
                    string password = passwordTextBox.Text;
                    string repeatPassword = repeatPasswordTextBox.Text;

                    if (usernameTextBox.watermarkApplied == true || passwordTextBox.watermarkApplied == true
                        || firstNameTextBox.watermarkApplied == true || repeatPasswordTextBox.watermarkApplied == true)
                    {
                        return;
                    }

                    if (ContainsRestrictedSymbols(name))
                    {
                        statusMessage.Text = "You can't use that name!";
                        firstNameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }
                    if (ContainsRestrictedSymbols(login))
                    {
                        statusMessage.Text = "You can't use that username!";
                        usernameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }
                    if (ContainsRestrictedSymbols(password))
                    {
                        statusMessage.Text = "You can't use that password!";
                        passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }
                    if (password != repeatPassword)
                    {
                        statusMessage.Text = "Passwords don't match.";
                        passwordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        repeatPasswordTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        return;
                    }

                    if (login == "" || password == "" || name == "")
                    {
                        return;
                    }

                    string serverReply = NetComponents.SignUpRequestClientside(name, login, password);

                    serverReply = serverReply.Substring(0, serverReply.IndexOf('\0'));
                    string[] serverReplySubstrings = serverReply.Split(serverReplySplitStrings, 2, StringSplitOptions.RemoveEmptyEntries);
                    //It isn't necessary to split strings at this point of functionality, but it might be needed in the future

                    switch (serverReplySubstrings[0])
                    {
                        case "sign_up_failure_1":
                            usernameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                            statusMessage.Text = "User with this username already exists.";
                            return;
                        case "sign_up_failure_2":
                            statusMessage.Text = "Server sign up service unavailable.";
                            return;
                        case "sign_up_success":
                            statusMessage.ForeColor = Color.Green;
                            statusMessage.Text = "You have successfully signed up!";

                            Application.DoEvents(); //TO DO: Find a way around this...
                            Thread.Sleep(1000);

                            PanelHide(this, eventArgs);
                            loginPanelReferenceObj.PanelShow(loginPanelReferenceObj, eventArgs);
                            return;
                    }
                    return;
                }

            }          

            public void PanelShow(object sender, EventArgs eventArgs)
            {
                Enabled = true;
                Visible = true;
                for (int i = Location.X; i <= 0; i = i + 32)
                {
                    Location = new Point(i, 0);
                    Update();
                    Thread.Sleep(2);
                }
            }

            public void PanelHide(object sender, EventArgs eventArgs)
            {         
                for (int i = 0; i >= -320; i = i - 32)
                {
                    Location = new Point(i, 0);
                    Update();
                    Thread.Sleep(2);
                }
                CleanUpPanel(this);
            }

            void CleanUpPanel(Panel currentPanel)
            {
                foreach(WaterMarkTextBox textBox in currentPanel.Controls.OfType<WaterMarkTextBox>())
                {
                    textBox.watermarkApplied = false;
                    textBox.Text = "";                  
                }
            }
        }

        public class LoadingPage : Panel
        {
            public Form1 ActiveForm { get; set; }
            PictureBox loadingImageBox;
            Label loadingMessageLabel;

            delegate void PageAddDelegate(Control control);
            delegate void BringToFrontDelegate();
            delegate void PanelSetupDelegate();
            delegate void MainPageAddDelegate(Control control);
            delegate void MessageChangeDelegate(string text);
            delegate void ControlDisposeDelegate();

            delegate string CookieLoginDelegate(string text);

            Size loadingPageSize = new Size(903, 480);
            Point loadingPageLocation = new Point(0, 0);


            public void OpenLoadingPage()
            {             
                loadingImageBox = new PictureBox();
                loadingImageBox.Size = new Size(250, 250);
                loadingImageBox.Location = new Point(loadingPageSize.Width / 2 - loadingImageBox.Width / 2, 75);
                loadingImageBox.Image = Properties.Resources.LoadingCog;
                

                loadingMessageLabel = new Label();
                loadingMessageLabel.Size = new Size(400, 21);
                loadingMessageLabel.Location = new Point(loadingPageSize.Width / 2 - loadingMessageLabel.Width / 2, 349);
                loadingMessageLabel.Font = titleFont;
                loadingMessageLabel.TextAlign = ContentAlignment.MiddleCenter;

                this.Controls.Add(loadingImageBox);
                this.Controls.Add(loadingMessageLabel);
                
                Thread initialHandshakeThread = new Thread(ClientStartUp);
                initialHandshakeThread.Start();
            }

            void ClientStartUp()
            {
                MessageChangeDelegate messageChangeDelegate = new MessageChangeDelegate(LoadingMessageSetText);
                            
                int attemptNumber = 2;
                string handshakeResult;

                Invoke(messageChangeDelegate, "Connecting...");
                while (loadingMessageLabel.Text != "Connected!")
                {
                    NetComponents.ClientSetServerEndpoints(NetComponents.ScanIP(), 8000);
                    handshakeResult = NetComponents.InitialHandshakeClient();

                    if (attemptNumber >= 100 || handshakeResult == "connection_fatal_error")
                    {
                        Invoke(messageChangeDelegate, "Server unavailable. Please try again later.");
                        loadingImageBox.Image = Client.Properties.Resources.WarningSign;

                        try
                        {
                            NetComponents.BreakBind(false);
                        }
                        catch
                        {

                        }

                        break;
                    }

                    if (handshakeResult == "session_expr")
                    {
                        Invoke(messageChangeDelegate, "Connected!");

                        Thread.Sleep(1000);

                        FrontDoorPage frontDoorPage = new FrontDoorPage();

                        PageAddDelegate pageAddDelegate = new PageAddDelegate(ActiveForm.Controls.Add);
                        BringToFrontDelegate bringToFrontDelegate = new BringToFrontDelegate(frontDoorPage.BringToFront);
                        PanelSetupDelegate panelSetupDelegate = new PanelSetupDelegate(frontDoorPage.PrepareCredentialPanels);
                        ControlDisposeDelegate controlDisposeDelegate = new ControlDisposeDelegate(this.Dispose);

                        Invoke(pageAddDelegate, frontDoorPage);
                        Invoke(bringToFrontDelegate);

                        Thread.Sleep(200);
                        Invoke(panelSetupDelegate);

                        return;
                    }
                    else if (handshakeResult.Substring(0, 13) == "login_success") // V This happens when user cookie matches server records V
                    {
                        Invoke(messageChangeDelegate, "Connected!");

                        Thread.Sleep(1000);

                        MainPage mainPage = new MainPage();
                        FrontDoorPage frontDoorPage = new FrontDoorPage();

                        MainPageAddDelegate mainPageAddDelegate = new MainPageAddDelegate(Application.OpenForms[0].Controls.Add);
                        OpenPanelDelegate openPanelDelegate = new OpenPanelDelegate(mainPage.OpenMainPage);
                        OpenTabDelegate openTabDelegate = new OpenTabDelegate(mainPage.OpenNewTab);
                        CookieLoginDelegate cookieLoginDelegate = new CookieLoginDelegate(frontDoorPage.LogInHandler);
                        BringToFrontDelegate bringToFrontDelegate = new BringToFrontDelegate(mainPage.BringToFront);
                        ControlDisposeDelegate controlDisposeDelegate = new ControlDisposeDelegate(this.Dispose);

                        Invoke(cookieLoginDelegate, handshakeResult);                        
                        Invoke(bringToFrontDelegate);
                        
                        connectedCheckTimer.Start();
                        return;
                    }
                    else
                    {
                        attemptNumber++;
                        Invoke(messageChangeDelegate, "Connection failure. Retrying, attempt " + attemptNumber + "...");
                    }
                }
            }

            void LoadingMessageSetText(string message)
            {
                loadingMessageLabel.Text = message;
            }
        }
    }

    public class DoubleBufferPanel : Panel
    {
        //public enum PanelType { Main, Friends, Dialogues, Search, Settings, LogOut}

        public DoubleBufferPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
  
    public class WaterMarkTextBox : TextBox
    {
        string customWatermark = "Watermark";
        public bool watermarkApplied = false;
        char specifiedPasswordChar;

        Font font;
        

        [Category("Appearance"), DefaultValue("Watermark"), Browsable(true)]
        public string Watermark
        {
            get
            {
                return customWatermark;
            }
            set
            {
                Text = "";
                customWatermark = value;
                Text = customWatermark;
            }
        }

        public WaterMarkTextBox()
        {
            font = new Font("Verdana", 10, FontStyle.Regular);
            Font = font;
            ForeColor = Color.Gray;           
            
            Text = customWatermark;
            watermarkApplied = true;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            specifiedPasswordChar = this.PasswordChar;
            PasswordChar = '\0';

            TextChanged += new EventHandler(this.WatermarkSwitch);
            KeyDown += new KeyEventHandler(this.SelectionMoved);
            MouseMove += new MouseEventHandler(this.ControlSelected);
            MouseDown += new MouseEventHandler(this.ControlSelected);

            WatermarkSwitch(this, EventArgs.Empty);

            this.SelectionStart = 0;
        }

        private void WatermarkSwitch(object sender, EventArgs eventArgs)
        {
            if (specifiedPasswordChar != '\0')
            {
                if (watermarkApplied == false)
                {
                    PasswordChar = specifiedPasswordChar;
                }
                else
                {
                    PasswordChar = '\0';
                }
            }

            if (Text.Length == 0 && watermarkApplied == false)
            {
                watermarkApplied = true;
                ForeColor = Color.Gray;
                Text = customWatermark;
                SelectionStart = 0;
            }

            if ((watermarkApplied == true && this.Text != customWatermark))
            {              
                watermarkApplied = false;
                ForeColor = Color.Black;

                try
                {
                    this.Text = Text.Replace(customWatermark, "");
                }
                catch
                {

                }
                this.SelectionStart = 1;
            }
        }

        private void SelectionMoved(object sender, KeyEventArgs eventArgs)
        {
            if ((eventArgs.KeyCode == Keys.Right || eventArgs.KeyCode == Keys.Left) && watermarkApplied == true)
            {
                eventArgs.Handled = true;
            }

            if((eventArgs.KeyCode == Keys.Down))
            {
                eventArgs.Handled = true;

                if (this.TabIndex < this.Parent.Controls.Count - 1)
                {
                    this.Parent.GetNextControl(this, true).Focus();
                }
            }

            if ((eventArgs.KeyCode == Keys.Up))
            {
                eventArgs.Handled = true;

                if (this.TabIndex > 0)
                {
                    this.Parent.GetNextControl(this, false).Focus();
                }

            }
        }

        private void ControlSelected(object sender, MouseEventArgs eventArgs)
        {
            if (watermarkApplied == true)
            {
                this.SelectionLength = 0;
                this.SelectionStart = 0;
            }
        }
    }

    public class WaterMarkRichTextBox : RichTextBox
    {
        string customWatermark = "Watermark";
        public bool watermarkApplied = false;

        int lengthOld;
        int selectionStartOld;

        Font font;


        [Category("Appearance"), DefaultValue("Watermark"), Browsable(true)]
        public string Watermark
        {
            get
            {
                return customWatermark;
            }
            set
            {
                Text = "";
                customWatermark = value;
                Text = customWatermark;
            }
        }

        public WaterMarkRichTextBox()
        {
            font = new Font("Verdana", 10, FontStyle.Regular);
            Font = font;
            ForeColor = Color.Gray;

            Text = customWatermark;
            watermarkApplied = true;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            TextChanged += new EventHandler(this.WatermarkSwitch);
            KeyDown += new KeyEventHandler(this.SelectionMoved);
            MouseMove += new MouseEventHandler(this.ControlSelected);
            MouseDown += new MouseEventHandler(this.ControlSelected);

            WatermarkSwitch(this, EventArgs.Empty);

            this.SelectionStart = 0;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            lengthOld = TextLength;
            selectionStartOld = SelectionStart;

            if(watermarkApplied == true)
            {
                lengthOld -= customWatermark.Length;
            }

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
            {
                Paste(DataFormats.GetFormat("Text"));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {                   
            base.OnKeyUp(e);

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
            {
                SelectionStart = selectionStartOld + (TextLength - lengthOld);
            }
        }

        private void WatermarkSwitch(object sender, EventArgs eventArgs)
        {
            if (Text.Length == 0 && watermarkApplied == false)
            {
                watermarkApplied = true;
                ForeColor = Color.Gray;
                Text = customWatermark;
                SelectionStart = 0;
            }

            if ((watermarkApplied == true && this.Text != customWatermark))
            {
                watermarkApplied = false;
                ForeColor = Color.Black;

                try
                {
                    this.Text = Text.Replace(customWatermark, "");
                }
                catch
                {

                }
                this.SelectionStart = 1;
            }
        }

        private void SelectionMoved(object sender, KeyEventArgs eventArgs)
        {
            if ((eventArgs.KeyCode == Keys.Right || eventArgs.KeyCode == Keys.Left) && watermarkApplied == true)
            {
                eventArgs.Handled = true;
            }

            if ((eventArgs.KeyCode == Keys.Down))
            {
                eventArgs.Handled = true;

                if (this.TabIndex < this.Parent.Controls.Count - 1)
                {
                    this.Parent.GetNextControl(this, true).Focus();
                }
            }

            if ((eventArgs.KeyCode == Keys.Up))
            {
                eventArgs.Handled = true;

                if (this.TabIndex > 0)
                {
                    this.Parent.GetNextControl(this, false).Focus();
                }

            }
        }

        private void ControlSelected(object sender, MouseEventArgs eventArgs)
        {
            if (watermarkApplied == true)
            {
                this.SelectionLength = 0;
                this.SelectionStart = 0;
            }
        }
    }
}