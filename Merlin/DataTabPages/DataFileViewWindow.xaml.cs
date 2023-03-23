using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AppMerlin;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using Merlin.ExtensionClasses;

namespace Merlin.DataTabPages
{
  /// <summary>
  /// Interaction logic for DataFileViewWindow.xaml
  /// </summary>
  public partial class DataFileViewWindow : Window
  {
    private Count fileWindowCount;
    public Count m_RefCount;
    private int currentBank;
    private Dictionary<string, CheckBox> m_Movements;
    private List<string> m_thisFileIntervals;
    private List<string> m_refCountIntervals;
    private List<string> fileLines;
    private List<string> selectedRows;
    private List<int> selectedColumns;
    private Dictionary<int, string> rowHeaderMap;
    private string m_fileName;
    public Stack<MerlinDataTableState> undoStack = new Stack<MerlinDataTableState>();
    public Stack<MerlinDataTableState> redoStack = new Stack<MerlinDataTableState>();
    List<string> acceptableApproachOptions = new List<string> { "SB", "WB", "NB", "EB", "N/A" };
    public bool? allBanksChecked = true;
    private bool changingBankBoxes = false;

    public DataFileViewWindow(string fileName, Count c, List<string> fileLines)
    {
      m_fileName = fileName;
      currentBank = 0;
      m_thisFileIntervals = new List<string>();
      m_refCountIntervals = new List<string>();
      this.fileLines = fileLines;
      selectedRows = new List<string>();
      selectedColumns = new List<int>();
      rowHeaderMap = new Dictionary<int, string>();
      m_RefCount = c;
      InitializeComponent();
      Resources["allBanksChecked"] = allBanksChecked;
    }

    private void On_ContentRendered(object sender, EventArgs e)
    {
      SetupFileCount();
      PopulateIntervalList();
      SetupBankTabs();
      SetDefaultState();
      SetupBankBoxes();
      m_Movements = new Dictionary<string, CheckBox>
      {
        {"SBR", SBRCheckBox},
        {"SBT", SBTCheckBox},
        {"SBL", SBLCheckBox},
        {"SBP", SBPCheckBox},
        {"WBR", WBRCheckBox},
        {"WBT", WBTCheckBox},
        {"WBL", WBLCheckBox},
        {"WBP", WBPCheckBox},
        {"NBR", NBRCheckBox},
        {"NBT", NBTCheckBox},
        {"NBL", NBLCheckBox},
        {"NBP", NBPCheckBox},
        {"EBR", EBRCheckBox},
        {"EBT", EBTCheckBox},
        {"EBL", EBLCheckBox},
        {"EBP", EBPCheckBox}
      };
    }

    private void SetupFileCount()
    {
      string startTime = "";
      string endTime = "";
      ParseFileDataTimes(m_fileName, out startTime, out endTime);
      fileWindowCount = new Count(startTime, endTime, m_RefCount.m_ParentIntersection);
      LoadFile(m_fileName);
      Title = "View File Data - " + m_fileName;
      PopulateDataGrid();
      nameOfSelectedData.Text = m_fileName.Split('\\')[m_fileName.Split('\\').Length - 1];
      nameOfSelectedData.ToolTip = m_fileName;
    }

    private void SetupBankTabs()
    {
      for (int i = 0; i < fileWindowCount.m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (fileWindowCount.m_ParentIntersection.m_ParentProject.m_Banks[i] != "NOT USED"
          || fileWindowCount.m_ParentIntersection.m_ParentProject.m_PedBanks[i] != PedColumnDataType.NA)
        {
          TabItem tab = new TabItem();

          if (!fileWindowCount.m_ParentIntersection.m_ParentProject.m_TCCDataFileRules)
          {
            tab.Tag = fileWindowCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
            tab.Header = fileWindowCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
          }
          else
          {
            if (fileWindowCount.m_ParentIntersection.m_ParentProject.m_Banks[i] == "FHWAPedsBikes")
            {
              tab.Tag = fileWindowCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab.Header = "Bikes & Peds";
            }
            else
            {
              tab.Tag = fileWindowCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab.Header = fileWindowCount.m_ParentIntersection.m_ParentProject.m_BankDictionary[i];
            }
          }
          bankTabs.Items.Add(tab);
        }
      }
    }

    private void SetupBankBoxes()
    {
      foreach (var bank in m_RefCount.m_ParentIntersection.m_ParentProject.m_Banks)
      {
        CheckBox box = new CheckBox();
        box.Content = bank;
        box.Margin = new Thickness(10, 2, 0, 2);
        box.Style = (Style)FindResource("singleFileImportWindowMovements");
        box.Checked += BankBox_Checked;
        box.Unchecked += BankBox_Checked;
        box.IsChecked = true;
        bankPanel.Children.Add(box);
      }
      //allBanksChecked = true;
      allBanksCheckBox.IsChecked = true;
    }

    private void ParseFileDataTimes(string fileName, out string startTime, out string endTime)
    {
      DateTime earliest = new DateTime();
      DateTime latest = new DateTime();
      DateTime.TryParse("23:55", out earliest);
      DateTime.TryParse("00:00", out latest);
      if (fileName.EndsWith("csv"))
      {
        for (int i = 0; i < fileLines.Count; i++)
        {
          string[] parsedLine = fileLines[i].Split(',');
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
            case "Bank number":
              break;
            default:
              short temp2;
              if (Int16.TryParse(parsedLine[0].Split(':')[0], out temp2) == true)
              {
                DateTime temp = new DateTime();
                if (!DateTime.TryParse(parsedLine[0], out temp))
                {
                  break;
                }
                if (temp < earliest)
                {
                  earliest = temp;
                }
                if (temp > latest)
                {
                  latest = temp;
                }
              }
              break;
          }
        }
      }
      else if (fileName.EndsWith("txt"))
      {
        for (int i = 0; i < fileLines.Count; i++)
        {
          string[] parsedLine = fileLines[i].Split(',');
          switch (parsedLine[0])
          {
            case "Start Date":
            case "Start Time":
            case "Site Code":
            case "Street Name":
              break;
            default:
              short temp2;
              if (Int16.TryParse(parsedLine[0].Split(':')[0], out temp2) == true)
              {
                DateTime temp = new DateTime();
                if (!DateTime.TryParse(parsedLine[0], out temp))
                {
                  break;
                }
                if (temp < earliest)
                {
                  earliest = temp;
                }
                if (temp > latest)
                {
                  latest = temp;
                }
              }
              break;
          }
        }

      }
      else
      {
        DialogResult = false;
      }

      startTime = earliest.TimeOfDay.ToString().Remove(5, 3);
      endTime = latest.AddMinutes(5).TimeOfDay.ToString().Remove(5, 3);
    }

    private void PopulateIntervalList()
    {
      DateTime currentInterval;
      DateTime lastInterval;
      const double iLength = 5.0;
      DateTime.TryParse(fileWindowCount.m_StartTime, out currentInterval);
      DateTime.TryParse(fileWindowCount.m_EndTime, out lastInterval);

      for (int i = 0; i < fileWindowCount.m_NumIntervals; i++)
      {
        m_thisFileIntervals.Add(currentInterval.TimeOfDay.ToString().Remove(5, 3));
        rowHeaderMap.Add(i, currentInterval.TimeOfDay.ToString().Remove(5, 3));
        currentInterval = currentInterval.AddMinutes(iLength);
      }
      DateTime.TryParse(m_RefCount.m_StartTime, out currentInterval);
      DateTime.TryParse(m_RefCount.m_EndTime, out lastInterval);
      for (int i = 0; i < m_RefCount.m_NumIntervals; i++)
      {
        m_refCountIntervals.Add(currentInterval.TimeOfDay.ToString().Remove(5, 3));
        currentInterval = currentInterval.AddMinutes(iLength);
      }
    }

    private void SetDefaultState()
    {
      //selectByGrid.IsChecked = true;
      approachesSection.IsEnabled = false;
      rangeSection.IsEnabled = false;
      SBCheckBox.IsChecked = true;
      WBCheckBox.IsChecked = true;
      NBCheckBox.IsChecked = true;
      EBCheckBox.IsChecked = true;
      SetInfoBarIntervalText();
      if (m_RefCount.m_ParentIntersection.m_ParentProject.m_NCDOTColSwappingEnabled)
      {
        columnSwappingNotification.Visibility = Visibility.Visible;
      }
      else
      {
        columnSwappingNotification.Visibility = Visibility.Collapsed;
      }
    }

    private void SetInfoBarIntervalText()
    {
      startingInterval.Text = fileWindowCount.m_StartTime;

      DateTime lastInterval;
      DateTime.TryParse(fileWindowCount.m_EndTime, out lastInterval);
      lastInterval = lastInterval.AddMinutes(-5);
      endingInterval.Text = lastInterval.TimeOfDay.ToString().Remove(5, 3);
    }

    private void LoadFile(string fileName)
    {
      if (m_RefCount.m_ParentIntersection.m_ParentProject.m_TCCDataFileRules)
      {
        if(fileName.EndsWith("csv"))
        {
          string approach = FindApproach(fileName);
          fileWindowCount.ImportTCC_CSVData(fileLines, fileName, approach);
        } 
        else 
        {
        MessageBox.Show("File has unknown extension for this project type. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
                  "Data File Error", MessageBoxButton.OK);
        DialogResult = false;
        }
      } 
      else 
      {
        if (fileName.EndsWith("csv"))
        {
          fileWindowCount.ImportCSVData(fileLines, fileName);
        }
        else if (fileName.EndsWith("txt"))
        {
          fileWindowCount.ImportASCIIData(fileLines, fileName);
        }
        else
        {
          MessageBox.Show("File has unknown extension. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
                    "Data File Error", MessageBoxButton.OK);
          DialogResult = false;
        }  
      }
    }

    private string FindApproach(string fileInput)
    {
      var splitFileSections = fileInput.Split('_');
      string splitFile;
      if(splitFileSections.Length > 1)
      {
        splitFile = splitFileSections[splitFileSections.Length -2];
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
        var project = m_RefCount.m_ParentIntersection.m_ParentProject;
        if (!acceptableApproachOptions.Contains(secondAttempt) && !project.m_ColumnHeaders[0].Contains(secondAttempt))
        {
          //Show Dialog for User to input approach name;
          ApproachHelper helper = new ApproachHelper(project.m_ColumnHeaders[0]);
          bool? result = helper.ShowDialog();
          if (result == true)
          {
            return helper.approachEntryBox.Text;
          }
          else
          {
            DialogResult = false;
            return "";
          }
        }
        return secondAttempt;
      }
      return hopefullyTheApproach;
    }

    private void FileWindowDataTabColumnHeader_Click(object sender, MouseButtonEventArgs e)
    {
      var columnHeader = sender as DataGridColumnHeader;
      if (e.ChangedButton == MouseButton.Left && columnHeader != null)
      {
        if (columnHeader.DataContext.ToString() == "Interval")
        {
          fileWindowDataGrid.SelectedCells.Clear();
          foreach (var column in fileWindowDataGrid.Columns)
          {
            if (column.Header.ToString() == "Interval")
            {
              continue;
            }
            foreach (var item in fileWindowDataGrid.Items)
            {
              fileWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
          }
        }
        else
        {
          fileWindowDataGrid.Focus();
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          {
            foreach (var item in fileWindowDataGrid.Items)
            {
              fileWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
          else
          {
            fileWindowDataGrid.SelectedCells.Clear();
            foreach (var item in fileWindowDataGrid.Items)
            {
              fileWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
        }
      }
    }

    private void BankTab_Changed(object sender, SelectionChangedEventArgs e)
    {
      string bank = ((string)((TabItem)bankTabs.SelectedItem).Tag).Split('&')[0].Trim();
      string pedBank = ((string)((TabItem)bankTabs.SelectedItem).Tag).Split('&')[1].Trim();
      for (int i = 0; i < fileWindowCount.m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (bank == fileWindowCount.m_ParentIntersection.m_ParentProject.m_Banks[i] && pedBank == fileWindowCount.m_ParentIntersection.m_ParentProject.m_PedBanks[i].ToString())
        {
          currentBank = i;
          break;
        }
      }
      SetDefaultState();
      PopulateDataGrid();
      fileWindowDataGrid.Focus();
    }

    private void ChangeTime_Click(object sender, RoutedEventArgs e)
    {
      ChangeTimeWindow ctw = new ChangeTimeWindow(fileWindowCount.m_NumIntervals);
      ctw.Owner = this;
      ctw.ShowDialog();
      if (ctw.DialogResult == false || String.IsNullOrEmpty(ctw.timeSelection))
      {
        return;
      }
      ChangeFileTime(ctw.timeSelection);
    }

    private void ChangeFileTime(string newTime)
    {
      //fileWindowCount.m_StartTime = newTime;
      DateTime currentInterval;
      DateTime.TryParse(newTime, out currentInterval);
      string endTime = currentInterval.AddMinutes(fileWindowCount.m_NumIntervals * 5.0).TimeOfDay.ToString().Remove(5, 3);
      DateTime lastInterval = currentInterval.AddMinutes((fileWindowCount.m_NumIntervals * 5.0) - 5);
      const double iLength = 5.0;
      m_thisFileIntervals.Clear();
      rowHeaderMap.Clear();
      Count tempCount = new Count(newTime, endTime == "00:00" ? "24:00" : endTime, fileWindowCount.m_ParentIntersection);
      for (int i = 0; i < fileWindowCount.m_NumIntervals; i++)
      {
        m_thisFileIntervals.Add(currentInterval.TimeOfDay.ToString().Remove(5, 3));
        rowHeaderMap.Add(i, currentInterval.TimeOfDay.ToString().Remove(5, 3));
        for (int j = 0; j < fileWindowCount.m_Data.Tables.Count; j++)
        {
          var map = fileWindowCount.m_ParentIntersection.m_ParentProject.m_ColumnHeaders[j];
          for (int k = 1; k < fileWindowCount.m_Data.Tables[j].Rows[i].ItemArray.Length; k++)
          {
            tempCount.m_Data.Tables[j].Rows[i][map[k]] = fileWindowCount.m_Data.Tables[j].Rows[i][map[k]];
          }
        }
        currentInterval = currentInterval.AddMinutes(iLength);
      }
      fileWindowCount = tempCount;
      SetInfoBarIntervalText();
      fileWindowDataGrid.ItemsSource = new DataView(fileWindowCount.m_Data.Tables[currentBank]);
      if (fileWindowDataGrid.Columns.Count > 0)
      {
        fileWindowDataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
    }

    private void rotateData_Click(object sender, RoutedEventArgs e)
    {
      RotateDataWindow rotateWindow = new RotateDataWindow(fileWindowCount);
      rotateWindow.Owner = this;
      bool? result = rotateWindow.ShowDialog();
      if (result == true)
      {
        ClearStacks();
        PopulateDataGrid();
      }

    }

    private void ApplyData_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Copy All Data in all Banks and Close Window. \nAre you sure?", "Copy All", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        bool copyZeroes = true; //(bool)CopyZeros.IsChecked;
        string endTime = DateTime.Parse(fileWindowCount.m_EndTime).AddMinutes(-5).TimeOfDay.ToString().Remove(5, 3);
        if (!m_RefCount.CopyData(m_fileName, ref fileWindowCount.m_Data, copyZeroes, fileWindowCount.m_StartTime, endTime))
        {
          var warningResult = MessageBox.Show("Data did not overlap with associated count time period.  Close file data?", "Warning", MessageBoxButton.OKCancel);
          if (warningResult == MessageBoxResult.Cancel)
          {
            return;
          }
        }
        DialogResult = true;
      }
    }

    private void ApplySelected_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Copy Selected Data in Selected Banks and Close Window. \nAre you sure?", "Copy All", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        bool foundBadTime = false;
        Dictionary<string, string> badTimes = new Dictionary<string, string>();
        List<string> banksToInclude = new List<string>();
        if (allBanksChecked == true)
        {
          banksToInclude = m_RefCount.m_ParentIntersection.m_ParentProject.m_Banks.Where(x => x != "NOT USED").ToList<string>();
        }
        else
        {
          foreach (var box in bankPanel.Children)
          {
            if (box.GetType() == (typeof(CheckBox)))
            {
              if (((CheckBox)box).IsChecked == true)
              {
                banksToInclude.Add(((CheckBox)box).Content.ToString());
              }
            }
          }
        }
        bool copyZeroes = true;  // (bool)CopyZeros.IsChecked;
        foreach (var item in fileWindowDataGrid.SelectedCells)
        {
          DataRowView row = item.Item as DataRowView;
          if(item.Column.DisplayIndex == 0)
          {
            continue;
          }
          string startTime = row.Row.ItemArray[0].ToString();
          string endTime = DateTime.Parse(startTime).AddMinutes(5).TimeOfDay.ToString().Remove(5, 3);
          if (!m_RefCount.CopyData(m_fileName, ref fileWindowCount.m_Data, copyZeroes, startTime, startTime, new List<int> { item.Column.DisplayIndex }, banksToInclude))
          {
            foundBadTime = true;
            badTimes.Add(row.Row.ItemArray[0].ToString(), item.Column.Header.ToString());
          }
        }
        if (foundBadTime)
        {
          StringBuilder errorMessage = new StringBuilder();
          errorMessage.AppendLine("Some cells did not overlap with associated count time period.  Results may not have been as expected.");
          foreach (var pair in badTimes)
          {
            errorMessage.AppendLine("Interval: " + pair.Key + "  Movement: " + pair.Value);
          }
          var warningResult = MessageBox.Show(errorMessage.ToString(), "Warning", MessageBoxButton.OK);
        }
        DialogResult = true;
      }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void PopulateDataGrid()
    {
      fileWindowDataGrid.ItemsSource = new DataView(fileWindowCount.m_Data.Tables[currentBank]);
      if (fileWindowDataGrid.Columns.Count > 0)
      {
        fileWindowDataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
    }

    #region Cell Selection Logic

    private void CellSelection_Changed(object sender, SelectedCellsChangedEventArgs e)
    {
      if (fileWindowDataGrid.SelectedCells.Count < 1)
      {
        selectedRows.Clear();
        selectedColumns.Clear();
        return;
      }
      UpdateSelectedColumnsRows(e);
      UpdateInfoBar();
    }

    private void UpdateSelectedColumnsRows(SelectedCellsChangedEventArgs e)
    {
      foreach (var cell in e.AddedCells)
      {
        if (cell.Item != null)
        {
          DataRowView row = cell.Item as DataRowView;
          if (!selectedRows.Contains(row.Row.ItemArray[0].ToString()))
          {
            selectedRows.Add(row.Row.ItemArray[0].ToString());
          }
        }
        if (!selectedColumns.Contains(cell.Column.DisplayIndex))
        {
          selectedColumns.Add(cell.Column.DisplayIndex);
        }
      }
      foreach (var cell in e.RemovedCells)
      {
        if (cell.Item != null)
        {
          DataRowView row = cell.Item as DataRowView;
          if (selectedRows.Contains(row.Row.ItemArray[0].ToString()))
          {
            selectedRows.Remove(row.Row.ItemArray[0].ToString());
          }
        }
        if (selectedColumns.Contains(cell.Column.DisplayIndex))
        {
          selectedColumns.Remove(cell.Column.DisplayIndex);
        }
      }
    }

    private void UpdateInfoBar()
    {
      if (!IntervalBoxCheck(startingInterval) || !IntervalBoxCheck(endingInterval))
      {
        return;
      }
      // Don't update anything if there is nothing selected.
      if (fileWindowDataGrid.SelectedCells.Count < 1)
      {
        return;
      }

      ResetApproachDictionary();
      var selectedCells = fileWindowDataGrid.SelectedCells;

      int lastRowIndex = fileWindowDataGrid.Items.Count - 1;
      DateTime firstInterval;
      DateTime lastInterval;

      startingInterval.Text =
        ((DataRowView)selectedCells[0].Item).Row.ItemArray[0].ToString();
      endingInterval.Text =
       ((DataRowView)selectedCells[selectedCells.Count - 1].Item).Row.ItemArray[0].ToString();
      DateTime.TryParse(startingInterval.Text, out firstInterval);
      DateTime.TryParse(endingInterval.Text, out lastInterval);

      var approaches = new List<string>();
      List<int> columnIdxs = new List<int>();
      foreach (DataGridCellInfo cell in selectedCells)
      {
        var col = cell.Column.Header.ToString();
        SanitizePedSuffix(ref col);
        if (!approaches.Contains(col) && col != "Time")
        {
          approaches.Add(col);
        }
        if (!columnIdxs.Contains(cell.Column.DisplayIndex))
        {
          columnIdxs.Add(cell.Column.DisplayIndex);
        }

        DataRowView d = cell.Item as DataRowView;
        DateTime cellInterval;
        DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
        if (cellInterval < firstInterval)
        {
          firstInterval = cellInterval;
          startingInterval.Text =
            d.Row.ItemArray[0].ToString();
        }
        else if (cellInterval > lastInterval)
        {
          lastInterval = cellInterval;
          endingInterval.Text =
            d.Row.ItemArray[0].ToString();
        }
      }
    
      foreach (string app in approaches)
      {
        m_Movements[app].IsChecked = true;
      }
      ParentCheckBoxCheck(SBPanel, SBCheckBox);
      ParentCheckBoxCheck(WBPanel, WBCheckBox);
      ParentCheckBoxCheck(NBPanel, NBCheckBox);
      ParentCheckBoxCheck(EBPanel, EBCheckBox);

    }

    private void SanitizePedSuffix(ref string colHeader)
    {
      if (colHeader.Length == 3 && (colHeader.Contains("-") || colHeader.Contains("U")))
      {
        colHeader = colHeader.Remove(2, 1);
      }
      else if (colHeader.Length == 4 && (colHeader.Contains("RT") || colHeader.Contains("RP") || colHeader.Contains("RH")
        || colHeader.Contains("UP") || colHeader.Contains("UH")))
      {
        colHeader = colHeader.Remove(2, 2);
      }
      else
      {
        return;
      }

      StringBuilder sb = new StringBuilder(colHeader);
      sb.Append("P");
      colHeader = sb.ToString();
    }

    //private void UpdateCellSelection()
    //{
    //  if (!IntervalBoxCheck(startingInterval) || !IntervalBoxCheck(endingInterval))
    //  {
    //    return;
    //  }

    //  if (EndIntervalIsBeforeStartInterval())
    //  {
    //    intervalRangeErrorMessage.Text = "Starting Interval must be before Ending Interval!";
    //    fileWindowDataGrid.SelectedCells.Clear();
    //  }
    //  else
    //  {
    //    intervalRangeErrorMessage.Text = "";
    //  }

    //  int firstRowIndex = 0;
    //  int lastRowIndex = fileWindowDataGrid.Items.Count - 1;
    //  List<int> columns = new List<int>();

    //  for (int i = 0; i < fileWindowDataGrid.Items.Count; i++)
    //  {
    //    DataRowView row = fileWindowDataGrid.Items.GetItemAt(i) as DataRowView;
    //    if (row.Row.ItemArray[0].ToString() == startingInterval.Text)
    //    {
    //      firstRowIndex = i;
    //    }
    //    if (row.Row.ItemArray[0].ToString() == endingInterval.Text)
    //    {
    //      lastRowIndex = i;
    //    }
    //  }
    //  int j = 1;
    //  foreach (var move in m_Movements)
    //  {
    //    if (move.Value.IsChecked == true)
    //    {
    //      columns.Add(j);
    //    }
    //    j++;
    //  }
    //  List<KeyValuePair<int, int>> cellsToSelect = new List<KeyValuePair<int, int>>();
    //  foreach (var colIdx in columns)
    //  {
    //    for (int k = firstRowIndex; k <= lastRowIndex; k++)
    //    {
    //      cellsToSelect.Add(new KeyValuePair<int, int>(k, colIdx));
    //    }
    //  }

    //  SelectCellsByIndexes(fileWindowDataGrid, cellsToSelect);
    //}

    public static void SelectCellsByIndexes(DataGrid dataGrid, IList<KeyValuePair<int, int>> cellIndexes)
    {
      dataGrid.SelectedCells.Clear();
      foreach (KeyValuePair<int, int> cellIndex in cellIndexes)
      {
        int rowIndex = cellIndex.Key;
        int columnIndex = cellIndex.Value;

        if (rowIndex < 0 || rowIndex > (dataGrid.Items.Count - 1))
          throw new ArgumentException(string.Format("{0} is an invalid row index.", rowIndex));

        if (columnIndex < 0 || columnIndex > (dataGrid.Columns.Count - 1))
          throw new ArgumentException(string.Format("{0} is an invalid column index.", columnIndex));

        object item = dataGrid.Items[rowIndex]; //= Product X
        DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
        if (row == null)
        {
          dataGrid.ScrollIntoView(item);
          row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
        }
        if (row != null)
        {
          DataGridCell cell = GetCell(dataGrid, row, columnIndex);
          if (cell != null)
          {
            DataGridCellInfo dataGridCellInfo = new DataGridCellInfo(cell);
            dataGrid.SelectedCells.Add(dataGridCellInfo);
            cell.Focus();
          }
        }
      }
    }

    public static DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
    {
      if (rowContainer != null)
      {
        DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
        if (presenter == null)
        {
          /* if the row has been virtualized away, call its ApplyTemplate() method
           * to build its visual tree in order for the DataGridCellsPresenter
           * and the DataGridCells to be created */
          rowContainer.ApplyTemplate();
          presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
        }
        if (presenter != null)
        {
          DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
          if (cell == null)
          {
            /* bring the column into view
             * in case it has been virtualized away */
            dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
            cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
          }
          return cell;
        }
      }
      return null;
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

    #endregion

    #region Info Bar Logic

    //private void RotationSelectionMethod_Changed(object sender, RoutedEventArgs e)
    //{
    //  if (selectByGrid.IsChecked == true)
    //  {
    //    approachesSection.IsEnabled = false;
    //    rangeSection.IsEnabled = false;
    //    fileWindowDataGrid.IsHitTestVisible = true;
    //    fileWindowDataGrid.Opacity = 1.0;
    //  }
    //  if (selectByInfoBar.IsChecked == true)
    //  {
    //    approachesSection.IsEnabled = true;
    //    rangeSection.IsEnabled = true;
    //    fileWindowDataGrid.IsHitTestVisible = false;
    //    fileWindowDataGrid.Opacity = .9;
    //  }
    //}

    private bool IntervalBoxCheck(TextBox box)
    {
      DateTime time;
      string convertedTime = box.Text;
      if (box.Text.Length > 3 && DateTime.TryParse(box.Text, out time))
      {
        if (box.Text.Split(':')[0].Length < 2)
        {
          convertedTime = "0" + box.Text;
        }
      }

      if (!m_thisFileIntervals.Contains(convertedTime))
      {
        fileWindowDataGrid.SelectedCells.Clear();
        box.BorderBrush = new SolidColorBrush(Colors.Red);
        intervalRangeErrorMessage.Text = "Invalid Interval";
        return false;
      }
      box.Text = convertedTime;
      intervalRangeErrorMessage.Text = "";
      box.BorderBrush = new SolidColorBrush(Colors.Black);

      return true;
    }

    private bool EndIntervalIsBeforeStartInterval()
    {
      DateTime currentInterval;
      DateTime lastInterval;
      DateTime.TryParse(startingInterval.Text, out currentInterval);
      DateTime.TryParse(endingInterval.Text, out lastInterval);
      if (lastInterval <= currentInterval)
      {
        return true;
      }

      return false;
    }

    private void ParentCheckBoxCheck(StackPanel panel, CheckBox box)
    {
      box.IsChecked = null;
      List<CheckBox> children = new List<CheckBox>();

      foreach (var child in panel.Children)
      {
        if (child.GetType() == (typeof(CheckBox)))
        {
          children.Add((CheckBox)child);
        }
      }
      if (children[1].IsChecked == true && children[2].IsChecked == true
          && children[3].IsChecked == true && children[4].IsChecked == true)
      {
        box.IsChecked = true;
      }
      if (children[1].IsChecked == false && children[2].IsChecked == false
          && children[3].IsChecked == false && children[4].IsChecked == false)
      {
        box.IsChecked = false;
      }
    }

    private void ResetApproachDictionary()
    {
      foreach (var kv in m_Movements)
      {
        kv.Value.IsChecked = false;
      }
    }

    private void HeaderData_Changed(object sender, TextChangedEventArgs e)
    {
      //if (selectByGrid.IsChecked == true)
      //{
      //  return;
      //}
      //UpdateCellSelection();
    }

    private void ApproachCheck_Changed(object sender, RoutedEventArgs e)
    {
      CheckBox box = (CheckBox)sender;
      List<CheckBox> children = new List<CheckBox>();
      foreach (var child in ((StackPanel)box.Parent).Children)
      {
        if (child.GetType() == (typeof(CheckBox)))
        {
          children.Add((CheckBox)child);
        }
      }
      if (box.IsChecked == true)
      {
        children[1].IsChecked = children[2].IsChecked = children[3].IsChecked = children[4].IsChecked = true;
      }

      if (box.IsChecked == false)
      {
        children[1].IsChecked = children[2].IsChecked = children[3].IsChecked = children[4].IsChecked = false;
      }
    }

    private void MovementCheck_Changed(object sender, RoutedEventArgs e)
    {
      StackPanel parentPanel = (StackPanel)((CheckBox)sender).Parent;
      CheckBox approachBox = (CheckBox)parentPanel.Children[0];
      ParentCheckBoxCheck(parentPanel, approachBox);
      //if (selectByGrid.IsChecked == true)
      //{
      //  return;
      //}
      //UpdateCellSelection();
    }

    private void DataGridHandleKeyPress(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        redoStack.Clear();
        UndoStackPush();
        DeleteSelectedCells(sender as DataGrid);
      }
    }

    private void CellEdit_End(object sender, DataGridCellEditEndingEventArgs e)
    {
      redoStack.Clear();
      UndoStackPush();
      int value;
      try
      {
        value = Int16.Parse(((TextBox)e.EditingElement).Text);
      }
      catch
      {
        //((TextBox)e.EditingElement).BorderBrush = System.Windows.Media.Brushes.Red;
        return;
      }
      int row = ((DataGrid)sender).ItemContainerGenerator.IndexFromContainer(e.Row);
      fileWindowCount.EditSingleCell(row, e.Column.DisplayIndex, currentBank, value);
    }

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      redoStack.Clear();
      UndoStackPush();
      try
      {
        var clippy = Clipboard.GetData(DataFormats.CommaSeparatedValue);
        if (clippy == null)
        {
          return;
        }
        // Parse Clipboard into cell array
        string[] rawClipData = clippy.ToString().Replace("\r", "").Trim().Split('\n');
        int clipRows = rawClipData.Length;
        int clipCols = rawClipData[0].Split(',').Length;

        string firstCell = rawClipData[0].Split(',')[0];
        DateTime tempTime;
        bool didUserGrabTimeColumn = DateTime.TryParse(firstCell, out tempTime);
        int startingClipCol = didUserGrabTimeColumn ? 1 : 0;

        // Determine selected cell top left index
        DataGrid thisGrid = ((DataGrid)sender);
        int topRow = 500;
        int leftCol = 500;
        if (thisGrid.SelectedCells.Count > 0)
        {
          foreach (var selCell in thisGrid.SelectedCells)
          {
            if (selCell.Column.DisplayIndex < leftCol)
            {
              leftCol = selCell.Column.DisplayIndex;
            }
            DataRowView d = selCell.Item as DataRowView;

            DateTime thisTime;
            DateTime countStartTime;
            string intervalTime = d.Row.ItemArray[0].ToString();
            DateTime.TryParse(intervalTime, out thisTime);
            DateTime.TryParse(fileWindowCount.m_StartTime, out countStartTime);
            if (thisTime < countStartTime)
            {
              thisTime = thisTime.AddDays(1);
            }
            int rowIndexDifference = (int)(thisTime - countStartTime).TotalMinutes / (int)fileWindowCount.m_ParentIntersection.m_ParentProject.m_IntervalLength;
            if (rowIndexDifference < topRow)
            {
              topRow = rowIndexDifference;
            }
          }
        }
        if (topRow > fileWindowCount.m_NumIntervals || leftCol > 16)
        {
          return;
        }
        //Determine actual pasting eligible area

        if (leftCol == 0)
        {
          leftCol++;
        }

        int pasteRows = (fileWindowCount.m_NumIntervals - topRow) < clipRows ? fileWindowCount.m_NumIntervals - topRow : clipRows;
        int pasteCols = (17 - leftCol) < clipCols ? 17 - leftCol : clipCols;


        // Paste cells
        DataTable thisTable = fileWindowCount.m_Data.Tables[currentBank];
        bool foundBadClipData = false;

        for (int i = 0; i < pasteRows; i++)
        {
          string[] thisPasteLine = rawClipData[i].Split(',');
          int c = leftCol;
          for (int j = startingClipCol; j < pasteCols; j++)
          {
            short targetValue = -1;
            if (Int16.TryParse(thisPasteLine[j], out targetValue))
            {
              thisTable.Rows[topRow][fileWindowCount.m_ParentIntersection.m_ParentProject.m_ColumnHeaders[currentBank][c]] = targetValue;
              c++;
            }
            else
            {
              foundBadClipData = true;
            }
          }
          topRow++;
        }
        fileWindowCount.RunDataState();
        PopulateDataGrid();
        fileWindowDataGrid.Focus();
        if (foundBadClipData)
        {
          MessageBox.Show("The Clipboard contained some non-numerical data.\nInvalid cells were skipped", "Paste Warning", MessageBoxButton.OK);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("Unexpected error with paste.  Check the details of the operation and try again.\n\n" + ex.Message, "Paste Failed", MessageBoxButton.OK);
      }
    }

    private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (fileWindowDataGrid.SelectedCells.Count > 0)
      {
        redoStack.Clear();
        UndoStackPush();
        ApplicationCommands.Copy.Execute(null, fileWindowDataGrid);
        DeleteSelectedCells(fileWindowDataGrid);
      }
    }

    private void DeleteSelectedCells(DataGrid grid)
    {
      foreach (var cell in grid.SelectedCells)
      {
        DataRowView d = cell.Item as DataRowView;
        DateTime cellInterval;
        DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
        string endPoint = cellInterval.AddMinutes(fileWindowCount.GetIntervalLength()).TimeOfDay.ToString().Remove(5, 3);
        fileWindowCount.ClearData(d.Row.ItemArray[0].ToString(), endPoint, new List<int> { cell.Column.DisplayIndex }, new List<string> { currentBank.ToString() });
      }
      grid.Focus();
    }

    private void UndoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        fileWindowCount.m_Data.Tables[currentBank],
        currentBank);

      if (undoStack.Count < 1 || undoStack.Peek() != newState)
      {
        undoStack.Push(newState);
      }
    }

    private void RedoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        fileWindowCount.m_Data.Tables[currentBank],
        currentBank);

      if (redoStack.Count < 1 || redoStack.Peek() != newState)
      {
        redoStack.Push(newState);
      }
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (undoStack.Count > 0)
      {
        RedoStackPush();

        MerlinDataTableState restoreThis = undoStack.Pop();
        bankTabs.SelectedIndex = restoreThis.Bank;
        fileWindowCount.CopyData(restoreThis.Table, restoreThis.Bank);

        PopulateDataGrid();
        fileWindowDataGrid.Focus();
      }
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (redoStack.Count > 0)
      {
        UndoStackPush();

        MerlinDataTableState restoreThis = redoStack.Pop();
        bankTabs.SelectedIndex = restoreThis.Bank;
        fileWindowCount.CopyData(restoreThis.Table, restoreThis.Bank);

        PopulateDataGrid();
        fileWindowDataGrid.Focus();
      }
    }

    private void ClearStacks()
    {
      undoStack.Clear();
      redoStack.Clear();
    }

    #endregion

    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
      var dataGrid = (DataGrid)sender;
      if (dataGrid.Columns.Count > 0)
      {
        dataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
    }

    private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      var grid = (DataGrid)sender;
      grid.Focus();
      if (grid.SelectedCells.Count == grid.Columns.Count * grid.Items.Count)
      {
        grid.SelectedCells.Clear();
      }
      else
      {
        grid.SelectAll();
      }
    }
    
    private void AllBanksCheck_Changed(object sender, RoutedEventArgs e)
    {
      allBanksChecked = ((CheckBox)sender).IsChecked;
      CheckBox box = (CheckBox)sender;
      List<CheckBox> children = new List<CheckBox>();
      changingBankBoxes = true;
      if (allBanksChecked == true)
      {
        foreach (var child in bankPanel.Children)
        {
          if (child.GetType() == (typeof(CheckBox)))
          {
            ((CheckBox)child).IsChecked = true;
          }
        }
      }
      if (allBanksChecked == false)
      {
        foreach (var child in bankPanel.Children)
        {
          if (child.GetType() == (typeof(CheckBox)))
          {
            ((CheckBox)child).IsChecked = false;
          }
        }
      }
      changingBankBoxes = false;
    }

    private void BankBox_Checked(object sender, RoutedEventArgs e)
    {
      if (!changingBankBoxes)
      {
        WrapPanel panel = (WrapPanel)((CheckBox)sender).Parent;
        if (panel != null)
        {
          StackPanel parentPanel = (StackPanel)panel.Parent;
          if (parentPanel != null)
          {
            CheckBox allBanksBox = (CheckBox)parentPanel.Children[1];
            BankParentCheckBoxCheck(panel, allBanksBox);
          }
        }
      }
    }

    private void BankParentCheckBoxCheck(WrapPanel panel, CheckBox box)
    {
      box.IsChecked = null;
      List<CheckBox> children = new List<CheckBox>();
      bool allAreChecked = true;
      bool noneAreChecked = true;
      foreach (var child in panel.Children)
      {
        if (child.GetType() == (typeof(CheckBox)))
        {
          if (((CheckBox)child).IsChecked == true)
          {
            noneAreChecked = false;
          }
          else
          {
            allAreChecked = false;
          }
        }
      }
      if (allAreChecked)
      {
        box.IsChecked = true;
      }
      if (noneAreChecked)
      {
        box.IsChecked = false;
      }
    }
  }
}
