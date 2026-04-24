namespace Qrmenue
{
    partial class MainForm
    {
        /// <summary>
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.version100ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.webSocketServerRunningOn22444ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pavoPosTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.autoStartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tmrFicheCheck = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.tmrStateInformer = new System.Windows.Forms.Timer(this.components);
            this.trayMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.trayMenu;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Qiox";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
            // 
            // trayMenu
            // 
            this.trayMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.version100ToolStripMenuItem,
            this.webSocketServerRunningOn22444ToolStripMenuItem,
            this.pavoPosTestToolStripMenuItem,
            this.toolStripMenuItem1,
            this.autoStartToolStripMenuItem,
            this.quitToolStripMenuItem});
            this.trayMenu.Name = "trayMenu";
            this.trayMenu.Size = new System.Drawing.Size(315, 114);
            // 
            // version100ToolStripMenuItem
            // 
            this.version100ToolStripMenuItem.Enabled = false;
            this.version100ToolStripMenuItem.Name = "version100ToolStripMenuItem";
            this.version100ToolStripMenuItem.Size = new System.Drawing.Size(314, 26);
            this.version100ToolStripMenuItem.Text = "Version: 2.0.0";
            // 
            // webSocketServerRunningOn22444ToolStripMenuItem
            // 
            this.webSocketServerRunningOn22444ToolStripMenuItem.Checked = true;
            this.webSocketServerRunningOn22444ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.webSocketServerRunningOn22444ToolStripMenuItem.Enabled = false;
            this.webSocketServerRunningOn22444ToolStripMenuItem.Name = "webSocketServerRunningOn22444ToolStripMenuItem";
            this.webSocketServerRunningOn22444ToolStripMenuItem.Size = new System.Drawing.Size(314, 26);
            this.webSocketServerRunningOn22444ToolStripMenuItem.Text = "WebSocket server running on 22444";
            // 
            // pavoPosTestToolStripMenuItem
            // 
            this.pavoPosTestToolStripMenuItem.Name = "pavoPosTestToolStripMenuItem";
            this.pavoPosTestToolStripMenuItem.Size = new System.Drawing.Size(314, 26);
            this.pavoPosTestToolStripMenuItem.Text = "Pavo POS Test";
            this.pavoPosTestToolStripMenuItem.Click += new System.EventHandler(this.pavoPosTestToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(311, 6);
            // 
            // autoStartToolStripMenuItem
            // 
            this.autoStartToolStripMenuItem.Checked = true;
            this.autoStartToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoStartToolStripMenuItem.Name = "autoStartToolStripMenuItem";
            this.autoStartToolStripMenuItem.Size = new System.Drawing.Size(314, 26);
            this.autoStartToolStripMenuItem.Text = "AutoStart";
            this.autoStartToolStripMenuItem.Click += new System.EventHandler(this.autoStartToolStripMenuItem_Click);
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(314, 26);
            this.quitToolStripMenuItem.Text = "Quit";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.quitToolStripMenuItem_Click);
            // 
            // tmrFicheCheck
            // 
            this.tmrFicheCheck.Interval = 1000;
            this.tmrFicheCheck.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(141, 81);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Working";
            // 
            // tmrStateInformer
            // 
            this.tmrStateInformer.Interval = 1000;
            this.tmrStateInformer.Tick += new System.EventHandler(this.tmrStateInformer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(356, 202);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "Qiox";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.trayMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem version100ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem webSocketServerRunningOn22444ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pavoPosTestToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem autoStartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.Timer tmrFicheCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer tmrStateInformer;
    }
}

