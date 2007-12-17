using System;
using System.Collections.Generic;
using System.Text;
using TvDatabase;
using ProjectInfinity;
using ProjectInfinity.Logging;
using ProjectInfinity.Localisation;
namespace MyTv
{
  public class RecordingModel
  {
    #region variables
    string _channel;
    Recording _recording;
    string _logo;
    #endregion

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingModel"/> class.
    /// </summary>
    /// <param name="recording">The recording.</param>
    public RecordingModel(Recording recording)
    {
      _recording = recording;
      _channel = _recording.ReferencedChannel().Name;
      _logo = System.IO.Path.ChangeExtension(recording.FileName, ".png");
      if (!System.IO.File.Exists(_logo))
      {
        _logo = "";
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the title.
    /// </summary>
    /// <value>The title.</value>
    public string Title
    {
      get
      {
        return _recording.Title;
      }
    }
    /// <summary>
    /// Gets the genre.
    /// </summary>
    /// <value>The genre.</value>
    public string Genre
    {
      get
      {
        return _recording.Genre;
      }
    }
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <value>The description.</value>
    public string Description
    {
      get
      {
        return _recording.Description;
      }
    }
    /// <summary>
    /// Gets the times watched.
    /// </summary>
    /// <value>The times watched.</value>
    public int TimesWatched
    {
      get
      {
        return _recording.TimesWatched;
      }
    }
    /// <summary>
    /// Gets the channel.
    /// </summary>
    /// <value>The channel.</value>
    public string Channel
    {
      get
      {
        return _channel;
      }
    }
    /// <summary>
    /// Gets the channel logo.
    /// </summary>
    /// <value>The logo.</value>
    public string Logo
    {
      get
      {
        return _logo;
      }
    }
    /// <summary>
    /// Gets the start time.
    /// </summary>
    /// <value>The start time.</value>
    public DateTime StartTime
    {
      get
      {
        return _recording.StartTime;
      }
    }
    /// <summary>
    /// Gets the end time.
    /// </summary>
    /// <value>The end time.</value>
    public DateTime EndTime
    {
      get
      {
        return _recording.EndTime;
      }
    }
    /// <summary>
    /// Gets the recording.
    /// </summary>
    /// <value>The recording.</value>
    public Recording Recording
    {
      get
      {
        return _recording;
      }
    }

    /// <summary>
    /// Gets the start-end label.
    /// </summary>
    /// <value>The start-end label.</value>
    public string StartEndLabel
    {
      get
      {
        string date = "";
        if (StartTime.Date == DateTime.Now.Date)
        {
          date = ServiceScope.Get<ILocalisation>().ToString("mytv", 133);//today
        }
        else if (StartTime.Date == DateTime.Now.Date.AddDays(1))
        {
          date = ServiceScope.Get<ILocalisation>().ToString("mytv", 134);//tomorrow
        }
        else
        {
          int dayofWeek = (int)StartTime.DayOfWeek;
          int month = StartTime.Month;
          date = String.Format("{0} {1} {2}", ServiceScope.Get<ILocalisation>().ToString("days", dayofWeek),
                                            StartTime.Day,
                                            ServiceScope.Get<ILocalisation>().ToString("months", month));
        }
        return String.Format("{0} {1}-{2}", date, StartTime.ToString("HH:mm"), EndTime.ToString("HH:mm"));
      }
    }
    /// <summary>
    /// Gets the duration.
    /// </summary>
    /// <value>The duration.</value>
    public string Duration
    {
      get
      {
        TimeSpan ts = EndTime - StartTime;
        if (ts.Minutes < 10)
          return String.Format("{0}:0{1}", ts.Hours, ts.Minutes);
        else
          return String.Format("{0}:B{1}", ts.Hours, ts.Minutes);
      }
    }
    #endregion
  }
}
