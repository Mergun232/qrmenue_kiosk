namespace QRMENUE
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelLogin = new System.Windows.Forms.Panel();
            this.gradientButton = new QRMENUE.GradientButton();
            this.btnSettings = new System.Windows.Forms.Button();
            this.chkRememberMe = new System.Windows.Forms.CheckBox();
            this.picRememberMe = new System.Windows.Forms.PictureBox();
            this.lblRememberMe = new System.Windows.Forms.Label();
            this.pnlPasswordInput = new QRMENUE.RoundedInputPanel();
            this.picEyeIcon = new System.Windows.Forms.PictureBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.picIconKey = new System.Windows.Forms.PictureBox();
            this.pnlUserInput = new QRMENUE.RoundedInputPanel();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.picIconUser = new System.Windows.Forms.PictureBox();
            this.pnlCompanyInput = new QRMENUE.RoundedInputPanel();
            this.txtCompanyCode = new System.Windows.Forms.TextBox();
            this.picIconCompany = new System.Windows.Forms.PictureBox();
            this.label4 = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.btnExit = new QRMENUE.GradientButton();
            ((System.ComponentModel.ISupportInitialize)(this.picRememberMe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEyeIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconKey)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconUser)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconCompany)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.panelLogin.SuspendLayout();
            this.pnlPasswordInput.SuspendLayout();
            this.pnlUserInput.SuspendLayout();
            this.pnlCompanyInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelLogin
            // 
            this.panelLogin.BackColor = System.Drawing.Color.White;
            this.panelLogin.Controls.Add(this.btnExit);
            this.panelLogin.Controls.Add(this.gradientButton);
            this.panelLogin.Controls.Add(this.btnSettings);
            this.panelLogin.Controls.Add(this.picRememberMe);
            this.panelLogin.Controls.Add(this.lblRememberMe);
            this.panelLogin.Controls.Add(this.chkRememberMe);
            this.panelLogin.Controls.Add(this.pnlPasswordInput);
            this.panelLogin.Controls.Add(this.pnlUserInput);
            this.panelLogin.Controls.Add(this.pnlCompanyInput);
            this.panelLogin.Controls.Add(this.label4);
            this.panelLogin.Controls.Add(this.picLogo);
            this.panelLogin.Location = new System.Drawing.Point(190, 0);
            this.panelLogin.Name = "panelLogin";
            this.panelLogin.Size = new System.Drawing.Size(420, 600);
            this.panelLogin.TabIndex = 0;
            // 
            // gradientButton
            // 
            this.gradientButton.Location = new System.Drawing.Point(32, 468);
            this.gradientButton.Name = "gradientButton";
            this.gradientButton.Size = new System.Drawing.Size(356, 48);
            this.gradientButton.TabIndex = 9;
            this.gradientButton.Text = "Giriş Yap";
            this.gradientButton.Click += new System.EventHandler(this.gradientButton_Click);
            // 
            // txtCompanyCode GotFocus
            // 
            this.txtCompanyCode.GotFocus += new System.EventHandler(this.Input_GotFocus);
            // 
            // txtUserName GotFocus
            // 
            this.txtUserName.GotFocus += new System.EventHandler(this.Input_GotFocus);
            // 
            // txtPassword GotFocus
            // 
            this.txtPassword.GotFocus += new System.EventHandler(this.Input_GotFocus);
            // 
            // chkRememberMe (gizli - sadece Checked state için)
            // 
            this.chkRememberMe.Location = new System.Drawing.Point(0, 0);
            this.chkRememberMe.Name = "chkRememberMe";
            this.chkRememberMe.Size = new System.Drawing.Size(1, 1);
            this.chkRememberMe.TabIndex = 8;
            this.chkRememberMe.UseVisualStyleBackColor = true;
            this.chkRememberMe.Visible = false;
            // 
            // picRememberMe
            // 
            this.picRememberMe.BackColor = System.Drawing.Color.Transparent;
            this.picRememberMe.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picRememberMe.Location = new System.Drawing.Point(32, 388);
            this.picRememberMe.Name = "picRememberMe";
            this.picRememberMe.Size = new System.Drawing.Size(36, 36);
            this.picRememberMe.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picRememberMe.TabIndex = 12;
            this.picRememberMe.TabStop = false;
            this.picRememberMe.Click += new System.EventHandler(this.picRememberMe_Click);
            // 
            // lblRememberMe
            // 
            this.lblRememberMe.AutoSize = true;
            this.lblRememberMe.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblRememberMe.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblRememberMe.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(70)))));
            this.lblRememberMe.Location = new System.Drawing.Point(74, 396);
            this.lblRememberMe.Name = "lblRememberMe";
            this.lblRememberMe.Size = new System.Drawing.Size(92, 21);
            this.lblRememberMe.TabIndex = 13;
            this.lblRememberMe.Text = "Beni hatırla";
            this.lblRememberMe.Click += new System.EventHandler(this.picRememberMe_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(248)))));
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe MDL2 Assets", 17F);
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(75)))), ((int)(((byte)(88)))));
            this.btnSettings.Location = new System.Drawing.Point(364, 386);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(36, 36);
            this.btnSettings.TabIndex = 14;
            this.btnSettings.TabStop = false;
            this.btnSettings.Text = "\uE713";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // pnlPasswordInput
            // 
            this.pnlPasswordInput.Controls.Add(this.picEyeIcon);
            this.pnlPasswordInput.Controls.Add(this.txtPassword);
            this.pnlPasswordInput.Controls.Add(this.picIconKey);
            this.pnlPasswordInput.Location = new System.Drawing.Point(32, 332);
            this.pnlPasswordInput.Name = "pnlPasswordInput";
            this.pnlPasswordInput.Size = new System.Drawing.Size(356, 44);
            this.pnlPasswordInput.TabIndex = 7;
            // 
            // picEyeIcon
            // 
            this.picEyeIcon.BackColor = System.Drawing.Color.Transparent;
            this.picEyeIcon.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picEyeIcon.Location = new System.Drawing.Point(310, 10);
            this.picEyeIcon.Name = "picEyeIcon";
            this.picEyeIcon.Size = new System.Drawing.Size(32, 24);
            this.picEyeIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picEyeIcon.TabIndex = 2;
            this.picEyeIcon.TabStop = false;
            this.picEyeIcon.Click += new System.EventHandler(this.picEyeIcon_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPassword.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(55)))));
            this.txtPassword.Location = new System.Drawing.Point(46, 12);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.Size = new System.Drawing.Size(292, 20);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.TextChanged += new System.EventHandler(this.txtPassword_TextChanged);
            // 
            // picIconKey
            // 
            this.picIconKey.BackColor = System.Drawing.Color.Transparent;
            this.picIconKey.Location = new System.Drawing.Point(12, 10);
            this.picIconKey.Name = "picIconKey";
            this.picIconKey.Size = new System.Drawing.Size(24, 24);
            this.picIconKey.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIconKey.TabIndex = 0;
            this.picIconKey.TabStop = false;
            // 
            // pnlUserInput
            // 
            this.pnlUserInput.Controls.Add(this.txtUserName);
            this.pnlUserInput.Controls.Add(this.picIconUser);
            this.pnlUserInput.Location = new System.Drawing.Point(32, 272);
            this.pnlUserInput.Name = "pnlUserInput";
            this.pnlUserInput.Size = new System.Drawing.Size(356, 44);
            this.pnlUserInput.TabIndex = 6;
            // 
            // txtUserName
            // 
            this.txtUserName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
            this.txtUserName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUserName.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtUserName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(55)))));
            this.txtUserName.Location = new System.Drawing.Point(46, 12);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(298, 20);
            this.txtUserName.TabIndex = 1;
            // 
            // picIconUser
            // 
            this.picIconUser.BackColor = System.Drawing.Color.Transparent;
            this.picIconUser.Location = new System.Drawing.Point(12, 10);
            this.picIconUser.Name = "picIconUser";
            this.picIconUser.Size = new System.Drawing.Size(24, 24);
            this.picIconUser.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIconUser.TabIndex = 0;
            this.picIconUser.TabStop = false;
            // 
            // pnlCompanyInput
            // 
            this.pnlCompanyInput.Controls.Add(this.txtCompanyCode);
            this.pnlCompanyInput.Controls.Add(this.picIconCompany);
            this.pnlCompanyInput.Location = new System.Drawing.Point(32, 212);
            this.pnlCompanyInput.Name = "pnlCompanyInput";
            this.pnlCompanyInput.Size = new System.Drawing.Size(356, 44);
            this.pnlCompanyInput.TabIndex = 5;
            // 
            // txtCompanyCode
            // 
            this.txtCompanyCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCompanyCode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
            this.txtCompanyCode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtCompanyCode.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtCompanyCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(55)))));
            this.txtCompanyCode.Location = new System.Drawing.Point(46, 12);
            this.txtCompanyCode.Name = "txtCompanyCode";
            this.txtCompanyCode.Size = new System.Drawing.Size(298, 20);
            this.txtCompanyCode.TabIndex = 1;
            this.txtCompanyCode.TextChanged += new System.EventHandler(this.txtCompanyCode_TextChanged);
            // 
            // picIconCompany
            // 
            this.picIconCompany.BackColor = System.Drawing.Color.Transparent;
            this.picIconCompany.Location = new System.Drawing.Point(12, 10);
            this.picIconCompany.Name = "picIconCompany";
            this.picIconCompany.Size = new System.Drawing.Size(24, 24);
            this.picIconCompany.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIconCompany.TabIndex = 0;
            this.picIconCompany.TabStop = false;
            // 
            // label4
            // 
            this.label4.AutoSize = false;
            this.label4.Font = new System.Drawing.Font("Segoe UI Semibold", 16F);
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(55)))));
            this.label4.Location = new System.Drawing.Point(32, 162);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(356, 30);
            this.label4.TabIndex = 0;
            this.label4.Text = "KIOSK GİRİŞ YAP";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Location = new System.Drawing.Point(80, 28);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(260, 72);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 11;
            this.picLogo.TabStop = false;
            // 
            // btnExit
            // 
            this.btnExit.IsRedTheme = true;
            this.btnExit.Location = new System.Drawing.Point(32, 528);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(356, 48);
            this.btnExit.TabIndex = 10;
            this.btnExit.Text = "Çıkış";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(14)))), ((int)(((byte)(103)))));
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.panelLogin);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Giriş";
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.LoginForm_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LoginForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LoginForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LoginForm_MouseUp);
            this.Resize += new System.EventHandler(this.LoginForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picRememberMe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picEyeIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconKey)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconUser)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picIconCompany)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.panelLogin.ResumeLayout(false);
            this.panelLogin.PerformLayout();
            this.pnlPasswordInput.ResumeLayout(false);
            this.pnlPasswordInput.PerformLayout();
            this.pnlUserInput.ResumeLayout(false);
            this.pnlUserInput.PerformLayout();
            this.pnlCompanyInput.ResumeLayout(false);
            this.pnlCompanyInput.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelLogin;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox picLogo;
        private RoundedInputPanel pnlCompanyInput;
        private System.Windows.Forms.TextBox txtCompanyCode;
        private System.Windows.Forms.PictureBox picIconCompany;
        private RoundedInputPanel pnlUserInput;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.PictureBox picIconUser;
        private RoundedInputPanel pnlPasswordInput;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.PictureBox picIconKey;
        private System.Windows.Forms.PictureBox picEyeIcon;
        private System.Windows.Forms.CheckBox chkRememberMe;
        private System.Windows.Forms.PictureBox picRememberMe;
        private System.Windows.Forms.Label lblRememberMe;
        private GradientButton gradientButton;
        private GradientButton btnExit;
        private System.Windows.Forms.Button btnSettings;
    }
}
