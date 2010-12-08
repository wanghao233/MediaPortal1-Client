#region Copyright (C) 2005-2010 Team MediaPortal

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

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.GUI.Library;
using MediaPortal.TagReader;

namespace MediaPortal.GUI.Music
{
  /// <summary>
  /// Summary description for MusicSort.
  /// </summary>
  public class MusicSort : IComparer<GUIListItem>
  {
    private SortMethod currentSortMethod;
    private bool sortAscending = true;

    public MusicSort(SortMethod method, bool ascending)
    {
      currentSortMethod = method;
      sortAscending = ascending;
    }

    public enum SortMethod
    {
      Name = 0,
      Date = 1, // Shares View = File Modification Date, Database View = Date Added
      Size = 2,
      Track = 3,
      Duration = 4,
      Title = 5,
      Artist = 6,
      Album = 7,
      Filename = 8,
      Rating = 9,
      AlbumArtist = 10, // Only used internally when albumartists or albums need to be sorted by Artist
      Year = 11, // Used Internally, when Sorting by Date is selected from GUI and Year defined as DefaultSort
      DiscID = 12,
    }

    public int Compare(GUIListItem item1, GUIListItem item2)
    {
      if (item1 == item2)
      {
        return 0;
      }
      if (item1 == null)
      {
        return -1;
      }
      if (item2 == null)
      {
        return -1;
      }
      if (item1.IsFolder && item1.Label == "..")
      {
        return -1;
      }
      if (item2.IsFolder && item2.Label == "..")
      {
        return -1;
      }
      if (item1.IsFolder && !item2.IsFolder)
      {
        return -1;
      }
      else if (!item1.IsFolder && item2.IsFolder)
      {
        return 1;
      }

      string strSize1 = "";
      string strSize2 = "";
      if (item1.FileInfo != null)
      {
        strSize1 = Util.Utils.GetSize(item1.FileInfo.Length);
      }
      if (item2.FileInfo != null)
      {
        strSize2 = Util.Utils.GetSize(item2.FileInfo.Length);
      }

      SortMethod method = currentSortMethod;
      bool bAscending = sortAscending;

      switch (method)
      {
        case SortMethod.Name:
          if (bAscending)
          {
            return String.Compare(item1.Label, item2.Label, true);
          }
          else
          {
            return String.Compare(item2.Label, item1.Label, true);
          }


        case SortMethod.Date:
          if (item1.FileInfo == null || item2.FileInfo == null)
          {
            // We didn't get a FileInfo. So it's a DB View and we sort on Date Added from DB
            DateTime time1 = DateTime.MinValue;
            DateTime time2 = DateTime.MinValue;
            if (item1.MusicTag != null)
            {
              time1 = ((MusicTag)item1.MusicTag).DateTimeModified;
            }
            if (item2.MusicTag != null)
            {
              time2 = ((MusicTag)item2.MusicTag).DateTimeModified;
            }

            item1.Label2 = time1.ToShortDateString();
            item2.Label2 = time2.ToShortDateString();
            if (bAscending)
            {
              return DateTime.Compare(time1, time2);
            }
            else
            {
              return DateTime.Compare(time2, time1);
            }
          }
          else
          {
            // Do sorting on File Date. Needed for Shares View
            item1.Label2 = item1.FileInfo.CreationTime.ToShortDateString() + " " +
                           item1.FileInfo.CreationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
            item2.Label2 = item2.FileInfo.CreationTime.ToShortDateString() + " " +
                           item2.FileInfo.CreationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
            if (bAscending)
            {
              return DateTime.Compare(item1.FileInfo.CreationTime, item2.FileInfo.CreationTime);
            }
            else
            {
              return DateTime.Compare(item2.FileInfo.CreationTime, item1.FileInfo.CreationTime);
            }
          }

        case SortMethod.Year:
          item1.Label2 = item1.Year.ToString();
          item2.Label2 = item2.Year.ToString();

          // When sorting on Year, we need to take also the Label into account and sort on that as well
          string compVal1 = item1.Year.ToString() + item1.Label;
          string compVal2 = item2.Year.ToString() + item2.Label;
          if (bAscending)
          {
            if (item1.Year == item2.Year)
            {
              // When the Year is equal just sort on the Label
              return String.Compare(item1.Label, item2.Label, true);
            }
            return String.Compare(compVal1, compVal2, true);
          }
          else
          {
            if (item1.Year == item2.Year)
            {
              // When the Year is equal, sort on label ASCENDING, altough sorting on year is DESC
              return String.Compare(item1.Label, item2.Label, true);
            }
            return String.Compare(compVal2, compVal1, true);
          }

        case SortMethod.Rating:
          int iRating1 = 0;
          int iRating2 = 0;
          if (item1.MusicTag != null)
          {
            iRating1 = ((MusicTag)item1.MusicTag).Rating;
          }
          if (item2.MusicTag != null)
          {
            iRating2 = ((MusicTag)item2.MusicTag).Rating;
          }
          if (bAscending)
          {
            return (int)(iRating1 - iRating2);
          }
          else
          {
            return (int)(iRating2 - iRating1);
          }

        case SortMethod.Size:
          if (item1.FileInfo == null)
          {
            return -1;
          }
          if (item2.FileInfo == null)
          {
            return -1;
          }
          if (bAscending)
          {
            return (int)(item1.FileInfo.Length - item2.FileInfo.Length);
          }
          else
          {
            return (int)(item2.FileInfo.Length - item1.FileInfo.Length);
          }

        case SortMethod.Track:
          int iTrack1 = 0;
          int iTrack2 = 0;
          int iDisk1 = 0;
          int iDisk2 = 0;
          MusicTag tag1 = (MusicTag)item1.MusicTag;
          MusicTag tag2 = (MusicTag)item2.MusicTag;
          if (tag1 != null)
          {
            iTrack1 = tag1.Track;
            iDisk1 = tag1.DiscID;
          }
          if (tag2 != null)
          {
            iTrack2 = tag2.Track;
            iDisk2 = tag2.DiscID;
          }
          if (bAscending)
          {
            if (iDisk1 != iDisk2)
            {
              return iDisk1.CompareTo(iDisk2);
            }
            else
            {
              return iTrack1.CompareTo(iTrack2);
            }
          }
          else
          {
            if (iDisk1 != iDisk2)
            {
              return iDisk2.CompareTo(iDisk1);
            }
            else
            {
              return iTrack2.CompareTo(iTrack1);
            }
          }

        case SortMethod.Duration:
          int iDuration1 = 0;
          int iDuration2 = 0;
          if (item1.MusicTag != null)
          {
            iDuration1 = ((MusicTag)item1.MusicTag).Duration;
          }
          if (item2.MusicTag != null)
          {
            iDuration2 = ((MusicTag)item2.MusicTag).Duration;
          }
          if (bAscending)
          {
            return (int)(iDuration1 - iDuration2);
          }
          else
          {
            return (int)(iDuration2 - iDuration1);
          }

        case SortMethod.Title:
          string strTitle1 = item1.Label;
          string strTitle2 = item2.Label;
          if (item1.MusicTag != null)
          {
            strTitle1 = ((MusicTag)item1.MusicTag).Title;
          }
          if (item2.MusicTag != null)
          {
            strTitle2 = ((MusicTag)item2.MusicTag).Title;
          }
          if (bAscending)
          {
            return String.Compare(strTitle1, strTitle2, true);
          }
          else
          {
            return String.Compare(strTitle2, strTitle1, true);
          }

        case SortMethod.Artist:
          string artist1 = "";
          string artist2 = "";
          if (item1.MusicTag != null)
          {
            artist1 = ((MusicTag)item1.MusicTag).Artist;
          }
          if (item2.MusicTag != null)
          {
            artist2 = ((MusicTag)item2.MusicTag).Artist;
          }
          if (bAscending)
          {
            return String.Compare(artist1, artist2, true);
          }
          else
          {
            return String.Compare(artist2, artist1, true);
          }

        case SortMethod.AlbumArtist:
          string albumartist1 = "";
          string albumartist2 = "";
          if (item1.MusicTag != null)
          {
            albumartist1 = ((MusicTag)item1.MusicTag).AlbumArtist;
          }
          if (item2.MusicTag != null)
          {
            albumartist2 = ((MusicTag)item2.MusicTag).AlbumArtist;
          }
          if (bAscending)
          {
            return String.Compare(albumartist1, albumartist2, true);
          }
          else
          {
            return String.Compare(albumartist2, albumartist1, true);
          }

        case SortMethod.Album:
          string strAlbum1 = "";
          string strAlbum2 = "";
          if (item1.MusicTag != null)
          {
            strAlbum1 = ((MusicTag)item1.MusicTag).Album;
          }
          if (item2.MusicTag != null)
          {
            strAlbum2 = ((MusicTag)item2.MusicTag).Album;
          }
          if (bAscending)
          {
            return String.Compare(strAlbum1, strAlbum2, true);
          }
          else
          {
            return String.Compare(strAlbum2, strAlbum1, true);
          }


        case SortMethod.Filename:
          string strFile1 = Util.Utils.GetFilename(item1.Path);
          string strFile2 = Util.Utils.GetFilename(item2.Path);
          if (bAscending)
          {
            return String.Compare(strFile1, strFile2, true);
          }
          else
          {
            return String.Compare(strFile2, strFile1, true);
          }

        case SortMethod.DiscID:
          if (bAscending)
          {
            return String.Compare(item1.Label2, item2.Label2, true);
          }
          else
          {
            return String.Compare(item2.Label2, item1.Label2, true);
          }
      }
      return 0;
    }
  }
}