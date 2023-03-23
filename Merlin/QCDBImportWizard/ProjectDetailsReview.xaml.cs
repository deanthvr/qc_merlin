using AppMerlin;
using QCCommon.QCData;
using QCCommon.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QCCommon.Exceptions;
using QCCommon.Extension;

namespace Merlin.QCDBImportWizard
{
  /// <summary>
  /// Interaction logic for ProjectDetailsReview.xaml
  /// </summary>
  public partial class ProjectDetailsReview
  {
    public class Location
    {
      public int ID;
      public int Index;
      public string NSStreet;
      public string EWStreet;
      public string Latitude;
      public string Longitude;
      public string SpecialRequirements;
      public List<SQLTimePeriod> TimePeriods;

      public Location()
      {
        TimePeriods = new List<SQLTimePeriod>();
      }

      public Location(int id, int index, string nsStreet, string ewStreet, string latitude,
                      string longitude, string requirements, List<SQLTimePeriod> timePeriods = null)
      {
        ID = id;
        Index = index;
        NSStreet = nsStreet;
        EWStreet = ewStreet;
        Latitude = latitude;
        Longitude = longitude;
        SpecialRequirements = requirements;
        TimePeriods = timePeriods;
      }
    }

    public class SQLTimePeriod : AppMerlin.TimePeriod
    {
      public int SiteNumber;

      public SQLTimePeriod(int ID, DateTime startTime, DateTime endTime)
      {
        this.ID = ID;
        StartTime = startTime;
        EndTime = endTime;
      }
      public SQLTimePeriod(int ID, int siteNumber, DateTime startTime, DateTime endTime)
      {
        this.ID = ID;
        SiteNumber = siteNumber;
        StartTime = startTime;
        EndTime = endTime;
      }
    }

    public IQCData Data;
    public DataImporter DataImporter;
    public int OrderNumber;
    public int TubeOrderNumber;
    public bool TubeOrderDifferent = false;
    public string ProjectName;
    public DateTime OrderDate;
    public List<Location> Locations = new List<Location>();
    public List<SQLTimePeriod> TimePeriods = new List<SQLTimePeriod>();
    public List<TubeSite> Tubes = new List<AppMerlin.TubeSite>();

    private delegate void UpdateProgressBarValueDelegate(
          DependencyProperty dp, Object value);
    private delegate void UpdateProgressBarTextDelegate(
          DependencyProperty dp, Object value);
    private Preferences m_preferences;

    public ProjectDetailsReview(Preferences prefs)
    {
      InitializeComponent();
      WindowGrid.DataContext = this;
      progressBar.Maximum = 5;
      progressBar.Opacity = 0;
      OrderNumberBox.Focus();
      m_preferences = prefs;
      dataSourceTextBlock.Text = $"Data Source: {m_preferences.m_dataSource.GetDescription()}";

      DataImporter = new DataImporter(m_preferences.m_dataSource);
      try
      {
        Data = DataImporter.GetDataConnection(m_preferences.m_userName, m_preferences.m_password);
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
      }
    }

    private void SQLOrderSearch_Click(object sender, RoutedEventArgs e)
    {
      Locations.Clear();
      Tubes.Clear();
      TimePeriods.Clear();
      int entry = 0;
      int tubeOrder = 0;
      try
      {
        entry = int.Parse(OrderNumberBox.Text);
        tubeOrder = String.IsNullOrEmpty(TubeOrderNumberBox.Text) ? entry : int.Parse(TubeOrderNumberBox.Text);
        TubeOrderDifferent = !String.IsNullOrEmpty(TubeOrderNumberBox.Text);
      }
      catch
      {
        MessageBox.Show("Bad integer entry in one of the order number boxes.",
          "Order Number Error", MessageBoxButton.OK);
        return;
      }
      ChangeButtonManipulability(false);

      ProcessLocations(entry, true, !TubeOrderDifferent);
      progressBar.Opacity = 0;
      if (TubeOrderDifferent)
      {
        ProcessLocations(tubeOrder, false, true);
      }
      progressBar.Opacity = 0;
      LoadTMCDetailsIntoWindow();
      LoadTubesIntoWindow();
      OrderNumber = entry;
      TubeOrderNumber = tubeOrder;
      ChangeButtonManipulability(true);
    }

    private void ProcessLocations(int orderNumber, bool includeTmcs = true, bool includeTubes = true)
    {
      UpdateProgress(0, "Initiating Connection.");
      progressBar.Opacity = 1;

      try
      {
        if (Data == null)
        {
          if (m_preferences.m_dataSource == QCDataSource.Api)
          {
            throw new QCDataConnectionException($"Could not connect to {m_preferences.m_dataSource.GetDescription()}. Please ensure user name and password are entered correctly.");
          }
          throw new QCDataConnectionException($"There was a problem connecting to a data source. Please verify a valid data source is selected in settings.");
        }

        Data.Setup();
        UpdateProgress(1, "Connected to QC Web.");

      }
      catch (QCDataConnectionException ex)
      {
        UpdateStatusMessage("Error retrieving order details.", new SolidColorBrush(Colors.Red));
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException?.Message}", "Connection Error", MessageBoxButton.OK);
        Data?.TearDown();
        CleanUp();
        return;
      }

      // Get Order Level Details
      try
      {
        UpdateProgress(2, "Retrieving Order Details for " + orderNumber + "...");
        var response1 = Data.GetOrderDetails(orderNumber);
        OrderNumberBlock.Text = response1.OrderID.ToString();
        OrderNumber = response1.OrderID;
        ProjectNameBlock.Text = response1.ProjectName;
        ProjectName = response1.ProjectName;
        OrderDateBlock.Text = response1.OrderDate.ToShortDateString();
        OrderDate = response1.OrderDate;
      }
      catch (Exception ex)
      {
        UpdateStatusMessage("Order Details Query Error", new SolidColorBrush(Colors.Red));
        Data?.TearDown();
        CleanUp();
        switch (ex)
        {
          case QCDataConnectionException _:
            MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException.Message}", "SQL Order Details Error", MessageBoxButton.OK);
            break;
          case QCDataNoEntriesFoundException _:
          case QCDataDuplicateEntriesFoundException _:
            MessageBox.Show(ex.Message, "Order Details Query Error", MessageBoxButton.OK);
            break;
          default:
            MessageBox.Show(ex.Message, "Order Details Query Error", MessageBoxButton.OK);
            break;
        }
        return;
      }


      // Get Location Level Details
      List<QCCommon.QCData.DataModels.Location> tmcLocationResponse = new List<QCCommon.QCData.DataModels.Location>();
      List<QCCommon.QCData.DataModels.LocationGroup> tubeLocationGroupsResponse = new List<QCCommon.QCData.DataModels.LocationGroup>();
      try
      {
        UpdateProgress(3, "Retrieving Location Details...");
        var locations = Data.GetLocationDetails(orderNumber);
        foreach (var locationGroup in locations.TurnCount)
        {
          foreach (var location in locationGroup.Locations)
          {
            tmcLocationResponse.Add(location);
          }
        }
        foreach (var locationGroup in locations.TubeCount)
        {
          tubeLocationGroupsResponse.Add(locationGroup);
        }
      }
      catch (QCDataNoLocationsFoundException ex)
      {
        MessageBox.Show(ex.Message, "Location Details Query Error", MessageBoxButton.OK);
        UpdateStatusMessage("No locations for this Order.", new SolidColorBrush(Colors.Red));
        Data?.TearDown();
        CleanUp();
        return;
      }
      catch (QCDataConnectionException ex)
      {
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException.Message}", "SQL Location Details Error", MessageBoxButton.OK);
        UpdateStatusMessage("Error attempting to retrieve locations.", new SolidColorBrush(Colors.Red));
        Data?.TearDown();
        CleanUp();
        return;
      }

      // Get time period Level Details
      try
      {
        UpdateProgress(4, "Retrieving Time Period Details...");

        if (includeTmcs)
        {
          foreach (var location in tmcLocationResponse)
          {
            List<SQLTimePeriod> tps = new List<SQLTimePeriod>();
            var id = location.OrderLocationID;
            var locationTimes =
               from c in Data.GetCountDetails(orderNumber).TurnCount
               join tp in Data.GetTimePeriodDetails(orderNumber).GetAllTurnCountTimePeriodsFromAllGroups().SelectMany(p => p.Days, (parentTP, DayToCount) => new { parentTP.ServiceTimePeriodID, DayToCount, parentTP.StartTime, parentTP.EndTime })
               on new { c.ServiceTimePeriodID, c.DayToCount, c.OrderLocationID } equals new { tp.ServiceTimePeriodID, tp.DayToCount, location.OrderLocationID }
               where c.OrderLocationID == id
               select new
               {
                 TimePeriodID = tp.ServiceTimePeriodID,
                 StartTime = tp.StartTime,
                 EndTime = tp.EndTime,
                 SiteCode = c.SiteNumber,
                 DayToCount = tp.DayToCount,
                 Id = tp.DayToCount == "Midweek" ? tp.ServiceTimePeriodID + 8 : tp.ServiceTimePeriodID + (int)((DayOfWeek)Enum.Parse(typeof(DayOfWeek), (tp.DayToCount ?? "Tuesday")))
               };

            foreach (var locationTime in locationTimes)
            {
              SQLTimePeriod tpForList = new SQLTimePeriod(locationTime.Id, locationTime.StartTime, locationTime.EndTime);
              SQLTimePeriod tpForLocation = new SQLTimePeriod(locationTime.Id, locationTime.SiteCode, locationTime.StartTime, locationTime.EndTime);
              if (!TimePeriods.Contains(tpForList))
              {
                TimePeriods.Add(tpForList);
              }
              tps.Add(tpForLocation);
            }
            Location loc = new Location(location.OrderLocationID, 0, location.Street.First(x => x.StreetDirectionCode == "NS").StreetName,
              location.Street.First(x => x.StreetDirectionCode == "EW").StreetName, location.Latitude.ToString(), location.Longitude.ToString(), location.Comments, tps);

            Locations.Add(loc);
          }
        }

        if (includeTubes)
        {
          foreach (var tubeLocationGroup in tubeLocationGroupsResponse)
          {
            switch (tubeLocationGroup.ServiceID)
            {
              case 34:
              case 58:
              case 60:
              case 62:
                if (includeTubes)
                {
                  foreach (var location in tubeLocationGroup.Locations)
                  {
                    Tubes.Add(DataImporter.GenerateTubeSite(location, AppMerlin.SurveyType.TubeVolumeOnly, orderNumber));
                  }
                }
                break;
              case 59:
              case 61:
              case 63:
              case 64:
                if (includeTubes)
                {
                  foreach (var location in tubeLocationGroup.Locations)
                  {
                    Tubes.Add(DataImporter.GenerateTubeSite(location, AppMerlin.SurveyType.TubeClass, orderNumber));
                  }
                }
                break;
            }
          }
        }

      }
      catch (QCDataNoLocationsFoundException ex)
      {
        MessageBox.Show(ex.Message, "Count Details Query Error", MessageBoxButton.OK);
        UpdateStatusMessage("No counts for this Order.", new SolidColorBrush(Colors.Red));
        Data?.TearDown();
        CleanUp();
        return;
      }
      catch (QCDataConnectionException ex)
      {
        MessageBox.Show(ex.Message + $"\n\nReceived this error message: \n\n{ex.InnerException.Message}", "Count Details Error", MessageBoxButton.OK);
        UpdateStatusMessage("Count level details for locations were not able to be retrieved.", new SolidColorBrush(Colors.Red));
        Data?.TearDown();
        CleanUp();
        return;
      }
      UpdateProgress(5, "Order Details Retrieved.");
      StatusLine.Foreground = new SolidColorBrush(Colors.Green);
      Data?.TearDown();

    }

    private void CleanUp()
    {
      progressBar.Opacity = 0;
      LocationPanel.Items.Clear();
      TubeLocationPanel.Items.Clear();
      ChangeButtonManipulability(true);
    }

    private void LoadTMCDetailsIntoWindow()
    {
      LocationPanel.Items.Clear();
      List<StackPanel> preSortedLocs = new List<StackPanel>();
      foreach (Location loc in Locations)
      {
        foreach (SQLTimePeriod tp in loc.TimePeriods)
        {
          StackPanel horPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 28 };
          TextBlock siteCodeBlock = new TextBlock
          {
            Text = tp.SiteNumber.ToString(),
            Margin = new Thickness(16, 2, 0, 2)
          };

          TextBlock streetNameBlock = new TextBlock
          {
            Text = loc.NSStreet + " & " + loc.EWStreet,
            Margin = new Thickness(48, 2, 0, 2)
          };

          TextBlock timeBlock = new TextBlock
          {
            Text =
              tp.StartTime.TimeOfDay.ToString().Remove(5, 3) + " - "
                + tp.EndTime.TimeOfDay.ToString().Remove(5, 3),
            Margin = new Thickness(48, 2, 0, 2)
          };

          horPanel.Children.Add(siteCodeBlock);
          horPanel.Children.Add(timeBlock);
          horPanel.Children.Add(streetNameBlock);
          horPanel.Tag = tp.SiteNumber.ToString();

          preSortedLocs.Add(horPanel);
        }

      }
      List<StackPanel> SortedList = preSortedLocs.OrderBy(o => o.Tag).ToList();
      StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 28 };
      TextBlock siteCodeHeader = new TextBlock
      {
        Text = "Site Code",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(16, 2, 0, 2)
      };
      TextBlock timeHeader = new TextBlock
      {
        Text = "Time Period",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(48, 2, 0, 2)
      };
      TextBlock streetNameHeader = new TextBlock
      {
        Text = "Street Names",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(48, 2, 0, 2)
      };
      headerPanel.Children.Add(siteCodeHeader);
      headerPanel.Children.Add(timeHeader);
      headerPanel.Children.Add(streetNameHeader);

      LocationPanel.Items.Add(headerPanel);
      foreach (StackPanel sl in SortedList)
      {
        LocationPanel.Items.Add(sl);
      }
      LocationPanel.Items.Refresh();
    }

    private void LoadTubesIntoWindow()
    {
      TubeLocationPanel.Items.Clear();
      List<StackPanel> preSortedLocs = new List<StackPanel>();
      foreach (AppMerlin.TubeSite loc in Tubes)
      {
        foreach (AppMerlin.TubeCount tc in loc.m_TubeCounts)
        {
          StackPanel horPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 28 };
          TextBlock siteCodeBlock = new TextBlock
          {
            Text = tc.m_SiteCode,
            Margin = new Thickness(16, 2, 0, 2)
          };

          TextBlock streetNameBlock = new TextBlock
          {
            Text = loc.m_Location,
            Margin = new Thickness(48, 2, 0, 2)
          };

          TextBlock timeBlock = new TextBlock
          {
            Text = tc.Duration + " Hours",
            Margin = new Thickness(48, 2, 0, 2)
          };

          horPanel.Children.Add(siteCodeBlock);
          horPanel.Children.Add(timeBlock);
          horPanel.Children.Add(streetNameBlock);
          horPanel.Tag = tc.m_SiteCode;

          preSortedLocs.Add(horPanel);
        }

      }
      List<StackPanel> SortedList = preSortedLocs.OrderBy(o => o.Tag).ToList();
      StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 28 };
      TextBlock siteCodeHeader = new TextBlock
      {
        Text = "Site Code",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(16, 2, 0, 2)
      };
      TextBlock timeHeader = new TextBlock
      {
        Text = "Duration",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(48, 2, 0, 2)
      };
      TextBlock streetNameHeader = new TextBlock
      {
        Text = "Location",
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(48, 2, 0, 2)
      };
      headerPanel.Children.Add(siteCodeHeader);
      headerPanel.Children.Add(timeHeader);
      headerPanel.Children.Add(streetNameHeader);

      TubeLocationPanel.Items.Add(headerPanel);
      foreach (StackPanel sl in SortedList)
      {
        TubeLocationPanel.Items.Add(sl);
      }
      TubeLocationPanel.Items.Refresh();
    }

    private void UpdateProgress(double pbValue, string message)
    {
      StatusLine.Foreground = new SolidColorBrush(Colors.Orange);
      UpdateProgressBarValueDelegate updatePBValueDelegate = (progressBar.SetValue);
      UpdateProgressBarTextDelegate updatePBTextDelegate = (StatusLine.SetValue);
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

    private void UpdateStatusMessage(string message, SolidColorBrush color)
    {
      StatusLine.Text = message;
      StatusLine.Foreground = color;
    }

    private void ChangeButtonManipulability(bool isManipulable)
    {
      acceptButton.IsEnabled = isManipulable;
      searchButton.IsEnabled = isManipulable;
      cancelButton.IsEnabled = isManipulable;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      if (includeTubesCheck.IsChecked != true)
      {
        Tubes.Clear();
      }
      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void includeTubes_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
