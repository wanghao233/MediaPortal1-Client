﻿using System;
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
using ProjectInfinity.Logging;
using ProjectInfinity.Localisation;
using ProjectInfinity.Navigation;
using ProjectInfinity;
using ProjectInfinity.Settings;
using Dialogs;

namespace MyWeather
{
    /// <summary>
    /// ViewModel Class for Weather.xaml
    /// </summary>
    public class WeatherViewModel : INotifyPropertyChanged
    {
        #region variables
        Window _window;
        Page _page;
        WeatherDataModel _dataModel;
        City _currCity;
        ICommand _updateWeatherCommand;
        ICommand _changeLocationCommand;
        List<City> _availableLocations;
        WeatherLocalizer _locals;

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherViewModel"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        public WeatherViewModel(Page page)
        {

            //store page & window
            _page = page;
            _window = Window.GetWindow(_page);
            // create localisation instance
            _locals = new WeatherLocalizer();
            // create the datamodel :)
            _dataModel = new WeatherDataModel(new WeatherDotComCatcher("configuration.xml"));
            // load locations
            LoadAvailableLocations();
            // get the last set city from configuration
            WeatherSettings settings = new WeatherSettings();
            ServiceScope.Get<ISettingsManager>().Load(settings);
            foreach(City c in AvailableLocations)
            {
                if (c.Id.Equals(settings.LocationCode))
                {
                    // okay, we found it, so let's set it
                    CurrentLocation = c;
                    break;
                }
            }
        }
        #endregion

        /// <summary>
        /// load all configured locations from settings
        /// </summary>
        public void LoadAvailableLocations()
        {
            _availableLocations = _dataModel.LoadLocationsData();
            ChangeProperty("AvailableLocations");
            ChangeProperty("CurrentLocation");
        }

        #region properties

        /// <summary>
        /// Notifies subscribers that property has been changed
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public void ChangeProperty(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        #region properties of the current location
        /// <summary>
        /// Gets the current Location as Typed City object 
        /// </summary>
        /// <value>current location</value>
        public City CurrentLocation
        {
            get
            {
                return _currCity;
            }
            set
            {
                _currCity = value;
                ChangeProperty("CurrentLocation");
            }
        }

        /// <summary>
        /// Gets the all availbe locations
        /// </summary>
        /// <value>list of locations</value>
        public List<City> AvailableLocations
        {
            get
            {
                return _availableLocations;
            }
        }

        /// <summary>
        /// Gets localized versions of the labels
        /// </summary>
        /// <value>WeatherLocalizer object</value>
        public WeatherLocalizer Localisation
        {
            get
            {
                return _locals;
            }
        }
        #endregion

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>The window.</value>
        public Window Window
        {
            get
            {
                return _window;
            }
        }
        /// <summary>
        /// Gets the current Page.
        /// </summary>
        /// <value>The page.</value>
        public Page Page
        {
            get
            {
                return _page;
            }
            set
            {
                _page = value;
                _window = Window.GetWindow(_page);
            }
        }

        #region commands
        /// <summary>
        /// Returns a ICommand for updating the Weather
        /// </summary>
        /// <value>The command.</value>
        public ICommand UpdateWeather
        {
            get
            {
                if (_updateWeatherCommand == null)
                {
                    _updateWeatherCommand = new UpdateWeatherCommand(this, _dataModel);
                }
                return _updateWeatherCommand;
            }
        }

        /// <summary>
        /// Returns a ICommand for updating the Location
        /// </summary>
        /// <value>The command.</value>
        public ICommand ChangeLocation
        {
            get
            {
                if (_changeLocationCommand == null)
                {
                    _changeLocationCommand = new ChangeLocationCommand(this, _dataModel);
                }
                return _changeLocationCommand;
            }
        }
        #endregion

        #region Commands subclasses

        #region base command class

        /// <summary>
        /// This is the Basecommand class for
        /// My Weather
        /// </summary>
        public abstract class WeatherBaseCommand : ICommand
        {
            protected WeatherViewModel _viewModel;
            protected WeatherDataModel _dataModel;
            public event EventHandler CanExecuteChanged;

            public WeatherBaseCommand(WeatherViewModel viewModel, WeatherDataModel dataModel)
            {
                _viewModel = viewModel;
                _dataModel = dataModel;
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

        #region ChangeLocationCommand  class
        /// <summary>
        /// ChangeLocationCommand will set a new location
        /// </summary> 
        public class ChangeLocationCommand : WeatherBaseCommand
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LocationChangedCommand"/> class.
            /// </summary>
            /// <param name="viewModel">The view model.</param>
            public ChangeLocationCommand(WeatherViewModel viewModel, WeatherDataModel dataModel)
                : base(viewModel, dataModel)
            {
            }

            /// <summary>
            /// Executes the command.
            /// </summary>
            /// <param name="parameter">The parameter.</param>
            public override void Execute(object parameter)
            {
                // update weather data for new location and update labels go in here
                if (_viewModel.AvailableLocations.Count == 0) return;
                MpMenu menu = new MpMenu();
                menu.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                menu.Owner = _viewModel.Window;
                menu.Items.Clear();
                menu.Header = ServiceScope.Get<ILocalisation>().ToString("myweather.config", 3);// "Please select your City";
                menu.SubTitle = "";
                // add items to the menu
                foreach (City c in _viewModel.AvailableLocations)
                {
                    menu.Items.Add(new DialogMenuItem(c.Name));
                }

                menu.ShowDialog();

                if (menu.SelectedIndex < 0) return;    // no menu item selected

                // get the id that belongs to the selected city and set the property
                _viewModel.CurrentLocation = ((List<City>)(_viewModel.AvailableLocations))[menu.SelectedIndex];
                // save the selected location code to settings
                WeatherSettings settings = new WeatherSettings();
                ServiceScope.Get<ISettingsManager>().Load(settings);
                settings.LocationCode = _viewModel.CurrentLocation.Id;
                ServiceScope.Get<ISettingsManager>().Save(settings);
            }
        }
        #endregion

        #region UpdateWeatherCommand  class
        /// <summary>
        /// UpdateWeatherCommand will fetch new weather data
        /// </summary> 
        public class UpdateWeatherCommand : WeatherBaseCommand
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LocationChangedCommand"/> class.
            /// </summary>
            /// <param name="viewModel">The view model.</param>
            public UpdateWeatherCommand(WeatherViewModel viewModel, WeatherDataModel dataModel)
                : base(viewModel,dataModel)
            {
            }

            /// <summary>
            /// Executes the command.
            /// </summary>
            /// <param name="parameter">The parameter.</param>
            public override void Execute(object parameter)
            {
                // update weather data and labels go in here
                _viewModel.LoadAvailableLocations();
            }
        }
        #endregion
        #endregion
        #endregion
    }
}

