using System;
using MediaPortal.GUI.Library;

namespace MediaPortal.GUI.Settings.Wizard
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class  GUIWizardRemote : GUIWindow
	{
		[SkinControlAttribute(4)]			protected GUICheckMarkControl cmMicrosoftUSA=null;
		[SkinControlAttribute(5)]			protected GUICheckMarkControl cmMicrosoftEU=null;
		[SkinControlAttribute(6)]			protected GUICheckMarkControl cmHauppauge=null;
		[SkinControlAttribute(7)]			protected GUICheckMarkControl cmFireDTV=null;
		[SkinControlAttribute(8)]			protected GUICheckMarkControl cmOther=null;
		[SkinControlAttribute(26)]		protected GUIButtonControl		btnNext=null;
		[SkinControlAttribute(10)]		protected GUIImage						imgRemote=null;

		public  GUIWizardRemote()
		{
			
			GetID=(int)GUIWindow.Window.WINDOW_WIZARD_REMOTE;
		}
    
		public override bool Init()
		{
			return Load (GUIGraphicsContext.Skin+@"\wizard_remote_control.xml");
		}
		protected override void OnPageLoad()
		{
			base.OnPageLoad ();
			LoadSettings();
		}

		protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
		{
			if (cmMicrosoftUSA==control) OnMicrosoftUSA();
			if (cmMicrosoftEU==control) OnMicrosoftEU();
			if (cmHauppauge==control) OnHauppauge();
			if (cmFireDTV==control) OnFireDTV();
			if (cmOther==control) OnOther();
			if (btnNext==control) OnNextPage();
			base.OnClicked (controlId, control, actionType);
		}

		void OnMicrosoftUSA()
		{
			cmMicrosoftUSA.Selected=true;
			cmMicrosoftEU.Selected=false;
			cmHauppauge.Selected=false;
			cmFireDTV.Selected=false;
			cmOther.Selected=false;
			imgRemote.SetFileName(@"Wizards\MCEUSA.jpg");
			GUIControl.FocusControl(GetID,cmMicrosoftUSA.GetID);
		}
		void OnMicrosoftEU()
		{
			cmMicrosoftUSA.Selected=false;
			cmMicrosoftEU.Selected=true;
			cmHauppauge.Selected=false;
			cmFireDTV.Selected=false;
			cmOther.Selected=false;
			imgRemote.SetFileName(@"Wizards\remote_mce_eu.png");
			GUIControl.FocusControl(GetID,cmMicrosoftEU.GetID);
		}

		void OnHauppauge()
		{
			cmMicrosoftUSA.Selected=false;
			cmMicrosoftEU.Selected=false;
			cmHauppauge.Selected=true;
			cmFireDTV.Selected=false;
			cmOther.Selected=false;
			imgRemote.SetFileName(@"Wizards\hauppauge.jpg");
			GUIControl.FocusControl(GetID,cmHauppauge.GetID);
		}
		
		void OnFireDTV()
		{
			cmMicrosoftUSA.Selected=false;
			cmMicrosoftEU.Selected=false;

			cmHauppauge.Selected=false;
			cmFireDTV.Selected=true;
			cmOther.Selected=false;
			imgRemote.SetFileName(@"Wizards\firedtv.png");
			GUIControl.FocusControl(GetID,cmFireDTV.GetID);
		}
		
		void OnOther()
		{
			cmMicrosoftUSA.Selected=false;
			cmMicrosoftEU.Selected=false;

			cmHauppauge.Selected=false;
			cmFireDTV.Selected=false;
			cmOther.Selected=true;
			imgRemote.SetFileName("");
			GUIControl.FocusControl(GetID,cmOther.GetID);
		}
		void LoadSettings()
		{
			using (MediaPortal.Profile.Xml xmlreader = new MediaPortal.Profile.Xml("MediaPortal.xml"))
			{
				bool useMCE=xmlreader.GetValueAsBool("remote", "mce2005", false);
				bool useMCEUSA=xmlreader.GetValueAsBool("remote", "USAModel", false);
				bool useHCW=xmlreader.GetValueAsBool("remote", "HCW", false);
				bool useFireDTV=xmlreader.GetValueAsBool("remote", "FireDTV", false);
				if (useMCE && useMCEUSA) OnMicrosoftUSA();
				else if (useMCE && !useMCEUSA) OnMicrosoftEU();
				else if (useHCW) OnHauppauge();
				else if (useFireDTV) OnFireDTV();
				else OnOther();
			}
		}
		void OnNextPage()
		{

			using (MediaPortal.Profile.Xml xmlwriter = new MediaPortal.Profile.Xml("MediaPortal.xml"))
			{
				xmlwriter.SetValueAsBool("remote", "mce2005", (cmMicrosoftUSA.Selected||cmMicrosoftEU.Selected));
				xmlwriter.SetValueAsBool("remote", "USAModel", cmMicrosoftUSA.Selected);
				xmlwriter.SetValueAsBool("remote", "HCW", cmHauppauge.Selected);
				xmlwriter.SetValueAsBool("remote", "FireDTV", cmFireDTV.Selected);
			}
			GUIPropertyManager.SetProperty("#Wizard.Remote.Done","yes");

			GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RESTART_REMOTE_CONTROLS,0,0,0,0,0,null);
			GUIGraphicsContext.SendMessage(msg);
			
			GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_WIZARD_CARDS_DETECTED);
		}
	}
}
