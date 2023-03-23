
using System.Windows.Controls;
using AppMerlin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Linq;
using Merlin.TmcCountFileImport;
using QCCommon.QCData;
using QCCommon.Exceptions;
using QCCommon.Extension;
using QCCommon.QCData.DataModels;

namespace Merlin.TubeImport
{

  /// <summary>
  /// Interaction logic for TubeDatabaseImporter.xaml
  /// </summary>
  public partial class TubeDatabaseImporter
  {
    TMCProject project;
    private delegate void UpdateProgressBarValueDelegate(
          DependencyProperty dp, Object value);
    private delegate void UpdateProgressBarTextDelegate(
          DependencyProperty dp, Object value);
    public List<TubeSite> NewTubes = new List<TubeSite>();
    public List<KeyValuePair<string, string>> Log;
    private IQCData Data { get; set; }
    private DataImporter DataImporter { get; set; }

    public TubeDatabaseImporter(TMCProject proj)
    {
      InitializeComponent();
      Log = new List<KeyValuePair<string, string>>();
      project = proj;
      progressBar.Maximum = 10;

      dataSourceTextBlock.Text = $"Data Source: {project.m_Prefs.m_dataSource.GetDescription()}";
    }

    private void windowContent_Rendered(object sender, EventArgs e)
    {
      ProcessFiles();
    }

    private void ProcessFiles()
    {
      UpdateProgress(0, "Initiating Connection.");

      //get data connection
      DataImporter = new DataImporter(project.m_Prefs.m_dataSource);
      try
      {
        Data = DataImporter.GetDataConnection(project.m_Prefs.m_userName, project.m_Prefs.m_password);
      }
      catch (Exception ex)
      {
        switch (ex)
        {
          case QCDataIncorrectCredentialsException _:
            MessageBox.Show("Incorrect credentials. Please update credentials in the Settings window", "Incorrect Credentials", MessageBoxButton.OK);
            break;
          default:
            MessageBox.Show("Merlin encountered the following error when attempting to connect to the data source:\n\n" + ex.Message, "Data Source Connection Error", MessageBoxButton.OK);
            break;
        }
        DialogResult = false;
        return;
      }

      //try to connect
      try
      {
        if (Data == null)
        {
          if (project.m_Prefs.m_dataSource == QCDataSource.Api)
          {
            throw new QCDataConnectionException($"Please enter user name and password to connect to {project.m_Prefs.m_dataSource.GetDescription()}.");
          }
          throw new QCDataConnectionException($"There was a problem connecting to a data source. Please verify a valid data source is selected in settings.");
        }

        Data.Setup();
        UpdateProgress(0, "Connected to QC Web.");

      }
      catch (QCDataConnectionException ex)
      {
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException?.Message}", "Connection Error", MessageBoxButton.OK);
        DialogResult = false;
        return;
      }

      var orderNumber = project.m_TubeOrderNumber ?? project.m_OrderNumber;

      // Get Location Level Details
      UpdateProgress(0, "Retrieving Location Details...");
      List<QCCommon.QCData.DataModels.LocationGroup> tubeLocationGroupsResponse = new List<QCCommon.QCData.DataModels.LocationGroup>();
      try
      {
        var locations = Data.GetLocationDetails(int.Parse(orderNumber));
        foreach (var locationGroup in locations.TubeCount)
        {
          tubeLocationGroupsResponse.Add(locationGroup);
        }
      }
      catch (QCDataNoLocationsFoundException ex)
      {
        MessageBox.Show(ex.Message, "Location Details Query Error", MessageBoxButton.OK);
        DialogResult = false;
        return;
      }
      catch (QCDataConnectionException ex)
      {
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException.Message}", "SQL Location Details Error", MessageBoxButton.OK);
        DialogResult = false;
        return;
      }

      // Get Time Period List and create tube counts, then import the tube data
      UpdateProgress(0, "Retrieving Time Period Details...");
      List<TubeSite> newTubeSites = new List<TubeSite>();
      int tubeLocationCount = 0;
      tubeLocationGroupsResponse.ForEach(x => tubeLocationCount += x.Locations.Count);
      progressBar.Maximum = tubeLocationCount;
      UpdateProgress(0, "Order Details Retrieved. Beginning data import");
      int idx = 1;
      try
      {
        foreach (var tubeLocationGroup in tubeLocationGroupsResponse)
        {
          switch (tubeLocationGroup.ServiceID)
          {
            case 34:
            case 58:
            case 60:
            case 62:
              foreach (var location in tubeLocationGroup.Locations)
              {
                UpdateProgress(idx++, "Importing volume data for " + String.Join(", ", location.Street.Select(x => x.StreetName).ToList()));
                ProcessTubeSite(DataImporter.GenerateTubeSite(location, AppMerlin.SurveyType.TubeVolumeOnly, int.Parse(orderNumber), project));
              }
              break;
            case 59:
            case 61:
            case 63:
            case 64:
              foreach (var location in tubeLocationGroup.Locations)
              {
                UpdateProgress(idx++, "Importing class data for " + String.Join(", ", location.Street.Select(x => x.StreetName).ToList()));
                ProcessTubeSite(DataImporter.GenerateTubeSite(location, AppMerlin.SurveyType.TubeClass, int.Parse(orderNumber), project));
              }
              break;
          }
        }
      }
      catch (QCDataNoLocationsFoundException ex)
      {
        MessageBox.Show(ex.Message, "Count Details Query Error", MessageBoxButton.OK);
        DialogResult = false;
        return;
      }
      catch (QCDataConnectionException ex)
      {
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException.Message}", "Count Details Error", MessageBoxButton.OK);
        DialogResult = false;
        return;
      }

      if (Log.Count > 0)
      {
        ImportSummary summaryWindow = new ImportSummary(Log);
        summaryWindow.Owner = this;
        summaryWindow.ShowDialog();
        MessageBox.Show("Tube Database Import Complete, Project saved.", "Auto-Save", MessageBoxButton.OK);
      }
      else
      {
        MessageBox.Show("Tube Database Import Complete with no logged results. Project saved.", "Auto-Save", MessageBoxButton.OK);
      }

      Data.TearDown();

      DialogResult = true;
    }

    private void ProcessTubeSite(TubeSite ts)
    {
      foreach (TubeCount tc in ts.m_TubeCounts)
      {
        if (tc.m_Type == SurveyType.TubeVolumeOnly)
        {
          string message;
          QCDataTubeVolume data;
          try
          {
            data = Data.GetTubeVolumeData(int.Parse(tc.m_SiteCode));
          }
          catch (Exception ex)
          {
            message = $"Could not retrieve tube data: {ex.Message}";
            Log.Add(new KeyValuePair<string, string>(tc.m_SiteCode, message));
            continue;
          }
          if (DataIsBad(data, out message))
          {
            tc.StartTime = DateTime.Today;
            Log.Add(new KeyValuePair<string, string>(tc.m_SiteCode, message));
            continue;
          }
          PopulateTubeInfo(data, tc);
          tc.FillDataFromVolumeObject(data);
        }
        else
        {
          string message;
          QCDataTubeClass data;
          try
          {
            data = Data.GetTubeClassData(int.Parse(tc.m_SiteCode));
          }
          catch (Exception ex)
          {
            message = $"Could not retrieve tube data: {ex.InnerException?.Message ?? ex.Message}";
            Log.Add(new KeyValuePair<string, string>(tc.m_SiteCode, message));
            continue;
          }
          if (DataIsBad(data, out message))
          {
            tc.StartTime = DateTime.Today;
            Log.Add(new KeyValuePair<string, string>(tc.m_SiteCode, message));
            continue;
          }
          PopulateTubeInfo(data, tc);
          tc.FillDataFromClassObject(data);
        }
      }
      NewTubes.Add(ts);
    }

    private void UpdateProgress(double pbValue, string message)
    {
      UpdateProgressBarValueDelegate updatePBValueDelegate = (progressBar.SetValue);
      UpdateProgressBarTextDelegate updatePBTextDelegate = (fileBlock.SetValue);
      if (pbValue > 0)
      {
        Dispatcher.Invoke(updatePBValueDelegate,
          System.Windows.Threading.DispatcherPriority.Background,
          new object[] { ProgressBar.ValueProperty, pbValue });
      }
      Dispatcher.Invoke(updatePBTextDelegate,
        System.Windows.Threading.DispatcherPriority.Background,
        new object[] { TextBlock.TextProperty, message });
    }

    private TubeSite GenerateTubeSite(Dictionary<string, string> location, List<Dictionary<string, string>> counts, SurveyType type)
    {
      var tubeSite = new TubeSite(project, int.Parse(location["LocationID"]), location["Latitude"], location["Longitude"], location["TubeLocation"], TubeLayouts.EB_WB);
      foreach (Dictionary<string, string> count in counts)
      {
        if (count["LocationID"] == location["LocationID"])
        {
          int id;
          if (count["DayToCount"] == "Midweek")
          {
            id = int.Parse(count["TimePeriodID"] + 8);
            count["DayToCount"] = "Tuesday";
          }
          else
          {
            DayOfWeek day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), count["DayToCount"]);
            id = int.Parse(count["TimePeriodID"] + (int)day);
          }
          var tubeCount = new TubeCount(count["SiteNumber"], type, DateTime.MinValue, int.Parse(count["DurationHours"]), tubeSite);
          tubeSite.m_TubeCounts.Add(tubeCount);
        }
      }
      return tubeSite;
    }

    private void PopulateTubeInfo(DataTable table, TubeCount tc)
    {
      table.DefaultView.Sort = "CountDateTime";
      table = table.DefaultView.ToTable();
      tc.StartTime = DateTime.Parse(table.Rows[0]["CountDateTime"].ToString());
      switch (table.Rows[0]["IntervalType"].ToString())
      {
        case "15":
          tc.m_IntervalSize = IntervalLength.Fifteen;
          break;
        case "5":
          tc.m_IntervalSize = IntervalLength.Five;
          break;
        case "60":
          tc.m_IntervalSize = IntervalLength.Sixty;
          break;
        case "30":
          tc.m_IntervalSize = IntervalLength.Thirty;
          break;
      }

      int directions = table.Rows.Count / (tc.m_Type == SurveyType.TubeVolumeOnly ? 1 : 14) / (tc.Duration * 4);
      switch (table.Rows[0]["Direction"].ToString())
      {
        case "SB":
          tc.m_ParentTubeSite.TubeLayout = directions == 2 ? TubeLayouts.NB_SB : TubeLayouts.No_NB;
          break;
        case "NB":
          tc.m_ParentTubeSite.TubeLayout = directions == 2 ? TubeLayouts.NB_SB : TubeLayouts.No_SB;
          break;
        case "WB":
          tc.m_ParentTubeSite.TubeLayout = directions == 2 ? TubeLayouts.EB_WB : TubeLayouts.No_EB;
          break;
        case "EB":
          tc.m_ParentTubeSite.TubeLayout = directions == 2 ? TubeLayouts.EB_WB : TubeLayouts.No_WB;
          break;
      }
    }

    private void PopulateTubeInfo(QCDataTubeVolume data, TubeCount tc)
    {
      IEnumerable<VolumeIntervalDirectionData> intervalsAllDirections =
        (data.Data.EB ?? new List<VolumeIntervalDirectionData>())
        .Concat((data.Data.WB ?? new List<VolumeIntervalDirectionData>())
        .Concat((data.Data.SB ?? new List<VolumeIntervalDirectionData>())
        .Concat((data.Data.NB ?? new List<VolumeIntervalDirectionData>()))));

      if (intervalsAllDirections.Count() == 0)
      {
        throw new Exception("Can't populate tube info using the tube data when the data has no intervals");
      }
      //Check if the data's interval size is a valid value in the IntervalLength enum
      if (!Array.ConvertAll((IntervalLength[])Enum.GetValues(typeof(IntervalLength)), value => (int)value).Contains(data.IntervalType))
      {
        throw new Exception($"The data has an interval size of {data.IntervalType} minutes which is not one of the valid interval sizes " +
          $"({string.Join(", ", Enum.GetValues(typeof(IntervalLength)))}).");
      }

      tc.StartTime = intervalsAllDirections.Min(x => x.CountDateTime);
      tc.m_IntervalSize = (IntervalLength)data.IntervalType;

      bool eb = data.Data.EB != null && data.Data.EB.Count > 0;
      bool wb = data.Data.WB != null && data.Data.WB.Count > 0;
      bool sb = data.Data.SB != null && data.Data.SB.Count > 0;
      bool nb = data.Data.NB != null && data.Data.NB.Count > 0;

      if (eb && !wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_WB;
      }
      else if (!eb && wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_EB;
      }
      else if (!eb && !wb && sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_NB;
      }
      else if (!eb && !wb && !sb && nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_SB;
      }
      else if (eb && wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.EB_WB;
      }
      else if (!eb && !wb && sb && nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.NB_SB;
      }
      else
      {
        throw new Exception($"Data service returned invalid combination of directions.");
      }
    }

    private void PopulateTubeInfo(QCDataTubeClass data, TubeCount tc)
    {
      IEnumerable<ClassIntervalDirectionData> intervalsAllDirections =
        (data.Data.EB ?? new List<ClassIntervalDirectionData>())
        .Concat((data.Data.WB ?? new List<ClassIntervalDirectionData>())
        .Concat((data.Data.SB ?? new List<ClassIntervalDirectionData>())
        .Concat((data.Data.NB ?? new List<ClassIntervalDirectionData>()))));

      if (intervalsAllDirections.Count() == 0)
      {
        throw new Exception("Can't populate tube info using the tube data when the data has no intervals");
      }
      //Check if the data's interval size is a valid value in the IntervalLength enum
      if (!Array.ConvertAll((IntervalLength[])Enum.GetValues(typeof(IntervalLength)), value => (int)value).Contains(data.IntervalType))
      {
        throw new Exception($"The data has an interval size of {data.IntervalType} minutes which is not one of the valid interval sizes " +
          $"({string.Join(", ", Enum.GetValues(typeof(IntervalLength)))}).");
      }

      tc.StartTime = intervalsAllDirections.Min(x => x.CountDateTime);
      tc.m_IntervalSize = (IntervalLength)data.IntervalType;

      bool eb = data.Data.EB != null && data.Data.EB.Count > 0;
      bool wb = data.Data.WB != null && data.Data.WB.Count > 0;
      bool sb = data.Data.SB != null && data.Data.SB.Count > 0;
      bool nb = data.Data.NB != null && data.Data.NB.Count > 0;

      if (eb && !wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_WB;
      }
      else if (!eb && wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_EB;
      }
      else if (!eb && !wb && sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_NB;
      }
      else if (!eb && !wb && !sb && nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.No_SB;
      }
      else if (eb && wb && !sb && !nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.EB_WB;
      }
      else if (!eb && !wb && sb && nb)
      {
        tc.m_ParentTubeSite.TubeLayout = TubeLayouts.NB_SB;
      }
      else
      {
        throw new Exception($"Data service returned invalid combination of directions.");
      }
    }

    private bool DataIsBad(QCDataTubeVolume data, out string message)
    {
      if (data == null || data.Data == null || ((data.Data.EB == null || data.Data.EB.Count == 0) && (data.Data.WB == null || data.Data.WB.Count == 0) && (data.Data.NB == null || data.Data.NB.Count == 0) && (data.Data.SB == null || data.Data.SB.Count == 0)))
      {
        message = "No data";
        return true;
      }
      switch (data.IntervalType)
      {
        case 15:
        case 5:
        case 60:
        case 30:
          break;
        default:
          message = "Interval length was found to be unacceptable " + data.IntervalType + " (must be 5, 15, 30 or 60)";
          return true;
      }
      message = "";
      return false;
    }

    private bool DataIsBad(QCDataTubeClass data, out string message)
    {
      if (data == null || data.Data == null || ((data.Data.EB == null || data.Data.EB.Count == 0) && (data.Data.WB == null || data.Data.WB.Count == 0) && (data.Data.NB == null || data.Data.NB.Count == 0) && (data.Data.SB == null || data.Data.SB.Count == 0)))
      {
        message = "No data";
        return true;
      }
      switch (data.IntervalType)
      {
        case 15:
        case 5:
        case 60:
        case 30:
          break;
        default:
          message = "Interval length was found to be unacceptable " + data.IntervalType + " (must be 5, 15, 30 or 60)";
          return true;
      }
      message = "";
      return false;
    }

  }
}
