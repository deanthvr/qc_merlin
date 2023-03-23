using AppMerlin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Merlin.QCDBImportWizard
{
  public enum BoxChoices
  {
    Web,
    Merlin
  }

  /// <summary>
  /// Interaction logic for ProjectDetailsSync.xaml
  /// </summary>
  public partial class ProjectDetailsSync : Window
  {
    private TMCProject project;

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

    public class SQLTimePeriod : TimePeriod
    {
      public int SiteNumber;

      public SQLTimePeriod(int ID, DateTime startTime, DateTime endTime)
      {
        this.ID = ID;
        SiteNumber = 0;
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

    public class Site
    {
      public Location Location { get; set; }
      public SQLTimePeriod TimePeriod { get; set; }
      public int SiteCode { get; set; }

      public Site(Location loc, SQLTimePeriod tp, int sc)
      {
        Location = loc;
        TimePeriod = tp;
        SiteCode = sc;
      }
    }

    public class WindowRowSet
    {
      public Site Web { get; set; }
      public int _NSStatus;
      public int _EWStatus;
      public int _TPStatus;
      public int _LocStatus;
      public int SiteCode { get; set; }
      public byte changeFlags;
      public Site Merlin { get; set; }

      public SolidColorBrush NSBrush
      {
        get
        {
          SolidColorBrush brush = new SolidColorBrush();
          switch (_LocStatus)
          {
            case 0:
              switch (_NSStatus)
              {
                case 0:
                  brush.Color = Colors.Green;
                  break;
                case 1:
                  brush.Color = Colors.Orange;
                  break;
              }
              break;
            case 1:
              brush.Color = Colors.Red;
              break;
          }
          return brush;
        }
      }

      public SolidColorBrush EWBrush
      {
        get
        {
          SolidColorBrush brush = new SolidColorBrush();
          switch (_LocStatus)
          {
            case 0:
              switch (_EWStatus)
              {
                case 0:
                  brush.Color = Colors.Green;
                  break;
                case 1:
                  brush.Color = Colors.Orange;
                  break;
              }
              break;
            case 1:
              brush.Color = Colors.Red;
              break;
          }
          return brush;
        }
      }

      public SolidColorBrush TPBrush
      {
        get
        {
          SolidColorBrush brush = new SolidColorBrush();
          if (_TPStatus == 1)
          {
            brush.Color = Colors.Red;
          }
          else
          {
            brush.Color = Colors.Green;
          }
          return brush;
        }
      }

      public bool StreetMatch
      {
        get
        {
          bool result = false;
          if(_NSStatus + _EWStatus > 0)
          {
            result = true;
          }
          return result;
        }
      }

      public bool TimeMatch
      {
        get
        {
          bool result = false;
          if (_TPStatus == 1)
          {
            result = true;
          }
          return result;
        }
      }

      public int status
      {
        get
        {
          if (_TPStatus == 1 || _NSStatus == 2 || _EWStatus == 2)
          {
            return 3;
          }
          else if (_NSStatus == 1 || _EWStatus == 1)
          {
            return 2;
          }
          else
          {
            return 1;
          }
        }
      }

      public ImageSource Light
      {
        get
        {
          Image image = new Image();
          BitmapImage bmp;
          switch (status)
          {
            case 1:
              bmp = new BitmapImage(new Uri("/Merlin;component/Resources/Icons/greenlight.jpg", UriKind.Relative));
              break;
            case 2:
              bmp = new BitmapImage(new Uri("/Merlin;component/Resources/Icons/yellowlight.jpg", UriKind.Relative));
              break;
            default:
              bmp = new BitmapImage(new Uri("/Merlin;component/Resources/Icons/redlight.jpg", UriKind.Relative));
              break;
          }
          image.Source = bmp;
          return bmp;
        }
      }

      public BoxChoices CurrentTimeChoice
      {
        get
        {
          return BoxChoices.Web;
        }
      }

      public BoxChoices CurrentStreetChoice
      {
        get
        {
          return BoxChoices.Merlin;
        }
      }

      public WindowRowSet()
      {

      }

      public WindowRowSet(Site wLoc, Site mLoc, int siteCode)
      {
        Web = wLoc;
        Merlin = mLoc;
        changeFlags = 0;
        SiteCode = siteCode;
      }
    }

    public int OrderNumber;
    public string ProjectName;
    public DateTime OrderDate;
    public List<Location> Locations = new List<Location>();
    public List<SQLTimePeriod> TimePeriods = new List<SQLTimePeriod>();
    //public Dictionary<Intersection, Location> Matches = new Dictionary<Intersection, Location>();
    //public List<Location> MismatchedWeb = new List<Location>();
    //public List<Intersection> MismatchedMerlin = new List<Intersection>();
    private ObservableCollection<WindowRowSet> _siteCodeRows = new ObservableCollection<WindowRowSet>();

    public ObservableCollection<WindowRowSet> siteCodeRows
    {
      get { return _siteCodeRows; }
    }

    private delegate void UpdateProgressBarValueDelegate(
          DependencyProperty dp, Object value);
    private delegate void UpdateProgressBarTextDelegate(
          DependencyProperty dp, Object value);

    public ProjectDetailsSync(TMCProject project)
    {
      this.project = project;
      InitializeComponent();
      WindowGrid.DataContext = this;
      //Resources["Details"] = _siteCodeRows;
      progressBar.Maximum = 5;
      progressBar.Opacity = 0;
      OrderNumberBlock.Text = project.m_OrderNumber;

    }

    private void Sync_Click(object sender, RoutedEventArgs e)
    {
      _siteCodeRows.Clear();
      ObtainProjectDetails();
      AttemptDetailsMatch();
      CodeResults();
    }

    private void ObtainProjectDetails()
    {
      int entry = 0;
      try
      {
        entry = int.Parse(project.m_OrderNumber);
      }
      catch
      {
        MessageBox.Show("Bad integer entry.",
          "Connection Error", MessageBoxButton.OK);
      }
      UpdateProgress(0, "Initiating Connection.");
      progressBar.Opacity = 1;
      SqlConnector sql = new SqlConnector();
      if (sql.Connect())
      {
        UpdateProgress(1, "Connected to QC Web.");
      }
      else
      {
        UpdateStatusMessage("Unable to connect to QC Web.", new SolidColorBrush(Colors.Red));
        CleanUp();
        return;
      }

      // Get Order Level Details
      UpdateProgress(2, "Retrieving Order Details...");
      Dictionary<string, string> response1 = sql.GetOrderDetails(entry);
      if (response1.Keys.Count > 0)
      {
        ProjectName = response1["ProjectName"].ToString();
        OrderDate = DateTime.Parse(response1["OrderDate"]);
      }
      else
      {
        UpdateStatusMessage("Order Number did not return any details.", new SolidColorBrush(Colors.Red));
        CleanUp();
        return;
      }


      // Get Location Level Details
      UpdateProgress(3, "Retrieving Location Details...");
      List<Dictionary<string, string>> locationResponse = sql.GetLocationDetails(entry);
      if (locationResponse.Count <= 0)
      {
        UpdateStatusMessage("No TMC Locations for this Order.", new SolidColorBrush(Colors.Red));
        CleanUp();
        return;
      }

      // Get Time Period List
      UpdateProgress(4, "Retrieving Time Period Details...");
      List<Dictionary<string, string>> countResponse = sql.GetTimePeriodDetails(entry);
      if (countResponse.Count <= 0)
      {
        UpdateStatusMessage("Count level details for locations were not able to be retrieved.", new SolidColorBrush(Colors.Red));
        CleanUp();
        return;
      }

      UpdateProgress(5, "Order Details Retrieved.");
      StatusLine.Foreground = new SolidColorBrush(Colors.Green);
      Locations.Clear();
      foreach (Dictionary<string, string> location in locationResponse)
      {
        List<SQLTimePeriod> tps = new List<SQLTimePeriod>();
        foreach (Dictionary<string, string> count in countResponse)
        {
          if (count["LocationID"] == location["LocationID"])
          {
            int id = 0;
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
            SQLTimePeriod tpForList = new SQLTimePeriod(id, DateTime.Parse(count["StartTime"]), DateTime.Parse(count["EndTime"]));
            SQLTimePeriod tpForLocation = new SQLTimePeriod(id, int.Parse(count["SiteNumber"]),
              DateTime.Parse(count["StartTime"]), DateTime.Parse(count["EndTime"]));
            if (!TimePeriods.Contains(tpForList))
            {
              TimePeriods.Add(tpForList);
            }

            tps.Add(tpForLocation);
          }
        }
        Location loc = new Location(int.Parse(location["LocationID"]), int.Parse(location["Index"]), location["NSStreet"], location["EWStreet"],
                          location["Latitude"], location["Longitude"], location["SpecialRequirements"], tps);
        Locations.Add(loc);
        progressBar.Opacity = 0;
      }

      sql.Disconnect();
    }

    private void CleanUp()
    {
      progressBar.Opacity = 0;
      SyncPanel.Items.Clear();
    }

    private void AttemptDetailsMatch()
    {
      for (int i = 1; i < 100; i++)
      {
        string suffix = i.ToString();
        if (suffix.Length == 1)
        {
          suffix = "0" + suffix;
        }
        int siteCode = int.Parse(project.m_OrderNumber + suffix);
        Site web = null;
        Site merlin = null;
        bool found = false;
        foreach (Location wLoc in Locations)
        {
          if (found)
          {
            break;
          }
          foreach (SQLTimePeriod tp in wLoc.TimePeriods)
          {
            if (siteCode == tp.SiteNumber)
            {
              web = new Site(wLoc, tp, siteCode);
              found = true;
              break;
            }
          }
        }
        found = false;
        foreach (Intersection inter in project.m_Intersections)
        {
          if (found)
          {
            break;
          }
          foreach (Count count in inter.m_Counts)
          {
            if (siteCode.ToString() == count.m_Id)
            {
              Location mLoc = new Location(inter.Id, inter.Index,
                inter.GetSBNBApproach(), inter.GetWBEBApproach(),
                inter.Latitude, inter.Longitude, "", new List<SQLTimePeriod>());
              SQLTimePeriod tp = new SQLTimePeriod(count.m_TimePeriodIndex, siteCode, DateTime.Parse(count.m_StartTime), DateTime.Parse(count.m_EndTime));
              merlin = new Site(mLoc, tp, siteCode);
              found = true;
              break;
            }
          }
        }
        if (web == null && merlin == null)
        {
          return;
        }
        WindowRowSet wrs = new WindowRowSet(web, merlin, siteCode);
        _siteCodeRows.Add(wrs);
        SyncPanel.Items.Refresh();
      }
    }

    private void CodeResults()
    {
      foreach (var row in _siteCodeRows)
      {
        row._TPStatus = row.Web.TimePeriod.Equals(row.Merlin.TimePeriod) ? 0 : 1;
        row._LocStatus = row.Web.Location.Equals(row.Merlin.Location) ? 0 : 1;        

        row._NSStatus = row.Web.Location.NSStreet.Equals(row.Merlin.Location.NSStreet) ? 0 : 1;
        row._EWStatus = row.Web.Location.EWStreet.Equals(row.Merlin.Location.EWStreet) ? 0 : 1;
      }
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

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
      foreach (var row in siteCodeRows)
      {
        if (row._TPStatus > 0 && row.CurrentTimeChoice == BoxChoices.Merlin)
        {
          foreach (var loc in Locations)
          {
            foreach (var tp in loc.TimePeriods)
            {
              if (tp.SiteNumber == row.SiteCode)
              {
                tp.StartTime = row.Merlin.TimePeriod.StartTime;
                tp.EndTime = row.Merlin.TimePeriod.EndTime;                
              }
            }
          }
        }
        
      }

      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
      var tempList = new WindowRowSet[siteCodeRows.Count];
      siteCodeRows.CopyTo(tempList, 0);

      for (int i = 0; i < (siteCodeRows.Count - 1); i++)
      {
        siteCodeRows[i].Merlin = tempList[i + 1].Merlin;
      }
      siteCodeRows[siteCodeRows.Count - 1].Merlin = tempList[0].Merlin;
      CodeResults();
      SyncPanel.Items.Refresh();
    }

    private void Down_Click(object sender, RoutedEventArgs e)
    {
      var tempList = new WindowRowSet[siteCodeRows.Count];
      for (int i = siteCodeRows.Count - 1; i > 0; i--)
      {
        siteCodeRows[i].Merlin = tempList[i - 1].Merlin;
      }

      siteCodeRows[0].Merlin = tempList[siteCodeRows.Count -1].Merlin;
      CodeResults();
      SyncPanel.Items.Refresh();
    }

    private T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
    {
      var parent = VisualTreeHelper.GetParent(child);
      if (parent != null && !(parent is T))
      {
        return (T)GetAncestorOfType<T>((FrameworkElement)parent);
      }
      return (T)parent;
    }
  }
}
