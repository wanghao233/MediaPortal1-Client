using System;
using System.Collections;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TvDatabase;
using TvControl;
using Dialogs;
using ProjectInfinity;
using ProjectInfinity.Players;
using ProjectInfinity.Logging;
using ProjectInfinity.Localisation;
using ProjectInfinity.Navigation;


namespace MyTv
{
  /// <summary>
  /// View Model for the recorded tv GUI
  /// </summary>
  public class TvRecordedViewModel : TvBaseViewModel
  {
    #region variables and enums
    /// <summary>
    /// Different ways the recordings view can be sorted
    /// </summary>
    public enum SortType
    {
      Duration,
      Channel,
      Date,
      Title,
      Genre,
      Watched
    };

    /// <summary>
    /// Different views on the recordings
    /// </summary>
    public enum ViewType
    {
      List,
      Icon
    };
    ICommand _sortCommand;
    ICommand _viewCommand;
    ICommand _cleanUpCommand;
    ICommand _deleteCommand;
    ICommand _contextMenuCommand;
    ICommand _keepUntilCommand;
    ViewType _viewMode = ViewType.Icon;
    RecordingCollectionView _recordingView;
    RecordingDatabaseModel _dataModel;

    #endregion

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="TvRecordedViewModel"/> class.
    /// </summary>
    /// <param name="page">The page.</param>
    public TvRecordedViewModel()
    {
      //create a new data model
      _dataModel = new RecordingDatabaseModel();
    }
    #endregion

    public override void Refresh()
    {
      base.Refresh();
      _dataModel.Reload();
      ChangeProperty("Recordings");
    }

    #region properties
    /// <summary>
    /// Gets the data model.
    /// </summary>
    /// <value>The data model.</value>
    public RecordingDatabaseModel DataModel
    {
      get
      {
        return _dataModel;
      }
    }

    /// <summary>
    /// Returns the ListViewCollection containing the recordings
    /// </summary>
    /// <value>The recordings.</value>
    public CollectionView Recordings
    {
      get
      {
        if (_recordingView == null)
        {
          _recordingView = new RecordingCollectionView(_dataModel);
        }
        return _recordingView;
      }
    }
    /// <summary>
    /// Gets or sets the view mode.
    /// </summary>
    /// <value>The view mode.</value>
    public ViewType ViewMode
    {
      get
      {
        return _viewMode;
      }
      set
      {
        if (_viewMode != value)
        {
          _viewMode = value;
          ChangeProperty("ViewModeType");
        }
      }
    }
    public string ViewModeType
    {
      get
      {
        switch( _viewMode)
        {
          case ViewType.Icon:
            return "Icon";
          default:
            return "List";
        }
      }
    }
    /// <summary>
    /// Gets the localized-label for the Switch button
    /// </summary>
    /// <value>The localized label.</value>
    public string SwitchLabel
    {
      get
      {
        return ServiceScope.Get<ILocalisation>().ToString("mytv", 81);//Switch
      }
    }
    /// <summary>
    /// Gets the localized-label for the Cleanup button
    /// </summary>
    /// <value>The localized label.</value>
    public string CleanupLabel
    {
      get
      {
        return ServiceScope.Get<ILocalisation>().ToString("mytv", 82);//Cleanup
      }
    }
    /// <summary>
    /// Gets the localized-label for the Compress button
    /// </summary>
    /// <value>The localized label.</value>
    public string CompressLabel
    {
      get
      {
        return ServiceScope.Get<ILocalisation>().ToString("mytv", 83);//Compress
      }
    }
    /// <summary>
    /// Gets the localized-label for the header
    /// </summary>
    /// <value>The localized label.</value>
    public override string HeaderLabel
    {
      get
      {
        return ServiceScope.Get<ILocalisation>().ToString("mytv", 78);//recorded
      }
    }
    /// <summary>
    /// Gets the localized-label for the Sort button
    /// </summary>
    /// <value>The localized label.</value>
    public string SortLabel
    {
      get
      {

        switch (_recordingView.SortMode)
        {
          case SortType.Channel:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 84);//"Sort:Channel";
          case SortType.Date:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 85);//"Sort:Date";
          case SortType.Duration:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 86);//"Sort:Duration";
          case SortType.Genre:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 87);//"Sort:Genre";
          case SortType.Title:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 88);//"Sort:Title";
          case SortType.Watched:
            return ServiceScope.Get<ILocalisation>().ToString("mytv", 89);//"Sort:Watched";
        }
        return "";
      }
    }
    /// <summary>
    /// Gets the localized-label for the View button
    /// </summary>
    /// <value>The localized label.</value>
    public string ViewLabel
    {
      get
      {
        return ServiceScope.Get<ILocalisation>().ToString("mytv", 79);//"View";
      }
    }

    #endregion

    #region commands
    /// <summary>
    /// Returns a ICommand for sorting
    /// </summary>
    /// <value>The command.</value>
    public ICommand Sort
    {
      get
      {
        if (_sortCommand == null)
        {
          _sortCommand = new SortCommand(this);
        }
        return _sortCommand;
      }
    }
    /// <summary>
    /// Returns a ICommand for changing the view mode.
    /// </summary>
    /// <value>The command.</value>
    public ICommand View
    {
      get
      {
        if (_viewCommand == null)
        {
          _viewCommand = new ViewCommand(this);
        }
        return _viewCommand;
      }
    }
    /// <summary>
    /// Returns a ICommand for cleaning up watched recordings
    /// </summary>
    /// <value>The command.</value>
    public ICommand CleanUp
    {
      get
      {
        if (_cleanUpCommand == null)
        {
          _cleanUpCommand = new CleanUpCommand(this);
        }
        return _cleanUpCommand;
      }
    }
    /// <summary>
    /// Returns a ICommand for deleting a recording
    /// </summary>
    /// <value>The command.</value>
    public ICommand Delete
    {
      get
      {
        if (_deleteCommand == null)
        {
          _deleteCommand = new DeleteCommand(this);
        }
        return _deleteCommand;
      }
    }
    /// <summary>
    /// Returns a ICommand for showing the context menu
    /// </summary>
    /// <value>The command.</value>
    public ICommand ContextMenu
    {
      get
      {
        if (_contextMenuCommand == null)
        {
          _contextMenuCommand = new ContextMenuCommand(this);
        }
        return _contextMenuCommand;
      }
    }
    /// <summary>
    /// Returns a ICommand for modifying the keep until date/time
    /// </summary>
    /// <value>The command.</value>
    public ICommand KeepUntil
    {
      get
      {
        if (_keepUntilCommand == null)
        {
          _keepUntilCommand = new KeepUntilCommand(this);
        }
        return _keepUntilCommand;
      }
    }
    #endregion

    #region Commands subclasses
    #region base command class
    public abstract class RecordedCommand : ICommand
    {
      protected TvRecordedViewModel _viewModel;
      public event EventHandler CanExecuteChanged;

      public RecordedCommand(TvRecordedViewModel viewModel)
      {
        _viewModel = viewModel;
      }

      public abstract void Execute(object parameter);

      public virtual bool CanExecute(object parameter)
      {
        return true;
      }

      protected void OnCanExecuteChanged()
      {
        if (this.CanExecuteChanged != null)
        {
          this.CanExecuteChanged(this, EventArgs.Empty);
        }
      }
    }
    #endregion

    #region sort command class
    /// <summary>
    /// SortCommand changes the way the view gets sorted
    /// </summary>
    public class SortCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="SortCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public SortCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        //show dialog menu with all sorting options
        MpMenu dlgMenu = new MpMenu();
        dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dlgMenu.Owner = _viewModel.Window;
        dlgMenu.Items.Clear();
        dlgMenu.Header = ServiceScope.Get<ILocalisation>().ToString("mytv", 68);// "Menu";
        dlgMenu.SubTitle = "";
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 97)/*Duration*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 2)/*Channel*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 73)/*Date*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 98)/*Title*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 99)/*Genre*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 100)/*Watched*/));
        RecordingCollectionView view = (RecordingCollectionView)_viewModel.Recordings;
        dlgMenu.SelectedIndex = (int)view.SortMode;
        dlgMenu.ShowDialog();
        if (dlgMenu.SelectedIndex < 0) return;//nothing selected

        //tell the view to sort
        view.SortMode = (TvRecordedViewModel.SortType)dlgMenu.SelectedIndex;

        //and tell the model that the sort property is changed
        _viewModel.ChangeProperty("SortLabel");
      }
    }
    #endregion

    #region view command class
    /// <summary>
    /// ViewCommand changes the way each listbox item gets displayed
    /// </summary>
    public class ViewCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="ViewCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public ViewCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        //change the viewmode
        switch (_viewModel.ViewMode)
        {
          case ViewType.Icon:
            _viewModel.ViewMode = ViewType.List;
            break;
          case ViewType.List:
            _viewModel.ViewMode = ViewType.Icon;
            break;
        }
      }
    }
    #endregion

    #region cleanup command class
    /// <summary>
    /// Cleanup command will delete recordings which have been watched
    /// </summary>
    public class CleanUpCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="CleanUpCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public CleanUpCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        MpDialogYesNo dlgMenu = new MpDialogYesNo();
        dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dlgMenu.Owner = _viewModel.Window;
        dlgMenu.Header = ServiceScope.Get<ILocalisation>().ToString("mytv", 68);// "Menu";
        dlgMenu.Content = ServiceScope.Get<ILocalisation>().ToString("mytv", 101);//"This will delete all recordings you have watched. Are you sure?";
        dlgMenu.ShowDialog();
        if (dlgMenu.DialogResult == DialogResult.No) return;
        IList itemlist = Recording.ListAll();
        foreach (Recording rec in itemlist)
        {
          if (rec.TimesWatched > 0)
          {
            _viewModel.DataModel.Delete(rec.IdRecording);
          }
        }
      }
    }
    #endregion

    #region Delete command class
    /// <summary>
    /// Delete command will delete a recoring
    /// </summary>
    public class DeleteCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="CleanUpCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public DeleteCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        Recording recording = parameter as Recording;
        MpDialogYesNo dlgMenu = new MpDialogYesNo();
        dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dlgMenu.Owner = _viewModel.Window;
        dlgMenu.Header = ServiceScope.Get<ILocalisation>().ToString("mytv", 68);//"Menu";
        dlgMenu.Content = ServiceScope.Get<ILocalisation>().ToString("mytv", 95);//"Are you sure to delete this recording ?";
        dlgMenu.ShowDialog();
        if (dlgMenu.DialogResult == DialogResult.No) return;

        _viewModel.DataModel.Delete(recording.IdRecording);
      }
    }
    #endregion

    #region ContextMenu command class
    /// <summary>
    /// ContextMenuCommand will show the context menu
    /// </summary> 
    public class ContextMenuCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="CleanUpCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public ContextMenuCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        RecordingModel item = parameter as RecordingModel;
        Recording recording = item.Recording;
        if (recording == null) return;
        MpMenu dlgMenu = new MpMenu();
        dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dlgMenu.Owner = _viewModel.Window;
        dlgMenu.Items.Clear();
        dlgMenu.Header = ServiceScope.Get<ILocalisation>().ToString("mytv", 68);//"Menu";
        dlgMenu.SubTitle = "";
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 92)/*Play recording*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 93)/*Delete recording*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 47)/*Keep until*/));
        dlgMenu.ShowDialog();
        if (dlgMenu.SelectedIndex < 0) return;//nothing selected
        switch (dlgMenu.SelectedIndex)
        {
          case 0:
            {
              ICommand command = _viewModel.Play;
              command.Execute(new PlayCommand.PlayParameter(recording.FileName, null, true));
            }
            break;

          case 1:
            {
              ICommand command = _viewModel.Delete;
              command.Execute(recording);
            }
            break;

          case 2:
            {
              ICommand command = _viewModel.KeepUntil;
              command.Execute(recording);
            }
            break;
        }
      }
    }
    #endregion

    #region KeepUntil command class
    /// <summary>
    /// KeepUntil command will KeepUntil a recoring
    /// </summary>
    public class KeepUntilCommand : RecordedCommand
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="CleanUpCommand"/> class.
      /// </summary>
      /// <param name="viewModel">The view model.</param>
      public KeepUntilCommand(TvRecordedViewModel viewModel)
        : base(viewModel)
      {
      }

      /// <summary>
      /// Executes the command.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      public override void Execute(object parameter)
      {
        Recording recording = parameter as Recording;
        if (recording == null) return;
        MpMenu dlgMenu = new MpMenu();
        dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dlgMenu.Owner = _viewModel.Window;
        dlgMenu.Items.Clear();
        dlgMenu.Header = ServiceScope.Get<ILocalisation>().ToString("mytv", 68);//"Menu";
        dlgMenu.SubTitle = ServiceScope.Get<ILocalisation>().ToString("mytv", 47);// Keep Until";
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 69)/*Until space needed*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 70)/*Until watched*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 71)/*Until Date*/));
        dlgMenu.Items.Add(new DialogMenuItem(ServiceScope.Get<ILocalisation>().ToString("mytv", 72)/*Always*/));
        dlgMenu.SelectedIndex = (int)recording.KeepUntil;
        dlgMenu.ShowDialog();
        if (dlgMenu.SelectedIndex < 0) return;//nothing selected
        recording.KeepUntil = dlgMenu.SelectedIndex;
        recording.Persist();
        if (dlgMenu.SelectedIndex == 2)
        {
          dlgMenu = new MpMenu();
          dlgMenu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
          dlgMenu.Owner = _viewModel.Window;
          dlgMenu.Items.Clear();
          dlgMenu.Header = "Menu";
          dlgMenu.SubTitle = "Date";
          int selected = 0;
          for (int days = 1; days <= 31; days++)
          {
            DateTime dt = DateTime.Now.AddDays(days);
            dlgMenu.Items.Add(new DialogMenuItem(dt.ToLongDateString()));
            if (dt.Date == recording.KeepUntilDate) selected = days - 1;
          }
          dlgMenu.ShowDialog();
          if (dlgMenu.SelectedIndex < 0) return;//nothing selected
          int daysChoosen = dlgMenu.SelectedIndex + 1;
          recording.KeepUntilDate = DateTime.Now.AddDays(daysChoosen);
          recording.Persist();
        }
      }
    }
    #endregion
    #endregion

    #region RecordingDatabaseModel class
    /// <summary>
    /// Class representing our database model.
    /// It simply retrieves all recordings from the tv database and 
    /// creates a list of RecordingModel
    /// </summary>
    public class RecordingDatabaseModel : INotifyPropertyChanged
    {
      #region variables
      public event PropertyChangedEventHandler PropertyChanged;
      List<RecordingModel> _listRecordings = new List<RecordingModel>();
      #endregion

      /// <summary>
      /// Initializes a new instance of the <see cref="RecordingDatabaseModel"/> class.
      /// </summary>
      public RecordingDatabaseModel()
      {
        Reload();
      }
      /// <summary>
      /// Refreshes the list with the database.
      /// </summary>
      public void Reload()
      {
        _listRecordings.Clear();
        if (false == ServiceScope.IsRegistered<ITvChannelNavigator>())
        {
          return;
        }
        if (false == ServiceScope.Get<ITvChannelNavigator>().IsInitialized)
        {
          return;
        }
        IList recordings = Recording.ListAll();

        foreach (Recording recording in recordings)
        {
          RecordingModel item = new RecordingModel(recording);
          _listRecordings.Add(item);
        }
        if (PropertyChanged != null)
        {
          PropertyChanged(this, new PropertyChangedEventArgs("Recordings"));
        }
      }

      public void Delete(int idRecording)
      {
        TvServer server = new TvServer();
        for (int i = 0; i < _listRecordings.Count; ++i)
        {
          if (_listRecordings[i].Recording.IdRecording == idRecording)
          {
            _listRecordings.RemoveAt(i);
            server.DeleteRecording(idRecording);
            if (PropertyChanged != null)
            {
              PropertyChanged(this, new PropertyChangedEventArgs("Recordings"));
            }
            break;
          }
        }
      }
      /// <summary>
      /// Gets the recordings.
      /// </summary>
      /// <value>IList containing 0 or more RecordingModel instances.</value>
      public IList Recordings
      {
        get
        {
          return _listRecordings;
        }
      }
    }
    #endregion

    #region RecordingCollectionView class
    /// <summary>
    /// This class represents the recording view
    /// </summary>
    class RecordingCollectionView : ListCollectionView
    {
      #region variables
      SortType _sortMode = SortType.Date;
      private RecordingDatabaseModel _model;
      #endregion

      /// <summary>
      /// Initializes a new instance of the <see cref="RecordingCollectionView"/> class.
      /// </summary>
      /// <param name="model">The database model.</param>
      public RecordingCollectionView(RecordingDatabaseModel model)
        : base(model.Recordings)
      {
        _model = model;
        _model.PropertyChanged += new PropertyChangedEventHandler(OnDatabaseChanged);
      }

      void OnDatabaseChanged(object sender, PropertyChangedEventArgs e)
      {
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
      }

      /// <summary>
      /// Gets or sets the sort mode.
      /// </summary>
      /// <value>The sort mode.</value>
      public SortType SortMode
      {
        get
        {
          return _sortMode;
        }
        set
        {
          if (_sortMode != value)
          {
            _sortMode = value;
            this.CustomSort = new RecordingComparer(_sortMode);
          }
        }
      }
    }
    #endregion

    #region RecordingComparer class
    /// <summary>
    /// Helper class to compare 2 RecordingModels
    /// </summary>
    public class RecordingComparer : IComparer
    {
      #region variables
      SortType _sortMode;
      #endregion

      /// <summary>
      /// Initializes a new instance of the <see cref="RecordingComparer"/> class.
      /// </summary>
      /// <param name="sortMode">The sort mode.</param>
      public RecordingComparer(SortType sortMode)
      {
        _sortMode = sortMode;
      }
      /// <summary>
      /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
      /// </summary>
      /// <param name="x">The first object to compare.</param>
      /// <param name="y">The second object to compare.</param>
      /// <returns>
      /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
      /// </returns>
      /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
      public int Compare(object x, object y)
      {
        RecordingModel model1 = (RecordingModel)x;
        RecordingModel model2 = (RecordingModel)y;
        switch (_sortMode)
        {
          case SortType.Channel:
            return String.Compare(model1.Channel, model2.Channel, true);
          case SortType.Date:
            return model1.StartTime.CompareTo(model2.StartTime);
          case SortType.Duration:
            TimeSpan t1 = model1.EndTime - model1.StartTime;
            TimeSpan t2 = model2.EndTime - model2.StartTime;
            return t1.CompareTo(t2);
          case SortType.Genre:
            return String.Compare(model1.Genre, model2.Genre, true);
          case SortType.Title:
            return String.Compare(model1.Title, model2.Title, true);
          case SortType.Watched:
            return model1.TimesWatched.CompareTo(model2.TimesWatched);
        }
        return 0;
      }
    }
    #endregion

  }
}
