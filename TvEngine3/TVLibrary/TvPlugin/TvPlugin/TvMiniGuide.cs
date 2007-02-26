#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Util;

using TvDatabase;
using TvControl;

using Gentle.Common;
using Gentle.Framework;

namespace TvPlugin
{
  /// <summary>
  /// GUIMiniGuide
  /// </summary>
  /// 
  public class TvMiniGuide : GUIWindow, IRenderLayer
  {
    // Member variables                                  
    [SkinControlAttribute(34)]    protected GUIButtonControl cmdExit = null;
    [SkinControlAttribute(35)]    protected GUIListControl lstChannels = null;
    [SkinControlAttribute(36)]    protected GUISpinControl spinGroup = null;

    bool _running = false;    
    int _parentWindowID = 0;
    GUIWindow _parentWindow = null;
    List<Channel> _tvChannelList = null;
    List<ChannelGroup> _channelGroupList = null;
    Channel _selectedChannel;
    bool _zap = true;

    /// <summary>
    /// Constructor
    /// </summary>
    public TvMiniGuide()
    {
      GetID = (int)GUIWindow.Window.WINDOW_MINI_GUIDE;
    }

    public override void OnAdded()
    {
      GUIWindowManager.Replace((int)GUIWindow.Window.WINDOW_MINI_GUIDE, this);
      Restore();
      PreInit();
      ResetAllControls();
    }

    public override bool SupportsDelayedLoad
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is tv.
    /// </summary>
    /// <value><c>true</c> if this instance is tv; otherwise, <c>false</c>.</value>
    public override bool IsTv
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Gets or sets the selected channel.
    /// </summary>
    /// <value>The selected channel.</value>
    public Channel SelectedChannel
    {
      get
      {
        return _selectedChannel;
      }
      set
      {
        _selectedChannel = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether [auto zap].
    /// </summary>
    /// <value><c>true</c> if [auto zap]; otherwise, <c>false</c>.</value>
    public bool AutoZap
    {
      get
      {
        return _zap;
      }
      set
      {
        _zap = value;
      }
    }

    /// <summary>
    /// Init method
    /// </summary>
    /// <returns></returns>
    public override bool Init()
    {
      bool bResult = Load(GUIGraphicsContext.Skin + @"\TVMiniGuide.xml");

      GetID = (int)GUIWindow.Window.WINDOW_MINI_GUIDE;
      GUILayerManager.RegisterLayer(this, GUILayerManager.LayerType.MiniEPG);
      return bResult;
    }


    /// <summary>
    /// Renderer
    /// </summary>
    /// <param name="timePassed"></param>
    public override void Render(float timePassed)
    {
      base.Render(timePassed);		// render our controls to the screen
    }

    /// <summary>
    /// On close
    /// </summary>
    void Close()
    {
      Log.Debug("miniguide: close()");
      GUIWindowManager.IsSwitchingToNewWindow = true;
      lock (this)
      {
        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT, GetID, 0, 0, 0, 0, null);
        OnMessage(msg);

        GUIWindowManager.UnRoute();
        _running = false;
      }
      GUIWindowManager.IsSwitchingToNewWindow = false;
    }

    /// <summary>
    /// On Message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public override bool OnMessage(GUIMessage message)
    {
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_CLICKED:
          {
            if (message.SenderControlId == 35) // listbox
            {
              if ((int)Action.ActionType.ACTION_SELECT_ITEM == message.Param1)
              {
                // switching logic
                SelectedChannel = (Channel)lstChannels.SelectedListItem.MusicTag;
                if (AutoZap)
                {
                  string selectedChan = (string)lstChannels.SelectedListItem.TVTag;
                  if (TVHome.Navigator.CurrentChannel != selectedChan)
                  {
                    TVHome.Navigator.ZapToChannel(_tvChannelList[lstChannels.SelectedListItemIndex], false);
                    TVHome.Navigator.ZapNow();
                  }
                }
                Close();
              }
            }
            else if (message.SenderControlId == 36) // spincontrol
            {
              // switch group
              TVHome.Navigator.SetCurrentGroup(spinGroup.GetLabel());
              FillChannelList();
            }
            else if (message.SenderControlId == 34) // exit button
            {
              // exit
              Close();
            }
            break;
          }
      }
      return base.OnMessage(message);
    }

    /// <summary>
    /// On action
    /// </summary>
    /// <param name="action"></param>
    public override void OnAction(Action action)
    {
      switch (action.wID)
      {
        case Action.ActionType.ACTION_CONTEXT_MENU:
          //_running = false;
          Close();
          return;
        case Action.ActionType.ACTION_PREVIOUS_MENU:
          //_running = false;
          Close();
          return;
        case Action.ActionType.ACTION_MOVE_LEFT:
          // switch group
          spinGroup.MoveUp();
          TVHome.Navigator.SetCurrentGroup(spinGroup.GetLabel());
          FillChannelList();
          return;
        case Action.ActionType.ACTION_MOVE_RIGHT:
          // switch group
          spinGroup.MoveDown();
          TVHome.Navigator.SetCurrentGroup(spinGroup.GetLabel());
          FillChannelList();
          return;
      }
      base.OnAction(action);
    }

    /// <summary>
    /// Page gets destroyed
    /// </summary>
    /// <param name="new_windowId"></param>
    protected override void OnPageDestroy(int new_windowId)
    {
      Log.Debug("miniguide: OnPageDestroy");
      base.OnPageDestroy(new_windowId);
      _running = false;
    }

    /// <summary>
    /// Page gets loaded
    /// </summary>
    protected override void OnPageLoad()
    {
      Log.Debug("miniguide: onpageload");
      // following line should stay. Problems with OSD not
      // appearing are already fixed elsewhere
      GUILayerManager.RegisterLayer(this, GUILayerManager.LayerType.MiniEPG);
      AllocResources();
      ResetAllControls();							// make sure the controls are positioned relevant to the OSD Y offset
      FillChannelList();
      FillGroupList();
      base.OnPageLoad();
    }

    /// <summary>
    /// Fill up the list with groups
    /// </summary>
    public void FillGroupList()
    {
      ChannelGroup current = null;
      _channelGroupList = TVHome.Navigator.Groups;
      // empty list of channels currently in the 
      // spin control
      spinGroup.Reset();
      // start to fill them up again
      for (int i = 0; i < _channelGroupList.Count; i++)
      {
        current = _channelGroupList[i];
        spinGroup.AddLabel(current.GroupName, i);
        // set selected
        if (current.GroupName.CompareTo(TVHome.Navigator.CurrentGroup.GroupName) == 0)
          spinGroup.Value = i;
      }
    }

    /// <summary>
    /// Fill the list with channels
    /// </summary>
    public void FillChannelList()
    {
      Log.Debug("miniguide: FillChannelList - Entry");
      _tvChannelList = new List<Channel>();
      foreach (GroupMap map in TVHome.Navigator.CurrentGroup.ReferringGroupMap())
      {
        Channel ch = map.ReferencedChannel();
        if (ch.VisibleInGuide && ch.IsTv)
          _tvChannelList.Add(ch);
      }
      Log.Debug("miniguide: FillChannelList - Got groups");
      TvBusinessLayer layer = new TvBusinessLayer();      
      Dictionary<int, NowAndNext> listNowNext = layer.GetNowAndNext();
      Log.Debug("miniguide: FillChannelList - Got NowNext channels");

      lstChannels.Clear();
      Channel currentChan = null;
      GUIListItem item = null;
      string logo = "";
      int selected = 0;
      int currentChanState = 0;
      bool checkChannelState = false;
      string pathIconNoTune = GUIGraphicsContext.Skin + @"\Media\remote_blue.png";
      string pathIconTimeshift = GUIGraphicsContext.Skin + @"\Media\remote_yellow.png";
      string pathIconRecord = GUIGraphicsContext.Skin + @"\Media\remote_red.png";
      Log.Debug("miniguide: FillChannelList - Init vars");

      if (!TVHome.TvServer.IsAnyCardIdle())
      {
        // no free card - check if a user is recording
        // TODO: add user check since an "owner" might be timeshifting and therefore preventing usage of a specific card
        if (TVHome.TvServer.IsAnyCardRecording())
          checkChannelState = true;
      }
      //checkChannelState = ;
      if (!checkChannelState)
        Log.Debug("miniguide: maybe usable card found - not checking channel state");

      for (int i = 0; i < _tvChannelList.Count; i++)
      {
        currentChan = _tvChannelList[i];
        if (checkChannelState)
          currentChanState = (int)TVHome.TvServer.GetChannelState(currentChan.IdChannel);

        if (currentChan.VisibleInGuide)
        {
          NowAndNext prog;
          if (listNowNext.ContainsKey(currentChan.IdChannel) != false)
          {
            prog = listNowNext[currentChan.IdChannel];
          }
          else
          {
            prog = new NowAndNext(currentChan.IdChannel, DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1), DateTime.Now.AddHours(2), DateTime.Now.AddHours(3), GUILocalizeStrings.Get(736), GUILocalizeStrings.Get(736), -1, -1);
          }

          StringBuilder sb = new StringBuilder();
          item = new GUIListItem("");
          // store here as it is not needed right now - please beat me later..
          item.TVTag = currentChan.Name;
          item.MusicTag = currentChan;

          logo = MediaPortal.Util.Utils.GetCoverArt(Thumbs.TVChannel, currentChan.Name);

          // if we are watching this channel mark it
          if (TVHome.Navigator.Channel.IdChannel == currentChan.IdChannel)
          {
            item.IsRemote = true;
            selected = lstChannels.Count;
          }

          if (System.IO.File.Exists(logo))
          {
            item.IconImageBig = logo;
            item.IconImage = logo;
          }
          else
          {
            item.IconImageBig = string.Empty;
            item.IconImage = string.Empty;
          }

          if (checkChannelState)
          {
            Log.Debug("miniguide: state of {0} is {1}", currentChan.Name, Convert.ToString(currentChanState));
            switch (currentChanState)
            {
              case 0:
                item.IconImageBig = pathIconNoTune;
                item.IconImage = pathIconNoTune;
                item.IsPlayed = true;
                break;
              case 2:
                item.IconImageBig = pathIconTimeshift;
                item.IconImage = pathIconTimeshift;
                break;
              case 3:
                item.IconImageBig = pathIconRecord;
                item.IconImage = pathIconRecord;
                break;
              default:
                item.IsPlayed = false;
                break;
            }
          }

          item.Label2 = prog.TitleNow;
          //                    item.Label3 = prog.Title + " [" + prog.StartTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat) + "-" + prog.EndTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat) + "]";

          item.Label3 = GUILocalizeStrings.Get(789) + prog.TitleNow;
          sb.Append(currentChan.Name);
          sb.Append(" - ");
          sb.Append(CalculateProgress(prog.NowStartTime, prog.NowEndTime).ToString());
          sb.Append("%");
          item.Label2 = sb.ToString();
          item.Label = GUILocalizeStrings.Get(790) + prog.TitleNext;

          lstChannels.Add(item);
        }
      }
      Log.Debug("miniguide: FillChannelList - Exit");
      lstChannels.SelectedListItemIndex = selected;
    }

    /// <summary>
    /// Get current tv program
    /// </summary>
    /// <param name="prog"></param>
    /// <returns></returns>
    private double CalculateProgress(DateTime start, DateTime end)
    {
      TimeSpan length = end - start;
      TimeSpan passed = DateTime.Now - start;
      if (length.TotalMinutes > 0)
      {
        double fprogress = (passed.TotalMinutes / length.TotalMinutes) * 100;
        fprogress = Math.Floor(fprogress);
        if (fprogress > 100.0f)
          return 100.0f;
        return fprogress;
      }
      else
        return 0;
    }

    /// <summary>
    /// Do this modal
    /// </summary>
    /// <param name="dwParentId"></param>
    public void DoModal(int dwParentId)
    {
      Log.Debug("miniguide: domodal");
      _parentWindowID = dwParentId;
      _parentWindow = GUIWindowManager.GetWindow(_parentWindowID);
      if (null == _parentWindow)
      {
        Log.Debug("miniguide: parentwindow=0");
        _parentWindowID = 0;
        return;
      }

      GUIWindowManager.IsSwitchingToNewWindow = true;
      GUIWindowManager.RouteToWindow(GetID);

      // activate this window...
      GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_WINDOW_INIT, GetID, 0, 0, -1, 0, null);
      OnMessage(msg);

      GUIWindowManager.IsSwitchingToNewWindow = false;
      _running = true;
      GUILayerManager.RegisterLayer(this, GUILayerManager.LayerType.Dialog);
      while (_running && GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.RUNNING)
      {
        GUIWindowManager.Process();
        if (!GUIGraphicsContext.Vmr9Active)
          System.Threading.Thread.Sleep(50);
      }
      GUILayerManager.UnRegisterLayer(this);

      Log.Debug("miniguide: closed");
    }

    // Overlay IRenderLayer members
    #region IRenderLayer
    public bool ShouldRenderLayer()
    {
      return true;
    }

    public void RenderLayer(float timePassed)
    {
      if (_running)
        Render(timePassed);
    }
    #endregion
  }
}