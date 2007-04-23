using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using ProjectInfinity.Navigation;
using ProjectInfinity.Settings;

namespace ProjectInfinity.Pictures
{
  public class PictureViewModel : INotifyPropertyChanged
  {
    private readonly PictureSettings settings;
    private readonly List<MediaItem> _model = new List<MediaItem>();
    private LaunchCommand _launchCommand;
    private CollectionView _itemView;
    private Folder _currentFolder;

    public PictureViewModel()
    {
      ISettingsManager settingMgr = ServiceScope.Get<ISettingsManager>();
      settings = new PictureSettings();
      settingMgr.Load(settings);
      //we save the settings here, to make sure they are in the configuration file.
      //because this plugin has no setup yet
      //TODO: remove saving settings here
      settingMgr.Save(settings);
      Reload(new Folder(new DirectoryInfo(settings.PictureFolders[0])), false);
    }

    #region INotifyPropertyChanged Members

    ///<summary>
    ///Occurs when a property value changes.
    ///</summary>
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    public Folder CurrentFolder
    {
      get { return _currentFolder; }
    }

    private void Reload(Folder dir, bool includeParent)
    {
      DirectoryInfo directoryInfo = dir.Info;
      DirectoryInfo parentInfo = directoryInfo.Parent;
      FileSystemInfo[] entries = directoryInfo.GetFileSystemInfos();
      MediaFactory factory = new MediaFactory(settings);
      _currentFolder = dir;
      OnPropertyChanged(new PropertyChangedEventArgs("CurrentFolder"));
      _model.Clear();
      if (includeParent && parentInfo != null)
      {
        _model.Add(new ParentFolder(parentInfo));
      }
      foreach (FileSystemInfo entry in entries)
      {
        MediaItem item = factory.Create(entry);
        if (item == null)
        {
          continue;
        }
        _model.Add(item);
      }
      Items = new CollectionView(_model);
    }

    public CollectionView Items
    {
      get { return _itemView; }
      private set
      {
        _itemView = value;
        OnPropertyChanged(new PropertyChangedEventArgs("Items"));
      }
    }

    public ICommand Launch
    {
      get
      {
        if (_launchCommand == null)
        {
          _launchCommand = new LaunchCommand(this);
        }
        return _launchCommand;
      }
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, e);
      }
    }

    private class LaunchCommand : ICommand, IMediaVisitor
    {
      private readonly PictureViewModel _viewModel;

      public LaunchCommand(PictureViewModel viewModel)
      {
        _viewModel = viewModel;
      }

      #region ICommand Members

      ///<summary>
      ///Defines the method that determines whether the command can execute in its current state.
      ///</summary>
      ///
      ///<returns>
      ///true if this command can be executed; otherwise, false.
      ///</returns>
      ///
      ///<param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
      public bool CanExecute(object parameter)
      {
        return true;
      }

      ///<summary>
      ///Occurs when changes occur which affect whether or not the command should execute.
      ///</summary>
      public event EventHandler CanExecuteChanged;

      ///<summary>
      ///Defines the method to be called when the command is invoked.
      ///</summary>
      ///
      ///<param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
      public void Execute(object parameter)
      {
        MediaItem item = _viewModel.Items.CurrentItem as MediaItem;
        if (item == null)
        {
          return;
        }
        item.Accept(this); //GOF Visitor Pattern
      }

      #endregion

      #region IMediaVisitor Members

      public void Visit(Folder folder)
      {
        _viewModel.Reload(folder, true);
      }

      public void Visit(Picture picture)
      {
        ServiceScope.Get<INavigationService>().Navigate(new FullScreenPictureView(_viewModel));
      }

      #endregion
    }
  }

  internal class MediaFactory
  {
    private readonly PictureSettings _settings;

    public MediaFactory(PictureSettings settings)
    {
      _settings = settings;
    }

    public MediaItem Create(FileSystemInfo fileSystemInfo)
    {
      if (!fileSystemInfo.Exists)
      {
        return null;
      }
      FileInfo file = fileSystemInfo as FileInfo;
      if (file != null)
      {
        return Create(file);
      }
      DirectoryInfo directory = fileSystemInfo as DirectoryInfo;
      if (directory != null)
      {
        return Create(directory);
      }
      return null;
    }

    private MediaItem Create(FileInfo fileInfo)
    {
      if (_settings.Extensions.Contains(fileInfo.Extension.ToLower()))
      {
        return new Picture(fileInfo);
      }
      return null;
    }

    private static MediaItem Create(DirectoryInfo directoryInfo)
    {
      return new Folder(directoryInfo);
    }
  }
}