using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MediaPortal.Configuration.Sections
{
	public class Project : MediaPortal.Configuration.SectionSettings
	{
		private MediaPortal.UserInterface.Controls.MPGroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.LinkLabel linkLabel3;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.LinkLabel linkLabel4;
		private System.ComponentModel.IContainer components = null;

		public Project() : this("Project")
		{
		}

		public Project(string name) : base(name)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.linkLabel4 = new System.Windows.Forms.LinkLabel();
			this.label7 = new System.Windows.Forms.Label();
			this.linkLabel3 = new System.Windows.Forms.LinkLabel();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.label3 = new System.Windows.Forms.Label();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.groupBox1.Location = new System.Drawing.Point(8, 8);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(440, 120);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Information";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(8, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(424, 88);
			this.label1.TabIndex = 0;
			this.label1.Text = @"MediaPortal is an OpenSource project, hosted at SourceForge, that will turn your home computer into a fully fledged multi media center (htpc). It allows you to listen to your favorite music and radio, watch your video's and DVD's, view, schedule and record live tv and much more!";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.linkLabel4);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.linkLabel3);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.linkLabel2);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.linkLabel1);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.groupBox2.Location = new System.Drawing.Point(8, 136);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(440, 232);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Contact";
			// 
			// linkLabel4
			// 
			this.linkLabel4.Location = new System.Drawing.Point(24, 160);
			this.linkLabel4.Name = "linkLabel4";
			this.linkLabel4.Size = new System.Drawing.Size(320, 16);
			this.linkLabel4.TabIndex = 21;
			this.linkLabel4.TabStop = true;
			this.linkLabel4.Text = "http://sourceforge.net/projects/mediaportal";
			this.linkLabel4.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel4_LinkClicked);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(16, 144);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(100, 15);
			this.label7.TabIndex = 20;
			this.label7.Text = "Sourceforge:";
			// 
			// linkLabel3
			// 
			this.linkLabel3.Location = new System.Drawing.Point(24, 120);
			this.linkLabel3.Name = "linkLabel3";
			this.linkLabel3.Size = new System.Drawing.Size(320, 16);
			this.linkLabel3.TabIndex = 19;
			this.linkLabel3.TabStop = true;
			this.linkLabel3.Text = "http://www.maisenbachers.de/dokuw/";
			this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel3_LinkClicked);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(16, 105);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(100, 15);
			this.label6.TabIndex = 18;
			this.label6.Text = "Wiki:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(24, 200);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(232, 16);
			this.label5.TabIndex = 17;
			this.label5.Text = "IRC network:EFNet  channel: #MediaPortal";
			this.label5.Click += new System.EventHandler(this.label5_Click);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 184);
			this.label4.Name = "label4";
			this.label4.TabIndex = 16;
			this.label4.Text = "IRC";
			this.label4.Click += new System.EventHandler(this.label4_Click);
			// 
			// linkLabel2
			// 
			this.linkLabel2.Location = new System.Drawing.Point(24, 80);
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.Size = new System.Drawing.Size(320, 16);
			this.linkLabel2.TabIndex = 15;
			this.linkLabel2.TabStop = true;
			this.linkLabel2.Text = "http://nolanparty.com/mediaportal.sourceforge.net/phpBB2/";
			this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 64);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(128, 16);
			this.label3.TabIndex = 14;
			this.label3.Text = "Forums:";
			this.label3.Click += new System.EventHandler(this.label3_Click);
			// 
			// linkLabel1
			// 
			this.linkLabel1.Location = new System.Drawing.Point(24, 40);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(224, 16);
			this.linkLabel1.TabIndex = 13;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "http://mediaportal.sourceforge.net";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(152, 16);
			this.label2.TabIndex = 12;
			this.label2.Text = "Homepage:";
			// 
			// Project
			// 
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "Project";
			this.Size = new System.Drawing.Size(456, 448);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void label5_Click(object sender, System.EventArgs e)
		{
		
		}

		private void label4_Click(object sender, System.EventArgs e)
		{
		
		}

		private void label3_Click(object sender, System.EventArgs e)
		{
		
		}

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if(linkLabel1.Text==null)
				return;
			if(linkLabel1.Text.Length>0)
			{
				System.Diagnostics.ProcessStartInfo sInfo = new System.Diagnostics.ProcessStartInfo(linkLabel1.Text);
				System.Diagnostics.Process.Start(sInfo);
			}
		}

		private void linkLabel2_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if(linkLabel2.Text==null)
				return;
			if(linkLabel2.Text.Length>0)
			{
				System.Diagnostics.ProcessStartInfo sInfo = new System.Diagnostics.ProcessStartInfo(linkLabel2.Text);
				System.Diagnostics.Process.Start(sInfo);
			}
		}

		private void linkLabel3_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if(linkLabel3.Text==null)
				return;
			if(linkLabel3.Text.Length>0)
			{
				System.Diagnostics.ProcessStartInfo sInfo = new System.Diagnostics.ProcessStartInfo(linkLabel3.Text);
				System.Diagnostics.Process.Start(sInfo);
			}
		}

			private void linkLabel4_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
			{
				if(linkLabel4.Text==null)
					return;
				if(linkLabel4.Text.Length>0)
				{
					System.Diagnostics.ProcessStartInfo sInfo = new System.Diagnostics.ProcessStartInfo(linkLabel4.Text);
					System.Diagnostics.Process.Start(sInfo);

				}
			}

	}
}

