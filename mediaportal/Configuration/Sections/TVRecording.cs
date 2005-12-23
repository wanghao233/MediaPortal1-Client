/* 
 *	Copyright (C) 2005 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Globalization;
using MediaPortal.Util;

namespace MediaPortal.Configuration.Sections
{
  public class TVRecording : MediaPortal.Configuration.SectionSettings
  {
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox1;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox startTextBox;
    private System.Windows.Forms.TextBox endTextBox;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    private System.Windows.Forms.CheckBox cbDeleteWatchedShows;
    private System.Windows.Forms.CheckBox cbAddRecordingsToMovie;
    private GroupBox groupBox2;
    private TextBox textBoxMovies;
    private Label label39;
    private Label label38;
    private TextBox textBoxSeries;
    private Label labelSampleMovies;
    private Label label6;
    private Label labelSampleSeries;
    private Label label8;
    private Label label7;
    private Label label5;
    private Label label10;
    private Label label13;
    private Label label12;
    private Label label11;
    private Label label9;
    private System.ComponentModel.IContainer components = null;

    public TVRecording()
      : this("Recording")
    {
    }

    public TVRecording(string name)
      : base(name)
    {
      // This call is required by the Windows Form Designer.
      InitializeComponent();

      // TODO: Add any initialization after the InitializeComponent call
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (components != null)
        {
          components.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    #region Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.groupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.cbAddRecordingsToMovie = new System.Windows.Forms.CheckBox();
      this.endTextBox = new System.Windows.Forms.TextBox();
      this.startTextBox = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.cbDeleteWatchedShows = new System.Windows.Forms.CheckBox();
      this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.label13 = new System.Windows.Forms.Label();
      this.label12 = new System.Windows.Forms.Label();
      this.label11 = new System.Windows.Forms.Label();
      this.label10 = new System.Windows.Forms.Label();
      this.label5 = new System.Windows.Forms.Label();
      this.label9 = new System.Windows.Forms.Label();
      this.label7 = new System.Windows.Forms.Label();
      this.labelSampleSeries = new System.Windows.Forms.Label();
      this.label8 = new System.Windows.Forms.Label();
      this.labelSampleMovies = new System.Windows.Forms.Label();
      this.label6 = new System.Windows.Forms.Label();
      this.textBoxMovies = new System.Windows.Forms.TextBox();
      this.label39 = new System.Windows.Forms.Label();
      this.label38 = new System.Windows.Forms.Label();
      this.textBoxSeries = new System.Windows.Forms.TextBox();
      this.groupBox1.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.SuspendLayout();
      // 
      // groupBox1
      // 
      this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox1.Controls.Add(this.cbAddRecordingsToMovie);
      this.groupBox1.Controls.Add(this.endTextBox);
      this.groupBox1.Controls.Add(this.startTextBox);
      this.groupBox1.Controls.Add(this.label4);
      this.groupBox1.Controls.Add(this.label3);
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.cbDeleteWatchedShows);
      this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.groupBox1.Location = new System.Drawing.Point(0, 0);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(472, 112);
      this.groupBox1.TabIndex = 0;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Settings";
      // 
      // cbAddRecordingsToMovie
      // 
      this.cbAddRecordingsToMovie.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbAddRecordingsToMovie.Location = new System.Drawing.Point(272, 80);
      this.cbAddRecordingsToMovie.Name = "cbAddRecordingsToMovie";
      this.cbAddRecordingsToMovie.Size = new System.Drawing.Size(184, 16);
      this.cbAddRecordingsToMovie.TabIndex = 7;
      this.cbAddRecordingsToMovie.Text = "Add recordings to movie database";
      // 
      // endTextBox
      // 
      this.endTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.endTextBox.Location = new System.Drawing.Point(112, 44);
      this.endTextBox.MaxLength = 3;
      this.endTextBox.Name = "endTextBox";
      this.endTextBox.Size = new System.Drawing.Size(176, 20);
      this.endTextBox.TabIndex = 4;
      this.endTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.endTextBox_KeyPress);
      // 
      // startTextBox
      // 
      this.startTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.startTextBox.Location = new System.Drawing.Point(112, 20);
      this.startTextBox.MaxLength = 3;
      this.startTextBox.Name = "startTextBox";
      this.startTextBox.Size = new System.Drawing.Size(176, 20);
      this.startTextBox.TabIndex = 1;
      this.startTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.startTextBox_KeyPress);
      // 
      // label4
      // 
      this.label4.Location = new System.Drawing.Point(16, 48);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(88, 16);
      this.label4.TabIndex = 3;
      this.label4.Text = "Stop recording";
      // 
      // label3
      // 
      this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.label3.Location = new System.Drawing.Point(296, 48);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(160, 16);
      this.label3.TabIndex = 5;
      this.label3.Text = "minute(s) after program ends.";
      // 
      // label2
      // 
      this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.label2.Location = new System.Drawing.Point(296, 24);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(168, 16);
      this.label2.TabIndex = 2;
      this.label2.Text = "minute(s) before program starts.";
      // 
      // label1
      // 
      this.label1.Location = new System.Drawing.Point(16, 24);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(88, 16);
      this.label1.TabIndex = 0;
      this.label1.Text = "Start recording";
      // 
      // cbDeleteWatchedShows
      // 
      this.cbDeleteWatchedShows.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cbDeleteWatchedShows.Location = new System.Drawing.Point(16, 80);
      this.cbDeleteWatchedShows.Name = "cbDeleteWatchedShows";
      this.cbDeleteWatchedShows.Size = new System.Drawing.Size(232, 16);
      this.cbDeleteWatchedShows.TabIndex = 6;
      this.cbDeleteWatchedShows.Text = "Automaticly delete recordings after watching";
      // 
      // groupBox2
      // 
      this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox2.Controls.Add(this.label13);
      this.groupBox2.Controls.Add(this.label12);
      this.groupBox2.Controls.Add(this.label11);
      this.groupBox2.Controls.Add(this.label10);
      this.groupBox2.Controls.Add(this.label5);
      this.groupBox2.Controls.Add(this.label9);
      this.groupBox2.Controls.Add(this.label7);
      this.groupBox2.Controls.Add(this.labelSampleSeries);
      this.groupBox2.Controls.Add(this.label8);
      this.groupBox2.Controls.Add(this.labelSampleMovies);
      this.groupBox2.Controls.Add(this.label6);
      this.groupBox2.Controls.Add(this.textBoxMovies);
      this.groupBox2.Controls.Add(this.label39);
      this.groupBox2.Controls.Add(this.label38);
      this.groupBox2.Controls.Add(this.textBoxSeries);
      this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.groupBox2.Location = new System.Drawing.Point(0, 120);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(472, 288);
      this.groupBox2.TabIndex = 1;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Custom Paths and Filenames";
      // 
      // label13
      // 
      this.label13.AutoSize = true;
      this.label13.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label13.Location = new System.Drawing.Point(388, 24);
      this.label13.Name = "label13";
      this.label13.Size = new System.Drawing.Size(13, 130);
      this.label13.TabIndex = 16;
      this.label13.Text = "=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=";
      // 
      // label12
      // 
      this.label12.AutoSize = true;
      this.label12.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label12.Location = new System.Drawing.Point(400, 24);
      this.label12.Name = "label12";
      this.label12.Size = new System.Drawing.Size(66, 130);
      this.label12.TabIndex = 15;
      this.label12.Text = "start day\r\nstart month\r\nstart year\r\nstart hours\r\nstart minutes\r\nend day\r\nend mont" +
          "h\r\nend year\r\nend hours\r\nend minutes";
      // 
      // label11
      // 
      this.label11.AutoSize = true;
      this.label11.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label11.Location = new System.Drawing.Point(311, 24);
      this.label11.Name = "label11";
      this.label11.Size = new System.Drawing.Size(72, 130);
      this.label11.TabIndex = 14;
      this.label11.Text = "%startday%\r\n%startmonth%\r\n%startyear%\r\n%starthh%\r\n%startmm%\r\n%endday%\r\n%endmonth%" +
          "\r\n%endyear%\r\n%endhh%\r\n%endmm%";
      this.label11.TextAlign = System.Drawing.ContentAlignment.TopRight;
      // 
      // label10
      // 
      this.label10.AutoSize = true;
      this.label10.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label10.Location = new System.Drawing.Point(229, 24);
      this.label10.Name = "label10";
      this.label10.Size = new System.Drawing.Size(82, 130);
      this.label10.TabIndex = 13;
      this.label10.Text = "channel name\r\ntitle\r\nepisode name\r\ngenre\r\nseries number\r\nepisode number\r\nepisode " +
          "part\r\ndate\r\nstart time\r\nend time";
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label5.Location = new System.Drawing.Point(217, 24);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(13, 130);
      this.label5.TabIndex = 11;
      this.label5.Text = "=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=\r\n=";
      // 
      // label9
      // 
      this.label9.AutoSize = true;
      this.label9.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label9.Location = new System.Drawing.Point(152, 24);
      this.label9.Name = "label9";
      this.label9.Size = new System.Drawing.Size(61, 130);
      this.label9.TabIndex = 12;
      this.label9.Text = "%channel%\r\n%title%\r\n%name%\r\n%genre%\r\n%series%\r\n%episode%\r\n%part%\r\n%date%\r\n%start%" +
          "\r\n%end%";
      this.label9.TextAlign = System.Drawing.ContentAlignment.TopRight;
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label7.Location = new System.Drawing.Point(16, 37);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(134, 104);
      this.label7.TabIndex = 0;
      this.label7.Text = "Leave empty for classic\r\nnaming. Use blockquotes\r\n[ ] to specify optional fields." +
          "\r\n\r\nNote: A recording is being\r\nconsidered as a movie, if\r\nthe recording type is" +
          "\r\nmanual or single recording.";
      // 
      // labelSampleSeries
      // 
      this.labelSampleSeries.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.labelSampleSeries.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.labelSampleSeries.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
      this.labelSampleSeries.Location = new System.Drawing.Point(66, 256);
      this.labelSampleSeries.Name = "labelSampleSeries";
      this.labelSampleSeries.Size = new System.Drawing.Size(392, 16);
      this.labelSampleSeries.TabIndex = 10;
      this.labelSampleSeries.Text = "line1";
      // 
      // label8
      // 
      this.label8.AutoSize = true;
      this.label8.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label8.Location = new System.Drawing.Point(16, 256);
      this.label8.Name = "label8";
      this.label8.Size = new System.Drawing.Size(45, 13);
      this.label8.TabIndex = 9;
      this.label8.Text = "Sample:";
      // 
      // labelSampleMovies
      // 
      this.labelSampleMovies.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.labelSampleMovies.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.labelSampleMovies.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
      this.labelSampleMovies.Location = new System.Drawing.Point(66, 200);
      this.labelSampleMovies.Name = "labelSampleMovies";
      this.labelSampleMovies.Size = new System.Drawing.Size(392, 16);
      this.labelSampleMovies.TabIndex = 6;
      this.labelSampleMovies.Text = "line1";
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label6.Location = new System.Drawing.Point(16, 200);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(45, 13);
      this.label6.TabIndex = 5;
      this.label6.Text = "Sample:";
      // 
      // textBoxMovies
      // 
      this.textBoxMovies.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxMovies.Location = new System.Drawing.Point(64, 168);
      this.textBoxMovies.Name = "textBoxMovies";
      this.textBoxMovies.Size = new System.Drawing.Size(392, 20);
      this.textBoxMovies.TabIndex = 4;
      this.textBoxMovies.TextChanged += new System.EventHandler(this.textBoxMovies_TextChanged);
      // 
      // label39
      // 
      this.label39.AutoSize = true;
      this.label39.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label39.Location = new System.Drawing.Point(16, 227);
      this.label39.Name = "label39";
      this.label39.Size = new System.Drawing.Size(39, 13);
      this.label39.TabIndex = 7;
      this.label39.Text = "Series:";
      // 
      // label38
      // 
      this.label38.AutoSize = true;
      this.label38.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label38.Location = new System.Drawing.Point(16, 171);
      this.label38.Name = "label38";
      this.label38.Size = new System.Drawing.Size(44, 13);
      this.label38.TabIndex = 3;
      this.label38.Text = "Movies:";
      // 
      // textBoxSeries
      // 
      this.textBoxSeries.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxSeries.Location = new System.Drawing.Point(64, 224);
      this.textBoxSeries.Name = "textBoxSeries";
      this.textBoxSeries.Size = new System.Drawing.Size(392, 20);
      this.textBoxSeries.TabIndex = 8;
      this.textBoxSeries.TextChanged += new System.EventHandler(this.textBoxSeries_TextChanged);
      // 
      // TVRecording
      // 
      this.Controls.Add(this.groupBox2);
      this.Controls.Add(this.groupBox1);
      this.Name = "TVRecording";
      this.Size = new System.Drawing.Size(472, 408);
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.ResumeLayout(false);

    }
    #endregion

    string ShowExample(string strInput, bool isMovie)
    {
      string channel;
      string title;
      string episode;
      string seriesNum;
      string episodeNum;
      string episodePart;
      DateTime startDate;
      DateTime endDate;
      string genre;

      if (isMovie)
      {
        channel = "ProSieben";
        title = "Philadelphia";
        episode = "unknown";
        seriesNum = "unknown";
        episodeNum = "unknown";
        episodePart = "unknown";
        IFormatProvider culture = new CultureInfo("en-US");
        startDate = DateTime.Parse("12/23/2005 08:15 PM", culture);
        endDate = DateTime.Parse("12/23/2005 10:45 PM", culture);
        genre = "Drama";
      }
      else
      {
        channel = "ABC";
        title = "Friends";
        episode = "Joey's Birthday";
        seriesNum = "4";
        episodeNum = "32";
        episodePart = "part 1 of 1";
        IFormatProvider culture = new CultureInfo("en-US");
        startDate = DateTime.Parse("12/23/2005 08:15 PM", culture);
        endDate = DateTime.Parse("12/23/2005 08:45 PM", culture);
        genre = "Comedy";
      }

      string strName = string.Empty;
      string strDirectory = string.Empty;

      string strDefaultName = String.Format("{0}_{1}_{2}{3:00}{4:00}{5:00}{6:00}p{7}{8}",
                                  channel, title,
                                  startDate.Year, startDate.Month, startDate.Day,
                                  startDate.Hour,
                                  startDate.Minute,
                                  DateTime.Now.Minute, DateTime.Now.Second);

      if (strInput != string.Empty)
      {
        strInput = Utils.ReplaceTag(strInput, "%channel%", channel, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%title%", title, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%name%", episode, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%series%", seriesNum, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%episode%", episodeNum, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%part%", episodePart, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%date%", startDate.ToShortDateString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%start%", startDate.ToShortTimeString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%end%", endDate.ToShortTimeString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%genre%", genre, "unknown");
        strInput = Utils.ReplaceTag(strInput, "%startday%", startDate.Day.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%startmonth%", startDate.Month.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%startyear%", startDate.Year.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%starthh%", startDate.Hour.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%startmm%", startDate.Minute.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%endday%", endDate.Day.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%endmonth%", endDate.Month.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%startyear%", endDate.Year.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%endhh%", endDate.Hour.ToString(), "unknown");
        strInput = Utils.ReplaceTag(strInput, "%endmm%", endDate.Minute.ToString(), "unknown");
        

        int index = strInput.LastIndexOf('\\');

        switch (index)
        {
          case -1:
            strName = strInput;
            break;
          case 0:
            strName = strInput.Substring(1);
            break;
          default:
            {
              strDirectory = "\\" + strInput.Substring(0, index);
              strName = strInput.Substring(index + 1);
            }
            break;
        }

        strDirectory = Utils.MakeDirectoryPath(strDirectory);
        strName = Utils.MakeFileName(strName);
      }

      if (strName == string.Empty)
        strName = strDefaultName;

      string strReturn = strDirectory;
      if (strDirectory != string.Empty)
        strReturn += "\\";
      strReturn += strName + ".dvr-ms";

      return strReturn;
    }

    public override void LoadSettings()
    {
      using (MediaPortal.Profile.Xml xmlreader = new MediaPortal.Profile.Xml("MediaPortal.xml"))
      {
        startTextBox.Text = Convert.ToString(xmlreader.GetValueAsInt("capture", "prerecord", 5));
        endTextBox.Text = Convert.ToString(xmlreader.GetValueAsInt("capture", "postrecord", 5));
        cbDeleteWatchedShows.Checked = xmlreader.GetValueAsBool("capture", "deletewatchedshows", false);
        cbAddRecordingsToMovie.Checked = xmlreader.GetValueAsBool("capture", "addrecordingstomoviedatabase", true);
        textBoxMovies.Text = xmlreader.GetValueAsString("capture", "moviesformat", string.Empty);
        textBoxSeries.Text = xmlreader.GetValueAsString("capture", "seriesformat", string.Empty);
      }
      labelSampleMovies.Text = ShowExample(textBoxMovies.Text, true);
      labelSampleSeries.Text = ShowExample(textBoxSeries.Text, false);
    }

    public override void SaveSettings()
    {
      using (MediaPortal.Profile.Xml xmlwriter = new MediaPortal.Profile.Xml("MediaPortal.xml"))
      {
        xmlwriter.SetValue("capture", "prerecord", startTextBox.Text);
        xmlwriter.SetValue("capture", "postrecord", endTextBox.Text);

        xmlwriter.SetValueAsBool("capture", "deletewatchedshows", cbDeleteWatchedShows.Checked);
        xmlwriter.SetValueAsBool("capture", "addrecordingstomoviedatabase", cbAddRecordingsToMovie.Checked);

        xmlwriter.SetValue("capture", "moviesformat", textBoxMovies.Text);
        xmlwriter.SetValue("capture", "seriesformat", textBoxSeries.Text);
      }
    }

    private void startTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
    {
      if (char.IsNumber(e.KeyChar) == false && e.KeyChar != 8)
      {
        e.Handled = true;
      }
    }

    private void endTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
    {
      if (char.IsNumber(e.KeyChar) == false && e.KeyChar != 8)
      {
        e.Handled = true;
      }
    }

    private void textBoxMovies_TextChanged(object sender, EventArgs e)
    {
      labelSampleMovies.Text = ShowExample(textBoxMovies.Text, true);
    }

    private void textBoxSeries_TextChanged(object sender, EventArgs e)
    {
      labelSampleSeries.Text = ShowExample(textBoxSeries.Text, false);
    }
  }
}

