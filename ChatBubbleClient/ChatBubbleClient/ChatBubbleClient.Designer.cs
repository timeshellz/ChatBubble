namespace ChatBubble.Client
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.mainPanel = new System.Windows.Forms.Panel();
            this.friendsListPanel = new ChatBubble.Client.DoubleBufferPanel();
            this.friendsTabLabel = new System.Windows.Forms.Label();
            this.friendsTabHat = new System.Windows.Forms.PictureBox();
            this.dialoguesPanel = new ChatBubble.Client.DoubleBufferPanel();
            this.dialoguesTabLabel = new System.Windows.Forms.Label();
            this.dialoguesTabHat = new System.Windows.Forms.PictureBox();
            this.mainPagePanel = new ChatBubble.Client.DoubleBufferPanel();
            this.mainPageTabLabel = new System.Windows.Forms.Label();
            this.mainPageTabHat = new System.Windows.Forms.PictureBox();
            this.settingsPanel = new ChatBubble.Client.DoubleBufferPanel();
            this.settingsTabLabel = new System.Windows.Forms.Label();
            this.settingsTabHat = new System.Windows.Forms.PictureBox();
            this.logOutButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.searchButton = new System.Windows.Forms.Button();
            this.dialogueButton = new System.Windows.Forms.Button();
            this.friendsButton = new System.Windows.Forms.Button();
            this.mainPageButton = new System.Windows.Forms.Button();
            this.profilePictureMain = new System.Windows.Forms.PictureBox();
            this.mainPanel.SuspendLayout();
            this.friendsListPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.friendsTabHat)).BeginInit();
            this.dialoguesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dialoguesTabHat)).BeginInit();
            this.mainPagePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainPageTabHat)).BeginInit();
            this.settingsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.settingsTabHat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.profilePictureMain)).BeginInit();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("mainPanel.BackgroundImage")));
            this.mainPanel.Controls.Add(this.friendsListPanel);
            this.mainPanel.Controls.Add(this.dialoguesPanel);
            this.mainPanel.Controls.Add(this.mainPagePanel);
            this.mainPanel.Controls.Add(this.settingsPanel);
            this.mainPanel.Controls.Add(this.logOutButton);
            this.mainPanel.Controls.Add(this.settingsButton);
            this.mainPanel.Controls.Add(this.searchButton);
            this.mainPanel.Controls.Add(this.dialogueButton);
            this.mainPanel.Controls.Add(this.friendsButton);
            this.mainPanel.Controls.Add(this.mainPageButton);
            this.mainPanel.Controls.Add(this.profilePictureMain);
            this.mainPanel.Enabled = false;
            this.mainPanel.Location = new System.Drawing.Point(0, -1);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(900, 480);
            this.mainPanel.TabIndex = 25;
            this.mainPanel.Visible = false;
            // 
            // friendsListPanel
            // 
            this.friendsListPanel.BackColor = System.Drawing.SystemColors.Window;
            this.friendsListPanel.Controls.Add(this.friendsTabLabel);
            this.friendsListPanel.Controls.Add(this.friendsTabHat);
            this.friendsListPanel.Location = new System.Drawing.Point(188, 7);
            this.friendsListPanel.Name = "friendsListPanel";
            this.friendsListPanel.Size = new System.Drawing.Size(705, 466);
            this.friendsListPanel.TabIndex = 7;
            this.friendsListPanel.Visible = false;
            // 
            // friendsTabLabel
            // 
            this.friendsTabLabel.Font = new System.Drawing.Font("Verdana", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.friendsTabLabel.Location = new System.Drawing.Point(20, 1);
            this.friendsTabLabel.Margin = new System.Windows.Forms.Padding(0);
            this.friendsTabLabel.Name = "friendsTabLabel";
            this.friendsTabLabel.Size = new System.Drawing.Size(684, 18);
            this.friendsTabLabel.TabIndex = 0;
            this.friendsTabLabel.Text = "Friends";
            // 
            // dialoguesPanel
            // 
            this.dialoguesPanel.Controls.Add(this.dialoguesTabLabel);
            this.dialoguesPanel.Controls.Add(this.dialoguesTabHat);
            this.dialoguesPanel.Location = new System.Drawing.Point(188, 7);
            this.dialoguesPanel.Name = "dialoguesPanel";
            this.dialoguesPanel.Size = new System.Drawing.Size(705, 466);
            this.dialoguesPanel.TabIndex = 8;
            this.dialoguesPanel.Visible = false;
            // 
            // dialoguesTabLabel
            // 
            this.dialoguesTabLabel.Font = new System.Drawing.Font("Verdana", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dialoguesTabLabel.Location = new System.Drawing.Point(20, 1);
            this.dialoguesTabLabel.Margin = new System.Windows.Forms.Padding(0);
            this.dialoguesTabLabel.Name = "dialoguesTabLabel";
            this.dialoguesTabLabel.Size = new System.Drawing.Size(684, 18);
            this.dialoguesTabLabel.TabIndex = 1;
            this.dialoguesTabLabel.Text = "Dialogues";
            // 
            // mainPagePanel
            // 
            this.mainPagePanel.BackColor = System.Drawing.SystemColors.Window;
            this.mainPagePanel.Controls.Add(this.mainPageTabLabel);
            this.mainPagePanel.Controls.Add(this.mainPageTabHat);
            this.mainPagePanel.Location = new System.Drawing.Point(188, 7);
            this.mainPagePanel.Name = "mainPagePanel";
            this.mainPagePanel.Size = new System.Drawing.Size(705, 466);
            this.mainPagePanel.TabIndex = 6;
            this.mainPagePanel.Visible = false;
            // 
            // mainPageTabLabel
            // 
            this.mainPageTabLabel.Font = new System.Drawing.Font("Verdana", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainPageTabLabel.Location = new System.Drawing.Point(20, 1);
            this.mainPageTabLabel.Margin = new System.Windows.Forms.Padding(0);
            this.mainPageTabLabel.Name = "mainPageTabLabel";
            this.mainPageTabLabel.Size = new System.Drawing.Size(684, 18);
            this.mainPageTabLabel.TabIndex = 1;
            this.mainPageTabLabel.Text = "Main Page";
            // 
            // settingsPanel
            // 
            this.settingsPanel.Controls.Add(this.settingsTabLabel);
            this.settingsPanel.Controls.Add(this.settingsTabHat);
            this.settingsPanel.Location = new System.Drawing.Point(188, 7);
            this.settingsPanel.Name = "settingsPanel";
            this.settingsPanel.Size = new System.Drawing.Size(705, 466);
            this.settingsPanel.TabIndex = 10;
            this.settingsPanel.Visible = false;
            // 
            // settingsTabLabel
            // 
            this.settingsTabLabel.Font = new System.Drawing.Font("Verdana", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsTabLabel.Location = new System.Drawing.Point(20, 1);
            this.settingsTabLabel.Margin = new System.Windows.Forms.Padding(0);
            this.settingsTabLabel.Name = "settingsTabLabel";
            this.settingsTabLabel.Size = new System.Drawing.Size(684, 18);
            this.settingsTabLabel.TabIndex = 1;
            this.settingsTabLabel.Text = "Settings";
            // 
            // logOutButton
            // 
            //this.logOutButton.BackgroundImage = global::ChatBubble.Client.Properties.Resources.buttonBackgroundLogOut;
            this.logOutButton.FlatAppearance.BorderSize = 0;
            this.logOutButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logOutButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logOutButton.Location = new System.Drawing.Point(7, 425);
            this.logOutButton.Name = "logOutButton";
            this.logOutButton.Size = new System.Drawing.Size(180, 49);
            this.logOutButton.TabIndex = 11;
            this.logOutButton.Text = "Log Out";
            this.logOutButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.logOutButton.UseVisualStyleBackColor = true;
            // 
            // settingsButton
            // 
            this.settingsButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("settingsButton.BackgroundImage")));
            this.settingsButton.FlatAppearance.BorderSize = 0;
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsButton.Location = new System.Drawing.Point(7, 377);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(180, 49);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "Settings";
            this.settingsButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsButton.UseVisualStyleBackColor = true;
            // 
            // searchButton
            // 
            this.searchButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("searchButton.BackgroundImage")));
            this.searchButton.FlatAppearance.BorderSize = 0;
            this.searchButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.searchButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.searchButton.Location = new System.Drawing.Point(7, 329);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(180, 49);
            this.searchButton.TabIndex = 4;
            this.searchButton.Text = "Search";
            this.searchButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.searchButton.UseVisualStyleBackColor = true;
            // 
            // dialogueButton
            // 
            this.dialogueButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("dialogueButton.BackgroundImage")));
            this.dialogueButton.FlatAppearance.BorderSize = 0;
            this.dialogueButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dialogueButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dialogueButton.Location = new System.Drawing.Point(7, 281);
            this.dialogueButton.Name = "dialogueButton";
            this.dialogueButton.Size = new System.Drawing.Size(180, 49);
            this.dialogueButton.TabIndex = 3;
            this.dialogueButton.Text = "Dialogues";
            this.dialogueButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dialogueButton.UseVisualStyleBackColor = true;
            // 
            // friendsButton
            // 
            this.friendsButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("friendsButton.BackgroundImage")));
            this.friendsButton.FlatAppearance.BorderSize = 0;
            this.friendsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.friendsButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.friendsButton.Location = new System.Drawing.Point(7, 233);
            this.friendsButton.Name = "friendsButton";
            this.friendsButton.Size = new System.Drawing.Size(180, 49);
            this.friendsButton.TabIndex = 2;
            this.friendsButton.Text = "Friends";
            this.friendsButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.friendsButton.UseVisualStyleBackColor = true;
            // 
            // mainPageButton
            // 
            this.mainPageButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("mainPageButton.BackgroundImage")));
            this.mainPageButton.FlatAppearance.BorderSize = 0;
            this.mainPageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.mainPageButton.Font = new System.Drawing.Font("Verdana", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainPageButton.Location = new System.Drawing.Point(7, 185);
            this.mainPageButton.Name = "mainPageButton";
            this.mainPageButton.Size = new System.Drawing.Size(180, 49);
            this.mainPageButton.TabIndex = 1;
            this.mainPageButton.Text = "Main Page";
            this.mainPageButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.mainPageButton.UseVisualStyleBackColor = true;
            // 
            // profilePictureMain
            // 
            this.profilePictureMain.Image = ((System.Drawing.Image)(resources.GetObject("profilePictureMain.Image")));
            this.profilePictureMain.Location = new System.Drawing.Point(7, 7);
            this.profilePictureMain.Name = "profilePictureMain";
            this.profilePictureMain.Size = new System.Drawing.Size(180, 180);
            this.profilePictureMain.TabIndex = 0;
            this.profilePictureMain.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(900, 479);
            this.Controls.Add(this.mainPanel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(918, 550);
            this.MinimumSize = new System.Drawing.Size(918, 479);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.mainPanel.ResumeLayout(false);
            this.friendsListPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.friendsTabHat)).EndInit();
            this.dialoguesPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dialoguesTabHat)).EndInit();
            this.mainPagePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainPageTabHat)).EndInit();
            this.settingsPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.settingsTabHat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.profilePictureMain)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button dialogueButton;
        private System.Windows.Forms.Button friendsButton;
        private System.Windows.Forms.Button mainPageButton;
        private System.Windows.Forms.PictureBox profilePictureMain;
        private System.Windows.Forms.Button logOutButton;
        private System.Windows.Forms.Label friendsTabLabel;
        private DoubleBufferPanel mainPagePanel;
        private DoubleBufferPanel dialoguesPanel;
        private DoubleBufferPanel settingsPanel;
        private System.Windows.Forms.Label settingsTabLabel;
        private System.Windows.Forms.Label dialoguesTabLabel;
        private System.Windows.Forms.Label mainPageTabLabel;
        public DoubleBufferPanel friendsListPanel;
        private System.Windows.Forms.PictureBox friendsTabHat;
        private System.Windows.Forms.PictureBox mainPageTabHat;
        private System.Windows.Forms.PictureBox dialoguesTabHat;
        private System.Windows.Forms.PictureBox settingsTabHat;
    }
}

