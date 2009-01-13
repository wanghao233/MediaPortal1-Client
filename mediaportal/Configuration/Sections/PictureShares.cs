#region Copyright (C) 2005-2008 Team MediaPortal

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

#endregion

using MediaPortal.Util;

#pragma warning disable 108

namespace MediaPortal.Configuration.Sections
{
  public class PictureShares : BaseShares
  {
    public PictureShares()
      : this("Picture Folders")
    {
    }

    public PictureShares(string name)
      : base(name)
    {
    }

    public override void LoadSettings()
    {
      base.LoadSettings("pictures", Win32API.GetFolderPath(Win32API.CSIDL_MYPICTURES));
    }

    public override void SaveSettings()
    {
      base.SaveSettings("pictures");
    }
  }
}