#if (UseCaptureCardDefinitions)
using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using DShowNET;
using DShowNET.Device;
using DShowNET.BDA;
using MediaPortal.Util;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.TV.Database;
using TVCapture;
using System.Xml;
using DirectX.Capture;

namespace MediaPortal.TV.Recording
{


	/// <summary>
	/// Implementation of IGraph for digital TV capture cards using the BDA driver architecture
	/// It handles any DVB-T, DVB-C, DVB-S TV Capture card with BDA drivers
	///
	/// A graphbuilder object supports one or more TVCapture cards and
	/// contains all the code/logic necessary todo
	/// -tv viewing
	/// -tv recording
	/// -tv timeshifting
	/// </summary>
	public class DVBGraphBDA : MediaPortal.TV.Recording.IGraph
	{
		[ComImport, Guid("6CFAD761-735D-4aa5-8AFC-AF91A7D61EBA")]
			class VideoAnalyzer {};

		[ComImport, Guid("AFB6C280-2C41-11D3-8A60-0000F81E0E4A")]
			class MPEG2Demultiplexer {}
    
		[ComImport, Guid("2DB47AE5-CF39-43c2-B4D6-0CD8D90946F4")]
		class StreamBufferSink {};

		[ComImport, Guid("FA8A68B2-C864-4ba2-AD53-D3876A87494B")]
		class StreamBufferConfig {}
	    
		[DllImport("advapi32", CharSet=CharSet.Auto)]
		private static extern bool ConvertStringSidToSid(string pStringSid, ref IntPtr pSID);

		[DllImport("kernel32", CharSet=CharSet.Auto)]
		private static extern IntPtr  LocalFree( IntPtr hMem);

		[DllImport("advapi32", CharSet=CharSet.Auto)]
		private static extern ulong RegOpenKeyEx(IntPtr key, string subKey, uint ulOptions, uint sam, out IntPtr resultKey);
		const int WS_CHILD				= 0x40000000;	
		const int WS_CLIPCHILDREN	= 0x02000000;
		const int WS_CLIPSIBLINGS	= 0x04000000;

		private static Guid CLSID_StreamBufferSink					= new Guid(0x2db47ae5, 0xcf39, 0x43c2, 0xb4, 0xd6, 0xc, 0xd8, 0xd9, 0x9, 0x46, 0xf4);
		private static Guid CLSID_Mpeg2VideoStreamAnalyzer	= new Guid(0x6cfad761, 0x735d, 0x4aa5, 0x8a, 0xfc, 0xaf, 0x91, 0xa7, 0xd6, 0x1e, 0xba);
		private static Guid CLSID_StreamBufferConfig				= new Guid(0xfa8a68b2, 0xc864, 0x4ba2, 0xad, 0x53, 0xd3, 0x87, 0x6a, 0x87, 0x49, 0x4b);

		enum State
		{ 
			None, 
			Created, 
			TimeShifting,
			Recording, 
			Viewing
		};

		int                     m_cardID								= -1;
		int                     m_iCurrentChannel				= 28;
		int											m_rotCookie							= 0;			// Cookie into the Running Object Table
		int                     m_iPrevChannel					= -1;
		bool                    m_bIsUsingMPEG					= false;
		State                   m_graphState						= State.None;
		DateTime								m_StartTime							= DateTime.Now;


		IBaseFilter             m_NetworkProvider				= null;			// BDA Network Provider
		IBaseFilter             m_TunerDevice						= null;			// BDA Digital Tuner Device
		IBaseFilter							m_CaptureDevice					= null;			// BDA Digital Capture Device
		IBaseFilter							m_MPEG2Demultiplexer		= null;			// Mpeg2 Demultiplexer that connects to Preview pin on Smart Tee (must connect before capture)
		IBaseFilter							m_TIF										= null;			// Transport Information Filter
		IBaseFilter							m_SectionsTables				= null;
		VideoAnalyzer						m_mpeg2Analyzer					= null;
		StreamBufferSink				m_StreamBufferSink=null;
		IGraphBuilder           m_graphBuilder					= null;
		ICaptureGraphBuilder2   m_captureGraphBuilder		= null;
		IVideoWindow            m_videoWindow						= null;
		IBasicVideo2            m_basicVideo						= null;
		IMediaControl						m_mediaControl					= null;
		NetworkType							m_NetworkType;
		
		TVCaptureDevice					m_Card;
		
		//streambuffer interfaces
		IPin												m_DemuxVideoPin				= null;
		IPin												m_DemuxAudioPin				= null;
		IPin												m_pinStreamBufferIn0	= null;
		IPin												m_pinStreamBufferIn1	= null;
		IStreamBufferRecordControl	m_recControl					= null;
		IStreamBufferSink						m_IStreamBufferSink		= null;
		IStreamBufferConfigure			m_IStreamBufferConfig	= null;
		StreamBufferConfig					m_StreamBufferConfig	= null;
		VMR9Util									  Vmr9								  = null; 
		GuideDataEvent							m_Event               = null;
		GCHandle										myHandle;
		int                         adviseCookie;
		bool												graphRunning=false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="pCard">instance of a TVCaptureDevice which contains all details about this card</param>
		public DVBGraphBDA(TVCaptureDevice pCard)	
		{
			m_Card								= pCard;
			m_cardID							= pCard.ID;
			m_bIsUsingMPEG				= true;
			m_graphState					= State.None;

			try
			{
				RegistryKey hkcu = Registry.CurrentUser;
				hkcu.CreateSubKey(@"Software\MediaPortal");
				RegistryKey hklm = Registry.LocalMachine;
				hklm.CreateSubKey(@"Software\MediaPortal");
			}
			catch(Exception){}
		}

		/// <summary>
		/// Creates a new DirectShow graph for the TV capturecard
		/// </summary>
		/// <returns>bool indicating if graph is created or not</returns>
		public bool CreateGraph()
		{
			if (m_graphState != State.None) 
				return false;
	      
			graphRunning=false;
			Log.Write("DVBGraphBDA:CreateGraph(). ");

			if (m_Card==null) 
			{
				Log.Write("DVBGraphBDA:card is not defined");
				return false;
			}
			if (!m_Card.LoadDefinitions())											// Load configuration for this card
			{
				DirectShowUtil.DebugWrite("DVBGraphBDA: Loading card definitions for card {0} failed", m_Card.CaptureName);
				return false;
			}
			
			if (m_Card.TvFilterDefinitions==null)
			{
				Log.Write("DVBGraphBDA:card does not contain filters?");
				return false;
			}
			if (m_Card.TvConnectionDefinitions==null)
			{
				Log.Write("DVBGraphBDA:card does not contain connections for tv?");
				return false;
			}

			Vmr9 =new VMR9Util("mytv");

			// Make a new filter graph
			Log.Write("DVBGraphBDA:create new filter graph (IGraphBuilder)");
			m_graphBuilder = (IGraphBuilder) Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.FilterGraph, true));

			// Get the Capture Graph Builder
			Log.Write("DVBGraphBDA:Get the Capture Graph Builder (ICaptureGraphBuilder2)");
			Guid clsid = Clsid.CaptureGraphBuilder2;
			Guid riid = typeof(ICaptureGraphBuilder2).GUID;
			m_captureGraphBuilder = (ICaptureGraphBuilder2) DsBugWO.CreateDsInstance(ref clsid, ref riid);

			Log.Write("DVBGraphBDA:Link the CaptureGraphBuilder to the filter graph (SetFiltergraph)");
			int hr = m_captureGraphBuilder.SetFiltergraph(m_graphBuilder);
			if (hr < 0) 
			{
				Log.Write("DVBGraphBDA:link FAILED:0x{0:X}",hr);
				return false;
			}
			Log.Write("DVBGraphBDA:Add graph to ROT table");
			DsROT.AddGraphToRot(m_graphBuilder, out m_rotCookie);


			// Loop through configured filters for this card, bind them and add them to the graph
			// Note that while adding filters to a graph, some connections may already be created...
			Log.Write("DVBGraphBDA: Adding configured filters...");
			foreach (string catName in m_Card.TvFilterDefinitions.Keys)
			{
				FilterDefinition dsFilter = m_Card.TvFilterDefinitions[catName] as FilterDefinition;
				dsFilter.DSFilter         = Marshal.BindToMoniker(dsFilter.MonikerDisplayName) as IBaseFilter;
				hr = m_graphBuilder.AddFilter(dsFilter.DSFilter, dsFilter.FriendlyName);
				if (hr == 0)
				{
					Log.Write("DVBGraphBDA:  Added filter <{0}> with moniker <{1}>", dsFilter.FriendlyName, dsFilter.MonikerDisplayName);
				}
				else
				{
					Log.Write("DVBGraphBDA:  Error! Failed adding filter <{0}> with moniker <{1}>", dsFilter.FriendlyName, dsFilter.MonikerDisplayName);
					Log.Write("DVBGraphBDA:  Error! Result code = {0}", hr);
				}

				// Support the "legacy" member variables. This could be done different using properties
				// through which the filters are accessable. More implementation independent...
				if (dsFilter.Category == "networkprovider") m_NetworkProvider       = dsFilter.DSFilter;
				if (dsFilter.Category == "tunerdevice") m_TunerDevice	 							= dsFilter.DSFilter;
				if (dsFilter.Category == "capture")			m_CaptureDevice							= dsFilter.DSFilter;
			}
			Log.Write("DVBGraphBDA: Adding configured filters...DONE");

			
			if(m_NetworkProvider == null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED networkprovider filter not found");
				return false;
			}
			if(m_CaptureDevice == null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED capture filter not found");
			}


			FilterDefinition sourceFilter;
			FilterDefinition sinkFilter;
			IPin sourcePin;
			IPin sinkPin;

			// Create pin connections. These connections are also specified in the definitions file.
			// Note that some connections might fail due to the fact that the connection is already made,
			// probably during the addition of filters to the graph (checked with GraphEdit...)
			//
			// Pin connections can be defined in two ways:
			// 1. Using the name of the pin.
			//		This method does work, but might be language dependent, meaning the connection attempt
			//		will fail because the pin cannot be found...
			// 2.	Using the 0-based index number of the input or output pin.
			//		This method is save. It simply tells to connect output pin #0 to input pin #1 for example.
			//
			// The code assumes method 1 is used. If that fails, method 2 is tried...

			Log.Write("DVBGraphBDA: Adding configured pin connections...");
			for (int i = 0; i < m_Card.TvConnectionDefinitions.Count; i++)
			{
				Log.Write(" {0}", i);
				sourceFilter = m_Card.TvFilterDefinitions[((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SourceCategory] as FilterDefinition;
				if (sourceFilter==null)
				{
					Log.Write("Cannot find source filter for connection:{0}",i);
					continue;
				}
				sinkFilter   = m_Card.TvFilterDefinitions[((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SinkCategory] as FilterDefinition;
				if (sinkFilter==null)
				{
					Log.Write("Cannot find sink filter for connection:{0}",i);
					continue;
				}

				Log.Write("DVBGraphBDA:  Connecting <{0}>:{1} with <{2}>:{3}", 
					sourceFilter.FriendlyName, ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SourcePinName,
					sinkFilter.FriendlyName, ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SinkPinName);
				//sourceFilter.DSFilter.FindPin(((ConnectionDefinition)m_Card.ConnectionDefinitions[i]).SourcePinName, out sourcePin);
				sourcePin    = DirectShowUtil.FindPin(sourceFilter.DSFilter, PinDirection.Output, ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SourcePinName);
				if (sourcePin == null)
				{
					String strPinName = ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SourcePinName;
					if ((strPinName.Length == 1) && (Char.IsDigit(strPinName, 0)))
					{
						sourcePin = DirectShowUtil.FindPinNr(sourceFilter.DSFilter, PinDirection.Output, Convert.ToInt32(strPinName));
						if (sourcePin==null)
							Log.Write("DVBGraphBDA:   Unable to find sourcePin: <{0}>", strPinName);
						else
							Log.Write("DVBGraphBDA:   Found sourcePin: <{0}> <{1}>", strPinName, sourcePin.ToString());
					}
				}
				else
					Log.Write("DVBGraphBDA:   Found sourcePin: <{0}> ", ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SourcePinName);

				//sinkFilter.DSFilter.FindPin(((ConnectionDefinition)m_Card.ConnectionDefinitions[i]).SinkPinName, out sinkPin);
				sinkPin      = DirectShowUtil.FindPin(sinkFilter.DSFilter, PinDirection.Input, ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SinkPinName);
				if (sinkPin == null)
				{
					String strPinName = ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SinkPinName;
					if ((strPinName.Length == 1) && (Char.IsDigit(strPinName, 0)))
					{
						sinkPin = DirectShowUtil.FindPinNr(sinkFilter.DSFilter, PinDirection.Input, Convert.ToInt32(strPinName));
						if (sinkPin==null)
							Log.Write("DVBGraphBDA:   Unable to find sinkPin: <{0}>", strPinName);
						else
							Log.Write("DVBGraphBDA:   Found sinkPin: <{0}> <{1}>", strPinName, sinkPin.ToString());
					}
				}
				else
					Log.Write("DVBGraphBDA:   Found sinkPin: <{0}> ", ((ConnectionDefinition)m_Card.TvConnectionDefinitions[i]).SinkPinName);

				if (sourcePin!=null && sinkPin!=null)
				{
					IPin conPin;
					hr      = sourcePin.ConnectedTo(out conPin);
					if (hr != 0)
						hr = m_graphBuilder.Connect(sourcePin, sinkPin);
					if (hr == 0)
						Log.Write("DVBGraphBDA:   Pins connected...");

					// Give warning and release pin...
					if (conPin != null)
					{
						Log.Write("DVBGraphBDA:   (Pin was already connected...)");
						Marshal.ReleaseComObject(conPin as Object);
						conPin = null;
						hr     = 0;
					}
				}

				if (hr != 0)
				{
					Log.Write("DVBGraphBDA:  Error: Unable to connect Pins 0x{0:X}", hr);
					if (hr == -2147220969)
					{
						Log.Write("DVBGraphBDA:   -- Cannot connect: {0} or {1}", sourcePin.ToString(), sinkPin.ToString());
					}
				}
			}
			Log.Write("DVBGraphBDA: Adding configured pin connections...DONE");

			// Find out which filter & pin is used as the interface to the rest of the graph.
			// The configuration defines the filter, including the Video, Audio and Mpeg2 pins where applicable
			// We only use the filter, as the software will find the correct pin for now...
			// This should be changed in the future, to allow custom graph endings (mux/no mux) using the
			// video and audio pins to connect to the rest of the graph (SBE, overlay etc.)
			// This might be needed by the ATI AIW cards (waiting for ob2 to release...)
			FilterDefinition lastFilter = m_Card.TvFilterDefinitions[m_Card.TvInterfaceDefinition.FilterCategory] as FilterDefinition;

			
			if(lastFilter == null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED interface filter not found");
			}

			//=========================================================================================================
			// add the MPEG-2 Demultiplexer 
			//=========================================================================================================
			// Use CLSID_Mpeg2Demultiplexer to create the filter
			m_MPEG2Demultiplexer = (IBaseFilter) Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.Mpeg2Demultiplexer, true));
			if(m_MPEG2Demultiplexer== null) 
			{
				Log.Write("DVBGraphBDA:Failed to create Mpeg2 Demultiplexer");
				return false;
			}

			// Add the Demux to the graph
			m_graphBuilder.AddFilter(m_MPEG2Demultiplexer, "MPEG-2 Demultiplexer");
			
			// The preview pin must be connected first so it can be registed with the network provider
			if(!ConnectFilters(ref lastFilter.DSFilter, ref m_MPEG2Demultiplexer)) 
			{
				Log.Write("DVBGraphBDA:Failed to connect interface filter->mpeg2 demultiplexer");
				return false;
			}
			/*
			IMPEG2StreamIdMap map;
			IPin pin;
			m_MPEG2Demultiplexer.FindPin("1", out pin);
			map = (IMPEG2StreamIdMap) pin;
			map.MapStreamId(0x12,1,0,0);
			map.MapStreamId(0x11,1,0,0);
			map.MapStreamId(0x10,1,0,0);
			map.MapStreamId(0x1 ,1,0,0);
*/
			//=========================================================================================================
			// Add the BDA MPEG2 Transport Information Filter
			//=========================================================================================================
			object tmpObject;
			if(!findNamedFilter(FilterCategories.KSCATEGORY_BDA_TRANSPORT_INFORMATION, "BDA MPEG2 Transport Information Filter", out tmpObject)) 
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED Failed to find BDA MPEG2 Transport Information Filter");
				return false;
			}
			m_TIF = (IBaseFilter) tmpObject;
			tmpObject = null;
			if(m_TIF == null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED BDA MPEG2 Transport Information Filter is null");
				return false;
			}
			m_graphBuilder.AddFilter(m_TIF, "BDA MPEG2 Transport Information Filter");
			if(!ConnectFilters(ref m_MPEG2Demultiplexer, ref m_TIF)) 
			{
				Log.Write("DVBGraphBDA:Failed to connect MPEG-2 Demultiplexer->BDA MPEG2 Transport Information Filter");
				return false;
			}

			//=========================================================================================================
			// Add the MPEG-2 Sections and Tables filter
			//=========================================================================================================
			if(!findNamedFilter(FilterCategories.KSCATEGORY_BDA_TRANSPORT_INFORMATION, "MPEG-2 Sections and Tables", out tmpObject)) 
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED Failed to find MPEG-2 Sections and Tables Filter");
				return false;
			}
			m_SectionsTables = (IBaseFilter) tmpObject;
			tmpObject = null;
			if(m_SectionsTables == null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED MPEG-2 Sections and Tables Filter is null");
			}
			m_graphBuilder.AddFilter(m_SectionsTables, "MPEG-2 Sections & Tables");
			if(!ConnectFilters(ref m_MPEG2Demultiplexer, ref m_SectionsTables)) 
			{
				Log.Write("DVBGraphBDA:Failed to connect MPEG-2 Demultiplexer to MPEG-2 Sections and Tables Filter");
				return false;
			}
			
			// MPEG-2 demultiplexer '1' -> BDA MPEG2 Transport Information Filter
			// MPEG-2 demultiplexer '2' -> MPEG-2 Sections and tables
			// MPEG-2 demultiplexer '3' -> video decoder
			// MPEG-2 demultiplexer '4' -> audio decoder
			// MPEG-2 demultiplexer '5' -> 

			m_MPEG2Demultiplexer.FindPin("3", out m_DemuxVideoPin);
			m_MPEG2Demultiplexer.FindPin("4", out m_DemuxAudioPin);
			if (m_DemuxVideoPin==null)
			{
				Log.Write("DVBGraphBDA:Failed to get pin '3' (video out) from MPEG-2 Demultiplexer");
				return false;
			}
			if (m_DemuxAudioPin==null)
			{
				Log.Write("DVBGraphBDA:Failed to get pin '4' (audio out)  from MPEG-2 Demultiplexer");
				return false;
			}


			// Initialise Tuning Space (using the setupTuningSpace function)
			if(!setupTuningSpace()) 
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt create tuning space");
				return false;
			}

			//=========================================================================================================
			// Create the streambuffer engine and mpeg2 video analyzer components since we need them for
			// recording and timeshifting
			//=========================================================================================================
			m_StreamBufferSink  = new StreamBufferSink();
			m_mpeg2Analyzer     = new VideoAnalyzer();
			m_IStreamBufferSink = (IStreamBufferSink) m_StreamBufferSink;
			m_graphState=State.Created;

			AdviseProgramInfo();
			return true;
		}
		
		void AdviseProgramInfo()
		{
			Log.Write("AdivseProgramInfo()");
			if (m_TIF==null) return;

			m_Event= new GuideDataEvent();
			myHandle = GCHandle.Alloc(m_Event, GCHandleType.Pinned);

			IConnectionPointContainer container = m_TIF as IConnectionPointContainer;
			if (container==null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt get IConnectionPointContainer");
				return ;
			}
			Guid iid=typeof(IGuideDataEvent).GUID;
			IConnectionPoint    connectPoint;      
			container.FindConnectionPoint( ref iid, out connectPoint);
			if (connectPoint==null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt get IGuideDataEvent");
				return ;
			}
			int hr=connectPoint.Advise( m_Event, out adviseCookie);
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt set advise");
				return ;
			}
			Log.Write("AdivseProgramInfo() done");
		}

		void UnAdviseProgramInfo()
		{
			Log.Write("UnAdviseProgramInfo()");
			if (adviseCookie==0) return;
			if (m_TIF==null) return;
			IConnectionPointContainer container = m_TIF as IConnectionPointContainer;
			if (container==null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt get IConnectionPointContainer");
				return ;
			}
			Guid iid=typeof(IGuideDataEvent).GUID;
			IConnectionPoint    connectPoint;      
			container.FindConnectionPoint( ref iid, out connectPoint);
			if (connectPoint==null)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt get IGuideDataEvent");
				return ;
			}

			int hr=connectPoint.Unadvise(adviseCookie);
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:CreateGraph() FAILED couldnt set advise");
			}
			adviseCookie=0;
			
			if (myHandle.IsAllocated)
			{
				myHandle.Free();
			}
			m_Event=null;
		}
		/// <summary>
		/// Deletes the current DirectShow graph created with CreateGraph()
		/// </summary>
		/// <remarks>
		/// Graph must be created first with CreateGraph()
		/// </remarks>
		public void DeleteGraph()
		{
			if (m_graphState < State.Created) 
				return;
			m_iPrevChannel = -1;
	
			Log.Write("DVBGraphBDA:DeleteGraph()");
			StopRecording();
			StopViewing();

			if (Vmr9!=null)
			{
				Vmr9.RemoveVMR9();
				Vmr9=null;
			}

			UnAdviseProgramInfo();
			if (m_recControl!=null) 
			{
				m_recControl.Stop(0);
				Marshal.ReleaseComObject(m_recControl); m_recControl=null;
			}
		      
			if (m_StreamBufferSink!=null) 
			{
				Marshal.ReleaseComObject(m_StreamBufferSink); m_StreamBufferSink=null;
			}

			if (m_mediaControl != null)
				m_mediaControl.Stop();
     
			graphRunning=false;

			if (m_videoWindow != null)
			{
				m_videoWindow.put_Visible(DsHlp.OAFALSE);
				m_videoWindow.put_Owner(IntPtr.Zero);
				m_videoWindow = null;
			}

			if (m_StreamBufferConfig != null) 
				Marshal.ReleaseComObject(m_StreamBufferConfig); m_StreamBufferConfig=null;

			if (m_IStreamBufferConfig != null) 
				Marshal.ReleaseComObject(m_IStreamBufferConfig); m_IStreamBufferConfig=null;

			if (m_pinStreamBufferIn1 != null) 
				Marshal.ReleaseComObject(m_pinStreamBufferIn1); m_pinStreamBufferIn1=null;

			if (m_pinStreamBufferIn0 != null) 
				Marshal.ReleaseComObject(m_pinStreamBufferIn0); m_pinStreamBufferIn0=null;

			if (m_IStreamBufferSink != null) 
				Marshal.ReleaseComObject(m_IStreamBufferSink); m_IStreamBufferSink=null;

			if (m_NetworkProvider != null)
				Marshal.ReleaseComObject(m_NetworkProvider); m_NetworkProvider = null;

			if (m_TunerDevice != null)
				Marshal.ReleaseComObject(m_TunerDevice); m_TunerDevice = null;

			if (m_CaptureDevice != null)
				Marshal.ReleaseComObject(m_CaptureDevice); m_CaptureDevice = null;
			
			if (m_MPEG2Demultiplexer != null)
				Marshal.ReleaseComObject(m_MPEG2Demultiplexer); m_MPEG2Demultiplexer = null;

			if (m_TIF != null)
				Marshal.ReleaseComObject(m_TIF); m_TIF = null;

			if (m_SectionsTables != null)
				Marshal.ReleaseComObject(m_SectionsTables); m_SectionsTables = null;

			m_basicVideo = null;
			m_mediaControl = null;
		      
			DsUtils.RemoveFilters(m_graphBuilder);

			if (m_rotCookie != 0)
				DsROT.RemoveGraphFromRot(ref m_rotCookie);
			m_rotCookie = 0;

			if (m_captureGraphBuilder != null)
				Marshal.ReleaseComObject(m_captureGraphBuilder); m_captureGraphBuilder = null;

			if (m_graphBuilder != null)
				Marshal.ReleaseComObject(m_graphBuilder); m_graphBuilder = null;


			foreach (string strfName in m_Card.TvFilterDefinitions.Keys)
			{
				FilterDefinition dsFilter = m_Card.TvFilterDefinitions[strfName] as FilterDefinition;
				if (dsFilter.DSFilter != null)
					Marshal.ReleaseComObject(dsFilter.DSFilter);
				((FilterDefinition)m_Card.TvFilterDefinitions[strfName]).DSFilter = null;
				dsFilter = null;
			}

			m_graphState = State.None;
			return;
		}
		
		/// <summary>
		/// Stops recording 
		/// </summary>
		/// <remarks>
		/// Graph should be recording. When Recording is stopped the graph is still 
		/// timeshifting
		/// </remarks>
		public void StopRecording()
		{
			if (m_graphState != State.Recording) return;
			Log.Write("DVBGraphBDA:stop recording...");

			if (m_recControl!=null) 
			{
				int hr=m_recControl.Stop(0);
				if (hr!=0) 
				{
					Log.Write("DVBGraphBDA: FAILED to stop recording:0x{0:x}",hr );
					return;
				}
				if (m_recControl!=null) Marshal.ReleaseComObject(m_recControl); m_recControl=null;
			}


			Log.Write("DVBGraphBDA: stop graph");
			m_mediaControl.Stop();
			graphRunning=false;
			m_graphState = State.Created;
			DeleteGraph();
			Log.Write("DVBGraphBDA:stopped recording...");
		}



		/// <summary>
		/// Switches / tunes to another TV channel
		/// </summary>
		/// <param name="iChannel">New channel</param>
		/// <remarks>
		/// Graph should be timeshifting. 
		/// </remarks>
		public void TuneChannel(AnalogVideoStandard standard,int iChannel,int country)
		{
			TestTune();
			return;
			Log.Write("DVBGraphBDA:TuneChannel() tune to channel:{0}", iChannel);
			TunerLib.TuneRequest newTuneRequest = null;
			TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;

			switch (m_NetworkType)
			{
				case NetworkType.ATSC: 
				{
					//TunerLib.IATSCTuningSpace myTuningSpace = (TunerLib.IATSCTuningSpace) m_TuningSpace;
					//newTuneRequest = myTuningSpace.CreateTuneRequest();
					//newTuneRequest = TVDatabase.GetTuneRequest(iChannel, "dvbt", newTuneRequest);
				} break;
				case NetworkType.DVBC: 
				{
					TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) myTuner.TuningSpace;
					newTuneRequest = myTuningSpace.CreateTuneRequest();
					newTuneRequest = TVDatabase.GetTuneRequest(iChannel, "dvbc", newTuneRequest);
				} break;
				case NetworkType.DVBS: 
				{
					TunerLib.IDVBSTuningSpace myTuningSpace = (TunerLib.IDVBSTuningSpace) myTuner.TuningSpace;
					newTuneRequest = myTuningSpace.CreateTuneRequest();
					newTuneRequest = TVDatabase.GetTuneRequest(iChannel, "dvbs", newTuneRequest);
				} break;
				case NetworkType.DVBT: 
				{
					TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) myTuner.TuningSpace;
					newTuneRequest = myTuningSpace.CreateTuneRequest();
					newTuneRequest = TVDatabase.GetTuneRequest(iChannel, "dvbt", newTuneRequest);
				} break;
			}

			// Submit the Tune Request
			if(m_NetworkProvider == null)
				return;

			if(myTuner != null) 
			{
				if(newTuneRequest == null)
				{
					Log.Write("DVBGraphBDA:TuneChannel() FAILED tv database does not contain tuning information for channel:{0}", iChannel);
					return;
				}
				// Submit the Tune Request to the Tuner (put_TuneRequest)
				myTuner.TuneRequest = newTuneRequest;
			} 
			else
			{
				Log.Write("DVBGraphBDA:CreateTuneRequest() FAILED interfacing ITuner with Network Provider");
				return;
			}
			// Release the Tune Request & Locator
			Marshal.ReleaseComObject(newTuneRequest);

			m_iPrevChannel		= m_iCurrentChannel;
			m_iCurrentChannel = iChannel;
			m_StartTime				= DateTime.Now;
		}

		/// <summary>
		/// Returns the current tv channel
		/// </summary>
		/// <returns>Current channel</returns>
		public int GetChannelNumber()
		{
			return m_iCurrentChannel;
		}

		/// <summary>
		/// Property indiciating if the graph supports timeshifting
		/// </summary>
		/// <returns>boolean indiciating if the graph supports timeshifting</returns>
		public bool SupportsTimeshifting()
		{
			return m_bIsUsingMPEG;
		}

		void AddPreferredCodecs()
		{				
			// add preferred video & audio codecs
			string strVideoCodec="";
			string strAudioCodec="";
			bool   bAddFFDshow=false;
			using (AMS.Profile.Xml   xmlreader=new AMS.Profile.Xml("MediaPortal.xml"))
			{
				bAddFFDshow=xmlreader.GetValueAsBool("mytv","ffdshow",false);
				strVideoCodec=xmlreader.GetValueAsString("mytv","videocodec","");
				strAudioCodec=xmlreader.GetValueAsString("mytv","audiocodec","");
			}
			if (strVideoCodec.Length>0) DirectShowUtil.AddFilterToGraph(m_graphBuilder,strVideoCodec);
			if (strAudioCodec.Length>0) DirectShowUtil.AddFilterToGraph(m_graphBuilder,strAudioCodec);
			if (bAddFFDshow) DirectShowUtil.AddFilterToGraph(m_graphBuilder,"ffdshow raw video filter");
		}
		
		/// <summary>
		/// Starts viewing the TV channel 
		/// </summary>
		/// <param name="iChannelNr">TV channel to which card should be tuned</param>
		/// <returns>boolean indicating if succeed</returns>
		/// <remarks>
		/// Graph must be created first with CreateGraph()
		/// </remarks>
		public bool StartViewing(AnalogVideoStandard standard, int iChannel,int country)
		{
			if (m_graphState != State.Created) return false;
			
			Log.Write("DVBGraphBDA:StartViewing()");

			// add VMR9 renderer
			Vmr9.AddVMR9(m_graphBuilder);

			// add the preferred video/audio codecs
			AddPreferredCodecs();

			// render the video/audio pins so they get connected to the video/audio codecs
			if(m_graphBuilder.Render(m_DemuxVideoPin) != 0)
			{
				Log.Write("DVBGraphBDA:Failed to render video out pin MPEG-2 Demultiplexer");
				return false;
			}
			if(m_graphBuilder.Render(m_DemuxAudioPin) != 0)
			{
				Log.Write("DVBGraphBDA:Failed to render audio out pin MPEG-2 Demultiplexer");
				return false;
			}

			if(m_mediaControl == null)
				m_mediaControl = (IMediaControl) m_graphBuilder;

			int hr;
			//if are using the overlay video renderer
			if (!Vmr9.UseVMR9inMYTV)
			{
				//then get the overlay video renderer interfaces
				m_videoWindow = m_graphBuilder as IVideoWindow;
				if (m_videoWindow == null)
				{
					Log.Write("DVBGraphBDA:FAILED:Unable to get IVideoWindow");
					return false;
				}

				m_basicVideo = m_graphBuilder as IBasicVideo2;
				if (m_basicVideo == null)
				{
					Log.Write("DVBGraphBDA:FAILED:Unable to get IBasicVideo2");
					return false;
				}

				// and set it up
				hr = m_videoWindow.put_Owner(GUIGraphicsContext.form.Handle);
				if (hr != 0) 
					Log.Write("DVBGraphBDA: FAILED:set Video window:0x{0:X}",hr);

				hr = m_videoWindow.put_WindowStyle(WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS);
				if (hr != 0) 
					Log.Write("DVBGraphBDA: FAILED:set Video window style:0x{0:X}",hr);

	      //show overlay window
				hr = m_videoWindow.put_Visible(DsHlp.OATRUE);
				if (hr != 0) 
					Log.Write("DVBGraphBDA: FAILED:put_Visible:0x{0:X}",hr);
			}

			//start the graph
			Log.Write("DVBGraphBDA: start graph");
			hr=m_mediaControl.Run();
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA: FAILED unable to start graph :0x{0:X}", hr);
			}
			graphRunning=true;
			
			GUIGraphicsContext.OnVideoWindowChanged += new VideoWindowChangedHandler(GUIGraphicsContext_OnVideoWindowChanged);
			m_graphState = State.Viewing;
			GUIGraphicsContext_OnVideoWindowChanged();


			// tune to the correct channel
			TuneChannel(standard,iChannel,country);

			Log.Write("DVBGraphBDA:Viewing..");
			return true;
		}


		/// <summary>
		/// Stops viewing the TV channel 
		/// </summary>
		/// <returns>boolean indicating if succeed</returns>
		/// <remarks>
		/// Graph must be viewing first with StartViewing()
		/// </remarks>
		public bool StopViewing()
		{
			if (m_graphState != State.Viewing) return false;
	       
			GUIGraphicsContext.OnVideoWindowChanged -= new VideoWindowChangedHandler(GUIGraphicsContext_OnVideoWindowChanged);
			Log.Write("DVBGraphBDA: StopViewing()");
			m_videoWindow.put_Visible(DsHlp.OAFALSE);

			Log.Write("DVBGraphBDA: stop graph");
			m_mediaControl.Stop();
			m_graphState = State.Created;
			DeleteGraph();
			return true;
		}
		
		/// <summary>
		/// Callback from GUIGraphicsContext. Will get called when the video window position or width/height changes
		/// </summary>
		private void GUIGraphicsContext_OnVideoWindowChanged()
		{
			if (m_graphState != State.Viewing) return;
			int iVideoWidth, iVideoHeight;
			if (Vmr9.UseVMR9inMYTV)
			{
				iVideoWidth=Vmr9.VideoWidth;
				iVideoHeight=Vmr9.VideoHeight;
			}
			else
			{
				m_basicVideo.GetVideoSize(out iVideoWidth, out iVideoHeight);
			}
			/*if (GUIGraphicsContext.Overlay==false)
			{
				if (Overlay!=false)
				{
					Log.Write("DVBGraphBDA:overlay disabled");
					Overlay=false;
				}
				return;
			}
			else
			{
				if (Overlay!=true)
				{
					Log.Write("DVBGraphBDA:overlay enabled");
					Overlay=true;
				}
			}*/
      
			if (GUIGraphicsContext.IsFullScreenVideo)
			{
				float x = GUIGraphicsContext.OverScanLeft;
				float y = GUIGraphicsContext.OverScanTop;
				int nw = GUIGraphicsContext.OverScanWidth;
				int nh = GUIGraphicsContext.OverScanHeight;
				if (nw <= 0 || nh <= 0) return;


				System.Drawing.Rectangle rSource, rDest;
				MediaPortal.GUI.Library.Geometry m_geometry = new MediaPortal.GUI.Library.Geometry();
				m_geometry.ImageWidth = iVideoWidth;
				m_geometry.ImageHeight = iVideoHeight;
				m_geometry.ScreenWidth = nw;
				m_geometry.ScreenHeight = nh;
				m_geometry.ARType = GUIGraphicsContext.ARType;
				m_geometry.PixelRatio = GUIGraphicsContext.PixelRatio;
				m_geometry.GetWindow(out rSource, out rDest);
				rDest.X += (int)x;
				rDest.Y += (int)y;

				if (!Vmr9.UseVMR9inMYTV)
				{
					m_basicVideo.SetSourcePosition(rSource.Left, rSource.Top, rSource.Width, rSource.Height);
					m_basicVideo.SetDestinationPosition(0, 0, rDest.Width, rDest.Height);
					m_videoWindow.SetWindowPosition(rDest.Left, rDest.Top, rDest.Width, rDest.Height);
					Log.Write("DVBGraphBDA: capture size:{0}x{1}",iVideoWidth, iVideoHeight);
					Log.Write("DVBGraphBDA: source position:({0},{1})-({2},{3})",rSource.Left, rSource.Top, rSource.Right, rSource.Bottom);
					Log.Write("DVBGraphBDA: dest   position:({0},{1})-({2},{3})",rDest.Left, rDest.Top, rDest.Right, rDest.Bottom);
				}
			}
			else
			{
				if (!Vmr9.UseVMR9inMYTV)
				{
					m_basicVideo.SetSourcePosition(0, 0, iVideoWidth, iVideoHeight);
					m_basicVideo.SetDestinationPosition(0, 0, GUIGraphicsContext.VideoWindow.Width, GUIGraphicsContext.VideoWindow.Height);
					m_videoWindow.SetWindowPosition(GUIGraphicsContext.VideoWindow.Left, GUIGraphicsContext.VideoWindow.Top, GUIGraphicsContext.VideoWindow.Width, GUIGraphicsContext.VideoWindow.Height);
					Log.Write("DVBGraphBDA: capture size:{0}x{1}",iVideoWidth, iVideoHeight);
					Log.Write("DVBGraphBDA: source position:({0},{1})-({2},{3})",0, 0, iVideoWidth, iVideoHeight);
					Log.Write("DVBGraphBDA: dest   position:({0},{1})-({2},{3})",GUIGraphicsContext.VideoWindow.Left, GUIGraphicsContext.VideoWindow.Top, GUIGraphicsContext.VideoWindow.Right, GUIGraphicsContext.VideoWindow.Bottom);
				}

			}
		}
		
		/// <summary>
		/// This method can be used to ask the graph if it should be rebuild when
		/// we want to tune to the new channel:ichannel
		/// </summary>
		/// <param name="iChannel">new channel to tune to</param>
		/// <returns>true : graph needs to be rebuild for this channel
		///          false: graph does not need to be rebuild for this channel
		/// </returns>
		public bool ShouldRebuildGraph(int iChannel)
		{
			return false;
		}

		public bool SignalPresent()
		{

			if(m_graphState==State.Created || m_graphState==State.None)
				return false;
			IBDA_Topology topology = m_TunerDevice as IBDA_Topology;
			if (topology==null)
			{
				Log.Write("DVBGraphBDA: could not get IBDA_Topology from tuner");
				return false;
			}
			int nodeTypeCount=0;
			int interfaceCount=0;
			int[] nodeTypes = new int[33];
			Guid[] guidInterfaces = new Guid[33];
			int hr=topology.GetNodeTypes(ref nodeTypeCount,32, nodeTypes);
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA: FAILED could not get node types from tuner");
				return false;
			}
			for (int i=0; i < nodeTypeCount;++i)
			{
				hr=topology.GetNodeInterfaces(i,ref interfaceCount,32,guidInterfaces);
				if (hr!=0)
				{
					Log.Write("DVBGraphBDA: FAILED could not GetNodeInterfaces for node:{0}",hr);
					return false;
				}
				else
				{
					for (int j=0; j < interfaceCount; ++j)
					{
						if ( guidInterfaces[i]== typeof(IBDA_SignalStatistics).GUID)
						{
							object objectNode;
							hr=topology.GetControlNode(0,1, nodeTypes[i], out objectNode);
							if (hr!=0)
							{
								Log.Write("DVBGraphBDA: FAILED could not GetControlNode for node:{0}",hr);
								return false;
							}
							IBDA_SignalStatistics signal = objectNode as IBDA_SignalStatistics;
							if (signal==null)
							{
								Log.Write("DVBGraphBDA: FAILED could not IBDA_SignalStatistics for node:{0}",hr);
								return false;
							}
							int strength=0;
							int quality=0;
							bool locked=false;
							bool present=false;
							hr=signal.get_SignalStrength(out strength);
							hr=signal.get_SignalQuality(out quality);
							hr=signal.get_SignalLocked(out locked);
							hr=signal.get_SignalPresent(out present);
							Marshal.ReleaseComObject(objectNode);
							if (locked || quality>0)
							{
								Marshal.ReleaseComObject(topology);
								return true;
							}
						}
					}
				}
			}
			Marshal.ReleaseComObject(topology);
			return false;
		}

		public long VideoFrequency()
		{
			return -1;
		}
		
		private bool CreateSinkSource(string fileName)
		{
			if(m_graphState!=State.Created)
				return false;
			int		hr				= 0;
			IPin	pinObj0		= null;
			IPin	pinObj1		= null;
			IPin	outPin		= null;

			Log.Write("DVBGraphBDA:CreateSinkSource()");

			Log.Write("DVBGraphBDA:Add streambuffersink()");
			hr=m_graphBuilder.AddFilter((IBaseFilter)m_StreamBufferSink,"StreamBufferSink");
			if(hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED cannot add StreamBufferSink");
				return false;
			}			
			Log.Write("DVBGraphBDA:Add mpeg2 analyzer()");
			hr=m_graphBuilder.AddFilter((IBaseFilter)m_mpeg2Analyzer,"Mpeg2 Analyzer");
			if(hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED cannot add mpeg2 analyzer to graph");
				return false;
			}

			Log.Write("DVBGraphBDA:find mpeg2 analyzer input pin()");
			pinObj0=DirectShowUtil.FindPinNr((IBaseFilter)m_mpeg2Analyzer,PinDirection.Input,0);
			if(pinObj0 == null)
			{
				Log.Write("DVBGraphBDA:FAILED cannot find mpeg2 analyzer input pin");
				return false;
			}
			Log.Write("DVBGraphBDA:connect demux video output->mpeg2 analyzer");
			hr=m_graphBuilder.Connect(m_DemuxVideoPin, pinObj0) ;
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED to connect demux video output->mpeg2 analyzer");
				return false;
			}

			Log.Write("DVBGraphBDA:mpeg2 analyzer output->streambuffersink in");
			pinObj1 = DirectShowUtil.FindPinNr((IBaseFilter)m_mpeg2Analyzer, PinDirection.Output, 0);	
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED cannot find mpeg2 analyzer output pin");
				return false;
			}
			IPin pinObj2 = DirectShowUtil.FindPinNr((IBaseFilter)m_StreamBufferSink, PinDirection.Input, 0);	
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED cannot find SBE input pin");
				return false;
			}
			hr=m_graphBuilder.Connect(pinObj1, pinObj2) ;
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED to connect mpeg2 analyzer->streambuffer sink");
				return false;
			}
			pinObj2 = DirectShowUtil.FindPinNr((IBaseFilter)m_StreamBufferSink, PinDirection.Input, 1);	
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED cannot find SBE input pin#2");
				return false;
			}
			hr=m_graphBuilder.Connect(m_DemuxAudioPin, pinObj2) ;
			if (hr!=0)
			{
				Log.Write("DVBGraphBDA:FAILED to connect mpeg2 demuxer audio out->streambuffer sink in#2");
				return false;
			}

			int ipos=fileName.LastIndexOf(@"\");
			string strDir=fileName.Substring(0,ipos);

			m_StreamBufferConfig	= new StreamBufferConfig();
			m_IStreamBufferConfig	= (IStreamBufferConfigure) m_StreamBufferConfig;
			// setting the timeshift behaviors
			IntPtr HKEY = (IntPtr) unchecked ((int)0x80000002L);
			IStreamBufferInitialize pTemp = (IStreamBufferInitialize) m_IStreamBufferConfig;
			IntPtr subKey = IntPtr.Zero;

			RegOpenKeyEx(HKEY, "SOFTWARE\\MediaPortal", 0, 0x3f, out subKey);
			hr=pTemp.SetHKEY(subKey);
			
			
			Log.Write("DVBGraphBDA:set timeshift folder to:{0}", strDir);
			hr = m_IStreamBufferConfig.SetDirectory(strDir);	
			if(hr != 0)
			{
				Log.Write("DVBGraphBDA:FAILED to set timeshift folder to:{0}", strDir);
				return false;
			}
			hr = m_IStreamBufferConfig.SetBackingFileCount(6, 8);    //4-6 files
			if(hr != 0)
				return false;
			
			hr = m_IStreamBufferConfig.SetBackingFileDuration(300); // 60sec * 4 files= 4 mins
			if(hr != 0)
				return false;

			subKey = IntPtr.Zero;
			HKEY = (IntPtr) unchecked ((int)0x80000002L);
			IStreamBufferInitialize pConfig = (IStreamBufferInitialize) m_StreamBufferSink;

			RegOpenKeyEx(HKEY, "SOFTWARE\\MediaPortal", 0, 0x3f, out subKey);
			hr = pConfig.SetHKEY(subKey);

			
			Log.Write("DVBGraphBDA:set timeshift file to:{0}", fileName);
			// lock on the 'filename' file
			if(m_IStreamBufferSink.LockProfile(fileName) != 0)
			{
				Log.Write("DVBGraphBDA:FAILED to set timeshift file to:{0}", fileName);
				return false;
			}
			if(pinObj0 != null)
				Marshal.ReleaseComObject(pinObj0);
			if(pinObj1 != null)
				Marshal.ReleaseComObject(pinObj1);
			if(pinObj2 != null)
				Marshal.ReleaseComObject(pinObj2);
			if(outPin != null)
				Marshal.ReleaseComObject(outPin);

			return true;
		}
		
		/// <summary>
		/// Starts recording live TV to a file
		/// <param name="strFileName">filename for the new recording</param>
		/// <param name="bContentRecording">Specifies whether a content or reference recording should be made</param>
		/// <param name="timeProgStart">Contains the starttime of the current tv program</param>
		/// </summary>
		/// <returns>boolean indicating if recorded is started or not</returns> 
		/// <remarks>
		/// Graph should be timeshifting. When Recording is started the graph is still 
		/// timeshifting
		/// 
		/// A content recording will start recording from the moment this method is called
		/// and ignores any data left/present in the timeshifting buffer files
		/// 
		/// A reference recording will start recording from the moment this method is called
		/// It will examine the timeshifting files and try to record as much data as is available
		/// from the timeProgStart till the moment recording is stopped again
		/// </remarks>
		public bool StartRecording(int country,AnalogVideoStandard standard,int iChannelNr, ref string strFileName, bool bContentRecording, DateTime timeProgStart)
		{
			if (m_graphState != State.TimeShifting) 
				return false;
			if (m_StreamBufferSink == null) 
				return false;

			Log.Write("DVBGraphBDA:StartRecording()");
			uint iRecordingType=0;
			if (bContentRecording) 
				iRecordingType = 0;
			else 
				iRecordingType = 1;										

			IntPtr recorderObj;
			if (m_IStreamBufferSink.CreateRecorder(strFileName, iRecordingType, out recorderObj) !=0 ) 
				return false;

			object objRecord = Marshal.GetObjectForIUnknown(recorderObj);
			if (objRecord == null) 
				if (m_recControl == null) 
				{
					Log.Write("DVBGraphBDA:FAILED to get IRecorder");
					return false;
				}
      
			Marshal.Release(recorderObj);

			m_recControl = objRecord as IStreamBufferRecordControl;
			if (m_recControl == null) 
			{
				Log.Write("DVBGraphBDA:FAILED to get IStreamBufferRecordControl");
				return false;
			}
			long lStartTime = 0;

			// if we're making a reference recording
			// then record all content from the past as well
			if (!bContentRecording)
			{
				// so set the startttime...
				uint uiSecondsPerFile;
				uint uiMinFiles, uiMaxFiles;
				m_IStreamBufferConfig.GetBackingFileCount(out uiMinFiles, out uiMaxFiles);
				m_IStreamBufferConfig.GetBackingFileDuration(out uiSecondsPerFile);
				lStartTime = uiSecondsPerFile;
				lStartTime *= (long)uiMaxFiles;

				// if start of program is given, then use that as our starttime
				if (timeProgStart.Year > 2000)
				{
					TimeSpan ts = DateTime.Now - timeProgStart;
					Log.Write("DVBGraphBDA: Start recording from {0}:{1:00}:{2:00} which is {3:00}:{4:00}:{5:00} in the past",
						timeProgStart.Hour, timeProgStart.Minute, timeProgStart.Second,
						ts.TotalHours, ts.TotalMinutes, ts.TotalSeconds);
															
					lStartTime = (long)ts.TotalSeconds;
				}
				else Log.Write("DVBGraphBDA: record entire timeshift buffer");
      
				TimeSpan tsMaxTimeBack = DateTime.Now - m_StartTime;
				if (lStartTime > tsMaxTimeBack.TotalSeconds)
				{
					lStartTime = (long)tsMaxTimeBack.TotalSeconds;
				}
        

				lStartTime *= -10000000L;//in reference time 
			}
			if (m_recControl.Start(ref lStartTime) != 0) 
			{
				//could not start recording...
				if (lStartTime != 0)
				{
					// try recording from livepoint instead from the past
					lStartTime = 0;
					if (m_recControl.Start(ref lStartTime) != 0)
						return false;
				}
				else
					return false;
			}
			m_graphState = State.Recording;
			return true;
		}
	    


		/// <summary>
		/// Starts timeshifting the TV channel and stores the timeshifting 
		/// files in the specified filename
		/// </summary>
		/// <param name="iChannelNr">TV channel to which card should be tuned</param>
		/// <param name="strFileName">Filename for the timeshifting buffers</param>
		/// <returns>boolean indicating if timeshifting is running or not</returns>
		/// <remarks>
		/// Graph must be created first with CreateGraph()
		/// </remarks>
		public bool StartTimeShifting(int country,AnalogVideoStandard standard, int iChannel, string strFileName)
		{
			if(m_graphState!=State.Created)
				return false;
			
			Log.Write("DVBGraphBDA:StartTimeShifting()");

			if(CreateSinkSource(strFileName))
			{
				if(m_mediaControl == null) 
				{
					m_mediaControl = (IMediaControl) m_graphBuilder;
				}
				//now start the graph
				Log.Write("DVBGraphBDA: start graph");
				int hr=m_mediaControl.Run();
				if (hr!=0)
				{
					Log.Write("DVBGraphBDA: FAILED unable to start graph :0x{0:X}", hr);
				}
				graphRunning=true;
				m_graphState = State.TimeShifting;
			}
			else 
			{
				Log.Write("DVBGraphBDA:Unable to create sinksource()");
				return false;
			}
			TuneChannel(standard, iChannel,country);

			Log.Write("DVBGraphBDA:timeshifting started");			
			return true;
		}
		
		/// <summary>
		/// Stops timeshifting and cleans up the timeshifting files
		/// </summary>
		/// <returns>boolean indicating if timeshifting is stopped or not</returns>
		/// <remarks>
		/// Graph should be timeshifting 
		/// </remarks>
		public bool StopTimeShifting()
		{
			if (m_graphState != State.TimeShifting) return false;
			Log.Write("DVBGraphBDA: StopTimeShifting()");
			m_mediaControl.Stop();
			m_graphState = State.Created;
			DeleteGraph();
			return true;
		}
		
		/// <summary>
		/// Finds and connects pins
		/// </summary>
		/// <param name="UpstreamFilter">The Upstream filter which has the output pin</param>
		/// <param name="DownstreamFilter">The downstream filter which has the input filter</param>
		/// <returns>true if succeeded, false if failed</returns>
		private bool ConnectFilters(ref IBaseFilter UpstreamFilter, ref IBaseFilter DownstreamFilter) 
		{
			if (UpstreamFilter == null || DownstreamFilter == null)
				return false;

			int ulFetched = 0;
			int hr = 0;
			IEnumPins pinEnum;

			hr = UpstreamFilter.EnumPins( out pinEnum );
			if((hr < 0) || (pinEnum == null))
				return false;

			IPin[] testPin = new IPin[1];
			while(pinEnum.Next(1, testPin, out ulFetched) == 0) 
			{	
				PinDirection pinDir;
				testPin[0].QueryDirection(out pinDir);
				if(pinDir == PinDirection.Output) // Go and find the input pin.
				{
					IEnumPins downstreamPins;

					DownstreamFilter.EnumPins(out downstreamPins);

					IPin[] dsPin = new IPin[1];
					while(downstreamPins.Next(1, dsPin, out ulFetched) == 0) 
					{
						PinDirection dsPinDir;
						dsPin[0].QueryDirection(out dsPinDir);
						if(dsPinDir == PinDirection.Input) 
						{
							hr = m_graphBuilder.Connect(testPin[0], dsPin[0]);
							if(hr != 0) 
							{
								Marshal.ReleaseComObject(dsPin[0]);
								continue;
							} 
							else 
							{
								//								try 
								//								{
								//									if(m_MediaControl.Run() == 0) 
								//									{
								//										m_MediaControl.Stop();
								//										return true;
								//									}
								//								} 
								//								catch 
								//								{
								//									Marshal.ReleaseComObject(dsPin[0]);
								//									continue;
								//								}
								return true;
							}
						}
					}
					Marshal.ReleaseComObject(downstreamPins);
				}
				Marshal.ReleaseComObject(testPin[0]);
			}
			Marshal.ReleaseComObject(pinEnum);
			return false;
		}

		/// <summary>
		/// This is the function for setting up a local tuning space.
		/// </summary>
		/// <returns>true if succeeded, fale if failed</returns>
		private bool setupTuningSpace() 
		{
			//int hr = 0;

			Log.Write("DVBGraphBDA: setupTuningSpace()");
			if(m_NetworkProvider != null) 
			{
				System.Guid classID;
				m_NetworkProvider.GetClassID(out classID);

				string strClassID = classID.ToString();

				strClassID = strClassID.ToLower();

				//Log.Write("m_NetworkProvider:{0}", strClassID);

				switch (strClassID) 
				{
					case "0dad2fdd-5fd7-11d3-8f50-00c04f7971e2":
						m_NetworkType = NetworkType.ATSC;
						break;
					case "dc0c0fe7-0485-4266-b93f-68fbf80ed834":
						m_NetworkType = NetworkType.DVBC;
						break;
					case "fa4b375a-45b4-4d45-8440-263957b11623":
						m_NetworkType = NetworkType.DVBS;
						break;
					case "216c62df-6d7f-4e9a-8571-05f14edb766a":
						m_NetworkType = NetworkType.DVBT;
						break;
					default:
						Log.Write("DVBGraphBDA: FAILED:unknown network type:{0} ",classID);
						return false;
				}
			}
			else 
			{
				Log.Write("DVBGraphBDA: FAILED:network provider is null ");
				return false;
			}


			TunerLib.ITuningSpaceContainer TuningSpaceContainer = (TunerLib.ITuningSpaceContainer) Activator.CreateInstance(Type.GetTypeFromCLSID(TuningSpaces.CLSID_SystemTuningSpaces, true));
			if(TuningSpaceContainer == null)
			{
				Log.Write("DVBGraphBDA: Failed to get ITuningSpaceContainer");
				return false;
			}

			TunerLib.ITuningSpaces myTuningSpaces = null;
			string uniqueName="";

			switch (m_NetworkType) 
			{
				case NetworkType.ATSC:
				{
					myTuningSpaces = TuningSpaceContainer._TuningSpacesForCLSID(ref TuningSpaces.CLSID_ATSCTuningSpace);
					//ATSCInputType = "Antenna"; // Need to change to allow cable
					uniqueName="Mediaportal ATSC";
				} break;
				case NetworkType.DVBC:
				{
					myTuningSpaces = TuningSpaceContainer._TuningSpacesForCLSID(ref TuningSpaces.CLSID_DVBTuningSpace);
					uniqueName="Mediaportal DVB-C";
				} break;
				case NetworkType.DVBS:
				{
					myTuningSpaces = TuningSpaceContainer._TuningSpacesForCLSID(ref TuningSpaces.CLSID_DVBSTuningSpace);
					uniqueName="Mediaportal DVB-S";
				} break;
				case NetworkType.DVBT:
				{
					myTuningSpaces = TuningSpaceContainer._TuningSpacesForCLSID(ref TuningSpaces.CLSID_DVBTuningSpace);
					uniqueName="Mediaportal DVB-T";
				} break;
			}
			TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;

			int Count = 0;
			Count = myTuningSpaces.Count;
			if(Count > 0)
			{
				Log.Write("DVBGraphBDA: found {0} tuning spaces", Count);
				TunerLib.IEnumTuningSpaces TuneEnum = myTuningSpaces.EnumTuningSpaces;

				uint ulFetched = 0;
				TunerLib.TuningSpace TuningSpace;
				int counter = 0;
				TuneEnum.Reset();
				while(counter <= Count)
				{
					TuneEnum.Next(1, out TuningSpace, out ulFetched);
					if (TuningSpace.UniqueName==uniqueName)
					{
						myTuner.TuningSpace = TuningSpace;
						Log.Write("DVBGraphBDA: used tuningspace:{0} {1} {2}", counter, TuningSpace.UniqueName,TuningSpace.FriendlyName);
						return true;
					}
				}
				Marshal.ReleaseComObject(myTuningSpaces);
				Marshal.ReleaseComObject(TuningSpaceContainer);
			}
			else
			{
				TunerLib.ITuningSpace TuningSpace ;
				Log.Write("DVBGraphBDA: create new tuningspace");
				switch (m_NetworkType) 
				{
					case NetworkType.ATSC: 
					{
						TuningSpace = (TunerLib.ITuningSpace) Activator.CreateInstance(Type.GetTypeFromCLSID(TuningSpaces.CLSID_ATSCTuningSpace, true));
						TunerLib.IATSCTuningSpace myTuningSpace = (TunerLib.IATSCTuningSpace) TuningSpace;
						myTuningSpace.set__NetworkType(ref NetworkProviders.CLSID_ATSCNetworkProvider);
						myTuningSpace.InputType = TunerLib.tagTunerInputType.TunerInputAntenna;
						myTuningSpace.MaxChannel			= 99;
						myTuningSpace.MaxMinorChannel		= 999;
						myTuningSpace.MaxPhysicalChannel	= 69;
						myTuningSpace.MinChannel			= 1;
						myTuningSpace.MinMinorChannel		= 0;
						myTuningSpace.MinPhysicalChannel	= 2;
						myTuningSpace.FriendlyName=uniqueName;
						myTuningSpace.UniqueName=uniqueName;

						TunerLib.Locator DefaultLocator = (TunerLib.Locator) Activator.CreateInstance(Type.GetTypeFromCLSID(Locators.CLSID_ATSCLocator, true));
						TunerLib.IATSCLocator myLocator = (TunerLib.IATSCLocator) DefaultLocator;

						myLocator.CarrierFrequency	 = -1;
						myLocator.InnerFEC					= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.InnerFECRate			= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.Modulation				= (TunerLib.ModulationType) ModulationType.BDA_MOD_NOT_SET;
						myLocator.OuterFEC					= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.OuterFECRate			= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.PhysicalChannel		= -1;
						myLocator.SymbolRate				= -1;
						myLocator.TSID							= -1;

						myTuningSpace.DefaultLocator = DefaultLocator;
						TuningSpaceContainer.Add((TunerLib.TuningSpace)myTuningSpace);
						myTuner.TuningSpace=(TunerLib.TuningSpace)TuningSpace;

					} break;
					case NetworkType.DVBC: 
					{
						TuningSpace = (TunerLib.ITuningSpace) Activator.CreateInstance(Type.GetTypeFromCLSID(TuningSpaces.CLSID_DVBTuningSpace, true));
						TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) TuningSpace;
						myTuningSpace.SystemType = TunerLib.DVBSystemType.DVB_Cable;
						myTuningSpace.set__NetworkType(ref NetworkProviders.CLSID_DVBCNetworkProvider);

						myTuningSpace.FriendlyName=uniqueName;
						myTuningSpace.UniqueName=uniqueName;
						TunerLib.Locator DefaultLocator = (TunerLib.Locator) Activator.CreateInstance(Type.GetTypeFromCLSID(Locators.CLSID_DVBCLocator, true));
						TunerLib.IDVBCLocator myLocator = (TunerLib.IDVBCLocator) DefaultLocator;

						myLocator.CarrierFrequency	= -1;
						myLocator.InnerFEC					= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.InnerFECRate			= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.Modulation				= (TunerLib.ModulationType) ModulationType.BDA_MOD_NOT_SET;
						myLocator.OuterFEC					= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.OuterFECRate			= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.SymbolRate				= -1;

						myTuningSpace.DefaultLocator = DefaultLocator;
						TuningSpaceContainer.Add((TunerLib.TuningSpace)myTuningSpace);
						myTuner.TuningSpace=(TunerLib.TuningSpace)TuningSpace;
					} break;
					case NetworkType.DVBS: 
					{
						TuningSpace = (TunerLib.ITuningSpace) Activator.CreateInstance(Type.GetTypeFromCLSID(TuningSpaces.CLSID_DVBSTuningSpace, true));
						TunerLib.IDVBSTuningSpace myTuningSpace = (TunerLib.IDVBSTuningSpace) TuningSpace;
						myTuningSpace.SystemType = TunerLib.DVBSystemType.DVB_Satellite;
						myTuningSpace.set__NetworkType(ref NetworkProviders.CLSID_DVBSNetworkProvider);
						myTuningSpace.LNBSwitch = -1;
						myTuningSpace.HighOscillator = -1;
						myTuningSpace.LowOscillator = 11250000;
						myTuningSpace.FriendlyName=uniqueName;
						myTuningSpace.UniqueName=uniqueName;
						
						TunerLib.Locator DefaultLocator = (TunerLib.Locator) Activator.CreateInstance(Type.GetTypeFromCLSID(Locators.CLSID_DVBSLocator, true));
						TunerLib.IDVBSLocator myLocator = (TunerLib.IDVBSLocator) DefaultLocator;
						
						myLocator.CarrierFrequency		= -1;
						myLocator.InnerFEC						= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.InnerFECRate				= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.OuterFEC						= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.OuterFECRate				= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.Modulation					= (TunerLib.ModulationType) ModulationType.BDA_MOD_NOT_SET;
						myLocator.SymbolRate					= -1;
						myLocator.Azimuth							= -1;
						myLocator.Elevation						= -1;
						myLocator.OrbitalPosition			= -1;
						myLocator.SignalPolarisation	= (TunerLib.Polarisation) Polarisation.BDA_POLARISATION_NOT_SET;
						myLocator.WestPosition				= false;

						myTuningSpace.DefaultLocator = DefaultLocator;
						TuningSpaceContainer.Add((TunerLib.TuningSpace)myTuningSpace);
						myTuner.TuningSpace=(TunerLib.TuningSpace)TuningSpace;
					} break;
					case NetworkType.DVBT: 
					{
						TuningSpace = (TunerLib.ITuningSpace) Activator.CreateInstance(Type.GetTypeFromCLSID(TuningSpaces.CLSID_DVBTuningSpace, true));
						TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) TuningSpace;
						myTuningSpace.SystemType = TunerLib.DVBSystemType.DVB_Terrestrial;
						myTuningSpace.set__NetworkType(ref NetworkProviders.CLSID_DVBTNetworkProvider);
						myTuningSpace.FriendlyName=uniqueName;
						myTuningSpace.UniqueName=uniqueName;

						TunerLib.Locator DefaultLocator = (TunerLib.Locator) Activator.CreateInstance(Type.GetTypeFromCLSID(Locators.CLSID_DVBTLocator, true));
						TunerLib.IDVBTLocator myLocator = (TunerLib.IDVBTLocator) DefaultLocator;

						myLocator.CarrierFrequency		= -1;
						myLocator.Bandwidth						= -1;
						myLocator.Guard								= (TunerLib.GuardInterval) GuardInterval.BDA_GUARD_NOT_SET;
						myLocator.HAlpha							= (TunerLib.HierarchyAlpha) HierarchyAlpha.BDA_HALPHA_NOT_SET;
						myLocator.InnerFEC						= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.InnerFECRate				= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.LPInnerFEC					= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.LPInnerFECRate			= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.Mode								= (TunerLib.TransmissionMode) TransmissionMode.BDA_XMIT_MODE_NOT_SET;
						myLocator.Modulation					= (TunerLib.ModulationType) ModulationType.BDA_MOD_NOT_SET;
						myLocator.OtherFrequencyInUse	= false;
						myLocator.OuterFEC						= (TunerLib.FECMethod) FECMethod.BDA_FEC_METHOD_NOT_SET;
						myLocator.OuterFECRate				= (TunerLib.BinaryConvolutionCodeRate) BinaryConvolutionCodeRate.BDA_BCC_RATE_NOT_SET;
						myLocator.SymbolRate					= -1;

						myTuningSpace.DefaultLocator = DefaultLocator;
						TuningSpaceContainer.Add((TunerLib.TuningSpace)myTuningSpace);
						myTuner.TuningSpace=(TunerLib.TuningSpace)TuningSpace;

					} break;
				}

			}
			return true;
		}

		/// <summary>
		/// Used to find the Network Provider for addition to the graph.
		/// </summary>
		/// <param name="ClassID">The filter category to enumerate.</param>
		/// <param name="FriendlyName">An identifier based on the DevicePath, used to find the device.</param>
		/// <param name="device">The filter that has been found.</param>
		/// <returns>true of succeeded, false if failed</returns>
		private bool findNamedFilter(System.Guid ClassID, string FriendlyName, out object device) 
		{
			int hr;
			ICreateDevEnum		sysDevEnum	= null;
			UCOMIEnumMoniker	enumMoniker	= null;
			
			sysDevEnum = (ICreateDevEnum) Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum, true));
			// Enumerate the filter category
			hr = sysDevEnum.CreateClassEnumerator(ref ClassID, out enumMoniker, 0);
			if( hr != 0 )
				throw new NotSupportedException( "No devices in this category" );

			int ulFetched = 0;
			UCOMIMoniker[] deviceMoniker = new UCOMIMoniker[1];
			while(enumMoniker.Next(1, deviceMoniker, out ulFetched) == 0) // while == S_OK
			{
				object bagObj = null;
				Guid bagId = typeof( IPropertyBag ).GUID;
				deviceMoniker[0].BindToStorage(null, null, ref bagId, out bagObj);
				IPropertyBag propBag = (IPropertyBag) bagObj;
				object val = "";
				propBag.Read("FriendlyName", ref val, IntPtr.Zero); 
				string Name = val as string;
				val = "";
				if(String.Compare(Name.ToLower(), FriendlyName.ToLower()) == 0) // If found
				{
					object filterObj = null;
					System.Guid filterID = typeof(IBaseFilter).GUID;
					deviceMoniker[0].BindToObject(null, null, ref filterID, out filterObj);
					device = filterObj;
					
					filterObj = null;
					if(device == null) 
					{
						continue;
					} 
					else 
					{
						return true;
					}
				}
				Marshal.ReleaseComObject(deviceMoniker[0]);
			}
			device = null;
			return false;
		}

		void TestTune()
		{
			//Scan();
			Log.Write("tune to nl 2");

			TunerLib.TuneRequest newTuneRequest = null;
			TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;
			TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) myTuner.TuningSpace;

			Log.Write("{0}", myTuningSpace.UniqueName, myTuningSpace.FriendlyName);
			newTuneRequest = myTuningSpace.CreateTuneRequest();

			TunerLib.IDVBTuneRequest myTuneRequest = (TunerLib.IDVBTuneRequest) newTuneRequest;

			TunerLib.IDVBTLocator myLocator = (TunerLib.IDVBTLocator) myTuneRequest.Locator;	

			myLocator.CarrierFrequency		= 698000;
			myTuneRequest.ONID	= -1;					//original network id
			myTuneRequest.TSID	= 1;						//transport stream id
			myTuneRequest.SID		= 12;						//service id
			myTuneRequest.Locator=(TunerLib.Locator)myLocator;
			myTuner.TuneRequest = newTuneRequest;
			Marshal.ReleaseComObject(myTuneRequest);
		}

		

		public void Scan()
		{
			Log.Write("Scanning...");
			XmlDocument doc= new XmlDocument();
			doc.Load("dvbt.xml");
			XmlNodeList frequencyList= doc.DocumentElement.SelectNodes("/dvbt/frequencies/frequency");
			foreach (XmlNode node in frequencyList)
			{
				int carrierFrequency= XmlConvert.ToInt32(node.Attributes.GetNamedItem(@"carrier").InnerText);

				TunerLib.TuneRequest newTuneRequest = null;
				TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;
				TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) myTuner.TuningSpace;
				newTuneRequest = myTuningSpace.CreateTuneRequest();
				TunerLib.IDVBTuneRequest myTuneRequest = (TunerLib.IDVBTuneRequest) newTuneRequest;
				TunerLib.IDVBTLocator myLocator = (TunerLib.IDVBTLocator) myTuneRequest.Locator;	

				myLocator.CarrierFrequency		= carrierFrequency;
				myTuneRequest.ONID	= -1;					//original network id
				myTuneRequest.TSID	= 1;						//transport stream id
				myTuneRequest.SID		= 12;						//service id
				myTuneRequest.Locator=(TunerLib.Locator)myLocator;
				myTuner.TuneRequest = newTuneRequest;
				int x=0;
				while (myTuner.SignalStrength<=0 && x<20)
				{
					System.Windows.Forms.Application.DoEvents();
					System.Threading.Thread.Sleep(50);
					System.Windows.Forms.Application.DoEvents();
					x++;
				}
				if (myTuner.SignalStrength>0)
				{
					myTuneRequest = (TunerLib.IDVBTuneRequest)myTuner.TuneRequest;
					Log.Write("found signal on carrier:{0} signal strength:{1} ONID:{2} TSID:{3} {4}", 
							carrierFrequency,myTuner.SignalStrength,myTuneRequest.ONID, myTuneRequest.TSID,x);
					GetAllPrograms(carrierFrequency);
				}
			}
			Log.Write("Scanning done...");
		}
		public void GetAllPrograms(int carrierFrequency)
		{
		}
		
		public void Process()
		{
			if(m_graphState==State.Created || m_graphState==State.None)
				return ;

			/*
			TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;
			TunerLib.IDVBTuneRequest myTuneRequest = (TunerLib.IDVBTuneRequest)myTuner.TuneRequest;
			Log.Write("ONID:{0} TSID:{1} SID:{2} signal:{3}", myTuneRequest.ONID, myTuneRequest.TSID, myTuneRequest.SID, myTuner.SignalStrength);
			for (int i=0; i < myTuneRequest.Components.Count;++i)
			{
				TunerLib.Component comp=myTuneRequest.Components[i];
				Log.Write("component:{0} lang:{1} desc:{2} status:{3} type:{4}",
					          i,comp.DescLangID,comp.Description,comp.Status.ToString(),comp.Type.Category.ToString());
			}*/

			if (GuideDataEvent.mutexProgramChanged.WaitOne(1,true))
			{
				Log.Write("got program change");
				IGuideData data= m_TIF as IGuideData;
				if (data!=null)
				{
					//Log.Write("got guide data");
					int iFetched;
					object[] varResults = new object[1];
					string[] channelParams = new string[20];
					IEnumVARIANT varEnum;
					Log.Write("GetGuideProgramIDs");
					data.GetGuideProgramIDs(out varEnum);
					if (varEnum!=null)
					{
						//Log.Write("variant enum");
						while(varEnum.Next(1,  varResults, out iFetched) == 0) 
						{
							//Log.Write("Get program properties for:{0}",varResults[0].ToString());
							IEnumGuideDataProperties enumProgramProperties;
							data.GetProgramProperties(varResults[0],out enumProgramProperties);
							if (enumProgramProperties!=null)
							{
								IGuideDataProperty[] properties = new IGuideDataProperty[1];
								while (enumProgramProperties.Next(1,properties, out iFetched) ==0)
								{
									//Log.Write("got property");
									object chanValue;
									int chanLanguage;
									string chanName;
									properties[0].Name( out chanName);
									properties[0].Value(out chanValue);
									properties[0].Language(out chanLanguage);
									Log.Write("channel name:{0} value:{1} language:{2}",chanName, chanValue, chanLanguage);
								}
							}
						}
					}
				}
			}

			if (GuideDataEvent.mutexServiceChanged.WaitOne(1,true))
			{
				//Log.Write("got guide data");
				IGuideData data= m_TIF as IGuideData;
				int iFetched;
				IEnumTuneRequests varRequests;
				Log.Write("GetServices");
				data.GetServices(out varRequests);
				if (varRequests!=null)
				{
					//Log.Write("variant enum");
					TunerLib.ITuneRequest[] tunerequests = new TunerLib.ITuneRequest[1];
					while(varRequests.Next(1,  tunerequests, out iFetched) == 0) 
					{
						IEnumGuideDataProperties enumProgramProperties;
						data.GetServiceProperties(tunerequests[0],out enumProgramProperties);
						if (enumProgramProperties!=null)
						{
							IGuideDataProperty[] properties = new IGuideDataProperty[1];
							while (enumProgramProperties.Next(1,properties, out iFetched) ==0)
							{
								//Log.Write("got property");
								object chanValue;
								int chanLanguage;
								string chanName;
								properties[0].Name( out chanName);
								properties[0].Value(out chanValue);
								properties[0].Language(out chanLanguage);
								Log.Write("service name:{0} value:{1} language:{2}",chanName, chanValue, chanLanguage);
							}
						}
					}
				}
			}
		}
		public void TuneFrequency(int frequency)
		{
		}

		
		public PropertyPageCollection PropertyPages()
		{
			return null;
		}
		
		public IBaseFilter AudiodeviceFilter()
		{
			return null;
		}

		public bool SupportsFrameSize(Size framesize)
		{	
			return false;
		}
		public NetworkType Network()
		{
				return m_NetworkType;
		}
		
		
		public void Tune(object tuningObject)
		{
			if (m_NetworkProvider==null) return;
			if (!graphRunning)
			{
				StartViewing(AnalogVideoStandard.None,1,1);
			}

			TunerLib.TuneRequest newTuneRequest = null;
			TunerLib.ITuner myTuner = m_NetworkProvider as TunerLib.ITuner;
			TunerLib.IDVBTuningSpace2 myTuningSpace = (TunerLib.IDVBTuningSpace2) myTuner.TuningSpace;

			newTuneRequest = myTuningSpace.CreateTuneRequest();

			TunerLib.IDVBTuneRequest myTuneRequest = (TunerLib.IDVBTuneRequest) newTuneRequest;

			TunerLib.IDVBTLocator myLocator = (TunerLib.IDVBTLocator) myTuneRequest.Locator;	

			myLocator.CarrierFrequency		= (int)tuningObject;
			myTuneRequest.ONID	= -1;					//original network id
			myTuneRequest.TSID	= -1;					//transport stream id
			myTuneRequest.SID		= -1;					//service id
			myTuneRequest.Locator=(TunerLib.Locator)myLocator;
			myTuner.TuneRequest = newTuneRequest;
			Marshal.ReleaseComObject(myTuneRequest);
		}
	}
}
//end of file
#endif