﻿namespace MpeCore.Classes.SectionPanel
{
    partial class BaseVerticalLayout
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
          System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseVerticalLayout));
          this.pictureBox1 = new System.Windows.Forms.PictureBox();
          this.panel2 = new System.Windows.Forms.Panel();
          this.groupBox1 = new System.Windows.Forms.GroupBox();
          ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
          this.panel2.SuspendLayout();
          this.SuspendLayout();
          // 
          // pictureBox1
          // 
          this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
          this.pictureBox1.Location = new System.Drawing.Point(0, 0);
          this.pictureBox1.Name = "pictureBox1";
          this.pictureBox1.Size = new System.Drawing.Size(168, 298);
          this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
          this.pictureBox1.TabIndex = 5;
          this.pictureBox1.TabStop = false;
          // 
          // panel2
          // 
          this.panel2.Controls.Add(this.pictureBox1);
          this.panel2.Location = new System.Drawing.Point(0, 0);
          this.panel2.Name = "panel2";
          this.panel2.Size = new System.Drawing.Size(168, 298);
          this.panel2.TabIndex = 6;
          // 
          // groupBox1
          // 
          this.groupBox1.Location = new System.Drawing.Point(-12, 296);
          this.groupBox1.Margin = new System.Windows.Forms.Padding(0);
          this.groupBox1.Name = "groupBox1";
          this.groupBox1.Padding = new System.Windows.Forms.Padding(0);
          this.groupBox1.Size = new System.Drawing.Size(519, 90);
          this.groupBox1.TabIndex = 7;
          this.groupBox1.TabStop = false;
          // 
          // BaseVerticalLayout
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.ClientSize = new System.Drawing.Size(495, 350);
          this.Controls.Add(this.groupBox1);
          this.Controls.Add(this.panel2);
          this.Name = "BaseVerticalLayout";
          this.Text = "BaseVerticalLayout";
          this.Load += new System.EventHandler(this.Base_Load);
          ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
          this.panel2.ResumeLayout(false);
          this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel2;
        public System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}