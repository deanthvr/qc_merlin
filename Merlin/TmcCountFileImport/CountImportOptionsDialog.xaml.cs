using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using AppMerlin;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Merlin.TmcCountFileImport
{
  /// <summary>
  /// Interaction logic for FileDialog1.xaml
  /// </summary>
  public partial class CountImportOptionsDialog : Window
  {
    public TMCProject project;
    private bool hasDaysError = false;
    private bool hasDirectoryError = false;

    public CountImportOptionsDialog(TMCProject proj)
    {
      InitializeComponent();
      project = proj;
      bool NetworkOk = CheckNetworkDirectoryPath(project.m_Prefs.m_NetworkDataDirectory);
      if (NetworkOk)
      {
        searchDirectory.Text = project.m_Prefs.m_NetworkDataDirectory;
      }
      else
      {
        searchDirectory.Text = project.m_Prefs.m_LocalDataDirectory;
      }
      searchDays.Text = project.m_Prefs.m_DaysToSearch.ToString();
      option2.IsChecked = true;
      daysErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
      daysErrorMessage.FontWeight = FontWeights.Bold;
      dirErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
      dirErrorMessage.FontWeight = FontWeights.Bold;
    }
    
    private bool CheckNetworkDirectoryPath(string path)
    {
      if (path == null)
      {
        return false;
      }
      string message;
      bool result = false;
      var task = new Task<bool>(() => { var dir = new DirectoryInfo(path); return dir.Exists; });
      Directory.Exists(path);

      try
      {
        task.Start();

        result = task.Wait(2000) && task.Result;
      }
      catch (Exception ex)
      {
        message = ex.Message;
      }

      while (!task.IsCompleted)
      {

      }
      if (!result)
      {
        MessageBox.Show("Could not access the following path.  Please check your connection: \n" + path + "\nUsing local path...", "Network path error", MessageBoxButton.OK);
      }
      return result;
    }


    private void filePath_LostFocus(object sender, RoutedEventArgs e)
    {
      dirErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
      dirErrorMessage.FontWeight = FontWeights.Bold;
      Regex reg = new Regex(@"[\\][\\]");
      if (String.IsNullOrEmpty(searchDirectory.Text))
      {
        dirErrorMessage.Text = "Directory Path is mandatory";
        return;
      }
      if (Char.IsLetter(searchDirectory.Text[0]))
      {
        if (!Directory.Exists(searchDirectory.Text))
        {
          hasDirectoryError = true;
          dirErrorMessage.Text = "Directory Path couldn't be found.";
        }
        else
        {
          hasDirectoryError = false;
          dirErrorMessage.Text = "";
        }
      }
      else if (reg.IsMatch(searchDirectory.Text))
      {
        hasDirectoryError = false;
        dirErrorMessage.Foreground = new SolidColorBrush(Colors.Black);
        dirErrorMessage.FontWeight = FontWeights.Normal;
        dirErrorMessage.Text = "Network path, no further validation";
      }
      else
      {
        hasDirectoryError = true;
        dirErrorMessage.Text = "Invalid Directory Path.";
      }
    }

    private void searchDays_LostFocus(object sender, RoutedEventArgs e)
    {
      int result;
      if (!int.TryParse(searchDays.Text, out result))
      {
        hasDaysError = true;
        daysErrorMessage.Text = "Search Days must be a number.";
      }
      else
      {
        hasDaysError = false;
        daysErrorMessage.Text = "";
      }
    }

    private void cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void fileSearch_Click(object sender, RoutedEventArgs e)
    {
      if (!hasDaysError && !hasDirectoryError)
      {
        DialogResult = true;
      }
      else
      {
        MessageBox.Show("Resolve Errors before continuing.", "Warning", MessageBoxButton.OK);
      }
    }

    private void browseButton_Click(object sender, RoutedEventArgs e)
    {
      ExtensionClasses.MerlinButton btn = (ExtensionClasses.MerlinButton)sender;

      System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
      System.Windows.Forms.DialogResult result = browser.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        searchDirectory.Text = browser.SelectedPath.ToString();
      }
    }

    private void filePath_TextChanged(object sender, TextChangedEventArgs e)
    {
      dirErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
      dirErrorMessage.FontWeight = FontWeights.Bold;
      Regex nReg = new Regex(@"^[\\][\\]");
      Regex lReg = new Regex(@"^[a-zA-Z][:][\\].*");
      if (String.IsNullOrEmpty(searchDirectory.Text))
      {
        hasDirectoryError = true;
        dirErrorMessage.Text = "Directory Path is mandatory";
        return;
      }
      if (Char.IsLetter(searchDirectory.Text[0]))
      {
        if (!lReg.IsMatch(searchDirectory.Text))
        {
          hasDirectoryError = true;
          dirErrorMessage.Text = "Not a valid local directory path.";
          return;
        }
        if (!Directory.Exists(searchDirectory.Text))
        {
          hasDirectoryError = true;
          dirErrorMessage.Text = "Directory Path couldn't be found.";
          return;
        }
          hasDirectoryError = false;
          dirErrorMessage.Text = "";
      }
      else if (nReg.IsMatch(searchDirectory.Text))
      {
        if (!Directory.Exists(searchDirectory.Text))
        {
          hasDirectoryError = true;
          dirErrorMessage.Text = "Network Path couldn't be found.";
          return;
        }
        hasDirectoryError = false;
        dirErrorMessage.Text = "";
      }
      else
      {
        hasDirectoryError = true;
        dirErrorMessage.Text = "Invalid Directory Path.";
      }
    }
  }
}
