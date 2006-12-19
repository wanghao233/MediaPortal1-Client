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
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MediaPortal.Visualization
{
    public interface IVisualizationManager
    {
        List<VisualizationInfo> GetVisualizationPluginsInfo();
        bool CreateVisualization(VisualizationInfo vizPluginInfo);
        bool CreatePreviewVisualization(VisualizationInfo vizPluginInfo);   // Used to preview visualizations when in the configuration app
        bool ResizeVisualizationWindow(Size newSize);
        bool Start();
        bool Pause();
        bool Stop();
        void ShutDown();
        int TargetFPS { get; set;}
        List<VisualizationInfo> VisualizationPluginsInfo { get;}
    }
}
