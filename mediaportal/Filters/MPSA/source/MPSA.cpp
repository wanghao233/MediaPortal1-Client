/* 
 *	Copyright (C) 2005 Media Portal
 *  Author: Agree
 *	http://mediaportal.sourceforge.net
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

#include <windows.h>
#include <commdlg.h>
#include <streams.h>
#include <initguid.h>
#include "Section.h"
#include "MPSA.h"
#include "SplitterSetup.h"
#include "proppage.h"
#include "mhwinputpin1.h"
#include "mhwinputpin2.h"
#include "epginputpin.h"
#include "atscparser.h"
// Setup data

extern void Log(const char *fmt, ...) ;
const AMOVIESETUP_MEDIATYPE sudPinTypes[2] =
{
	{&MEDIATYPE_MPEG2_SECTIONS, &MEDIASUBTYPE_DVB_SI},         // Minor type
	{&MEDIATYPE_MPEG2_SECTIONS, &MEDIASUBTYPE_ATSC_SI}         // Minor type
};
const AMOVIESETUP_MEDIATYPE sudPinTypesMHW =
{
	&MEDIATYPE_MPEG2_SECTIONS, &MEDIASUBTYPE_DVB_SI,         // Minor type
};

const AMOVIESETUP_MEDIATYPE sudPinTypesEPG =
{
	&MEDIATYPE_MPEG2_SECTIONS, &MEDIASUBTYPE_DVB_SI 
};

const AMOVIESETUP_PIN sudPins[4] =
{
	{
		L"Input",                   // Pin string name
		FALSE,                      // Is it rendered
		FALSE,                      // Is it an output
		FALSE,                      // Allowed none
		FALSE,                      // Likewise many
		&CLSID_NULL,                // Connects to filter
		L"Output",                  // Connects to pin
		2,                          // Number of types
		&sudPinTypes[0]             // Pin information
	},
		
	{
		L"MHWd2",                   // Pin string name
		FALSE,                      // Is it rendered
		FALSE,                      // Is it an output
		FALSE,                      // Allowed none
		FALSE,                      // Likewise many
		&CLSID_NULL,                // Connects to filter
		L"Output",                  // Connects to pin
		1,                          // Number of types
		&sudPinTypesMHW             // Pin information
	},
		
	{
		L"MHWd3",                   // Pin string name
		FALSE,                      // Is it rendered
		FALSE,                      // Is it an output
		FALSE,                      // Allowed none
		FALSE,                      // Likewise many
		&CLSID_NULL,                // Connects to filter
		L"Output",                  // Connects to pin
		1,                          // Number of types
		&sudPinTypesMHW             // Pin information
	},
			
	{
		L"EPG",                   // Pin string name
		FALSE,                      // Is it rendered
		FALSE,                      // Is it an output
		FALSE,                      // Allowed none
		FALSE,                      // Likewise many
		&CLSID_NULL,                // Connects to filter
		L"EPG",                  // Connects to pin
		1,                          // Number of types
		&sudPinTypesEPG             // Pin information
	},
};

const AMOVIESETUP_FILTER sudDump =
{
    &CLSID_MPDSA,          // Filter CLSID
    L"MediaPortal Stream Analyzer",   // String name
    MERIT_DO_NOT_USE,           // Filter merit
    4,                          // Number pins
    sudPins                    // Pin details
};


//
//  Object creation stuff
//
CFactoryTemplate g_Templates[]= {
	{L"MediaPortal Stream Analyzer", &CLSID_MPDSA, CStreamAnalyzer::CreateInstance, NULL, &sudDump},
	{ L"MP StreamAnalyzer PropPage",&CLSID_StreamAnalyzerPropPage, MPDSTProperties::CreateInstance }};
int g_cTemplates = 2;


// Constructor

CStreamAnalyzerFilter::CStreamAnalyzerFilter(CStreamAnalyzer *pDump,
                         LPUNKNOWN pUnk,
                         CCritSec *pLock,
                         HRESULT *phr) :
    CBaseFilter(NAME("Stream Analyzer MP"), pUnk, pLock, CLSID_MPDSA),
    m_pDump(pDump)
{
}

STDMETHODIMP CStreamAnalyzer::GetPages(CAUUID *pPages) 
{
    CheckPointer(pPages,E_POINTER);

    pPages->cElems = 1;
    pPages->pElems = (GUID *) CoTaskMemAlloc(sizeof(GUID));

    if (pPages->pElems == NULL)
        return E_OUTOFMEMORY;

    *(pPages->pElems) = CLSID_StreamAnalyzerPropPage;

    return NOERROR;

} // GetPages

//
// GetPin
//
CBasePin * CStreamAnalyzerFilter::GetPin(int n)
{
    if (n ==0) 
	    return m_pDump->m_pPin;
    else if (n ==1) 
		return m_pDump->m_pMHWPin1;
    else if (n ==2) 
		return m_pDump->m_pMHWPin2;
	else if (n ==3)
		return m_pDump->m_pEPGPin;
	else {
        return NULL;
    }
}


//
// GetPinCount
//
int CStreamAnalyzerFilter::GetPinCount()
{
    return 4;
}


//
// Stop
//
// Overriden to close the dump file
//
STDMETHODIMP CStreamAnalyzerFilter::Stop()
{
    CAutoLock cObjectLock(m_pLock);
	
    return CBaseFilter::Stop();
}

//
// Pause
//
// Overriden to open the dump file
//
STDMETHODIMP CStreamAnalyzerFilter::Pause()
{
    CAutoLock cObjectLock(m_pLock);


    return CBaseFilter::Pause();
}


//
// Run
//
// Overriden to open the dump file
//
STDMETHODIMP CStreamAnalyzerFilter::Run(REFERENCE_TIME tStart)
{
    CAutoLock cObjectLock(m_pLock);

	m_pDump->ResetParser();
	return CBaseFilter::Run(tStart);
}


//
//  Definition of CStreamAnalyzerSectionsPin
//
CStreamAnalyzerSectionsPin::CStreamAnalyzerSectionsPin(CStreamAnalyzer *pDump,
                             LPUNKNOWN pUnk,
                             CBaseFilter *pFilter,
                             CCritSec *pLock,
                             CCritSec *pReceiveLock,
                             HRESULT *phr) :

    CRenderedInputPin(NAME("CStreamAnalyzerSectionsPin"),
                  pFilter,                   // Filter
                  pLock,                     // Locking
                  phr,                       // Return code
                  L"Input"),                 // Pin name
    m_pReceiveLock(pReceiveLock),
    m_pDump(pDump),
    m_tLast(0)
{
}


//
// CheckMediaType
//
// Check if the pin can support this specific proposed type and format
//
HRESULT CStreamAnalyzerSectionsPin::CheckMediaType(const CMediaType *pmt)
{
	if(pmt->majortype==MEDIATYPE_MPEG2_SECTIONS )
		return S_OK;
	return S_FALSE;
}

//
// BreakConnect
//
// Break a connection
//
HRESULT CStreamAnalyzerSectionsPin::BreakConnect()
{
//	Log("Section:BreakConnect");
    return CRenderedInputPin::BreakConnect();
}

HRESULT CStreamAnalyzerSectionsPin::CompleteConnect(IPin *pPin)
{
	HRESULT hr=CBasePin::CompleteConnect(pPin);
	m_pDump->OnConnectSections();
	return hr;
}

//
// ReceiveCanBlock
//
// We don't hold up source threads on Receive
//
STDMETHODIMP CStreamAnalyzerSectionsPin::ReceiveCanBlock()
{
    return S_FALSE;
}


//
// Receive
//
// Do something with this media sample
//
STDMETHODIMP CStreamAnalyzerSectionsPin::Receive(IMediaSample *pSample)
{
	
    CheckPointer(pSample,E_POINTER);

    //CAutoLock lock(m_pReceiveLock);
    PBYTE pbData=NULL;

    // Has the filter been stopped yet?

    REFERENCE_TIME tStart, tStop;
    pSample->GetTime(&tStart, &tStop);

    m_tLast = tStart;
	long lDataLen=0;

    HRESULT hr = pSample->GetPointer(&pbData);
    if (FAILED(hr)) {
        return hr;
    }
	
	lDataLen=pSample->GetActualDataLength();
	// decode
	if(lDataLen>5)
		m_pDump->Process(pbData,lDataLen);

    return S_OK;
}
void CStreamAnalyzerSectionsPin::ResetPids()
{
	m_pDump->ResetPids();
}

//
// EndOfStream
//
STDMETHODIMP CStreamAnalyzerSectionsPin::EndOfStream(void)
{
    CAutoLock lock(m_pReceiveLock);
//	Log("Sections:end of stream");
    return CRenderedInputPin::EndOfStream();

} // EndOfStream


//
// NewSegment
//
// Called when we are seeked
//
STDMETHODIMP CStreamAnalyzerSectionsPin::NewSegment(REFERENCE_TIME tStart,
                                       REFERENCE_TIME tStop,
                                       double dRate)
{
    m_tLast = 0;
    return S_OK;

} // NewSegment


//
//  CStreamAnalyzer class
//
CStreamAnalyzer::CStreamAnalyzer(LPUNKNOWN pUnk, HRESULT *phr) :
    CUnknown(NAME("CStreamAnalyzer"), pUnk),
    m_pFilter(NULL),
    m_pPin(NULL),m_pSections(NULL),m_pDemuxer(NULL),
	m_patChannelsCount(0),m_pmtGrabProgNum(0),
	m_currentPMTLen(0)
{
	Log("Initialize MPSA");
	m_bDecodeATSC=false;
	m_bReset=true;
    ASSERT(phr);
    memset(m_pmtGrabData,0,4096);
	m_pSections=new Sections();
	if(m_pSections==NULL)
	{
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }

	m_pDemuxer=new SplitterSetup(m_pSections);

	if(m_pDemuxer==NULL)
	{
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }

    m_pFilter = new CStreamAnalyzerFilter(this, GetOwner(), &m_Lock, phr);
    if (m_pFilter == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }

    m_pPin = new CStreamAnalyzerSectionsPin(this,GetOwner(),
                               m_pFilter,
                               &m_Lock,
                               &m_ReceiveLock,
                               phr);
    if (m_pPin == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }

	m_pMHWPin1 = new CMHWInputPin1(this,GetOwner(),
                               m_pFilter,
                               &m_Lock,
                               &m_ReceiveLock,
                               phr);
    if (m_pMHWPin1 == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }

	m_pMHWPin2 = new CMHWInputPin2(this,GetOwner(),
                               m_pFilter,
                               &m_Lock,
                               &m_ReceiveLock,
                               phr);
    if (m_pMHWPin2 == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }
	m_pEPGPin = new CEPGInputPin(this,GetOwner(),
                               m_pFilter,
                               &m_Lock,
                               &m_ReceiveLock,
                               phr);
    if (m_pEPGPin == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
        return;
    }
	m_atscParser.SetDemuxer(m_pDemuxer);
}



// Destructor

CStreamAnalyzer::~CStreamAnalyzer()
{
//	Log("~CStreamAnalyzer()");
	delete m_pDemuxer;
	delete m_pSections;
	delete m_pPin;
	delete m_pMHWPin1;
	delete m_pMHWPin2;
	delete m_pEPGPin;
	delete m_pFilter;

}


//
// CreateInstance
//
// Provide the way for COM to create a dump filter
//
CUnknown * WINAPI CStreamAnalyzer::CreateInstance(LPUNKNOWN punk, HRESULT *phr)
{
    ASSERT(phr);
    
    CStreamAnalyzer *pNewObject = new CStreamAnalyzer(punk, phr);
    if (pNewObject == NULL) {
        if (phr)
            *phr = E_OUTOFMEMORY;
    }
    return pNewObject;

} // CreateInstance

STDMETHODIMP CStreamAnalyzer::ResetParser()
{
	
	Log("ResetParser");
	HRESULT hr=m_pDemuxer->UnMapAllPIDs();
	m_pPin->ResetPids();
	m_pMHWPin1->ResetPids();
	m_pMHWPin2->ResetPids();
	m_pEPGPin->ResetPids();
	return hr;
}
//
// NonDelegatingQueryInterface
//
// Override this to say what interfaces we support where
//
STDMETHODIMP CStreamAnalyzer::NonDelegatingQueryInterface(REFIID riid, void ** ppv)
{
    CheckPointer(ppv,E_POINTER);
    CAutoLock lock(&m_Lock);
    // Do we have this interface
	if (riid == IID_IStreamAnalyzer)
	{
		return GetInterface((IStreamAnalyzer*)this, ppv);
	}
	else if (riid == IID_IEPGGrabber)
	{
		return GetInterface((IEPGGrabber*)this, ppv);
	}
	else if (riid == IID_IMHWGrabber)
	{
		return GetInterface((IMHWGrabber*)this, ppv);
	}
	else if (riid == IID_ATSCGrabber)
	{
		return GetInterface((IATSCGrabber*)this, ppv);
	}
    else if (riid == IID_IBaseFilter || riid == IID_IMediaFilter || riid == IID_IPersist)
	{
        return m_pFilter->NonDelegatingQueryInterface(riid, ppv);
    } 
	else if (riid == IID_ISpecifyPropertyPages) 
	{
        return GetInterface((ISpecifyPropertyPages *) this, ppv);
	}
    return CUnknown::NonDelegatingQueryInterface(riid, ppv);

} // NonDelegatingQueryInterface

HRESULT CStreamAnalyzer::OnConnectSections()
{
//	Log("OnConnectSections");
	m_pDemuxer->SetDemuxPins(m_pFilter->GetFilterGraph());
	IPin *pin;
	m_pPin->ConnectedTo(&pin);
	if(pin!=NULL)
	{
		m_pDemuxer->SetSectionsPin(pin);
	}	
//	Log("OnConnectSections done");
	return S_OK;
}

HRESULT CStreamAnalyzer::OnConnectEPG()
{
//	Log("OnConnectEPG");
	m_pDemuxer->SetDemuxPins(m_pFilter->GetFilterGraph());
	IPin *pin;
	m_pEPGPin->ConnectedTo(&pin);
	if(pin!=NULL)
	{
		m_pDemuxer->SetEPGPin(pin);
	}
//	Log("OnConnectEPG done");
	return S_OK;
}
HRESULT CStreamAnalyzer::OnConnectMHW1()
{
//	Log("OnConnectMHW1");
	m_pDemuxer->SetDemuxPins(m_pFilter->GetFilterGraph());
	IPin *pin;
	m_pMHWPin1->ConnectedTo(&pin);
	if(pin!=NULL)
	{
		m_pDemuxer->SetMHW1Pin(pin);
	}
//	Log("OnConnectMHW1 done");
	return S_OK;
}
HRESULT CStreamAnalyzer::OnConnectMHW2()
{
//	Log("OnConnectMHW2");
	m_pDemuxer->SetDemuxPins(m_pFilter->GetFilterGraph());
	IPin *pin;
	m_pMHWPin2->ConnectedTo(&pin);
	if(pin!=NULL)
	{
		m_pDemuxer->SetMHW2Pin(pin);
	}
//	Log("OnConnectMHW2 done");
	return S_OK;
}
	
STDMETHODIMP CStreamAnalyzer::ResetPids()
{
	m_bReset=true;
    return NOERROR;
}
HRESULT CStreamAnalyzer::ProcessEPG(BYTE *pbData,long len)
{
	if (pbData==NULL) return S_OK;
	if (len <=3) return S_OK;
	try
	{
		if (m_bDecodeATSC) return S_OK;
		if (m_pSections->IsEPGGrabbing())
		{
			if(pbData[0]==0x00 && pbData[1]==0x00 && pbData[2]==0x01)
			{
				//PES PACKET
				return S_OK;
			}
			if (pbData[0]>=0x50 && pbData[0] <= 0x6f) //EPG
			{
				//Log("decode EPG");
				m_pSections->DecodeEPG(pbData,len);
				//Log("decode EPG done");
			}
		}
	}
	catch(...)
	{
		Log("ProcessEPG exception");
	}
	return S_OK;
}
HRESULT CStreamAnalyzer::Process(BYTE *pbData,long len)
{
	
	try
	{
		if (m_bReset)
		{
			Log("Reset sections");
			m_bReset=false;
			m_patChannelsCount=0;
			m_pSections->Reset();
			m_atscParser.Reset();
		}		
		//CAutoLock lock(&m_Lock);
/*
		if(pbData[0]==0x00 && pbData[1]==0x00 && pbData[2]==0x01)
		{
			//pes packet
			AudioHeader audio;
			BYTE *d=new BYTE[len];
			m_pSections->GetPES(pbData,len,d);
			if(m_pSections->ParseAudioHeader(d,&audio)==S_OK)
			{
				// we can check audio
				int a=0;
			}
			delete [] d;
			return S_OK;
		}
*/
		if (m_bDecodeATSC)
		{
			if (pbData[0]==0xc7)
			{
				m_atscParser.ATSCDecodeMasterGuideTable(pbData,len,&m_patChannelsCount);
			}
			if (pbData[0]==0xc8 || pbData[0]==0xc9)
			{
				if (m_patChannelsCount==0)
				{
					//decode ATSC: Virtual Channel Table (pid 0xc8 / 0xc9)
					m_atscParser.ATSCDecodeChannelTable(pbData,m_patTable, &m_patChannelsCount,len);
				}
			}

			//decode ATSC: EPG
			if (m_patChannelsCount>0)
				m_atscParser.ATSCDecodeEPG(pbData,len);
			return S_OK;
		}
		
			
		if(pbData[0]==0x02)// pmt
		{
			ULONG prgNumber=(pbData[3]<<8)+pbData[4];
			for(int n=0;n<m_patChannelsCount;n++)
			{
				if(m_patTable[n].ProgrammNumber==prgNumber && m_patTable[n].PMTReady==false)
				{
					//Log("decode PMT");
					m_pSections->decodePMT(pbData,&m_patTable[n],len);
					//Log("PMT decoded");
					//if(m_patTable[n].Pids.AudioPid1>0)
					//	m_pDemuxer->MapAdditionalPayloadPID(m_patTable[n].Pids.AudioPid1);
					//if(m_patTable[n].Pids.AudioPid2>0)
					//	m_pDemuxer->MapAdditionalPayloadPID(m_patTable[n].Pids.AudioPid2);
					//if(m_patTable[n].Pids.AudioPid3>0)
					//	m_pDemuxer->MapAdditionalPayloadPID(m_patTable[n].Pids.AudioPid3);
				}
			}
			if(m_pmtGrabProgNum==prgNumber && len<=4096)
			{
				memset(m_pmtGrabData,0,4096);
				memcpy(m_pmtGrabData,pbData,len);// save the pmt in the buffer
				m_currentPMTLen=len;
			}		
		}
		if(pbData[0]==0x00)// pat
		{
			// we need to check if we received a new PAT
			// reason, after submitting a tune request (zap to another channel) we might still
			// receive the old PAT for a couple of msec until the tuner has
			// finished tuning to the new channel
			if (m_pSections->IsNewPat(pbData,len))
			{
				if (m_patChannelsCount>0) 
				{
					Log("Found new PAT, decode channels");
					ResetParser();
				}
				//m_pDemuxer->UnMapSectionPIDs();
				//Log("decode pat");
				m_pSections->decodePAT(pbData,m_patTable,&m_patChannelsCount,len);
				//Log("PAT decoded and found %d channels, map pids", m_patChannelsCount);
				for(int n=0;n<m_patChannelsCount;n++)
				{
					m_pDemuxer->MapAdditionalPID(m_patTable[n].ProgrammPMTPID);
				}
				//Log("PAT decoded and pids mapped");
			}
		}
		if(pbData[0]==0x42)// sdt
		{
			//Log("decode SDT");
			if (m_patChannelsCount>0)
			{
				m_pSections->decodeSDT(pbData,m_patTable,m_patChannelsCount,len);
			}
			//Log("SDT decoded");
		}
	}
	catch(...)
	{
		Log("Section:process() exception");
	}
	return S_OK;
}

STDMETHODIMP CStreamAnalyzer::IsChannelReady(ULONG channel)
{
	if(channel<0 || channel>(ULONG)m_patChannelsCount-1)
		return S_FALSE;

	if((bool)m_patTable[channel].SDTReady==true && (bool)m_patTable[channel].PMTReady==true)
		return S_OK;

	return S_FALSE;
}
STDMETHODIMP CStreamAnalyzer::get_IPin (IPin **ppPin)
{
    *ppPin = m_pPin->GetConnected ();
	if(*ppPin!=NULL)
		(*ppPin)->AddRef();
    return NOERROR ;
} // get_IPin


//
// put_MediaType
//
// INull method.
//
STDMETHODIMP CStreamAnalyzer::put_MediaType(CMediaType *pmt)
{
     return NOERROR;
} // put_MediaType


//
// get_MediaType
//
// INull method.
// Set *pmt to the current preferred media type.
//
STDMETHODIMP CStreamAnalyzer::get_MediaType(CMediaType **pmt)
{
    return NOERROR;
} // get_MediaType


//
// get_State
//
// INull method
// Set *state to the current state of the filter (State_Stopped etc)
//
STDMETHODIMP CStreamAnalyzer::get_State(FILTER_STATE *pState)
{
    return NOERROR;

} // get_State

STDMETHODIMP CStreamAnalyzer::GetChannelCount(WORD *count)
{
	if (m_bReset) 
		*count=0;
	else
		*count=m_patChannelsCount;
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::SetPMTProgramNumber(ULONG prgNum)
{
	m_pmtGrabProgNum=prgNum;
	return S_OK;
}

STDMETHODIMP CStreamAnalyzer::UseATSC(BOOL yesNo)
{
	m_bDecodeATSC=yesNo;
	m_pDemuxer->UseATSC(yesNo);
	if (m_bDecodeATSC)
		Log("use ATSC:yes");
	else
		Log("use ATSC:no");
	
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::IsATSCUsed(BOOL* yesNo)
{
	*yesNo=m_bDecodeATSC;
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GrabEPG()
{
//	Log("StreamAnalyzer:GrabEPG");
	m_pSections->GrabEPG();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::IsEPGReady(BOOL* yesNo)
{
	*yesNo=m_pSections->IsEPGReady();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetEPGChannelCount( ULONG* channelCount)
{
	*channelCount=m_pSections->GetEPGChannelCount( );
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetEPGEventCount( ULONG channel,  ULONG* eventCount)
{
	*eventCount=m_pSections->GetEPGEventCount( channel);
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetEPGChannel( ULONG channel,  WORD* networkId,  WORD* transportid, WORD* service_id  )
{
	m_pSections->GetEPGChannel( channel,  networkId,  transportid, service_id  );
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetEPGEvent( ULONG channel,  ULONG eventid,ULONG* language, ULONG* dateMJD, ULONG* timeUTC, ULONG* duration, char**event,  char** text, char** genre    )
{
	m_pSections->GetEPGEvent( channel,  eventid, language,dateMJD, timeUTC, duration, event,  text, genre    );
	return S_OK;
}


STDMETHODIMP CStreamAnalyzer::GrabMHW()
{
	m_pMHWPin1->GrabMHW();
	m_pMHWPin2->GrabMHW();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::IsMHWReady(BOOL* yesNo)
{
	if (m_pMHWPin1->IsReady() && m_pMHWPin2->IsReady() )
	{
		*yesNo=TRUE;
		return S_OK;
	}
	*yesNo=FALSE;
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetMHWTitleCount(WORD* count)
{
	*count=m_pMHWPin1->m_MHWParser.GetTitleCount();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetMHWTitle(WORD program, WORD* id, WORD* transportId, WORD* networkId, WORD* channelId, WORD* programId, WORD* themeId, WORD* PPV, BYTE* Summaries, WORD* duration, ULONG* dateStart, ULONG* timeStart,char** title,char** programName)
{	
	m_pMHWPin1->m_MHWParser.GetTitle(program, id, transportId, networkId, channelId, programId, themeId, PPV, Summaries, duration, dateStart,timeStart,title,programName);
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetMHWChannel(WORD channelNr, WORD* channelId,WORD* networkId, WORD* transportId, char** channelName)
{
	m_pMHWPin2->m_MHWParser.GetChannel(channelNr,channelId, networkId, transportId, channelName);
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetMHWSummary(WORD programId, char** summary)
{
	m_pMHWPin2->m_MHWParser.GetSummary(programId, summary);
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetMHWTheme(WORD themeId, char** theme)
{
	m_pMHWPin2->m_MHWParser.GetTheme(themeId, theme);
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GrabATSC()
{
	m_atscParser.Reset();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::IsATSCReady (BOOL* yesNo)
{
	*yesNo = m_atscParser.IsReady();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetATSCTitleCount(WORD* count)
{
	*count=m_atscParser.GetEPGCount();
	return S_OK;
}
STDMETHODIMP CStreamAnalyzer::GetATSCTitle(WORD no, WORD* source_id, ULONG* starttime, WORD* length_in_secs, char** title, char** description)
{
	m_atscParser.GetEPGTitle(no, source_id, starttime, length_in_secs, title, description);
	return S_OK;
}


STDMETHODIMP CStreamAnalyzer::GetPMTData(BYTE *data)
{
	BYTE *buf=m_pmtGrabData;
	if(m_currentPMTLen>0 && m_pmtGrabProgNum>0)
	{
		memcpy(data,buf,m_currentPMTLen);
		return m_currentPMTLen;
	}
	return -1;
}

STDMETHODIMP CStreamAnalyzer::GetChannel(WORD channel,BYTE *ch)
{
	
	if(channel>=0 && channel < m_patChannelsCount && m_patChannelsCount>0)
	{
		memcpy(ch,&m_patTable[channel],m_pSections->CISize());
		if(m_patTable[channel].SDTReady==false || m_patTable[channel].PMTReady==false)
		{
			if (m_patTable[channel].SDTReady==false)
				Log("GetChannel(%d) SDT not found");
			if (m_patTable[channel].PMTReady==false)
				Log("GetChannel(%d) PMT not found");
			return S_FALSE;
		}
		return S_OK;
	}
	return S_FALSE;
}
STDMETHODIMP CStreamAnalyzer::GetCISize(WORD *size)
{
	*size=m_pSections->CISize();
	return S_OK;
}
////////////////////////////////////////////////////////////////////////
//
// Exported entry points for registration and unregistration 
// (in this case they only call through to default implementations).
//
////////////////////////////////////////////////////////////////////////

//
// DllRegisterSever
//
// Handle the registration of this filter
//
STDAPI DllRegisterServer()
{
    return AMovieDllRegisterServer2( TRUE );

} // DllRegisterServer


//
// DllUnregisterServer
//
STDAPI DllUnregisterServer()
{
    return AMovieDllRegisterServer2( FALSE );

} // DllUnregisterServer


//
// DllEntryPoint
//
extern "C" BOOL WINAPI DllEntryPoint(HINSTANCE, ULONG, LPVOID);

BOOL APIENTRY DllMain(HANDLE hModule, 
                      DWORD  dwReason, 
                      LPVOID lpReserved)
{
	return DllEntryPoint((HINSTANCE)(hModule), dwReason, lpReserved);
}

