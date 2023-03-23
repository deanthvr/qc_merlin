using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AppMerlin;
using System.Linq;
using System.Text;
using Merlin.TmcCountFileImport;
using Merlin.TubeImport;
using Merlin.DataTabPages;
using Merlin.DetailsTab;
using Merlin.Printing;
using Merlin.QCDBImportWizard;
using Merlin.Settings;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using RadioButton = System.Windows.Controls.RadioButton;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Diagnostics;
using Merlin.BankPresets;
using System.Windows.Threading;
using System.Xml.Serialization;
using Merlin.SpecialWindows;
using Merlin.Notes;
using Brush = System.Windows.Media.Brush;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TabControl = System.Windows.Controls.TabControl;
using Merlin.ExtensionClasses;
using System.Net;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Class Variables

    public TMCProject m_Project;
    public XmlFile m_File;
    public string m_FilePath;
    public Preferences m_DefaultPreferences;
    public Flag m_CurrentFlag = new Flag();
    public Flag m_LastSelectedFlag = new Flag();
    public Count m_CurrentDataTabCount = new Count();
    public int m_DataTabCurrentBank = 0;
    public int m_FlagTabCurrentBank = 0;
    public bool editingFlagNote = false;
    public string m_NumericTextBoxTracker; //tracks order number while entering in case of bad char
    public StartingScreen splash = new StartingScreen(); //splash screen
    public Canvas bg = new Canvas(); //splash screen
    public Image image = new Image(); //spash screen
    public StateMachine m_CurrentState;
    public bool m_HasADateBeenSetInCreatingMode;
    public int m_BalancingGridCurrentMouseRow;
    public int m_BalancingGridCurrentMouseCol;
    public SolidColorBrush m_BalancingGridBackground;
    public SolidColorBrush m_BalancingGridHover;
    public Point? lastCenterPositionOnTarget;
    public Point? lastMousePositionOnTarget;
    public Point? lastDragPoint;
    public ContextMenu m_BalancingMenuOccupied;
    public ContextMenu m_BalancingMenuAvailable;
    public BalancingModule m_ClickedBM; //tracks BalancingModule that was most recently clicked
    public MerlinKeyValuePair<int, int> m_AvailableCellClicked; //tracks empty cell that was most recently clicked
    public List<IEnumerable<string>> m_ListsOfIntervals;
    public bool m_IsBalancingTabCurrentlyPopulating;
    public string[] m_RecentProjects = new string[10];
    public RoutedCommand addLocation;
    public ContextMenu m_PeakMenu;
    public DataTable blankTable = new DataTable();
    public DataView dataGridView = new DataView();
    public Stack<MerlinDataTableState> undoStack = new Stack<MerlinDataTableState>();
    public Stack<MerlinDataTableState> redoStack = new Stack<MerlinDataTableState>();
    public Stack<MerlinDataTableState> flagUndoStack = new Stack<MerlinDataTableState>();
    public Stack<MerlinDataTableState> flagRedoStack = new Stack<MerlinDataTableState>();
    public bool m_UnhandledExecptionClose = false;
    public static string m_CurrentUser = Utilities.GetCurrentUser();
    public static bool amIADeveloper = false;
    public DataTable columnSums = new DataTable();
    public DataTable rowSums = new DataTable();

    //Temp Variable
    public List<long> times = new List<long>();

    #endregion

    #region Constructors

    public MainWindow()
    {
      CreateDefaultTable();
      InitializeComponent();
      this.DataContext = this;
      SetDeveloperStatus();
      if (!amIADeveloper)
      {
        HookInExceptionEventHandlers();
      }

      AddHandler(LocationModule.LocationDeletedEvent, new RoutedEventHandler(DeleteLocation_Click));
      AddHandler(TubeLocationModule.LocationDeletedEvent, new RoutedEventHandler(DeleteLocation_Click));
      AddHandler(TubeTimePeriodUI.SurveyTimeCopiedEvent, new RoutedEventHandler(TubeSurveyTimeCopy_Click));

      bg.Background = new SolidColorBrush(Colors.Black);
      image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/highway-1031149_1920.jpg", UriKind.Relative));
      image.Stretch = Stretch.Fill;
      ShowSplashScreen(true);
      RunDateDependentEvents();

      CheckForSettingsFile();

      SetUpBanks();

      GenerateTimePeriodList();


      LoadDefaultPreferences();
      LoadRecentProjects();

      m_NumericTextBoxTracker = "";

      DisableAllTabs();

      BalancingScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
      BalancingScrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
      BalancingScrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
      BalancingScrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

      BalancingScrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
      BalancingScrollViewer.MouseMove += OnMouseMove;

      slider.Value = 1;
      slider.ValueChanged += OnSliderValueChanged;

      BalancingGrid.Cursor = ((FrameworkElement)Resources["CursorGrab"]).Cursor;
      m_BalancingMenuOccupied = InitializeBalancingMenuOccupied();
      m_BalancingMenuOccupied.Opened += m_BalancingMenuOccupied_Opened;
      m_BalancingMenuAvailable = new ContextMenu();
      m_AvailableCellClicked = new MerlinKeyValuePair<int, int>(-1, -1);

      m_CurrentState = new StateMachine();
      m_CurrentState.m_DetailsTabState = DetailsTabState.Viewing;
      m_HasADateBeenSetInCreatingMode = false;
      m_BalancingGridCurrentMouseRow = -1;
      m_BalancingGridCurrentMouseCol = -1;
      m_BalancingGridBackground = new SolidColorBrush(Colors.White);
      m_BalancingGridHover = new SolidColorBrush(Colors.WhiteSmoke);
      //m_ListsOfIntervals = new List<List<string>>();
      m_IsBalancingTabCurrentlyPopulating = false;
      versionText.Text = GetPublishedVersion();
      userText.Text = "User: " + m_CurrentUser;
      addLocation = new RoutedCommand();
      addLocation.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control));
      InitializeCommandBindings();
      CheckForOpenWithProjectFile();
    }

    #endregion

    #region Application Setup & Visibility

    private void CheckForOpenWithProjectFile()
    {
      if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] != null)
      {
        var args = Environment.GetCommandLineArgs();
        OpenProject(args[1]);
        m_FilePath = args[1];
        ShowSplashScreen(false);
      }
    }

    private void RunDateDependentEvents()
    {
      int month = DateTime.Today.Month;
      int day = DateTime.Today.Day;
      if (month == 10 && day == 14)
      {
        ShowNPMWindow();
      }
      else if (month == 4 && day == 1)
      {
        ShowFoolsWindow();
      }
      else if (month == 1 && day == 26)
      {
        ShowBucsWindow();
      }
    }

    private void ShowNPMWindow()
    {
      NPMWindow window = new NPMWindow();
      window.ShowDialog();
    }

    private void ShowBucsWindow()
    {
      BucsWindow window = new BucsWindow();
      window.ShowDialog();
    }

    private void ShowFoolsWindow()
    {
      AprilFoolsWindow window = new AprilFoolsWindow();
      window.ShowDialog();
    }

    private void HookInExceptionEventHandlers()
    {
      Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
      AppDomain.CurrentDomain.UnhandledException += App_CurrentDomainUnhandledException;
    }

    private void SetUpBanks()
    {
      for (int i = 0; i < Constants.MAX_BANKS_ALLOWED; i++)
      {
        //Construct bank UI controls

        StackPanel container = new StackPanel();
        container.Orientation = Orientation.Horizontal;

        Label lbl = new Label();
        lbl.Content = "Bank " + i.ToString();
        lbl.Width = 48.0;
        lbl.FontSize = 10.0;
        lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

        ComboBox bankCB = new ComboBox();
        bankCB.Name = "Bank" + i.ToString();
        bankCB.Width = 200.0;
        bankCB.ToolTip = FindResource("bankComboBoxTooltip");
        bankCB.Style = FindResource("RoundedLeft") as Style;
        PopulateVehBankDropDownList(bankCB);
        bankCB.SelectedValue = "NOT USED";
        bankCB.SelectionChanged += vehOrPedCBankComboBox_SelectionChanged;

        ComboBox pedColCB = new ComboBox();
        pedColCB.Name = "Bank" + i.ToString() + "PedColumn";
        pedColCB.Width = 150.0;
        pedColCB.ToolTip = FindResource("pedColumnComboBoxTooltip");
        pedColCB.Style = FindResource("RoundedRight") as Style;
        PopulatePedBankDropDownList(pedColCB);
        pedColCB.SelectedValue = "NA";
        pedColCB.SelectionChanged += vehOrPedCBankComboBox_SelectionChanged;

        container.Children.Add(lbl);
        container.Children.Add(bankCB);
        container.Children.Add(pedColCB);

        BanksList.Items.Add(container);
      }
      //apply the standard bank preset
      PopulateGuiBanksOnDetailsTab(GlobalBankPresets.Standard);
      ncdotColSwapCheckBox.IsChecked = GlobalBankPresets.presetNamesWithColumnSwapping.Contains(GlobalBankPresets.PRESET_NAME_STANDARD);
      tccCheckBox.IsChecked = GlobalBankPresets.presetNamesWithTccRules.Contains(GlobalBankPresets.PRESET_NAME_STANDARD);
    }

    void vehOrPedCBankComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdateVisibleBankComboBoxes();
    }

    private void UpdateVisibleBankComboBoxes()
    {
      var vehBoxes = GetVehicleBankComboBoxes();
      var pedBoxes = GetPedBankComboBoxes();
      int indexOfLastUsedBank = -1; //sentinel

      for (int i = 0; i < vehBoxes.Count; i++)
      {
        if ((vehBoxes[i].SelectedValue != null && vehBoxes[i].SelectedValue.ToString() != "NOT USED") || (pedBoxes[i].SelectedValue != null && pedBoxes[i].SelectedValue.ToString() != PedColumnDataType.NA.ToString() && !(bool)tccCheckBox.IsChecked))
        {
          //this bank is in use
          indexOfLastUsedBank = i;
        }
      }
      for (int i = 0; i < BanksList.Items.Count; i++)
      {
        if (i < indexOfLastUsedBank + 2)
        {
          ((StackPanel)BanksList.Items.GetItemAt(i)).Visibility = Visibility.Visible;
        }
        else
        {
          ((StackPanel)BanksList.Items.GetItemAt(i)).Visibility = Visibility.Collapsed;
        }
      }
    }

    private void CheckForSettingsFile()
    {
      string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (!File.Exists(appData + @"\Merlin\Settings.xml"))
      {
        CreateSettingsWindow csw = new CreateSettingsWindow();
        bool? result = csw.ShowDialog();
        CreateRecentProjectFile();
      }
    }

    private void InitializeCommandBindings()
    {
      CommandBinding undoCommand = new CommandBinding(ApplicationCommands.Undo);
      CommandBinding redoCommand = new CommandBinding(ApplicationCommands.Redo);
      CommandBinding cutCommand = new CommandBinding(ApplicationCommands.Cut);
      CommandBinding copyCommand = new CommandBinding(ApplicationCommands.Copy);
      CommandBinding pasteCommand = new CommandBinding(ApplicationCommands.Paste);
      CommandBinding addLocationCommand = new CommandBinding(addLocation, AddLocationCommandExecuted);

      CommandBindings.Add(undoCommand);
      CommandBindings.Add(redoCommand);
      CommandBindings.Add(cutCommand);
      CommandBindings.Add(copyCommand);
      CommandBindings.Add(pasteCommand);
      CommandBindings.Add(addLocationCommand);

    }

    private void CreateDefaultTable()
    {
      DataColumn c = new DataColumn("Time");

      c.DataType = Type.GetType("System.String");
      c.ReadOnly = true;
      c.Unique = true;
      blankTable.Columns.Add(c);
      List<string> columnHeaders = new List<string> 
      { 
        "Time",
        "SBR", "SBT", "SBL", "SBP",
        "WBR", "WBT", "WBL", "WBP",
        "NBR", "NBT", "NBL", "NBP",
        "EBR", "EBT", "EBL", "EBP"
      };
      int[] columns = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      for (int j = 1; j < columnHeaders.Count(); j++)
      {
        string colHeader = columnHeaders[j];
        DataColumn dc = new DataColumn(colHeader);
        dc.DataType = Type.GetType("System.Int16");
        blankTable.Columns.Add(dc);
      }

      string time = "07:00";
      DateTime time2;
      DateTime.TryParse(time, out time2);

      for (int k = 0; k < 12; k++)
      {
        DataRow r = blankTable.NewRow();

        r[0] = time2.TimeOfDay.ToString().Remove(5, 3);
        for (int m = 1; m < (columns.Count() + 1); m++)
        {
          r[m] = columns[m - 1];
        }
        blankTable.Rows.Add(r);
        time2 = time2.AddMinutes(5);
      }
    }

    private void CreateRecentProjectFile()
    {
      XmlFile file = new XmlFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Merlin\", "RecentProjects.xml");
      file.SerializeToFile<string[]>(m_RecentProjects);
    }

    private void LoadRecentProjects()
    {
      XmlFile file = new XmlFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Merlin\", "RecentProjects.xml");
      file.TryFileLoad<string[]>();
      file.DeserializeFromFile<string[]>(ref m_RecentProjects);
      PopulateRecentProjectMenuItems();
    }

    private void LoadDefaultPreferences()
    {
      XmlFile file = new XmlFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Merlin\", "Settings.xml");
      file.TryFileLoad<Preferences>();
      file.DeserializeFromFile<Preferences>(ref m_DefaultPreferences);
      if(m_DefaultPreferences.m_userName == null)
      {
        m_DefaultPreferences.m_userName = "";
      }
      if (m_DefaultPreferences.m_password == null)
      {
        m_DefaultPreferences.m_password = new NetworkCredential("", "").SecurePassword;
      }
    }

    private void PopulateRecentProjectMenuItems()
    {
      recentProjectsMenuItem.Items.Clear();
      int idx = 1;
      foreach (string projectFile in m_RecentProjects)
      {
        if (!String.IsNullOrEmpty(projectFile))
        {
          MenuItem item = new MenuItem();
          item.Header = projectFile.Split('\\')[projectFile.Split('\\').Length - 1];
          item.Icon = idx.ToString();
          item.Tag = projectFile;
          item.ToolTip = projectFile;
          item.Click += RecentOpen_Click;
          recentProjectsMenuItem.Items.Add(item);
          idx++;
        }
      }
    }

    private bool OpenProject(string filePath)
    {
      if (m_CurrentState.m_ProjectState == ProjectState.Loaded)
      {
        if (!CloseProject())
        {
          return false;
        }
      }
      m_File = new XmlFile(filePath);
      if (!m_File.TryFileLoad<TMCProject>() || !m_File.DeserializeProjectFile(ref m_Project))
      {
        MessageBox.Show(Application.Current.MainWindow, m_File.GetErrorMessage(), m_File.GetHelperMessage(), MessageBoxButton.OK);
        return false;
      }
      if (!amIADeveloper)
      {
        MarkProjectFileReadOnly(filePath, true);
      }
      RunVersionControl();
      m_Project.ReloadNonSerializableMembers();
      m_CurrentState.m_ProjectState = ProjectState.Loaded;
      EnableAllTabs();
      DetailsTab.IsSelected = true;
      AddFileToRecentList(filePath);
      PopulateRecentProjectMenuItems();
      //reload time period list with as many as the opened project contains
      TimePeriodList.Items.Clear();
      for (int i = 0; i < m_Project.m_TimePeriodLabels.Count; i++)
      {
        TimePeriodList.Items.Add(new TimePeriodModule(this, m_Project.m_TimePeriodIDs[i]));
      }
      ChangeDetailsTabState(DetailsTabState.Viewing);
      //Check if existing project is mixing regular and FHWA vehicle types and project uses TCC rules. If so, alert user and force them to fix in edit mode.
      if (m_Project.MixingRegularAndFHWA() && m_Project.m_TCCDataFileRules)
      {
        ChangeDetailsTabState(DetailsTabState.Editing);
        Edit_Cancel.IsEnabled = false;
        StringBuilder msg = new StringBuilder();
        msg.Append("Mixing of standard and FHWA vehicle types detected in project file which is no longer allowed as of version 4.0.1. You must reconfigure banks now.\n\nOld bank configuration:\n");
        foreach (KeyValuePair<int, string> kvp in m_Project.m_BankDictionary)
        {
          msg.AppendLine("\u2022 Bank " + kvp.Key.ToString() + ": " + kvp.Value);
        }
        MessageBox.Show(msg.ToString(), "Incompatible Vehicle Types", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
      return true;
    }

    private void SaveProjectWithDialog(bool mustSave)
    {
      bool networkOk = CheckNetworkDirectoryPath(m_Project.m_Prefs.m_NetworkProjectDirectory);

      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save Project";
      if (networkOk)
      {
        sfd.InitialDirectory = (String.IsNullOrEmpty(m_FilePath)) ? m_Project.m_Prefs.m_NetworkProjectDirectory : m_FilePath;
      }
      else
      {
        sfd.InitialDirectory = (String.IsNullOrEmpty(m_FilePath)) ? m_Project.m_Prefs.m_LocalProjectDirectory : m_FilePath;
      }
      sfd.RestoreDirectory = true;
      sfd.OverwritePrompt = true;
      sfd.ValidateNames = true;
      sfd.Filter = "TMC Project Files (*.tmc)|*.tmc";
      //uses current file name if it exists, otherwise order number + project name (if present)
      sfd.FileName = (String.IsNullOrEmpty(m_FilePath)) ? ((String.IsNullOrEmpty(m_Project.m_ProjectName)) ? m_Project.m_OrderNumber + ".tmc" : m_Project.m_OrderNumber + " - " + m_Project.m_ProjectName + ".tmc") : m_FilePath.Split('\\')[m_FilePath.Split('\\').Length - 1];
      bool? result = null;
      if (mustSave)
      {
        while (result == false || result == null)
        {
          result = sfd.ShowDialog();
        }
      }
      else
      {
        result = sfd.ShowDialog();
      }
      if (result == true)
      {
        //if (File.Exists(sfd.FileName))
        //{
        //  var warningResult = MessageBox.Show("This file already exists \nYes to Overwrite \nNo to choose another file name \nCancel to abort", "Overwrite Exising", MessageBoxButton.YesNoCancel);
        //  if (warningResult == MessageBoxResult.Yes)
        //  {
        //    m_Project.SaveFile(sfd.FileName);
        //    m_FilePath = sfd.FileName;
        //    AddFileToRecentList(sfd.FileName);
        //  }
        //  else if (warningResult == MessageBoxResult.No)
        //  {
        //    result = sfd.ShowDialog();
        //  }
        //  else
        //  {
        //    return;
        //  }
        //}
        //else
        //{
        SaveFile(sfd.FileName);
        m_FilePath = sfd.FileName;
        AddFileToRecentList(sfd.FileName);
        //}
      }
    }

    private void SaveProjectWithoutDialog()
    {
      if (String.IsNullOrEmpty(m_FilePath))
      {
        SaveProjectWithDialog(false);
      }
      else
      {
        SaveFile(m_FilePath);
      }
      UpdateWindowTitle(this);
    }

    public bool SaveFile(string filePath)
    {
      bool result = true;
      MarkProjectFileReadOnly(filePath, false);
      //foreach (Intersection intersection in m_Project.m_Intersections)
      //{
      //  intersection.UpdateXmlNeighbors();
      //}
      CreateFilePath(filePath);
      try
      {
        XmlSerializer x = new XmlSerializer(typeof(TMCProject));
        TextWriter writer = new StreamWriter(filePath);
        x.Serialize(writer, m_Project);
        writer.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Something went wrong with the save, please verify file details and location.\n\n" + ex.Message, "File Error", MessageBoxButton.OK);
        result = false;
      }
      if (amIADeveloper)
      {
        MarkProjectFileReadOnly(filePath, true);
      }
      return result;
    }

    private void CreateFilePath(string filePath)
    {
      try
      {
        FileInfo file = new FileInfo(filePath);
        file.Directory.Create();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Something went wrong with the save, please verify file details and location.\n\n" + ex.Message, "File Error", MessageBoxButton.OK);

        System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.DialogResult result = browser.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
          CreateFilePath(browser.SelectedPath.ToString());
        }
      }
    }

    private bool CloseProject()
    {
      if (m_Project != null)
      {
        //if in create/edit mode, give option to close without an option to save, or cancel
        if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating || m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
        {
          MessageBoxResult result = MessageBox.Show("Project cannot be saved while in Create/Edit mode.\n\nAre you sure you want to close without saving", "Close Without Saving?", MessageBoxButton.YesNo);
          if (result == MessageBoxResult.No)
          {
            return false;
          }
        }
        else
        {
          MessageBoxResult result = AskIfSaveDesiredUponClose();
          if (result == MessageBoxResult.Cancel)
          {
            return false;
          }
          if (result == MessageBoxResult.Yes)
          {
            SaveFile(m_FilePath);
            AddFileToRecentList(m_FilePath);
          }
        }
      }
      DetailsTab.IsSelected = true;
      // Clear All applicable variables
      MarkProjectFileReadOnly(m_FilePath, false);
      m_Project = null;
      m_File = null;
      m_FilePath = "";
      m_CurrentFlag = new Flag();
      m_LastSelectedFlag = new Flag();
      m_CurrentDataTabCount = new Count();
      m_DataTabCurrentBank = 0;
      m_NumericTextBoxTracker = ""; //tracks order number while entering in case of bad char
      m_CurrentState.ResetStates();
      m_HasADateBeenSetInCreatingMode = false;
      m_ListsOfIntervals = new List<IEnumerable<string>>();
      m_IsBalancingTabCurrentlyPopulating = false;
      times = new List<long>();

      ClearStacks();
      dataTabTreeList.Items.Clear();
      dataTabFileList.Items.Clear();
      dataBankTabs = new TabControl();
      ClearFlagStacks();
      ClearFlagDetails();
      FlagList.Items.Clear();
      m_CurrentState.m_FlagTabState = FlagTabState.Empty;
      TurnOnFlagsPageOpacity();

      PopulateRecentProjectMenuItems();
      DisableAllTabs();
      UpdateWindowTitle(this);
      return true;
    }

    private MessageBoxResult AskIfSaveDesiredUponClose()
    {
      return MessageBox.Show("Save before closing " + m_Project.m_OrderNumber + " - " + m_Project.m_ProjectName + "?",
        "Close Current Project", MessageBoxButton.YesNoCancel);
    }

    private void ShowSplashScreen(bool doShow)
    {
      if (doShow)
      {
        EntireWindowGrid.Children.Add(bg);
        EntireWindowGrid.Children.Add(image);
        EntireWindowGrid.Children.Add(splash);
        mainGrid.IsEnabled = false;
      }
      else
      {
        EntireWindowGrid.Children.Remove(bg);
        EntireWindowGrid.Children.Remove(splash);
        EntireWindowGrid.Children.Remove(image);
        mainGrid.IsEnabled = true; ;
      }
    }

    private void DisableAllTabs()
    {
      DetailsTab.IsEnabled = false;
      DataTab.IsEnabled = false;
      BalancingTab.IsEnabled = false;
      FlagTab.IsEnabled = false;
      NotesTab.IsEnabled = false;
      CompletionTab.IsEnabled = false;
    }

    private void EnableAllTabs()
    {
      DetailsTab.IsEnabled = true;
      DataTab.IsEnabled = true;
      BalancingTab.IsEnabled = true;
      FlagTab.IsEnabled = true;
      NotesTab.IsEnabled = true;
      CompletionTab.IsEnabled = true;
    }

    private void NavTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!e.OriginalSource.GetType().Equals(typeof(TabControl)))
      {
        //This event handler responds to any combobox selection change, and possibly other controls. This ensures anything
        //other than changing the tab item will be ignored.
        return;
      }

      if (BalancingTab.IsSelected)
      {
        RefreshBalancingTotals();
      }
      if (DataTab.IsSelected)
      {
        DataTabSelectedTasks();
      }
    }

    private void ButtonHandler(object sender, RoutedEventArgs e)
    {
      var fe = e.OriginalSource as FrameworkElement;
      switch (fe.Name)
      {
        case "open":
          MenuOpen_Click(sender, e);
          break;
        case "newScratch":
          MenuNewScratch_Click(sender, e);
          break;
        case "newQCWeb":
          MenuNewWeb_Click(sender, e);
          break;

      }
      e.Handled = true;
    }

    private void AddFileToRecentList(string project)
    {
      int currentIndex = -1;
      for (int i = 0; i < 10; i++)
      {
        if (m_RecentProjects[i] == project)
        {
          currentIndex = i;
        }
      }
      if (currentIndex >= 0)
      {
        m_RecentProjects[currentIndex] = null;
        for (int j = currentIndex; j >= 1; j--)
        {
          m_RecentProjects[j] = m_RecentProjects[j - 1];
        }
      }
      else
      {
        for (int j = 9; j >= 1; j--)
        {
          m_RecentProjects[j] = m_RecentProjects[j - 1];
        }
      }
      m_RecentProjects[0] = project;
    }

    private string GetPublishedVersion()
    {
      return "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

      //if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
      //{
      //  //Returns current published version if using ClickOnce
      //  return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.
      //      CurrentVersion.ToString();
      //}
      ////Returns assembly version when debugging
      //return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " (Debug Mode)";
    }

    private void ShowModuleNotPurchasedDialog()
    {
      MessageBox.Show("This module has not yet been purchased.", "Module Not Available",
        MessageBoxButton.OK, MessageBoxImage.Hand);
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!m_UnhandledExecptionClose)
      {
        //Close project and let user decide if they want to save upon exiting
        if (!CloseProject())
        {
          //The user canceled out of closing the program
          e.Cancel = true;
          return;
        }
        //serialize recent projects
        XmlFile file = new XmlFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Merlin\", "RecentProjects.xml");
        file.SerializeToFile(m_RecentProjects);
      }
      if (!String.IsNullOrEmpty(m_FilePath))
      {
        MarkProjectFileReadOnly(m_FilePath, false);
      }
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
        this.Cursor = Cursors.Wait;
      }
      this.Cursor = Cursors.Arrow;
      if (!result)
      {
        MessageBox.Show("Could not access the following path.  Please check your connection: \n" + path + "\nUsing local path...", "Network path error", MessageBoxButton.OK);
      }
      return result;
    }

    private void RunVersionControl()
    {
      List<string> changes = m_Project.RunVersionControl(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3), m_CurrentUser);

      StringBuilder changesString = new StringBuilder();
      if (changes.Count > 0)
      {
        foreach (string changeItem in changes)
        {
          changesString.AppendLine("\u2022 " + changeItem);
        }
        MessageBox.Show("This project was created by an older version of Merlin.  Forward conversion complete.  \n\nChange List:\n\n" + changesString, "Merlin Compatibility", MessageBoxButton.OK);

        if ((changes.Contains("Changes to Merlin Threshold Settings. You will need to verify settings.")
          || changes.Contains("Changes to Merlin Threshold Settings. Please verify the new TCC flag thresholds in the Settings Window."))
          && !IsSettingsFileUpToDate())
        {
          CreateSettingsWindow csw = new CreateSettingsWindow();
          bool? result = csw.ShowDialog();
        }
      }
    }

    private bool IsSettingsFileUpToDate()
    {
      string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      Directory.CreateDirectory(appData + @"\Merlin");

      XmlFile serializedPreferences = new XmlFile(appData + @"\Merlin\", "Settings.xml");
      Preferences tempPrefs = new Preferences();
      serializedPreferences.TryFileLoad<Preferences>();
      serializedPreferences.DeserializeFromFile<Preferences>(ref tempPrefs);
      if (tempPrefs.m_TimePeriodLow > 0 && tempPrefs.m_fhwa1 > 0)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      LogException(sender, e, 1);
      e.Handled = true;
      MessageBox.Show("Something went wrong.  Merlin needs to close.  \n\nException Log and Autosave project file can be found at: " +
        "\n\n C:\\MerlinLogs\\ \n\nPlease note last few attempted actions in Merlin to send to developers with log and project file."
        , "Clipped Wings", MessageBoxButton.OK);
      m_UnhandledExecptionClose = true;
      Application.Current.Shutdown();
    }

    private void App_CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
      LogException(sender, args, 2);
      MessageBox.Show("Something went wrong.  Merlin needs to close.  \n\nException Log and Autosave project file can be found at: " +
        "\n\n C:\\MerlinLogs\\ \n\nPlease note last few attempted actions in Merlin to send to developers with log and project file."
        , "Clipped Wings", MessageBoxButton.OK);
      m_UnhandledExecptionClose = true;
      Application.Current.Shutdown();
    }

    private void LogException(object sender, object ex, int n)
    {
      Exception x;
      if (n == 1)
      {
        var d = ex as DispatcherUnhandledExceptionEventArgs;
        x = d.Exception;
      }
      else
      {
        var d = ex as UnhandledExceptionEventArgs;
        x = d.ExceptionObject as Exception;
      }
      StringBuilder output = new StringBuilder();
      if (sender != null)
      {
        output.AppendLine("SENDER:");
        output.AppendLine(sender.ToString());
      }
      if (x != null)
      {
        if (x.Message != null)
        {
          output.AppendLine("EXCEPTION:");
          output.AppendLine(x.Message);
        }
        if (x.Data != null)
        {
          output.AppendLine("DATA:");
          output.AppendLine(x.Data.ToString());
        }
        if (x.InnerException != null)
        {
          output.AppendLine("INNER EXCEPTION");
          output.AppendLine(x.InnerException.ToString());
        }
        if (x.StackTrace != null)
        {
          output.AppendLine("STACK TRACE");
          output.AppendLine(x.StackTrace);
        }
      }
      string directory = @"C:\MerlnLogs\";
      string dailyDirectory = DateTime.Now.Day + @"_" + DateTime.Now.Month + @"_" + DateTime.Now.Year + @"\";
      Directory.CreateDirectory(directory);
      Directory.CreateDirectory(directory + dailyDirectory);
      string aggDir = directory + dailyDirectory;
      string datetime = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString()
        + "_" + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
      string projectId = "new_project";
      if (m_Project != null && m_Project.m_OrderNumber != null)
      {
        projectId = m_Project.m_OrderNumber;
      }
      string exceptionFileName = @"ExceptionLog_" + projectId + @"_" + datetime + @".txt";
      string projectFileName = @"Autosave_" + projectId + @"_" + datetime + @".tmc";
      using (StreamWriter writer = File.AppendText(aggDir + exceptionFileName))
      {
        writer.WriteLine(output);
      }

      if (m_Project != null)
      {
        SaveFile(aggDir + projectFileName);
      }
      MarkProjectFileReadOnly(m_FilePath, false);
    }

    private void MarkProjectFileReadOnly(string filePath, bool readOnly)
    {
      if (File.Exists(filePath))
      {
        FileInfo file = new FileInfo(filePath);
        file.IsReadOnly = readOnly;
      }
    }

    private static void SetDeveloperStatus()
    {
      switch (m_CurrentUser)
      {
        case "David Crisman":
        case "Andrew Walters":
        case "LEGOLAS\\DavidG":
          amIADeveloper = true;
          break;
        default:
          break;
      }
    }

    private ProjectDetailsReview SetHardcodedData(int dataSet)
    {
      ProjectDetailsReview pdr = new ProjectDetailsReview(m_Project.m_Prefs);

      switch (dataSet)
      {
        case 0:
          pdr.OrderNumber = 136562;
          pdr.OrderDate = new DateTime(2015, 11, 18);
          pdr.ProjectName = "UH Manoa Turn Counts";
          pdr.TimePeriods = new List<ProjectDetailsReview.SQLTimePeriod>
        {
          new ProjectDetailsReview.SQLTimePeriod(0, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
          new ProjectDetailsReview.SQLTimePeriod(1, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          new ProjectDetailsReview.SQLTimePeriod(2, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
          new ProjectDetailsReview.SQLTimePeriod(3, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
        };
          pdr.Locations = new List<ProjectDetailsReview.Location>
        {
          new ProjectDetailsReview.Location(1, 0, "University Ave", "Maile Way","21.301752","-157.820429",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656201, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656202, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656203, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656204, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(2, 1, "Farrington Rd", "Maile Way","21.301277","-157.818024",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656205, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656206, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656207, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656208, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(3, 2, "East-West Rd", "Maile Way","21.301435","-157.814631",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656209, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656210, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656211, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656212, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(4, 3, "Farrington//Campus Rd", "Varney Circle","21.29999","-157.818188",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656213, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656214, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656215, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656216, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(5, 4, "University Ave", "Metcalf St//Campus Rd","21.299056","-157.82119",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656217, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656218, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656219, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656220, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(6, 5, "East-West Rd", "Correa Rd","21.298486","-157.81473",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656221, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656222, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656223, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656224, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(7, 6, "University Ave", "Dole St","21.296782","-157.821125",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656225, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656226, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656227, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656228, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(8, 7, "Lower Campus Rd", "Dole St","21.296732","-157.819988",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656229, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656230, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656231, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656232, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(9, 8, "Pedestrian Crossing", "Dole St","21.296709","-157.818145",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656233, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656234, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656235, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656236, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(10, 9, "East-West Rd", "Dole St","21.296254","-157.815233",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656237, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656238, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656239, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656240, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          }),
          new ProjectDetailsReview.Location(11, 10, "Kalele Rd", "Lower Campus Rd","21.292065","-157.815802",null, new List<ProjectDetailsReview.SQLTimePeriod>
          {
            new ProjectDetailsReview.SQLTimePeriod(0, 13656241, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(1, 13656242, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(2, 13656243, new DateTime(2016,7,23,7,0,0), new DateTime(2016,7,23,9,0,0)),
            new ProjectDetailsReview.SQLTimePeriod(3, 13656244, new DateTime(2016,7,23,16,0,0), new DateTime(2016,7,23,18,0,0)),
          })
    
        };
          break;
        default:
          pdr.OrderNumber = 137101;
          pdr.OrderDate = new DateTime(2016, 2, 3);
          pdr.ProjectName = "Himes Ave TMC";
          pdr.TimePeriods = new List<ProjectDetailsReview.SQLTimePeriod>
        {
          new ProjectDetailsReview.SQLTimePeriod(365758,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365768,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0)),
          new ProjectDetailsReview.SQLTimePeriod(365908,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))

        };
          pdr.Locations = new List<ProjectDetailsReview.Location>
          {
                  new ProjectDetailsReview.Location(140463,0,"N Himes Ave","W Idlewild Ave","28.001838","-82.500887",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365758,13710101,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365768,13710102,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140570,1,"N Himes Ave","W Waters Ave","28025451","-82.50078",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710103,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710104,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140569,2,"N Himes Ave","W Lambright St","28.007076","-82.500887",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710105,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710106,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140566,3,"N Himes Ave","W Hillsborough Ave","27.996097","-82.500951",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710107,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710108,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140565,4,"N Himes Ave","Tampa Bay Park Dwy","27.983203","-82.501338",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710109,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710110,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140568,5,"Site Dwy 1 (North)","W Idlewild Ave","28.001786","-82.50185",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710111,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710112,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140567,6,"N Himes Ave","Site Dwy 2 (East - Northern)","28.00109","-82.50104",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710113,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710114,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          }),
          new ProjectDetailsReview.Location(140564,7,"N Himes Ave","Site Dwy 3 (East - Sourthern)","28.000481","-82.501002",null,new List<ProjectDetailsReview.SQLTimePeriod>
          {
          new ProjectDetailsReview.SQLTimePeriod(365908,13710115,new DateTime(2017,2,11,7,30,0),new DateTime(2017,2,11,8,30,0)),
          new ProjectDetailsReview.SQLTimePeriod(365918,13710116,new DateTime(2017,2,11,14,15,0),new DateTime(2017,2,11,15,15,0))
          })
        };
          break;

      }
      return pdr;
    }

    #endregion

    #region Menu Buttons

    private void MenuNewScratch_Click(object sender, RoutedEventArgs e)
    {
      NewProject();
    }

    private void MenuNewWeb_Click(object sender, RoutedEventArgs e)
    {
      //Attempts to close current project if there is one and then enters project creation mode
      if (!NewProject())
      {
        return;
      }

      if (m_CurrentUser != "Andrew Waltersz")
      {
        ProjectDetailsReview pdr = new ProjectDetailsReview(m_DefaultPreferences);
        pdr.Owner = this;
        bool? result = pdr.ShowDialog();
        if (result == true)
        {
          m_CurrentState.m_CreatingWithImportedDetails = true;

          PopulateDetailsTabWithImportedDetails(pdr);
        }
      }
      else
      {
        m_CurrentState.m_CreatingWithImportedDetails = true;
        ProjectDetailsReview pdr = SetHardcodedData(69);
        PopulateDetailsTabWithImportedDetails(pdr);
      }
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        SaveProjectWithoutDialog();
      }
      else
      {
        MessageBox.Show("Cannot save project while editing project details.",
          "Cannot Save", MessageBoxButton.OK, MessageBoxImage.Hand);
      }
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
      SaveProjectWithDialog(false);
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
      Application.Current.Shutdown();
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
      string settingsNetworkFilePath;
      string settingsLocalFilePath;
      if (m_Project == null)
      {
        settingsNetworkFilePath = m_DefaultPreferences.m_NetworkProjectDirectory;
        settingsLocalFilePath = m_DefaultPreferences.m_LocalProjectDirectory;
      }
      else
      {
        settingsNetworkFilePath = m_Project.m_Prefs.m_NetworkProjectDirectory;
        settingsLocalFilePath = m_Project.m_Prefs.m_LocalProjectDirectory;
      }
      bool networkOk = CheckNetworkDirectoryPath(settingsNetworkFilePath);
      OpenFileDialog dlg = new OpenFileDialog();

      dlg.DefaultExt = ".tmc";
      dlg.Filter = "Turn Count Project (*.tmc)|*.tmc|All files (*.*)|*.*";
      dlg.Title = "Select a Turn Count Project";
      if (networkOk)
      {
        dlg.InitialDirectory = (String.IsNullOrEmpty(m_FilePath)) ? settingsNetworkFilePath : m_FilePath;
      }
      else
      {
        dlg.InitialDirectory = (String.IsNullOrEmpty(m_FilePath)) ? settingsLocalFilePath : m_FilePath;
      }

      bool? result = dlg.ShowDialog();

      using (new WaitCursor())
      {
        if (result == true && OpenProject(dlg.FileName))
        {
          m_FilePath = dlg.FileName;
          ShowSplashScreen(false);
        }
        else
        {
          //
        }
      }
    }

    private void RecentOpen_Click(object sender, RoutedEventArgs e)
    {
      OpenProject(((MenuItem)sender).Tag.ToString());
      m_FilePath = ((MenuItem)sender).Tag.ToString();

    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
      if (!CloseProject())
      {
        return;
      }

      ShowSplashScreen(true);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
      PrefWindow prefWindow = new PrefWindow(m_Project, m_DefaultPreferences);
      prefWindow.ShowDialog();

      if (BalancingTab.IsSelected)
      {
        RefreshBalancingTotals();
      }
    }

    private void ExportConvert_Click(object sender, RoutedEventArgs e)
    {
      if (ExportCounts())
      {
        RunConversionWizard();
      }

    }

    private void TubeQuery_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult warningResult = MessageBox.Show("WARNING!. \nThis will overwrite all tube counts in the file with the details that are retrieved. "
        + "\nCurrent data will be lost as well as balancing associations.  \nAre you sure?", "Tube Import Warning", MessageBoxButton.OKCancel);
      if (warningResult != MessageBoxResult.OK)
        return;

      TubeDatabaseImporter dbImporter = new TubeDatabaseImporter(m_Project);
      dbImporter.Owner = this;
      bool? result = dbImporter.ShowDialog();
      if (result != true)
      {
        return;
      }

      foreach(TubeSite ts in m_Project.m_Tubes)
      {
        ts.RemoveAllNeighbors();
      }
      m_Project.m_Tubes = dbImporter.NewTubes;
      SaveProjectWithoutDialog();
      ChangeDetailsTabState(DetailsTabState.Viewing);

    }

    private void ImportTubeDataFiles_Click(object sender, RoutedEventArgs e)
    {
      TubeImportOptionsDialog fd1 = GetTubeImportParameters();
      if (fd1 == null)
      {
        return;
      }

      TubeFileAssociationDialog fd2 = PerformTubeFileSearch(fd1.searchDirectory.Text, int.Parse(fd1.searchDays.Text));
      if (fd2 == null)
      {
        return;
      }
      var files = fd2.files.Select(file => file.Value).ToList<ImportDataFile>();
      TubeDataImporter fd3 = ProcessImportedTubeFiles(files);
      if (fd3 == null)
      {
        return;
      }
      if (fd3.log.Count > 0)
      {
        ImportSummary summaryWindow = new ImportSummary(fd3.log);
        summaryWindow.Owner = this;
        summaryWindow.ShowDialog();
        MessageBox.Show("Tube Import Complete, Project saved.", "Auto-Save", MessageBoxButton.OK);
      }
      else
      {
        MessageBox.Show("Import Complete with no logged results. Project saved.", "Auto-Save", MessageBoxButton.OK);
      }
      SaveProjectWithoutDialog();

      if (BalancingTab.IsSelected)
      {
        RefreshBalancingTotals();
      }
    }

    private void PrintNotes_Click(object sender, RoutedEventArgs e)
    {
      PrintNotes();
    }

    private void AboutMerlin_Click(object sender, RoutedEventArgs e)
    {
      AboutMerlin about = new AboutMerlin();
      about.ShowDialog();
    }

    private bool ExportCounts(List<Count> counts = null, bool generateExcelInsteadOfAscii = false)
    {
      MerlinProgressWindow progressWindow = new MerlinProgressWindow("Export Counts");
      progressWindow.Show();

      bool NetworkOk = CheckNetworkDirectoryPath(m_Project.m_Prefs.m_NetworkAsciiDirectory);
      string directory;
      if (NetworkOk)
      {
        directory = m_Project.m_Prefs.m_NetworkAsciiDirectory + "\\" + m_Project.m_OrderNumber;
      }
      else
      {
        directory = m_Project.m_Prefs.m_LocalAsciiDirectory + "\\" + m_Project.m_OrderNumber;
      }
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }
      if (counts == null)
      {
        if (m_Project.m_ProjectDataState == DataState.Empty)
        {
          MessageBox.Show("No Data in project.", "Data Error", MessageBoxButton.OK);
          progressWindow.Close();
          return false;
        }
        List<Count> allCounts = m_Project.GetAllTmcCounts();
        for (int i = 0; i < allCounts.Count; i++)
        {
          progressWindow.SetPct(i * 100 / allCounts.Count);
          if (generateExcelInsteadOfAscii)
          {
            allCounts[i].GenerateExcelDeliverable(directory);
          }
          else
          {
            allCounts[i].ExportData(directory, exportOldWayCheckBox.IsChecked == true);
          }
        }
        progressWindow.Close();
        return true;
      }

      for (int i = 0; i < counts.Count; i++)
      {
        progressWindow.SetPct((i + 1) * 100 / counts.Count);
        if (generateExcelInsteadOfAscii)
        {
          counts[i].GenerateExcelDeliverable(directory);
        }
        else
        {
          counts[i].ExportData(directory, exportOldWayCheckBox.IsChecked == true);
        }
      }
      progressWindow.Close();
      return true;
    }

    private void RunConversionWizard()
    {
      //TODO: This can be uncommented and run, but QCConversion will throw an error because it can't connect to SQL outside of QC.  The Path 
      // must be changed to your local executable as well.
      bool NetworkOk = CheckNetworkDirectoryPath(m_Project.m_Prefs.m_NetworkQCConversionDirectory);
      string path;
      if (NetworkOk)
      {
        path = m_Project.m_Prefs.m_NetworkQCConversionDirectory;
      }
      else
      {
        path = m_Project.m_Prefs.m_LocalQCConversionDirectory;
      }
      string conversionPath = path + @"\QCConversion.exe";
      if (!File.Exists(conversionPath))
      {
        MessageBox.Show("Conversion Wizard not found in the specified path.", "File Not Found", MessageBoxButton.OK);
        return;
      }
      Process.Start(conversionPath);
    }

    #endregion

    #region New Project Setup

    //Sets up Merlin for creating a new project, handling closing of opened project if there is one. Bool indicates if user allowed this to happen successfully.
    private bool NewProject()
    {
      if (m_CurrentState.m_ProjectState == ProjectState.Loaded)
      {
        if (!CloseProject())
        {
          return false;
        }
      }
      ChangeDetailsTabState(DetailsTabState.Creating);
      DetailsTab.IsSelected = true;
      ShowSplashScreen(false);
      return true;
    }

    //Populates details tab with results of import; assumes in creation mode with NewProject() executed.
    private void PopulateDetailsTabWithImportedDetails(ProjectDetailsReview pdr)
    {
      OrderNumTextBox.Text = pdr.OrderNumber.ToString();
      if (pdr.TubeOrderDifferent)
      {
        TubeOrderNumTextBox.Text = pdr.TubeOrderNumber.ToString();
        SeparateTubeOrderNumCheckBox.IsChecked = true;
      }
      ProjectNameTextBox.Text = pdr.ProjectName;

      #region Add Time Periods To Details Tab
      //Add time periods to UI
      //Since this is in new project mode we will have 8 time period modules already in the UI
      List<ProjectDetailsReview.SQLTimePeriod> sortedProjTPs = pdr.TimePeriods.OrderBy(x => x.StartTime.TimeOfDay).ToList();
      TimePeriodModule uiTP;
      int numAM = 0;
      int numMID = 0;
      int numPM = 0;
      int numCustom = 0;
      int i;
      for (i = 0; i < sortedProjTPs.Count; i++)
      {
        //adds a time period module to UI if needed
        if (TimePeriodList.Items.Count < i + 1)
        {
          AddTimePeriodToDetailsTab(sortedProjTPs[i].ID);
        }

        uiTP = (TimePeriodModule)TimePeriodList.Items.GetItemAt(i);
        uiTP.ActiveCheckBox.IsChecked = true;
        uiTP.m_ID = sortedProjTPs[i].ID;
        int startHour = sortedProjTPs[i].StartTime.Hour;
        int endHour = sortedProjTPs[i].EndTime.Hour;
        //label the time period in the UI
        if (startHour >= 6 && endHour <= 10)
        {
          uiTP.TimePeriodLabel.Text = "AM " + (++numAM).ToString();
        }
        else if (startHour >= 10 && endHour <= 15)
        {
          uiTP.TimePeriodLabel.Text = "MID " + (++numMID).ToString();
        }
        else if (startHour >= 15 && endHour <= 19)
        {
          uiTP.TimePeriodLabel.Text = "PM " + (++numPM).ToString();
        }
        else
        {
          uiTP.TimePeriodLabel.Text = "Custom " + (++numCustom).ToString();
        }
        //set time
        uiTP.StartTimePicker.hasEnteredBoxValChanged = true;
        uiTP.StartTimePicker.ignoreTextChangedEvent = true;
        uiTP.EndTimePicker.hasEnteredBoxValChanged = true;
        uiTP.EndTimePicker.ignoreTextChangedEvent = true;
        uiTP.StartTimePicker.hours.Text = sortedProjTPs[i].StartTime.ToString("%h");
        uiTP.StartTimePicker.minutes.Text = sortedProjTPs[i].StartTime.ToString("mm");
        uiTP.EndTimePicker.hours.Text = sortedProjTPs[i].EndTime.ToString("%h");
        uiTP.EndTimePicker.minutes.Text = sortedProjTPs[i].EndTime.ToString("mm");
        uiTP.StartTimePicker.hasEnteredBoxValChanged = false;
        uiTP.StartTimePicker.ignoreTextChangedEvent = false;
        uiTP.EndTimePicker.hasEnteredBoxValChanged = false;
        uiTP.EndTimePicker.ignoreTextChangedEvent = false;
        //set AM or PM
        if (sortedProjTPs[i].StartTime.Hour < 12)
        {
          uiTP.StartTimePicker.AMorPM.SelectedIndex = 0;
        }
        else
        {
          uiTP.StartTimePicker.AMorPM.SelectedIndex = 1;
        }
        if (sortedProjTPs[i].EndTime.Hour < 12)
        {
          uiTP.EndTimePicker.AMorPM.SelectedIndex = 0;
        }
        else
        {
          uiTP.EndTimePicker.AMorPM.SelectedIndex = 1;
        }
        //calculate time period duration
        uiTP.SetCountDurationText();
      }

      //uncheck and clear any remaining unused time period modules
      for (; i < TimePeriodList.Items.Count; i++)
      {
        uiTP = (TimePeriodModule)TimePeriodList.Items.GetItemAt(i);

        uiTP.m_ID = NextTimePeriodID();

        uiTP.TimePeriodLabel.Text = "";
        uiTP.StartDatePicker.SelectedDate = null;

        uiTP.StartTimePicker.hours.Text = "";
        uiTP.StartTimePicker.minutes.Text = "";
        uiTP.StartTimePicker.AMorPM.SelectedIndex = 0;

        uiTP.EndTimePicker.hours.Text = "";
        uiTP.EndTimePicker.minutes.Text = "";
        uiTP.EndTimePicker.AMorPM.SelectedIndex = 0;

        uiTP.ActiveCheckBox.IsChecked = false;
      }
      #endregion

      #region Add Locations To Details Tab

      LocationModule currentLocInUI;
      foreach (ProjectDetailsReview.Location loc in pdr.Locations.OrderBy(loc => loc.TimePeriods.Min(tp => tp.SiteNumber)))
      {
        //add location module in UI
        currentLocInUI = AddTMCLocationToList(loc.ID, loc.Latitude, loc.Longitude);

        currentLocInUI.NBSB.Text = loc.NSStreet;
        currentLocInUI.EBWB.Text = loc.EWStreet;

        //iterate through each visible UI TimePeriod (should be one for each active project TP) of this UI Location
        foreach (TimePeriodUI LocUITP in currentLocInUI.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().Where(x => x.Visibility == Visibility.Visible))
        {
          //get UI project tp that corresponds to this current location TP
          TimePeriodModule uiProjTP = (TimePeriodModule)TimePeriodList.Items.GetItemAt(LocUITP.timePeriodIndex);
          //will find a matching time period in pdr if this location has a count for this time period
          ProjectDetailsReview.SQLTimePeriod importTP = loc.TimePeriods.FirstOrDefault(x => x.ID == uiProjTP.m_ID);
          //found a match, get details
          if (importTP != null)
          {
            LocUITP.isActiveCheckBox.IsChecked = true;
            LocUITP.SiteCode.Text = importTP.SiteNumber.ToString().Substring(6);
          }
          //no match, the imported location doesn't have a count for this particular time period
          else
          {
            LocUITP.isActiveCheckBox.IsChecked = false;
          }
        }
      }

      #endregion

      //TODO: Make it so single time period label group (AM, MID, PM, Custom) doesn't get numbered if it's the only one.

      #region Tubes

      var addTubeLocButton = TubeLocationListBox.Items.GetItemAt(TubeLocationListBox.Items.Count - 1);
      TubeLocationListBox.Items.Clear();
      foreach (TubeSite tube in pdr.Tubes.OrderBy(tubeLoc => tubeLoc.m_TubeCounts.Min(tubeCount => tubeCount.m_SiteCode)))
      {
        TubeLocationListBox.Items.Add(new TubeLocationModule(this, TubeLocationListBox, tube));
      }
      TubeLocationListBox.Items.Add(addTubeLocButton);

      #endregion

    }

    private void DetailsTab_LeftMouseClicked(object sender, MouseButtonEventArgs e)
    {
      ClearStacks();
      ClearFlagStacks();
    }

    private void detailsTabApplyBankPresetButton_Click(object sender, RoutedEventArgs e)
    {
      LaunchPresetWindow();
    }

    public void LaunchPresetWindow()
    {
      ApplyBankPresetWindow presetWindow = new ApplyBankPresetWindow();
      presetWindow.Owner = this;
      presetWindow.ShowDialog();

      if (presetWindow.GUIclickedPreset != null)
      {
        ncdotColSwapCheckBox.IsChecked = presetWindow.colSwap;
        tccCheckBox.IsChecked = presetWindow.tccRules;
        PopulateGuiBanksOnDetailsTab(presetWindow.clickedPresetDic);
      }
    }

    /// <summary>
    /// Repopulates bank combobox with standard or FHWA vehicle types depending on whether TCC checkbox is checked.
    /// </summary>
    /// <param name="comboBoxBanks">Bank ComboBox</param>
    private void PopulateVehBankDropDownList(ComboBox comboBoxBanks)
    {
      comboBoxBanks.Items.Clear();
      //If TCC checkbox is checked, populate with FHWA vehicle types
      if ((bool)tccCheckBox.IsChecked)
      {
        foreach (var bank in Enum.GetValues(typeof(BankVehicleTypes)))
        {
          if (bank.ToString().StartsWith("FHWA"))
          {
            comboBoxBanks.Items.Add(bank.ToString());
          }
        }
      }
      ////Else populate with standard vehicle types
      //else
      //{
      //  foreach (var bank in Enum.GetValues(typeof(BankVehicleTypes)))
      //  {
      //    if (!bank.ToString().StartsWith("FHWA"))
      //    {
      //      comboBoxBanks.Items.Add(bank.ToString());
      //    }
      //  }
      //}
      //Else populate with ALL vehicle types, because there can be FHWA types in non-TCC projects
      else
      {
        foreach (var bank in Enum.GetValues(typeof(BankVehicleTypes)))
        {
          comboBoxBanks.Items.Add(bank.ToString());
        }
      }
      comboBoxBanks.Items.Add("NOT USED");
      comboBoxBanks.SelectedValue = "NOT USED";
    }

    private void PopulatePedBankDropDownList(ComboBox comboBoxPeds)
    {
      comboBoxPeds.Items.Clear();
      foreach (var pedColType in Enum.GetValues(typeof(PedColumnDataType)))
      {
        comboBoxPeds.Items.Add(pedColType.ToString());
      }
      comboBoxPeds.SelectedValue = PedColumnDataType.NA.ToString();
    }

    private void PopulateGuiBanksOnDetailsTab(Dictionary<int, KeyValuePair<string, PedColumnDataType>> banks)
    {
      List<ComboBox> vehCombos = GetVehicleBankComboBoxes();
      List<ComboBox> pedCombos = GetPedBankComboBoxes();

      for (int i = 0; i < Constants.MAX_BANKS_ALLOWED; i++)
      {
        vehCombos[i].SelectedValue = banks[i].Key;
        pedCombos[i].SelectedValue = banks[i].Value.ToString();
      }

      UpdateVisibleBankComboBoxes();
    }

    //Generates the default 8 starting time periods
    private void GenerateTimePeriodList()
    {

      List<string> timePeriodNames = new List<string>();

      timePeriodNames.AddRange(new string[] { "AM", "MID", "PM", "C1", "C2", "C3", "C4", "C5" });
      for (int i = 0; i < 8; i++)
      {
        TimePeriodModule timePeriod = new TimePeriodModule(this, NextTimePeriodID());
        timePeriod.Name = timePeriodNames[i];
        //timePeriod.Width = 463;
        TimePeriodList.Items.Add(timePeriod);
        timePeriod.TimePeriodLabel.Text = timePeriodNames[i];
        timePeriod.Margin = new Thickness(0, 0, 0, 2);
      }
      //By default sets AM and PM to active and sets them 7-9am and 4-6pm respectively
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).ActiveCheckBox.IsChecked = true;
      //To be able to set times, must temporarily change hasEnteredBoxValChanged for it to work with TimePicker
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).StartTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).EndTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).StartTimePicker.hours.Text = "7";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).StartTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).StartTimePicker.AMorPM.SelectedIndex = 0;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).EndTimePicker.hours.Text = "9";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).EndTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).EndTimePicker.AMorPM.SelectedIndex = 0;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).StartTimePicker.hasEnteredBoxValChanged = false;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(0)).EndTimePicker.hasEnteredBoxValChanged = false;

      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).ActiveCheckBox.IsChecked = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).StartTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).EndTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).StartTimePicker.hours.Text = "4";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).StartTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).StartTimePicker.AMorPM.SelectedIndex = 1;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).EndTimePicker.hours.Text = "6";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).EndTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).EndTimePicker.AMorPM.SelectedIndex = 1;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).StartTimePicker.hasEnteredBoxValChanged = false;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(2)).EndTimePicker.hasEnteredBoxValChanged = false;

      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).StartTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).EndTimePicker.hasEnteredBoxValChanged = true;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).StartTimePicker.hours.Text = "11";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).StartTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).StartTimePicker.AMorPM.SelectedIndex = 0;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).EndTimePicker.hours.Text = "1";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).EndTimePicker.minutes.Text = "00";
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).EndTimePicker.AMorPM.SelectedIndex = 1;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).StartTimePicker.hasEnteredBoxValChanged = false;
      ((TimePeriodModule)TimePeriodList.Items.GetItemAt(1)).EndTimePicker.hasEnteredBoxValChanged = false;

    }

    private void ImportSync_Click(object sender, RoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
      {
        ProjectDetailsSync pds = new ProjectDetailsSync(m_Project);
        pds.Owner = this;
        bool? result = pds.ShowDialog();
        if (result == true)
        {

        }

      }
      else
      {
        MenuNewWeb_Click(sender, e);
      }
    }


    #region Synching Between Time Period Modules and Location Modules

    public delegate void LocUpdateFunc(int tpIndex, LocationModule loc);

    public void UpdateLocationTimePeriodLabel(int tpIndex, LocationModule loc)
    {
      TimePeriodUI locTP = loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == tpIndex);
      locTP.TimePeriodText.Text = ((TimePeriodModule)TimePeriodList.Items[tpIndex]).TimePeriodLabel.Text;
    }

    public void AddTimePeriodToLocation(int tpIndex, LocationModule loc)
    {
      TimePeriodUI count = new TimePeriodUI(tpIndex);
      count.OrderNumForSiteCode.Text = OrderNumTextBox.Text;
      loc.locationTimePeriodsPanel.Children.Add(count);
    }

    public void ChangeLocationTimePeriodActiveState(int tpIndex, LocationModule loc)
    {
      TimePeriodUI currentLocTP;
      currentLocTP = loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == tpIndex);

      if ((bool)((TimePeriodModule)TimePeriodList.Items[tpIndex]).ActiveCheckBox.IsChecked)
      {
        currentLocTP.Visibility = Visibility.Visible;
      }
      else
      {
        currentLocTP.Visibility = Visibility.Collapsed;
      }
    }

    //Updates all location modules, only updating the necessary elements as specified by the list of functions passed in.
    public void updateAllLocationsTimePeriods(int indexOfChangedProjectTimePeriod, List<LocUpdateFunc> changesToMakeToLocationModules)
    {
      foreach (LocationModule loc in LocationListBox.Items.OfType<LocationModule>())
      {
        foreach (LocUpdateFunc func in changesToMakeToLocationModules)
        {
          func(indexOfChangedProjectTimePeriod, loc);
        }
      }
    }

    #endregion

    private void AddLocationCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating || m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
      {
        if (TubeTab.IsSelected)
        {
          AddTubeLocationToList(NextLocationID(), "", "");
        }
        else
        {
          AddTMCLocationToList(NextLocationID(), "", "");
        }
      }
    }

    //Bubbles mouse scroll up to parent
    private void BubbleUp_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (!e.Handled)
      {
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
        eventArg.RoutedEvent = UIElement.MouseWheelEvent;
        eventArg.Source = sender;
        var parent = ((Control)sender).Parent as UIElement;
        parent.RaiseEvent(eventArg);
      }
    }

    //validates numeric text only in a textbox on value changed
    public void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox callingTextBox = (TextBox)sender;
      int value;
      if ((callingTextBox.Text == "") || (Int32.TryParse(callingTextBox.Text, out value)))
      //text is valid (numeric)
      {
        //update current textbox text tracker
        m_NumericTextBoxTracker = callingTextBox.Text;

        //update all applicable TMC and tube location modules
        if (callingTextBox.Name == "OrderNumTextBox")
        {
          foreach (LocationModule loc in LocationListBox.Items.OfType<LocationModule>())
          {
            foreach (TimePeriodUI tp in loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>())
            {
              tp.OrderNumForSiteCode.Text = callingTextBox.Text;
            }
          }
          if (SeparateTubeOrderNumCheckBox.IsChecked != true)
          {
            ApplyOrderNumToTubeSurveysInGUI(callingTextBox.Text);
          }
        }
        else
        {
          ApplyOrderNumToTubeSurveysInGUI(callingTextBox.Text);
        }

        return;
      }
      else
      {
        //disregard change
        callingTextBox.Text = m_NumericTextBoxTracker;
        //move caret to end
        callingTextBox.CaretIndex = callingTextBox.GetLineLength(0);
      }

    }

    //Numeric text boxes that use the tracker and NumericTextBox_TextChanged
    //to validate numeric entry only need to use this event handler
    public void NumericTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      m_NumericTextBoxTracker = "";
    }

    //Deletes selected location (tmc or tube), showing a confirmation message box
    private void DeleteLocation_Click(object sender, RoutedEventArgs e)
    {
      string messageBoxLocation = "";
      if (e.Source is LocationModule)
      {
        LocationModule location = ((LocationModule)(e.Source));
        string NS = location.NBSB.Text;
        string EW = location.EBWB.Text;


        //If both street name fields blank, just delete
        if (string.IsNullOrWhiteSpace(NS) && string.IsNullOrWhiteSpace(EW))
        {
          if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
          {
            LocationListBox.Items.Remove(location);
          }
          else
          {
            location.Visibility = Visibility.Collapsed;
          }
        }
        //Otherwise delete upon user confirmation
        else
        {
          //Builds name of intersection to display to be deleted
          if (!(string.IsNullOrWhiteSpace(NS)) && !(string.IsNullOrWhiteSpace(EW)))
            messageBoxLocation = NS + " & " + EW;

          MessageBoxResult result = MessageBox.Show("Are you sure you want to remove the selected location?\n\n"
            + messageBoxLocation, "Delete Location", MessageBoxButton.YesNo);

          if (result == MessageBoxResult.Yes)
          {
            if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
            {
              LocationListBox.Items.Remove(location);
            }
            else
            {
              location.Visibility = Visibility.Collapsed;
            }
          }
        }
      }
      else if (e.Source is TubeLocationModule)
      {
        TubeLocationModule location = ((TubeLocationModule)e.Source);

        if (string.IsNullOrWhiteSpace(location.locationTextBox.Text))
        {
          if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
          {
            TubeLocationListBox.Items.Remove(location);
          }
          else
          {
            location.Visibility = Visibility.Collapsed;
          }
        }
        else
        {
          MessageBoxResult result = MessageBox.Show("Are you sure you want to remove the selected location?\n\n"
            + location.locationTextBox.Text, "Delete Location", MessageBoxButton.YesNo);

          if (result == MessageBoxResult.Yes)
          {
            if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
            {
              TubeLocationListBox.Items.Remove(location);
            }
            else
            {
              location.Visibility = Visibility.Collapsed;
            }
          }
        }
      }


      SetIntersectionListBackgrounds(sender);
    }

    private void NextScreen_Customize_Click(object sender, RoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating || m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
      {
        StringBuilder errors = ValidateProjSetupScreen_Errors();
        if (errors.Length == 0)
        {
          //No errors
          foreach (var tp in TimePeriodList.Items.OfType<TimePeriodModule>())
          {
            tp.UpdateInternalStartAndEndTimes();
          }

          StringBuilder warnings = ValidateProjSetupScreen_Warnings();

          //display any warnings
          if (warnings.Length > 0)
          {
            MessageBoxResult result = MessageBox.Show("Proceed with the following warnings?\n\n" + warnings, "Warnings", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
              return;
            }
          }

          //No errors, user proceeded past warnings or there were no warnings, execute project creation or saving of edits (depending on mode)

          if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
          {
            CreateProjectFromScratch();
            SaveProjectWithDialog(true);
            MarkProjectFileReadOnly(m_FilePath, true);
            AddFileToRecentList(m_FilePath);
            PopulateRecentProjectMenuItems();
          }
          else
          {
            //else in editing mode

            //apply edits internally
            string editWarningText = "Any changes made will be reflected in the current project.\n\nTMCs:\nReduction or removal of time periods, removal of counts, removal of a bank, or unselecting RTOR or U-Turns will cause all information associated with that data to be deleted.\n\nTubes:\nChanging a tube location's diagram layout will cause all data to be deleted for that location. Changing a tube survey's start or end times or tube survey type will cause all data to be deleted for that tube survey.\n\nProceed?";
            MessageBoxResult result = MessageBox.Show(editWarningText,
              "Confirm Project Edits", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
            {
              if (ApplyEditsToProject())
              {
                //ApplyEditsToProject returning true means user wants to save file
                if (String.IsNullOrEmpty(m_FilePath))
                {
                  SaveProjectWithDialog(false);
                }
                else
                {
                  SaveFile(m_FilePath);
                }
              }
            }
          }
        }
        else
        {
          //There are errors, show them and don't proceed
          MessageBox.Show("Correct the following errors:\n\n" + errors, "Can't Proceed", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
      }

    }

    #region Input Validation

    //Validates project setup screen, returning StringBuilder of error lines
    private StringBuilder ValidateProjSetupScreen_Errors()
    {
      StringBuilder errorText = new StringBuilder();

      #region Order Number & Banks

      int tryParseInt;
      //tests order number text box
      if (OrderNumTextBox.Text.Length != 6 || !Int32.TryParse(OrderNumTextBox.Text, out tryParseInt))
      {
        errorText.AppendLine("Project Order Number is not a 6-digit numeric value");
      }
      if ((bool)SeparateTubeOrderNumCheckBox.IsChecked && (TubeOrderNumTextBox.Text.Length != 6 || !Int32.TryParse(OrderNumTextBox.Text, out tryParseInt)))
      {
        errorText.AppendLine("Tube Order Number is not a 6-digit numeric value");
      }
      //tests data bank menus

      //...for no vehicle banks selected
      int vehBankCount = 0;
      foreach (ComboBox cb in GetVehicleBankComboBoxes())
      {
        if (cb.SelectedValue.ToString() != "NOT USED")
        {
          vehBankCount++;
        }
      }
      if (vehBankCount == 0)
      {
        errorText.AppendLine("No vehicle banks selected");
      }

      //...for a vehicle type being assigned to multiple banks
      List<int> selectedBankIndices = new List<int>();
      foreach (ComboBox cb in GetVehicleBankComboBoxes())
      {
        selectedBankIndices.Add(cb.SelectedIndex);
      }

      var duplicates = selectedBankIndices.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);
      foreach (var index in duplicates)
      {
        string duplicateVehicleType = GetVehicleBankComboBoxes()[0].Items.GetItemAt(index).ToString();
        if (duplicateVehicleType != "NOT USED")
        {
          errorText.AppendLine(duplicateVehicleType + " has been assigned to more than one bank");
        }
      }

      //...for passenger vehicles and FHWA 1,2,3 both selected
      List<string> selectedDataBanks = new List<string>();
      foreach (ComboBox cb in GetVehicleBankComboBoxes())
      {
        selectedDataBanks.Add(cb.SelectedValue.ToString());
      }
      if (selectedDataBanks.Contains(BankVehicleTypes.FHWA1_2_3.ToString()) && selectedDataBanks.Contains(BankVehicleTypes.Passenger.ToString()))
      {
        errorText.AppendLine("Passenger and FHWA 1-3 are both selected; type overlap");
      }

      //...for non-consecutive banks
      bool hasEmptyBankBeenEncountered = false;
      List<ComboBox> veh = GetVehicleBankComboBoxes();
      List<ComboBox> ped = GetPedBankComboBoxes();
      for (int i = 0; i < veh.Count; i++)
      {
        bool currentBankIsUnused;
        if ((bool)tccCheckBox.IsChecked)
        {
          currentBankIsUnused = veh[i].SelectedValue.ToString() == "NOT USED";
        }
        else
        {
          currentBankIsUnused = veh[i].SelectedValue.ToString() == "NOT USED" && ped[i].SelectedValue.ToString() == "NA";
        }
        if (currentBankIsUnused)
        {
          hasEmptyBankBeenEncountered = true;
        }
        else
        {
          if (hasEmptyBankBeenEncountered)
          {
            errorText.AppendLine("Banks don't start at 0 or are not consecutive");
            break;
          }
        }
      }

      if (!(bool)tccCheckBox.IsChecked)
      {
        //Tests ped column data type dropdown menus
        List<string> selectedPedBankTypes = new List<string>();
        foreach (ComboBox cb in GetPedBankComboBoxes())
        {
          selectedPedBankTypes.Add(cb.SelectedValue.ToString());
        }

        //...for invalid combinations (such as combined U-Turns and either heavy or passenger U-Turns)
        if (selectedPedBankTypes.Contains(PedColumnDataType.RTOR.ToString()) && (selectedPedBankTypes.Contains(PedColumnDataType.PassengerRTOR.ToString())
          || selectedPedBankTypes.Contains(PedColumnDataType.HeavyRTOR.ToString())))
        {
          errorText.AppendLine("Normal RTORs and classified RTORs both selected");
        }
        //if (selectedPedBankTypes.Contains(PedColumnDataType.UTurn.ToString()) && (selectedPedBankTypes.Contains(PedColumnDataType.PassengerUTurn.ToString())
        //  || selectedPedBankTypes.Contains(PedColumnDataType.HeavyUTurn.ToString())))
        //{
        //  errorText.AppendLine("Normal U-Turns and classified U-Turns both selected");
        //}

        //...for duplicate selections
        var duplicatePedBanks = selectedPedBankTypes.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var type in duplicatePedBanks)
        {
          if (type != "NA")
          {
            errorText.AppendLine(type + " has been assigned to more than one bank");
          }
        }
      }

      #endregion

      #region Time Period Modules

      //tests time period modules
      bool areAnyTimePeriodsChecked = false;

      foreach (TimePeriodModule tp in TimePeriodList.Items.OfType<TimePeriodModule>())
      {
        if ((bool)tp.ActiveCheckBox.IsChecked)
        {
          areAnyTimePeriodsChecked = true;
          //Checking to make sure DatePicker is not null is now redundant because I'm already checking for the location module DatePickers
          if (/*(tp.StartDatePicker.SelectedDate == null && tp.StartDatePicker.Visibility == Visibility.Visible)
            ||*/ !tp.StartTimePicker.IsTimeValid() || !tp.EndTimePicker.IsTimeValid())
          {
            errorText.AppendLine(tp.TimePeriodLabel.Text + " time period information is not complete");
          }
        }
      }
      if (areAnyTimePeriodsChecked == false)
      {
        errorText.AppendLine("No time periods have been selected");
      }

      var duplicateTPNames = TimePeriodList.Items.OfType<TimePeriodModule>().Where(x => (bool)x.ActiveCheckBox.IsChecked).GroupBy(x => x.TimePeriodLabel.Text).Where(x => x.Count() > 1).Select(x => x.Key);
      foreach (var duplicateTP in duplicateTPNames)
      {
        errorText.AppendLine("Time Period label \"" + duplicateTP + "\" assigned to more than one time period");
      }

      #endregion

      #region Location Modules

      //tests location modules

      int locCount = 0;

      //Stores (count info, site code) for every count so in case of duplicate site code error, count info is available
      Dictionary<string, string> siteCodeInventory = new Dictionary<string, string>();
      //Dictionary of time period index/bools for each time period in the project which tracks whether that time period has been assigned at least one count
      Dictionary<int, bool> timePeriods_AtLeastOneCount = new Dictionary<int, bool>();
      //initialize to false
      foreach (TimePeriodModule projTP in TimePeriodList.Items.OfType<TimePeriodModule>().Where(x => (bool)x.ActiveCheckBox.IsChecked))
      {
        timePeriods_AtLeastOneCount.Add(TimePeriodList.Items.IndexOf(projTP), false);
      }

      foreach (LocationModule loc in (LocationListBox.Items).OfType<LocationModule>())
      {
        if (loc.Visibility == Visibility.Visible)
        {
          locCount++;
          //...for empty street names
          if (string.IsNullOrWhiteSpace(loc.NBSB.Text) || string.IsNullOrWhiteSpace(loc.EBWB.Text))
          {
            errorText.AppendLine(loc.LocationNum.Text + " has missing street name(s)");
          }

          bool LocHasAtLeastOneTP = false;
          foreach (TimePeriodUI tp in loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().Where(x => (bool)x.isActiveCheckBox.IsChecked && x.Visibility == Visibility.Visible))
          {
            //the time period this count is assigned to has at least one count assigned to it
            timePeriods_AtLeastOneCount[tp.timePeriodIndex] = true;
            LocHasAtLeastOneTP = true;
            string offendingCount = loc.NBSB.Text + " & " + loc.EBWB.Text + ", " + tp.TimePeriodText.Text + " count: ";

            //...for invalid count dates
            if (tp.CountDate.SelectedDate == null)
            {
              errorText.AppendLine(offendingCount + "Invalid Date");
            }
            //...for invalid site codes
            if (string.IsNullOrWhiteSpace(tp.SiteCode.Text))
            {
              errorText.AppendLine(offendingCount + "Invalid Site Code");
            }
            //add count to site code dictionary for the upcoming duplicate site code test
            if (!string.IsNullOrEmpty(tp.SiteCode.Text))
            {
              string key = "\t\u25E6 " + loc.LocationNum.Text + " " + loc.NBSB.Text + " & " + loc.EBWB.Text + ", " + tp.TimePeriodText.Text + " count";

              //Handles the scenario where duplicate site codes are assigned to counts whose time periods share the same label for some reason
              int num = 1;
              while (siteCodeInventory.ContainsKey(key))
              {
                key = "\t\u25E6 " + loc.LocationNum.Text + " " + loc.NBSB.Text + " & " + loc.EBWB.Text + ", " + tp.TimePeriodText.Text + "(" + num++ + ") count";
              }

              siteCodeInventory.Add(key, tp.OrderNumForSiteCode.Text + tp.SiteCode.Text);
            }
          }
          //...for no time periods assigned
          if (!LocHasAtLeastOneTP)
          {
            errorText.AppendLine(loc.NBSB.Text + " & " + loc.EBWB.Text + ": No Time Periods Selected");
          }
        }
      }

      //...to make sure for every project time period at least one location is using it
      foreach (var entry in timePeriods_AtLeastOneCount)
      {
        if (entry.Value == false)
        {
          errorText.AppendLine(((TimePeriodModule)TimePeriodList.Items.GetItemAt(entry.Key)).TimePeriodLabel.Text + " Time Period Not Assigned To Any Counts");
        }
      }

      //...for zero number of locations
      if (locCount == 0)
      {
        errorText.AppendLine("There are no TMC locations entered");
      }

      #endregion

      #region Tubes

      //Check tube location modules
      foreach (TubeLocationModule loc in (TubeLocationListBox.Items).OfType<TubeLocationModule>().Where(x => x.Visibility == Visibility.Visible))
      {
        string offender = "[Tube] " + loc.locationTextBox.Text + ": ";
        //...for empty street names
        if (string.IsNullOrWhiteSpace(loc.locationTextBox.Text))
        {
          errorText.AppendLine("Tube has missing street name(s)");
        }

        //...for no survey times
        int surveyCount = 0;
        foreach (TubeTimePeriodUI tp in loc.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>())
        {
          if (tp.Visibility == Visibility.Visible)
          {
            surveyCount++;
          }
        }
        if (surveyCount == 0)
        {
          errorText.AppendLine(offender + "No tube survey times added");
        }

        //Check tube location module's survey modules
        foreach (TubeTimePeriodUI tp in loc.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>().Where(x => x.Visibility == Visibility.Visible))
        {
          //...for invalid survey dates
          if (tp.StartTime == null || tp.EndTime == null || tp.EndTime <= tp.StartTime)
          {
            errorText.AppendLine(offender + "A tube survey contains an invalid Date/Time");
          }
          //...for invalid site codes
          if (string.IsNullOrWhiteSpace(tp.SiteCodeTextBox.Text))
          {
            errorText.AppendLine(offender + "A tube survey contains an invalid Site Code");
          }

          //add count to site code dictionary for the upcoming duplicate site code test
          if (!string.IsNullOrEmpty(tp.SiteCodeTextBox.Text))
          {
            string key = "\t\u25E6 The #" + (loc.surveyTimesWrapPanel.Children.IndexOf(tp) + 1) + " tube survey at the #" + (TubeLocationListBox.Items.IndexOf(loc) + 1) + " location \"" + loc.locationTextBox.Text + "\"";

            siteCodeInventory.Add(key, tp.OrderNumForSiteCode.Text + tp.SiteCodeTextBox.Text);
          }
        }
      }

      #endregion

      if (errorText.Length > 0)
      {
        //Some formatting of the error text
        errorText.Insert(0, "\u2022 ");
        errorText.Replace("\n", "\n\u2022 ");
        errorText.Replace("\u2022 \t\u25E6 ", "\t\u25E6 ");
        errorText.Remove(errorText.Length - 2, 2);
      }

      //Check proejct for duplicate site codes
      DuplicateSiteCodeTests(siteCodeInventory, errorText);

      return errorText;
    }

    //helper for ValidateProjSetupScreen_Errors
    private void DuplicateSiteCodeTests(Dictionary<string, string> siteCodeList, StringBuilder errors)
    {
      var duplicateValues = siteCodeList.GroupBy(x => x.Value).Where(x => x.Count() > 1);

      foreach (var repeatedSiteCode in duplicateValues)
      {
        errors.AppendLine("The site code " + repeatedSiteCode.Key + " is assigned to multiple counts:");
        foreach (var offendingCount in siteCodeList)
        {
          if (offendingCount.Value == repeatedSiteCode.Key)
          {
            errors.AppendLine(offendingCount.Key);
          }
        }
      }
    }

    //Validates project setup screen, returning StringBuilder of warning lines
    private StringBuilder ValidateProjSetupScreen_Warnings()
    {
      //check for heavy u-turns without heavy bank
      StringBuilder warningText = new StringBuilder();
      bool heaviesInVehBanks = false;
      bool heaviesInPedCol = false;

      List<string> selectedDataBanks = new List<string>();
      foreach (ComboBox cb in GetVehicleBankComboBoxes())
      {
        selectedDataBanks.Add(cb.SelectedValue.ToString());
      }

      List<string> selectedPedBanks = new List<string>();
      foreach (ComboBox cb in GetPedBankComboBoxes())
      {
        selectedPedBanks.Add(cb.SelectedValue.ToString());
      }

      if (selectedDataBanks.Contains(BankVehicleTypes.Heavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.Buses.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.LightHeavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.MediumHeavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.HeavyHeavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA4_5_6_7.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA8_9_10.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA11_12_13.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.SingleUnitHeavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.MultiUnitHeavies.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA4.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA5.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA6.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA7.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA8.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA9.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA10.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA11.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA12.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA13.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.TEX_UTurnLaneHeavy.ToString()) ||
        selectedDataBanks.Contains(BankVehicleTypes.FHWA6through13.ToString()))
      {
        heaviesInVehBanks = true;
      }

      if (selectedPedBanks.Contains(PedColumnDataType.HeavyRTOR.ToString()) || selectedPedBanks.Contains(PedColumnDataType.HeavyUTurn.ToString()))
      {
        heaviesInPedCol = true;
      }

      if (heaviesInPedCol && !heaviesInVehBanks)
      {
        warningText.AppendLine("Project contains heavy ped column data but no heavy bank");
      }


      if (warningText.Length > 0)
      {
        //Some formatting of the error text
        warningText.Insert(0, "\u2022 ");
        warningText.Replace("\n", "\n\u2022 ");
        warningText.Remove(warningText.Length - 2, 2);
      }
      return warningText;
    }

    #endregion

    //updates alternating colors in intersection listbox
    //  also updates location numbers
    private void SetIntersectionListBackgrounds(object sender)
    {
      SolidColorBrush oddColor = (SolidColorBrush)FindResource("DataGridBackgroundColor");
      SolidColorBrush evenColor = (SolidColorBrush)FindResource("AlternatingDataGridRowColor");

      //just adding to end of list
      if (sender == AddLocListBoxItem)
      {
        LocationModule addedLocation = ((LocationModule)LocationListBox.Items.GetItemAt(LocationListBox.Items.OfType<LocationModule>().Count() - 1));
        addedLocation.LocationNum.Text = "Location " + (LocationListBox.Items.Count - 1).ToString();
        if ((LocationListBox.Items.Count - 1) % 2 == 0)
          addedLocation.Background = evenColor;
        else
          addedLocation.Background = oddColor;
      }
      //any other case (deletion, reordering)
      else
      {
        foreach (LocationModule loc in (LocationListBox.Items).OfType<LocationModule>())
        {
          loc.LocationNum.Text = "Location " + (LocationListBox.Items.IndexOf(loc) + 1).ToString();
          if ((LocationListBox.Items.IndexOf(loc) + 1) % 2 == 0)
            loc.Background = evenColor;
          else
            loc.Background = oddColor;
        }
      }

    }

    //Moves a location up or down in the list
    private void DownOrUp_Click(object sender, RoutedEventArgs e)
    {
      //if no item selected or last item (delete button) selected...
      if (LocationListBox.SelectedIndex == -1 || LocationListBox.SelectedIndex == LocationListBox.Items.Count - 1)
        return;
      int currentIndex = LocationListBox.SelectedIndex;
      if (((Button)sender).Name == "Up")
      {
        if (currentIndex != 0)
        {
          LocationModule temp = (LocationModule)LocationListBox.Items.GetItemAt(currentIndex - 1);
          LocationListBox.Items.RemoveAt(currentIndex - 1);
          LocationListBox.Items.Insert(currentIndex, temp);
        }
      }
      else
      {
        if (currentIndex != LocationListBox.Items.Count - 2)
        {
          LocationModule temp = (LocationModule)LocationListBox.Items.GetItemAt(currentIndex + 1);
          LocationListBox.Items.RemoveAt(currentIndex + 1);
          LocationListBox.Items.Insert(currentIndex, temp);
        }
      }
      SetIntersectionListBackgrounds(sender);
    }

    ////Prevents LocationListBox items from being selectable, however reordering locations
    ////  currently relies on a location being selected
    //private void LocationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //  if (null != sender && sender is ListBox)
    //  {
    //    ListBox lv = sender as ListBox;
    //    lv.SelectedIndex = -1;
    //  }
    //}

    private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
      AddPlus.FontWeight = FontWeights.Bold;
      TubeAddPlus.FontWeight = FontWeights.Bold;
      AddLocation.FontWeight = FontWeights.Bold;
      TubeAddLocation.FontWeight = FontWeights.Bold;
      this.Cursor = Cursors.Cross;
    }

    private void ListBoxItem_MouseLeave(object sender, MouseEventArgs e)
    {
      AddPlus.FontWeight = FontWeights.Normal;
      TubeAddPlus.FontWeight = FontWeights.Normal;
      AddLocation.FontWeight = FontWeights.Normal;
      TubeAddLocation.FontWeight = FontWeights.Normal;
      this.Cursor = Cursors.Arrow;
    }

    //Adds new location and disables time period checkboxes for inactive time periods,
    //  else enables and checks checkbox
    private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
    {
      AddTMCLocationToList(NextLocationID(), "", "");
    }

    private void Edit_Cancel_Click(object sender, RoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
      {
        MessageBoxResult result = MessageBox.Show("Are you sure you want to stop editing?\n\nAny edits made in this window will be lost and project will remain unchanged.",
          "Stop Editing", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.OK)
        {
          ChangeDetailsTabState(DetailsTabState.Viewing);
        }
        else
        {
          return;
        }
      }
      else if (m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        ChangeDetailsTabState(DetailsTabState.Editing);
      }
    }

    private LocationModule AddTMCLocationToList(int ID, string lat, string lon)
    {
      LocationModule newLocation = new LocationModule(LocationListBox, ID, lat, lon);
      LocationListBox.Items.Insert(LocationListBox.Items.OfType<LocationModule>().Count(), newLocation);
      SetIntersectionListBackgrounds(AddLocListBoxItem);
      if ((bool)locationsCondensedRadio.IsChecked)
      {
        newLocation.CondenseModule();
      }
      else
      {
        newLocation.ExpandModule();
      }
      LocListScrollviewer.ScrollToEnd();
      //fill out location module's time period details based on project time period list
      foreach (TimePeriodModule projTP in TimePeriodList.Items.OfType<TimePeriodModule>())
      {
        TimePeriodUI newLocTP = new TimePeriodUI(TimePeriodList.Items.IndexOf(projTP));
        newLocation.locationTimePeriodsPanel.Children.Add(newLocTP);
        newLocTP.isActiveCheckBox.IsChecked = true;
        newLocTP.TimePeriodText.Text = projTP.TimePeriodLabel.Text;
        newLocTP.OrderNumForSiteCode.Text = OrderNumTextBox.Text;
        newLocTP.CountDate.SelectedDate = projTP.StartDatePicker.SelectedDate;
        if ((bool)projTP.ActiveCheckBox.IsChecked)
        {
          newLocTP.Visibility = Visibility.Visible;
        }
        else
        {
          newLocTP.Visibility = Visibility.Collapsed;
        }
      }
      return newLocation;
    }

    //changes state of details tab
    public void ChangeDetailsTabState(DetailsTabState changeTo)
    {
      #region Change to Viewing Mode
      if (changeTo == DetailsTabState.Viewing)
      {
        m_CurrentState.m_DetailsTabState = DetailsTabState.Viewing;
        m_CurrentState.m_CreatingWithImportedDetails = false;

        Edit_Cancel.Content = "Edit";

        BanksList.IsEnabled = false;
        ProjectOrderNumPanel.IsEnabled = false;
        ProjectNamePanel.IsEnabled = false;
        TimePeriodList.IsEnabled = false;
        LocationListBox.IsEnabled = false;
        Up.Visibility = Visibility.Collapsed;
        Down.Visibility = Visibility.Collapsed;
        manageLocationsSeparator1.Visibility = Visibility.Collapsed;
        manageLocationsSeparator2.Visibility = Visibility.Collapsed;
        manageProjects.Visibility = Visibility.Visible;
        NextScreen_Customize.Visibility = Visibility.Collapsed;
        Edit_Cancel.IsEnabled = true;
        projectDetailButtons.IsEnabled = false;
        detailsTabApplyBankPresetButton.Visibility = Visibility.Collapsed;
        detailsTabImportDetailsButton.Visibility = Visibility.Collapsed;
        RegenerateSideCodes.Visibility = Visibility.Collapsed;
        columnSwapArea.Visibility = m_Project.m_NCDOTColSwappingEnabled ? Visibility.Visible : Visibility.Collapsed;
        tccArea.Visibility = m_Project.m_TCCDataFileRules ? Visibility.Visible : Visibility.Collapsed;
        ManageTubeLocationsGroupBox.IsEnabled = false;
        TubeLocationListBox.IsEnabled = false;

        PopulateBalancingTab();

        EnableAllTabs();

        LoadProjDetailsInGUI();

        foreach (TimePeriodModule tp in TimePeriodList.Items)
        {
          if ((bool)tp.ActiveCheckBox.IsChecked == false)
          {
            tp.Visibility = Visibility.Collapsed;
            tp.StartDatePicker.Visibility = Visibility.Hidden;
          }
        }

        AddLocListBoxItem.Visibility = Visibility.Collapsed;
        AddTubeLocListBoxItem.Visibility = Visibility.Collapsed;
        AddTimePeriodButton.Visibility = Visibility.Hidden;
        TubeLocationListBox.Items.OfType<TubeLocationModule>().ToList().ForEach(x => x.SetUneditable());
      }
      #endregion

      #region Change to Editing Mode
      else if (changeTo == DetailsTabState.Editing)
      {
        m_CurrentState.m_DetailsTabState = DetailsTabState.Editing;
        m_CurrentState.m_CreatingWithImportedDetails = false;

        foreach (TimePeriodModule tp in TimePeriodList.Items)
        {
          tp.Visibility = Visibility.Visible;
          tp.StartDatePicker.Visibility = Visibility.Visible;
          //if ((bool)tp.ActiveCheckBox.IsChecked == false)
          //{
          //  tp.Visibility = Visibility.Collapsed;
          //}
        }

        Edit_Cancel.Content = "Cancel";

        BanksList.IsEnabled = true;
        ProjectOrderNumPanel.IsEnabled = true;
        ProjectNamePanel.IsEnabled = true;
        TimePeriodList.IsEnabled = true;
        LocationListBox.IsEnabled = true;
        Up.Visibility = Visibility.Collapsed;
        Down.Visibility = Visibility.Collapsed;
        manageLocationsSeparator1.Visibility = Visibility.Collapsed;
        manageLocationsSeparator2.Visibility = Visibility.Collapsed;
        manageProjects.Visibility = Visibility.Visible;
        NextScreen_Customize.Visibility = Visibility.Visible;
        NextScreen_Customize.Content = "Commit Changes";
        Edit_Cancel.IsEnabled = true;
        projectDetailButtons.IsEnabled = true;
        detailsTabApplyBankPresetButton.Visibility = Visibility.Visible;
        detailsTabImportDetailsButton.Visibility = Visibility.Collapsed;
        detailsTabImportDetailsButton.Content = "Web Sync";
        RegenerateSideCodes.Visibility = Visibility.Visible;
        columnSwapArea.Visibility = Visibility.Visible;
        tccArea.Visibility = Visibility.Visible;
        ManageTubeLocationsGroupBox.IsEnabled = true;
        TubeLocationListBox.IsEnabled = true;

        DisableAllTabs();

        AddLocListBoxItem.Visibility = Visibility.Visible;
        AddTubeLocListBoxItem.Visibility = Visibility.Visible;
        AddTimePeriodButton.Visibility = Visibility.Visible;
        TubeLocationListBox.Items.OfType<TubeLocationModule>().ToList().ForEach(x => x.SetEditable());
      }
      #endregion

      #region Change to Creating Mode
      else if (changeTo == DetailsTabState.Creating)
      {
        m_CurrentState.m_DetailsTabState = DetailsTabState.Creating;
        m_CurrentState.m_CreatingWithImportedDetails = false;

        foreach (TimePeriodModule tp in TimePeriodList.Items)
        {
          tp.Visibility = Visibility.Visible;
          tp.StartDatePicker.Visibility = Visibility.Visible;
        }

        Edit_Cancel.Content = "Cancel";

        BanksList.IsEnabled = true;
        ProjectOrderNumPanel.IsEnabled = true;
        ProjectNamePanel.IsEnabled = true;
        TimePeriodList.IsEnabled = true;
        LocationListBox.IsEnabled = true;
        Up.Visibility = Visibility.Visible;
        Down.Visibility = Visibility.Visible;
        manageLocationsSeparator1.Visibility = Visibility.Visible;
        manageLocationsSeparator2.Visibility = Visibility.Visible;
        manageProjects.Visibility = Visibility.Visible;
        NextScreen_Customize.Visibility = Visibility.Visible;
        NextScreen_Customize.Content = "Create Project";
        Edit_Cancel.IsEnabled = false;
        projectDetailButtons.IsEnabled = true;
        detailsTabApplyBankPresetButton.Visibility = Visibility.Visible;
        detailsTabImportDetailsButton.Visibility = Visibility.Visible;
        detailsTabImportDetailsButton.Content = "Web Import";
        RegenerateSideCodes.Visibility = Visibility.Visible;
        columnSwapArea.Visibility = Visibility.Visible;
        tccArea.Visibility = Visibility.Visible;
        ManageTubeLocationsGroupBox.IsEnabled = true;
        TubeLocationListBox.IsEnabled = true;

        DisableAllTabs();

        ClearProject();

        AddLocListBoxItem.Visibility = Visibility.Visible;
        AddTubeLocListBoxItem.Visibility = Visibility.Visible;
        AddTimePeriodButton.Visibility = Visibility.Visible;
        TubeLocationListBox.Items.OfType<TubeLocationModule>().ToList().ForEach(x => x.SetEditable());

        SeparateTubeOrderNumCheckBox.IsChecked = false;
        TMCTab.IsSelected = true;
      }
      #endregion

      foreach (LocationModule loc in (LocationListBox.Items).OfType<LocationModule>())
      {
        loc.UpdateStateVisibility(changeTo);
      }
      UpdateMainMenuEnabledItems();
    }

    private void UpdateMainMenuEnabledItems()
    {
      bool noRestrictions = false;

      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        noRestrictions = true;
      }
      foreach (MenuItem menuBarItem in mainMenu.Items.OfType<MenuItem>())
      {
        foreach (MenuItem item in menuBarItem.Items.OfType<MenuItem>())
        {
          if ((string)item.Header == "_New Project" || (string)item.Header == "_Save Project" || (string)item.Header == "S_ave As"
          || (string)item.Header == "_Import Data Files" || (string)item.Header == "Import _Test Counts"
          || (string)item.Name == "export" || (string)item.Header == "_Tubes" || (string)item.Header == "_TMCs" || (string)item.Header == "_Print Notes")
          {
            item.IsEnabled = noRestrictions;
          }
          if ((string)item.Header == "_New Project" && m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
          {
            item.IsEnabled = true;
          }
        }
      }
    }

    private void ClearProject()
    {
      m_Project = null;
      UpdateWindowTitle(this);
      OrderNumTextBox.Text = "";
      ProjectNameTextBox.Text = "";
      TubeOrderNumTextBox.Text = "";
      TimePeriodList.Items.Clear();

      GenerateTimePeriodList();

      for (int i = LocationListBox.Items.OfType<LocationModule>().Count() - 1; i >= 0; i--)
      {
        LocationListBox.Items.RemoveAt(i);
      }

      for (int i = TubeLocationListBox.Items.OfType<TubeLocationModule>().Count() - 1; i >= 0; i--)
      {
        TubeLocationListBox.Items.RemoveAt(i);
      }

    }

    #region Project Creation from GUI

    //Creates project from scratch
    public void CreateProjectFromScratch()
    {
      #region Create Time Periods

      List<string> timePeriods = new List<string>();
      List<string> timePeriodLabels = new List<string>();
      List<int> timePeriodIDs = new List<int>();

      foreach (TimePeriodModule tp in TimePeriodList.Items.OfType<TimePeriodModule>())
      {
        if (tp.ActiveCheckBox.IsChecked == true)
        {
          timePeriods.Add(tp.GetTimePeriod());
          timePeriodLabels.Add(tp.TimePeriodLabel.Text);
        }
        else
        {
          timePeriods.Add("NOT USED");
          timePeriodLabels.Add(null);
        }
        timePeriodIDs.Add(tp.m_ID);
      }

      #endregion

      #region Create Banks

      //Populate list of banks
      List<string> banks = new List<string>();
      foreach (ComboBox cb in GetVehicleBankComboBoxes())
      {
        banks.Add(cb.Text);
      }

      //List of ped columns data types
      List<PedColumnDataType> RTORsUTurns = GetPedColumnDataListFromGUI();

      #endregion

      #region Create Project

      //sets project's Merlin version
      string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

      ProjectSource source = ProjectSource.Scratch; //(m_CurrentState.m_CreatingWithImportedDetails) ? ProjectSource.Web : ProjectSource.Scratch;

      string tubeOrder = (bool)SeparateTubeOrderNumCheckBox.IsChecked ? TubeOrderNumTextBox.Text : null;

      //construct
      m_Project = new TMCProject(OrderNumTextBox.Text, m_DefaultPreferences, ProjectNameTextBox.Text, timePeriods, timePeriodLabels, timePeriodIDs, banks, RTORsUTurns, version, source, (bool)ncdotColSwapCheckBox.IsChecked, (bool)tccCheckBox.IsChecked, tubeOrder);

      //Updates window title
      UpdateWindowTitle(this);

      #endregion

      #region Create Intersections, Counts, and Tubes

      //construct intersections and counts for m_Project
      CreateIntersectionsAndCounts();

      #endregion

      //Update state
      ChangeDetailsTabState(DetailsTabState.Viewing);
      m_CurrentState.m_ProjectState = ProjectState.Loaded;
      m_CurrentState.m_DataTabState = DataTabState.Empty;

    }

    //helper for CreateProjectFromScratch
    private List<PedColumnDataType> GetPedColumnDataListFromGUI()
    {
      List<ComboBox> vehBoxes = GetVehicleBankComboBoxes();
      List<ComboBox> pedBoxes = GetPedBankComboBoxes();
      bool isTCC = (bool)tccCheckBox.IsChecked;

      List<PedColumnDataType> pedColData = new List<PedColumnDataType>();
      for (int i = 0; i < pedBoxes.Count; i++)
      {
        if (isTCC)
        {
          // In a TCC, we set ped columns by what we see in the vehicle type for each bank, not by what is on the GUI ped ComboBoxes.
          // Set to ped if vehicle type is FHWAPedsBikes, NA if vehicle type is NOT USED, otherwise U-Turn corresponding to the 
          // vehicle type of that bank.
          if ((vehBoxes[i].SelectedItem.ToString()) == (BankVehicleTypes.FHWAPedsBikes.ToString()))
          {
            pedColData.Add(PedColumnDataType.Pedestrian);
          }
          else
          {
            if (vehBoxes[i].SelectedItem.ToString() == "NOT USED")
            {
              pedColData.Add(PedColumnDataType.NA);
            }
            else
            {
              pedColData.Add(PedColumnDataType.UTurn);
            }
          }
        }
        else
        {
          pedColData.Add((PedColumnDataType)Enum.Parse(typeof(PedColumnDataType), pedBoxes[i].SelectedItem.ToString()));
        }
      }
      return pedColData;
    }

    //Helper function for CreateProjectFromScratch() and ApplyEditsToProject()
    public void CreateIntersectionsAndCounts()
    {
      foreach (LocationModule uiLoc in LocationListBox.Items.OfType<LocationModule>())
      {
        //creates the intersection
        Intersection thisIntersection = new Intersection(
          uiLoc.NBSB.Text, uiLoc.diagram.GetLegFlow(StandardIntersectionApproaches.SB),
          uiLoc.NBSB.Text, uiLoc.diagram.GetLegFlow(StandardIntersectionApproaches.NB),
          uiLoc.EBWB.Text, uiLoc.diagram.GetLegFlow(StandardIntersectionApproaches.WB),
          uiLoc.EBWB.Text, uiLoc.diagram.GetLegFlow(StandardIntersectionApproaches.EB),
          m_Project, false, uiLoc.m_ID, uiLoc.m_Latitude, uiLoc.m_Longitude);

        if (uiLoc.m_CustomizedMovements != null)
        {
          //UI recorded custom movements for this intersection, assign to intersection
          thisIntersection.SetIntersectionMovements(uiLoc.m_CustomizedMovements);
        }

        List<Count> countsInThisIntersection = new List<Count>();

        //Adds counts for this intersection for each time period that is checked (meaning the location should have a count for the time period) and is visible (meaning the time period exists for the project)
        foreach (TimePeriodUI tp in uiLoc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().Where(x => (bool)x.isActiveCheckBox.IsChecked == true && x.Visibility == Visibility.Visible))
        {
          Count countToAdd = new Count(tp.OrderNumForSiteCode.Text + tp.SiteCode.Text, ((TimePeriodModule)TimePeriodList.Items.GetItemAt(tp.timePeriodIndex)).GetTimePeriod(), tp.timePeriodIndex, thisIntersection, (DateTime)tp.CountDate.SelectedDate);

          countsInThisIntersection.Add(countToAdd);
        }
        //assigns list of counts we just made as this intersection's m_Counts
        thisIntersection.m_Counts = countsInThisIntersection;

        //now adds this intersection to the project's intersections
        m_Project.m_Intersections.Add(thisIntersection);
      }

      //create tubes and add to project
      foreach (TubeLocationModule uiLoc in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        TubeSite ts = new TubeSite(m_Project, uiLoc.m_ID, uiLoc.m_Latitude, uiLoc.m_Longitude, uiLoc.locationTextBox.Text, uiLoc.tubeDiagram.CurrentLayout);
        foreach (TubeTimePeriodUI tubeTP in uiLoc.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>())
        {
          ts.m_TubeCounts.Add(new TubeCount(tubeTP.OrderNumForSiteCode.Text + tubeTP.SiteCodeTextBox.Text, tubeTP.SurveyType, (DateTime)tubeTP.StartTime, (int)tubeTP.GetSurveyLengthFromDurationTextBoxes(true).TotalHours, ts));
        }
        m_Project.m_Tubes.Add(ts);
      }

    }

    #endregion

    private void bankImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      System.Windows.Controls.Image clickedImage = (System.Windows.Controls.Image)sender;

      if (((BitmapImage)clickedImage.Source).UriSource == new Uri("Resources/None.png", UriKind.Relative))
      {
        clickedImage.Source = new BitmapImage(new Uri("Resources/RTOR.png", UriKind.Relative));
      }
      else if (((BitmapImage)clickedImage.Source).UriSource == new Uri("Resources/RTOR.png", UriKind.Relative))
      {
        clickedImage.Source = new BitmapImage(new Uri("Resources/UTurn.png", UriKind.Relative));
      }
      else
      {
        //current image is UTurn
        clickedImage.Source = new BitmapImage(new Uri("Resources/None.png", UriKind.Relative));
      }
    }

    private void AddTimePeriodButton_MouseEnter(object sender, MouseEventArgs e)
    {
      addPlusTP.FontWeight = FontWeights.Bold;
      AddTimePeriodText.FontWeight = FontWeights.Bold;
      this.Cursor = Cursors.Cross;
    }

    private void AddTimePeriodButton_MouseLeave(object sender, MouseEventArgs e)
    {
      addPlusTP.FontWeight = FontWeights.Normal;
      AddTimePeriodText.FontWeight = FontWeights.Normal;
      this.Cursor = Cursors.Arrow;
    }

    private void AddTimePeriodButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      AddTimePeriodToDetailsTab(NextTimePeriodID());
    }

    private TimePeriodModule AddTimePeriodToDetailsTab(int ID)
    {
      TimePeriodModule timePeriod = new TimePeriodModule(this, ID);
      TimePeriodList.Items.Add(timePeriod);
      timePeriod.Name = "C" + (TimePeriodList.Items.Count - 2).ToString();
      //timePeriod.Width = 463;
      timePeriod.Margin = new Thickness(0, 0, 0, 2);
      //time period added, update the locations' time periods
      updateAllLocationsTimePeriods(TimePeriodList.Items.Count - 1, new List<LocUpdateFunc>() { AddTimePeriodToLocation, });
      timePeriod.TimePeriodLabel.Text = "C" + (TimePeriodList.Items.Count - 2).ToString();
      timePeriod.ActiveCheckBox.IsChecked = true;

      return timePeriod;
    }

    private int NextTimePeriodID()
    {
      int next = 0;
      foreach (TimePeriodModule tpModule in TimePeriodList.Items.OfType<TimePeriodModule>())
      {
        if (tpModule.m_ID >= next)
        {
          next = tpModule.m_ID + 1;
        }
      }
      return next;
    }

    private int NextLocationID()
    {
      int next = 0;
      foreach (LocationModule locModule in LocationListBox.Items.OfType<LocationModule>())
      {
        if (locModule.m_ID >= next)
        {
          next = locModule.m_ID + 1;
        }
      }
      foreach (TubeLocationModule tm in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        if (tm.m_ID >= next)
        {
          next = tm.m_ID + 1;
        }
      }
      return next;
    }

    private void TimePeriodList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Tab)
      {
        //causes tab to go to next textbox or combobox only, cycling within the time period list
        TraversalRequest tr = new TraversalRequest(FocusNavigationDirection.Next);
        UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;
        do
        {
          if (keyboardFocus != null)
          {
            keyboardFocus.MoveFocus(tr);
          }
          keyboardFocus = Keyboard.FocusedElement as UIElement;
        } while (keyboardFocus.GetType() != typeof(TextBox) && keyboardFocus.GetType() != typeof(ComboBox));
        e.Handled = true;
      }
    }

    private void locationsCondensedRadio_Checked(object sender, RoutedEventArgs e)
    {
      CondenseAllLocations();
    }

    private void locationsExpandedRadio_Checked(object sender, RoutedEventArgs e)
    {
      ExpandAllLocations();
    }

    private void CondenseAllLocations()
    {
      if (LocationListBox == null)
      {
        //This would happen when the app loads and condense radio gets checked before locationListBox loaded
        return;
      }
      foreach (LocationModule locMod in LocationListBox.Items.OfType<LocationModule>())
      {
        locMod.CondenseModule();
      }
    }

    private void ExpandAllLocations()
    {
      if (LocationListBox == null)
      {
        //This would happen when the app loads and condense radio gets checked before locationListBox loaded
        return;
      }
      foreach (LocationModule locMod in LocationListBox.Items.OfType<LocationModule>())
      {
        locMod.ExpandModule();
      }
    }

    private void RegenerateSideCodes_Click(object sender, RoutedEventArgs e)
    {
      if(m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        return;
      }
      MessageBoxResult result = MessageBox.Show("Are you sure you want to overwrite all TMC and tube count site codes with Merlin's best guesses?", "Overwrite all site codes?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
      if (result != MessageBoxResult.Yes)
      {
        return;
      }
      CalculateSiteCodes();
    }

    private void CalculateSiteCodes()
    {
      int currentSiteCode = 1;

      //Iterate through every location
      foreach (LocationModule loc in LocationListBox.Items.OfType<LocationModule>())
      {
        //In each location, iterate through all time periods
        foreach (TimePeriodUI tp in loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>())
        {
          if ((bool)tp.isActiveCheckBox.IsChecked && tp.Visibility == Visibility.Visible)
          {
            tp.SiteCode.Text = (currentSiteCode++).ToString("D2");
          }
        }
      }
      //Next iterate through the tubes
      foreach(TubeLocationModule tubeSite in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        foreach(TubeTimePeriodUI tubeCount in tubeSite.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>())
        {
          tubeCount.SiteCodeTextBox.Text = (currentSiteCode++).ToString("D2");
        }
      }
    }

    private void ncdotHelp_MouseDown(object sender, MouseButtonEventArgs e)
    {
      Window win = new Window();
      win.ResizeMode = System.Windows.ResizeMode.NoResize;
      win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
      win.Title = "NCDOT Column Swapping Help";
      win.Content = new TextBlock()
      {
        Text = "Enabling column swapping will cause Merlin to swap the data in columns 4, 8, 12, and 16 between the pedestrian bank marked as Pedestrian, and the one marked as U-Turns as the data is imported."
      };
      win.ShowDialog();
    }

    private void ncdotColSwapCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      tccCheckBox.IsChecked = false;
    }

    private void tccCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      ncdotColSwapCheckBox.IsChecked = false;
      SetBankComboBoxesToTCCMode();
    }

    private void tccCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      SetBankComboBoxesToNormalMode();
    }

    private void SetBankComboBoxesToNormalMode()
    {
      foreach (ComboBox cb in FindVisualChildren<ComboBox>(BanksList))
      {
        if (cb.Name.Contains("Ped"))
        {
          cb.Visibility = Visibility.Visible;
        }
        else
        {
          //regular bank combobox
          cb.Width = 200.0;
          PopulateVehBankDropDownList(cb);
        }
      }
    }

    private void SetBankComboBoxesToTCCMode()
    {
      foreach (ComboBox cb in FindVisualChildren<ComboBox>(BanksList))
      {
        if (cb.Name.Contains("Ped"))
        {
          cb.Visibility = Visibility.Hidden;
        }
        else
        {
          //regular bank combobox
          cb.Width = 350.0;
          PopulateVehBankDropDownList(cb);
        }
      }
    }

    /// <summary>
    /// Gets all visual children of a control that are a given type no matter how deep down the hierarchy they are
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="depObj">Parent control</param>
    /// <returns></returns>
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
      if (depObj != null)
      {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
          DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
          if (child != null && child is T)
          {
            yield return (T)child;
          }

          foreach (T childOfChild in FindVisualChildren<T>(child))
          {
            yield return childOfChild;
          }
        }
      }
    }

    public List<ComboBox> GetVehicleBankComboBoxes()
    {
      List<ComboBox> bankComboBoxes = new List<ComboBox>();
      foreach (StackPanel sp in BanksList.Items)
      {
        foreach (ComboBox cb in sp.Children.OfType<ComboBox>())
        {
          if (!cb.Name.Contains("Ped"))
          {
            bankComboBoxes.Add(cb);
          }
        }
      }
      //foreach (ComboBox cb in FindVisualChildren<ComboBox>(BanksList))
      //{
      //  if (!cb.Name.Contains("Ped"))
      //  {
      //    bankComboBoxes.Add(cb);
      //  }
      //}
      return bankComboBoxes;
    }

    public List<ComboBox> GetPedBankComboBoxes()
    {
      List<ComboBox> bankComboBoxes = new List<ComboBox>();
      foreach (StackPanel sp in BanksList.Items)
      {
        foreach (ComboBox cb in sp.Children.OfType<ComboBox>())
        {
          if (cb.Name.Contains("Ped"))
          {
            bankComboBoxes.Add(cb);
          }
        }
      }
      //foreach (ComboBox cb in BanksList.Items.OfType<ComboBox>())
      //{
      //  if (cb.Name.Contains("Ped"))
      //  {
      //    bankComboBoxes.Add(cb);
      //  }
      //}
      return bankComboBoxes;
    }

    #endregion

    #region Tubes

    private void AddTubeLocListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      AddTubeLocationToList(NextLocationID(), "", "");
    }

    private TubeLocationModule AddTubeLocationToList(int ID, string lat, string lon)
    {
      TubeLocationModule newLocation = new TubeLocationModule(this, TubeLocationListBox, ID, lat, lon);
      TubeLocationListBox.Items.Insert(TubeLocationListBox.Items.OfType<TubeLocationModule>().Count(), newLocation);

      return newLocation;
    }

    private void ApplyOrderNumToTubeSurveysInGUI(string orderNum)
    {
      foreach (TubeLocationModule tm in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        foreach (TubeTimePeriodUI tp in tm.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>())
        {
          tp.OrderNumForSiteCode.Text = orderNum;
        }
      }
    }

    public string GetOrderNumForTubes()
    {
      if ((bool)SeparateTubeOrderNumCheckBox.IsChecked)
      {
        return TubeOrderNumTextBox.Text;
      }
      else
      {
        return OrderNumTextBox.Text;
      }
    }


    #region Tube Data Import

    private TubeImportOptionsDialog GetTubeImportParameters()
    {
      TubeImportOptionsDialog fd = new TubeImportOptionsDialog(m_Project);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }

    private TubeFileAssociationDialog PerformTubeFileSearch(string dir, int days)
    {
      TubeFileAssociationDialog fd = new TubeFileAssociationDialog(m_Project, dir, days);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }
    private TubeDataImporter ProcessImportedTubeFiles(List<ImportDataFile> files)
    {
      TubeDataImporter fd = new TubeDataImporter(m_Project, files);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }




    #endregion  // Tube Data Import

    #endregion

    #region Edit Project

    //edit project, return value is if user wants to save project upon making edits
    public bool ApplyEditsToProject()
    {
      try
      {
        #region Order Number & Project Name

        //Update internal project from the project details section
        m_Project.m_OrderNumber = OrderNumTextBox.Text;
        m_Project.m_ProjectName = ProjectNameTextBox.Text;

        #endregion

        #region Banks

        List<string> banks = new List<string>();
        foreach (ComboBox cb in GetVehicleBankComboBoxes())
        {
          banks.Add(cb.Text);
        }

        m_Project.UpdateBanks(banks, GetPedColumnDataListFromGUI());

        m_Project.m_NCDOTColSwappingEnabled = (bool)ncdotColSwapCheckBox.IsChecked;
        m_Project.m_TCCDataFileRules = (bool)tccCheckBox.IsChecked;

        #endregion

        #region Time Periods
        //The 4 possible options are added, removed, intervals changed and/or label changed, or nothing
        //  (except for TP modules added to GUI in editing that can only add to the time periods).
        foreach (TimePeriodModule tp in TimePeriodList.Items.OfType<TimePeriodModule>())
        {
          //Covers the case where the current GUI TP was added, not yet tracked in project, add to project.
          if (TimePeriodList.Items.IndexOf(tp) > m_Project.m_TimePeriods.Count - 1)
          {
            if ((bool)tp.ActiveCheckBox.IsChecked)
            {
              //The new GUI time period indicates an active time period
              m_Project.m_TimePeriods.Add(tp.GetTimePeriod());
              m_Project.m_TimePeriodLabels.Add(tp.TimePeriodLabel.Text);
            }
            else
            {
              //The user for some reason added a new GUI time period but didn't mark it as active.
              m_Project.m_TimePeriods.Add("NOT USED");
              m_Project.m_TimePeriodLabels.Add(null);
            }
            m_Project.m_TimePeriodIDs.Add(tp.m_ID);
          }
          //Updates the existing internal time period entries
          else
          {
            if ((bool)tp.ActiveCheckBox.IsChecked)
            {
              if (m_Project.m_TimePeriods[TimePeriodList.Items.IndexOf(tp)] != "NOT USED" && m_Project.m_TimePeriods[TimePeriodList.Items.IndexOf(tp)] != tp.GetTimePeriod())
              {
                //This means an existing time period's time was changed, update all affected counts
                UpdateAllCountsAffectedByTimePeriodTimeChanged(TimePeriodList.Items.IndexOf(tp));
              }
              m_Project.m_TimePeriods[TimePeriodList.Items.IndexOf(tp)] = tp.GetTimePeriod();
              m_Project.m_TimePeriodLabels[TimePeriodList.Items.IndexOf(tp)] = tp.TimePeriodLabel.Text;
            }
            else
            {
              m_Project.m_TimePeriods[TimePeriodList.Items.IndexOf(tp)] = "NOT USED";
              m_Project.m_TimePeriodLabels[TimePeriodList.Items.IndexOf(tp)] = null;
            }
            m_Project.m_TimePeriodIDs[TimePeriodList.Items.IndexOf(tp)] = tp.m_ID;
          }
        }

        #endregion

        #region Intersections & Counts

        //Deletes existing intersections that were "deleted" in the GUI (visibility collapsed)
        for (int i = m_Project.m_Intersections.Count - 1; i >= 0; i--)
        {
          if (((LocationModule)LocationListBox.Items.GetItemAt(i)).Visibility != Visibility.Visible)
          {
            Intersection intersectionToDelete = m_Project.m_Intersections.ElementAt(i);
            BalancingModule bmToDelete = GetBalancingModule(intersectionToDelete);
            //remove corresponding BalancingModule from grid, if it exists
            if (bmToDelete != null)
            {
              RemoveBalancingModuleFromGrid(bmToDelete, true);
            }

            //remove from internal project
            m_Project.m_Intersections.RemoveAt(i);
          }
        }

        //Remove "deleted" (hidden) LocationModules in GUI so that their indices match internal locations
        for (int i = LocationListBox.Items.Count - 2; i >= 0; i--)
        {
          if (((LocationModule)LocationListBox.Items.GetItemAt(i)).Visibility != Visibility.Visible)
          {
            LocationListBox.Items.RemoveAt(i);
          }
        }

        //Adds & removes counts within existing intersections based on changes made in GUI, modifies existing intersection info
        for (int i = 0; i < m_Project.m_Intersections.Count; i++)
        {
          //applies street names, approach types, and regenerates possible movements in case those were changed by user
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[0].ApproachName = ((LocationModule)LocationListBox.Items.GetItemAt(i)).NBSB.Text;
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[1].ApproachName = ((LocationModule)LocationListBox.Items.GetItemAt(i)).NBSB.Text;
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[2].ApproachName = ((LocationModule)LocationListBox.Items.GetItemAt(i)).EBWB.Text;
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[3].ApproachName = ((LocationModule)LocationListBox.Items.GetItemAt(i)).EBWB.Text;
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[0].TrafficFlowType = ((LocationModule)LocationListBox.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.NB);
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[1].TrafficFlowType = ((LocationModule)LocationListBox.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.SB);
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[2].TrafficFlowType = ((LocationModule)LocationListBox.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.EB);
          m_Project.m_Intersections[i].m_ApproachesInThisIntersection[3].TrafficFlowType = ((LocationModule)LocationListBox.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.WB);
          if (((LocationModule)LocationListBox.Items.GetItemAt(i)).m_CustomizedMovements != null)
          {
            //location had custom movements recorded in location module, assign to internal
            m_Project.m_Intersections[i].SetIntersectionMovements(((LocationModule)LocationListBox.Items.GetItemAt(i)).m_CustomizedMovements);
          }
          else
          {
            //location did not have custom movements recorded in location module, generate standard movements based on approach configurations
            m_Project.m_Intersections[i].GenerateIntersectionMovements();
          }
          m_Project.m_Intersections[i].Id = ((LocationModule)LocationListBox.Items.GetItemAt(i)).m_ID;
          m_Project.m_Intersections[i].Latitude = ((LocationModule)LocationListBox.Items.GetItemAt(i)).m_Latitude;
          m_Project.m_Intersections[i].Longitude = ((LocationModule)LocationListBox.Items.GetItemAt(i)).m_Longitude;

          List<bool> TPsContainedInCurrentInternalIntersection = new List<bool>();
          for (int k = 0; k < TimePeriodList.Items.Count; k++)
          {
            TPsContainedInCurrentInternalIntersection.Add(false);
          }
          //Iterate through counts in the current intersection
          for (int j = m_Project.m_Intersections[i].m_Counts.Count - 1; j >= 0; j--)
          {
            //Deletes count if found in internal project but not active in GUI, else count remains and date, site code, and intervals range (if different) are updated
            Count currentInternalCount = m_Project.m_Intersections[i].m_Counts[j];
            TimePeriodUI currentGUITimePeriod = ((LocationModule)LocationListBox.Items.GetItemAt(i)).locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == currentInternalCount.m_TimePeriodIndex);

            //internal count exists but either unselected in GUI or hidden because that time period was unselected at the project level, delete
            if (!(bool)currentGUITimePeriod.isActiveCheckBox.IsChecked || currentGUITimePeriod.Visibility != Visibility.Visible)
            {
              m_Project.m_Intersections[i].m_Counts.RemoveAt(j);
              TPsContainedInCurrentInternalIntersection[currentInternalCount.m_TimePeriodIndex] = false;
            }
            //Otherwise count remains, updates date, site code, and time period range (if different)
            else
            {
              currentInternalCount.m_FilmDate = (DateTime)currentGUITimePeriod.CountDate.SelectedDate;
              currentInternalCount.m_Id = currentGUITimePeriod.OrderNumForSiteCode.Text + currentGUITimePeriod.SiteCode.Text;
              string fullTimePeriod = ((TimePeriodModule)TimePeriodList.Items.GetItemAt((currentInternalCount.m_TimePeriodIndex))).GetTimePeriod();
              currentInternalCount.m_StartTime = fullTimePeriod.Split('-')[0];
              currentInternalCount.m_EndTime = fullTimePeriod.Split('-')[1];
              currentInternalCount.m_NumIntervals = currentInternalCount.CalculateNumberOfIntervals();
              //Add_RemoveDataIntervals(currentInternalCount);

              TPsContainedInCurrentInternalIntersection[currentInternalCount.m_TimePeriodIndex] = true;
            }
          }
          //Adds counts that are newly active for existing locations in GUI but don't yet exist in internal project
          int tpIndex;
          foreach (TimePeriodModule tp in TimePeriodList.Items.OfType<TimePeriodModule>().Where(x => (bool)x.ActiveCheckBox.IsChecked))
          {
            tpIndex = TimePeriodList.Items.IndexOf(tp);
            AddInternalCountIfDoesntExist(TPsContainedInCurrentInternalIntersection[tpIndex], tpIndex, i, (((LocationModule)LocationListBox.Items.GetItemAt(i)).locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().First(x => x.timePeriodIndex == tpIndex)));
          }
        }

        #endregion

        #region TubeSites & TubeCounts

        //Deletes existing internal tube sites that were "deleted" in the GUI (visibility collapsed)
        for (int i = m_Project.m_Tubes.Count - 1; i >= 0; i--)
        {
          if (((TubeLocationModule)TubeLocationListBox.Items.GetItemAt(i)).Visibility != Visibility.Visible)
          {
            TubeSite tubeSiteToDelete = m_Project.m_Tubes.ElementAt(i);
            BalancingModule tubeBMToDelete = GetBalancingModule(tubeSiteToDelete);
            //remove corresponding BalancingModule from grid, if it exists
            if (tubeBMToDelete != null)
            {
              RemoveBalancingModuleFromGrid(tubeBMToDelete, true);
            }

            //remove from internal project
            m_Project.m_Tubes.RemoveAt(i);
          }
        }

        //Remove "deleted" (hidden) TubeLocationModules in GUI so that their indices match internal locations
        for (int i = TubeLocationListBox.Items.Count - 2; i >= 0; i--)
        {
          if (((TubeLocationModule)TubeLocationListBox.Items.GetItemAt(i)).Visibility != Visibility.Visible)
          {
            TubeLocationListBox.Items.RemoveAt(i);
          }
        }

        //update existing internal tubes with any user changes
        TubeSite currentTS;
        TubeLocationModule currentTubeModule;
        for (int i = 0; i < m_Project.m_Tubes.Count; i++)
        {
          //Update TubeSites
          currentTS = m_Project.m_Tubes.ElementAt(i);
          currentTubeModule = (TubeLocationModule)TubeLocationListBox.Items.GetItemAt(i);

          currentTS.TubeLayout = currentTubeModule.tubeDiagram.CurrentLayout;
          currentTS.m_Location = currentTubeModule.locationTextBox.Text;
          currentTS.Id = currentTubeModule.m_ID;
          currentTS.Latitude = currentTubeModule.m_Latitude;
          currentTS.Longitude = currentTubeModule.m_Longitude;

          //For each TubeSite, update TubeCounts

          //Deletes existing internal tube surveys that were "deleted" in the GUI (visibility collapsed)
          for (int j = currentTS.m_TubeCounts.Count - 1; j >= 0; j--)
          {
            TubeTimePeriodUI currentTCGUI = (TubeTimePeriodUI)currentTubeModule.surveyTimesWrapPanel.Children[j];
            if (currentTCGUI.Visibility != Visibility.Visible)
            {
              currentTS.m_TubeCounts.RemoveAt(j);
            }
          }
          //Remove "deleted" (collapsed) TubeTimePeriodUIs in GUI so that their indices match internal TubeCounts
          for (int j = currentTubeModule.surveyTimesWrapPanel.Children.Count - 1; j >= 0; j--)
          {
            TubeTimePeriodUI currentTCGUI = (TubeTimePeriodUI)currentTubeModule.surveyTimesWrapPanel.Children[j];
            if (currentTCGUI.Visibility != Visibility.Visible)
            {
              currentTubeModule.surveyTimesWrapPanel.Children.Remove(currentTCGUI);
            }
          }
          //update details of remaining, currently existing internal tube surveys (excluding surveys that were added during project editing)
          for (int j = currentTS.m_TubeCounts.Count - 1; j >= 0; j--)
          {
            TubeTimePeriodUI currentTCGUI = (TubeTimePeriodUI)currentTubeModule.surveyTimesWrapPanel.Children[j];
            TubeCount currentTCInternal = currentTS.m_TubeCounts[j];
            currentTCInternal.StartTime = (DateTime)currentTCGUI.StartTime;
            currentTCInternal.Duration = (int)currentTCGUI.GetSurveyLengthFromDurationTextBoxes(true).TotalHours;
            currentTCInternal.m_Type = currentTCGUI.SurveyType;
            currentTCInternal.m_SiteCode = currentTCGUI.OrderNumForSiteCode.Text + currentTCGUI.SiteCodeTextBox.Text;
          }
          //Add newly added surveys to the TubeSite
          for (int j = currentTS.m_TubeCounts.Count; j < currentTubeModule.surveyTimesWrapPanel.Children.Count; j++)
          {
            TubeTimePeriodUI currentTCGUI = (TubeTimePeriodUI)currentTubeModule.surveyTimesWrapPanel.Children[j];
            currentTS.m_TubeCounts.Add(new TubeCount(currentTCGUI.OrderNumForSiteCode.Text + currentTCGUI.SiteCodeTextBox.Text, currentTCGUI.SurveyType, (DateTime)currentTCGUI.StartTime, (int)currentTCGUI.GetSurveyLengthFromDurationTextBoxes(true).TotalHours, currentTS));
          }

        }

        //Update tube order number
        m_Project.m_TubeOrderNumber = (bool)SeparateTubeOrderNumCheckBox.IsChecked ? TubeOrderNumTextBox.Text : null;

        #endregion

        //The remaining locations and their child counts in locationslistbox and tubes in tubelocationlistbox are new, add to internal project
        CreateNewlyAddedIntersectionsAndTheirCounts();

        ChangeDetailsTabState(DetailsTabState.Viewing);
        m_CurrentState.m_DataTabState = DataTabState.Empty;
        m_CurrentState.m_DataTabVisited = false;

        MessageBoxResult result = MessageBox.Show("Project was successfully edited!\n\nDo you want to save your changes right now?", "Save Changes Now?", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("There was a problem editing the project:\n\n" + ex.Message, "Error", MessageBoxButton.OK);
      }
      return false;
    }

    //helper for ApplyEditsToProject()
    public void AddInternalCountIfDoesntExist(bool doesCountAlreadyExist, int tpIndex, int locIndex, TimePeriodUI GUItp)
    {
      if ((bool)GUItp.isActiveCheckBox.IsChecked && GUItp.Visibility == Visibility.Visible && !doesCountAlreadyExist)
      {
        Count countToAdd = new Count(GUItp.OrderNumForSiteCode.Text + GUItp.SiteCode.Text, ((TimePeriodModule)TimePeriodList.Items.GetItemAt(tpIndex)).GetTimePeriod(), tpIndex, m_Project.m_Intersections[locIndex], (DateTime)GUItp.CountDate.SelectedDate);
        m_Project.m_Intersections[locIndex].m_Counts.Add(countToAdd);
        if (m_Project.m_Intersections[locIndex].m_Counts.Count(x => x.m_Id == countToAdd.m_Id) > 1)
        {
          ;
        }
      }
    }

    //helper for ApplyEditsToProject()
    public void CreateNewlyAddedIntersectionsAndTheirCounts()
    {

      #region TMC

      LocationModule loc;
      //iterate over the newly added locations to add to internal project
      for (int i = m_Project.m_Intersections.Count; i < LocationListBox.Items.Count - 1; i++)
      {
        loc = (LocationModule)LocationListBox.Items.GetItemAt(i);

        //creates the intersection
        Intersection thisIntersection = new Intersection(
          loc.NBSB.Text, loc.diagram.GetLegFlow(StandardIntersectionApproaches.SB),
          loc.NBSB.Text, loc.diagram.GetLegFlow(StandardIntersectionApproaches.NB),
          loc.EBWB.Text, loc.diagram.GetLegFlow(StandardIntersectionApproaches.WB),
          loc.EBWB.Text, loc.diagram.GetLegFlow(StandardIntersectionApproaches.EB),
          m_Project, false, loc.m_ID, loc.m_Latitude, loc.m_Longitude);

        if (loc.m_CustomizedMovements != null)
        {
          //UI recorded custom movements for this intersection, assign to intersection
          thisIntersection.SetIntersectionMovements(loc.m_CustomizedMovements);
        }

        List<Count> countsInThisIntersection = new List<Count>();

        //Adds count for each checked time period of the intersection
        foreach (TimePeriodUI tp in loc.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>().Where(x => (bool)x.isActiveCheckBox.IsChecked && x.Visibility == Visibility.Visible))
        {
          countsInThisIntersection.Add(new Count(tp.OrderNumForSiteCode.Text + tp.SiteCode.Text, ((TimePeriodModule)TimePeriodList.Items.GetItemAt(tp.timePeriodIndex)).GetTimePeriod(), tp.timePeriodIndex, thisIntersection, (DateTime)tp.CountDate.SelectedDate));
        }

        //assigns list of counts we just made as this intersection's m_Counts
        thisIntersection.m_Counts = countsInThisIntersection;

        //adds this intersection to loaded project intersections
        m_Project.m_Intersections.Add(thisIntersection);
      }

      #endregion

      #region Tube

      TubeLocationModule tubeLoc;
      //iterate over the newly added tubes to add to internal project
      for (int i = m_Project.m_Tubes.Count; i < TubeLocationListBox.Items.Count - 1; i++)
      {
        tubeLoc = (TubeLocationModule)TubeLocationListBox.Items.GetItemAt(i);
        TubeSite newTubeSite = new TubeSite(m_Project, tubeLoc.m_ID, tubeLoc.m_Latitude, tubeLoc.m_Longitude, tubeLoc.locationTextBox.Text, tubeLoc.tubeDiagram.CurrentLayout);
        foreach (TubeTimePeriodUI tp in tubeLoc.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>())
        {
          TubeCount newTubeCount = new TubeCount(tp.OrderNumForSiteCode.Text + tp.SiteCodeTextBox.Text, tp.SurveyType, (DateTime)tp.StartTime, (int)tp.GetSurveyLengthFromDurationTextBoxes(true).TotalHours, newTubeSite);
          newTubeSite.m_TubeCounts.Add(newTubeCount);
        }
        m_Project.m_Tubes.Add(newTubeSite);
      }

      #endregion

    }

    //helper for ApplyEditsToProject()
    public void UpdateAllCountsAffectedByTimePeriodTimeChanged(int tpIndex)
    {
      //Relies on the time period module in details tab to have new time but the internal project time period's times have not yet been updated.

      int removeFromStart = 0;
      int removeFromEnd = 0;
      int addToStart = 0;
      int addToEnd = 0;

      DateTime oldStart, oldEnd, newStart, newEnd;
      DateTime.TryParse(m_Project.m_TimePeriods[tpIndex].Split('-')[0], out oldStart);
      DateTime.TryParse(m_Project.m_TimePeriods[tpIndex].Split('-')[1], out oldEnd);
      newStart = oldStart.Date + ((TimePeriodModule)TimePeriodList.Items.GetItemAt(tpIndex)).GetStartTime().TimeOfDay;
      newEnd = oldEnd.Date + ((TimePeriodModule)TimePeriodList.Items.GetItemAt(tpIndex)).GetEndTime().TimeOfDay;

      //Since we only parsed time of day, if the new or old start time is greater than the end time, then end time is on the next day
      //  which the day is obviously bogus but having it correctly be the next day will help our calculations.
      if (oldStart > oldEnd)
      {
        oldEnd = oldEnd.AddDays(1);
      }
      if (newStart > newEnd)
      {
        newEnd = newEnd.AddDays(1);
      }

      //Determine where to add/remove intervals.
      if ((newStart - oldStart).TotalMinutes < 0.0)
      {
        //new start was made earlier
        addToStart = (int)(oldStart - newStart).TotalMinutes / 5;
      }
      else
      {
        //new start was made later
        removeFromStart = (int)(newStart - oldStart).TotalMinutes / 5;
      }
      if ((newEnd - oldEnd).TotalMinutes < 0.0)
      {
        //new end was made earlier
        removeFromEnd = (int)(oldEnd - newEnd).TotalMinutes / 5;
      }
      else
      {
        //new end was made later
        addToEnd = (int)(newEnd - oldEnd).TotalMinutes / 5;
      }


      foreach (Count count in m_Project.GetCountsInTimePeriod(tpIndex))
      {
        if (addToStart > 0 || addToEnd > 0)
        {
          count.AddIntervals(addToStart, addToEnd);
        }
        if (removeFromStart > 0 || removeFromEnd > 0)
        {
          count.RemoveIntervals(removeFromStart, removeFromEnd);
        }
      }
    }

    //loads controls with internal project values
    public void LoadProjDetailsInGUI()
    {
      if (m_Project != null)
      {
        UpdateWindowTitle(this);
        OrderNumTextBox.Text = m_Project.m_OrderNumber;
        ProjectNameTextBox.Text = m_Project.m_ProjectName;
        ncdotColSwapCheckBox.IsChecked = m_Project.m_NCDOTColSwappingEnabled;
        tccCheckBox.IsChecked = m_Project.m_TCCDataFileRules;

        //Load banks
        List<ComboBox> bankComboBoxes = GetVehicleBankComboBoxes();
        for (int i = 0; i < bankComboBoxes.Count; i++)
        {
          if (m_Project.m_Banks.Count > i)
          {
            bankComboBoxes[i].SelectedIndex = bankComboBoxes[i].Items.IndexOf(m_Project.m_Banks[i]);
            if (bankComboBoxes[i].SelectedValue == null)
            {
              bankComboBoxes[i].SelectedValue = "NOT USED";
            }
          }
          else
          {
            bankComboBoxes[i].SelectedValue = "NOT USED";
          }
        }

        List<ComboBox> pedBankComboBoxes = GetPedBankComboBoxes();
        for (int i = 0; i < pedBankComboBoxes.Count; i++)
        {
          if (m_Project.m_PedBanks.Count > i)
          {
            pedBankComboBoxes[i].SelectedItem = m_Project.m_PedBanks[i].ToString();
          }
          else
          {
            pedBankComboBoxes[i].SelectedValue = "NA";
          }
        }
        UpdateVisibleBankComboBoxes();

        //Since this is going to refresh GUI within same project, time periods will not change so no need to clear and re-add TP modules.
        foreach (TimePeriodModule tpMod in TimePeriodList.Items.OfType<TimePeriodModule>())
        {
          UpdateTimePeriodModule(tpMod, TimePeriodList.Items.IndexOf(tpMod));
        }

        var addLocButton = LocationListBox.Items.GetItemAt(LocationListBox.Items.Count - 1);
        LocationListBox.Items.Clear();
        foreach (Intersection intersection in m_Project.m_Intersections)
        {
          //add location module for each location in project
          LocationModule locMod = AddTMCLocationToList(intersection.Id, intersection.Latitude, intersection.Longitude);
          locMod.NBSB.Text = intersection.GetSBNBApproach();
          locMod.EBWB.Text = intersection.GetWBEBApproach();

          //Determine if this location has non-standard movements for the approach types. For getting the UI right, a LocationModule's list of customized movements is null unless it has non-standard movements.
          List<string> standardMovements = Intersection.CalculateStandardMovements(intersection.m_ApproachesInThisIntersection);
          List<string> a = intersection.m_MovementsInThisIntersection.Except(standardMovements).ToList<string>();
          List<string> b = standardMovements.Except(intersection.m_MovementsInThisIntersection).ToList<string>();
          if (!(a.Count == 0 && b.Count == 0))
          {
            locMod.m_CustomizedMovements = intersection.m_MovementsInThisIntersection;
          }

          //Set diagram for each intersection from internal project
          IntersectionApproach sbApproach = m_Project.m_Intersections[LocationListBox.Items.IndexOf(locMod)].m_ApproachesInThisIntersection[1];
          IntersectionApproach wbApproach = m_Project.m_Intersections[LocationListBox.Items.IndexOf(locMod)].m_ApproachesInThisIntersection[3];
          IntersectionApproach nbApproach = m_Project.m_Intersections[LocationListBox.Items.IndexOf(locMod)].m_ApproachesInThisIntersection[0];
          IntersectionApproach ebApproach = m_Project.m_Intersections[LocationListBox.Items.IndexOf(locMod)].m_ApproachesInThisIntersection[2];
          locMod.diagram.SetLegFlows(sbApproach.TrafficFlowType, wbApproach.TrafficFlowType, nbApproach.TrafficFlowType, ebApproach.TrafficFlowType);

          //add time periods to location module
          foreach (TimePeriodUI tp in locMod.locationTimePeriodsPanel.Children.OfType<TimePeriodUI>())
          {
            Count internalCount = intersection.m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == tp.timePeriodIndex);
            if (internalCount == null)
            {
              //this means there is not a count for this time peirod
              tp.isActiveCheckBox.IsChecked = false;
              tp.CountDate.SelectedDate = null;
            }
            else
            {
              //this means there is a count for this time period
              tp.isActiveCheckBox.IsChecked = true;
              tp.CountDate.SelectedDate = internalCount.m_FilmDate;
              tp.SiteCode.Text = internalCount.GetId().Substring(6);
            }
          }
        }
        //add the add location button back to the end of the locations list box now that we are done adding locations
        LocationListBox.Items.Add(addLocButton);
        SetIntersectionListBackgrounds(this);
        //updateLocsSiteCodeEnabledState();

        //load the tubes
        SeparateTubeOrderNumCheckBox.IsChecked = m_Project.m_TubeOrderNumber != null;
        TubeOrderNumTextBox.Text = m_Project.m_TubeOrderNumber != null ? m_Project.m_TubeOrderNumber : "";
        var addTubeLocButton = TubeLocationListBox.Items.GetItemAt(TubeLocationListBox.Items.Count - 1);
        TubeLocationListBox.Items.Clear();
        foreach (TubeSite tube in m_Project.m_Tubes)
        {
          TubeLocationListBox.Items.Add(new TubeLocationModule(this, TubeLocationListBox, tube));
        }
        TubeLocationListBox.Items.Add(addTubeLocButton);
      }
    }

    //helper function for LoadProjDetailsInGUI
    private void UpdateLocationModule(int projTPindex, CheckBox cb, TimePeriodName tpName, Intersection loc, LocationModule locModule)
    {
      if (m_Project.m_TimePeriods[projTPindex] == "NOT USED")
      {
        cb.Visibility = Visibility.Hidden;
        locModule.m_SiteCodes[projTPindex] = "";
      }
      else
      {
        foreach (Count count in loc.m_Counts)
        {
          if (count.m_TimePeriod == tpName)
          {
            cb.IsChecked = true;
            locModule.m_SiteCodes[projTPindex] = count.m_Id;
          }
        }
      }
    }

    //helper function for LoadProjDetailsInGUI
    private void UpdateTimePeriodModule(TimePeriodModule tpModule, int projTPindex)
    {
      if (m_Project.m_TimePeriodLabels[projTPindex] == null)
      {
        tpModule.TimePeriodLabel.Text = "unused time period...";
      }
      else
      {
        tpModule.TimePeriodLabel.Text = m_Project.m_TimePeriodLabels[projTPindex];
      }
      tpModule.StartTimePicker.hasEnteredBoxValChanged = true;
      tpModule.EndTimePicker.hasEnteredBoxValChanged = true;

      if (m_Project.m_TimePeriods[projTPindex] == "NOT USED")
      {
        tpModule.ActiveCheckBox.IsChecked = false;
        tpModule.StartDatePicker.SelectedDate = null;
        tpModule.StartTimePicker.hours.Text = "";
        tpModule.StartTimePicker.minutes.Text = "";
        tpModule.StartTimePicker.AMorPM.SelectedValue = "";
        tpModule.EndTimePicker.hours.Text = "";
        tpModule.EndTimePicker.minutes.Text = "";
        tpModule.EndTimePicker.AMorPM.SelectedValue = "";
      }
      else
      {
        tpModule.ActiveCheckBox.IsChecked = true;
        //tpModule.StartDatePicker.Visibility = Visibility.Hidden;
        tpModule.StartDatePicker.SelectedDateChanged -= tpModule.DatePicker_SelectedDateChanged;
        tpModule.StartDatePicker.SelectedDate = null;
        tpModule.StartDatePicker.SelectedDateChanged += tpModule.DatePicker_SelectedDateChanged;
        tpModule.StartTimePicker.hours.Text = Int32.Parse((m_Project.m_TimePeriods[projTPindex].Split('-')[0]).Split(':')[0]).ToString();
        tpModule.StartTimePicker.minutes.Text = (m_Project.m_TimePeriods[projTPindex].Split('-')[0]).Split(':')[1];
        if (Int32.Parse((m_Project.m_TimePeriods[projTPindex].Split('-')[0]).Split(':')[0]) > 11)
        {
          tpModule.StartTimePicker.AMorPM.SelectedValue = "PM";
        }
        else
        {
          tpModule.StartTimePicker.AMorPM.SelectedValue = "AM";
        }
        tpModule.EndTimePicker.hours.Text = Int32.Parse((m_Project.m_TimePeriods[projTPindex].Split('-')[1]).Split(':')[0]).ToString();
        tpModule.EndTimePicker.minutes.Text = (m_Project.m_TimePeriods[projTPindex].Split('-')[1]).Split(':')[1];
        if (Int32.Parse((m_Project.m_TimePeriods[projTPindex].Split('-')[1]).Split(':')[0]) > 11)
        {
          tpModule.EndTimePicker.AMorPM.SelectedValue = "PM";
        }
        else
        {
          tpModule.EndTimePicker.AMorPM.SelectedValue = "AM";
        }
      }
      tpModule.m_ID = m_Project.m_TimePeriodIDs[projTPindex];
    }

    //sets title of a window
    public void UpdateWindowTitle(Window win)
    {
      if (m_Project != null)
      {
        if (!String.IsNullOrEmpty(m_FilePath))
        {
          win.Title = m_FilePath.Split('\\')[m_FilePath.Split('\\').Length - 1];
          return;
        }
        else if (!string.IsNullOrWhiteSpace(m_Project.m_OrderNumber))
        {
          if (!string.IsNullOrWhiteSpace(m_Project.m_ProjectName))
          {
            //display order number with proj name if both present
            win.Title = m_Project.m_OrderNumber + " - " + m_Project.m_ProjectName + " - Merlin";
          }
          else
          {
            //display order number if present but proj name is not
            win.Title = m_Project.m_OrderNumber + " - Merlin";
          }
          return;
        }
      }
      //if project is null or order number is null or empty just display program name
      win.Title = "Merlin";

    }

    private void detailsTabImportDetailsButton_Click(object sender, RoutedEventArgs e)
    {
      if (m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
      {
        MenuNewWeb_Click(sender, e);
      }
      else
      {
        MessageBox.Show("Sorry but our princess is in another castle...\n\n (Feature not available in Beta)", "Under Construction", MessageBoxButton.OK, MessageBoxImage.Hand);

        /*ProjectDetailsSync pds = new ProjectDetailsSync(m_Project);
        pds.Owner = this;
        bool? result = pds.ShowDialog();
        if (result == true)
        {
          //Now modify details tab fields according to the changed details retrieved.
        }*/
      }
    }

    #endregion

    #region Data Manipulation

    private void DataTab_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void DataTab_Selected(object sender, RoutedEventArgs e)
    {
      DataTabSelectedTasks();
    }

    private void DataTabSelectedTasks()
    {
      ClearFlagStacks();
      if (!m_CurrentState.m_DataTabVisited)
      {
        m_CurrentDataTabCount = m_Project.m_Intersections[0].m_Counts[0];
        SetupBankTabs();
        m_CurrentState.m_DataTabVisited = true;
        PopulateDataGrid();
        PopulateDataTabIntersectionList();
        dataTabRemoveCountFileButton.IsEnabled = false;
        dataTabViewCountFileButton.IsEnabled = false;
        dataTabChangeCountFileButton.IsEnabled = false;
      }

      if (m_Project.m_ProjectDataState == DataState.Empty)
      {
        SetDataEmptyState();
      }
      else
      {
        if (m_CurrentState.m_DataTabState != DataTabState.Loaded)
        {
          SetDataPresentState();
        }
      }
    }

    private void SetupBankTabs()
    {
      if (dataBankTabs.Items.Count > 0)
      {
        dataBankTabs.SelectedItem = null;
        dataBankTabs.Items.Clear();
      }
      for (int i = 0; i < m_Project.m_Banks.Count; i++)
      {
        if (m_Project.m_Banks[i] != "NOT USED" || m_Project.m_PedBanks[i] != PedColumnDataType.NA)
        {
          TabItem tab = new TabItem();
          if (!m_Project.m_TCCDataFileRules)
          {
            tab.Tag = m_Project.GetCombinedBankNames(i);
            tab.Header = m_Project.GetCombinedBankNames(i);
          }
          else
          {
            if (m_Project.m_Banks[i] == "FHWAPedsBikes")
            {
              tab.Tag = m_Project.GetCombinedBankNames(i);
              tab.Header = "Bikes & Peds";
            }
            else
            {
              tab.Tag = m_Project.GetCombinedBankNames(i);
              tab.Header = m_Project.m_BankDictionary[i];
            }
          }
          dataBankTabs.Items.Add(tab);
        }
      }
    }

    private void SetDataEmptyState()
    {
      rotateCountButton.IsEnabled = false;
      dataTabPrintCountButton.IsEnabled = false;
      DisplayDataTabMessage("No Data associated with this project.");
    }

    private void SetDataPresentState()
    {
      dataBankTabs.IsEnabled = true;
      dataTabData.IsEnabled = true;
      rotateCountButton.IsEnabled = true;
      dataTabPrintCountButton.IsEnabled = true;
      DisplayDataTabMessage("");

    }

    private void DisplayDataTabMessage(string message)
    {
      dataTabStatusMessage.Text = message;
      dataTabStatusMessage.FontSize = 14;
      dataTabStatusMessage.Foreground = new SolidColorBrush(Colors.Red);
      dataTabStatusMessage.FontWeight = FontWeights.Bold;
    }

    private void PopulateDataTabIntersectionList()
    {
      dataTabTreeList.Items.Clear();
      foreach (var intersection in m_Project.m_Intersections)
      {
        TreeViewItem intItem = new TreeViewItem
        {

          Header = intersection.GetLocationName(),
          Margin = new Thickness(2, 8, 0, 0)
        };

        foreach (var count in intersection.m_Counts)
        {
          TreeViewItem item = new TreeViewItem
          {
            Tag = count,
            Header = count.GetTimePeriodLabel() + " - " + count.m_Id,
            ToolTip = count.GetTimePeriodLabel() + " - " + count.m_Id,
            Margin = new Thickness(2, 6, 0, 0)
          };

          intItem.Items.Add(item);
        }
        dataTabTreeList.Items.Add(intItem);
      }
      ((TreeViewItem)dataTabTreeList.Items[0]).ExpandSubtree();
      ((TreeViewItem)((TreeViewItem)dataTabTreeList.Items[0]).Items[0]).IsSelected = true;
    }

    private void ImportDataFiles_Click(object sender, RoutedEventArgs e)
    {
      CountImportOptionsDialog fd1 = GetFileImportParameters();
      if (fd1 == null)
      {
        return;
      }


      CountDataFileAssociationDialog fd2 = PerformFileSearch(fd1.searchDirectory.Text, int.Parse(fd1.searchDays.Text));
      if (fd2 == null)
      {
        return;
      }

      ClearStacks();

      int option = 3;
      if (fd1.option1.IsChecked == true)
      {
        option = 1;
      }
      else if (fd1.option2.IsChecked == true)
      {
        option = 2;
      }
      CountDataImporter fd3 = ProcessImportFiles(fd2.files, option);
      if (fd3 == null)
      {
        return;
      }
      if (m_Project.m_ProjectDataState != DataState.Empty)
      {
        if (fd3.log.Count > 0)
        {
          ImportSummary summaryWindow = new ImportSummary(fd3.log);
          summaryWindow.Owner = this;
          summaryWindow.ShowDialog();
          MessageBox.Show("Import Complete, Project saved.", "Auto-Save", MessageBoxButton.OK);
        }
        else
        {
          MessageBox.Show("Import Complete with no logged results. Project saved.", "Auto-Save", MessageBoxButton.OK);
        }
        SaveProjectWithoutDialog();
        SetDataPresentState();
        m_CurrentState.m_DataTabState = DataTabState.Loaded;
        PopulateFileList();
        PopulateSums();
      }

      if (BalancingTab.IsSelected)
      {
        RefreshBalancingTotals();
      }
    }

    private CountImportOptionsDialog GetFileImportParameters()
    {
      CountImportOptionsDialog fd = new CountImportOptionsDialog(m_Project);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }

    private CountDataFileAssociationDialog PerformFileSearch(string dir, int days)
    {
      CountDataFileAssociationDialog fd = new CountDataFileAssociationDialog(m_Project, dir, days);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }

    private CountDataImporter ProcessImportFiles(Dictionary<string, Tuple<string, string>> files, int option)
    {
      CountDataImporter fd = new CountDataImporter(m_Project, files, option);
      fd.Owner = this;
      bool? result = fd.ShowDialog();
      if (result == true)
      {
        return fd;
      }
      return null;
    }

    private void DataTabClearData_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("WARNING!. \nThis will delete all the data in all counts.  \nAre you sure?", "Clear All Data", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        MessageBoxResult result2 = MessageBox.Show("WARNING!. \nThis will delete all the data in all counts.  \nAre you VERY sure?", "Clear All Data", MessageBoxButton.OKCancel);
        if (result2 == MessageBoxResult.OK)
        {
          foreach (Intersection intersection in m_Project.m_Intersections)
          {
            foreach (Count count in intersection.m_Counts)
            {
              count.ClearData();
              count.ClearDataFiles();
            }
          }
          PopulateDataGrid();
          PopulateFileList();
          PopulateSums();
          SetDataEmptyState();
          m_CurrentState.m_DataTabState = DataTabState.Empty;
        }
      }
    }

    private void PopulateDataGrid()
    {
      using (new WaitCursor())
      {
        resetSumNumbers();
        dataTabData.ItemsSource =
          new DataView(m_CurrentDataTabCount.m_Data.Tables[m_DataTabCurrentBank]);
        PopulateSums();
        if (dataTabData.Columns.Count > 0)
        {
          dataTabData.Columns[0].Visibility = Visibility.Collapsed;
        }
        dataTabNameOfSelectedData.Text =
          m_CurrentDataTabCount.m_Id + " - " +
          m_CurrentDataTabCount.m_ParentIntersection.GetLocationName();
      }
    }

    private void PopulateNoteContainer()
    {
      dataTabNotesPanel.Children.Clear();
      NoteContainer dataNotes = new NoteContainer(ref m_CurrentDataTabCount.m_Notes, NoteType.CountLevel_Data,
          m_CurrentDataTabCount.m_Id + " - " +
          m_CurrentDataTabCount.GetLocation(), m_CurrentUser);
      dataNotes.LostFocus += DataNotes_LostFocus;
      dataTabNotesPanel.Children.Add(dataNotes);
    }

    private void DataNotes_LostFocus(object sender, RoutedEventArgs e)
    {
      RefreshNoteLists();
    }

    private void resetSumNumbers()
    {
      dataTabSelectionSum.Text = "";
    }

    private void DataTabDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
      if (dataTabData.Columns.Count > 0)
      {
        dataTabData.Columns[0].Visibility = Visibility.Collapsed;
      }
      var mainGridScrollViewer = FindVisualChild<ScrollViewer>(dataTabData);
      //mainGridScrollViewer.ScrollChanged += DataTabScroll_Changed;
    }

    private void PopulateFileList()
    {
      dataTabFileList.Items.Clear();
      foreach (DataFile file in m_CurrentDataTabCount.m_AssociatedDataFiles)
      {
        if (file.Name == "Manual Entry" || file.Name == "Unknown Source")
        {
          continue;
        }
        ListBoxItem item = new ListBoxItem();
        item.Content = file.Name.Split('\\')[file.Name.Split('\\').Length - 1];
        item.ToolTip = file.Name;
        item.Tag = file;
        dataTabFileList.Items.Add(item);
      }
    }

    private void RotateData_Click(object sender, RoutedEventArgs e)
    {
      RotateDataWindow rotateWindow = new RotateDataWindow(m_CurrentDataTabCount);
      rotateWindow.Owner = this;
      bool? result = rotateWindow.ShowDialog();
      if (result == true)
      {
        undoStack.Clear();
        redoStack.Clear();
        PopulateDataGrid();
      }

    }

    private void DataTabCountSelect_Changed(object sender, RoutedEventArgs e)
    {
      if (dataTabTreeList.SelectedItem == null)
      {
        return;
      }
      if (!((TreeViewItem)dataTabTreeList.SelectedItem).HasItems)
      {
        m_CurrentDataTabCount = (Count)((TreeViewItem)dataTabTreeList.SelectedItem).Tag;

        if (m_CurrentDataTabCount != null)
        {
          PopulateDataGrid();
          PopulateFileList();
          PopulateNoteContainer();
        }
      }
      else
      {
        ((TreeViewItem)dataTabTreeList.SelectedItem).ExpandSubtree();
        ((TreeViewItem)((TreeViewItem)dataTabTreeList.SelectedItem).Items[0]).IsSelected = true;
      }
    }

    private void DataTabExpand_Click(object sender, RoutedEventArgs e)
    {
      foreach (var location in dataTabTreeList.Items)
      {
        TreeViewItem item = (TreeViewItem)location;
        if (item.HasItems && !item.IsExpanded)
        {
          item.ExpandSubtree();
        }
      }
    }

    private void DataTabCollapse_Click(object sender, RoutedEventArgs e)
    {
      foreach (var location in dataTabTreeList.Items)
      {
        TreeViewItem item = (TreeViewItem)location;
        if (item.IsExpanded)
        {
          item.IsExpanded = false;
        }
      }
    }

    private void DataTabBankTab_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (dataBankTabs.IsEnabled && dataBankTabs.SelectedItem != null)
      {
        string bank = ((string)((TabItem)dataBankTabs.SelectedItem).Tag).Split('&')[0].Trim();
        string pedBank = ((string)((TabItem)dataBankTabs.SelectedItem).Tag).Split('&')[1].Trim();
        for (int i = 0; i < m_Project.m_Banks.Count; i++)
        {
          if (bank == m_Project.m_Banks[i] && pedBank == m_Project.m_PedBanks[i].ToString())
          {
            m_DataTabCurrentBank = i;
            break;
          }
        }
        PopulateDataGrid();
        dataTabData.Focus();
      }
    }

    private void DataTabColumnHeader_Click(object sender, MouseButtonEventArgs e)
    {
      var columnHeader = sender as DataGridColumnHeader;
      if (e.ChangedButton == MouseButton.Left && columnHeader != null)
      {
        if (columnHeader.DataContext.ToString() == "Interval")
        {
          dataTabData.SelectedCells.Clear();
          foreach (var column in dataTabData.Columns)
          {
            if (column.Header.ToString() == "Interval")
            {
              continue;
            }
            foreach (var item in dataTabData.Items)
            {
              dataTabData.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
          }
        }
        else
        {
          dataTabData.Focus();
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          {
            foreach (var item in dataTabData.Items)
            {
              var cell = new DataGridCellInfo(item, columnHeader.Column);
              if (dataTabData.SelectedCells.Contains(cell))
              {
                dataTabData.SelectedCells.Remove(cell);
              }
              else
              {
                dataTabData.SelectedCells.Add(cell);
              }
            }
          }
          else
          {
            dataTabData.SelectedCells.Clear();
            foreach (var item in dataTabData.Items)
            {
              dataTabData.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
          }
        }
      }
    }

    private void DataTabCellSelection_Changed(object sender, SelectedCellsChangedEventArgs e)
    {
      int selectionSum = 0;

      if (dataTabData.SelectedCells.Count > 0)
      {
        foreach (var cell in dataTabData.SelectedCells)
        {
          var cellValue = ((DataRowView)cell.Item).Row.ItemArray[cell.Column.DisplayIndex];
          if (cellValue.GetType() == (typeof(Int16)) || cellValue.GetType() == (typeof(int)))
          {
            selectionSum += int.Parse(cellValue.ToString());
          }
        }
        dataTabSelectionSum.Text = "Selected Cells: " + selectionSum;
      }
      else
      {
        return;
      }
    }

    private void DataGridHandleKeyPress(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete && dataTabData.SelectedCells.Count > 0)
      {
        redoStack.Clear();
        UndoStackPush();
        DeleteSelectedCells();
        PopulateSums();
      }
    }

    private void DataTabCellEdit_End(object sender, DataGridCellEditEndingEventArgs e)
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
      m_CurrentDataTabCount.EditSingleCell(row, e.Column.DisplayIndex, m_DataTabCurrentBank, value);
      PopulateSums();
    }

    private void DataTabPaste_Executed(object sender, ExecutedRoutedEventArgs e)
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
            if (selCell.Column.DisplayIndex == 0)
            {
              continue;
            }
            DataGridCell cell;
            var cellContent = selCell.Column.GetCellContent(selCell.Item);
            if (cellContent == null)
            {
              MessageBox.Show("Paste error, please ensure a cell in the data grid is selected.",
                "Paste error", MessageBoxButton.OK);
              return;
            }
            cell = (DataGridCell)cellContent.Parent;

            if (cell.Column.DisplayIndex < leftCol)
            {
              leftCol = cell.Column.DisplayIndex;
            }
            DataRowView d = selCell.Item as DataRowView;

            DateTime thisTime;
            DateTime countStartTime;
            string intervalTime = d.Row.ItemArray[0].ToString();
            DateTime.TryParse(intervalTime, out thisTime);
            DateTime.TryParse(m_CurrentDataTabCount.m_StartTime, out countStartTime);
            if (thisTime < countStartTime)
            {
              thisTime = thisTime.AddDays(1);
            }
            int rowIndexDifference = (int)(thisTime - countStartTime).TotalMinutes / (int)m_Project.m_IntervalLength;
            if (rowIndexDifference < topRow)
            {
              topRow = rowIndexDifference;
            }
          }
        }
        if (topRow > m_CurrentDataTabCount.m_NumIntervals || leftCol > 16)
        {
          return;
        }
        //Determine actual pasting eligible area

        if (leftCol == 0)
        {
          leftCol++;
        }

        int pasteRows = (m_CurrentDataTabCount.m_NumIntervals - topRow) < clipRows ? m_CurrentDataTabCount.m_NumIntervals - topRow : clipRows;
        int pasteCols = (17 - leftCol) < clipCols ? 17 - leftCol : clipCols;


        // Paste cells
        DataTable thisTable = m_CurrentDataTabCount.m_Data.Tables[m_DataTabCurrentBank];
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
              thisTable.Rows[topRow][m_Project.m_ColumnHeaders[m_DataTabCurrentBank][c]] = targetValue;
              c++;
            }
            else
            {
              foundBadClipData = true;
            }
          }
          topRow++;
        }
        m_CurrentDataTabCount.RunDataState();
        PopulateDataGrid();
        dataTabData.Focus();
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

    private void DataTabCut_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (dataTabData.SelectedCells.Count > 0)
      {
        redoStack.Clear();
        UndoStackPush();
        ApplicationCommands.Copy.Execute(null, dataTabData);
        DeleteSelectedCells();
        PopulateSums();
      }
    }

    private void DeleteSelectedCells()
    {
      foreach (var cell in dataTabData.SelectedCells)
      {
        if (cell.Column.DisplayIndex != 0)
        {
          DataRowView d = cell.Item as DataRowView;
          DateTime cellInterval;
          DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
          string endPoint =
            cellInterval.AddMinutes(m_CurrentDataTabCount.GetIntervalLength())
              .TimeOfDay.ToString()
              .Remove(5, 3);
          m_CurrentDataTabCount.ClearData(d.Row.ItemArray[0].ToString(), endPoint,
            new List<int> { cell.Column.DisplayIndex }, new List<string> { m_DataTabCurrentBank.ToString() });
        }
      }
      dataTabData.Focus();
    }

    private void DataTabFileListSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (dataTabFileList.SelectedItem == null)
      {
        dataTabRemoveCountFileButton.IsEnabled = false;
        dataTabViewCountFileButton.IsEnabled = false;
        dataTabChangeCountFileButton.IsEnabled = false;
      }
      else
      {
        dataTabRemoveCountFileButton.IsEnabled = true;
        dataTabViewCountFileButton.IsEnabled = true;
        dataTabChangeCountFileButton.IsEnabled = true;
      }
    }

    private void DataTabFileName_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (dataTabFileList.SelectedItem != null)
      {
        DataTabViewFile(((ListBoxItem)dataTabFileList.SelectedItem).ToolTip.ToString());
      }
    }

    private void DataTabViewFile_Click(object sender, RoutedEventArgs e)
    {
      if (dataTabFileList.SelectedItem != null)
      {
        DataTabViewFile(((ListBoxItem)dataTabFileList.SelectedItem).ToolTip.ToString());
      }
    }

    private void DataTabViewFile(string fileName)
    {

      List<string> fileLines = new List<string>();
      if (!fileName.EndsWith("csv") && !fileName.EndsWith("txt"))
      {
        MessageBox.Show("File has unknown extension. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
                  "Data File Error", MessageBoxButton.OK);
        return;
      }
      FileInput fileInput = new FileInput(fileName);
      if (!fileInput.TryFileLoad<string>())
      {
        MessageBox.Show("Could not open file. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1] + "\n\n" + fileInput.GetErrorMessage(),
                  "Data File Error", MessageBoxButton.OK);
        return;
      }
      if (!fileInput.GetDataFileLines(ref fileLines))
      {
        MessageBox.Show("No data in file: \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
          "File Empty", MessageBoxButton.OK);
        return;
      }

      DataFileViewWindow dfvw = new DataFileViewWindow(fileName, m_CurrentDataTabCount, fileLines);
      dfvw.Owner = this;
      bool? result = dfvw.ShowDialog();
      if (result == true)
      {
        undoStack.Clear();
        redoStack.Clear();
        SetDataPresentState();
        PopulateSums();
      }
    }

    private void DataTabAddFile_Click(object sender, RoutedEventArgs e)
    {
      bool NetworkOk = CheckNetworkDirectoryPath(m_Project.m_Prefs.m_NetworkDataDirectory);

      OpenFileDialog dlg = new OpenFileDialog();
      dlg.Filter = "Data Files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
      dlg.Title = "Add a Count File";
      dlg.Multiselect = true;
      if (NetworkOk)
      {
        dlg.InitialDirectory = m_Project.m_Prefs.m_NetworkDataDirectory;
      }
      else
      {
        dlg.InitialDirectory = m_Project.m_Prefs.m_LocalDataDirectory;
      }
      bool? result = dlg.ShowDialog();
      if (result == true)
      {
        foreach (var file in dlg.FileNames)
        {
          DataFile dataFile = new DataFile(file);
          if (!m_CurrentDataTabCount.HasDataFile(dataFile))
          {
            m_CurrentDataTabCount.AddFileNameToFileList(dataFile);
          }
          else
          {
            MessageBox.Show(file + " is already in the list.", "", MessageBoxButton.OK);
          }
        }
      }
      PopulateFileList();
    }

    private void DataTabRemoveFile_Click(object sender, RoutedEventArgs e)
    {
      RemoveFileFromList();
    }

    private void DataTabFileListKey_Press(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        RemoveFileFromList();
      }
    }

    private void RemoveFileFromList()
    {
      MessageBoxResult result = MessageBox.Show("Are you sure you want to remove this file association? \n\n"
        + ((ListBoxItem)dataTabFileList.SelectedItem).ToolTip, "Remove File From Project", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        var file = ((DataFile)((ListBoxItem)dataTabFileList.SelectedItem).Tag);
        m_CurrentDataTabCount.RemoveFileNameFromFileList(file);
        dataTabFileList.Items.Remove(dataTabFileList.SelectedItem);
      }
    }

    private void DataTabChangeCountFile_Click(object sender, RoutedEventArgs e)
    {
      string fileName = ((ListBoxItem)dataTabFileList.SelectedItem).Content.ToString();
      CountFileChangeSelection cfcs = new CountFileChangeSelection(m_Project, fileName);
      cfcs.Owner = this;
      bool? result = cfcs.ShowDialog();
      if (result == false)
      {
        return;
      }
      var oldFile = ((DataFile)((ListBoxItem)dataTabFileList.SelectedItem).Tag);
      m_CurrentDataTabCount.RemoveFileNameFromFileList(oldFile);
      DataFile newFile = new DataFile(oldFile.Name, oldFile.LastModified, oldFile.Counter);
      cfcs.count.AddFileNameToFileList(newFile);
      PopulateFileList();
    }

    private void DataTabPrintCount_Click(object sender, RoutedEventArgs e)
    {
      MerlinPrintDocument printJob = new MerlinPrintDocument(m_Project);
      printJob.PrintDataSet(m_CurrentDataTabCount.m_Data, m_CurrentDataTabCount.m_Id, m_CurrentDataTabCount.m_ParentIntersection.GetLocationName());
    }

    private void UndoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        dataTabTreeList.SelectedItem as TreeViewItem,
        m_CurrentDataTabCount.m_Data.Tables[m_DataTabCurrentBank],
        m_DataTabCurrentBank,
        m_CurrentDataTabCount.m_AssociatedDataFiles);
      //m_CurrentDataTabCount.m_DataCellToFileMap);

      if (undoStack.Count < 1 || undoStack.Peek() != newState)
      {
        undoStack.Push(newState);
      }
    }

    private void RedoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        dataTabTreeList.SelectedItem as TreeViewItem,
        m_CurrentDataTabCount.m_Data.Tables[m_DataTabCurrentBank],
        m_DataTabCurrentBank,
        m_CurrentDataTabCount.m_AssociatedDataFiles);
      //m_CurrentDataTabCount.m_DataCellToFileMap);

      if (redoStack.Count < 1 || redoStack.Peek() != newState)
      {
        redoStack.Push(newState);
      }
    }

    private void DataTabUndo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (undoStack.Count > 0)
      {
        MerlinDataTableState restoreThis = undoStack.Pop();
        TreeViewItem selectedCount = restoreThis.Selection as TreeViewItem;
        selectedCount.IsSelected = true;
        dataBankTabs.SelectedIndex = restoreThis.Bank;

        RedoStackPush();

        m_CurrentDataTabCount = restoreThis.Selection.Tag as Count;
        m_DataTabCurrentBank = restoreThis.Bank;
        m_CurrentDataTabCount.CopyData(restoreThis.Table, restoreThis.Bank);
        m_CurrentDataTabCount.m_AssociatedDataFiles = restoreThis.FileAssocations.ToList<DataFile>();
        m_CurrentDataTabCount.InvertDataFileToCellMapping();
        //m_CurrentDataTabCount.m_DataCellToFileMap = restoreThis.CellMap.ToDictionary(x => x.Key, y => y.Value);

        PopulateDataGrid();
        dataTabData.Focus();
      }
    }

    private void DataTabRedo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (redoStack.Count > 0)
      {
        MerlinDataTableState restoreThis = redoStack.Pop();
        TreeViewItem selectedCount = restoreThis.Selection as TreeViewItem;
        selectedCount.IsSelected = true;
        dataBankTabs.SelectedIndex = restoreThis.Bank;

        UndoStackPush();

        m_CurrentDataTabCount = restoreThis.Selection.Tag as Count;
        m_DataTabCurrentBank = restoreThis.Bank;
        m_CurrentDataTabCount.CopyData(restoreThis.Table, restoreThis.Bank);
        m_CurrentDataTabCount.m_AssociatedDataFiles = restoreThis.FileAssocations;
        m_CurrentDataTabCount.InvertDataFileToCellMapping();
        //m_CurrentDataTabCount.m_DataCellToFileMap = restoreThis.CellMap;

        PopulateDataGrid();
        dataTabData.Focus();
      }
    }

    private void ClearStacks()
    {
      undoStack.Clear();
      redoStack.Clear();
    }

    private void PopulateSums()
    {
      if (dataTabData != null && m_CurrentDataTabCount != null)
      {
        InitializeTables();
        int[] tempRowSums = new int[m_CurrentDataTabCount.m_NumIntervals];
        int[] tempColSums = new int[16];
        int countSum = 0;

        for (int i = 0; i < m_CurrentDataTabCount.m_NumIntervals; i++)
        {
          DataRow row = m_CurrentDataTabCount.m_Data.Tables[m_DataTabCurrentBank].Rows[i];
          for (int j = 1; j < row.ItemArray.Length; j++)
          {
            tempRowSums[i] += Int16.Parse(row.ItemArray[j].ToString());
            tempColSums[j - 1] += Int16.Parse(row.ItemArray[j].ToString());
            countSum += Int16.Parse(row.ItemArray[j].ToString());
          }
        }

        for (int r = 0; r < tempRowSums.Length; r++)
        {
          rowSums.Rows[r][0] = tempRowSums[r];
        }

        for (int c = 0; c < tempColSums.Length; c++)
        {
          columnSums.Rows[0][c + 1] = tempColSums[c];
        }
        dataTabRowSumGrid.ItemsSource = new DataView(rowSums);
        dataTabColumnSumGrid.ItemsSource = new DataView(columnSums);
        if (dataTabColumnSumGrid.Columns.Count > 0)
        {
          dataTabColumnSumGrid.Columns[0].Visibility = Visibility.Collapsed;
        }
        dataTabCountSum.Text = countSum.ToString();
      }
    }

    private void dataTabColumnSumGrid_Loaded(object sender, RoutedEventArgs e)
    {
      if (dataTabColumnSumGrid.Columns.Count > 0)
      {
        dataTabColumnSumGrid.Columns[0].Visibility = Visibility.Collapsed;
      }
    }

    private void InitializeTables()
    {
      columnSums = new DataTable();

      DataColumn c = new DataColumn("Time");
      c.DataType = Type.GetType("System.String");
      c.ReadOnly = true;
      c.Unique = true;
      columnSums.Columns.Add(c);
      Dictionary<int, List<string>> columnHeaders = m_Project.m_ColumnHeaders;
      int[] columns = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      for (int j = 1; j < columnHeaders[m_DataTabCurrentBank].Count(); j++)
      {
        string colHeader = columnHeaders[m_DataTabCurrentBank][j];
        DataColumn dc = new DataColumn(colHeader);
        dc.DataType = Type.GetType("System.Int32");
        columnSums.Columns.Add(dc);
      }

      DataRow s = columnSums.NewRow();
      s[0] = "Total";
      for (int m = 1; m < columns.Length; m++)
      {
        s[m] = columns[m - 1];
      }
      columnSums.Rows.Add(s);

      rowSums = new DataTable();
      DataColumn d = new DataColumn("Total");
      d.DataType = Type.GetType("System.Int32");
      rowSums.Columns.Add(d);

      for (int k = 0; k < m_CurrentDataTabCount.m_NumIntervals; k++)
      {
        DataRow r = rowSums.NewRow();
        r[0] = 0;
        rowSums.Rows.Add(r);
      }
    }


    #endregion

    #region Count Balancing

    private void BalancingTab_LeftMouseClicked(object sender, MouseButtonEventArgs e)
    {
      ClearStacks();
      ClearFlagStacks();
    }

    /// <summary>
    /// Should be called when project detail tab state changes to "viewing" to re-populate the tab, as in after loading,
    /// creating, or editing a project. Available balancing filters as well as any existing internal
    /// grid placements and neighbors will be populated on the balancing grid.
    /// </summary>
    private void PopulateBalancingTab()
    {
      //prevent the population from calling the refresh balancing numbers function multiple times
      m_IsBalancingTabCurrentlyPopulating = true;

      m_ListsOfIntervals = CreateTimePeriodIntervalLists();

      //set up grid
      RemoveAllBalancingModulesFromGrid(false);
      int size = m_Project.GetAllProjectLocations().Count;
      SetupBalancingGrid(size);

      //set up right-click menu for adding locations
      m_BalancingMenuAvailable.Items.Clear();
      foreach (Location loc in m_Project.GetAllProjectLocations())
      {
        MenuItem item = new MenuItem();
        item.Header = loc.GetLocationName();
        item.Click += new RoutedEventHandler(BalancingGrid_RightClickMenuAvailableItemClicked);
        item.Tag = loc;
        if(m_Project.m_Tubes.Count > 0)
        {
          item.Icon = new Image() { Width = 30, Height = 30, Source = new BitmapImage(new Uri( loc.GetType() == typeof(TubeSite) ? "Resources/tube2.png" : "Resources/camcorder.png", UriKind.Relative)) };
        }
        m_BalancingMenuAvailable.Items.Add(item);
      }

      //set up time period RadioButtons
      BalancingTabTPRadiosWrapPanel.Children.Clear();
      bool hasFirstVisibleTPBeenChecked = false;
      for (int i = 0; i < m_Project.m_TimePeriodLabels.Count; i++)
      {
        if (m_Project.m_TimePeriodLabels.ElementAt(i) != null)
        {
          RadioButton rb = new RadioButton();
          rb.Content = m_Project.m_TimePeriodLabels.ElementAt(i);
          rb.GroupName = "TPs";
          rb.MinWidth = 45;
          rb.Checked += Balancing_TPRadioButton_Checked;
          rb.Tag = i; //tag is the index of the corresponding time period in both m_TimePeriodLabels and m_TimePeriods
          BalancingTabTPRadiosWrapPanel.Children.Add(rb);

          if (!hasFirstVisibleTPBeenChecked)
          {
            rb.IsChecked = true;
            hasFirstVisibleTPBeenChecked = true;
          }
        }
      }

      //add saved balancing modules to grid
      foreach (Location loc in m_Project.GetAllProjectLocations())
      {
        if (loc.m_BalancingLocation.Key >= 0 && loc.m_BalancingLocation.Value >= 0)
        {
          BalancingModule newBM = CreateNewBalancingModule(loc.m_BalancingLocation.Key, loc.m_BalancingLocation.Value, loc, false);
        }
      }

      //set up bank CheckBoxes
      BalancingVehBanksWrapPanel.Children.Clear();
      for (int i = 0; i < m_Project.m_Banks.Count; i++)
      {
        CheckBox cb = new CheckBox();
        cb.Content = m_Project.m_Banks.ElementAt(i);
        cb.Name = "IncludeBank" + i.ToString() + "CheckBox";
        cb.MinWidth = 60.0;
        cb.Checked += BalancingBankCheckBox_Changed;
        cb.Unchecked += BalancingBankCheckBox_Changed;
        BalancingVehBanksWrapPanel.Children.Add(cb);

        if (m_Project.m_Banks.ElementAt(i) == "NOT USED")
        {
          cb.Visibility = Visibility.Collapsed;
          cb.IsChecked = false;
        }
        else
        {
          cb.Visibility = Visibility.Visible;
          if (m_Project.m_Banks.ElementAt(i) == BankVehicleTypes.Bicycles.ToString() || m_Project.m_Banks.ElementAt(i) == BankVehicleTypes.FHWAPedsBikes.ToString())
          {
            cb.IsChecked = false;
          }
          else
          {
            cb.IsChecked = true;
          }
        }
      }

      //set up U-Turn/RTOR CheckBoxes
      if (m_Project.m_PedBanks.Contains(PedColumnDataType.UTurn) && !m_Project.m_TCCDataFileRules)
      {
        IncludeUTurnsCheckBox.Visibility = Visibility.Visible;
        IncludeUTurnsCheckBox.IsChecked = true;
      }
      else
      {
        IncludeUTurnsCheckBox.Visibility = Visibility.Collapsed;
        IncludeUTurnsCheckBox.IsChecked = false;
      }
      if (m_Project.m_PedBanks.Contains(PedColumnDataType.RTOR))
      {
        IncludeRTORsCheckBox.Visibility = Visibility.Visible;
        IncludeRTORsCheckBox.IsChecked = true;
      }
      else
      {
        IncludeRTORsCheckBox.Visibility = Visibility.Collapsed;
        IncludeRTORsCheckBox.IsChecked = false;
      }

      if (m_Project.m_PedBanks.Contains(PedColumnDataType.PassengerRTOR))
      {
        IncludePassengerRTORsCheckBox.Visibility = Visibility.Visible;
        IncludePassengerRTORsCheckBox.IsChecked = true;
      }
      else
      {
        IncludePassengerRTORsCheckBox.Visibility = Visibility.Collapsed;
        IncludePassengerRTORsCheckBox.IsChecked = false;
      }
      if (m_Project.m_PedBanks.Contains(PedColumnDataType.HeavyRTOR))
      {
        IncludeHeavyRTORsCheckBox.Visibility = Visibility.Visible;
        IncludeHeavyRTORsCheckBox.IsChecked = true;
      }
      else
      {
        IncludeHeavyRTORsCheckBox.Visibility = Visibility.Collapsed;
        IncludeHeavyRTORsCheckBox.IsChecked = false;
      }
      if (m_Project.m_PedBanks.Contains(PedColumnDataType.PassengerUTurn))
      {
        IncludePassengerUTurnsCheckBox.Visibility = Visibility.Visible;
        IncludePassengerUTurnsCheckBox.IsChecked = true;
      }
      else
      {
        IncludePassengerUTurnsCheckBox.Visibility = Visibility.Collapsed;
        IncludePassengerUTurnsCheckBox.IsChecked = false;
      }
      if (m_Project.m_PedBanks.Contains(PedColumnDataType.HeavyUTurn))
      {
        IncludeHeavyUTurnsCheckBox.Visibility = Visibility.Visible;
        IncludeHeavyUTurnsCheckBox.IsChecked = true;
      }
      else
      {
        IncludeHeavyUTurnsCheckBox.Visibility = Visibility.Collapsed;
        IncludeHeavyUTurnsCheckBox.IsChecked = false;
      }

      BalancingGrid.ContextMenu = m_BalancingMenuAvailable;
      m_IsBalancingTabCurrentlyPopulating = false;
      //select view neighbors
      ViewNeighborsMode.IsChecked = true;
      matchTMCsCheckBox.IsChecked = true;
      showDatesCheckBox.IsChecked = true;
      includeUnclassifiedCheckBox.IsChecked = true;
      //checking the above box triggers RefreshBalancingTotals()

    }

    /// <summary>
    /// Creates lists of intervals for all time periods, creating lists of the single element "NOT USED" for unused time periods
    /// to keep indexing consistent. This should be called when project details change to update the lists.
    /// </summary>
    /// <returns></returns>
    private List<IEnumerable<string>> CreateTimePeriodIntervalLists()
    {
      List<IEnumerable<string>> lists = new List<IEnumerable<string>>();

      foreach (string tp in m_Project.m_TimePeriods)//.Where(x => x != "NOT USED"))
      {
        if (tp == "NOT USED")
        {
          //this time period not active, add dummie entry to keep list indexing valid
          List<string> unused = new List<string>();
          unused.Add("NOT USED");
          lists.Add(unused);
        }
        else
        {
          //build the interval list
          List<string> intervalList = new List<string>();
          string nextInterval = tp.Split('-')[0];
          do
          {
            //do-while ensures executes at least once in the case where start and end are the same (24-hr count) where a while loop wouldn't run
            intervalList.Add(nextInterval);
            nextInterval = Count.AddXMinutes(nextInterval, 5);
          } while (nextInterval != tp.Split('-')[1]);

          lists.Add(intervalList);
        }
      }

      return lists;
    }

    /// <summary>
    /// Creates a new balancing module and adds it to the balancing grid.
    /// </summary>
    /// <param name="row">zero-based row to add this to</param>
    /// <param name="col">zero-based row to add this to</param>
    /// <param name="internalLocation">The Intersection object corresponding with this BalancingModule</param>
    /// <param name="isUserPlacement">Indicates if this BalancingModule is being added by the user as opposed to being populated 
    /// by the program. BalancingModules added by the user will call the CalculateMyNeighbors method.</param>
    /// <returns></returns>
    private BalancingModule CreateNewBalancingModule(int row, int col, Location internalLocation, bool isUserPlacement = true)
    {
      //removes existing balancing module for this intersection from the grid if it exists
      if (internalLocation.m_BalancingLocation.Key > -1 && isUserPlacement)
      {
        RemoveBalancingModuleFromGrid(GetBalancingModule(internalLocation));
      }

      BalancingModule newBM = new BalancingModule(row, col, internalLocation, this);
      BalancingGrid.Children.Add(newBM);
      Grid.SetRow(newBM, row);
      Grid.SetColumn(newBM, col);

      if (isUserPlacement)
      {
        GuessMyNeighbors(newBM);
      }
      else
      {
        //delete any neighbor connections which are now impossible
        internalLocation.m_Neighbors.RemoveAll(x => !internalLocation.IsConnectionPossible(x.Key, x.Value.Key, x.Value.Value));
      }

      newBM.SetLayoutMode(m_CurrentState.m_BalancingTabState);

      RefreshBalancingTotals();

      SetBalancingGridToolTip();

      return newBM;
    }

    /// <summary>
    /// Populates the first and last interval ComboBoxes on the balancing tab.
    /// </summary>
    /// <param name="tpIndex">Index of the time period as it exists within the time period List<> in TMCProject.</param>
    private void PopulateIntervalRangeComboBoxes(int tpIndex)
    {
      //populate ComboBoxes
      FirstInterval.ItemsSource = m_ListsOfIntervals.ElementAt(tpIndex);
      LastInterval.ItemsSource = m_ListsOfIntervals.ElementAt(tpIndex);

      FirstInterval.SelectedIndex = 0;
      LastInterval.SelectedIndex = LastInterval.Items.Count - 1;
      SetIntervalsBasedOnRangeType();
    }

    /// <summary>
    /// Updates balancing totals and neighbor difference percentages stored in the BalancingModules based on the current parameters selected by the user in the GUI.
    /// </summary>
    public void RefreshBalancingTotals()
    {
      //prevents this function from getting called multiple times while balancing tab is getting populated
      if (m_IsBalancingTabCurrentlyPopulating)
      {
        return;
      }
      //try
      //{
      List<int> banks = new List<int>();
      List<int> uTurnBanks = new List<int>();
      List<int> RTORBanks = new List<int>();
      DateTime start = new DateTime(1, 1, 1, int.Parse(FirstInterval.SelectedItem.ToString().Split(':')[0]), int.Parse(FirstInterval.SelectedItem.ToString().Split(':')[1]), 0);
      DateTime end = start + new TimeSpan(0, (LastInterval.SelectedIndex - FirstInterval.SelectedIndex) * 5, 0);
      //DateTime end = new DateTime(1, 1, 1, int.Parse(LastInterval.SelectedItem.ToString().Split(':')[0]), int.Parse(LastInterval.SelectedItem.ToString().Split(':')[1]), 0);
      end = end.AddMinutes(5);
      TimeSpan length = end - start;
      int numIntervals;

      //get banks to include
      List<CheckBox> checkBoxes = FindVisualChildren<CheckBox>(BalancingVehBanksWrapPanel).ToList();
      for (int i = 0; i < m_Project.m_Banks.Count; i++)
      {
        if (checkBoxes.ElementAt(i).Visibility == Visibility.Visible && (bool)checkBoxes.ElementAt(i).IsChecked)
        {
          banks.Add(i);
        }
      }
      numIntervals = (int)length.TotalMinutes / ((int)m_Project.m_IntervalLength);

      //get U-Turn and RTOR banks to include
      for (int i = 0; i < m_Project.m_PedBanks.Count(); i++)
      {
        if (m_Project.m_TCCDataFileRules)
        {
          //U-Turns not selectable, instead automatically include U-Turns for vehicle column classes that are checked
          if ((m_Project.m_PedBanks[i] == PedColumnDataType.UTurn && banks.Contains(i)) ||
              (m_Project.m_PedBanks[i] == PedColumnDataType.PassengerUTurn && banks.Contains(i)) ||
              (m_Project.m_PedBanks[i] == PedColumnDataType.HeavyUTurn && banks.Contains(i)))
          {
            uTurnBanks.Add(i);
          }
        }
        else
        {
          //Include all U-Turns that are checked
          if ((m_Project.m_PedBanks[i] == PedColumnDataType.UTurn && (bool)IncludeUTurnsCheckBox.IsChecked) ||
              (m_Project.m_PedBanks[i] == PedColumnDataType.PassengerUTurn && (bool)IncludePassengerUTurnsCheckBox.IsChecked) ||
              (m_Project.m_PedBanks[i] == PedColumnDataType.HeavyUTurn && (bool)IncludeHeavyUTurnsCheckBox.IsChecked))
          {
            uTurnBanks.Add(i);
          }
          else if ((m_Project.m_PedBanks[i] == PedColumnDataType.RTOR && (bool)IncludeRTORsCheckBox.IsChecked) ||
                   (m_Project.m_PedBanks[i] == PedColumnDataType.PassengerRTOR && (bool)IncludePassengerRTORsCheckBox.IsChecked) ||
                   (m_Project.m_PedBanks[i] == PedColumnDataType.HeavyRTOR && (bool)IncludeHeavyRTORsCheckBox.IsChecked))
          {
            RTORBanks.Add(i);
          }
        }
      }

      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        //update the numbers stored within the balancing modules
        bm.UpdateNumbers(GetTimePeriodIndexFromRadio(GetSelectedBalancingRadioButton(false)),
          banks.ToArray(), uTurnBanks.ToArray(), RTORBanks.ToArray(), FirstInterval.SelectedValue.ToString(), numIntervals, (DateTime?)tubeDate.SelectedDate, (bool)matchTMCsCheckBox.IsChecked, (bool)includeUnclassifiedCheckBox.IsChecked);
        bm.areBalancingParametersNull = false;
      }
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        //now that numbers are updated, calculate percentages
        bm.SetPercentages();

        //refresh the numbers displayed on balancing modules in GUI
        bm.SetLayoutMode(m_CurrentState.m_BalancingTabState);
      }

      //refresh the selectable tube dates
      List<DateTime> tubeDates = m_Project.GetTubeDatesInProject();
      if(tubeDates.Count > 0)
      {
        tubeDate.DisplayDateStart = tubeDates.Min();
        tubeDate.DisplayDateEnd = tubeDates.Max();
      }

      foreach(BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.SetTextBlockColors();
      }

      //}
      //catch (Exception ex)
      //{
      //balancing numbers could not update because some parameter was not selected
      //foreach (BalancingModule bm in GetCurrentBalancingModules())
      //{
      //  bm.areBalancingParametersNull = true;
      //  bm.DisplayNull();
      //  MessageBox.Show("An unexpected error occured when trying to update balancing numbers:\n\n" + ex.Message, "Balancing Error", MessageBoxButton.OK, MessageBoxImage.Error);
      //}
      //}
    }

    //this is not currently used
    private void ShowPercentages()
    {
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.DisplayPercentages();
      }
    }

    //this is not currently used
    private void ShowTotals()
    {
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.DisplayTotals();
      }
    }

    public void RemoveBalancingModuleFromGrid(BalancingModule BMtoRemove)
    {
      RemoveBalancingModuleFromGrid(BMtoRemove, true);
    }

    public void RemoveBalancingModuleFromGrid(BalancingModule BMtoRemove, bool shouldResetBalancingLoc)
    {
      IEnumerable<Location> previousNeighborsTemp = BMtoRemove.internalLocation.m_Neighbors.GroupBy(x => x.Value.Key).Select(x => x.First().Value.Key);
      List<Location> previousNeighbors = previousNeighborsTemp.ToList();

      if (shouldResetBalancingLoc)
      {
        BMtoRemove.internalLocation.m_BalancingLocation = new MerlinKeyValuePair<int, int>(-1, -1);
        BMtoRemove.internalLocation.RemoveAllNeighbors();
      }
      BalancingGrid.Children.Remove(BMtoRemove);

      if (shouldResetBalancingLoc)
      {
        foreach (Location previousNeighbor in previousNeighbors)
        {
          GuessMyNeighbors(previousNeighbor);
        }
      }

      RefreshBalancingTotals();
      SetBalancingGridToolTip();
    }

    private void RemoveAllBalancingModulesFromGrid()
    {
      RemoveAllBalancingModulesFromGrid(true);
    }

    private void RemoveAllBalancingModulesFromGrid(bool shouldResetBalancingLoc)
    {
      IEnumerable<BalancingModule> BMs = BalancingGrid.Children.OfType<BalancingModule>();
      IEnumerable<Rectangle> cellBGs = BalancingGrid.Children.OfType<Rectangle>();

      for (int i = BMs.Count() - 1; i >= 0; i--)
      {
        RemoveBalancingModuleFromGrid(BMs.ElementAt(i), shouldResetBalancingLoc);
      }
    }

    private void SetLayoutMode_Checked(object sender, RoutedEventArgs e)
    {
      if (((RadioButton)sender).Name == "ViewNeighborsMode")
      {
        SetModeViewNeighbors();
      }
      else if (((RadioButton)sender).Name == "ViewDifferenceMode")
      {
        SetModeViewDifference();
      }
      else //(((RadioButton)sender).Name == "ViewTotalsMode")
      {
        SetModeViewTotals();
      }
    }

    private void SetModeViewNeighbors()
    {
      m_CurrentState.m_BalancingTabState = BalancingTabState.ViewNeighbors;
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.SetLayoutMode(m_CurrentState.m_BalancingTabState);
      }
    }

    private void SetModeViewDifference()
    {
      m_CurrentState.m_BalancingTabState = BalancingTabState.ViewDifference;
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.SetLayoutMode(m_CurrentState.m_BalancingTabState);
      }
    }

    private void SetModeViewTotals()
    {
      m_CurrentState.m_BalancingTabState = BalancingTabState.ViewTotals;
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.SetLayoutMode(m_CurrentState.m_BalancingTabState);
      }
    }

    /// <summary>
    /// Re-sizes the balancing grid to the given number of rows and columns, should not be called if grid is not clear of BalancingModules.
    /// </summary>
    /// <param name="numRows">Number of rows</param>
    /// <param name="numCols">Number of columns</param>
    /// 
    private void SetupBalancingGrid(int numRows, int numCols)
    {
      BalancingGrid.Width = numCols * 200;
      BalancingGrid.Height = numRows * 200;
      IEnumerable<Rectangle> cellBGs = BalancingGrid.Children.OfType<Rectangle>();

      //remove background rectangles
      for (int i = cellBGs.Count() - 1; i >= 0; i--)
      {
        BalancingGrid.Children.Remove(cellBGs.ElementAt(i));
      }

      //redefine rows
      BalancingGrid.RowDefinitions.Clear();
      for (int i = 0; i < numRows; i++)
      {
        RowDefinition newRow = new RowDefinition();
        newRow.Height = new GridLength(200);
        BalancingGrid.RowDefinitions.Add(newRow);
      }
      //redefine columns
      BalancingGrid.ColumnDefinitions.Clear();
      for (int i = 0; i < numCols; i++)
      {
        ColumnDefinition newCol = new ColumnDefinition();
        newCol.Width = new GridLength(200);
        BalancingGrid.ColumnDefinitions.Add(newCol);
      }

      //adds background cell rectangles
      for (int i = 0; i < numRows; i++)
      {
        for (int j = 0; j < numCols; j++)
        {
          Rectangle cellBackground = new Rectangle();
          cellBackground.Fill = m_BalancingGridBackground;
          BalancingGrid.Children.Add(cellBackground);
          Grid.SetRow(cellBackground, i);
          Grid.SetColumn(cellBackground, j);
        }
      }
    }

    private void SetupBalancingGrid(int size)
    {
      SetupBalancingGrid(size, size);
    }

    private void SetRightClickMenuAvailable()
    {
      BalancingGrid.ContextMenu = m_BalancingMenuAvailable;
      //Make context menu items bold that have been placed
      MerlinKeyValuePair<int, int> itemLocation;
      foreach (MenuItem item in m_BalancingMenuAvailable.Items)
      {
        itemLocation = m_Project.GetAllProjectLocations().Find(x => x == (Location)item.Tag).m_BalancingLocation;
        if (itemLocation.Key >= 0 && itemLocation.Value >= 0)
        {
          item.FontWeight = FontWeights.Bold;
        }
        else
        {
          item.FontWeight = FontWeights.Normal;
        }
      }

    }

    private void SetRightClickMenuOccupied()
    {
      BalancingGrid.ContextMenu = m_BalancingMenuOccupied;
    }

    private ContextMenu InitializeBalancingMenuOccupied()
    {
      ContextMenu cm = new ContextMenu();
      cm.Items.Add(new MenuItem());
      cm.Items.Add(new MenuItem());
      ((MenuItem)cm.Items.GetItemAt(0)).Header = "Go To Count...";
      ((MenuItem)cm.Items.GetItemAt(1)).Header = "Remove From Balancing Grid";
      ((MenuItem)cm.Items.GetItemAt(0)).Icon = new System.Windows.Controls.Image
      {
        Source = new BitmapImage(new Uri("Resources/Icons/data_sheet-24.png", UriKind.Relative)),
        Width = Constants.MENU_ICON_WIDTH,
        Height = Constants.MENU_ICON_HEIGHT
      };
      ((MenuItem)cm.Items.GetItemAt(1)).Icon = new System.Windows.Controls.Image
      {
        Source = new BitmapImage(new Uri("Resources/Icons/delete_sign-24.png", UriKind.Relative)),
        Width = Constants.MENU_ICON_WIDTH,
        Height = Constants.MENU_ICON_HEIGHT
      };
      ((MenuItem)cm.Items.GetItemAt(0)).Click += new RoutedEventHandler(BalancingGrid_RightClickMenuOccupiedItemClicked);
      ((MenuItem)cm.Items.GetItemAt(1)).Click += new RoutedEventHandler(BalancingGrid_RightClickMenuOccupiedItemClicked);

      return cm;
    }

    private void m_BalancingMenuOccupied_Opened(object sender, RoutedEventArgs e)
    {
      if (((MenuItem)m_BalancingMenuOccupied.Items.GetItemAt(m_BalancingMenuOccupied.Items.Count - 1)).Header.ToString() == "Balancing Notes")
      {
        m_BalancingMenuOccupied.Items.RemoveAt(m_BalancingMenuOccupied.Items.Count - 1);
      }

      if (m_ClickedBM.internalLocation is Intersection)
      {
        //notes
        Count clickedCount = ((Intersection)m_ClickedBM.internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)GetSelectedBalancingRadioButton(false).Tag);
        if (clickedCount != null)
        {
          MenuItem notesParent = new MenuItem
          {
            Header = "Balancing Notes",
            Icon = clickedCount.m_Notes.Where(x => x.m_Type == NoteType.CountLevel_Balancing).Count().ToString()
          };
          notesParent.Click += new RoutedEventHandler(BalancingGrid_RightClickMenuOccupiedItemClicked);
          m_BalancingMenuOccupied.Items.Add(notesParent);
          //notesParent.Items.Add(new Balancing.BalancingNoteList(clickedCount.m_Notes));
        }
      }
      else
      {
        ((MenuItem)m_BalancingMenuOccupied.Items.GetItemAt(0)).IsEnabled = false;
      }
    }

    private void GuessMyNeighbors(Location loc)
    {
      GuessMyNeighbors(GetCurrentBalancingModules().First(x => x.internalLocation == loc));
    }

    private void GuessMyNeighbors(BalancingModule bm)
    {
      BalancingModule intersection = bm;
      BalancingModule northNeighbor = null;
      BalancingModule southNeighbor = null;
      BalancingModule westNeighbor = null;
      BalancingModule eastNeighbor = null;
      IEnumerable<BalancingModule> possibleNeighbors;

      //first clear all current neighbors
      intersection.internalLocation.m_Neighbors.Clear();

      //populates possibleNeighbors with other BMs that have the same x or y value
      possibleNeighbors = GetCurrentBalancingModules().OfType<BalancingModule>().Where(x => (Grid.GetRow(x) == Grid.GetRow(intersection) || Grid.GetColumn(x) == Grid.GetColumn(intersection)) && x != intersection);

      ////neighbors were previously set based on closest neighbors in all 4 directions
      //northNeighbor = possibleNeighbors.OrderBy(x => Grid.GetRow(x)).LastOrDefault(x => Grid.GetRow(x) < Grid.GetRow(intersection) && Grid.GetColumn(x) == Grid.GetColumn(intersection));
      //southNeighbor = possibleNeighbors.OrderBy(x => Grid.GetRow(x)).FirstOrDefault(x => Grid.GetRow(x) > Grid.GetRow(intersection) && Grid.GetColumn(x) == Grid.GetColumn(intersection));
      //westNeighbor = possibleNeighbors.OrderBy(x => Grid.GetColumn(x)).LastOrDefault(x => Grid.GetColumn(x) < Grid.GetColumn(intersection) && Grid.GetRow(x) == Grid.GetRow(intersection));
      //eastNeighbor = possibleNeighbors.OrderBy(x => Grid.GetColumn(x)).FirstOrDefault(x => Grid.GetColumn(x) > Grid.GetColumn(intersection) && Grid.GetRow(x) == Grid.GetRow(intersection));

      //neighbors now only get set if they are in the first adjacent cell in all 4 directions
      northNeighbor = possibleNeighbors.FirstOrDefault(x => Grid.GetRow(x) == (Grid.GetRow(intersection) - 1));
      southNeighbor = possibleNeighbors.FirstOrDefault(x => Grid.GetRow(x) == (Grid.GetRow(intersection) + 1));
      westNeighbor = possibleNeighbors.FirstOrDefault(x => Grid.GetColumn(x) == (Grid.GetColumn(intersection) - 1));
      eastNeighbor = possibleNeighbors.FirstOrDefault(x => Grid.GetColumn(x) == (Grid.GetColumn(intersection) + 1));

      if (northNeighbor != null)
      {
        //set north side
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.NBExiting, northNeighbor.internalLocation);
        northNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.NBEntering, intersection.internalLocation);
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.SBEntering, northNeighbor.internalLocation);
        northNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.SBExiting, intersection.internalLocation);
      }

      if (southNeighbor != null)
      {
        //set south side
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.SBExiting, southNeighbor.internalLocation);
        southNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.SBEntering, intersection.internalLocation);
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.NBEntering, southNeighbor.internalLocation);
        southNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.NBExiting, intersection.internalLocation);
      }

      if (westNeighbor != null)
      {
        //set west side
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.WBExiting, westNeighbor.internalLocation);
        westNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.WBEntering, intersection.internalLocation);
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.EBEntering, westNeighbor.internalLocation);
        westNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.EBExiting, intersection.internalLocation);
      }

      if (eastNeighbor != null)
      {
        //set east side
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.EBExiting, eastNeighbor.internalLocation);
        eastNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.EBEntering, intersection.internalLocation);
        intersection.internalLocation.SetNeighbor(BalancingInsOuts.WBEntering, eastNeighbor.internalLocation);
        eastNeighbor.internalLocation.SetNeighbor(BalancingInsOuts.WBExiting, intersection.internalLocation);
      }
    }

    private void GuessAllNeighbors()
    {
      foreach (BalancingModule bm in BalancingGrid.Children.OfType<BalancingModule>())
      {
        GuessMyNeighbors(bm);
      }
    }

    /// <summary>
    /// Returns the BalancingModule corresponding to the given intersection object. Returns the first 
    /// BalancingModule found, and there should only be one on the grid anyways. Returns null if none found.
    /// </summary>
    /// <param name="internalLocation"></param>
    /// <returns></returns>
    public BalancingModule GetBalancingModule(Location internalLocation)
    {
      return BalancingGrid.Children.OfType<BalancingModule>().FirstOrDefault(x => x.internalLocation == internalLocation);
    }

    public IEnumerable<BalancingModule> GetCurrentBalancingModules()
    {
      return BalancingGrid.Children.OfType<BalancingModule>();
    }

    private int GetTimePeriodIndexFromRadio(RadioButton rb)
    {
      if (rb.Tag != null)
      {
        return (int)rb.Tag;
      }
      return -1;
    }

    private void SetBalancingGridToolTip()
    {
      if (GetCurrentBalancingModules().Count() < 1)
      {
        BalancingGrid.ToolTip = "Right-click a balancing square to add an intersection";
      }
      else
      {
        BalancingGrid.ToolTip = null;
      }
    }

    private void SetIntervalsBasedOnRangeType()
    {
      if ((bool)rangeType15Min.IsChecked)
      {
        LastInterval.SelectedIndex = Math.Min(FirstInterval.SelectedIndex + 2, LastInterval.Items.Count - 1);
        FirstInterval.SelectedIndex = LastInterval.SelectedIndex - 2;
      }
      else if ((bool)rangeTypeHour.IsChecked)
      {
        LastInterval.SelectedIndex = Math.Min(FirstInterval.SelectedIndex + 11, LastInterval.Items.Count - 1);
        FirstInterval.SelectedIndex = LastInterval.SelectedIndex - 11;
      }
    }

    private void BalancingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (FirstInterval.SelectedItem != null && LastInterval.SelectedItem != null)
      {
        if ((ComboBox)sender == FirstInterval)
        {
          SetIntervalsBasedOnRangeType();
        }
        RefreshBalancingTotals();
      }

    }

    private void BalancingBankCheckBox_Changed(object sender, RoutedEventArgs e)
    {
      RefreshBalancingTotals();
    }

    //a time period radio checked
    private void Balancing_TPRadioButton_Checked(object sender, RoutedEventArgs e)
    {
      int tpIndex = (int)((RadioButton)sender).Tag;

      FirstInterval.SelectionChanged -= new SelectionChangedEventHandler(BalancingComboBox_SelectionChanged);
      LastInterval.SelectionChanged -= new SelectionChangedEventHandler(BalancingComboBox_SelectionChanged);

      FilterEnabledRangeTypeRadios();
      PopulateIntervalRangeComboBoxes(tpIndex);

      FirstInterval.SelectionChanged += new SelectionChangedEventHandler(BalancingComboBox_SelectionChanged);
      LastInterval.SelectionChanged += new SelectionChangedEventHandler(BalancingComboBox_SelectionChanged);

      RefreshBalancingTotals();
    }

    //enables/disables appropriate time range selection options based on what is valid for the currently selected time period
    private void FilterEnabledRangeTypeRadios()
    {
      int numIntervals = m_Project.GetCountsInTimePeriod((int)GetSelectedBalancingRadioButton(false).Tag)[0].m_NumIntervals;
      if (numIntervals < 12)
      {
        //time period is less than an hour
        rangeTypeHour.IsEnabled = false;
        rangeTypeHour.Opacity = 0.4;
        calculatePeakHour.IsEnabled = false;
        calculatePeakHour.Opacity = 0.4;
        if ((bool)rangeTypeHour.IsChecked)
        {
          rangeTypeCustom.IsChecked = true;
        }
        if (numIntervals < 3)
        {
          //time period is also less than 15 minutes
          rangeType15Min.IsEnabled = false;
          rangeType15Min.Opacity = 0.4;
          calculatePeak15.IsEnabled = false;
          calculatePeak15.Opacity = 0.4;
          if ((bool)rangeType15Min.IsChecked)
          {
            rangeTypeCustom.IsChecked = true;
          }
        }
        else
        {
          rangeType15Min.IsEnabled = true;
          rangeType15Min.Opacity = 1;
          calculatePeak15.IsEnabled = true;
          calculatePeak15.Opacity = 1;
        }
      }
      else
      {
        rangeTypeHour.IsEnabled = true;
        rangeTypeHour.Opacity = 1;
        calculatePeakHour.IsEnabled = true;
        calculatePeakHour.Opacity = 1;
        rangeType15Min.IsEnabled = true;
        rangeType15Min.Opacity = 1;
        calculatePeak15.IsEnabled = true;
        calculatePeak15.Opacity = 1;
      }
    }

    private void BalancingGrid_RightClickMenuAvailableItemClicked(object sender, RoutedEventArgs e)
    {
      MenuItem item = (MenuItem)sender;
      CreateNewBalancingModule(m_AvailableCellClicked.Key, m_AvailableCellClicked.Value, m_Project.GetAllProjectLocations().First(x => x == item.Tag));
    }

    private void BalancingGrid_RightClickMenuOccupiedItemClicked(object sender, RoutedEventArgs e)
    {
      MenuItem item = (MenuItem)sender;
      if ((string)item.Header == "Go To Count..." && item.IsEnabled)
      {
        //Count object that the user wants to go see the data for. Could be null if count doesn't exist for the selected TP.
        Count count = ((Intersection)m_ClickedBM.internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)GetSelectedBalancingRadioButton(false).Tag);
        //test if user clicked go to count on an intersection that doesn't have a count for the currently selected time period
        if (count == null)
        {
          MessageBox.Show("This intersection doesn't have a count in the currently selected time period, " + GetSelectedBalancingRadioButton(false).Content.ToString(), "Count Doesn't Exist", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
        Dispatcher.Invoke((Action)(() => NavTabs.SelectedItem = DataTab)); //this is where it sometimes crashes
        TreeViewItem parentIntersection = ((TreeViewItem)(dataTabTreeList.Items.GetItemAt(m_Project.m_Intersections.IndexOf(m_ClickedBM.internalLocation as Intersection))));
        parentIntersection.IsExpanded = true;
        //figure out the count to show on the data tab
        IEnumerable<RadioButton> radios = BalancingTabTPRadiosWrapPanel.Children.OfType<RadioButton>().Where(x => x.Visibility == Visibility.Visible);
        for (int i = 0; i < radios.Count(); i++)
        {
          if ((bool)radios.ElementAt(i).IsChecked)
          {
            foreach (TreeViewItem treeCount in parentIntersection.Items)
            {
              if ((Count)treeCount.Tag == count)
              {
                treeCount.IsSelected = true;
                break;
              }
            }
            break;
          }
        }
      }
      else if ((string)item.Header == "Remove From Balancing Grid")
      {
        RemoveBalancingModuleFromGrid(m_ClickedBM);
      }
      else if ((string)item.Header == "Balancing Notes")
      {
        //Balancing notes item only exists on contextmenu if the balancing module's internal location is an intersection
        Count clickedCount = ((Intersection)m_ClickedBM.internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)GetSelectedBalancingRadioButton(false).Tag);
        //CountNoteList cnl = new CountNoteList(clickedCount, NoteType.CountLevel_Balancing);
        //cnl.ShowDialog();
        ShowCountNotesWindow(clickedCount, NoteType.CountLevel_Balancing);
      }
      else
      {
        return;
      }
    }

    private void ShowCountNotesWindow(Count count, NoteType type)
    {
      Window win = new Window();
      Grid winContent = new Grid();
      win.Content = winContent;
      win.ShowInTaskbar = false;
      win.Title = "Notes";
      win.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Merlin;component/Resources/Sample Logo/merlin-light-blue-no-text.ico", UriKind.RelativeOrAbsolute));
      win.ResizeMode = System.Windows.ResizeMode.NoResize;
      win.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
      StackPanel sp = new StackPanel();

      TextBlock intersectionText = new TextBlock();
      intersectionText.SetResourceReference(Control.StyleProperty, "rotateWindowHeaders");
      intersectionText.Text = count.m_ParentIntersection.GetLocationName();
      //intersectionText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

      //TextBlock siteCodeTimePeriodNoteTypeText = new TextBlock();
      //siteCodeTimePeriodNoteTypeText.SetResourceReference(Control.StyleProperty, "SectionHelpers");
      //siteCodeTimePeriodNoteTypeText.Text = count.GetId() + " | " + count.GetTimePeriodLabel()
      //        + " (" + count.GetTimePeriod() + ")";// | Count " + type + " Notes";
      //siteCodeTimePeriodNoteTypeText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

      sp.Children.Add(intersectionText);
      //sp.Children.Add(siteCodeTimePeriodNoteTypeText);
      sp.Children.Add(new NoteContainer(ref count.m_Notes, type, count.GetId() + " | "
        + count.GetTimePeriodLabel() + " (" + count.GetTimePeriod() + ")", m_CurrentUser));
      winContent.Children.Add(sp);

      win.ShowDialog();

      RefreshNoteLists();
    }

    private void BalancingGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
      var element = (UIElement)e.Source;
      int row = Grid.GetRow(element);
      int col = Grid.GetColumn(element);

      if (BalancingGrid.Children.OfType<BalancingModule>().Any(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == col))
      {
        SetRightClickMenuOccupied();
        m_ClickedBM = BalancingGrid.Children.OfType<BalancingModule>().First(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == col);
      }
      else
      {
        m_AvailableCellClicked = new MerlinKeyValuePair<int, int>(row, col);
        SetRightClickMenuAvailable();
      }
    }

    private void BalancingGrid_MouseMove(object sender, MouseEventArgs e)
    {
      var element = (UIElement)e.Source;
      int row = Grid.GetRow(element);
      int col = Grid.GetColumn(element);

      //true if cell mouse is hovering has changed
      if (row != m_BalancingGridCurrentMouseRow || col != m_BalancingGridCurrentMouseCol)
      {
        //set old rectangle back to background color
        if (m_BalancingGridCurrentMouseRow != -1 && m_BalancingGridCurrentMouseCol != -1)
        {
          Rectangle oldRect = BalancingGrid.Children.OfType<Rectangle>().First(x => Grid.GetRow(x) == m_BalancingGridCurrentMouseRow && Grid.GetColumn(x) == m_BalancingGridCurrentMouseCol);
          oldRect.Fill = m_BalancingGridBackground;
        }

        //set new rectangle
        if (BalancingGrid.Children.OfType<Rectangle>().Any(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == col))
        {
          ((Rectangle)BalancingGrid.Children.OfType<Rectangle>().First(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == col)).Fill = m_BalancingGridHover;
          m_BalancingGridCurrentMouseRow = row;
          m_BalancingGridCurrentMouseCol = col;
        }
        else
        {
          m_BalancingGridCurrentMouseRow = -1;
          m_BalancingGridCurrentMouseCol = -1;
        }
      }
    }

    private void BalancingGrid_MouseEnter(object sender, MouseEventArgs e)
    {
      m_BalancingGridCurrentMouseRow = -1;
      m_BalancingGridCurrentMouseCol = -1;
    }

    private void BalancingGrid_MouseLeave(object sender, MouseEventArgs e)
    {
      if (BalancingGrid.Children.OfType<Rectangle>().Any(x => Grid.GetRow(x) == m_BalancingGridCurrentMouseRow && Grid.GetColumn(x) == m_BalancingGridCurrentMouseCol))
      {
        Rectangle oldRect = BalancingGrid.Children.OfType<Rectangle>().First(x => Grid.GetRow(x) == m_BalancingGridCurrentMouseRow && Grid.GetColumn(x) == m_BalancingGridCurrentMouseCol);
        oldRect.Fill = m_BalancingGridBackground;
      }
    }

    private void BalancingTab_Enabled(object sender, RoutedEventArgs e)
    {

    }

    private void MenuTestCounts_Click(object sender, RoutedEventArgs e)
    {
      ShowModuleNotPurchasedDialog();
    }

    private void RemoveAllBMsButton_Click(object sender, RoutedEventArgs e)
    {
      if (GetCurrentBalancingModules().Count() == 0)
      {
        return;
      }
      MessageBoxResult result = MessageBox.Show("Are you sure you want to remove all intersections from the balancing area?"
        + "\n\nThis will also remove all relationships", "Remove All Intersections?", MessageBoxButton.YesNo);
      if (result == MessageBoxResult.Yes)
      {
        RemoveAllBalancingModulesFromGrid();
      }
    }

    private void ResetNeighborsButton_Click(object sender, RoutedEventArgs e)
    {
      if (GetCurrentBalancingModules().Count() == 0)
      {
        return;
      }
      MessageBoxResult result = MessageBox.Show("Are you sure you want to reset all relationships and connect to adjacent intersections?",
        "Reset All Relationships To Default?", MessageBoxButton.YesNo);
      if (result == MessageBoxResult.Yes)
      {
        GuessAllNeighbors();
        RefreshBalancingTotals();
      }
    }

    private void ClearNeighborsButton_Click(object sender, RoutedEventArgs e)
    {
      if (GetCurrentBalancingModules().Count() == 0)
      {
        return;
      }
      MessageBoxResult result = MessageBox.Show("Are you sure you want to remove all intersection relationships?",
        "Remove All Relationships?", MessageBoxButton.YesNo);
      if (result == MessageBoxResult.Yes)
      {
        foreach (BalancingModule bm in GetCurrentBalancingModules())
        {
          bm.internalLocation.m_Neighbors.Clear();
        }
        RefreshBalancingTotals();
      }
    }

    private void RangeTypeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
      RadioButton clickedRadio = (RadioButton)sender;

      switch (clickedRadio.Name)
      {
        case "rangeTypeCustom":
          FirstInterval.IsEnabled = true;
          LastInterval.IsEnabled = true;
          FirstInterval.SelectedIndex = 0;
          LastInterval.SelectedIndex = LastInterval.Items.Count - 1;
          break;
        case "rangeType15Min":
          FirstInterval.IsEnabled = true;
          LastInterval.IsEnabled = false;
          SetIntervalsBasedOnRangeType();
          break;
        case "rangeTypeHour":
          FirstInterval.IsEnabled = true;
          LastInterval.IsEnabled = false;
          SetIntervalsBasedOnRangeType();
          break;
        //case "rangeTypePeak15":
        //case "rangeTypePeakHour":
        //  FirstInterval.IsEnabled = false;
        //  LastInterval.IsEnabled = false;
        //  m_PeakMenu = PopulatePeakMenu();
        //  m_PeakMenu.IsOpen = true;
        //  break;
      }
    }

    //populates menu that appears upon selecting peak hour/15min balancing
    private ContextMenu PopulatePeakMenu()
    {
      ContextMenu menu = new ContextMenu();

      MenuItem menuText = new MenuItem();
      menuText.Header = "Calculated from " + m_Project.m_TimePeriodLabels[(int)GetSelectedBalancingRadioButton(false).Tag] + " counts filmed on:";
      menuText.IsHitTestVisible = false;
      menuText.FontWeight = FontWeights.Bold;
      menu.Items.Add(menuText);
      menu.Items.Add(new Separator());

      foreach (DateTime date in m_Project.GetFilmDatesOfTimePeriod((int)GetSelectedBalancingRadioButton(false).Tag))
      {
        MenuItem item = new MenuItem();
        item.Header = date.ToString("dddd, M/d/yyyy");
        item.Tag = date;
        item.Click += BalancingPeakMenuItem_Click;
        Uri iconPath;
        //set the icon path
        switch (date.DayOfWeek)
        {
          case DayOfWeek.Monday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/monday-24.png");
            break;
          case DayOfWeek.Tuesday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/tuesday-24.png");
            break;
          case DayOfWeek.Wednesday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/wednesday-24.png");
            break;
          case DayOfWeek.Thursday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/thursday-24.png");
            break;
          case DayOfWeek.Friday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/friday-24.png");
            break;
          case DayOfWeek.Saturday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/saturday-24.png");
            break;
          case DayOfWeek.Sunday:
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/sunday-24.png");
            break;
          default:
            //put something in so VS won't complain, should never happen though
            iconPath = new Uri("pack://application:,,,/Merlin;component/Resources/Icons/monday-24.png");
            break;
        }
        //sets icon
        item.Icon = new System.Windows.Controls.Image
        {
          Source = new BitmapImage(iconPath),
          Width = Constants.MENU_ICON_WIDTH,
          Height = Constants.MENU_ICON_HEIGHT
        };
        //finally adds MenuItem containing RadioButton to ContextMenu
        menu.Items.Add(item);
      }
      return menu;
    }

    //A date was clicked in balancing tab peak 15/hour menu
    void BalancingPeakMenuItem_Click(object sender, RoutedEventArgs e)
    {
      ShowPeak(sender as MenuItem, (string)((ContextMenu)((sender as MenuItem).Parent)).Tag);
    }

    private void ShowPeak(MenuItem clickedItem, string menu)
    {
      int intervalDiff = 0;
      if (menu == calculatePeak15.Name)
      {
        intervalDiff = 2;
        rangeType15Min.IsChecked = true;
      }
      else if (menu == calculatePeakHour.Name)
      {
        intervalDiff = 11;
        rangeTypeHour.IsChecked = true;
      }
      List<int> banksToIgnore = new List<int>();
      foreach (var entry in m_Project.m_BankDictionary)
      {
        if (entry.Value == BankVehicleTypes.Bicycles.ToString() || entry.Value == BankVehicleTypes.FHWAPedsBikes.ToString())
        {
          banksToIgnore.Add(entry.Key);
          break;
        }
      }
      List<int> pedBanksToIgnore = new List<int>();
      foreach (PedColumnDataType entry in m_Project.m_PedBanks)
      {
        if (entry == PedColumnDataType.NA || entry == PedColumnDataType.Pedestrian)
        {
          pedBanksToIgnore.Add(m_Project.m_PedBanks.IndexOf(entry));
        }
      }
      FirstInterval.SelectedItem = m_Project.GetSystemWidePeak((int)GetSelectedBalancingRadioButton(false).Tag, intervalDiff + 1, banksToIgnore, pedBanksToIgnore, (DateTime)clickedItem.Tag);
      LastInterval.SelectedIndex = FirstInterval.SelectedIndex + intervalDiff;
      //FirstInterval.IsEnabled = false;
      //LastInterval.IsEnabled = false;
    }

    private void peakRadioButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (((TextBlock)sender).Name == "calculatePeak15")
      {
        m_PeakMenu = PopulatePeakMenu();
      }
      else if (((TextBlock)sender).Name == "calculatePeakHour")
      {
        m_PeakMenu = PopulatePeakMenu();
      }
      m_PeakMenu.Tag = ((TextBlock)sender).Name;
      m_PeakMenu.IsOpen = true;

      e.Handled = true;
    }

    #region Panning & Zooming

    //http://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer

    void OnMouseMove(object sender, MouseEventArgs e)
    {
      if (lastDragPoint.HasValue)
      {
        Point posNow = e.GetPosition(BalancingScrollViewer);

        double dX = posNow.X - lastDragPoint.Value.X;
        double dY = posNow.Y - lastDragPoint.Value.Y;

        lastDragPoint = posNow;

        BalancingScrollViewer.ScrollToHorizontalOffset(BalancingScrollViewer.HorizontalOffset - dX);
        BalancingScrollViewer.ScrollToVerticalOffset(BalancingScrollViewer.VerticalOffset - dY);
      }
    }

    void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.OriginalSource is TextBlock && ((TextBlock)e.OriginalSource).Tag != null && ((TextBlock)e.OriginalSource).Tag.ToString() == "neighborTextBlock")
      {
        return;
      }
      var mousePos = e.GetPosition(BalancingScrollViewer);
      if (mousePos.X <= BalancingScrollViewer.ViewportWidth && mousePos.Y <
          BalancingScrollViewer.ViewportHeight) //make sure we still can use the scrollbars
      {
        BalancingScrollViewer.Cursor = ((FrameworkElement)Resources["CursorGrabbing"]).Cursor;
        lastDragPoint = mousePos;
        Mouse.Capture(BalancingScrollViewer);
      }
    }

    void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      lastMousePositionOnTarget = Mouse.GetPosition(BalancingGrid);

      if (e.Delta > 0)
      {
        slider.Value += 0.1;
      }
      if (e.Delta < 0)
      {
        slider.Value -= 0.1;
      }

      e.Handled = true;
    }

    void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      BalancingScrollViewer.Cursor = Cursors.Arrow;
      BalancingScrollViewer.ReleaseMouseCapture();
      lastDragPoint = null;
    }

    void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      scaleTransform.ScaleX = e.NewValue;
      scaleTransform.ScaleY = e.NewValue;

      var centerOfViewport = new Point(BalancingScrollViewer.ViewportWidth / 2,
                                       BalancingScrollViewer.ViewportHeight / 2);
      lastCenterPositionOnTarget = BalancingScrollViewer.TranslatePoint(centerOfViewport, BalancingGrid);

    }

    void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
      {
        Point? targetBefore = null;
        Point? targetNow = null;

        if (!lastMousePositionOnTarget.HasValue)
        {
          if (lastCenterPositionOnTarget.HasValue)
          {
            var centerOfViewport = new Point(BalancingScrollViewer.ViewportWidth / 2,
                                             BalancingScrollViewer.ViewportHeight / 2);
            Point centerOfTargetNow =
                  BalancingScrollViewer.TranslatePoint(centerOfViewport, BalancingGrid);

            targetBefore = lastCenterPositionOnTarget;
            targetNow = centerOfTargetNow;
          }
        }
        else
        {
          targetBefore = lastMousePositionOnTarget;
          targetNow = Mouse.GetPosition(BalancingGrid);

          lastMousePositionOnTarget = null;
        }

        if (targetBefore.HasValue)
        {
          double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
          double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

          double multiplicatorX = e.ExtentWidth / BalancingGrid.Width;
          double multiplicatorY = e.ExtentHeight / BalancingGrid.Height;

          double newOffsetX = BalancingScrollViewer.HorizontalOffset -
                              dXInTargetPixels * multiplicatorX;
          double newOffsetY = BalancingScrollViewer.VerticalOffset -
                              dYInTargetPixels * multiplicatorY;

          if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
          {
            return;
          }

          BalancingScrollViewer.ScrollToHorizontalOffset(newOffsetX);
          BalancingScrollViewer.ScrollToVerticalOffset(newOffsetY);
        }
      }
    }

    #endregion

    public void ResetParametersGUI()
    {

    }

    //Returns selected radio button in balancing tab and optionally unselects it. Returns null if none selected
    public RadioButton GetSelectedBalancingRadioButton(bool clearIt)
    {
      foreach (RadioButton rb in BalancingTabTPRadiosWrapPanel.Children.OfType<RadioButton>())
      {
        if ((bool)rb.IsChecked)
        {
          if (clearIt)
          {
            rb.IsChecked = false;
          }
          return rb;
        }
      }

      return null;
    }

    #endregion

    #region Flags Page

    private void FlagTab_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void FlagTab_Enabled(object sender, RoutedEventArgs e)
    {
      if (!m_CurrentState.m_FlagTabVisited)
      {
        m_CurrentState.m_FlagTabVisited = true;
        SetFlagToggles();
      }
      ClearStacks();
      if (FlagList.SelectedItem == null)
      {
        flagDataDetails.ItemsSource = new DataView(blankTable);
        flagDataDetails.Opacity = 0;
        flagTabDataGridTabs.Opacity = 0;
        flagNoteAndTogglePanel.Visibility = Visibility.Collapsed;
        if (flagDataDetails.Columns.Count > 0)
        {
          flagDataDetails.Columns[0].Visibility = Visibility.Collapsed;
        }
      }
      else
      {
        Flag currentFlag = ((Flag)((ListBoxItem)FlagList.SelectedItem).Tag);
        if (currentFlag.m_Note != null)
        {
          flagTabNote.Text = currentFlag.m_Note.LatestNote;
        }
      }
    }

    private void RunFlags_Click(object sender, RoutedEventArgs e)
    {
      ClearFlagStacks();
      FlagList.Items.Clear();
      if (m_Project.m_ProjectDataState != DataState.Empty)
      {
        m_Project.ProcessProjectFlags(GetFlagToggleBoxes());
        PopulateFlagList(hideFilteredFlagsCheck.IsChecked == true);
        m_CurrentState.m_FlagTabState = FlagTabState.Generated;
        TurnOffFlagsPageOpacity();
        if (CheckForAllFlagsAccepted())
        {
          m_CurrentState.m_FlagTabState = FlagTabState.Accepted;
        }
      }
      else
      {
        MessageBox.Show("No data files are present in the project. Cannot generate Flags.",
          "No Data Warning", MessageBoxButton.OK);
      }
    }

    private Dictionary<string, bool> GetFlagToggleBoxes()
    {
      Dictionary<string, bool> dictToReturn = new Dictionary<string, bool>
      {
        {"IgnoreBikeImpossibleMovements", ignoreBicyclesFlagsCheck.IsChecked == true},
        {FlagType.EmptyInterval.ToString(), emptyIntervalTestToggle.IsChecked == true},
        {FlagType.DataBeyondEndTime.ToString(), true},  // Currently no toggle for this, but it is installed in case it ever becomes applicable.
        {FlagType.ImpossibleMovement.ToString(), impossibleMovementsTestToggle.IsChecked == true},
        {FlagType.LowInterval.ToString(), lowIntervalTestToggle.IsChecked == true},
        {FlagType.HighInterval.ToString(), highIntervalTestToggle.IsChecked == true},
        {FlagType.NoVehicleWarning.ToString(), noVehicleTestToggle.IsChecked == true},
        {FlagType.LowHeavies.ToString(), lowHeaviesTestToggle.IsChecked == true},
        {FlagType.SuspiciousTrafficFlow.ToString(), suspiciousTrafficFlowTestToggle.IsChecked == true},
        {FlagType.ImpossibleUTurn.ToString(), inappropriateUTurnTestToggle.IsChecked == true},
        {FlagType.InappropriateRTOR.ToString(), inappropriateRTORTestToggle.IsChecked == true},
        {FlagType.SuspiciousMovement.ToString(), suspiciousMovementTestToggle.IsChecked == true},
        {FlagType.CountClassificationDiscrepancy.ToString(), classCountAgainstProject.IsChecked == true},
        {FlagType.FileDataClassificationDiscrepancy.ToString(), classHourAgainstCount.IsChecked == true},
        {FlagType.HourClassificationDiscrepancy.ToString(), classFileAgainstCount.IsChecked == true},
        {FlagType.NoData.ToString(), true}// Currently no toggle for this, but it is installed in case it ever becomes applicable.
      };

      return dictToReturn;
    }

    private void SetFlagToggles()
    {
      if (m_Project.m_TCCDataFileRules)
      {
        classCountTestTogglePanel.Visibility = Visibility.Visible;
        classCountAgainstProject.IsChecked = true;
        classHourAgainstCount.IsChecked = true;
        classFileAgainstCount.IsChecked = true;

        tmcOnlyTestTogglePanel.Visibility = Visibility.Collapsed;
        inappropriateUTurnTestToggle.IsChecked = false;
        inappropriateRTORTestToggle.IsChecked = false;
      }
      else
      {
        classCountTestTogglePanel.Visibility = Visibility.Collapsed;
        classCountAgainstProject.IsChecked = false;
        classHourAgainstCount.IsChecked = false;
        classFileAgainstCount.IsChecked = false;

        tmcOnlyTestTogglePanel.Visibility = Visibility.Visible;
        inappropriateUTurnTestToggle.IsChecked = true;
        inappropriateRTORTestToggle.IsChecked = true;
      }
    }

    private void SetupFlagTabBankTabs()
    {
      if (flagTabDataGridTabs.Items.Count > 0)
      {
        flagTabDataGridTabs.SelectedItem = null;
        flagTabDataGridTabs.Items.Clear();
      }
      for (int i = 0; i < m_Project.m_Banks.Count; i++)
      {
        if (m_Project.m_Banks[i] != "NOT USED" || m_Project.m_PedBanks[i] != PedColumnDataType.NA)
        {
          TabItem tab = new TabItem();
          if (!m_Project.m_TCCDataFileRules)
          {
            tab.Tag = m_Project.GetCombinedBankNames(i);
            tab.Header = m_Project.GetCombinedBankNames(i);
          }
          else
          {
            if (m_Project.m_Banks[i] == "FHWAPedsBikes")
            {
              tab.Tag = m_Project.GetCombinedBankNames(i);
              tab.Header = "Bikes & Peds";
            }
            else
            {
              tab.Tag = m_Project.GetCombinedBankNames(i);
              tab.Header = m_Project.m_BankDictionary[i];
            }
          }
          flagTabDataGridTabs.Items.Add(tab);
        }
      }
    }

    private void FlagTabBankTab_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (flagTabDataGridTabs.IsEnabled && flagTabDataGridTabs.SelectedItem != null)
      {
        string bank = ((string)((TabItem)flagTabDataGridTabs.SelectedItem).Tag).Split('&')[0].Trim();
        string pedBank = ((string)((TabItem)flagTabDataGridTabs.SelectedItem).Tag).Split('&')[1].Trim();
        for (int i = 0; i < m_Project.m_Banks.Count; i++)
        {
          if (bank == m_Project.m_Banks[i] && pedBank == m_Project.m_PedBanks[i].ToString())
          {
            m_FlagTabCurrentBank = i;
            break;
          }
        }
        PopulateFlagDetailDataGrid();
      }
    }

    private void HideFiltered_Click(object sender, RoutedEventArgs e)
    {
      FlagList.Items.Clear();
      ClearFlagDetails();
      if (m_Project.m_ProjectDataState != DataState.Empty)
      {
        PopulateFlagList(true);
      }
      else
      {
        MessageBox.Show("You must Run the Flags before selecting this toggle.", "", MessageBoxButton.OK);
      }
    }

    private void ShowAllFlags_Click(object sender, RoutedEventArgs e)
    {
      FlagList.Items.Clear();
      if (m_Project.m_ProjectDataState != DataState.Empty)
      {
        if (m_Project.m_FlagsGenerated)
        {
          PopulateFlagList(false);
        }
        else
        {
          MessageBox.Show("You must Run the Flags before selecting this toggle.", "", MessageBoxButton.OK);
        }
      }
      else
      {
        MessageBox.Show("No data files are present in the project. Cannot generate Flags.",
          "No Data Warning", MessageBoxButton.OK);
      }
    }

    private void PopulateFlagList(bool filter)
    {
      foreach (Flag flag in m_Project.GetAllFlags())
      {
        if (filter && flag.m_IsAcceptable)
        {
          continue;
        }
        ListBoxItem item = new ListBoxItem();
        item.Tag = flag;
        item.Content = flag.MakeListMessage();
        item.ToolTip = flag.MakeListMessage();

        if (flag.m_IsAcceptable)
        {
          item.Foreground = new SolidColorBrush(Colors.Green);
          item.FontWeight = FontWeights.Normal;
        }
        else
        {
          item.Foreground = new SolidColorBrush(Colors.Red);
          item.FontWeight = FontWeights.Bold;
        }
        FlagList.Items.Add(item);
      }
    }

    private void FlagSelect_Changed(object sender, RoutedEventArgs e)
    {
      if (FlagList.SelectedItem != null)
      {
        m_LastSelectedFlag = m_CurrentFlag;
        m_CurrentFlag = (Flag)((ListBoxItem)FlagList.SelectedItem).Tag;
        bool changeGrid = ((m_LastSelectedFlag.m_ParentCount.m_Id != m_CurrentFlag.m_ParentCount.m_Id)
          || m_LastSelectedFlag.m_Bank != m_CurrentFlag.m_Bank);

        if (m_CurrentFlag != null)
        {
          flagNoteAndTogglePanel.Visibility = Visibility.Visible;
          flagTabInformation.Text = m_CurrentFlag.m_Type.ToString() + " - " + m_CurrentFlag.m_Information;
          flagTabInterval.Text = m_CurrentFlag.m_IntervalContainingError;
          flagTabMovement.Text = m_CurrentFlag.m_Movement;
          flagTabLocation.Text = m_CurrentFlag.m_ParentCount.m_Id + " - " + m_CurrentFlag.m_ParentCount.GetLocation();
          flagTabTimePeriod.Text = m_CurrentFlag.m_ParentCount.GetTimePeriod();
          acceptableFlag.IsChecked = m_CurrentFlag.m_IsAcceptable;

          if (m_CurrentFlag.m_Note != null)
          {
            flagTabNote.Text = m_CurrentFlag.m_Note.LatestNote;
          }
          else
          {
            flagTabNote.Text = "";
          }
          foreach (var bank in m_Project.m_BankDictionary)
          {
            if (m_CurrentFlag.m_Bank == bank.Value)
            {
              m_FlagTabCurrentBank = bank.Key;
            }
          }
          if (changeGrid)
          {
            if (String.IsNullOrEmpty(m_LastSelectedFlag.m_ParentCount.m_Id))
            {
              SetupFlagTabBankTabs();
            }
            PopulateFlagDetailDataGrid();
          }
          else
          {
            HighlightFlag();
          }
        }
      }
      else
      {
        ClearFlagDetails();
      }
    }

    private void PopulateFlagDetailDataGrid()
    {
      flagDataDetails.Opacity = 1;
      flagTabDataGridTabs.Opacity = 1;
      flagDataDetails.IsEnabled = true;
      flagTabDataGridTabs.IsEnabled = true;
      flagDataDetails.ItemsSource = new DataView(m_CurrentFlag.m_ParentCount.m_Data.Tables[m_FlagTabCurrentBank]);
      if (flagDataDetails.Columns.Count > 0)
      {
        flagDataDetails.Columns[0].Visibility = Visibility.Collapsed;
      }
      flagTabDataGridTabs.SelectedItem = flagTabDataGridTabs.Items[m_FlagTabCurrentBank];
      flagDataDetails.ScrollIntoView(m_CurrentFlag.m_ParentCount.m_Data.Tables[m_FlagTabCurrentBank].Rows);
      HighlightFlag();
    }

    private void FlagTabDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
      if (flagDataDetails.Columns.Count > 0)
      {
        flagDataDetails.Columns[0].Visibility = Visibility.Collapsed;
      }
      //HighlightFlag();
    }

    //private void HighlightFlag()
    //{
    //  if (!String.IsNullOrEmpty(m_CurrentFlag.m_Movement))
    //  {
    //    int colIdx = -1;
    //    int bank = -1;
    //    foreach (var bankPair in m_Project.m_BankDictionary)
    //    {
    //      if (bankPair.Value == m_CurrentFlag.m_Bank)
    //      {
    //        bank = bankPair.Key;
    //      }
    //    }
    //    if (bank == -1)
    //    {
    //      return;
    //    }

    //    for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[bank].Columns.Count; i++)
    //    {
    //      string movement = m_CurrentFlag.m_Movement;
    //      if (movement == m_CurrentFlag.m_ParentCount.m_Data.Tables[bank].Columns[i].ColumnName)
    //      {
    //        colIdx = i;
    //      }
    //    }
    //    List<KeyValuePair<int, int>> cellsToSelect = new List<KeyValuePair<int, int>>();
    //    if (!string.IsNullOrEmpty(m_CurrentFlag.m_IntervalContainingError))
    //    {
    //      for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows.Count; i++)
    //      {
    //        if (m_CurrentFlag.m_IntervalContainingError == (string)m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows[i].ItemArray[0])
    //        {
    //          cellsToSelect.Add(new KeyValuePair<int, int>(i, colIdx));
    //        }
    //      }
    //    }
    //    else
    //    {
    //      for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows.Count; i++)
    //      {
    //        cellsToSelect.Add(new KeyValuePair<int, int>(i, colIdx));
    //      }
    //    }
    //    SelectCellsByIndexes(flagDataDetails, cellsToSelect);
    //  }
    //}

    private List<KeyValuePair<int, int>> IdentifyCells()
    {
      List<KeyValuePair<int, int>> cells = new List<KeyValuePair<int, int>>();
      if (flagDataDetails.Columns.Count <= 0)
      {
        return cells;
      }

      int colIdx = -1;
      int bank = 0;
      foreach (var bankPair in m_Project.m_BankDictionary)
      {
        if (bankPair.Value == m_CurrentFlag.m_Bank)
        {
          bank = bankPair.Key;
        }
      }
      for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[bank].Columns.Count; i++)
      {
        string movement = m_CurrentFlag.m_Movement;
        if (movement == m_CurrentFlag.m_ParentCount.m_Data.Tables[bank].Columns[i].ColumnName)
        {
          colIdx = i;
        }
      }
      if (!string.IsNullOrEmpty(m_CurrentFlag.m_IntervalContainingError))
      {
        for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows.Count; i++)
        {
          if (m_CurrentFlag.m_IntervalContainingError == (string)m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows[i].ItemArray[0])
          {
            cells.Add(new KeyValuePair<int, int>(i, colIdx));
          }
        }
      }
      else
      {
        for (int i = 0; i < m_CurrentFlag.m_ParentCount.m_Data.Tables[0].Rows.Count; i++)
        {
          cells.Add(new KeyValuePair<int, int>(i, colIdx));
        }
      }

      return cells;
    }

    private void ResetCellStyles(DataGrid grid)
    {
      Style blackCellStyle = new Style(typeof(DataGridCell));
      blackCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      for (int i = 1; i < grid.Columns.Count; i++)
      {
        for (int j = 0; j < grid.Items.Count; j++)
        {
          DataGridCell cell = GetCell(grid, grid.ItemContainerGenerator.ContainerFromIndex(j) as DataGridRow, i);
          if (cell != null)
          {
            cell.Style = blackCellStyle;
          }
        }
      }
    }

    private void HighlightFlag()
    {
      ResetCellStyles(flagDataDetails);
      BrushConverter bc = new BrushConverter();
      Brush brush = (Brush)bc.ConvertFrom("#E37681");
      brush.Freeze();
      Style redCellStyle = new Style(typeof(DataGridCell));
      redCellStyle.Setters.Add(new Setter(BackgroundProperty, brush));
      redCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

      List<KeyValuePair<int, int>> relevantCells = IdentifyCells();
      if (flagDataDetails.Columns.Count <= 0)
      {
        return;
      }
      foreach (KeyValuePair<int, int> rCell in relevantCells)
      {
        int rowIndex = rCell.Key;
        int columnIndex = rCell.Value;

        if (rowIndex >= 0 && rowIndex < flagDataDetails.Items.Count
          && columnIndex > 0 && columnIndex < flagDataDetails.Columns.Count)
        {
          object item = flagDataDetails.Items[rowIndex];
          DataGridRow row = flagDataDetails.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          if (row == null)
          {
            flagDataDetails.ScrollIntoView(item);
            row = flagDataDetails.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
          }
          if (row != null)
          {
            DataGridCell cell = GetCell(flagDataDetails, row, columnIndex);
            if (cell != null)
            {
              cell.Style = redCellStyle;
            }
          }
        }
      }
      if (!string.IsNullOrEmpty(m_CurrentFlag.m_IntervalContainingError) && flagDataDetails.Items.Count > 0 && flagDataDetails.Items[0] != null)
      {
        flagDataDetails.ScrollIntoView(flagDataDetails.Items[0]);
      }
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

    private void ClearFlagDetails()
    {
      flagTabInformation.Text = "";
      flagTabInterval.Text = "";
      flagTabMovement.Text = "";
      flagTabLocation.Text = "";
      flagTabTimePeriod.Text = "";
      acceptableFlag.IsChecked = false;
      flagTabNote.Text = "";
      flagTabDataGridTabs.Items.Clear();
      m_CurrentFlag = new Flag();
      m_LastSelectedFlag = new Flag();
      flagDataDetails.ItemsSource = new DataView(blankTable);
      flagDataDetails.Opacity = 0;
      flagDataDetails.IsEnabled = false;
      flagTabDataGridTabs.Opacity = 0;
      flagTabDataGridTabs.IsEnabled = false;
      flagNoteAndTogglePanel.Visibility = Visibility.Collapsed;

    }

    private void MarkAsAcceptable_Click(object sender, RoutedEventArgs e)
    {
      if (FlagList.SelectedIndex != -1)
      {
        m_CurrentFlag = (Flag)((ListBoxItem)FlagList.SelectedItem).Tag;
        if (m_CurrentFlag != null)
        {
          m_CurrentFlag.m_IsAcceptable = acceptableFlag.IsChecked.GetValueOrDefault();
          ToggleFlagInList(m_CurrentFlag);
        }
      }

      if (m_Project != null)
      {
        if (CheckForAllFlagsAccepted())
        {
          m_CurrentState.m_FlagTabState = FlagTabState.Accepted;
        }
      }
    }

    private bool CheckForAllFlagsAccepted()
    {
      foreach (Flag flag in m_Project.GetAllFlags())
      {
        if (!flag.m_IsAcceptable)
        {
          return false;
        }
      }
      return true;
    }

    private void ClearFlags_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result =
        MessageBox.Show("Are you sure you want to clear all existing flags from the project?",
          "Warning", MessageBoxButton.OKCancel);

      if (result == MessageBoxResult.Cancel)
      {
        return;
      }
      ClearFlagStacks();
      ClearFlagDetails();
      FlagList.Items.Clear();
      m_Project.ClearFlags();
      m_CurrentState.m_FlagTabState = FlagTabState.Empty;
      TurnOnFlagsPageOpacity();
    }

    private void TurnOnFlagsPageOpacity()
    {
      FlagList.IsEnabled = false;
      hideFilteredFlagsCheck.IsEnabled = false;
      clearFlagsButton.IsEnabled = false;
    }

    private void TurnOffFlagsPageOpacity()
    {
      FlagList.IsEnabled = true;
      hideFilteredFlagsCheck.IsEnabled = true;
      clearFlagsButton.IsEnabled = true;
    }

    private void ToggleFlagInList(Flag flag)
    {
      var item = (ListBoxItem)FlagList.SelectedItem;
      if (flag.m_IsAcceptable)
      {
        if (!hideFilteredFlagsCheck.IsChecked.Value)
        {
          item.Foreground = new SolidColorBrush(Colors.Green);
          item.FontWeight = FontWeights.Normal;
        }
        else
        {
          FlagList.Items.Remove(item);
        }

      }
      else
      {
        item.Foreground = new SolidColorBrush(Colors.Red);
        item.FontWeight = FontWeights.Bold;
        item.Foreground.Opacity = 1;
      }
    }

    private void FlagTabText_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      editingFlagNote = true;
    }

    private void FlagTabText_Changed(object sender, KeyboardFocusChangedEventArgs e)
    {
      FlagNoteChanged();
    }

    private void FlagTabTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        FlagNoteChanged();
        Keyboard.ClearFocus();
      }
    }

    private void FlagNoteChanged()
    {
      if (editingFlagNote)
      {
        if (m_CurrentFlag.m_Note == null)
        {
          Note newNote = new Note(m_CurrentFlag.m_Key, NoteType.CountLevel_Flag, flagTabNote.Text, m_CurrentUser);
          m_CurrentFlag.m_ParentCount.m_Notes.Add(newNote);
          m_CurrentFlag.m_Note = newNote;
        }
        else
        {
          m_CurrentFlag.m_Note.Edit(flagTabNote.Text, m_CurrentUser);
        }
        RefreshNoteLists();
      }
    }

    private void FlagTabDataGridHandleKeyPress(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        flagRedoStack.Clear();
        FlagUndoStackPush();
        FlagTabDeleteSelectedCells();
      }
    }

    private void FlagTabCellEdit_End(object sender, DataGridCellEditEndingEventArgs e)
    {
      flagRedoStack.Clear();
      FlagUndoStackPush();
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
      m_CurrentFlag.m_ParentCount.EditSingleCell(row, e.Column.DisplayIndex, m_FlagTabCurrentBank, value);
    }

    private void FlagTabPaste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      flagRedoStack.Clear();
      FlagUndoStackPush();
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
            DateTime.TryParse(m_CurrentFlag.m_ParentCount.m_StartTime, out countStartTime);
            if (thisTime < countStartTime)
            {
              thisTime = thisTime.AddDays(1);
            }
            int rowIndexDifference = (int)(thisTime - countStartTime).TotalMinutes / (int)m_Project.m_IntervalLength;
            if (rowIndexDifference < topRow)
            {
              topRow = rowIndexDifference;
            }
          }
        }
        if (topRow > m_CurrentFlag.m_ParentCount.m_NumIntervals || leftCol > 16)
        {
          return;
        }
        //Determine actual pasting eligible area

        if (leftCol == 0)
        {
          leftCol++;
        }

        int pasteRows = (m_CurrentFlag.m_ParentCount.m_NumIntervals - topRow) < clipRows ? m_CurrentFlag.m_ParentCount.m_NumIntervals - topRow : clipRows;
        int pasteCols = (17 - leftCol) < clipCols ? 17 - leftCol : clipCols;


        // Paste cells
        DataTable thisTable = m_CurrentFlag.m_ParentCount.m_Data.Tables[m_FlagTabCurrentBank];
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
              thisTable.Rows[topRow][m_Project.m_ColumnHeaders[m_FlagTabCurrentBank][c]] = targetValue;
              c++;
            }
            else
            {
              foundBadClipData = true;
            }
          }
          topRow++;
        }
        m_CurrentFlag.m_ParentCount.RunDataState();
        PopulateFlagDetailDataGrid();
        flagDataDetails.Focus();
        if (foundBadClipData)
        {
          MessageBox.Show("The Clipboard contained some or all non-numerical data.\nInvalid cells were skipped", "Paste Warning", MessageBoxButton.OK);
        }

      }
      catch (Exception ex)
      {
        MessageBox.Show("Unexpected error with paste.  Check the details of the operation and try again.\n\n" + ex.Message, "Paste Failed", MessageBoxButton.OK);
      }
    }

    private void FlagTabCut_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (flagDataDetails.SelectedCells.Count > 0)
      {
        flagRedoStack.Clear();
        FlagUndoStackPush();
        ApplicationCommands.Copy.Execute(null, flagDataDetails);
        FlagTabDeleteSelectedCells();
      }
    }

    private void FlagTabDeleteSelectedCells()
    {
      foreach (var cell in flagDataDetails.SelectedCells)
      {
        if (cell.Column.DisplayIndex != 0)
        {
          DataRowView d = cell.Item as DataRowView;
          DateTime cellInterval;
          DateTime.TryParse(d.Row.ItemArray[0].ToString(), out cellInterval);
          string endPoint =
            cellInterval.AddMinutes(m_CurrentFlag.m_ParentCount.GetIntervalLength())
              .TimeOfDay.ToString()
              .Remove(5, 3);
          m_CurrentFlag.m_ParentCount.ClearData(d.Row.ItemArray[0].ToString(), endPoint,
            new List<int> { cell.Column.DisplayIndex }, new List<string> { m_FlagTabCurrentBank.ToString() });
        }
      }
      flagDataDetails.Focus();
    }

    private void FlagKey_Press(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.A || e.Key == Key.Space)
      {
        if (FlagList.SelectedItem != null)
        {
          m_CurrentFlag.m_IsAcceptable = !m_CurrentFlag.m_IsAcceptable;
          ToggleFlagInList(m_CurrentFlag);
        }
        if (CheckForAllFlagsAccepted())
        {
          m_CurrentState.m_FlagTabState = FlagTabState.Accepted;
        }
      }
    }

    private void FlagUndoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        FlagList.SelectedItem as ListBoxItem,
        m_CurrentFlag.m_ParentCount.m_Data.Tables[m_FlagTabCurrentBank],
        m_FlagTabCurrentBank,
        m_CurrentFlag.m_ParentCount.m_AssociatedDataFiles);
      //m_CurrentFlag.m_ParentCount.m_DataCellToFileMap);

      if (undoStack.Count < 1 || flagUndoStack.Peek() != newState)
      {
        flagUndoStack.Push(newState);
      }
    }

    private void FlagRedoStackPush()
    {
      MerlinDataTableState newState = new MerlinDataTableState(
        FlagList.SelectedItem as ListBoxItem,
        m_CurrentFlag.m_ParentCount.m_Data.Tables[m_FlagTabCurrentBank],
        m_FlagTabCurrentBank,
        m_CurrentFlag.m_ParentCount.m_AssociatedDataFiles);
      //m_CurrentFlag.m_ParentCount.m_DataCellToFileMap);

      if (redoStack.Count < 1 || flagRedoStack.Peek() != newState)
      {
        flagRedoStack.Push(newState);
      }
    }

    private void FlagTabUndo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (flagUndoStack.Count > 0)
      {
        MerlinDataTableState restoreThis = flagUndoStack.Pop();
        ListBoxItem selectedFlag = restoreThis.Selection as ListBoxItem;
        if (selectedFlag != null)
        {
          selectedFlag.IsSelected = true;

          FlagRedoStackPush();

          m_FlagTabCurrentBank = restoreThis.Bank;
          m_CurrentFlag.m_ParentCount.CopyData(restoreThis.Table, restoreThis.Bank);
          m_CurrentFlag.m_ParentCount.m_AssociatedDataFiles = restoreThis.FileAssocations;
          m_CurrentFlag.m_ParentCount.InvertDataFileToCellMapping();
          //m_CurrentFlag.m_ParentCount.m_DataCellToFileMap = restoreThis.CellMap;

          PopulateFlagDetailDataGrid();
          flagDataDetails.Focus();
        }
      }
    }

    private void FlagTabRedo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (flagRedoStack.Count > 0)
      {
        MerlinDataTableState restoreThis = flagRedoStack.Pop();
        ListBoxItem selectedFlag = restoreThis.Selection as ListBoxItem;
        if (selectedFlag != null)
        {
          selectedFlag.IsSelected = true;

          FlagUndoStackPush();

          m_FlagTabCurrentBank = restoreThis.Bank;
          m_CurrentFlag.m_ParentCount.CopyData(restoreThis.Table, restoreThis.Bank);
          m_CurrentFlag.m_ParentCount.m_AssociatedDataFiles = restoreThis.FileAssocations;
          m_CurrentFlag.m_ParentCount.InvertDataFileToCellMapping();
          //m_CurrentFlag.m_ParentCount.m_DataCellToFileMap = restoreThis.CellMap;

          PopulateFlagDetailDataGrid();
          flagDataDetails.Focus();
        }
      }
    }

    private void ClearFlagStacks()
    {
      flagUndoStack.Clear();
      flagRedoStack.Clear();
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


    #endregion

    #region Notes Tab

    private void notesTab_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void notesTab_Selected(object sender, MouseButtonEventArgs e)
    {
      if (!m_CurrentState.m_NotesTabVisited)
      {
        m_CurrentState.m_NotesTabVisited = true;
        PopulateProjectNotes();
        PopulateIntersectionNotes();
      }

    }

    private void PopulateProjectNotes()
    {
      projectNotesPanel.Children.Clear();
      NoteContainer projectNotesContainer = new NoteContainer(ref m_Project.m_Notes, NoteType.ProjectLevel, "", m_CurrentUser);
      projectNotesPanel.Children.Add(projectNotesContainer);
    }

    private void PopulateIntersectionNotes()
    {
      intersectionListPanel.Children.Clear();
      foreach (Intersection intersection in m_Project.m_Intersections)
      {
        NoteContainer thisLocNoteContainer = new NoteContainer(ref intersection.m_Notes, NoteType.IntersectionLevel,
          intersection.GetLocationName(), m_CurrentUser);
        intersectionListPanel.Children.Add(thisLocNoteContainer);
        StackPanel countPanel = new StackPanel();
        countPanel.Orientation = Orientation.Vertical;
        countPanel.Margin = new Thickness(12, 0, 0, 0);
        foreach (Count count in intersection.m_Counts)
        {
          string headerString = count.m_Id + "   " + count.GetTimePeriod() + "   " + count.m_FilmDate.ToShortDateString();
          NoteContainer thisCountNoteContainer = new NoteContainer(ref count.m_Notes, NoteType.CountLevel_Conglomerate, headerString, m_CurrentUser);
          countPanel.Children.Add(thisCountNoteContainer);
        }
        intersectionListPanel.Children.Add(countPanel);
        intersectionListPanel.Children.Add(new Separator());
      }
    }

    private void RefreshNoteLists()
    {
      PopulateProjectNotes();
      PopulateIntersectionNotes();
    }

    private void NotesDataTabPrintCount_Click(object sender, RoutedEventArgs e)
    {
      PrintNotes();
    }

    private void PrintNotes()
    {
      MerlinPrintDocument mpd = new MerlinPrintDocument(m_Project);
      mpd.PrintNotes();
    }


    #endregion

    #region Summary Page

    private void summaryTab_Loaded(object sender, RoutedEventArgs e)
    {
      summaryTabDetails.FontSize = 24;
      summaryTabData.FontSize = 24;
      summaryTabBalancing.FontSize = 24;
      summaryTabFlags.FontSize = 24;
      summaryTabExport.FontSize = 24;
    }

    private void summaryTab_Selected(object sender, MouseButtonEventArgs e)
    {
      ClearStacks();
      ClearFlagStacks();
      PopulateSummaryStates();
      generateExcelButton.IsEnabled = m_Project.m_TCCDataFileRules;
    }

    private void RefreshStates_Click(object sender, RoutedEventArgs e)
    {
      PopulateSummaryStates();
    }

    private void PopulateSummaryStates()
    {
      //Project Details
      summaryTabDetails.Text = "Ok";
      summaryTabDetails.Foreground = new SolidColorBrush(Colors.ForestGreen);

      //Data
      switch (m_Project.m_ProjectDataState)
      {
        case DataState.Empty:
          summaryTabData.Text = "No Counts Contain Data";
          summaryTabData.Foreground = new SolidColorBrush(Colors.Red);
          break;
        case DataState.Partial:
          summaryTabData.Text = "Some Counts Contain Some Data";
          summaryTabData.Foreground = new SolidColorBrush(Colors.Goldenrod);
          break;
        default:
          summaryTabData.Text = "All Counts Contain At Least Some Data";
          summaryTabData.Foreground = new SolidColorBrush(Colors.ForestGreen);
          break;
      }

      //Balancing
      if (m_Project == null || m_Project.m_Intersections.Count < 2 || !m_Project.AnyCountsHaveSameTimePeriodAndDate())
      {
        //project null or only one intersection or no time period has more than one count
        summaryTabBalancing.Text = "N/A";
        summaryTabBalancing.Foreground = new SolidColorBrush(Colors.ForestGreen);
      }
      else if (GetCurrentBalancingModules().Count() == 0)
      {
        summaryTabBalancing.Text = "No Relationships Set";
        summaryTabBalancing.Foreground = new SolidColorBrush(Colors.Red);
      }
      else
      {
        if (m_Project.m_ProjectDataState == DataState.Empty)
        {
          summaryTabBalancing.Text = "Relationships Set; No Data To Evaluate";
          summaryTabBalancing.Foreground = new SolidColorBrush(Colors.Goldenrod);
        }
        else
        {
          summaryTabBalancing.Text = "All Neighbors Balance";
          summaryTabBalancing.Foreground = new SolidColorBrush(Colors.ForestGreen);
          IEnumerable<RadioButton> tpRadios = BalancingTabTPRadiosWrapPanel.Children.OfType<RadioButton>().Where(x => x.Visibility == Visibility.Visible);
          RadioButton currentSelected = GetSelectedBalancingRadioButton(true);
          for (int i = 0; i < tpRadios.Count(); i++)
          {
            tpRadios.ElementAt(i).IsChecked = true;
            if (!DoAllDisplayedNeighborsBalance())
            {
              summaryTabBalancing.Text = "At Least One Count Doesn't Balance";
              summaryTabBalancing.Foreground = new SolidColorBrush(Colors.Goldenrod);
              break;
            }
          }
          currentSelected.IsChecked = true;
        }
      }

      //Flags
      switch (m_CurrentState.m_FlagTabState)
      {
        case FlagTabState.Empty:
          summaryTabFlags.Text = "Flags Have Not Been Generated";
          summaryTabFlags.Foreground = new SolidColorBrush(Colors.Red);
          break;
        case FlagTabState.Generated:
          summaryTabFlags.Text = "Flags Generated";
          summaryTabFlags.Foreground = new SolidColorBrush(Colors.Goldenrod);
          break;
        case FlagTabState.Accepted:
          summaryTabFlags.Text = "All Current Flags Acceptable";
          summaryTabFlags.Foreground = new SolidColorBrush(Colors.ForestGreen);
          break;
      }

      CheckFileExport();
      //Export
      switch (m_CurrentState.m_ExportState)
      {
        case ExportState.None:
          summaryTabExport.Text = "No counts found in ASCII Folder";
          summaryTabExport.Foreground = new SolidColorBrush(Colors.Red);
          break;
        case ExportState.Some:
          summaryTabExport.Text = "Some counts found in ASCII Folder";
          summaryTabExport.Foreground = new SolidColorBrush(Colors.Goldenrod);
          break;
        case ExportState.All:
          summaryTabExport.Text = "An ASCII File has been generated for each count";
          summaryTabExport.Foreground = new SolidColorBrush(Colors.ForestGreen);
          break;
      }
    }

    private void CheckFileExport()
    {
      bool networkAsciiDirectoryExists = false;
      bool localAsciiDirectoryExists = false;
      List<string> foundFilesForTheseSiteCodes = new List<string>();

      if (m_Project != null && m_Project.m_Prefs.m_NetworkAsciiDirectory != null
        && Directory.Exists(m_Project.m_Prefs.m_NetworkAsciiDirectory))
      {
        if (Directory.Exists(m_Project.m_Prefs.m_NetworkAsciiDirectory + "\\"
            + m_Project.m_OrderNumber))
        {
          networkAsciiDirectoryExists = true;
          string[] files = Directory.GetFiles(m_Project.m_Prefs.m_NetworkAsciiDirectory + "\\"
            + m_Project.m_OrderNumber, "*" + m_Project.m_OrderNumber + "*.txt");
          foreach (string f in files)
          {
            string trimmedF = f.Split('\\')[f.Split('\\').Length - 1];
            if (!foundFilesForTheseSiteCodes.Contains(trimmedF.Split('.')[0]))
            {
              foundFilesForTheseSiteCodes.Add(trimmedF.Split('.')[0]);
            }
          }
        }
      }

      if (m_Project != null && m_Project.m_Prefs.m_LocalAsciiDirectory != null
         && Directory.Exists(m_Project.m_Prefs.m_LocalAsciiDirectory))
      {
        if (Directory.Exists(m_Project.m_Prefs.m_LocalAsciiDirectory + "\\"
            + m_Project.m_OrderNumber))
        {
          localAsciiDirectoryExists = true;
          string[] files = Directory.GetFiles(m_Project.m_Prefs.m_LocalAsciiDirectory + "\\"
            + m_Project.m_OrderNumber, "*" + m_Project.m_OrderNumber + "*.txt");
          foreach (string f in files)
          {
            string trimmedF = f.Split('\\')[f.Split('\\').Length - 1];
            if (!foundFilesForTheseSiteCodes.Contains(trimmedF.Split('.')[0]))
            {
              foundFilesForTheseSiteCodes.Add(trimmedF.Split('.')[0]);
            }
          }
        }
      }

      if ((!networkAsciiDirectoryExists && !localAsciiDirectoryExists) || foundFilesForTheseSiteCodes.Count < 1)
      {
        m_CurrentState.m_ExportState = ExportState.None;
      }
      else if (foundFilesForTheseSiteCodes.Count < m_Project.GetNumberOfCounts())
      {
        m_CurrentState.m_ExportState = ExportState.Some;
      }
      else if (foundFilesForTheseSiteCodes.Count == m_Project.GetNumberOfCounts())
      {
        m_CurrentState.m_ExportState = ExportState.All;
      }

    }

    private bool DoAllDisplayedNeighborsBalance()
    {
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        foreach (TextBlock tb in bm.balancingTextBlocks.Values)
        {
          if (((SolidColorBrush)tb.Foreground).Color == Colors.Red)
          {
            return false;
          }
        }
      }
      return true;
    }

    private void summaryTabExport_Click(object sender, RoutedEventArgs e)
    {
      CountExportSelection ces = new CountExportSelection(m_Project);
      ces.Owner = this;
      bool? result = ces.ShowDialog();
      if (result == false)
      {
        return;
      }
      var defaultResult = MessageBox.Show("Use Default ASCII File Location?", "Default Location", MessageBoxButton.YesNo);
      if (defaultResult == MessageBoxResult.Yes)
      {
        if (ExportCounts(ces.countsToExport))
        {
          MessageBox.Show("Selected Counts Exported.", "Export Complete",
          MessageBoxButton.OK);
        }
      }
      else
      {
        System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.DialogResult fdResult = fd.ShowDialog();
        string savedPath;
        if (fdResult == System.Windows.Forms.DialogResult.OK)
        {
          //Warning... Major hack to follow...
          savedPath = m_Project.m_Prefs.m_NetworkAsciiDirectory;
          m_Project.m_Prefs.m_NetworkAsciiDirectory = fd.SelectedPath.ToString();
          if (ExportCounts(ces.countsToExport))
          {
            MessageBox.Show("Selected Counts Exported.", "Export Complete",
            MessageBoxButton.OK);
          }
          m_Project.m_Prefs.m_NetworkAsciiDirectory = savedPath;
        }
      }

      PopulateSummaryStates();

    }

    private void summaryTabExportAll_Click(object sender, RoutedEventArgs e)
    {
      var defaultResult = MessageBox.Show("Use Default ASCII File Location?", "Default Location", MessageBoxButton.YesNo);
      if (defaultResult == MessageBoxResult.Yes)
      {
        if (ExportCounts())
        {
          PopulateSummaryStates();
          MessageBox.Show("Counts Exported.", "Export Complete",
          MessageBoxButton.OK);
        }
      }
      else
      {
        System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.DialogResult fdResult = fd.ShowDialog();
        string savedPath;
        if (fdResult == System.Windows.Forms.DialogResult.OK)
        {
          //Warning... Major hack to follow...
          savedPath = m_Project.m_Prefs.m_NetworkAsciiDirectory;
          m_Project.m_Prefs.m_NetworkAsciiDirectory = fd.SelectedPath.ToString();
          if (ExportCounts())
          {
            PopulateSummaryStates();
            MessageBox.Show("Counts Exported.", "Export Complete",
            MessageBoxButton.OK);
          }
          m_Project.m_Prefs.m_NetworkAsciiDirectory = savedPath;
        }
      }
    }

    private void summaryTabExportConvert_Click(object sender, RoutedEventArgs e)
    {
      var defaultResult = MessageBox.Show("Use Default ASCII File Location?", "Default Location", MessageBoxButton.YesNo);
      if (defaultResult == MessageBoxResult.Yes)
      {
        if (ExportCounts())
        {
          PopulateSummaryStates();
          MessageBox.Show("Counts Exported \nRunning Conversion Wizard.", "Export Complete",
          MessageBoxButton.OK);
          RunConversionWizard();
        }
      }
      else
      {
        System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.DialogResult fdResult = fd.ShowDialog();
        string savedPath;
        if (fdResult == System.Windows.Forms.DialogResult.OK)
        {
          //Warning... Major hack to follow...
          savedPath = m_Project.m_Prefs.m_NetworkAsciiDirectory;
          m_Project.m_Prefs.m_NetworkAsciiDirectory = fd.SelectedPath.ToString();
          if (ExportCounts())
          {
            PopulateSummaryStates();
            MessageBox.Show("Counts Exported \nRunning Conversion Wizard.", "Export Complete",
            MessageBoxButton.OK);
            RunConversionWizard();
          }
          m_Project.m_Prefs.m_NetworkAsciiDirectory = savedPath;
        }
      }
    }

    private void summaryTabSummaryFile_Click(object sender, RoutedEventArgs e)
    {
      ShowModuleNotPurchasedDialog();
    }

    private void generateExcelButton_Click(object sender, RoutedEventArgs e)
    {
      var generateAllResult = MessageBox.Show("Generate Excel deliverables for all counts?", "Default Location", MessageBoxButton.YesNo);
      bool generateAll = generateAllResult == MessageBoxResult.Yes;
      List<Count> countsToGenerate;
      if (generateAll)
      {
        countsToGenerate = m_Project.GetAllTmcCounts();
      }
      else
      {
        CountExportSelection ces = new CountExportSelection(m_Project);
        ces.Owner = this;
        bool? result = ces.ShowDialog();
        if (result == false)
        {
          return;
        }
        countsToGenerate = ces.countsToExport;
      }
      var defaultResult = MessageBox.Show("Use Default ASCII/Excel Deliverable File Location?", "Default Location", MessageBoxButton.YesNo);
      if (defaultResult == MessageBoxResult.Yes)
      {
        if (ExportCounts(countsToGenerate, true))
        {
          MessageBox.Show("Excel deliverable files generated for the selected counts.", "Export Complete",
          MessageBoxButton.OK);
        }
      }
      else
      {
        System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.DialogResult fdResult = fd.ShowDialog();
        string savedPath;
        if (fdResult == System.Windows.Forms.DialogResult.OK)
        {
          //Warning... Major hack to follow...
          savedPath = m_Project.m_Prefs.m_NetworkAsciiDirectory;
          m_Project.m_Prefs.m_NetworkAsciiDirectory = fd.SelectedPath.ToString();
          if (ExportCounts(countsToGenerate, true))
          {
            MessageBox.Show("Excel deliverable files generated for the selected counts.", "Export Complete",
            MessageBoxButton.OK);
          }
          m_Project.m_Prefs.m_NetworkAsciiDirectory = savedPath;
        }
      }
    }

    private void summaryTabConvert_Click(object sender, RoutedEventArgs e)
    {
      RunConversionWizard();
    }

    private void GenerateExcelDeliverables()
    {
      foreach (Count cnt in m_Project.GetAllTmcCounts())
      {
        cnt.GenerateExcelDeliverable(@"C:\");
      }
    }

    #endregion

    private void tubeDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
      if(tubeDate.SelectedDate == null)
      {
        return;
      }
      matchTMCsCheckBox.IsChecked = false;
      RefreshBalancingTotals();
    }

    private void showDatesCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.DatesShowing = true;
      }
    }

    private void showDatesCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      foreach (BalancingModule bm in GetCurrentBalancingModules())
      {
        bm.DatesShowing = false;
      }
    }

    private void SeparateTubeOrderNumCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      ApplyOrderNumToTubeSurveysInGUI(TubeOrderNumTextBox.Text);
    }

    private void SeparateTubeOrderNumCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      ApplyOrderNumToTubeSurveysInGUI(OrderNumTextBox.Text);
    }

    private void matchTMCsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      if(BalancingTab.IsSelected == false)
      {
        return;
      }
      tubeDate.SelectedDate = null;
      RefreshBalancingTotals();
    }

    private void matchTMCsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (BalancingTab.IsSelected == false)
      {
        return;
      }
      RefreshBalancingTotals();
    }

    private void DiffTubeOrderText_MouseDown(object sender, MouseButtonEventArgs e)
    {
      SeparateTubeOrderNumCheckBox.IsChecked = !SeparateTubeOrderNumCheckBox.IsChecked;
    }

    private void TubeSurveyTimeCopy_Click(object sender, RoutedEventArgs e)
    {
      TubeTimePeriodUI source = e.OriginalSource as TubeTimePeriodUI;

      //find the index in the list of the clicked survey time so that that time can be copied to the survey in the same position in all tube locations
      int tpIndex = -1;
      foreach(TubeLocationModule tlm in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        if(tlm.surveyTimesWrapPanel.Children.OfType<TubeTimePeriodUI>().Contains(source))
        {
          tpIndex = tlm.surveyTimesWrapPanel.Children.IndexOf(source);
          break;
        }
      }
      //copy time to each location, if it has a survey in the same position as the source survey
      foreach(TubeLocationModule tlm in TubeLocationListBox.Items.OfType<TubeLocationModule>())
      {
        if(tlm.surveyTimesWrapPanel.Children.Count > tpIndex)
        {
          TubeTimePeriodUI tpUI = tlm.surveyTimesWrapPanel.Children[tpIndex] as TubeTimePeriodUI;
          tpUI.SetTimePeriod((DateTime)source.StartTime, (DateTime)source.EndTime);
        }
      }

    }

    private void includeUnclassifiedCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      RefreshBalancingTotals();
    }

    private void includeUnclassifiedCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      RefreshBalancingTotals();
    }

  }
}
