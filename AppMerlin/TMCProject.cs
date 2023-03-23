using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Data;
using System.Net;

namespace AppMerlin
{
  public class TMCProject
  {
    public string m_OrderNumber;
    public string m_ProjectName;
    public IntervalLength m_IntervalLength;
    public bool m_IsRTOR;
    public bool m_IsUTurn;
    public string m_Directory;
    public bool m_DataImported;
    public bool m_FlagsGenerated;
    public ProjectSource m_projectSource;
    public List<string> m_Banks;
    public List<string> m_TimePeriods;  //start and end time
    public List<string> m_TimePeriodLabels; //name
    public List<Intersection> m_Intersections;
    public Preferences m_Prefs;
    public List<Note> m_Notes;  //project-level notes
    [XmlIgnore]
    public Dictionary<int, string> m_BankDictionary;
    public List<int> m_HeavyBanks;
    public List<PedColumnDataType> m_PedBanks;
    public string m_MerlinVersion;
    [XmlIgnore]
    public Dictionary<int, List<string>> m_ColumnHeaders;
    public List<string> COLUMNHEADERS;    //This is an unfortunate artifact of an old system.  Must be present for de-serializing.
    public List<int> m_TimePeriodIDs;
    public DataState m_ProjectDataState
    {
      get { return GetProjectDataState(); }
    }
    public bool m_NCDOTColSwappingEnabled;
    public List<TubeSite> m_Tubes;
    public bool m_TCCDataFileRules;
    public string m_TubeOrderNumber;

    #region Constructors

    public TMCProject()
    {
      m_ColumnHeaders = new Dictionary<int, List<string>>();
      m_Intersections = new List<Intersection>();
      m_TimePeriods = new List<string>();
      m_TimePeriodLabels = new List<string>();
      m_TimePeriodIDs = new List<int>();
      m_Prefs = new Preferences();
      m_Notes = new List<Note>();
      m_Banks = new List<string>();
      m_BankDictionary = new Dictionary<int, string>();
      m_HeavyBanks = new List<int>();
      m_PedBanks = new List<PedColumnDataType>();
      COLUMNHEADERS = new List<string>();
      m_NCDOTColSwappingEnabled = false;
      m_Tubes = new List<TubeSite>();
      m_TCCDataFileRules = false;
      m_TubeOrderNumber = null;
    }

    //constructor
    public TMCProject(string OrderNumber, Preferences prefs, string ProjectName, List<string> TimePeriods, List<string> TimePeriodLabels, List<int> TimePeriodIDs, List<string> Banks, List<PedColumnDataType> PedBanks, string version, ProjectSource source, bool colSwap, bool tccRules, string tubeOrderNumber = null)
    {
      m_OrderNumber = OrderNumber;
      m_ProjectName = ProjectName;
      m_TimePeriods = TimePeriods;
      m_TimePeriodLabels = TimePeriodLabels;
      m_Directory = "";
      m_DataImported = false;
      m_FlagsGenerated = false;
      m_Banks = Banks;
      m_Intersections = new List<Intersection>();
      m_Prefs = prefs;
      m_Notes = new List<Note>();
      m_IntervalLength = IntervalLength.Five;
      m_BankDictionary = SetupBankDictionary();
      m_HeavyBanks = GetBanksConsideredHeavies();
      m_PedBanks = PedBanks;
      m_MerlinVersion = version;
      m_IsRTOR = IsRTOR();
      m_IsUTurn = IsUTurn();
      m_projectSource = source;
      m_ColumnHeaders = SetupColumnHeaders();
      m_TimePeriodIDs = TimePeriodIDs;
      m_NCDOTColSwappingEnabled = colSwap;
      m_Tubes = new List<TubeSite>();
      m_TCCDataFileRules = tccRules;
      m_TubeOrderNumber = tubeOrderNumber;
    }

    #endregion Constructors

    #region Version Control

    public List<string> RunVersionControl(string version, string currentUser)
    {
      List<string> changesList = new List<string>();
      if (m_MerlinVersion == null)
      {
        m_MerlinVersion = "0.0.0";
      }

      int majorVersion = int.Parse(m_MerlinVersion.Split('.')[0]);
      int minorVersion = int.Parse(m_MerlinVersion.Split('.')[1]);
      int buildVersion = int.Parse(m_MerlinVersion.Split('.')[2]);

      if (majorVersion < 1 && minorVersion <= 4 && buildVersion <= 5)
      {
        PopulatePedBanks();

        // Handle changes to Bank Vehicle Types
        for (int i = 0; i < m_Banks.Count; i++)
        {
          if (m_Banks[i] == "BicyclesAndUTurns")
          {
            m_Banks[i] = "Bicycles";
          }
        }

      }
      if (majorVersion < 1 && minorVersion <= 4 && buildVersion <= 11)
      {
        m_ColumnHeaders = SetupColumnHeaders();
        ConvertColumnHeaders();
        changesList.Add("Column Headers and Bank Tabs Updated");
      }
      if (majorVersion < 1 && minorVersion <= 4 && buildVersion <= 18)
      {
        //Count.cs now uses index in m_Projects list of time periods to store TP, instead of one of previous 8 TP enums
        ConvertCountTimePeriodStorage();
      }

      if (majorVersion < 1 && minorVersion <= 4 && buildVersion <= 20)
      {
        ConvertSettings();
        changesList.Add("Changes to Merlin Threshold Settings. You will need to verify settings.");
      }

      if (majorVersion < 2)
      {
        AssignIDsToLocationsAndTimePeriods();
        m_projectSource = ProjectSource.Scratch;
        changesList.Add("Project file upgraded for web interaction, however this project will not be able to be synced with the web.");
        ConvertFlagNotes(currentUser);
        changesList.Add("Updated Project to hold 'Notes'");
      }

      //3.0.6 changes adding NCDOT column swapping bool
      if (IsVersionALessThanVersionB(m_MerlinVersion, "3.0.6"))
      {
        m_NCDOTColSwappingEnabled = false;
        changesList.Add("Project file upgraded to be compatible with the NCDOT column swapping feature.");
      }

      //4.0.1 changes adding TCC rules and expanded banks
      if (IsVersionALessThanVersionB(m_MerlinVersion, "4.0.1"))
      {
        m_TCCDataFileRules = false;
        //Add the extra banks. Note: Hard-coded 14 because that's how many are currently allowed. If we ever expand,
        //we would need to do this agin but for projects less than a different version number.
        while (m_Banks.Count < 14)
        {
          m_Banks.Add("NOT USED");
        }
        while (m_PedBanks.Count < 14)
        {
          m_PedBanks.Add(PedColumnDataType.NA);
        }
        //reinitialize bank dictionary
        m_BankDictionary = SetupBankDictionary();

        changesList.Add("Project file upgraded to support up to 14 banks.");
      }

      //4.0.3 Changes the way data files are stored in the count 
      if (IsVersionALessThanVersionB(m_MerlinVersion, "4.0.3"))
      {
        UpdatedDataFilesToNewDataStructure();
        changesList.Add("Files associated with counts now track the data from which they are currently the source.");
      }

      if (IsVersionALessThanVersionB(m_MerlinVersion, "4.0.6"))
      {
        CheckForStandardDataFiles();
        changesList.Add("Ensure 'Manual Entry' is an option for data file association.");
      }

      if (IsVersionALessThanVersionB(m_MerlinVersion, "4.1.1"))
      {
        AddTCCPreferences();
        changesList.Add("Changes to Merlin Threshold Settings. Please verify the new TCC flag thresholds in the Settings Window.");
      }

      if (IsVersionALessThanVersionB(m_MerlinVersion, "4.3.8"))
      {
        m_ColumnHeaders = SetupColumnHeaders();
        ConvertColumnHeaders();
        changesList.Add("Ensured RTOR Ped Suffixes did not include extra space.");
      }

      //5.0.3 changes adding tubes to project
      //5.0.5 changes referring to location numbers when serializing neighbors for balancing were their position in the project's list of intersections to now the location's unique ID
      if (IsVersionALessThanVersionB(m_MerlinVersion, "5.0.5"))
      {
        m_Tubes = new List<TubeSite>();
        foreach (Intersection intersection in this.m_Intersections)
        {
          intersection.UpdateArrayNeighborReferenceID(this);
        }
        changesList.Add("Project file upgraded to be compatible with tubes.");
        //Note: not necessary to set m_TubeOrderNumber because it is designed to be null if there isn't a separate tube order (which would obviously be the case for legacy projects since they wouldn't have tubes)
      }

      //5.0.1 changes adding interval length to TubeCount
      if (IsVersionALessThanVersionB(m_MerlinVersion, "5.1.1"))
      {
        foreach(TubeCount tc in GetAllTubeCounts())
        {
          tc.m_IntervalSize = IntervalLength.Fifteen;
        }
        if(!IsVersionALessThanVersionB(m_MerlinVersion, "5.0.5"))
        {
          changesList.Add("Project file upgraded to be compatible with tubes.");
        }
      }

      // 5.2.0 changes adding user name and password to settings file.
      if (IsVersionALessThanVersionB(m_MerlinVersion, "5.2.0"))
      {
        m_Prefs.m_userName = "";
        m_Prefs.m_password = new NetworkCredential("", "").SecurePassword;
      }

      m_MerlinVersion = version;
      return changesList;
    }

    public bool MixingRegularAndFHWA()
    {
      int regularCount = 0;
      int fhwaCount = 0;
      foreach (string bank in m_Banks.Where(x => x != "NOT USED" && x != BankVehicleTypes.Bicycles.ToString()))
      {
        if (bank.StartsWith("FHWA"))
        {
          fhwaCount++;
        }
        else
        {
          regularCount++;
        }
      }
      if (regularCount > 0 && fhwaCount > 0)
      {
        return true;
      }
      return false;
    }

    #endregion

    #region Version Control Helper Functions

    private bool IsVersionALessThanVersionB(string versionA, string versionB)
    {
      string[] A = versionA.Split('.');
      string[] B = versionB.Split('.');
      int[] a = { int.Parse(A[0]), int.Parse(A[1]), int.Parse(A[2]) };
      int[] b = { int.Parse(B[0]), int.Parse(B[1]), int.Parse(B[2]) };

      for (int i = 0; i < a.Length; i++)
      {
        if (a[i] < b[i])
        {
          return true;
        }
        if (a[i] > b[i])
        {
          return false;
        }
      }

      //Alternatively:
      //if(a[0] < b[0] || (a[0] == b[0] && (a[1] < b[1] || (a[1] == b[1] && a[2] < b[2]))))
      //{
      //  return true;
      //}

      return false;
    }

    private void PopulatePedBanks()
    {
      m_PedBanks.Clear();
      m_PedBanks.Add(PedColumnDataType.Pedestrian);

      if (m_IsRTOR)
      {
        m_PedBanks.Add(PedColumnDataType.RTOR);
      }
      else
      {
        m_PedBanks.Add(PedColumnDataType.NA);
      }

      if (m_IsUTurn)
      {
        m_PedBanks.Add(PedColumnDataType.UTurn);
      }
      else
      {
        m_PedBanks.Add(PedColumnDataType.NA);
      }
      m_PedBanks.Add(PedColumnDataType.NA);
      m_PedBanks.Add(PedColumnDataType.NA);
      m_PedBanks.Add(PedColumnDataType.NA);
    }

    private void ConvertColumnHeaders()
    {
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          for (int i = 0; i < count.m_Data.Tables.Count; i++)
          {
            DataTable table = count.m_Data.Tables[i];
            for (int j = 1; j < table.Columns.Count; j++)
            {
              table.Columns[j].ColumnName = m_ColumnHeaders[i][j];
            }
          }
        }
      }
    }

    private void ConvertCountTimePeriodStorage()
    {
      foreach (Count count in GetAllTmcCounts())
      {
        count.m_TimePeriodIndex = (int)count.m_TimePeriod;
      }
    }

    private void ConvertSettings()
    {
      m_Prefs.m_TimePeriodLow = 0.8;
      m_Prefs.m_TimePeriodHigh = 1.25;
      m_Prefs.m_TimePeriodMin = 200;
      m_Prefs.m_CrossPeakDiff = .25;
      m_Prefs.m_BalancingDiff = .02;

    }

    private void AssignIDsToLocationsAndTimePeriods()
    {
      m_TimePeriodIDs = new List<int>();
      for (int i = 0; i < m_TimePeriods.Count; i++)
      {
        m_TimePeriodIDs.Add(i);
      }
      for (int i = 0; i < m_Intersections.Count; i++)
      {
        m_Intersections[i].Id = i;
        m_Intersections[i].Index = i;
        m_Intersections[i].Latitude = "";
        m_Intersections[i].Longitude = "";
      }
    }

    private void ConvertFlagNotes(string currentUser)
    {
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          foreach (Flag flag in count.m_Flags)
          {
            if (!String.IsNullOrEmpty(flag.m_UserNote))
            {
              if (flag.m_Note == null)
              {
                Note newNote = new Note(flag.m_Key, NoteType.CountLevel_Flag, flag.m_UserNote, currentUser);
                flag.m_ParentCount.m_Notes.Add(newNote);
                flag.m_Note = newNote;
              }
              else
              {
                flag.m_Note.Edit(flag.m_UserNote, currentUser);
              }
            }
          }
        }
      }
    }

    private void UpdatedDataFilesToNewDataStructure()
    {
      foreach (var intersection in m_Intersections)
      {
        foreach (var count in intersection.m_Counts)
        {
          foreach (var file in count.m_DataFiles)
          {
            DataFile dataFile = new DataFile(file);
            count.AddFileNameToFileList(dataFile);
          }
          count.AddManualEntryToAssociatedDataFiles();
          count.m_DataFiles.Clear();
          count.InvertDataFileToCellMapping();
        }
      }
    }

    public void CheckForStandardDataFiles()
    {
      foreach (var intersection in m_Intersections)
      {
        foreach (var count in intersection.m_Counts)
        {
          if (!count.HasDataFile("Manual Entry"))
          {
            count.AddManualEntryToAssociatedDataFiles();
          }
        }
      }
    }

    private void AddTCCPreferences()
    {
      m_Prefs.m_fhwa1 = 5.0F;
      m_Prefs.m_fhwa2 = 5.0F;
      m_Prefs.m_fhwa3 = 5.0F;
      m_Prefs.m_fhwa4 = 5.0F;
      m_Prefs.m_fhwa5 = 5.0F;
      m_Prefs.m_fhwa6 = 5.0F;
      m_Prefs.m_fhwa7 = 5.0F;
      m_Prefs.m_fhwa8 = 5.0F;
      m_Prefs.m_fhwa9 = 5.0F;
      m_Prefs.m_fhwa10 = 5.0F;
      m_Prefs.m_fhwa11 = 5.0F;
      m_Prefs.m_fhwa12 = 5.0F;
      m_Prefs.m_fhwa13 = 5.0F;
      m_Prefs.m_fhwa6_13 = 5.0F;
    }


    #endregion

    #region Accessors

    public List<Count> GetAllTmcCounts()
    {
      List<Count> allCounts = new List<Count>();

      foreach (Intersection intersection in m_Intersections)
      {
        allCounts.AddRange(intersection.m_Counts);
      }
      return allCounts;
    }

    public List<TubeCount> GetAllTubeCounts()
    {
      List<TubeCount> allTubeCounts = new List<TubeCount>();

      foreach (TubeSite ts in m_Tubes)
      {
        allTubeCounts.AddRange(ts.m_TubeCounts);
      }
      return allTubeCounts;
    }

    public List<Location> GetAllProjectLocations()
    {
      List<Location> locs = new List<Location>();
      locs.AddRange(this.m_Intersections);
      locs.AddRange(this.m_Tubes);
      return locs;
    }

    /// <summary>
    /// Get a location in the project.
    /// </summary>
    /// <param name="locationID">Location ID of the desired location.</param>
    /// <returns>Location with the given ID, otherwise 'null' if not found.</returns>
    public Location GetLocation(int locationID)
    {
      return GetAllProjectLocations().FirstOrDefault(x => x.Id == locationID);
    }

    /// <summary>
    /// Returns actual number of banks being used for data. This is useful because TMCProject's list of banks currently contains unused banks.
    /// </summary>
    /// <returns></returns>
    public int GetNumberOfBanksInUse()
    {
      int count = 0;
      for (int i = 0; i < m_Banks.Count; i++)
      {
        if (m_Banks[i] != "NOT USED" || m_PedBanks[i] != PedColumnDataType.NA)
        {
          count++;
        }
      }

      return count;
    }

    public int GetNumberOfCounts()
    {
      int number = 0;
      foreach (Intersection intersection in m_Intersections)
      {
        number += intersection.m_Counts.Count;
      }
      return number;
    }

    private DataState GetProjectDataState()
    {
      bool atLeastOneCountEmpty = false;
      bool allCountsEmpty = true;
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          switch (count.m_DataState)
          {
            case DataState.Empty:
              atLeastOneCountEmpty = true;
              break;
            case DataState.Complete:
              allCountsEmpty = false;
              break;
            case DataState.Partial:
              allCountsEmpty = false;
              break;
          }
        }
      }

      if (allCountsEmpty)
      {
        return DataState.Empty;
      }
      if (!allCountsEmpty && atLeastOneCountEmpty)
      {
        return DataState.Partial;
      }
      return DataState.Complete;
    }

    public bool IsRTOR()
    {
      if (m_PedBanks.Contains(PedColumnDataType.RTOR)
        || m_PedBanks.Contains(PedColumnDataType.PassengerRTOR)
        || m_PedBanks.Contains(PedColumnDataType.HeavyRTOR))
      {
        return true;
      }
      return false;
    }

    public bool IsUTurn()
    {
      if (m_PedBanks.Contains(PedColumnDataType.UTurn)
        || m_PedBanks.Contains(PedColumnDataType.PassengerUTurn)
        || m_PedBanks.Contains(PedColumnDataType.HeavyUTurn))
      {
        return true;
      }
      return false;
    }

    public List<Flag> GetAllFlags()
    {
      List<Flag> output = new List<Flag>();
      //int i = 1;
      //Count thisCount = null;
      //string id;

      //Get all the flags by type
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          foreach (Flag flag in count.m_Flags)
          {
            foreach (FlagType type in Enum.GetValues(typeof(FlagType)))
            {
              if (flag.m_Type == type)
              {
                output.Add(flag);
              }
            }
          }
        }
      }
      //Sort the flags by site code



      ////Set thisCount to be count with first site code
      //foreach (Intersection intersection in m_Intersections)
      //{
      //  foreach (Count count in intersection.m_Counts)
      //  {
      //    if (count.m_Id.EndsWith("01"))
      //    {
      //      thisCount = count;
      //      break;
      //    }
      //  }
      //}
      ////Loop until the site code is not found
      //while (thisCount != null)
      //{
      //  //Gather flags
      //  foreach (Flag flag in thisCount.m_Flags)
      //  {
      //    foreach (FlagType type in Enum.GetValues(typeof(FlagType)))
      //    {
      //      if (flag.m_Type == type)
      //      {
      //        output.Add(flag);
      //      }
      //    }
      //  }
      //  //Increase site code and reset site code
      //  i++;
      //  if (i < 10)
      //  {
      //    id = "0" + i;
      //  }
      //  else
      //  {
      //    id = i.ToString();
      //  }
      //  thisCount = null;
      //  //Look for incremented site code
      //  foreach (Intersection intersection in m_Intersections)
      //  {
      //    foreach (Count count in intersection.m_Counts)
      //    {
      //      if (count.m_Id.EndsWith(id))
      //      {
      //        thisCount = count;
      //        break;
      //      }
      //    }
      //  }
      //}
      m_FlagsGenerated = true;
      return output;
    }

    public List<DateTime> GetTubeDatesInProject()
    {
      List<DateTime> tubeDates = new List<DateTime>();
      foreach (TubeSite ts in m_Tubes)
      {
        foreach (DateTime date in ts.GetTubeDates())
        {
          if (!tubeDates.Contains(date))
          {
            tubeDates.Add(date);
          }
        }
      }
      return tubeDates;
    }

    public List<DateTime> GetFilmDatesInProject()
    {
      List<DateTime> filmDates = new List<DateTime>();
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          if (!filmDates.Exists(x => x.Date == count.m_FilmDate.Date))
          {
            filmDates.Add(count.m_FilmDate.Date);
          }
        }
      }
      filmDates.OrderBy(x => x.Date);
      return filmDates;
    }

    public List<DateTime> GetFilmDatesOfTimePeriod(int tpIndex)
    {
      List<DateTime> filmDates = new List<DateTime>();
      foreach (Intersection intersection in m_Intersections)
      {
        //iterate over every count in the time period specified by tpIndex
        foreach (Count count in intersection.m_Counts.Where(x => x.m_TimePeriodIndex == tpIndex))
        {
          if (!filmDates.Exists(x => x.Date == count.m_FilmDate.Date))
          {
            //add the count's film date to the list if not already in the list
            filmDates.Add(count.m_FilmDate.Date);
          }
        }
      }
      filmDates.OrderBy(x => x.Date);
      return filmDates;
    }

    public List<Count> GetCountsInTimePeriod(int tpIndex)
    {
      List<Count> results = new List<Count>();
      foreach (Intersection intersection in m_Intersections)
      {
        if (intersection.m_Counts.Exists(x => x.m_TimePeriodIndex == tpIndex))
        {
          results.Add(intersection.m_Counts.Find(x => x.m_TimePeriodIndex == tpIndex));
        }
      }
      return results;
    }

    /// <summary>
    /// Returns first interval of the system wide peak time span passed in. Returns null if request is invalid.
    /// </summary>
    /// <param name="tpIndex">The index of the time period to look for the peak in</param>
    /// <param name="num5MinIntervalsToSpan">The span of the peak to find, expressed as a number of 5 minute intervals</param>
    /// <param name="dataBanksToIgnore">A list of banks for which to ignore non-ped column data</param>
    /// <param name="pedColBanksToIgnore">A list of banks for which to ignore ped column data</param>
    /// <param name="date">Film date of the counts to base the peak calculation on. Counts not filmed on this date will not be included in the calculation.</param>
    /// <returns></returns>
    public string GetSystemWidePeak(int tpIndex, int num5MinIntervalsToSpan, List<int> dataBanksToIgnore, List<int> pedColBanksToIgnore, DateTime date)
    {
      List<Count> counts = GetCountsInTimePeriod(tpIndex).Where(x => x.m_FilmDate.Date == date.Date).ToList();  //counts to query
      List<MerlinKeyValuePair<string, int>> intervalTotals = new List<MerlinKeyValuePair<string, int>>(); //interval totals (sum from all the involved counts)
      //List<MerlinKeyValuePair<string, int>> spanTotals = new List<MerlinKeyValuePair<string, int>>(); //sum of each possible total of given time span
      int tp5MinIntervals;  //num 5 minute intervals in the time period (will account for possibility of non-5 minute intervals)
      int peakTotal = 0;
      string peakSpanStart = null;
      int currentHourTotal = 0;

      foreach (Count count in counts)
      {
        count.SetPrimaryKeyToIntervalColumn();
      }

      if (counts.Count == 0)
      {
        //no counts in time period (although this should never be able to happen)
        return null;
      }

      //Normalize number of intervals in the time period to 5 minutes, if necessary
      tp5MinIntervals = counts[0].m_NumIntervals;
      if (m_IntervalLength == IntervalLength.Fifteen)
      {
        tp5MinIntervals *= 3;
      }
      else if (m_IntervalLength == IntervalLength.Thirty)
      {
        tp5MinIntervals *= 6;
      }
      else if (m_IntervalLength == IntervalLength.Sixty)
      {
        tp5MinIntervals *= 12;
      }

      if (tp5MinIntervals < num5MinIntervalsToSpan)
      {
        //Peak time span requested is larger than the time period
        return null;
      }
      if (tp5MinIntervals == num5MinIntervalsToSpan)
      {
        //The time period is exactly as long as the span of time passed in for the peak, the peak must begin at the start of the time period
        return counts[0].m_StartTime;
      }

      //populates list of interval totals
      for (int i = 0; i < tp5MinIntervals; i++)
      {
        intervalTotals.Add(new MerlinKeyValuePair<string, int>(counts[0].m_Data.Tables[0].Rows[i][0].ToString(), 0));

        foreach (Count count in counts)
        {
          intervalTotals[i].Value += count.GetIntervalTotal(intervalTotals[i].Key, dataBanksToIgnore, pedColBanksToIgnore);
        }
      }

      //now have system wide interval totals, now find highest span total!
      int thisIterSumDiff;
      for (int i = 0; i < tp5MinIntervals - num5MinIntervalsToSpan + 1; i++)
      {
        //in first iteration, get first span total
        if (i == 0)
        {
          for (int j = 0; j < num5MinIntervalsToSpan; j++)
          {
            peakTotal += intervalTotals[j].Value;
          }
          peakSpanStart = intervalTotals[i].Key;
          currentHourTotal = peakTotal;
        }
        //in remaining iterations, find the max span total to get the peak
        else
        {
          thisIterSumDiff = intervalTotals[i + num5MinIntervalsToSpan - 1].Value - intervalTotals[i - 1].Value;
          currentHourTotal += thisIterSumDiff;
          if (currentHourTotal > peakTotal)
          {
            //updates the peak if this span is larger than current max using new interval minus first interval that was just removed to determine
            peakTotal = currentHourTotal;
            peakSpanStart = intervalTotals[i].Key;
          }
        }
      }
      return peakSpanStart;
    }

    public bool AnyCountsHaveSameTimePeriodAndDate()
    {
      foreach (string tp in m_TimePeriodLabels)
      {
        if (tp != null)
        {
          List<Count> countsInTP = GetCountsInTimePeriod(m_TimePeriodLabels.IndexOf(tp));
          IEnumerable<DateTime> filmDates = countsInTP.Select(x => x.m_FilmDate);
          if (countsInTP.Count > 1 && filmDates.Distinct().Count() < countsInTP.Count)
          {
            return true;
          }
        }
      }
      return false;
    }

    public string GetCombinedBankNames(int index)
    {
      return m_Banks[index] + " & " + m_PedBanks[index].ToString();
    }

    #endregion

    #region Static Methods



    #endregion

    private Dictionary<int, List<string>> SetupColumnHeaders()
    {
      Dictionary<int, List<string>> returnSet = new Dictionary<int, List<string>>();
      for (int i = 0; i < this.m_Banks.Count; i++)
      {
        string suffix = DeterminePedHeaderSuffix(i);
        returnSet.Add(i, new List<string>{ "Time", "SBR", "SBT", "SBL", "SB" + suffix, 
          "WBR", "WBT", "WBL", "WB" + suffix, "NBR", "NBT", "NBL", "NB" + suffix, "EBR", "EBT", "EBL", "EB" + suffix });
      }
      return returnSet;
    }

    public string DeterminePedHeaderSuffix(int bank)
    {
      string suffix = "";
      PedColumnDataType thisPed = m_PedBanks[bank];
      switch (thisPed)
      {
        case PedColumnDataType.Pedestrian:
          suffix = "P";
          break;
        case PedColumnDataType.RTOR:
          if (m_IsRTOR)
          {
            suffix = "RT";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.UTurn:
          if (m_IsUTurn)
          {
            suffix = "U";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.PassengerRTOR:
          if (m_IsRTOR)
          {
            suffix = "RP";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.HeavyRTOR:
          if (m_IsRTOR)
          {
            suffix = "RH";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.PassengerUTurn:
          if (m_IsUTurn)
          {
            suffix = "UP";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.HeavyUTurn:
          if (m_IsUTurn)
          {
            suffix = "UH";

          }
          else
          {
            suffix = "-";
          }
          break;
        case PedColumnDataType.NA:
          suffix = "-";
          break;

      }
      return suffix;
    }

    public Dictionary<int, string> SetupBankDictionary()
    {
      Dictionary<int, string> dict = new Dictionary<int, string>();
      for (int i = 0; i < m_Banks.Count; i++)
      {
        dict.Add(i, m_Banks[i]);
      }

      return dict;
    }

    public void PopulateDataCellMaps()
    {
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          count.InvertDataFileToCellMapping();
        }
      }
    }

    public List<int> GetBanksConsideredHeavies(bool includeBuses = true)
    {
      List<int> bankList = new List<int>();
      //This should be in Types.cs, or even better vehicle types should be their own class
      List<string> heavyBanks = new List<string>()
      {
        BankVehicleTypes.Heavies.ToString(),
        BankVehicleTypes.LightHeavies.ToString(),
        BankVehicleTypes.MediumHeavies.ToString(),
        BankVehicleTypes.HeavyHeavies.ToString(),
        BankVehicleTypes.FHWA4_5_6_7.ToString(),
        BankVehicleTypes.FHWA8_9_10.ToString(),
        BankVehicleTypes.FHWA11_12_13.ToString(),
        BankVehicleTypes.SingleUnitHeavies.ToString(),
        BankVehicleTypes.MultiUnitHeavies.ToString(),
        BankVehicleTypes.FHWA5.ToString(),
        BankVehicleTypes.FHWA6.ToString(),
        BankVehicleTypes.FHWA7.ToString(),
        BankVehicleTypes.FHWA8.ToString(),
        BankVehicleTypes.FHWA9.ToString(),
        BankVehicleTypes.FHWA10.ToString(),
        BankVehicleTypes.FHWA11.ToString(),
        BankVehicleTypes.FHWA12.ToString(),
        BankVehicleTypes.FHWA13.ToString(),
        BankVehicleTypes.TEX_UTurnLaneHeavy.ToString(),
        BankVehicleTypes.FHWA6through13.ToString(),
      };

      if (includeBuses)
      {
        heavyBanks.Add(BankVehicleTypes.Buses.ToString());
        heavyBanks.Add(BankVehicleTypes.FHWA4.ToString());
      }

      for (int i = 0; i < m_Banks.Count; i++)
      {
        if (heavyBanks.Contains(m_Banks[i]))
        {
          bankList.Add(i);
        }
      }
      return bankList;
    }

    public void ReloadNonSerializableMembers()
    {
      foreach (Location location in this.GetAllProjectLocations())
      {
        if (location is Intersection)
        {
          foreach (var count in ((Intersection)location).m_Counts)
          {
            foreach (var flag in count.m_Flags)
            {
              flag.m_ParentCount = count;
              if (flag.m_Note != null)
              {
                foreach (Note note in count.m_Notes)
                {
                  if (note.Equals(flag.m_Note))
                  {
                    flag.m_Note = note;
                  }
                }
              }
            }
            count.m_ParentIntersection = ((Intersection)location);
            count.InvertDataFileToCellMapping();
          }
        }
        else if(location is TubeSite)
        {
          foreach(TubeCount tc in ((TubeSite)location).m_TubeCounts)
          {
            tc.m_ParentTubeSite = (TubeSite)location;
          }
        }

        location.m_ParentProject = this;
        location.SetNeighborsFromXml();
      }

      m_BankDictionary = SetupBankDictionary();
      m_HeavyBanks = GetBanksConsideredHeavies();
      m_ColumnHeaders = SetupColumnHeaders();
    }

    public void ProcessProjectFlags(Dictionary<string, bool> dictionaryOfTestToggles)
    {
      List<Flag> tempFlags = new List<Flag>();

      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          foreach (Flag flag in count.m_Flags)
          {
            if (flag.m_IsAcceptable)
            {
              tempFlags.Add(flag);
            }
          }
          count.m_Flags.Clear();

          count.RunTests(dictionaryOfTestToggles);
        }
      }
      //Don't go into these blocks unless at least one of the flags is one.
      if (dictionaryOfTestToggles[FlagType.SuspiciousTrafficFlow.ToString()]
        || dictionaryOfTestToggles[FlagType.SuspiciousMovement.ToString()])
      {
        SuspiciousFlowFlagTests(dictionaryOfTestToggles);
      }

      if (m_TCCDataFileRules &&
          (dictionaryOfTestToggles[FlagType.CountClassificationDiscrepancy.ToString()]
        || dictionaryOfTestToggles[FlagType.FileDataClassificationDiscrepancy.ToString()]
        || dictionaryOfTestToggles[FlagType.HourClassificationDiscrepancy.ToString()]))
      {
        TCC_ClassPercentageTests(dictionaryOfTestToggles);
      }
      ProcessFlagChange(tempFlags);
    }

    private void SuspiciousFlowFlagTests(Dictionary<string, bool> dictionaryOfTestToggles)
    {
      List<int> amTimePeriods = new List<int>();
      List<int> pmTimePeriods = new List<int>();
      // Determine if Project Qualifies (AM and PM time periods)
      for (int i = 0; i < m_TimePeriods.Count; i++)
      {
        if (m_TimePeriods[i] == "NOT USED")
        {
          continue;
        }
        string startTime = m_TimePeriods[i].Split('-')[0];
        string endTime = m_TimePeriods[i].Split('-')[1];
        DateTime start;
        DateTime end;
        DateTime.TryParse(startTime, out start);
        DateTime.TryParse(endTime, out end);

        if (start.TimeOfDay >= DateTime.Today.AddHours(5).TimeOfDay
          && end.TimeOfDay <= DateTime.Today.AddHours(10).TimeOfDay)
        {
          amTimePeriods.Add(i);
          continue;
        }
        if (start.TimeOfDay >= DateTime.Today.AddHours(15).TimeOfDay
          && end.TimeOfDay <= DateTime.Today.AddHours(20).TimeOfDay)
        {
          pmTimePeriods.Add(i);
          continue;
        }
      }

      if (amTimePeriods.Count < 1 || pmTimePeriods.Count < 1)
      {
        return;
      }
      // 
      foreach (Intersection intersection in m_Intersections)
      {
        if (dictionaryOfTestToggles[FlagType.SuspiciousTrafficFlow.ToString()])
        {
          intersection.CrossPeakSuspiciousFlowTest(amTimePeriods, pmTimePeriods);
        }
        if (dictionaryOfTestToggles[FlagType.SuspiciousMovement.ToString()])
        {
          intersection.MovementSuspiciousFlowTest();
        }
      }

    }

    private void TCC_ClassPercentageTests(Dictionary<string, bool> dictionaryOfTestToggles)
    {
      Dictionary<string, double> projectClassPercentages = new Dictionary<string, double>();
      Dictionary<string, double> classSums = new Dictionary<string, double>();
      Dictionary<string, Dictionary<string, double>> countsDataPercentages = new Dictionary<string, Dictionary<string, double>>();
      int totalProjectSum = 0;
      foreach (var bank in m_Banks)
      {
        projectClassPercentages.Add(bank, 0);
        classSums.Add(bank, 0);
      }
      foreach (var intersection in m_Intersections)
      {
        foreach (var count in intersection.m_Counts)
        {
          // This will be used for the other two flags.
          Dictionary<string, double> countClassSums = new Dictionary<string, double>();
          Dictionary<string, double> countClassPercentages = new Dictionary<string, double>();
          int countSum = 0;
          foreach (var bank in m_Banks)
          {
            countClassSums.Add(bank, 0);
            countClassPercentages.Add(bank, 0);
          }
          for (int t = 0; t < count.m_Data.Tables.Count; t++)
          {
            string thisBank = m_BankDictionary[t];
            foreach (DataRow row in count.m_Data.Tables[t].Rows)
            {
              for (int i = 1; i < row.ItemArray.Length; i++)
              {
                int cell = int.Parse(row.ItemArray[i].ToString());
                totalProjectSum += cell;
                classSums[thisBank] += cell;
                countSum += cell;
                countClassSums[thisBank] += cell;
              }
            }
          }

          foreach (var classSum in countClassSums)
          {
            countClassPercentages[classSum.Key] = countSum > 0 ? (classSum.Value / countSum) * 100 : 0;
          }
          countsDataPercentages.Add(count.m_Id, countClassPercentages);

          //Don't run the tests if the DataState is Empty;
          if (count.m_DataState != DataState.Empty)
          {
            if (dictionaryOfTestToggles[FlagType.FileDataClassificationDiscrepancy.ToString()])
            {
              count.CheckClassPercentageByFile(countClassPercentages);
            }

            if (dictionaryOfTestToggles[FlagType.HourClassificationDiscrepancy.ToString()])
            {
              count.CheckClassPercentageByHour(countClassPercentages);
            }
          }
        }
      }
      //Only run the test if the test type is toggled to on.
      if (dictionaryOfTestToggles[FlagType.CountClassificationDiscrepancy.ToString()])
      {
        Dictionary<string, float> thresholdMap = new Dictionary<string, float>
        {
          {"FHWA1", m_Prefs.m_fhwa1}, 
          {"FHWA2", m_Prefs.m_fhwa2}, 
          {"FHWA3", m_Prefs.m_fhwa3}, 
          {"FHWA4", m_Prefs.m_fhwa4}, 
          {"FHWA5", m_Prefs.m_fhwa5}, 
          {"FHWA6", m_Prefs.m_fhwa6}, 
          {"FHWA7", m_Prefs.m_fhwa7}, 
          {"FHWA8", m_Prefs.m_fhwa8}, 
          {"FHWA9", m_Prefs.m_fhwa9}, 
          {"FHWA10", m_Prefs.m_fhwa10}, 
          {"FHWA11", m_Prefs.m_fhwa11}, 
          {"FHWA12", m_Prefs.m_fhwa12}, 
          {"FHWA13", m_Prefs.m_fhwa13}, 
          {"FHWA6through13", m_Prefs.m_fhwa6_13},
        };

        foreach (var classSum in classSums)
        {
          projectClassPercentages[classSum.Key] = totalProjectSum > 0 ? ((double)classSum.Value / (double)totalProjectSum) * 100 : 0;
        }

        //Run the project level test here
        foreach (var intersection in m_Intersections)
        {
          foreach (var count in intersection.m_Counts)
          {
            //Don't run the test if the DataState is Empty;
            if (count.m_DataState != DataState.Empty)
            {
              foreach (var dataSet in countsDataPercentages[count.m_Id])
              {
                //TODO: Update to use a threshold instead of 10%
                float thresholdToUse = thresholdMap.ContainsKey(dataSet.Key) ? thresholdMap[dataSet.Key] : 5;
                if (Math.Abs(dataSet.Value - projectClassPercentages[dataSet.Key]) > thresholdToUse)
                {
                  string information = string.Format("{0}  Count: {1}%  Project: {2}%", dataSet.Key, dataSet.Value.ToString("n2"),
                    projectClassPercentages[dataSet.Key].ToString("n2"));
                  if (count.m_Flags.All(f => f.m_Key != (count.m_Id.Substring(6) + FlagType.CountClassificationDiscrepancy + information)))
                  {
                    count.m_Flags.Add(new Flag(FlagType.CountClassificationDiscrepancy, count, dataSet.Key, information));
                  }
                }
              }
            }
          }
        }
      }
    }

    private void ProcessFlagChange(List<Flag> preProcessFlags)
    {
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          for (int i = 0; i < count.m_Flags.Count; i++)
          {
            foreach (Flag acceptableFlag in preProcessFlags)
            {
              if (count.m_Flags[i].m_Key == acceptableFlag.m_Key)
              {
                count.m_Flags[i] = acceptableFlag;
              }
            }
          }
        }
      }
    }

    public void ClearFlags()
    {
      foreach (Intersection intersection in m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          count.m_Flags.Clear();
        }
      }
    }

    public void UpdateBanks(List<string> regularBanks, List<PedColumnDataType> pedBanks)
    {
      m_Banks = regularBanks;
      m_PedBanks = pedBanks;
      m_IsRTOR = IsRTOR();
      m_IsUTurn = IsUTurn();
      m_BankDictionary = SetupBankDictionary();
      m_HeavyBanks = GetBanksConsideredHeavies();
      m_ColumnHeaders = SetupColumnHeaders();

      foreach (Count count in GetAllTmcCounts())
      {
        count.UpdateTables();
      }
    }


  }
}