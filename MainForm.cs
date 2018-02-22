using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StudioFancy.SpacePhotoExplorer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Width = (int)(0.75 * Screen.PrimaryScreen.Bounds.Width);
            this.Height = (int)(0.75 * Screen.PrimaryScreen.Bounds.Height);
        }

        private void toolBtnOpenDir_Click(object sender, EventArgs e)
        {
            folderDialog.SelectedPath = @"E:\Photo\100_1030\Small";
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = folderDialog.SelectedPath;
                photoBox.OpenPhotoByPath(path);
            }
        }

        private void toolButtonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Space's photo explorer. version 0.1    2011");
        }


    }
}
