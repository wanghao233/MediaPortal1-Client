using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MediaPortal.GUI.Library;
using MediaPortal.Util;


namespace MediaPortal.GUI.Home
{
  public partial class GUIHomeSetupForm : System.Windows.Forms.Form , ISetupForm
  {
    public GUIHomeSetupForm()
    {
      InitializeComponent();
      LoadSettings();
    }

    private void LoadSettings()
    {
      cboxFormat.Items.Add("<Day> <DD>.<Month>");
      cboxFormat.Items.Add("<Day> <Month> <DD>");

      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        chkboxFixScrollbar.Checked = xmlreader.GetValueAsBool("home", "scrollfixed", false);
        chkBoxUseMyPlugins.Checked = xmlreader.GetValueAsBool("home", "usemyplugins", true);
        chkBoxAnimation.Checked = xmlreader.GetValueAsBool("home", "enableanimation", true);
        int index = xmlreader.GetValueAsInt("home", "dateformatindex", 1);
        if (index > 1)
        {
          string text = xmlreader.GetValueAsString("home", "dateformat", "<Day> <Month> <DD>");
          cboxFormat.Items.Add(text);
        }
        cboxFormat.SelectedIndex = index;
      }
    }


    private void SaveSettings()
    {
      using (MediaPortal.Profile.Settings xmlWriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        xmlWriter.SetValueAsBool("home", "scrollfixed",  chkboxFixScrollbar.Checked);
        xmlWriter.SetValueAsBool("home", "usemyplugins", chkBoxUseMyPlugins.Checked);
        xmlWriter.SetValueAsBool("home", "enableanimation", chkBoxAnimation.Checked);
        xmlWriter.SetValue("home", "dateformatindex", cboxFormat.SelectedIndex);
        xmlWriter.SetValue("home", "dateformat", cboxFormat.SelectedText);
      }
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
      SaveSettings();
      this.Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void btnDayText_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<Day>";
      UpdateTestBox();
    }

    private void btnDayNumber_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<DD>";
      UpdateTestBox();
    }

    private void btnMonthText_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<Month>";
      UpdateTestBox();
    }

    private void btnMonthNumber_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<MM>";
      UpdateTestBox();
    }

    private void btnYearText_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<Year>";
      UpdateTestBox();
    }

    private void btnYearNumber_Click(object sender, EventArgs e)
    {
      cboxFormat.Text += "<YY>";
      UpdateTestBox();
    }

    private void cboxFormat_TextUpdate(object sender, EventArgs e)
    {
      UpdateTestBox();
    }

    private void UpdateTestBox()
    {
      tboxTest.Text = "";
      string dateString = cboxFormat.Text;
      if ((dateString == null) || (dateString.Length == 0)) return;

      DateTime cur = DateTime.Now;
      string day;
      switch (cur.DayOfWeek)
      {
        case DayOfWeek.Monday: day = "Monday"; break;
        case DayOfWeek.Tuesday: day = "Tuesday"; break;
        case DayOfWeek.Wednesday: day = "Wednesday"; break;
        case DayOfWeek.Thursday: day = "Thursday"; break;
        case DayOfWeek.Friday: day = "Friday"; break;
        case DayOfWeek.Saturday: day = "Saturday"; break;
        default: day = "Sunday"; break;
      }

      string month;
      switch (cur.Month)
      {
        case 1: month = "January"; break;
        case 2: month = "February"; break;
        case 3: month = "March"; break;
        case 4: month = "April"; break;
        case 5: month = "May"; break;
        case 6: month = "June"; break;
        case 7: month = "July"; break;
        case 8: month = "August"; break;
        case 9: month = "September"; break;
        case 10: month = "October"; break;
        case 11: month = "November"; break;
        default: month = "December"; break;
      }
      
      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<Day>", day, "unknown");
      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<DD>", cur.Day.ToString(), "unknown");

      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<Month>", month, "unknown");
      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<MM>", cur.Month.ToString(), "unknown");

      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<Year>", cur.Year.ToString(), "unknown");
      dateString = MediaPortal.Util.Utils.ReplaceTag(dateString, "<YY>", (cur.Year - 2000).ToString("00"), "unknown");

      tboxTest.Text = dateString;
    }

    private void cboxFormat_SelectedIndexChanged(object sender, EventArgs e)
    {
      UpdateTestBox();
    }

    private void cboxFormat_KeyPress(object sender, KeyPressEventArgs e)
    {
      UpdateTestBox();
    }

    #region ISetupForm members
    public string PluginName()
    {
      return "Home";
    }

    public string Description()
    {
      return "Home Screen";
    }

    public string Author()
    {
      return "Bavarian";
    }
    public void ShowPlugin()
    {
      System.Windows.Forms.Form setup = new GUIHomeSetupForm();
      setup.ShowDialog();
    }
    //System.Reflection.TargetInvocationException
    public bool HasSetup()
    {
      return true;
    }

    public bool CanEnable()
    {
      return true;
    }

    public bool DefaultEnabled()
    {
      return true;
    }

    public int GetWindowId()
    {
      return (int)GUIWindow.Window.WINDOW_HOME;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = PluginName();
      strButtonImage = String.Empty;
      strButtonImageFocus = String.Empty;
      strPictureImage = String.Empty;
      return true;
    }

    #endregion



  }
}