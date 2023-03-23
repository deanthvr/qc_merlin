using Merlin.DetailsTab;
using System.Linq;
using System.Windows.Controls;

namespace Merlin.CustomizeLocationsWindow
{
  /// <summary>
  /// Interaction logic for CustomizeLocationModule.xaml
  /// </summary>
  public partial class CustomizeLocationModule : UserControl
  {
    public LocationModule m_Location;
    public MainWindow m_Main;
    public string CustomizeModuleNBSB;
    public string CustomizeModuleEBWB;
    
    public CustomizeLocationModule(LocationModule location, MainWindow main)
    {
      InitializeComponent();

      m_Location = location;
      m_Main = main;
      CustomizeModuleNBSB = location.NBSB.Text;
      CustomizeModuleEBWB = location.EBWB.Text;

      //populate (or hide if inactive) each time period
      IntersectionTitle.Text = "Location #" +(((ListBox)location.Parent).Items.IndexOf(location) + 1).ToString() + ": " + CustomizeModuleNBSB + " & " + CustomizeModuleEBWB;
      TimePeriodsPanel.Children.Clear();
      foreach(TimePeriodModule tpModule in m_Main.TimePeriodList.Items.OfType<TimePeriodModule>())
      {
        if((bool)tpModule.ActiveCheckBox.IsChecked)
        {
          TimePeriod timePeriod = new TimePeriod(main);
          timePeriod.Tag = m_Main.TimePeriodList.Items.IndexOf(tpModule);
          PopulateATimePeriod(timePeriod);
          TimePeriodsPanel.Children.Add(timePeriod);
        }
      }
    }

    //populates the TimePeriod if active for this location, otherwise hide
    private void PopulateATimePeriod(TimePeriod tp)
    {
      tp.TimePeriodText.Text = ((TimePeriodModule)m_Main.TimePeriodList.Items.GetItemAt((int)tp.Tag)).TimePeriodLabel.Text;
      tp.OrderNumForSiteCode.Text = m_Main.OrderNumTextBox.Text;
      //program in create new project state
      if (m_Main.m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
      {
        //Populate date from main screen (site codes will be set in CustomizeLocsWindow constructor)
        tp.CountDate.SelectedDate = ((TimePeriodModule)m_Main.TimePeriodList.Items[(int)tp.Tag]).StartDatePicker.SelectedDate;
      }
      tp.isActiveCheckBox.IsChecked = true;
    }

  }
}
