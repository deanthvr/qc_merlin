using QCCommon.QCData.DataModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Serialization;

namespace AppMerlin
{
  public class TubeCount
  {
    //Property backing field
    private DateTime m_StartTime;
    public List<string> classes;

    public const string INTERVAL_COLUMN = "Interval";
    public const string TOTAL_COLUMN = "Total";
    public const string VOLUME = "Volume";
    public const string UNCLASSIFIED = "Unclassified";
    public string m_SiteCode;
    //Property backing field
    private int m_Duration;
    public DataSet m_Data;
    public SurveyType m_Type;
    [XmlIgnore]
    public TubeSite m_ParentTubeSite;
    public IntervalLength m_IntervalSize;

    #region Properties

    public DateTime StartTime
    {
      get
      {
        return m_StartTime;
      }
      set
      {
        if (m_StartTime != value && m_StartTime != DateTime.MinValue)
        {
          //when changed (except when deserializing), drop existing data
          m_Data = new DataSet();
        }
        m_StartTime = value;
      }
    }

    [XmlIgnore]
    public DateTime EndTime
    {
      get
      {
        return StartTime.AddHours(m_Duration);
      }
    }

    public int Duration
    {
      get
      {
        return m_Duration;
      }
      set
      {
        if (m_Duration != value && m_Duration != 0)
        {
          //when changed, drop existing data
          m_Data = new DataSet();
        }
        m_Duration = value;
      }
    }

    public SurveyType SurveyType
    {
      get
      {
        return m_Type;
      }
      set
      {
        if (m_Type != value && value != SurveyType.Unknown)
        {
          //when changed (except when deserializing), drop existing data
          m_Data = new DataSet();
        }
        m_Type = value;
      }
    }

    #endregion

    #region Constructors

    public TubeCount()
    {
      classes = new List<string>();
    }

    public TubeCount(string siteCode, SurveyType type, DateTime startTime, int duration, TubeSite site)
    {
      m_SiteCode = siteCode;
      m_Type = type;
      m_StartTime = startTime;
      m_ParentTubeSite = site;
      m_Duration = duration;
      classes = new List<string>();
      m_ParentTubeSite = site;
      m_IntervalSize = IntervalLength.Fifteen;
      m_Data = new DataSet();
    }

    #endregion

    #region Public Methods

    public void ImportAsciiData(List<string> lines, string direction, string fileDataType)
    {
      //DetermineIntervalLength(lines);

      if (m_Data == null || m_Data.Tables.Count == 0)
      {
        SetupData();
      }

      string dataColumn = ConvertDirection(direction);
      foreach (string fileLine in lines)
      {
        if (!String.IsNullOrEmpty(fileLine))
        {
          List<string> dataLines = fileLine.Split(':').ToList();
          switch (dataLines[0])
          {
            case "File Name":
            case "Start Date":
            case "Start Time":
            case "Site Code":
              break;
            default:
              var parsedLine = fileLine.Split(',');
              if (!IsFileLineData(parsedLine))
              {
                continue;
              }
              var intervalTime = MakeDateTime(parsedLine[0], parsedLine[1]);

              bool intervalFound = false;
              for (int r = 0; r < m_Data.Tables[0].Rows.Count; r++)
              {
                if (intervalFound) break;
                if (Convert.ToDateTime(m_Data.Tables[0].Rows[r].ItemArray[0]) == intervalTime)
                {
                  switch (fileDataType)
                  {
                    case "Speed":
                    case "Volume":
                      InsertSpeedOrVolumeDataLine(r, dataColumn, parsedLine);
                      break;
                    case "Class":
                      InsertClassDataLine(r, dataColumn, parsedLine);
                      break;
                  }
                  intervalFound = true;
                }
                //TODO: The interval was not in the time period.
              }
              break;
          }
        }
      }
    }

    public void FillDataFromDataBaseTable(DataTable table)
    {
      if (m_Data == null || m_Data.Tables.Count == 0)
      {
        SetupData();
      }

      foreach (DataRow row in table.Rows)
      {
        var intervalIndex = (int)((DateTime.Parse(row["CountDateTime"].ToString()) - m_StartTime).TotalMinutes) / ((int)m_IntervalSize);
        var classIndex = m_Type == AppMerlin.SurveyType.TubeVolumeOnly ? 0 : int.Parse(row["VehicleClassIndex"].ToString()) - 1;
        var direction = row["Direction"].ToString();
        string dataColumn = ConvertDirection(direction);
        var dataColumnName = m_Type == AppMerlin.SurveyType.TubeVolumeOnly ? "VolumeCount" : "VehicleCount";
        m_Data.Tables[classIndex].Rows[intervalIndex][dataColumn] = int.Parse(row[dataColumnName].ToString());
      }
    }

    public void FillDataFromVolumeObject(QCDataTubeVolume data)
    {
      if (m_Type != SurveyType.TubeVolumeOnly)
      {
        throw new Exception($"Attempting to fill tube count of type '{m_Type.ToString()}' with volume-only tube data.");
      }
      if ((int)m_IntervalSize != data.IntervalType)
      {
        throw new Exception($"Attempting to fill tube count that uses {(int)m_IntervalSize}-minute intervals with data that is in {data.IntervalType}-minute intervals.");
      }

      if (m_Data == null || m_Data.Tables.Count == 0)
      {
        SetupData();
      }

      foreach (var interval in (data.Data.EB ?? new List<VolumeIntervalDirectionData>()).Select(x => new { Direction = "EB", CountDateTime = x.CountDateTime, VolumeCount = x.VolumeCount })
                       .Concat((data.Data.WB ?? new List<VolumeIntervalDirectionData>()).Select(x => new { Direction = "WB", CountDateTime = x.CountDateTime, VolumeCount = x.VolumeCount })
                       .Concat((data.Data.NB ?? new List<VolumeIntervalDirectionData>()).Select(x => new { Direction = "NB", CountDateTime = x.CountDateTime, VolumeCount = x.VolumeCount })
                       .Concat((data.Data.SB ?? new List<VolumeIntervalDirectionData>()).Select(x => new { Direction = "SB", CountDateTime = x.CountDateTime, VolumeCount = x.VolumeCount })))))
      {
        var intervalIndex = (int)((interval.CountDateTime - m_StartTime).TotalMinutes) / ((int)m_IntervalSize);
        var classIndex = 0;
        var direction = interval.Direction;
        string dataColumn = ConvertDirection(direction);
        m_Data.Tables[classIndex].Rows[intervalIndex][dataColumn] = interval.VolumeCount;
      }
    }

    public void FillDataFromClassObject(QCDataTubeClass data)
    {
      if (m_Type != SurveyType.TubeClass && m_Type != SurveyType.TubeSpeedClass)
      {
        throw new Exception($"Attempting to fill tube count of type '{m_Type.ToString()}' with class tube data.");
      }
      if ((int)m_IntervalSize != data.IntervalType)
      {
        throw new Exception($"Attempting to fill tube count that uses {(int)m_IntervalSize}-minute intervals with data that is in {data.IntervalType}-minute intervals.");
      }

      if (m_Data == null || m_Data.Tables.Count == 0)
      {
        SetupData();
      }

      foreach (var interval in (data.Data.EB ?? new List<ClassIntervalDirectionData>()).Select(x => new Tuple<string, ClassIntervalDirectionData>("EB", x))
                       .Concat((data.Data.WB ?? new List<ClassIntervalDirectionData>()).Select(x => new Tuple<string, ClassIntervalDirectionData>("WB", x))
                       .Concat((data.Data.NB ?? new List<ClassIntervalDirectionData>()).Select(x => new Tuple<string, ClassIntervalDirectionData>("NB", x))
                       .Concat((data.Data.SB ?? new List<ClassIntervalDirectionData>()).Select(x => new Tuple<string, ClassIntervalDirectionData>("SB", x))))))
      {
        var intervalIndex = (int)((interval.Item2.CountDateTime - m_StartTime).TotalMinutes) / ((int)m_IntervalSize);
        var direction = interval.Item1;
        string dataColumn = ConvertDirection(direction);

        m_Data.Tables[0].Rows[intervalIndex][dataColumn] = interval.Item2.Class1;
        m_Data.Tables[1].Rows[intervalIndex][dataColumn] = interval.Item2.Class2;
        m_Data.Tables[2].Rows[intervalIndex][dataColumn] = interval.Item2.Class3;
        m_Data.Tables[3].Rows[intervalIndex][dataColumn] = interval.Item2.Class4;
        m_Data.Tables[4].Rows[intervalIndex][dataColumn] = interval.Item2.Class5;
        m_Data.Tables[5].Rows[intervalIndex][dataColumn] = interval.Item2.Class6;
        m_Data.Tables[6].Rows[intervalIndex][dataColumn] = interval.Item2.Class7;
        m_Data.Tables[7].Rows[intervalIndex][dataColumn] = interval.Item2.Class8;
        m_Data.Tables[8].Rows[intervalIndex][dataColumn] = interval.Item2.Class9;
        m_Data.Tables[9].Rows[intervalIndex][dataColumn] = interval.Item2.Class10;
        m_Data.Tables[10].Rows[intervalIndex][dataColumn] = interval.Item2.Class11;
        m_Data.Tables[11].Rows[intervalIndex][dataColumn] = interval.Item2.Class12;
        m_Data.Tables[12].Rows[intervalIndex][dataColumn] = interval.Item2.Class13;
        m_Data.Tables[13].Rows[intervalIndex][dataColumn] = interval.Item2.Class14;
      }
    }


    public bool ContainsInterval(DateTime interval)
    {
      foreach (DataTable dt in m_Data.Tables)
      {
        if (dt.Rows.Contains(DTHelper.ConvertDT(interval)))
        {
          return true;
        }
      }
      return false;
    }

    public int GetVolume(BalancingInsOuts connection, DateTime firstInterval, TimeSpan length, bool includeUnclassed, List<BankVehicleTypes> FHWAClasses = null)
    {
      //return a volume of zero under the following conditions
      if (firstInterval < StartTime ||                           //desired first interval is before count's first interval
        firstInterval >= EndTime ||                             //desired first interval is at or after count's end time
        length == TimeSpan.Zero ||                              //desired length of time to get volume from is zero
        (DateTime)(firstInterval + length) > EndTime            //desired end time of period to get volume from is after count's end time
        || firstInterval.Minute % (int)m_IntervalSize != 0      //desired first interval does not align with an interval for this count
        || length.Minutes % (int)m_IntervalSize != 0)           //desired length of time to get volume from is not a multiple of this count's interval size
      {
        return 0;
      }

      int numIntervals = (int)length.TotalMinutes / (int)m_IntervalSize;
      int volume = 0;
      List<DataTable> tables = new List<DataTable>();
      if (FHWAClasses == null)
      {
        if (SurveyType == SurveyType.TubeSpeed || SurveyType == SurveyType.TubeVolumeOnly)
        {
          tables.Add(m_Data.Tables[VOLUME]);
        }
        else if (SurveyType == SurveyType.TubeClass || SurveyType == SurveyType.TubeSpeedClass)
        {
          foreach (DataTable dt in m_Data.Tables)
          {
            if (dt.TableName == UNCLASSIFIED && includeUnclassed == false)
            {
              continue;
            }
            tables.Add(dt);
          }
        }
        else
        {
          throw new Exception(string.Format("Cannot get tube volume on a tube count of type {0}!", SurveyType.ToString()));
        }
      }
      else
      {
        foreach (BankVehicleTypes vehType in FHWAClasses)
        {
          if (!m_Data.Tables.Contains(vehType.ToString()))
          {
            //throw new Exception(string.Format("This tube count does not contain a table named \"{0}\".", vehType.ToString()));
            return 0;
          }
          tables.Add(m_Data.Tables[vehType.ToString()]);
        }
        if (includeUnclassed == true)
        {
          tables.Add(m_Data.Tables[UNCLASSIFIED]);
        }
      }

      //iterate over intervals
      for (int i = 0; i < numIntervals; i++)
      {
        //in the current interval, iterate over the tables
        foreach (DataTable table in tables)
        {
          DataRow interval = table.Rows.Find(DTHelper.ConvertDT(firstInterval.AddMinutes((int)m_IntervalSize * i)));
          if (interval == null)
          {
            throw new RowNotInTableException(string.Format("This tube count does not contain data in the {0} interval for the \"{1}\" class.",
                                                            firstInterval.AddMinutes((int)m_IntervalSize * i),
                                                            table.TableName));
          }
          //add the current interval and current vehicle class to the total
          volume += (int)interval[DirectionColumnNameFromConnectionEnum(connection)];
        }
      }

      return volume;
    }

    public List<DateTime> GetDatesContainingData()
    {
      List<DateTime> dates = new List<DateTime>();
      foreach (DataTable table in m_Data.Tables)
      {
        if (table.Rows.Count < 1)
        {
          continue;
        }
        DateTime dateOfCurrentInterval = DTHelper.ConvertStr(table.Rows[0][0] as string);
        DateTime dateOfLastInterval = DTHelper.ConvertStr(table.Rows[table.Rows.Count - 1][0] as string);

        do
        {
          if (!dates.Contains(dateOfCurrentInterval))
          {
            dates.Add(dateOfCurrentInterval.Date);
            dateOfCurrentInterval = dateOfCurrentInterval.Date.AddDays(1);
          }
        } while (dateOfCurrentInterval.Date <= dateOfLastInterval.Date);
        break;
      }
      return dates;
    }

    #endregion

    #region Private Helpers

    private void DetermineIntervalLength(List<string> lines)
    {
      DateTime lineA = DateTime.MinValue;
      DateTime lineB = DateTime.MinValue;
      foreach (string fileLine in lines)
      {
        if (String.IsNullOrEmpty(fileLine))
        {
          continue;
        }
        var parsedLine = fileLine.Split(',');
        if (!IsFileLineData(parsedLine))
        {
          continue;
        }
        if (lineA == DateTime.MinValue)
        {
          lineA = MakeDateTime(parsedLine[0], parsedLine[1]);
        }
        else if (lineB == DateTime.MinValue && MakeDateTime(parsedLine[0], parsedLine[1]) != lineA)
        {
          lineB = MakeDateTime(parsedLine[0], parsedLine[1]);
        }
        if (lineA != DateTime.MinValue && lineB != DateTime.MinValue)
        {
          break;
        }
      }
      var spanInMinutes = (lineB - lineA).TotalMinutes;
      switch ((int)spanInMinutes)
      {
        case 5:
          m_IntervalSize = IntervalLength.Five;
          break;
        case 30:
          m_IntervalSize = IntervalLength.Thirty;
          break;
        case 60:
          m_IntervalSize = IntervalLength.Sixty;
          break;
        default:
          m_IntervalSize = IntervalLength.Fifteen;
          break;
      }
    }

    private void SetupData()
    {
      DataSet set = new DataSet();
      classes.Clear();
      if (m_Type == SurveyType.TubeClass || m_Type == SurveyType.TubeSpeedClass)
      {
        PopulateClassSet();
      }
      else
      {
        classes.Add(VOLUME);
      }
      foreach (var @class in classes)
      {
        var table = SetupDataTable(@class);
        set.Tables.Add(table);
      }
      m_Data = set;
    }

    private DataTable SetupDataTable(string bank)
    {
      DataTable table = new DataTable(bank);
      DataColumn c = new DataColumn("Time");

      c.DataType = Type.GetType("System.String");
      c.ReadOnly = true;
      table.Columns.Add(c);
      table.PrimaryKey = new DataColumn[] { c };
      List<string> columnHeaders = new List<string> { "SB/WB", "NB/EB" };

      for (int j = 0; j < columnHeaders.Count(); j++)
      {
        string colHeader = columnHeaders[j];
        DataColumn dc = new DataColumn(colHeader);
        dc.DataType = Type.GetType("System.Int32");
        table.Columns.Add(dc);
      }

      DateTime workingTime = StartTime;
      int numIntervals = CalculateNumberOfIntervals();

      for (int k = 0; k < numIntervals; k++)
      {
        DataRow r = table.NewRow();

        r[0] = DTHelper.ConvertDT(workingTime);
        for (int m = 0; m < (columnHeaders.Count()); m++)
        {
          r[m + 1] = 0;
        }
        table.Rows.Add(r);
        workingTime = workingTime.AddMinutes((int)m_IntervalSize);
      }
      return table;
    }

    private void PopulateClassSet()
    {
      List<BankVehicleTypes> bankClasses = new List<BankVehicleTypes>()
      {
        BankVehicleTypes.FHWA1,
        BankVehicleTypes.FHWA2,
        BankVehicleTypes.FHWA3,
        BankVehicleTypes.FHWA4,
        BankVehicleTypes.FHWA5,
        BankVehicleTypes.FHWA6,
        BankVehicleTypes.FHWA7,
        BankVehicleTypes.FHWA8,
        BankVehicleTypes.FHWA9,
        BankVehicleTypes.FHWA10,
        BankVehicleTypes.FHWA11,
        BankVehicleTypes.FHWA12,
        BankVehicleTypes.FHWA13,
        BankVehicleTypes.Unclassified
      };

      classes.AddRange(bankClasses.Select(x => x.ToString()));
    }

    //private void ImportAsciiSpeedOrVolume(List<string> lines, string direction)
    //{
    //  string dataColumn = ConvertDirection(direction);
    //  foreach (string fileLine in lines)
    //  {
    //    if (fileLine != "" || fileLine != String.Empty)
    //    {
    //      List<string> dataLines = fileLine.Split(':').ToList();
    //      switch (dataLines[0])
    //      {
    //        case "File Name":
    //        case "Start Date":
    //        case "Start Time":
    //        case "Site Code":
    //          break;
    //        default:
    //          var parsedLine = fileLine.Split(',');
    //          if (!IsFileLineData(parsedLine))
    //          {
    //            continue;
    //          }
    //          var intervalTime = MakeDateTime(parsedLine[0], parsedLine[1]);
    //          int volume = 0;
    //          for (int i = 2; i < parsedLine.Length; i++)
    //          {
    //            volume += int.Parse(parsedLine[i]);
    //          }
    //          bool intervalFound = false;
    //          foreach (DataRow r in m_Data.Tables["Volume"].Rows)
    //          {
    //            if (intervalFound) break;
    //            if (Convert.ToDateTime(r.ItemArray[0]) == intervalTime)
    //            {
    //              r[dataColumn] = volume;
    //              intervalFound = true;
    //            }
    //            //TODO: The interval was not in the time period.
    //          }
    //          break;
    //      }
    //    }
    //  }
    //}

    //private void ImportAsciiClass(List<string> lines, string direction)
    //{
    //  string dataColumn = ConvertDirection(direction);
    //  foreach (string fileLine in lines)
    //  {
    //    if (!String.IsNullOrEmpty(fileLine))
    //    {
    //      List<string> dataLines = fileLine.Split(':').ToList();
    //      switch (dataLines[0])
    //      {
    //        case "File Name":
    //        case "Start Date":
    //        case "Start Time":
    //        case "Site Code":
    //          break;
    //        default:
    //          var parsedLine = fileLine.Split(',');
    //          if (!IsFileLineData(parsedLine))
    //          {
    //            continue;
    //          }
    //          var intervalTime = MakeDateTime(parsedLine[0], parsedLine[1]);

    //          bool intervalFound = false;
    //          for (int r = 0; r < m_Data.Tables[0].Rows.Count; r++)
    //          {
    //            if (intervalFound) break;
    //            if (Convert.ToDateTime(m_Data.Tables[0].Rows[r].ItemArray[0]) == intervalTime)
    //            {
    //              //InsertDataLine(r, dataColumn, parsedLine);
    //              intervalFound = true;
    //            }
    //            //TODO: The interval was not in the time period.
    //          }
    //          break;
    //      }
    //    }
    //  }
    //}

    private bool IsFileLineData(string[] fileLine)
    {
      if (fileLine.Length <= 2)
      {
        return false;
      }
      DateTime trialDate;
      if (DateTime.TryParse(fileLine[0] + " " + fileLine[1], out trialDate))
      {
        return true;
      }
      return false;
    }

    private void InsertClassDataLine(int row, string column, string[] dataLine)
    {
      if (m_Type == SurveyType.TubeClass || m_Type == SurveyType.TubeSpeedClass)
      {
        for (int idx = 2; idx < dataLine.Length; idx++)
        {
          double dataPoint = double.Parse(dataLine[idx]);
          m_Data.Tables[idx - 2].Rows[row][column] = dataPoint;
        }
      }
      else
      {
        int volume = 0;
        for (int i = 2; i < dataLine.Length; i++)
        {
          volume += int.Parse(dataLine[i]);
        }
        m_Data.Tables[0].Rows[row][column] = volume;
      }
    }

    private void InsertSpeedOrVolumeDataLine(int row, string column, string[] dataLine)
    {
      int volume = 0;
      for (int i = 2; i < dataLine.Length; i++)
      {
        volume += int.Parse(dataLine[i]);
      }
      if (m_Type == SurveyType.TubeClass || m_Type == SurveyType.TubeSpeedClass)
      {
        m_Data.Tables[13].Rows[row][column] = volume;
      }
      else
      {
        m_Data.Tables[0].Rows[row][column] = volume;
      }
    }

    private DateTime MakeDateTime(string date, string time)
    {
      try
      {
        return DateTime.Parse(date + " " + time);
      }
      catch (Exception)
      {
        return new DateTime();
      }
    }

    private string ConvertDirection(string direction)
    {
      if (direction == "SB" || direction == "WB")
      {
        return "SB/WB";
      }
      else
      {
        return "NB/EB";
      }
    }

    private int CalculateNumberOfIntervals()
    {
      DateTime begin = StartTime;
      DateTime end = EndTime;

      double span = (end - begin).TotalHours;
      double intervals = (span / GetIntervalLength()) * 60;

      return int.Parse(intervals.ToString());
    }

    private static string DirectionColumnNameFromConnectionEnum(BalancingInsOuts connection)
    {
      string tableName;
      switch (connection)
      {
        case BalancingInsOuts.SBEntering:
        case BalancingInsOuts.SBExiting:
        case BalancingInsOuts.WBEntering:
        case BalancingInsOuts.WBExiting:
          tableName = "SB/WB";
          break;
        case BalancingInsOuts.NBEntering:
        case BalancingInsOuts.NBExiting:
        case BalancingInsOuts.EBEntering:
        case BalancingInsOuts.EBExiting:
          tableName = "NB/EB";
          break;
        default:
          tableName = "";
          break;
      }
      return tableName;
    }

    private int GetIntervalLength()
    {
      return (int)m_IntervalSize;
    }

    #endregion

    #region Methods Created For Alternate System


    //private bool ImportCSVData(List<string> lines)
    //{
    //  DataTable data = null;
    //  SurveyType fileType = SurveyType.TMC;
    //  string location = "";
    //  string city = "";
    //  string state = "";
    //  string direction = "";

    //  foreach (string fileLine in lines)
    //  {
    //    if (fileLine != "" || fileLine != String.Empty)
    //    {
    //      List<string> parsedLine = fileLine.Split(',').ToList();
    //      switch (parsedLine[0])
    //      {
    //        case "QUALITY COUNTS REPORT":
    //        case "=====================":
    //        case "Specific Location:":
    //        case "QCJobNo:":
    //        case "Date:":
    //        case "Comments:":
    //          break;
    //        case "Type:":
    //          {
    //            if (parsedLine[1] == "Volume Data")
    //            {
    //              fileType = SurveyType.TubeVolumeOnly;
    //            }
    //            else if (parsedLine[1] == "Vehicle Classification Data")
    //            {
    //              fileType = SurveyType.TubeClass;
    //            }
    //            else
    //            {
    //              return false;
    //            }
    //            break;
    //          }
    //        case "Location:":
    //          {
    //            location = parsedLine[1];
    //            break;
    //          }
    //        case "City/State:":
    //          {
    //            city = parsedLine[1];
    //            state = parsedLine[2];
    //            break;
    //          }
    //        case "Direction:":
    //          {
    //            direction = parsedLine[1];
    //            break;
    //          }
    //        case "'===============================================================================================================================":
    //          {
    //            if (direction == "" || lines.Count < 13)
    //            {
    //              return false;
    //            }

    //            lines.RemoveRange(0, 12);
    //            if (fileType == SurveyType.TubeClass)
    //            {
    //              data = ParseCSVDataClass(lines, direction);
    //            }
    //            else
    //            {
    //              //volume only tube count
    //              //data = ParseCSVDataVolume(lines, direction);
    //            }
    //            if (data != null)
    //            {
    //              AddToDataSet(data, fileType, location, city, state);
    //              return true;
    //            }
    //            else
    //            {
    //              return false;
    //            }

    //          }
    //        default:
    //          {
    //            return false;
    //          }
    //      }
    //    }
    //  }
    //  return false;
    //}

    ///// <summary>
    ///// Parses class tube data file (must have header info removed so Merlin doesn't get confused by 'Date:' in first column)
    ///// </summary>
    ///// <param name="lines"></param>
    ///// <param name="direction"></param>
    ///// <returns></returns>
    //private DataTable ParseCSVDataClass(List<string> lines, string direction)
    //{
    //  DataTable data = SetupClassTable(direction);
    //  DateTime currentDate = new DateTime(1970, 1, 1);
    //  foreach (string fileLine in lines)
    //  {
    //    if (fileLine != "" || fileLine != String.Empty)
    //    {
    //      List<string> parsedLine = fileLine.Split(',').ToList();
    //      switch (parsedLine[0])
    //      {
    //        case "Date:":
    //          if (!DateTime.TryParse(parsedLine[1], out currentDate))
    //          {
    //            return null;
    //          }
    //          break;
    //        case "SUMMARY:":
    //          return data;
    //        default:
    //          if (parsedLine[0].EndsWith("AM") || parsedLine[0].EndsWith("PM"))
    //          {
    //            //this line has data; add to table
    //            DateTime interval;
    //            if (!DateTime.TryParse(parsedLine[0], out interval))
    //            {
    //              return null;
    //            }
    //            DataRow row = data.NewRow();
    //            row[0] = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, interval.Hour, interval.Minute, 0);
    //            for (int i = 1; i <= 14; i++)
    //            {
    //              row[i] = parsedLine[i];
    //            }
    //            data.Rows.Add(row);
    //          }
    //          break;
    //      }
    //    }
    //  }
    //  return null;
    //}

    //private DataTable ParseCSVDataVolume(List<string> lines, string direction)
    //{
    //  throw new NotImplementedException();
    //}

    //private DataTable SetupClassTable(string directionName)
    //{
    //  DataTable table = SetupIntervalColumn(directionName);
    //  List<BankVehicleTypes> classes = new List<BankVehicleTypes>()
    //  {
    //    BankVehicleTypes.FHWA1,
    //    BankVehicleTypes.FHWA2,
    //    BankVehicleTypes.FHWA3,
    //    BankVehicleTypes.FHWA4,
    //    BankVehicleTypes.FHWA5,
    //    BankVehicleTypes.FHWA6,
    //    BankVehicleTypes.FHWA7,
    //    BankVehicleTypes.FHWA8,
    //    BankVehicleTypes.FHWA9,
    //    BankVehicleTypes.FHWA10,
    //    BankVehicleTypes.FHWA11,
    //    BankVehicleTypes.FHWA12,
    //    BankVehicleTypes.FHWA13,
    //    BankVehicleTypes.Unclassified
    //  };

    //  foreach (BankVehicleTypes c in classes)
    //  {
    //    table.Columns.Add(new DataColumn(c.ToString(), Type.GetType("System.Int16")));
    //  }
    //  table.Columns.Add(new DataColumn(TOTAL_COLUMN, Type.GetType("System.Int16")));

    //  return table;
    //}

    //private DataTable SetupVolumeTable(string directionName)
    //{
    //  DataTable table = SetupIntervalColumn(directionName);

    //  table.Columns.Add(new DataColumn(TOTAL_COLUMN, Type.GetType("System.Int16")));

    //  return table;
    //}

    ///// <summary>
    ///// Creates tube data table for a direction and creates interval column.
    ///// </summary>
    ///// <param name="intervalColumnName">Name of the interval column, something like "Interval" or "Time".</param>
    ///// <param name="directionName">Name of the table which should be the direction.</param>
    ///// <returns>DataTable for one direction of tube data with the interval column added.</returns>
    //private DataTable SetupIntervalColumn(string directionName)
    //{
    //  DataTable table = new DataTable(directionName);
    //  DataColumn col = new DataColumn(INTERVAL_COLUMN, System.Type.GetType("System.DateTime"));
    //  //col.DataType = System.Type.GetType("System.DateTime");
    //  col.ReadOnly = true;
    //  table.Columns.Add(col);
    //  table.PrimaryKey = new DataColumn[] { col };
    //  return table;
    //}

    //private void AddToDataSet(DataTable data, SurveyType type, string location, string city, string state)
    //{
    //  bool typeMismatch = ((m_Type == SurveyType.TubeVolumeOnly || m_Type == SurveyType.TubeClass) && m_Type != type);
    //  bool directionMismatch = (((data.TableName == "EB" || data.TableName == "WB") && (m_Data.Tables.Contains("SB") || m_Data.Tables.Contains("NB")))
    //                           || ((data.TableName == "SB" || data.TableName == "NB") && (m_Data.Tables.Contains("EB") || m_Data.Tables.Contains("WB"))));

    //  if (typeMismatch || directionMismatch)
    //  {
    //    //type or direction mismatch between incoming parsed data and existing (cannot coexist);
    //    //  assume the new data is what should stay so throw out all existing data then add this data
    //    m_Data = new DataSet();
    //  }
    //  else if (m_Data.Tables.Contains(data.TableName))
    //  {
    //    //data for this direction already exists in the tube count; replace it
    //    m_Data.Tables.Remove(data.TableName);
    //  }

    //  //tube survey type should always be set to type of incoming parsed data
    //  m_Type = type;
    //  //Take this parsed data's location name, city and state and set for the tube object
    //  m_ParentTubeSite.m_Location = location;
    //  //add the parsed data file to the tube
    //  m_Data.Tables.Add(data);
    //}

    ///// <summary>
    ///// Gets interval total for the specified FHWA classes or for all types for the direction the table this row belongs to represents
    ///// </summary>
    ///// <param name="row">DataRow representing the interval</param>
    ///// <param name="classes">Specific FHWA classes to include</param>
    ///// <returns></returns>
    //private int ClassIntervalTotal(DataRow row, List<BankVehicleTypes> classes = null)
    //{
    //  if (classes == null)
    //  {
    //    return VolumeIntervalTotal(row);
    //  }

    //  int rowTotal = 0;
    //  foreach (BankVehicleTypes fhwaClass in classes)
    //  {
    //    int? cell = row[fhwaClass.ToString()] as int?;
    //    if (cell != null)
    //    {
    //      rowTotal += (int)cell;
    //    }
    //  }
    //  return rowTotal;
    //}

    ///// <summary>
    ///// Gets interval total for all vehicles for the direction the table this row belongs to represents
    ///// </summary>
    ///// <param name="row">DataRow representing the interval</param>
    ///// <returns></returns>
    //private int VolumeIntervalTotal(DataRow row)
    //{
    //  return (int)row[TOTAL_COLUMN];
    //}

    #endregion


  }
}
