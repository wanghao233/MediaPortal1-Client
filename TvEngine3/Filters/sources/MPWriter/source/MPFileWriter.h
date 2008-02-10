/* 
 *	Copyright (C) 2006-2008 Team MediaPortal
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
#include <map>
#include "filewriter.h"
#include "ProgramToTransportStream.h"
#include "ProgramToTransportStreamRecorder.h"
#include "TeletextGrabber.h"
#include "ChannelScan.h"
using namespace std;

enum RecordingMode
{
    ProgramStream=0,
    TransportStream=1
};

class CMPFileWriterMPEG2InputPin;
class CMPFileWriterTeletextInputPin;
class CMPFileWriter;
class CMPFileWriterFilter;

DEFINE_GUID(CLSID_MPFileWriter, 0xdb35f5ed, 0x26b2, 0x4a2a,  0x92, 0xd3, 0x85, 0x2e, 0x14, 0x5b, 0xf3, 0x2d );

DEFINE_GUID(IID_IMPFileRecord,0xd5ff805e, 0xa98b, 0x4d56,  0xbe, 0xde, 0x3f, 0x1b, 0x8e, 0xf7, 0x25, 0x33);

// interface
//	//STDMETHOD(SetRecordingMode)(THIS_ int mode)PURE;

DECLARE_INTERFACE_(IMPFileRecord, IUnknown)
{
	STDMETHOD(SetRecordingMode)(THIS_ int mode)PURE;
	STDMETHOD(SetRecordingFileName)(THIS_ char* pszFileName)PURE;
	STDMETHOD(StartRecord)(THIS_ )PURE;
	STDMETHOD(StopRecord)(THIS_ )PURE;
	STDMETHOD(IsReceiving)(THIS_ BOOL* yesNo)PURE;
	STDMETHOD(Reset)(THIS_)PURE;
	STDMETHOD(SetTimeShiftFileName)(THIS_ char* pszFileName)PURE;
	STDMETHOD(StartTimeShifting)(THIS_ )PURE;
	STDMETHOD(StopTimeShifting)(THIS_ )PURE;
	STDMETHOD(PauseTimeShifting)(THIS_ int onOff)PURE;
	STDMETHOD(SetTimeShiftParams)(THIS_ int minFiles, int maxFiles, ULONG maxFileSize)PURE;
	STDMETHOD(TTxSetCallBack)(THIS_ IAnalogTeletextCallBack* callback)PURE;
};
// Main filter object

class CMPFileWriterFilter : public CBaseFilter
{
	CMPFileWriter * const m_pMPFileWriter;

public:

	// Constructor
	CMPFileWriterFilter(CMPFileWriter *m_pMPFileWriter,
		LPUNKNOWN pUnk,
		CCritSec *pLock,
		HRESULT *phr);

	// Pin enumeration
	CBasePin * GetPin(int n);
	int GetPinCount();

	// Open and close the file as necessary
	STDMETHODIMP Run(REFERENCE_TIME tStart);
	STDMETHODIMP Pause();
	STDMETHODIMP Stop();
};


//  Pin object

class CMPFileWriterMPEG2InputPin : public CRenderedInputPin
{
	CMPFileWriter*  const m_pMPFileWriter;	// Main renderer object
	CCritSec* const	m_pReceiveLock;		    // Sample critical section
public:

	CMPFileWriterMPEG2InputPin(CMPFileWriter *m_pMPFileWriter,
		LPUNKNOWN pUnk,
		CBaseFilter *pFilter,
		CCritSec *pLock,
		CCritSec *pReceiveLock,
		HRESULT *phr);

	// Do something with this media sample
	STDMETHODIMP Receive(IMediaSample *pSample);
	STDMETHODIMP EndOfStream(void);
	STDMETHODIMP ReceiveCanBlock();

	// Write detailed information about this sample to a file
	//    HRESULT WriteStringInfo(IMediaSample *pSample);

	// Check if the pin can support this specific proposed type and format
	HRESULT		CheckMediaType(const CMediaType *);
	// Break connection
	HRESULT		BreakConnect();
	BOOL			IsReceiving();
	void			Reset();
	// Track NewSegment
	STDMETHODIMP NewSegment(REFERENCE_TIME tStart,REFERENCE_TIME tStop,double dRate);
private:
	BOOL				m_bIsReceiving;
	DWORD				m_lTickCount;
	CCritSec		m_section;
};


//  CMPFileWriter object which has filter and pin members

class CMPFileWriter : public CUnknown, public IMPFileRecord
{
	friend class CMPFileWriterFilter;
	friend class CMPFileWriterMPEG2InputPin;
	friend class CMPFileWriterTeletextInputPin;
	CMPFileWriterFilter*	m_pFilter;						// Methods for filter interfaces
	CMPFileWriterMPEG2InputPin*	m_pMPEG2InputPin;			// A MPEG2 rendered input pin
	CMPFileWriterTeletextInputPin*	m_pTeletextInputPin;	// A Teletext rendered input pin
	CCritSec 		m_Lock;									// Main renderer critical section
	CCritSec 		m_ReceiveLock;							// Sublock for received samples
	FileWriter* m_pRecordFile;
public:
	DECLARE_IUNKNOWN

	CMPFileWriter(LPUNKNOWN pUnk, HRESULT *phr);
	~CMPFileWriter();

	static CUnknown * WINAPI CreateInstance(LPUNKNOWN punk, HRESULT *phr);

	HRESULT		 Write(PBYTE pbData, LONG lDataLength);
	HRESULT		 WriteTeletext(PBYTE pbData, LONG lDataLength);
	STDMETHODIMP SetRecordingMode(int mode);
	STDMETHODIMP SetRecordingFileName(char* pszFileName);
	STDMETHODIMP StartRecord();
	STDMETHODIMP StopRecord();

	STDMETHODIMP IsReceiving( BOOL* yesNo);
	STDMETHODIMP Reset();

	STDMETHODIMP SetTimeShiftFileName(char* pszFileName);
	STDMETHODIMP StartTimeShifting();
	STDMETHODIMP StopTimeShifting();
	STDMETHODIMP PauseTimeShifting(int onOff);
	STDMETHODIMP SetTimeShiftParams( int minFiles, int maxFiles, ULONG maxFileSize);
	STDMETHODIMP TTxSetCallBack(IAnalogTeletextCallBack* callback);
private:

	// Overriden to say what interfaces we support where
	STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);

	char	m_strRecordingFileName[1024];
	char	m_strTimeShiftFileName[1024];
	CProgramToTransportStream m_tsWriter;
	bool m_bIsTimeShifting;
	bool m_bPaused;
	CProgramToTransportStreamRecorder m_tsRecorder;
	bool m_bIsRecording;
	RecordingMode m_recordingMode;
	CTeletextGrabber*	m_pTeletextGrabber;
	CChannelScan* m_pChannelScan;
};





class CMPFileWriterTeletextInputPin : public CRenderedInputPin
{
	CMPFileWriter*  const m_pMPFileWriter;	// Main renderer object
	CCritSec* const	m_pReceiveLock;		    // Sample critical section
public:

	CMPFileWriterTeletextInputPin(CMPFileWriter *m_pMPFileWriter,
		LPUNKNOWN pUnk,
		CBaseFilter *pFilter,
		CCritSec *pLock,
		CCritSec *pReceiveLock,
		HRESULT *phr);

	// Do something with this media sample
	STDMETHODIMP Receive(IMediaSample *pSample);
	STDMETHODIMP EndOfStream(void);
	STDMETHODIMP ReceiveCanBlock();

	// Write detailed information about this sample to a file
	//    HRESULT WriteStringInfo(IMediaSample *pSample);

	// Check if the pin can support this specific proposed type and format
	HRESULT		CheckMediaType(const CMediaType *);
	// Break connection
	HRESULT		BreakConnect();
	BOOL			IsReceiving();
	void			Reset();
	// Track NewSegment
	STDMETHODIMP NewSegment(REFERENCE_TIME tStart,REFERENCE_TIME tStop,double dRate);
private:
	BOOL				m_bIsReceiving;
	DWORD				m_lTickCount;
	CCritSec		m_section;
};


