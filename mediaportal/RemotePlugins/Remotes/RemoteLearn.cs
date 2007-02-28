#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal - author: diehard2
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

#endregion

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;
using MediaPortal.InputDevices;
using MediaPortal.Configuration;
using MediaPortal.Profile;


namespace MediaPortal.InputDevices
{
  public partial class RemoteLearn : Form
  {

    #region Variables

    string m_sRemoteModel;
    string m_sRemoteClass;
    bool m_bchangedsettings = false;
    bool m_bLearn = false;
    bool m_bInitialized = false;
    object RemoteInterface = null;
    XmlDocument doc;
    ArrayList RemoteCommand;
    ArrayList MPCommand;

    #endregion

    #region Setup RemoteLearning

    public RemoteLearn(string RemoteClass, string Remotemodel, object Remote)
    {

      m_sRemoteModel = Remotemodel;
      m_sRemoteClass = RemoteClass;
      Log.Info("RemoteLearn: Loaded Remote Mapping");
      InitializeComponent();
      InitializeListView();
      mpListView.Enabled = false;
      this.Text = "Generic Remote Control Learning";
      LoadMapping(Remotemodel + ".xml", false);
      RemoteCommand = new ArrayList(150);
      MPCommand = new ArrayList(150);


      if (Remote == null)
      {
        MessageBox.Show("Could not initialize device");
        m_bInitialized = false;
      }
      else
      {
        //This will be eventually setup for all remotes
        if (RemoteClass == "X10")
        {
          try
          {
            RemoteInterface = (X10Remote)Remote;
            ((X10Remote)RemoteInterface).X10KeyPressed += new X10Remote.X10Event(CatchKeyPress);
            this.Text = "Teach " + Remotemodel + " Remote Control";
            m_bInitialized = true;
          }
          catch (Exception ex)
          {
            Log.Info("RemoteLearn: Could not cast to X10 object - {0}", ex.GetBaseException());
          }
        }
        else if (RemoteClass == "HCW")
        {
          try
          {
            
            RemoteInterface = (HcwRemote)Remote;
            ((HcwRemote)RemoteInterface).HCWKeyPressed += new HcwRemote.HCWEvent(CatchKeyPress);
            this.Text = "Teach " + Remotemodel + " Remote Control";
            m_bInitialized = true;
          }
          catch (Exception ex)
          {
            Log.Info("RemoteLearn: Could not cast to HCW object - {0}", ex.GetBaseException());
          }



        }
        else
        {
          MessageBox.Show("Remote class not recognized");
          m_bInitialized = false;
        }
      }

    }

    //Initialize the listview

    private void InitializeListView()
    {

      mpListView.FullRowSelect = true;

    }

    #endregion

    #region Form Control

    //Button control
    private void mpOK_Click(object sender, EventArgs e)
    {
      if (m_bchangedsettings)
        FindandReplaceNode();
      SaveMapping(m_sRemoteModel + ".xml");

      this.Close();
    }

    private void mpApply_Click(object sender, EventArgs e)
    {
      if (m_bchangedsettings)
      {
        FindandReplaceNode();
        SaveMapping(m_sRemoteModel + ".xml");
      }

    }

    private void mpCancel_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void ButtonStartLearn_Click(object sender, EventArgs e)
    {
      mpListView.Enabled = true;
      mpListView.Select();
      mpListView.Items[0].Selected = true;
      m_bLearn = true;

    }

    private void ButtonEndLearn_Click(object sender, EventArgs e)
    {
      mpListView.Enabled = false;
      m_bLearn = false;
    }

    private void InputMapperButton_Click(object sender, EventArgs e)
    {
      if (m_bchangedsettings)
      {
        FindandReplaceNode();
        SaveMapping(m_sRemoteModel);
      }
      doc = null;
      InputMappingForm dlg;
      dlg = new InputMappingForm(m_sRemoteModel);
      dlg.ShowDialog(this);
      LoadMapping(m_sRemoteModel + ".xml", false);
      mpListView.Invalidate();
    }

    private void mpRemotenumber_SelectedIndexChanged(object sender, EventArgs e)
    {
      mpListView.Items.Clear();
      LoadMapping(m_sRemoteModel + ".xml", false);
      mpListView.Select();
      mpListView.Focus();
      mpListView.Items[0].Selected = true;

    }

    private void RemoteLearn_Shown(object sender, EventArgs e)
    {
      if (m_bInitialized == false)
      {
        MessageBox.Show("Failed to get remote control interface");
        this.Close();
      }

    }

    #endregion

    #region Callback

    void CatchKeyPress(int keypress)
    {
      if (m_bLearn == true)
      {
        m_bchangedsettings = true;
        mpListView.Select();
        
        mpListView.SelectedItems[0].SubItems[1].Text = keypress.ToString();

        int index = mpListView.SelectedItems[0].Index;

        MPCommand.Add(mpListView.SelectedItems[0].Text);
        RemoteCommand.Add(keypress.ToString());

        mpListView.Items[index + 5].EnsureVisible();
        mpListView.Items[index + 1].Selected = true;
        mpListView.Invalidate();
      }
    }

    //Input mapping functions

    #endregion

    #region Load and Save mappings

    void FindandReplaceNode()
    {

      XmlNodeList listRemotes = doc.DocumentElement.SelectNodes("/mappings/remote");

      int counter = 0;

      foreach (XmlNode nodeRemote in listRemotes)
      {
        
        if (nodeRemote.Attributes["family"].Value.ToString() == mpRemotenumber.SelectedItem.ToString())
        {
          XmlNodeList listButtons = nodeRemote.SelectNodes("button");

          while (RemoteCommand.Count > counter)
          {

            foreach (XmlNode nodeButton in listButtons)
            {
              if (nodeButton.Attributes["name"].Value.ToString() == MPCommand[counter].ToString())
              {
                nodeButton.Attributes["code"].Value = RemoteCommand[counter].ToString();
              }
            }
            counter++;
          }
        }
      }

    }

    void LoadRemotes()
    {
      using (Settings xmlreader = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {

        XmlNodeList listRemotes = doc.DocumentElement.SelectNodes("/mappings/remote");

        foreach (XmlNode nodeRemote in listRemotes)
        {

          mpRemotenumber.Items.Add(nodeRemote.Attributes["family"].Value.ToString());
        }

        int Index = xmlreader.GetValueAsInt("remote", "remotenumberindex" + m_sRemoteClass, -1);

        if (mpRemotenumber.Items.Count > 1 && Index == -1)
        {
          MessageBox.Show("Please select the correct remote control model");
          mpRemotenumber.SelectedIndex = 0;
        }
        else if (Index == -1)
          mpRemotenumber.SelectedIndex = 0;
        else
          mpRemotenumber.SelectedIndex = Index;

      }
    }

    void LoadMapping(string xmlFile, bool defaults)
    {
      try
      {

        doc = new XmlDocument();
        string path = "InputDeviceMappings\\defaults\\" + xmlFile;

        if (!defaults && File.Exists(Config.GetFile(Config.Dir.CustomInputDevice, xmlFile)))
          path = Config.GetFile(Config.Dir.CustomInputDevice, xmlFile);
        if (!File.Exists(path))
        {
          MessageBox.Show("Can't locate mapping file " + xmlFile + "\n\nMake sure it exists in /InputDeviceMappings/defaults", "Mapping file missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }

        doc.Load(path);

        XmlNodeList listRemotes = doc.DocumentElement.SelectNodes("/mappings/remote");
        string[] data = new string[2];

        if (mpRemotenumber.SelectedItem == null)
          LoadRemotes();


        foreach (XmlNode nodeRemote in listRemotes)
        {
          if (nodeRemote.Attributes["family"].Value.ToString() == mpRemotenumber.SelectedItem.ToString())
          {
            XmlNodeList listButtons = nodeRemote.SelectNodes("button");

            foreach (XmlNode nodeButton in listButtons)
            {
              ListViewItem lvi;
              data[0] = nodeButton.Attributes["name"].Value.ToString();
              data[1] = nodeButton.Attributes["code"].Value.ToString();
              lvi = new ListViewItem(data);
              mpListView.Items.Add(lvi);
            }
          }
        }

      }
      catch (Exception ex)
      {
        Log.Error(ex);
        File.Delete(Config.GetFile(Config.Dir.CustomInputDevice, xmlFile));
        LoadMapping(m_sRemoteModel + ".xml", true);
      }
    }

    bool SaveMapping(string xmlFile)
    {
      if (m_bchangedsettings == true)
      {
        try
        {
          string directory = Config.GetFolder(Config.Dir.CustomInputDevice);
          DirectoryInfo dir = Directory.CreateDirectory(directory);
          doc.Save(directory + "\\" + xmlFile);
        }
        catch
        {
          Log.Info("MAP: Error accessing directory \"InputDeviceMappings\\custom\"");
        }
      }
      using (Settings xmlwriter = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        xmlwriter.SetValue("remote", "remotenumberindex" + m_sRemoteClass, mpRemotenumber.SelectedIndex);
      }
      return true;

    }
    #endregion
  }
}