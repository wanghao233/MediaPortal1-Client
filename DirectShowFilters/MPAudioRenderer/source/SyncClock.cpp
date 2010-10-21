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

// This code is based on Arto J�rvinen's work - http://www.ostrogothia.com/video/

#pragma once

#include "stdafx.h"
#include "SyncClock.h"
#include "MpAudioRenderer.h"
#include "TimeSource.h"

#include "alloctracing.h"

extern void Log(const char *fmt, ...);

CSyncClock::CSyncClock(LPUNKNOWN pUnk, HRESULT *phr, CMPAudioRenderer* pRenderer, bool pUseHWRefClock)
  : CBaseReferenceClock(NAME("SyncClock"), pUnk, phr),
  m_pCurrentRefClock(0),
  m_pPrevRefClock(0),
  m_dAdjustment(1.0),
  m_dBias(1.0),
  m_pAudioRenderer(pRenderer),
  m_ullStartQpcHW(0),
  m_ullStartTimeHW(0),
  m_ullStartTimeSystem(0),
  m_ullPrevTimeHW(0),
  m_ullPrevQpcHW(0),
  m_dSystemClockMultiplier(1.0),
  m_bHWBasedRefClock(pUseHWRefClock),
  m_llDurationHW(0),
  m_llDurationSystem(0),
  m_ullPrevSystemTime(0),
  m_ullPrivateTime(0)
{
}

void CSyncClock::SetBias(double pBias)
{
  m_SynchCorrection.SetBias(pBias);
  m_dBias = pBias;
}

void CSyncClock::SetAdjustment(double pAdjustment)
{

  m_SynchCorrection.SetAdjustment(pAdjustment);
  m_dAdjustment = pAdjustment;
}

double CSyncClock::Bias()
{
  return m_dBias;
}

double CSyncClock::Adjustment()
{
  return m_dAdjustment;
}

void CSyncClock::GetClockData(CLOCKDATA *pClockData)
{
  // pClockData pointer is validated already in CMPAudioRenderer
  pClockData->driftMultiplier = m_SynchCorrection.GetAVMult();
  pClockData->driftHWvsSystem = (m_llDurationHW - m_llDurationSystem) / 10000.0;
  pClockData->driftAdjustment = m_SynchCorrection.GetCurrentDrift() / 10000.0;
  pClockData->driftHWvsCorrected = 0.0; // remove this and rename others...
}

void CSyncClock::AudioResampled(double sourceLength, double resampleLength,double driftMultiplier)
{
  CAutoLock cObjectLock(this);
  m_SynchCorrection.AudioResampled(sourceLength, resampleLength, driftMultiplier); 
}

char* CSyncClock::DebugData()
{
  return m_SynchCorrection.DebugData(); 
}

double CSyncClock::SuggestedAudioMultiplier(UINT64 sampleLength)
{
  CAutoLock cObjectLock(this);
  return m_SynchCorrection.SuggestedAudioMultiplier(sampleLength);
}

double CSyncClock::GetBias()
{
  CAutoLock cObjectLock(this);
  return m_SynchCorrection.GetBias();
}

REFERENCE_TIME CSyncClock::GetPrivateTime()
{
  CAutoLock cObjectLock(this);

  UINT64 qpcNow = GetCurrentTimestamp();

  UINT64 hwClock(0);
  UINT64 hwQpc(0);

  UINT64 hwClockEnd(0);
  UINT64 hwQpcEnd(0);

  HRESULT hr = m_pAudioRenderer->AudioClock(hwClock, hwQpc);

  if (hr == S_OK && hwClock > 5000000)
  {
    if (m_ullStartQpcHW == 0)
    {
      m_ullStartQpcHW = hwQpc;
      m_ullStartTimeSystem = qpcNow;
      m_ullPrevSystemTime = qpcNow;
    }

    if (m_ullStartTimeHW == 0)
      m_ullStartTimeHW = hwClock;

    m_llDurationHW = (hwClock - m_ullStartTimeHW);
    m_llDurationSystem = (qpcNow - m_ullStartTimeSystem); 

    if (m_ullPrevTimeHW > hwClock)
    {
      m_ullStartTimeHW = m_ullPrevTimeHW = hwClock;
      m_ullStartQpcHW = m_ullPrevQpcHW = hwQpc;
      m_ullStartTimeSystem = qpcNow;
    }
    else
    {
      double clockDiff = hwClock - m_ullStartTimeHW;
      double qpcDiff = hwQpc - m_ullStartQpcHW;

      double prevMultiplier = m_dSystemClockMultiplier;

      if (clockDiff > 0 && qpcDiff > 0)
        m_dSystemClockMultiplier = clockDiff / qpcDiff;

      if (m_dSystemClockMultiplier < 0.95 || m_dSystemClockMultiplier > 1.05)
        m_dSystemClockMultiplier = prevMultiplier;

	  if (m_bHWBasedRefClock)
	  {
        m_SynchCorrection.SetAVMult(m_dSystemClockMultiplier);
      }

      m_ullPrevTimeHW = hwClock;
      m_ullPrevQpcHW = hwQpc;
    }
  }
  else if (hr != S_OK)
  {
    //Log("AudioClock() returned error (0x%08x)");
    if (m_ullStartTimeSystem == 0)
      m_ullStartTimeSystem = qpcNow;

    if (m_ullPrevSystemTime == 0)
      m_ullPrevSystemTime = qpcNow;
  }

  INT64 delta = qpcNow - m_ullPrevSystemTime;
  INT64 deltaOrig = delta;

  if (qpcNow < m_ullPrevSystemTime)
  {
    delta += REFERENCE_TIME(ULLONG_MAX) + 1;
  }

  m_ullPrevSystemTime = qpcNow;
  

  INT64 synchCorrectedDelta = m_SynchCorrection.GetCorrectedTimeDelta(deltaOrig);

  //Log("diff %I64d delta: %I64d synchCorrectedDelta: %I64d", delta - synchCorrectedDelta, delta, synchCorrectedDelta);

  m_ullPrivateTime = m_ullPrivateTime + synchCorrectedDelta;
  return m_ullPrivateTime;
}
