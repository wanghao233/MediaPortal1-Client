#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace MediaPortal.DeployTool
{
  class MediaPortalChecker: IInstallationPackage
  {
    public string GetDisplayName()
    {
      return "MediaPortal";
    }

    public bool Download()
    {
        string prg = "MediaPortal";
        DialogResult result;
        result = Utils.DownloadFile(prg);
        FileInfo info = new FileInfo(Application.StartupPath + "\\deploy\\" + Utils.GetDownloadFile(prg));

        for (int i = 0; i < 5; i++)
        {
            if (info.Length < 100000)
                result = Utils.DownloadFile(prg);
            else
                break;
        }
        return (result == DialogResult.OK);
    }
    public bool Install()
    {
      string nsis = Application.StartupPath + "\\deploy\\" + Utils.GetDownloadFile("MediaPortal");
      string targetDir = InstallationProperties.Instance["MPDir"];
      Process setup = Process.Start(nsis, "/S /D=" + targetDir);
      try
      {
          setup.WaitForExit();
      }
      catch { }
      return true;
    }
    public bool UnInstall()
    {
      RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + InstallationProperties.Instance["RegistryKeyAdd"] + "Microsoft\\Windows\\CurrentVersion\\Uninstall\\MediaPortal");
      if (key == null)
      {
          return false;
      }
      key.Close();
      Process setup = Process.Start((string)key.GetValue("UninstallString"));
      try
      {
          setup.WaitForExit();
      }
      catch { }
      return true;
    }
    public CheckResult CheckStatus()
    {
      CheckResult result;
      result.needsDownload = !File.Exists(Application.StartupPath + "\\deploy\\" + Utils.GetDownloadFile("MediaPortal"));
      if (InstallationProperties.Instance["InstallType"] == "download_only")
      {
          if (result.needsDownload == false)
              result.state = CheckState.DOWNLOADED;
          else
              result.state = CheckState.NOT_DOWNLOADED;
          return result;
      }
      RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + InstallationProperties.Instance["RegistryKeyAdd"] + "Microsoft\\Windows\\CurrentVersion\\Uninstall\\MediaPortal");
      if (key == null)
      {
          result.state = CheckState.NOT_INSTALLED;
      }
      else
      {
          string MpPath = (string)key.GetValue("UninstallString");
          key.Close();    
          if (MpPath == null | !File.Exists(MpPath))
              result.state = CheckState.NOT_INSTALLED;
          else
              result.state = CheckState.INSTALLED;
          /*
          string version = (string)key.GetValue("DisplayVersion");
          key.Close();
          if (version == "0.9.3.0")
            result.state = CheckState.INSTALLED;
          else
            result.state = CheckState.VERSION_MISMATCH;
          */
      }
      return result;
    }
  }
}
