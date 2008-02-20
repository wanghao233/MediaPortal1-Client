#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Core.InputManager;
using SkinEngine;
using SkinEngine.DirectX;
using Rectangle = System.Drawing.Rectangle;
using SkinEngine.Controls.Visuals.Styles;

namespace SkinEngine.Controls.Visuals
{
  public class VisualAssetContext : IAsset
  {
    public VertexBuffer _vertexBuffer;
    public Texture _texture;
    public DateTime LastTimeUsed;

    public VisualAssetContext()
    {
      LastTimeUsed = SkinContext.Now;
    }
    #region IAsset Members

    public bool IsAllocated
    {
      get
      {
        return (VertexBuffer != null || Texture != null);
      }
    }

    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - LastTimeUsed;
        if (ts.TotalSeconds >= 1)
        {
          return true;
        }

        return false;
      }
    }
    public VertexBuffer VertexBuffer
    {
      get
      {
        return _vertexBuffer;
      }
      set
      {
        if (_vertexBuffer != null)
        {
          _vertexBuffer.Dispose();
        }
        _vertexBuffer = value;
        LastTimeUsed = SkinContext.Now;
      }
    }
    public Texture Texture
    {
      get
      {
        return _texture;
      }
      set
      {
        if (_texture != null)
        {
          _texture.Dispose();
        }
        _texture = value;
        LastTimeUsed = SkinContext.Now;
      }
    }

    public bool Free(bool force)
    {
      if (VertexBuffer != null)
      {
        VertexBuffer.Dispose();
        VertexBuffer = null;
      }

      if (Texture != null)
      {
        Texture.Dispose();
        Texture = null;
      }
      return false;
    }

    #endregion
  }
}
