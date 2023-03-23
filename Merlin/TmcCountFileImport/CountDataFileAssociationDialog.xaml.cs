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

namespace Merlin.TmcCountFileImport
{
  /// <summary>
  /// Interaction logic for FileDialog2.xaml
  /// </summary>
  public partial class CountDataFileAssociationDialog : Window
  {
    private TMCProject project;
    public Dictionary<string, Tuple<string, string>> files;
    private string directory;
    private int days;
    private DateTime today;
    private Regex dayRegex;
    private Regex monthRegex;
    private List<string> possibleMonths;
    private List<string> possibleDays;
    private List<string> possibleSiteCodes;
    private bool allCountsAssigned = true;
    public bool backFlag = false;
    private string errorMessage = "You must assign all files to site codes before moving forward.";
    List<string> acceptableApproachOptions = new List<string> { "SB", "WB", "NB", "EB", "N/A" };

    public CountDataFileAssociationDialog(TMCProject proj, string dir, int d)
    {
      possibleMonths = new List<string>();
      possibleDays = new List<string>();
      possibleSiteCodes = new List<string>();
      files = new Dictionary<string, Tuple<string, string>>();
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
      SearchForFiles();
      if (project.m_TCCDataFileRules)
      {
        errorMessage =  "You must assign all files to site codes and approaches before moving forward.";
      }
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
      //TMCs
      foreach(Count count in project.GetAllTmcCounts())
      {
        possibleSiteCodes.Add(count.m_Id);
      }
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
      foreach (var file in Directory.GetFiles(directory, "*" + project.m_OrderNumber + "*.csv"))
      {
        if (!file.Contains("statistics"))
        {
          AddFile(file);
        }
      }
      if (!project.m_TCCDataFileRules)
      {
        foreach (var file in Directory.GetFiles(directory, "*" + project.m_OrderNumber + "*.txt"))
        {
          AddFile(file);
        }
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
            foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.csv"))
            {
              if (!file.Contains("statistics"))
              {
                AddFile(file);
              }
            }
            if (!project.m_TCCDataFileRules)
            {
              foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.txt"))
              {
                AddFile(file);
              }
            }
          }
        }
        else
        {
          SearchSubDirectories(subDir);
          foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.csv"))
          {
            if (!file.Contains("statistics"))
            {
              AddFile(file);
            }
          }
          if (!project.m_TCCDataFileRules)
          {
            foreach (var file in Directory.GetFiles(subDir, "*" + project.m_OrderNumber + "*.txt"))
            {
              AddFile(file);
            }
          }
        }
      }
    }

    private void AddFile(string file)
    {
      bool found = false;
      string fileName = file.Split('\\')[file.Split('\\').Length - 1];
      string parsedSiteCodeAttempt = FindSiteCode(fileName);
      string parsedApproachAttempt = project.m_TCCDataFileRules ? FindApproach(fileName) : "N/A";
      var emptySiteCode = new Tuple<string, string>("???", parsedApproachAttempt);
      if (String.IsNullOrEmpty(parsedSiteCodeAttempt))
      {
        files.Add(file, emptySiteCode);
        messageBlock.Text = errorMessage;
      }
      else
      {
        foreach (Intersection intersection in project.m_Intersections)
        {
          if (!found)
          {
            foreach (Count count in intersection.m_Counts)
            {
              if (count.m_Id == parsedSiteCodeAttempt)
              {
                found = true;
                var stuff = new Tuple<string, string>(count.m_Id, parsedApproachAttempt);
                files.Add(file, stuff);
                break;
              }
            }
          }
        }
        if (!found)
        {
          files.Add(file, emptySiteCode);
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
      var splitFileSections = fileInput.Split('_');
      string splitFile;
      if (splitFileSections.Length > 1)
      {
        splitFile = splitFileSections[splitFileSections.Length - 2];
      }
      else
      {
        splitFile = splitFileSections[0];
      }
      string hopefullyTheApproach = (splitFile[splitFile.Length - 2].ToString() + splitFile[splitFile.Length - 1].ToString()).ToUpper();

      if (!acceptableApproachOptions.Contains(hopefullyTheApproach))
      {
        string secondAttempt = "";
        for (int i = splitFile.Length - 1; i >= 0; i--)
        {
          if (splitFile[i] == 'B' || splitFile[i] == 'b')
          {
            if (i > 0)
            {
              secondAttempt = (splitFile[i - 1].ToString() + splitFile[i].ToString()).ToUpper();
            }
            List<char> movementSuffixes = new List<char> { 'R', 'L', 'T', 'U', 'P', 'r', 'l', 't', 'u', 'p' };
            if (i < splitFile.Length - 1 && movementSuffixes.Contains(splitFile[i + 1]))
            {
              secondAttempt = secondAttempt + splitFile[i + 1].ToString();
            }
          }
        }
        if (!acceptableApproachOptions.Contains(secondAttempt) && !project.m_ColumnHeaders[0].Contains(secondAttempt))
        {
          return "???";
        }
        return secondAttempt;
      }
      return hopefullyTheApproach;
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
      dlg.Filter = "Data Files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
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
      if (allCountsAssigned && files.Count > 0)
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
      else
      {
        MessageBox.Show(errorMessage, "Warning", MessageBoxButton.OK);
      }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
      if (fileListView.SelectedItems != null)
      {
        if (fileListView.SelectedItems.Count > 1)
        {
          StringBuilder fileStringList = new StringBuilder();
          KeyValuePair<string, Tuple<string, string>> firstKv =
            (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItems[0];
          fileStringList.Append(firstKv.Key);
          for (int i = 1; i < fileListView.SelectedItems.Count; i++)
          {
            KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItems[i];
            fileStringList.Append("\n" + kv.Key);
          }
          MessageBoxResult result = MessageBox.Show("Are you sure you want to remove these file? \n\n" + fileStringList,
            "Remove File Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
          if (result == MessageBoxResult.OK)
          {
            foreach (var item in fileListView.SelectedItems)
            {
              KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)item;
              RemoveFile(kv);
            }
          }
        }
        else
        {
          KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItem;
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

    private void CheckSiteCodeAssignment()
    {
      allCountsAssigned = true;
      foreach (var file in files)
      {
        if (file.Value.Item1 == "???" || file.Value.Item2 == "???")
        {
          allCountsAssigned = false;
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
    }

    private void Key_Down(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete && fileListView.SelectedItem != null)
      {
        if (fileListView.SelectedItems != null)
        {
          if (fileListView.SelectedItems.Count > 1)
          {
            StringBuilder fileStringList = new StringBuilder();
            KeyValuePair<string, Tuple<string, string>> firstKv =
              (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItems[0];
            fileStringList.Append(firstKv.Key);
            for (int i = 1; i < fileListView.SelectedItems.Count; i++)
            {
              KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItems[i];
              fileStringList.Append("\n" + kv.Key);
            }
            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove these file? \n\n" + fileStringList,
              "Remove File Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
            {
              foreach (var item in fileListView.SelectedItems)
              {
                KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)item;
                RemoveFile(kv);
              }
            }
          }
          else
          {
            KeyValuePair<string, Tuple<string, string>> kv = (KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItem;
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
    }

    private void RemoveFile(KeyValuePair<string, Tuple<string, string>> pair)
    {
      files.Remove(pair.Key);
    }

    private void SiteCode_Changed(object sender, TextChangedEventArgs e)
    {
      TextBox tb = ((TextBox)sender);
      ListViewItem select = GetAncestorOfType<ListViewItem>(tb);
      fileListView.SelectedItems.Clear();
      select.IsSelected = true;
      string file = ((KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItem).Key;
      if (possibleSiteCodes.Contains(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Black);
        var newTuple = new Tuple<string, string>(tb.Text, files[file].Item2);
        files[file] = newTuple;
      }
      else
      {
        tb.Foreground = new SolidColorBrush(Colors.Red);
        var newTuple = new Tuple<string, string>("???", files[file].Item2);
        files[file] = newTuple;
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
      string file = ((KeyValuePair<string, Tuple<string, string>>)fileListView.SelectedItem).Key;

      if (acceptableApproachOptions.Contains(tb.Text) || project.m_ColumnHeaders[0].Contains(tb.Text))
      {
        tb.Foreground = new SolidColorBrush(Colors.Black);
        var newTuple = new Tuple<string, string>(files[file].Item1, tb.Text);
        files[file] = newTuple;
      }
      else
      {
        tb.Foreground = new SolidColorBrush(Colors.Red);
        var newTuple = new Tuple<string, string>(files[file].Item1, "???");
        files[file] = newTuple;
      }
      CheckSiteCodeAssignment();
    }

    private void ApproachTextBox_Loaded(object sender, RoutedEventArgs e)
    {
      if(!project.m_TCCDataFileRules)
      {
        ((TextBox)sender).IsEnabled = false;
      }
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
