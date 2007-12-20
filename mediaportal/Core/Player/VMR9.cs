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
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DShowNET;
using DShowNET.Helper;
using DirectShowLib;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
using MediaPortal.Configuration;

namespace MediaPortal.Player
{
  /// <summary>
  /// General helper class to add the Video Mixing Render9 filter to a graph
  /// , set it to renderless mode and provide it our own allocator/presentor
  /// This will allow us to render the video to a direct3d texture
  /// which we can use to draw the transparent OSD on top of it
  /// Some classes which work together:
  ///  VMR9Util								: general helper class
  ///  AllocatorWrapper.cs		: implements our own allocator/presentor for vmr9 by implementing
  ///                           IVMRSurfaceAllocator9 and IVMRImagePresenter9
  ///  PlaneScene.cs          : class which draws the video texture onscreen and mixes it with the GUI, OSD,...                          
  /// </summary>
  /// // {324FAA1F-7DA6-4778-833B-3993D8FF4151}

  #region IVMR9PresentCallback interface
  [ComVisible(true), ComImport,
  Guid("324FAA1F-7DA6-4778-833B-3993D8FF4151"),
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IVMR9PresentCallback
  {
    [PreserveSig]
    int PresentImage(Int16 cx, Int16 cy, Int16 arx, Int16 ary, uint dwImg);
    [PreserveSig]
    int PresentSurface(Int16 cx, Int16 cy, Int16 arx, Int16 ary, uint dwImg);
  }

  #endregion


  public class VMR9Util : IDisposable
  {
    
    #region imports
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern bool Vmr9Init(IVMR9PresentCallback callback, uint dwD3DDevice, IBaseFilter vmr9Filter, uint monitor);
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern void Vmr9Deinit();
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern void Vmr9SetDeinterlaceMode(Int16 mode);
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern void Vmr9SetDeinterlacePrefs(uint dwMethod);
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern bool EvrInit(IVMR9PresentCallback callback, uint dwD3DDevice, IBaseFilter vmr9Filter, uint monitor);//, uint dwWindow);
    [DllImport("dshowhelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    unsafe private static extern void EvrDeinit();
    #endregion

    #region static vars
    public static VMR9Util g_vmr9 = null;
    static int _instanceCounter = 0;
    #endregion

    #region enums
    enum Vmr9PlayState
    {
      Playing,
      Repaint
    }
    #endregion

    #region vars
    PlaneScene _scene = null;
    bool _useVmr9 = false;
    IRender _renderFrame;
    IBaseFilter _vmr9Filter = null;
    int _videoHeight, _videoWidth;
    int _videoAspectRatioX, _videoAspectRatioY;

    IQualProp _qualityInterface = null;
    int _frameCounter = 0;
    DateTime _repaintTimer = DateTime.Now;
    IVMRMixerBitmap9 _vmr9MixerBitmapInterface = null;
    IGraphBuilder _graphBuilderInterface = null;
    bool _isVmr9Initialized = false;
    int _threadId;
    Vmr9PlayState currentVmr9State = Vmr9PlayState.Playing;
  
    #endregion

    #region ctor
    /// <summary>
    /// Constructor
    /// </summary>
    public VMR9Util()
    {

      _useVmr9 = true;
      Log.Info("vmr9:ctor()");
      if (!GUIGraphicsContext.VMR9Allowed)
      {
        Log.Info("vmr9:ctor() not allowed");
        _useVmr9 = false;
        return;
      }
      _renderFrame = GUIGraphicsContext.RenderGUI;
      if (GUIGraphicsContext.DX9Device == null)
      {
        _useVmr9 = false;
        Log.Info("vmr9:ctor() DX9Device=null");
      }
      if (_renderFrame == null)
      {
        _useVmr9 = false;
        Log.Info("vmr9:ctor() _renderFrame=null");
      }
      if (g_vmr9 != null || GUIGraphicsContext.Vmr9Active)
      {
        _useVmr9 = false;
        Log.Info("vmr9:ctor() already active");
      }
      Log.Info("vmr9:ctor() done:{0}", _useVmr9);
    }
    #endregion

    #region properties

    public bool UseVmr9
    {
      get { return _useVmr9; }
    }

    public int FrameCounter
    {
      get
      {
        return _frameCounter;
      }
      set
      {
        _frameCounter = value;
      }
    }

    /// <summary>
    /// returns the width of the video
    /// </summary>
    public int VideoWidth
    {
      get
      {
        return _videoWidth;
      }
      set
      {
        _videoWidth = value;
      }
    }

    /// <summary>
    /// returns the height of the video
    /// </summary>
    public int VideoHeight
    {
      get
      {
        return _videoHeight;
      }
      set
      {
        _videoHeight = value;
      }
    }


    /// <summary>
    /// returns the width of the video
    /// </summary>
    public int VideoAspectRatioX
    {
      get
      {
        return _videoAspectRatioX;
      }
      set
      {
        _videoAspectRatioX = value;
      }
    }

    /// <summary>
    /// returns the height of the video
    /// </summary>
    public int VideoAspectRatioY
    {
      get
      {
        return _videoAspectRatioY;
      }
      set
      {
        _videoAspectRatioY = value;
      }
    }
    public IPin PinConnectedTo
    {
      get
      {
        if (!_isVmr9Initialized) return null;
        if (_vmr9Filter == null || !_useVmr9)
        {
          return null;
        }

        IPin pinIn, pinConnected;
        pinIn = DsFindPin.ByDirection(_vmr9Filter, PinDirection.Input, 0);
        if (pinIn == null)
        {
          //no input pin found, vmr9 is not possible
          return null;
        }
        pinIn.ConnectedTo(out pinConnected);
        Marshal.ReleaseComObject(pinIn);
        return pinConnected;
      }
    }
    /// <summary>
    /// This method returns true if VMR9 is enabled AND WORKING!
    /// this allows players to check if if VMR9 is working after setting up the playing graph
    /// by checking if VMR9 is possible they can for example fallback to the overlay device
    /// </summary>
    public bool IsVMR9Connected
    {
      get
      {
        if (!_isVmr9Initialized) return false;
        // check if vmr9 is enabled and if initialized
        if (_vmr9Filter == null || !_useVmr9)
        {
          Log.Warn("vmr9: not used or no filter:{0} {1:x}", _useVmr9, _vmr9Filter);
          return false;
        }

        int hr = 0;
        //get the VMR9 input pin#0 is connected
        for (int i = 0; i < 3; ++i)
        {
          IPin pinIn, pinConnected;
          pinIn = DsFindPin.ByDirection(_vmr9Filter, PinDirection.Input, i);
          if (pinIn == null)
          {
            //no input pin found, vmr9 is not possible
            Log.Warn("vmr9: no input pin {0} found", i);
            continue;
          }

          //check if the input is connected to a video decoder
          hr = pinIn.ConnectedTo(out pinConnected);
          if (pinConnected == null)
          {
            //no pin is not connected so vmr9 is not possible
            Log.Warn("vmr9: pin:{0} not connected:{1:x}", i, hr);
          }
          else
          {
            //Log.Info("vmr9: pin:{0} is connected",i);
            if (pinIn != null)
              hr = Marshal.ReleaseComObject(pinIn);
            if (pinConnected != null)
              hr = Marshal.ReleaseComObject(pinConnected);
            return true;
          }
          if (pinIn != null)
            hr = Marshal.ReleaseComObject(pinIn);
          if (pinConnected != null)
            hr = Marshal.ReleaseComObject(pinConnected);
        }
        return false;
      }//get {
    }//public bool IsVMR9Connected

    #endregion

    #region public members
    /// <summary>
    /// Add VMR9 filter to graph and configure it
    /// </summary>
    /// <param name="graphBuilder"></param>
    public bool AddVMR9(IGraphBuilder graphBuilder)
    {
      Log.Info("vmr9:addvmr9");
      if (!_useVmr9)
      {
        Log.Info("vmr9:addvmr9: dont use vmr9");
        return false;
      }
      if (_isVmr9Initialized)
      {
        Log.Info("vmr9:addvmr9: vmr9 already initialized");
        return false;
      }
      bool _useEvr = GUIGraphicsContext.IsEvr;
      Log.Info("EVR-Flag is set to {0}", _useEvr);
      //Log.Info("VMR9Helper:AddVmr9");
      if (_instanceCounter != 0)
      {
        Log.Error("VMR9Helper:Multiple instances of VMR9 running!!!");
        throw new Exception("VMR9Helper:Multiple instances of VMR9 running!!!");
      }

      if (_useEvr)
        _vmr9Filter = (IBaseFilter)new EnhancedVideoRenderer();
      else
        _vmr9Filter = (IBaseFilter)new VideoMixingRenderer9();
      if (_vmr9Filter == null)
      {
        Error.SetError("Unable to play movie", "VMR9 is not installed");
        Log.Error("VMR9Helper:Failed to get instance of VMR9 ");
        return false;
      }

      IntPtr hMonitor;
      AdapterInformation ai = MediaPortal.GUI.Library.GUIGraphicsContext.currentFullscreenAdapterInfo;
      hMonitor = Manager.GetAdapterMonitor(ai.Adapter);
      IntPtr upDevice = DirectShowUtil.GetUnmanagedDevice(GUIGraphicsContext.DX9Device);

      _scene = new PlaneScene(_renderFrame, this);
      _scene.Init();

      if (_useEvr)
        EvrInit(_scene, (uint)upDevice.ToInt32(), _vmr9Filter, (uint)hMonitor.ToInt32());
      //(uint)GUIGraphicsContext.ActiveForm);
      else
        Vmr9Init(_scene, (uint)upDevice.ToInt32(), _vmr9Filter, (uint)hMonitor.ToInt32());
      HResult hr = new HResult(graphBuilder.AddFilter(_vmr9Filter, "Video Mixing Renderer 9"));
      if (hr != 0)
      {
        if (_useEvr)
          EvrDeinit();
        else
          Vmr9Deinit();
        _scene.Stop();
        _scene.Deinit();
        _scene = null;

        Marshal.ReleaseComObject(_vmr9Filter);
        _vmr9Filter = null;
        Error.SetError("Unable to play movie", "Unable to initialize VMR9");
        Log.Error("vmr9:Failed to add vmr9 to filtergraph");
        return false;
      }

      _qualityInterface = _vmr9Filter as IQualProp;
      _vmr9MixerBitmapInterface = _vmr9Filter as IVMRMixerBitmap9;
      _graphBuilderInterface = graphBuilder;
      _instanceCounter++;
      _isVmr9Initialized = true;
      if (!_useEvr)
      {
        SetDeinterlacePrefs();
        System.OperatingSystem os = Environment.OSVersion;
        if (os.Platform == System.PlatformID.Win32NT)
        {
          long version = os.Version.Major * 10000000 + os.Version.Minor * 10000 + os.Version.Build;
          if (version >= 50012600) // we need at least win xp sp2 for VMR9 YUV mixing mode
          {
            IVMRMixerControl9 mixer = _vmr9Filter as IVMRMixerControl9;
            if (mixer != null)
            {

              VMR9MixerPrefs dwPrefs;
              mixer.GetMixingPrefs(out dwPrefs);
              dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;

              dwPrefs |= VMR9MixerPrefs.RenderTargetYUV; // YUV saves graphics bandwith  http://msdn2.microsoft.com/en-us/library/ms788177(VS.85).aspx
              hr.Set(mixer.SetMixingPrefs(dwPrefs));
              Log.Info("VMR9Helper: enabled YUV mixing - " + hr.ToDXString());

              using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
              {
                //Enable nonsquaremixing
                if (xmlreader.GetValueAsBool("general", "nonsquare", true))
                {
                  mixer.GetMixingPrefs(out dwPrefs);
                  dwPrefs |= VMR9MixerPrefs.NonSquareMixing;
                  hr.Set(mixer.SetMixingPrefs(dwPrefs));
                  Log.Info("VRM9Helper: Turning on nonsquare mixing - " + hr.ToDXString());
                  hr.Set(mixer.SetMixingPrefs(dwPrefs));
                }

                // Enable DecimateMask - this will effectively use only half of the input width & length
                if (xmlreader.GetValueAsBool("general", "dx9decimatemask", false))
                {                  
                  mixer.GetMixingPrefs(out dwPrefs);
                  dwPrefs &= ~VMR9MixerPrefs.DecimateMask;
                  dwPrefs |= VMR9MixerPrefs.DecimateOutput;
                  hr.Set(mixer.SetMixingPrefs(dwPrefs));
                  Log.Info("VRM9Helper: Enable decimatemask - " + hr.ToDXString());
                  hr.Set(mixer.SetMixingPrefs(dwPrefs));
                }
                
                // see  D3DTEXTUREFILTERTYPE Enumerated Type documents for further information
                // MixerPref9_PointFiltering
                // MixerPref9_BiLinearFiltering
                // MixerPref9_AnisotropicFiltering
                // MixerPref9_PyramidalQuadFiltering
                // MixerPref9_GaussianQuadFiltering

                mixer.SetMixingPrefs(dwPrefs);
                mixer.GetMixingPrefs(out dwPrefs);
                dwPrefs &= ~VMR9MixerPrefs.FilteringMask;
                string filtermode9 = xmlreader.GetValueAsString("general", "dx9filteringmode", "Gaussian Quad Filtering");
                if (filtermode9 == "Point Filtering")
                  dwPrefs |= VMR9MixerPrefs.PointFiltering;
                else if (filtermode9 == "Bilinear Filtering")
                  dwPrefs |= VMR9MixerPrefs.BiLinearFiltering;
                else if (filtermode9 == "Anisotropic Filtering")
                  dwPrefs |= VMR9MixerPrefs.AnisotropicFiltering;
                else if (filtermode9 == "Pyrimidal Quad Filtering")
                  dwPrefs |= VMR9MixerPrefs.PyramidalQuadFiltering;
                else
                  dwPrefs |= VMR9MixerPrefs.GaussianQuadFiltering;

                hr.Set(mixer.SetMixingPrefs(dwPrefs));
                Log.Info("VRM9Helper: Set filter mode - " + filtermode9 + " " + hr.ToDXString());
              }
            }
          }
        }
      }
      _threadId = Thread.CurrentThread.ManagedThreadId;
      GUIGraphicsContext.Vmr9Active = true;
      g_vmr9 = this;
      Log.Info("VMR9Helper:Vmr9 Added");
      return true;
    }

    /// <summary>
    /// repaints the last frame
    /// </summary>
    public void Repaint()
    {
      if (!_isVmr9Initialized) return;
      if (currentVmr9State == Vmr9PlayState.Playing) return;
      _scene.Repaint();
    }

    public void SetRepaint()
    {
      if (!_isVmr9Initialized) return;
      if (!GUIGraphicsContext.Vmr9Active) return;
      Log.Info("VMR9Helper: SetRepaint()");
      FrameCounter = 0;
      _repaintTimer = DateTime.Now;
      currentVmr9State = Vmr9PlayState.Repaint;
      _scene.DrawVideo = false;
    }
    public bool IsRepainting
    {
      get
      {
        return (currentVmr9State == Vmr9PlayState.Repaint);
      }
    }
    public void Process()
    {
      if (!_isVmr9Initialized)
      {
        return;
      }
      if (!GUIGraphicsContext.Vmr9Active)
      {
        return;
      }
      if (g_Player.Playing && g_Player.IsDVD && g_Player.IsDVDMenu)
      {
        GUIGraphicsContext.Vmr9FPS = 0f;
        currentVmr9State = Vmr9PlayState.Playing;
        _scene.DrawVideo = true;
        _repaintTimer = DateTime.Now;
        return;
      }

      TimeSpan ts = DateTime.Now - _repaintTimer;
      int frames = FrameCounter;
      if (ts.TotalMilliseconds >= 1000 || (currentVmr9State == Vmr9PlayState.Repaint && FrameCounter>0))
      {
        GUIGraphicsContext.Vmr9FPS = ((float)(frames * 1000)) / ((float)ts.TotalMilliseconds);
//         Log.Info("VMR9Helper:frames:{0} fps:{1} time:{2}", frames, GUIGraphicsContext.Vmr9FPS,ts.TotalMilliseconds);
        FrameCounter = 0;

        if (_threadId == Thread.CurrentThread.ManagedThreadId)
        {
            if (_qualityInterface != null)
            {
                VideoRendererStatistics.Update(_qualityInterface);
            }
            else
                Log.Info("_qualityInterface is null!");
        }
        _repaintTimer = DateTime.Now;
      }

      if (currentVmr9State == Vmr9PlayState.Repaint && frames > 0)
      {
        Log.Info("VMR9Helper: repaint->playing {0}", frames);
        GUIGraphicsContext.Vmr9FPS = 50f;
        currentVmr9State = Vmr9PlayState.Playing;
        _scene.DrawVideo = true;
        _repaintTimer = DateTime.Now;
      }
      else if (currentVmr9State == Vmr9PlayState.Playing && GUIGraphicsContext.Vmr9FPS < 5f)
      {
        Log.Info("VMR9Helper: playing->repaint {0}", frames);
        GUIGraphicsContext.Vmr9FPS = 0f;
        currentVmr9State = Vmr9PlayState.Repaint;
        _scene.DrawVideo = false;
      }
    }
    /// <summary>
    /// returns a IVMRMixerBitmap9 interface
    /// </summary>
    public IVMRMixerBitmap9 MixerBitmapInterface
    {
      get { return _vmr9MixerBitmapInterface; }
    }

    public void SetDeinterlacePrefs()
    {
      if (!_isVmr9Initialized) return;
      int DeInterlaceMode = 3;
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        //None
        //Bob
        //Weave
        //Best
        DeInterlaceMode = xmlreader.GetValueAsInt("mytv", "deinterlace", 3);
      }
      Vmr9SetDeinterlacePrefs((uint)DeInterlaceMode);
    }
    public void SetDeinterlaceMode()
    {
      if (!GUIGraphicsContext.IsEvr)
      {
        if (!_isVmr9Initialized) return;
        int DeInterlaceMode = 3;
        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
        {
          //None
          //Bob
          //Weave
          //Best
          DeInterlaceMode = xmlreader.GetValueAsInt("mytv", "deinterlace", 3);
        }
        Vmr9SetDeinterlaceMode((short)DeInterlaceMode);
      }
    }
    public void Enable(bool onOff)
    {
      //Log.Info("Vmr9:Enable:{0}", onOff);
      if (!_isVmr9Initialized) return;
      if (_scene != null) _scene.Enabled = onOff;
      if (onOff)
      {
        _repaintTimer = DateTime.Now;
        FrameCounter = 50;
      }
    }

    public bool Enabled
    {
      get
      {
        if (!_isVmr9Initialized) return true;
        if (_scene == null) return true;
        return _scene.Enabled;
      }
    }

    public bool SaveBitmap(System.Drawing.Bitmap bitmap, bool show, bool transparent, float alphaValue)
    {
      if (!_isVmr9Initialized) return false;
      if (_vmr9Filter == null)
        return false;

      if (MixerBitmapInterface == null)
        return false;

      if (GUIGraphicsContext.Vmr9Active == false)
      {
        Log.Info("SaveVMR9Bitmap() failed, no VMR9");
        return false;
      }
      int hr = 0;
      // transparent image?
      using (System.IO.MemoryStream mStr = new System.IO.MemoryStream())
      {
        if (bitmap != null)
        {
          if (transparent == true)
            bitmap.MakeTransparent(Color.Black);
          bitmap.Save(mStr, System.Drawing.Imaging.ImageFormat.Bmp);
          mStr.Position = 0;
        }

        VMR9AlphaBitmap bmp = new VMR9AlphaBitmap();

        if (show == true)
        {

          // get AR for the bitmap
          Rectangle src, dest;
          VMR9Util.g_vmr9.GetVideoWindows(out src, out dest);

          int width = VMR9Util.g_vmr9.VideoWidth;
          int height = VMR9Util.g_vmr9.VideoHeight;

          float xx = (float)src.X / width;
          float yy = (float)src.Y / height;
          float fx = (float)(src.X + src.Width) / width;
          float fy = (float)(src.Y + src.Height) / height;
          //

          using (Microsoft.DirectX.Direct3D.Surface surface = GUIGraphicsContext.DX9Device.CreateOffscreenPlainSurface(GUIGraphicsContext.Width, GUIGraphicsContext.Height, Microsoft.DirectX.Direct3D.Format.X8R8G8B8, Microsoft.DirectX.Direct3D.Pool.SystemMemory))
          {
            Microsoft.DirectX.Direct3D.SurfaceLoader.FromStream(surface, mStr, Microsoft.DirectX.Direct3D.Filter.None, 0);
            bmp.dwFlags = (VMR9AlphaBitmapFlags)(4 | 8);
            bmp.clrSrcKey = 0;
            unsafe
            {
              bmp.pDDS = (System.IntPtr)surface.UnmanagedComPointer;
            }
            bmp.rDest = new NormalizedRect();
            bmp.rDest.top = yy;
            bmp.rDest.left = xx;
            bmp.rDest.bottom = fy;
            bmp.rDest.right = fx;
            bmp.fAlpha = alphaValue;
            //Log.Info("SaveVMR9Bitmap() called");
            hr = VMR9Util.g_vmr9.MixerBitmapInterface.SetAlphaBitmap(ref bmp);
            if (hr != 0)
            {
              //Log.Info("SaveVMR9Bitmap() failed: error {0:X} on SetAlphaBitmap()",hr);
              return false;
            }
          }
        }
        else
        {
          bmp.dwFlags = (VMR9AlphaBitmapFlags)1;
          bmp.clrSrcKey = 0;
          bmp.rDest = new NormalizedRect();
          bmp.rDest.top = 0.0f;
          bmp.rDest.left = 0.0f;
          bmp.rDest.bottom = 1.0f;
          bmp.rDest.right = 1.0f;
          bmp.fAlpha = alphaValue;
          hr = VMR9Util.g_vmr9.MixerBitmapInterface.UpdateAlphaBitmapParameters(ref bmp);
          if (hr != 0)
          {
            return false;
          }
        }
      }
      // dispose
      return true;
    }// savevmr9bitmap

    public void GetVideoWindows(out System.Drawing.Rectangle rSource, out System.Drawing.Rectangle rDest)
    {
      MediaPortal.GUI.Library.Geometry m_geometry = new MediaPortal.GUI.Library.Geometry();
      // get the window where the video/tv should be shown
      float x = GUIGraphicsContext.VideoWindow.X;
      float y = GUIGraphicsContext.VideoWindow.Y;
      float nw = GUIGraphicsContext.VideoWindow.Width;
      float nh = GUIGraphicsContext.VideoWindow.Height;

      //sanity checks
      if (nw > GUIGraphicsContext.OverScanWidth)
        nw = GUIGraphicsContext.OverScanWidth;
      if (nh > GUIGraphicsContext.OverScanHeight)
        nh = GUIGraphicsContext.OverScanHeight;

      //are we supposed to show video in fullscreen or in a preview window?
      if (GUIGraphicsContext.IsFullScreenVideo || !GUIGraphicsContext.ShowBackground)
      {
        //yes fullscreen, then use the entire screen
        x = GUIGraphicsContext.OverScanLeft;
        y = GUIGraphicsContext.OverScanTop;
        nw = GUIGraphicsContext.OverScanWidth;
        nh = GUIGraphicsContext.OverScanHeight;
      }

      //calculate the video window according to the current aspect ratio settings
      float fVideoWidth = (float)VideoWidth;
      float fVideoHeight = (float)VideoHeight;
      m_geometry.ImageWidth = (int)fVideoWidth;
      m_geometry.ImageHeight = (int)fVideoHeight;
      m_geometry.ScreenWidth = (int)nw;
      m_geometry.ScreenHeight = (int)nh;
      m_geometry.ARType = GUIGraphicsContext.ARType;
      m_geometry.PixelRatio = GUIGraphicsContext.PixelRatio;
      m_geometry.GetWindow(VideoAspectRatioX, VideoAspectRatioY, out rSource, out rDest);
      rDest.X += (int)x;
      rDest.Y += (int)y;
      m_geometry = null;
    }
    #endregion


    #region IDisposeable
    /// <summary>
    /// removes the vmr9 filter from the graph and free up all unmanaged resources
    /// </summary>
    public void Dispose()
    {
      Log.Info("vmr9:Dispose");
      if (false == _isVmr9Initialized) return;
      if (_threadId != Thread.CurrentThread.ManagedThreadId)
      {
        Log.Error("VMR9:Dispose() from wrong thread");
        return;
      }
      if (_vmr9Filter == null)
      {
        Log.Error("VMR9:Dispose() no filter");
        return;
      }

      if (_scene != null)
      {
        //Log.Info("VMR9Helper:stop planescene");
        _scene.Stop();
        _instanceCounter--;
        _scene.Deinit();
        GUIGraphicsContext.Vmr9Active = false;
        GUIGraphicsContext.Vmr9FPS = 0f;
        GUIGraphicsContext.InVmr9Render = false;
        currentVmr9State = Vmr9PlayState.Playing;
      }
      int result;

      //if (_vmr9MixerBitmapInterface!= null)
      //	while ( (result=Marshal.ReleaseComObject(_vmr9MixerBitmapInterface))>0); 
      //result=Marshal.ReleaseComObject(_vmr9MixerBitmapInterface);
      _vmr9MixerBitmapInterface = null;

      //				if (_qualityInterface != null)
      //					while ( (result=Marshal.ReleaseComObject(_qualityInterface))>0); 
      //Marshal.ReleaseComObject(_qualityInterface);
      _qualityInterface = null;

      try
      {
        result = _graphBuilderInterface.RemoveFilter(_vmr9Filter);
        if (result != 0) Log.Info("VMR9:RemoveFilter():{0}", result);
      }
      catch (Exception)
      {
      }

      try
      {
        do
        {
          result = Marshal.ReleaseComObject(_vmr9Filter);
          Log.Info("VMR9:ReleaseComObject():{0}", result);
        }
        while (result > 0);
      }
      catch (Exception)
      {
      }
      if (GUIGraphicsContext.IsEvr)
          EvrDeinit();
      else
          Vmr9Deinit();
      _vmr9Filter = null;
      _graphBuilderInterface = null;
      _scene = null;
      g_vmr9 = null;
    }
    #endregion

  }//public class VMR9Util
}//namespace MediaPortal.Player 
