#region Copyright (C) 2007 Team MediaPortal
/* 
 *	Copyright (C) 2007 Team MediaPortal
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

#region Usings
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace TvEngine.PowerScheduler.Interfaces
{
  public interface IStandbyHandler
  {
    /// <summary>
    /// Indicator whether or not to allow suspension/hibernation of the system
    /// </summary>
    bool DisAllowShutdown { get; }

    /// <summary>
    /// Called when the user turns away from the system.
    /// </summary>
    void UserShutdownNow();

    /// <summary>
    /// Description of the source that allows / disallows shutdown
    /// </summary>
    string HandlerName { get; }
  }
}
