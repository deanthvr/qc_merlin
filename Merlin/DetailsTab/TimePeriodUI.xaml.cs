using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
  /// Interaction logic for TimePeriodUI.xaml
  /// </summary>
  public partial class TimePeriodUI : UserControl
  {
    private string m_SiteCodeTextBoxTracker = "";
    private MainWindow m_Main;
    public int timePeriodIndex = -1; //index this time period is of project time periods

    public TimePeriodUI(MainWindow main)
    {
      InitializeComponent();

      m_Main = main;
      if (main.m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        isActiveCheckBox.Visibility = Visibility.Hidden;
      }
    } //not used

    public TimePeriodUI(int index)
    {
      InitializeComponent();

      timePeriodIndex = index;
    }

    //validates numeric text only in a textbox on value changed
    public void SiteCode_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox callingTextBox = (TextBox)sender;
      int value;
      if ((callingTextBox.Text == "") || (Int32.TryParse(callingTextBox.Text, out value)))
      //text is valid (numeric)
      {
        //update current textbox text tracker
        m_SiteCodeTextBoxTracker = callingTextBox.Text;
        return;
      }
      else
      {
        //disregard change
        callingTextBox.Text = m_SiteCodeTextBoxTracker;
        //move caret to end
        callingTextBox.CaretIndex = callingTextBox.GetLineLength(0);
      }
    }

    private void SiteCode_GotFocus(object sender, RoutedEventArgs e)
    {
      m_SiteCodeTextBoxTracker = "";
    }

    private void SiteCode_LostFocus(object sender, RoutedEventArgs e)
    {
      int tryParseInt;

      //if user enters
      if(((TextBox)sender).Text.Length == 1)
      {
        ((TextBox)sender).Text = ((TextBox)sender).Text.Insert(0, "0");
      }
      if (Int32.TryParse(((TextBox)sender).Text, out tryParseInt))
      {
        if(tryParseInt < 1)
        {
          ((TextBox)sender).Text = "";
        }
      }
    }

    private void CountDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
      //TimePeriod locTP;
      //List<DateTime> dates = new List<DateTime>();
      //foreach (LocationModule loc in m_Main.LocationListBox.Items.OfType<LocationModule>())
      //{
      //  locTP = loc.locationTimePeriodsPanel.Children.OfType<TimePeriod>().First(x => x.timePeriodIndex == timePeriodIndex);
      //  //builds a list of unique dates from all locations for the time period
      //  if (locTP.CountDate.SelectedDate != null && !dates.Exists(x => x.Date == ((DateTime)locTP.CountDate.SelectedDate).Date))
      //  {
      //    dates.Add((DateTime)locTP.CountDate.SelectedDate);
      //  }
      //}
      //TimePeriodModule projTP = (TimePeriodModule)m_Main.TimePeriodList.Items.GetItemAt(timePeriodIndex);
      //projTP.StartDatePicker.SelectedDateChanged -= projTP.DatePicker_SelectedDateChanged;
      //if(dates.Count == 1)
      //{
      //  projTP.StartDatePicker.SelectedDate = dates[0];
      //}
      //else
      //{
      //  projTP.StartDatePicker.SelectedDate = null;
      //}
      //projTP.StartDatePicker.SelectedDateChanged += projTP.DatePicker_SelectedDateChanged;
    }
  }
}
