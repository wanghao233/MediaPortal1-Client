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

namespace MediaPortal.GUI.Library
{
    /// <summary>
    /// The implementation of a PlayList item button with move up, move down, and delete playlist item buttons.
    /// </summary>
    public class GUIPlayListButtonControl : GUIButtonControl
	{
        #region Variables

        [XMLSkinElement("textureMoveUp")]
        string TextureMoveUpFileName = "playlist_item_up_nofocus.png";

        [XMLSkinElement("textureMoveUpFocused")]
        string TextureMoveUpFocusedFileName = "playlist_item_up_focus.png";

        [XMLSkinElement("textureMoveDown")]
        string TextureMoveDownFileName = "playlist_item_down_nofocus.png";

        [XMLSkinElement("textureMoveDownFocused")]
        string TextureMoveDownFocusedFileName = "playlist_item_down_focus.png";

        [XMLSkinElement("textureDelete")]
        string TextureDeleteFileName = "playlist_item_delete_nofocus.png";

        [XMLSkinElement("textureDeleteFocused")]
        string TextureDeleteFocusedFileName = "playlist_item_delete_focus.png";

        //bool _isAscending = true;
        bool IsEditImageHot = false;

        [XMLSkinElement("upBtnWidth")]
        int UpBtnWidth = 35;

        [XMLSkinElement("downBtnWidth")]
        int DownBtnWidth = 35;

        [XMLSkinElement("deleteBtnWidth")]
        int DeleteBtnWidth = 35;

        [XMLSkinElement("upBtnHeight")]
        int UpBtnHeight = 38;

        [XMLSkinElement("downBtnHeight")]
        int DownBtnHeight = 38;

        [XMLSkinElement("deleteBtnHeight")]
        int DeleteBtnHeight = 38;

        [XMLSkinElement("upBtnXOffset")]
        int UpBtnXOffset = 0;

        [XMLSkinElement("downBtnXOffset")]
        int DownBtnXOffset = 0;

        [XMLSkinElement("deleteBtnXOffset")]
        int DeleteBtnXOffset = 0;

        [XMLSkinElement("upBtnYOffset")]
        int UpBtnYOffset = 0;

        [XMLSkinElement("downBtnYOffset")]
        int DownBtnYOffset = 0;

        [XMLSkinElement("deleteBtnYOffset")]
        int DeleteBtnYOffset = 0;

        public enum ActiveButton { None, Main, Up, Down, Delete }
        private ActiveButton _CurrentActiveButton = ActiveButton.None;

        private static ActiveButton _LastActiveButton = ActiveButton.None;
        private static bool _SuppressActiveButtonReset = false;

        private GUIImage ImgUpButtonNormal = null;
        private GUIImage ImgUpButtonFocused = null;
        private GUIImage ImgUpButtonDisabled = null;

        private GUIImage ImgDownButtonNormal = null;
        private GUIImage ImgDownButtonFocused = null;
        private GUIImage ImgDownButtonDisabled = null;

        private GUIImage ImgDeleteButtonNormal = null;
        private GUIImage ImgDeleteButtonFocused = null;
        private GUIImage ImgDeleteButtonDisabled = null;

        private bool _UpButtonEnabled = true;
        private bool _DownButtonEnabled = true;
        private bool _DeleteButtonEnabled = true;
        private bool OrigUpButtonEnabled = true;
        private bool OrigDownButtonEnabled = true;
        private bool OrigDeleteButtonEnabled = true;

        private int _NavigateLeft;
        private int _NavigateRight;
        
        #endregion Fields
        
        #region Properties
        public override bool Disabled
        {
            get
            {
                return base.Disabled;
            }
            set
            {
                base.Disabled = value;

                if (value)
                {
                    _UpButtonEnabled = OrigUpButtonEnabled;
                    _DownButtonEnabled = OrigDownButtonEnabled;
                    _DeleteButtonEnabled = OrigDeleteButtonEnabled;
                }

                else
                {
                    _UpButtonEnabled = value;
                    _DownButtonEnabled = value;
                    _DeleteButtonEnabled = value;
                }
            }
        }

        public bool UpButtonEnabled
        {
            get{return _UpButtonEnabled;}
            set
            {
                _UpButtonEnabled = value;
                OrigUpButtonEnabled = value;
            }
        }

        public bool DownButtonEnabled
        {
            get { return _DownButtonEnabled; }
            set
            {
                _DownButtonEnabled = value;
                OrigDownButtonEnabled = value;
            }
        }

        public bool DeleteButtonEnabled
        {
            get { return _DeleteButtonEnabled; }
            set
            {
                _DeleteButtonEnabled = value;
                OrigDeleteButtonEnabled = value;
            }
        }

        public static ActiveButton LastActiveButton
        {
            get { return _LastActiveButton; }
            set { _LastActiveButton = value; }
        }

        public static bool SuppressActiveButtonReset
        {
            get { return _SuppressActiveButtonReset; }
            set { _SuppressActiveButtonReset = value; }
        }

        public ActiveButton CurrentActiveButton
        {
            get { return _CurrentActiveButton; }
            set 
            {
                if (_SuppressActiveButtonReset)
                    _CurrentActiveButton = _LastActiveButton;

                else
                    _CurrentActiveButton = value; 
            }
        }

        #endregion Properties

        #region Constructors

		public GUIPlayListButtonControl(int parentId) : base(parentId)
		{
			
		}


        /// <summary>
        /// The constructor of the GUIPlayListButtonControl class.
        /// </summary>
        /// <param name="dwParentID">The parent of this control.</param>
        /// <param name="dwControlId">The ID of this control.</param>
        /// <param name="dwPosX">The X position of this control.</param>
        /// <param name="dwPosY">The Y position of this control.</param>
        /// <param name="dwWidth">The width of this control.</param>
        /// <param name="dwHeight">The height of this control.</param>
        /// <param name="strTextureFocus">The filename containing the texture of the butten, when the button has the focus.</param>
        /// <param name="strTextureNoFocus">The filename containing the texture of the butten, when the button does not have the focus.</param>
         public GUIPlayListButtonControl(int dwParentID, int dwControlId, int dwPosX, int dwPosY, int dwWidth, int dwHeight, string strTextureFocus, string strTextureNoFocus,
            int upBtnWidth,
            int downBtnWidth,
            int deleteBtnWidth,
            int upBtnHeight,
            int downBtnHeight,
            int deleteBtnHeight,
            string strUp,
            string strDown,
            string strDelete,
            string strUpFocus,
            string strDownFocus,
            string strDeleteFocus,
            int upBtnXOffset,
            int downBtnXOffset,
            int deleteBtnXOffset,
            int upBtnYOffset,
            int downBtnYOffset,
            int deleteBtnYOffset)
            : base(dwParentID, dwControlId, dwPosX, dwPosY, dwWidth, dwHeight, strTextureFocus, strTextureNoFocus)
        {
            _focusedTextureName = strTextureFocus;
            _nonFocusedTextureName = strTextureNoFocus;

            UpBtnWidth = upBtnWidth;
            DownBtnWidth = downBtnWidth;
            DeleteBtnWidth = deleteBtnWidth;
            UpBtnHeight = upBtnHeight;
            DownBtnHeight = downBtnHeight;
            DeleteBtnHeight = deleteBtnHeight;
            TextureMoveUpFileName = strUp;
            TextureMoveDownFileName = strDown;
            TextureDeleteFileName = strDelete;
            TextureMoveUpFocusedFileName = strUpFocus;
            TextureMoveDownFocusedFileName = strDownFocus;
            TextureDeleteFocusedFileName = strDeleteFocus;
            UpBtnXOffset = upBtnXOffset;
            DownBtnXOffset = downBtnXOffset;
            DeleteBtnXOffset = deleteBtnXOffset;
            UpBtnYOffset = upBtnYOffset;
            DownBtnYOffset = downBtnYOffset;
            DeleteBtnYOffset = deleteBtnYOffset;
            FinalizeConstruction();
        }
		#endregion Constructors

		#region Events

		//public event SortEventHandler	SortChanged;

		#endregion Events

		#region Methods

		public override void AllocResources()
		{
			base.AllocResources();

            ImgUpButtonNormal.AllocResources();
            ImgUpButtonFocused.AllocResources();

            ImgDownButtonNormal.AllocResources();
            ImgDownButtonFocused.AllocResources();

            ImgDeleteButtonNormal.AllocResources();
            ImgDeleteButtonFocused.AllocResources();

            if(ImgUpButtonDisabled != null)
                ImgUpButtonDisabled.AllocResources();

            if(ImgDownButtonDisabled != null)
                ImgDownButtonDisabled.AllocResources();

            if (ImgDeleteButtonDisabled != null)
                ImgDeleteButtonDisabled.AllocResources();
		}

        public override void FinalizeConstruction()
        {
            base.FinalizeConstruction();
            ImgUpButtonNormal = new GUIImage(WindowId, WindowId + 10000, UpBtnXOffset, UpBtnYOffset, UpBtnWidth, UpBtnHeight, TextureMoveUpFileName, 0);
            ImgUpButtonFocused = new GUIImage(WindowId, WindowId + 10001, 0, 0, UpBtnWidth, UpBtnHeight, TextureMoveUpFocusedFileName, 0);
            ImgUpButtonNormal.ParentControl = this;
            ImgUpButtonFocused.ParentControl = this;
            ImgUpButtonNormal.DimColor = DimColor;
            ImgUpButtonFocused.DimColor = DimColor;
            ImgUpButtonNormal.BringIntoView();
            ImgUpButtonFocused.BringIntoView();

            ImgDownButtonNormal = new GUIImage(WindowId, WindowId + 10003, DownBtnXOffset, DownBtnYOffset, DownBtnWidth, DownBtnHeight, TextureMoveDownFileName, 0);
            ImgDownButtonFocused = new GUIImage(WindowId, WindowId + 10004, 0, 0, DownBtnWidth, DownBtnHeight, TextureMoveDownFocusedFileName, 0);
            ImgDownButtonNormal.ParentControl = this;
            ImgDownButtonFocused.ParentControl = this;
            ImgDownButtonNormal.DimColor = DimColor;
            ImgDownButtonFocused.DimColor = DimColor;
            ImgDownButtonNormal.BringIntoView();
            ImgDownButtonFocused.BringIntoView();

            ImgDeleteButtonNormal = new GUIImage(WindowId, WindowId + 10006, DeleteBtnXOffset, DeleteBtnYOffset, DeleteBtnWidth, DeleteBtnHeight, TextureDeleteFileName, 0);
            ImgDeleteButtonFocused = new GUIImage(WindowId, WindowId + 10007, 0, 0, DeleteBtnWidth, DeleteBtnHeight, TextureDeleteFocusedFileName, 0);
            ImgDeleteButtonNormal.ParentControl = this;
            ImgDeleteButtonFocused.ParentControl = this;
            ImgDeleteButtonNormal.DimColor = DimColor;
            ImgDeleteButtonFocused.DimColor = DimColor;
            ImgDeleteButtonNormal.BringIntoView();
            ImgDeleteButtonFocused.BringIntoView();

            // Keep track of the original NavigateLeft and NavigateRight values...
            _NavigateLeft = NavigateLeft;
            _NavigateRight = NavigateRight;

            string skinFolderPath = string.Format(@"{0}\{1}\media\", System.Windows.Forms.Application.StartupPath, GUIGraphicsContext.Skin);
            ImgUpButtonDisabled = CreateDisableButtonImage(skinFolderPath, TextureMoveUpFileName, WindowId, WindowId + 10002, UpBtnXOffset, UpBtnYOffset, UpBtnWidth, UpBtnHeight);

            if (ImgUpButtonDisabled != null)
            {
                ImgDeleteButtonNormal.ParentControl = this;
                ImgUpButtonDisabled.BringIntoView();
            }

            ImgDownButtonDisabled = CreateDisableButtonImage(skinFolderPath, TextureMoveDownFileName, WindowId, WindowId + 10005, DownBtnXOffset, DownBtnYOffset, DownBtnWidth, DownBtnHeight);

            if (ImgDownButtonDisabled != null)
            {
                ImgDownButtonDisabled.ParentControl = this;
                ImgDownButtonDisabled.BringIntoView();
            }

            ImgDeleteButtonDisabled = CreateDisableButtonImage(skinFolderPath, TextureDeleteFileName, WindowId, WindowId + 10008, DeleteBtnXOffset, DeleteBtnYOffset, DeleteBtnWidth, DeleteBtnHeight);

            if (ImgDeleteButtonDisabled != null)
            {
                ImgDeleteButtonDisabled.ParentControl = this;
                ImgDeleteButtonDisabled.BringIntoView();
            }
        }
        
		public override void ScaleToScreenResolution()
		{
			base.ScaleToScreenResolution();
		}

		public override void FreeResources()
		{
			base.FreeResources();

            ImgUpButtonNormal.FreeResources();
            ImgUpButtonFocused.FreeResources();

            ImgDownButtonNormal.FreeResources();
            ImgDownButtonFocused.FreeResources();

            ImgDeleteButtonNormal.FreeResources();
            ImgDeleteButtonFocused.FreeResources();

            if(ImgUpButtonDisabled != null)
                ImgUpButtonDisabled.FreeResources();

            if(ImgDownButtonDisabled != null)
                ImgDownButtonDisabled.FreeResources();

            if (ImgDeleteButtonDisabled != null)
                ImgDeleteButtonDisabled.FreeResources();
		}

		public override void PreAllocResources()
		{
			base.PreAllocResources();

            ImgUpButtonNormal.PreAllocResources();
            ImgUpButtonFocused.PreAllocResources();

            ImgDownButtonNormal.PreAllocResources();
            ImgDownButtonFocused.PreAllocResources();

            ImgDeleteButtonNormal.PreAllocResources();
            ImgDeleteButtonFocused.PreAllocResources();

            if (ImgUpButtonDisabled != null)
                ImgUpButtonDisabled.PreAllocResources();

            if (ImgDownButtonDisabled != null)
                ImgDownButtonDisabled.PreAllocResources();

            if (ImgDeleteButtonDisabled != null)
                ImgDeleteButtonDisabled.PreAllocResources();
        }

        public override void Render(float timePassed)
        {
            bool isFocused = this.Focus;

            if (!isFocused)
                _CurrentActiveButton = ActiveButton.Main;

            if (IsEditImageHot)
                Focus = false;

            int xPos = 0;
            int yPos = 0;

            if (!_SuppressActiveButtonReset && Focus && _CurrentActiveButton == ActiveButton.Main)
                _imageFocused.Render(timePassed);

            else
                _imageNonFocused.Render(timePassed);

            xPos = _imageNonFocused.XPosition + UpBtnXOffset;
            yPos = _imageNonFocused.YPosition + UpBtnYOffset;
            ImgUpButtonFocused.SetPosition(xPos, yPos);
            ImgUpButtonNormal.SetPosition(xPos, yPos);
            
            if(ImgUpButtonDisabled != null)
                ImgUpButtonDisabled.SetPosition(xPos, yPos);

            if (isFocused && _CurrentActiveButton == ActiveButton.Up && _UpButtonEnabled)
                ImgUpButtonFocused.Render(timePassed);

            else
            {
                if (!_UpButtonEnabled && ImgUpButtonDisabled != null)
                    ImgUpButtonDisabled.Render(timePassed);

                else
                    ImgUpButtonNormal.Render(timePassed);
            }

            xPos = _imageNonFocused.XPosition + DownBtnXOffset;
            yPos = _imageNonFocused.YPosition + DownBtnYOffset;
            ImgDownButtonFocused.SetPosition(xPos, yPos);
            ImgDownButtonNormal.SetPosition(xPos, yPos);

            if(ImgDownButtonDisabled != null)
                ImgDownButtonDisabled.SetPosition(xPos, yPos);

            if (isFocused && _CurrentActiveButton == ActiveButton.Down && _DownButtonEnabled)
                ImgDownButtonFocused.Render(timePassed);

            else
            {
                if(!_DownButtonEnabled && ImgDownButtonDisabled != null)
                    ImgDownButtonDisabled.Render(timePassed);

                else
                    ImgDownButtonNormal.Render(timePassed);
            }

            xPos = _imageNonFocused.XPosition + DeleteBtnXOffset;
            yPos = _imageNonFocused.YPosition + DeleteBtnYOffset;
            ImgDeleteButtonFocused.SetPosition(xPos, yPos);
            ImgDeleteButtonNormal.SetPosition(xPos, yPos);
            
            if(ImgDeleteButtonDisabled != null)
                ImgDeleteButtonDisabled.SetPosition(xPos, yPos);

            if (isFocused && _CurrentActiveButton == ActiveButton.Delete && _DeleteButtonEnabled)
                ImgDeleteButtonFocused.Render(timePassed);

            else
            {
                if (!_DeleteButtonEnabled && ImgDeleteButtonDisabled != null)
                    ImgDeleteButtonDisabled.Render(timePassed);

                else
                    ImgDeleteButtonNormal.Render(timePassed);
            }
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void OnAction(Action action)
        {
            if (action.wID == Action.ActionType.ACTION_MOUSE_CLICK || action.wID == Action.ActionType.ACTION_SELECT_ITEM)
            {
                //Console.WriteLine("ACTION_MOUSE_CLICK ActiveButton:{0}", _CurrentActiveButton);

                switch (_CurrentActiveButton)
                {
                    case ActiveButton.Main:
                        {
                            //Console.WriteLine("Clicked ActiveButton.Main");
                            break;
                        }

                    case ActiveButton.Up:
                        {
                            //Console.WriteLine("Clicked ActiveButton.Up");
                            break;
                        }

                    case ActiveButton.Down:
                        {
                            //Console.WriteLine("Clicked ActiveButton.Down");
                            break;
                        }

                    case ActiveButton.Delete:
                        {
                            //Console.WriteLine("Clicked ActiveButton.Delete");
                            break;
                        }

                    case ActiveButton.None:
                        {
                            // We should never get here!
                            //Console.WriteLine("Clicked ActiveButton.None");
                            break;
                        }
                }
            }

            else if (action.wID == Action.ActionType.ACTION_MOVE_LEFT)
            {
                if (_CurrentActiveButton != ActiveButton.Main)
                {
                    FocusPreviousButton();
                    return;
                }

                else
                {
                    if (NavigateLeft != _windowId)
                        _CurrentActiveButton = ActiveButton.None;

                    else
                        return;
                }
            }

            else if (action.wID == Action.ActionType.ACTION_MOVE_RIGHT)
            {
                if (_CurrentActiveButton != ActiveButton.Delete)
                {
                    FocusNextButton();
                    return;
                }


                else
                {
                    if (NavigateRight != _windowId)
                        _CurrentActiveButton = ActiveButton.None;

                    else
                        return;
                }
            }
            base.OnAction(action);
        }

        private void FocusNextButton()
        {
            if (_CurrentActiveButton == ActiveButton.Main)
            {
                if (_UpButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Up;

                else if(_DownButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Down;

                else if(_DeleteButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Delete;
            }

            else if (_CurrentActiveButton == ActiveButton.Up)
            {
                if (_DownButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Down;

                else if (_DeleteButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Delete;
            }

            else if (_CurrentActiveButton == ActiveButton.Down)
            {
                if (_DeleteButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Delete;
            }
        }

        private void FocusPreviousButton()
        {
            if (_CurrentActiveButton == ActiveButton.Delete)
            {
                if (_DownButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Down;

                else if (_UpButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Up;

                else
                    _CurrentActiveButton = ActiveButton.Main;
            }

            else if (_CurrentActiveButton == ActiveButton.Down)
            {
                if (_UpButtonEnabled)
                    _CurrentActiveButton = ActiveButton.Up;

                else
                    _CurrentActiveButton = ActiveButton.Main;
            }

            else if (_CurrentActiveButton == ActiveButton.Up)
            {
                _CurrentActiveButton = ActiveButton.Main;
            }
       }
        
        public override bool OnMessage(GUIMessage message)
        {
            if(message.Message == GUIMessage.MessageType.GUI_MSG_SETFOCUS || message.Message == GUIMessage.MessageType.GUI_MSG_LOSTFOCUS)
                IsEditImageHot = false;

            else if (message.Message == GUIMessage.MessageType.GUI_MSG_LOSTFOCUS)
            {
                _CurrentActiveButton = ActiveButton.None;
            }

            else if (message.Message == GUIMessage.MessageType.GUI_MSG_SETFOCUS)
            {
                _CurrentActiveButton = ActiveButton.Main;
            }

            return base.OnMessage (message);
        }

        public override bool HitTest(int x, int y, out int controlID, out bool focused)
        {
            int focusedControlID = 0;

            if (ImgUpButtonNormal.InControl(x, y, out focusedControlID))
            {
                _CurrentActiveButton = ActiveButton.Up;
            }

            else if (ImgDownButtonNormal.InControl(x, y, out focusedControlID))
            {
                _CurrentActiveButton = ActiveButton.Down;
            }

            else if (ImgDeleteButtonNormal.InControl(x, y, out focusedControlID))
            {
                _CurrentActiveButton = ActiveButton.Delete;
            }

            else
            {
                _CurrentActiveButton = ActiveButton.Main;
            }

            return base.HitTest(x, y, out controlID, out focused);
       }

		#endregion Methods

        public bool CanMoveRight()
        {
            if (_CurrentActiveButton == ActiveButton.Main)
            {
                if (_UpButtonEnabled)
                    return true;

                else if (_DownButtonEnabled)
                    return true;

                else if (_DeleteButtonEnabled)
                    return true;
            }

            else if (_CurrentActiveButton == ActiveButton.Up)
            {
                if (_DownButtonEnabled)
                    return true;

                else if (_DeleteButtonEnabled)
                    return true;
            }

            else if (_CurrentActiveButton == ActiveButton.Down)
            {
                if (_DeleteButtonEnabled)
                    return true;
            }

            return false;
        }

        public bool CanMoveLeft()
        {
            if (_CurrentActiveButton == ActiveButton.Delete)
            {
                if (_DownButtonEnabled)
                    return true;

                else if (_UpButtonEnabled)
                    return true;

                else
                    return true;
            }

            else if (_CurrentActiveButton == ActiveButton.Down)
            {
                if (_UpButtonEnabled)
                    return true;

                else
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a "disabled" button image from an normal (unfocused) button image by creating a
        /// copy of the normal image and reducing the alpha component of each pixel by 50%. If the image
        /// exists, the existing version is used
        /// </summary>
        /// <param name="skinFolderImagePath"></param>
        /// <param name="origImageFileName"></param>
        /// <param name="parentId"></param>
        /// <param name="id"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>GUIImage</returns>
        private GUIImage CreateDisableButtonImage(string skinFolderImagePath, string origImageFileName, int parentId, int id, int xOffset, int yOffset, int width, int height)
        {
            string imagePath = System.IO.Path.Combine(skinFolderImagePath, origImageFileName);

            // If the original image doesn't exist bail out.
            if (!System.IO.File.Exists(imagePath))
                return null;

            string ext = System.IO.Path.GetExtension(origImageFileName);
            string baseImgFileName = System.IO.Path.GetFileNameWithoutExtension(origImageFileName);
            string dimmedImgFileName = baseImgFileName + "_dimmed" + ext;
            string fullImagePath = System.IO.Path.Combine(skinFolderImagePath, dimmedImgFileName);
            GUIImage guiImg = null;

            // If the dimmed image already exists, use it to create the GUIImage
            if (System.IO.File.Exists(fullImagePath))
                guiImg = new GUIImage(parentId, id, xOffset, yOffset, width, height, dimmedImgFileName, 0);

            else
            {
                System.Drawing.Bitmap origImg = new System.Drawing.Bitmap(imagePath);
                System.Drawing.Bitmap newImg = new System.Drawing.Bitmap(origImg);

                if (origImg == null || newImg == null)
                    return null;

                for (int y = 0; y < origImg.Height; y++)
                {
                    for (int x = 0; x < origImg.Width; x++)
                    {
                        System.Drawing.Color c = origImg.GetPixel(x, y);

                        byte alpha = c.A;

                        if (alpha > 50)
                            alpha = (byte)((float)alpha * .5f);

                        newImg.SetPixel(x, y, System.Drawing.Color.FromArgb(alpha, c.R, c.G, c.B));
                    }
                }

                newImg.Save(fullImagePath, System.Drawing.Imaging.ImageFormat.Png);
                origImg.Dispose();

                if (System.IO.File.Exists(fullImagePath))
                    guiImg = new GUIImage(parentId, id, xOffset, yOffset, width, height, dimmedImgFileName, 0);

                newImg.Dispose();
            }

            return guiImg;
        }

      public override int DimColor
      {
        get { return base.DimColor; }
        set
        {
          base.DimColor = value;
          if (ImgUpButtonNormal != null) ImgUpButtonNormal.DimColor = value;
          if (ImgUpButtonFocused != null) ImgUpButtonFocused.DimColor = value;
          if (ImgUpButtonDisabled != null) ImgUpButtonDisabled.DimColor = value;

          if (ImgDownButtonNormal != null) ImgDownButtonNormal.DimColor = value;
          if (ImgDownButtonFocused != null) ImgDownButtonFocused.DimColor = value;
          if (ImgDownButtonDisabled != null) ImgDownButtonDisabled.DimColor = value;

          if (ImgDeleteButtonNormal != null) ImgDeleteButtonNormal.DimColor = value;
          if (ImgDeleteButtonFocused != null) ImgDeleteButtonFocused.DimColor = value;
          if (ImgDeleteButtonDisabled != null) ImgDeleteButtonDisabled.DimColor = value;
        }
      }

    }
}