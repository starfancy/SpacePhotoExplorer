namespace StudioFancy.SpacePhotoExplorer
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolBtnOpenDir = new System.Windows.Forms.ToolStripButton();
            this.folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.photoBox = new StudioFancy.SpacePhotoBox.PhotoBox();
            this.toolButtonAbout = new System.Windows.Forms.ToolStripButton();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolBtnOpenDir,
            this.toolButtonAbout});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(296, 25);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "工具栏";
            // 
            // toolBtnOpenDir
            // 
            this.toolBtnOpenDir.Image = ((System.Drawing.Image)(resources.GetObject("toolBtnOpenDir.Image")));
            this.toolBtnOpenDir.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolBtnOpenDir.Name = "toolBtnOpenDir";
            this.toolBtnOpenDir.Size = new System.Drawing.Size(87, 22);
            this.toolBtnOpenDir.Text = "打开文件夹";
            this.toolBtnOpenDir.Click += new System.EventHandler(this.toolBtnOpenDir_Click);
            // 
            // photoBox
            // 
            this.photoBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.photoBox.BackColor = System.Drawing.Color.Black;
            this.photoBox.Location = new System.Drawing.Point(2, 28);
            this.photoBox.Name = "photoBox";
            this.photoBox.Size = new System.Drawing.Size(294, 261);
            this.photoBox.TabIndex = 0;
            // 
            // toolButtonAbout
            // 
            this.toolButtonAbout.Image = ((System.Drawing.Image)(resources.GetObject("toolButtonAbout.Image")));
            this.toolButtonAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButtonAbout.Name = "toolButtonAbout";
            this.toolButtonAbout.Size = new System.Drawing.Size(51, 22);
            this.toolButtonAbout.Text = "关于";
            this.toolButtonAbout.Click += new System.EventHandler(this.toolButtonAbout_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(296, 291);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.photoBox);
            this.Name = "MainForm";
            this.Text = "Space\'s Photo Exploer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SpacePhotoBox.PhotoBox photoBox;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolBtnOpenDir;
        private System.Windows.Forms.FolderBrowserDialog folderDialog;
        private System.Windows.Forms.ToolStripButton toolButtonAbout;
    }
}

