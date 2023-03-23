using AppMerlin;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Merlin.DetailsTab
{
  /// <summary>
  /// Interaction logic for TubeLocationModule.xaml
  /// </summary>
  public partial class TubeLocationModule : UserControl
  {
    #region Class Variables

    private MainWindow m_Main;
    private ListBox m_Parent;
    public int m_ID;
    public string m_Latitude;
    public string m_Longitude;

    public static readonly RoutedEvent LocationDeletedEvent =
    EventManager.RegisterRoutedEvent("LocationDeletedEvent", RoutingStrategy.Bubble,
    typeof(RoutedEventHandler), typeof(TubeLocationModule));

    #endregion

    #region Constructors

    public TubeLocationModule(MainWindow main, ListBox parent, int ID, string lat, string lon, string location = "", TubeLayouts layout = TubeLayouts.EB_WB)
    {
      InitializeComponent();

      m_Main = main;
      m_Parent = parent;
      m_ID = ID;
      m_Latitude = lat;
      m_Longitude = lon;
      locationTextBox.Text = location;
      tubeDiagram.CurrentLayout = layout;
      AddHandler(TubeTimePeriodUI.SurveyTimeDeletedEvent, new RoutedEventHandler(DeleteSurveyTime_Click));
    }

    /// <summary>
    /// Creates a TubeLocationModule and uses TubeSite passed in to automatically populate itself with details and tube time periods
    /// </summary>
    /// <param name="main">Bad OO design to include the main window, but I need to be able to see the current tube order number for when I add new tube surveys</param>
    /// <param name="parent">Reference to the ListBox holding this TubeLocationModule</param>
    /// <param name="ts">Backing TubeSite which will be used to populate the TubeLocationModule</param>
    public TubeLocationModule(MainWindow main, ListBox parent, TubeSite ts)
    {
      InitializeComponent();

      m_Main = main;
      m_Parent = parent;
      m_ID = ts.Id;
      m_Latitude = ts.Latitude;
      m_Longitude = ts.Longitude;
      locationTextBox.Text = ts.m_Location;
      tubeDiagram.CurrentLayout = ts.TubeLayout;
      AddHandler(TubeTimePeriodUI.SurveyTimeDeletedEvent, new RoutedEventHandler(DeleteSurveyTime_Click));

      foreach(TubeCount tc in ts.m_TubeCounts)
      {
        surveyTimesWrapPanel.Children.Add(new TubeTimePeriodUI(tc.m_Type, (tc.StartTime == DateTime.MinValue ? null : tc.StartTime as DateTime?), tc.Duration, tc.m_SiteCode, tc.m_IntervalSize));
      }
    }

    #endregion

    public event RoutedEventHandler LocationDeleted
    {
      add { AddHandler(LocationDeletedEvent, value); }
      remove { RemoveHandler(LocationDeletedEvent, value); }
    }

    public void UpdateStateVisibility(DetailsTabState state)
    {
      if (state == DetailsTabState.Creating)
      {
        DeleteX.Visibility = Visibility.Visible;
      }
      else if (state == DetailsTabState.Editing)
      {
        DeleteX.Visibility = Visibility.Visible;
      }
      else if (state == DetailsTabState.Viewing)
      {
        DeleteX.Visibility = Visibility.Hidden;
      }
    }

    private void Grid_MouseEnter(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Bold;
      this.Cursor = Cursors.No;
    }

    //mouse leaves delete button area
    private void Grid_MouseLeave(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Normal;
      this.Cursor = Cursors.Arrow;
    }

    //Delete "x" clicked
    private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
    {
      RaiseEvent(new RoutedEventArgs(TubeLocationModule.LocationDeletedEvent));
    }

    private void addTimePeriodBtn_Click(object sender, RoutedEventArgs e)
    {
      Button caller = (Button)sender;

      switch (caller.Name)
      {
        case "addVBtn":
          surveyTimesWrapPanel.Children.Add(new TubeTimePeriodUI(m_Main.GetOrderNumForTubes(), "", SurveyType.TubeVolumeOnly));
          break;
        case "addSBtn":
          surveyTimesWrapPanel.Children.Add(new TubeTimePeriodUI(m_Main.GetOrderNumForTubes(), "", SurveyType.TubeSpeed));
          break;
        case "addCBtn":
          surveyTimesWrapPanel.Children.Add(new TubeTimePeriodUI(m_Main.GetOrderNumForTubes(), "", SurveyType.TubeClass));
          break;
        case "addSCBtn":
          surveyTimesWrapPanel.Children.Add(new TubeTimePeriodUI(m_Main.GetOrderNumForTubes(), "", SurveyType.TubeSpeedClass));
          break;
        default:
          throw new Exception("Tube survey type ambiguous");
      }
      
    }

    private void DeleteSurveyTime_Click(object sender, RoutedEventArgs e)
    {
      TubeTimePeriodUI survey = e.OriginalSource as TubeTimePeriodUI;

      if(survey.StartTime != null || survey.EndTime != null || !string.IsNullOrWhiteSpace(survey.SiteCodeTextBox.Text))
      {
        MessageBoxResult result = MessageBox.Show("Are you sure you want to remove this survey time?\n\nLocation:\t" + locationTextBox.Text + "\nSurvey:\t" + survey.StartTime.ToString() + " to " + survey.EndTime.ToString(), "Delete Location", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes)
        {
          return;
        }
      }

      if(m_Main.m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
      {
        surveyTimesWrapPanel.Children.Remove(survey);
      }
      else
      {
        survey.Visibility = Visibility.Collapsed;
      }
    }

    public void SetEditable()
    {
      DeleteGrid.Visibility = Visibility.Visible;
      addCBtn.Visibility = Visibility.Visible;
      addVBtn.Visibility = Visibility.Visible;
      surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>().ToList().ForEach(x => x.SetEditable());
    }

    public void SetUneditable()
    {
      DeleteGrid.Visibility = Visibility.Collapsed;
      addCBtn.Visibility = Visibility.Collapsed;
      addVBtn.Visibility = Visibility.Collapsed;
      surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>().ToList().ForEach(x => x.SetUneditable());
    }

  }
}
