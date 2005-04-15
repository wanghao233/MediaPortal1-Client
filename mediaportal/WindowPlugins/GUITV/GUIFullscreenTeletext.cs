using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using MediaPortal.TV.Recording;
using MediaPortal.TV.Database;
using MediaPortal.GUI.Pictures;


namespace MediaPortal.GUI.TV
{
	/// <summary>
	/// 
	/// </summary>
	public class GUITVFullscreenTeletext : GUIWindow
	{
		enum Controls
		{
			LBL_MESSAGE=27,
			IMG_TELETEXT_PAGE=500,
		};

		DVBTeletext	m_teleText;
		Bitmap	m_pageBitmap;
		string	m_strInput="";
		int		m_actualPage=100;
		int		m_actualSubPage=0;
		bool	m_pageDirty=false;




		public  GUITVFullscreenTeletext()
		{
			GetID=(int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT;
		}
    
		public override bool Init()
		{
			return Load (GUIGraphicsContext.Skin+@"\myfsteletext.xml");
		}
		
    
		#region Serialisation
		void LoadSettings()
		{
			using (MediaPortal.Profile.Xml   xmlreader=new MediaPortal.Profile.Xml("MediaPortal.xml"))
			{
			}
		}

		void SaveSettings()
		{
			using (MediaPortal.Profile.Xml   xmlwriter=new MediaPortal.Profile.Xml("MediaPortal.xml"))
			{
			}
		}
		#endregion

		public override void OnAction(Action action)
		{
			switch (action.wID)
			{
				case Action.ActionType.ACTION_PREVIOUS_MENU:
				{
					GUIWindowManager.ShowPreviousWindow();
					return;
				}
				case Action.ActionType.ACTION_KEY_PRESSED:
					if (action.m_key!=null)
						OnKeyCode((char)action.m_key.KeyChar);
					break;

				case Action.ActionType.ACTION_SELECT_ITEM:
					break;

			}
			base.OnAction(action);
			
		}
		public override bool OnMessage(GUIMessage message)
		{
			switch ( message.Message )
			{

				case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
				{
					if ( !GUITVHome.IsTVWindow(message.Param1) )
					{
						if (! g_Player.Playing)
						{
							if (GUIGraphicsContext.ShowBackground)
							{
								// stop timeshifting & viewing... 
	              
								Recorder.StopViewing();
							}
						}
					}
					base.OnMessage(message);
					return true;
				}

				case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
				{
					base.OnMessage(message);
					ShowMessage(100,0);
					
					if(m_teleText==null)
					{
						Log.Write("dvb-teletext: no teletext object");
						GUIWindowManager.ShowPreviousWindow();
						return false;
					}
					GUIImage pictureBox = (GUIImage )GetControl( (int)Controls.IMG_TELETEXT_PAGE);
					if(pictureBox!=null && m_teleText!=null)
					{
						pictureBox.Width=GUIGraphicsContext.OverScanWidth;
						pictureBox.Height=GUIGraphicsContext.OverScanHeight;
						pictureBox.XPosition=GUIGraphicsContext.OverScanLeft;
						pictureBox.YPosition=GUIGraphicsContext.OverScanTop;
						m_teleText.SetPageSize(pictureBox.Width,pictureBox.Height);
					}
					m_actualPage=100;
					m_actualSubPage=0;
					GetNewPage();

					return true;
				}
					//break;

				case GUIMessage.MessageType.GUI_MSG_CLICKED:
					int iControl=message.SenderControlId;

					break;

			}
			return base.OnMessage(message);;
		}

		void GetNewPage()
		{
			if(m_teleText!=null)
			{
				m_pageBitmap=m_teleText.GetPage(m_actualPage,m_actualSubPage);
				Redraw();
				Log.Write("dvb-teletext: select page {0} / subpage {1}",Convert.ToString(m_actualPage),Convert.ToString(m_actualSubPage));
			}
		}


		void OnKeyCode(char chKey)
		{


			if(chKey=='w' || chKey=='W')
			{
				GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_TELETEXT);
			}
			if((chKey>='0'&& chKey <='9') || (chKey=='+' || chKey=='-')) //navigation
			{
				if (chKey=='0' && m_strInput.Length==0) return;

				// page up
				if((byte)chKey==0x2B && m_actualPage<899) // +
				{
					m_actualPage++;
					m_actualSubPage=0;
					if(m_teleText!=null)
					{
						m_pageBitmap=m_teleText.GetPage(m_actualPage,m_actualSubPage);
						Redraw();
						Log.Write("dvb-teletext: select page {0} / subpage {1}",Convert.ToString(m_actualPage),Convert.ToString(m_actualSubPage));
						m_strInput="";
						return;
					}

				}
				// page down
				if((byte)chKey==0x2D && m_actualPage>100) // -
				{
					m_actualPage--;
					m_actualSubPage=0;
					if(m_teleText!=null)
					{
						m_pageBitmap=m_teleText.GetPage(m_actualPage,m_actualSubPage);
						Redraw();
						Log.Write("dvb-teletext: select page {0} / subpage {1}",Convert.ToString(m_actualPage),Convert.ToString(m_actualSubPage));
						m_strInput="";
						return;
					}

				}
				if(chKey>='0' && chKey<='9')
					m_strInput+= chKey;

				if (m_strInput.Length==3)
				{
					// change channel
					m_actualPage=Convert.ToInt16(m_strInput);
					m_actualSubPage=0;
					if(m_actualPage<100)
						m_actualPage=100;
					if(m_actualPage>899)
						m_actualPage=899;
					if(m_teleText!=null)
					{
						m_pageBitmap=m_teleText.GetPage(m_actualPage,m_actualSubPage);
						Redraw();
					}
					Log.Write("dvb-teletext: select page {0} / subpage {1}",Convert.ToString(m_actualPage),Convert.ToString(m_actualSubPage));
					m_strInput="";
					
				}
				//
				// get page
				//
			}
		}

		public override void SetObject(object obj)
		{
			if(obj.GetType()==typeof(DVBTeletext))
			{
				m_teleText=(DVBTeletext)obj;
				if(m_teleText==null)
					return;
				m_teleText.PageUpdatedEvent+=new MediaPortal.TV.Recording.DVBTeletext.PageUpdated(m_teleText_PageUpdatedEvent);
				m_teleText.TransparentMode=true;
			}
		}
		//
		//
		void ShowMessage(int page,int subpage)
		{

			string msg=String.Format("Waiting for Page {0}/{1}...",page,subpage);
			GUIControl.SetControlLabel(GetID,(int)Controls.LBL_MESSAGE,msg);
			GUIControl.ShowControl(GetID,(int)Controls.LBL_MESSAGE);

		}
		//
		//
		private void m_teleText_PageUpdatedEvent()
		{
			// make sure the callback returns as soon as possible!!
			// here is only a flag set to true, the bitmap is getting
			// in a timer-elapsed event!

			if(GUIWindowManager.ActiveWindow==GetID)
			{
				m_pageDirty=true;
			}
		}

		public override void Process()
		{
			if(m_pageDirty==true)
			{
				Log.Write("dvb-teletext page updated. {0:X}/{1}",m_actualPage,m_actualSubPage);
				m_pageBitmap=m_teleText.GetPage(m_actualPage,m_actualSubPage);
				Redraw();
				m_pageDirty=false;
			}
		}

		void Redraw()
		{
			Log.Write("dvb-teletext redraw()");
			try
			{

				GUIImage pictureBox = (GUIImage )GetControl( (int)Controls.IMG_TELETEXT_PAGE);
				if(m_pageBitmap==null)
				{
					ShowMessage(m_actualPage,m_actualSubPage);
					pictureBox.FreeResources();
					pictureBox.SetFileName("button_small_settings_nofocus.png");
					pictureBox.AllocResources();
					return;
				}
				GUIControl.HideControl(GetID,(int)Controls.LBL_MESSAGE);

				
				pictureBox.FileName="";
				pictureBox.FreeResources();
				pictureBox.IsVisible=false;
				Utils.FileDelete(@"teletext.jpg");
				GUITextureManager.ReleaseTexture(@"teletext.jpg");
				m_pageBitmap.Save(@"teletext.jpg",System.Drawing.Imaging.ImageFormat.Jpeg);
				pictureBox.FileName=@"teletext.jpg";
				pictureBox.AllocResources();
				pictureBox.IsVisible=true;
			}
			catch (Exception ex)
			{
				Log.Write("ex:{0} {1} {2}", ex.Message,ex.Source,ex.StackTrace);
			}
		}

	}// class
}// namespace