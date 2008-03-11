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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Text;
using Microsoft.Win32;
using DirectShowLib;
using TvLibrary.Implementations;
using DirectShowLib.SBE;
using TvLibrary.Log;
using TvLibrary.Interfaces;
using TvLibrary.Teletext;
using TvLibrary.Epg;
using TvLibrary.Implementations.DVB;
using TvLibrary.Implementations.DVB.Structures;
using TvLibrary.Implementations.Helper;
using TvLibrary.Helper;
using TvLibrary.ChannelLinkage;
using TvLibrary.Interfaces.Analyzer;

namespace TvLibrary.Implementations.Analog
{
  /// <summary>
  /// Implementation of <see cref="T:TvLibrary.Interfaces.ITVCard"/> which handles analog tv cards
  /// </summary>
  public class AnalogSubChannel : BaseSubChannel, ITvSubChannel, IAnalogTeletextCallBack, IAnalogVideoAudioObserver
  {
    #region variables
    private TvCardAnalog _card;
    private IBaseFilter _filterTvTunerFilter;
    private IBaseFilter _filterTvAudioTuner;
    private IBaseFilter _mpFileWriter;
    private IMPRecord _mpRecord;
    #endregion

    #region ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalogSubChannel"/> class.
    /// </summary>
    public AnalogSubChannel(TvCardAnalog card, int subchnnelId, IBaseFilter filterTvTunerFilter, IBaseFilter filterTvAudioTuner, IPin pinVBI, IBaseFilter mpFileWriter):base()
    {
      _card = card;
      _hasTeletext = (pinVBI != null);
      _filterTvTunerFilter = filterTvTunerFilter;
      _filterTvAudioTuner = filterTvAudioTuner;
      _mpFileWriter = mpFileWriter;
      _mpRecord = (IMPRecord)_mpFileWriter;
      _subChannelId = 0;
    }
    #endregion

    #region tuning and graph methods

    /// <summary>
    /// Should be called before tuning to a new channel
    /// resets the state
    /// </summary>
    public override void OnBeforeTune()
    {
      Log.Log.WriteFile("analog subch:{0} OnBeforeTune", _subChannelId);
      if (IsTimeShifting)
      {
        _mpRecord.PauseTimeShifting(1);
      }
    }

    /// <summary>
    /// Should be called when the graph is tuned to the new channel
    /// resets the state
    /// </summary>
    public override void OnAfterTune()
    {
      Log.Log.WriteFile("analog subch:{0} OnAfterTune", _subChannelId);
      if (IsTimeShifting)
      {
        _mpRecord.PauseTimeShifting(0);
      }
    }

    /// <summary>
    /// Should be called when the graph is about to start
    /// Resets the state 
    /// If graph is already running, starts the pmt grabber to grab the
    /// pmt for the new channel
    /// </summary>
    public override void OnGraphStart()
    {
      Log.Log.WriteFile("analog subch:{0} OnGraphStart", _subChannelId);
      if (_teletextDecoder != null)
      {
        _teletextDecoder.ClearBuffer();
      }
    }

    /// <summary>
    /// Should be called when the graph has been started
    /// sets up the pmt grabber to grab the pmt of the channel
    /// </summary>
    public override void OnGraphStarted()
    {
      Log.Log.WriteFile("analog subch:{0} OnGraphStarted", _subChannelId);
      _graphRunning = true;
      _dateTimeShiftStarted = DateTime.MinValue;
    }

    public override void OnGraphStop()
    {
    }

    public override void OnGraphStopped()
    {
    }
    #endregion

    #region Timeshifting - Recording methods
    /// <summary>
    /// sets the filename used for timeshifting
    /// </summary>
    /// <param name="fileName">timeshifting filename</param>
    protected override bool OnStartTimeShifting(string fileName)
    {
      _timeshiftFileName = fileName;
      Log.Log.WriteFile("analog:SetTimeShiftFileName:{0}", fileName);
      Log.Log.WriteFile("analog:SetTimeShiftFileName: uses .ts");
      ScanParameters _parameters = _card.Parameters;
      _mpRecord.SetVideoAudioObserver(this);
      _mpRecord.SetTimeShiftParams(_parameters.MinimumFiles, _parameters.MaximumFiles, _parameters.MaximumFileSize);
      _mpRecord.SetTimeShiftFileName(fileName);
      _mpRecord.StartTimeShifting();
      _dateTimeShiftStarted = DateTime.Now;
      return true;
    }

    /// <summary>
    /// Stops timeshifting
    /// </summary>
    /// <returns></returns>
    protected override void OnStopTimeShifting()
    {
      Log.Log.WriteFile("Analog: StopTimeShifting()");
      _mpRecord.SetVideoAudioObserver(null);
      _mpRecord.StopTimeShifting();
    }

    /// <summary>
    /// Starts recording
    /// </summary>
    /// <param name="transportStream">Recording type (content or reference)</param>
    /// <param name="fileName">filename to which to recording should be saved</param>
    /// <returns></returns>
    protected override void OnStartRecording(bool transportStream, string fileName)
    {
      Log.Log.WriteFile("analog:StartRecord({0})", fileName);
      if (transportStream)
      {
        Log.Log.WriteFile("dvb:SetRecording: uses .ts");
        _mpRecord.SetRecordingMode(TimeShiftingMode.TransportStream);
      } else
      {
        Log.Log.WriteFile("dvb:SetRecording: uses .mpg");
        _mpRecord.SetRecordingMode(TimeShiftingMode.ProgramStream);
      }
      _mpRecord.SetRecordingFileName(fileName);
      _mpRecord.StartRecord();
    }

    /// <summary>
    /// Stop recording
    /// </summary>
    /// <returns></returns>
    protected override void OnStopRecording()
    {
      Log.Log.WriteFile("analog:StopRecord()");
      _mpRecord.StopRecord();
    }

    #endregion

    #region audio streams
    /// <summary>
    /// returns the list of available audio streams
    /// </summary>
    public override List<IAudioStream> AvailableAudioStreams
    {
      get
      {
        List<IAudioStream> streams = new List<IAudioStream>();
        if (_filterTvAudioTuner == null) return streams;
        IAMTVAudio tvAudioTunerInterface = _filterTvAudioTuner as IAMTVAudio;
        TVAudioMode availableAudioModes;
        tvAudioTunerInterface.GetAvailableTVAudioModes(out availableAudioModes);
        if ((availableAudioModes & (TVAudioMode.Stereo)) != 0)
        {
          AnalogAudioStream stream = new AnalogAudioStream();
          stream.AudioMode = TVAudioMode.Stereo;
          stream.Language = "Stereo";
          streams.Add(stream);
        }
        if ((availableAudioModes & (TVAudioMode.Mono)) != 0)
        {
          AnalogAudioStream stream = new AnalogAudioStream();
          stream.AudioMode = TVAudioMode.Mono;
          stream.Language = "Mono";
          streams.Add(stream);
        }
        if ((availableAudioModes & (TVAudioMode.LangA)) != 0)
        {
          AnalogAudioStream stream = new AnalogAudioStream();
          stream.AudioMode = TVAudioMode.LangA;
          stream.Language = "LangA";
          streams.Add(stream);
        }
        if ((availableAudioModes & (TVAudioMode.LangB)) != 0)
        {
          AnalogAudioStream stream = new AnalogAudioStream();
          stream.AudioMode = TVAudioMode.LangB;
          stream.Language = "LangB";
          streams.Add(stream);
        }
        if ((availableAudioModes & (TVAudioMode.LangC)) != 0)
        {
          AnalogAudioStream stream = new AnalogAudioStream();
          stream.AudioMode = TVAudioMode.LangC;
          stream.Language = "LangC";
          streams.Add(stream);
        }

        return streams;
      }
    }

    /// <summary>
    /// get/set the current selected audio stream
    /// </summary>
    public override IAudioStream CurrentAudioStream
    {
      get
      {
        if (_filterTvAudioTuner == null) return null;
        IAMTVAudio tvAudioTunerInterface = _filterTvAudioTuner as IAMTVAudio;
        TVAudioMode mode;
        tvAudioTunerInterface.get_TVAudioMode(out mode);
        List<IAudioStream> streams = AvailableAudioStreams;
        foreach (AnalogAudioStream stream in streams)
        {
          if (stream.AudioMode == mode) return stream;
        }
        return null;
      }
      set
      {
        AnalogAudioStream stream = value as AnalogAudioStream;
        if (stream != null && _filterTvAudioTuner != null)
        {
          IAMTVAudio tvAudioTunerInterface = _filterTvAudioTuner as IAMTVAudio;
          tvAudioTunerInterface.put_TVAudioMode(stream.AudioMode);
        }
      }
    }
    #endregion

    #region video stream
    /// <summary>
    /// Returns true when unscrambled audio/video is received otherwise false
    /// </summary>
    /// <returns>true of false</returns>
    public override bool IsReceivingAudioVideo
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Retursn the video format (always returns MPEG2). 
    /// </summary>
    /// <value>The number of channels decrypting.</value>
    public override int GetCurrentVideoStream
    {
      get
      {
        return 2;
      }
    }
    #endregion

    #region teletext
    protected override void OnGrabTeletext()
    {
      if (_hasTeletext)
      {
        if (_grabTeletext)
        {
          _mpRecord.TTxSetCallback(this);
        } else
        {
          _mpRecord.TTxSetCallback(null);
        }
      } else
      {
        _grabTeletext = false;
        _mpRecord.TTxSetCallback(null);
      }
    }
    #endregion

    #region OnDecompose
    protected override void OnDecompose()
    {
    }
    #endregion


  }
}

