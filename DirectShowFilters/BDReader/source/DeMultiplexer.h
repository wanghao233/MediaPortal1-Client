/*
 *  Copyright (C) 2005 Team MediaPortal
 *  http://www.team-mediaportal.com
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
#pragma once

#include "StdAfx.h"
#include <atlbase.h>
#include <atlcoll.h>
#include "pcrdecoder.h"
#include "..\..\shared\packetSync.h"
#include "pidtable.h"
#include "..\..\shared\tsheader.h"
#include "patparser.h"
#include "channelInfo.h"
#include "PidTable.h"
#include "..\..\shared\Pcr.h"
#include <vector>
#include <map>
#include <dvdmedia.h>
#include "MpegPesParser.h"
#include <bluray.h>
#include "BDEventObserver.h"

using namespace std;
class CBDReaderFilter;

#define MAX_BUF_SIZE 200000
#define READ_SIZE (1344 * 60)
#define INITIAL_READ_SIZE (READ_SIZE * 1024)

// TODO - enum
#define SUPERCEEDED_AUDIO 1
#define SUPERCEEDED_VIDEO 2
#define SUPERCEEDED_SUBTITLE 4

// 0.5s
#define ALLOWED_PACKET_DIFF 640000LL

struct PlaylistInfo
{
  int playlist;
  int clip;
  bool noAudio;

  REFERENCE_TIME lastStart;
  REFERENCE_TIME lastDuration;
  REFERENCE_TIME lastVideoStart;
  REFERENCE_TIME lastVideoDuration;
  REFERENCE_TIME lastSubtitleStart;
  REFERENCE_TIME lastSubtitleDuration;
  REFERENCE_TIME firstTimestamp;
  REFERENCE_TIME previousPlaylistEndTimestamp;
  REFERENCE_TIME timeStampCorrection;
  REFERENCE_TIME clipDuration;

  int superceeded;
};


class Packet : public CAtlArray<BYTE>
{
public:

  Packet() 
  { 
    nClipNumber = -100;
    nPlaylist = -100;
    bDiscontinuity = false;
    bSyncPoint = false;
    bAppendable = false;
    rtStart = -100;
    rtStop = -100;
    pmt = NULL;
  }

  virtual ~Packet() {if(pmt) DeleteMediaType(pmt);}
  virtual int GetDataSize() {return GetCount();}

  INT32 nClipNumber;
  INT32 nPlaylist;
  bool bDiscontinuity, bSyncPoint, bAppendable;
	static const REFERENCE_TIME INVALID_TIME = _I64_MIN;
	REFERENCE_TIME rtStart, rtStop;
	AM_MEDIA_TYPE* pmt;
	void SetData(const void* ptr, DWORD len) {SetCount(len); memcpy(GetData(), ptr, len);}
};

class CDeMultiplexer : public CPacketSync, public IPatParserCallback, public BDEventObserver
{
public:
  CDeMultiplexer(CBDReaderFilter& filter);
  virtual ~CDeMultiplexer(void);

  HRESULT    Start();
  void       Flush(bool pSoftFlush);
  Packet*    GetVideo();
  Packet*    GetAudio();
  Packet*    GetAudio(int playlist, int clip);
  Packet*    GetSubtitle();
  void       OnTsPacket(byte* tsPacket);
  void       OnNewChannel(CChannelInfo& info);
  void       FillSubtitle(CTsHeader& header, byte* tsPacket);
  void       FillAudio(CTsHeader& header, byte* tsPacket);
  void       ParseAudioHeader(CTsHeader& header, byte* tsPacket);
  void       FillVideo(CTsHeader& header, byte* tsPacket);
  void       FillVideoH264PESPacket(CTsHeader& header, CAutoPtr<Packet> p);
  void       FillVideoVC1PESPacket(CTsHeader& header, CAutoPtr<Packet> p);
  void       FillVideoH264(CTsHeader& header, byte* tsPacket);
  void       FillVideoMPEG2(CTsHeader& header, byte* tsPacket);
  void       CheckVideoFormat(Packet* p);
  void       SetEndOfFile(bool bEndOfFile);
  CPidTable  GetPidTable();

  int        GetAudioBufferPts(CRefTime& First, CRefTime& Last);
  int        GetVideoBufferPts(CRefTime& First, CRefTime& Last);

  bool       SetAudioStream(__int32 stream);
  bool       GetAudioStream(__int32 &stream);

  void       GetAudioStreamInfo(int stream, char* szName);
  void       GetAudioStreamType(int stream, CMediaType& pmt);
  void       GetVideoStreamType(CMediaType &pmt);
  int        GetAudioStreamCount();

  // BDReader::ISubtitleStream uses these
  bool       SetSubtitleStream(__int32 stream);
  bool       GetSubtitleStreamType(__int32 stream, __int32& count);
  bool       GetSubtitleStreamCount(__int32 &count);
  bool       GetCurrentSubtitleStream(__int32 &stream);
  bool       GetSubtitleStreamLanguage(__int32 stream, char* szLanguage);
  bool       SetSubtitleResetCallback( int (CALLBACK *pSubUpdateCallback)(int c, void* opts, int* select));

  bool       EndOfFile();
  bool       HoldAudio();
  void       SetHoldAudio(bool onOff);
  bool       HoldVideo();
  void       SetHoldVideo(bool onOff);
  bool       HoldSubtitle();
  void       SetHoldSubtitle(bool onOff);
  void       ThreadProc();
  void       FlushVideo(bool pSoftFlush);
  void       FlushAudio(bool pSoftFlush);
  void       FlushSubtitle(bool pSoftFlush);
  int        GetVideoServiceType();

  void SetMediaChanging(bool onOff);
  bool IsMediaChanging();

  // From BDEventObserver
  void HandleBDEvent(BD_EVENT& pEv, UINT64 pPos);
  void HandleOSDUpdate(OSDTexture& pTexture);
  void HandleMenuStateChange(bool pVisible);

  bool m_bAudioVideoReady;

  // Used for playlist/clip tracking
  REFERENCE_TIME GetCompensationForClip(int playlist, int clip);
  PlaylistInfo* GetLatestPlaylist();

  bool m_audioPlSeen;
  bool m_videoPlSeen;

private:
  void ResetClipInfo(int pDebugMark);
  Packet* GenerateFakeAudio();
  void PacketDelivery(Packet* p, CTsHeader header);
  struct stAudioStream
  {
    int pid;
    int audioType;
    char language[7];
  };
  struct stSubtitleStream
  {
    int pid;
    int subtitleType;  // 0=DVB, 1=teletext <-- needs enum
    char language[4];
  };

  vector<struct stAudioStream> m_audioStreams;
  vector<struct stSubtitleStream> m_subtitleStreams;
  int ReadFromFile(bool isAudio, bool isVideo);
  void ResetStream();
  bool m_bEndOfFile;
  HRESULT RenderFilterPin(CBasePin* pin, bool isAudio, bool isVideo);
  CCritSec m_sectionAudio;
  CCritSec m_sectionVideo;
  CCritSec m_sectionSubtitle;
  CCritSec m_sectionRead;
  CCritSec m_sectionAudioChanging;
  CCritSec m_sectionMediaChanging;
  CPatParser m_patParser;
  CMpegPesParser *m_mpegPesParser;
  CPidTable m_pids;
  vector<Packet*> m_vecSubtitleBuffers;
  vector<Packet*> m_vecVideoBuffers;
  vector<Packet*> m_t_vecVideoBuffers;
  vector<Packet*> m_vecAudioBuffers;
  vector<Packet*> m_t_vecAudioBuffers;
  typedef vector<Packet*>::iterator ivecVBuffers;
  typedef vector<Packet*>::iterator ivecABuffers;
  typedef vector<Packet*>::iterator ivecSBuffers;
  bool m_AudioValidPES;
  bool m_VideoValidPES;
  bool m_mVideoValidPES;
  int  m_WaitHeaderPES;
  REFERENCE_TIME m_FirstAudioSample;
  REFERENCE_TIME m_LastAudioSample;
  REFERENCE_TIME m_FirstVideoSample;
  REFERENCE_TIME m_LastVideoSample;

  Packet* m_pCurrentSubtitleBuffer;
  Packet* m_pCurrentVideoBuffer;
  Packet* m_pCurrentAudioBuffer;

  CPcr m_streamPcr;
  CPcr m_lastVideoPTS;
  CPcr m_lastAudioPTS;
  CBDReaderFilter& m_filter;
  unsigned int m_iAudioStream;
  int m_AudioStreamType;

  unsigned int m_audioPid;
  unsigned int m_currentSubtitlePid;
  unsigned int m_iSubtitleStream;

  bool m_bHoldAudio;
  bool m_bHoldVideo;
  bool m_bHoldSubtitle;
  int m_iAudioIdx;
  int m_iPatVersion;

  int m_loopLastSearch;
  int m_fakeAudioPacketCount;
  bool m_fakeAudioVideoSeen;
  int m_fakeAudioPlaylist;
  int m_fakeAudioClip;

  bool  m_bWaitForMediaChange;
  DWORD m_tWaitForMediaChange;
  bool  m_bWaitForAudioSelection;
  DWORD m_tWaitForAudioSelection;

  bool m_bStarting;
  bool m_bReadFailed;

  bool m_bRebuildOnVideoChange;
  bool m_bDoDelayedRebuild;
  bool m_bSetAudioDiscontinuity;
  bool m_bSetVideoDiscontinuity;
  CPcr m_subtitlePcr;

  REFERENCE_TIME m_rtPrev, m_rtOffset, m_rtMaxShift;

  int (CALLBACK *pSubUpdateCallback)(int c, void* opts,int* bi);

  // Used only for H.264 stream demuxing
  CAutoPtr<Packet> m_p;
  CAutoPtr<Packet> m_pBuild;
  CAutoPtrList<Packet> m_pl;
  bool m_fHasAccessUnitDelimiters;
  DWORD m_lastStart;
  CPcr m_VideoPts;
  CPcr m_CurrentVideoPts;
  bool m_bInBlock;
  double m_curFrameRate;
  int m_LastValidFrameCount;
  CPcr m_LastValidFramePts;

  bool m_bShuttingDown;

  map<int, CMediaType> m_mAudioMediaTypes;
  int m_audioStreamsToBeParsed;
  bool m_bDelayedAudioTypeChange;

  byte m_readBuffer[READ_SIZE];

  // TODO rename / cleanup
  REFERENCE_TIME m_rtNewOffset;
  UINT64 m_newOffsetPos;
  INT32 m_nClip;
  INT32 m_nPlaylist;
  bool m_bOffsetBackwards;

  REFERENCE_TIME m_rtVideoOffset;
  REFERENCE_TIME m_rtAudioOffset;
  REFERENCE_TIME m_rtSubtitleOffset;

  bool m_bForceUpdateAudioOffset;
  bool m_bForceUpdateVideoOffset;
  bool m_bForceUpdateSubtitleOffset;

  INT32 m_nAudioClip;
  INT32 m_nAudioPl;
  INT32 m_nVideoClip;
  INT32 m_nVideoPl;
  INT32 m_nSubtitleClip;
  INT32 m_nSubtitlePl;

  PlaylistInfo* NewPlayList();

  // use per PID
  CMediaType m_lpcmPmt;
  CFrameHeaderParser m_audioParser;
  //Used for playlist/clip tracking
  vector<PlaylistInfo*> m_vecActivePlaylists;
  void SetNewPlaylist(int playlist, int clip, REFERENCE_TIME startTimeOffset, bool noAudio, REFERENCE_TIME duration);
  bool CorrectPacketPlaylist(Packet* p, int packetType);
  void ResetPlayListMonitor();
  bool IsValidPacket(Packet* p);
  typedef vector<PlaylistInfo*>::iterator ivecPlaylists;
  PlaylistInfo* GetPlaylistInfo(int playlist, int clip);
  PlaylistInfo* GetCurrentVideoPlaylist();
};
