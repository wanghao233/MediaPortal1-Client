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

#region usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Animation;

using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Player;
using MediaPortal.Playlists;
using MediaPortal.Picture.Database;
using MediaPortal.Music.Database;
using MediaPortal.Dialogs;
using MediaPortal.Configuration;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
#endregion

namespace MediaPortal.GUI.Pictures
{
  class SlidePicture
  {
    const int MAX_PICTURE_WIDTH = 2040;
    const int MAX_PICTURE_HEIGHT = 2040;

    private Texture _texture;
    private int _width = 0;
    private int _height = 0;
    private int _rotation = 0;

    private string _filePath;
    private bool _useActualSizeTexture;

    public Texture Texture
    {
      get { return _texture; }
    }

    public string FilePath
    {
      get { return _filePath; }
    }

    public bool TrueSizeTexture
    {
      get { return _useActualSizeTexture; }
    }

    public int Width
    {
      get { return _width; }
    }

    public int Height
    {
      get { return _height; }
    }

    public int Rotation
    {
      get { return _rotation; }
    }

    public SlidePicture(string strFilePath, bool useActualSizeTexture)
    {
      _filePath = strFilePath;      

      using (PictureDatabase dbs = new PictureDatabase())
      {
        _rotation = dbs.GetRotation(_filePath);
      }
      int iMaxWidth = GUIGraphicsContext.OverScanWidth;
      int iMaxHeight = GUIGraphicsContext.OverScanHeight;

      _useActualSizeTexture = useActualSizeTexture;
      if (_useActualSizeTexture)
      {
        iMaxWidth = MAX_PICTURE_WIDTH;
        iMaxHeight = MAX_PICTURE_HEIGHT;
      }

      _texture = MediaPortal.Util.Picture.Load(strFilePath, _rotation, iMaxWidth, iMaxHeight, true, false, true, out _width, out _height);
    }

    ~SlidePicture()
    {
      if (_texture != null && !_texture.Disposed)
      {
        _texture.Dispose();
        _texture = null;
      }
    }
  }

  class SlideCache
  {
    enum RelativeIndex
    {
      Prev = 0,
      Curr = 1,
      Next = 2
    }

    Thread _prefetchingThread;
    Object _prefetchingThreadLock = new Object();

    SlidePicture[] _slides = new SlidePicture[3];
    Object _slidesLock = new Object();

    string _neededSlideFilePath;
    RelativeIndex _neededSlideRelativeIndex;

    private SlidePicture NeededSlide
    {
      get { return _slides[(int)_neededSlideRelativeIndex]; }
      set { _slides[(int)_neededSlideRelativeIndex] = value; }
    }

    private SlidePicture PrevSlide
    {
      get { return _slides[(int)RelativeIndex.Prev]; }
      set { _slides[(int)RelativeIndex.Prev] = value; }
    }

    private SlidePicture CurrentSlide
    {
      get { return _slides[(int)RelativeIndex.Curr]; }
      set { _slides[(int)RelativeIndex.Curr] = value; }
    }

    private SlidePicture NextSlide
    {
      get { return _slides[(int)RelativeIndex.Next]; }
      set { _slides[(int)RelativeIndex.Next] = value; }
    }

    public SlidePicture GetCurrentSlide(string slideFilePath)
    {
      // wait for any (needed) prefetching to complete
      lock (_prefetchingThreadLock)
      {
        if (_prefetchingThread != null)
        {
          // only wait for the prefetching if it is for the slide file that we need
          if (_neededSlideFilePath == slideFilePath)
          {
            _prefetchingThread.Priority = ThreadPriority.AboveNormal;
          }
          else
          {
            // uneeded, abort
            _prefetchingThread.Abort();
            _prefetchingThread = null;
          }
        }
      }

      while (_prefetchingThread != null)
        GUIWindowManager.Process();

      lock (_slidesLock)
      {
        // try and use pre-fetched slide if appropriate
        if (NextSlide != null && NextSlide.FilePath == slideFilePath)
          return NextSlide;
        else if (PrevSlide != null && PrevSlide.FilePath == slideFilePath)
          return PrevSlide;
        else if (CurrentSlide != null && CurrentSlide.FilePath == slideFilePath)
          return CurrentSlide;
        else
        {
          // slide is not in cache, so get it now
          CurrentSlide = new SlidePicture(slideFilePath, false);
          return CurrentSlide;
        }
      }
    }

    public void PrefetchNextSlide(string prevPath, string currPath, string nextPath)
    {
      lock (_prefetchingThreadLock)
      {
        // assume that any incomplete prefetching is uneeded, abort
        if (_prefetchingThread != null)
        {
          _prefetchingThread.Abort();
          _prefetchingThread = null;
        }
      }

      lock (_slidesLock)
      {
        // shift slides and determine _neededSlideRelativeIndex
        if (NextSlide != null && NextSlide.FilePath == currPath)
        {
          PrevSlide = CurrentSlide;
          CurrentSlide = NextSlide;
          _neededSlideFilePath = nextPath;
          _neededSlideRelativeIndex = RelativeIndex.Next;
        }
        else if (PrevSlide != null && PrevSlide.FilePath == currPath)
        {
          NextSlide = CurrentSlide;
          CurrentSlide = PrevSlide;
          _neededSlideFilePath = prevPath;
          _neededSlideRelativeIndex = RelativeIndex.Prev;
        }
        else
        {
          // may need all 3, but just get next
          _neededSlideFilePath = nextPath;
          _neededSlideRelativeIndex = RelativeIndex.Next;
        }
      }

      lock (_prefetchingThreadLock)
      {
        _prefetchingThread = new Thread(new ThreadStart(LoadNextSlideThread));
        _prefetchingThread.Priority = ThreadPriority.BelowNormal;
        string cacheString = String.Format("cache:{0}|{1}|{2} ",
          _slides[0] != null ? "1" : "0",
          _slides[1] != null ? "1" : "0",
          _slides[2] != null ? "1" : "0");
        Trace.WriteLine(cacheString + String.Format("prefetching {0} slide {1}", _neededSlideRelativeIndex.ToString("G"), System.IO.Path.GetFileNameWithoutExtension(_neededSlideFilePath)));
        _prefetchingThread.Start();
      }
    }

    /// <summary>
    /// Method to do the work of actually loading the image from file. This method
    /// should only be used by the prefetching thread.
    /// </summary>
    public void LoadNextSlideThread()
    {
      try
      {
        Debug.Assert(System.Threading.Thread.CurrentThread == _prefetchingThread);

        lock (_slidesLock)
        {
          NeededSlide = new SlidePicture(_neededSlideFilePath, false);
        }

        lock (_prefetchingThreadLock)
        {
          _prefetchingThread = null;
        }
      }
      catch (System.Threading.ThreadAbortException)
      {
        // abort is expected when slide changes outpace prefetch, ignore
        Trace.WriteLine(String.Format("  ...aborted {0} slide {1}", _neededSlideRelativeIndex.ToString("G"), System.IO.Path.GetFileNameWithoutExtension(_neededSlideFilePath)));
      }
    }

    public void InvalidateSlide(string slideFilePath)
    {
      lock (_slidesLock)
      {
        for (int i = 0; i < _slides.Length; i++)
        {
          SlidePicture slide = _slides[i];
          if (slide != null && slide.FilePath == slideFilePath)
          {
            _slides[i] = null;
          }
        }
      }

      // Note that we could pre-fetch the invalidated slide, but if the new version
      // of the slide is going to be requested immediately (as with DoRotate) then
      // pre-fetching won't help.
    }
  }

  /// <summary>
  /// todo : adding zoom OSD (stripped if for KenBurns)
  /// </summary>
  public class GUISlideShow : GUIWindow
  {
    private SlidePicture LoadCurrentSlide()
    {
      if (_slideList.Count == 0) return null;
      string slideFilePath = _slideList[_currentSlideIndex];

      _currentSlide = _slideCache.GetCurrentSlide(slideFilePath);

      GUIPropertyManager.SetProperty("#selecteditem", Util.Utils.GetFilename(slideFilePath));

      ResetCurrentZoom(_currentSlide);

      PrefetchNextSlide();

      return _currentSlide;
    }

    private void PrefetchNextSlide()
    {
      if (_slideList.Count != 0)
      {
        string prev = _slideList[PreviousSlideIndex(false, _currentSlideIndex)];
        string curr = _slideList[_currentSlideIndex];
        string next = _slideList[NextSlideIndex(false, _currentSlideIndex)];

        _slideCache.PrefetchNextSlide(prev, curr, next);
      }
    }

    private void InvalidateCurrentSlide()
    {
      if (_slideList.Count == 0) return;
      string slideFilePath = _slideList[_currentSlideIndex];

      InvalidateSlide(slideFilePath);
    }

    private void InvalidateSlide(string slideFilePath)
    {
      _slideCache.InvalidateSlide(slideFilePath);
    }

    private void ResetCurrentZoom(SlidePicture slide)
    {
      _kenBurnsEffect = 0;
      _currentZoomFactor = 1.0f;
      _currentZoomLeft = 0;
      _currentZoomTop = 0;
      _zoomInfoVisible = false;

      CalculateBestZoom(slide.Width, slide.Height);
    }

    #region enums
    enum DirectionType
    {
      Left,
      Right,
      Up,
      Down
    }
    #endregion

    #region constants
    const float TIME_PER_FRAME = 0.02f;
    const int MAX_RENDER_METHODS = 10;
    const int MAX_ZOOM_FACTOR = 10;
    const int MAX_PICTURE_WIDTH = 2040;
    const int MAX_PICTURE_HEIGHT = 2040;

    const int LABEL_ROW1 = 10;
    const int LABEL_ROW2 = 11;
    const int LABEL_ROW2_EXTRA = 12;

    const float KENBURNS_ZOOM_FACTOR = 1.30f; // Zoom factor for pictures that have a black border on the sides
    const float KENBURNS_ZOOM_FACTOR_FS = 1.20f; // Zoom factor for pictures that are filling the whole screen

    const float KENBURNS_MAXZOOM = 1.30f;
    const int KENBURNS_XFADE_FRAMES = 60;
    #endregion

    #region variables
    int _slideShowTransistionFrames = 60;
    int _kenBurnTransistionSpeed = 40;

    List<string> _slideList = new List<string>();
    int _slideTime = 0;
    int _counter = 0;

    public float _currentZoomFactor = 1.0f;
    public float _currentZoomLeft = 0;
    public float _currentZoomTop = 0;
    public int _currentZoomType = 0;

    public float _zoomFactorBackground = 1.0f;
    public float _zoomLeftBackground = 0;
    public float _zoomTopBackground = 0;
    public int _zoomTypeBackground = 0;

    SlideCache _slideCache = new SlideCache();

    //Texture _backgroundTexture = null;
    //float _backgroundSlide.Width = 0;
    //float _backgroundSlide.Height = 0;
    //float _zoomFactorBackground = 1.0f;
    //float _zoomLeft = 0;
    //float _zoomTop = 0;
    //int _zoomTypeBackground = 0;
    SlidePicture _backgroundSlide = null;

    //Texture _currentTexture = null;
    //float _currentSlide.Width = 0;
    //float _currentSlide.Height = 0;
    //float _currentZoomFactor = 1.0f;
    //float _currentZoomLeft = 0;
    //float _currentZoomTop = 0;
    //int _currentZoomType = 0;
    SlidePicture _currentSlide = null;

    int _frameCounter = 0;
    int _currentSlideIndex = 0;
    int _lastSlideShown = -1;
    int _transitionMethod = 0;
    bool _isSlideShow = false;
    bool _infoVisible = false;
    bool _zoomInfoVisible = false;
    bool _autoHideOsd = true;
    //string _backgroundSlideFileName = "";
    //string _currentSlideFileName = "";
    bool _isPaused = false;
    float _zoomWidth = 0, _zoomHeight = 0;
    //int _rotation = 0;
    int _speed = 3;
    bool _showOverlayFlag;
    bool _update = false;
    bool _useRandomTransitions = true;
    float _defaultZoomFactor = 1.0f;
    bool _isPictureZoomed = false;
    float _userZoomLevel = 1.0f;
    //bool _trueSizeTexture = false;
    bool _autoShuffle = false;
    bool _autoRepeat = false;

    Random _randomizer = new Random(DateTime.Now.Second);

    // Kenburns transition variables
    bool _useKenBurns = true;
    bool _landScape = false;
    bool _fullScreen = false;
    float _bestZoomFactorCurrent = 1.0f;
    float _endZoomFactor = 1.0f;
    float _startZoomFactor = 1.0f;

    int _kenBurnsEffect = 0;
    int _kenBurnsState = 0;
    int _frameNumber;
    int _startPoint;
    int _endPoint;

    float _panYChange;
    float _panXChange;
    float _zoomChange;
    bool _isLoadingRawPicture = false;
    bool _isBackgroundMusicEnabled = false;
    bool _isBackgroundMusicPlaying = false;
    string[] _musicFileExtensions;
    int _lastSegmentIndex = -1;
    float _renderTimer;
    public static readonly string SegmentIndicator = "#segment";
    PlayListPlayer playlistPlayer;
    MusicDatabase mDB = null;
    #endregion

    #region GUIWindow overrides
    public GUISlideShow()
    {
      GetID = (int)GUIWindow.Window.WINDOW_SLIDESHOW;
      playlistPlayer = PlayListPlayer.SingletonPlayer;
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\slideshow.xml");
    }

    public override bool OnMessage(GUIMessage message)
    {
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
          _showOverlayFlag = GUIGraphicsContext.Overlay;
          base.OnMessage(message);
          GUIGraphicsContext.Overlay = false;
          _update = false;
          _lastSlideShown = -1;
          _currentSlideIndex = -1;
          LoadSettings();
          return true;

        case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
          Reset();
          GUIGraphicsContext.Overlay = _showOverlayFlag;
          break;

        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED:
          if (mDB == null)
            mDB = new MusicDatabase();
          ShowSong();
          break;
      }
      return base.OnMessage(message);
    }
    public override int GetFocusControlId()
    {
      return 1;
    }

    public override bool NeedRefresh()
    {
      return _isSlideShow;
    }

    public override bool FullScreenVideoAllowed
    {
      get { return false; }
    }

    public override void OnDeviceRestored()
    {
      if (GUIWindowManager.ActiveWindow == GetID)
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_PICTURES);
    }

    public override void OnAction(Action action)
    {
      switch (action.wID)
      {
        case Action.ActionType.ACTION_MOUSE_CLICK:
          int x = (int)action.fAmount1;

          if (!_isPictureZoomed)
          {
            // Divide screen into three sections (previous / pause / next)
            if (x < (GUIGraphicsContext.OverScanWidth / 3))
              ShowPrevious();
            else if (x > (GUIGraphicsContext.OverScanWidth / 3) * 2)
              ShowNext();
            else
              if (_isSlideShow) _isPaused = !_isPaused;
          }
          else
          {
            _userZoomLevel = 1.0f;
            ZoomBackGround(_defaultZoomFactor);
            _isPaused = false;
          }
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_STOP:
        case Action.ActionType.ACTION_PREVIOUS_MENU:
          ShowPreviousWindow();
          break;

        case Action.ActionType.ACTION_DELETE_ITEM:
          OnDelete();
          break;

        case Action.ActionType.ACTION_PREV_ITEM:
          if (_lastSegmentIndex != -1)
            ShowPrevious(true);

          break;
        case Action.ActionType.ACTION_PREV_PICTURE:
          if (!_isPictureZoomed)
          {
            ShowPrevious();
          }
          else
          {
            // Move picture
            _zoomLeftBackground -= 25;
            if (_zoomLeftBackground < 0) _zoomLeftBackground = 0;
            _slideTime = (int)(DateTime.Now.Ticks / 10000);
          }
          break;
        case Action.ActionType.ACTION_NEXT_ITEM:
          if (_lastSegmentIndex != -1)
            ShowNext(true);

          break;
        case Action.ActionType.ACTION_NEXT_PICTURE:
          if (!_isPictureZoomed)
          {
            ShowNext();
          }
          else
          {
            // Move picture
            _zoomLeftBackground += 25;
            if (_zoomLeftBackground > (int)_backgroundSlide.Width - _zoomWidth) _zoomLeftBackground = (_backgroundSlide.Width - _zoomWidth);
            _slideTime = (int)(DateTime.Now.Ticks / 10000);
          }
          break;

        case Action.ActionType.ACTION_MOVE_LEFT:
          _zoomLeftBackground -= 25;
          if (_zoomLeftBackground < 0) _zoomLeftBackground = 0;
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_MOVE_RIGHT:
          _zoomLeftBackground += 25;
          if (_zoomLeftBackground > (int)_backgroundSlide.Width - _zoomWidth) _zoomLeftBackground = (_backgroundSlide.Width - _zoomWidth);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_MOVE_DOWN:
          if (_isPictureZoomed) _zoomTopBackground += 25;
          if (_zoomTopBackground > (int)_backgroundSlide.Height - _zoomHeight) _zoomTopBackground = (_backgroundSlide.Height - _zoomHeight);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_MOVE_UP:
          if (_isPictureZoomed) _zoomTopBackground -= 25;
          if (_zoomTopBackground < 0) _zoomTopBackground = 0;
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_SHOW_INFO:
          if (_infoVisible)
          {
            _infoVisible = false;
            _zoomInfoVisible = false;
          }
          else
          {
            if (_isPictureZoomed) _zoomInfoVisible = true;
            _infoVisible = true;
            _autoHideOsd = true;
            _slideTime = (int)(DateTime.Now.Ticks / 10000);
          }
          break;

        case Action.ActionType.ACTION_PAUSE_PICTURE:
          if (_isSlideShow)
          {
            if (_isPictureZoomed)
            {
              _userZoomLevel = 1.0f;
              ZoomBackGround(_defaultZoomFactor);
              _isPaused = false;
            }
            else
              _isPaused = !_isPaused;
          }
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_ZOOM_OUT:
          _userZoomLevel -= 0.25f;
          if (_userZoomLevel < 1.0f) // use < instead of <= to make it possible to reach 100% before "fit to screen" 
          {
            _userZoomLevel = 1.0f;
            _isPictureZoomed = false; // make that it is possible to switch to the next or prev. image again
            if (LoadCurrentSlide() == null)
              Log.Debug("GUISlideShow: current slide unknown for zooming out");
            break;
          }
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_ZOOM_IN:
          if (_isPictureZoomed) _userZoomLevel += 0.25f; // the if _isPictureZoomed condition is used to make it possible to reach 100% (not directly 125%). 
          if (_userZoomLevel > 20.0f) _userZoomLevel = 20.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_ROTATE_PICTURE:
          DoRotate();
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;

        case Action.ActionType.ACTION_ZOOM_LEVEL_NORMAL:
          //Fit current image to screen
          _userZoomLevel = 1.0f;
          _isPictureZoomed = false;
          if (LoadCurrentSlide() == null)
            Log.Debug("GUISlideShow: current slide unknown for zooming out");
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_1:
          _userZoomLevel = 1.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_2:
          _userZoomLevel = 2.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_3:
          _userZoomLevel = 3.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_4:
          _userZoomLevel = 4.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_5:
          _userZoomLevel = 5.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_6:
          _userZoomLevel = 6.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_7:
          _userZoomLevel = 7.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_8:
          _userZoomLevel = 8.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ZOOM_LEVEL_9:
          _userZoomLevel = 9.0f;
          ZoomBackGround(_defaultZoomFactor * _userZoomLevel);
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
          break;
        case Action.ActionType.ACTION_ANALOG_MOVE:
          float fX = 2 * action.fAmount1;
          float fY = 2 * action.fAmount2;
          if (fX != 0.0f || fY != 0.0f)
          {
            if (_isPictureZoomed)
            {
              _zoomLeftBackground += (int)fX;
              _zoomTopBackground -= (int)fY;
              if (_zoomTopBackground < 0) _zoomTopBackground = 0;
              if (_zoomLeftBackground < 0) _zoomLeftBackground = 0;
              if (_zoomTopBackground > _backgroundSlide.Height - _zoomHeight) _zoomTopBackground = (_backgroundSlide.Height - _zoomHeight);
              if (_zoomLeftBackground > _backgroundSlide.Width - _zoomWidth) _zoomLeftBackground = (_backgroundSlide.Width - _zoomWidth);

              _slideTime = (int)(DateTime.Now.Ticks / 10000);
            }
          }
          break;

        case Action.ActionType.ACTION_CONTEXT_MENU:
          ShowContextMenu();
          break;
      }
    }

    public override void Render(float timePassed)
    {
      //Log.Info("Render:{0} {1} {2}", timePassed, _renderTimer, _frameCounter);
      if (!_isPaused && !_isPictureZoomed)
      {
        if (_frameCounter > 0)
        {
          _renderTimer += timePassed;
          while (_renderTimer >= TIME_PER_FRAME)
          {
            _frameCounter++;
            _renderTimer -= TIME_PER_FRAME;
          }
        }
        else _frameCounter = 1;
      }
      int iSlides = _slideList.Count;
      if (0 == iSlides) return;

      if (_update || _isSlideShow || null == _backgroundSlide)
      {
        _update = false;
        if (iSlides > 1 || _backgroundSlide == null)
        {
          if (_currentSlide == null)
          {
            int totalFrames = (_speed * (int)(1.0 / TIME_PER_FRAME)) + _slideShowTransistionFrames;
            if (_useKenBurns) totalFrames = _kenBurnTransistionSpeed * 30;
            if (_frameCounter >= totalFrames || _backgroundSlide == null)
            {
              if ((!_isPaused && !_isPictureZoomed) || _backgroundSlide == null)
              {
                _currentSlideIndex++;
                if (_currentSlideIndex >= _slideList.Count)
                {
                  if (_autoRepeat)
                  {
                    _currentSlideIndex = 0;
                    if (_autoShuffle)
                      Shuffle();
                  }
                  else
                    // How to exit back to GUIPictures?
                    ShowPreviousWindow();
                }
              }
            }
          }
        }

        if (_currentSlideIndex != _lastSlideShown)
        {
          // Reset
          _frameCounter = 0;
          _lastSlideShown = _currentSlideIndex;

          // Get selected picture (zoomed to full screen)
          LoadCurrentSlide();

          if (_useKenBurns && _isSlideShow)
          {
            //Select transition based upon picture width/height
            //_bestZoomFactorCurrent = CalculateBestZoom(_currentSlide.Width, _currentSlide.Height);
            _bestZoomFactorCurrent = _currentZoomFactor;
            _kenBurnsEffect = InitKenBurnsTransition();
            KenBurns(_kenBurnsEffect, true);
            ZoomCurrent(_currentZoomFactor);
          }

          int iNewMethod;
          if (_useRandomTransitions)
          {
            do
            {
              iNewMethod = _randomizer.Next(MAX_RENDER_METHODS);
            } while (iNewMethod == _transitionMethod);
            _transitionMethod = iNewMethod;
          }
          else
          {
            _transitionMethod = 9;
            //                                          _transitionMethod=1;
          }


          //g_application.ResetScreenSaver();
        }


        // swap our buffers over
        if (null == _backgroundSlide)
        {
          if (null == _currentSlide) return;
          PushCurrentTextureToBackground();
        }
      }

      // render the background overlay
      float x, y, width, height;

      // x-fade
      GUIGraphicsContext.DX9Device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
      if (_transitionMethod != 9 || _currentSlide == null)
      {
        GetOutputRect(_backgroundSlide.Width, _backgroundSlide.Height, _zoomFactorBackground, out x, out y, out width, out height);
        if (_zoomTopBackground + _zoomHeight > _backgroundSlide.Height) _zoomHeight = _backgroundSlide.Height - _zoomTopBackground;
        if (_zoomLeftBackground + _zoomWidth > _backgroundSlide.Width) _zoomWidth = _backgroundSlide.Width - _zoomLeftBackground;
        MediaPortal.Util.Picture.RenderImage(_backgroundSlide.Texture, x, y, width, height, _zoomWidth, _zoomHeight, _zoomLeftBackground, _zoomTopBackground, true);

        //MediaPortal.Util.Picture.DrawLine(10,10,300,300,0xffffffff);
        //MediaPortal.Util.Picture.DrawRectangle( new Rectangle(100,100,30,30),0xaaff0000,true);
        //MediaPortal.Util.Picture.DrawRectangle( new Rectangle(200,100,30,30),0xffffffff,false);
      }

      //        g_graphicsContext.Get3DDevice()->UpdateOverlay(m_pSurfaceBackGround, &source, &dest, true, 0x00010001);

      if (_currentSlide != null)
      {
        // render the new picture
        bool bResult = false;
        //Log.Info("method:{0} frame:{1}", _transitionMethod, _frameCounter);
        switch (_transitionMethod)
        {
          case 0:
            bResult = RenderMethod1();// open from left->right
            break;
          case 1:
            bResult = RenderMethod2();// move into the screen from left->right
            break;
          case 2:
            bResult = RenderMethod3();// move into the screen from right->left
            break;
          case 3:
            bResult = RenderMethod4();// move into the screen from up->bottom
            break;
          case 4:
            bResult = RenderMethod5();// move into the screen from bottom->top
            break;
          case 5:
            bResult = RenderMethod6();// open from up->bottom
            break;
          case 6:
            bResult = RenderMethod7();// slide from left<-right
            break;
          case 7:
            bResult = RenderMethod8();// slide from down->up
            break;
          case 8:
            bResult = RenderMethod9();// grow from middle
            break;
          case 9:
            bResult = RenderMethod10();// x-fade
            break;
        }

        if (bResult)
        {
          if (null == _currentSlide) return;
          PushCurrentTextureToBackground();
        }
      }
      else
      {
        // Start KenBurns effect
        if (_useKenBurns && _isSlideShow && !_isPaused)
        {
          if (_isPictureZoomed)
            _kenBurnsEffect = 0;
          else
            KenBurns(_kenBurnsEffect, false);
        }
      }

      if (_isSlideShow)
      {
        if (RenderPause()) return;
      }

      if (!_infoVisible && !_zoomInfoVisible && !_isLoadingRawPicture)
      {
        _autoHideOsd = true;
        return;
      }

      // Auto hide OSD
      if (_autoHideOsd && (_infoVisible || _zoomInfoVisible || _isLoadingRawPicture))
      {
        int dwOSDTimeElapsed = ((int)(DateTime.Now.Ticks / 10000)) - _slideTime;
        if (dwOSDTimeElapsed >= 3000)
        {
          _infoVisible = false;
          _zoomInfoVisible = false;
        }
      }


      if (_zoomInfoVisible || _infoVisible)
      {
        GUIControl.SetControlLabel(GetID, LABEL_ROW1, "");
        GUIControl.SetControlLabel(GetID, LABEL_ROW2, "");

        string strZoomInfo;
        //strZoomInfo=String.Format("{0}% ({1} , {2})", (int)(_userZoomLevel*100.0f), (int)_zoomLeft, (int)_zoomTop);
        strZoomInfo = String.Format("{0}% ({1} , {2})", (int)(_zoomFactorBackground * 100.0f), (int)_zoomLeftBackground, (int)_zoomTopBackground);

        GUIControl.SetControlLabel(GetID, LABEL_ROW2_EXTRA, strZoomInfo);
      }

      if (_infoVisible || _isLoadingRawPicture)
      {
        string strFileInfo, strSlideInfo;
        string strFileName = System.IO.Path.GetFileName(_backgroundSlide.FilePath);
        if (_backgroundSlide.TrueSizeTexture)
          strFileInfo = String.Format("{0} ({1}x{2}) ", strFileName, _backgroundSlide.Width - 2, _backgroundSlide.Height - 2);
        else
          strFileInfo = String.Format("{0}", strFileName);

        if (_isLoadingRawPicture)
        {
          strFileInfo = String.Format("{0}", GUILocalizeStrings.Get(13012));
          _zoomInfoVisible = false;
        }


        GUIControl.SetControlLabel(GetID, LABEL_ROW1, strFileInfo);
        strSlideInfo = String.Format("{0}/{1}", 1 + _currentSlideIndex, _slideList.Count);
        GUIControl.SetControlLabel(GetID, LABEL_ROW2, strSlideInfo);

        if (!_zoomInfoVisible)
        {
          GUIControl.SetControlLabel(GetID, LABEL_ROW2_EXTRA, "");
        }
      }
      base.Render(timePassed);
    }

    private void PushCurrentTextureToBackground()
    {
      if (null != _backgroundSlide)
      {
        //_backgroundSlide.Texture.Dispose();
        _backgroundSlide = null;
      }
      _backgroundSlide = _currentSlide;
      _zoomFactorBackground = _currentZoomFactor;
      _zoomLeftBackground = _currentZoomLeft;
      _zoomTopBackground = _currentZoomTop;
      _zoomTypeBackground = _currentZoomType;
      _currentSlide = null;
      _slideTime = (int)(DateTime.Now.Ticks / 10000);
    }

    #endregion

    #region public members
    public bool Playing
    {
      get { return _backgroundSlide != null; }
    }

    public int Count
    {
      get
      {
        return _slideList.Count;
      }
    }
    public void Add(string filename)
    {
      /*if (string.Compare(filename, SegmentIndicator) == 0)
      {
        if (_slideList.Count - _lastSegmentIndex > 1)
          _lastSegmentIndex = _slideList.Add(filename);
      }
      else if (MediaPortal.Util.Utils.IsPicture(filename))
      {
        _slideList.Add(filename);
      }*/
      _slideList.Add(filename);
    }

    public void Select(string strFile)
    {
      for (int i = 0; i < _slideList.Count; ++i)
      {
        string strSlide = _slideList[i];
        if (strSlide == strFile)
        {
          _currentSlideIndex = i - 1;
          return;
        }
      }
    }

    public bool InSlideShow
    {
      get { return _isSlideShow; }
    }

    public void Shuffle()
    {
      Random r = new System.Random(DateTime.Now.Millisecond);
      int nItemCount = _slideList.Count;

      // iterate through each catalogue item performing arbitrary swaps
      for (int nItem = 0; nItem < nItemCount; nItem++)
      {
        int nArbitrary = r.Next(nItemCount);

        if (nArbitrary != nItem)
        {
          string anItem = _slideList[nArbitrary];
          _slideList[nArbitrary] = _slideList[nItem];
          _slideList[nItem] = anItem;
        }
      }
    }

    public void StartSlideShow()
    {
      _isBackgroundMusicPlaying = false;

      if (_autoShuffle)
        Shuffle();

      _isSlideShow = true;
    }

    public void StartSlideShow(string path)
    {
      _isBackgroundMusicPlaying = false;

      StartBackgroundMusic(path);

      if (_autoShuffle)
        Shuffle();

      _isSlideShow = true;
    }


    public void Reset()
    {
      _slideList.Clear();
      _infoVisible = false;
      _zoomInfoVisible = false;
      _isSlideShow = false;
      _isPaused = false;

      _zoomFactorBackground = _defaultZoomFactor;
      _currentZoomFactor = _defaultZoomFactor;
      _kenBurnsEffect = 0;
      _isPictureZoomed = false;
      _currentZoomLeft = 0;
      _currentZoomTop = 0;
      _currentZoomLeft = 0;
      _currentZoomTop = 0;
      _frameCounter = 0;
      _currentSlideIndex = -1;
      _lastSlideShown = -1;
      _slideTime = 0;
      _userZoomLevel = 1.0f;
      _lastSegmentIndex = -1;

      if (null != _backgroundSlide)
      {
        //_backgroundSlide.Texture.Dispose();
        _backgroundSlide = null;
      }

      if (null != _currentSlide)
      {
        //_currentSlide.Texture.Dispose();
        _currentSlide = null;
      }

      if (_isBackgroundMusicPlaying)
        g_Player.Stop();
    }

    #endregion

    #region private members

    //Texture GetSlide(bool bTrueSize, out float dwWidth, out float dwHeight, out string strSlide)
    //{
    //  dwWidth = 0;
    //  dwHeight = 0;
    //  strSlide = "";
    //  if (_slideList.Count == 0) return null;

    //  _rotation = 0;
    //  _currentZoomFactor = 1.0f;
    //  _kenBurnsEffect = 0;
    //  _currentZoomleftFactor = 0;
    //  _currentZoomTopFactor = 0;
    //  _zoomInfoVisible = false;

    //  strSlide = _slideList[_currentSlide];
    //  Log.Info("Next Slide: {0}/{1} : {2}", _currentSlide + 1, _slideList.Count, strSlide);
    //  using (PictureDatabase dbs = new PictureDatabase())
    //  {
    //    _rotation = dbs.GetRotation(strSlide);
    //  }
    //  int iMaxWidth = GUIGraphicsContext.OverScanWidth;
    //  int iMaxHeight = GUIGraphicsContext.OverScanHeight;

    //  _trueSizeTexture = bTrueSize;
    //  if (bTrueSize)
    //  {
    //    iMaxWidth = MAX_PICTURE_WIDTH;
    //    iMaxHeight = MAX_PICTURE_HEIGHT;
    //  }

    //  int X, Y;
    //  Texture texture = MediaPortal.Util.Picture.Load(strSlide, _rotation, iMaxWidth, iMaxHeight, true, false, true, out X, out Y);
    //  dwWidth = X;
    //  dwHeight = Y;

    //  CalculateBestZoom(dwWidth, dwHeight);
    //  _currentZoomFactor = _defaultZoomFactor;
    //  return texture;
    //}

    void ShowNext()
    {
      ShowNext(false);
    }

    void ShowNext(bool jump)
    {
      if (!_isSlideShow)
        _update = true;

      // check image transition
      if (_currentSlide != null)
        PushCurrentTextureToBackground();

      _currentSlideIndex = NextSlideIndex(jump, _currentSlideIndex);

      if (_currentSlideIndex == 0)
      {
        if (_autoRepeat)
        {
          if (_autoShuffle)
            Shuffle();
        }
        else
        {
          ShowPreviousWindow();
          return;
        }
      }

      // Reset slide time
      _slideTime = (int)(DateTime.Now.Ticks / 10000);
      _frameCounter = 0;
    }

    private int NextSlideIndex(bool jump, int slideIndex)
    {
      slideIndex++;

      if (jump)
      {
        while (slideIndex < _slideList.Count && object.ReferenceEquals(_slideList[++slideIndex], SegmentIndicator) == false) ;

        slideIndex++;
      }
      else if (_lastSegmentIndex != -1)
      {
        if (object.ReferenceEquals(_slideList[slideIndex], SegmentIndicator))
          slideIndex++;
      }

      if (slideIndex >= _slideList.Count)
      {
        slideIndex = 0;
      }

      return slideIndex;
    }

    void ShowPrevious()
    {
      ShowPrevious(false);
    }

    void ShowPrevious(bool jump)
    {
      if (!_isSlideShow)
        _update = true;

      // check image transition
      if (_currentSlide != null)
        PushCurrentTextureToBackground();

      _currentSlideIndex = PreviousSlideIndex(jump, _currentSlideIndex);

      //PrefetchNextSlide();

      // Reset slide time
      _slideTime = (int)(DateTime.Now.Ticks / 10000);
      _frameCounter = 0;
    }

    private int PreviousSlideIndex(bool jump, int slideIndex)
    {
      slideIndex--;

      if (jump)
      {
        while (slideIndex >= 0 && object.ReferenceEquals(_slideList[slideIndex], SegmentIndicator) == false)
          slideIndex--;

        slideIndex++;
      }
      else if (_lastSegmentIndex != -1)
      {
        if (slideIndex > 0 && object.ReferenceEquals(_slideList[slideIndex], SegmentIndicator))
          slideIndex--;
      }

      if (slideIndex < 0)
        slideIndex = _slideList.Count - 1;
      return slideIndex;
    }

    #region render transition methods
    //pan from left->right
    bool RenderKenBurns(float zoom, float pan, DirectionType direction)
    {
      //zoom (75%-100%)
      if (zoom < 75) zoom = 75;
      if (zoom > 100) zoom = 100;

      //pan 75%-125%
      if (pan < 75) pan = 75;
      if (pan > 125) pan = 125;

      //direction (left,right,up,down)

      return true;
    }

    // Select transition based upon picture width/height
    int InitKenBurnsTransition()
    {
      int iEffect = 0;
      int iRandom = 0;

      iRandom = _randomizer.Next(2);
      switch (iRandom)
      {
        default:
        case 0:
          iEffect = 1; // Zoom
          break;
        case 1:
          iEffect = 2; // Pan
          break;
      }
      return iEffect;
    }

    // open from left->right
    bool RenderMethod1()
    {
      bool bResult = false;

      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = width / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float iExpandWidth = _frameCounter * iStep;
      if (iExpandWidth >= width)
      {
        iExpandWidth = width;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, y, iExpandWidth, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // move into the screen from left->right
    bool RenderMethod2()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = width / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float iPosX = _frameCounter * iStep - (int)width;
      if (iPosX >= x)
      {
        iPosX = x;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, iPosX, y, width, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // move into the screen from right->left
    bool RenderMethod3()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = width / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float posx = x + width - _frameCounter * iStep;
      if (posx <= x)
      {
        posx = x;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, posx, y, width, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // move into the screen from up->bottom
    bool RenderMethod4()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = height / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float posy = _frameCounter * iStep - height;
      if (posy >= y)
      {
        posy = y;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, posy, width, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // move into the screen from bottom->top
    bool RenderMethod5()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = height / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float posy = y + height - _frameCounter * iStep;
      if (posy <= y)
      {
        posy = y;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, posy, width, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }


    // open from up->bottom
    bool RenderMethod6()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = height / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float newheight = _frameCounter * iStep;
      if (newheight >= height)
      {
        newheight = height;
        bResult = true;
      }
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, y, width, newheight, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // slide from left<-right
    bool RenderMethod7()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = width / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float newwidth = _frameCounter * iStep;
      if (newwidth >= width)
      {
        newwidth = width;
        bResult = true;
      }

      //right align the texture
      float posx = x + width - newwidth;
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, posx, y, newwidth, height, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }


    // slide from down->up
    bool RenderMethod8()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStep = height / _slideShowTransistionFrames;
      if (0 == iStep) iStep = 1;
      float newheight = _frameCounter * iStep;
      if (newheight >= height)
      {
        newheight = height;
        bResult = true;
      }

      //bottom align the texture
      float posy = y + height - newheight;
      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, posy, width, newheight, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // grow from middle
    bool RenderMethod9()
    {
      bool bResult = false;
      float x, y, width, height;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      float iStepX = width / _slideShowTransistionFrames;
      float iStepY = height / _slideShowTransistionFrames;
      if (0 == iStepX) iStepX = 1;
      if (0 == iStepY) iStepY = 1;
      float newheight = _frameCounter * iStepY;
      float newwidth = _frameCounter * iStepX;
      if (newheight >= height)
      {
        newheight = height;
        bResult = true;
      }
      if (newwidth >= width)
      {
        newwidth = width;
        bResult = true;
      }

      //center align the texture
      float posx = x + (width - newwidth) / 2;
      float posy = y + (height - newheight) / 2;

      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, posx, posy, newwidth, newheight, _currentSlide.Width, _currentSlide.Height, 0, 0, false);
      return bResult;
    }

    // fade in
    bool RenderMethod10()
    {
      bool bResult = false;

      float x, y, width, height;
      GetOutputRect(_backgroundSlide.Width, _backgroundSlide.Height, _zoomFactorBackground, out x, out y, out width, out height);
      if (_zoomTopBackground + _zoomHeight > _backgroundSlide.Height) _zoomHeight = _backgroundSlide.Height - _zoomTopBackground;
      if (_zoomLeftBackground + _zoomWidth > _backgroundSlide.Width) _zoomWidth = _backgroundSlide.Width - _zoomLeftBackground;

      float iStep = 0xff / _slideShowTransistionFrames;
      if (_useKenBurns) iStep = 0xff / KENBURNS_XFADE_FRAMES;

      if (0 == iStep) iStep = 1;
      int iAlpha = (int)(_frameCounter * iStep);
      if (iAlpha >= 0xff)
      {
        iAlpha = 0xff;
        bResult = true;
      }


      //Log.Info("method 10 count:{0} alpha:{1:X}", _frameCounter, iAlpha);
      //render background first
      int lColorDiffuse = (0xff - iAlpha);
      lColorDiffuse <<= 24;
      lColorDiffuse |= 0xffffff;

      MediaPortal.Util.Picture.RenderImage(_backgroundSlide.Texture, x, y, width, height, _zoomWidth, _zoomHeight, _zoomLeftBackground, _zoomTopBackground, lColorDiffuse);

      //next render new image
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      lColorDiffuse = (iAlpha);
      lColorDiffuse <<= 24;
      lColorDiffuse |= 0xffffff;

      MediaPortal.Util.Picture.RenderImage(_currentSlide.Texture, x, y, width, height, _zoomWidth, _zoomHeight, _currentZoomLeft, _currentZoomTop, lColorDiffuse);
      return bResult;
    }

    /// <summary>
    /// Ken Burn effects
    /// </summary>
    /// <param name="iEffect"></param>
    /// <returns></returns>
    bool KenBurns(int iEffect, bool bReset)
    {
      bool bEnd = false;
      int iNrOfFramesPerEffect = _kenBurnTransistionSpeed * 30;

      // Init methode
      if (bReset)
      {
        // Set first state parameters: start and end zoom factor
        _frameNumber = 0;
        _kenBurnsState = 0;
      }

      // Check single effect end
      if (_frameNumber == iNrOfFramesPerEffect)
      {
        _frameNumber = 0;
        _kenBurnsState++;
      }

      // Select effect
      switch (iEffect)
      {
        default:
        case 0:
          // No effects, just wait for next picture
          break;

        case 1:
          bEnd = KenBurnsRandomZoom(_kenBurnsState, _frameNumber, iNrOfFramesPerEffect, bReset);
          break;

        case 2:
          bEnd = KenBurnsRandomPan(_kenBurnsState, _frameNumber, iNrOfFramesPerEffect, bReset);
          break;

      }

      // Check new rectangle
      if (_backgroundSlide != null)
      {
        if (_zoomTopBackground > (_backgroundSlide.Height - _zoomHeight)) _zoomTopBackground = (_backgroundSlide.Height - _zoomHeight);
        if (_zoomLeftBackground > (_backgroundSlide.Width - _zoomWidth)) _zoomLeftBackground = (_backgroundSlide.Width - _zoomWidth);
        if (_zoomTopBackground < 0) _zoomTopBackground = 0;
        if (_zoomLeftBackground < 0) _zoomLeftBackground = 0;
      }

      if (iEffect != 0)
      {
        if (!bEnd)
        {
          if (!bReset)
          {
            _frameNumber++;
          }
          _slideTime = (int)(DateTime.Now.Ticks / 10000);
        }
        else
        {
          // end
          _slideTime = (int)(DateTime.Now.Ticks / 10000) - (_speed * 1000);
        }
      }
      return bEnd;
    }

    /* Zoom types:
       * 0: // centered, centered
       * 1: // Width centered, Top unchanged
       * 2: // Heigth centered, Left unchanged
       * 3: // Widht centered, Bottom unchanged
       * 4: // Height centered, Right unchanged
       * 5: // Top Left unchanged
       * 6: // Top Right unchanged
       * 7: // Bottom Left unchanged
       * 8: // Bottom Right unchanged
       * */

    /* Zoom points arround the rectangle
     * Selected zoom type will hold the selected point at the same place while zooming the rectangle
     *
     *     1---------2---------3
     *     |                   |
     *     8         0         4
     *     |                   |
     *     7---------6---------5
     *
     */

    bool KenBurnsRandomZoom(int iState, int iFrameNr, int iNrOfFramesPerEffect, bool bReset)
    {
      int iRandom;
      bool bEnd = false;
      if (bReset)
      {
        iRandom = _randomizer.Next(3);
        switch (iRandom)
        {
          case 0:
            if (_landScape)
              _currentZoomType = 8; // from left
            else
              _currentZoomType = 2; // from top
            break;

          case 1:
            if (_landScape)
              _currentZoomType = 4; // from right
            else
              _currentZoomType = 6; // from bottom
            break;

          default:
          case 2:
            _currentZoomType = 0; // centered
            break;
        }

        // Init zoom
        if (_fullScreen)
          _endZoomFactor = _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR_FS;
        else
          _endZoomFactor = _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR;

        _startZoomFactor = _bestZoomFactorCurrent;
        _zoomChange = (_endZoomFactor - _startZoomFactor) / iNrOfFramesPerEffect;
        _currentZoomFactor = _startZoomFactor;
      }
      else
      {
        switch (iState)
        {
          case 0: // Zoom in
            float m_fZoomFactor = _startZoomFactor + _zoomChange * iFrameNr;
            ZoomBackGround(m_fZoomFactor);
            break;

          default:
            bEnd = true;
            break;
        }
      }

      return bEnd;
    }



    bool KenBurnsRandomPan(int iState, int iFrameNr, int iNrOfFramesPerEffect, bool bReset)
    {
      // For Landscape picutres zoomstart BestWidth than Pan
      int iRandom;
      bool bEnd = false;
      if (bReset)
      {
        // find start and end points (8 possible points around the rectangle)
        iRandom = _randomizer.Next(14);
        if (_landScape)
        {
          switch (iRandom)
          {
            default:
            case 0:
              _startPoint = 1;
              _endPoint = 4;
              break;
            case 1:
              _startPoint = 1;
              _endPoint = 5;
              break;
            case 2:
              _startPoint = 8;
              _endPoint = 3;
              break;
            case 3:
              _startPoint = 8;
              _endPoint = 4;
              break;
            case 4:
              _startPoint = 8;
              _endPoint = 5;
              break;
            case 5:
              _startPoint = 7;
              _endPoint = 4;
              break;
            case 6:
              _startPoint = 7;
              _endPoint = 3;
              break;
            case 7:
              _startPoint = 5;
              _endPoint = 8;
              break;
            case 8:
              _startPoint = 5;
              _endPoint = 1;
              break;
            case 9:
              _startPoint = 4;
              _endPoint = 7;
              break;
            case 10:
              _startPoint = 4;
              _endPoint = 8;
              break;
            case 11:
              _startPoint = 4;
              _endPoint = 1;
              break;
            case 12:
              _startPoint = 3;
              _endPoint = 7;
              break;
            case 13:
              _startPoint = 3;
              _endPoint = 8;
              break;
          }
        }
        else
        {
          // Portrait
          switch (iRandom)
          {
            default:
            case 0:
              _startPoint = 1;
              _endPoint = 6;
              break;
            case 1:
              _startPoint = 1;
              _endPoint = 5;
              break;
            case 2:
              _startPoint = 2;
              _endPoint = 7;
              break;
            case 3:
              _startPoint = 2;
              _endPoint = 6;
              break;
            case 4:
              _startPoint = 2;
              _endPoint = 5;
              break;
            case 5:
              _startPoint = 3;
              _endPoint = 7;
              break;
            case 6:
              _startPoint = 3;
              _endPoint = 6;
              break;
            case 7:
              _startPoint = 5;
              _endPoint = 2;
              break;
            case 8:
              _startPoint = 5;
              _endPoint = 1;
              break;
            case 9:
              _startPoint = 6;
              _endPoint = 3;
              break;
            case 10:
              _startPoint = 6;
              _endPoint = 2;
              break;
            case 11:
              _startPoint = 6;
              _endPoint = 1;
              break;
            case 12:
              _startPoint = 7;
              _endPoint = 3;
              break;
            case 13:
              _startPoint = 7;
              _endPoint = 2;
              break;
          }
        }

        // Init 120% top center fixed
        if (_fullScreen)
          _currentZoomFactor = _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR_FS;
        else
          _currentZoomFactor = _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR;

        _currentZoomType = _startPoint;
      }
      else
      {
        switch (iState)
        {
          case 0: // - Pan start point to end point
            if (iFrameNr == 0)
            {
              // Init single effect
              float iDestY = 0;
              float iDestX = 0;
              switch (_endPoint)
              {
                case 8:
                  iDestY = (float)_backgroundSlide.Height / 2;
                  iDestX = (float)_zoomWidth / 2;
                  break;
                case 4:
                  iDestY = (float)_backgroundSlide.Height / 2;
                  iDestX = (float)_backgroundSlide.Width - (float)_zoomWidth / 2;
                  break;
                case 2:
                  iDestY = (float)_zoomHeight / 2;
                  iDestX = (float)_backgroundSlide.Width / 2;
                  break;
                case 6:
                  iDestY = (float)_backgroundSlide.Height - (float)_zoomHeight / 2;
                  iDestX = (float)_backgroundSlide.Width / 2;
                  break;
                case 1:
                  iDestY = (float)_zoomHeight / 2;
                  iDestX = (float)_zoomWidth / 2;
                  break;
                case 3:
                  iDestY = (float)_zoomHeight / 2;
                  iDestX = (float)_backgroundSlide.Width - (float)_zoomWidth / 2;
                  break;
                case 7:
                  iDestY = (float)_backgroundSlide.Height - (float)_zoomHeight / 2;
                  iDestX = (float)_zoomWidth / 2;
                  break;
                case 5:
                  iDestY = (float)_backgroundSlide.Height - (float)_zoomHeight / 2;
                  iDestX = (float)_backgroundSlide.Width - (float)_zoomWidth / 2;
                  break;
              }

              _panYChange = (iDestY - (_currentZoomTop + (float)_zoomHeight / 2)) / iNrOfFramesPerEffect; // Travel Y;
              _panXChange = (iDestX - (_currentZoomLeft + (float)_zoomWidth / 2)) / iNrOfFramesPerEffect; // Travel Y;
            }

            PanBackGround(_panXChange, _panYChange);
            break;

          default:
            bEnd = true;
            break;
        }
      }

      return bEnd;
    }

    /// <summary>
    /// Pan picture rectangle
    /// </summary>
    /// <param name="fPanX"></param>
    /// <param name="fPanY"></param>
    /// <returns>False if panned outside the picture</returns>

    bool PanBackGround(float fPanX, float fPanY)
    {
      if ((fPanX == 0.0f) && (fPanY == 0.0f)) return false;

      _zoomLeftBackground += fPanX;
      _zoomTopBackground += fPanY;

      if (_zoomTopBackground < 0) return false;
      if (_zoomLeftBackground < 0) return false;
      if (_zoomTopBackground > (_backgroundSlide.Height - _zoomHeight)) return false;
      if (_zoomLeftBackground > (_backgroundSlide.Width - _zoomWidth)) return false;

      return true;
    }
    #endregion

    float CalculateBestZoom(float fWidth, float fHeight)
    {
      float fZoom;
      // Default picutes is zoom best fit (max width or max height)
      float fPixelRatio = GUIGraphicsContext.PixelRatio;
      float ZoomFactorX = (float)(GUIGraphicsContext.OverScanWidth * fPixelRatio) / fWidth;
      float ZoomFactorY = (float)GUIGraphicsContext.OverScanHeight / fHeight;

      // Get minimal zoom level (1.0==100%)
      fZoom = ZoomFactorX;//-ZoomFactorY+1.0f;
      _landScape = true;
      if (ZoomFactorY < ZoomFactorX)
      {
        fZoom = ZoomFactorY;//-ZoomFactorX+1.0f;
        _landScape = false;
      }

      _fullScreen = false;
      if ((ZoomFactorY < KENBURNS_ZOOM_FACTOR_FS) && (ZoomFactorX < KENBURNS_ZOOM_FACTOR_FS))
        _fullScreen = true;

      // Fit to screen default zoom factor
      _defaultZoomFactor = fZoom;

      // Zoom 100%..150%
      if (fZoom < 1.00f)
        fZoom = 1.00f;
      if (fZoom > KENBURNS_MAXZOOM)
        fZoom = KENBURNS_MAXZOOM;

      //return fZoom;
      return 1.0f;
    }

    void ShowContextMenu()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      dlg.SetHeading(498); // menu

      dlg.AddLocalizedString(117); //delete
      dlg.AddLocalizedString(735); //rotate
      if (!_isSlideShow) dlg.AddLocalizedString(108); //start slideshow
      dlg.AddLocalizedString(940); //properties
      dlg.AddLocalizedString(970); //Exit

      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;
      switch (dlg.SelectedId)
      {
        case 117: // Delete
          OnDelete();
          break;

        case 735: // Rotate
          DoRotate();
          break;

        case 108: // Start slideshow
          StartSlideShow();
          break;

        case 940: // Properties
          OnShowInfo();
          break;

        case 970:
          ShowPreviousWindow();
          break;
      }
    }

    void OnDelete()
    {
      // delete current picture
      GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);
      if (null == dlgYesNo) return;
      if (_backgroundSlide == null || _backgroundSlide.FilePath.Length == 0) return;
      bool bPause = _isPaused;
      _isPaused = true;
      string strFileName = System.IO.Path.GetFileName(_backgroundSlide.FilePath);
      dlgYesNo.SetHeading(GUILocalizeStrings.Get(664));
      dlgYesNo.SetLine(1, String.Format("{0}/{1}", 1 + _currentSlideIndex, _slideList.Count));
      dlgYesNo.SetLine(2, strFileName);
      dlgYesNo.SetLine(3, "");
      dlgYesNo.DoModal(GetID);

      _isPaused = bPause;
      if (!dlgYesNo.IsConfirmed) return;
      if (MediaPortal.Util.Utils.FileDelete(_backgroundSlide.FilePath) == true)
      {
        if (_currentSlideIndex < _slideList.Count) _slideList.RemoveAt(_currentSlideIndex);

        _slideTime = (int)(DateTime.Now.Ticks / 10000);
        _lastSlideShown = -1;
        _update = true;
      }
    }

    void OnShowInfo()
    {
      GUIDialogExif exifDialog = (GUIDialogExif)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_EXIF);
      exifDialog.FileName = _backgroundSlide.FilePath;
      exifDialog.DoModal(GetID);
    }


    bool RenderPause()
    {
      _counter++;
      if (_counter > 25)
      {
        _counter = 0;
      }
      if ((!_isPaused && !_infoVisible && !_zoomInfoVisible && !_isPictureZoomed) || _zoomInfoVisible || _infoVisible) return false;

      if (_counter < 13) return false;
      GUIFont pFont = GUIFontManager.GetFont("font13");
      if (pFont != null)
      {
        string szText = GUILocalizeStrings.Get(112);
        pFont.DrawShadowText(500.0f, 60.0f, 0xffffffff, szText, GUIControl.Alignment.ALIGN_LEFT, 2, 2, 0xff000000);
      }
      return true;
    }

    void DoRotate()
    {
      int rotation = _backgroundSlide.Rotation;
      rotation++;
      if (rotation >= 4)
      {
        rotation = 0;
      }

      using (PictureDatabase dbs = new PictureDatabase())
      {
        dbs.SetRotation(_backgroundSlide.FilePath, rotation);
      }

      InvalidateSlide(_backgroundSlide.FilePath);

      if (null != _currentSlide)
      {
        //_currentSlide.Texture.Dispose();
        _currentSlide = null;
      }
      if (_backgroundSlide == null || _backgroundSlide.FilePath.Length == 0) return;

      LoadCurrentSlide();

      //int iMaxWidth = GUIGraphicsContext.OverScanWidth;
      //int iMaxHeight = GUIGraphicsContext.OverScanHeight;
      //int X, Y;
      //_currentTexture = MediaPortal.Util.Picture.Load(_backgroundSlideFileName, _rotation, iMaxWidth, iMaxHeight, true, true, out X, out Y);
      //_currentSlide.Width = X;
      //_currentSlide.Height = Y;
      //_currentSlideFileName = _backgroundSlideFileName;
      //_zoomFactorBackground = _defaultZoomFactor;
      //_kenBurnsEffect = 0;
      //_userZoomLevel = 1.0f;
      //_isPictureZoomed = false;
      //_zoomLeft = 0;
      //_zoomTop = 0;
      //_slideTime = (int)(DateTime.Now.Ticks / 10000);
      //_frameCounter = 0;
      _transitionMethod = 9;

      DeleteThumb(_backgroundSlide.FilePath);
    }

    void GetOutputRect(float iSourceWidth, float iSourceHeight, float fZoomLevel, out float x, out float y, out float width, out float height)
    {
      // calculate aspect ratio correction factor
      float iOffsetX1 = GUIGraphicsContext.OverScanLeft;
      float iOffsetY1 = GUIGraphicsContext.OverScanTop;
      float iScreenWidth = GUIGraphicsContext.OverScanWidth;
      float iScreenHeight = GUIGraphicsContext.OverScanHeight;
      float fPixelRatio = GUIGraphicsContext.PixelRatio;

      float fSourceFrameAR = ((float)iSourceWidth) / ((float)iSourceHeight);
      float fOutputFrameAR = fSourceFrameAR / fPixelRatio;

      width = (iSourceWidth / fPixelRatio) * fZoomLevel;
      height = iSourceHeight * fZoomLevel;

      _zoomWidth = iSourceWidth;
      _zoomHeight = iSourceHeight;

      // check org rectangle
      if (width > iScreenWidth)
      {
        width = iScreenWidth;
        _zoomWidth = (width * fPixelRatio) / fZoomLevel;
      }

      if (height > iScreenHeight)
      {
        height = iScreenHeight;
        _zoomHeight = height / fZoomLevel;
      }

      if (_zoomHeight > iSourceHeight)
      {
        _zoomHeight = iSourceHeight;
        _zoomWidth = _zoomHeight * fSourceFrameAR;
      }

      if (_zoomWidth > iSourceWidth)
      {
        _zoomWidth = iSourceWidth;
        _zoomHeight = _zoomWidth / fSourceFrameAR;
      }

      x = (iScreenWidth - width) / 2 + iOffsetX1;
      y = (iScreenHeight - height) / 2 + iOffsetY1;
    }

    void ZoomCurrent(float fZoom)
    {
      if (fZoom > MAX_ZOOM_FACTOR || fZoom < 0.0f)
        return;

      // Start and End point positions along the picture rectangle
      // Point zoom in/out only works if the selected Point is at the border
      // example:  "m_dwWidthBackGround == m_iZoomLeft + _zoomWidth"  and zooming to the left (iZoomType=4)
      float middlex = _currentSlide.Width / 2;
      float middley = _currentSlide.Height / 2;
      float xend = _currentSlide.Width;
      float yend = _currentSlide.Height;

      float x, y, width, height;
      _currentZoomFactor = fZoom;
      GetOutputRect(_currentSlide.Width, _currentSlide.Height, _currentZoomFactor, out x, out y, out width, out height);
      if (_currentZoomTop + _zoomHeight > _currentSlide.Height) _zoomHeight = _currentSlide.Height - _currentZoomTop;
      if (_currentZoomLeft + _zoomWidth > _currentSlide.Width) _zoomWidth = _currentSlide.Width - _currentZoomLeft;

      switch (_currentZoomType)
      {
        /* 0: // centered, centered
          * 1: // Top Left unchanged
          * 2: // Width centered, Top unchanged
          * 3: // Top Right unchanged
          * 4: // Height centered, Right unchanged
          * 5: // Bottom Right unchanged
          * 6: // Widht centered, Bottom unchanged
          * 7: // Bottom Left unchanged
          * 8: // Heigth centered, Left unchanged
          * */
        case 0: // centered, centered
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          break;
        case 2: // Width centered, Top unchanged
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          break;
        case 8: // Heigth centered, Left unchanged
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          break;
        case 6: // Widht centered, Bottom unchanged
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          _currentZoomTop = yend - _zoomHeight;
          break;
        case 4: // Height centered, Right unchanged
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          _currentZoomLeft = xend - _zoomWidth;
          break;
        case 1: // Top Left unchanged
          break;
        case 3: // Top Right unchanged
          _currentZoomLeft = xend - _zoomWidth;
          break;
        case 7: // Bottom Left unchanged
          _currentZoomTop = yend - _zoomHeight;
          break;
        case 5: // Bottom Right unchanged
          _currentZoomTop = yend - _zoomHeight;
          _currentZoomLeft = xend - _zoomWidth;
          break;
      }
      if (_currentZoomLeft > _currentSlide.Width - _zoomWidth) _currentZoomLeft = (_currentSlide.Width - _zoomWidth);
      if (_currentZoomTop > _currentSlide.Height - _zoomHeight) _currentZoomTop = (_currentSlide.Height - _zoomHeight);
      if (_currentZoomLeft < 0) _currentZoomLeft = 0;
      if (_currentZoomTop < 0) _currentZoomTop = 0;
    }

    void LoadRawPictureThread()
    {
      // load picture
      string slideFilePath = _slideList[_currentSlideIndex];
      _backgroundSlide = new SlidePicture(slideFilePath, true);
      _isLoadingRawPicture = false;
    }

    void ZoomBackGround(float fZoom)
    {
      if (fZoom > MAX_ZOOM_FACTOR || fZoom < 0.0f)
        return;

      // to make it possibel to reach a 100% zoom level without changing the slideshow code, this condition are used
      if (_isSlideShow) _isPictureZoomed = _userZoomLevel == 1.0f ? false : true;
      else _isPictureZoomed = _userZoomLevel == _defaultZoomFactor ? false : true;
            
      // Load raw picture when zooming
      if (!_backgroundSlide.TrueSizeTexture && _isPictureZoomed)
      {
        _zoomInfoVisible = true;
        _isLoadingRawPicture = true;
        using (WaitCursor cursor = new WaitCursor())
        {
          Thread WorkerThread = new Thread(new ThreadStart(LoadRawPictureThread));
          WorkerThread.Start();


          // Update window
          while (_isLoadingRawPicture)
            GUIWindowManager.Process();

          // load picture
          //_backgroundTexture=GetSlide(true, out _backgroundSlide.Width,out _backgroundSlide.Height, out _currentSlideFileName);
          //_isLoadingRawPicture=false;
        }
        fZoom = _defaultZoomFactor * _userZoomLevel;
      }

      // Start and End point positions along the picture rectangle
      // Point zoom in/out only works if the selected Point is at the border
      // example:  "m_dwWidthBackGround == m_iZoomLeft + _zoomWidth"  and zooming to the left (iZoomType=4)
      float middlex = _zoomLeftBackground + _zoomWidth / 2;
      float middley = _zoomTopBackground + _zoomHeight / 2;
      float xend = _backgroundSlide.Width;
      float yend = _backgroundSlide.Height;

      _zoomFactorBackground = fZoom;

      float x, y, width, height;
      GetOutputRect(_backgroundSlide.Width, _backgroundSlide.Height, _zoomFactorBackground, out x, out y, out width, out height);

      if (_isPictureZoomed) _zoomTypeBackground = 0;
      switch (_zoomTypeBackground)
      {
        /* 0: // centered, centered
           * 1: // Top Left unchanged
           * 2: // Width centered, Top unchanged
           * 3: // Top Right unchanged
           * 4: // Height centered, Right unchanged
           * 5: // Bottom Right unchanged
           * 6: // Widht centered, Bottom unchanged
           * 7: // Bottom Left unchanged
           * 8: // Heigth centered, Left unchanged
           * */
        case 0: // centered, centered
          _zoomLeftBackground = middlex - _zoomWidth * 0.5f;
          _zoomTopBackground = middley - _zoomHeight * 0.5f;
          break;
        case 2: // Width centered, Top unchanged
          _zoomLeftBackground = middlex - _zoomWidth * 0.5f;
          break;
        case 8: // Heigth centered, Left unchanged
          _zoomTopBackground = middley - _zoomHeight * 0.5f;
          break;
        case 6: // Widht centered, Bottom unchanged
          _zoomLeftBackground = middlex - _zoomWidth * 0.5f;
          _zoomTopBackground = yend - _zoomHeight;
          break;
        case 4: // Height centered, Right unchanged
          _zoomTopBackground = middley - _zoomHeight * 0.5f;
          _zoomLeftBackground = xend - _zoomWidth;
          break;
        case 1: // Top Left unchanged
          break;
        case 3: // Top Right unchanged
          _zoomLeftBackground = xend - _zoomWidth;
          break;
        case 7: // Bottom Left unchanged
          _zoomTopBackground = yend - _zoomHeight;
          break;
        case 5: // Bottom Right unchanged
          _zoomTopBackground = yend - _zoomHeight;
          _zoomLeftBackground = xend - _zoomWidth;
          break;
      }
      if (_zoomLeftBackground > _backgroundSlide.Width - _zoomWidth) _zoomLeftBackground = (_backgroundSlide.Width - _zoomWidth);
      if (_zoomTopBackground > _backgroundSlide.Height - _zoomHeight) _zoomTopBackground = (_backgroundSlide.Height - _zoomHeight);
      if (_zoomLeftBackground < 0) _zoomLeftBackground = 0;
      if (_zoomTopBackground < 0) _zoomTopBackground = 0;
    }

    void LoadSettings()
    {
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        _speed = xmlreader.GetValueAsInt("pictures", "speed", 3);
        _slideShowTransistionFrames = xmlreader.GetValueAsInt("pictures", "transition", 20);
        _kenBurnTransistionSpeed = xmlreader.GetValueAsInt("pictures", "kenburnsspeed", 20);
        _useKenBurns = xmlreader.GetValueAsBool("pictures", "kenburns", false);
        _useRandomTransitions = xmlreader.GetValueAsBool("pictures", "random", true);
        _autoShuffle = xmlreader.GetValueAsBool("pictures", "autoShuffle", false);
        _autoRepeat = xmlreader.GetValueAsBool("pictures", "autoRepeat", false);
        //                              _isBackgroundMusicEnabled = xmlreader.GetValueAsBool("pictures", "backgroundmusic", false);
        _isBackgroundMusicEnabled = true;

        if (_isBackgroundMusicEnabled)
        {
          string extensions = xmlreader.GetValueAsString("music", "extensions", ".mp3,.pls,.wpl");
          _musicFileExtensions = extensions.Split(',');
        }
      }
    }

    void DeleteThumb(string strSlide)
    {
      string strThumb = GUIPictures.GetThumbnail(strSlide);
      MediaPortal.Util.Utils.FileDelete(strThumb);
      strThumb = GUIPictures.GetLargeThumbnail(strSlide);
      MediaPortal.Util.Utils.FileDelete(strThumb);
    }

    void StartBackgroundMusic(string path)
    {
      if (g_Player.IsMusic || g_Player.IsRadio || g_Player.IsTV || g_Player.IsVideo)
        return;

      if (_musicFileExtensions == null)
      {
        using (MediaPortal.Profile.Settings reader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
          _musicFileExtensions = reader.GetValueAsString("music", "extensions", ".mp3,.pls,.wpl").Split(',');
      }

      foreach (string extension in _musicFileExtensions)
      {
        string filename = string.Format(@"{0}\Folder{1}", path, extension);

        if (File.Exists(filename) == false)
          continue;

        try
        {
          playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_MUSIC_TEMP).Clear();
          playlistPlayer.Reset();
          playlistPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_MUSIC_TEMP;

          PlayListItem playlistItem = new PlayListItem();

          playlistItem.Type = Playlists.PlayListItem.PlayListItemType.Audio;
          playlistItem.FileName = filename;
          playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_MUSIC_TEMP).Add(playlistItem);
          playlistPlayer.Play(0);

          _isBackgroundMusicPlaying = true;
        }
        catch (Exception e)
        {
          Log.Info("GUISlideShow.StartBackgroundMusic", e.Message);
        }

        break;
      }
    }

    void ShowPreviousWindow()
    {
      if (_isBackgroundMusicPlaying)
        g_Player.Stop();

      GUIWindowManager.ShowPreviousWindow();
    }

    void ShowSong()
    {
      GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
      if (dlg == null) return;

      //get albumart
      string albumart = g_Player.CurrentFile;
      int e = albumart.LastIndexOf(@"\") + 1;
      albumart = albumart.Remove(e);
      if (_slideList[_currentSlideIndex].Contains(albumart))
        albumart = string.Empty;
      else
      {
        albumart = albumart + "folder.jpg";
        if (!File.Exists(albumart))
          albumart = string.Empty;
      }
      // get Song-info

      // hwahrmann 2006-11-22 Using the Tagreader caused a COM exception in Win Media SDK, when reading WMA files
      // Accessing the Music Database instead of using the Tagreader.
      //MediaPortal.TagReader.MusicTag tag = MediaPortal.TagReader.TagReader.ReadTag(g_Player.CurrentFile);
      Song song = new Song();

      // If we don't have a tag in the db, we use the filename without the extension as song.title
      song.Title = Path.GetFileNameWithoutExtension(g_Player.CurrentFile);
      mDB.GetSongByFileName(g_Player.CurrentFile, ref song);

      // Show Dialog
      dlg.Reset();
      dlg.ClearAll();
      dlg.SetImage(albumart);
      dlg.SetHeading(4540);
      //dlg.SetText(tag.Title + "\n" + tag.Artist + "\n" + tag.Album);
      dlg.SetText(song.Title + "\n" + song.Artist + "\n" + song.Album);
      dlg.TimeOut = 5;
      dlg.DoModal(GUIWindowManager.ActiveWindow);
    }

    #endregion
  }
}