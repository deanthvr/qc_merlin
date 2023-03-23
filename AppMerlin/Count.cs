using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;
using MessageBox = System.Windows.MessageBox;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using OfficeOpenXml.Drawing;


namespace AppMerlin
{
  public class Count
  {
    public string m_Id;
    public string m_StartTime;
    public string m_EndTime;
    public TimePeriodName m_TimePeriod; //No longer used to store the time period this count belongs to
    public int m_TimePeriodIndex;
    public int m_NumIntervals;
    public bool m_HasDataInExtraInterval;
    public DataState m_DataState;
    public DataSet m_Data;
    public List<Flag> m_Flags;
    public List<Note> m_Notes;
    public List<string> m_DataFiles;
    public List<DataFile> m_AssociatedDataFiles;
    [XmlIgnore]
    public Intersection m_ParentIntersection; //Intersection this count belongs to.
    public DateTime m_FilmDate;                      //(start) date of count
    [XmlIgnore]
    public Dictionary<string, DataFile> m_DataCellToFileMap;


    #region Constants

    public static readonly List<string> possibleStandardMovements = new List<string>()
      {
        "SBR", "SBT", "SBL", "SBP",
        "WBR", "WBT", "WBL", "WBP",
        "NBR", "NBT", "NBL", "NBP",
        "EBR", "EBT", "EBL", "EBP"
      };


    #endregion

    #region Constructors

    //Constructors

    public Count()
    {
      m_Flags = new List<Flag>();
      m_Notes = new List<Note>();
      m_DataFiles = new List<string>();
      m_HasDataInExtraInterval = false;
      m_ParentIntersection = new Intersection();
      m_DataState = DataState.Empty;
      m_AssociatedDataFiles = new List<DataFile>();
      m_DataCellToFileMap = new Dictionary<string, DataFile>();
    }

    //updated constructor to account for new Intersection class
    public Count(string Id, string time, int timePeriodIndex, Intersection parentIntersection, DateTime date, DataState dataState = DataState.Empty)
    {
      m_Id = Id;
      m_StartTime = time.Split('-')[0];
      m_EndTime = time.Split('-')[1];
      m_TimePeriodIndex = timePeriodIndex;
      m_HasDataInExtraInterval = false;
      m_Flags = new List<Flag>();
      m_Notes = new List<Note>();
      m_DataFiles = new List<string>();
      m_AssociatedDataFiles = new List<DataFile>();
      m_DataCellToFileMap = new Dictionary<string, DataFile>();
      m_ParentIntersection = parentIntersection; //Intersection this count belongs to.
      m_FilmDate = date;
      m_DataState = dataState;
      m_NumIntervals = CalculateNumberOfIntervals();
      m_Data = SetupTables();
      SetPrimaryKeyToIntervalColumn();
      AddManualEntryToAssociatedDataFiles();
    }

    public Count(string startTime, string endTime, Intersection parentIntersection)
    {
      m_StartTime = startTime;
      m_EndTime = endTime;
      m_HasDataInExtraInterval = false;
      m_Flags = new List<Flag>();
      m_Notes = new List<Note>();
      m_DataFiles = new List<string>();
      m_AssociatedDataFiles = new List<DataFile>();
      m_DataCellToFileMap = new Dictionary<string, DataFile>();
      m_ParentIntersection = parentIntersection;
      m_NumIntervals = CalculateNumberOfIntervals();
      m_Data = SetupTables();
      SetPrimaryKeyToIntervalColumn();
      AddManualEntryToAssociatedDataFiles();
    }

    public Count CopyLite()
    {
      Count newCount = new Count(m_Id, GetTimePeriod(), m_TimePeriodIndex, m_ParentIntersection, m_FilmDate, m_DataState);

      return newCount;
    }

    public Count Copy()
    {
      Count newCount = new Count(m_Id, GetTimePeriod(), m_TimePeriodIndex, m_ParentIntersection, m_FilmDate, m_DataState);
      newCount.m_Data = m_Data.Copy();
      newCount.m_AssociatedDataFiles = new List<DataFile>(m_AssociatedDataFiles.Select(x => (DataFile)x.Clone()));

      return newCount;
    }

    #endregion Constructors

    #region Accessors

    // Accessor for the Site Code
    public string GetId()
    {
      return m_Id;
    }

    public string GetTimePeriod()
    {
      return m_StartTime + "-" + m_EndTime;
    }

    public List<Flag> GetFlagsByType(FlagType type)
    {
      List<Flag> result = new List<Flag>();
      foreach (Flag flag in m_Flags)
      {
        if (flag.m_Type == type)
        {
          result.Add(flag);
        }
      }

      return result;
    }

    public string GetLocation()
    {
      StringBuilder location = new StringBuilder();

      location.Append(m_ParentIntersection.GetSBNBApproach());
      location.Append(" & ");
      location.Append(m_ParentIntersection.GetWBEBApproach());

      return location.ToString();
    }

    public int GetIntervalLength()
    {
      return m_ParentIntersection.m_ParentProject.m_IntervalLength != 0 ? (int)m_ParentIntersection.m_ParentProject.m_IntervalLength : 5;
    }
    
    /// <summary>
    /// Returns the user-defined time period label for this count
    /// </summary>
    /// <returns></returns>
    public string GetTimePeriodLabel()
    {
      return m_ParentIntersection.m_ParentProject.m_TimePeriodLabels.ElementAt(m_TimePeriodIndex);
    }

    /// <summary>
    /// Returns balancing total for this count.
    /// </summary>
    /// <param name="direction">For example, "NBEntering"</param>
    /// <param name="banks">Bank integers to include in balancing total</param>
    /// <param name="uTurnBanks">Which banks contain U-Turn data in the peds column and should be included in the balancing total</param>
    /// <param name="RTORBanks">Which banks contain RTOR data in the peds column and should be included in the balancing total</param>
    /// <param name="startingInterval">First interval</param>
    /// <param name="numberOfIntervals">Number of intervals</param>
    /// <returns></returns>
    public int GetBalancingSumByDirection(string direction, int[] banks, int[] uTurnBanks, int[] RTORBanks, string startingInterval, int numberOfIntervals)
    {
      int directionTotal = 0;
      string uTurnToInclude;
      string RTORToInclude;
      DateTime intersectionStartTime = DateTime.Parse(m_StartTime);
      DateTime intersectionEndTime = DateTime.Parse(m_EndTime);
      DateTime balancingStartTime = DateTime.Parse(startingInterval);
      DateTime balancingEndTime = DateTime.Parse(startingInterval).AddMinutes(numberOfIntervals * 5);

      //Just make an array of the interval times we are using for balancing.
      List<string> timesToAdd = new List<string>();
      for (int i = 0; i < numberOfIntervals; ++i)
      {
        timesToAdd.Add(startingInterval);
        startingInterval = AddXMinutes(startingInterval, 5);
      }

      //selects list of movements to include based on direction parameter
      List<string> movementsInDirection = new List<string>();
      //TODO: Make this a constant somewhere else
      switch (direction)
      {
        case "NBEntering":
          movementsInDirection.AddRange(new[] { "NBR", "NBT", "NBL" });
          uTurnToInclude = "NB";
          RTORToInclude = "NB";
          break;
        case "SBExiting":
          movementsInDirection.AddRange(new[] { "WBL", "SBT", "EBR" });
          uTurnToInclude = "NB";
          RTORToInclude = "EB";
          break;
        case "WBEntering":
          movementsInDirection.AddRange(new[] { "WBR", "WBT", "WBL" });
          uTurnToInclude = "WB";
          RTORToInclude = "WB";
          break;
        case "EBExiting":
          movementsInDirection.AddRange(new[] { "SBL", "EBT", "NBR" });
          uTurnToInclude = "WB";
          RTORToInclude = "NB";
          break;
        case "SBEntering":
          movementsInDirection.AddRange(new[] { "SBR", "SBT", "SBL" });
          uTurnToInclude = "SB";
          RTORToInclude = "SB";
          break;
        case "NBExiting":
          movementsInDirection.AddRange(new[] { "EBL", "NBT", "WBR" });
          uTurnToInclude = "SB";
          RTORToInclude = "WB";
          break;
        case "EBEntering":
          movementsInDirection.AddRange(new[] { "EBR", "EBT", "EBL" });
          uTurnToInclude = "EB";
          RTORToInclude = "EB";
          break;
        case "WBExiting":
          movementsInDirection.AddRange(new[] { "NBL", "WBT", "SBR" });
          uTurnToInclude = "EB";
          RTORToInclude = "SB";
          break;
        default:
          throw new Exception("Merlin attempted to get balancing sum for an invalid movement.");
      }

      SetPrimaryKeyToIntervalColumn();
      foreach (string movement in movementsInDirection)
      {
        foreach (int bankToInclude in banks)
        {
          foreach (string time in timesToAdd)
          {
            directionTotal +=
              Convert.ToInt32(m_Data.Tables["Bank " + bankToInclude.ToString()].Rows.Find(time)[movement]);
          }
        }
      }
      //}

      //Adds U-Turns to total
      for (int i = 0; i < uTurnBanks.Length; i++)
      {
        foreach (string time in timesToAdd)
        {
          directionTotal += Convert.ToInt32(m_Data.Tables["Bank " + uTurnBanks[i].ToString()].Rows.Find(time)[uTurnToInclude + m_ParentIntersection.m_ParentProject.DeterminePedHeaderSuffix(uTurnBanks[i])]);
        }
      }

      //Adds RTOR to total
      for (int i = 0; i < RTORBanks.Length; i++)
      {
        foreach (string time in timesToAdd)
        {
          directionTotal += Convert.ToInt32(m_Data.Tables["Bank " + RTORBanks[i].ToString()].Rows.Find(time)[RTORToInclude + m_ParentIntersection.m_ParentProject.DeterminePedHeaderSuffix(RTORBanks[i])]);
        }
      }

      return directionTotal;
    }

    public int GetBalancingSumByDirection(string direction)
    {
      int numberOfIntervals = m_NumIntervals;
      string startingInterval = m_StartTime;
      List<int> banks = new List<int>();
      List<int> RTORBanks = new List<int>();
      List<int> uTurnBanks = new List<int>();
      for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_PedBanks.Count; i++)
      {
        if (m_ParentIntersection.m_ParentProject.m_PedBanks[i].ToString().Contains("RTOR"))
        {
          RTORBanks.Add(i);
        }
        else if (m_ParentIntersection.m_ParentProject.m_PedBanks[i].ToString().Contains("UTurn"))
        {
          uTurnBanks.Add(i);
        }
      }
      for (int j = 0; j < m_ParentIntersection.m_ParentProject.m_Banks.Count; j++)
      {
        if (m_ParentIntersection.m_ParentProject.m_Banks[j] != "NOT USED")
        {
          banks.Add(j);
        }
      }

      int directionTotal = 0;
      string uTurnToInclude;
      string RTORToInclude;
      DateTime intersectionStartTime = DateTime.Parse(m_StartTime);
      DateTime intersectionEndTime = DateTime.Parse(m_EndTime);
      DateTime balancingStartTime = DateTime.Parse(startingInterval);
      DateTime balancingEndTime = DateTime.Parse(startingInterval).AddMinutes(numberOfIntervals * 5);

      //Just make an array of the interval times we are using for balancing.
      List<string> timesToAdd = new List<string>();
      for (int i = 0; i < numberOfIntervals; ++i)
      {
        timesToAdd.Add(startingInterval);
        startingInterval = AddXMinutes(startingInterval, 5);
      }

      //selects list of movements to include based on direction parameter
      List<string> movementsInDirection = new List<string>();
      //TODO: Make this a constant somewhere else
      switch (direction)
      {
        case "NBEntering":
          movementsInDirection.AddRange(new[] { "NBR", "NBT", "NBL" });
          uTurnToInclude = "NB";
          RTORToInclude = "NB";
          break;
        case "SBExiting":
          movementsInDirection.AddRange(new[] { "WBL", "SBT", "EBR" });
          uTurnToInclude = "NB";
          RTORToInclude = "EB";
          break;
        case "WBEntering":
          movementsInDirection.AddRange(new[] { "WBR", "WBT", "WBL" });
          uTurnToInclude = "WB";
          RTORToInclude = "WB";
          break;
        case "EBExiting":
          movementsInDirection.AddRange(new[] { "SBL", "EBT", "NBR" });
          uTurnToInclude = "WB";
          RTORToInclude = "NB";
          break;
        case "SBEntering":
          movementsInDirection.AddRange(new[] { "SBR", "SBT", "SBL" });
          uTurnToInclude = "SB";
          RTORToInclude = "SB";
          break;
        case "NBExiting":
          movementsInDirection.AddRange(new[] { "EBL", "NBT", "WBR" });
          uTurnToInclude = "SB";
          RTORToInclude = "WB";
          break;
        case "EBEntering":
          movementsInDirection.AddRange(new[] { "EBR", "EBT", "EBL" });
          uTurnToInclude = "EB";
          RTORToInclude = "EB";
          break;
        case "WBExiting":
          movementsInDirection.AddRange(new[] { "NBL", "WBT", "SBR" });
          uTurnToInclude = "EB";
          RTORToInclude = "SB";
          break;
        default:
          return -1;
      }

      if ((balancingStartTime < intersectionStartTime) || (balancingEndTime > intersectionEndTime))
      {
        return -1;
      }
      else  //adds specified direction, banks, and intervals to total
      {
        SetPrimaryKeyToIntervalColumn();
        foreach (string movement in movementsInDirection)
        {
          foreach (int bankToInclude in banks)
          {
            foreach (string time in timesToAdd)
            {
              directionTotal +=
                Convert.ToInt32(m_Data.Tables["Bank " + bankToInclude.ToString()].Rows.Find(time)[movement]);
            }
          }
        }
      }

      //Adds U-Turns to total
      for (int i = 0; i < uTurnBanks.Count; i++)
      {
        foreach (string time in timesToAdd)
        {
          directionTotal += Convert.ToInt32(m_Data.Tables["Bank " + uTurnBanks[i].ToString()].Rows.Find(time)[uTurnToInclude + m_ParentIntersection.m_ParentProject.DeterminePedHeaderSuffix(uTurnBanks[i])]);
        }
      }

      //Adds RTOR to total
      for (int i = 0; i < RTORBanks.Count; i++)
      {
        foreach (string time in timesToAdd)
        {
          directionTotal += Convert.ToInt32(m_Data.Tables["Bank " + RTORBanks[i].ToString()].Rows.Find(time)[RTORToInclude + m_ParentIntersection.m_ParentProject.DeterminePedHeaderSuffix(RTORBanks[i])]);
        }
      }

      return directionTotal;
    }

    public int GetIntervalTotal(string interval, List<int> dataBanksToIgnore, List<int> pedColBanksToIgnore)
    {
      int total = 0;

      foreach (DataTable table in m_Data.Tables)
      {
        int currentBank = Int32.Parse(table.TableName.Split(' ')[1]);
        if (m_ParentIntersection.m_ParentProject.m_BankDictionary[currentBank] == "NOT USED")
        {
          //project doesn't have the bank represented by the current table
          continue;
        }
        DataRow intervalRow = table.Rows.Find(interval);

        for (int i = 1; i < table.Columns.Count; i++)
        {
          if (!(IsPedCol(table.Columns[i]) && pedColBanksToIgnore.Contains(currentBank)) && !(!IsPedCol(table.Columns[i]) && dataBanksToIgnore.Contains(currentBank)))
          {
            //adds this cell to total as long as not a vehicle or ped column for which the current bank is in their respective "do not include" lists
            total += Convert.ToInt32(intervalRow[i]);
          }
        }
      }
      return total;
    }

    private bool IsPedCol(DataColumn col)
    {
      if (col.ColumnName.Contains(m_ParentIntersection.m_ParentProject.DeterminePedHeaderSuffix(Int32.Parse(col.Table.TableName.Split(' ')[1]))))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public int GetMovementTotal(int movementColumnIndex)
    {
      int sum = 0;
      foreach (DataTable table in m_Data.Tables)
      {
        foreach (DataRow row in table.Rows)
        {
          sum += int.Parse(row[movementColumnIndex].ToString());
        }
      }
      return sum;
    }

    public int GetMovementTotal(int movementColumnIndex, List<int> banks)
    {
      int sum = 0;
      for (int i = 0; i < m_Data.Tables.Count; i++)
      {
        if (banks.Contains(i))
        {
          foreach (DataRow row in m_Data.Tables[i].Rows)
          {
            sum += int.Parse(row[movementColumnIndex].ToString());
          }
        }
      }
      return sum;
    }

    public int GetCountDataTotal()
    {
      int sum = 0;
      foreach (DataTable table in m_Data.Tables)
      {
        foreach (DataRow row in table.Rows)
        {
          for (int i = 1; i < m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0].Count; i++)
          {
            sum += int.Parse(row[i].ToString());
          }
        }
      }

      return sum;
    }

    #endregion Accessors

    #region Setup Helpers

    public void SetPrimaryKeyToIntervalColumn()
    {
      for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
        {
          continue;
        }
        m_Data.Tables["Bank " + i].PrimaryKey = new DataColumn[] { m_Data.Tables["Bank " + i].Columns["Time"] };
      }

    }

    public int CalculateNumberOfIntervals()
    {
      DateTime begin = DateTime.Parse(m_StartTime);
      DateTime end = DateTime.Parse(m_EndTime);
      if (begin >= end)
      {
        end = end.AddHours(24);
      }
      double span = (end - begin).TotalHours;
      double intervals = (span / GetIntervalLength()) * 60;

      return int.Parse(intervals.ToString());
    }

    private DataSet SetupTables()
    {
      DataSet set = new DataSet();
      for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
        {
          continue;
        }
        set.Tables.Add(CreateTable(i));
      }
      return set;
    }

    private DataTable CreateTable(int bank)
    {
      //DataTable table = new DataTable(m_CountProject.m_Banks[i].ToString());
      DataTable table = new DataTable("Bank " + bank);
      DataColumn c = new DataColumn("Time");

      c.DataType = Type.GetType("System.String");
      c.ReadOnly = true;
      c.Unique = true;
      table.Columns.Add(c);
      Dictionary<int, List<string>> columnHeaders = m_ParentIntersection.m_ParentProject.m_ColumnHeaders;
      int[] columns = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      for (int j = 1; j < columnHeaders[bank].Count(); j++)
      {
        string colHeader = columnHeaders[bank][j];
        DataColumn dc = new DataColumn(colHeader);
        dc.DataType = Type.GetType("System.Int16");
        table.Columns.Add(dc);
      }

      DateTime time2;
      DateTime.TryParse(m_StartTime, out time2);

      for (int k = 0; k < m_NumIntervals; k++)
      {
        DataRow r = table.NewRow();

        r[0] = time2.TimeOfDay.ToString().Remove(5, 3);
        for (int m = 1; m < (columns.Count() + 1); m++)
        {
          r[m] = columns[m - 1];
        }
        table.Rows.Add(r);
        time2 = time2.AddMinutes(5);
      }
      return table;
    }

    public void AddManualEntryToAssociatedDataFiles()
    {
      DataFile manual = new DataFile();
      manual.Name = "Manual Entry";
      manual.ImportDate = DateTime.Now;
      manual.Counter = "Merin User";
      AddFileNameToFileList(manual);
    }

    #endregion Setup Helpers

    /// <summary>
    /// Adds and removes DataTables as necessary to match the banks currently in use by the parent project.
    /// </summary>
    public void UpdateTables()
    {
      for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
        {
          //this bank isn't used in the project
          if (m_Data.Tables.Contains("bank " + i))
          {
            //delete existing DataTable for this bank since the bank is no longer used in the project
            m_Data.Tables.Remove("bank " + i);
          }
        }
        else
        {
          //this bank is used in the project
          if (m_Data.Tables.Contains("bank " + i))
          {
            //table already exists, update column headers because the table could have changed bank types
            for (int j = 1; j < m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i].Count(); j++)
            {
              m_Data.Tables["bank " + i].Columns[j].ColumnName = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][j];
            }
          }
          else
          {
            //count doesn't have a DataTable for this bank, create it
            m_Data.Tables.Add(CreateTable(i));
          }
        }
      }
    }

    #region Data Import Functions And Helpers

    public void ImportCSVData(List<string> lines, string fileName, string approach = null)
    {
      Dictionary<int, List<string>> columnHeaders = m_ParentIntersection.m_ParentProject.m_ColumnHeaders;
      int bank = 0;
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
              break;
            case "Bank number":
              bank = int.Parse(parsedLine[1]);
              if (bank >= m_ParentIntersection.m_ParentProject.GetNumberOfBanksInUse())
              {
                return;
              }

              break;
            default:
              DateTime testTime;
              if (!DateTime.TryParse(parsedLine[0], out testTime))
              {
                break;
              }
              //int sum = 0;
              //for (int s = 1; s < parsedLine.Count; s++)
              //{
              //  sum += int.Parse(parsedLine[s]);
              //}
              //if (sum < 1)
              //{
              //  break;
              //}
              int thisRow = 0;
              string interval = parsedLine[0];
              if (interval.Length != 5)
              {
                interval = "0" + interval;
              }
              foreach (DataRow r in m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == interval)
                {
                  //TODO: The interval was not in the time period.
                  break;
                }
                thisRow++;
              }
              if (thisRow >= m_NumIntervals)
              {
                break;
              }
              for (int i = 0; i < columnHeaders[bank].Count(); i++)
              {
                if (i == 0)
                {
                  continue;
                }
                int tempBank = bank;
                //NCDOT Column Swapping - bank 0 and bank 2 ped columns swap
                if ((bank == 0 || bank == 2) && m_ParentIntersection.m_ParentProject.m_NCDOTColSwappingEnabled && (i % 4) == 0)
                {
                  tempBank = bank == 0 ? 2 : 0;
                }
                m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]] =
                  parsedLine[i];

                string key = m_Data.Tables["Bank " + tempBank].TableName + "," + columnHeaders[tempBank][i] + "," + m_Data.Tables["Bank " + tempBank].Rows[thisRow].ItemArray[0];
                UpdateDataCellAssociation(fileName, key);

              }
              m_DataState = DataState.Partial;
              break;
          }
        }
      }
    }

    public void ImportASCIIData(List<string> lines, string fileName, string approach = null)
    {
      Dictionary<int, List<string>> columnHeaders = m_ParentIntersection.m_ParentProject.m_ColumnHeaders;
      int bank = 0;
      string interval = "";
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Start Date":
            case "Start Time":
            case "Site Code":
            case "Street Name":
              break;
            default:
              DateTime testTime;
              if (!DateTime.TryParse(parsedLine[0], out testTime))
              {
                break;
              }
              if (parsedLine[0] == interval)
              {
                bank++;
              }
              else
              {
                bank = 0;
                interval = parsedLine[0];
              }              
              if (bank >= m_ParentIntersection.m_ParentProject.GetNumberOfBanksInUse())
              {
                break;
              }
              int thisRow = 0;
              string adjustedInterval;
              string[] time = parsedLine[0].Split(' ');
              if (time[1] == "AM")
              {
                adjustedInterval = time[0];
              }
              else
              {
                int hour = int.Parse(time[0].Split(':')[0]);
                hour = hour + 12;
                adjustedInterval = hour + ":" + time[0].Split(':')[1];
              }

              foreach (DataRow r in m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == adjustedInterval)
                {
                  //TODO: The interval was not in the time period.
                  break;
                }
                thisRow++;
              }
              if (thisRow >= m_NumIntervals)
              {
                break;
              }
              for (int i = 1; i < columnHeaders[bank].Count(); i++)
              {
                int tempBank = bank;
                //NCDOT Column Swapping - bank 0 and bank 2 ped columns swap
                if ((bank == 0 || bank == 2) && m_ParentIntersection.m_ParentProject.m_NCDOTColSwappingEnabled && (i % 4) == 0)
                {
                  tempBank = bank == 0 ? 2 : 0;
                }
                m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]] =
                  parsedLine[i];

                string key = m_Data.Tables["Bank " + tempBank].TableName + "," + columnHeaders[tempBank][i] + "," + m_Data.Tables["Bank " + tempBank].Rows[thisRow].ItemArray[0];
                UpdateDataCellAssociation(fileName, key);

              }
              m_DataState = DataState.Partial;
              break;
          }
        }
      }
    }

    public void ImportTCC_CSVData(List<string> lines, string fileName, string approach)
    {
      Dictionary<int, List<string>> columnHeaders = m_ParentIntersection.m_ParentProject.m_ColumnHeaders;
      var approaches = ParseApproach(approach);
      if (approaches.Length == 0)
      {
        return;
      }
      int bank = 0;
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
              break;
            case "Bank number":
              bank = int.Parse(parsedLine[1]);
              //Check to see if we are looking at a bank that we won't use.
              if (bank >= approaches.Length)
              {
                return;
              }

              break;
            default:
              DateTime testTime;
              if (!DateTime.TryParse(parsedLine[0], out testTime))
              {
                break;
              }
              // If sum of interval is 0, don't do anything.  This could be an extra interval, or a time the counter intentionally didn't count.
              //int sum = 0;
              //for (int s = 1; s < parsedLine.Count; s++)
              //{
              //  sum += int.Parse(parsedLine[s]);
              //}
              //if (sum < 1)
              //{
              //  break;
              //}
              int thisRow = 0;
              // If the time is before 10am and after midnight, we need to add the leading 0.
              string interval = parsedLine[0];
              if (interval.Length != 5)
              {
                interval = "0" + interval;
              }

              // Line up the intervals to enter the data in the right spot
              foreach (DataRow r in m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == interval)
                {
                  break;
                }
                thisRow++;
              }
              // Can't go past the number of intervals in the count.
              if (thisRow >= m_NumIntervals)
              {
                break;
              }

              for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count - 1; i++)
              {
                if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED")
                {
                  continue;
                }
                m_Data.Tables[i].Rows[thisRow][approaches[bank]] = parsedLine[i + 1];
                string key = m_Data.Tables[i].TableName + "," + approaches[bank] + "," + m_Data.Tables[i].Rows[thisRow].ItemArray[0];
                UpdateDataCellAssociation(fileName, key);
              }
              if (approaches[bank][2] == 'T' && approach.Length == 2)
              {
                int pedBank = m_ParentIntersection.m_ParentProject.m_PedBanks.IndexOf(PedColumnDataType.Pedestrian);
                if (pedBank >= 0)
                {
                  m_Data.Tables[pedBank].Rows[thisRow][approach + "P"] = parsedLine[m_ParentIntersection.m_ParentProject.m_Banks.Count];
                  string key = m_Data.Tables[pedBank].TableName + "," + approach + "P" + "," + m_Data.Tables[pedBank].Rows[thisRow].ItemArray[0];
                  UpdateDataCellAssociation(fileName, key);
                }
              }
              if (approaches[bank][2] != 'U')
              {
                var bikeCheck = m_ParentIntersection.m_ParentProject.m_Banks.FirstOrDefault(x => x == "Bicycles" || x == "FHWAPedsBikes");

                int bikeBank = -1;
                if (bikeCheck != null)
                {
                  bikeBank = m_ParentIntersection.m_ParentProject.m_Banks.IndexOf(
                    m_ParentIntersection.m_ParentProject.m_Banks.FirstOrDefault(x => x == "Bicycles" || x == "FHWAPedsBikes"));
                }
                if (bikeBank >= 0)
                {
                  m_Data.Tables[bikeBank].Rows[thisRow][approaches[bank]] = parsedLine[m_ParentIntersection.m_ParentProject.m_Banks.Count + 1];
                  string key = m_Data.Tables[bikeBank].TableName + "," + approaches[bank] + "," + m_Data.Tables[bikeBank].Rows[thisRow].ItemArray[0];
                  UpdateDataCellAssociation(fileName, key);
                }
              }
              m_DataState = DataState.Partial;
              break;
          }
        }
      }
    }

    private string[] ParseApproach(string approach)
    {
      if (approach.Length == 2)
      {
        return new string[] { approach + "T", approach + "R", approach + "L", approach + "U" };
      }
      else if (approach.Length == 3)
      {
        return new string[] { approach };
      }
      return new string[0];
    }

    #endregion

    #region Writing Data to a File

    public void ExportData(string directory, bool useLegacyRules)
    {
      TMCProject proj = m_ParentIntersection.m_ParentProject;
      if (m_DataState == DataState.Empty)
      {
        MessageBoxResult result = MessageBox.Show(
          "No Data in count: \n\n" + m_Id + "\n\n Create ASCII file with all zeroes?", "No data",
          MessageBoxButton.YesNo);
        if (result == MessageBoxResult.No)
        {
          return;
        }
      }
      string filePath = directory + "\\" + m_Id + ".txt";
      if (File.Exists(filePath))
      {
        MessageBoxResult result =
          MessageBox.Show("ASCII File already exists for " + m_Id + "\n\nWould you like to overwrite?\n\n" + "(No will save a copy with date-time suffix)", "ASCII File Exists",
            MessageBoxButton.YesNoCancel);
        if (result == MessageBoxResult.Cancel)
        {
          return;
        }
        if (result == MessageBoxResult.No)
        {
          filePath = directory + DateTime.Today + "\\" + m_Id + ".txt";
        }
      }
      try
      {
        StreamWriter writer = new StreamWriter(filePath, false);
        writer.WriteLine("Start Date," + m_FilmDate.Month + "/" + m_FilmDate.Day + "/" + m_FilmDate.Year);
        writer.WriteLine("Start Time," + m_StartTime);
        writer.WriteLine("Site Code," + m_Id);
        StringBuilder newLine = new StringBuilder();
        newLine.Append(m_ParentIntersection.GetSBNBApproach());
        newLine.Append("--Southbound,,,,");
        newLine.Append(m_ParentIntersection.GetWBEBApproach());
        newLine.Append("--Westbound,,,,");
        newLine.Append(m_ParentIntersection.GetSBNBApproach());
        newLine.Append("--Northbound,,,,");
        newLine.Append(m_ParentIntersection.GetWBEBApproach());
        newLine.Append("--Eastbound,,,,");
        writer.WriteLine("Street Name," + newLine);
        const string line1 = "Right,Thru ,Left ,Peds,";
        const string line2 = "Right ,Thru ,Left ,Peds,";
        writer.WriteLine("Start Time," + line1 + line2 + line2 + line2);


        // Get all the bank data

        // Passenger and Pedestrian bank info
        var passengerBanks = proj.m_BankDictionary
          .Where(kv =>
              kv.Value == "Passenger"
              || kv.Value == "FHWA1_2_3"
              || kv.Value == "FHWA1"
              || kv.Value == "FHWA2"
              || kv.Value == "FHWA3")
          .Select(kv => kv.Key)
          .ToList<int>();

        int pedPedBank = -1;
        if (proj.m_PedBanks.IndexOf(PedColumnDataType.Pedestrian) >= 0)
        {
          pedPedBank = proj.m_PedBanks.IndexOf(PedColumnDataType.Pedestrian);
        }

        // RTOR Bank Data
        List<int> passengerRTORBanks = new List<int>();
        List<int> heavyRTORBanks = new List<int>();

        if (proj.m_IsRTOR)
        {
          for (int u = 0; u < proj.m_PedBanks.Count; u++)
          {
            if (proj.m_PedBanks[u] == PedColumnDataType.PassengerRTOR
              || proj.m_PedBanks[u] == PedColumnDataType.RTOR)
            {
              passengerRTORBanks.Add(u);
            }
            if (proj.m_PedBanks[u] == PedColumnDataType.HeavyRTOR)
            {
              heavyRTORBanks.Add(u);
            }
          }
        }

        // Bank 2 and U-Turn Bank Data

        var bikeEnums = new List<string> { "Bicycles", "FHWAPedsBikes" };
        if (useLegacyRules)
        {
          bikeEnums.Add("E_Scooters");
        }

        var bikeBanks = proj.m_Banks.Select((n, i) => new { n, i })
                        .Where(bank => bikeEnums.Contains(bank.n))
                        .Select(bank => bank.i).ToList();
        
        //U-Turn Data
        List<int> passengerUTurnBanks = new List<int>();
        List<int> heavyUTurnBanks = new List<int>();
        if (proj.m_IsUTurn)
        {
          for (int u = 0; u < proj.m_PedBanks.Count; u++)
          {
            if (proj.m_PedBanks[u] == PedColumnDataType.PassengerUTurn
              || proj.m_PedBanks[u] == PedColumnDataType.UTurn)
            {
              passengerUTurnBanks.Add(u);
            }
            if (proj.m_PedBanks[u] == PedColumnDataType.HeavyUTurn)
            {
              heavyUTurnBanks.Add(u);
            }
          }
        }

        //Writing out the data
        for (int interval = 0; interval < m_NumIntervals; interval++)
        {
          string intervalTime = ConvertTimeFromMilitary(m_Data.Tables[0].Rows[interval].ItemArray[0].ToString());

          // Bank 0
          newLine.Clear();
          newLine.Append(intervalTime);
          for (int k = 1; k < 17; k++)
          {
            if (k == 4 || k == 8 || k == 12 || k == 16)
            {
              if (pedPedBank < 0)
              {
                newLine.Append(",0");
              }
              else
              {
                newLine.Append("," + m_Data.Tables[pedPedBank].Rows[interval].ItemArray[k]);
              }
            }
            else
            {
              int cellSum = 0;
              foreach (var passBank in passengerBanks)
              {
                cellSum += int.Parse(m_Data.Tables[passBank].Rows[interval].ItemArray[k].ToString());
              }
              newLine.Append("," + cellSum);
            }
          }
          writer.WriteLine(newLine.ToString());

          // All Heavies
          newLine.Clear();
          newLine.Append(intervalTime);
          for (int k = 1; k < 17; k++)
          {
            if (k == 4 || k == 8 || k == 12 || k == 16)
            {
              int sumRTORs = 0;
              foreach (var rPedBank in passengerRTORBanks)
              {
                sumRTORs += int.Parse(m_Data.Tables[rPedBank].Rows[interval].ItemArray[k].ToString());
              }

              foreach (var rPedBank in heavyRTORBanks)
              {
                sumRTORs += int.Parse(m_Data.Tables[rPedBank].Rows[interval].ItemArray[k].ToString());
              }
              newLine.Append("," + sumRTORs);
            }
            else
            {
              int sumHeavies = 0;
              var heavyBanksToUse = useLegacyRules ? proj.m_HeavyBanks : proj.GetBanksConsideredHeavies(false);
              foreach (var hBank in heavyBanksToUse)
              {
                sumHeavies += int.Parse(m_Data.Tables[hBank].Rows[interval].ItemArray[k].ToString());
              }
              newLine.Append("," + sumHeavies);
            }
          }
          writer.WriteLine(newLine.ToString());

          // Bank 2
          newLine.Clear();
          newLine.Append(intervalTime);
          for (int k = 1; k < 17; k++)
          {
            if (k == 4 || k == 8 || k == 12 || k == 16)
            {
              int sumUTurns = 0;
              foreach (var uPedBank in passengerUTurnBanks)
              {
                var cellToAdd = int.Parse(m_Data.Tables[uPedBank].Rows[interval].ItemArray[k].ToString());
                sumUTurns += int.Parse(m_Data.Tables[uPedBank].Rows[interval].ItemArray[k].ToString());
              }

              foreach (var uPedBank in heavyUTurnBanks)
              {
                sumUTurns += int.Parse(m_Data.Tables[uPedBank].Rows[interval].ItemArray[k].ToString());
              }
              newLine.Append("," + sumUTurns);
            }
            else
            {
              int sumBikes = 0;
              foreach (var bank in bikeBanks)
              {
                sumBikes += int.Parse(m_Data.Tables[bank].Rows[interval].ItemArray[k].ToString());
              }
              newLine.Append("," + sumBikes);
            }
          }
          writer.WriteLine(newLine.ToString());

          // Bank 3 - 5
          if (!useLegacyRules)
          {
            // Bank 3 - Buses
            newLine.Clear();
            newLine.Append(intervalTime);
            for (int k = 1; k < 17; k++)
            {
              if (k == 4 || k == 8 || k == 12 || k == 16)
              {
                newLine.Append(",0");
              }
              else
              {
                int sumBuses = 0;
                var banksToUse = proj.m_Banks.Select((n, i) => new { n, i })
                        .Where(bank => bank.n == "Buses" || bank.n == "FHWA4")
                        .Select(bank => bank.i).ToList();
                foreach (var bank in banksToUse)
                {
                  sumBuses += int.Parse(m_Data.Tables[bank].Rows[interval].ItemArray[k].ToString());
                }
                newLine.Append("," + sumBuses);
              }
            }
            writer.WriteLine(newLine.ToString());

            // Bank 4 - E-Scooters
            newLine.Clear();
            newLine.Append(intervalTime);
            for (int k = 1; k < 17; k++)
            {
              if (k == 4 || k == 8 || k == 12 || k == 16)
              {
                newLine.Append(",0");
              }
              else
              {
                int sumScooters = 0;
                var banksToUse = proj.m_Banks.Select((n, i) => new { n, i })
                        .Where(bank => bank.n == "E_Scooters")
                        .Select(bank => bank.i).ToList();
                foreach (var bank in banksToUse)
                {
                  sumScooters += int.Parse(m_Data.Tables[bank].Rows[interval].ItemArray[k].ToString());
                }
                newLine.Append("," + sumScooters);
              }
            }
            writer.WriteLine(newLine.ToString());

            // Bank 5
            newLine.Clear();
            newLine.Append(intervalTime);
            for (int k = 1; k < 17; k++)
            {
              newLine.Append(",0");
            }
            writer.WriteLine(newLine.ToString());
          }
          else
          {
            // Legacy Banks 3-5
            for (int z = 3; z < 6; z++)
            {
              newLine.Clear();
              newLine.Append(intervalTime);
              for (int k = 1; k < 17; k++)
              {
                newLine.Append(",0");
              }
              writer.WriteLine(newLine.ToString());
            }
          }
        }
        writer.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Could not write to file. \n\n" + filePath + "\n\n" + ex.Message,
                  "Export File Error", MessageBoxButton.OK);
      }
    }

    public void GenerateExcelDeliverable(string directory)
    {
      if (m_ParentIntersection.m_ParentProject.m_TCCDataFileRules)
      {
        DirectoryInfo outputDir = new DirectoryInfo(directory);
        FileInfo newFile = new FileInfo(outputDir.FullName + "\\" + m_Id + ".xlsx");
        if (newFile.Exists)
        {
          string version = "";
          //the following handles the case where the file it's trying to write to already exists and is open
          while (true)
          {
            try
            {
              newFile.Delete();  // ensures we create a new workbook
              newFile = new FileInfo(outputDir.FullName + "\\" + m_Id + version + ".xlsx");
              break;
            }
            catch
            {
              if (version == "")
              {
                version = " (1)";
              }
              else
              {
                int versionNum = int.Parse(version.Split('(', ')')[1]) + 1;
                version = " (" + versionNum.ToString() + ")";
              }
              newFile = new FileInfo(outputDir.FullName + "\\" + m_Id + version + ".xlsx");
            }
          }
          //newFile.Delete();  // ensures we create a new workbook
          //newFile = new FileInfo(outputDir.FullName + "\\" + m_Id + ".xlsx");
        }
        ExcelPackage package = new ExcelPackage(newFile);

        int[] columnOrder = new int[16] { 2, 3, 1, 4, 6, 7, 5, 8, 10, 11, 9, 12, 14, 15, 13, 16 };
        //starting top-left cell of the data; the placement of the entire table references this in case it were to change
        int startRow = 11;
        int startCol = 2;
        //get information about the banks which are not bike since we don't create those tabs until the end regardless of if or where they show up in the banks
        List<KeyValuePair<int, string>> nonBikeBanks = new List<KeyValuePair<int, string>>();
        foreach (KeyValuePair<int, string> bank in m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(x => x.Value != BankVehicleTypes.Bicycles.ToString() && x.Value != BankVehicleTypes.FHWAPedsBikes.ToString()))
        {
          nonBikeBanks.Add(new KeyValuePair<int, string>(bank.Key, bank.Value));
        }
        //The number of rows and columns the data spans
        int numDataRows = m_NumIntervals / 3;
        int numDataCols = nonBikeBanks.Count;

        //create the 16 tabs
        for (int i = 0; i < 16; i++)
        {
          //create the sheet for the movement
          ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(m_Data.Tables[0].Columns[columnOrder[i]].ColumnName);
          if (worksheet.Name[2] == 'U')
          {
            //it's a u-turn tab, rename
            worksheet.Name = worksheet.Name.Substring(0, 2) + " " + Descriptions.MovementDescription["U"];
          }
          worksheet.View.ZoomScale = 70;
          worksheet.View.SelectedRange = "P1";

          //add logo
          ExcelPicture pic = worksheet.Drawings.AddPicture("logo", AppMerlin.Properties.Resources.QCLogoBlueRedNotOpaque);
          //information section
          worksheet.Cells["H1"].Value = "Date Counted:";
          worksheet.Cells["I1"].Value = m_FilmDate.ToString("d");
          worksheet.Cells["H2"].Value = @"Location/Intersection:";
          worksheet.Cells["I2"].Value = GetLocation();
          worksheet.Cells["H3"].Value = "Direction Counted:";
          string movement = m_Data.Tables[0].Columns[columnOrder[i]].ColumnName;
          worksheet.Cells["I3"].Value = Descriptions.ApproachDescription[movement.Substring(0, 2)] + " " + Descriptions.MovementDescription[movement.Substring(2, 1)];

          //interval time column - need to set number format first so Excel properly recognizes incoming data as time
          ExcelRange intervalCells = worksheet.Cells[startRow, startCol - 1, startRow + numDataRows - 1, startCol - 1];
          intervalCells.Style.Numberformat.Format = "h:mm AM/PM";
          for (int row = 0; row < numDataRows; row++)
          {
            DateTime interval = DateTime.Parse(m_Data.Tables[0].Rows[row * 3][0].ToString());
            worksheet.Cells[row + startRow, startCol - 1].Value = interval.ToString("h:mm tt");
            //worksheet.Cells[row + startRow, startCol - 1].Value = interval.ToString("HH:mm:ss");
            //worksheet.Cells[row + startRow, startCol - 1].Value = m_Data.Tables[0].Rows[row * 3][0];
          }
          worksheet.Cells[startRow + numDataRows, startCol - 1].Value = "Total";
          worksheet.Cells[startRow + numDataRows + 1, startCol - 1].Value = "%";
          //columns of data
          for (int col = 0; col < numDataCols; col++)
          {
            //take care of two header rows
            BankVehicleTypes thisVehicleType = (BankVehicleTypes)Enum.Parse(typeof(BankVehicleTypes), nonBikeBanks[col].Value);
            worksheet.Cells[startRow - 2, startCol + col].Value = Descriptions.FhwaToClass[thisVehicleType];
            worksheet.Cells[startRow - 1, startCol + col].Value = Descriptions.FhwaToDescription[thisVehicleType];
            //now write data
            for (int row = 0; row < numDataRows; row++)
            {
              //getting data for one cell in the sheet requires aggregating three cells in the datatable since this deliverable is in 15-minute intervals
              int cell1 = Int32.Parse(m_Data.Tables[nonBikeBanks[col].Key].Rows[row * 3][columnOrder[i]].ToString());
              int cell2 = Int32.Parse(m_Data.Tables[nonBikeBanks[col].Key].Rows[(row * 3) + 1][columnOrder[i]].ToString());
              int cell3 = Int32.Parse(m_Data.Tables[nonBikeBanks[col].Key].Rows[(row * 3) + 2][columnOrder[i]].ToString());
              worksheet.Cells[startRow + row, startCol + col].Value = cell1 + cell2 + cell3;
            }
            //column sum and percentage of total footer rows
            worksheet.Cells[startRow + numDataRows, startCol + col].Formula = "SUM(" + worksheet.Cells[startRow, startCol + col].Address + ":" + worksheet.Cells[startRow + numDataRows - 1, startCol + col].Address + ")";
            worksheet.Cells[startRow + numDataRows + 1, startCol + col].Formula = worksheet.Cells[startRow + numDataRows, startCol + col].ToString() + " * 100" + "/" + worksheet.Cells[startRow + numDataRows, startCol + numDataCols].ToString();
            worksheet.Cells[startRow + numDataRows + 1, startCol + col].Style.Numberformat.Format = "#0\\.00%";
            worksheet.Cells[startRow + numDataRows + 2, startCol + col].Value = Descriptions.FhwaToClass[thisVehicleType];
          }
          //interval totals column
          worksheet.Cells[startRow - 1, startCol + numDataCols].Value = "Interval Total";
          for (int row = 0; row < numDataRows; row++)
          {
            worksheet.Cells[row + startRow, startCol + numDataCols].Formula = "SUM(" + worksheet.Cells[row + startRow, startCol].Address + ":" + worksheet.Cells[row + startRow, startCol + numDataCols - 1].Address + ")";
          }
          //grand total
          worksheet.Cells[startRow + numDataRows, startCol + numDataCols].Formula = "SUM(" + worksheet.Cells[startRow, startCol + numDataCols].Address + ":" + worksheet.Cells[startRow + numDataRows - 1, startCol + numDataCols].Address + ")";
          //merge grand total cells
          ExcelRange grandTotalCells = worksheet.Cells[startRow + numDataRows, startCol + numDataCols, startRow + numDataRows + 1, startCol + numDataCols];
          grandTotalCells.Merge = true;
          grandTotalCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

          #region Sheet Formatting

          //--CELL ALIGNMENTS--
          //set all cells to center alignment
          worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
          //set top info cells alignment
          worksheet.Cells["H1:H3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
          worksheet.Cells["I1:I3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
          //interval cells alignment
          intervalCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

          //--FONTS--
          //set larger font of info rows
          worksheet.Cells[1, 1, 5, 30].Style.Font.SetFromFont(new Font("Arial", 14));
          //set smaller font for everywhere else
          worksheet.Cells[6, 1, startRow + numDataRows + 10, 30].Style.Font.SetFromFont(new Font("Arial", 10));

          //--BORDERS & CELL COLORS--
          //all borders
          SetAllBorders(worksheet.Cells[startRow - 2, startCol, startRow - 1, startCol + numDataCols - 1], ExcelBorderStyle.Thin);
          SetAllBorders(worksheet.Cells[startRow, startCol - 1, startRow + numDataRows + 1, startCol + numDataCols], ExcelBorderStyle.Thin);
          SetAllBorders(worksheet.Cells[startRow + numDataRows + 2, startCol, startRow + numDataRows + 2, startCol + numDataCols - 1], ExcelBorderStyle.Thin);
          //outside thick borders
          worksheet.Cells[startRow - 2, startCol, startRow - 1, startCol + numDataCols - 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          worksheet.Cells[startRow - 1, startCol + numDataCols].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          worksheet.Cells[startRow, startCol - 1, startRow + numDataRows + 1, startCol + numDataCols].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          worksheet.Cells[startRow, startCol, startRow + numDataRows + 1, startCol + numDataCols - 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          worksheet.Cells[startRow + numDataRows + 2, startCol, startRow + numDataRows + 2, startCol + numDataCols - 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          worksheet.Cells[startRow + numDataRows, startCol - 1, startRow + numDataRows + 1, startCol + numDataCols].Style.Border.BorderAround(ExcelBorderStyle.Medium);
          //outside thick borders and alternating colors for by hour (accounting for the fact a count might not begin or end on the hour)
          Color alternatingColor = Color.FromArgb(255, 254, 194);
          bool useAlternateColor = false;
          for (int row = startRow; row < startRow + numDataRows; row++)
          {
            //get the hour portion of previous and current interval to detect if we've reached a new hour
            string prevIntervalHour = worksheet.Cells[row - 1, startCol - 1].Value as string;
            prevIntervalHour = prevIntervalHour == null ? "" : prevIntervalHour.Split(':')[0];
            string currIntervalHour = (worksheet.Cells[row, startCol - 1].Value as string).Split(':')[0];
            if (prevIntervalHour != currIntervalHour && !string.IsNullOrEmpty(prevIntervalHour))
            {
              //detected a new hour
              useAlternateColor = !useAlternateColor;
              worksheet.Cells[row, startCol - 1, row, startCol + numDataCols].Style.Border.Top.Style = ExcelBorderStyle.Medium;
            }
            if (useAlternateColor)
            {
              worksheet.Cells[row, startCol, row, startCol + numDataCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
              worksheet.Cells[row, startCol, row, startCol + numDataCols].Style.Fill.BackgroundColor.SetColor(alternatingColor);
            }
          }

          //--ROW HEIGHTS--
          //info rows
          SetHeightOfRows(worksheet, 1, 5, 19.5);
          //in between info and data column headers
          SetHeightOfRows(worksheet, 6, 8, 12.75);
          //data column headers
          SetHeightOfRows(worksheet, 9, 10, 14.25);
          //data rows plus total row
          SetHeightOfRows(worksheet, startRow, startRow + numDataRows, 12.75);
          //percentage and class number rows
          SetHeightOfRows(worksheet, startRow + numDataRows + 1, startRow + numDataRows + 2, 14.25);

          //--COLUMN WIDTHS--
          //interval column
          SetWidthOfColumns(worksheet, 1, 1, GetTrueColumnWidth(9.14));
          //data columns
          SetWidthOfColumns(worksheet, startCol, startCol + numDataCols - 1, GetTrueColumnWidth(12.71));
          //interval total column
          SetWidthOfColumns(worksheet, startCol + numDataCols, startCol + numDataCols, GetTrueColumnWidth(10.86));
          //some more columns the same width as data columns since in peds and bikes we had to delete 12 columns (keeps widths consistent
          SetWidthOfColumns(worksheet, startCol + numDataCols + 1, startCol + numDataCols + 1, GetTrueColumnWidth(14.57));
          SetWidthOfColumns(worksheet, startCol + numDataCols + 2, startCol + numDataCols + 20, GetTrueColumnWidth(12.71));


          #endregion

        }

        #region Ped & Bike Tabs

        //now create ped and bike tabs by copying and modifying one of the FHWA class tabs
        int pedBikeBank = m_ParentIntersection.m_ParentProject.m_Banks.IndexOf(BankVehicleTypes.FHWAPedsBikes.ToString());
        DataTable pedBikeData = m_Data.Tables[pedBikeBank];

        //BIKE
        int[] bikeColumnOrder = new int[] { 2, 3, 1, 6, 7, 5, 10, 11, 9, 14, 15, 13 };
        ExcelWorksheet bikeSheet = package.Workbook.Worksheets.Add("Bikes", package.Workbook.Worksheets["SBT"]);
        bikeSheet.DeleteColumn(10, 1);
        //merge headers
        for (int col = startCol; col < startCol + bikeColumnOrder.Length; col++)
        {
          ExcelRange toMerge = bikeSheet.Cells[startRow - 2, col, startRow - 1, col];
          toMerge.Merge = true;
          toMerge.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
        //remove percentage row
        bikeSheet.DeleteRow(startRow + numDataRows + 1, 1, true);
        bikeSheet.Cells[startRow + numDataRows, startCol - 1, startRow + numDataRows, startCol + numDataCols - 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);

        //populate data columns
        //cols
        for (int i = 0; i < bikeColumnOrder.Length; i++)
        {
          bikeSheet.Cells[startRow - 2, startCol + i].Value = pedBikeData.Columns[bikeColumnOrder[i]].ColumnName;
          //rows
          for (int j = 0; j < numDataRows; j++)
          {
            //getting data for one cell in the sheet requires aggregating three cells in the datatable since this deliverable is in 15-minute intervals
            int cell1 = Int32.Parse(pedBikeData.Rows[j * 3][bikeColumnOrder[i]].ToString());
            int cell2 = Int32.Parse(pedBikeData.Rows[(j * 3) + 1][bikeColumnOrder[i]].ToString());
            int cell3 = Int32.Parse(pedBikeData.Rows[(j * 3) + 2][bikeColumnOrder[i]].ToString());
            bikeSheet.Cells[startRow + j, startCol + i].Value = cell1 + cell2 + cell3;
          }
          bikeSheet.Cells[startRow + numDataRows + 1, startCol + i].Value = pedBikeData.Columns[bikeColumnOrder[i]].ColumnName;
        }
        bikeSheet.Cells["I3"].Value = "Bicycles";

        //PED
        ExcelWorksheet pedSheet = package.Workbook.Worksheets.Add("Peds", package.Workbook.Worksheets["Bikes"]);
        pedSheet.DeleteColumn(3, 8);
        int[] pedColumnOrder = new int[] { 4, 8, 12, 16 };

        //populate data columns
        //cols
        for (int i = 0; i < 4; i++)
        {
          pedSheet.Cells[startRow - 2, startCol + i].Value = Descriptions.PedApproachToSide[pedBikeData.Columns[pedColumnOrder[i]].ColumnName];
          //rows
          for (int j = 0; j < numDataRows; j++)
          {
            //getting data for one cell in the sheet requires aggregating three cells in the datatable since this deliverable is in 15-minute intervals
            int cell1 = Int32.Parse(pedBikeData.Rows[j * 3][pedColumnOrder[i]].ToString());
            int cell2 = Int32.Parse(pedBikeData.Rows[(j * 3) + 1][pedColumnOrder[i]].ToString());
            int cell3 = Int32.Parse(pedBikeData.Rows[(j * 3) + 2][pedColumnOrder[i]].ToString());
            pedSheet.Cells[startRow + j, startCol + i].Value = cell1 + cell2 + cell3;
          }
          pedSheet.Cells[startRow + numDataRows + 1, startCol + i].Value = Descriptions.PedApproachToSide[pedBikeData.Columns[pedColumnOrder[i]].ColumnName];
        }

        //fix information section
        SetWidthOfColumns(pedSheet, 8, 20, GetTrueColumnWidth(12.71));
        pedSheet.Cells["H1"].Value = bikeSheet.Cells["H1"].Value;
        pedSheet.Cells["I1"].Value = bikeSheet.Cells["I1"].Value;
        pedSheet.Cells["H2"].Value = bikeSheet.Cells["H2"].Value;
        pedSheet.Cells["I2"].Value = bikeSheet.Cells["I2"].Value;
        pedSheet.Cells["H3"].Value = bikeSheet.Cells["H3"].Value;
        pedSheet.Cells["I3"].Value = "Pedestrians";

        pedSheet.Cells["H1:H3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        pedSheet.Cells["I1:I3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

        #endregion

        package.Save();

      }
    }

    public void PrintOutFileAssociation()
    {
      string filePath = @"C:\QCProjects\" + m_Id + "_FA_" + DateTime.Now.Ticks + ".txt";

      using (StreamWriter writer = new StreamWriter(filePath, false))
      {
        foreach (var cell in m_DataCellToFileMap)
        {
          writer.WriteLine(cell.Key + " - " + cell.Value.TidyName);
        }
      }

    }


    #region Helpers for Excel Generation

    private bool SetHeightOfRows(ExcelWorksheet sheet, int firstRow, int lastRow, double height)
    {
      if (firstRow > lastRow || firstRow < 1 || lastRow > sheet.Dimension.End.Row)
      {
        return false;
      }
      for (int row = firstRow; row <= lastRow; row++)
      {
        sheet.Row(row).Height = height;
      }

      return true;
    }

    private bool SetWidthOfColumns(ExcelWorksheet sheet, int firstCol, int lastCol, double width)
    {
      if (firstCol > lastCol || firstCol < 1 || lastCol > sheet.Dimension.End.Column)
      {
        return false;
      }
      for (int col = firstCol; col <= lastCol; col++)
      {
        sheet.Column(col).Width = width;
      }

      return true;
    }

    /// <summary>
    /// This is an unfortunately needed function to get the column widths correct due to a bug in EPPlus where if you set the column widths directly they will be off.
    /// </summary>
    /// <param name="dblWidth">Width that you need</param>
    /// <returns>Modified width that should cause EPPlus to render the actual width you wanted</returns>
    private double GetTrueColumnWidth(double dblWidth)
    {
      //DEDUCE WHAT THE COLUMN WIDTH WOULD REALLY GET SET TO
      double z = 1d;
      if (dblWidth >= (1 + 2 / 3))
        z = Math.Round((Math.Round(7 * (dblWidth - 1 / 256), 0) - 5) / 7, 2);
      else
        z = Math.Round((Math.Round(12 * (dblWidth - 1 / 256), 0) - Math.Round(5 * dblWidth, 0)) / 12, 2);

      //HOW FAR OFF? (WILL BE LESS THAN 1)
      double errorAmt = dblWidth - z;

      //CALCULATE WHAT AMOUNT TO TACK ONTO THE ORIGINAL AMOUNT TO RESULT IN THE CLOSEST POSSIBLE SETTING 
      double adjAmt = 0d;
      if (dblWidth >= (1 + 2 / 3))
        adjAmt = (Math.Round(7 * errorAmt - 7 / 256, 0)) / 7;
      else
        adjAmt = ((Math.Round(12 * errorAmt - 12 / 256, 0)) / 12) + (2 / 12);

      //RETURN A SCALED-VALUE THAT SHOULD RESULT IN THE NEAREST POSSIBLE VALUE TO THE TRUE DESIRED SETTING
      if (z > 0)
        return dblWidth + adjAmt;
      return 0d;

    }

    private void SetAllBorders(ExcelRange cells, ExcelBorderStyle borderStyle)
    {
      cells.Style.Border.Top.Style = borderStyle;
      cells.Style.Border.Bottom.Style = borderStyle;
      cells.Style.Border.Left.Style = borderStyle;
      cells.Style.Border.Right.Style = borderStyle;
    }

    #endregion

    #endregion

    private string ConvertTimeFromMilitary(string incomingTime)
    {
      StringBuilder newTime = new StringBuilder();
      string period = " AM";
      string[] time = incomingTime.Split(':');
      int hour = int.Parse(time[0]);

      switch (hour)
      {
        case 12:
          period = " PM";
          break;
        case 0:
          hour = 12;
          break;
        default:
          if (hour > 12)
          {
            hour = hour - 12;
            period = " PM";
          }
          break;
      }
      string newHour;
      if (hour < 10)
      {
        newHour = "0" + hour;
      }
      else
      {
        newHour = hour.ToString();
      }
      newTime.Append(newHour);
      newTime.Append(":");
      newTime.Append(time[1]);
      newTime.Append(period);

      return newTime.ToString();
    }

    #region DataFile Management
    public void AddFileNameToFileList(DataFile file)
    {
      m_AssociatedDataFiles.Add(file);
    }

    public bool RemoveFileNameFromFileList(DataFile file)
    {
      return m_AssociatedDataFiles.Remove(file);
    }

    public bool HasDataFile(DataFile file)
    {
      foreach (var f in m_AssociatedDataFiles)
      {
        if (f.Name.ToUpper() == file.Name.ToUpper())
        {
          return true;
        }
      }
      return false;
    }

    public bool HasDataFile(string file)
    {
      foreach (var f in m_AssociatedDataFiles)
      {
        if (f.Name.ToUpper() == file.ToUpper())
        {
          return true;
        }
      }
      return false;
    }

    private DataFile GetDataFileByName(string name)
    {
      foreach (var file in m_AssociatedDataFiles)
      {
        if (file.Name.ToUpper() == name.ToUpper())
        {
          return file;
        }
      }
      return null;
    }

    private void UpdateDataCellAssociation(string fileName, string key)
    {
      FindAndRemoveDataFileAssociation(key);
      if (GetDataFileByName(fileName) != null)
      {
        GetDataFileByName(fileName).AddDataCell(key);
        if (m_DataCellToFileMap.ContainsKey(key))
        {
          m_DataCellToFileMap[key] = GetDataFileByName(fileName);
        }
      }
    }

    private void MarkDataCellAsManual(string key)
    {
      DataFile manual = GetDataFileByName("Manual Entry");
      manual.AddDataCell(key);
      if (m_DataCellToFileMap.ContainsKey(key))
      {
        m_DataCellToFileMap[key] = manual;
      }
    }

    private void MarkDataCellAsUnknown(string key)
    {
      DataFile unknown = new DataFile();
      unknown.Name = "Unknown Source";
      unknown.ImportDate = DateTime.Now;
      if (m_DataCellToFileMap.ContainsKey(key))
      {
        m_DataCellToFileMap[key] = unknown;
      }
    }

    private DataFile FindDataFileAssociationByKey(string key)
    {
      foreach (var file in m_AssociatedDataFiles)
      {
        if (file.DataCellMap.Contains(key))
        {
          return file;
        }
      }
      return null;
    }

    private void FindAndRemoveDataFileAssociation(string key)
    {
      if (m_DataCellToFileMap.ContainsKey(key))
      {
        var oldFile = m_DataCellToFileMap[key];
        if (oldFile != null)
        {
          oldFile.RemoveDataCell(key);
        }
      }
    }

    public void InvertDataFileToCellMapping()
    {
      DataFile unknown = new DataFile();
      unknown.Name = "Unknown Source";
      unknown.ImportDate = DateTime.Now;
      //First assume all the cells in the data set are unknown so we don't have to figure out which ones didn't get marked later.
      m_DataCellToFileMap.Clear();
      for (int i = 0; i < m_Data.Tables.Count; i++)
      {
        var table = m_Data.Tables[i];
        for (int j = 1; j < table.Columns.Count; j++)
        {
          for (int k = 0; k < table.Rows.Count; k++)
          {
            m_DataCellToFileMap.Add(table.TableName + "," + table.Columns[j].ColumnName + "," + table.Rows[k].ItemArray[0], unknown);
          }
        }
      }

      // Second update all the ones who have a data file associated with them.
      foreach (var file in m_AssociatedDataFiles)
      {
        foreach (string key in file.DataCellMap)
        {
          if (m_DataCellToFileMap.ContainsKey(key))
          {
            m_DataCellToFileMap[key] = file;
          }
        }
      }
    }

    public void ClearDataFiles()
    {
      List<DataFile> clearedList = new List<DataFile>(m_AssociatedDataFiles.Where(x => x.Name == "Manual Entry").Select(x => (DataFile)x.Clone()));
      m_AssociatedDataFiles = clearedList;
    }

    #endregion

    /// <summary>
    /// Add Intervals to count 
    /// </summary>
    /// <param name="addToBeginning"></param>
    /// <param name="addToEnd"></param>
    public void AddIntervals(int addToBeginning, int addToEnd)
    {
      if (addToBeginning < 1 && addToEnd < 1)
      {
        return;
      }

      DateTime time2;
      foreach (DataTable dt in m_Data.Tables)
      {
        #region Add To Beginning

        DateTime.TryParse(m_StartTime, out time2);
        time2 = time2.AddMinutes(-5);
        for (int k = 0; k < addToBeginning; k++)
        {
          DataRow r = dt.NewRow();

          r[0] = time2.TimeOfDay.ToString().Remove(5, 3);
          for (int m = 1; m < dt.Columns.Count; m++)
          {
            r[m] = 0;
          }
          dt.Rows.InsertAt(r, 0);
          time2 = time2.AddMinutes(-5);
        }

        #endregion

        #region Add To End

        DateTime.TryParse(m_EndTime, out time2);
        for (int k = 0; k < addToEnd; k++)
        {
          DataRow r = dt.NewRow();

          r[0] = time2.TimeOfDay.ToString().Remove(5, 3);
          for (int m = 1; m < dt.Columns.Count; m++)
          {
            r[m] = 0;
          }
          dt.Rows.Add(r);
          time2 = time2.AddMinutes(5);

        }

        #endregion
      }

      //Update start time, end time, and number of intervals
      DateTime newStart, newEnd;
      DateTime.TryParse(m_StartTime, out newStart);
      DateTime.TryParse(m_EndTime, out newEnd);
      newStart = newStart.AddMinutes((addToBeginning * 5) * -1);
      newEnd = newEnd.AddMinutes(addToEnd * 5);
      m_StartTime = newStart.TimeOfDay.ToString().Remove(5, 3);
      m_EndTime = newEnd.TimeOfDay.ToString().Remove(5, 3);
      m_NumIntervals = m_NumIntervals + addToBeginning + addToEnd;
    }

    /// <summary>
    /// Remove Intervals from count
    /// </summary>
    /// <param name="fromBeginning"></param>
    /// <param name="fromEnd"></param>
    public void RemoveIntervals(int fromBeginning, int fromEnd)
    {
      //This method hasn't been tested, and at this time is not used since the program doesn't allow editing of time periods

      int numRows = 0;
      List<DataRow> rowsToDelete = new List<DataRow>();
      foreach (DataTable dt in m_Data.Tables)
      {
        //just look at first table to get number of intervals
        numRows = dt.Rows.Count;
        break;
      }

      foreach (DataTable dt in m_Data.Tables)
      {
        foreach (DataRow dr in dt.Rows)
        {
          //deletes from beginning and end or all if fromBeginning + fromEnd > numRows
          if (dt.Rows.IndexOf(dr) < fromBeginning || dt.Rows.IndexOf(dr) >= (numRows - fromEnd))
          {
            //adds the row to delete to the delete list
            rowsToDelete.Add(dr);
          }
        }
        foreach (DataRow row in rowsToDelete)
        {
          dt.Rows.Remove(row);
        }
        dt.AcceptChanges();
        rowsToDelete.Clear();
      }

      //Update start time, end time, and number of intervals
      DateTime newStart, newEnd;
      DateTime.TryParse(m_StartTime, out newStart);
      DateTime.TryParse(m_EndTime, out newEnd);
      newStart = newStart.AddMinutes(fromBeginning * 5);
      newEnd = newEnd.AddMinutes((fromEnd * 5) * -1);
      m_StartTime = newStart.TimeOfDay.ToString().Remove(5, 3);
      m_EndTime = newEnd.TimeOfDay.ToString().Remove(5, 3);
      m_NumIntervals = m_NumIntervals - fromBeginning - fromEnd;
    }

    #region Data Manipulation

    public bool EditSingleCell(int row, int col, int bank, int newValue)
    {
      m_Data.Tables[bank].Rows[row][col] = newValue;
      string key = m_Data.Tables[bank].TableName + "," + m_Data.Tables[bank].Columns[col].ColumnName + "," + m_Data.Tables[bank].Rows[row].ItemArray[0];
      FindAndRemoveDataFileAssociation(key);
      MarkDataCellAsManual(key);

      if (newValue != 0)
      {
        m_DataState = DataState.Partial;
      }
      else
      {
        RunDataState();
      }

      return true;
    }

    public bool CopyData(string fileName, ref DataSet copyFromData, bool copyZeroes = true, string beginningInterval = "", string endingInterval = "", List<int> movements = null, List<string> banks = null)
    {
      bool dataChanged = false;
      if (beginningInterval == "")
      {
        beginningInterval = m_StartTime;
      }
      if (endingInterval == "")
      {
        endingInterval = DateTime.Parse(m_EndTime).AddMinutes(-5).TimeOfDay.ToString().Remove(5, 3);
      }

      if (movements == null)
      {
        movements = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0].Where(x => x != "Time").Select(x => m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0].IndexOf(x)).ToList<int>();
      }

      if (banks == null)
      {
        banks = new List<string>();
        for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
        {
          if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
          {
            continue;
          }
          banks.Add(m_ParentIntersection.m_ParentProject.m_Banks[i]);
        }
      }
      for (int i = 0; i < movements.Count; i++)
      {
        if (CopyApproachDataFromFile(fileName, movements[i], beginningInterval, endingInterval, ref copyFromData, copyZeroes, banks))
        {
          dataChanged = true;
        }
      }
      RunDataState();
      return dataChanged;
    }

    public void CopyData(DataTable copyFromData, int destBank)
    {

      for (int i = 0; i < m_Data.Tables[destBank].Rows.Count; i++)
      {

        for (int j = 1; j < m_Data.Tables[destBank].Columns.Count; j++)
        {
          m_Data.Tables[destBank].Rows[i][j] = copyFromData.Rows[i][j];
        }
      }
      RunDataState();
    }

    /// <summary>
    /// Data Rotation Method
    /// </summary>
    /// <param name="beginningInterval"></param>
    /// <param name="endingInterval"></param>
    /// <param name="direction"></param>
    /// <param name="noOfTurns"></param>
    /// <param name="movements"></param>
    public void RotateCountData(string beginningInterval, string endingInterval,
      RotationDirection direction = RotationDirection.Clockwise, int noOfTurns = 1,
      List<int> movements = null)
    {
      if (m_DataState == DataState.Empty)
      {
        return;
      }

      string clearDataEndingInterval = DateTime.Parse(endingInterval).AddMinutes(5).ToString("HH:mm");

      int rotationFactor;
      if (noOfTurns == 1)
      {
        if (direction == RotationDirection.Clockwise)
        {
          rotationFactor = 4;
        }
        else
        {
          rotationFactor = 12;
        }
      }
      else
      {
        rotationFactor = 8;
      }
      // Must make deep copy of the data that and file assocations before this rotation so they can be used in the copy
      DataSet tempData = m_Data.Copy();
      var tempFileList = new List<DataFile>(m_AssociatedDataFiles.Select(x => (DataFile)x.Clone()));

      ClearData(beginningInterval, clearDataEndingInterval, movements);
      var headers = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0];

      for (int i = 0; i < movements.Count; i++)
      {
        int destMove = movements[i] + rotationFactor;
        if (destMove > headers.Count - 1)
        {
          destMove -= headers.Count - 1;
        }
        CopyApproachDataFromWithin(movements[i], destMove, tempFileList, beginningInterval, endingInterval, ref tempData);
      }
    }

    /// <summary>
    /// Clears Data in specified Range of time and movements.  If called with no parameters, will clear all data.
    /// </summary>
    /// <param name="begin">Beginning Interval</param>
    /// <param name="end">Last Interval that will be cleared</param>
    /// <param name="movements">List of Movements to clear</param>
    /// <param name="banks">List of Banks to clear</param>
    public void ClearData(string begin = "", string end = "", List<int> movements = null, List<string> banks = null)
    {
      if (m_DataState == DataState.Empty)
      {
        return;
      }

      //Setup details of rotation
      if (begin == "")
      {
        begin = m_StartTime;
      }
      if (end == "")
      {
        end = m_EndTime;
      }

      if (movements == null)
      {
        movements = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
      }

      if (banks == null)
      {
        banks = new List<string>();
        for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
        {
          if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
          {
            continue;
          }
          banks.Add(i.ToString());
        }
      }

      DateTime beginTime;
      DateTime endTime;
      DateTime.TryParse(begin, out beginTime);
      DateTime.TryParse(end, out endTime);
      if (endTime <= beginTime)
      {
        endTime = endTime.AddDays(1);
      }
      for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
        {
          continue;
        }

        if (banks.Contains(i.ToString()))
        {
          var bank = m_Data.Tables[i];
          foreach (var move in movements)
          {
            string approach = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][move];
            if (approach == "Time")
            {
              continue;
            }
            for (int j = 0; j < bank.Rows.Count; j++)
            {
              DateTime time;
              DateTime.TryParse(bank.Rows[j].ItemArray[0].ToString(), out time);
              if (time >= beginTime && time < endTime)
              {
                bank.Rows[j][approach] = 0;

                string key = bank.TableName + "," + bank.Columns[approach].ColumnName + "," + bank.Rows[j].ItemArray[0];
                FindAndRemoveDataFileAssociation(key);
                MarkDataCellAsManual(key);
              }
            }
          }
        }
      }
      RunDataState();
    }

    public bool CopyApproachDataFromWithin(int sourceMovement, int destMovement, List<DataFile> sourceFileList,
      string begin, string end, ref DataSet sourceData, List<string> banks = null)
    {
      bool dataChanged = false;
      DateTime beginTime;
      DateTime endTime;
      DateTime.TryParse(begin, out beginTime);
      DateTime.TryParse(end, out endTime);
      if (endTime < beginTime)
      {
        endTime = endTime.AddDays(1);
      }

      Dictionary<DateTime, Dictionary<string, int>> rowMaps = SetupRowMaps(beginTime, endTime, sourceData);

      if (banks == null)
      {
        banks = new List<string>();
        for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
        {
          if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
          {
            continue;
          }
          banks.Add(i.ToString());
        }
      }

      foreach (var interval in rowMaps)
      {
        if (interval.Value["This"] > -1)
        {
          for (int i = 0; i < m_Data.Tables.Count; i++)
          {
            if (banks.Contains(i.ToString()))
            {
              string convertedSourceMovement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][sourceMovement];
              string convertedDestMovement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][destMovement];

              m_Data.Tables[i].Rows[interval.Value["This"]][convertedDestMovement] =
                sourceData.Tables[i].Rows[interval.Value["Source"]][convertedSourceMovement];

              string destKey = m_Data.Tables[i].TableName + "," + m_Data.Tables[i].Columns[convertedDestMovement].ColumnName + "," + m_Data.Tables[i].Rows[interval.Value["This"]].ItemArray[0];
              string sourceKey = m_Data.Tables[i].TableName + "," + m_Data.Tables[i].Columns[convertedSourceMovement].ColumnName + "," + m_Data.Tables[i].Rows[interval.Value["Source"]].ItemArray[0];

              FindAndRemoveDataFileAssociation(destKey);

              foreach (var file in sourceFileList)
              {
                if (file.DataCellMap.Contains(sourceKey))
                {
                  DataFile sourceFile = GetDataFileByName(file.Name);
                  sourceFile.AddDataCell(destKey);
                  m_DataCellToFileMap[destKey] = sourceFile;
                  break;
                }
              }
              
              dataChanged = true;
            }
          }
        }
      }

      return dataChanged;
    }

    public bool CopyApproachDataFromFile(string fileName, int movement,
      string begin, string end, ref DataSet sourceData, bool copyZeroes = true, List<string> banks = null)
    {
      bool dataChanged = false;
      DateTime beginTime;
      DateTime endTime;
      DateTime.TryParse(begin, out beginTime);
      DateTime.TryParse(end, out endTime);
      if (endTime < beginTime)
      {
        endTime = endTime.AddDays(1);
      }

      Dictionary<DateTime, Dictionary<string, int>> rowMaps = SetupRowMaps(beginTime, endTime, sourceData);

      if (banks == null)
      {
        banks = new List<string>();
        for (int i = 0; i < m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
        {
          if (m_ParentIntersection.m_ParentProject.m_Banks[i] == "NOT USED" && m_ParentIntersection.m_ParentProject.m_PedBanks[i] == PedColumnDataType.NA)
          {
            continue;
          }
          banks.Add(m_ParentIntersection.m_ParentProject.m_Banks[i]);
        }
      }

      foreach (var interval in rowMaps)
      {
        if (interval.Value["This"] > -1)
        {
          for (int i = 0; i < m_Data.Tables.Count; i++)
          {
            if (banks.Contains(m_ParentIntersection.m_ParentProject.m_Banks[i]))
            {
              string convertedMovement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][movement];

              if (!copyZeroes && Int16.Parse(sourceData.Tables[i].Rows[interval.Value["Source"]][convertedMovement].ToString()) == 0)
              {
                continue;
              }
              m_Data.Tables[i].Rows[interval.Value["This"]][convertedMovement] =
                sourceData.Tables[i].Rows[interval.Value["Source"]][convertedMovement];

              string key = m_Data.Tables[i].TableName + "," + m_Data.Tables[i].Columns[convertedMovement].ColumnName + "," + m_Data.Tables[i].Rows[interval.Value["This"]].ItemArray[0];

              UpdateDataCellAssociation(fileName, key);

              dataChanged = true;
            }
          }
        }
      }

      return dataChanged;
    }

    private Dictionary<DateTime, Dictionary<string, int>> SetupRowMaps(DateTime begin, DateTime end, DataSet sourceDataSet)
    {
      Dictionary<DateTime, Dictionary<string, int>> returnMap = new Dictionary<DateTime, Dictionary<string, int>>();
      bool sourceAndCurrentStartEndAreSame = false;
      DataTable sourceData = sourceDataSet.Tables[0];

      if (sourceData.Rows[0]["Time"].ToString() == m_Data.Tables[0].Rows[0]["Time"].ToString() &&
        sourceData.Rows[sourceData.Rows.Count - 1]["Time"].ToString()
          == m_Data.Tables[0].Rows[m_Data.Tables[0].Rows.Count - 1]["Time"].ToString())
      {
        sourceAndCurrentStartEndAreSame = true;
      }
      bool beginningFound = false;
      bool endFound;

      for (int i = 0; i < sourceData.Rows.Count; i++)
      {
        if (sourceData.Rows[i]["Time"].ToString() == begin.TimeOfDay.ToString().Remove(5, 3))
        {
          returnMap.Add(begin, new Dictionary<string, int> { { "Source", i }, { "This", sourceAndCurrentStartEndAreSame ? i : -1 } });
          beginningFound = true;
          break;
        }
      }

      if (beginningFound)
      {
        DateTime current = begin.AddMinutes(5);
        int i = returnMap[begin]["Source"];
        i++;
        while (current <= end)
        {
          returnMap.Add(current, new Dictionary<string, int> { { "Source", i }, { "This", sourceAndCurrentStartEndAreSame ? i : -1 } });
          current = current.AddMinutes(5.0);
          i++;
        }
      }

      beginningFound = false;
      endFound = false;
      if (!sourceAndCurrentStartEndAreSame)
      {
        DataTable data = m_Data.Tables[0];
          for (int i = 0; i < data.Rows.Count; i++)
          {
            if (data.Rows[i]["Time"].ToString() == begin.TimeOfDay.ToString().Remove(5, 3))
            {
              returnMap[begin]["This"] = i;
              beginningFound = true;
            }
            if (data.Rows[i]["Time"].ToString() == end.TimeOfDay.ToString().Remove(5, 3))
            {
              returnMap[end]["This"] = i;
              endFound = true;
              break;
            }
          }

        if (beginningFound)
        {
          DateTime current = begin;
          int i = returnMap[begin]["This"];
          while (current <= end)
          {
            if (data.Rows.Count > i)
            {
              returnMap[current]["This"] = i;
            }
            current = current.AddMinutes(5.0);
            i++;
          }
        }
        else if (endFound)
        {
          DateTime current = end;
          int i = returnMap[end]["This"];
          while (current >= begin)
          {
            if (data.Rows.Count > i)
            {
              returnMap[current]["This"] = i;
            }
            current = current.AddMinutes(-5.0);
            i--;
          }
        }
          //begin is before count start time and end is after count end time;
        else
        {
          DateTime current = DateTime.Parse(m_StartTime);
          DateTime countEndTime = DateTime.Parse(m_EndTime).AddMinutes(-5);
          int i = 0;
          while (current <= countEndTime)
          {
            if (data.Rows.Count > i)
            {
              returnMap[current]["This"] = i;
            }
            current = current.AddMinutes(5.0);
            i++;
          }
        }
      }

      return returnMap;
    }

    #endregion

    // Overall function to process the file for errors
    public void RunTests(Dictionary<string, bool> dictionaryOfTestToggles)
    {
      //AddIntHeadersToList(errorList);
      List<string> possibleIntersectionMovements;
      if (!m_ParentIntersection.m_HasGreaterThanFourLegs)
      {
        possibleIntersectionMovements = new List<string> 
        {
          "SBR", "SBT", "SBL", "SBU",
          "WBR", "WBT", "WBL", "WBU",
          "NBR", "NBT", "NBL", "NBU",
          "EBR", "EBT", "EBL", "EBU"
        };
      }
      else
      {
        possibleIntersectionMovements = new List<string> 
        {
          "SBR", "SBT", "SBL", "SBU",
          "WBR", "WBT", "WBL", "WBU",
          "NBR", "NBT", "NBL", "NBU",
          "EBR", "EBT", "EBL", "EBU"
        };

        //TODO: What is going on here?
        foreach (string movement in m_ParentIntersection.m_MovementsInThisIntersection)
        {
          if (!possibleIntersectionMovements.Contains(movement))
          {
            possibleIntersectionMovements.Add(movement);
          }
        }
      }

      if (dictionaryOfTestToggles[FlagType.NoData.ToString()] && m_DataState == DataState.Empty)
      {
        string information = "Count contains no data in any bank";
        if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6, 2) + FlagType.NoData + information)))
        {
          string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[
              int.Parse(m_Data.Tables[0].TableName[m_Data.Tables[0].TableName.Length - 1].ToString())];
          m_Flags.Add(new Flag(FlagType.NoData, this,
            bvt, information));
        }
      }
      else
      {
        // Run Tests
        if (dictionaryOfTestToggles[FlagType.EmptyInterval.ToString()])
        {
          CheckEmptyIntervals();
        }
        if (dictionaryOfTestToggles[FlagType.ImpossibleMovement.ToString()])
        {
          CheckImpossibleMovements(possibleIntersectionMovements, dictionaryOfTestToggles["IgnoreBikeImpossibleMovements"]);
        }
        if (dictionaryOfTestToggles[FlagType.NoVehicleWarning.ToString()])
        {
          CheckPossibleMovementsAreNotEmpty(possibleIntersectionMovements);
        }
        if (dictionaryOfTestToggles[FlagType.EmptyInterval.ToString()])
        {
          CheckIntervalsByRange(possibleIntersectionMovements,
            dictionaryOfTestToggles[FlagType.LowInterval.ToString()],
            dictionaryOfTestToggles[FlagType.HighInterval.ToString()]);
        }
        if (dictionaryOfTestToggles[FlagType.LowHeavies.ToString()])
        {
          CheckHeavies(possibleIntersectionMovements);
        }
        if (dictionaryOfTestToggles[FlagType.InappropriateRTOR.ToString()])
        {
          CheckRTOR();
        }
        if (dictionaryOfTestToggles[FlagType.ImpossibleUTurn.ToString()])
        {
          CheckUTurn();
        }
      }
    }

    #region Tests

    private void CheckEmptyIntervals()
    {
      List<int> banksToUse =
        m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(
            x => x.Value != "Bicycles"
              && x.Value != "FHWAPedsBikes"
              && x.Value != "NOT USED")
            .Select(x => x.Key).ToList();

      for (int r = 0; r < m_Data.Tables[0].Rows.Count; r++)
      {
        int sum = 0;
        foreach (int bank in banksToUse)
        {
          for (int i = 1; i < m_Data.Tables[bank].Rows[r].ItemArray.Length; i++)
          {
            sum += int.Parse(m_Data.Tables[bank].Rows[r].ItemArray[i].ToString());
          }
        }
        if (sum == 0)
        {
          string intervalTime = m_Data.Tables[0].Rows[r].ItemArray[0].ToString();
          string information = "0 vehicles in " + (intervalTime) + " interval";
          if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.EmptyInterval + information + intervalTime)))
          {
            m_Flags.Add(new Flag(FlagType.EmptyInterval, this, m_ParentIntersection.m_ParentProject.m_BankDictionary[0],
              information, "", Convert.ToString(intervalTime)));
          }
        }
      }

    }

    /// <summary>
    /// Checks impossible movements in all banks.  Skips bicycles if ignoreImpossibleBikes is checked.
    /// </summary>
    /// <param name="possibleIntersectionMovements"></param>
    /// <param name="ignoreImpossibleBikes"></param>
    private void CheckImpossibleMovements(List<string> possibleIntersectionMovements, bool ignoreBikesChecked)
    {
      int[] bikeBank = m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(x => x.Value == "Bicycles" || x.Value == "FHWAPedsBikes").Select(x => x.Key).ToArray();

      List<int> indices = new List<int> { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15 };
      List<int> pedIndices = new List<int> { 4, 8, 12, 16 };
      for (int i = 0; i < m_Data.Tables.Count; i++)
      {
        foreach (DataRow row in m_Data.Tables[i].Rows)
        {
          //check vehicle columns unless this is a bike bank and we are ignoring bike impossible movements
          if (!ignoreBikesChecked || !bikeBank.Contains(i))
          {
            foreach (int index in indices)
            {
              string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][index];
              if (!m_ParentIntersection.m_MovementsInThisIntersection.Contains(movement))
              {
                if (Convert.ToInt32(row[movement]) != 0)
                {
                  //impossible movement found
                  string information = Convert.ToString(row[movement]) + " " + movement + " in "
                                         + row.Table;
                  if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.ImpossibleMovement + information
                            + row[0])))
                  {
                    string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[i];
                    m_Flags.Add(new Flag(FlagType.ImpossibleMovement, this,
                      bvt, information, movement, Convert.ToString(row[0])));
                  }
                }

              }
            }
          }
          foreach (int index in pedIndices)
          {
            string baseMovement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0][index];
            string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[i][index];
            switch (movement.Split('B')[1].Trim())
            {
              case "P":
                break;
              case "RT":
              case "RP":
              case "RH":
                string convertToRight = movement.Split('B')[0] + "BR";
                if (!m_ParentIntersection.m_MovementsInThisIntersection.Contains(convertToRight))
                {
                  if (Convert.ToInt32(row[movement]) != 0)
                  {
                    string information = Convert.ToString(row[movement]) + " " + movement + " in "
                                           + row.Table;
                    if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.ImpossibleMovement + information
                              + row[0])))
                    {
                      string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[
                          int.Parse(m_Data.Tables[i].TableName[m_Data.Tables[i].TableName.Length - 1].ToString())];
                      m_Flags.Add(new Flag(FlagType.ImpossibleMovement, this,
                        bvt, information, movement, Convert.ToString(row[0])));
                    }
                  }
                }

                break;
              case "U":
              case "UP":
              case "UH":
                string convertToBase = movement.Split('B')[0] + "BU";
                if (!m_ParentIntersection.m_MovementsInThisIntersection.Contains(convertToBase))
                {
                  if (Convert.ToInt32(row[movement]) != 0)
                  {
                    string information = Convert.ToString(row[movement]) + " " + movement + " in "
                                           + row.Table;
                    if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.ImpossibleMovement + information
                              + row[0])))
                    {
                      string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[
                          int.Parse(m_Data.Tables[i].TableName[m_Data.Tables[i].TableName.Length - 1].ToString())];
                      m_Flags.Add(new Flag(FlagType.ImpossibleMovement, this,
                        bvt, information, movement, Convert.ToString(row[0])));
                    }
                  }
                }
                break;
              case "-":
                if (Convert.ToInt32(row[movement]) != 0)
                {
                  string information = Convert.ToString(row[movement]) + " " + movement + " in "
                                         + row.Table;
                  if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.ImpossibleMovement + information
                            + row[0])))
                  {
                    string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[
                        int.Parse(m_Data.Tables[i].TableName[m_Data.Tables[i].TableName.Length - 1].ToString())];
                    m_Flags.Add(new Flag(FlagType.ImpossibleMovement, this,
                      bvt, information, movement, Convert.ToString(row[0])));
                  }
                }
                break;
            }
          }
        }
      }
    }

    /// <summary>
    /// This test looks for places where possible movements contain no data despite being marked as possible.
    /// </summary>
    /// <param name="possibleIntersectionMovements"></param>
    private void CheckPossibleMovementsAreNotEmpty(List<string> possibleIntersectionMovements)
    {
      List<int> indices = new List<int> { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15 };
      List<int> banksToUse =
        m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(
            x => x.Value != "Bicycles"
              && x.Value != "FHWAPedsBikes"
              && x.Value != "NOT USED")
            .Select(x => x.Key).ToList();

      if (banksToUse == null || banksToUse.Count < 1)
      {
        return;
      }
      //only check movements that are in this intersection
      foreach (int index in indices.Where(x => m_ParentIntersection.m_MovementsInThisIntersection.Contains(possibleStandardMovements[x - 1])))
      {
        int sum = 0;
        foreach (int bankIndex in banksToUse)
        {
          string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[bankIndex][index];
          foreach (DataRow row in m_Data.Tables[bankIndex].Rows)
          {
            sum += Convert.ToInt32(row[movement]);
          }
          
        }
        if (sum == 0)
        {
          string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0][index];
          string information = movement + " is possible, but the movement total for all vehicles is 0.";
          if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.NoVehicleWarning + information)))
          {
            string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[0];
            m_Flags.Add(new Flag(FlagType.NoVehicleWarning, this, bvt, information, movement));
          }
        }
      }
    }

    /// <summary>
    /// This test checks intervals in a movement to be sure they are not outside the average for the movement by more than predefined percentages.
    /// </summary>
    /// <param name="possibleIntersectionMovements"></param>
    private void CheckIntervalsByRange(List<string> possibleIntersectionMovements, bool checkLow, bool checkHigh)
    {
      double lowerThreshold = m_ParentIntersection.m_ParentProject.m_Prefs.m_LowInterval;
      double higherThreshold = m_ParentIntersection.m_ParentProject.m_Prefs.m_HighInterval;
      int intervalMinimum = m_ParentIntersection.m_ParentProject.m_Prefs.m_IntervalMin;

      List<int> indices = new List<int> { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15 };

      List<int> banksToUse =
        m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(
            x => x.Value != "Bicycles"
              && x.Value != "FHWAPedsBikes"
              && x.Value != "NOT USED")
            .Select(x => x.Key).ToList();

      TMCProject proj = m_ParentIntersection.m_ParentProject;
      Dictionary<DateTime, Dictionary<string, int>> hourDataSums = new Dictionary<DateTime, Dictionary<string, int>>();
      Dictionary<DateTime, int> hourIntervalCount = new Dictionary<DateTime, int>();

      DateTime time = DateTime.Parse(m_StartTime);
      DateTime endTime = DateTime.Parse(m_EndTime);
      if (endTime <= time)
      {
        endTime = endTime.AddDays(1);
      }

      //Add the first hour into the dictionary, which might be a partial.
      Dictionary<string, int> dictionaryWithMovements = new Dictionary<string, int>();
      foreach (string movement in proj.m_ColumnHeaders[0])
      {
        if (movement == "Time")
        {
          continue;
        }
        dictionaryWithMovements.Add(movement, 0);
      }
      hourDataSums.Add(time, dictionaryWithMovements);
      hourIntervalCount.Add(time, 0);

      //Line time up to the next hour
      var minute = time.Minute;
      time = time.AddMinutes(60 - time.Minute);

      //Do this until time is past end time, for that do nothing.  These are just dictionary entry keys, all the rest should be on the hour.
      while (time < endTime)
      {
        Dictionary<string, int> tempDictionary = new Dictionary<string, int>();
        foreach (var movement in proj.m_ColumnHeaders[0])
        {
          if (movement == "Time")
          {
            continue;
          }
          tempDictionary.Add(movement, 0);
        }
        hourDataSums.Add(time, tempDictionary);
        hourIntervalCount.Add(time, 0);
        time = time.AddMinutes(60);
      }

      // Collect the sums by hour
      foreach (var m in indices)
      {
        if (m_ParentIntersection.m_MovementsInThisIntersection.Contains(proj.m_ColumnHeaders[0][m]))
        {
          for (int r = 0; r < m_NumIntervals; r++)
          {
            //Determine which entry in the main dictionary this row belongs to by subtracting 5 minuts and checking to see if the key is there.
            DateTime entryForThisRow = DateTime.Parse(m_Data.Tables[0].Rows[r][0].ToString());
            for (int h = 0; h < 12; h++)
            {
              if (hourDataSums.ContainsKey(entryForThisRow))
              {
                break;
              }
              entryForThisRow = entryForThisRow.AddMinutes(-1 * 5);
            }
            //Now add all the sums to the appropriate place, including the totals dictionary.
            for (int t = 0; t < banksToUse.Count; t++)
            {
              int cell = int.Parse(m_Data.Tables[banksToUse[t]].Rows[r][proj.m_ColumnHeaders[banksToUse[t]][m]].ToString());
              hourDataSums[entryForThisRow][proj.m_ColumnHeaders[0][m]] += cell;
            }
            hourIntervalCount[entryForThisRow]++;
          }
        }
      }
      int movementsUsed = indices.Where(x => m_ParentIntersection.m_MovementsInThisIntersection.Contains(proj.m_ColumnHeaders[0][x])).ToArray().Length;

      foreach (var hour in hourDataSums)
      {
        hourIntervalCount[hour.Key] = hourIntervalCount[hour.Key] / movementsUsed;
      }

      // Run the test
      foreach (int index in indices)
      {
        string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0][index];
        if (m_ParentIntersection.m_MovementsInThisIntersection.Contains(movement))
        {
          for (int r = 0; r < m_NumIntervals; r++)
          {
            //Determine which entry in the main dictionary this row belongs to by subtracting 5 minuts and checking to see if the key is there.
            DateTime entryForThisRow = DateTime.Parse(m_Data.Tables[0].Rows[r][0].ToString());
            for (int h = 0; h < 12; h++)
            {
              if (hourDataSums.ContainsKey(entryForThisRow))
              {
                break;
              }
              entryForThisRow = entryForThisRow.AddMinutes(-1 * 5);
            }

            //Check to see if this hour and movement meet the volume minimum for the test.
            if (hourDataSums[entryForThisRow][movement] * (12 / hourIntervalCount[entryForThisRow]) >= intervalMinimum)
            {
              int average = (int)Math.Round((double)hourDataSums[entryForThisRow][movement] / (double)hourIntervalCount[entryForThisRow]);

              int thisCellSum = 0;
              foreach (int bank in banksToUse)
              {
                thisCellSum += Convert.ToInt32(m_Data.Tables[bank].Rows[r][movement]);
              }
              if (checkLow && thisCellSum < (average * lowerThreshold))
              {
                string information = thisCellSum + " vehicles in " + movement + " (" + entryForThisRow.ToString("HH:mm") + " Average: " + average + ")";
                if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.LowInterval + information + m_Data.Tables[0].Rows[r].ItemArray[0])))
                {
                  m_Flags.Add(new Flag(FlagType.LowInterval, this, m_ParentIntersection.m_ParentProject.m_BankDictionary[0], information, movement, Convert.ToString(m_Data.Tables[0].Rows[r].ItemArray[0])));
                }
              }
              if (checkHigh && thisCellSum > (average * higherThreshold))
              {
                string information = thisCellSum + " vehicles in " + movement + " (" + entryForThisRow.ToString("HH:mm") + " Average: " + average + ")";
                if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.HighInterval + information + m_Data.Tables[0].Rows[r].ItemArray[0])))
                {
                  m_Flags.Add(new Flag(FlagType.HighInterval, this, m_ParentIntersection.m_ParentProject.m_BankDictionary[0], information, movement, Convert.ToString(m_Data.Tables[0].Rows[r].ItemArray[0])));
                }

              }
            }
          }

        }
      }
    }

    /// <summary>
    /// This test checks the percentage of heavies versus the expected minimum percentage above a given threshold.  The values are stored in settings.
    /// </summary>
    /// <param name="possibleIntersectionMovements"></param>
    private void CheckHeavies(List<string> possibleIntersectionMovements)
    {
      if (m_ParentIntersection.m_ParentProject.m_HeavyBanks.Count <= 0)
      {
        return;
      }

      List<int> passengerBanks =
        m_ParentIntersection.m_ParentProject.m_BankDictionary.Where(
          x => x.Value == "Passenger"
            || x.Value == "FHWA1"
            || x.Value == "FHWA2"
            || x.Value == "FHWA3"
            || x.Value == "FHWA1_2_3")
            .Select(x => x.Key).ToList();

      List<int> indices = new List<int> { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15 };
      double percentAcceptable = m_ParentIntersection.m_ParentProject.m_Prefs.m_LowHeavies;
      int lowerVehicleTotalThreshold = m_ParentIntersection.m_ParentProject.m_Prefs.m_HeaviesMin * (m_NumIntervals / 12);

      foreach (int index in indices)
      {
        string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[0][index];
        if (m_ParentIntersection.m_MovementsInThisIntersection.Contains(movement))
        {
          int sum = 0;
          int sumHeavies = 0;
          foreach (int bank in passengerBanks)
          {
            movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[bank][index];
            foreach (DataRow row in m_Data.Tables[bank].Rows)
            {
              if (!String.IsNullOrEmpty(Convert.ToString(row[0])))
              {
                sum += Convert.ToInt32(row[movement]);
              }
            }
          }
          foreach (var heavyBank in m_ParentIntersection.m_ParentProject.m_HeavyBanks)
          {
            movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[heavyBank][index];
            foreach (DataRow row in m_Data.Tables[heavyBank].Rows)
            {
              if (!String.IsNullOrEmpty(Convert.ToString(row[0])))
              {
                sumHeavies += Convert.ToInt32(row[movement]);
              }
            }
          }

          movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[m_ParentIntersection.m_ParentProject.m_HeavyBanks[0]][index];
          if (sum > lowerVehicleTotalThreshold && sumHeavies < ((sum + sumHeavies) * (percentAcceptable * .01)))
          {
            string information = movement + ": Total Heavies: " + sumHeavies + "  Total Vehicles: " + (sum + sumHeavies) + " (" + (((sumHeavies / (float)(sum + sumHeavies)) * 100).ToString("n2") + "%)");
            if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.LowHeavies + information)))
            {
              m_Flags.Add(new Flag(FlagType.LowHeavies, this, "SumOfAllHeavies", information, movement));
            }
          }
        }
      }
    }

    /// <summary>
    /// This test checks for RTOR data in the typical RTOR ped bank when RTOR is checked OFF for the project and the bank was marked as NA.
    /// </summary>
    private void CheckRTOR()
    {
      List<int> indices = new List<int> { 4, 8, 12, 16 };
      if (!m_ParentIntersection.m_ParentProject.m_IsRTOR && m_ParentIntersection.m_ParentProject.m_PedBanks[1] == PedColumnDataType.NA)
      {
        DataTable bank1 = m_Data.Tables[1];
        foreach (DataRow row in bank1.Rows)
        {
          foreach (int index in indices)
          {
            string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[1][index];
            if (Convert.ToInt32(row[index]) != 0)
            {
              string information = Convert.ToString(row[index]) + " key presses in Bank 1 " + movement;
              if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.InappropriateRTOR + information
                          + row[0])))
              {
                string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[1];
                m_Flags.Add(new Flag(FlagType.InappropriateRTOR, this, bvt, information, movement, Convert.ToString(row[0])));
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// This test looks for data in the typical U-Turn ped bank when U-Turns are checked OFF for the project and the bank was marked as NA
    /// 01-APR-2017 It appears the name of the flag (ImpossibleUTurn) is slightly misleading as it is not necessarily impossible, but just not 
    /// intended to be counted in the project.  If it were changed, version controlling would be necessary.
    /// </summary>
    private void CheckUTurn()
    {
      List<int> indices = new List<int> { 4, 8, 12, 16 };
      if (!m_ParentIntersection.m_ParentProject.m_IsUTurn && m_ParentIntersection.m_ParentProject.m_PedBanks[2] == PedColumnDataType.NA)
      {
        DataTable bank2 = m_Data.Tables[2];
        foreach (DataRow row in bank2.Rows)
        {
          foreach (int index in indices)
          {
            string movement = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[2][index];
            if (Convert.ToInt32(row[index]) != 0)
            {
              string information = Convert.ToString(row[index]) + " key presses in Bank 2 " + movement;
              if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.ImpossibleUTurn + information
                          + row[0])))
              {
                string bvt = m_ParentIntersection.m_ParentProject.m_BankDictionary[1];
                m_Flags.Add(new Flag(FlagType.ImpossibleUTurn, this, bvt, information, movement, Convert.ToString(row[0])));
              }
            }
          }
        }
      }
    }

    public void CheckClassPercentageByHour(Dictionary<string, double> countPercentages)
    {
      TMCProject proj = m_ParentIntersection.m_ParentProject;
      Dictionary<DateTime, Dictionary<string, int>> hourDataSums = new Dictionary<DateTime, Dictionary<string, int>>();
      Dictionary<DateTime, int> hourTotals = new Dictionary<DateTime, int>();

      DateTime time = DateTime.Parse(m_StartTime);
      DateTime endTime = DateTime.Parse(m_EndTime);
      if (endTime <= time)
      {
        endTime = endTime.AddDays(1);
      }

      //Add the first hour into the dictionary, which might be a partial.
      Dictionary<string, int> dictionaryWithBanks = new Dictionary<string, int>();
      foreach (var bank in proj.m_Banks)
      {
        dictionaryWithBanks.Add(bank, 0);
      }
      hourDataSums.Add(time, dictionaryWithBanks);
      hourTotals.Add(time, 0);

      //Line time up to the next hour
      var minute = time.Minute;
      time = time.AddMinutes(60 - time.Minute);

      //Do this until time is past end time, for that do nothing.  These are just dictionary entry keys, all the rest should be on the hour.
      while (time < endTime)
      {
        Dictionary<string, int> tempDictionary = new Dictionary<string, int>();
        foreach (var bank in proj.m_Banks)
        {
          tempDictionary.Add(bank, 0);
        }
        hourDataSums.Add(time, tempDictionary);
        hourTotals.Add(time, 0);
        time = time.AddMinutes(60);
      }

      // Collect the sums by hour
      for (int t = 0; t < m_Data.Tables.Count; t++)
      {
        foreach (DataRow row in m_Data.Tables[t].Rows)
        {
          //Determine which entry in the main dictionary this row belongs to by subtracting 5 minuts and checking to see if the key is there.
          DateTime entryForThisRow = DateTime.Parse(row.ItemArray[0].ToString());
          for (int r = 0; r < 12; r++)
          {
            if (hourDataSums.ContainsKey(entryForThisRow))
            {
              break;
            }
            entryForThisRow = entryForThisRow.AddMinutes(-1 * 5);
          }
          //Now add all the sums to the appropriate place, including the totals dictionary.
          for (int i = 1; i < row.ItemArray.Length; i++)
          {
            int cell = int.Parse(row.ItemArray[i].ToString());
            hourDataSums[entryForThisRow][proj.m_BankDictionary[t]] += cell;
            hourTotals[entryForThisRow] += cell;
          }
        }
      }

      Dictionary<string, float> thresholdMap = new Dictionary<string, float>
        {
          {"FHWA1", proj.m_Prefs.m_fhwa1}, 
          {"FHWA2", proj.m_Prefs.m_fhwa2}, 
          {"FHWA3", proj.m_Prefs.m_fhwa3}, 
          {"FHWA4", proj.m_Prefs.m_fhwa4}, 
          {"FHWA5", proj.m_Prefs.m_fhwa5}, 
          {"FHWA6", proj.m_Prefs.m_fhwa6}, 
          {"FHWA7", proj.m_Prefs.m_fhwa7}, 
          {"FHWA8", proj.m_Prefs.m_fhwa8}, 
          {"FHWA9", proj.m_Prefs.m_fhwa9}, 
          {"FHWA10", proj.m_Prefs.m_fhwa10}, 
          {"FHWA11", proj.m_Prefs.m_fhwa11}, 
          {"FHWA12", proj.m_Prefs.m_fhwa12}, 
          {"FHWA13", proj.m_Prefs.m_fhwa13}, 
          {"FHWA6through13", proj.m_Prefs.m_fhwa6_13},
        };

      // Run the flag test
      foreach (var hour in hourDataSums)
      {
        foreach (var bankClass in hour.Value)
        {
          double percentage = hourTotals[hour.Key] > 0 ? (double)bankClass.Value / (double)hourTotals[hour.Key] * 100 : 0;
          float thresholdToUse = thresholdMap.ContainsKey(bankClass.Key) ? thresholdMap[bankClass.Key] : 5;
          if (Math.Abs(percentage - countPercentages[bankClass.Key]) > thresholdToUse)
          {
            string information = string.Format("{0} hour  {1}: {2}%  Count: {3}%", bankClass.Key, hour.Key.ToString("HH:mm"), percentage.ToString("n2"),
                countPercentages[bankClass.Key].ToString("n2"));
            if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.CountClassificationDiscrepancy + information)))
            {
              m_Flags.Add(new Flag(FlagType.HourClassificationDiscrepancy, this, bankClass.Key, information, "", hour.Key.ToString("HH:mm")));
            }
          }
        }
      }
    }

    public void CheckClassPercentageByFile(Dictionary<string, double> countPercentages)
    {
      TMCProject proj = m_ParentIntersection.m_ParentProject;

      Dictionary<string, float> thresholdMap = new Dictionary<string, float>
        {
          {"FHWA1", proj.m_Prefs.m_fhwa1}, 
          {"FHWA2", proj.m_Prefs.m_fhwa2}, 
          {"FHWA3", proj.m_Prefs.m_fhwa3}, 
          {"FHWA4", proj.m_Prefs.m_fhwa4}, 
          {"FHWA5", proj.m_Prefs.m_fhwa5}, 
          {"FHWA6", proj.m_Prefs.m_fhwa6}, 
          {"FHWA7", proj.m_Prefs.m_fhwa7}, 
          {"FHWA8", proj.m_Prefs.m_fhwa8}, 
          {"FHWA9", proj.m_Prefs.m_fhwa9}, 
          {"FHWA10", proj.m_Prefs.m_fhwa10}, 
          {"FHWA11", proj.m_Prefs.m_fhwa11}, 
          {"FHWA12", proj.m_Prefs.m_fhwa12}, 
          {"FHWA13", proj.m_Prefs.m_fhwa13}, 
          {"FHWA6through13", proj.m_Prefs.m_fhwa6_13},
        };

      foreach (var dataFile in m_AssociatedDataFiles)
      {
        //Check to see the file will quality for the test, if so, add it to the dictionary
        var fileIntervals = QualifyingNumberOfDataFileIntervals(dataFile);
        if (fileIntervals.Count > 0)
        {
          Dictionary<string, int> dataSums = new Dictionary<string, int>();
          int fileTotal = 0;
          foreach (var bank in proj.m_Banks)
          {
            dataSums.Add(bank, 0);
          }

          //We know the rows to sum, so we iterate over the rows first this time.
          foreach (var timeAndMovement in fileIntervals)
          {
            for (int t = 0; t < m_Data.Tables.Count - 1; t++)
            {
              int cell = int.Parse(m_Data.Tables[t].Rows[timeAndMovement.Item1].ItemArray[timeAndMovement.Item2].ToString());
              dataSums[proj.m_BankDictionary[t]] += cell;
              fileTotal += cell;
            }
          }

          //Run the flag test now since we can use the number of interval information.
          foreach (var bankClass in dataSums)
          {
            double percentage = fileTotal > 0 ? (double)bankClass.Value / (double)fileTotal * 100 : 0;
            float thresholdToUse = thresholdMap.ContainsKey(bankClass.Key) ? thresholdMap[bankClass.Key] : 5;
            if (Math.Abs(percentage - countPercentages[bankClass.Key]) > thresholdToUse)
            {
              string information = string.Format("{0}  {1}: {2}%  Count: {3}%  ({4} intervals included in test).", bankClass.Key, dataFile.TidyName, percentage.ToString("n2"), countPercentages[bankClass.Key].ToString("n2"), fileIntervals.Count);

              if (m_Flags.All(f => f.m_Key != (m_Id.Substring(6) + FlagType.CountClassificationDiscrepancy + information)))
              {
                m_Flags.Add(new Flag(FlagType.FileDataClassificationDiscrepancy, this, bankClass.Key, information));
              }
            }
          }
        }
      }
    }

    private List<Tuple<int, int>> QualifyingNumberOfDataFileIntervals(DataFile file)
    {
      // List of Row Number, ColumnHeaders
      List<Tuple<int, int>> fullIntervals = new List<Tuple<int, int>>();
      for (int i = 0; i < m_Data.Tables[0].Rows.Count; i++)
      {
        for (int k = 1; k < m_Data.Tables[0].Rows[i].ItemArray.Length; k++)
        {
          bool thisOneCounts = true;
          for (int j = 0; j < m_Data.Tables.Count - 1; j++)
          {
            if (!thisOneCounts)
            {
              break;
            }
            string columnHeader = m_ParentIntersection.m_ParentProject.m_ColumnHeaders[j][k];
            string key = file.MakeDataCellKey(m_Data.Tables[j].TableName, columnHeader, m_Data.Tables[j].Rows[i].ItemArray[0].ToString());
            if (!file.DataCellMap.Contains(key))
            {
              thisOneCounts = false;
              break;
            }
          }
          if (thisOneCounts)
          {
            fullIntervals.Add(new Tuple<int, int>(i, k));
          }
        }
      }

      return fullIntervals;
    }

    #endregion Tests

    public void RunDataState()
    {
      foreach (DataTable table in m_Data.Tables)
      {
        foreach (DataRow row in table.Rows)
        {
          for (int j = 1; j < row.ItemArray.Length; j++)
          {
            if (Int16.Parse(row.ItemArray[j].ToString()) > 0)
            {
              m_DataState = DataState.Partial;
              return;
            }
          }
        }
      }
      m_DataState = DataState.Empty;
    }

    public static string AddXMinutes(string startTime, int minutesToAdd)
    {
      int hour = int.Parse(startTime.Substring(0, 2));
      int minutes = int.Parse(startTime.Substring(3, 2)) + minutesToAdd;
      StringBuilder newTime = new StringBuilder();

      if (minutes >= 60)
      {
        minutes = minutes - 60;
        hour++;
        if (hour > 23)
        {
          hour = hour - 24;
        }
      }
      if (hour < 10)
      {
        newTime.Append("0");
      }
      newTime.Append(hour + ":");

      if (minutes < 10)
      {
        newTime.Append("0");
      }
      newTime.Append(minutes);

      return newTime.ToString();
    }
  }
}
