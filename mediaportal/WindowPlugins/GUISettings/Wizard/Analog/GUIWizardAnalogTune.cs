using System;
using System.Collections;
using System.Xml;
using System.Threading;
using MediaPortal.TV.Database;
using MediaPortal.GUI.Library;
using MediaPortal.TV.Recording;
using MediaPortal.Util;
using MediaPortal.GUI.Settings.Wizard;
using DShowNET;
namespace WindowPlugins.GUISettings.Wizard.Analog
{
	/// <summary>
	/// Summary description for GUIWizardAnalogTune.
	/// </summary>
	public class GUIWizardAnalogTune: GUIWindow
	{
		[SkinControlAttribute(26)]			protected GUILabelControl lblChannelsFound=null;
		[SkinControlAttribute(27)]			protected GUILabelControl lblStatus=null;
		[SkinControlAttribute(24)]			protected GUIListControl  listChannelsFound=null;
		[SkinControlAttribute(5)]			  protected GUIButtonControl  btnNext=null;
		[SkinControlAttribute(25)]			protected GUIButtonControl  btnBack=null;
		[SkinControlAttribute(20)]			protected GUIProgressControl progressBar=null;

		int card=0;
		
		int        currentFrequencyIndex=0;
		bool updateList=false;
		int newChannels=0;

		public GUIWizardAnalogTune()
		{
			GetID=(int)GUIWindow.Window.WINDOW_WIZARD_ANALOG_TUNE;
		}
    
		public override bool Init()
		{
			return Load (GUIGraphicsContext.Skin+@"\wizard_tvcard_analog_scan.xml");
		}
		protected override void OnPageDestroy(int newWindowId)
		{
			base.OnPageDestroy (newWindowId);
			GUIGraphicsContext.VMR9Allowed=true;
		}

		protected override void OnPageLoad()
		{
			base.OnPageLoad ();
			GUIGraphicsContext.VMR9Allowed=false;
			btnNext.Disabled=true;
			btnBack.Disabled=true;
			progressBar.Percentage=0;
			progressBar.Disabled=false;
			progressBar.IsVisible=true;
			UpdateList();
			Thread WorkerThread = new Thread(new ThreadStart(ScanThread));
			WorkerThread.Start();
		}
		public void ScanThread()
		{
			newChannels=0;;
			TVCaptureDevice captureCard=null;
			if (card >=0 && card < Recorder.Count)
			{
				captureCard =Recorder.Get(card);
				captureCard.CreateGraph();
			}
			else
			{
				btnNext.Disabled=false;
				btnBack.Disabled=false;
				return;
			}
			try
			{
				updateList=false;
				if (captureCard==null) return;
				currentFrequencyIndex=0;
				while (true)
				{
					if (currentFrequencyIndex >= 200)
					{
						btnNext.Disabled=false;
						btnBack.Disabled=false;
						return;
					}

					UpdateStatus();
					ScanNextFrequency(captureCard,0);
					if (captureCard.SignalPresent())
					{
						ScanChannels(captureCard);
					}
					currentFrequencyIndex++;
				}
			}
			finally
			{
				captureCard.DeleteGraph();
				captureCard=null;
				progressBar.Percentage=100;
				lblChannelsFound.Label=String.Format("Finished, found {0} tv channels",newChannels);
				lblStatus.Label="Press Next to continue the setup";
				MapTvToOtherCards(captureCard.ID);
				MapRadioToOtherCards(captureCard.ID);
				GUIControl.FocusControl(GetID,btnNext.GetID);
				GUIPropertyManager.SetProperty("#Wizard.Analog.Done","yes");

			}
		}
		void ScanChannels(TVCaptureDevice captureCard)
		{
			Log.Write("Analog-scan:ScanChannels() {0}/{1}",currentFrequencyIndex,200);
			if (currentFrequencyIndex < 0 || currentFrequencyIndex >=200) return;

			string description=String.Format("Found signal at channel:{0} MHz", currentFrequencyIndex);

			updateList=true;
			newChannels++;
			lblStatus.Label=String.Format("Found {0} tv channels",newChannels);
			Log.Write("Analog-scan:ScanChannels() done");
		}

		void ScanNextFrequency(TVCaptureDevice captureCard,int offset)
		{
			Log.Write("Analog-scan:ScanNextFrequency() {0}/{1}",currentFrequencyIndex,200);
			if (currentFrequencyIndex <0) currentFrequencyIndex =0;
			if (currentFrequencyIndex >=200) return;


			TVChannel chan = new TVChannel();
			chan.Number=currentFrequencyIndex;
			chan.Country=captureCard.DefaultCountryCode;
			chan.TVStandard=AnalogVideoStandard.None;

			string description=String.Format("Channel:{0}", currentFrequencyIndex);

			Log.WriteFile(Log.LogType.Capture,"Analog-scan:tune:{0}",currentFrequencyIndex);

			captureCard.ViewChannel(chan);
			Log.WriteFile(Log.LogType.Capture,"Analog-scan:tuned");
			return;
		}

		public override void Process()
		{
			if (updateList)
			{
				UpdateList();
				updateList=false;
			}
	
			base.Process ();
		}

		void UpdateList()
		{
			listChannelsFound.Clear();
			ArrayList channels = new ArrayList();
			TVDatabase.GetChannels(ref channels);
			if (channels.Count==0)
			{
				GUIListItem item = new GUIListItem();
				item.Label="No channels found";
				item.IsFolder=false;
				listChannelsFound.Add(item);
				return;

			}
			int count=1;
			foreach (TVChannel chan in channels)
			{
				GUIListItem item = new GUIListItem();
				item.Label=String.Format("{0}. {1}", count,chan.Name);
				item.IsFolder=false;
				string strLogo=Utils.GetCoverArt(Thumbs.TVChannel,chan.Name);
				if (!System.IO.File.Exists(strLogo))
				{
					strLogo="defaultVideoBig.png";
				}
				item.ThumbnailImage=strLogo;
				item.IconImage=strLogo;
				item.IconImageBig=strLogo;
				listChannelsFound.Add(item);
				count++;
			}
			listChannelsFound.ScrollToEnd();
		}
		void UpdateStatus()
		{
			int currentFreq=currentFrequencyIndex;
			if (currentFrequencyIndex<0) currentFreq=0;
			float percent = ((float)currentFreq) / ((float)200);
			percent *= 100.0f;
			
			progressBar.Percentage=(int)percent;
			string description=String.Format("Channel:{0}", currentFreq);
			lblChannelsFound.Label=description;
		}
		void MapTvToOtherCards(int id)
		{
			ArrayList tvchannels = new ArrayList();
			TVDatabase.GetChannelsForCard(ref tvchannels,id);
			for (int i=0; i < Recorder.Count;++i)
			{
				TVCaptureDevice dev = Recorder.Get(i);
				if (dev.Network==NetworkType.Analog && dev.ID != id)
				{
					foreach (TVChannel chan in tvchannels)
					{
						TVDatabase.MapChannelToCard(chan.ID,dev.ID);
					}
				}
			}
		}
		void MapRadioToOtherCards(int id)
		{
			ArrayList radioChans = new ArrayList();
			MediaPortal.Radio.Database.RadioDatabase.GetStationsForCard(ref radioChans,id);
			for (int i=0; i < Recorder.Count;++i)
			{
				TVCaptureDevice dev = Recorder.Get(i);

				if (dev.Network==NetworkType.Analog && dev.ID != id)
				{
					foreach (MediaPortal.Radio.Database.RadioStation chan in radioChans)
					{
						MediaPortal.Radio.Database.RadioDatabase.MapChannelToCard(chan.ID,dev.ID);
					}
				}
			}
		}

		protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
		{
			if (control==btnNext)
			{
				GUIWizardCardsDetected.ScanNextCardType();
				return;
			}
			base.OnClicked (controlId, control, actionType);
		}
	}
}
