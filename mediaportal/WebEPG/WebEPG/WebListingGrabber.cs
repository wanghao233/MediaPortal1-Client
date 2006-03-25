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
using System.IO;
using System.Net;
using System.Web;
using System.Text;
using System.Threading;
using System.Collections;
using System.Globalization;
using MediaPortal.Webepg.Profile;
using MediaPortal.Webepg.GUI.Library;
using MediaPortal.TV.Database;
using MediaPortal.WebEPG;
using MediaPortal.Utils.Web;
using MediaPortal.Utils.Time;

namespace MediaPortal.EPG
{
  public enum Expect
  {
    Start,
    Morning,
    Afternoon
  }

  /// <summary>
  /// Summary description for Class1
  /// </summary>
  public class WebListingGrabber
  {
    WorldTimeZone _SiteTimeZone = null;
    string _strURLbase = string.Empty;
    string _strSubURL = string.Empty;
    string _strURLsearch = string.Empty;
    string _strID = string.Empty;
    string _strBaseDir = "";
    string _SubListingLink;
    string _strRepeat;
    string _strSubtitles;
    string _strEpNum;
    string _strEpTotal;
    string _removeProgramsList;
    string[] _strDayNames = null;
    string _strWeekDay;
    bool _grabLinked;
    bool _monthLookup;
    bool _searchRegex;
    bool _searchRemove;
    bool _timeAdjustOnly;
    int _listingTime;
    int _linkStart;
    int _linkEnd;
    int _maxListingCount;
    int _offsetStart;
    int _LastStart;
    int _grabDelay;
    int _guideDays;
    //int _addDays;
    bool _bNextDay;
    Profiler _templateProfile;
    //Parser _templateParser;
    Profiler _templateSubProfile;
    //Parser _templateSubParser;
    MediaPortal.Webepg.Profile.Xml _xmlreader;
    ArrayList _programs;
    ArrayList _dbPrograms;
    DateTime _StartGrab;
    int _dbLastProg;
    int _MaxGrabDays;
    int _GrabDay;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="maxGrabDays">The number of days to grab</param>
    /// <param name="baseDir">The baseDir for grabber files</param>
    public WebListingGrabber(int maxGrabDays, string baseDir)
    {
      _MaxGrabDays = maxGrabDays;
      _strBaseDir = baseDir;
    }

    public bool Initalise(string File)
    {
      string listingTemplate;

      Log.WriteFile(Log.LogType.Log, false, "WebEPG: Opening {0}", File);

      _xmlreader = new MediaPortal.Webepg.Profile.Xml(_strBaseDir + File);

      _strURLbase = _xmlreader.GetValueAsString("Listing", "BaseURL", "");
      if (_strURLbase == "")
      {
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: {0}: No BaseURL defined", File);
        return false;
      }

      _strURLsearch = _xmlreader.GetValueAsString("Listing", "SearchURL", "");
      _grabDelay = _xmlreader.GetValueAsInt("Listing", "GrabDelay", 500);
      _maxListingCount = _xmlreader.GetValueAsInt("Listing", "MaxCount", 0);
      _offsetStart = _xmlreader.GetValueAsInt("Listing", "OffsetStart", 0);
      _guideDays = _xmlreader.GetValueAsInt("Info", "GuideDays", 0);

      string strTimeZone = _xmlreader.GetValueAsString("Info", "TimeZone", "");
      if (strTimeZone != "")
      {
        _timeAdjustOnly = _xmlreader.GetValueAsBool("Info", "TimeAdjustOnly", false);
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, Local: {0}", TimeZone.CurrentTimeZone.StandardName);
        try
        {
          Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, Site : {0}", strTimeZone);
          Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, debug: {0}", _timeAdjustOnly);
          _SiteTimeZone = new WorldTimeZone(strTimeZone);
        }
        catch (ArgumentException)
        {
          Log.WriteFile(Log.LogType.Log, true, "WebEPG: TimeZone Not valid");
          _SiteTimeZone = null;
        }
      }
      else
      {
        _SiteTimeZone = null;
      }

      string ListingType = _xmlreader.GetValueAsString("Listing", "ListingType", "");

      switch(ListingType)
      {
      case "XML":
        XMLProfilerData data = new XMLProfilerData();
        data.ChannelEntry = _xmlreader.GetValueAsString("Listing", "ChannelEntry", "");
        data.StartEntry = _xmlreader.GetValueAsString("Listing", "StartEntry", "");
        data.EndEntry = _xmlreader.GetValueAsString("Listing", "EndEntry", "");
        data.TitleEntry = _xmlreader.GetValueAsString("Listing", "TitleEntry", "");
        data.SubtitleEntry = _xmlreader.GetValueAsString("Listing", "SubtitleEntry", "");
        data.DescEntry = _xmlreader.GetValueAsString("Listing", "DescEntry", "");
        data.GenreEntry = _xmlreader.GetValueAsString("Listing", "GenreEntry", "");
        data.XPath = _xmlreader.GetValueAsString("Listing", "XPath", "");
        _templateProfile = new XMLProfiler("", data);
        break;

      case "DATA":
        string strListingDelimitor = _xmlreader.GetValueAsString("Listing", "ListingDelimitor", "\n");
        string strDataDelimitor = _xmlreader.GetValueAsString("Listing", "DataDelimitor", "\t");
        listingTemplate = _xmlreader.GetValueAsString("Listing", "Template", "");
        if (listingTemplate == "")
        {
          Log.WriteFile(Log.LogType.Log, true, "WebEPG: {0}: No Template", File);
          return false;
        }
        _templateProfile = new DataProfiler(listingTemplate, strDataDelimitor[0], strListingDelimitor[0]);
        break;

      default: // HTML
        string strGuideStart = _xmlreader.GetValueAsString("Listing", "Start", "<body");
        string strGuideEnd = _xmlreader.GetValueAsString("Listing", "End", "</body");
        //bool bAhrefs = _xmlreader.GetValueAsBool("Listing", "Ahrefs", false);
        string tags = _xmlreader.GetValueAsString("Listing", "Tags", "T");
        string encoding = _xmlreader.GetValueAsString("Listing", "Encoding", "");
        listingTemplate = _xmlreader.GetValueAsString("Listing", "Template", "");
        if (listingTemplate == "")
        {
          Log.WriteFile(Log.LogType.Log, true, "WebEPG: {0}: No Template", File);
          return false;
        }
        //_templateProfile = new HTMLProfiler(listingTemplate, bAhrefs, strGuideStart, strGuideEnd);
        _templateProfile = new HTMLProfiler(listingTemplate, tags, strGuideStart, strGuideEnd, encoding);

        _searchRegex = _xmlreader.GetValueAsBool("Listing", "SearchRegex", false);
        if(_searchRegex)
        {
          _searchRemove = _xmlreader.GetValueAsBool("Listing", "SearchRemove", false);
          _strRepeat = _xmlreader.GetValueAsString("Listing", "SearchRepeat", "");
          _strSubtitles = _xmlreader.GetValueAsString("Listing", "SearchSubtitles", "");
          _strEpNum = _xmlreader.GetValueAsString("Listing", "SearchEpNum", "");
          _strEpTotal = _xmlreader.GetValueAsString("Listing", "SearchEpTotal", "");
        }

        _SubListingLink = _xmlreader.GetValueAsString("Listing", "SubListingLink", "");
        if(_SubListingLink != "")
        {
          string strSubStart = _xmlreader.GetValueAsString("SubListing", "Start", "<body");
          string strSubEnd = _xmlreader.GetValueAsString("SubListing", "End", "</body");
          string subencoding = _xmlreader.GetValueAsString("SubListing", "Encoding", "");
          _strSubURL = _xmlreader.GetValueAsString("SubListing", "URL", "");
          string Subtags = _xmlreader.GetValueAsString("SubListing", "Tags", "T");
          string sublistingTemplate = _xmlreader.GetValueAsString("SubListing", "Template", "");
          if (sublistingTemplate == "")
          {
            Log.WriteFile(Log.LogType.Log, true, "WebEPG: {0}: No SubTemplate", File);
            _SubListingLink="";
          }
          else
          {
            _templateSubProfile = new HTMLProfiler(sublistingTemplate, Subtags, strSubStart, strSubEnd, subencoding);
          }
        }

        string firstDay = _xmlreader.GetValueAsString("DayNames", "0", "");
        if(firstDay != "" && _guideDays != 0)
        {
          _strDayNames = new string[_guideDays];
          _strDayNames[0] = firstDay;
          for(int i=1; i < _guideDays; i++)
            _strDayNames[i] = _xmlreader.GetValueAsString("DayNames", i.ToString(), "");
        }
        break;
      }

      _monthLookup = _xmlreader.GetValueAsBool("DateTime", "Months", false);
      return true;
    }

    public long GetEpochTime(DateTime dtCurTime)
    {
      DateTime dtEpochStartTime = Convert.ToDateTime("1/1/1970 8:00:00 AM");
      TimeSpan ts = dtCurTime.Subtract(dtEpochStartTime);

      long epochtime;
      epochtime = ((((((ts.Days * 24) + ts.Hours) * 60) + ts.Minutes) * 60) + ts.Seconds);
      return epochtime;
    }

    public long GetEpochDate(DateTime dtCurTime)
    {
      DateTime dtEpochStartTime = Convert.ToDateTime("1/1/1970 8:00:00 AM");
      TimeSpan ts = dtCurTime.Subtract(dtEpochStartTime);

      long epochdate;
      epochdate = (ts.Days);
      return epochdate;
    }

    private int getMonth(string month)
    {
      if (_monthLookup)
        return _xmlreader.GetValueAsInt("DateTime", month, 0);
      else
        return int.Parse(month);
    }

    private string getGenre(string genre)
    {
      return _xmlreader.GetValueAsString("GenreMap", genre, genre);
    }

    private long GetLongDateTime(DateTime dt)
    {
      long lDatetime;

      lDatetime = dt.Year;
      lDatetime *= 100;
      lDatetime += dt.Month;
      lDatetime *= 100;
      lDatetime += dt.Day;
      lDatetime *= 100;
      lDatetime += dt.Hour;
      lDatetime *= 100;
      lDatetime += dt.Minute;
      lDatetime *= 100;
      // no seconds

      return lDatetime;
    }

    private TVProgram dbProgram(string Title, long Start)
    {
      if (_dbPrograms.Count > 0)
      {
        for (int i = _dbLastProg; i < _dbPrograms.Count; i++)
        {
          TVProgram prog = (TVProgram) _dbPrograms[i];

          if (prog.Title == Title && prog.Start == Start)
          {
            _dbLastProg = i;
            return prog;
          }
        }

        for (int i = 0; i < _dbLastProg; i++)
        {
          TVProgram prog = (TVProgram) _dbPrograms[i];

          if (prog.Title == Title && prog.Start == Start)
          {
            _dbLastProg = i;
            return prog;
          }
        }
      }
      return null;
    }

    private bool AdjustTime(ref ProgramData guideData, ref TVProgram program)
    {
      int addDays = 1;

      Log.WriteFile(Log.LogType.Log, false, "WebEPG: Guide, Program Debug: {0}:{1} - {2}", guideData.StartTime[0], guideData.StartTime[1], guideData.Title);

      // Day
      if (guideData.Day == 0)
      {
        guideData.Day = _StartGrab.Day;
      }
      else
      {
        if (guideData.Day != _StartGrab.Day && _listingTime != (int)Expect.Start)
        {
          _GrabDay++;
          _StartGrab = _StartGrab.AddDays(1);
          _bNextDay = false;
          _LastStart = 0;
          _listingTime = (int)Expect.Morning;
        }
      }

      // Start Time
      switch (_listingTime)
      {
      case (int)Expect.Start:
        if (_GrabDay == 0)
        {
          if (guideData.StartTime[0] < _StartGrab.Hour)
            return false;

          if (guideData.StartTime[0] <= 12)
          {
            _listingTime = (int)Expect.Morning;
            goto case (int)Expect.Morning;
          }

          _listingTime = (int)Expect.Afternoon;
          goto case (int)Expect.Afternoon;
        }

        if (guideData.StartTime[0] >= 20)
          return false;				// Guide starts on pervious day ignore these listings.

        _listingTime = (int)Expect.Morning;
        goto case (int)Expect.Morning;      // Pass into Morning Code

      case (int)Expect.Morning:
        if (_LastStart > guideData.StartTime[0])
        {
          _listingTime = (int)Expect.Afternoon;
          //if (_bNextDay)
          //{
          //    _GrabDay++;
          //}
        }
        else
        {
          if (guideData.StartTime[0] <= 12)
            break;						// Do nothing
        }

        // Pass into Afternoon Code
        //_LastStart = 0;
        goto case (int)Expect.Afternoon;

      case (int)Expect.Afternoon:
        if (guideData.StartTime[0] < 12)		// Site doesn't have correct time
          guideData.StartTime[0] += 12;     // starts again at 1:00 with "pm"

        if (_LastStart > guideData.StartTime[0])
        {
          guideData.StartTime[0] -= 12;
          if (_bNextDay)
          {
            addDays++;
            _GrabDay++;
            _StartGrab = _StartGrab.AddDays(1);
            //_bNextDay = false;
          }
          else
          {
            _bNextDay = true;
          }
          _listingTime = (int)Expect.Morning;
          break;
        }

        break;

      default:
        break;
      }

      //Month
      int month;
      if (guideData.Month == "")
      {
        month = _StartGrab.Month;
      }
      else
      {
        month = getMonth(guideData.Month);
      }

      // Create DateTime
      DateTime dtStart;
      try
      {
        dtStart = new DateTime(_StartGrab.Year, month, guideData.Day, guideData.StartTime[0], guideData.StartTime[1], 0, 0);
      } catch
      {
        Log.WriteFile(Log.LogType.Log, true, "WebEPG: DateTime Error Program: {0}", guideData.Title);
        return false; // DateTime error
      }
      if (_bNextDay)
        dtStart = dtStart.AddDays(addDays);
      // Check TimeZone
      if (_SiteTimeZone != null && !_SiteTimeZone.IsLocalTimeZone())
      {
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, Adjusting from Guide Time: {0} {1}", dtStart.ToShortTimeString(), dtStart.ToShortDateString());
        dtStart = _SiteTimeZone.ToLocalTime(dtStart);
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, debug                    : {0} {1}", dtStart.ToShortTimeString(), dtStart.ToShortDateString());
        if (_timeAdjustOnly)
          dtStart = new DateTime(dtStart.Year, month, guideData.Day, dtStart.Hour, dtStart.Minute, 0, 0);
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: TimeZone, Adjusting to   Local Time: {0} {1}", dtStart.ToShortTimeString(), dtStart.ToShortDateString());
      }

      program.Start = GetLongDateTime(dtStart);
      _LastStart = guideData.StartTime[0];

      if (guideData.EndTime != null)
      {
        DateTime dtEnd = new DateTime(_StartGrab.Year, month, guideData.Day, guideData.EndTime[0], guideData.EndTime[1], 0, 0);
        if (_bNextDay)
        {
          if (guideData.StartTime[0] > guideData.EndTime[0])
            dtEnd = dtEnd.AddDays(addDays + 1);
          else
            dtEnd = dtEnd.AddDays(addDays);
        }
        else
        {
          if (guideData.StartTime[0] > guideData.EndTime[0])
            dtEnd = dtEnd.AddDays(addDays);
        }
        if (_SiteTimeZone != null && !_SiteTimeZone.IsLocalTimeZone())
          dtEnd = _SiteTimeZone.ToLocalTime(dtEnd);
        program.End = GetLongDateTime(dtEnd);

        Log.WriteFile(Log.LogType.Log, false, "WebEPG: Guide, Program Info: {0} / {1} - {2}", program.Start, program.End, guideData.Title);
      }
      else
      {
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: Guide, Program Info: {0} - {1}", program.Start, guideData.Title);
      }
      Log.WriteFile(Log.LogType.Log, false, "WebEPG: Guide, Program Debug: [{0} {1}]", _GrabDay, _bNextDay);

      return true;
    }

    private TVProgram GetProgram(Profiler guideProfile, int index)
    {
      //Parser Listing = guideProfile.GetProfileParser(index);

      TVProgram program = new TVProgram();
      HTMLProfiler htmlProf = null;
      if(guideProfile is HTMLProfiler)
      {
        htmlProf = (HTMLProfiler) guideProfile;

        if(_searchRegex)
        {
          string repeat = htmlProf.SearchRegex(index, _strRepeat, _searchRemove);
          string subtitles = htmlProf.SearchRegex(index, _strSubtitles, _searchRemove);
          string epNum = htmlProf.SearchRegex(index, _strEpNum, _searchRemove);
          string epTotal  = htmlProf.SearchRegex(index, _strEpTotal, _searchRemove);
        }
      }
      ProgramData guideData = new ProgramData();
      ParserData data = (ParserData) guideData;
      guideProfile.GetParserData(index, ref data); //_templateParser.GetProgram(Listing);

      if(guideData.StartTime == null || guideData.Title == "")
        return null;

      if (guideData.IsProgram(_removeProgramsList))
        return null;

      program.Channel = _strID;
      program.Title = guideData.Title;

      // Adjust Time
      if (!AdjustTime(ref guideData, ref program))
        return null;

      // Check TV db if program exists
      TVProgram dbProg = dbProgram(program.Title, program.Start);
      if (dbProg != null)
      {
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: Program in db copying it");
        dbProg.Channel = _strID;
        return dbProg;
      }

      if (guideData.Description != "")
        program.Description = guideData.Description;

      if (guideData.Genre != "")
        program.Genre = getGenre(guideData.Genre);

      // SubLink
      if(_grabLinked && _SubListingLink != ""
         && guideData.StartTime[0] >= _linkStart
         && guideData.StartTime[0] <= _linkEnd
         && htmlProf != null)
      {
        string linkURL;
        if(_strSubURL != "")
          linkURL = _strSubURL;
        else
          linkURL = _strURLbase;

        string strLinkURL = htmlProf.GetHyperLink(index, _SubListingLink, linkURL);

        if(strLinkURL != "")
        {
          Log.WriteFile(Log.LogType.Log, false, "WebEPG: Reading {0}", strLinkURL);
          Thread.Sleep(_grabDelay);
          Profiler SubProfile = _templateSubProfile.GetPageProfiler(strLinkURL);
          int Count = SubProfile.subProfileCount();

          if(Count > 0)
          {
            ProgramData SubData =  new ProgramData();
            ParserData refdata = (ParserData) SubData;
            SubProfile.GetParserData(0, ref refdata);

            if (SubData.IsProgram(_removeProgramsList))
              return null;

            if (SubData.Description != "")
              program.Description = SubData.Description;

            if (SubData.Genre != "")
              program.Genre = getGenre(SubData.Genre);

            if (SubData.SubTitle != "")
              program.Episode = SubData.SubTitle;
          }
        }

      }

      return program;
    }

    private bool GetListing(string strURL, int offset, string strChannel, out bool error)
    {
      Profiler guideProfile;
      int listingCount = 0;
      bool bMore = false;
      error = false;

      strURL = strURL.Replace("#LIST_OFFSET", offset.ToString());

      Log.WriteFile(Log.LogType.Log, false, "WebEPG: Reading {0}{1}", _strURLbase, strURL);

      if(_templateProfile is XMLProfiler)
      {
        XMLProfiler templateProfile = (XMLProfiler) _templateProfile;
        templateProfile.SetChannelID(strChannel);
      }
      guideProfile = _templateProfile.GetPageProfiler(_strURLbase + strURL);
      if(guideProfile != null)
        listingCount = guideProfile.subProfileCount();

      if(listingCount == 0) // && _maxListingCount == 0)
      {
        if (_maxListingCount == 0 || (_maxListingCount != 0 && offset == 0))
        {
          Log.WriteFile(Log.LogType.Log, false, "WebEPG: No Listings Found");
          _GrabDay++;
          error = true;
        }
        else
        {
          Log.WriteFile(Log.LogType.Log, false, "WebEPG: Listing Count 0");
        }
        //_GrabDay++;
      }
      else
      {
        Log.WriteFile(Log.LogType.Log, false, "WebEPG: Listing Count {0}", listingCount);

        if (listingCount == _maxListingCount)
          bMore = true;

        for (int i = 0; i < listingCount; i++)
        {
          TVProgram program = GetProgram(guideProfile, i);
          if (program != null)
          {
            _programs.Add(program);
          }
        }

        if (_GrabDay > _MaxGrabDays)
          bMore = false;
      }

      return bMore;
    }


    public ArrayList GetGuide(string strChannelID,  bool Linked, int linkStart, int linkEnd)
    {
      _strID = strChannelID;
      _grabLinked = Linked;
      _linkStart = linkStart;
      _linkEnd = linkEnd;
      int offset = 0;

      string searchID = _xmlreader.GetValueAsString("ChannelList", strChannelID, "");
      string searchLang = _xmlreader.GetValueAsString("Listing", "SearchLanguage", "en-US");
      _strWeekDay = _xmlreader.GetValueAsString("Listing", "WeekdayString", "dddd");
      CultureInfo culture = new CultureInfo(searchLang);

      _removeProgramsList = _xmlreader.GetValueAsString("RemovePrograms", "*", "");
      if (_removeProgramsList != "")
        _removeProgramsList += ";";
      string chanRemovePrograms = _xmlreader.GetValueAsString("RemovePrograms", strChannelID, "");
      if (chanRemovePrograms != "")
      {
        _removeProgramsList += chanRemovePrograms;
        _removeProgramsList += ";";
      }

      if (searchID == "")
      {
        Log.WriteFile(Log.LogType.Log, true, "WebEPG: ChannelId: {0} not found!", strChannelID);
        return null;
      }

      _programs = new ArrayList();

      string strURLid = _strURLsearch.Replace("#ID", searchID);
      string strURL;

      Log.WriteFile(Log.LogType.Log, false, "WebEPG: ChannelId: {0}", strChannelID);

      _GrabDay = 0;
      _StartGrab = DateTime.Now;

      //TVDatabase.BeginTransaction();
      //TVDatabase.ClearCache();
      //TVDatabase.RemoveOldPrograms();

      int dbChannelId;
      string dbChannelName;
      _dbPrograms = new ArrayList();
      _dbLastProg = 0;

      if(TVDatabase.GetEPGMapping(strChannelID, out dbChannelId, out dbChannelName)) // (nodeId.InnerText, out idTvChannel, out strTvChannel);
      {
        DateTime endGrab = _StartGrab.AddDays(_MaxGrabDays+1);
        DateTime startGrab = _StartGrab.AddHours(-1);
        TVDatabase.GetProgramsPerChannel(dbChannelName, GetLongDateTime(startGrab), GetLongDateTime(endGrab), ref _dbPrograms);
      }


      while (_GrabDay < _MaxGrabDays)
      {
        strURL = strURLid;
        if(_strDayNames != null)
          strURL = strURL.Replace("#DAY_NAME", _strDayNames[_GrabDay]);

        strURL = strURL.Replace("#DAY_OFFSET", (_GrabDay+_offsetStart).ToString());
        strURL = strURL.Replace("#EPOCH_TIME", GetEpochTime(_StartGrab).ToString());
        strURL = strURL.Replace("#EPOCH_DATE", GetEpochDate(_StartGrab).ToString());
        strURL = strURL.Replace("#YYYY", _StartGrab.Year.ToString());
        strURL = strURL.Replace("#MM", String.Format("{0:00}", _StartGrab.Month));
        strURL = strURL.Replace("#_M", _StartGrab.Month.ToString());
        strURL = strURL.Replace("#MONTH", _StartGrab.ToString("MMMM", culture));
        strURL = strURL.Replace("#DD", String.Format("{0:00}", _StartGrab.Day));
        strURL = strURL.Replace("#_D", _StartGrab.Day.ToString());
        strURL = strURL.Replace("#WEEKDAY", _StartGrab.ToString(_strWeekDay, culture));

        offset = 0;
        _LastStart=0;
        _bNextDay = false;
        _listingTime = (int) Expect.Start;

        bool error;
        while (GetListing(strURL, offset, searchID, out error))
        {
          Thread.Sleep(_grabDelay);
          if (_maxListingCount == 0)
            break;
          offset += _maxListingCount;
        }
        if (error)
        {
          Log.WriteFile(Log.LogType.Log, true, "WebEPG: ChannelId: {0} grabber error", strChannelID);
          break;
        }
        //_GrabDay++;
        if (strURL != strURLid)
        {
          _StartGrab = _StartGrab.AddDays(1);
          _GrabDay++;
        }
        else
        {
          if (strURL.IndexOf("#LIST_OFFSET") == -1)
            break;
        }
      }

      return _programs;
    }
  }
}
