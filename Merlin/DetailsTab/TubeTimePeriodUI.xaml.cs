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
using AppMerlin;

namespace Merlin.DetailsTab
{
  /// <summary>
  /// Interaction logic for TimePeriodUI.xaml
  /// </summary>
  public partial class TubeTimePeriodUI : UserControl
  {
    private string m_SiteCodeTextBoxTracker = "";

    public static readonly RoutedEvent SurveyTimeDeletedEvent =
    EventManager.RegisterRoutedEvent("SurveyTimeDeletedEvent", RoutingStrategy.Bubble,
    typeof(RoutedEventHandler), typeof(TubeTimePeriodUI));

    public static readonly RoutedEvent SurveyTimeCopiedEvent =
    EventManager.RegisterRoutedEvent("SurveyTimeCopiedEvent", RoutingStrategy.Bubble,
    typeof(RoutedEventHandler), typeof(TubeTimePeriodUI));

    #region Constructors

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="surveyType">Tube survey type</param>
    /// <param name="start">Start time</param>
    /// <param name="end">End time</param>
    /// <param name="fullSiteCode">Full site code</param>
    public TubeTimePeriodUI(SurveyType surveyType = SurveyType.Unknown, DateTime? start = null, int? duration = null, string fullSiteCode = "", IntervalLength intervalSize = IntervalLength.Fifteen)
    {
      InitializeComponent();

      SurveyType = surveyType;
      StartTime = start;
      
      if(duration != null && duration > 0)
      {
        TimeSpan ts = new TimeSpan((int)duration, 0, 0);
        DaysTextBox.Text = ts.Days.ToString();
        HoursTextBox.Text = ts.Hours.ToString();
        MinutesTextBox.Text = ts.Minutes.ToString();
      }

      OrderNumForSiteCode.Text = fullSiteCode.Length >= 6 ? fullSiteCode.Substring(0, 6) : fullSiteCode;
      SiteCodeTextBox.Text = fullSiteCode.Length > 6 ? fullSiteCode.Substring(6) : "";

      StartTimeUI.intervalRestriction = intervalSize;
      EndTimeUI.intervalRestriction = intervalSize;

      HandleStartOrEndTimeChanged();
    }

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="orderNum">6-digit order number</param>
    /// <param name="siteCodeSuffix">The portion of the site code after the first 6 digits</param>
    /// <param name="surveyType">Tube survey type</param>
    /// <param name="start">Start time</param>
    /// <param name="end">End time</param>
    public TubeTimePeriodUI(string orderNum, string siteCodeSuffix = "", SurveyType surveyType = SurveyType.Unknown, DateTime? start = null, int? duration = null, IntervalLength intervalSize = IntervalLength.Fifteen)
      : this(surveyType, start, duration, orderNum + siteCodeSuffix, intervalSize) { }

    #endregion

    #region Properties

    public DateTime? StartTime
    {
      get
      {
        if (StartDateUI.SelectedDate == null || StartTimeUI.GetTimeDT() == null)
        {
          return null;
        }
        else
        {
          return ((DateTime)StartDateUI.SelectedDate).Date.Add(((DateTime)StartTimeUI.GetTimeDT()).TimeOfDay);
        }
      }
      set
      {
        DateTime? dt = value;
        StartDateUI.SelectedDate = dt;
        StartTimeUI.SetTimeDT(dt);
      }
    }

    public DateTime? EndTime
    {
      get
      {
        if (EndDateUI.SelectedDate == null || EndTimeUI.GetTimeDT() == null)
        {
          return null;
        }
        else
        {
          return ((DateTime)EndDateUI.SelectedDate).Date.Add(((DateTime)EndTimeUI.GetTimeDT()).TimeOfDay);
        }
      }
      set
      {
        DateTime? dt = value;
        EndDateUI.SelectedDate = dt;
        EndTimeUI.SetTimeDT(dt);
      }
    }

    //public TimeSpan SurveyLength
    //{
    //  get
    //  {
    //    if(StartTime == null || EndTime == null)
    //    {
    //      return TimeSpan.Zero;
    //    }
    //    return (DateTime)EndTime - (DateTime)StartTime;
    //  }
    //}

    private SurveyType _surveyType;
    public SurveyType SurveyType
    { 
      get
      {
        return _surveyType;
      }
      set
      {
        _surveyType = value;
        string btnText;
        switch (_surveyType)
        {
          case SurveyType.TubeClass:
            btnText = "Class";
            break;
          case SurveyType.TubeSpeed:
            btnText = "S";
            break;
          case SurveyType.TubeSpeedClass:
            btnText = "SC";
            break;
          case SurveyType.TubeVolumeOnly:
            btnText = "Volume";
            break;
          default:
            btnText = "?";
            break;
        }
        surveyTypeBtn.Content = btnText;
      }
    }

    //public IntervalLength IntervalSize
    //{
    //  get
    //  {
    //    if ((bool)rb5.IsChecked)
    //    {
    //      return IntervalLength.Five;
    //    }
    //    if ((bool)rb15.IsChecked)
    //    {
    //      return IntervalLength.Fifteen;
    //    }
    //    if ((bool)rb60.IsChecked)
    //    {
    //      return IntervalLength.Sixty;
    //    }
    //    throw new Exception("Something went wrong; cannot determine tube interval length");
    //    return IntervalLength.Fifteen;
    //  }
    //  set
    //  {
    //    switch (value)
    //    {
    //      case IntervalLength.Five:
    //        rb5.IsChecked = true;
    //        break;
    //      case IntervalLength.Fifteen:
    //        rb15.IsChecked = true;
    //        break;
    //      case IntervalLength.Sixty:
    //        rb60.IsChecked = true;
    //        break;
    //      default:
    //        break;
    //    }
    //  }
    //}

    #endregion

    #region Event Handlers

    public event RoutedEventHandler LocationDeleted
    {
      add { AddHandler(SurveyTimeDeletedEvent, value); }
      remove { RemoveHandler(SurveyTimeDeletedEvent, value); }
    }

    public event RoutedEventHandler SurveyTimeCopied
    {
      add { AddHandler(SurveyTimeCopiedEvent, value); }
      remove { RemoveHandler(SurveyTimeCopiedEvent, value); }
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
      HandleStartOrEndTimeChanged();
    }

    private void surveyTypeBtn_Click(object sender, RoutedEventArgs e)
    {
      switch (SurveyType)
      {
        case SurveyType.TubeVolumeOnly:
          //SurveyType = SurveyType.TubeSpeed;
          //break;
        case SurveyType.TubeSpeed:
          SurveyType = SurveyType.TubeClass;
          break;
        case SurveyType.TubeClass:
          //SurveyType = SurveyType.TubeSpeedClass;
          //break;
        case SurveyType.TubeSpeedClass:
          SurveyType = SurveyType.TubeVolumeOnly;
          break;
        default:
          break;
      }
    }

    private void MenuBtn_Click(object sender, RoutedEventArgs e)
    {
      if(StartTime == null || EndTime == null)
      {
        MessageBox.Show("Survey time must be valid before copying to other locations.", "Can't copy :(", MessageBoxButton.OK, MessageBoxImage.Hand);
        return;
      }
      MessageBoxResult result = MessageBox.Show(string.Format("Are you sure you want to copy this time to each location that contains a survey in the same list position?\n\nStart:\t\t{0}\nEnd:\t\t{1}\nDuration:\t{2} total hours",
                                                        ((DateTime)StartTime).ToString(), ((DateTime)EndTime).ToString(), GetSurveyLengthFromStartAndEndTimes().TotalHours.ToString()), "Copy Time?", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if(result == MessageBoxResult.No)
      {
        return;
      }

      RaiseEvent(new RoutedEventArgs(TubeTimePeriodUI.SurveyTimeCopiedEvent));
    }

    private void DeleteStackPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
      RaiseEvent(new RoutedEventArgs(TubeTimePeriodUI.SurveyTimeDeletedEvent));
    }

    private void DeleteStackPanel_MouseEnter(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Bold;
      this.Cursor = Cursors.No;
    }

    private void DeleteStackPanel_MouseLeave(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Normal;
      this.Cursor = Cursors.Arrow;
    }
    
    private void StartTimeUI_TimeChanged(object sender, RoutedEventArgs e)
    {
      HandleStartOrEndTimeChanged();
    }

    private void EndTimeUI_TimeChanged(object sender, RoutedEventArgs e)
    {
      HandleStartOrEndTimeChanged();
    }

    private void DurationTextBoxes_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = sender as TextBox;
      StringBuilder sb = new StringBuilder();
      foreach (char ch in tb.Text)
      {
        int parsedChar;
        if (int.TryParse(ch.ToString(), out parsedChar))
        {
          sb.Append(ch);
        }
      }
      if (sb.Length == 0)
      {
        tb.Text = "";
        return;
      }
      int num = Math.Max(0, int.Parse(sb.ToString()));
      switch (tb.Name)
      {
        case "DaysTextBox":
          break;
        case "HoursTextBox":
          num = Math.Min(num, 23);
          break;
        case "MinutesTextBox":
          num = Math.Min(num, 59);
          break;
        default:
          break;
      }
      tb.Text = num.ToString();
      tb.CaretIndex = tb.Text.Length;
    }

    private void DurationTextBoxes_LostFocus(object sender, RoutedEventArgs e)
    {
      TextBox tb = sender as TextBox;
      if (tb.Name == "MinutesTextBox" && tb.Text.Length != 0)
      {
        tb.Text = ((int.Parse(tb.Text) / 15) * 15).ToString();
      }

      TimeSpan duration = GetSurveyLengthFromDurationTextBoxes(true);
      if (duration != TimeSpan.Zero && StartTime != null)
      {
        EndDateUI.SelectedDateChanged -= CountDate_SelectedDateChanged;
        EndTimeUI.TimeChanged -= EndTimeUI_TimeChanged;
        EndTime = StartTime + duration;
        EndDateUI.SelectedDateChanged += CountDate_SelectedDateChanged;
        EndTimeUI.TimeChanged += EndTimeUI_TimeChanged;

      }
    }

    #endregion

    private void HandleStartOrEndTimeChanged()
    {
      TimeSpan spanViaStartAndEnd = GetSurveyLengthFromStartAndEndTimes();
      TimeSpan spanViaDurationTextBoxes = GetSurveyLengthFromDurationTextBoxes(true);

      DaysTextBox.LostFocus -= DurationTextBoxes_LostFocus;
      HoursTextBox.LostFocus -= DurationTextBoxes_LostFocus;
      MinutesTextBox.LostFocus -= DurationTextBoxes_LostFocus;

      EndDateUI.SelectedDateChanged -= CountDate_SelectedDateChanged;
      EndTimeUI.TimeChanged -= EndTimeUI_TimeChanged;

      if(spanViaStartAndEnd != TimeSpan.Zero)
      {
        if (((DateTime)EndTime).Minute != ((DateTime)StartTime).Minute)
        {
          EndTime = ((DateTime)EndTime).Subtract(new TimeSpan(0, ((DateTime)EndTime).Minute, 0)).Add(new TimeSpan(0, ((DateTime)StartTime).Minute, 0));
          spanViaStartAndEnd = GetSurveyLengthFromStartAndEndTimes();
        }
        //start and end time are valid; update the duration textboxes
        DaysTextBox.Text = spanViaStartAndEnd.Days.ToString();
        HoursTextBox.Text = spanViaStartAndEnd.Hours.ToString();
        MinutesTextBox.Text = spanViaStartAndEnd.Minutes.ToString();
      }
      else if(StartTime != null && spanViaDurationTextBoxes != null)
      {
        //start time and duration textboxes valid but end time is not; set end time based on the two aforementioned values
        EndTime = StartTime + spanViaDurationTextBoxes - new TimeSpan(0, spanViaDurationTextBoxes.Minutes, 0);
      }

      EndDateUI.SelectedDateChanged += CountDate_SelectedDateChanged;
      EndTimeUI.TimeChanged += EndTimeUI_TimeChanged;
      
      DaysTextBox.LostFocus += DurationTextBoxes_LostFocus;
      HoursTextBox.LostFocus += DurationTextBoxes_LostFocus;
      MinutesTextBox.LostFocus += DurationTextBoxes_LostFocus;

    }

    public TimeSpan GetSurveyLengthFromStartAndEndTimes()
    {
      if (StartTime == null || EndTime == null)
      {
        return TimeSpan.Zero;
      }
      return (DateTime)EndTime - (DateTime)StartTime;
    }

    public TimeSpan GetSurveyLengthFromDurationTextBoxes(bool ignoreMinuteComponent)
    {
      int days;
      int hours;
      int minutes;

      if(ignoreMinuteComponent == true)
      {
        if (int.TryParse(DaysTextBox.Text, out days) && int.TryParse(HoursTextBox.Text, out hours))
        {
          return new TimeSpan(days, hours, 0, 0);
        }
        return TimeSpan.Zero;
      }
      else
      {
        if (int.TryParse(DaysTextBox.Text, out days) && int.TryParse(HoursTextBox.Text, out hours) && int.TryParse(MinutesTextBox.Text, out minutes))
        {
          return new TimeSpan(days, hours, minutes, 0);
        }
        return TimeSpan.Zero;
      }
    }

    public bool SetTimePeriod(DateTime start, DateTime end)
    {
      if(start == DateTime.MinValue || end == DateTime.MaxValue || start >= end)
      {
        return false;
      }

      StartDateUI.SelectedDateChanged -= CountDate_SelectedDateChanged;
      StartTimeUI.TimeChanged -= StartTimeUI_TimeChanged;
      EndDateUI.SelectedDateChanged -= CountDate_SelectedDateChanged;
      EndTimeUI.TimeChanged -= EndTimeUI_TimeChanged;

      DaysTextBox.LostFocus -= DurationTextBoxes_LostFocus;
      HoursTextBox.LostFocus -= DurationTextBoxes_LostFocus;
      MinutesTextBox.LostFocus -= DurationTextBoxes_LostFocus;

      TimeSpan duration = end - start;

      StartTime = start;
      EndTime = end;

      DaysTextBox.Text = duration.Days.ToString();
      HoursTextBox.Text = duration.Hours.ToString();
      MinutesTextBox.Text = duration.Minutes.ToString();

      StartDateUI.SelectedDateChanged += CountDate_SelectedDateChanged;
      StartTimeUI.TimeChanged += StartTimeUI_TimeChanged;
      EndDateUI.SelectedDateChanged += CountDate_SelectedDateChanged;
      EndTimeUI.TimeChanged += EndTimeUI_TimeChanged;

      DaysTextBox.LostFocus += DurationTextBoxes_LostFocus;
      HoursTextBox.LostFocus += DurationTextBoxes_LostFocus;
      MinutesTextBox.LostFocus += DurationTextBoxes_LostFocus;

      return true;
    }

    public void SetEditable()
    {
      DeleteStackPanel.Visibility = Visibility.Visible;
      surveyTypeBtn.Background = new SolidColorBrush(Color.FromRgb(245, 236, 223));
      surveyTypeBtn.BorderThickness = new Thickness(1);
      MenuBtn.Visibility = Visibility.Visible;
    }

    public void SetUneditable()
    {
      DeleteStackPanel.Visibility = Visibility.Collapsed;
      surveyTypeBtn.Background = null;
      surveyTypeBtn.BorderThickness = new Thickness(0);
      MenuBtn.Visibility = Visibility.Collapsed;
    }

    //private void IntervalRadio_Checked(object sender, RoutedEventArgs e)
    //{
    //  StartTimeUI.intervalRestriction = IntervalSize;
    //  EndTimeUI.intervalRestriction = IntervalSize;
    //}

  }
}
