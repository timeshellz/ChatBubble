namespace ChatBubble.Server
{
    partial class Server
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
            this.components = new System.ComponentModel.Container();
            this.logTextbox = new System.Windows.Forms.RichTextBox();
            this.commandTextbox = new System.Windows.Forms.TextBox();
            this.currentIPTextbox = new System.Windows.Forms.TextBox();
            this.currentSocketTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.updateDataTimer = new System.Windows.Forms.Timer(this.components);
            this.currentState1Textbox = new System.Windows.Forms.TextBox();
            this.currentState2Textbox = new System.Windows.Forms.TextBox();
            this.clientCountTextbox = new System.Windows.Forms.TextBox();
            this.currentTimeTextbox = new System.Windows.Forms.TextBox();
            this.currentDateTextbox = new System.Windows.Forms.TextBox();
            this.clientStateRereshTimer = new System.Windows.Forms.Timer(this.components);
            this.loggedInCountTextbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // logTextbox
            // 
            this.logTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.logTextbox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logTextbox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.logTextbox.ForeColor = System.Drawing.SystemColors.WindowText;
            this.logTextbox.Location = new System.Drawing.Point(337, 16);
            this.logTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.logTextbox.Name = "logTextbox";
            this.logTextbox.ReadOnly = true;
            this.logTextbox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.logTextbox.Size = new System.Drawing.Size(432, 627);
            this.logTextbox.TabIndex = 0;
            this.logTextbox.Text = "";
            // 
            // commandTextbox
            // 
            this.commandTextbox.BackColor = System.Drawing.Color.White;
            this.commandTextbox.Location = new System.Drawing.Point(337, 652);
            this.commandTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.commandTextbox.Name = "commandTextbox";
            this.commandTextbox.Size = new System.Drawing.Size(432, 22);
            this.commandTextbox.TabIndex = 1;
            this.commandTextbox.TextChanged += new System.EventHandler(this.command_textbox_TextChanged);
            this.commandTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.command_textbox_KeyDown);
            // 
            // currentIPTextbox
            // 
            this.currentIPTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentIPTextbox.Location = new System.Drawing.Point(13, 62);
            this.currentIPTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.currentIPTextbox.Name = "currentIPTextbox";
            this.currentIPTextbox.ReadOnly = true;
            this.currentIPTextbox.Size = new System.Drawing.Size(312, 22);
            this.currentIPTextbox.TabIndex = 2;
            // 
            // currentSocketTextbox
            // 
            this.currentSocketTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentSocketTextbox.Location = new System.Drawing.Point(13, 84);
            this.currentSocketTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.currentSocketTextbox.Name = "currentSocketTextbox";
            this.currentSocketTextbox.ReadOnly = true;
            this.currentSocketTextbox.Size = new System.Drawing.Size(312, 22);
            this.currentSocketTextbox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(87, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "ChatBubble Server App";
            // 
            // updateDataTimer
            // 
            this.updateDataTimer.Enabled = true;
            this.updateDataTimer.Interval = 1;
            this.updateDataTimer.Tick += new System.EventHandler(this.update_data_timer_Tick);
            // 
            // currentState1Textbox
            // 
            this.currentState1Textbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentState1Textbox.Location = new System.Drawing.Point(13, 106);
            this.currentState1Textbox.Margin = new System.Windows.Forms.Padding(4);
            this.currentState1Textbox.Name = "currentState1Textbox";
            this.currentState1Textbox.ReadOnly = true;
            this.currentState1Textbox.Size = new System.Drawing.Size(312, 22);
            this.currentState1Textbox.TabIndex = 6;
            // 
            // currentState2Textbox
            // 
            this.currentState2Textbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentState2Textbox.Location = new System.Drawing.Point(13, 128);
            this.currentState2Textbox.Margin = new System.Windows.Forms.Padding(4);
            this.currentState2Textbox.Name = "currentState2Textbox";
            this.currentState2Textbox.ReadOnly = true;
            this.currentState2Textbox.Size = new System.Drawing.Size(312, 22);
            this.currentState2Textbox.TabIndex = 7;
            // 
            // clientCountTextbox
            // 
            this.clientCountTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.clientCountTextbox.Location = new System.Drawing.Point(13, 185);
            this.clientCountTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.clientCountTextbox.Name = "clientCountTextbox";
            this.clientCountTextbox.ReadOnly = true;
            this.clientCountTextbox.Size = new System.Drawing.Size(312, 22);
            this.clientCountTextbox.TabIndex = 8;
            this.clientCountTextbox.Visible = false;
            // 
            // currentTimeTextbox
            // 
            this.currentTimeTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentTimeTextbox.Location = new System.Drawing.Point(13, 652);
            this.currentTimeTextbox.Name = "currentTimeTextbox";
            this.currentTimeTextbox.ReadOnly = true;
            this.currentTimeTextbox.Size = new System.Drawing.Size(108, 22);
            this.currentTimeTextbox.TabIndex = 9;
            this.currentTimeTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // currentDateTextbox
            // 
            this.currentDateTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.currentDateTextbox.Location = new System.Drawing.Point(135, 652);
            this.currentDateTextbox.Name = "currentDateTextbox";
            this.currentDateTextbox.ReadOnly = true;
            this.currentDateTextbox.Size = new System.Drawing.Size(190, 22);
            this.currentDateTextbox.TabIndex = 10;
            this.currentDateTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // clientStateRereshTimer
            // 
            this.clientStateRereshTimer.Interval = 500;
            this.clientStateRereshTimer.Tick += new System.EventHandler(this.ClientStateReresh_Tick);
            // 
            // loggedInCountTextbox
            // 
            this.loggedInCountTextbox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.loggedInCountTextbox.Location = new System.Drawing.Point(13, 215);
            this.loggedInCountTextbox.Margin = new System.Windows.Forms.Padding(4);
            this.loggedInCountTextbox.Name = "loggedInCountTextbox";
            this.loggedInCountTextbox.ReadOnly = true;
            this.loggedInCountTextbox.Size = new System.Drawing.Size(312, 22);
            this.loggedInCountTextbox.TabIndex = 11;
            this.loggedInCountTextbox.Visible = false;
            // 
            // Server
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(787, 682);
            this.Controls.Add(this.loggedInCountTextbox);
            this.Controls.Add(this.currentDateTextbox);
            this.Controls.Add(this.currentTimeTextbox);
            this.Controls.Add(this.clientCountTextbox);
            this.Controls.Add(this.currentState2Textbox);
            this.Controls.Add(this.currentState1Textbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.currentSocketTextbox);
            this.Controls.Add(this.currentIPTextbox);
            this.Controls.Add(this.commandTextbox);
            this.Controls.Add(this.logTextbox);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(805, 780);
            this.MinimumSize = new System.Drawing.Size(805, 729);
            this.Name = "Server";
            this.Text = "ChatBubbleServer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox commandTextbox;
        public System.Windows.Forms.RichTextBox logTextbox;
        private System.Windows.Forms.TextBox currentIPTextbox;
        private System.Windows.Forms.TextBox currentSocketTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer updateDataTimer;
        private System.Windows.Forms.TextBox currentState1Textbox;
        private System.Windows.Forms.TextBox currentState2Textbox;
        private System.Windows.Forms.TextBox clientCountTextbox;
        private System.Windows.Forms.TextBox currentTimeTextbox;
        private System.Windows.Forms.TextBox currentDateTextbox;
        private System.Windows.Forms.Timer clientStateRereshTimer;
        private System.Windows.Forms.TextBox loggedInCountTextbox;
    }
}

