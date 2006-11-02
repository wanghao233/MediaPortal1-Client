/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
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
using System.Net;
using System.Net.Sockets;

using TvLibrary.Log;
using TvDatabase;
using TvControl;
using TvEngine.Events;

namespace TvService
{
  /// <summary>
  /// Scheduler class.
  /// This class will take care of recording all schedules in the database
  /// </summary>
  public class Scheduler
  {
    #region const
    const int ScheduleInterval = 30;//secs
    #endregion

    #region variables
    DiskManagement _diskManagement;
    EpisodeManagement _episodeManagement;
    RecordingManagement _recordingManagement;
    IController _controller;
    TVController _tvController;
    bool _reEntrant = false;
    System.Timers.Timer _timer = new System.Timers.Timer();
    DateTime _scheduleCheckTimer;
    List<RecordingDetail> _recordingsInProgressList;
    IList _channels;
    int _preRecordInterval = 5;
    int _postRecordInterval = 5;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="controller">instance of the TVController</param>
    public Scheduler(TVController controller)
    {
      _tvController = controller;
    }
    #endregion

    #region public members
    /// <summary>
    /// Resets the scheduler timer. This causes the scheduler to immediatly check
    /// if any schedule should be recorded
    /// </summary>
    public void ResetTimer()
    {
      _scheduleCheckTimer = DateTime.MinValue;
    }

    /// <summary>
    /// Starts the scheduler
    /// </summary>
    public void Start()
    {
      Log.Write("Scheduler: started");
      _controller = RemoteControl.Instance;
      _recordingsInProgressList = new List<RecordingDetail>();
      _channels = Channel.ListAll();
      IList schedules = Schedule.ListAll();
      Log.Write("Scheduler: loaded {0} schedules", schedules.Count);
      _scheduleCheckTimer = DateTime.MinValue;
      _timer.Interval = 1000;
      _timer.Enabled = true;
      _timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

      _diskManagement = new DiskManagement();
      _recordingManagement = new RecordingManagement();
      _episodeManagement = new EpisodeManagement();
    }

    /// <summary>
    /// Stops the scheduler
    /// </summary>
    public void Stop()
    {
      Log.Write("Scheduler: stopped");
      _timer.Enabled = false;

      _diskManagement = null;
      _recordingManagement = null;
      _episodeManagement = null;

    }
    #endregion

    #region private members
    /// <summary>
    /// Timer callback which gets fired every 30 seconds
    /// The method will check if a schedule should be started to record
    /// and/or if a recording which is currently being recorded should be stopped
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      //security check, dont allow re-entrancy here
      if (_reEntrant) return;

      try
      {
        _reEntrant = true;
        TimeSpan ts = DateTime.Now - _scheduleCheckTimer;
        if (ts.TotalSeconds < ScheduleInterval) return;

        DoSchedule();
        HandleRecordingList();
        _scheduleCheckTimer = DateTime.Now;
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
    /// DoSchedule() will start recording any schedule if its time todo so
    /// </summary>
    void DoSchedule()
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      _preRecordInterval = Int32.Parse(layer.GetSetting("preRecordInterval", "5").Value);
      _postRecordInterval = Int32.Parse(layer.GetSetting("postRecordInterval", "5").Value);

      DateTime now = DateTime.Now;
      IList schedules = Schedule.ListAll();
      foreach (Schedule schedule in schedules)
      {
        //if schedule has been canceled then do nothing
        if (schedule.Canceled != Schedule.MinSchedule) continue;

        //if we are already recording this schedule then do nothing
        int cardId;
        if (IsRecordingSchedule(schedule.IdSchedule, out cardId)) continue;

        //check if its time to record this schedule.
        RecordingDetail newRecording;
        if (IsTimeToRecord(schedule, now, out newRecording))
        {
          //yes, then lets recording it
          StartRecord(newRecording, GetUser());
        }
      }
    }

    /// <summary>
    /// HandleRecordingList() will stop any recording which should be stopped
    /// </summary>
    void HandleRecordingList()
    {
      while (true)
      {
        bool removed = false;
        foreach (RecordingDetail recording in _recordingsInProgressList)
        {
          if (!recording.IsRecording)
          {
            StopRecord(recording);
            removed = true;
            break;
          }
        }

        if (removed == false)
        {
          return;
        }
      }
    }

    /// <summary>
    /// Method which checks if its time to record the schedule specified
    /// </summary>
    /// <param name="schedule">Schedule</param>
    /// <param name="currentTime">current Date/Time</param>
    /// <param name="newRecording">Recording detail which is used to further process the recording</param>
    /// <returns>true if schedule should be recorded now, else false</returns>
    bool IsTimeToRecord(Schedule schedule, DateTime currentTime, out RecordingDetail newRecording)
    {
      newRecording = null;
      ScheduleRecordingType type = (ScheduleRecordingType)schedule.ScheduleType;
      if (type == ScheduleRecordingType.Once)
      {
        if (currentTime >= schedule.StartTime.AddMinutes(-_preRecordInterval) &&
            currentTime <= schedule.EndTime.AddMinutes(_postRecordInterval))
        {

          newRecording = new RecordingDetail(schedule, schedule.ReferencedChannel().Name, schedule.EndTime.AddMinutes(_postRecordInterval));
          return true;
        }
        return false;
      }

      if (type == ScheduleRecordingType.Daily)
      {
        DateTime start = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
        DateTime end = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);
        if (currentTime >= start.AddMinutes(-_preRecordInterval) &&
            currentTime <= end.AddMinutes(_postRecordInterval))
        {
          if (!schedule.IsSerieIsCanceled(start))
          {
            newRecording = new RecordingDetail(schedule, schedule.ReferencedChannel().Name, end.AddMinutes(_postRecordInterval));
            return true;
          }
        }
        return false;
      }

      if (type == ScheduleRecordingType.Weekends)
      {
        if (currentTime.DayOfWeek == DayOfWeek.Saturday || currentTime.DayOfWeek == DayOfWeek.Sunday)
        {
          DateTime start = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
          DateTime end = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);
          if (currentTime >= start.AddMinutes(-_preRecordInterval) &&
              currentTime <= end.AddMinutes(_postRecordInterval))
          {

            if (!schedule.IsSerieIsCanceled(start))
            {
              newRecording = new RecordingDetail(schedule, schedule.ReferencedChannel().Name, end.AddMinutes(_postRecordInterval));
              return true;
            }
          }
        }
        return false;
      }
      if (type == ScheduleRecordingType.WorkingDays)
      {
        if (currentTime.DayOfWeek != DayOfWeek.Saturday && currentTime.DayOfWeek != DayOfWeek.Sunday)
        {
          DateTime start = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
          DateTime end = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);
          if (currentTime >= start.AddMinutes(-_preRecordInterval) &&
              currentTime <= end.AddMinutes(_postRecordInterval))
          {
            if (!schedule.IsSerieIsCanceled(start))
            {
              newRecording = new RecordingDetail(schedule, schedule.ReferencedChannel().Name, end.AddMinutes(_postRecordInterval));
              return true;
            }
          }
        }
        return false;
      }

      if (type == ScheduleRecordingType.Weekly)
      {
        if (currentTime.DayOfWeek == schedule.StartTime.DayOfWeek)
        {
          DateTime start = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
          DateTime end = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);
          if (currentTime >= start.AddMinutes(-_preRecordInterval) &&
              currentTime <= end.AddMinutes(_postRecordInterval))
          {
            if (!schedule.IsSerieIsCanceled(start))
            {
              newRecording = new RecordingDetail(schedule, schedule.ReferencedChannel().Name, end.AddMinutes(_postRecordInterval));
              return true;
            }
          }
        }
        return false;
      }

      if (type == ScheduleRecordingType.EveryTimeOnThisChannel)
      {
        TvDatabase.Program current = schedule.ReferencedChannel().CurrentProgram;
        TvDatabase.Program next = schedule.ReferencedChannel().NextProgram;
        if (current != null)
        {
          if (currentTime >= current.StartTime.AddMinutes(-_preRecordInterval) && currentTime <= current.EndTime.AddMinutes(_postRecordInterval))
          {
            if (String.Compare(current.Title, schedule.ProgramName, true) == 0)
            {
              if (!schedule.IsSerieIsCanceled(current.StartTime))
              {
                newRecording = new RecordingDetail(schedule, current.ReferencedChannel().Name, current.EndTime.AddMinutes(_postRecordInterval));
                return true;
              }
            }
          }
        }
        if (next != null)
        {
          if (currentTime >= next.StartTime.AddMinutes(-_preRecordInterval) && currentTime <= next.EndTime.AddMinutes(_postRecordInterval))
          {
            if (String.Compare(next.Title, schedule.ProgramName, true) == 0)
            {
              if (!schedule.IsSerieIsCanceled(next.StartTime))
              {
                newRecording = new RecordingDetail(schedule, next.ReferencedChannel().Name, next.EndTime.AddMinutes(_postRecordInterval));
                return true;
              }
            }
          }
        }
      }

      if (type == ScheduleRecordingType.EveryTimeOnEveryChannel)
      {
        foreach (Channel channel in _channels)
        {
          TvDatabase.Program current = channel.CurrentProgram;
          TvDatabase.Program next = channel.NextProgram;
          if (current != null)
          {
            if (currentTime >= current.StartTime.AddMinutes(-_preRecordInterval) && currentTime <= current.EndTime.AddMinutes(_postRecordInterval))
            {
              if (String.Compare(current.Title, schedule.ProgramName, true) == 0)
              {
                if (!schedule.IsSerieIsCanceled(current.StartTime))
                {
                  newRecording = new RecordingDetail(schedule, current.ReferencedChannel().Name, current.EndTime.AddMinutes(_postRecordInterval));
                  return true;
                }
              }
            }
          }
          if (next != null)
          {
            if (currentTime >= next.StartTime.AddMinutes(-_preRecordInterval) && currentTime <= next.EndTime.AddMinutes(_postRecordInterval))
            {
              if (String.Compare(next.Title, schedule.ProgramName, true) == 0)
              {
                if (!schedule.IsSerieIsCanceled(next.StartTime))
                {
                  newRecording = new RecordingDetail(schedule, next.ReferencedChannel().Name, next.EndTime.AddMinutes(_postRecordInterval));
                  return true;
                }
              }
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Starts recording the recording specified
    /// </summary>
    /// <param name="recording">Recording instance</param>
    /// <returns>true if recording is started, otherwise false</returns>
    bool StartRecord(RecordingDetail recording, User user)
    {
      Log.Write("Scheduler : time to record {0} {1}-{2} {3}", recording.Channel, DateTime.Now, recording.EndTime, recording.Schedule.ProgramName);
      TvResult result;
      List<CardDetail> freeCards = _tvController.GetFreeCardsForChannelName(recording.Channel, user, out result);
      if (freeCards.Count == 0) return false;
      CardDetail cardInfo = freeCards[0];
      Log.Write("Scheduler : record on card:{0} priority:{1}", cardInfo.Id, cardInfo.Card.Priority);
      bool lockedCard = false;
      try
      {
        if (_tvController.IsTimeShifting(cardInfo.Id) == false)
        {
          //if card is idle, then we lock it.
          _tvController.LockCard(cardInfo.Id, user);
          lockedCard = true;
        }

        _tvController.Fire(this, new TvServerEventArgs(TvServerEventType.StartRecording, new VirtualCard(cardInfo.Id), user, recording.Schedule, null));

        if (cardInfo.Card.RecordingFolder == String.Empty)
          cardInfo.Card.RecordingFolder = System.IO.Directory.GetCurrentDirectory();

        Log.Write("Scheduler : record, first tune to channel");
        if (false == _controller.TuneScan(cardInfo.Id, cardInfo.TuningDetail))
        {
          if (lockedCard) _tvController.UnlockCard(cardInfo.Id);
          return false;
        }
        Log.Write("Scheduler : record, now start timeshift");
        string timeshiftFileName = String.Format(@"{0}\live{1}.ts", cardInfo.Card.RecordingFolder, cardInfo.Id);
        if (TvResult.Succeeded != _controller.StartTimeShifting(cardInfo.Id, timeshiftFileName))
        {
          if (lockedCard) _tvController.UnlockCard(cardInfo.Id);
          return false;
        }

        recording.MakeFileName(cardInfo.Card.RecordingFolder);
        recording.CardInfo = cardInfo;
        Log.Write("Scheduler : record to {0}", recording.FileName);
        string fileName = recording.FileName;
        if (false == _controller.StartRecording(cardInfo.Id, ref fileName, false, 0))
        {
          if (lockedCard) _tvController.UnlockCard(cardInfo.Id);
          return false;
        }
        recording.FileName = fileName;
        recording.RecordingStartDateTime = DateTime.Now;
        _recordingsInProgressList.Add(recording);
        _tvController.Fire(this, new TvServerEventArgs(TvServerEventType.RecordingStarted, new VirtualCard(cardInfo.Id), user, recording.Schedule, null));
        Log.Write("recList:count:{0} add scheduleid:{1} card:{2}", _recordingsInProgressList.Count, recording.Schedule.IdSchedule, recording.CardInfo.Card.Name);
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return true;
    }

    /// <summary>
    /// stops recording the specified recording 
    /// </summary>
    /// <param name="recording">Recording</param>
    void StopRecord(RecordingDetail recording)
    {
      try
      {
        Log.Write("Scheduler : stop record {0} {1}-{2} {3}", recording.Channel, recording.RecordingStartDateTime, recording.EndTime, recording.Schedule.ProgramName);
        _controller.StopRecording(recording.CardInfo.Id);

        bool stopTimeshifting = true;
        User user;
        _controller.IsCardInUse(recording.CardInfo.Id, out user);
        if (user != null && user.Name != GetUser().Name)
        {
          stopTimeshifting = false;
        }
        if (stopTimeshifting)
        {
          Log.Write("Scheduler : stop timeshifting");
          _controller.StopTimeShifting(recording.CardInfo.Id, GetUser());
        }

        int idServer = recording.CardInfo.Card.IdServer;
        Recording newRec = new Recording(recording.Schedule.IdChannel, recording.RecordingStartDateTime, DateTime.Now, recording.Program.Title,
                            recording.Program.Description, recording.Program.Genre, recording.FileName, (int)recording.Schedule.KeepMethod,
                            recording.Schedule.KeepDate, 0, idServer);
        newRec.Persist();
        _tvController.Fire(this, new TvServerEventArgs(TvServerEventType.RecordingEnded, new VirtualCard(recording.CardInfo.Id), GetUser(), recording.Schedule, newRec));


        //DatabaseManager.Instance.SaveChanges();

        PostProcessing processor = new PostProcessing();
        processor.Process(recording);
        if ((ScheduleRecordingType)recording.Schedule.ScheduleType == ScheduleRecordingType.Once)
        {
          recording.Schedule.Delete();
        }
        else
        {
          if (DateTime.Now < recording.Program.EndTime)
          {
            CanceledSchedule canceled = new CanceledSchedule(recording.Schedule.IdSchedule, recording.Program.StartTime);
            canceled.Persist();
          }
          _episodeManagement.OnScheduleEnded(recording.FileName, recording.Schedule, recording.Program);
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      finally
      {
        _recordingsInProgressList.Remove(recording);
      }
    }

    /// <summary>
    /// Method which returns which card is currently recording the Schedule with the specified scheduleid
    /// </summary>
    /// <param name="idSchedule">database id of the schedule</param>
    /// <param name="cardId">id of card currently recording the schedule or -1 if none is recording the schedule</param>
    /// <returns>if a card is recording the schedule, else false</returns>
    public bool IsRecordingSchedule(int idSchedule, out int cardId)
    {
      cardId = -1;
      foreach (RecordingDetail rec in _recordingsInProgressList)
      {
        if (rec.Schedule.IdSchedule == idSchedule)
        {
          cardId = rec.CardInfo.Id;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Stop recording the Schedule with the specified schedule id
    /// </summary>
    /// <param name="idSchedule">database schedule id</param>
    public void StopRecordingSchedule(int idSchedule)
    {
      Log.Write("recList:StopRecordingSchedule{0}", idSchedule);

      foreach (RecordingDetail rec in _recordingsInProgressList)
      {
        if (rec.Schedule.IdSchedule == idSchedule)
        {
          StopRecord(rec);
          return;
        }
      }
      return;
    }

    /// <summary>
    /// Method which returns the database schedule id for the card
    /// </summary>
    /// <param name="cardId">id of the card</param>
    /// <returns>id of schedule the card is recording or -1 if its not recording</returns>
    public int GetRecordingScheduleForCard(int cardId)
    {
      foreach (RecordingDetail rec in _recordingsInProgressList)
      {
        if (rec.CardInfo.Id == cardId)
        {
          return rec.Schedule.IdSchedule;
        }
      }
      return -1;
    }

    /// <summary>
    /// Stops recording on the card specified
    /// </summary>
    /// <param name="cardId">id of the card</param>
    public void StopRecordingOnCard(int cardId)
    {
      Log.Write("recList:StopRecordingOnCard{0}", cardId);
      foreach (RecordingDetail rec in _recordingsInProgressList)
      {
        if (rec.CardInfo.Id == cardId)
        {
          StopRecord(rec);
          return;
        }
      }
    }

    public User GetUser()
    {
      return new User("Scheduler", true);
    }
    #endregion
  }
}
