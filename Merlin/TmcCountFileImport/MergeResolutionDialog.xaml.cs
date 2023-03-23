using AppMerlin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Merlin.DataTabPages;
using System.Text;
using Merlin.ExtensionClasses;

namespace Merlin.TmcCountFileImport
{
  /// <summary>
  /// Interaction logic for FileDialog4.xaml
  /// </summary>
  public partial class MergeResolutionDialog : Window
  {
    //private TMCProject project;
    private Count fileCount;
    public string fileName;
    private Count currentCount;
    List<KeyValuePair<int, int>> conflictedRedData;
    List<KeyValuePair<int, int>> conflictedOrangeData;
    public int currentBank;
    private bool changingBanks = false;
    private Style redCellStyle;
    private Style blackCellStyle;
    private Style orangeCellStyle;
    public Stack<MerlinDataTableState> undoStack = new Stack<MerlinDataTableState>();
    public Stack<MerlinDataTableState> redoStack = new Stack<MerlinDataTableState>();
    public bool useCurrentCount = true;
    public bool applyAllData = true;
    public bool? allBanksChecked = true;
    private bool changingBankBoxes = false;
    public Tuple<DateTime, DateTime> timeBoundaries;

    private List<string> selectedRows;
    private List<int> selectedColumns;
    private Dictionary<string, CheckBox> m_Movements;


    public MergeResolutionDialog(Count currentdata, Count fc, string file, Tuple<DateTime, DateTime> timeBoundaries)
    {
      fileCount = fc;
      fileName = file;
      currentCount = currentdata;
      this.timeBoundaries = timeBoundaries;
      conflictedRedData = new List<KeyValuePair<int, int>>();
      conflictedOrangeData = new List<KeyValuePair<int, int>>();
      selectedRows = new List<string>();
      selectedColumns = new List<int>();
      currentBank = 0;
      redCellStyle = new Style(typeof(DataGridCell));
      blackCellStyle = new Style(typeof(DataGridCell));
      orangeCellStyle = new Style(typeof(DataGridCell));
      SetupStyles();
      InitializeComponent();
      SetupBankTabs();
      SetupGrids();
      SetupBankBoxes();
      SetDefaultState();
      Resources["allBanksChecked"] = allBanksChecked;
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

    private void SetupStyles()
    {
      BrushConverter bc = new BrushConverter();
      Brush redBrush = (Brush)bc.ConvertFrom("#E37681");
      redBrush.Freeze();
      Brush orangeBrush = (Brush)bc.ConvertFrom("#E3A776");
      redCellStyle.Setters.Add(new Setter(BackgroundProperty, redBrush));
      redCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      orangeCellStyle.Setters.Add(new Setter(BackgroundProperty, orangeBrush));
      orangeCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      blackCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

    }

    private void SetupBankTabs()
    {
      for (int i = 0; i < currentCount.m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (currentCount.m_ParentIntersection.m_ParentProject.m_Banks[i] != "NOT USED"
          || currentCount.m_ParentIntersection.m_ParentProject.m_PedBanks[i] != PedColumnDataType.NA)
        {
          TabItem tab1 = new TabItem();
          TabItem tab2 = new TabItem();
          if (!currentCount.m_ParentIntersection.m_ParentProject.m_TCCDataFileRules)
          {
            tab1.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
            tab2.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
            tab1.Header = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
            tab2.Header = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
          }
          else
          {
            if (currentCount.m_ParentIntersection.m_ParentProject.m_Banks[i] == "FHWAPedsBikes")
            {
              tab1.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab2.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab1.Header = "Bikes & Peds";
              tab2.Header = "Bikes & Peds";
            }
            else
            {
              tab1.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab2.Tag = currentCount.m_ParentIntersection.m_ParentProject.GetCombinedBankNames(i);
              tab1.Header = currentCount.m_ParentIntersection.m_ParentProject.m_BankDictionary[i];
              tab2.Header = currentCount.m_ParentIntersection.m_ParentProject.m_BankDictionary[i];
            }
          }
          fileTabs.Items.Add(tab1);
          countTabs.Items.Add(tab2);
        }
      }
    }

    private void SetupGrids()
    {
      PopulateDataGrid();
      mainHeader.Text =
      currentCount.m_Id + " - " +
      currentCount.m_ParentIntersection.GetLocationName();
      mainHeader.FontWeight = FontWeights.Bold;
      mainHeader.FontSize = 24;

      fileCountHeader.Text = fileName;
      fileCountHeader.FontSize = 18;
      currentCountHeader.Text = "Current Data";
      currentCountHeader.FontSize = 18;
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
      if (currentCount.m_ParentIntersection.m_ParentProject.m_NCDOTColSwappingEnabled)
      {
        columnSwappingNotification.Visibility = Visibility.Visible;
      }
      else
      {
        columnSwappingNotification.Visibility = Visibility.Collapsed;
      }
    }

    private void SetupBankBoxes()
    {
      foreach (var bank in currentCount.m_ParentIntersection.m_ParentProject.m_Banks)
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
      allBanksCheckBox.IsChecked = true;
    }

    private void windowContent_Rendered(object sender, EventArgs e)
    {
      IdentifyConflicts();
      HighlightConflicts();
    }

    private void PopulateDataGrid()
    {
      fileDataGrid.ItemsSource = new DataView(fileCount.m_Data.Tables[currentBank]);
      if (fileDataGrid.Columns.Count > 0)
      {
        fileDataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
      countDataGrid.ItemsSource = new DataView(currentCount.m_Data.Tables[currentBank]);

      if (countDataGrid.Columns.Count > 0)
      {
        countDataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
      IdentifyConflicts();
      HighlightConflicts();
    }

    private void IdentifyConflicts()
    {
      if (fileDataGrid.Columns.Count <= 0)
      {
        return;
      }
      conflictedRedData.Clear();
      conflictedOrangeData.Clear();
      for (int i = 0; i < fileCount.m_Data.Tables[currentBank].Rows.Count; i++)
      {
        for (int j = 1; j < fileCount.m_Data.Tables[currentBank].Columns.Count; j++)
        {
          int fileCellValue = (Int16)fileCount.m_Data.Tables[currentBank].Rows[i][j];
          int countCellValue = (Int16)currentCount.m_Data.Tables[currentBank].Rows[i][j];

          DateTime thisTime = DateTime.Parse(fileCount.m_Data.Tables[0].Rows[i].ItemArray[0].ToString());

          if (fileCellValue != countCellValue)
          {
            if (thisTime >= timeBoundaries.Item1 && thisTime <= timeBoundaries.Item2)
            {
              conflictedRedData.Add(new KeyValuePair<int, int>(i, j));
            }
            else
            {
              conflictedOrangeData.Add(new KeyValuePair<int, int>(i, j));
            }
          }
        }
      }
    }

    private void HighlightConflicts()
    {
      if (fileDataGrid.Columns.Count <= 0)
      {
        return;
      }
      foreach (KeyValuePair<int, int> conflict in conflictedRedData)
      {
        int rowIndex = conflict.Key;
        int columnIndex = conflict.Value;

        if (rowIndex >= 0 && rowIndex < fileDataGrid.Items.Count
          && columnIndex > 0 && columnIndex < fileDataGrid.Columns.Count)
        {
          object item = fileDataGrid.Items[rowIndex]; 
          DataGridRow row = fileDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          if (row == null)
          {
            fileDataGrid.ScrollIntoView(item);
            row = fileDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          }
          if (row != null)
          {
            DataGridCell cell = GetCell(fileDataGrid, row, columnIndex);
            if (cell != null)
            {
              cell.Style = redCellStyle;
            }
          }
        }
      }
      foreach (KeyValuePair<int, int> conflict in conflictedOrangeData)
      {
        int rowIndex = conflict.Key;
        int columnIndex = conflict.Value;

        if (rowIndex >= 0 && rowIndex < fileDataGrid.Items.Count
          && columnIndex > 0 && columnIndex < fileDataGrid.Columns.Count)
        {
          object item = fileDataGrid.Items[rowIndex];
          DataGridRow row = fileDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          if (row == null)
          {
            fileDataGrid.ScrollIntoView(item);
            row = fileDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          }
          if (row != null)
          {
            DataGridCell cell = GetCell(fileDataGrid, row, columnIndex);
            if (cell != null)
            {
              cell.Style = orangeCellStyle;
            }
          }
        }
      }
      if (fileDataGrid.Items.Count > 0 && fileDataGrid.Items[0] != null)
      {
        fileDataGrid.ScrollIntoView(fileDataGrid.Items[0]);
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

    private void bankTab_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (changingBanks)
      {
        return;
      }
      changingBanks = true;
      string bank = ((string)((TabItem)((TabControl)sender).SelectedItem).Tag).Split('&')[0].Trim();
      string pedBank = ((string)((TabItem)((TabControl)sender).SelectedItem).Tag).Split('&')[1].Trim();
      int index = ((TabControl)sender).SelectedIndex;
      for (int i = 0; i < currentCount.m_ParentIntersection.m_ParentProject.m_Banks.Count; i++)
      {
        if (bank == currentCount.m_ParentIntersection.m_ParentProject.m_Banks[i] && pedBank
          == currentCount.m_ParentIntersection.m_ParentProject.m_PedBanks[i].ToString())
        {
          currentBank = i;
          break;
        }
      }

      if (sender == fileTabs)
      {
        countTabs.SelectedItem = countTabs.Items[index];
      }
      else
      {
        fileTabs.SelectedItem = fileTabs.Items[index];
      }
      using (new WaitCursor())
      {
        PopulateDataGrid();
      }
      changingBanks = false;
    }

    private void rotateData_Click(object sender, RoutedEventArgs e)
    {
      RotateDataWindow rotateWindow = new RotateDataWindow(fileCount);
      rotateWindow.Owner = this;
      bool? result = rotateWindow.ShowDialog();
      if (result == true)
      {
        ClearStacks();
        PopulateDataGrid();
      }

    }

    private void dataGridHandleKeyPress(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        redoStack.Clear();
        UndoStackPush();
        DeleteSelectedCells(sender as DataGrid);
      }
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
            DateTime.TryParse(fileCount.m_StartTime, out countStartTime);
            if (thisTime < countStartTime)
            {
              thisTime = thisTime.AddDays(1);
            }
            int rowIndexDifference = (int)(thisTime - countStartTime).TotalMinutes / (int)currentCount.m_ParentIntersection.m_ParentProject.m_IntervalLength;
            if (rowIndexDifference < topRow)
            {
              topRow = rowIndexDifference;
            }
          }
        }
        if (topRow > fileCount.m_NumIntervals || leftCol > 16)
        {
          return;
        }
        //Determine actual pasting eligible area

        if (leftCol == 0)
        {
          leftCol++;
        }

        int pasteRows = (fileCount.m_NumIntervals - topRow) < clipRows ? fileCount.m_NumIntervals - topRow : clipRows;
        int pasteCols = (17 - leftCol) < clipCols ? 17 - leftCol : clipCols;


        // Paste cells
        DataTable thisTable = fileCount.m_Data.Tables[currentBank];
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
              thisTable.Rows[topRow][currentCount.m_ParentIntersection.m_ParentProject.m_ColumnHeaders[currentBank][c]] = targetValue;
              c++;
            }
            else
            {
              foundBadClipData = true;
            }
          }
          topRow++;
        }
        fileCount.RunDataState();
        PopulateDataGrid();
        fileDataGrid.Focus();
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
      if (fileDataGrid.SelectedCells.Count > 0)
      {
        redoStack.Clear();
        UndoStackPush();
        ApplicationCommands.Copy.Execute(null, fileDataGrid);
        DeleteSelectedCells(fileDataGrid);
      }
    }

    private void DeleteSelectedCells(DataGrid grid)
    {
      foreach (var cell in grid.SelectedCells)
      {
        DataRowView d = cell.Item as DataRowView;
        DateTime cellInterval;
        DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
        string endPoint = cellInterval.AddMinutes(fileCount.GetIntervalLength()).TimeOfDay.ToString().Remove(5, 3);
        fileCount.ClearData(d.Row.ItemArray[0].ToString(), endPoint, new List<int> { cell.Column.DisplayIndex }, new List<string> { currentBank.ToString() });
      }
      IdentifyConflicts();
      HighlightConflicts();
      grid.Focus();
    }

    private void UndoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        fileCount.m_Data.Tables[currentBank],
        currentBank);

      if (undoStack.Count < 1 || undoStack.Peek() != newState)
      {
        undoStack.Push(newState);
      }
    }

    private void RedoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        fileCount.m_Data.Tables[currentBank],
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
        fileTabs.SelectedIndex = restoreThis.Bank;
        fileCount.CopyData(restoreThis.Table, restoreThis.Bank);

        PopulateDataGrid();
        fileDataGrid.Focus();
      }
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (redoStack.Count > 0)
      {
        UndoStackPush();

        MerlinDataTableState restoreThis = redoStack.Pop();
        fileTabs.SelectedIndex = restoreThis.Bank;
        fileCount.CopyData(restoreThis.Table, restoreThis.Bank);

        PopulateDataGrid();
        fileDataGrid.Focus();
      }
    }
    private void ClearStacks()
    {
      undoStack.Clear();
      redoStack.Clear();
    }

    private void fileGridColumnHeader_Click(object sender, MouseButtonEventArgs e)
    {
      var columnHeader = sender as DataGridColumnHeader;
      if (e.ChangedButton == MouseButton.Left && columnHeader != null)
      {
        if (columnHeader.DataContext.ToString() == "")
        {
          fileDataGrid.SelectedCells.Clear();
          foreach (var column in fileDataGrid.Columns)
          {
            if (column.Header.ToString() == "")
            {
              continue;
            }
            foreach (var item in fileDataGrid.Items)
            {
              fileDataGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
          }
        }
        else
        {
          fileDataGrid.Focus();
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          {
            foreach (var item in fileDataGrid.Items)
            {
              fileDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
          else
          {
            fileDataGrid.SelectedCells.Clear();
            foreach (var item in fileDataGrid.Items)
            {
              fileDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
        }
      }
    }

    private void countGridColumnHeader_Click(object sender, MouseButtonEventArgs e)
    {
      var columnHeader = sender as DataGridColumnHeader;
      if (e.ChangedButton == MouseButton.Left && columnHeader != null)
      {
        if (columnHeader.DataContext.ToString() == "Interval")
        {
          countDataGrid.SelectedCells.Clear();
          foreach (var column in countDataGrid.Columns)
          {
            if (column.Header.ToString() == "Interval")
            {
              continue;
            }
            foreach (var item in countDataGrid.Items)
            {
              countDataGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
          }
        }
        else
        {
          countDataGrid.Focus();
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          {
            foreach (var item in countDataGrid.Items)
            {
              countDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
          else
          {
            countDataGrid.SelectedCells.Clear();
            foreach (var item in countDataGrid.Items)
            {
              countDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
        }
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
        return;
      }
      int row = ((DataGrid)sender).ItemContainerGenerator.IndexFromContainer(e.Row);
      fileCount.EditSingleCell(row, e.Column.DisplayIndex, currentBank, value);

      EvaluateCellConflict(e.Row, e.Column);

    }

    private bool EvaluateCellConflict(DataGridRow row, DataGridColumn column)
    {
      DataRow dr = ((DataRowView)row.Item).Row;
      int rowIndex = fileCount.m_Data.Tables[currentBank].Rows.IndexOf(dr);
      int colIndex = column.DisplayIndex;
      DataGridRow countRow = (DataGridRow)countDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
      KeyValuePair<int, int> kvp = new KeyValuePair<int, int>(rowIndex, colIndex);
      int newValue = int.Parse(((TextBox)column.GetCellContent(row)).Text);
      int countValue = int.Parse(currentCount.m_Data.Tables[currentBank].Rows[rowIndex][colIndex].ToString());
      if (newValue != countValue)
      {
        if (newValue == 0 || countValue == 0)
        {
          if (!conflictedOrangeData.Contains(kvp))
          {
            conflictedOrangeData.Add(kvp);
            DataRowView item = fileDataGrid.Items[rowIndex] as DataRowView;
            DataGridCell cell = (DataGridCell)fileDataGrid.Columns[colIndex].GetCellContent(item).Parent;
            cell.Style = orangeCellStyle;
          }
        }
        else
        {
          if (!conflictedRedData.Contains(kvp))
          {
            conflictedRedData.Add(kvp);
            DataRowView item = fileDataGrid.Items[rowIndex] as DataRowView;
            DataGridCell cell = (DataGridCell)fileDataGrid.Columns[colIndex].GetCellContent(item).Parent;
            cell.Style = redCellStyle;
          }
        }
        return true;
      }
      else
      {
        if (conflictedRedData.Contains(kvp))
        {
          DataRowView item = fileDataGrid.Items[rowIndex] as DataRowView;
          DataGridCell cell = (DataGridCell)fileDataGrid.Columns[colIndex].GetCellContent(item).Parent;
          cell.Style = blackCellStyle;
          conflictedRedData.Remove(kvp);
        }
        if (conflictedOrangeData.Contains(kvp))
        {
          DataRowView item = fileDataGrid.Items[rowIndex] as DataRowView;
          DataGridCell cell = (DataGridCell)fileDataGrid.Columns[colIndex].GetCellContent(item).Parent;
          cell.Style = blackCellStyle;
          conflictedOrangeData.Remove(kvp);
        }
        return false;
      }
    }


    #region Cell Selection Logic

    private void CellSelection_Changed(object sender, SelectedCellsChangedEventArgs e)
    {
      if (((DataGrid)sender).SelectedCells.Count < 1)
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
      // Don't update anything if there is nothing selected.
      if (fileDataGrid.SelectedCells.Count < 1)
      {
        return;
      }

      ResetApproachDictionary();
      var selectedCells = fileDataGrid.SelectedCells;

      int lastRowIndex = fileDataGrid.Items.Count - 1;
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
    
    #endregion

    #region Info Bar Logic
    
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
    }
    
    #endregion


    private void finished_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Use Current data, and do not import any data from this file. \nAre you sure?", "Copy All", MessageBoxButton.OKCancel); if (result == MessageBoxResult.OK)
      {
        DialogResult = true;
      }
    }

    private void got_Focus(object sender, RoutedEventArgs e)
    {
      //HighlightConflicts();
    }

    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
      var dataGrid = (DataGrid)sender;
      if (dataGrid.Columns.Count > 0)
      {
        dataGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
      IdentifyConflicts();
      HighlightConflicts();
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

    private void ApplyData_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Import ALL File Data in ALL Banks. \nAll banks and Red 0s will be imported. "
        + "\nOrange sections indicate the data is not present in the file, and will not be imported. \nAre you sure?", "Copy All", MessageBoxButton.OKCancel); 
      if (result == MessageBoxResult.OK)
      {
        useCurrentCount = false;
        DialogResult = true;
      }
    }

    private void ApplySelected_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Import SELECTED data in SELECTED banks (including selected 0s). \nAre you sure?", "Copy All", MessageBoxButton.OKCancel); if (result == MessageBoxResult.OK)
      {
        useCurrentCount = false;
        applyAllData = false;
        DialogResult = true;
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
