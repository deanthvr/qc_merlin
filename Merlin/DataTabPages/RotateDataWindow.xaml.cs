using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AppMerlin;
using CheckBox = System.Windows.Controls.CheckBox;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using TextBox = System.Windows.Controls.TextBox;
using System.Text;

namespace Merlin.DataTabPages
{
  /// <summary>
  /// Interaction logic for RotateDataWindow.xaml
  /// </summary>
  public partial class RotateDataWindow : Window
  {
    private Count m_RefCount;
    private Count m_tempCount;
    private Dictionary<string, CheckBox> m_Movements;
    private List<string> m_Intervals;
    private bool m_CurrentlyRotatingSelection;

    public RotateDataWindow(Count currentCount)
    {
      m_CurrentlyRotatingSelection = false;
      m_RefCount = currentCount;
      m_tempCount = m_RefCount.Copy();
      m_tempCount.InvertDataFileToCellMapping();
      m_Intervals = new List<string>();
      PopulateIntervalList();
      InitializeComponent();
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
      rotateWindowDataGrid.ItemsSource = new DataView(m_tempCount.m_Data.Tables[0]);
      nameOfSelectedData.Text = m_tempCount.m_Id + " - " +
        m_tempCount.m_ParentIntersection.GetLocationName(); ;
      SetDefaultState();
    }

    #region Set Up

    private void SetDefaultState()
    {
      rDirectionClockwise.IsChecked = true;
      nTurnsOne.IsChecked = true;
      selectByGrid.IsChecked = true;
      SBCheckBox.IsChecked = true;
      WBCheckBox.IsChecked = true;
      NBCheckBox.IsChecked = true;
      EBCheckBox.IsChecked = true;
      rotateWindowStartingInterval.Text = m_tempCount.m_StartTime;

      DateTime lastInterval;
      DateTime.TryParse(m_tempCount.m_EndTime, out lastInterval);
      lastInterval = lastInterval.AddMinutes(-5);
      rotateWindowEndingInterval.Text = lastInterval.TimeOfDay.ToString().Remove(5, 3);
    }

    private void PopulateIntervalList()
    {
      DateTime currentInterval;
      DateTime lastInterval;
      const double iLength = 5.0;
      DateTime.TryParse(m_tempCount.m_StartTime, out currentInterval);
      DateTime.TryParse(m_tempCount.m_EndTime, out lastInterval);
      while (currentInterval <= lastInterval)
      {
        m_Intervals.Add(currentInterval.TimeOfDay.ToString().Remove(5, 3));
        currentInterval = currentInterval.AddMinutes(iLength);
      }
    }

    #endregion

    #region Cell Selection Logic

    private void rotateWindowCellSelection_Changed(object sender, SelectedCellsChangedEventArgs e)
    {
      if (rotateWindowDataGrid.SelectedCells.Count < 1)
      {
        return;
      }
      UpdateInfoBar();
    }

    private void UpdateInfoBar()
    {
      //Don't updateInfo Bar if user is editing with it or if we are changing the selection after a rotation
      if (selectByInfoBar.IsChecked == true || m_CurrentlyRotatingSelection)
      {
        return;
      }
      //Don't update anything if the user has bogus information in the interval boxes.
      if (!IntervalBoxCheck(rotateWindowStartingInterval) || !IntervalBoxCheck(rotateWindowEndingInterval))
      {
        return;
      }
      // Don't update anything if there is nothing selected.
      if (rotateWindowDataGrid.SelectedCells.Count < 1)
      {
        return;
      }


      //rWindowDebugBox.Items.Clear();
      ResetApproachDictionary();
      var selectedCells = rotateWindowDataGrid.SelectedCells;
      int firstRowIndex = 0;
      int lastRowIndex = rotateWindowDataGrid.Items.Count - 1;
      DateTime firstInterval;
      DateTime lastInterval;
      bool cellSelectMustChange = false;
      rotateWindowStartingInterval.Text =
        ((DataRowView)selectedCells[0].Item).Row.ItemArray[0].ToString();
      rotateWindowEndingInterval.Text =
       ((DataRowView)selectedCells[selectedCells.Count - 1].Item).Row.ItemArray[0].ToString();
      DateTime.TryParse(rotateWindowStartingInterval.Text, out firstInterval);
      DateTime.TryParse(rotateWindowEndingInterval.Text, out lastInterval);

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

        //ListBoxItem item = new ListBoxItem();
        DataRowView d = cell.Item as DataRowView;
        DateTime cellInterval;
        DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
        if (cellInterval < firstInterval)
        {
          firstInterval = cellInterval;
          cellSelectMustChange = true;
          rotateWindowStartingInterval.Text =
            d.Row.ItemArray[0].ToString();
        }
        else if (cellInterval > lastInterval)
        {
          lastInterval = cellInterval;
          cellSelectMustChange = true;
          rotateWindowEndingInterval.Text =
            d.Row.ItemArray[0].ToString();
        }
        //item.Content = d.Row.ItemArray[0] + ", " + col;
        //rWindowDebugBox.Items.Add(item);
      }

      if (!cellSelectMustChange)
      {
        for (int i = 0; i < rotateWindowDataGrid.Items.Count; i++)
        {
          DataRowView row = rotateWindowDataGrid.Items.GetItemAt(i) as DataRowView;
          if (row.Row.ItemArray[0].ToString() == rotateWindowStartingInterval.Text)
          {
            firstRowIndex = i;
          }
          if (row.Row.ItemArray[0].ToString() == rotateWindowEndingInterval.Text)
          {
            lastRowIndex = i;
          }
        }
        List<KeyValuePair<int, int>> cellsToSelect = new List<KeyValuePair<int, int>>();
        foreach (var colIdx in columnIdxs)
        {
          for (int k = firstRowIndex; k <= lastRowIndex; k++)
          {
            cellsToSelect.Add(new KeyValuePair<int, int>(k, colIdx));
          }
        }
        m_CurrentlyRotatingSelection = true;
        SelectCellsByIndexes(rotateWindowDataGrid, cellsToSelect);
        m_CurrentlyRotatingSelection = false;
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

    private void UpdateCellSelection()
    {
      if (!IntervalBoxCheck(rotateWindowStartingInterval) || !IntervalBoxCheck(rotateWindowEndingInterval))
      {
        return;
      }

      if (EndIntervalIsBeforeStartInterval())
      {
        intervalRangeErrorMessage.Text = "Starting Interval must be before Ending Interval!";
        rotateWindowDataGrid.SelectedCells.Clear();
      }
      else
      {
        intervalRangeErrorMessage.Text = "";
      }

      int firstRowIndex = 0;
      int lastRowIndex = rotateWindowDataGrid.Items.Count - 1;
      List<int> columns = new List<int>();

      for (int i = 0; i < rotateWindowDataGrid.Items.Count; i++)
      {
        DataRowView row = rotateWindowDataGrid.Items.GetItemAt(i) as DataRowView;
        if (row.Row.ItemArray[0].ToString() == rotateWindowStartingInterval.Text)
        {
          firstRowIndex = i;
        }
        if (row.Row.ItemArray[0].ToString() == rotateWindowEndingInterval.Text)
        {
          lastRowIndex = i;
        }
      }
      int j = 1;
      foreach (var move in m_Movements)
      {
        if (move.Value.IsChecked == true)
        {
          columns.Add(j);
        }
        j++;
      }
      List<KeyValuePair<int, int>> cellsToSelect = new List<KeyValuePair<int, int>>();
      foreach (var colIdx in columns)
      {
        for (int k = firstRowIndex; k <= lastRowIndex; k++)
        {
          cellsToSelect.Add(new KeyValuePair<int, int>(k, colIdx));
        }
      }

      SelectCellsByIndexes(rotateWindowDataGrid, cellsToSelect);
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
            if (!dataGrid.SelectedCells.Contains(dataGridCellInfo))
            {
              dataGrid.SelectedCells.Add(dataGridCellInfo);
            }
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

    private void RotateCellSelection()
    {
      m_CurrentlyRotatingSelection = true;

      int turns = nTurnsOne.IsChecked == true ? 1 : 2;
      int firstRowIndex = 0;
      int lastRowIndex = rotateWindowDataGrid.Items.Count - 1;
      List<int> columns = new List<int>();

      for (int i = 0; i < rotateWindowDataGrid.Items.Count; i++)
      {
        DataRowView row = rotateWindowDataGrid.Items.GetItemAt(i) as DataRowView;
        if (row.Row.ItemArray[0].ToString() == rotateWindowStartingInterval.Text)
        {
          firstRowIndex = i;
        }
        if (row.Row.ItemArray[0].ToString() == rotateWindowEndingInterval.Text)
        {
          lastRowIndex = i;
        }
      }
      foreach (var cell in rotateWindowDataGrid.SelectedCells)
      {
        if (cell.Column.DisplayIndex != 0 && !columns.Contains(cell.Column.DisplayIndex))
        {
          columns.Add(cell.Column.DisplayIndex);
        }
      }

      List<KeyValuePair<int, int>> cellsToSelect = new List<KeyValuePair<int, int>>();
      for (int i = 0; i < columns.Count; i++)
      {
        if (rDirectionClockwise.IsChecked == true)
        {
          columns[i] = columns[i] + (4 * turns);
          if (columns[i] > 16)
          {
            columns[i] -= 16;
          }
          if (columns[i] == 0)
          {
            columns[i]++;
          }
        }
        else
        {
          columns[i] = columns[i] - (4 * turns);
          if (columns[i] < 0)
          {
            columns[i] += 16;
          }
          if (columns[i] == 0)
          {
            columns[i]++;
          }
        }
        for (int k = firstRowIndex; k <= lastRowIndex; k++)
        {
          cellsToSelect.Add(new KeyValuePair<int, int>(k, columns[i]));
        }
      }

      SelectCellsByIndexes(rotateWindowDataGrid, cellsToSelect);

      m_CurrentlyRotatingSelection = false;
      UpdateInfoBar();
    }

    private void rotateWindowMouse_Click(object sender, MouseButtonEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
      {
        rotateWindowDataGrid.SelectedCells.Clear();
      }
    }

    private void rotateWindowDataTabColumnHeader_Click(object sender, MouseButtonEventArgs e)
    {
      var columnHeader = sender as DataGridColumnHeader;
      if (e.ChangedButton == MouseButton.Left && columnHeader != null)
      {
        if (columnHeader.DataContext.ToString() == "Interval")
        {
          rotateWindowDataGrid.SelectedCells.Clear();
          foreach (var column in rotateWindowDataGrid.Columns)
          {
            if (column.Header.ToString() == "Interval")
            {
              continue;
            }
            foreach (var item in rotateWindowDataGrid.Items)
            {
              rotateWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
          }
        }
        else
        {
          rotateWindowDataGrid.Focus();
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          {
            foreach (var item in rotateWindowDataGrid.Items)
            {
              rotateWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
          else
          {
            rotateWindowDataGrid.SelectedCells.Clear();
            foreach (var item in rotateWindowDataGrid.Items)
            {
              rotateWindowDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
        }
      }
    }

    #endregion

    #region Info Bar Logic

    private void RotationSelectionMethod_Changed(object sender, RoutedEventArgs e)
    {
      if (selectByGrid.IsChecked == true)
      {
        rWindowApproachesSection.IsEnabled = false;
        rWindowRangeSection.IsEnabled = false;
        rotateWindowDataGrid.IsHitTestVisible = true;
        rotateWindowDataGrid.Opacity = 1.0;
      }
      if (selectByInfoBar.IsChecked == true)
      {
        rWindowApproachesSection.IsEnabled = true;
        rWindowRangeSection.IsEnabled = true;
        rotateWindowDataGrid.IsHitTestVisible = false;
        rotateWindowDataGrid.Opacity = .9;
      }
    }

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

      if (!m_Intervals.Contains(convertedTime))
      {
        rotateWindowDataGrid.SelectedCells.Clear();
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
      DateTime.TryParse(rotateWindowStartingInterval.Text, out currentInterval);
      DateTime.TryParse(rotateWindowEndingInterval.Text, out lastInterval);
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
      if (selectByGrid.IsChecked == true)
      {
        return;
      }
      UpdateCellSelection();
    }

    private void DirectionCheck_Changed(object sender, RoutedEventArgs e)
    {
      rotateWindowDataGrid.Focus();
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
      if (selectByGrid.IsChecked == true)
      {
        return;
      }
      UpdateCellSelection();
    }

    private void NumberRotations_Changed(object sender, RoutedEventArgs e)
    {
      rotateWindowDataGrid.Focus();
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
    #endregion

    #region Button Presses

    private void RotateData_Click(object sender, RoutedEventArgs e)
    {
      RotationDirection direction = rDirectionClockwise.IsChecked == true
        ? RotationDirection.Clockwise : RotationDirection.CounterClockwise;

      int turns = nTurnsOne.IsChecked == true ? 1 : 2;

      List<int> moves = new List<int>();
      int idx = 1;
      foreach (var move in m_Movements)
      {
        if (move.Value.IsChecked == true)
        {
          moves.Add(idx);
        }
        idx++;
      }
      m_tempCount.RotateCountData(rotateWindowStartingInterval.Text, rotateWindowEndingInterval.Text, direction, turns, moves);
      rotateWindowDataGrid.Focus();
      RotateCellSelection();
    }

    private void rWindowReset_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Are you sure you want to revert changes?", "Reset", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        m_tempCount.m_Data = m_RefCount.m_Data.Copy();
        m_tempCount.m_AssociatedDataFiles = new List<DataFile>(m_RefCount.m_AssociatedDataFiles.Select(x => (DataFile)x.Clone()));
        m_tempCount.m_DataCellToFileMap = m_RefCount.m_DataCellToFileMap.ToDictionary(x => (string)x.Key.Clone(), x => (DataFile)x.Value.Clone());
        rotateWindowDataGrid.ItemsSource = new DataView(m_tempCount.m_Data.Tables[0]);
        if (rotateWindowDataGrid.Columns.Count > 0)
        {
          rotateWindowDataGrid.Columns[0].Visibility = Visibility.Collapsed;
        }

      }
    }

    private void rWindowSave_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Save Changes and Close Window. \nAre you sure?", "Save Changes", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        m_RefCount.m_Data = m_tempCount.m_Data.Copy();
        m_RefCount.m_AssociatedDataFiles = new List<DataFile>(m_tempCount.m_AssociatedDataFiles.Select(x => (DataFile)x.Clone()));
        m_RefCount.InvertDataFileToCellMapping();
        DialogResult = true;
      }
    }

    private void rWindowCancel_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("Close window and discard Changes. \nAre you sure?", "Cancel", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
      if (result == MessageBoxResult.OK)
      {
        DialogResult = false;
      }
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
  }
}
