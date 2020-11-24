using ImageProcessor;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Direct2D1 = SharpDX.Direct2D1;
using Direct3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ChatBubble.Client
{
    public partial class Form1 : Form
    {
        public static Direct3D11.Device d3dDevice;
        public static DXGI.Device dxgiDevice;
        public static Direct2D1.Device d2dDevice;

        public static float DpiX;
        public static float DpiY;
        public static float StandardDpiScale = 96;

        public static Size DefaultFormSize = new Size(915, 517);
        public static Size DefaultLoadingSize = new Size(900, 480);

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
            GetDevices();

            Size = DefaultLoadingSize;

            SuspendLayout();
            ActiveForm.AutoScaleDimensions = new System.Drawing.SizeF((float)DpiX, (float)DpiY);
            ActiveForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ResumeLayout();

            TopMost = false;
           
            this.ControlBox = false;
            this.Text = "";

            LoadingPage loadingPage = new LoadingPage();
            loadingPage.ActiveForm = this;

            loadingPage.Size = Size;
            loadingPage.Location = new Point(0, 0);
            this.Controls.Add(loadingPage);
            loadingPage.BringToFront();

            loadingPage.OpenLoadingPage();         
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                DisposeDevices();

                Form1.connectedCheckTimer.Stop();

                NetComponents.BreakBind(false);
            }
            catch
            {

            }
        }

        public static Size GetDpiAdjustedSize(int width, int height)
        {
            if(DpiX == 0 || DpiY == 0)
            {
                DpiX = 120;
                DpiY = 120;
            }

            return new Size((int)(Math.Floor(width * DpiX/StandardDpiScale)), (int)(Math.Floor(height * DpiY/StandardDpiScale)));
        }

        public static Point GetDpiAdjustedPoint(int x, int y)
        {
            if (DpiX == 0 || DpiY == 0)
            {
                DpiX = 120;
                DpiY = 120;
            }

            return new Point((int)(Math.Floor(x * DpiX/StandardDpiScale)), (int)(Math.Floor(y * DpiY/StandardDpiScale)));
        }

        public static int GetDpiAdjustedX(int x)
        {
            if (DpiX == 0)
            {
                DpiX = 120;
            }

            return (int)(Math.Floor(x * DpiX/StandardDpiScale));
        }

        public static int GetDpiAdjustedY(int y)
        {
            if (DpiY == 0)
            {
                DpiY = 120;
            }

            return (int)(Math.Floor(y * DpiY/StandardDpiScale));
        }

        public static float GetDpiAdjustedFloat(float pos)
        {
            if (DpiY == 0)
            {
                DpiY = 120;
            }

            return (pos * DpiY / StandardDpiScale);
        }

        private void GetDevices()
        {
            d3dDevice = new Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            dxgiDevice = d3dDevice.QueryInterface<DXGI.Device>();
            d2dDevice = new Direct2D1.Device(dxgiDevice);

            Graphics graphics = CreateGraphics();
            DpiX = graphics.DpiX;
            DpiY = graphics.DpiY;

            graphics.Dispose();
        }

        private void DisposeDevices()
        {
            d3dDevice.Dispose();
            dxgiDevice.Dispose();
            d2dDevice.Dispose();
        }

        public class MainPage : DoubleBufferPanel
        {
            DXGI.Device dxgiDevice { get; set; }
            Direct2D1.Device d2dDevice { get; set; }

            delegate void TriggerNotificationDelegate(NotificationType type, string content);

            public enum TabType { MainPage, Friends, Dialogues, ActiveDialogue, Search, Settings, LogOut }
            public enum NotificationType { NewFriend, NewMessage }

            static Size mainPageSize { get; set; } = DefaultFormSize;
            static Point mainPageLocation { get; set; } = new Point(0, 0);

            public static List<Panel> TabHistory;

            public static Point tabLocation = new Point(60, 7);
            public static Size tabSize = new Size(840, 463);

            System.Timers.Timer notificationDetectionTimer = new System.Timers.Timer(200);

            public MainPage()
            {
                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                dxgiDevice = Form1.dxgiDevice;
                d2dDevice = Form1.d2dDevice;

                Size = mainPageSize;
                Location = mainPageLocation;
                Dock = DockStyle.Fill;
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

            public void ClearAll(bool ignoreTopMost = false)
            {
                foreach (Control panel in Controls.OfType<Control>())
                {
                    Type panelType = panel.GetType();

                    if (panelType != typeof(MenuButton) && panelType != typeof(PictureBox))
                    {
                        if (!ignoreTopMost || panel != TabHistory.Last())
                        {
                            Controls.Remove(panel);
                            TabHistory.Clear();
                            panel.Dispose();
                        }
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

                foreach (TabType tabType in Enum.GetValues(typeof(TabType)))
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
                if (!NetComponents.receivedMessagesCollection.IsEmpty)
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

            public void OpenNewTabArbitrary(Panel tab, string tabName, string tabArgument = "")
            {
                if (Controls.OfType<Panel>().Count() == 0)
                {
                    TabHistory = new List<Panel>();
                }

                tab.Size = tabSize;
                tab.Location = tabLocation;
                tab.Name = tabName;

                TabHistory.Add(tab);
                Controls.Add(tab);         

                tab.BringToFront();

                SuspendLayout();
                Application.OpenForms[0].PerformAutoScale();
                ResumeLayout();
            }

            public void OpenNewTab(TabType tabType, string tabArgument = "")
            {
                if (Controls.OfType<Panel>().Count() == 0)
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
                            mainTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                            friendsTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                            dialoguesTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

                            TabHistory.Add(dialoguesTab);

                            Controls.Add(dialoguesTab);
                            dialoguesTab.BringToFront();
                            break;
                        }
                    case TabType.ActiveDialogue:
                        {
                            ActiveDialogueTab activeDialogueTab = new ActiveDialogueTab(dxgiDevice, d2dDevice, tabArgument);

                            activeDialogueTab.Size = tabSize;
                            activeDialogueTab.Location = tabLocation;
                            activeDialogueTab.BackgroundImage = null;
                            activeDialogueTab.Name = tabType.ToString();
                            activeDialogueTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                            searchTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                            settingsTab.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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

                SuspendLayout();
                Application.OpenForms[0].PerformAutoScale();
                ResumeLayout();

                if (TabHistory.Count > 1)
                {
                    TabHistory[TabHistory.Count - 2].Enabled = false;
                }
            }

            public void TriggerNotifications(NotificationType notificationType, string notificationContent)
            {
                switch (notificationType)
                {
                    case NotificationType.NewMessage:
                        string[] pendingMessages = notificationContent.Split(new string[] { "msg=" }, StringSplitOptions.RemoveEmptyEntries);
                        int totalMessages = pendingMessages.Length;

                        string[] pendingMessageSubstrings = pendingMessages[pendingMessages.Length - 1].Split(new string[] { "sender=", "time=", "message=" }, StringSplitOptions.RemoveEmptyEntries);
                        //[0] = sender id, [1] - message time, [2] = message text

                        string messageSenderData = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetUserSummaryRequest, "reqid=" + pendingMessageSubstrings[0], true, true);
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

            private class ResultPanel : DoubleBufferPanel
            {
                public enum ResultType { Success, Failure, ServerFailure };
                public enum AppearanceType { LeftBorder, RightBorder };

                private enum MovementState { Open, Closed, Moving };

                Point InitialLocation;

                MovementState CurrentPos;
                AppearanceType AppearType;

                delegate void LocationChangeDelegate(Point newLocation);

                Label informationLabel;

                public ResultPanel(AppearanceType appearancePoint, Size panelSize, int panelTop)
                {
                    AppearType = appearancePoint;
                    CurrentPos = MovementState.Closed;

                    Size = panelSize;
                    Location = new Point(0 - Width, 0);

                    informationLabel = new Label();
                    informationLabel.Font = new Font("Verdana", 12, FontStyle.Regular);
                    informationLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                    informationLabel.Size = new Size(340, 80);
                    informationLabel.TextAlign = ContentAlignment.MiddleCenter;
                    informationLabel.Location = new Point(Width / 2 - informationLabel.Width / 2, Height / 2 - informationLabel.Height / 2 - 30);

                    this.HandleCreated += (o, e) =>
                    {
                        if (appearancePoint == AppearanceType.RightBorder)
                        {
                            InitialLocation = new Point(Parent.Width, GetDpiAdjustedY(panelTop));
                        }
                        else
                        {
                            InitialLocation = new Point(Parent.Left, GetDpiAdjustedY(panelTop));
                        }

                        Location = InitialLocation;

                        Controls.Add(informationLabel);
                    };

                    this.LostFocus += (o, e) =>
                    {
                        if (CurrentPos == MovementState.Open )
                        {
                            RunSlideAnimation();
                        }
                    };
                }

                public void CreateResult(ResultType resultType, string resultMessage = "")
                {
                    if (CurrentPos == MovementState.Open || CurrentPos == MovementState.Moving)
                    {
                        return;
                    }

                    if (resultMessage != "")
                    {
                        informationLabel.Text = resultMessage;
                    }
                    else
                    {
                        switch (resultType)
                        {
                            case ResultType.Success:
                                informationLabel.Text = "Success!";
                                break;
                            case ResultType.Failure:
                                informationLabel.Text = "Something went wrong!";
                                break;
                            case ResultType.ServerFailure:
                                informationLabel.Text = "Server encountered an error.\nPlease contact support.";
                                break;
                        }
                    }

                    RunSlideAnimation();
                }

                public void RunSlideAnimation()
                {
                    System.Timers.Timer animationTimer = new System.Timers.Timer(1);
                    float animationTime = 0;
                    float animationModifier = 0;
                    float animationStep = 0.02f;

                    MovementState previousPosition = MovementState.Closed;

                    int animationDirectionModifier = 1;

                    if (CurrentPos == MovementState.Closed)
                    {
                        if (AppearType == AppearanceType.LeftBorder)
                        {
                            animationDirectionModifier = -1;
                        }
                        else
                        {
                            animationDirectionModifier = 1;                            
                        }

                        previousPosition = MovementState.Closed;
                    }
                    else
                    {
                        if (AppearType == AppearanceType.LeftBorder)
                        {
                            animationDirectionModifier = 1;
                        }
                        else
                        {                          
                            animationDirectionModifier = -1;
                        }

                        previousPosition = MovementState.Open;
                    }

                    LocationChangeDelegate locationChange = new LocationChangeDelegate(x => Location = x);

                    animationTimer.Elapsed += (o, e) =>
                    {
                        if (Disposing || IsDisposed || !IsHandleCreated)
                        {
                            animationTimer.Dispose();
                            return;
                        }

                        if (animationTime < 1)
                        {
                            int newX = 0; ;

                            if (animationTime < 0.5f)
                            {
                                animationModifier = 2 * animationTime * animationTime * Width;

                                newX = (int)(InitialLocation.X - animationModifier * animationDirectionModifier);
                                animationTime += animationStep;
                            }

                            if (animationTime > 0.5f)
                            {
                                animationModifier = (2 * (animationTime - 0.5f) - 2 * (animationTime - 0.5f) * (animationTime - 0.5f) + 0.5f) * Width;

                                newX = (int)(InitialLocation.X - animationModifier * animationDirectionModifier);
                                animationTime += animationStep;
                            }

                            try
                            {
                                Invoke(locationChange, new Point(newX, Location.Y));
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                Invoke(locationChange, new Point(InitialLocation.X - Width * animationDirectionModifier, InitialLocation.Y));
                            }
                            catch { }

                            animationTimer.Dispose();

                            InitialLocation = Location;

                            if (previousPosition == MovementState.Closed)
                            {
                                CurrentPos = MovementState.Open;
                            }
                            else
                            {
                                CurrentPos = MovementState.Closed;
                            }

                            previousPosition = MovementState.Moving;
                        }
                    };

                    CurrentPos = MovementState.Moving;
                    animationTimer.Start();
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
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
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

                    System.Drawing.Bitmap thumbnailImage = new System.Drawing.Bitmap(Properties.Resources.PlaceholderProfilePicture, notificationThumbnail.Size);
                    ImageFactory imageEditor = new ImageFactory();
                    imageEditor.Load(thumbnailImage);
                    imageEditor.Format(new ImageProcessor.Imaging.Formats.PngFormat());
                    imageEditor.RoundedCorners(notificationThumbnail.Height / 2);
                    imageEditor.BackgroundColor(Color.Transparent);
                    imageEditor.Save(profilePictureStream);

                    notificationThumbnail.BackgroundImage = System.Drawing.Image.FromStream(profilePictureStream);
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

                                if ((notificationCounter - 1) % 10 == 1)
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

                    switch (notificationPresent)
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

                bool renderOverride;

                TabType ParentTabType { get; set; }

                public TabHatImage(TabType parentTabType, string tabName = "Unnamed Tab", bool renderReturnButtonBackground = false)
                {
                    Size = tabSize;
                    Location = GetDpiAdjustedPoint(0, 0);
                    ParentTabType = parentTabType;

                    renderOverride = renderReturnButtonBackground;

                    GetRegion();

                    tabNameLabel.Location = GetDpiAdjustedPoint(46, 5);
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
                        bubblePictureBox.BackColor = Color.FromArgb(255, 93, 143, 215);
                        bubblePictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                        Controls.Add(bubblePictureBox);
                    }

                    Controls.Add(tabNameLabel);
                }

                void GetRegion()
                {
                    GraphicsPath = new GraphicsPath();

                    GraphicsPath.StartFigure();
                    GraphicsPath.AddLine(0, 0, this.Width, 0);
                    GraphicsPath.AddLine(this.Width, 0, this.Width, GetDpiAdjustedY(39));
                    GraphicsPath.AddLine(this.Width, GetDpiAdjustedY(39), GetDpiAdjustedX(53), GetDpiAdjustedY(39));
                    GraphicsPath.AddArc(new RectangleF(0, GetDpiAdjustedY(39), GetDpiAdjustedX(91), GetDpiAdjustedY(91)), 270, -90);
                    GraphicsPath.AddLine(0, GetDpiAdjustedY(91), 0, 0);
                    GraphicsPath.CloseFigure();

                    Region = new Region(GraphicsPath);

                    if (ParentTabType == TabType.ActiveDialogue || renderOverride == true)
                    {
                        AuxillaryPath = new GraphicsPath();

                        AuxillaryPath.StartFigure();
                        AuxillaryPath.AddEllipse(new RectangleF(GetDpiAdjustedFloat(0.25f), GetDpiAdjustedFloat(25.25f),
                            GetDpiAdjustedFloat(23.125f), GetDpiAdjustedFloat(23.125f)));
                        AuxillaryPath.CloseFigure();
                    }
                }

                protected override void OnPaint(PaintEventArgs pe)
                {
                    pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    pe.Graphics.FillPath(SolidBrush, GraphicsPath);

                    base.OnPaint(pe);

                    if (ParentTabType == TabType.ActiveDialogue || renderOverride == true)
                    {
                        pe.Graphics.FillPath(WhiteBrush, AuxillaryPath);
                    }
                }
            }

            private class LastTabButton : Button
            {
                GraphicsPath RegionPath;

                public LastTabButton()
                {
                    Size = new Size(22, 22);
                    Location = GetDpiAdjustedPoint(0, 25);
                    FlatAppearance.BorderSize = 0;
                    FlatStyle = FlatStyle.Flat;
                    BackgroundImage = Properties.Resources.returnButtonIdle;
                    BackgroundImageLayout = ImageLayout.Stretch;

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
                    BackgroundImage = Properties.Resources.returnButtonHover;
                }

                void OnMouseLeave(object sender, EventArgs e)
                {
                    BackgroundImage = Properties.Resources.returnButtonIdle;
                }

                void OnMouseDown(object sender, MouseEventArgs e)
                {
                    BackgroundImage = Properties.Resources.returnButtonClick;
                }

                void OnMouseUp(object sender, MouseEventArgs e)
                {
                    BackgroundImage = Properties.Resources.returnButtonIdle;

                    MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                    mainPage.TabClose(TabHistory[TabHistory.Count - 1]);
                }
            }

            private class MainTab : DoubleBufferPanel   //Tab classes encompass Tab-specific controls and functionality
            {                                  //Tab controls are added inside tab constructors 
                ProfileInfoPanel profileInfoPanel;

                public string UserPageID { get; private set; }

                public MainTab(string userID = "")
                {
                    UserPageID = userID;

                    profileInfoPanel = new ProfileInfoPanel();

                    if (UserPageID != "")
                    {
                        profileInfoPanel.userID = UserPageID;
                    }

                    Controls.Add(profileInfoPanel);

                    HorizontalScroll.Maximum = 0;
                    AutoScroll = false;
                    VerticalScroll.Visible = false;
                    AutoScroll = true;                  
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

                    List<Control> PreparedControlsList;

                    public ProfileInfoPanel()
                    {
                        Size = ProfileInfoPanelSize;
                        Location = ProfileInfoPanelLocation;
                        BackgroundImage = Properties.Resources.mainTabBackground;
                        Dock = DockStyle.Fill;

                        this.HandleCreated += (o, e) =>
                        {
                            PrepareProfileInfo();
                            AddProfileControls(PreparedControlsList);
                        };
                    }

                    private const int WM_HSCROLL = 0x114;
                    private const int WM_VSCROLL = 0x115;

                    protected override void WndProc(ref System.Windows.Forms.Message m)
                    {
                        if ((m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                        && (((int)m.WParam & 0xFFFF) == 5))
                        {
                            // Change SB_THUMBTRACK to SB_THUMBPOSITION
                            m.WParam = (IntPtr)(((int)m.WParam & ~0xFFFF) | 4);
                        }
                        base.WndProc(ref m);
                    }

                    [DllImport("user32.dll", SetLastError = true)]
                    private static extern bool LockWindowUpdate(IntPtr hWnd);

                    private void PrepareProfileInfo()
                    {
                        string[] profileInfoSplitstrings = { "id=", "login=", "name=", "status=", "main=", "bubscore=" };
                        string profileInfoString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetUserSummaryRequest, "reqid=" + userID, true, true);

                        if(profileInfoString.Contains("\0"))
                        {
                            profileInfoString = profileInfoString.Substring(0, profileInfoString.IndexOf('\0'));
                        }

                        if (profileInfoString == NetComponents.ConnectionCodes.DatabaseError)
                        {
                            return;
                        }

                        string[] allProfileData = profileInfoString.Split(profileInfoSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                        //[0] = id, [1] = login, [2] = name, [3] = status summary, [4] = main summary, [5] = bubscore

                        for (int i = 0; i < allProfileData.Length; i++)
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

                        System.Drawing.Bitmap thumbnailImage = new System.Drawing.Bitmap(Properties.Resources.PlaceholderProfilePicture, profilePicture.Size);
                        ImageFactory imageEditor = new ImageFactory();
                        imageEditor.Load(thumbnailImage);
                        imageEditor.Format(new ImageProcessor.Imaging.Formats.PngFormat());
                        imageEditor.RoundedCorners(profilePicture.Height / 2);
                        imageEditor.BackgroundColor(Color.Transparent);
                        imageEditor.Save(profilePictureStream);

                        profilePicture.Image = System.Drawing.Image.FromStream(profilePictureStream);
                        profilePicture.BackColor = Color.Transparent;
                        profilePicture.Name = "userthumb_" + allProfileData[0];
                        profilePicture.SizeMode = PictureBoxSizeMode.StretchImage;
                        profilePicture.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                        nameLabel.AutoSize = true;
                        nameLabel.Location = new Point(profilePicture.Width + 10, 6);
                        nameLabel.Font = titleFont;
                        nameLabel.Text = allProfileData[2];
                        nameLabel.ForeColor = Color.White;
                        nameLabel.BackColor = Color.Transparent;
                        nameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                        usernameLabel.AutoSize = true;
                        usernameLabel.Location = new Point(nameLabel.Location.X + 10, nameLabel.Location.Y + nameLabel.Height + 34);
                        usernameLabel.Font = subtitleFont;
                        usernameLabel.ForeColor = Color.Gray;
                        usernameLabel.Text = allProfileData[1];
                        usernameLabel.ForeColor = Color.White;
                        usernameLabel.BackColor = Color.Transparent;
                        usernameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                        bubScoreLabel.Text = allProfileData[5];
                        bubScoreLabel.Width = 100;
                        bubScoreLabel.TextAlign = ContentAlignment.MiddleCenter;
                        bubScoreLabel.Font = scoreFont;
                        bubScoreLabel.Location = new Point(746, 55);
                        bubScoreLabel.ForeColor = Color.White;
                        bubScoreLabel.BackColor = Color.Transparent;
                        bubScoreLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                        statusLabel.Width = tabSize.Width - profilePicture.Width - 62;
                        statusLabel.Height = 25;
                        statusLabel.Location = new Point(usernameLabel.Location.X, usernameLabel.Location.Y + usernameLabel.Height + 21);
                        statusLabel.Font = subtitleFont;
                        statusLabel.Text = allProfileData[3];
                        statusLabel.BackColor = Color.Transparent;
                        statusLabel.Name = "statusLabel";
                        statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                        summaryLabel.Size = new Size(tabSize.Width - profilePicture.Width - 62, 81);
                        summaryLabel.Location = new Point(statusLabel.Location.X, statusLabel.Location.Y + statusLabel.Height + 8);
                        summaryLabel.Font = generalFont;
                        summaryLabel.Text = allProfileData[4];
                        summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
                        summaryLabel.Name = "summaryLabel";
                        summaryLabel.BackColor = Color.Transparent;
                        summaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                        PreparedControlsList = new List<Control>();
                        PreparedControlsList.Add(profilePicture);
                        PreparedControlsList.Add(nameLabel);
                        PreparedControlsList.Add(usernameLabel);
                        PreparedControlsList.Add(bubScoreLabel);
                        PreparedControlsList.Add(statusLabel);
                        PreparedControlsList.Add(summaryLabel);

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
                            editDescriptionButton.Location = new Point(tabSize.Width - editDescriptionButton.Width - 8, statusLabel.Location.Y - 3);
                            editDescriptionButton.FlatStyle = FlatStyle.Flat;
                            editDescriptionButton.BackgroundImage = Properties.Resources.editDescriptionButtonIdle;
                            editDescriptionButton.Name = "buttonEdit";
                            editDescriptionButton.FlatAppearance.BorderSize = 0;
                            editDescriptionButton.BackgroundImageLayout = ImageLayout.Stretch;
                            editDescriptionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                            editDescriptionButtonConfirm.Size = new Size(24, 24);
                            editDescriptionButtonConfirm.Location = new Point(editDescriptionButton.Location.X + 4, statusLabel.Location.Y + editDescriptionButton.Height + 1);
                            editDescriptionButtonConfirm.FlatStyle = FlatStyle.Flat;
                            editDescriptionButtonConfirm.BackgroundImage = Properties.Resources.confirmEditButtonIdle;
                            editDescriptionButtonConfirm.Name = "buttonConfirm";
                            editDescriptionButtonConfirm.FlatAppearance.BorderSize = 0;
                            editDescriptionButtonConfirm.Visible = false;
                            editDescriptionButtonConfirm.Enabled = false;
                            editDescriptionButtonConfirm.BackgroundImageLayout = ImageLayout.Stretch;
                            editDescriptionButtonConfirm.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                            editDescriptionButtonCancel.Size = editDescriptionButtonConfirm.Size;
                            editDescriptionButtonCancel.Location = new Point(editDescriptionButton.Location.X + 4, editDescriptionButton.Location.Y + 4);
                            editDescriptionButtonCancel.FlatStyle = FlatStyle.Flat;
                            editDescriptionButtonCancel.BackgroundImage = Properties.Resources.cancelEditButtonIdle;
                            editDescriptionButtonCancel.Name = "buttonCancel";
                            editDescriptionButtonCancel.FlatAppearance.BorderSize = 0;
                            editDescriptionButtonCancel.Visible = false;
                            editDescriptionButtonCancel.Enabled = false;
                            editDescriptionButtonCancel.BackgroundImageLayout = ImageLayout.Stretch;
                            editDescriptionButtonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                            PreparedControlsList.Add(editDescriptionButton);
                            PreparedControlsList.Add(editDescriptionButtonConfirm);
                            PreparedControlsList.Add(editDescriptionButtonCancel);
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
                            returnToLastPageButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                            returnToLastPageButton.BringToFront();

                            sendMessageButton.Size = new Size(40, 40);
                            sendMessageButton.Location = new Point(profilePicture.Location.X, profilePicture.Location.Y + profilePicture.Height - sendMessageButton.Height);
                            sendMessageButton.BackColor = Color.Transparent;
                            sendMessageButton.Text = "#";
                            sendMessageButton.FlatStyle = FlatStyle.Flat;
                            sendMessageButton.FlatAppearance.BorderSize = 0;
                            sendMessageButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                            sendMessageButton.BringToFront();

                            addRemoveFriendButton.Size = sendMessageButton.Size;
                            addRemoveFriendButton.Location = new Point(sendMessageButton.Location.X + profilePicture.Width - addRemoveFriendButton.Width, sendMessageButton.Location.Y);
                            addRemoveFriendButton.BackColor = Color.Transparent;
                            addRemoveFriendButton.Text = "+";
                            addRemoveFriendButton.FlatStyle = FlatStyle.Flat;
                            addRemoveFriendButton.FlatAppearance.BorderSize = 0;
                            addRemoveFriendButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                            addRemoveFriendButton.BringToFront();

                            PreparedControlsList.Add(returnToLastPageButton);
                            PreparedControlsList.Add(sendMessageButton);
                            PreparedControlsList.Add(addRemoveFriendButton);
                        }
                    }

                    private void AddProfileControls(List<Control> controlList)
                    {
                        Controls.AddRange(controlList.ToArray());

                        foreach(Control control in Controls)
                        {
                            if(control.GetType() == typeof(Button))
                            {
                                control.BringToFront();
                            }
                        }
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
                            if (label.Name == "statusLabel")
                            {
                                statusTextBox.Location = new Point(label.Location.X, label.Location.Y - 3);
                                statusTextBox.Size = label.Size;
                                statusTextBox.Text = label.Text;
                                statusTextBox.Font = label.Font;
                                statusTextBox.ForeColor = label.ForeColor;
                                statusTextBox.MaxLength = 50;
                                statusTextBox.Name = "statusTextBox";
                            }
                            if (label.Name == "summaryLabel")
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

                        for (int i = 0; i < controlButtons.Length; i++)
                        {
                            if (controlButtons[i].Name == "buttonEdit")
                            {
                                controlButtons[i].Visible = false;
                                controlButtons[i].Enabled = false;

                                controlButtons[i].MouseUp -= EditProfileDescription;
                            }
                            if (controlButtons[i].Name == "buttonCancel")
                            {
                                controlButtons[i].Visible = true;
                                controlButtons[i].Enabled = true;

                                controlButtons[i].MouseUp += CancelEdit;
                            }
                            if (controlButtons[i].Name == "buttonConfirm")
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
                        NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.EditUserSummaryRequest, descriptionChangeRequest, true, true);

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
            private class FriendsTab : DoubleBufferPanel
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

                    List<List<Control>> PreparedControlsList;

                    public FriendPanel()
                    {
                        Size = FriendPanelSize;
                        Location = FriendPanelLocation;
                        PreparedControlsList = new List<List<Control>>();

                        GetFriendList();
                        PrepareFriendList();

                        this.HandleCreated += (o, e) => AddControls(PreparedControlsList);
                    }

                    void GetFriendList()
                    {
                        string friendListResultString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetFriendListRequest, "", true, true);

                        if(friendListResultString.Contains("\0"))
                        {
                            friendListResultString = friendListResultString.Substring(0, friendListResultString.IndexOf('\0'));
                        }

                        if (friendListResultString == NetComponents.ConnectionCodes.DatabaseError)      //TO DO: Output an error message here
                        {
                            allFriendsDataByPage = null;
                            return;
                        }

                        string[] allFriendsData = friendListResultString.Split(new string[] { "user=" }, StringSplitOptions.RemoveEmptyEntries);
                        int isLastPageFilled = 0;

                        if (allFriendsData.Length % 12 > 0)
                        {
                            isLastPageFilled = 1;
                        }

                        allFriendsDataByPage = new string[12, allFriendsData.Length / 12 + isLastPageFilled];

                        int currentFriend = 0;
                        for (int pageNum = 0; pageNum < allFriendsDataByPage.GetLength(1); pageNum++)
                        {
                            for (int friendNum = 0; friendNum < 12; friendNum++)
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

                    void PrepareFriendList()
                    {
                        string[] friendListSplitstrings = { "id=", "login=", "name=" };

                        Button previousPageButton = new Button();
                        Button nextPageButton = new Button();

                        PreparedControlsList.Clear();

                        if (allFriendsDataByPage == null || allFriendsDataByPage.Length == 0)
                        {
                            return;
                        }

                        if (pageNumber + 1 > allFriendsDataByPage.GetLength(1))
                        {
                            pageNumber = allFriendsDataByPage.GetLength(1) - 1;
                        }

                        int friendBoxRow = 0;
                        int friendBoxColumn = 0;
                        for (int i = 0; i < 12; i++)
                        {
                            System.IO.MemoryStream thumbnailStream = new System.IO.MemoryStream();

                            Button removeFriendButton = new Button();
                            PictureBox friendThumbnail = new PictureBox();
                            Label friendUsername = new Label();
                            Label friendName = new Label();
                            WaterMarkTextBox friendSearchQuery = new WaterMarkTextBox();
                            List<Control> friendBoxCollection = new List<Control>();

                            if (allFriendsDataByPage[i, pageNumber] == null)
                            {
                                break;
                            }

                            string[] friendData = allFriendsDataByPage[i, pageNumber].Split(friendListSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                            //[0] = id, [1] = login, [1] = name                                                    
                            PreparedControlsList.Add(friendBoxCollection);
                            PreparedControlsList[i].Add(new DoubleBufferPanel());
                            PreparedControlsList[i][0].Size = new Size((FriendPanelSize.Width - 6) / 6, (FriendPanelSize.Height - 30) / 2);
                            PreparedControlsList[i][0].Name = "friend_" + friendData[0];

                            removeFriendButton.Size = new Size(24, 24);
                            removeFriendButton.Location = new Point(PreparedControlsList[i][0].Size.Width - removeFriendButton.Width, 0);
                            removeFriendButton.BackgroundImage = Properties.Resources.removeFriendButton;
                            removeFriendButton.FlatStyle = FlatStyle.Flat;
                            removeFriendButton.FlatAppearance.BorderSize = 0;
                            PreparedControlsList[i].Add(removeFriendButton);

                            friendThumbnail.Size = new Size(PreparedControlsList[i][0].Width - 18, PreparedControlsList[i][0].Width - 18);
                            friendThumbnail.Location = new Point(0, 0);
                            friendThumbnail.Name = "userthumb_" + friendData[0];
                            PreparedControlsList[i].Add(friendThumbnail);

                            System.Drawing.Bitmap thumbnailImage = new System.Drawing.Bitmap(Properties.Resources.PlaceholderProfilePicture, friendThumbnail.Size);
                            ImageFactory imageEditor = new ImageFactory();
                            imageEditor.Load(thumbnailImage);
                            imageEditor.RoundedCorners(friendThumbnail.Height / 2);
                            imageEditor.BackgroundColor(Color.White);
                            imageEditor.Save(thumbnailStream);

                            friendThumbnail.BackgroundImage = System.Drawing.Image.FromStream(thumbnailStream);

                            friendName.AutoSize = true;
                            friendName.Font = titleFont;
                            friendName.Location = new Point(3, friendThumbnail.Height + 3);
                            friendName.Text = friendData[2];
                            PreparedControlsList[i].Add(friendName);

                            friendUsername.AutoSize = true;
                            friendUsername.Font = subtitleFont;
                            friendUsername.ForeColor = Color.Gray;
                            friendUsername.Location = new Point(6, friendName.Height + friendName.Location.Y + 3);
                            friendUsername.Text = friendData[1];
                            PreparedControlsList[i].Add(friendUsername);

                            if (friendBoxCollection.Count >= 1)
                            {
                                if (friendBoxColumn == 6)
                                {
                                    friendBoxRow++;
                                    friendBoxColumn = 0;
                                }
                                if (friendBoxColumn < 7)
                                {
                                    PreparedControlsList[i][0].Location = new Point(6 + PreparedControlsList[i][0].Size.Width * friendBoxColumn, PreparedControlsList[i][0].Size.Height * friendBoxRow + 30);
                                    friendBoxColumn++;
                                }
                            }

                            friendThumbnail.Click += new EventHandler(ShowUserProfile);

                            removeFriendButton.MouseEnter += new EventHandler(RemoveFriendMouseEnter);
                            removeFriendButton.MouseLeave += new EventHandler(RemoveFriendMouseLeave);
                            removeFriendButton.MouseUp += new MouseEventHandler(RemoveFriendMouseUp);

                        }

                        List<Control> auxControlsList = new List<Control>();

                        if (pageNumber + 1 > 1)
                        {
                            previousPageButton.Size = new Size(30, 30);
                            previousPageButton.Location = new Point(FriendPanelSize.Width - previousPageButton.Width * 2 - 8, 0);
                            previousPageButton.Text = "<";

                            //Invoke(previousPageAddControlDelegate, previousPageButton);
                            auxControlsList.Add(previousPageButton);
                            previousPageButton.Click += new EventHandler(ShowPreviousPage);
                        }
                        if (allFriendsDataByPage.GetLength(1) != pageNumber + 1)
                        {
                            nextPageButton.Size = new Size(30, 30);
                            nextPageButton.Location = new Point(FriendPanelSize.Width - nextPageButton.Width - 7, 0);
                            nextPageButton.Text = ">";

                            //Invoke(nextPageAddControlDelegate, nextPageButton);
                            auxControlsList.Add(nextPageButton);
                            nextPageButton.Click += new EventHandler(ShowNextPage);
                        }

                        PreparedControlsList.Add(auxControlsList);
                    }

                    void AddControls(List<List<Control>> controlCollection)
                    {
                        foreach(List<Control> controls in controlCollection)
                        {                     
                            if (controls.Count > 0 && controls[0].GetType() == typeof(DoubleBufferPanel))
                            {
                                Controls.Add(controls[0]);
                                controls[0].Controls.AddRange(controls.Skip(1).ToArray());
                            }
                            else
                            {
                                Controls.AddRange(controls.ToArray());
                            }
                        }
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

                        NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.RemoveFriendRequest, "fid=" + requestID, true, true);

                        button.Parent.Parent.Controls.Clear();

                        GetFriendList();
                        PrepareFriendList();
                        AddControls(PreparedControlsList);
                    }

                    void ShowNextPage(object sender, EventArgs eventArgs)
                    {
                        pageNumber++;
                        Controls.Clear();
                        PrepareFriendList();
                        AddControls(PreparedControlsList);
                    }

                    void ShowPreviousPage(object sender, EventArgs eventArgs)
                    {
                        pageNumber--;
                        Controls.Clear();
                        PrepareFriendList();
                        AddControls(PreparedControlsList);
                    }
                }
            }
            private class DialoguesTab : DoubleBufferPanel
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

                private class DialogueListPanel : DoubleBufferPanel
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
                        Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                            string senderData = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetUserSummaryRequest, "reqid=" + senderID, true, true);
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

                        for (int i = 0; i < currentDialoguesList.Count; i++)
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
                            System.Drawing.Bitmap thumbnailImage = new System.Drawing.Bitmap(Properties.Resources.PlaceholderProfilePicture,
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

                            dialogueThumbnail.Image = System.Drawing.Image.FromStream(thumbnailStream);

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
                            if (dialogueFilenameArray[i] == "chatid=" + currentChatID)
                            {
                                fileIO.RemoveFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + dialogueFilenameArray[i] + ".txt");
                                currentDialoguesList.RemoveAt(i);
                            }
                        }

                        GetCurrentDialogues(Parent, eventArgs);
                    }
                }

            }

            private class ActiveDialogueTab : DoubleBufferPanel
            {
                TabHatImage tabHatImage;
                ChatInputPanel chatInputPanel;
                ChatMessagesPanel chatMessagesPanel;
                LastTabButton lastTabButton;

                static int chatInputPanelHeight { get; set; } = 39;

                static string ChatID { get; set; }

                public ActiveDialogueTab(DXGI.Device dxgid, Direct2D1.Device d2d1d, string id)
                {
                    Name = "ActiveDialogue";

                    ChatID = id;
                    string recepientData = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetUserSummaryRequest, "reqid=" + ChatID, true, true);
                    string chatName = recepientData.Split(new string[] {
                        "id=", "login=", "name=", "status=", "main=", "bubscore=" }, StringSplitOptions.RemoveEmptyEntries)[2];

                    chatMessagesPanel = new ChatMessagesPanel(dxgid, d2d1d);
                    tabHatImage = new TabHatImage(TabType.ActiveDialogue, chatName);
                    lastTabButton = new LastTabButton();

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

                private class ChatMessagesPanel : DoubleBufferPanel
                {
                    DXGI.Device dxgiDevice;
                    Direct2D1.Device d2dDevice;
                    Direct2D1.DeviceContext deviceContext;
                    SharpDX.DirectWrite.Factory dxWriteFactory;
                    SharpDX.DirectWrite.TextFormat dxWriteTextFormatMain;
                    SharpDX.DirectWrite.TextFormat dxWriteTextFormatTimestamp;

                    DXGI.SwapChain swapChain;
                    SolidColorBrush blueSolidBrush;
                    SolidColorBrush blackSolidBrush;
                    SolidColorBrush whiteSolidBrush;
                    SolidColorBrush graySolidBrush;

                    List<MessageBox> messageBoxList;

                    bool isRenderingAnimation = false;
                    bool areAllMessagesPrepared;

                    long messagesPreparedTimeMs;

                    static Object renderLock = new Object();

                    enum MessageType { Read, Unread, Self };
                    enum DecorTier { Small, Medium, Big };

                    private const int WM_HSCROLL = 0x114;
                    private const int WM_VSCROLL = 0x115;

                    protected override void WndProc(ref System.Windows.Forms.Message m)
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

                    public ChatMessagesPanel(DXGI.Device dxgid, Direct2D1.Device d2d1d)
                    {
                        dxgiDevice = dxgid;
                        d2dDevice = d2d1d;

                        messageBoxList = new List<MessageBox>();
                        // messageDecorBoxList = new List<MessageDecorBox>();

                        Location = new Point(0, 39);
                        Size = new Size(tabSize.Width + 15, tabSize.Height - chatInputPanelHeight - 39);

                        HorizontalScroll.Maximum = 0;
                        AutoScroll = false;
                        VerticalScroll.Visible = true;
                        AutoScrollMargin = new Size(0, 9);
                        AutoScroll = true;

                        this.HandleCreated += new EventHandler(InitializeRendering);
                        this.HandleCreated += new EventHandler(GetMessages);
                        this.MouseWheel += new MouseEventHandler(OnScroll);
                    }

                    void InitializeRendering(object sender, EventArgs eventArgs)
                    {
                        SwapChainDescription chainDescription = new SwapChainDescription()
                        {
                            BufferCount = 2,
                            IsWindowed = true,
                            ModeDescription = new ModeDescription(Width, Height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                            SwapEffect = SwapEffect.FlipSequential,
                            OutputHandle = this.Handle,
                            Flags = SwapChainFlags.GdiCompatible,
                            Usage = Usage.RenderTargetOutput,
                            SampleDescription = new SampleDescription(1, 0),
                        };

                        swapChain = new SwapChain(new DXGI.Factory2(), dxgiDevice, chainDescription);
                        Surface surface = swapChain.GetBackBuffer<Surface>(0);

                        deviceContext = new Direct2D1.DeviceContext(d2dDevice, DeviceContextOptions.EnableMultithreadedOptimizations);

                        Direct2D1.Bitmap1 bitmapTarget = new Direct2D1.Bitmap1(deviceContext, surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Direct2D1.AlphaMode.Premultiplied),
                            120, 120, BitmapOptions.Target | BitmapOptions.CannotDraw | BitmapOptions.GdiCompatible));
                        deviceContext.Target = bitmapTarget;

                        deviceContext.DotsPerInch = new SharpDX.Size2F(Form1.DpiX, Form1.DpiY);
                        deviceContext.AntialiasMode = AntialiasMode.PerPrimitive;
                        deviceContext.TextAntialiasMode = TextAntialiasMode.Cleartype;
                        deviceContext.UnitMode = UnitMode.Pixels;
                        deviceContext.StrokeWidth = 3;

                        SharpDX.Mathematics.Interop.RawColor4 rawColorBlue = new SharpDX.Mathematics.Interop.RawColor4(0.3647f, 0.5608f, 0.8510f, 1);

                        blueSolidBrush = new SolidColorBrush(deviceContext, rawColorBlue, new BrushProperties() { Opacity = 1 });
                        blackSolidBrush = new SolidColorBrush(deviceContext, SharpDX.Color.Black, new BrushProperties() { Opacity = 1 });
                        whiteSolidBrush = new SolidColorBrush(deviceContext, SharpDX.Color.White, new BrushProperties() { Opacity = 1 });
                        graySolidBrush = new SolidColorBrush(deviceContext, SharpDX.Color.Gray, new BrushProperties() { Opacity = 1 });

                        dxWriteFactory = new SharpDX.DirectWrite.Factory();
                        SharpDX.DirectWrite.RenderingParams renderingParams = new SharpDX.DirectWrite.RenderingParams(dxWriteFactory, 150, 1, 1, SharpDX.DirectWrite.PixelGeometry.Flat, SharpDX.DirectWrite.RenderingMode.CleartypeGdiClassic);
                        deviceContext.TextRenderingParams = renderingParams;

                        dxWriteTextFormatMain = new SharpDX.DirectWrite.TextFormat(dxWriteFactory, "Verdana",
                            SharpDX.DirectWrite.FontWeight.UltraLight, SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, 16)
                        {
                            TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
                            WordWrapping = SharpDX.DirectWrite.WordWrapping.WholeWord,
                        };

                        dxWriteTextFormatTimestamp = new SharpDX.DirectWrite.TextFormat(dxWriteFactory, "Verdana", SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Italic, 10)
                        {
                            TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
                            WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap,
                        };
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

                            messageBoxList.Insert(0, new MessageBox(messageData[0], messageData[1], messageData[2], dxWriteFactory, dxWriteTextFormatMain));

                            if (messageBoxList[0].MessageType == MessageType.Self)
                            {
                                messageBoxList[0].Left = tabSize.Width - messageBoxList[0].Width - 60;
                            }
                            if (messageBoxList[0].MessageType == MessageType.Unread)
                            {
                                messageBoxList[0].Left = 45;
                            }
                            if (messageBoxList[0].MessageType == MessageType.Read)
                            {
                                messageBoxList[0].Left = 9;
                            }
                        }

                        PrepareMessages();
                        RenderMessages();
                    }

                    int MessageSortByTime(string message1, string message2)
                    {
                        string[] messageSubstrings1 = message1.Split(new string[] { "time=", "content=", "status=" }, StringSplitOptions.RemoveEmptyEntries);
                        string[] messageSubstrings2 = message2.Split(new string[] { "time=", "content=", "status=" }, StringSplitOptions.RemoveEmptyEntries);

                        DateTime dateTime1 = DateTime.Parse(messageSubstrings1[0]).ToUniversalTime();
                        DateTime dateTime2 = DateTime.Parse(messageSubstrings2[0]).ToUniversalTime();

                        return (dateTime1.CompareTo(dateTime2));
                    }

                    void PrepareMessages()
                    {
                        EventHandler messageStatusChanged = new EventHandler(OnMessageStatusChanged);

                        areAllMessagesPrepared = false;
                        int totalMessagesHeight = 0;

                        Controls.Clear();

                        deviceContext.BeginDraw();
                        deviceContext.Clear(SharpDX.Color.White);

                        foreach (MessageBox message in messageBoxList)
                        {
                            totalMessagesHeight += message.Height + 6;
                        }

                        for (int i = 0; i < messageBoxList.Count; i++)
                        {
                            if (i == 0)
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

                            Controls.Add(messageBoxList[i]);

                            if (messageBoxList.Count() > 0)
                            {
                                ScrollControlIntoView(messageBoxList[0]);
                            }

                            if (messageBoxList[i].Bottom >= -50 && messageBoxList[i].Top <= tabSize.Height + 50)
                            {
                                DrawMessage(messageBoxList[i]);
                            }

                            messageBoxList[i].StatusChanged += messageStatusChanged;
                        }

                        deviceContext.EndDraw();
                        areAllMessagesPrepared = true;
                        messagesPreparedTimeMs = DateTime.Now.Millisecond;
                    }

                    void DrawMessage(MessageBox messageBox)
                    {
                        //---------------Drawing Message-----------------

                        RoundedRectangle roundedRectangle = new RoundedRectangle()
                        {
                            Rect = new SharpDX.Mathematics.Interop.RawRectangleF(messageBox.Left, messageBox.Top, messageBox.Right, messageBox.Bottom),
                            RadiusX = 12.5f,
                            RadiusY = 12.5f,
                        };

                        SharpDX.Mathematics.Interop.RawRectangleF contentRectangle =
                            new SharpDX.Mathematics.Interop.RawRectangleF(messageBox.Left + 6,
                            messageBox.Top + 8, messageBox.Right - 6, messageBox.Bottom - 8);

                        if (messageBox.MessageType == MessageType.Self)
                        {
                            deviceContext.FillRoundedRectangle(roundedRectangle, blueSolidBrush);

                            deviceContext.DrawTextLayout(new SharpDX.Mathematics.Interop.RawVector2(messageBox.Left + 8, messageBox.Top + 8),
                            messageBox.TextLayout, whiteSolidBrush, DrawTextOptions.Clip);
                        }
                        if (messageBox.MessageType == MessageType.Unread || messageBox.MessageType == MessageType.Read)
                        {
                            deviceContext.DrawRoundedRectangle(roundedRectangle, blueSolidBrush, 4);

                            deviceContext.DrawTextLayout(new SharpDX.Mathematics.Interop.RawVector2(messageBox.Left + 8, messageBox.Top + 8),
                            messageBox.TextLayout, blackSolidBrush, DrawTextOptions.Clip);
                        }

                        //---------------Drawing Timestamp----------------

                        deviceContext.DrawText(messageBox.LocalMessageTime, dxWriteTextFormatTimestamp,
                            new SharpDX.Mathematics.Interop.RawRectangleF(tabSize.Width - 50, messageBox.Top + messageBox.Height / 2 - 5, tabSize.Width, messageBox.Top + messageBox.Height / 2 + 5), graySolidBrush);


                        //---------------Drawing Decor-----------------

                        if (messageBox.MessageType == MessageType.Unread)
                        {
                            Ellipse ellipse1 = new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(9, messageBox.Top + 18), 5, 5);   //Fill both
                            Ellipse ellipse2 = new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(27, messageBox.Top + 17), 10, 10);

                            if (messageBox.DecorTier != DecorTier.Small)
                            {
                                Ellipse ellipseMed1 = new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(24, messageBox.Top + 30), 12, 12);
                                Ellipse ellipseMed2 = new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(27, messageBox.Top + 17), 11, 11);

                                deviceContext.DrawEllipse(ellipseMed1, blueSolidBrush, 2);
                                deviceContext.DrawEllipse(ellipseMed2, whiteSolidBrush, 2);

                                if (messageBox.DecorTier == DecorTier.Big)
                                {
                                    Ellipse ellipseBig1 = new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(35.5f, messageBox.Top + 49.5f), 4.5f, 4.5f);

                                    deviceContext.FillEllipse(ellipseBig1, blueSolidBrush);
                                }
                            }

                            deviceContext.FillEllipse(ellipse1, blueSolidBrush);
                            deviceContext.FillEllipse(ellipse2, blueSolidBrush);
                        }
                    }

                    void RenderMessages()
                    {
                        lock (renderLock)
                        {
                            deviceContext.BeginDraw();
                            deviceContext.Clear(SharpDX.Color.White);

                            foreach (MessageBox message in messageBoxList)
                            {
                                if (message.Bottom >= -50 && message.Top <= tabSize.Height + 50)
                                {
                                    DrawMessage(message);
                                }
                            }

                            deviceContext.EndDraw();

                            swapChain.Present(0, PresentFlags.None);
                        }
                    }


                    void OnScroll(object sender, MouseEventArgs eventArgs)
                    {
                        DoubleBufferPanel panel = (DoubleBufferPanel)sender;

                        if (panel.VerticalScroll.Value >= panel.VerticalScroll.Minimum && panel.VerticalScroll.Value <= panel.VerticalScroll.Maximum && !isRenderingAnimation)
                        {
                            RenderMessages();
                        }
                    }

                    void OnMessageStatusChanged(object sender, EventArgs eventArgs)
                    {
                        lock (renderLock)
                        {
                            if (!isRenderingAnimation)
                            {
                                isRenderingAnimation = true;

                                int animationTicks = 0;

                                System.Timers.Timer animationTimer = new System.Timers.Timer(1);
                                animationTimer.Elapsed += new ElapsedEventHandler((object s, ElapsedEventArgs elAr) =>
                                {
                                    RenderMessages();
                                    animationTicks++;

                                    if (animationTicks > 12)
                                    {
                                        animationTimer.Stop();
                                        animationTimer.Dispose();

                                        isRenderingAnimation = false;
                                    }

                                });

                                animationTimer.Start();
                            }
                        }
                    }

                    private class MessageBox : DoubleBufferPanel
                    {
                        delegate void MessageSwipeDelegate();

                        public event EventHandler StatusChanged;

                        public SharpDX.DirectWrite.TextLayout TextLayout { get; set; }

                        public string MessageContent { get; set; }
                        public Size MessageContentSize { get; set; }

                        public MessageType MessageType { get; set; }

                        public DecorTier DecorTier { get; set; }

                        public DateTime UniversalMessageTime { get; set; }
                        public string LocalMessageTime { get; set; }

                        public MessageBox(string messageTime, string messageStatus, string messageContent, SharpDX.DirectWrite.Factory dxwFactory, SharpDX.DirectWrite.TextFormat dxwFormat)
                        {
                            BackColor = Color.Wheat;
                            Region = new Region(new Rectangle(0, 0, 0, 0));

                            MessageContent = ContentWordWrap(messageContent);
                            Size messageContentSize = TextRenderer.MeasureText(MessageContent, new Font("Verdana", 10, FontStyle.Regular));

                            TextLayout = new SharpDX.DirectWrite.TextLayout(dxwFactory,
                            MessageContent, dxwFormat, 500, 500)
                            {
                                WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap,
                            };

                            UniversalMessageTime = DateTime.Parse(messageTime);
                            LocalMessageTime = UniversalMessageTime.ToLocalTime().ToShortTimeString();

                            switch (messageStatus)
                            {
                                case ("unread"):
                                    MessageType = MessageType.Unread;
                                    break;
                                case ("read"):
                                    MessageType = MessageType.Read;
                                    break;
                                case ("sent"):
                                    MessageType = MessageType.Self;
                                    break;
                            }

                            Size = new Size((int)TextLayout.Metrics.Width + 16, (int)TextLayout.Metrics.Height + 16);

                            TextLayout.MaxWidth = Size.Width;
                            TextLayout.MaxHeight = Size.Height;

                            if (MessageType == MessageType.Unread)
                            {
                                System.Timers.Timer unreadDisplayWaitTimer = new System.Timers.Timer(900);
                                unreadDisplayWaitTimer.Elapsed += new ElapsedEventHandler(MakeMessageRead);
                                unreadDisplayWaitTimer.Start();
                            }
                        }

                        void MakeMessageRead(object sender, ElapsedEventArgs eventArgs)
                        {
                            System.Timers.Timer originalTimer = (System.Timers.Timer)sender;
                            originalTimer.Stop();
                            originalTimer.Dispose();

                            FileIOStreamer fileIO = new FileIOStreamer();

                            fileIO.SwapFileEntry(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + ChatID + ".txt",
                                UniversalMessageTime.ToString("dddd, dd MMMM yyyy HH: mm:ss") + Environment.NewLine + "status=", "unread", "read", false, true);

                            MessageType = MessageType.Read;

                            ChatMessagesPanel parentMessagesPanel = (ChatMessagesPanel)Parent;

                            while (true)
                            {
                                if (parentMessagesPanel.areAllMessagesPrepared && DateTime.Now.Millisecond == parentMessagesPanel.messagesPreparedTimeMs + 100)
                                {
                                    System.Timers.Timer animationTimer = new System.Timers.Timer(1);

                                    int animationTicks = 0;

                                    animationTimer.Elapsed += new ElapsedEventHandler((object s, ElapsedEventArgs elAr) =>
                                    {
                                        animationTicks++;

                                        MessageSwipeDelegate messageSwipe = new MessageSwipeDelegate(ChangeMessageLocation);
                                        Invoke(messageSwipe);

                                        if (animationTicks >= 12)
                                        {
                                            animationTimer.Stop();
                                            animationTimer.Dispose();
                                        }
                                    });

                                    animationTimer.Start();

                                    OnStatusChanged(EventArgs.Empty);
                                    break;
                                }
                            }
                        }

                        protected virtual void OnStatusChanged(EventArgs e)
                        {
                            StatusChanged.Invoke(this, e);
                        }

                        void ChangeMessageLocation()
                        {
                            Left -= 3;
                        }

                        string ContentWordWrap(string content)
                        {
                            int lineLength;

                            if (content.Length < 150)
                            {
                                lineLength = 30;
                                DecorTier = DecorTier.Small;
                            }
                            else if (content.Length < 300)
                            {
                                lineLength = 40;
                                DecorTier = DecorTier.Medium;
                            }
                            else
                            {
                                lineLength = 55;
                                DecorTier = DecorTier.Big;
                            }

                            for (int i = 0; i < content.Length; i++)
                            {
                                int indexOfPreviousLineChange = content.LastIndexOf("\n");

                                if (indexOfPreviousLineChange == i - lineLength)
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
                        Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

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
                        if (e.KeyCode == Keys.Enter)
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
                            pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
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
                            Location = new Point(779, 403);
                            Size = new Size(70, 70);
                            Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
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

            private class SearchTab : DoubleBufferPanel
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

                            string searchResultString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.SearchRequest, searchQueryString, true);

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

                private class SearchResultsPanel : DoubleBufferPanel
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
                        Anchor = AnchorStyles.Left | AnchorStyles.Top;

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

                            if(searchResultSubstrings[0] == NetComponents.ConnectionCodes.ConnectionFailure)
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
                            searchResultBoxesList[i].Anchor = (AnchorStyles.Left | AnchorStyles.Right);

                            searchResultThumbnail.Height = searchResultBoxesList[i].Height - 6;
                            searchResultThumbnail.Width = searchResultThumbnail.Height;
                            searchResultThumbnail.Name = "userthumb_" + searchResultSubstrings[0];
                            searchResultThumbnail.Location = new Point(0, 0);
                            System.Drawing.Bitmap thumbnailImage = new System.Drawing.Bitmap(Properties.Resources.PlaceholderProfilePicture,
                                                            searchResultThumbnail.Height, searchResultThumbnail.Width);
                            searchResultThumbnail.Anchor = AnchorStyles.Left | AnchorStyles.Top;

                            ImageFactory imageEditor = new ImageFactory();
                            imageEditor.Load(thumbnailImage);
                            imageEditor.RoundedCorners(searchResultThumbnail.Height / 2);
                            imageEditor.BackgroundColor(Color.White);
                            imageEditor.Save(thumbnailStream);

                            searchResultTitleLabel.AutoSize = true;
                            searchResultTitleLabel.Location = new Point(searchResultThumbnail.Location.X + searchResultThumbnail.Width, searchResultThumbnail.Location.Y + 12);
                            searchResultTitleLabel.Font = titleFont;
                            searchResultTitleLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

                            searchResultSubtitleLabel.AutoSize = true;
                            searchResultSubtitleLabel.Location = new Point(searchResultTitleLabel.Location.X + 2, searchResultTitleLabel.Height + searchResultTitleLabel.Location.Y + 3);
                            searchResultSubtitleLabel.Font = subtitleFont;
                            searchResultSubtitleLabel.ForeColor = Color.Gray;
                            searchResultSubtitleLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

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
                                searchResultSendMessageButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;

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
                                searchResultSendFriendRequestButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
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

                            searchResultThumbnail.Image = System.Drawing.Image.FromStream(thumbnailStream);
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

                        NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.AddFriendRequest, "addid=" + requestID, true, true);
                    }
                }
            }
            private class SettingsTab : DoubleBufferPanel
            {
                TabHatImage tabHatImage;
                TitlePanel titlePanel;
                SettingsButtonLayout settingsButtonLayout;

                public SettingsTab()
                {
                    Name = "Settings";

                    tabHatImage = new TabHatImage(TabType.Search, Name);
                    titlePanel = new TitlePanel();
                    settingsButtonLayout = new SettingsButtonLayout();

                    Controls.Add(tabHatImage);
                    Controls.Add(titlePanel);
                    Controls.Add(settingsButtonLayout);
                }

                private class SettingsButtonLayout : DoubleBufferPanel
                {
                    enum ButtonFunction { SetName, SetInfo, SetPrivacy, SetBlacklist, SetPassword, SetEmail, SetPhone, SetSecurityQuestions, SetNotifications }
                    enum ButtonType { ProfileSettings, SecuritySettings, NotificationSettings }

                    List<TypeSeparator> TypeSeparators = new List<TypeSeparator>();
                    List<SettingsButton> ProfileSettingsButtons = new List<SettingsButton>();
                    List<SettingsButton> SecuritySettingsButtons = new List<SettingsButton>();
                    List<SettingsButton> NotificationSettingsButtons = new List<SettingsButton>();

                    public static Pen GrayPenWide = new Pen(Color.FromArgb(255, 232, 241, 255), 2);
                    public static Pen GrayPenNarrow = new Pen(Color.FromArgb(255, 232, 241, 255), 1);

                    public SettingsButtonLayout()
                    {
                        Size = new Size(466, 413);
                        Location = new Point(0, 63);

                        PrepareLayout();

                        this.HandleCreated += (o, e) => AddLayout();
                    }

                    void PrepareLayout()
                    {
                        for (int i = 0; i < Enum.GetNames(typeof(ButtonFunction)).Length; i++)
                        {
                            SettingsButton button = new SettingsButton((ButtonFunction)i);

                            switch (button.CurrentButtonFunction)
                            {
                                case ButtonFunction.SetName:
                                case ButtonFunction.SetInfo:
                                case ButtonFunction.SetPrivacy:
                                case ButtonFunction.SetBlacklist:
                                    ProfileSettingsButtons.Add(button);
                                    break;
                                case ButtonFunction.SetPassword:
                                case ButtonFunction.SetEmail:
                                case ButtonFunction.SetPhone:
                                case ButtonFunction.SetSecurityQuestions:
                                    SecuritySettingsButtons.Add(button);
                                    break;
                                case ButtonFunction.SetNotifications:
                                    NotificationSettingsButtons.Add(button);
                                    break;
                            }
                        }

                        int currentButtonY = 0;
                        int buttonOffsetX = 28;

                        for (int i = 0; i < ProfileSettingsButtons.Count(); i++)
                        {
                            if (i == 0)
                            {
                                TypeSeparators.Add(new TypeSeparator(ButtonType.ProfileSettings) { Location = new Point(0, currentButtonY) });

                                currentButtonY += TypeSeparators[TypeSeparators.Count - 1].Height;
                            }

                            ProfileSettingsButtons[i].Location = new Point(buttonOffsetX, currentButtonY);
                            currentButtonY += ProfileSettingsButtons[i].Height;
                        }

                        for (int i = 0; i < SecuritySettingsButtons.Count(); i++)
                        {
                            if (i == 0)
                            {
                                currentButtonY += 10;

                                TypeSeparators.Add(new TypeSeparator(ButtonType.SecuritySettings) { Location = new Point(0, currentButtonY) });

                                currentButtonY += TypeSeparators[TypeSeparators.Count - 1].Height;
                            }

                            SecuritySettingsButtons[i].Location = new Point(buttonOffsetX, currentButtonY);
                            currentButtonY += SecuritySettingsButtons[i].Height;
                        }

                        for (int i = 0; i < NotificationSettingsButtons.Count(); i++)
                        {
                            if (i == 0)
                            {
                                currentButtonY += 10;

                                TypeSeparators.Add(new TypeSeparator(ButtonType.NotificationSettings) { Location = new Point(0, currentButtonY) });

                                currentButtonY += TypeSeparators[TypeSeparators.Count - 1].Height;
                            }

                            NotificationSettingsButtons[i].Location = new Point(buttonOffsetX, currentButtonY);
                            currentButtonY += NotificationSettingsButtons[i].Height;
                        }
                    }

                    void AddLayout()
                    {
                        Controls.AddRange(TypeSeparators.ToArray());
                        Controls.AddRange(ProfileSettingsButtons.ToArray());
                        Controls.AddRange(SecuritySettingsButtons.ToArray());
                        Controls.AddRange(NotificationSettingsButtons.ToArray());
                    }


                    private class SettingsButton : Button
                    {
                        public ButtonFunction CurrentButtonFunction;
                        public SettingsButton(ButtonFunction buttonFunction)
                        {
                            CurrentButtonFunction = buttonFunction;

                            Size = new Size(411, 29);

                            ForeColor = Color.FromArgb(255, 93, 143, 217);
                            BackColor = Color.White;
                            Font = new Font("Verdana", 8, FontStyle.Regular);
                            TextAlign = ContentAlignment.MiddleLeft;
                            FlatStyle = FlatStyle.Flat;
                            FlatAppearance.BorderSize = 0;
                            FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 243, 247, 255);
                            FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 232, 241, 255);

                            Click += new EventHandler(OpenSettingsScreen);

                            switch (CurrentButtonFunction)
                            {
                                case ButtonFunction.SetName:
                                    Text = "Change profile name";
                                    break;
                                case ButtonFunction.SetInfo:
                                    Text = "Edit profile information - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetPrivacy:
                                    Text = "Privacy settings - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetBlacklist:
                                    Text = "Blocked accounts - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetPassword:
                                    Text = "Change password";
                                    break;
                                case ButtonFunction.SetEmail:
                                    Text = "Update email address - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetPhone:
                                    Text = "Update phone number - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetSecurityQuestions:
                                    Text = "Update security questions - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                                case ButtonFunction.SetNotifications:
                                    Text = "Notification settings - unavailable";
                                    BackColor = Color.WhiteSmoke;
                                    Enabled = false;
                                    break;
                            }
                        }

                        void OpenSettingsScreen(object sender, EventArgs e)
                        {
                            MainPage mainPage = Form.ActiveForm.Controls.OfType<MainPage>().First();

                            switch (CurrentButtonFunction)
                            {
                                case ButtonFunction.SetName:
                                    NameChangePanel nameChangePanel = new NameChangePanel();

                                    mainPage.OpenNewTabArbitrary(nameChangePanel, "NameChangePanel");
                                    break;
                                case ButtonFunction.SetInfo:

                                    break;
                                case ButtonFunction.SetPrivacy:

                                    break;
                                case ButtonFunction.SetBlacklist:

                                    break;
                                case ButtonFunction.SetPassword:
                                    PasswordChangePanel passwordChangePanel = new PasswordChangePanel();

                                    mainPage.OpenNewTabArbitrary(passwordChangePanel, "PasswordChangePanel");
                                    break;
                                case ButtonFunction.SetEmail:

                                    break;
                                case ButtonFunction.SetPhone:

                                    break;
                                case ButtonFunction.SetSecurityQuestions:

                                    break;
                                case ButtonFunction.SetNotifications:

                                    break;
                            }
                        }

                        protected override void OnPaint(PaintEventArgs pevent)
                        {
                            base.OnPaint(pevent);

                            pevent.Graphics.DrawLine(GrayPenNarrow, new Point(0, Height - 1), new Point(Width, Height - 1));
                        }
                    }

                    private class TypeSeparator : DoubleBufferPanel
                    {
                        ButtonType ButtonCollectionType;

                        public TypeSeparator(ButtonType separationType)
                        {
                            ButtonCollectionType = separationType;

                            Size = new Size(466, 33);

                            Label typeLabel = new Label();
                            typeLabel.Font = new Font("Verdana", 12, FontStyle.Regular);
                            typeLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                            typeLabel.BackColor = Color.White;
                            typeLabel.Size = new Size(442, 28);

                            switch (ButtonCollectionType)
                            {
                                case ButtonType.ProfileSettings:
                                    typeLabel.Text = "Profile";
                                    break;
                                case ButtonType.SecuritySettings:
                                    typeLabel.Text = "Security";
                                    break;
                                case ButtonType.NotificationSettings:
                                    typeLabel.Text = "Notifications";
                                    break;
                            }


                            typeLabel.Location = new Point(13, 2);

                            this.HandleCreated += (o, e) => Controls.Add(typeLabel);
                        }

                        protected override void OnPaint(PaintEventArgs e)
                        {
                            e.Graphics.DrawLine(GrayPenWide, GetDpiAdjustedPoint(14, 32), GetDpiAdjustedPoint(454, 32));

                            base.OnPaint(e);
                        }
                    }
                }

                private class TitlePanel : DoubleBufferPanel
                {
                    PictureBox logotypePictureBox;
                    Label versionLabel;
                    Label versionDateLabel;
                    Label rightsLabel;

                    Pen GrayPen = new Pen(Color.FromArgb(255, 232, 241, 255), 2);

                    public TitlePanel()
                    {
                        Size = new Size(374, 433);
                        Location = new Point(466, 40);

                        logotypePictureBox = new PictureBox();
                        logotypePictureBox.Size = Properties.Resources.chatBubbleLogo.Size;
                        logotypePictureBox.Location = new Point(Width / 2 - logotypePictureBox.Width / 2, Height / 2 - logotypePictureBox.Height + 15);
                        logotypePictureBox.Image = Properties.Resources.chatBubbleLogo;
                        logotypePictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

                        versionLabel = new Label();
                        versionLabel.Size = new Size(Width - 8, 16);
                        versionLabel.Location = new Point(4, 270);
                        versionLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                        versionLabel.Font = new Font("Verdana", 9.2f, FontStyle.Regular);
                        versionLabel.TextAlign = ContentAlignment.MiddleCenter;
                        versionLabel.Text = "ChatBubble v" + typeof(Form1).Assembly.GetName().Version.Major + "." + typeof(Form1).Assembly.GetName().Version.Minor;

                        versionDateLabel = new Label();
                        versionDateLabel.Size = new Size(Width - 8, 16);
                        versionDateLabel.Location = new Point(4, 292);
                        versionDateLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                        versionDateLabel.Font = new Font("Verdana", 8, FontStyle.Regular);
                        versionDateLabel.TextAlign = ContentAlignment.MiddleCenter;
                        versionDateLabel.Text = "03.10.2020";

                        rightsLabel = new Label();
                        rightsLabel.Size = new Size(Width - 8, 50);
                        rightsLabel.Location = new Point(4, 344);
                        rightsLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                        rightsLabel.Font = new Font("Verdana", 9.6f, FontStyle.Regular);
                        rightsLabel.TextAlign = ContentAlignment.MiddleCenter;
                        rightsLabel.Text = "Timofey Zheludkov\nAll Rights Reserved";

                        this.HandleCreated += (o, e) =>
                        {
                            Controls.Add(versionLabel);
                            Controls.Add(versionDateLabel);
                            Controls.Add(rightsLabel);
                            Controls.Add(logotypePictureBox);
                        };
                    }

                    protected override void OnPaint(PaintEventArgs e)
                    {
                        e.Graphics.DrawLine(GrayPen, 2, 20, 2, Height - 20);

                        base.OnPaint(e);
                    }
                }

                private class NameChangePanel : DoubleBufferPanel
                {
                    TabHatImage tabHatImage;
                    Label manualLabel;
                    WaterMarkTextBox firstNameTextBox;
                    WaterMarkTextBox secondNameTextBox;
                    LastTabButton lastTabButton;

                    RoundedBackgroundButton confirmButton;

                    ResultPanel resultPanel;

                    Pen GrayPen = new Pen(Color.FromArgb(255, 232, 241, 255), 2);

                    public NameChangePanel()
                    {
                        tabHatImage = new TabHatImage(TabType.Settings, "Change Profile Name", true);
                        lastTabButton = new LastTabButton();
                        Anchor = AnchorStyles.Left | AnchorStyles.Top;

                        manualLabel = new Label();
                        manualLabel.Font = new Font("Verdana", 12, FontStyle.Regular);
                        manualLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                        manualLabel.Size = new Size(442, 28);
                        manualLabel.Location = new Point(13, 100);
                        manualLabel.Text = "Please provide your new preferred name here";
                        manualLabel.TextAlign = ContentAlignment.MiddleCenter;

                        firstNameTextBox = new WaterMarkTextBox();
                        firstNameTextBox.Watermark = "First name";
                        firstNameTextBox.Font = titleFont;
                        firstNameTextBox.Size = new Size(350, 16);
                        firstNameTextBox.Location = new Point(468 / 2 - firstNameTextBox.Width / 2, 170);                       
                        firstNameTextBox.RoundedBorderColor = manualLabel.ForeColor;
                        firstNameTextBox.RoundedBorderWidth = 3;
                        firstNameTextBox.TabIndex = 0;

                        secondNameTextBox = new WaterMarkTextBox();
                        secondNameTextBox.Watermark = "Last name";
                        secondNameTextBox.Font = titleFont;
                        secondNameTextBox.Size = new Size(350, 16);
                        secondNameTextBox.Location = new Point(468 / 2 - secondNameTextBox.Width / 2, firstNameTextBox.Bottom + 16);                      
                        secondNameTextBox.RoundedBorderColor = manualLabel.ForeColor;
                        secondNameTextBox.RoundedBorderWidth = 3;
                        secondNameTextBox.TabIndex = 1;

                        confirmButton = new RoundedBackgroundButton();
                        confirmButton.DefaultRectangleColor = Color.FromArgb(255, 93, 143, 217);
                        confirmButton.Size = new Size(firstNameTextBox.Width + 18, secondNameTextBox.Bottom - firstNameTextBox.Top);
                        confirmButton.Location = new Point(secondNameTextBox.Left - 9, secondNameTextBox.Bottom + 8);
                        confirmButton.Text = "Confirm";
                        confirmButton.Font = titleFont;
                        confirmButton.ForeColor = Color.White;
                        confirmButton.Click += new EventHandler(ConfirmNameChange);
                        confirmButton.TabIndex = 2;

                        resultPanel = new ResultPanel(ResultPanel.AppearanceType.RightBorder, new Size(tabSize.Width - 470, tabSize.Height - 40), 40);

                        firstNameTextBox.CreateRoundedBorder = true;
                        secondNameTextBox.CreateRoundedBorder = true;

                        this.HandleCreated += (o, e) =>
                        {
                            Controls.Add(tabHatImage);
                            Controls.Add(manualLabel);
                            Controls.Add(firstNameTextBox);
                            Controls.Add(secondNameTextBox);
                            Controls.Add(lastTabButton);
                            Controls.Add(confirmButton);                            
                            Controls.Add(resultPanel);

                            tabHatImage.BringToFront();
                            lastTabButton.BringToFront();
                        };
                    }

                    void ConfirmNameChange(object sender, EventArgs e)
                    {
                        if (firstNameTextBox.WatermarkApplied || secondNameTextBox.WatermarkApplied)
                        {
                            return;
                        }

                        string firstName = firstNameTextBox.Text;
                        string lastName = secondNameTextBox.Text;

                        firstName = Char.ToUpper(firstName[0]) + firstName.Substring(1);
                        lastName = Char.ToUpper(lastName[0]) + lastName.Substring(1);

                        string serverReply = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.ChangeNameRequest, "newname=" + firstName + " " + lastName, true, true);

                        if (serverReply == NetComponents.ConnectionCodes.NmChgSuccess)
                        {
                            foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                            {
                                textBox.RoundedBorderColor = Color.FromArgb(255, 141, 179, 16);
                                textBox.Click += new EventHandler(RefreshOutlineColors);
                            }

                            confirmButton.DefaultRectangleColor = Color.FromArgb(255, 141, 179, 16);
                            
                            resultPanel.CreateResult(ResultPanel.ResultType.Success, "Name changed successfully!");
                        }
                        if (serverReply == NetComponents.ConnectionCodes.RestrictedError)
                        {
                            foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                            {
                                textBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                                textBox.Click += new EventHandler(RefreshOutlineColors);
                            }

                            resultPanel.CreateResult(ResultPanel.ResultType.Failure, "Wrong password!");
                        }
                        if (serverReply == NetComponents.ConnectionCodes.DatabaseError)
                        {
                            foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                            {
                                textBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                                textBox.Click += new EventHandler(RefreshOutlineColors);
                            }
                            confirmButton.DefaultRectangleColor = Color.FromArgb(255, 255, 98, 78);

                            resultPanel.CreateResult(ResultPanel.ResultType.ServerFailure, "");
                        }

                        confirmButton.MouseEnter += new EventHandler(RefreshOutlineColors);
                        resultPanel.Focus();
                    }

                    void RefreshOutlineColors(object sender, EventArgs eventArgs)
                    {
                        foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                        {
                            textBox.RoundedBorderColor = Color.FromArgb(255, 93, 143, 217);
                            textBox.Click -= RefreshOutlineColors;
                        }

                        foreach (RoundedBackgroundButton button in Controls.OfType<RoundedBackgroundButton>())
                        {
                            button.DefaultRectangleColor = Color.FromArgb(255, 93, 143, 217);
                            button.Focus();
                        }
                    }

                    protected override void OnPaint(PaintEventArgs e)
                    {
                        e.Graphics.DrawLine(GrayPen, manualLabel.Left, manualLabel.Bottom + 1, manualLabel.Right, manualLabel.Bottom + 1);
                        e.Graphics.DrawLine(GrayPen, new Point(468, 60), new Point(468, tabSize.Height - 20));

                        base.OnPaint(e);
                    }
                }

                private class PasswordChangePanel : DoubleBufferPanel
                {
                    TabHatImage tabHatImage;
                    Label manualLabel;
                    WaterMarkTextBox oldPasswordTextBox;
                    WaterMarkTextBox newPasswordTextBox;
                    WaterMarkTextBox repeatPasswordTextBox;
                    LastTabButton lastTabButton;

                    ResultPanel resultPanel;

                    RoundedBackgroundButton confirmButton;

                    Pen GrayPen = new Pen(Color.FromArgb(255, 232, 241, 255), 2);

                    public PasswordChangePanel()
                    {
                        tabHatImage = new TabHatImage(TabType.Settings, "Change Password", true);
                        lastTabButton = new LastTabButton();
                        Anchor = AnchorStyles.Left | AnchorStyles.Top;

                        manualLabel = new Label();
                        manualLabel.Font = new Font("Verdana", 12, FontStyle.Regular);
                        manualLabel.ForeColor = Color.FromArgb(255, 93, 143, 217);
                        manualLabel.Size = new Size(442, 28);
                        manualLabel.Location = new Point(13, 100);
                        manualLabel.Text = "Please provide necessary information";
                        manualLabel.TextAlign = ContentAlignment.TopCenter;

                        oldPasswordTextBox = new WaterMarkTextBox();
                        oldPasswordTextBox.Watermark = "Old password";
                        oldPasswordTextBox.Font = titleFont;
                        oldPasswordTextBox.PasswordEnabled = true;
                        oldPasswordTextBox.Size = new Size(350, 16);
                        oldPasswordTextBox.Location = new Point(468 / 2 - oldPasswordTextBox.Width / 2, 170);                      
                        oldPasswordTextBox.RoundedBorderColor = manualLabel.ForeColor;
                        oldPasswordTextBox.RoundedBorderWidth = 3;
                        oldPasswordTextBox.TabIndex = 0;

                        newPasswordTextBox = new WaterMarkTextBox();
                        newPasswordTextBox.Watermark = "New password";
                        newPasswordTextBox.Font = titleFont;
                        newPasswordTextBox.PasswordEnabled = true;
                        newPasswordTextBox.Size = new Size(350, 16);
                        newPasswordTextBox.Location = new Point(468 / 2 - newPasswordTextBox.Width / 2, oldPasswordTextBox.Bottom + 16);                    
                        newPasswordTextBox.RoundedBorderColor = manualLabel.ForeColor;
                        newPasswordTextBox.RoundedBorderWidth = 3;
                        newPasswordTextBox.TabIndex = 1;

                        repeatPasswordTextBox = new WaterMarkTextBox();
                        repeatPasswordTextBox.Watermark = "Repeat password";
                        repeatPasswordTextBox.Font = titleFont;
                        repeatPasswordTextBox.PasswordEnabled = true;
                        repeatPasswordTextBox.Size = new Size(350, 16);
                        repeatPasswordTextBox.Location = new Point(468 / 2 - repeatPasswordTextBox.Width / 2, newPasswordTextBox.Bottom + 16);                     
                        repeatPasswordTextBox.RoundedBorderColor = manualLabel.ForeColor;
                        repeatPasswordTextBox.RoundedBorderWidth = 3;
                        repeatPasswordTextBox.TabIndex = 2;

                        confirmButton = new RoundedBackgroundButton();
                        confirmButton.DefaultRectangleColor = Color.FromArgb(255, 93, 143, 217);
                        confirmButton.Size = new Size(repeatPasswordTextBox.Width + 18, repeatPasswordTextBox.Bottom - newPasswordTextBox.Top);
                        confirmButton.Location = new Point(repeatPasswordTextBox.Left - 9, repeatPasswordTextBox.Bottom + 8);
                        confirmButton.Text = "Confirm";
                        confirmButton.Font = titleFont;
                        confirmButton.ForeColor = Color.White;
                        confirmButton.Click += new EventHandler(ConfirmPasswordChange);
                        confirmButton.TabIndex = 3;

                        resultPanel = new ResultPanel(ResultPanel.AppearanceType.RightBorder, new Size(tabSize.Width - 470, tabSize.Height - 40), 40);

                        oldPasswordTextBox.CreateRoundedBorder = true;
                        newPasswordTextBox.CreateRoundedBorder = true;
                        repeatPasswordTextBox.CreateRoundedBorder = true;

                        this.HandleCreated += (o, e) =>
                        {
                            Controls.Add(tabHatImage);
                            Controls.Add(manualLabel);
                            Controls.Add(oldPasswordTextBox);
                            Controls.Add(newPasswordTextBox);
                            Controls.Add(repeatPasswordTextBox);
                            Controls.Add(lastTabButton);
                            Controls.Add(confirmButton);                         
                            Controls.Add(resultPanel);

                            resultPanel.BringToFront();

                            tabHatImage.BringToFront();
                            lastTabButton.BringToFront();
                        };
                    }

                    protected override void OnPaint(PaintEventArgs e)
                    {
                        e.Graphics.DrawLine(GrayPen, manualLabel.Left, manualLabel.Bottom + 1, manualLabel.Right, manualLabel.Bottom + 1);
                        e.Graphics.DrawLine(GrayPen, new Point(468, 60), new Point(468, tabSize.Height - 20));

                        base.OnPaint(e);
                    }

                    void ConfirmPasswordChange(object sender, EventArgs e)
                    {
                        if (oldPasswordTextBox.WatermarkApplied || newPasswordTextBox.WatermarkApplied || repeatPasswordTextBox.WatermarkApplied)
                        {
                            return;
                        }

                        if (oldPasswordTextBox.Text == repeatPasswordTextBox.Text && oldPasswordTextBox.Text == newPasswordTextBox.Text)
                        {
                            foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                            {
                                textBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                                textBox.Click += new EventHandler(RefreshOutlineColors);
                            }

                            confirmButton.DefaultRectangleColor = Color.FromArgb(255, 255, 98, 78);
                            confirmButton.MouseEnter += new EventHandler(RefreshOutlineColors);

                            resultPanel.CreateResult(ResultPanel.ResultType.Failure, "New password can't be\n the same as the old password!");

                            resultPanel.Focus();
                            return;
                        }

                        if (newPasswordTextBox.Text == repeatPasswordTextBox.Text)
                        {
                            string serverReply = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.ChangePasswdRequest, "oldpass=" + oldPasswordTextBox.Text + "newpass=" + newPasswordTextBox.Text, true, true);

                            if (serverReply == NetComponents.ConnectionCodes.PswdChgSuccess)
                            {
                                foreach(WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                                {
                                    textBox.RoundedBorderColor = Color.FromArgb(255, 141, 179, 16);
                                    textBox.Click += new EventHandler(RefreshOutlineColors);
                                }

                                confirmButton.DefaultRectangleColor = Color.FromArgb(255, 141, 179, 16);
                                confirmButton.MouseEnter += new EventHandler(RefreshOutlineColors);

                                resultPanel.CreateResult(ResultPanel.ResultType.Success, "Password changed successfully!");
                            }
                            if (serverReply == NetComponents.ConnectionCodes.PswdChgFailure)
                            {
                                oldPasswordTextBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                                oldPasswordTextBox.Click += new EventHandler(RefreshOutlineColors);

                                resultPanel.CreateResult(ResultPanel.ResultType.Failure, "Wrong password!");
                            }
                            if (serverReply == NetComponents.ConnectionCodes.DatabaseError)
                            {
                                foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                                {
                                    textBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                                    textBox.Click += new EventHandler(RefreshOutlineColors);
                                }

                                confirmButton.DefaultRectangleColor = Color.FromArgb(255, 255, 98, 78);
                                confirmButton.MouseEnter += new EventHandler(RefreshOutlineColors);

                                resultPanel.CreateResult(ResultPanel.ResultType.ServerFailure, "");
                            }
                        }
                        else
                        {
                            resultPanel.CreateResult(ResultPanel.ResultType.Failure, "Passwords don't match!");

                            newPasswordTextBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                            newPasswordTextBox.Click += new EventHandler(RefreshOutlineColors);

                            repeatPasswordTextBox.RoundedBorderColor = Color.FromArgb(255, 255, 98, 78);
                            repeatPasswordTextBox.Click += new EventHandler(RefreshOutlineColors);
                        }

                        resultPanel.Focus();
                    }

                    void RefreshOutlineColors(object sender, EventArgs eventArgs)
                    {
                        foreach (WaterMarkTextBox textBox in Controls.OfType<WaterMarkTextBox>())
                        {
                            textBox.RoundedBorderColor = Color.FromArgb(255, 93, 143, 217);
                            textBox.Click -= RefreshOutlineColors;
                        }      
                        
                        foreach (RoundedBackgroundButton button in Controls.OfType<RoundedBackgroundButton>())
                        {
                            button.DefaultRectangleColor = Color.FromArgb(255, 93, 143, 217);
                            button.Focus();
                        }
                    }
                }
            }
            private class LogOutTab : DoubleBufferPanel
            {



                public LogOutTab()
                {
                    //tabHatImage.Image = Properties.Resources.tabBackgroundLogOutHat;
                    //this.Controls.Add(tabHatImage);

                    //tabNameLabel.Text = "Log Out";
                    //this.Controls.Add(tabNameLabel);
                }

                public void LogOut(bool sendTCPResetRequest)
                {
                    Form1.connectedCheckTimer.Stop();

                    if (sendTCPResetRequest == true)
                    {
                        NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.LogOutCall, "", false);
                    }

                    NetComponents.BreakBind(true);

                    FileIOStreamer fileIO = new FileIOStreamer();
                    fileIO.WriteToFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt", "invalid_invalid", true);

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
            }

            private class MenuButton : Button   //This class encompasses all the buttons to the left of the tabs
            {
                Size buttonSize = new Size(60, 60);
                TabType thisTabType;

                System.Drawing.Bitmap backgroundImageIdle;
                System.Drawing.Bitmap backgroundImageOnHover;
                System.Drawing.Bitmap backgroundImageOnClick;


                public MenuButton(Panel parentMainPage, MainPage instanceMainPage, TabType tabType)
                {
                    FlatStyle = FlatStyle.Flat;
                    FlatAppearance.BorderSize = 0;
                    TextAlign = ContentAlignment.MiddleLeft;
                    BackgroundImageLayout = ImageLayout.Stretch;
                    Font = titleFont;
                    thisTabType = tabType;

                    int buttonY;

                    if (parentMainPage.Controls.OfType<MenuButton>().Count() > 0)
                    {
                        buttonY = parentMainPage.Controls.OfType<MenuButton>().Last().Location.Y + parentMainPage.Controls.OfType<MenuButton>().Last().Height - 1;
                    }
                    else
                    {
                        buttonSize.Height = 97;
                        buttonY = 7;
                    }

                    Size = buttonSize;
                    Location = new Point(0, buttonY);
                    Anchor = AnchorStyles.Left;

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

                        instanceMainPage.OpenNewTab(tabType);
                        instanceMainPage.ClearAll(true);                       
                    }

                }
            }

        }

        public class FrontDoorPage : DoubleBufferPanel
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
                    passwordTextBox.PasswordEnabled = true;
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

                    foreach (WaterMarkTextBox textBox in this.Controls.OfType<WaterMarkTextBox>())
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
                    if (loginTextBox.WatermarkApplied == true || passwordTextBox.WatermarkApplied == true)
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

                    if (statusMessage.Text == "Wrong login or password.")
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

                if (serverReplySubstrings[0] == NetComponents.ConnectionCodes.LoginSuccess)
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

                    return (NetComponents.ConnectionCodes.LoginSuccess);
                }
                else if (serverReply == NetComponents.ConnectionCodes.LoginFailure)
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
                    passwordTextBox.PasswordEnabled = true;
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

                    foreach (WaterMarkTextBox textBox in this.Controls.OfType<WaterMarkTextBox>())
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

                    if (usernameTextBox.WatermarkApplied == true || passwordTextBox.WatermarkApplied == true
                        || firstNameTextBox.WatermarkApplied == true || repeatPasswordTextBox.WatermarkApplied == true)
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

                    if(serverReplySubstrings[0] == NetComponents.ConnectionCodes.SignUpFailure)
                    {
                        usernameTextBoxBackgroundPicture.BackgroundImage = Properties.Resources.frontDoorTextBoxBorderError;
                        statusMessage.Text = "User with this username already exists.";
                        return;
                    }
                    if(serverReplySubstrings[0] == NetComponents.ConnectionCodes.DatabaseError)
                    {
                        statusMessage.Text = "Server sign up service unavailable.";
                        return;
                    }
                    if(serverReplySubstrings[0] == NetComponents.ConnectionCodes.SignUpSuccess)
                    {
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
                foreach (WaterMarkTextBox textBox in currentPanel.Controls.OfType<WaterMarkTextBox>())
                {
                    textBox.WatermarkApplied = false;
                    textBox.Text = "";
                }
            }
        }

        public class LoadingPage : DoubleBufferPanel
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
                loadingImageBox.Size = loadingPageSize;
                loadingImageBox.Location = loadingPageLocation;
                loadingImageBox.Image = Properties.Resources.loadingScreen20fps;
                loadingImageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                loadingMessageLabel = new Label();
                loadingMessageLabel.Size = new Size(600, 26);
                loadingMessageLabel.Location = new Point(loadingPageSize.Width / 2 - loadingMessageLabel.Width / 2, 339);
                loadingMessageLabel.Font = new Font("Verdana", 11, FontStyle.Italic);
                loadingMessageLabel.TextAlign = ContentAlignment.MiddleCenter;
                loadingMessageLabel.BackColor = Color.Transparent;
                loadingMessageLabel.ForeColor = Color.White;

                this.Controls.Add(loadingImageBox);
                loadingImageBox.Controls.Add(loadingMessageLabel);

                loadingMessageLabel.BringToFront();

                Thread initialHandshakeThread = new Thread(ClientStartUp);
                initialHandshakeThread.Start();
            }

            void ClientStartUp()
            {
                while (ActiveForm.IsHandleCreated != true)
                {
                    Thread.Sleep(1);
                }

                MessageChangeDelegate messageChangeDelegate = new MessageChangeDelegate(LoadingMessageSetText);

                int attemptNumber = 2;
                string handshakeResult;

                Invoke(messageChangeDelegate, "Connecting...");
                while (loadingMessageLabel.Text != "Connected!")
                {
                    //"68.183.203.93"
                    NetComponents.ClientSetServerEndpoints("192.168.0.144", 8000);
                    handshakeResult = NetComponents.InitialHandshakeClient();

                    if (attemptNumber >= 100 || handshakeResult == NetComponents.ConnectionCodes.ConnectionFailure)
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

                    if (handshakeResult == NetComponents.ConnectionCodes.ExpiredSessionStatus)
                    {
                        Invoke(messageChangeDelegate, "Connected!");

                        Thread.Sleep(1000);

                        FrontDoorPage frontDoorPage = new FrontDoorPage();
                        PageAddDelegate pageAddDelegate = new PageAddDelegate(ActiveForm.Controls.Add);
                        BringToFrontDelegate bringToFrontDelegate = new BringToFrontDelegate(frontDoorPage.BringToFront);
                        PanelSetupDelegate panelSetupDelegate = new PanelSetupDelegate(frontDoorPage.PrepareCredentialPanels);
                        ControlDisposeDelegate controlDisposeDelegate = new ControlDisposeDelegate(this.Dispose);

                        Invoke(pageAddDelegate, frontDoorPage);

                        Invoke(new Action(
                            () =>
                            {
                                ActiveForm.ControlBox = true;
                                ActiveForm.Text = "ChatBubble";
                                ActiveForm.Size = DefaultFormSize;
                            }));

                        Invoke(bringToFrontDelegate);

                        Thread.Sleep(200);

                        Invoke(panelSetupDelegate);

                        Invoke(new Action(
                            () =>
                            {
                                loadingImageBox.Image = null;
                                loadingImageBox.Dispose();
                                this.Dispose();
                            }
                            ));

                        return;
                    }
                    else if (handshakeResult.Substring(0, NetComponents.ConnectionCodes.DefaultFlagLength) == NetComponents.ConnectionCodes.LoginSuccess) // V This happens when user cookie matches server records V
                    {
                        Invoke(messageChangeDelegate, "Connected!");

                        Thread.Sleep(1000);

                        MainPage mainPage = new MainPage();
                        FrontDoorPage frontDoorPage = new FrontDoorPage();

                        CookieLoginDelegate cookieLoginDelegate = new CookieLoginDelegate(frontDoorPage.LogInHandler);
                        BringToFrontDelegate bringToFrontDelegate = new BringToFrontDelegate(mainPage.BringToFront);

                        Invoke(cookieLoginDelegate, handshakeResult);

                        Invoke(new Action(
                            () =>
                            {
                                ActiveForm.ControlBox = true;
                                ActiveForm.Text = "ChatBubble";
                                ActiveForm.Size = DefaultFormSize;
                            }));

                        Invoke(bringToFrontDelegate);

                        Invoke(new Action(
                            () =>
                            {
                                loadingImageBox.Image = null;
                                loadingImageBox.Dispose();
                                this.Dispose();
                            }
                            ));

                        connectedCheckTimer.Start();
                        return;
                    }
                    else
                    {
                        attemptNumber++;
                        Invoke(messageChangeDelegate, "Connecting, attempt " + attemptNumber + "...");
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

            this.BackgroundImageLayout = ImageLayout.Stretch; 
        }
    }

    public class WaterMarkTextBox : TextBox
    {
        BorderPictureBox borderPictureBox;

        string customWatermark = "Watermark";
        public bool WatermarkApplied = false;
        bool createRoundedBorder = false;
        bool borderEnabledBeforeResize = false;
        bool passwordEnabled;
        char specifiedPasswordChar;

        Font font;

        Color color1;
        int width1;

        public bool CreateRoundedBorder
        {
            get
            {
                return createRoundedBorder;
            }
            set
            {
                createRoundedBorder = value;
                BorderStyle = BorderStyle.None;
            }
        }

        public bool PasswordEnabled
        {
            get
            {
                return passwordEnabled;
            }
            set
            {
                passwordEnabled = value;
                specifiedPasswordChar = '•';
                PasswordChar = '\0';
            }
        }

        public Color RoundedBorderColor
        {
            get
            {
                return color1;
            }
            set
            {
                color1 = value;
                OnRoundedBorderChanged(this, EventArgs.Empty);
            }
        }

        public int RoundedBorderWidth
        {
            get
            {
                return width1;
            }
            set
            {
                width1 = value;
                OnRoundedBorderChanged(this, EventArgs.Empty);
            }
        }


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
            WatermarkApplied = true;

            this.HandleCreated += new EventHandler(PrepareControl);
        }

        void PrepareControl(object sender, EventArgs e)
        {
            if (CreateRoundedBorder)
            {
                CreateBorder();
            }

            TextChanged += new EventHandler(this.WatermarkSwitch);
            KeyDown += new KeyEventHandler(this.SelectionMoved);
            MouseMove += new MouseEventHandler(this.ControlSelected);
            MouseDown += new MouseEventHandler(this.ControlSelected);

            WatermarkSwitch(this, EventArgs.Empty);

            this.SelectionStart = 0;
        }

        private void CreateBorder()
        {
            borderPictureBox = new BorderPictureBox(this, RoundedBorderColor, RoundedBorderWidth);

            Parent.Controls.Add(borderPictureBox);
        }

        private void WatermarkSwitch(object sender, EventArgs eventArgs)
        {
            if (specifiedPasswordChar != '\0')
            {
                if (WatermarkApplied == false)
                {
                    PasswordChar = specifiedPasswordChar;
                }
                else
                {
                    PasswordChar = '\0';
                }
            }

            if (Text.Length == 0 && WatermarkApplied == false)
            {
                WatermarkApplied = true;
                ForeColor = Color.Gray;
                Text = customWatermark;
                SelectionStart = 0;
            }

            if ((WatermarkApplied == true && this.Text != customWatermark))
            {              
                WatermarkApplied = false;
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
            if ((eventArgs.KeyCode == Keys.Right || eventArgs.KeyCode == Keys.Left) && WatermarkApplied == true)
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

        protected virtual void OnRoundedBorderChanged(object sender, EventArgs eventArgs)
        {
            if (this.Created)
            {
                borderPictureBox.GetNewPen(RoundedBorderColor, RoundedBorderWidth);
            }
        }

        private void ControlSelected(object sender, MouseEventArgs eventArgs)
        {
            if (WatermarkApplied == true)
            {
                this.SelectionLength = 0;
                this.SelectionStart = 0;
            }
        }

        private class BorderPictureBox : PictureBox
        {
            GraphicsPath borderPath;
            Pen BorderPen;

            public BorderPictureBox(WaterMarkTextBox primaryTextBox, Color borderColor, int borderWidth)
            {
                Size = new Size(primaryTextBox.Width + borderWidth * 3 + 8, primaryTextBox.Height + borderWidth * 3 + 3);

                Location = new Point(primaryTextBox.Left - borderWidth - 7, primaryTextBox.Top - borderWidth - 6);

                GetNewPen(borderColor, borderWidth);

                this.HandleCreated += (o, e) => GetRegion();
            }

            public void GetNewPen(Color color, int width)
            {
                BorderPen = new Pen(color, width);
                Invalidate();
            }

            void GetRegion()
            {
                int cornerRadius = 20;
                int borderMargin = (int)BorderPen.Width;

                borderPath = new GraphicsPath();

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
            }

            protected override void OnPaint(PaintEventArgs pe)
            {
                pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                pe.Graphics.CompositingMode = CompositingMode.SourceOver;
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                pe.Graphics.DrawPath(BorderPen, borderPath);

                base.OnPaint(pe);
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

    public class RoundedBackgroundButton : Button
    {    
        Color RectangleIdleColor { get; set; }
        Color RectangleMouseEnterColor { get; set; }
        Color RectangleMouseDownColor { get; set; }
        Color defaultColor;
        public Color DefaultRectangleColor
        {
            get
            {
                return defaultColor;
            }
            set
            {
                defaultColor = value;
                GetColors();
            }
        }

        SolidBrush BackgroundBrush;
        SolidBrush TextBrush;

        GraphicsPath borderPath;
        Size TextSize;

        public RoundedBackgroundButton()
        {
            FlatAppearance.BorderSize = 0;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;

            Text = "test";
        }

        protected override void OnCreateControl()
        {
            GetColors();
            GetBackground();

            base.OnCreateControl();
        }

        void GetColors()
        {
            RectangleIdleColor = DefaultRectangleColor;

            BackColor = Color.White;

            int newRed = DefaultRectangleColor.R - 22;
            int newGreen = DefaultRectangleColor.G - 32;
            int newBlue = DefaultRectangleColor.B - 54;

            if (newRed < 0)
            {
                newRed = 0;
            }
            if (newGreen < 0)
            {
                newGreen = 0;
            }
            if (newBlue < 0)
            {
                newBlue = 0;
            }

            RectangleMouseDownColor = Color.FromArgb(255, newRed, newGreen, newBlue);
            RectangleMouseEnterColor = Color.FromArgb(255, newRed + 12, newGreen + 18, newBlue + 30);

            BackgroundBrush = new System.Drawing.SolidBrush(RectangleIdleColor);
            TextBrush = new System.Drawing.SolidBrush(ForeColor);
        }

        void GetBackground()
        {
            int cornerRadius = 25;
            int borderMargin = 1;

            borderPath = new GraphicsPath();

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

            TextSize = TextRenderer.MeasureText(Text, Font);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            BackgroundBrush = new SolidBrush(RectangleMouseEnterColor);

            base.OnMouseEnter(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            BackgroundBrush = new SolidBrush(RectangleMouseDownColor);

            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            BackgroundBrush = new SolidBrush(RectangleMouseEnterColor);
            
            base.OnMouseUp(mevent);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            BackgroundBrush = new SolidBrush(RectangleIdleColor);

            base.OnMouseLeave(e);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            GetColors();
            
            base.OnBackColorChanged(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            pevent.Graphics.CompositingMode = CompositingMode.SourceOver;

            base.OnPaint(pevent);

            pevent.Graphics.FillPath(BackgroundBrush, borderPath);
            pevent.Graphics.DrawString(Text, Font, TextBrush, Width/2 - TextSize.Width / 2, Height/2 - TextSize.Height / 2);
        }
    }
}