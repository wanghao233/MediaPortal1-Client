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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    #region Protected fields

    protected Property _selectionChangedProperty;

    #endregion

    #region Ctor

    public ListView()
    {
      Init();
    }

    void Init()
    {
      _selectionChangedProperty = new Property(typeof(ICommandStencil), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ListView lv = source as ListView;
      SelectionChanged = copyManager.GetCopy(lv.SelectionChanged);
    }

    #endregion

    #region Events

    public Property SelectionChangedProperty
    {
      get { return _selectionChangedProperty; }
    }

    public ICommandStencil SelectionChanged
    {
      get { return (ICommandStencil)_selectionChangedProperty.GetValue(); }
      set { _selectionChangedProperty.SetValue(value); }
    }

    #endregion

    #region Input handling

    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      UpdateCurrentItem();
    }

    public override void OnKeyPressed(ref Key key)
    {
      UpdateCurrentItem();
      base.OnKeyPressed(ref key);
    }

    void UpdateCurrentItem()
    {
      UIElement element = FindElement(FocusFinder.Instance);
      if (element == null)
        CurrentItem = null;
      else
      {
        // FIXME Albert78: This does not necessarily find the right ListViewItem
        while (!(element is ListViewItem) && element.VisualParent != null)
          element = element.VisualParent as UIElement;
        CurrentItem = element.Context;
      }
      if (SelectionChanged != null)
        SelectionChanged.Execute(new object[] { CurrentItem });
    }

    #endregion

    protected override UIElement PrepareItemContainer(object dataItem)
    {
      ListViewItem container = new ListViewItem();
      container.Style = ItemContainerStyle;
      container.Context = dataItem;
      container.ContentTemplate = ItemTemplate;
      container.ContentTemplateSelector = ItemTemplateSelector;
      container.Content = (FrameworkElement)ItemTemplate.LoadContent();
      container.VisualParent = _itemsHostPanel;
      return container;
    }
  }
}
