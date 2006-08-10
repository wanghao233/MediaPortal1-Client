/* 
 *	Copyright (C) 2005 Team MediaPortal
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

#include <streams.h>
#include "tsreader.h"
#include "audiopin.h"

byte MPEG1AudioFormat[] = 
{
  0x50, 0x00,				//wFormatTag
  0x02, 0x00,				//nChannels
  0x80, 0xBB,	0x00, 0x00, //nSamplesPerSec
  0x00, 0x7D,	0x00, 0x00, //nAvgBytesPerSec
  0x00, 0x03,				//nBlockAlign
  0x00, 0x00,				//wBitsPerSample
  0x16, 0x00,				//cbSize
  0x02, 0x00,				//wValidBitsPerSample
  0x00, 0xE8,				//wSamplesPerBlock
  0x03, 0x00,				//wReserved
  0x01, 0x00,	0x01,0x00,  //dwChannelMask
  0x01, 0x00,	0x1C, 0x00, 0x00, 0x00,	0x00, 0x00, 0x00, 0x00, 0x00, 0x00

};
extern void LogDebug(const char *fmt, ...) ;

CAudioPin::CAudioPin(LPUNKNOWN pUnk, CTsReaderFilter *pFilter, HRESULT *phr,CCritSec* section) :
	CSourceStream(NAME("pinAudio"), phr, pFilter, L"Audio"),
	m_pTsReaderFilter(pFilter),
	CSourceSeeking(NAME("pinAudio"),pUnk,phr,section),
	m_section(section)
{
	m_bDropPackets=false;
	m_rtStart=0;
}

CAudioPin::~CAudioPin()
{
	LogDebug("pin:dtor()");
}
STDMETHODIMP CAudioPin::NonDelegatingQueryInterface( REFIID riid, void ** ppv )
{
	if (riid == IID_IAsyncReader)
  {
		int x=1;
	}
  if (riid == IID_IMediaSeeking)
  {
      return CSourceSeeking::NonDelegatingQueryInterface( riid, ppv );
  }
  if (riid == IID_IMediaPosition)
  {
		return CSourceSeeking::NonDelegatingQueryInterface( riid, ppv );
  }
  return CSourceStream::NonDelegatingQueryInterface(riid, ppv);
}

HRESULT CAudioPin::GetMediaType(CMediaType *pmt)
{

	pmt->InitMediaType();
	pmt->SetType      (& MEDIATYPE_Audio);
	pmt->SetSubtype   (& MEDIASUBTYPE_MPEG2_AUDIO);
	pmt->SetSampleSize(1);
	pmt->SetTemporalCompression(FALSE);
	pmt->SetVariableSize();
	pmt->SetFormat(MPEG1AudioFormat,sizeof(MPEG1AudioFormat));

	return S_OK;
}

HRESULT CAudioPin::DecideBufferSize(IMemAllocator *pAlloc, ALLOCATOR_PROPERTIES *pRequest)
{
	HRESULT hr;


	CheckPointer(pAlloc, E_POINTER);
	CheckPointer(pRequest, E_POINTER);

	if (pRequest->cBuffers == 0)
	{
			pRequest->cBuffers = 1;
	}

	pRequest->cbBuffer = 0x10000;


	ALLOCATOR_PROPERTIES Actual;
	hr = pAlloc->SetProperties(pRequest, &Actual);
	if (FAILED(hr))
	{
			return hr;
	}

	if (Actual.cbBuffer < pRequest->cbBuffer)
	{
			return E_FAIL;
	}

	return S_OK;
}

HRESULT CAudioPin::CompleteConnect(IPin *pReceivePin)
{
	LogDebug("pin:CompleteConnect()");
	HRESULT hr = CBaseOutputPin::CompleteConnect(pReceivePin);
	if (SUCCEEDED(hr))
	{
		LogDebug("pin:CompleteConnect() done");
	}
	else
	{
		LogDebug("pin:CompleteConnect() failed:%x",hr);
	}
	return hr;
}

HRESULT CAudioPin::FillBuffer(IMediaSample *pSample)
{
//	::OutputDebugStringA("CAudioPin::FillBuffer()\n");
	pSample->SetActualDataLength(0);
	CDeMultiplexer& demux=m_pTsReaderFilter->GetDemultiplexer();
	CBuffer* buffer;
	byte* pSampleBuffer;
	HRESULT hr = pSample->GetPointer(&pSampleBuffer);
	if (FAILED(hr))
	{
		::OutputDebugStringA("CVideoPin::FillBuffer() invalid ptr\n");
		return hr;
	}
	CRefTime streamTime;
	m_pTsReaderFilter->StreamTime(streamTime);
	double dStreamTime=streamTime.Millisecs();
	double dStartTime= m_rtStart.Millisecs();

	while (demux.AudioPacketCount()>2)
	{
		buffer=demux.GetAudio();
		delete buffer;
		::OutputDebugString("A:drop...\n");
		m_bDiscontinuity=TRUE;
	}
	buffer=demux.GetAudio();
	if (buffer==NULL)
	{
		::OutputDebugStringA("CVideoPin::FillBuffer() no audio\n");
		return NOERROR;
	}


	if (buffer->Pts()>0)
	{
		long presentationTime=(long)(buffer->Pts() - m_pTsReaderFilter->GetStartTime());
		CRefTime referenceTime(presentationTime);
		CRefTime timeStamp=referenceTime - m_rtStart; 
		REFERENCE_TIME refTimeStamp=(REFERENCE_TIME)timeStamp;
		pSample->SetTime(&refTimeStamp,NULL);
#if DEBUG
		char buf[100];
		sprintf(buf,"  A: %05.2f %05.2f %05.2f %05.2f %d %d (%d)\n",
					(dStartTime/1000.0),
					(dStreamTime/1000.0), 
					(referenceTime.Millisecs()/1000.0),
					(timeStamp.Millisecs()/1000.0),
					demux.VideoPacketCount(), demux.AudioPacketCount(),m_bDropPackets);
		::OutputDebugString(buf);
#endif		
	}

	pSample->SetActualDataLength(buffer->Length());
	memcpy(pSampleBuffer, buffer->Data(), buffer->Length());
	pSample->SetDiscontinuity(m_bDiscontinuity);
	delete buffer;

	m_bDiscontinuity=FALSE;
	//::OutputDebugStringA("CAudioPin::FillBuffer() done\n");
  return NOERROR;
}

// CSourceSeeking
HRESULT CAudioPin::ChangeStart()
{
	double startTime=m_rtStart/UNITS;
	CAutoLock lock(m_pTsReaderFilter->pStateLock());
	char buf[100];
	sprintf(buf,"CAudioPin::ChangeStart %x %05.2f\n",(DWORD)m_rtStart,startTime);
	::OutputDebugString(buf);
	m_pTsReaderFilter->Seek(m_rtStart);
	FlushOutput();
	::OutputDebugStringA("CAudioPin::ChangeStart done()\n");
	return S_OK;
}
HRESULT CAudioPin::ChangeStop()
{
	::OutputDebugString("CAudioPin::ChangeStop()\n");
	FlushOutput();
	return S_OK;
}
HRESULT CAudioPin::ChangeRate()
{
	::OutputDebugString("CAudioPin::ChangeRate()\n");
	FlushOutput();
	return S_OK;
}

void CAudioPin::SetDuration()
{
	REFERENCE_TIME refTime;
	m_pTsReaderFilter->GetDuration(&refTime);
	m_rtDuration=refTime;
	m_rtStop=refTime;
}

HRESULT CAudioPin::OnThreadStartPlay()
{    
	::OutputDebugString("CAudioPin::OnThreadStartPlay()\n");
	m_bDiscontinuity = TRUE;
	CDeMultiplexer& demux=m_pTsReaderFilter->GetDemultiplexer();
	demux.Reset();
  return DeliverNewSegment(m_rtStart, m_rtStop, m_dRateSeeking);
}
void CAudioPin::FlushOutput()
{
	if (ThreadExists()) 
  {
		::OutputDebugString("CAudioPin::FlushOutput()\n");
    DeliverBeginFlush();
    Stop();
    DeliverEndFlush();
    Run();
  }
}

void CAudioPin::SetStart(CRefTime rtStartTime)
{
	if (m_rtStart==rtStartTime) return;
	m_rtStart=rtStartTime;
	double startTime=m_rtStart/UNITS;
	char buf[100];
	sprintf(buf,"CAudioPin::SetStart %x %05.2f\n",(DWORD)m_rtStart,startTime);
	::OutputDebugString(buf);
}