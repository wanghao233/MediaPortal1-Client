/* 
 *	Copyright (C) 2005 Team MediaPortal
 *  Author: tourettes
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

#pragma warning( disable: 4995 4996 )

#include "DVBSub.h"
#include "SubtitleInputPin.h"

const int subtitleSizeInBytes = 720 * 576 * 3;

extern void Log(const char *fmt, ...);

// Setup data
const AMOVIESETUP_MEDIATYPE sudPinTypesSubtitle =
{
	&MEDIATYPE_MPEG2_SECTIONS, &MEDIASUBTYPE_DVB_SI 
};

const AMOVIESETUP_PIN sudPins[1] =
{
	{
		L"Subtitle",				  // Pin string name
		FALSE,						    // Is it rendered
		FALSE,						    // Is it an output
		FALSE,						    // Allowed none
		FALSE,						    // Likewise many
		&CLSID_NULL,				  // Connects to filter
		L"Subtitle",				  // Connects to pin
		1,							      // Number of types
		&sudPinTypesSubtitle  // Pin information
	}
};
//
// Constructor
//
CDVBSub::CDVBSub( LPUNKNOWN pUnk, HRESULT *phr, CCritSec *pLock ) :
  CBaseFilter( NAME("MediaPortal DVBSub"), pUnk, &m_Lock, CLSID_DVBSub ),
  m_pSubtitlePin( NULL ),
	m_pSubDecoder( NULL ),
	m_pSubtitle( NULL )
{
	// Create subtitle decoder
	m_pSubDecoder = new CDVBSubDecoder();
	
	if( m_pSubDecoder == NULL ) 
	{
    if( phr )
	  {
      *phr = E_OUTOFMEMORY;
	  }
    return;
  }

	// Create subtitle input pin
	m_pSubtitlePin = new CSubtitleInputPin( this,
								GetOwner(),
								this,
								&m_Lock,
								&m_ReceiveLock,
								m_pSubDecoder, 
								phr );
    
	if ( m_pSubtitlePin == NULL ) 
	{
    if( phr )
		{
      *phr = E_OUTOFMEMORY;
		}
    return;
  }

	m_curSubtitleData = NULL;
	m_pSubDecoder->SetObserver( this );
	//m_pSubtitlePin->SetSubtitlePID( 0xfbb );
	m_pSubtitlePin->SetSubtitlePID( 0x403 );
}

CDVBSub::~CDVBSub()
{
	m_pSubDecoder->SetObserver( NULL );
	delete m_pSubDecoder;
	delete m_pSubtitlePin;
}

//
// GetPin
//
CBasePin * CDVBSub::GetPin( int n )
{
	if( n == 0 )
	{
		return m_pSubtitlePin;
	}
	else 
	{
    return NULL;
  }
}

int CDVBSub::GetPinCount()
{
	return 1; // subtitle
}


STDMETHODIMP CDVBSub::Run( REFERENCE_TIME tStart )
{
  CAutoLock cObjectLock( m_pLock );
	Reset();
	return CBaseFilter::Run( tStart );
}

STDMETHODIMP CDVBSub::Pause()
{
  CAutoLock cObjectLock( m_pLock );
	//Reset();
	return CBaseFilter::Pause();
}

STDMETHODIMP CDVBSub::Stop()
{
CAutoLock cObjectLock( m_pLock );
	
	Reset();

	return CBaseFilter::Stop();
}

void CDVBSub::Reset()
{
	CAutoLock cObjectLock( m_pLock );

	m_pSubDecoder->Reset();
	m_pSubtitle = NULL;	// NULL the local pointer, as cache is deleted

	m_pSubtitlePin->Reset();

	m_firstPTS = -1;
}

void CDVBSub::Notify()
{
  // Save subtitle to vobsub format here
  // m_pSubtitle->GetLatestSubtitle() + m_VobSub->Save()? + m_pSubtitleReleaseOldestSubtitle()
}

//
// Interface methods
//
STDMETHODIMP CDVBSub::SetSubtitlePID( ULONG pPID )
{
	if( m_pSubtitlePin )
	{
		m_pSubtitlePin->SetSubtitlePID( pPID );
    return S_OK;
	}
  else
  {
    return S_FALSE;  
  }
}

//
// CreateInstance
//
CUnknown * WINAPI CDVBSub::CreateInstance( LPUNKNOWN punk, HRESULT *phr )
{
  ASSERT( phr );
    
  CDVBSub *pFilter = new CDVBSub( punk, phr, NULL );
  if( pFilter == NULL ) 
	{
    if (phr)
		{
      *phr = E_OUTOFMEMORY;
		}
  }
  return pFilter;
}