using System;
using System.Collections;
using System.Globalization;
using MediaPortal.Dialogs;
using MediaPortal.Util;
using MediaPortal.GUI.Library;
using MediaPortal.TV.Recording;
using MediaPortal.TV.Database;

namespace MediaPortal.GUI.TV
{
	/// <summary>
	/// Summary description for GUITVProgramInfo.
	/// </summary>
	public class GUITvRecordedInfo : GUIWindow
	{
		[SkinControlAttribute(17)]			  protected GUILabelControl					lblProgramGenre=null;
		[SkinControlAttribute(15)]			  protected GUITextScrollUpControl	lblProgramDescription=null;
		[SkinControlAttribute(14)]			  protected GUILabelControl					lblProgramTime=null;
		[SkinControlAttribute(13)]			  protected GUIFadeLabel						lblProgramTitle=null;
		[SkinControlAttribute(2)]					protected GUIButtonControl				btnKeep=null;

		static TVRecorded currentProgram=null;

		public GUITvRecordedInfo()
		{
			GetID=(int)GUIWindow.Window.WINDOW_TV_RECORDED_INFO;//759
		}
		public override bool Init()
		{
			bool bResult=Load (GUIGraphicsContext.Skin+@"\mytvRecordedInfo.xml");
			return bResult;
		}
		protected override void OnPageLoad()
		{
			base.OnPageLoad ();
			Update();
		}

		static public TVRecorded CurrentProgram
		{
			get { return currentProgram;}
			set { currentProgram=value;}
		}

		void Update()
		{
			if (currentProgram==null) return;

			string strTime=String.Format("{0} {1} - {2}", 
				Utils.GetShortDayString(currentProgram.StartTime) , 
				currentProgram.StartTime.ToString("t",CultureInfo.CurrentCulture.DateTimeFormat),
				currentProgram.EndTime.ToString("t",CultureInfo.CurrentCulture.DateTimeFormat));

			lblProgramGenre.Label=currentProgram.Genre;
			lblProgramTime.Label=strTime;
			lblProgramDescription.Label=currentProgram.Description;
			lblProgramTitle.Label=currentProgram.Title;
		}

		protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
		{
			if (control==btnKeep)
				OnKeep();
			base.OnClicked (controlId, control, actionType);
		}

		void OnKeep()
		{
			GUIDialogMenu dlg=(GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
			if (dlg==null) return;
			dlg.Reset();
			dlg.SetHeading(1042);
			dlg.AddLocalizedString( 1043);//Until watched
			dlg.AddLocalizedString( 1044);//Until space needed
			dlg.AddLocalizedString( 1045);//Until date
			dlg.AddLocalizedString( 1046);//Always
			switch (currentProgram.KeepRecordingMethod)
			{
				case TVRecorded.KeepMethod.UntilWatched: 
					dlg.SelectedLabel=0;
					break;
				case TVRecorded.KeepMethod.UntilSpaceNeeded: 
					dlg.SelectedLabel=1;
					break;
				case TVRecorded.KeepMethod.TillDate: 
					dlg.SelectedLabel=2;
					break;
				case TVRecorded.KeepMethod.Always: 
					dlg.SelectedLabel=3;
					break;
			}
			dlg.DoModal( GetID);
			if (dlg.SelectedLabel==-1) return;
			switch (dlg.SelectedId)
			{
				case 1043: 
					currentProgram.KeepRecordingMethod=TVRecorded.KeepMethod.UntilWatched;
					break;
				case 1044: 
					currentProgram.KeepRecordingMethod=TVRecorded.KeepMethod.UntilSpaceNeeded;
				
					break;
				case 1045: 
					currentProgram.KeepRecordingMethod=TVRecorded.KeepMethod.TillDate;
					dlg.Reset();
					dlg.ShowQuickNumbers=false;
					dlg.SetHeading(1045);
					for (int iDay=1; iDay <= 100; iDay++)
					{
						DateTime dt=currentProgram.StartTime.AddDays(iDay);
						if (currentProgram.StartTime < DateTime.Now)
							dt=DateTime.Now.AddDays(iDay);

						dlg.Add(dt.ToLongDateString());
					}
					TimeSpan ts=(currentProgram.KeepRecordingTill-currentProgram.StartTime);
					if (currentProgram.StartTime < DateTime.Now)
							ts=(currentProgram.KeepRecordingTill-DateTime.Now);
					int days=(int)ts.TotalDays;
					if (days >=100) days=30;
					dlg.SelectedLabel=days-1;
					dlg.DoModal(GetID);
					if (dlg.SelectedLabel<0) return;
					if (currentProgram.StartTime < DateTime.Now)
						currentProgram.KeepRecordingTill=DateTime.Now.AddDays(dlg.SelectedLabel+1);
					else
						currentProgram.KeepRecordingTill=currentProgram.StartTime.AddDays(dlg.SelectedLabel+1);
					break;
				case 1046: 
					currentProgram.KeepRecordingMethod=TVRecorded.KeepMethod.Always;
					break;
			}
			TVDatabase.SetRecordedKeep(currentProgram);
		}
	}
}
