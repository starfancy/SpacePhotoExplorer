namespace StudioFancy.SpacePhotoBox
{
    partial class PhotoBox
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

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.bitmapTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // bitmapTimer
            // 
            this.bitmapTimer.Enabled = true;
            this.bitmapTimer.Interval = 10;
            this.bitmapTimer.Tick += new System.EventHandler(this.bitmapTimer_Tick);
            // 
            // PhotoBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Name = "PhotoBox";
            this.Load += new System.EventHandler(this.PhotoBox_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PhotoBox_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PhotoBox_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PhotoBox_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PhotoBox_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer bitmapTimer;
    }
}
