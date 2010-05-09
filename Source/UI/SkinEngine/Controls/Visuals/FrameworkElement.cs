#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum VerticalAlignmentEnum
  {
    Top = 0,
    Center = 1,
    Bottom = 2,
    Stretch = 3,
  };

  public enum HorizontalAlignmentEnum
  {
    Left = 0,
    Center = 1,
    Right = 2,
    Stretch = 3,
  };

  public enum MoveFocusDirection
  {
    Up,
    Down,
    Left,
    Right
  }

  public class FocusableElementFinder : IFinder
  {
    private static FocusableElementFinder _instance = null;

    public bool Query(UIElement current)
    {
      FrameworkElement fe = current as FrameworkElement;
      if (fe == null)
        return false;
      return fe.Focusable;
    }

    public static FocusableElementFinder Instance
    {
      get
      {
        if (_instance == null)
          _instance = new FocusableElementFinder();
        return _instance;
      }
    }
  }

  public class FrameworkElement: UIElement
  {
    public const string GOTFOCUS_EVENT = "FrameworkElement.GotFocus";
    public const string LOSTFOCUS_EVENT = "FrameworkElement.LostFocus";
    public const string MOUSEENTER_EVENT = "FrameworkElement.MouseEnter";
    public const string MOUSELEAVE_EVENT = "FrameworkElement.MouseEnter";

    #region Protected fields

    protected AbstractProperty _widthProperty;
    protected AbstractProperty _heightProperty;

    protected AbstractProperty _actualWidthProperty;
    protected AbstractProperty _actualHeightProperty;
    protected AbstractProperty _horizontalAlignmentProperty;
    protected AbstractProperty _verticalAlignmentProperty;
    protected AbstractProperty _styleProperty;
    protected AbstractProperty _focusableProperty;
    protected AbstractProperty _hasFocusProperty;
    protected AbstractProperty _isMouseOverProperty;
    protected VisualAssetContext _opacityMaskContext;
    protected AbstractProperty _fontSizeProperty;
    protected AbstractProperty _fontFamilyProperty;

    protected AbstractProperty _contextMenuCommandProperty;

    protected bool _updateOpacityMask = false;
    protected bool _updateFocus = false;
    protected Matrix _inverseFinalTransform = Matrix.Identity;

    #endregion

    #region Ctor

    public FrameworkElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      // Default is not set
      _widthProperty = new SProperty(typeof(double), Double.NaN);
      _heightProperty = new SProperty(typeof(double), Double.NaN);

      // Default is not set
      _actualWidthProperty = new SProperty(typeof(double), Double.NaN);
      _actualHeightProperty = new SProperty(typeof(double), Double.NaN);

      // Default is not set
      _styleProperty = new SProperty(typeof(Style), null);

      // Default is stretch
      _horizontalAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Stretch);
      _verticalAlignmentProperty = new SProperty(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Stretch);

      // Focus properties
      _focusableProperty = new SProperty(typeof(bool), false);
      _hasFocusProperty = new SProperty(typeof(bool), false);

      _isMouseOverProperty = new SProperty(typeof(bool), false);

      // Context menu
      _contextMenuCommandProperty = new SProperty(typeof(IExecutableCommand), null);

      // Font properties
      _fontFamilyProperty = new SProperty(typeof(string), String.Empty);
      _fontSizeProperty = new SProperty(typeof(int), -1);
    }

    void Attach()
    {
      _widthProperty.Attach(OnLayoutPropertyChanged);
      _heightProperty.Attach(OnLayoutPropertyChanged);
      _actualHeightProperty.Attach(OnActualSizeChanged);
      _actualWidthProperty.Attach(OnActualSizeChanged);
      _styleProperty.Attach(OnStyleChanged);
      _hasFocusProperty.Attach(OnFocusPropertyChanged);
      _fontFamilyProperty.Attach(OnFontChanged);
      _fontSizeProperty.Attach(OnFontChanged);

      _opacityProperty.Attach(OnOpacityChanged);
      _opacityMaskProperty.Attach(OnOpacityChanged);
    }

    void Detach()
    {
      _widthProperty.Detach(OnLayoutPropertyChanged);
      _heightProperty.Detach(OnLayoutPropertyChanged);
      _actualHeightProperty.Detach(OnActualSizeChanged);
      _actualWidthProperty.Detach(OnActualSizeChanged);
      _styleProperty.Detach(OnStyleChanged);
      _hasFocusProperty.Detach(OnFocusPropertyChanged);
      _fontFamilyProperty.Detach(OnFontChanged);
      _fontSizeProperty.Detach(OnFontChanged);

      _opacityProperty.Detach(OnOpacityChanged);
      _opacityMaskProperty.Detach(OnOpacityChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      FrameworkElement fe = (FrameworkElement) source;
      Width = fe.Width;
      Height = fe.Height;
      Style = fe.Style; // No copying necessary - Styles should be immutable
      ActualWidth = fe.ActualWidth;
      ActualHeight = fe.ActualHeight;
      HorizontalAlignment = fe.HorizontalAlignment;
      VerticalAlignment = fe.VerticalAlignment;
      Focusable = fe.Focusable;
      FontSize = fe.FontSize;
      FontFamily = fe.FontFamily;
      Attach();
    }

    #endregion

    protected void UpdateFocus()
    {
      if (!_updateFocus)
        return;
      _updateFocus = false;
      CheckFocus();
    }

    protected void CheckFocus()
    {
      Screen screen = Screen;
      if (screen == null)
      {
        _updateFocus = true;
        return;
      }
      if (HasFocus)
      {
        MakeVisible(this, ActualBounds);
        if (!screen.FrameworkElementGotFocus(this))
        {
          _updateFocus = true;
          return;
        }
      }
      else
        screen.FrameworkElementLostFocus(this);
    }

    protected virtual void OnFontChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
      InvalidateParentLayout();
    }

    protected virtual void OnStyleChanged(AbstractProperty property, object oldValue)
    {
      ///@optimize: 
      Style.Set(this);
      InvalidateLayout();
      InvalidateParentLayout();
    }

    void OnActualSizeChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    void OnFocusPropertyChanged(AbstractProperty property, object oldValue)
    {
      CheckFocus();
    }

    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling InvalidateLayout() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
    }

    void OnOpacityChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    #region Public properties

    public AbstractProperty WidthProperty
    {
      get { return _widthProperty; }
    }

    public double Width
    {
      get { return (double) _widthProperty.GetValue(); }
      set { _widthProperty.SetValue(value); }
    }

    public AbstractProperty HeightProperty
    {
      get { return _heightProperty; }
    }

    public double Height
    {
      get { return (double) _heightProperty.GetValue(); }
      set { _heightProperty.SetValue(value); }
    }

    public AbstractProperty ActualWidthProperty
    {
      get { return _actualWidthProperty; }
    }

    public double ActualWidth
    {
      get { return (double) _actualWidthProperty.GetValue(); }
      set { _actualWidthProperty.SetValue(value); }
    }

    public AbstractProperty ActualHeightProperty
    {
      get { return _actualHeightProperty; }
    }

    public double ActualHeight
    {
      get { return (double) _actualHeightProperty.GetValue(); }
      set { _actualHeightProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets this element's bounds in this element's coordinate system.
    /// This is a derived property which is calculated by the layout system.
    /// </summary>
    public RectangleF ActualBounds
    {
      get { return _innerRect; }
    }

    /// <summary>
    /// Gets the actual bounds plus <see cref="UIElement.Margin"/> plus the space which is needed for our
    /// <see cref="UIElement.LayoutTransform"/>.
    /// </summary>
    public RectangleF ActualTotalBounds
    {
      get { return _outerRect ?? new RectangleF(); }
    }

    public AbstractProperty HorizontalAlignmentProperty
    {
      get { return _horizontalAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalAlignment
    {
      get { return (HorizontalAlignmentEnum) _horizontalAlignmentProperty.GetValue(); }
      set { _horizontalAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalAlignmentProperty
    {
      get { return _verticalAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalAlignment
    {
      get { return (VerticalAlignmentEnum) _verticalAlignmentProperty.GetValue(); }
      set { _verticalAlignmentProperty.SetValue(value);  }
    }

    public AbstractProperty StyleProperty
    {
      get { return _styleProperty; }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public Style Style
    {
      get { return (Style) _styleProperty.GetValue(); }
      set { _styleProperty.SetValue(value); }
    }

    public AbstractProperty HasFocusProperty
    {
      get { return _hasFocusProperty; }
    }

    public virtual bool HasFocus
    {
      get { return (bool) _hasFocusProperty.GetValue(); }
      internal set { _hasFocusProperty.SetValue(value); }
    }

    public AbstractProperty FocusableProperty
    {
      get { return _focusableProperty; }
    }

    public bool Focusable
    {
      get { return (bool) _focusableProperty.GetValue(); }
      set { _focusableProperty.SetValue(value); }
    }

    public AbstractProperty IsMouseOverProperty
    {
      get { return _isMouseOverProperty; }
    }

    public bool IsMouseOver
    {
      get { return (bool) _isMouseOverProperty.GetValue(); }
      internal set { _isMouseOverProperty.SetValue(value); }
    }

    public AbstractProperty ContextMenuCommandProperty
    {
      get { return _contextMenuCommandProperty; }
    }

    public IExecutableCommand ContextMenuCommand
    {
      get { return (IExecutableCommand) _contextMenuCommandProperty.GetValue(); }
      internal set { _contextMenuCommandProperty.SetValue(value); }
    }

    public AbstractProperty FontFamilyProperty
    {
      get { return _fontFamilyProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public string FontFamily
    {
      get { return (string) _fontFamilyProperty.GetValue(); }
      set { _fontFamilyProperty.SetValue(value); }
    }

    public AbstractProperty FontSizeProperty
    {
      get { return _fontSizeProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public int FontSize
    {
      get { return (int) _fontSizeProperty.GetValue(); }
      set { _fontSizeProperty.SetValue(value); }
    }

    #endregion

    public string GetFontFamilyOrInherited()
    {
      string result = FontFamily;
      Visual current = this;
      while (string.IsNullOrEmpty(result) && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontFamily;
      }
      return string.IsNullOrEmpty(result) ? FontManager.DefaultFontFamily : result;
    }

    public int GetFontSizeOrInherited()
    {
      int result = FontSize;
      Visual current = this;
      while (result == -1 && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontSize;
      }
      return result == -1 ? FontManager.DefaultFontSize : result;
    }

    public override void OnKeyPreview(ref Key key)
    {
      base.OnKeyPreview(ref key);
      if (!HasFocus)
        return;
      if (key == Key.None) return;
      if (key == Key.ContextMenu && ContextMenuCommand != null)
      {
        ContextMenuCommand.Execute();
        key = Key.None;
      }
    }

    /// <summary>
    /// Checks if this element is focusable. This is the case if the element is visible, enabled and
    /// focusable. If this is the case, this method will set the focus to this element.
    /// </summary>
    public bool TrySetFocus(bool checkChildren)
    {
      if (IsVisible && IsEnabled && Focusable)
      {
        HasFocus = true;
        return true;
      }
      else if (checkChildren)
      {
        foreach (UIElement child in GetChildren())
        {
          FrameworkElement fe = child as FrameworkElement;
          if (fe == null || !fe.IsVisible || !fe.IsEnabled)
            continue;
          if (fe.TrySetFocus(true))
            return true;
        }
      }
      return false;
    }

    protected override void MeasureOverride(ref SizeF totalSize)
    {
      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

      SizeF calculatedSize = CalculateDesiredSize(new SizeF(totalSize));

      if (double.IsNaN(Width))
        totalSize.Width = calculatedSize.Width;
      if (double.IsNaN(Height))
        totalSize.Height = calculatedSize.Height;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      ActualWidth = _innerRect.Width;
      ActualHeight = _innerRect.Height;
      UpdateFocus();
    }

    protected virtual SizeF CalculateDesiredSize(SizeF totalSize)
    {
      return new SizeF();
    }

    /// <summary>
    /// Arranges the child horizontal and vertical in a given area. If the area is bigger than
    /// the child's desired size, the child will be arranged according to its
    /// <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChild(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width))
      {
        if (desiredSize.Width < childSize.Width)
        {
          // Width takes precedence over Stretch - Use Center as fallback
          if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center ||
              (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch && !double.IsNaN(child.Width)))
          {
            location.X += (childSize.Width - desiredSize.Width)/2;
            childSize.Width = desiredSize.Width;
          }
          else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
          {
            location.X += childSize.Width - desiredSize.Width;
            childSize.Width = desiredSize.Width;
          }
          else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Left)
          {
            // Leave location unchanged
            childSize.Width = desiredSize.Width;
          }
          //else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch)
          // Do nothing
        }
      }

      if (!double.IsNaN(desiredSize.Height))
      {
        if (desiredSize.Height < childSize.Height)
        {
          // Height takes precedence over Stretch - Use Center as fallback
          if (child.VerticalAlignment == VerticalAlignmentEnum.Center ||
              (child.VerticalAlignment == VerticalAlignmentEnum.Stretch && !double.IsNaN(child.Height)))
          {
            location.Y += (childSize.Height - desiredSize.Height)/2;
            childSize.Height = desiredSize.Height;
          }
          else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
          {
            location.Y += childSize.Height - desiredSize.Height;
            childSize.Height = desiredSize.Height;
          }
          else if (child.VerticalAlignment == VerticalAlignmentEnum.Top)
          {
            // Leave location unchanged
            childSize.Height = desiredSize.Height;
          }
          //else if (child.VerticalAlignment == VerticalAlignmentEnum.Stretch)
          // Do nothing
        }
      }
    }

    /// <summary>
    /// Arranges the child horizontal in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to its <see cref="HorizontalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildHorizontal(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width) && desiredSize.Width < childSize.Width)
      {
        // Width takes precedence over Stretch - Use Center as fallback
        if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center ||
            (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch && !double.IsNaN(child.Width)))
        {
          location.X += (childSize.Width - desiredSize.Width) / 2;
          childSize.Width = desiredSize.Width;
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
        {
          location.X += childSize.Width - desiredSize.Width;
          childSize.Width = desiredSize.Width;
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Left)
        {
          // Leave location unchanged
          childSize.Width = desiredSize.Width;
        }
        //else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch)
        // Do nothing
      }
    }

    /// <summary>
    /// Arranges the child vertical in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to its <see cref="VerticalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildVertical(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width) && desiredSize.Height < childSize.Height)
      {
        // Height takes precedence over Stretch - Use Center as fallback
        if (child.VerticalAlignment == VerticalAlignmentEnum.Center ||
            (child.VerticalAlignment == VerticalAlignmentEnum.Stretch && !double.IsNaN(child.Height)))
        {
          location.Y += (childSize.Height - desiredSize.Height) / 2;
          childSize.Height = desiredSize.Height;
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
        {
          location.Y += childSize.Height - desiredSize.Height;
          childSize.Height = desiredSize.Height;
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Top)
        {
          // Leave location unchanged
          childSize.Height = desiredSize.Height;
        }
        //else if (child.VerticalAlignment == VerticalAlignmentEnum.Stretch)
        // Do nothing
      }
    }

    protected void TransformMouseCoordinates(ref float x, ref float y)
    {
      _inverseFinalTransform.Transform(ref x, ref y);
    }

    public override void OnMouseMove(float x, float y)
    {
      float xTrans = x;
      float yTrans = y;
      TransformMouseCoordinates(ref xTrans, ref yTrans);
      if (ActualBounds.Contains(xTrans, yTrans))
      {
        if (!IsMouseOver)
        {
          IsMouseOver = true;
          FireEvent(MOUSEENTER_EVENT);
        }
        if (!HasFocus && IsInVisibleArea(xTrans, yTrans))
          TrySetFocus(false);
      }
      else
      {
        if (IsMouseOver)
        {
          IsMouseOver = false;
          FireEvent(MOUSELEAVE_EVENT);
        }
        if (HasFocus)
          HasFocus = false;
      }
      base.OnMouseMove(x, y);
    }

    public override bool IsInVisibleArea(float x, float y)
    {
      UIElement parent = VisualParent as UIElement;
      return parent != null ? parent.IsChildVisibleAt(this, x, y) : IsInArea(x, y);
    }

    public override bool IsInArea(float x, float y)
    {
      return x >= ActualPosition.X && x <= ActualPosition.X + ActualWidth &&
          y >= ActualPosition.Y && y <= ActualPosition.Y + ActualHeight;
    }

    #region Focus & control predicition

    #region Focus movement

    protected FrameworkElement GetFocusedElementOrChild()
    {
      FrameworkElement result = Screen == null ? null : Screen.FocusedElement;
      if (result == null)
        foreach (UIElement child in GetChildren())
        {
          result = child as FrameworkElement;
          if (result != null)
            break;
        }
      return result;
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to the first child element in the given
    /// direction.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocus1(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement nextElement = PredictFocus(currentElement.ActualBounds, direction);
      if (nextElement == null) return false;
      return nextElement.TrySetFocus(true);
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to our last child in the given
    /// direction. For example if <c>direction == MoveFocusDirection.Up</c>, this method tries to focus the
    /// topmost child.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocusN(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement nextElement;
      while ((nextElement = PredictFocus(currentElement.ActualBounds, direction)) != null)
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    #endregion

    /// <summary>
    /// Predicts the next control which is positioned in the specified direction
    /// <paramref name="dir"/> to the specified <paramref name="currentFocusRect"/> and
    /// which is able to get the focus.
    /// This method will search the control tree down starting with this element as root element.
    /// </summary>
    /// <param name="currentFocusRect">The borders of the currently focused control.</param>
    /// <param name="dir">Direction, the result control should be positioned relative to the
    /// currently focused control.</param>
    /// <returns>Framework element which should get the focus, or <c>null</c>, if this control
    /// tree doesn't contain an appropriate control. The returned control will be
    /// visible, focusable and enabled.</returns>
    public virtual FrameworkElement PredictFocus(RectangleF? currentFocusRect, MoveFocusDirection dir)
    {
      if (!IsVisible)
        return null;
      // Check if this control is a possible return value
      if (IsEnabled && Focusable)
        if (!currentFocusRect.HasValue ||
            (dir == MoveFocusDirection.Up && ActualPosition.Y < currentFocusRect.Value.Top) ||
            (dir == MoveFocusDirection.Down && ActualPosition.Y + ActualHeight > currentFocusRect.Value.Bottom) ||
            (dir == MoveFocusDirection.Left && ActualPosition.X < currentFocusRect.Value.Left) ||
            (dir == MoveFocusDirection.Right && ActualPosition.X + ActualWidth > currentFocusRect.Value.Right))
          return this;
      // Check child controls
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        FrameworkElement match = fe.PredictFocus(currentFocusRect, dir);
        if (match != null)
        {
          if (!currentFocusRect.HasValue)
            // If we don't have a comparison rect, simply return first match.
            return match;
          // Calculate and compare distances of all matches
          float centerDistance = CenterDistance(match.ActualBounds, currentFocusRect.Value);
          if (centerDistance == 0)
            // If the control's center is exactly the center of the currently focused element,
            // it won't be used as next focus element
            continue;
          float distance = BorderDistance(match.ActualBounds, currentFocusRect.Value);
          if (bestMatch == null || distance < bestDistance ||
              distance == bestDistance && centerDistance < bestCenterDistance)
          {
            bestMatch = match;
            bestDistance = distance;
            bestCenterDistance = centerDistance;
          }
        }
      }
      return bestMatch;
    }

    protected static float BorderDistance(RectangleF r1, RectangleF r2)
    {
      float distX;
      float distY;
      if (r1.Left > r2.Right)
        distX = r1.Left - r2.Right;
      else if (r1.Right < r2.Left)
        distX = r2.Left - r1.Right;
      else
        distX = 0;
      if (r1.Top > r2.Bottom)
        distY = r1.Top - r2.Bottom;
      else if (r1.Bottom < r2.Top)
        distY = r2.Top - r1.Bottom;
      else
        distY = 0;
      return (float) Math.Sqrt(distX * distX + distY * distY);
    }

    protected static float CenterDistance(RectangleF r1, RectangleF r2)
    {
      float distX = Math.Abs((r1.Left + r1.Right) / 2 - (r2.Left + r2.Right) / 2);
      float distY = Math.Abs((r1.Top + r1.Bottom) / 2 - (r2.Top + r2.Bottom) / 2);
      return (float) Math.Sqrt(distX * distX + distY * distY);
    }

    #endregion

    public virtual void DoRender(RenderContext localRenderContext)
    {
    }

    public void RenderToTexture(Texture texture, RenderContext renderContext)
    {
      // TODO: Rework this: Add RenderTarget property to RenderContext, use standard Render method

      // We do the following here:
      // 1. Set the rendertarget to the given texture
      // 4. Render the control; since the rendertarget is the given texture, the control is rendered into the texture
      // 5. Restore the rendertarget to the backbuffer

      GraphicsDevice.Device.EndScene();

      // Get the current backbuffer
      using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
      {
        SurfaceDescription backbufferDesc = backBuffer.Description;
        // Get the surface of our opacity texture
        using (Surface renderTextureSurface = texture.GetSurfaceLevel(0))
        {
          // TODO: Can we leave out the step to copy the backbuffer and make our rendering effect use the opacity parameter
          // in the texture where our control was not rendered so the backbuffer isn't cleared with black?
          SurfaceDescription renderTextureDesc = renderTextureSurface.Description;
          // Copy the backbuffer to the render texture
          GraphicsDevice.Device.StretchRectangle(backBuffer,
              new Rectangle(0, 0, backbufferDesc.Width, backbufferDesc.Height),
              renderTextureSurface,
              new Rectangle(0, 0, renderTextureDesc.Width, renderTextureDesc.Height),
              TextureFilter.None);

          // Change the rendertarget to the opacitytexture
          GraphicsDevice.Device.SetRenderTarget(0, renderTextureSurface);

          // Render the control (will be rendered into the given texture)
          GraphicsDevice.Device.BeginScene();
          DoRender(renderContext);
          GraphicsDevice.Device.EndScene();

          // Restore the backbuffer
          GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
        }
      }
    }

    public override void Render(RenderContext parentRenderContext)
    {
      if (!IsVisible)
        return;

      RectangleF bounds = ActualBounds;
      if (bounds.Width <= 0 || bounds.Height <= 0)
        return;

      Matrix? layoutTransformMatrix = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      Matrix? renderTransformMatrix = RenderTransform == null ? new Matrix?() : RenderTransform.GetTransform();

      RenderContext localRenderContext = parentRenderContext.Derive(bounds, layoutTransformMatrix,
          renderTransformMatrix, RenderTransformOrigin, Opacity);
      _inverseFinalTransform = Matrix.Invert(localRenderContext.Transform);

      if (OpacityMask == null)
        // Simply render without opacity mask
        DoRender(localRenderContext);
      else
      { // Control has an opacity mask
        // TODO: Rework this: Add RenderTarget property to RenderContext, simplify rendering opacity brushes

        // What we do here is that
        // 1. Create an opacity texture which has the same dimensions as the control
        // 2. Copy the part of the current backbuffer where the control is rendered to the opacity texture
        // 3. Render the control into the opacity texture
        // 4. Render the opacitytexture using the opacitymask brush
        UpdateOpacityMask(localRenderContext.ZOrder);
        RenderToTexture(_opacityMaskContext.Texture, localRenderContext);

        // Now render the opacitytexture with the OpacityMask brush
        GraphicsDevice.Device.BeginScene();
        OpacityMask.BeginRenderOpacityBrush(_opacityMaskContext.Texture, parentRenderContext); // Not: localRenderContext! All transforms + Opacity have been applied in method RenderToTexture
        GraphicsDevice.Device.VertexFormat = _opacityMaskContext.VertexFormat;
        GraphicsDevice.Device.SetStreamSource(0, _opacityMaskContext.VertexBuffer, 0, _opacityMaskContext.StrideSize);
        GraphicsDevice.Device.DrawPrimitives(_opacityMaskContext.PrimitiveType, 0, 2);
        OpacityMask.EndRender();

        _opacityMaskContext.LastTimeUsed = SkinContext.FrameRenderingStartTime;
      }
    }

    #region Opacitymask

    /// <summary>
    /// Updates the opacity mask texture.
    /// </summary>
    /// <param name="zPos">Z position of this element.</param>
    void UpdateOpacityMask(float zPos)
    {
      if (!_updateOpacityMask)
        return;
      _updateOpacityMask = false;
      if (_opacityMaskContext != null)
      {
        _opacityMaskContext.Free(false);
        _opacityMaskContext = null;
      }
      if (OpacityMask == null)
        return;

      PositionColored2Textured[] verts = new PositionColored2Textured[6];

      Color4 col = ColorConverter.FromColor(Color.White);
      col.Alpha *= (float) Opacity;
      int color = col.ToArgb();

      // Upper left
      verts[0].X = 0;
      verts[0].Y = 0;
      verts[0].Color = color;
      verts[0].Tu1 = 0;
      verts[0].Tv1 = 0;
      verts[0].Z = zPos;

      // Bottom left
      verts[1].X = 0;
      verts[1].Y = SkinContext.SkinResources.SkinHeight;
      verts[1].Color = color;
      verts[1].Tu1 = 0;
      verts[1].Tv1 = 1;
      verts[1].Z = zPos;

      // Bottom right
      verts[2].X = SkinContext.SkinResources.SkinWidth;
      verts[2].Y = SkinContext.SkinResources.SkinHeight;
      verts[2].Color = color;
      verts[2].Tu1 = 1;
      verts[2].Tv1 = 1;
      verts[2].Z = zPos;

      // Upper left
      verts[3].X = 0;
      verts[3].Y = 0;
      verts[3].Color = color;
      verts[3].Tu1 = 0;
      verts[3].Tv1 = 0;
      verts[3].Z = zPos;

      // Upper right
      verts[4].X = SkinContext.SkinResources.SkinWidth;
      verts[4].Y = 0;
      verts[4].Color = color;
      verts[4].Tu1 = 1;
      verts[4].Tv1 = 0;
      verts[4].Z = zPos;

      // Bottom right
      verts[5].X = SkinContext.SkinResources.SkinWidth;
      verts[5].Y = SkinContext.SkinResources.SkinHeight;
      verts[5].Color = color;
      verts[5].Tu1 = 1;
      verts[5].Tv1 = 1;
      verts[5].Z = zPos;

      _opacityMaskContext = new VisualAssetContext("FrameworkElement.OpacityMaskContext:" + Name, Screen.Name,
          verts, PrimitiveType.TriangleList, new Texture(GraphicsDevice.Device,
              SkinContext.SkinResources.SkinWidth, SkinContext.SkinResources.SkinHeight, 1,
              Usage.RenderTarget, Format.X8R8G8B8, Pool.Default));
      ContentManager.Add(_opacityMaskContext);

      OpacityMask.SetupBrush(ref verts, zPos);
      PositionColored2Textured.Set(_opacityMaskContext.VertexBuffer, verts);
    }

    #endregion

    public override void Allocate()
    {
      base.Allocate();
      if (_opacityMaskContext != null)
        ContentManager.Add(_opacityMaskContext);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_opacityMaskContext != null)
      {
        _opacityMaskContext.Free(true);
        ContentManager.Remove(_opacityMaskContext);
        _opacityMaskContext = null;
      }
    }
  }
}
