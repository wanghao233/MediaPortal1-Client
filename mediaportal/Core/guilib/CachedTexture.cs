#define USE_NEW_TEXTURE_ENGINE
using System;
using System.Drawing;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using System.Runtime.InteropServices;

namespace MediaPortal.GUI.Library
{
  /// <summary>
  /// A datastructure for caching textures.
  /// This is used by the GUITextureManager which keeps a cache of all textures in use
  /// </summary>
  public class CachedTexture 
  {
#if USE_NEW_TEXTURE_ENGINE
		[DllImport("fontEngine.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
		unsafe private static extern void FontEngineRemoveTexture(int textureNo);

		[DllImport("fontEngine.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
		unsafe private static extern int  FontEngineAddTexture(int hasCode,void* fontTexture);
		
		[DllImport("fontEngine.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
		unsafe private static extern void FontEngineDrawTexture(int textureNo,float x, float y, float nw, float nh, float uoff, float voff, float umax, float vmax, int color);

		[DllImport("fontEngine.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
		unsafe private static extern void FontEnginePresentTextures();
#endif

		/// <summary>
		/// Class which contains a single frame
		/// A cached texture can contain more then 1 frames for example when its an animated gif
		/// </summary>
    public class Frame 
    {
      Texture _Image;			//texture of current frame
      int     _Duration;	//duration of current frame
#if USE_NEW_TEXTURE_ENGINE
			int     _TextureNo;
			public readonly bool    UseNewTextureEngine=true;
#else
			public readonly bool    UseNewTextureEngine=false;
#endif      
      public Frame(Texture image, int duration)
      {
        _Image = image;
        _Duration = duration;
#if USE_NEW_TEXTURE_ENGINE
				if (image!=null)
				{
					unsafe
					{
						IntPtr ptr=DShowNET.DsUtils.GetUnmanagedTexture(_Image);
						_TextureNo=FontEngineAddTexture(ptr.ToInt32(),(void*) ptr.ToPointer());
					}
				}
#endif
      }

			/// <summary>
			/// Property to get/set the texture
			/// </summary>
      public Texture Image
      {
        get { return _Image;}
        set {
          if (_Image!=null) 
          {
#if USE_NEW_TEXTURE_ENGINE
						FontEngineRemoveTexture(_TextureNo);
#endif
            if (!_Image.Disposed) 
              _Image.Dispose();
          }
          _Image=value;
					
#if USE_NEW_TEXTURE_ENGINE
					if (_Image!=null)
					{
						unsafe
						{
							IntPtr ptr=DShowNET.DsUtils.GetUnmanagedTexture(_Image);
							_TextureNo=FontEngineAddTexture(ptr.ToInt32(),(void*) ptr.ToPointer());
						}
					}
#endif
        }
      }

			/// <summary>
			/// property to get/set the duration for this frame
			/// (only usefull if the frame belongs to an animation, like an animated gif)
			/// </summary>
      public int Duration
      {
        get { return _Duration;}
        set { _Duration=value;}
      }
      #region IDisposable Members

      public void Dispose()
      {
        if (_Image!=null)
        {
#if USE_NEW_TEXTURE_ENGINE
					FontEngineRemoveTexture(_TextureNo);
#endif
					if (!_Image.Disposed)
          {
            _Image.Dispose();
          }
          _Image=null;
        }
      }

      #endregion

			public void Draw(float x, float y, float nw, float nh, float uoff, float voff, float umax, float vmax, int color)
			{
#if USE_NEW_TEXTURE_ENGINE
				FontEngineDrawTexture(_TextureNo,x, y, nw, nh, uoff, voff, umax, vmax, color);
#endif
			}
    }

    string    m_strName="";								// filename of the texture
    ArrayList m_Frames=new ArrayList();	  // array to hold all frames
    int       m_iWidth=0;									// width of the texture
    int       m_iHeight=0;								// height of the texture
    int       m_iFrames=0;								// number of frames in the animation
    Image     m_Image=null;								// GDI image of the texture

		/// <summary>
		/// The (emtpy) constructor of the CachedTexture class.
		/// </summary>
    public CachedTexture()
    {
    }


		/// <summary>
		/// Get/set the filename/location of the texture.
		/// </summary>
    public string Name
    {
      get { return m_strName;}
      set { m_strName=value;}
    }

		/// <summary>
		/// Get/set the DirectX texture for the 1st frame
		/// </summary>
    public Frame texture
    {
      get 
			{ 
				if (m_Frames.Count==0) return null;
				return (Frame )m_Frames[0];
			}
      set 
			{ 
          Dispose();      // cleanup..
          m_Frames.Clear();
          m_Frames.Add(value);
      }
    }

		/// <summary>
		/// Get/set the GDI Image 
		/// </summary>
    public Image image
    {
      get { return m_Image;}
      set 
      {
        if (m_Image!=null)
        {
          m_Image.Dispose();
        }
        m_Image=value;
      }
    }

		/// <summary>
		/// Get/set the width of the texture.
		/// </summary>
    public int Width
    {
      get { return m_iWidth;}
      set { m_iWidth=value;}
    }

		/// <summary>
		/// Get/set the height of the texture.
		/// </summary>
    public int Height
    {
      get { return m_iHeight;}
      set { m_iHeight=value;}
    }

		/// <summary>
		/// Get/set the number of frames out of which the texture exsists.
		/// </summary>
    public int Frames
    {
      get { return m_iFrames;}
      set { m_iFrames=value;}
    }

		/// <summary>
		/// indexer to get a Frame or to set a Frame
		/// </summary>
    public Frame this [int index]
    {
      get 
      {
				if (index <0 || index >= m_Frames.Count) return null;
        return (Frame)m_Frames[index];
      }
      set 
      {
				if (index <0) return;

        if (m_Frames.Count <= index)
          m_Frames.Add(value);
        else
        {
          Frame frame=(Frame)m_Frames[index];
          if (frame!=value)
          {
            frame.Dispose();
            m_Frames[index]=value;
          }
        }
      }
    }
    #region IDisposable Members
		/// <summary>
		/// Releases the resources used by the texture.
		/// </summary>
    public void Dispose()
    {

      foreach (Frame tex in m_Frames)
      {
        tex.Dispose();
      }
      m_Frames.Clear();
      if (m_Image!=null)
      {
        m_Image.Dispose();
        m_Image=null;
      }
    }
    #endregion
  }
}
