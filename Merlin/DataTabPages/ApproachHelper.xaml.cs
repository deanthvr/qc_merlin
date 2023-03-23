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
using System.Windows.Shapes;

namespace Merlin.DataTabPages
{
  /// <summary>
  /// Interaction logic for ApproachHelper.xaml
  /// </summary>
  public partial class ApproachHelper : Window
  {
    List<string> AcceptableApproaches;
    bool approachIsGood = false;
    List<string> acceptableApproachOptions = new List<string> { "SB", "WB", "NB", "EB", "N/A" };

    public ApproachHelper(List<string> acceptableApproaches)
    {
      AcceptableApproaches = acceptableApproaches;
      InitializeComponent();
      approachEntryBox.Text = "???";
      CheckApproach();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
      if (approachIsGood)
      {
        DialogResult = true;
      }
      else
      {
        MessageBox.Show("Approach is not an acceptable option.", "Warning", MessageBoxButton.OK);
      }
    }

    private void Approach_Changed(object sender, TextChangedEventArgs e)
    {
      CheckApproach();
      if (approachIsGood)
      {
        approachEntryBox.Foreground = new SolidColorBrush(Colors.Black);
      }
      else
      {
        approachEntryBox.Foreground = new SolidColorBrush(Colors.Red);
      }      
    }

    private void CheckApproach()
    {
      if (acceptableApproachOptions.Contains(approachEntryBox.Text) || AcceptableApproaches.Contains(approachEntryBox.Text))
      {
        approachIsGood = true;
      }
      else
      {
        approachIsGood = false;
      }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (approachIsGood)
      {
        DialogResult = true;
      }
      else
      {
        DialogResult = false;
      }
    }
  }
}
