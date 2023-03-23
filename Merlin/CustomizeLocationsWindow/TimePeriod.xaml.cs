using System;
using System.Windows;
using System.Windows.Controls;

namespace Merlin.CustomizeLocationsWindow
{
  /// <summary>
  /// Interaction logic for TimePeriod.xaml
  /// </summary>
  public partial class TimePeriod : UserControl
  {
    private string m_SiteCodeTextBoxTracker = "";
    private MainWindow m_Main;
    
    public TimePeriod(MainWindow main)
    {
      InitializeComponent();

      m_Main = main;
      if(main.m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        isActiveCheckBox.Visibility = Visibility.Hidden;
      }
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
  }
}
