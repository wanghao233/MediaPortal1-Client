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

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using DirectShowLib.SBE;
using TvLibrary;
using TvLibrary.Implementations;
using TvLibrary.Interfaces;
using TvLibrary.Implementations.Analog;
using TvLibrary.Implementations.DVB;
using TvLibrary.Implementations.Hybrid;
using TvLibrary.Channels;
using TvLibrary.Epg;
using TvLibrary.ChannelLinkage;
using TvLibrary.Log;
using TvLibrary.Streaming;
using TvControl;
using TvEngine;
using TvDatabase;
using TvEngine.Events;

namespace TvService
{
  public class TimeShifter
  {
    ITvCardHandler _cardHandler;
    bool _linkageScannerEnabled;
    bool _timeshiftingEpgGrabberEnabled;
    ChannelLinkageGrabber _linkageGrabber = null;
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerShifter"/> class.
    /// </summary>
    /// <param name="cardHandler">The card handler.</param>
    public TimeShifter(ITvCardHandler cardHandler)
    {
      _cardHandler = cardHandler;
      TvBusinessLayer layer = new TvBusinessLayer();
      _linkageScannerEnabled = (layer.GetSetting("linkageScannerEnabled", "no").Value == "yes");

      _linkageGrabber = new ChannelLinkageGrabber(cardHandler.Card);
      _timeshiftingEpgGrabberEnabled = (layer.GetSetting("timeshiftingEpgGrabberEnabled", "no").Value == "yes");
    }


    /// <summary>
    /// Gets the name of the time shift file.
    /// </summary>
    /// <value>The name of the time shift file.</value>
    public string FileName(ref User user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return "";
        if (_cardHandler.IsLocal == false)
        {
          try
          {
            RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
            return RemoteControl.Instance.TimeShiftFileName(ref user);
          }
          catch (Exception)
          {
            Log.Error("card: unable to connect to slave controller at:{0}", _cardHandler.DataBaseCard.ReferencedServer().HostName);
            return "";
          }
        }

        TvCardContext context = _cardHandler.Card.Context as TvCardContext;
        if (context == null) return null;
        context.GetUser(ref user);
        ITvSubChannel subchannel = _cardHandler.Card.GetSubChannel(user.SubChannel);
        if (subchannel == null) return null;
        return subchannel.TimeShiftFileName + ".tsbuffer";
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return "";
      }
    }

    /// <summary>
    /// Returns if the card is timeshifting or not
    /// </summary>
    /// <param name="cardId">id of the card.</param>
    /// <returns>true when card is timeshifting otherwise false</returns>
    public bool IsTimeShifting(ref User user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return false;
        if (_cardHandler.IsLocal == false)
        {
          try
          {
            RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
            return RemoteControl.Instance.IsTimeShifting(ref user);
          }
          catch (Exception)
          {
            Log.Error("card: unable to connect to slave controller at:{0}", _cardHandler.DataBaseCard.ReferencedServer().HostName);
            return false;
          }
        }

        TvCardContext context = _cardHandler.Card.Context as TvCardContext;
        if (context == null) return false;
        context.GetUser(ref user);
        ITvSubChannel subchannel = _cardHandler.Card.GetSubChannel(user.SubChannel);
        if (subchannel == null) return false;
        return subchannel.IsTimeShifting;
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return false;
      }
    }


    /// <summary>
    /// returns the date/time when timeshifting has been started for the card specified
    /// </summary>
    /// <param name="cardId">id of the card.</param>
    /// <returns>DateTime containg the date/time when timeshifting was started</returns>
    public DateTime TimeShiftStarted(User user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return DateTime.MinValue;
        if (_cardHandler.IsLocal == false)
        {
          try
          {
            RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
            return RemoteControl.Instance.TimeShiftStarted(user);
          }
          catch (Exception)
          {
            Log.Error("card: unable to connect to slave controller at:{0}", _cardHandler.DataBaseCard.ReferencedServer().HostName);
            return DateTime.MinValue;
          }
        }
        TvCardContext context = _cardHandler.Card.Context as TvCardContext;
        if (context == null) return DateTime.MinValue;
        context.GetUser(ref user);
        ITvSubChannel subchannel = _cardHandler.Card.GetSubChannel(user.SubChannel);
        if (subchannel == null) return DateTime.MinValue;
        return subchannel.StartOfTimeShift;
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return DateTime.MinValue;
      }
    }

    /// <summary>
    /// Start timeshifting.
    /// </summary>
    /// <param name="cardId">id of the card.</param>
    /// <param name="fileName">Name of the timeshiftfile.</param>
    /// <returns>TvResult indicating whether method succeeded</returns>
    public TvResult Start(ref User user, ref  string fileName)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return TvResult.CardIsDisabled;
        Log.Write("card: StartTimeShifting {0} {1} ", _cardHandler.DataBaseCard.IdCard, fileName);
        lock (this)
        {
          if (_cardHandler.IsLocal == false)
          {
            try
            {
              RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
              return RemoteControl.Instance.StartTimeShifting(ref user, ref fileName);
            }
            catch (Exception)
            {
              Log.Error("card: unable to connect to slave controller at:{0}", _cardHandler.DataBaseCard.ReferencedServer().HostName);
              return TvResult.UnknownError;
            }
          }

          TvCardContext context = _cardHandler.Card.Context as TvCardContext;
          if (context == null) return TvResult.UnknownChannel;
          context.GetUser(ref user);
          ITvSubChannel subchannel = _cardHandler.Card.GetSubChannel(user.SubChannel);
          if (subchannel == null) return TvResult.UnknownChannel;
          if (WaitForUnScrambledSignal(ref user) == false)
          {
            Log.Write("card: channel is scrambled");
            _cardHandler.Users.RemoveUser(user);

            return TvResult.ChannelIsScrambled;
          }
					

          if (subchannel.IsTimeShifting)
          {
						if (!WaitForTimeShiftFile(ref user, fileName + ".tsbuffer"))
						{
							if (_cardHandler.IsScrambled(ref user))
							{
								_cardHandler.Users.RemoveUser(user);
								return TvResult.ChannelIsScrambled;
							}
							_cardHandler.Users.RemoveUser(user);
							return TvResult.NoVideoAudioDetected;
						}

            context.OnZap(user);
            if (_linkageScannerEnabled)
              _cardHandler.Card.StartLinkageScanner(_linkageGrabber);
            if (_timeshiftingEpgGrabberEnabled)
            {
              Channel channel = Channel.Retrieve(user.IdChannel);
              if (channel.GrabEpg)
                _cardHandler.Card.GrabEpg();
              else
                Log.Info("TimeshiftingEPG: channel {0} is not configured for grabbing epg", channel.DisplayName);
            }
            return TvResult.Succeeded;
          }

          bool result = subchannel.StartTimeShifting(fileName);
          if (result == false)
          {
            _cardHandler.Users.RemoveUser(user);
            return TvResult.UnableToStartGraph;
          }
					fileName += ".tsbuffer";
          if (!WaitForTimeShiftFile(ref user, fileName))
          {
            if (_cardHandler.IsScrambled(ref user))
            {
              _cardHandler.Users.RemoveUser(user);
              return TvResult.ChannelIsScrambled;
            }
            _cardHandler.Users.RemoveUser(user);
            return TvResult.NoVideoAudioDetected;
          }
          context.OnZap(user);
          if (_linkageScannerEnabled)
            _cardHandler.Card.StartLinkageScanner(_linkageGrabber);
          if (_timeshiftingEpgGrabberEnabled)
          {
            Channel channel = Channel.Retrieve(user.IdChannel);
            if (channel.GrabEpg)
              _cardHandler.Card.GrabEpg();
            else
              Log.Info("TimeshiftingEPG: channel {0} is not configured for grabbing epg", channel.DisplayName);
          }
          return TvResult.Succeeded;
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return TvResult.UnknownError;
    }

    /// <summary>
    /// Stops the time shifting.
    /// </summary>
    /// <param name="cardId">id of the card.</param>
    /// <returns></returns>
    public bool Stop(ref User user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return true;
        if (false == IsTimeShifting(ref user)) return true;
        if (_cardHandler.Recorder.IsRecording(ref user)) return true;

        Log.Write("card: StopTimeShifting user:{0} sub:{1}", user.Name, user.SubChannel);
        lock (this)
        {
          bool result = false;
          if (_cardHandler.IsLocal == false)
          {
            try
            {
              RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
              result = RemoteControl.Instance.StopTimeShifting(ref user);
              return result;
            }
            catch (Exception)
            {
              Log.Error("card: unable to connect to slave controller at:{0}", _cardHandler.DataBaseCard.ReferencedServer().HostName);
              return false;
            }
          }
          TvCardContext context = _cardHandler.Card.Context as TvCardContext;
          if (context == null) return true;
          if (_linkageScannerEnabled)
            _cardHandler.Card.ResetLinkageScanner();
          context.Remove(user);
          if (_cardHandler.IsIdle)
          {
            _cardHandler.StopCard(user);
          }
          return result;
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return false;
    }


    /// <summary>
    /// Start timeshifting on the card
    /// </summary>
    /// <param name="idCard">The id card.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>TvResult indicating whether method succeeded</returns>
    public TvResult CardTimeShift(ref User user, ref string fileName)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false) return TvResult.CardIsDisabled;
        Log.WriteFile("card: CardTimeShift {0} {1}", _cardHandler.DataBaseCard.IdCard, fileName);
        if (IsTimeShifting(ref user)) return TvResult.Succeeded;
        return Start(ref user, ref fileName);
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return TvResult.UnknownError;
      }
    }

    /// <summary>
    /// Waits for un scrambled signal.
    /// </summary>
    /// <param name="cardId">The card id.</param>
    /// <returns>true if channel is unscrambled else false</returns>
    public bool WaitForUnScrambledSignal(ref User user)
    {
      if (_cardHandler.DataBaseCard.Enabled == false) return false;
      Log.Write("card: WaitForUnScrambledSignal");
      DateTime timeStart = DateTime.Now;
      while (true)
      {
        if (_cardHandler.IsScrambled(ref user))
        {
          Log.Write("card:   scrambled, sleep 100");
          System.Threading.Thread.Sleep(100);
          TimeSpan timeOut = DateTime.Now - timeStart;
          if (timeOut.TotalMilliseconds >= 5000)
          {
            Log.Write("card:   return scrambled");
            return false;
          }
        }
        else
        {
          Log.Write("card:   return not scrambled");
          return true;
        }
      }
    }

    /// <summary>
    /// Waits for time shift file to be at leat 300kb.
    /// </summary>
    /// <param name="cardId">The card id.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>true when timeshift files is at least of 300kb, else timeshift file is less then 300kb</returns>
    public bool WaitForTimeShiftFile(ref User user, string fileName)
    {
      if (_cardHandler.DataBaseCard.Enabled == false) return false;
      Log.Write("card: WaitForTimeShiftFile");
      if (!WaitForUnScrambledSignal(ref user)) return false;
      DateTime timeStart = DateTime.Now;
      ulong fileSize = 0;
			ulong initialFileSize = 0;
      if (_cardHandler.Card.SubChannels.Length <= 0) return false;
      IChannel channel = _cardHandler.Card.SubChannels[0].CurrentChannel;
      bool isRadio = channel.IsRadio;
      ulong minTimeShiftFile = 500 * 1024;//300Kb
      if (isRadio)
        minTimeShiftFile = 100 * 1024;//100Kb

      timeStart = DateTime.Now;
			bool initialFileSizeRetrieved = false;

			FileStream stream = null;
			BinaryReader reader = null;
			
      try
      {				
        while (true)
        {					
          if (System.IO.File.Exists(fileName))
          {
						if (stream == null)
						{						
							stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
						}						
            if (stream.Length > 0)
            {							
							if (reader == null)
							{
								reader = new BinaryReader(stream);
							}

							if (!initialFileSizeRetrieved)
							{
								try
								{
									initialFileSizeRetrieved = true;
									stream.Seek(0, SeekOrigin.Begin);
									initialFileSize = reader.ReadUInt64();									
								}
								catch (Exception e)
								{
									if (reader != null)
									{
										reader.Close();
										reader = null;
									}
								}
							}
							stream.Seek(0, SeekOrigin.Begin);
							ulong newfileSize = 0;
							try
							{
								if (reader == null)
								{
									reader = new BinaryReader(stream);
								}
								newfileSize = reader.ReadUInt64();
							}
							catch (Exception e)
							{
								Log.Write(e.Message);
								if (reader != null)
								{
									reader.Close();
									reader = null;
								}
							}
							if (newfileSize > fileSize)
							{
								Log.Write("card: timeshifting fileSize:{0}", fileSize);
								timeStart = DateTime.Now;
							}
							fileSize = newfileSize;

							if (fileSize >= (initialFileSize + minTimeShiftFile))
							{
								TimeSpan ts = DateTime.Now - timeStart;
								Log.Write("card: timeshifting fileSize:{0} {1}", fileSize, ts.TotalMilliseconds);
								return true;
							}              
            }            
          }

          System.Threading.Thread.Sleep(100);
          TimeSpan timeOut = DateTime.Now - timeStart;
          if (timeOut.TotalMilliseconds >= 15000)
          {						
            Log.Write("card: timeshifting fileSize:{0} TIMEOUT", fileSize);
            return false;
          }
        }
      }			
      catch (Exception ex)
      {
        Log.Write(ex);
      }
			finally
			{
				if (reader != null)
				{
					reader.Close();
					reader = null;
				}

				if (stream != null)
				{
					stream.Close();
					stream.Dispose();
				}
			}
      return false;
    }

  }
}
