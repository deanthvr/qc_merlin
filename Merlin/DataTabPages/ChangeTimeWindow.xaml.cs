using System;
using System.Windows;
using System.Windows.Controls;

namespace Merlin.DataTabPages
{
  /// <summary>
  /// Interaction logic for ChangeTimeWindow.xaml
  /// </summary>
  public partial class ChangeTimeWindow : Window
  {
    public string timeSelection = "";
    public int intervals;

    public ChangeTimeWindow(int intervals)
    {
      this.intervals = intervals;
      InitializeComponent();
      SetupComboBox();
    }

    private void SetupComboBox()
    {
      DateTime currentInterval;
      DateTime lastInterval;
      const double iLength = 5.0;
      DateTime.TryParse("00:00", out currentInterval);
      DateTime.TryParse("23:55", out lastInterval);
      //lastInterval = lastInterval.AddMinutes(-(intervals - 1) * 5);
      int i = 0;
      while (currentInterval <= lastInterval)
      {
        ComboBoxItem item = new ComboBoxItem();
        item.Content = currentInterval.TimeOfDay.ToString().Remove(5, 3);
        dataStartTime.Items.Add(item);
        currentInterval = currentInterval.AddMinutes(iLength);
        i++;
      }
    }

    private void Time_Selected(object sender, SelectionChangedEventArgs e)
    {
      if (((ComboBoxItem)dataStartTime.SelectedItem) != null)
      {
        timeSelection = ((ComboBoxItem)dataStartTime.SelectedItem).Content.ToString();
      }
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }
}
