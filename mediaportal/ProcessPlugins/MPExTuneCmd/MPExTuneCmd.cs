using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Threading;
using System.Text;
using System.IO;
using Microsoft.Win32;

using MediaPortal.GUI.Library;

namespace MediaPortal.MPExTuneCmd
{
	/// <summary>
	/// Summary description for MPExTuneCmd.
	/// </summary>
	public class MPExTuneCmd: IPlugin, ISetupForm
	{
		public static int WINDOW_MPExTuneCmd = 9099;	// a window ID shouldn't be needed when a non visual plugin ?!
		private static string s_TuneCmd		= "";
		private static string s_TuneParam	= "";
		private const string  s_version     = "0.1";

		public MPExTuneCmd()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public void Start()
		{
			Log.Write("MPExTuneCmd {0} plugin starting.",s_version);

			LoadSettings();

			Log.Write("Adding message handler for MPExTuneCmd {0}.",s_version);

			GUIWindowManager.Receivers += new SendMessageHandler(this.OnThreadMessage);
			return;
		}

		public void Stop()
		{
			Log.Write("MPExTuneCmd {0} plugin stopping.",s_version);
			return;
		}

		private void LoadSettings()
		{
			using(AMS.Profile.Xml xmlreader = new AMS.Profile.Xml("MediaPortal.xml"))
			{
				s_TuneCmd = xmlreader.GetValueAsString("MPExTuneCmd","commandloc","C:\\dtvcon\\dtvcmd.exe");
				s_TuneParam = xmlreader.GetValueAsString("MPExTuneCmd","commanddelim","");
			}
		}
		
		private void OnThreadMessage(GUIMessage message)
		{
			switch (message.Message)
			{
				case GUIMessage.MessageType.GUI_MSG_TUNE_EXTERNAL_CHANNEL : 
					bool bIsInteger;
					double retNum;	
					bIsInteger = Double.TryParse(message.Label, System.Globalization.NumberStyles.Integer,System.Globalization.NumberFormatInfo.InvariantInfo, out retNum );
					this.ChangeTunerChannel( message.Label );
					break;
			}
		}

		public void ChangeTunerChannel(string channel_data) 
		{
			Log.Write("MPExTuneCmd processing external tuner cmd: {0}", s_TuneCmd + " " + s_TuneParam + channel_data );
			this.RunProgram(s_TuneCmd, s_TuneParam + channel_data );

		}

		/// <summary>
		/// Runs a particular program in the local file system
		/// </summary>
		/// <param name="exeName"></param>
		/// <param name="argsLine"></param>
		private void RunProgram(string exeName, string argsLine)
		{
			ProcessStartInfo psI = new ProcessStartInfo(exeName, argsLine);
			Process newProcess = new Process();

			try
			{
				newProcess.StartInfo.FileName = exeName;
				newProcess.StartInfo.Arguments = argsLine;
				newProcess.StartInfo.UseShellExecute = true;
				newProcess.StartInfo.CreateNoWindow = true;
				newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
				newProcess.Start();
			}

			catch(Exception e)
			{
				throw e;
			}
		}

		#region ISetupForm Members


		public bool CanEnable()
		{
			return true;
		}

		public string PluginName()
		{
			return "MPExTuneCmd Plugin";
		}

		public bool HasSetup()
		{
			return true;
		}
		public bool DefaultEnabled()
		{
			return false;
		}

		public int GetWindowId()
		{
			return WINDOW_MPExTuneCmd;
		}

		public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
		{
			strButtonText = "MPExTuneCmd Plugin";
			strButtonImage = "";
			strButtonImageFocus = "";
			strPictureImage = "";
			return false;
		}

		public string Author()
		{
			return "901Racer";
		}

		public string Description()
		{
			return "External Tuner command interface";
		}

		/// <summary>
		/// This method is called by the plugin screen to show the configuration for the foobar plugin
		/// </summary>
		public void ShowPlugin()
		{ 
			System.Windows.Forms.Form setup = new MPExTuneCmdForm();
			setup.ShowDialog();
		}
		

		#endregion
	}
}