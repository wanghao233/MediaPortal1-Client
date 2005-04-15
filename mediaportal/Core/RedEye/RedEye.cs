/*
 * UIR/IRMan serial support
 * 2004-12-12
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Collections;
using JH.CommBase;
using System.Windows.Forms;
using MediaPortal.GUI.Library;

namespace MediaPortal.RedEyeIR
{
	/// <summary>
	/// This class will handle all communication with an external RedEye device
	/// The Serial UIR, allows your PC to Receive infrared signals -- 
	/// exactly like those used by the collection of remote controls you've acquired for your TV, 
	/// VCR, Audio System, etc. 
	/// </summary>
	public class LearningEventArgs : System.EventArgs
	{
		public string Button;
		public LearningEventArgs(string button)
		{
			this.Button = button;
		}
	}

	public class ListeningEventArgs : System.EventArgs
	{
		public string Code;
		public ListeningEventArgs(string Code)
		{
			this.Code = Code;
		}
	}

	public delegate void StartLearningEventHandler(object sender, LearningEventArgs e);

	public delegate void StartListeningEventHandler(object sender, ListeningEventArgs e);

	public class RedEye : CommLine
	{
		/// <summary>
		/// Private constructor disables the user from creating an instance of the class. All access
		/// to methods should be done through the singleton property "Instance".
		/// </summary>
		private RedEye()
		{
		}

		public delegate void OnRemoteCommand(object command);

		public int LearningTimeOut = 4000; // time out in milliseconds
		public int CommandDelay = 300; // time between 2 commands

		private bool loaded = false;
		private bool is3DigitTuner = true;
		private OnRemoteCommand remoteCommandCallback = null;

		private bool recInternalCommands = false;
		private const string remotefile = "remotevalues.xml";
		static RedEye instance = null;
		private string currentChannel = "0";

		public OnRemoteCommand RemoteCommandCallback
		{
			set 
			{
				remoteCommandCallback = value;
			}
		}

		public new bool DTR 
		{
			set 
			{
				base.DTR = value;
			}
			get 
			{
				return base.DTR;
			}
		}

		public new bool RTS
		{
			set 
			{
				base.RTS = value;
			}
			get 
			{
				return base.RTS;
			}
		}

		private string commport     = "COM1:";
		private int    baudrate     = 9600;
		private string handshake    = "None";
		private string parity       = "None";
		private int    irbytes      = 8;
		private bool   uirirmaninit = false;

		protected override CommBaseSettings CommSettings() 
		{
			CommBaseSettings cs = new CommBaseSettings();
			Handshake hs;
			switch(handshake)
			{
				case "CtsRts":
					hs = Handshake.CtsRts;
					break;
				case "DsrDtr":
					hs = Handshake.DsrDtr;
					break;
				case "XonXoff":
					hs = Handshake.XonXoff;
					break;
				default:
					hs = Handshake.none;
					break;
			}
			cs.SetStandard(commport,baudrate,hs);
			switch(parity)
			{
				case "Odd":
					cs.parity = Parity.odd;
					break;
				case "Even":
					cs.parity = Parity.even;
					break;
				case "Mark":
					cs.parity = Parity.mark;
					break;
				case "Space":
					cs.parity = Parity.space;
					break;
				default:
					cs.parity = Parity.none;
					break;
			}
			cs.sendTimeoutConstant = 0;
			cs.sendTimeoutMultiplier = 450;
			return cs;
		}

		
		public static RedEye Create(OnRemoteCommand remoteCommandCallback)
		{
			try
			{
				if (instance == null)
					instance = new RedEye(remoteCommandCallback);
			}
			catch (Exception)
			{
			}
			return instance;
		}

		public static RedEye Instance 
		{
			get 
			{
				return instance;
			}
		}

		public bool InternalCommandsActive 
		{
			get 
			{
				return this.recInternalCommands;
			}

			set 
			{
				this.recInternalCommands = value;
			}
		}

		public bool ReOpen()
		{
			base.Close();
			if (base.IsPortAvailable(commport) == PortStatus.available)
				return base.Open();
			else
				return false;
		}

		public bool SetPort(string CommPort)
		{
			commport = CommPort;
			return ReOpen();
		}

		public bool SetBaudRate(int BaudRate)
		{
			baudrate = BaudRate;
			return ReOpen();
		}

		public bool SetHandShake(string Handshake)
		{
			handshake = Handshake;
			return ReOpen();
		}

		public bool SetParity(string pr)
		{
			parity = pr;
			return ReOpen();
		}

		public bool SetIRBytes(int IRBytes)
		{
			irbytes = IRBytes;
			return ReOpen();
		}

		public bool SetUIRIRmanInit(bool UIRIRManInit)
		{
			uirirmaninit = UIRIRManInit;
			return ReOpen();
		}

		private RedEye(OnRemoteCommand remoteCommandCallback)
		{
			try
			{
				using(MediaPortal.Profile.Xml   xmlreader=new MediaPortal.Profile.Xml("MediaPortal.xml"))
				{
					recInternalCommands = xmlreader.GetValueAsString("RedEye", "internal", "false") == "true";
					commport        = xmlreader.GetValueAsString("RedEye", "commport",     "COM1:");
					baudrate        = xmlreader.GetValueAsInt(   "RedEye", "baudrate",     9600);
					handshake       = xmlreader.GetValueAsString("RedEye", "handshake",    "none");
					irbytes         = xmlreader.GetValueAsInt(   "RedEye", "irbytes",      6);
					uirirmaninit    = xmlreader.GetValueAsString("RedEye", "uirirmaninit", "true") == "true";
					LearningTimeOut = 1000 * xmlreader.GetValueAsInt(   "RedEye", "timeout",      4);
					CommandDelay    = xmlreader.GetValueAsInt(   "RedEye", "delay",        300);
				}
				this.remoteCommandCallback = remoteCommandCallback;
				base.Open();
			}
			catch(Exception )
			{
				//most users dont have serial device on their system so will get a exception here
			}
		}

		public string GetName()
		{
			return "RedEye";
		}

		public string GetVersions()
		{
			if(loaded)
			{
				DateTime plugdate = new DateTime(2004,12,10);
				string plug = "Plugin Version: 1.1 ("+plugdate.ToString("MMMM, dd, yyyy")+")";
				return plug;
			}
			else
			{
				return "plugin is offline";
			}
		}

		bool ignore = true;
		DateTime timestamp = DateTime.Now;

		DateTime bytetimestamp = DateTime.Now;

		protected override void OnRxChar(byte ch) 
		{
			if (ignore)
				return;
		}
		//Set the Prequency mode to IRDA
		public void SetIRDA()
		{
			if (base.Online)
			{
				base.SendImmediate((byte)'%');
				Log.Write("RedEye IRDA set");
				Sleep(500);
			}
			else
			{
				Log.Write("IRDA set failed, Port not Online");
				throw new System.Exception("IRDA set failed, Port not Online");
				
			}
		}
		
		// Set the Frequency Mode to RC5
		public void SetRC5()
		{
			if (base.Online)
			{
				base.SendImmediate((byte)'&');
				Log.Write("RedEye RC5 Set");
				Sleep(500);
			}
			else
			{
				Log.Write("RC5 set failed, Port not Online");
				throw new System.Exception("RC5 set failed, Port not Online");
				
			}
		}

		// Set the Frequency mode to SKY
		public void SetSKY()
		{
			if (base.Online)
			{
				base.SendImmediate((byte)'$');
				Log.Write("RedEye SkY set");
				Sleep(500);
			}
			else
			{
				Log.Write("SKY set failed, Port not Online");
				throw new System.Exception("SKY set failed, Port not Online");
				
			}
		}

		protected override bool AfterOpen() 
		{
			Sleep(280);
			ignore = false;
			return true;
		}
		public void ChangeTunerChannel(string channel)
			
		{
			int length = channel.Length;
			
			if ((!this.is3DigitTuner && length >2) || (length >3))
				throw new System.Exception("invalid channel length");

			if (!channel.Equals(currentChannel))
			{			
				currentChannel = channel;
				for (int i = 0; i<length; i++ )
				{
	
					if (channel[i] < '0' || channel[i] > '9')
					{
						throw new System.Exception("invalid digit in channel: "+channel);
					}
					else
					{
						int channelchr = (int)channel[i];
						if (base.Online)
						{
							
	
							base.SendImmediate((byte)'=');
							base.SendImmediate((byte)(char)channelchr);
							base.SendImmediate((byte)'\x002A');	
															
						}
						else
						{
							Log.Write("Redeye Failed to Send channel change : " + channel);
							throw new System.Exception("Redeye Failed to Send channel change : " + channel);
							
						}
						Sleep(CommandDelay);
					}	
				}
				Log.Write("RedEye Transmitted Channel : " + channel);
			}											
		}
	}

}