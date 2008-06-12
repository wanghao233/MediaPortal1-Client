/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
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
using System.Collections.Generic;
using System.Text;
using TvControl;

using Gentle.Common;
using Gentle.Framework;
using TvDatabase;
using TvLibrary;
using TvLibrary.Log;
using TvLibrary.Interfaces;
using TvLibrary.Channels;
using TvLibrary.Epg;
using TvEngine.Events;
using System.Threading;
using System.Collections.Specialized;

namespace TvService
{
  /// <summary>
  /// Class which will  grab the epg for for a specific card
  public class EpgCard : BaseEpgGrabber, IDisposable
  {
    #region const
    const int EpgGrabInterval = 60;//secs
    int _epgTimeOut = (10 * 60);// 10 mins
    #endregion

    #region enums
    enum EpgState
    {
      Idle,
      Grabbing,
      Updating,
      Stopped
    }

    #endregion

    #region variables
    System.Timers.Timer _epgTimer = new System.Timers.Timer();
    EpgState _state;
    DateTime _lastEpgGrabTime;

    bool _reEntrant = false;
    List<EpgChannel> _epg;

    DateTime _grabStartTime;
    bool _isRunning;
    TVController _tvController;

    Transponder _currentTransponder;
    
    TvServerEventHandler _eventHandler;
    bool _disposed = false;
    Card _card;
    User _user;
    EpgDBUpdater _dbUpdater;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="controller">instance of a TVController</param>
    public EpgCard(TVController controller, Card card)
    {
      _card = card;
      _user = new User("epg", false, -1);

      _tvController = controller;
      _lastEpgGrabTime = DateTime.MinValue;
      _grabStartTime = DateTime.MinValue;
      _state = EpgState.Idle;

      _epgTimer.Interval = 30000;
      _epgTimer.Elapsed += new System.Timers.ElapsedEventHandler(_epgTimer_Elapsed);
      _eventHandler = new TvServerEventHandler(controller_OnTvServerEvent);
      _dbUpdater = new EpgDBUpdater("IdleEpgGrabber", true);
      controller.OnTvServerEvent += _eventHandler;
    }

    #endregion

    #region properties

    /// <summary>
    /// Gets the card.
    /// </summary>
    /// <value>The card.</value>
    public Card Card
    {
      get
      {
        return _card;
      }
    }

    /// <summary>
    /// Property which returns true if EPG grabber is currently grabbing the epg
    /// or false is epg grabber is idle
    /// </summary>
    public bool IsRunning
    {
      get
      {
        return _isRunning;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is grabbing.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is grabbing; otherwise, <c>false</c>.
    /// </value>
    public bool IsGrabbing
    {
      get
      {
        return (_state != EpgState.Idle);
      }
    }
    #endregion

    #region epg callback

    /// <summary>
    /// Gets called when epg has been cancelled
    /// Should be overriden by the class
    /// </summary>
    public override void OnEpgCancelled()
    {
      Log.Epg("epg grabber:epg cancelled");

      if (_state == EpgState.Idle) return;
      _state = EpgState.Idle;
      _tvController.StopGrabbingEpg(_user);
      _user.CardId = -1;
      return;
    }

    /// <summary>
    /// Callback fired by the tvcontroller when EPG data has been received
    /// The method checks if epg grabbing was in progress and ifso creates a new workerthread
    /// to update the database with the new epg data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="epg">new epg data</param>
    public override int OnEpgReceived()
    {
      try
      {
        //is epg grabbing in progress?
        /*if (_state == EpgState.Idle)
        {
          Log.Epg("Epg: card:{0} OnEpgReceived while idle", _user.CardId);
          return 0;
        }*/
        //is epg grabber already updating the database?

        if (_state == EpgState.Updating)
        {
          Log.Epg("Epg: card:{0} OnEpgReceived while updating", _user.CardId);
          return 0;
        }

        //is the card still idle?
        if (IsCardIdle(_user) == false)
        {
          Log.Epg("Epg: card:{0} OnEpgReceived but card is not idle", _user.CardId);
          _state = EpgState.Idle;
          _tvController.StopGrabbingEpg(_user);
          _user.CardId = -1;
          _currentTransponder.InUse = false;
          return 0;
        }

        List<EpgChannel> epg = _tvController.Epg(_user.CardId);
        if (epg == null)
        {
          epg = new List<EpgChannel>();
        }
        //did we receive epg info?
        if (epg.Count == 0)
        {
          //no epg found for this transponder
          Log.Epg("Epg: card:{0} no epg found", _user.CardId);
          _currentTransponder.InUse = false;
          _currentTransponder.OnTimeOut();

          _state = EpgState.Idle;
          _tvController.StopGrabbingEpg(_user);
          _tvController.StopCard(_user);
          _user.CardId = -1;
          return 0;
        }


          //create worker thread to update the database
          Log.Epg("Epg: card:{0} received epg for {1} channels", _user.CardId, epg.Count);
          _state = EpgState.Updating;
          _epg = epg;
          Thread workerThread = new Thread(new ThreadStart(UpdateDatabaseThread));
          workerThread.IsBackground = true;
          workerThread.Name = "EPG Update thread";
          workerThread.Start();

      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return 0;
    }
    #endregion

    #region public members
    /// <summary>
    /// Grabs the epg.
    /// </summary>
    /// <param name="transponders">The transponders.</param>
    /// <param name="index">The index.</param>
    /// <param name="channel">The channel.</param>
    public void GrabEpg()
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      Setting s = layer.GetSetting("timeoutEPG", "10");
      if (Int32.TryParse(s.Value, out _epgTimeOut) == false)
      {
        _epgTimeOut = 10;
      }
      _currentTransponder = TransponderList.Instance.CurrentTransponder;
      Channel channel = _currentTransponder.CurrentChannel;

      Log.Epg("grab epg card:#{0} transponder: #{1} ch:{2} ", _card.IdCard, TransponderList.Instance.CurrentIndex, channel.Name);

      _state = EpgState.Idle;
      _isRunning = true;
      _user = new User("epg", false, -1);
      if (GrabEpgForChannel(channel, _currentTransponder.Tuning, _card))
      {
        Log.Epg("Epg: card:{0} start grab {1}", _user.CardId, _currentTransponder.Tuning.ToString());
        _currentTransponder.InUse = true;
        //succeeded, then wait for epg to be received
        _state = EpgState.Grabbing;
        _grabStartTime = DateTime.Now;
        _epgTimer.Enabled = true;
        return;
      }
      else
      {
        Log.Epg("unable to grab epg transponder: {0} ch: {1} started on {2}", TransponderList.Instance.CurrentIndex, channel.Name, _user.CardId);
        Log.Epg("{0}", _currentTransponder.Tuning.ToString());
      }
    }

    /// <summary>
    /// Stops this instance.
    /// </summary>
    public void Stop()
    {
      if (_user.CardId >= 0)
      {
        Log.Epg("Epg: card:{0} stop grab", _user.CardId);
        _tvController.StopGrabbingEpg(_user);
      }
      if (_currentTransponder!=null)
        _currentTransponder.InUse = false;

      _epgTimer.Enabled = false;
      _isRunning = false;
    }

    #endregion

    #region private members
    /// <summary>
    /// timer callback.
    /// This method is called by a timer every 30 seconds to wake up the epg grabber
    /// the epg grabber will check if its time to grab the epg for a channel
    /// and ifso it starts the grabbing process
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void _epgTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      //security check, dont allow re-entrancy here
      if (_reEntrant) return;

      try
      {
        _reEntrant = true;
        //if epg grabber is idle, then grab epg for the next channel
        if (_state == EpgState.Idle)
        {
          return;
        }
        else if (_state == EpgState.Grabbing)
        {
          //if we are grabbing epg, then check if card is still idle
          if (_user.CardId >= 0)
          {
            if (!IsCardIdle(_user))
            {
              //not idle? then cancel epg grabbing
              if (_state != EpgState.Idle)
              {
                Log.Epg("Canceled epg, card is not idle:{0}", _user.CardId);
              }
              _state = EpgState.Idle;
              _user.CardId = -1;
              _currentTransponder.InUse = false;
              _reEntrant = false;
              return;
            }
          }

          //wait until grabbing has finished
          TimeSpan ts = DateTime.Now - _grabStartTime;
          if (ts.TotalMinutes > _epgTimeOut)
          {
            //epg grabber timed out. Update database
            //and go back to idle mode
            Log.Epg("Epg: card:{0} timeout after {1} mins", _user.CardId, ts.TotalMinutes);
            _tvController.AbortEPGGrabbing(_user.CardId);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      finally
      {
        _reEntrant = false;
      }
    }

    /// <summary>
    /// This method will try to start the epg grabber for the channel and tuning details specified
    /// Epg grabbing can only be started if there is a card idle which can receive the channel specified
    /// </summary>
    /// <param name="channel">channel to grab/param>
    /// <param name="tuning">tuning information</param>
    /// <param name="card">card to use for grabbing</param>
    /// <returns>true if grabbing has started else false</returns>
    bool GrabEpgForChannel(Channel channel, IChannel tuning, Card card)
    {
      if (channel == null)
      {
        Log.Error("Epg: invalid channel");
        return false;
      }
      if (tuning == null)
      {
        Log.Error("Epg: invalid tuning");
        return false;
      }
      if (card == null)
      {
        Log.Error("Epg: invalid card");
        return false;
      }
      if (_tvController == null)
      {
        Log.Error("Epg: invalid tvcontroller");
        return false;
      }
      if (_user == null)
      {
        Log.Error("Epg: invalid user");
        return false;
      }
      //remove following check to enable multi-card epg grabbing (still beta)
      if (_tvController.AllCardsIdle == false)
      {
        Log.Epg("Epg: card:{0} cards are not idle", card.IdCard);
        return false;
      }
      IList dbsCards = Card.ListAll();

      TvResult result;
      //handle ATSC
      ATSCChannel atscChannel = tuning as ATSCChannel;
      if (atscChannel != null)
      {
        if (_tvController.Type(card.IdCard) == CardType.Atsc)
        {
          if (IsCardIdle(card.IdCard) == false)
          {
            Log.Epg("Epg: card:{0} atsc card is not idle", card.IdCard);
            return false;//card is busy
          }
          try
          {
            User cardUser;
            if (_tvController.IsCardInUse(card.IdCard, out cardUser) == false)
            {
              _user.CardId = card.IdCard;
              result = RemoteControl.Instance.Tune(ref _user, tuning, channel.IdChannel);
              if (result == TvResult.Succeeded)
              {
                if (!_isRunning || false == _tvController.GrabEpg(this, card.IdCard))
                {
                  if (!_isRunning)
                    Log.Epg("Tuning finished but EpgGrabber no longer enabled");
                  _tvController.StopGrabbingEpg(_user);
                  _user.CardId = -1;
                  Log.Epg("Epg: card:{0} could not start atsc epg grabbing", card.IdCard);
                  return false;
                }
                _user.CardId = card.IdCard;
                return true;
              }
              else
              {
                _user.CardId = -1;
                Log.Epg("Epg: card:{0} could not tune to channel:{1}", card.IdCard, result.ToString());
                return false;
              }
            }
          }
          catch (Exception ex)
          {
            Log.Write(ex);
            throw ex;
          }
          return false;
        }
        else
        {
          Log.Epg("Epg: card:{0} could not tune to atsc channel:{1}", card.IdCard, tuning.ToString());
        }
        return false;
      }

      //handle DVBC
      DVBCChannel dvbcChannel = tuning as DVBCChannel;
      if (dvbcChannel != null)
      {
        if (_tvController.Type(card.IdCard) == CardType.DvbC)
        {
          if (IsCardIdle(card.IdCard) == false)
          {
            Log.Epg("Epg: card:{0} dvbc card is not idle", card.IdCard);
            return false;//card is busy
          }
          try
          {
            _user.CardId = card.IdCard;
            result = RemoteControl.Instance.Tune(ref _user, tuning, channel.IdChannel);
            if (result == TvResult.Succeeded)
            {
              if (!_isRunning || false == _tvController.GrabEpg(this, card.IdCard))
              {
                if (!_isRunning)
                  Log.Epg("Tuning finished but EpgGrabber no longer enabled");
                _tvController.StopGrabbingEpg(_user);
                _user.CardId = -1;
                Log.Epg("Epg: card:{0} could not start dvbc epg grabbing", card.IdCard);
                return false;
              }
              _user.CardId = card.IdCard;
              return true;
            }
            else
            {
              _user.CardId = -1;
              Log.Epg("Epg: card:{0} could not tune to channel:{1}", card.IdCard, result.ToString());
              return false;
            }
          }
          catch (Exception ex)
          {
            Log.Write(ex);
            throw ex;
          }
          //unreachable return false;
        }
        else
        {
          Log.Epg("Epg: card:{0} could not tune to dvbc channel:{1}", card.IdCard, tuning.ToString());
        }
        return false;
      }

      //handle DVBS
      DVBSChannel dvbsChannel = tuning as DVBSChannel;
      if (dvbsChannel != null)
      {
        if (_tvController.Type(card.IdCard) == CardType.DvbS)
        {
          if (IsCardIdle(card.IdCard) == false)
          {
            Log.Epg("Epg: card:{0} dvbs card is not idle", card.IdCard);
            return false;//card is busy
          }
          try
          {
            _user.CardId = card.IdCard;
            result = RemoteControl.Instance.Tune(ref _user, tuning, channel.IdChannel);
            if (result == TvResult.Succeeded)
            {
              if (!_isRunning || false == _tvController.GrabEpg(this, card.IdCard))
              {
                if (!_isRunning)
                  Log.Epg("Tuning finished but EpgGrabber no longer enabled");
                _tvController.StopGrabbingEpg(_user);
                _user.CardId = -1;
                Log.Epg("Epg: card:{0} could not start dvbs epg grabbing", card.IdCard);
                return false;
              }
              _user.CardId = card.IdCard;
              return true;
            }
            else
            {
              _user.CardId = -1;
              Log.Epg("Epg: card:{0} could not tune to channel:{1}", card.IdCard, result.ToString());
              return false;
            }
          }
          catch (Exception ex)
          {
            Log.Write(ex);
            throw ex;
          }
          //unreachable return false;
        }
        else
        {
          Log.Epg("Epg: card:{0} could not tune to dvbs channel:{1}", card.IdCard, tuning.ToString());
        }
        return false;
      }

      //handle DVBT
      DVBTChannel dvbtChannel = tuning as DVBTChannel;
      if (dvbtChannel != null)
      {
        if (_tvController.Type(card.IdCard) == CardType.DvbT)
        {
          if (IsCardIdle(card.IdCard) == false)
          {
            Log.Epg("Epg: card:{0} dvbt card is not idle", card.IdCard);
            return false;//card is busy
          }
          try
          {
            _user.CardId = card.IdCard;
            result = RemoteControl.Instance.Tune(ref _user, tuning, channel.IdChannel);
            if (result == TvResult.Succeeded)
            {
              if (!_isRunning || false == _tvController.GrabEpg(this, card.IdCard))
              {
                if (!_isRunning)
                  Log.Epg("Tuning finished but EpgGrabber no longer enabled");
                _tvController.StopGrabbingEpg(_user);
                _user.CardId = -1;
                Log.Epg("Epg: card:{0} could not start atsc grabbing", card.IdCard);
                return false;
              }
              _user.CardId = card.IdCard;
              return true;
            }
            else
            {
              _user.CardId = -1;
              Log.Epg("Epg: card:{0} could not tune to channel:{1}", card.IdCard, result.ToString());
              return false;
            }
          }
          catch (Exception ex)
          {
            Log.Write(ex);
            throw ex;
          }
          //unreachable return false;
        }
        else
        {
          Log.Epg("Epg: card:{0} could not tune to dvbt channel:{1}", card.IdCard, tuning.ToString());
        }
        return false;
      }
      Log.Epg("Epg: card:{0} could not tune to channel:{1}", card.IdCard, tuning.ToString());
      return false;
    }

    #region database update routines
    /// <summary>
    /// workerthread which will update the database with the new epg received
    /// </summary>
    void UpdateDatabaseThread()
    {
      System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;

      //if card is not idle anymore we return
      if (IsCardIdle(_user) == false)
      {
        _currentTransponder.InUse = false;
        return;
      }
      TvBusinessLayer layer = new TvBusinessLayer();
      Log.Epg("Epg: card:{0} Updating database with new programs", _user.CardId);
      bool timeOut = false;
      _dbUpdater.ReloadConfig();
      try
      {
        foreach (EpgChannel epgChannel in _epg)
        {
          _dbUpdater.UpdateEpgForChannel(epgChannel);
          if (_state != EpgState.Updating)
          {
            Log.Epg("Epg: card:{0} stopped updating state changed", _user.CardId);
            timeOut = true;
            return;
          }
          if (IsCardIdle(_user) == false)
          {
            Log.Epg("Epg: card:{0} stopped updating card not idle", _user.CardId);
            timeOut = true;
            return;
          }
        }
        _epg.Clear();
        Log.Epg("Epg: card:{0} Finished updating the database.",_user.CardId);
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      finally
      {
        if (timeOut == false)
        {
          _currentTransponder.OnTimeOut();
        }
        if (_state != EpgState.Idle && _user.CardId >= 0)
        {
          _tvController.StopGrabbingEpg(_user);
          _tvController.StopCard(_user);
        }
        _state = EpgState.Idle;
        _user.CardId = -1;
				_tvController.Fire(this, new TvServerEventArgs(TvServerEventType.ProgramUpdated));
      }
    }
    #endregion

    #region helper methods
    /// <summary>
    /// Method which returns if the card is idle or not
    /// </summary>
    /// <param name="cardId">id of card</param>
    /// <returns>true if card is idle, otherwise false</returns>
    bool IsCardIdle(int cardId)
    {
      if (_isRunning == false) return false;
      if (cardId < 0) return false;
      User user = new User();
      user.CardId = cardId;
      if (RemoteControl.Instance.IsRecording(ref user)) return false;
      if (RemoteControl.Instance.IsTimeShifting(ref user)) return false;
      if (RemoteControl.Instance.IsScanning(user.CardId)) return false;
      User cardUser;
      if (_tvController.IsCardInUse(cardId, out cardUser)) return false;
      if (_tvController.IsCardInUse(user.CardId, out cardUser))
      {
        if (cardUser != null)
        {
          if (cardUser.Name != _user.Name) return false;
          return true;
        }
        return false;
      }
      return true;
    }
    /// <summary>
    /// Method which returns if the card is idle or not
    /// </summary>
    /// <param name="cardId">id of card</param>
    /// <returns>true if card is idle, otherwise false</returns>
    bool IsCardIdle(User user)
    {
      if (_isRunning == false) return false;
      if (user.CardId < 0) return false;
      if (RemoteControl.Instance.IsRecording(ref _user)) return false;
      if (RemoteControl.Instance.IsTimeShifting(ref _user)) return false;
      if (RemoteControl.Instance.IsScanning(user.CardId)) return false;
      User cardUser;
      if (_tvController.IsCardInUse(user.CardId, out cardUser))
      {
        if (cardUser != null)
        {
          if (cardUser.Name != _user.Name) return false;
          return true;
        }
        return false;
      }
      return true;
    }

    /// <summary>
    /// Gets the channel.
    /// </summary>
    /// <param name="idChannel">The id channel.</param>
    /// <returns></returns>
    Channel GetChannel(int idChannel)
    {

      foreach (Transponder transponder in TransponderList.Instance)
      {
        foreach (Channel ch in transponder.Channels)
        {
          if (ch.IdChannel == idChannel) return ch;
        }
      }
      return Channel.Retrieve(idChannel);
    }

    #endregion
    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      if (!_disposed)
      {
        _tvController.OnTvServerEvent -= _eventHandler;
        _disposed = true;
      }
    }

    void controller_OnTvServerEvent(object sender, EventArgs eventArgs)
    {
      if (_state == EpgState.Idle) return;
      TvServerEventArgs tvArgs = eventArgs as TvServerEventArgs;
      if (eventArgs == null) return;
      switch (tvArgs.EventType)
      {
        case TvServerEventType.StartTimeShifting:
          Log.Epg("epg cancelled due to start timeshifting");
          StopEpg();
          break;

        case TvServerEventType.StartRecording:
          Log.Epg("epg cancelled due to start recording");
          StopEpg();
          break;
      }
    }

    void StopEpg()
    {
      _tvController.StopGrabbingEpg(_user);
    }

    #endregion
  }
}
