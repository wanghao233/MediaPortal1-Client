﻿#region Copyright (C) 2007-2008 Team MediaPortal

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

using SlimDX;
using SlimDX.Direct3D9;

namespace SkinEngine
{
  public class ExtendedMatrix
  {
    #region variables

    public Matrix Matrix;
    public Vector4 Alpha;
    public Vector3 Translation;
    public double Opacity;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedMatrix"/> class.
    /// </summary>
    public ExtendedMatrix()
    {
      Matrix = Matrix.Identity;
      Opacity = 1.0;
      Alpha = new Vector4(1, 1, 1, 1);
    }
    public ExtendedMatrix(double opacity)
    {
      Matrix = Matrix.Identity;
      Opacity = opacity;
      Alpha = new Vector4(1, 1, 1, 1);
    }

    /// <summary>
    /// Multiplies the specified matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns></returns>
    public ExtendedMatrix Multiply(ExtendedMatrix matrix)
    {
      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix * matrix.Matrix;
      m.Alpha.X = Alpha.X * matrix.Alpha.X;
      m.Alpha.Y = Alpha.Y * matrix.Alpha.Y;
      m.Alpha.Z = Alpha.Z * matrix.Alpha.Z;
      m.Alpha.W = Alpha.W * matrix.Alpha.W;
      m.Opacity = Opacity * matrix.Opacity;
      return m;
    }

    /// <summary>
    /// decomposes the matrix.
    /// </summary>
    /// <param name="mat">The mat.</param>
    /// <param name="vTrans">The v trans.</param>
    /// <param name="vScale">The v scale.</param>
    /// <param name="mRot">The m rot.</param>
    public void MatrixDecompose(Matrix mat,
                                out Vector3 vTrans,
                                out Vector3 vScale,
                                out Matrix mRot)
    {
      vTrans = new Vector3();
      vScale = new Vector3();
      mRot = Matrix.Identity;

      Vector3[] vCols = new Vector3[]
        {
          new Vector3(mat.M11, mat.M12, mat.M13),
          new Vector3(mat.M21, mat.M22, mat.M23),
          new Vector3(mat.M31, mat.M32, mat.M33)
        };

      vScale.X = vCols[0].Length();
      vScale.Y = vCols[1].Length();
      vScale.Z = vCols[2].Length();


      vTrans.X = mat.M41 / (vScale.X == 0 ? 1 : vScale.X);
      vTrans.Y = mat.M42 / (vScale.Y == 0 ? 1 : vScale.Y);
      vTrans.Z = mat.M43 / (vScale.Z == 0 ? 1 : vScale.Z);

      if (vScale.X != 0)
      {
        vCols[0].X /= vScale.X;
        vCols[0].Y /= vScale.X;
        vCols[0].Z /= vScale.X;
      }
      if (vScale.Y != 0)
      {
        vCols[1].X /= vScale.Y;
        vCols[1].Y /= vScale.Y;
        vCols[1].Z /= vScale.Y;
      }
      if (vScale.Z != 0)
      {
        vCols[2].X /= vScale.Z;
        vCols[2].Y /= vScale.Z;
        vCols[2].Z /= vScale.Z;
      }

      mRot.M11 = vCols[0].X;
      mRot.M12 = vCols[0].Y;
      mRot.M13 = vCols[0].Z;
      mRot.M14 = 0;
      mRot.M41 = 0;
      mRot.M21 = vCols[1].X;
      mRot.M22 = vCols[1].Y;
      mRot.M23 = vCols[1].Z;
      mRot.M24 = 0;
      mRot.M42 = 0;
      mRot.M31 = vCols[2].X;
      mRot.M32 = vCols[2].Y;
      mRot.M33 = vCols[2].Z;
      mRot.M34 = 0;
      mRot.M43 = 0;
      mRot.M44 = 1;
    }


    public void TransformPoint(ref System.Drawing.PointF p)
    {
      float w = p.X;
      float h = p.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      p.X = w1;
      p.Y = h1;
    }

    public void TransformPoint(ref System.Drawing.Point p)
    {
      float w = p.X;
      float h = p.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      p.X = (int)w1;
      p.Y = (int)h1;
    }


    public void TransformSize(ref System.Drawing.SizeF size)
    {
      float w = size.Width;
      float h = size.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      size.Width = (float)w1;
      size.Height = (float)h1;
    }
    public void TransformSize(ref System.Drawing.Size size)
    {
      float w = size.Width;
      float h = size.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      size.Width = (int)w1;
      size.Height = (int)h1;
    }

    public void TransformRect(ref System.Drawing.Rectangle rect)
    {
      float w = rect.Width;
      float h = rect.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.Width = (int)w1;
      rect.Height = (int)h1;

      w = rect.X;
      h = rect.Y;
      w1 = w * Matrix.M11 + h * Matrix.M21;
      h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.X = (int)w1;
      rect.Y = (int)h1;
    }
    public void TransformRect(ref System.Drawing.RectangleF rect)
    {
      float w = rect.Width;
      float h = rect.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.Width = w1;
      rect.Height = h1;

      w = rect.X;
      h = rect.Y;
      w1 = w * Matrix.M11 + h * Matrix.M21;
      h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.X = w1;
      rect.Y = h1;
    }

    public void InvertSize(ref System.Drawing.SizeF size)
    {
      Matrix inverse = Matrix.Invert(Matrix);
      float w1 = size.Width * inverse.M11 + size.Height * inverse.M21;
      float h1 = size.Width * inverse.M12 + size.Height * inverse.M22;
      size.Width = w1;
      size.Height = h1;
    }

    public void InvertXY(ref float x, ref float y)
    {
      Matrix inverse = Matrix.Invert(Matrix);
      float w1 = x * inverse.M11 + y * inverse.M21;
      float h1 = x * inverse.M12 + y * inverse.M22;
      x = w1;
      y = h1;
    }

    public void TransformRectLocation(ref System.Drawing.Rectangle rect)
    {
      float w = rect.X;
      float h = rect.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.X = (int)w1;
      rect.Y = (int)h1;
    }

    public void TransformXY(ref float w, ref float h)
    {
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      w = w1;
      h = h1;
    }

    public void TransformXY(ref Vector2 vector)
    {
      float w1 = vector.X * Matrix.M11 + vector.Y * Matrix.M21;
      float h1 = vector.X * Matrix.M12 + vector.Y * Matrix.M22;
      vector.X = w1;
      vector.Y = h1;
    }
    public Vector3 Transform(Vector3 vector)
    {
      float w1 = vector.X * Matrix.M11 + vector.Y * Matrix.M21;
      float h1 = vector.X * Matrix.M12 + vector.Y * Matrix.M22;
      vector.X = w1;
      vector.Y = h1;
      return vector;
    }

    public System.Drawing.Drawing2D.Matrix Get2dMatrix()
    {
      return new System.Drawing.Drawing2D.Matrix(Matrix.M11, Matrix.M12, Matrix.M21, Matrix.M22, 0, 0);
    }

  } ;
}
