using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MediaPortal.GUI.Library;

namespace MediaPortal.Profile
{
  public class CacheSettingsProvider : ISettingsProvider
  {
    Dictionary<SettingKey, object> cache = new Dictionary<SettingKey, object>();
    ISettingsProvider provider;
    private int cacheHit = 0;
    private int cacheMiss = 0;

    #region class SettingKey
    private class SettingKey
    {
      private string section;
      private string entry;

      public SettingKey(string section, string entry)
      {
        this.entry = entry;
        this.section = section;
      }

      public string Entry
      {
        get { return entry; }
        set { entry = value; }
      }

      public string Section
      {
        get { return section; }
        set { section = value; }
      }

      public override int GetHashCode()
      {
        return section.GetHashCode() ^ entry.GetHashCode();
      }

      public override bool Equals(object obj)
      {
        SettingKey other = (SettingKey)obj;
        if (this.entry == other.entry && this.section == other.section)
          return true;
        else
          return false;
      }
    }
    #endregion

    public CacheSettingsProvider(ISettingsProvider provider)
    {
      this.provider = provider;

      if (provider is ISettingsPrefetchable)
      {
        ISettingsPrefetchable prefetcher = (ISettingsPrefetchable)provider;
        prefetcher.Prefetch(Remember);
      }
    }

    private void Remember(string section, string entry, object value)
    {
      SettingKey key = new SettingKey(section, entry);
      cache.Add(key, value);
    }

    public string FileName
    {
      get { return provider.FileName; }
    }

    public object GetValue(string section, string entry)
    {
      SettingKey key = new SettingKey(section, entry);

      object obj;
      if (!cache.TryGetValue(key, out obj))
      {
        cacheMiss++;
        obj = provider.GetValue(section, entry);
        cache.Add(key, obj);
      }
      else
      {
        cacheHit++;
      }
      return obj;
    }

    public void RemoveEntry(string section, string entry)
    {
      SettingKey key = new SettingKey(section, entry);
      cache.Remove(key);
      provider.RemoveEntry(section, entry);
    }

    public void Save()
    {
      provider.Save();
    }

    public void SetValue(string section, string entry, object value)
    {
      SettingKey key = new SettingKey(section, entry);
      cache.Remove(key);
      cache.Add(key, value);
      provider.SetValue(section, entry, value);
    }

#if DEBUG
    ~CacheSettingsProvider()
    { 
      Log.Write("Filename: {0} Cachehit: {1} Cachemiss: {2}", provider.FileName, cacheHit, cacheMiss);
    }
#endif
  }
}
