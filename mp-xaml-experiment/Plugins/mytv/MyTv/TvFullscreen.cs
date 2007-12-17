using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dialogs;
using TvDatabase;
using TvControl;
using ProjectInfinity;
using ProjectInfinity.Players;
using ProjectInfinity.Logging;
using ProjectInfinity.Localisation;
using ProjectInfinity.Navigation;
using ProjectInfinity.Controls;
namespace MyTv
{
  /// <summary>
  /// Interaction logic for TvFullscreen.xaml
  /// </summary>

  public partial class TvFullscreen : View
  {

    TvFullScreenModel _model;
    /// <summary>
    /// Initializes a new instance of the <see cref="TvFullscreen"/> class.
    /// </summary>
    public TvFullscreen()
    {
      this.Loaded+=new RoutedEventHandler(TvFullscreen_Loaded);
      this.Unloaded += new RoutedEventHandler(TvFullscreen_Unloaded);
    }

    void TvFullscreen_Unloaded(object sender, RoutedEventArgs e)
    {
      _model.Dispose();
      _model = null;
    }

    void TvFullscreen_Loaded(object sender, RoutedEventArgs e)
    {
      _model = new TvFullScreenModel();
      DataContext = _model;
      this.InputBindings.Add(new KeyBinding(_model.FullScreen, new KeyGesture(System.Windows.Input.Key.Enter, ModifierKeys.Alt)));
      this.InputBindings.Add(new KeyBinding(NavigationCommands.BrowseBack, new KeyGesture(System.Windows.Input.Key.Escape)));
      
      this.Background = _model.VideoBrush;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
      if (e.Key == Key.Up)
      {
        _model.OnChannelUp();
        e.Handled = true;
        return;
      }
      if (e.Key == Key.Down)
      {
        _model.OnChannelDown();
        e.Handled = true;
        return;
      }
      if (e.Key >= Key.D0 && e.Key <= Key.D9)
      {
        _model.OnChannelKey(e.Key);
        e.Handled = true;
        return;
      }
      if (e.Key == Key.Left || e.Key == Key.Right)
      {
        _model.OnSeek(e.Key);
        e.Handled = true;
        return;
      }
      if (e.Key == Key.Escape || e.Key == Key.X)
      {
        //return to previous screen
        e.Handled = true;
        ServiceScope.Get<INavigationService>().GoBack();
        return;
      }
      if (e.Key == Key.Space)
      {
        e.Handled = true;
        _model.Pause();
        return;
      }
    }

  }
}