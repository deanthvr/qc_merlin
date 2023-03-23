using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Merlin.TmcCountFileImport
{
  /// <summary>
  /// Interaction logic for ImportSummary.xaml
  /// </summary>
  public partial class ImportSummary : Window
  {

    public List<KeyValuePair<string, KeyValuePair<string,string>>> Log;

    public ImportSummary(List<KeyValuePair<string, string>> log)
    {
      Log = SetupLog(log); 
      Resources["Log"] = Log;
      InitializeComponent();
    }

    private List<KeyValuePair<string, KeyValuePair<string, string>>> SetupLog(List<KeyValuePair<string, string>> log)
    {
      List<KeyValuePair<string, KeyValuePair<string, string>>> returnLog = new List<KeyValuePair<string, KeyValuePair<string, string>>>();
      foreach (KeyValuePair<string, string> entry in log)
      {
        returnLog.Add(new KeyValuePair<string, KeyValuePair<string,string>>(entry.Value, new KeyValuePair<string, string>(entry.Key, entry.Key.Split('\\')[entry.Key.Split('\\').Length - 1])));
      }

      return returnLog;
    }

    private void Finished_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    private void LogListSelection_Changed(object sender, SelectionChangedEventArgs e)
    {

    }

    private void Key_Down(object sender, KeyEventArgs e)
    {

    }
  }
}
