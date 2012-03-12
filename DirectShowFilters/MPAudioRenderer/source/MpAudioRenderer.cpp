// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

// parts of the code are based on MPC-HC audio renderer source code

#include "stdafx.h"
#include <initguid.h>
#include "moreuuids.h"
#include <ks.h>
#include <ksmedia.h>
#include <propkey.h>
#include <FunctionDiscoveryKeys_devpkey.h>

#include "MpAudioRenderer.h"
#include "FilterApp.h"

#include "alloctracing.h"

CFilterApp theApp;

CUnknown* WINAPI CMPAudioRenderer::CreateInstance(LPUNKNOWN punk, HRESULT* phr)
{
  ASSERT(phr);
  CMPAudioRenderer *pNewObject = new CMPAudioRenderer(punk, phr);

  if (!pNewObject)
  {
    if (phr)
      *phr = E_OUTOFMEMORY;
  }
  return pNewObject;
}

// for logging
extern void Log(const char* fmt, ...);
extern void LogWaveFormat(const WAVEFORMATEX* pwfx, const char* text);
extern void StopLogger();

CMPAudioRenderer::CMPAudioRenderer(LPUNKNOWN punk, HRESULT* phr)
  : CBaseRenderer(__uuidof(this), NAME("MediaPortal - Audio Renderer"), punk, phr),
  m_dRate(1.0),
  m_pReferenceClock(NULL),
  m_dBias(1.0),
  m_dAdjustment(1.0),
  m_dSampleCounter(0),
  m_pVolumeHandler(NULL),
  m_pWASAPIRenderer(NULL),
  m_pAC3Encoder(NULL),
  m_pInBitDepthAdapter(NULL),
  m_pOutBitDepthAdapter(NULL),
  m_pSampleRateConverter(NULL),
  m_pRenderer(NULL),
  m_pTimeStretch(NULL)
{
  Log("CMPAudioRenderer - instance 0x%x", this);

  m_pClock = new CSyncClock(static_cast<IBaseFilter*>(this), phr, this, m_Settings.m_bHWBasedRefClock);

  if (!m_pClock)
  {
    if (phr)
      *phr = E_OUTOFMEMORY;
    return;
  }

  m_pClock->SetAudioDelay(m_Settings.m_lAudioDelay * 10000); // setting in registry is in ms

  m_pVolumeHandler = new CVolumeHandler(punk);

  if (m_pVolumeHandler)
    m_pVolumeHandler->AddRef();
  else
  {
    if (phr)
      *phr = E_OUTOFMEMORY;
    return;
  }

  // CBaseRenderer is using a lazy initialization for the CRendererPosPassThru - we need it always
  CBasePin *pPin = GetPin(0);
  HRESULT hr = E_OUTOFMEMORY;
  m_pPosition = new CRendererPosPassThru(NAME("Renderer CPosPassThru"), CBaseFilter::GetOwner(), &hr, pPin);
  if (!m_pPosition && FAILED(hr))
  {
    if (phr)
      *phr = hr;
    return;
  }
  
  hr = SetupFilterPipeline();
  if (FAILED(hr))
  {
    if (phr)
      *phr = hr;
    return;
  }  

  hr = m_pPipeline->Init();
  if (FAILED(hr))
  {
    if (phr)
      *phr = hr;
    return;
  }  

  m_pPipeline->Start(0);
}

CMPAudioRenderer::~CMPAudioRenderer()
{
  Log("MP Audio Renderer - destructor - instance 0x%x", this);
  
  CAutoLock cInterfaceLock(&m_InterfaceLock);

  if (m_pVolumeHandler)
    m_pVolumeHandler->Release();

  delete m_pClock;

  if (m_pReferenceClock)
  {
    SetSyncSource(NULL);
    SAFE_RELEASE(m_pReferenceClock);
  }

  HRESULT hr = m_pPipeline->Cleanup();
  if (FAILED(hr))
    Log("Pipeline cleanup failed with: (0x%08x)");

  m_pPipeline->DisconnectAll();
  if (FAILED(hr))
    Log("Pipeline DisconnectAll failed with: (0x%08x)");

  delete m_pWASAPIRenderer;
  delete m_pAC3Encoder;
  delete m_pInBitDepthAdapter;
  delete m_pOutBitDepthAdapter;
  delete m_pTimestretchFilter;
  delete m_pSampleRateConverter;
  delete m_pChannelMixer;

  Log("MP Audio Renderer - destructor - instance 0x%x - end", this);
  StopLogger();
}

HRESULT CMPAudioRenderer::SetupFilterPipeline()
{
  m_pWASAPIRenderer = new CWASAPIRenderFilter(&m_Settings, m_pClock);
  if (!m_pWASAPIRenderer)
    return E_OUTOFMEMORY;

  m_pRenderer = static_cast<IRenderFilter*>(m_pWASAPIRenderer);

  m_pAC3Encoder = new CAC3EncoderFilter(&m_Settings);
  if (!m_pAC3Encoder)
    return E_OUTOFMEMORY;

  m_pInBitDepthAdapter = new CBitDepthAdapter();
  if (!m_pInBitDepthAdapter)
    return E_OUTOFMEMORY;

  m_pOutBitDepthAdapter = new CBitDepthAdapter();
  if (!m_pOutBitDepthAdapter)
    return E_OUTOFMEMORY;

  m_pTimestretchFilter = new CTimeStretchFilter(&m_Settings, m_pClock);
  if (!m_pTimestretchFilter)
    return E_OUTOFMEMORY;

  m_pTimeStretch = static_cast<ITimeStretch*>(m_pTimestretchFilter);

  m_pSampleRateConverter = new CSampleRateConverter(&m_Settings);
  if (!m_pSampleRateConverter)
    return E_OUTOFMEMORY;

  m_pChannelMixer = new CChannelMixer(&m_Settings, m_pWASAPIRenderer);
  if (!m_pChannelMixer)
    return E_OUTOFMEMORY;

  m_pInBitDepthAdapter->ConnectTo(m_pSampleRateConverter);
  m_pSampleRateConverter->ConnectTo(m_pChannelMixer);
  m_pChannelMixer->ConnectTo(m_pTimestretchFilter);
  m_pTimestretchFilter->ConnectTo(m_pOutBitDepthAdapter);
  m_pOutBitDepthAdapter->ConnectTo(m_pAC3Encoder);
  m_pAC3Encoder->ConnectTo(m_pWASAPIRenderer);
  
  // Entry point for the audio filter pipeline
  m_pPipeline = m_pInBitDepthAdapter;

  return S_OK;
}

HRESULT CMPAudioRenderer::CheckInputType(const CMediaType* pmt)
{
  return CheckMediaType(pmt);
}

HRESULT	CMPAudioRenderer::CheckMediaType(const CMediaType* pmt)
{
  HRESULT hr = S_OK;
  
  if (!pmt) 
    return E_INVALIDARG;
  
  if ((pmt->majortype	!= MEDIATYPE_Audio) ||
      (pmt->formattype != FORMAT_WaveFormatEx))
  {
    Log("CheckMediaType Not supported");
    return VFW_E_TYPE_NOT_ACCEPTED;
  }

  WAVEFORMATEX* pwfx = (WAVEFORMATEX*)pmt->Format();

  if (!pwfx) 
    return VFW_E_TYPE_NOT_ACCEPTED;

  if (IS_WAVEFORMATEXTENSIBLE(pwfx))
    return m_pPipeline->NegotiateFormat((WAVEFORMATEXTENSIBLE*)(pwfx), 0);
  else
  {
    WAVEFORMATEXTENSIBLE* wfe = NULL;
    hr = ToWaveFormatExtensible(&wfe, pwfx);
    if (SUCCEEDED(hr))
    {
      hr = m_pPipeline->NegotiateFormat(wfe, 0);
      delete wfe;
    }
    return hr;
  }
}

HRESULT CMPAudioRenderer::AudioClock(UINT64& pTimestamp, UINT64& pQpc)
{
  if (m_pRenderer)
    return m_pRenderer->AudioClock(pTimestamp, pQpc);
  else
    return S_FALSE;

  //TRACE(_T("AudioClock query pos: %I64d qpc: %I64d"), pTimestamp, pQpc);
}

BOOL CMPAudioRenderer::ScheduleSample(IMediaSample* pMediaSample)
{
  if (!pMediaSample) return false;

  REFERENCE_TIME rtSampleTime = 0;
  REFERENCE_TIME rtSampleEndTime = 0;

  HRESULT hr = GetSampleTimes(pMediaSample, &rtSampleTime, &rtSampleEndTime);
  if (FAILED(hr)) return false;

  if (hr == S_FALSE || hr == S_OK) 
  {
    WAVEFORMATEXTENSIBLE* wfe = NULL;
    AM_MEDIA_TYPE* pmt = NULL;

    if (SUCCEEDED(pMediaSample->GetMediaType(&pmt)) && pmt)
    {
      WAVEFORMATEX* pwfx = (WAVEFORMATEX*)pmt->pbFormat;
  
      // Convert WAVEFORMATEX to WAVEFORMATEXTENSIBLE for internal use
      if (!IS_WAVEFORMATEXTENSIBLE(pwfx))
      {
        ToWaveFormatExtensible(&wfe, pwfx);
        FreeMediaType(*pmt);
        pmt->pbFormat = (BYTE*)CoTaskMemAlloc(sizeof(WAVEFORMATEXTENSIBLE));
        memcpy(pmt->pbFormat, wfe, sizeof(WAVEFORMATEXTENSIBLE));
        pMediaSample->SetMediaType(pmt);
        delete wfe;
      }
    }

    m_pPipeline->PutSample(pMediaSample);
    
    // Lie about the sample being dropped. This way we dont have to
    // go thru the extra round trip that firing the m_RenderEvent
    // would cause. Real scheduling is done in the WASAPI renderer 
    // at the other end of the pipeline
    
    return false;
  }
  
  return false;
}

HRESULT	CMPAudioRenderer::DoRenderSample(IMediaSample* pMediaSample)
{
  // Sample was already put into the pipeline in ScheduleSample().
  return S_OK;
}

STDMETHODIMP CMPAudioRenderer::NonDelegatingQueryInterface(REFIID riid, void** ppv)
{
  if (riid == IID_IReferenceClock)
    return GetInterface(static_cast<IReferenceClock*>(m_pClock), ppv);

  if (riid == IID_IAVSyncClock) 
    return GetInterface(static_cast<IAVSyncClock*>(this), ppv);

  if (riid == IID_IMediaSeeking) 
    return GetInterface(static_cast<IMediaSeeking*>(this), ppv);

  if (riid == IID_IBasicAudio)
    return GetInterface(static_cast<IBasicAudio*>(m_pVolumeHandler), ppv);

	return CBaseRenderer::NonDelegatingQueryInterface (riid, ppv);
}

HRESULT CMPAudioRenderer::SetMediaType(const CMediaType* pmt)
{
  if (!pmt) return E_POINTER;
  
  HRESULT hr = S_OK;

  Log("SetMediaType - filter state: %d", m_State);

  WAVEFORMATEXTENSIBLE* wfe = NULL;
  WAVEFORMATEX* pwfx = (WAVEFORMATEX*)pmt->Format();
  
  // Dynamic format changes are handled in the pipeline on sample basis  
  int depth = m_State != State_Stopped ? 0 : INFINITE;

  if (IS_WAVEFORMATEXTENSIBLE(pwfx))
    hr = m_pPipeline->NegotiateFormat((WAVEFORMATEXTENSIBLE*)pwfx, depth);
  else
  {
    WAVEFORMATEXTENSIBLE* wfe = NULL;
    hr = ToWaveFormatExtensible(&wfe, pwfx);
    if (SUCCEEDED(hr))
    {
      hr = m_pPipeline->NegotiateFormat(wfe, depth);
      delete wfe;
    }
    else
      return hr;
  }

  if (FAILED(hr))
    return hr;

  return CBaseRenderer::SetMediaType(pmt);
}

HRESULT CMPAudioRenderer::CompleteConnect(IPin* pReceivePin)
{
  Log("CompleteConnect");

  HRESULT hr = S_OK;
  PIN_INFO pinInfo;
  FILTER_INFO filterInfo;
  
  hr = pReceivePin->QueryPinInfo(&pinInfo);
  if (!SUCCEEDED(hr)) return E_FAIL;
  if (pinInfo.pFilter == NULL) return E_FAIL;
  hr = pinInfo.pFilter->QueryFilterInfo(&filterInfo);
  filterInfo.pGraph->Release();
  pinInfo.pFilter->Release();

  if (FAILED(hr)) 
    return E_FAIL;
  
  Log("CompleteConnect - audio decoder: %S", &filterInfo.achName);

  if (SUCCEEDED(hr)) hr = CBaseRenderer::CompleteConnect(pReceivePin);
  if (SUCCEEDED(hr)) Log("CompleteConnect Success");

  return hr;
}

STDMETHODIMP CMPAudioRenderer::Run(REFERENCE_TIME tStart)
{
  Log("Run");
  CAutoLock cInterfaceLock(&m_InterfaceLock);
  
  HRESULT	hr = S_OK;

  if (m_State == State_Running) 
    return hr;

  if (m_pClock)
    m_pClock->Reset();

  hr = m_pPipeline->Run(tStart);
     
  if (FAILED(hr))
    return hr;

  return CBaseRenderer::Run(tStart);
}

STDMETHODIMP CMPAudioRenderer::Stop() 
{
  Log("Stop");

  CAutoLock cInterfaceLock(&m_InterfaceLock);
  
  m_pPipeline->BeginStop();
  m_pPipeline->EndStop();

  return CBaseRenderer::Stop(); 
};

STDMETHODIMP CMPAudioRenderer::Pause()
{
  Log("Pause");
  CAutoLock cInterfaceLock(&m_InterfaceLock);

  m_dSampleCounter = 0;
  HRESULT hr = m_pPipeline->Pause();

  if (FAILED(hr))
    return hr;

  return CBaseRenderer::Pause(); 
}

HRESULT CMPAudioRenderer::GetReferenceClockInterface(REFIID riid, void **ppv)
{
  HRESULT hr = S_OK;

  if (m_pReferenceClock)
    return m_pReferenceClock->NonDelegatingQueryInterface(riid, ppv);

  m_pReferenceClock = new CBaseReferenceClock (NAME("MP Audio Clock"), NULL, &hr);
	
  if (!m_pReferenceClock)
    return E_OUTOFMEMORY;

  m_pReferenceClock->AddRef();

  hr = SetSyncSource(m_pReferenceClock);
  if (FAILED(hr)) 
  {
    SetSyncSource(NULL);
    return hr;
  }

  return GetReferenceClockInterface(riid, ppv);
}

HRESULT CMPAudioRenderer::EndOfStream()
{
  Log("EndOfStream");

  HRESULT hr = m_pPipeline->EndOfStream();
  if (FAILED(hr))
    return hr;

  return CBaseRenderer::EndOfStream();
}

HRESULT CMPAudioRenderer::BeginFlush()
{
  Log("BeginFlush");

  CAutoLock cInterfaceLock(&m_InterfaceLock);

  HRESULT hr = CBaseRenderer::BeginFlush(); 
  if (FAILED(hr))
    return hr;

  return m_pPipeline->BeginFlush();
}

HRESULT CMPAudioRenderer::EndFlush()
{
  Log("EndFlush");
  CAutoLock cInterfaceLock(&m_InterfaceLock);
  
  m_dSampleCounter = 0;

  m_pPipeline->EndFlush();
  m_pClock->Reset();

  return CBaseRenderer::EndFlush(); 
}

// TODO - implement TsReader side as well

/*
bool CMPAudioRenderer::CheckForLiveSouce()
{
  FILTER_INFO filterInfo;
  ZeroMemory(&filterInfo, sizeof(filterInfo));
  m_EVRFilter->QueryFilterInfo(&filterInfo); // This addref's the pGraph member

  CComPtr<IBaseFilter> pBaseFilter;

  HRESULT hr = filterInfo.pGraph->FindFilterByName(L"MediaPortal File Reader", &pBaseFilter);
  filterInfo.pGraph->Release();
}*/

// IAVSyncClock interface implementation

HRESULT CMPAudioRenderer::AdjustClock(DOUBLE pAdjustment)
{
  //CAutoLock cAutoLock(&m_csResampleLock);
  
  if (m_Settings.m_bUseTimeStretching && m_Settings.m_bEnableSyncAdjustment)
  {
    m_dAdjustment = pAdjustment;
    m_pClock->SetAdjustment(m_dAdjustment);
    
    if (m_pTimeStretch)
      m_pTimeStretch->setTempo(m_dBias, m_dAdjustment);

    return S_OK;
  }
  else
    return S_FALSE;
}

HRESULT CMPAudioRenderer::SetEVRPresentationDelay(DOUBLE pEVRDelay)
{
  //CAutoLock cAutoLock(&m_csResampleLock);

  bool ret = S_FALSE;

  if (m_Settings.m_bUseTimeStretching)
  {
    Log("SetPresentationDelay: %1.10f", pEVRDelay);

    m_pClock->SetEVRDelay(pEVRDelay * 10000); // Presenter sets delay in ms

    ret = S_OK;
  }
  else
  {
    Log("SetPresentationDelay: %1.10f - failed, time stretching is disabled", pEVRDelay);
    ret = S_FALSE;  
  }

  return ret;
}

HRESULT CMPAudioRenderer::SetBias(DOUBLE pBias)
{
  //CAutoLock cAutoLock(&m_csResampleLock);

  bool ret = S_FALSE;

  if (m_Settings.m_bUseTimeStretching)
  {
    Log("SetBias: %1.10f", pBias);

    if (pBias < m_Settings.m_dMinBias)
    {
      Log("   bias value too small - using 1.0");
      m_dBias = 1.0;
      ret = S_FALSE; 
    }
    else if(pBias > m_Settings.m_dMaxBias)
    {
      Log("   bias value too big - using 1.0");
      m_dBias = 1.0;
      ret = S_FALSE; 
    }
    else
    {
      m_dBias = pBias;
      ret = S_OK;  
    }
    
    m_pClock->SetBias(m_dBias);
    if (m_pTimeStretch)
    {
      m_pTimeStretch->setTempo(m_dBias, m_dAdjustment);
      Log("SetBias - updated SoundTouch tempo");
      // ret is not set since we want to be able to indicate the too big / small bias value	  
    }
    else
    {
      Log("SetBias - no timestretch filter in pipeline");
      ret = S_FALSE;
    }
  }
  else
  {
    Log("SetBias: %1.10f - failed, time stretching is disabled", pBias);
    ret = S_FALSE;  
  }

  return ret;
}

HRESULT CMPAudioRenderer::GetBias(DOUBLE* pBias)
{
  CheckPointer(pBias, E_POINTER);
  *pBias = m_pClock->Bias();

  return S_OK;
}

HRESULT CMPAudioRenderer::GetMaxBias(DOUBLE *pMaxBias)
{
  CheckPointer(pMaxBias, E_POINTER);
  *pMaxBias = m_Settings.m_dMaxBias;

  return S_OK;
}

HRESULT CMPAudioRenderer::GetMinBias(DOUBLE *pMinBias)
{
  CheckPointer(pMinBias, E_POINTER);
  *pMinBias = m_Settings.m_dMinBias;

  return S_OK;
}

HRESULT CMPAudioRenderer::GetClockData(CLOCKDATA *pClockData)
{
  CheckPointer(pClockData, E_POINTER);
  m_pClock->GetClockData(pClockData);

  return S_OK;
}
