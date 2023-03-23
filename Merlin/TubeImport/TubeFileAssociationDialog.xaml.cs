using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AppMerlin;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;

namespace Merlin.TubeImport
{
  /// <summary>
  /// Interaction logic for TubeFileAssociationDialog.xaml
  /// </summary>
  public partial class TubeFileAssociationDialog : Window
  {
    private TMCProject project;
    public Dictionary<string, ImportDataFile> files;
    private string directory;
    private int days;
    private DateTime today;
    private Regex dayRegex;
    private Regex monthRegex;
    private List<string> possibleMonths;
    private List<string> possibleDays;
    private List<string> possibleSiteCodes;
    private bool allCountsAssigned = true;
    private bool someCombinedFiles = false;
    public bool backFlag = false;
    private string errorMessage = "All files must have a valid site code, approach, and type.";
    private string combinedWarningMessage = "Files with combined approaches are not allowed.";
    List<string> acceptableApproachOptions = new List<string> { "SB", "WB", "NB", "EB" };
    List<string> combinedApproachPossibilities = new List<string> { "SBNB", "WBEB", "NBSB", "EBWB" };
    List<string> acceptableTypeOptions = new List<string> { "Speed", "Class", "Volume" };

    public TubeFileAssociationDialog(TMCProject proj, string dir, int d)
    {
      possibleMonths = new List<string>();
      possibleDays = new List<string>();
      possibleSiteCodes = new List<string>();
      files = new Dictionary<string, ImportDataFile>();
      Resources["Files"] = files;
      InitializeComponent();
      project = proj;
      directory = dir;
      days = d;
      today = DateTime.Today;
      dayRegex = new Regex(@"^[0-9][0-9][0-9][0-9](_)[0-1][0-9](_)[0-3][0-9]$");
      monthRegex = new Regex(@"^[0-9][0-9][0-9][0-9](_)[0-1][0-9]$");
      removeFileButton.IsEnabled = false;
      GetPossibleFolders();
      GetPossibleSiteCodes();
      messageBlock.Foreground = new SolidColorBrush(Colors.Red);
      messageBlock.FontWeight = FontWeights.Bold;
      messageBlock2.Foreground = new SolidColorBrush(Colors.Orange);
      messageBlock2.FontWeight = FontWeights.Bold;
      SearchForFiles();
      RefreshList();
    }

    private void GetPossibleFolders()
    {
      for (int i = 0; i <= days; i++)
      {
        TimeSpan ts = new TimeSpan(i, 0, 0, 0);
        string currentYear = today.Year.ToString();
        string currentDay = today.Subtract(ts).Day.ToString();
        string currentMonth = today.Subtract(ts).Month.ToString();
        if (currentDay.Length == 1)
        {
          currentDay = "0" + currentDay;
        }
        if (currentMonth.Length == 1)
        {
          currentMonth = "0" + currentMonth;
        }
        possibleDays.Add(currentYear + "_" + currentMonth + "_" + currentDay);
        if (!possibleMonths.Contains(currentYear + "_" + currentMonth))
        {
          possibleMonths.Add(currentYear + "_" + currentMonth);
        }
      }

    }

    private void GetPossibleSiteCodes()
    {
      //Tubes
      foreach (TubeSite tube in project.m_Tubes)
      {
        foreach (TubeCount tc in tube.m_TubeCounts)
        {
          possibleSiteCodes.Add(tc.m_SiteCode);
        }
      }
      possibleSiteCodes.Sort();
    }

    private void SearchForFiles()
    {
      if (!Directory.Exists(directory))
      {
        MessageBox.Show("The directory you have chosen is not available.\n" + directory, "Bad Path", MessageBoxButton.OK);
        directory = project.m_Prefs.m_LocalDataDirectory;
        return;
      }
      SearchSubDirectories(directory);
      foreach (var file in Directory.GetFiles(directory, "*" + project.m_OrderNumber + "*.txt"))
      {
        AddFile(file);
      }
      CheckSiteCodeAssignment();
    }

    private void SearchSubDirectories(string currentDir)
    {
      foreach (string subDir in Directory.GetDirectories(currentDir))
      {
        string relPath = subDir.Split('\\')[subDir.Split('\\').Length - 1];
        if (monthRegex.IsMatch(relPath))
        {
          if (!possibleMonths.Contains(relPath))
          {
            continue;
          }
          SearchSubDirectories(subDir);
        }
        else if (dayRegex.IsMatch(relPath))
        {
          if (possibleDays.Contains(relPath))
          {
            foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.txt"))
            {
              AddFile(file);
            }
          }
        }
        else
        {
          SearchSubDirectories(subDir);
          foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.txt"))
          {
            AddFile(file);
          }
        }
      }
    }

    private void AddFile(string file)
    {
      bool found = false;
      string fileName = file.Split('\\')[file.Split('\\').Length - 1];
      fileName = fileName.Replace(".txt", "");
      string parsedSiteCodeAttempt = FindSiteCode(fileName);
      string parsedApproachAttempt = FindApproach(fileName);
      string parsedTypeAttempt = FindType(fileName);
      var importFile = new ImportDataFile(file, parsedSiteCodeAttempt, parsedApproachAttempt, parsedTypeAttempt);
      if (String.IsNullOrEmpty(parsedSiteCodeAttempt))
      {
        files.Add(file, importFile);
        messageBlock.Text = errorMessage;
      }
      else
      {
        foreach (TubeSite site in project.m_Tubes)
        {
          if (!found)
          {
            foreach (TubeCount count in site.m_TubeCounts)
            {
              if (count.m_SiteCode == parsedSiteCodeAttempt)
              {
                found = true;
                files.Add(file, importFile);
                break;
              }
            }
          }
        }
        if (!found)
        {
          files.Add(file, importFile);
          messageBlock.Text = errorMessage;
        }
      }

      RefreshList();
    }

    private string FindSiteCode(string fileInput)
    {
      List<string> foundCodes = new List<string>();
      string currentString = "";
      foreach (char character in fileInput)
      {
        if (char.IsDigit(character))
        {
          currentString = currentString + character.ToString();
        }
        else
        {
          if (currentString.Length > 0)
          {
            foundCodes.Add(currentString);
            currentString = "";
          }
        }
      }
      string stringToMatch = "";
      foreach (var code in foundCodes)
      {
        if (!possibleSiteCodes.Contains(code))
        {
          continue;
        }
        stringToMatch = code;
      }

      return stringToMatch;
    }

    private string FindApproach(string fileInput)
    {
      var splitFileSections = fileInput.Split(' ');

      string approachName = "";
      foreach (string section in splitFileSections)
      {
        switch (section)
        {
          case "SB":
          case "WB":
          case "NB":
          case "EB":
            approachName += section;
            break;
          default:
            break;
        }
      }
      if (approachName == string.Empty)
      {
        approachName = "???";
      }
      return approachName;
    }

    private string FindType(string fileInput)
    {
      var splitFileSections = fileInput.Split(' ');

      foreach (string section in splitFileSections)
      {
        var sanitizedSection = section.Replace(".txt", "");
        switch (sanitizedSection)
        {
          case "Speed":
          case "Class":
          case "Volume":
            return sanitizedSection;
          default:
            break;
        }
      }
      return "???";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void FileListSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (fileListView.SelectedItem == null)
      {
        removeFileButton.IsEnabled = false;
      }
      else
      {
        removeFileButton.IsEnabled = true;
      }
    }

    private void AddMoreFiles_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.Filter = "ASCII Files (*.txt)|*.txt|All files (*.*)|*.*";
      dlg.Title = "Add a Count File";
      dlg.Multiselect = true;
      dlg.InitialDirectory = directory;

      bool? result = dlg.ShowDialog();
      if (result == true)
      {
        foreach (var file in dlg.FileNames)
        {
          if (!files.ContainsKey(file))
          {
            AddFile(file);
          }
          else
          {
            MessageBox.Show(file + " is already in the list.", "", MessageBoxButton.OK);
          }
        }
      }
      RefreshList();
      CheckSiteCodeAssignment();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
      if (allCountsAssigned && !someCombinedFiles && files.Count > 0)
      {
        DialogResult = true;
      }
      else if (files.Count == 0)
      {
        var result = MessageBox.Show("No Files to add.  Import will close.", "File Import Empty", MessageBoxButton.OKCancel);
        if (result == MessageBoxResult.Cancel)
        {
          return;
        }
        DialogResult = false;
      }
      else if (!allCountsAssigned)
      {
        MessageBox.Show(errorMessage, "Warning", MessageBoxButton.OK);
      }
      else
      {
        MessageBox.Show(combinedWarningMessage, "Warning", MessageBoxButton.OK);
      }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
      RemoveFileFromListView();
    }

    private void CheckSiteCodeAssignment()
    {
      allCountsAssigned = true;
      someCombinedFiles = false;

      foreach (var file in files)
      {
        if (file.Value.SiteCode == "???" || file.Value.Approach == "???" || file.Value.Type == "???")
        {
          allCountsAssigned = false;
        }
        if (CheckFileForCombinedData(file.Value.Approach))
        {
          someCombinedFiles = true;
        }
      }
      if (!allCountsAssigned)
      {
        messageBlock.Text = errorMessage;
      }
      else
      {
        messageBlock.Text = "";
      }

      if (someCombinedFiles)
      {
        messageBlock2.Text = combinedWarningMessage;
      }
      else
      {
        messageBlock2.Text = "";
      }
    }

    private bool CheckFileForCombinedData(string approach)
    {
      if (approach.Length <= 3) return false;

      if (combinedApproachPossibilities.Contains(approach))
      {
        return true;
      }
      return false;
    }

    private void Key_Down(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete && fileListView.SelectedItem != null)
      {
        RemoveFileFromListView();
      }
    }

    private void RemoveFileFromListView()
    {
      if (fileListView.SelectedItems != null)
      {
        if (fileListView.SelectedItems.Count > 1)
        {
          StringBuilder fileStringList = new StringBuilder();
          KeyValuePair<string, ImportDataFile> firstKv =
            (KeyValuePair<string, ImportDataFile>)fileListView.SelectedItems[0];
          fileStringList.Append(firstKv.Key);
          for (int i = 1; i < fileListView.SelectedItems.Count; i++)
          {
            KeyValuePair<string, ImportDataFile> kv = (KeyValuePair<string, ImportDataFile>)fileListView.SelectedItems[i];
            fileStringList.Append("\n" + kv.Key);
          }
          MessageBoxResult result = MessageBox.Show("Are you sure you want to remove these file? \n\n" + fileStringList,
            "Remove File Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
          if (result == MessageBoxResult.OK)
          {
            foreach (var item in fileListView.SelectedItems)
            {
              KeyValuePair<string, ImportDataFile> kv = (KeyValuePair<string, ImportDataFile>)item;
              RemoveFile(kv);
            }
          }
        }
        else
        {
          KeyValuePair<string, ImportDataFile> kv = (KeyValuePair<string, ImportDataFile>)fileListView.SelectedItem;
          MessageBoxResult result = MessageBox.Show("Are you sure you want to remove this file? \n\n" + kv.Key,
            "Remove File Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
          if (result == MessageBoxResult.OK)
          {
            RemoveFile(kv);
          }
        }

      }
      RefreshList();
      CheckSiteCodeAssignment();
    }

    private void RemoveFile(KeyValuePair<string, ImportDataFile> pair)
    {
      files.Remove(pair.Key);
    }

    private void SiteCode_Changed(object sender, TextChangedEventArgs e)
    {
      TextBox tb = ((TextBox)sender);
      ListViewItem select = GetAncestorOfType<ListViewItem>(tb);
      fileListView.SelectedItems.Clear();
      select.IsSelected = true;
      string file = ((KeyValuePair<string, ImportDataFile>)fileListView.SelectedItem).Key;
      if (possibleSiteCodes.Contains(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Black);
        var newDataFile = new ImportDataFile(file, tb.Text, files[file].Approach, files[file].Type);
        files[file] = newDataFile;
      }
      else
      {
        tb.Foreground = new SolidColorBrush(Colors.Red);
        var newDataFile = new ImportDataFile(file, "???", files[file].Approach, files[file].Type);
        files[file] = newDataFile;
      }
      CheckSiteCodeAssignment();
    }

    private T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
    {
      var parent = VisualTreeHelper.GetParent(child);
      if (parent != null && !(parent is T))
      {
        return (T)GetAncestorOfType<T>((FrameworkElement)parent);
      }
      return (T)parent;
    }

    public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(obj, i);
        if (child != null && child is T)
          return (T)child;
        else
        {
          T childOfChild = FindVisualChild<T>(child);
          if (childOfChild != null)
            return childOfChild;
        }
      }
      return null;
    }

    private void RefreshList()
    {
      fileListView.Items.Refresh();
    }

    private void Approach_Changed(object sender, TextChangedEventArgs e)
    {
      TextBox tb = ((TextBox)sender);
      ListViewItem select = GetAncestorOfType<ListViewItem>(tb);
      fileListView.SelectedItems.Clear();
      select.IsSelected = true;
      var fileInfo = ((KeyValuePair<string, ImportDataFile>)fileListView.SelectedItem);
      string file = fileInfo.Key;

      if (CheckFileForCombinedData(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Orange);
        var newDataFile = new ImportDataFile(file, files[file].SiteCode, tb.Text, files[file].Type);
        files[file] = newDataFile;
      }
      else if (acceptableApproachOptions.Contains(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Black);
        var newDataFile = new ImportDataFile(file, files[file].SiteCode, tb.Text, files[file].Type);
        files[file] = newDataFile;
      }
      else
      {
        tb.Foreground = new SolidColorBrush(Colors.Red);
        var newDataFile = new ImportDataFile(file, files[file].SiteCode, "???", files[file].Type);
        files[file] = newDataFile;
      }
      CheckSiteCodeAssignment();
      //CheckForCombinedDataFiles();
    }

    private void Type_Changed(object sender, TextChangedEventArgs e)
    {
      TextBox tb = ((TextBox)sender);
      ListViewItem select = GetAncestorOfType<ListViewItem>(tb);
      fileListView.SelectedItems.Clear();
      select.IsSelected = true;
      string file = ((KeyValuePair<string, ImportDataFile>)fileListView.SelectedItem).Key;

      if (acceptableTypeOptions.Contains(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Black);
        var newDataFile = new ImportDataFile(file, files[file].SiteCode, files[file].Approach, tb.Text);
        files[file] = newDataFile;
      }
      else
      {
        tb.Foreground = new SolidColorBrush(Colors.Red);
        var newDataFile = new ImportDataFile(file, files[file].SiteCode, files[file].Approach, "???");
        files[file] = newDataFile;
      }
      CheckSiteCodeAssignment();
    }

  }

  public class ValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (null != value)
      {
        if (value.ToString() == "???")
          return true;
      }
      return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return null;
    }
  }

}
