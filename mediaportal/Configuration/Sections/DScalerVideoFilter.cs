using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Win32;
using System.Runtime.InteropServices;

using DShowNET;
using DShowNET.Device;

namespace MediaPortal.Configuration.Sections
{

	public class DScalerVideoFilter : MediaPortal.Configuration.SectionSettings
	{
		private MediaPortal.UserInterface.Controls.MPGroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox tbVideoOffset;
		private System.Windows.Forms.CheckBox cbAspectRatio;
		private System.Windows.Forms.CheckBox cbForcedSubtitles;
		private System.Windows.Forms.ComboBox cbDeinterlace;
		private System.Windows.Forms.CheckBox cbSmoothing;
		private System.Windows.Forms.ComboBox cbDVBAR;
		private System.Windows.Forms.CheckBox cbHardcodePal;
		private System.Windows.Forms.ComboBox cbIDCT;
		private System.Windows.Forms.ComboBox cbColorSpace;
		private System.Windows.Forms.CheckBox cbAnalogBlanking;
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 
		/// </summary>
		public DScalerVideoFilter() : this("DScaler Video Decoder")
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public DScalerVideoFilter(string name) : base(name)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

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
      this.cbAnalogBlanking = new System.Windows.Forms.CheckBox();
      this.cbColorSpace = new System.Windows.Forms.ComboBox();
      this.label6 = new System.Windows.Forms.Label();
      this.cbIDCT = new System.Windows.Forms.ComboBox();
      this.label4 = new System.Windows.Forms.Label();
      this.cbHardcodePal = new System.Windows.Forms.CheckBox();
      this.cbDVBAR = new System.Windows.Forms.ComboBox();
      this.label2 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.tbVideoOffset = new System.Windows.Forms.TextBox();
      this.cbAspectRatio = new System.Windows.Forms.CheckBox();
      this.label5 = new System.Windows.Forms.Label();
      this.cbForcedSubtitles = new System.Windows.Forms.CheckBox();
      this.cbDeinterlace = new System.Windows.Forms.ComboBox();
      this.cbSmoothing = new System.Windows.Forms.CheckBox();
      this.label3 = new System.Windows.Forms.Label();
      this.groupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // groupBox1
      // 
      this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox1.Controls.Add(this.cbAnalogBlanking);
      this.groupBox1.Controls.Add(this.cbColorSpace);
      this.groupBox1.Controls.Add(this.label6);
      this.groupBox1.Controls.Add(this.cbIDCT);
      this.groupBox1.Controls.Add(this.label4);
      this.groupBox1.Controls.Add(this.cbHardcodePal);
      this.groupBox1.Controls.Add(this.cbDVBAR);
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.tbVideoOffset);
      this.groupBox1.Controls.Add(this.cbAspectRatio);
      this.groupBox1.Controls.Add(this.label5);
      this.groupBox1.Controls.Add(this.cbForcedSubtitles);
      this.groupBox1.Controls.Add(this.cbDeinterlace);
      this.groupBox1.Controls.Add(this.cbSmoothing);
      this.groupBox1.Controls.Add(this.label3);
      this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.groupBox1.Location = new System.Drawing.Point(0, 0);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(472, 288);
      this.groupBox1.TabIndex = 1;
      this.groupBox1.TabStop = false;
      // 
      // cbAnalogBlanking
      // 
      this.cbAnalogBlanking.Checked = true;
      this.cbAnalogBlanking.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbAnalogBlanking.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbAnalogBlanking.Location = new System.Drawing.Point(16, 224);
      this.cbAnalogBlanking.Name = "cbAnalogBlanking";
      this.cbAnalogBlanking.Size = new System.Drawing.Size(136, 16);
      this.cbAnalogBlanking.TabIndex = 31;
      this.cbAnalogBlanking.Text = "Do analog blanking";
      // 
      // cbColorSpace
      // 
      this.cbColorSpace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.cbColorSpace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbColorSpace.Items.AddRange(new object[] {
                                                      "YV12",
                                                      "YUY2"});
      this.cbColorSpace.Location = new System.Drawing.Point(168, 92);
      this.cbColorSpace.Name = "cbColorSpace";
      this.cbColorSpace.Size = new System.Drawing.Size(288, 21);
      this.cbColorSpace.TabIndex = 30;
      // 
      // label6
      // 
      this.label6.Location = new System.Drawing.Point(16, 96);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(128, 16);
      this.label6.TabIndex = 29;
      this.label6.Text = "Output colorspace:";
      // 
      // cbIDCT
      // 
      this.cbIDCT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.cbIDCT.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbIDCT.Items.AddRange(new object[] {
                                                "Reference",
                                                "MMX only",
                                                "Accelerated"});
      this.cbIDCT.Location = new System.Drawing.Point(168, 68);
      this.cbIDCT.Name = "cbIDCT";
      this.cbIDCT.Size = new System.Drawing.Size(288, 21);
      this.cbIDCT.TabIndex = 28;
      // 
      // label4
      // 
      this.label4.Location = new System.Drawing.Point(16, 72);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(128, 16);
      this.label4.TabIndex = 27;
      this.label4.Text = "IDCT to use:";
      // 
      // cbHardcodePal
      // 
      this.cbHardcodePal.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbHardcodePal.Location = new System.Drawing.Point(16, 200);
      this.cbHardcodePal.Name = "cbHardcodePal";
      this.cbHardcodePal.Size = new System.Drawing.Size(184, 16);
      this.cbHardcodePal.TabIndex = 26;
      this.cbHardcodePal.Text = "Hardcode for PAL with FFdshow";
      // 
      // cbDVBAR
      // 
      this.cbDVBAR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.cbDVBAR.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbDVBAR.Items.AddRange(new object[] {
                                                 "16:9 Display",
                                                 "4:3 Display Center cut out",
                                                 "4:3 Display Letterbox"});
      this.cbDVBAR.Location = new System.Drawing.Point(168, 44);
      this.cbDVBAR.Name = "cbDVBAR";
      this.cbDVBAR.Size = new System.Drawing.Size(288, 21);
      this.cbDVBAR.TabIndex = 25;
      // 
      // label2
      // 
      this.label2.Location = new System.Drawing.Point(16, 48);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(128, 16);
      this.label2.TabIndex = 24;
      this.label2.Text = "DVB Aspect preference:";
      // 
      // label1
      // 
      this.label1.Location = new System.Drawing.Point(156, 256);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(36, 16);
      this.label1.TabIndex = 23;
      this.label1.Text = "msec.";
      // 
      // tbVideoOffset
      // 
      this.tbVideoOffset.Location = new System.Drawing.Point(120, 252);
      this.tbVideoOffset.Name = "tbVideoOffset";
      this.tbVideoOffset.Size = new System.Drawing.Size(32, 20);
      this.tbVideoOffset.TabIndex = 22;
      this.tbVideoOffset.Text = "0";
      // 
      // cbAspectRatio
      // 
      this.cbAspectRatio.Checked = true;
      this.cbAspectRatio.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbAspectRatio.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbAspectRatio.Location = new System.Drawing.Point(16, 176);
      this.cbAspectRatio.Name = "cbAspectRatio";
      this.cbAspectRatio.Size = new System.Drawing.Size(168, 16);
      this.cbAspectRatio.TabIndex = 21;
      this.cbAspectRatio.Text = "Use accurate Aspect Ratios";
      // 
      // label5
      // 
      this.label5.Location = new System.Drawing.Point(16, 256);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(104, 16);
      this.label5.TabIndex = 20;
      this.label5.Text = "Video delay offset:";
      // 
      // cbForcedSubtitles
      // 
      this.cbForcedSubtitles.Checked = true;
      this.cbForcedSubtitles.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbForcedSubtitles.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbForcedSubtitles.Location = new System.Drawing.Point(16, 128);
      this.cbForcedSubtitles.Name = "cbForcedSubtitles";
      this.cbForcedSubtitles.Size = new System.Drawing.Size(136, 16);
      this.cbForcedSubtitles.TabIndex = 13;
      this.cbForcedSubtitles.Text = "Display Forced Subtitles";
      // 
      // cbDeinterlace
      // 
      this.cbDeinterlace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.cbDeinterlace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbDeinterlace.Items.AddRange(new object[] {
                                                       "Automatic",
                                                       "Force Weave",
                                                       "Force Bob"});
      this.cbDeinterlace.Location = new System.Drawing.Point(168, 20);
      this.cbDeinterlace.Name = "cbDeinterlace";
      this.cbDeinterlace.Size = new System.Drawing.Size(288, 21);
      this.cbDeinterlace.TabIndex = 12;
      // 
      // cbSmoothing
      // 
      this.cbSmoothing.Checked = true;
      this.cbSmoothing.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbSmoothing.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbSmoothing.Location = new System.Drawing.Point(16, 152);
      this.cbSmoothing.Name = "cbSmoothing";
      this.cbSmoothing.Size = new System.Drawing.Size(136, 16);
      this.cbSmoothing.TabIndex = 11;
      this.cbSmoothing.Text = "3:2 Playback smoothing";
      // 
      // label3
      // 
      this.label3.Location = new System.Drawing.Point(16, 24);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(96, 16);
      this.label3.TabIndex = 8;
      this.label3.Text = "Deinterlace mode:";
      // 
      // DScalerVideoFilter
      // 
      this.Controls.Add(this.groupBox1);
      this.Name = "DScalerVideoFilter";
      this.Size = new System.Drawing.Size(472, 408);
      this.groupBox1.ResumeLayout(false);
      this.ResumeLayout(false);

    }
		#endregion

		public override void LoadSettings()
		{
			RegistryKey hkcu = Registry.CurrentUser;
			RegistryKey subkey = hkcu.CreateSubKey(@"Software\DScaler5\MpegVideo Filter");
			if (subkey!=null)
			{
				try
				{
					Int32 regValue=(Int32)subkey.GetValue("3:2 playback smoothing");
					if (regValue==1) cbSmoothing.Checked=true;
					else cbSmoothing.Checked=false;

					regValue=(Int32)subkey.GetValue("Display Forced Subtitles");
					if (regValue==1) cbForcedSubtitles.Checked=true;
					else cbForcedSubtitles.Checked=false;

					regValue=(Int32)subkey.GetValue("Use accurate aspect ratios");
					if (regValue==1) cbAspectRatio.Checked=true;
					else cbAspectRatio.Checked=false;
					
					regValue=(Int32)subkey.GetValue("Hardcode for PAL with ffdshow");
					if (regValue==1) cbHardcodePal.Checked=true;
					else cbHardcodePal.Checked=false;
					
					regValue=(Int32)subkey.GetValue("Do Analog Blanking");
					if (regValue==1) cbAnalogBlanking.Checked=true;
					else cbAnalogBlanking.Checked=false;
					
					regValue=(Int32)subkey.GetValue("Video Delay");
					tbVideoOffset.Text=regValue.ToString();

					regValue=(Int32)subkey.GetValue("Deinterlace Mode");
					cbDeinterlace.SelectedIndex=regValue;

					regValue=(Int32)subkey.GetValue("DVB Aspect Preferences");
					cbDVBAR.SelectedIndex=regValue;

					regValue=(Int32)subkey.GetValue("IDCT to Use");
					cbIDCT.SelectedIndex=regValue;

					regValue=(Int32)subkey.GetValue("Colour space to output");
					cbColorSpace.SelectedIndex=regValue;

				}
				catch(Exception )
				{
				}
				finally
				{
					subkey.Close();
				}
			}
		}

		public override void SaveSettings()
		{
			RegistryKey hkcu = Registry.CurrentUser;
			RegistryKey subkey = hkcu.CreateSubKey(@"Software\DScaler5\MpegVideo Filter");
			if (subkey!=null)
			{
				Int32 regValue;
				if (cbSmoothing.Checked) regValue=1;
				else regValue=0;
				subkey.SetValue("3:2 playback smoothing",regValue);

				if (cbForcedSubtitles.Checked) regValue=1;
				else regValue=0;
				subkey.SetValue("Display Forced Subtitles",regValue);

				if (cbAspectRatio.Checked) regValue=1;
				else regValue=0;
				subkey.SetValue("Use accurate aspect ratios",regValue);

				if (cbHardcodePal.Checked) regValue=1;
				else regValue=0;
				subkey.SetValue("Hardcode for PAL with ffdshow",regValue);
		
				if (cbAnalogBlanking.Checked) regValue=1;
				else regValue=0;
				subkey.SetValue("Do Analog Blanking",regValue);
					
				regValue=(Int32)Int32.Parse(tbVideoOffset.Text);
				subkey.SetValue("Video Delay",regValue);

				subkey.SetValue("Deinterlace Mode",(Int32)cbDeinterlace.SelectedIndex);

				subkey.SetValue("DVB Aspect Preferences",(Int32)cbDVBAR.SelectedIndex);

				subkey.SetValue("IDCT to Use", (Int32)cbIDCT.SelectedIndex);

				subkey.SetValue("Colour space to output", (Int32)cbColorSpace.SelectedIndex);
				subkey.Close();
			}
		}


	}
}

