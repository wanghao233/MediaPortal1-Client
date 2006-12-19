#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
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
using System.IO;

namespace MediaPortal.Support
{
  public class DxDiagLog : ILogCreator
  {
    private ProcessRunner runner;

    public DxDiagLog(ProcessRunner runner)
    {
      this.runner = runner;
    }

    public void CreateLogs(string destinationFolder)
    {
      string tmpFile = Environment.GetEnvironmentVariable("SystemDrive") + "\\_dxdiag.txt";
      string dstFile = destinationFolder + "\\dxdiag.txt";
      CreateDxDiagFile(tmpFile);
      if (File.Exists(dstFile)) 
        File.Delete(dstFile);
      File.Move(tmpFile, dstFile);
    }

    private void CreateDxDiagFile(string tmpFile)
    {
      string executable = Environment.GetEnvironmentVariable("windir") + @"\system32\dxdiag.exe";
      string arguments = "/whql:off /t " + tmpFile;
      runner.Arguments = arguments;
      runner.Executable = executable;
      runner.Run();
    }

    public string ActionMessage
    {
      get { return "Gathering DirectX diagnostics text file..."; }
    }
  }
}
