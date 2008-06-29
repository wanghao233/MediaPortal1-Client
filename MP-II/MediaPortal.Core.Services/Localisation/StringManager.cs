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

using System;
using System.Globalization;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Services.Localisation
{
  /// <summary>
  /// This class manages localisation strings.
  /// </summary>
  public class StringManager : ILocalisation
  {
    #region Variables
    private LocalisationStrings _strings;
    #endregion

    #region Constructors/Destructors
    public StringManager()
    {
      ServiceScope.Get<ILogger>().Debug("StringsManager: Loading Settings");
      RegionSettings settings = new RegionSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

      if (settings.Culture == string.Empty)
      {
        ServiceScope.Get<ILogger>().Info("StringsManager: Culture Not found in settings");
        _strings = new LocalisationStrings("Language", null);
        settings.Culture = _strings.CurrentCulture.Name;

        ServiceScope.Get<ILogger>().Info("StringsManager: Culture set to: " + _strings.CurrentCulture.Name);
        ServiceScope.Get<ISettingsManager>().Save(settings);
        ServiceScope.Get<ILogger>().Info("StringsManager: Saving settings");
      }
      else
      {
        ServiceScope.Get<ILogger>().Debug("StringsManager: Using culture: " + settings.Culture);
        _strings = new LocalisationStrings("Language", settings.Culture);
      }

      IQueue queue = ServiceScope.Get<IMessageBroker>().Get(PluginMessaging.Queue);
      queue.OnMessageReceive += new MessageReceivedHandler(OnPluginMessageReceive);
    }

    public StringManager(string directory, string cultureName)
    {
      _strings = new LocalisationStrings(directory, cultureName);
    }
    #endregion

    #region ILocalisation Implementation
    #region Events
    public event LanguageChangeHandler LanguageChange;
    #endregion

    #region proterties
    public CultureInfo CurrentCulture
    {
      get { return _strings.CurrentCulture; }
    }
    #endregion

    #region public methods
    public void ChangeLanguage(string cultureName)
    {
      _strings.ChangeLanguage(cultureName);
      RegionSettings settings = new RegionSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      settings.Culture = cultureName;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      //send language change event
      LanguageChange(this);
    }

    public string ToString(string section, string name, object[] parameters)
    {
      return _strings.ToString(section, name, parameters);
    }

    public string ToString(string section, string name)
    {
      return _strings.ToString(section, name);
    }

    public string ToString(StringId id)
    {
      return _strings.ToString(id.Section, id.Name);
    }

    public bool IsLocaleSupported(string cultureName)
    {
      return _strings.IsLocaleSupported(cultureName);
    }

    public CultureInfo[] AvailableLanguages()
    {
      return _strings.AvailableLanguages();
    }

    public CultureInfo GetBestLanguage()
    {
      return _strings.GetBestLanguage();
    }

    public void AddDirectory(string stringsDirectory)
    {
      _strings.AddDirectory(stringsDirectory);
    }
    #endregion
    #endregion

    #region Event Handlers
    /// <summary>
    /// Called when [plugin message is received].
    /// Adds Plugin language resource folders to the Directory list when plugins are enabled.
    /// </summary>
    /// <param name="message">The message.</param>
    private void OnPluginMessageReceive(MPMessage message)
    {
      try
      {
        if (((PluginMessaging.NotificationType)message.MetaData[PluginMessaging.Notification]) == PluginMessaging.NotificationType.OnPluginEnable)
        {
          if (message.MetaData.ContainsKey(PluginMessaging.Resources))
          {
            foreach (PluginResource resource in (List<PluginResource>)message.MetaData[PluginMessaging.Resources])
            {
              if (resource.Type == PluginResource.ResourceType.Language)
                AddDirectory(resource.Location);
            }
          }
        }
      }
      catch (Exception)
      {
        ServiceScope.Get<ILogger>().Error("StringsManager: Error in Plugin Message");
      }
    }
    #endregion
  }
}
