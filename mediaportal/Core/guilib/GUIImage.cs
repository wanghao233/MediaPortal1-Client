using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;


namespace MediaPortal.GUI.Library
{
	/// <summary>
	/// A GUIControl for displaying Images.
	/// </summary>
	public class GUIImage :  GUIControl
	{

		[XMLSkinElement("colorkey")] private long                    m_dwColorKey=0;
		//private VertexBuffer						m_vbBuffer=null;
		[XMLSkinElement("texture")] private string m_strFileName="";
		/// <summary>The width of the current texture.</summary>
		private int                     m_iTextureWidth=0;
		private int                     m_iTextureHeight=0;
		/// <summary>The width of the image containing the textures.</summary>
		private int                     m_iImageWidth=0;
		private int                     m_iImageHeight=0;
		private int                     m_iBitmap=0;
		private int                     m_dwItems=0;
		private int                     m_iCurrentLoop=0;
		private int										  m_iCurrentImage=0;
		[XMLSkinElement("keepaspectratio")] private bool    m_bKeepAspectRatio=false;
    [XMLSkinElement("zoom")] private bool    m_bZoom=false;
    [XMLSkinElement("zoomfromtop")] private bool    m_bZoomFromTop=false;
    [XMLSkinElement("fixedheight")] private bool    m_bFixedHeight=false;    
		private CachedTexture.Frame[]  m_vecTextures = null;

    //TODO GIF PALLETTE
    //private PaletteEntry						m_pPalette=null;
		/// <summary>The width of in which the texture will be rendered after scaling texture.</summary>
		private int                     m_iRenderWidth=0;
		private int                     m_iRenderHeight=0;
		private bool										m_bWasVisible=false;
    private System.Drawing.Image    m_image=null;
    private Rectangle               m_destRect;
    string                          m_strTextureFileName="";
    int                             g_nAnisotropy=0;
	  [XMLSkinElement("filtered")] bool	m_bFiltering=true;
    [XMLSkinElement("centered")] bool m_bCentered=false;
    string                          m_strTxt;
    DateTime                        m_AnimationTime=DateTime.MinValue;
    bool                            ContainsProperty=false;
//    StateBlock                      savedStateBlock;
		Rectangle                       sourceRect;
    Rectangle                       destinationRect;
    Vector3                         pntPosition;
    float                           scaleX=1;
    float                           scaleY=1;
		float _fx,_fy,_nw,_nh,_uoff,_voff,_umax,_vmax;
		int   _color;
		public GUIImage (int dwParentID) : base(dwParentID)
		{
		}
		/// <summary>
		/// The constructor of the GUIImage class.
		/// </summary>
		/// <param name="dwParentID">The parent of this GUIImage control.</param>
		/// <param name="dwControlId">The ID of this GUIImage control.</param>
		/// <param name="dwPosX">The X position of this GUIImage control.</param>
		/// <param name="dwPosY">The Y position of this GUIImage control.</param>
		/// <param name="dwWidth">The width of this GUIImage control.</param>
		/// <param name="dwHeight">The height of this GUIImage control.</param>
		/// <param name="strTexture">The filename of the texture of this GUIImage control.</param>
		/// <param name="dwColorKey">The color that indicates transparancy.</param>
		public GUIImage(int dwParentID, int dwControlId, int dwPosX, int dwPosY, int dwWidth, int dwHeight, string strTexture,long dwColorKey)
			:base(dwParentID, dwControlId,dwPosX, dwPosY, dwWidth, dwHeight)
		{		
			m_colDiffuse	= 0xFFFFFFFF;  
			m_strFileName=strTexture;
			m_iTextureWidth=0;
			m_iTextureHeight=0;
			m_dwColorKey=dwColorKey;
			m_iBitmap=0;
			
			m_iCurrentImage=0;
			m_bKeepAspectRatio=false;
      m_bZoom=false;
			m_iCurrentLoop=0;
			m_iImageWidth = 0;
			m_iImageHeight = 0;
			FinalizeConstruction();
			
    }
    /// <summary>
    /// Does any scaling on the inital size\position values to fit them to screen 
    /// resolution. 
    /// </summary>
		public override void ScaleToScreenResolution()
		{
      if (m_strFileName==null) m_strFileName=String.Empty;
			if (m_strFileName != "-" && m_strFileName != "")
			{
				if (m_dwWidth == 0 || m_dwHeight == 0)
				{		
					try
					{
						string strFileNameTemp="";
						if (!System.IO.File.Exists(m_strFileName))
						{
							if (m_strFileName[1] != ':')
								strFileNameTemp = GUIGraphicsContext.Skin + @"\media\" + m_strFileName;
						}
						using (Image img = Image.FromFile(strFileNameTemp))
						{
							if (0 == m_dwWidth)  m_dwWidth = img.Width;
							if (0 == m_dwHeight) m_dwHeight = img.Height;
						}
					}
					catch (Exception)
					{
					}
				}
			}
			base.ScaleToScreenResolution();
    }
    /// <summary> 
    /// This function is called after all of the XmlSkinnable fields have been filled
    /// with appropriate data.
    /// Use this to do any construction work other than simple data member assignments,
    /// for example, initializing new reference types, extra calculations, etc..
    /// </summary>
		public override void FinalizeConstruction()
		{
			base.FinalizeConstruction ();
	
			m_dwItems=1;
			m_bWasVisible = IsVisible;
			
			m_iRenderWidth=m_dwWidth;
			m_iRenderHeight=m_dwHeight;
      if (m_strFileName.IndexOf("#")>=0) ContainsProperty=true;
		}
	
		/// <summary>
		/// Get/Set the TextureWidth
		/// </summary>
		public int TextureWidth
		{ 
			get { return m_iTextureWidth;}
			set {
        if (m_iTextureWidth<0) return;
        m_iTextureWidth=value;Update();
      }
		}

		/// <summary>
		/// Get/Set the TextureHeight
		/// </summary>
		public int TextureHeight
		{
			get { return m_iTextureHeight;}
			set { 
        if (m_iTextureHeight<0) return;
        m_iTextureHeight=value;Update();
      }
		}

		/// <summary>
		/// Get the filename of the texture.
		/// </summary>
		public string FileName
		{
			get {return m_strFileName;}
		}
		
		/// <summary>
		/// Get the transparent color.
		/// </summary>
		public long	ColorKey 
		{
			get {return m_dwColorKey;}
		}

		/// <summary>
		/// Get/Set if the aspectratio of the texture needs to be preserved during rendering.
		/// </summary>
		public bool KeepAspectRatio
		{
			get { return m_bKeepAspectRatio;}
			set { m_bKeepAspectRatio=value;}
		}

		/// <summary>
		/// Get the width in which the control is rendered.
		/// </summary>
		public int RenderWidth
		{
			get { return m_iRenderWidth;}
		}

		/// <summary>
		/// Get the height in which the control is rendered.
		/// </summary>
		public int RenderHeight
		{
			get { return m_iRenderHeight;}
		}

		/// <summary>
		/// Returns if the control can have the focus.
		/// </summary>
		/// <returns>False</returns>
		public override bool CanFocus() 
		{
			return false;
		}
		
		/// <summary>
		/// If the texture holds more then 1 frame (like an animated gif)
		/// then you can select the current frame with this method
		/// </summary>
		/// <param name="iBitmap"></param>
		public void Select(int iBitmap)
		{
			if (m_iBitmap==iBitmap) return;
			m_iBitmap=iBitmap;
			Update();
		}
		
		/// <summary>
		/// If the texture has more then 1 frame like an animated gif then
		/// you can specify the max# of frames to play with this method
		/// </summary>
		/// <param name="iItems"></param>
		public void SetItems(int iItems)
		{
			m_dwItems=iItems;
		}


		/// <summary>
		/// This function will do the animation (when texture is an animated gif)
		/// by switching from frame 1->frame2->frame 3->...
		/// </summary>
		protected void Process()
		{
			if (m_vecTextures==null) return;
			// If the number of textures that correspond to this control is lower than or equal to 1 do not change the texture.
			if (m_vecTextures.Length <= 1)
				return;
			
			// If the GUIImage has not been visible before start at the first texture in the m_vecTextures.
			if (!m_bWasVisible)
			{
				m_iCurrentLoop = 0;
				m_iCurrentImage = 0;
				m_bWasVisible = true;
				return;
			}
			
      if (m_iCurrentImage >= m_vecTextures.Length) 
        m_iCurrentImage =0;

      CachedTexture.Frame frame=m_vecTextures[m_iCurrentImage];
      string strFile=m_strFileName;
      if (ContainsProperty)
        strFile=GUIPropertyManager.Parse(m_strFileName);
			
      // Check the delay.
			int dwDelay    = frame.Duration;
      int iMaxLoops=0;

			// Default delay = 100;
			if (0==dwDelay) dwDelay=100;
			
      TimeSpan ts = DateTime.Now-m_AnimationTime;
      if (ts.TotalMilliseconds> dwDelay)
			{
        m_AnimationTime=DateTime.Now;

        // Reset the current image
				if (m_iCurrentImage+1 >= m_vecTextures.Length)
				{
					// Check if another loop is required
					if (iMaxLoops > 0)
					{
						// Go to the next loop
						if (m_iCurrentLoop+1 < iMaxLoops)
						{
							m_iCurrentLoop++;
							m_iCurrentImage=0;
						}
					}
					else
					{
						// 0 == loop forever
						m_iCurrentImage=0;
					}
				}
				// Switch to the next image.
				else
				{
					m_iCurrentImage++;
				}
			}
		}
		
		/// <summary>
		/// Allocate the DirectX resources needed for rendering this GUIImage.
		/// </summary>
		public override void AllocResources()
		{
			//imageSprite = new Sprite(GUIGraphicsContext.DX9Device);

      g_nAnisotropy=GUIGraphicsContext.DX9Device.DeviceCaps.MaxAnisotropy;
			if (m_strFileName=="-") return;

      //reset animation
			m_iCurrentImage=0;
			m_iCurrentLoop=0;

      //get the filename of the texture
      string strFile=m_strFileName;
      if (ContainsProperty)
        strFile=GUIPropertyManager.Parse(m_strFileName);

      //load the texture
			int iImages = GUITextureManager.Load(strFile, m_dwColorKey,m_iRenderWidth,m_iTextureHeight);
			if (0==iImages) return;// unable to load texture

      //get each frame of the texture
			m_vecTextures = new CachedTexture.Frame [iImages];
			for (int i=0; i < iImages; i++)
			{
				m_vecTextures[i]=GUITextureManager.GetTexture(strFile,i, out m_iTextureWidth,out m_iTextureHeight);//,m_pPalette);
			}

  		
      // Create a vertex buffer for rendering the image
/*
 *		m_vbBuffer = new VertexBuffer(typeof(CustomVertex.TransformedColoredTextured),
                                    4, GUIGraphicsContext.DX9Device, 
                                    Usage.WriteOnly, CustomVertex.TransformedColoredTextured.Format, 
                                    Pool.Managed);
*/
			// Set state to render the image
      Update();

      //create a directx9 stateblock
      //CreateStateBlock();
		}

		/// <summary>
		/// Free the DirectX resources needed for rendering this GUIImage.
		/// </summary>
		public override void FreeResources()
		{
			lock (this)
			{
				//if (imageSprite!=null)
				//{
				//	imageSprite .Dispose();
				//	imageSprite =null;
				//}
				if (m_strFileName!=null && m_strFileName!=String.Empty)
				{
					if (GUITextureManager.IsTemporary(m_strFileName))
					{
						GUITextureManager.ReleaseTexture(m_strFileName);
					}
				}
				Cleanup();
			}
		}
		void Cleanup()
		{
        m_strTextureFileName="";
        /*if (m_vbBuffer!=null)
        {
          if (!m_vbBuffer.Disposed) m_vbBuffer.Dispose();
          m_vbBuffer=null;
        }*/

        m_image=null;
				
        m_vecTextures=null;
        m_iCurrentImage=0;
        m_iCurrentLoop=0;
        m_iImageWidth=0;
        m_iImageHeight=0;
        m_iTextureWidth=0;
        m_iTextureHeight=0;
        //if (savedStateBlock!=null) savedStateBlock.Dispose();
        //savedStateBlock=null;
		}

		/// <summary>
		/// Sets the state to render the image
		/// </summary>
    protected override void Update()
    {
      //if (m_vbBuffer==null) return;
      if (m_vecTextures==null) return;
      float x=(float)m_dwPosX;
      float y=(float)m_dwPosY;

      CachedTexture.Frame frame=m_vecTextures[m_iCurrentImage];
      Direct3D.Texture texture=frame.Image;
      if (texture==null)
      {
        //no texture? then nothing todo
        return;
      }

      // if texture is disposed then free its resources and return
      if (texture.Disposed)
      {
        FreeResources();
        return;
      }

      // on first run, get the image width/height of the texture
      if (0==m_iImageWidth|| 0==m_iImageHeight)
      {
        Direct3D.SurfaceDescription desc;
        desc=texture.GetLevelDescription(0);
        m_iImageWidth = desc.Width;
        m_iImageHeight = desc.Height;
      }

      // Calculate the m_iTextureWidth and m_iTextureHeight 
      // based on the m_iImageWidth and m_iImageHeight
      if (0==m_iTextureWidth|| 0==m_iTextureHeight)
      {
        m_iTextureWidth  = (int)Math.Round( ((float)m_iImageWidth) / ((float)m_dwItems) );
        m_iTextureHeight = m_iImageHeight;

        if (m_iTextureHeight > (int)GUIGraphicsContext.Height )
          m_iTextureHeight = (int)GUIGraphicsContext.Height;

        if (m_iTextureWidth > (int)GUIGraphicsContext.Width )
          m_iTextureWidth = (int)GUIGraphicsContext.Width;
      }
			
      // If there are multiple frames in the GUIImage thne the e m_iTextureWidth is equal to the m_dwWidth
      if (m_dwWidth >0 && m_dwItems>1)
      {
        m_iTextureWidth=(int)m_dwWidth;
      }

      // Initialize the with of the control based on the texture width
      if (m_dwWidth==0) 
        m_dwWidth=m_iTextureWidth;

      // Initialize the height of the control based on the texture height
      if (m_dwHeight==0) 
        m_dwHeight=m_iTextureHeight;


      float nw =(float)m_dwWidth;
      float nh =(float)m_dwHeight;

      //adjust image based on current aspect ratio setting
			float fSourceFrameRatio = 1;
			float fOutputFrameRatio = 1;
      if (!m_bZoom && !m_bZoomFromTop && m_bKeepAspectRatio && m_iTextureWidth!=0 && m_iTextureHeight!=0)
      {
        // TODO: remove or complete HDTV_1080i code
        //int iResolution=g_stSettings.m_ScreenResolution;
        fSourceFrameRatio = ((float)m_iTextureWidth) / ((float)m_iTextureHeight);       
        fOutputFrameRatio = fSourceFrameRatio / GUIGraphicsContext.PixelRatio; 
        //if (iResolution == HDTV_1080i) fOutputFrameRatio *= 2;

        // maximize the thumbnails width
        float fNewWidth  = (float)m_dwWidth;
        float fNewHeight = fNewWidth/fOutputFrameRatio;

        // make sure the height is not larger than the maximum
        if (fNewHeight > m_dwHeight)
        {
          fNewHeight = (float)m_dwHeight;
          fNewWidth = fNewHeight*fOutputFrameRatio;
        }
        // this shouldnt happen, but just make sure that everything still fits onscreen
        if (fNewWidth > m_dwWidth || fNewHeight > m_dwHeight)
        {
          fNewWidth=(float)m_dwWidth;
          fNewHeight=(float)m_dwHeight;
        }
        nw=fNewWidth;
        nh=fNewHeight;
      }
			
      // set the width/height the image gets rendererd
      m_iRenderWidth=(int)Math.Round(nw);
      m_iRenderHeight=(int)Math.Round(nh);

      // reposition if calibration of the UI has been done
      if (CalibrationEnabled)
      {
        GUIGraphicsContext.Correct(ref x,ref y);
      }

      // if necessary then center the image 
      // in the controls rectangle
      if (m_bCentered)
      {
        x += ((((float)m_dwWidth)-nw)/2.0f);
        y += ((((float)m_dwHeight)-nh)/2.0f); 
      }


      // Calculate source Texture
      int iSourceX = 0;
      int iSourceY = 0;
      int iSourceWidth = m_iTextureWidth;
      int iSourceHeight = m_iTextureHeight;

      if ((m_bZoom || m_bZoomFromTop) && m_bKeepAspectRatio)
      {
        fSourceFrameRatio = ((float)nw) / ((float)nh);       
        fOutputFrameRatio = fSourceFrameRatio * GUIGraphicsContext.PixelRatio; 

        if (((float)iSourceWidth/(nw*GUIGraphicsContext.PixelRatio)) < ((float)iSourceHeight/nh))
        {
          //Calc height
          iSourceHeight = (int)((float)iSourceWidth/fOutputFrameRatio);          
          if (iSourceHeight > m_iTextureHeight)
          {
            iSourceHeight = m_iTextureHeight;
            iSourceWidth = (int)((float)iSourceHeight*fOutputFrameRatio);
          }          
        }
        else
        {
          //Calc width
          iSourceWidth = (int)((float)iSourceHeight*fOutputFrameRatio);         
          if (iSourceWidth > m_iTextureWidth)
          {
            iSourceWidth = m_iTextureWidth;
            iSourceHeight = (int)((float)iSourceWidth/fOutputFrameRatio);          
          }
        }

        if (!m_bZoomFromTop) iSourceY = (m_iTextureHeight-iSourceHeight)/2;         
        iSourceX = (m_iTextureWidth-iSourceWidth)/2;
      }

      if (m_bFixedHeight)
      {
        y=(float)m_dwPosY;
        nh =(float)m_dwHeight;
      }

      // copy all coordinates to the vertex buffer
			// x-offset in texture
      float uoffs = ((float)(m_iBitmap * m_dwWidth + iSourceX)) / ((float)m_iImageWidth);

			// y-offset in texture
			float voffs = ((float)iSourceY) / ((float)m_iImageHeight);

			// width copied from texture
      float u = ((float)iSourceWidth)  / ((float)m_iImageWidth);

			// height copied from texture
      float v = ((float)iSourceHeight) / ((float)m_iImageHeight);

			
			if (uoffs<0 || uoffs >1) uoffs=0;
			if (u<0 || u >1) u=1;
			if (v<0 || v >1) v=1;
			if (u+uoffs > 1)
			{
				uoffs=0;
				u=1;
			}
			if (x <0) x=0;
			if (x > GUIGraphicsContext.Width) x=GUIGraphicsContext.Width;
			if (y <0) y=0;
			if (y > GUIGraphicsContext.Height) y=GUIGraphicsContext.Height;
			if (nw <0) nw=0;
			if (nh <0) nh=0;
			if (x+nw >GUIGraphicsContext.Width) 
			{
				nw=GUIGraphicsContext.Width-x;
			}
			if (y+nh >GUIGraphicsContext.Height) 
			{
				nh=GUIGraphicsContext.Height-y;
			}

			_fx=x;
			_fy=y;
			_nw=nw;
			_nh=nh;
			_uoff=uoffs;
			_voff=voffs;
			_umax=u;
			_vmax=v;
			_color=(int)m_colDiffuse;
			/*
      CustomVertex.TransformedColoredTextured[] verts = (CustomVertex.TransformedColoredTextured[])m_vbBuffer.Lock(0,LockFlags.Discard);
      verts[0].X= x- 0.5f; verts[0].Y=y+nh- 0.5f; verts[0].Z= 0.0f; verts[0].Rhw=1.0f ;
      verts[0].Color = (int)m_colDiffuse;
      verts[0].Tu = uoffs;
      verts[0].Tv = voffs+v;

      verts[1].X= x- 0.5f; verts[1].Y= y- 0.5f; verts[1].Z= 0.0f; verts[1].Rhw= 1.0f;
      verts[1].Color = (int)m_colDiffuse;
      verts[1].Tu = uoffs;
      verts[1].Tv = voffs;

      verts[2].X=  x+nw- 0.5f; verts[2].Y=y+nh- 0.5f;verts[1].Z=  0.0f; verts[2].Rhw= 1.0f;
      verts[2].Color = (int)m_colDiffuse;
      verts[2].Tu = uoffs+u;
      verts[2].Tv = voffs+v;

      verts[3].X= x+nw- 0.5f; verts[3].Y= y- 0.5f; verts[3].Z=0.0f; verts[3].Rhw=1.0f ;
      verts[3].Color = (int)m_colDiffuse;
      verts[3].Tu = uoffs+u;
      verts[3].Tv = voffs;
      m_vbBuffer.Unlock();
*/       

      pntPosition=new Vector3(x,y,0);
      sourceRect=new Rectangle(m_iBitmap * m_dwWidth + iSourceX, iSourceY, iSourceWidth, iSourceHeight);
      destinationRect=new Rectangle(0,0,(int)nw,(int)nh);
      m_destRect=new Rectangle((int)x,(int)y,(int)nw,(int)nh);

      scaleX = (float)destinationRect.Width / (float)iSourceWidth;
      scaleY = (float)destinationRect.Height / (float)iSourceHeight;
      pntPosition.X /= scaleX;
      pntPosition.Y /= scaleY;
		}

		/// <summary>
		/// Check 
		///  -IsVisible
		///  -Filename
		///  -Filename changed cause it contains a property
		///  -m_vecTextures
		///  -m_vbBuffer
		///  -GUIGraphicsContext.DX9Device
		/// </summary>
		/// <returns></returns>
    public bool PreRender()
    {
      // Do not render if not visible
      if (false==m_bVisible)
      {
        m_bWasVisible = false;
        return false;
      }

      // if filename contains a property, then get the value of the property
      if (ContainsProperty)
      {
        m_strTxt=GUIPropertyManager.Parse(m_strFileName);
          
        // if value changed or if we dont got any textures yet
        if (m_strTextureFileName != m_strTxt || 0==m_vecTextures.Length)
        {
          // then free our resources, and reload the (new) image
          FreeResources();
          m_strTextureFileName =m_strTxt;
          if (m_strTxt.Length==0)
          {
            // filename for new image is empty
            // no need to load it
            return false;
          }
          IsVisible=true;
          AllocResources();
          Update();
        }
      }

        
      // if we are not rendering the GUI background
      if (!GUIGraphicsContext.ShowBackground)
      {
        // then check if this image is the background
        if (m_iRenderWidth==GUIGraphicsContext.Width && m_iRenderHeight==GUIGraphicsContext.Height)
        {
          // and we're playing video or tv
          if (GUIGraphicsContext.IsPlaying && GUIGraphicsContext.IsPlayingVideo)
          {
            //if all true then don't render this image
            return false;
          }
        }
      }

      //check if we should use GDI to draw the image
      if (GUIGraphicsContext.graphics!=null)
      {
        // yes, If the GDI Image is not loaded, load the Image
        if (m_image==null)
        {
          string strFileName=m_strFileName;
          if (ContainsProperty)
            strFileName=GUIPropertyManager.Parse(m_strFileName);
          if (strFileName != "-")
          {
            if (!System.IO.File.Exists(strFileName))
            {
              if (strFileName[1]!=':')
                strFileName=GUIGraphicsContext.Skin+@"\media\"+strFileName;
            }
            m_image= GUITextureManager.GetImage(strFileName);
          }
        }

        // Draw the GDI image
        if (m_image!=null)
        {
          GUIGraphicsContext.graphics.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
          GUIGraphicsContext.graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceOver;
          GUIGraphicsContext.graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          GUIGraphicsContext.graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;

          try
          {
            GUIGraphicsContext.graphics.DrawImage(m_image,m_destRect);
          }
          catch(Exception)
          {
          }
          return false;        
        }
      }

      // if image is an animation then present the next frame
			if (m_vecTextures==null) return false;
      if (m_vecTextures.Length != 1)
        Process();

      // if the current frame is invalid then return
      if (m_iCurrentImage< 0 || m_iCurrentImage >=m_vecTextures.Length) return false;

      //get the current frame
      CachedTexture.Frame frame=m_vecTextures[m_iCurrentImage];
      if (frame==null) return false; // no frame? then return

      //get the texture of the frame
      Direct3D.Texture texture=frame.Image;
      if (texture==null)
      {
        // no texture? then return
        FreeResources();
        return false;
      }
      // is texture still valid?
      if (texture.Disposed)
      {
        //no? then return
        FreeResources();
        return false;
      }
        
      return true;
    }

    public void RenderToSprite(Sprite sprite)
    {
      if (sprite==null) return;
      if (sprite.Disposed) return;

      if (!PreRender()) return; // SLOW

      //get the current frame
      CachedTexture.Frame frame=m_vecTextures[m_iCurrentImage];
      if (frame==null) return ; // no frame? then return

      //get the texture of the frame
      Direct3D.Texture texture=frame.Image;
      // Set the scaling transform
      sprite.Transform = Matrix.Scaling(scaleX, scaleY, 1.0f);
      sprite.Draw(texture, sourceRect, new Vector3(), pntPosition, unchecked((int)m_colDiffuse) );
    }
		
    /// <summary>
		/// Renders the Image
		/// </summary>
		public override void Render()
    {
			//return;
      //lock (this)
      {
        if (!PreRender()) return;
				
        //get the current frame
        CachedTexture.Frame frame=m_vecTextures[m_iCurrentImage];
        if (frame==null) return ; // no frame? then return

				if (frame.UseNewTextureEngine)
				{
					frame.Draw(_fx,_fy,_nw,_nh,_uoff,_voff,_umax,_vmax,_color);
					return;
				}
/*
        //get the texture of the frame
        Direct3D.Texture texture=frame.Image;

				GUIGraphicsContext.DX9Device.SetTexture( 0, texture); //SLOW
        GUIGraphicsContext.DX9Device.SetStreamSource( 0, m_vbBuffer, 0); // SLOW
        GUIGraphicsContext.DX9Device.VertexFormat = CustomVertex.TransformedColoredTextured.Format;
        GUIGraphicsContext.DX9Device.DrawPrimitives( PrimitiveType.TriangleStrip, 0, 2 ); //SLOW

        GUIGraphicsContext.DX9Device.SetTexture( 0, null);
*/
      }
		}

		/// <summary>
		/// Set the filename of the texture and re-allocates the DirectX resources for this GUIImage.
		/// </summary>
		/// <param name="strFileName"></param>
		public void SetFileName(string strFileName)
		{
      if (strFileName==null) return;
      if (m_strFileName==strFileName) return;// same file, no need to do anything

			m_strFileName=strFileName;
      if (m_strFileName.IndexOf("#")>=0) ContainsProperty=true;
      else ContainsProperty=false;
			
      //reallocate & load then new image
      Cleanup();
      AllocResources();
		}
    
		/// <summary>
		/// Gets the rectangle in which this GUIImage is rendered.
		/// </summary>
    public Rectangle rect
    {
      get { return m_destRect;}
    }

    /// <summary>
    /// Property to enable/disable filtering
    /// </summary>
		public bool Filtering
		{
			get { return m_bFiltering;}
			set {m_bFiltering=value;/*CreateStateBlock();*/}
    }

    /// <summary>
    /// Property which indicates if the image should be centered in the
    /// given rectangle of the control
    /// </summary>
    public bool Centered
    {
      get { return m_bCentered;}
      set {m_bCentered=value;}
    }

    /// <summary>
    /// Property which indicates if the image should be zoomed in the
    /// given rectangle of the control
    /// </summary>
    public bool Zoom
    {
      get { return m_bZoom;}
      set 
      {
        m_bZoom=value;
        Update();
      }
    }

    /// <summary>
    /// Property which indicates if the image should retain its height 
    /// after it has been zoomed or aspectratio adjusted
    /// </summary>
    public bool FixedHeight
    {
      get { return m_bFixedHeight;}
      set 
      {
        m_bFixedHeight=value;
        Update();
      }
    }

    /// <summary>
    /// Property which indicates if the image should be zoomed into the
    /// given rectangle of the control. Zoom with fixed top, center width
    /// </summary>
    public bool ZoomFromTop
    {
      get { return m_bZoomFromTop;}
      set 
      {
        m_bZoomFromTop=value;
        Update();
      }
    }

    // recalculate the image dimensions & position
    public void Refresh()
    {
      Update();
    }

    /// <summary>
    /// property which returns true when this instance has a valid image
    /// </summary>
    public bool Allocated
    {
      get
      {
        if (FileName.Length==0) return false;
        if (FileName.Equals("-") ) return false;
        return true;
      }
    }

    /// <summary>
    /// Create a Direct3d stateblock
    /// </summary>
    /// 
		/*
    void CreateStateBlock()
    {
      
      lock (this)
      {
        if (savedStateBlock!=null)
        {
          savedStateBlock.Dispose();
        }
        savedStateBlock=null;
        bool supportsAlphaBlend = Manager.CheckDeviceFormat(
          GUIGraphicsContext.DX9Device.DeviceCaps.AdapterOrdinal, 
          GUIGraphicsContext.DX9Device.DeviceCaps.DeviceType, 
          GUIGraphicsContext.DX9Device.DisplayMode.Format, 
          Usage.RenderTarget | Usage.QueryPostPixelShaderBlending, ResourceType.Textures, 
          Format.A8R8G8B8);
        bool supportsFiltering=Manager.CheckDeviceFormat(
          GUIGraphicsContext.DX9Device.DeviceCaps.AdapterOrdinal, 
          GUIGraphicsContext.DX9Device.DeviceCaps.DeviceType, 
          GUIGraphicsContext.DX9Device.DisplayMode.Format, 
          Usage.RenderTarget | Usage.QueryFilter, ResourceType.Textures, 
          Format.A8R8G8B8);

        GUIGraphicsContext.DX9Device.BeginStateBlock();
        
          
        


        GUIGraphicsContext.DX9Device.TextureState[0].ColorOperation =Direct3D.TextureOperation.Modulate;
        GUIGraphicsContext.DX9Device.TextureState[0].ColorArgument1 =Direct3D.TextureArgument.TextureColor;
        GUIGraphicsContext.DX9Device.TextureState[0].ColorArgument2 =Direct3D.TextureArgument.Diffuse;
  				
        GUIGraphicsContext.DX9Device.TextureState[0].AlphaOperation =Direct3D.TextureOperation.Modulate;
  				
        GUIGraphicsContext.DX9Device.TextureState[0].AlphaArgument1 =Direct3D.TextureArgument.TextureColor;
        GUIGraphicsContext.DX9Device.TextureState[0].AlphaArgument2 =Direct3D.TextureArgument.Diffuse;
        GUIGraphicsContext.DX9Device.TextureState[1].ColorOperation =Direct3D.TextureOperation.Disable;
        GUIGraphicsContext.DX9Device.TextureState[1].AlphaOperation =Direct3D.TextureOperation.Disable ;

        if (m_bFiltering)
        { 
          if (supportsFiltering)
          {
            GUIGraphicsContext.DX9Device.SamplerState[0].MinFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[0].MagFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[0].MipFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[0].MaxAnisotropy=g_nAnisotropy;
    	      
            GUIGraphicsContext.DX9Device.SamplerState[1].MinFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[1].MagFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[1].MipFilter=TextureFilter.Linear;
            GUIGraphicsContext.DX9Device.SamplerState[1].MaxAnisotropy=g_nAnisotropy;
          }
          else
          {
            GUIGraphicsContext.DX9Device.SamplerState[0].MinFilter=TextureFilter.Point;
            GUIGraphicsContext.DX9Device.SamplerState[0].MagFilter=TextureFilter.Point;
            GUIGraphicsContext.DX9Device.SamplerState[0].MipFilter=TextureFilter.Point;
    	      
            GUIGraphicsContext.DX9Device.SamplerState[1].MinFilter=TextureFilter.Point;
            GUIGraphicsContext.DX9Device.SamplerState[1].MagFilter=TextureFilter.Point;
            GUIGraphicsContext.DX9Device.SamplerState[1].MipFilter=TextureFilter.Point;
          }
        }
        else
        {
          GUIGraphicsContext.DX9Device.SamplerState[0].MinFilter=TextureFilter.None;
          GUIGraphicsContext.DX9Device.SamplerState[0].MagFilter=TextureFilter.None;
          GUIGraphicsContext.DX9Device.SamplerState[0].MipFilter=TextureFilter.None;
          GUIGraphicsContext.DX9Device.SamplerState[1].MinFilter=TextureFilter.None;
          GUIGraphicsContext.DX9Device.SamplerState[1].MagFilter=TextureFilter.None;
          GUIGraphicsContext.DX9Device.SamplerState[1].MipFilter=TextureFilter.None;
        }
        GUIGraphicsContext.DX9Device.RenderState.ZBufferEnable=false;
        GUIGraphicsContext.DX9Device.RenderState.FogEnable=false;
        GUIGraphicsContext.DX9Device.RenderState.FogTableMode=Direct3D.FogMode.None;
        GUIGraphicsContext.DX9Device.RenderState.FillMode=Direct3D.FillMode.Solid;
        GUIGraphicsContext.DX9Device.RenderState.CullMode=Direct3D.Cull.CounterClockwise;
        if (supportsAlphaBlend)
        {
          GUIGraphicsContext.DX9Device.RenderState.AlphaBlendEnable=true;
          GUIGraphicsContext.DX9Device.RenderState.SourceBlend=Direct3D.Blend.SourceAlpha;
          GUIGraphicsContext.DX9Device.RenderState.DestinationBlend=Direct3D.Blend.InvSourceAlpha;
        }
        else
        {
          GUIGraphicsContext.DX9Device.RenderState.AlphaBlendEnable=false;
        }
        savedStateBlock = GUIGraphicsContext.DX9Device.EndStateBlock();
      }
    }*/
	}
}
