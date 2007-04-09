using System;
using System.Collections.Generic;
using System.Text;
using TvDatabase;
namespace MyTv
{
  public class ProgramModel
  {
    #region variables
    string _channel;
    Program _program;
    string _logo;
    #endregion

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramModel"/> class.
    /// </summary>
    /// <param name="program">The program.</param>
    public ProgramModel(Program program)
    {
      _program = program;
      _channel = _program.ReferencedChannel().Name;
      _logo = String.Format(@"{0}\{1}", System.IO.Directory.GetCurrentDirectory(), Thumbs.GetLogoFileName(_channel));
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
        return _program.Title;
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
        return _program.Genre;
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
        return _program.Description;
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
        return _program.StartTime;
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
        return _program.EndTime;
      }
    }
    /// <summary>
    /// Gets the program.
    /// </summary>
    /// <value>The program.</value>
    public Program Program
    {
      get
      {
        return _program;
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
        return String.Format("{0}-{1}", StartTime.ToString("HH:mm"), EndTime.ToString("HH:mm"));
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
