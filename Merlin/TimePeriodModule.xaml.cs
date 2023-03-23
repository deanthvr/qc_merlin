using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Merlin.DetailsTab;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for TimePeriodModule.xaml
  /// </summary>
  public partial class TimePeriodModule : UserControl
  {
    //start and end times stored as DateTimes
    public DateTime m_StartTime;
    public DateTime m_EndTime;
    public bool startDateHasEverReceivedFocus = false;
    public MainWindow m_Main;
    public int m_ID;



    public TimePeriodModule(MainWindow main, int ID)
    {
      InitializeComponent();

      //Handles TimeChangedEvent that bubbled up from TimePicker
      AddHandler(TimePicker.TimeChangedEvent, new RoutedEventHandler(TimePicker_ValueChanged));

      m_StartTime = new DateTime(1970, 1, 1, 0, 0, 0);
      m_EndTime = new DateTime(1970, 1, 1, 0, 0, 0);

      CountDurationTextHrMin.Text = "";
      CountDurationTextIntervals.Text = "";

      m_Main = main;
      m_ID = ID;
    }

    public string GetTimePeriod()
    {
      return StartTimePicker.GetTimeString() + "-" + EndTimePicker.GetTimeString();
    }

    public DateTime GetStartTime()
    {
      UpdateInternalStartAndEndTimes();
      return m_StartTime;
    }

    public DateTime GetEndTime()
    {
      UpdateInternalStartAndEndTimes();
      return m_EndTime;
    }

    //Handles DatePicker value changed event
    public void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
      DatePicker callingDatePicker = (DatePicker)e.OriginalSource;

      ////Obsolete code for when there used to be an end date picker.
      //updates other datepicker if its value hasn't been set or change would make start > end
      //if (callingDatePicker.Name == "StartDatePicker")
      //{
      //  if (m_Main.m_CurrentState.m_DetailsTabState != DetailsTabState.Viewing && (endDateHasEverReceivedFocus == false || StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value))
      //  {
      //    EndDatePicker.SelectedDate = StartDatePicker.SelectedDate.Value;
      //  }
      //}
      //else
      //{
      //  if (m_Main.m_CurrentState.m_DetailsTabState != DetailsTabState.Viewing && (startDateHasEverReceivedFocus == false || EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value))
      //  {
      //    StartDatePicker.SelectedDate = EndDatePicker.SelectedDate.Value;
      //  }
      //}

      if (m_Main.m_HasADateBeenSetInCreatingMode == false)
      {
        foreach (TimePeriodModule tpModule in m_Main.TimePeriodList.Items)
        {
          if((bool)tpModule.ActiveCheckBox.IsChecked)
          {
            if(!tpModule.startDateHasEverReceivedFocus)
            {
              tpModule.StartDatePicker.SelectedDate = callingDatePicker.SelectedDate;
            }
          }
        }
        m_Main.m_HasADateBeenSetInCreatingMode = true;
      }

      //will exit out of try statement if timepickers aren't completely filled out
      try
      {
        int startAddlHours = 0;
        int endAddlHours = 0;
        if(StartTimePicker.AMorPM.SelectedValue.ToString() == "PM")
        {
          startAddlHours = 12;
        }
        if (EndTimePicker.AMorPM.SelectedValue.ToString() == "PM")
        {
          endAddlHours = 12;
        }
          
        DateTime start = new DateTime(1970, 1, 1, Int32.Parse(StartTimePicker.hours.Text) + startAddlHours, Int32.Parse(StartTimePicker.minutes.Text), 0);
        DateTime end = new DateTime(1970, 1, 1, Int32.Parse(EndTimePicker.hours.Text) + endAddlHours, Int32.Parse(EndTimePicker.minutes.Text), 0);
        if(start > end)
        {
          StartTimePicker.hours.Text = EndTimePicker.hours.Text;
          StartTimePicker.minutes.Text = EndTimePicker.minutes.Text;
        }
      }
      catch
      {
        ;
      }

      //update internal start and end DateTime objects and updates count duration display
      UpdateInternalStartAndEndTimes();
      SetCountDurationText();

      //Change count dates for each location's time periods
      int tpIndex = m_Main.TimePeriodList.Items.IndexOf(this);
      TimePeriodUI locTP;
      List<DateTime> dates = new List<DateTime>();
      foreach(LocationModule loc in m_Main.LocationListBox.Items.OfType<LocationModule>())
      {
        locTP = loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == tpIndex);
        //builds a list of unique dates from all locations for the time period
        if(locTP.CountDate.SelectedDate != null && !dates.Exists(x => x.Date == ((DateTime)locTP.CountDate.SelectedDate).Date))
        {
          dates.Add((DateTime)locTP.CountDate.SelectedDate);
        }
      }
      //if the list has more than one unique date, we should ask the user for confirmation to overwrite customized film dates
      MessageBoxResult result = MessageBoxResult.Yes;
      if (dates.Count > 1)
      {
        result = MessageBox.Show("Currently there are multiple film dates assigned for this time period. Setting the film date here will overwrite the dates for all counts in this time period.\n\nProceed?",
          "Overwrite Dates?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
      }
      if (result == MessageBoxResult.Yes)
      {
        foreach(LocationModule loc in m_Main.LocationListBox.Items.OfType<LocationModule>())
        {
          locTP = loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == tpIndex);
          locTP.CountDate.SelectedDate = StartDatePicker.SelectedDate;
        }
      }
      else
      {
        StartDatePicker.SelectedDateChanged -= DatePicker_SelectedDateChanged;
        StartDatePicker.SelectedDate = null;
        StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
      }

    }

    private void StartDatePicker_GotFocus(object sender, RoutedEventArgs e)
    {
      startDateHasEverReceivedFocus = true;
    }

    private void ActiveCheckBox_StateChanged(object sender, RoutedEventArgs e)
    {
      //MainWindow win = (MainWindow)Window.GetWindow(this);
      //win.updateLocsSiteCodeEnabledState();

      if((bool)ActiveCheckBox.IsChecked)
      {
        mainGrid.Background.Opacity = 1;
        opacity1.Opacity = 1;
        opacity2.Opacity = 1;
        
      }
      else
      {
        mainGrid.Background.Opacity = 0.4;
        opacity1.Opacity = 0.4;
        opacity2.Opacity = 0.4;

      }
      //Only updates the locations' time periods if this is not getting checked as a result of being created and set to checked. This
      //  method will be called anyways from the method that adds a project time period and calling it here is a problem because it has
      //  not been added to the list yet.
      if(m_Main.TimePeriodList.Items.IndexOf(this) >= 0)
      {
        m_Main.updateAllLocationsTimePeriods(m_Main.TimePeriodList.Items.IndexOf(this), new List<MainWindow.LocUpdateFunc>() { m_Main.ChangeLocationTimePeriodActiveState });
      }
    }

    //Handles TimePicker value changed event
    private void TimePicker_ValueChanged(object sender, RoutedEventArgs e)
    {
      TimePicker callingTimePicker = (TimePicker)e.OriginalSource;
      int startHour;
      int startMinute;
      string startAMorPM;
      int endHour;
      int endMinute;
      string endAMorPM;
      //true if both TimePickers are completely filled out

      callingTimePicker.hasEnteredBoxValChanged = true;
      
      try
      {
        startHour = Int32.Parse(StartTimePicker.hours.Text);
        startMinute = Int32.Parse(StartTimePicker.minutes.Text);
        startAMorPM = StartTimePicker.AMorPM.SelectedValue.ToString();
        endHour = Int32.Parse(EndTimePicker.hours.Text);
        endMinute = Int32.Parse(EndTimePicker.minutes.Text);
        endAMorPM = EndTimePicker.AMorPM.SelectedValue.ToString();

        //updates other timepicker if its value hasn't been set or change would make start > end
        if (callingTimePicker.Name == "StartTimePicker")
        {
          if (EndTimePicker.hours.Text == "" && EndTimePicker.minutes.Text == "" && EndTimePicker.AMorPM.SelectedValue.ToString() == "")
          {
            EndTimePicker.hours.Text = StartTimePicker.hours.Text;
            EndTimePicker.minutes.Text = StartTimePicker.minutes.Text;
            EndTimePicker.AMorPM.SelectedIndex = StartTimePicker.AMorPM.SelectedIndex;
          }
        }
        else
        {
          if (StartTimePicker.hours.Text == "" && StartTimePicker.minutes.Text == "" && StartTimePicker.AMorPM.SelectedValue.ToString() == "")
          {
            StartTimePicker.hours.Text = EndTimePicker.hours.Text;
            StartTimePicker.minutes.Text = EndTimePicker.minutes.Text;
            StartTimePicker.AMorPM.SelectedIndex = EndTimePicker.AMorPM.SelectedIndex;
          }
        }

        //update internal start and end DateTime objects and updates count duration display
        UpdateInternalStartAndEndTimes();
        SetCountDurationText();
      }
      catch
      {
        return;
      }
    }

    //Sets count duration text
    public void SetCountDurationText()
    {
      UpdateInternalStartAndEndTimes();
      TimeSpan duration = m_EndTime - m_StartTime;
      
      if(StartTimePicker.IsTimeValid() && EndTimePicker.IsTimeValid())// && m_Main.m_CurrentState.m_DetailsTabState != DetailsTabState.Viewing)
      {
        CountDurationTextHrMin.Text = ((int)duration.TotalHours).ToString() + " hr(s) " + duration.Minutes.ToString() + " min(s) | ";
        CountDurationTextIntervals.Text = (duration.TotalMinutes / 5).ToString() + " interval(s)";
      }
      else
      {
        CountDurationTextHrMin.Text = "";
        CountDurationTextIntervals.Text = "";
      }
    }

    //Updates internal start and end DateTime objects
    public void UpdateInternalStartAndEndTimes()
    {
      //If time fields are filled out, updates internal start time
      if (StartTimePicker.hours.Text != "" && StartTimePicker.minutes.Text != "" && StartTimePicker.AMorPM.SelectedValue.ToString() != "")
      {
        if(StartDatePicker.SelectedDate != null)
        {
          m_StartTime = ((DateTime)StartDatePicker.SelectedDate);
        }
        else
        {
          //the date will be null/not set in edit mode, so give it arbitrary date so the times can still be extracted (like at the top of CustomizeLocsWindow)
          m_StartTime = DateTime.Today;
        }
        m_StartTime = m_StartTime.AddHours(Int32.Parse(StartTimePicker.hours.Text));
        m_StartTime = m_StartTime.AddMinutes(Int32.Parse(StartTimePicker.minutes.Text));
        if (StartTimePicker.AMorPM.SelectedValue.ToString() == "PM" && m_StartTime.Hour != 12)
        {
          m_StartTime = m_StartTime.AddHours(12);
        }
        if(StartTimePicker.hours.Text == "12" && StartTimePicker.AMorPM.SelectedValue.ToString() == "AM")
        {
          m_StartTime = m_StartTime.Subtract(new TimeSpan(12, 0, 0));
        }
      }
      //If time fields are filled out, updates internal end time
      if (EndTimePicker.hours.Text != "" && EndTimePicker.minutes.Text != "" && EndTimePicker.AMorPM.SelectedValue.ToString() != "")
      {
        if (StartDatePicker.SelectedDate != null)
        {
          m_EndTime = ((DateTime)StartDatePicker.SelectedDate);
        }
        else
        {
          //the date will be null/not set in edit mode, so give it arbitrary date so the times can still be extracted (like at the top of CustomizeLocsWindow)
          m_EndTime = DateTime.Today;
        }
        m_EndTime = m_EndTime.AddHours(Int32.Parse(EndTimePicker.hours.Text));
        m_EndTime = m_EndTime.AddMinutes(Int32.Parse(EndTimePicker.minutes.Text));
        if (EndTimePicker.AMorPM.SelectedValue.ToString() == "PM" && m_EndTime.Hour != 12)
        {
          m_EndTime = m_EndTime.AddHours(12);
        }
        if (EndTimePicker.hours.Text == "12" && EndTimePicker.AMorPM.SelectedValue.ToString() == "AM")
        {
          m_EndTime = m_EndTime.Subtract(new TimeSpan(12, 0, 0));
        }
        if(m_EndTime <= m_StartTime)
        {
          //End time is <= start time, so it should be the next day
          m_EndTime = m_EndTime.AddHours(24);
        }
      }
    }

    private void TimePeriodLabel_TextChanged(object sender, TextChangedEventArgs e)
    {
      m_Main.updateAllLocationsTimePeriods(m_Main.TimePeriodList.Items.IndexOf(this), new List<MainWindow.LocUpdateFunc>() { m_Main.UpdateLocationTimePeriodLabel });
    }

  }
}
