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
using AppMerlin;
using System.Globalization;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for TimePicker.xaml
  /// </summary>
  public partial class TimePicker : UserControl
  {

    public bool hasEnteredBoxValChanged = false;
    public string pressedKeyValue;
    public int tempDebuggingIterator = 0;
    public static readonly RoutedEvent TimeChangedEvent =
      EventManager.RegisterRoutedEvent("TimeChangedEvent", RoutingStrategy.Bubble,
      typeof(RoutedEventHandler), typeof(TimePicker));
    public bool ignoreTextChangedEvent = false;
    public IntervalLength? intervalRestriction;

    public TimePicker(IntervalLength? intervalRestriction = null, DateTime? time = null)
    {
      InitializeComponent();
      hours.TextAlignment = TextAlignment.Right;
      hours.MaxLength = 2;
      minutes.TextAlignment = TextAlignment.Left;
      minutes.MaxLength = 2;

      this.intervalRestriction = intervalRestriction;
      if (time != null)
      {
        hours.Text = time.Value.Hour.ToString();
        hours.Text = time.Value.Minute.ToString();
        AMorPM.SelectedValue = time.Value.ToString("tt", CultureInfo.InvariantCulture);
      }
      else
      {
        hours.Text = "";
        minutes.Text = "";
      }
    }

    public TimePicker() : this(null, null) { }

    public event RoutedEventHandler TimeChanged
    {
      add { AddHandler(TimeChangedEvent, value); }
      remove { RemoveHandler(TimeChangedEvent, value); }
    }

    //Returns time as string in 24-hour format
    //  to match format in timeperiod and count classes
    public string GetTimeString()
    {
      string returnHours = hours.Text;

      if(!IsTimeValid())
      {
        return "";
      }
      if (AMorPM.SelectedValue.ToString() == "AM")
      {
        if (hours.Text == "12")
        {
          returnHours = "00";
        }
      }
      else
      {
        if (hours.Text != "12")
          returnHours = (Int32.Parse(hours.Text) + 12).ToString();
      }

      if (returnHours.Length != 2)
        returnHours = "0" + returnHours;

      return returnHours + ":" + minutes.Text;


    }

    public DateTime? GetTimeDT()
    {
      DateTime parsedTime;
      DateTime? nullTime = null;
      return DateTime.TryParse(GetTimeString(), out parsedTime) ? parsedTime : nullTime;
    }

    public void SetTimeDT(DateTime? time)
    {
      ignoreTextChangedEvent = true;
      if (time == null)
      {
        hours.Text = "";
        minutes.Text = "";
      }
      else
      {
        hours.Text = ((DateTime)time).ToString("hh");
        minutes.Text = ((DateTime)time).ToString("mm");
        AMorPM.SelectedValue = ((DateTime)time).Hour >= 12 ? "PM" : "AM";
      }
      ignoreTextChangedEvent = false;
    }

    //Determines if the time is completely filled out and valid
    public bool IsTimeValid()
    {
      if (!string.IsNullOrWhiteSpace(hours.Text) && !string.IsNullOrWhiteSpace(minutes.Text) && AMorPM.SelectedValue != null)
      {
        if (intervalRestriction == null || (int.Parse(minutes.Text)) % ((int)intervalRestriction) == 0)
        {
          return true;
        }
      }
      return false;
    }

    private void hoursOrMinutes_GotFocus(object sender, RoutedEventArgs e)
    {
      Console.WriteLine("got focus " + tempDebuggingIterator++);
      hasEnteredBoxValChanged = false;
      TextBox callingTextBox = (TextBox)sender;
      callingTextBox.Background = Brushes.Black;
      callingTextBox.Foreground = Brushes.White;
      if (callingTextBox.Name == "minutes" && minutes.Text == "" && hours.Text != "")
      {
        ignoreTextChangedEvent = true;
        minutes.Text = "00";
        ignoreTextChangedEvent = false;
      }
    }

    private void hoursOrMinutes_LostFocus(object sender, RoutedEventArgs e)
    {
      TextBox callingTextBox = (TextBox)sender;
      callingTextBox.Background = Brushes.White;
      callingTextBox.Foreground = Brushes.Black;
      if (callingTextBox.Text != "")
      {
        if (callingTextBox.Name == "hours")
        {
          //removes leading zero from hours field, if present
          callingTextBox.Text = Int32.Parse(callingTextBox.Text).ToString();

          if (callingTextBox.Text == "0" || callingTextBox.Text == "00")
            callingTextBox.Text = "12";
        }
        if (callingTextBox.Name == "minutes")
        {
          int minutesVal = Int32.Parse(callingTextBox.Text);
          string minutesValString;

          int intervalLength = intervalRestriction == null ? 5 : (int)intervalRestriction;
          minutesVal /= intervalLength;
          minutesVal *= intervalLength;
          minutesValString = minutesVal.ToString();
          if (minutesValString.Length == 1)
            minutesValString = minutesValString.Insert(0, "0");
          callingTextBox.Text = minutesValString;
        }
      }
      //Time changed event fires when textbox focus is lost
      RaiseEvent(new RoutedEventArgs(TimePicker.TimeChangedEvent));
    }

    private void hoursOrMinutes_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (ignoreTextChangedEvent)
      {
        return;
      }

      TextBox callingTextBox = (TextBox)sender;
      int value;

      if (Int32.TryParse(callingTextBox.Text, out value) && hasEnteredBoxValChanged == true)
      {
        if (callingTextBox.Name == "hours")
        {
          //User inputs AM time in 24 hr format: auto tab out and set dropdown to AM
          if (value >= 0 && value < 10 && callingTextBox.Text.Length == 2)
          {
            AMorPM.SelectedValue = "AM";
            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
            request.Wrapped = true;
            ((TextBox)sender).MoveFocus(request);
            return;
          }
          //User inputs PM time in 24 hr format: auto tab out, set dropdown to PM, and convert
          //  hour back to 12 hr format
          else if (value >= 13 && value <= 23)
          {
            AMorPM.SelectedValue = "PM";
            callingTextBox.Text = (value - 12).ToString();
            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
            request.Wrapped = true;
            ((TextBox)sender).MoveFocus(request);
            return;
          }
          //User enters valid time in 12 hr format: do nothing
          if (value >= 0 && value <= 12)
          {
            if (callingTextBox.Text.Length == 2)
            {
              TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
              request.Wrapped = true;
              ((TextBox)sender).MoveFocus(request);
            }
            return;
          }
          //Else, user entered invalid hour: control falls to end of function
          //  which resets to blank
        }
        if (callingTextBox.Name == "minutes")
        {
          if (value >= 0 && value <= 59)
          {
            if (callingTextBox.Text.Length == 2 || callingTextBox.Text == "")
            {
              TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
              request.Wrapped = true;
              ((TextBox)sender).MoveFocus(request);
            }
            return;
          }
          else
            callingTextBox.Text = "00";
        }
      }
      callingTextBox.Text = "";
      Console.WriteLine("invalid entry");
      hasEnteredBoxValChanged = false;
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //Time changed event fires when AM/PM selection changes
      RaiseEvent(new RoutedEventArgs(TimePicker.TimeChangedEvent));
    }

    private void hoursOrMinutes_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      //KeysConverter kc = new KeysConverter();
      //string keyChar = kc.ConvertToString(keyData);

      //Console.WriteLine(e.);
      //if(e.Key = Key.Right)
    }

    private void hoursOrMinutes_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      if (hasEnteredBoxValChanged == false)
      {
        ((TextBox)e.OriginalSource).Clear();
        ((TextBox)e.OriginalSource).Text = e.Text;
        hasEnteredBoxValChanged = true;
      }
    }
  }
}
