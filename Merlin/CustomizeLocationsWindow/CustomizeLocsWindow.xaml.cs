using AppMerlin;
using Merlin.DetailsTab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Merlin.CustomizeLocationsWindow
{
  /// <summary>
  /// Interaction logic for CustomizeLocsWindow.xaml
  /// </summary>
  public partial class CustomizeLocsWindow : Window
  {
    MainWindow main; //reference to main window
    public bool bypassClosingMessage = false;
    public bool do_CreateOrEdit = false;
    public bool canBringIntoView = false;
    
    public CustomizeLocsWindow(MainWindow mainW)
    {
      InitializeComponent();

      main = mainW;

      //lists all time periods active for the project at the top of the window
      ListTimePeriodsAtTop(main);

      //populates list of CustomizeLocationModules
      foreach (LocationModule loc in (main.LocationListBox.Items).OfType<LocationModule>())
      {
        if(loc.Visibility == Visibility.Visible)
        {
          CustomizeLocationModule newModule = new CustomizeLocationModule(loc, main);
          Thickness margin = Margin;
          margin.Bottom = 5;
          margin.Top = 5;
          newModule.Margin = margin;
          if(CustomizeLocationsList.Items.Count % 2 == 0)
          {
            newModule.Background = new SolidColorBrush(Colors.Beige);
          }
          else
          {
            newModule.Background = new SolidColorBrush(Colors.BlanchedAlmond);
          }
          CustomizeLocationsList.Items.Add(newModule);

          //in editing or viewing mode, populates location details for counts that already exist in the project
          if (main.m_CurrentState.m_DetailsTabState != DetailsTabState.Creating && main.LocationListBox.Items.IndexOf(loc) < main.m_Project.m_Intersections.Count)
          {
            //Set diagram for each intersection from internal project
            IntersectionApproach sbApproach = main.m_Project.m_Intersections[main.LocationListBox.Items.IndexOf(loc)].m_ApproachesInThisIntersection[1];
            IntersectionApproach wbApproach = main.m_Project.m_Intersections[main.LocationListBox.Items.IndexOf(loc)].m_ApproachesInThisIntersection[3];
            IntersectionApproach nbApproach = main.m_Project.m_Intersections[main.LocationListBox.Items.IndexOf(loc)].m_ApproachesInThisIntersection[0];
            IntersectionApproach ebApproach = main.m_Project.m_Intersections[main.LocationListBox.Items.IndexOf(loc)].m_ApproachesInThisIntersection[2];
            newModule.diagram.SetLegFlows(sbApproach.TrafficFlowType, wbApproach.TrafficFlowType, nbApproach.TrafficFlowType, ebApproach.TrafficFlowType);
            //Assign site codes and dates from internal project for each count
            foreach(Count count in main.m_Project.m_Intersections[main.LocationListBox.Items.IndexOf(loc)].m_Counts)
            {
              foreach(TimePeriod tp in newModule.TimePeriodsPanel.Children.OfType<TimePeriod>())
              {
                AssignDateAndSiteCodeFromInternalProject(tp, (int)tp.Tag, count);
              }
            }
            foreach(TimePeriod tp in newModule.TimePeriodsPanel.Children.OfType<TimePeriod>())
            {
              if (tp.SiteCode.Text == "" && tp.CountDate.SelectedDate == null)
              {
                tp.isActiveCheckBox.IsChecked = false;
              }
            }
          }
        }
      }
      //In create new project mode
      if (main.m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
      {
        //assigns site codes
        CalculateSiteCodes(main);
      }
      if(main.m_CurrentState.m_DetailsTabState == DetailsTabState.Viewing)
      {
        RegenerateSideCodes.Visibility = Visibility.Collapsed;
      }
    }

    //helper function to assign internal date and site code to second screen time period in edit mode (if it exists)
    private void AssignDateAndSiteCodeFromInternalProject(TimePeriod tp, int tpIndex, Count cnt)
    {
      if (cnt.m_TimePeriodIndex == tpIndex && (bool)tp.isActiveCheckBox.IsChecked)
      {
        tp.SiteCode.Text = cnt.m_Id.Substring(6);
        tp.CountDate.SelectedDate = cnt.m_FilmDate;
      }
    }
    
    //lists time periods and their times at top of window
    public void ListTimePeriodsAtTop(MainWindow main)
    {
      foreach (TimePeriodModule tp in (main.TimePeriodList.Items).OfType<TimePeriodModule>())
      {
        if ((bool)tp.ActiveCheckBox.IsChecked)
        {
          TextBlock tb = new TextBlock();
          StringBuilder sb = new StringBuilder();
          tp.UpdateInternalStartAndEndTimes();
          sb.Append(tp.m_StartTime.ToString("h:mm tt") + " - " + tp.m_EndTime.ToString("h:mm tt"));
          //sb.Append(tp.GetTimePeriod());

          //prints how many days later the end time is if different date from start time
          if (tp.m_StartTime.Date != tp.m_EndTime.Date)
          {
            int numCalendarDatesLater = (tp.m_EndTime.Date - tp.m_StartTime.Date).Days;
            sb.Append("(+" + numCalendarDatesLater + " day");
            if (numCalendarDatesLater > 1)
            {
              sb.Append("s");
            }
            sb.Append(")");
          }

          sb.Append("     ");
          tb.Inlines.Add(new Bold(new Run(tp.TimePeriodLabel.Text + ": ")));
          tb.Inlines.Add(sb.ToString());
          //tb.Text = sb.ToString();
          ListOfTimePeriods.Children.Add(tb);
        }
      }

    }

    /// <summary>
    /// Calculates site codes in customize locations window. AM & PM only will alternate, otherwise assigns all locations in first
    /// time period, then all locations in the next time period, etc.
    /// </summary>
    /// <param name="main">Main window</param>
    private void CalculateSiteCodes(MainWindow main)
    {
      int siteCodeSuffix = 1;

      //Iterate through every location
      foreach (CustomizeLocationModule loc in CustomizeLocationsList.Items)
      {
        //In each location, iterate through all time periods
        foreach (TimePeriod tp in loc.TimePeriodsPanel.Children.OfType<TimePeriod>())
        {
          //try to assign the site code (if count doesn't exist for current time period, AssignSiteCode won't do anything)
          AssignSiteCode(ref siteCodeSuffix, tp, main);
        }
      }

      //int siteCodeSuffix = 1;
      //bool isAMandPMonly = true;

      ////tests if time period other than AM or PM is active
      //foreach (TimePeriodModule tp in (main.TimePeriodList.Items).OfType<TimePeriodModule>())
      //{
      //  if((bool)tp.ActiveCheckBox.IsChecked && main.TimePeriodList.Items.IndexOf(tp) != 0 && main.TimePeriodList.Items.IndexOf(tp) != 2)
      //  {
      //    isAMandPMonly = false;
      //    break;
      //  }
      //}

      //if(isAMandPMonly)
      //{
      //  foreach (CustomizeLocationModule loc in CustomizeLocationsList.Items)
      //  {
      //    AssignSiteCode(ref siteCodeSuffix, loc.AM, main);
      //    AssignSiteCode(ref siteCodeSuffix, loc.PM, main);
      //  }
      //}
      //else
      //{
      //  //Iterate through every active time period
      //  foreach (TimePeriodModule tp in (main.TimePeriodList.Items).OfType<TimePeriodModule>().Where(x => (bool)x.ActiveCheckBox.IsChecked))
      //  {
      //    //Iterate through every count in this time period and assign site codes
      //    foreach (CustomizeLocationModule loc in CustomizeLocationsList.Items)
      //    {
      //      switch(main.TimePeriodList.Items.IndexOf(tp))
      //      {
      //        case 0:
      //          AssignSiteCode(ref siteCodeSuffix, loc.AM, main);
      //          break;
      //        case 1:
      //          AssignSiteCode(ref siteCodeSuffix, loc.MID, main);
      //          break;
      //        case 2:
      //          AssignSiteCode(ref siteCodeSuffix, loc.PM, main);
      //          break;
      //        case 3:
      //          AssignSiteCode(ref siteCodeSuffix, loc.C1, main);
      //          break;
      //        case 4:
      //          AssignSiteCode(ref siteCodeSuffix, loc.C2, main);
      //          break;
      //        case 5:
      //          AssignSiteCode(ref siteCodeSuffix, loc.C3, main);
      //          break;
      //        case 6:
      //          AssignSiteCode(ref siteCodeSuffix, loc.C4, main);
      //          break;
      //        case 7:
      //          AssignSiteCode(ref siteCodeSuffix, loc.C5, main);
      //          break;
      //        default:
      //          AssignSiteCode(ref siteCodeSuffix, loc.AM, main);
      //          break;
      //      }
      //    }
      //  }
      //}
    }
    
    //helper for CalculateSiteCodes
    private void AssignSiteCode(ref int currentSiteCode, TimePeriod tp, MainWindow mainPage)
    {
      if ((bool)tp.isActiveCheckBox.IsChecked)
      {
        tp.SiteCode.Text = (currentSiteCode++).ToString("D2");
      }
    }

    private void cancel_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void saveAndFinish_Click(object sender, RoutedEventArgs e)
    {
      StringBuilder errors = ValidateCustomizeLocationsWindow();
      if (errors.Length == 0)
      {
        //No errors, create project if in creating project mode
        if (main.m_CurrentState.m_DetailsTabState == DetailsTabState.Creating)
        {
          do_CreateOrEdit = true;
        }
        //No errors, modify project, program is in edit project state
        else if (main.m_CurrentState.m_DetailsTabState == DetailsTabState.Editing)
        {
          MessageBoxResult result = MessageBox.Show("Any changes made will be reflected in the current project. Reduction of time periods, removal of counts, removing a bank, or unselecting RTOR or U-Turns will cause all information associated with that data to be deleted.\n\nProceed?",
            "Confirm Project Edits", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
          if (result == MessageBoxResult.OK)
          {
            do_CreateOrEdit = true;
          }
          else
          {
            return;
          }
        }
        bypassClosingMessage = true;
        this.Close();
        bypassClosingMessage = false;
      }
      else
      {
        //There are errors, show them and don't proceed
        MessageBox.Show("Correct the following errors:\n\n" + errors, "Can't Proceed", MessageBoxButton.OK, MessageBoxImage.Hand);
      }
      
    }
    
    public StringBuilder ValidateCustomizeLocationsWindow()
    {
      StringBuilder errorText = new StringBuilder();
      //Stores count info ready to print in case of duplicate site code error, site code for every count
      Dictionary<string, string> siteCodeInventory = new Dictionary<string, string>();
      //Dictionary of time period index/bools for each time period in the project which tracks whether that time period has been assigned at least one count
      Dictionary<int, bool> timePeriods_AtLeastOneCount = new Dictionary<int, bool>();
      //initialize to false
      foreach(TimePeriod tp in ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(0)).TimePeriodsPanel.Children.OfType<TimePeriod>())
      {
        timePeriods_AtLeastOneCount.Add((int)tp.Tag, false);
      }
      foreach (CustomizeLocationModule loc in CustomizeLocationsList.Items)
      {
        bool LocHasAtLeastOneTP = false;
        foreach (TimePeriod tp in loc.TimePeriodsPanel.Children.OfType<TimePeriod>())
        {
          if ((bool)tp.isActiveCheckBox.IsChecked)
          {
            //the time period this count is assigned to has at least one count assigned to it
            timePeriods_AtLeastOneCount[(int)tp.Tag] = true;
            LocHasAtLeastOneTP = true;
            //runs date and site code validation tests for each active time period for each location
            ValidationTests(errorText, tp, loc, siteCodeInventory);
          }
        }
        //ensures the current location has at least one time period selected
        if (!LocHasAtLeastOneTP)
        {
          errorText.AppendLine("\u2022 " + loc.IntersectionTitle.Text + ": No Time Periods Selected");
        }
      }
      DuplicateSiteCodeTests(siteCodeInventory, errorText);

      //ensures every time period has at least one count
      foreach(var entry in timePeriods_AtLeastOneCount)
      {
        if(entry.Value == false)
        {
          errorText.AppendLine("\u2022 " + ((TimePeriod)((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(0)).TimePeriodsPanel.Children.OfType<TimePeriod>().First(x => (int)x.Tag == entry.Key)).TimePeriodText.Text + " Time Period Not Assigned To Any Counts");
        }
      }

      return errorText;
    }

    //tests a count
    private void ValidationTests(StringBuilder errorList, TimePeriod tp, CustomizeLocationModule parentLoc, Dictionary<string, string> siteCodeList)
    {
      string offendingCount = "\u2022 " + parentLoc.IntersectionTitle.Text + ", " + tp.TimePeriodText.Text + " count: ";
      
      //invalid date
      if(tp.CountDate.SelectedDate == null)
      {
        errorList.AppendLine(offendingCount + "Invalid Date");
      }
      //invalid site code
      if(string.IsNullOrWhiteSpace(tp.SiteCode.Text))
      {
        errorList.AppendLine(offendingCount + "Invalid Site Code");
      }
      //add count to site code dictionary for the duplicate site code test
      siteCodeList.Add("\t\u25E6 " + parentLoc.IntersectionTitle.Text + ", " + tp.TimePeriodText.Text + " count", tp.SiteCode.Text);
    }
    
    private void DuplicateSiteCodeTests(Dictionary<string, string> siteCodeList, StringBuilder errors)
    {
      var duplicateValues = siteCodeList.GroupBy(x => x.Value).Where(x => x.Count() > 1);
      
      foreach(var repeatedSiteCode in duplicateValues)
      {
        errors.AppendLine("\u2022 " + "The site code " + repeatedSiteCode.Key + " is assigned to multiple counts:");
        foreach(var offendingCount in siteCodeList)
        {
          if(offendingCount.Value == repeatedSiteCode.Key)
          {
            errors.AppendLine(offendingCount.Key);
          }
        }
      }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if(!bypassClosingMessage && main.m_CurrentState.m_DetailsTabState != DetailsTabState.Viewing)
      {
        MessageBoxResult result = MessageBox.Show("Are you sure you want to go back? Any changes made in this window will be lost.",
          "Close Window", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (result == MessageBoxResult.OK)
        {

        }
        else
        {
          e.Cancel = true;
        }
      }
    }

    #region Project Creation

    ////Creates project from scratch -- this is now done in the main window
    //public void CreateProjectFromScratch()
    //{
    //  //try
    //  //{
    //    List<string> timePeriods = new List<string>();
    //    List<string> timePeriodLabels = new List<string>();
    //    //loops through list of TimePeriodModules to populate list of time periods
    //    SetTPsFromTPList(timePeriods, timePeriodLabels);

    //    //Populate list of banks
    //    List<string> banks = new List<string>();
    //    banks.Add(main.Bank0.Text);
    //    banks.Add(main.Bank1.Text);
    //    banks.Add(main.Bank2.Text);
    //    banks.Add(main.Bank3.Text);
    //    banks.Add(main.Bank4.Text);
    //    banks.Add(main.Bank5.Text);

    //    main.m_Project = new TMCProject(main.OrderNumTextBox.Text, main.m_DefaultPreferences, main.ProjectNameTextBox.Text, timePeriods, timePeriodLabels, banks, (bool)main.isRTORCheckBox.IsChecked, (bool)main.countUTurnCheckBox.IsChecked);

    //    //Updates window title
    //    main.UpdateWindowTitle(this);

    //    //constructs intersections and counts for m_Project
    //    PopulateLoadedProjectIntersectionsAndCounts();

    //    main.ChangeDetailsTabState(DetailsTabState.Viewing);
    //    main.m_CurrentState.m_ProjectState = ProjectState.Loaded;

    //    bypassClosingMessage = true;
    //    this.Close();
    //    bypassClosingMessage = false;
    //    main.m_CurrentState.m_DataTabState = DataTabState.Empty;


    //  //}
    //  //catch
    //  //{
    //  //  MessageBox.Show("There was a problem saving the project.", "Error", MessageBoxButton.OK);
    //  //}
    //}

    //Helper function for CreateProjectFromScratch() and ApplyEditsToProject()
    public void CreateIntersectionsAndCounts()
    {
      CustomizeLocationModule loc;
      for (int i = main.m_Project.m_Intersections.Count; i < CustomizeLocationsList.Items.Count; i++)
      {
        loc = (CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i);

        //creates the intersection
        Intersection thisIntersection = new Intersection(
          loc.CustomizeModuleNBSB, loc.diagram.GetLegFlow(StandardIntersectionApproaches.SB), 
          loc.CustomizeModuleNBSB, loc.diagram.GetLegFlow(StandardIntersectionApproaches.NB), 
          loc.CustomizeModuleEBWB, loc.diagram.GetLegFlow(StandardIntersectionApproaches.WB),
          loc.CustomizeModuleEBWB, loc.diagram.GetLegFlow(StandardIntersectionApproaches.EB), 
          main.m_Project, false, -1, "", "");

        List<Count> countsInThisIntersection = new List<Count>();

        //Adds count for each checked time period of the intersection
        foreach(TimePeriod tp in loc.TimePeriodsPanel.Children.OfType<TimePeriod>())
        {
          if((bool)tp.isActiveCheckBox.IsChecked)
          {
            countsInThisIntersection.Add(CountGenerator((int)tp.Tag, thisIntersection, tp));
          }
        }

        //assigns list of counts we just made as this intersection's m_Counts
        thisIntersection.m_Counts = countsInThisIntersection;

        //adds this intersection to loaded project intersections
        main.m_Project.m_Intersections.Add(thisIntersection);

      }

    }

    //Helper function for CreateProjectFromScratch()
    public void SetTPsFromTPList(List<string> internalTPs, List<string> internalTPLabels)
    {
      int numIterations = main.TimePeriodList.Items.OfType<TimePeriodModule>().Count();
      internalTPs.Clear();
      internalTPLabels.Clear();
      for (int i = 0; i < numIterations; i++)
      {
        TimePeriodModule currentTimePeriod = ((TimePeriodModule)main.TimePeriodList.Items.GetItemAt(i));
        if (currentTimePeriod.ActiveCheckBox.IsChecked == true)
        {
          internalTPs.Add(currentTimePeriod.GetTimePeriod());
          internalTPLabels.Add(currentTimePeriod.TimePeriodLabel.Text);
        }
        else
        {
          internalTPs.Add("NOT USED");
          internalTPLabels.Add(null);
        }
      }
    }

    //helper function for PopulateLoadedProjectIntersectionsAndCounts() to generate counts for a particular intersection
    public Count CountGenerator(int TimePeriodListIndex, Intersection parent, TimePeriod locModuleCount)
    {
      return new Count(locModuleCount.OrderNumForSiteCode.Text + locModuleCount.SiteCode.Text, ((TimePeriodModule)main.TimePeriodList.Items.GetItemAt(TimePeriodListIndex)).GetTimePeriod(), TimePeriodListIndex, parent, (DateTime)locModuleCount.CountDate.SelectedDate);
    }

    #endregion

    #region Project Editing

    ////edits the project -- this is now done in the main window
    //public void ApplyEditsToProject()
    //{
    //  try
    //  {
    //    //Update internal project from the project details section
    //    main.m_Project.m_OrderNumber = main.OrderNumTextBox.Text;
    //    main.m_Project.m_ProjectName = main.ProjectNameTextBox.Text;
    //    main.m_Project.m_IsRTOR = (bool)main.isRTORCheckBox.IsChecked;
    //    main.m_Project.m_IsUTurn = (bool)main.countUTurnCheckBox.IsChecked;
    //    main.m_Project.m_Banks[0] = main.Bank0.Text;
    //    main.m_Project.m_Banks[1] = main.Bank1.Text;
    //    main.m_Project.m_Banks[2] = main.Bank2.Text;
    //    main.m_Project.m_Banks[3] = main.Bank3.Text;
    //    main.m_Project.m_Banks[4] = main.Bank4.Text;
    //    main.m_Project.m_Banks[5] = main.Bank5.Text;

    //    //Updates the internal active time periods
    //    SetTPsFromTPList(main.m_Project.m_TimePeriods, main.m_Project.m_TimePeriodLabels);

    //    //Deletes existing intersections that were "deleted" in the GUI (visibility collapsed)
    //    for (int i = main.m_Project.m_Intersections.Count - 1; i >= 0; i--)
    //    {
    //      if(((LocationModule)main.LocationListBox.Items.GetItemAt(i)).Visibility != Visibility.Visible)
    //      {
    //        Intersection intersectionToDelete = main.m_Project.m_Intersections.ElementAt(i);
    //        BalancingModule bmToDelete = main.GetBalancingModule(intersectionToDelete);
    //        //remove corresponding BalancingModule from grid, if it exists
    //        if(bmToDelete != null)
    //        {
    //          main.RemoveBalancingModuleFromGrid(bmToDelete, true);
    //        }

    //        //remove from internal project
    //        main.m_Project.m_Intersections.RemoveAt(i);
    //      }
    //    }

    //    //Adds & removes counts within existing intersections based on changes made in GUI, modifies existing intersection info
    //    for(int i = 0; i < main.m_Project.m_Intersections.Count; i++)
    //    {
    //      //applies street names, approach types, and regenerates possible movements in case those were changed by user
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[0].ApproachName = ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).NBSB.Text;
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[1].ApproachName = ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).NBSB.Text;
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[2].ApproachName = ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).EBWB.Text;
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[3].ApproachName = ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).EBWB.Text;
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[0].TrafficFlowType = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.NB);
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[1].TrafficFlowType = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.SB);
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[2].TrafficFlowType = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.EB);
    //      main.m_Project.m_Intersections[i].m_ApproachesInThisIntersection[3].TrafficFlowType = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).diagram.GetLegFlow(StandardIntersectionApproaches.WB);
    //      main.m_Project.m_Intersections[i].m_MovementsInThisIntersection.Clear();
    //      main.m_Project.m_Intersections[i].GenerateMovements();

    //      List<bool> TPsContainedInCurrentInternalIntersection = new List<bool>() { false, false, false, false, false, false, false, false };
    //      //Iterate through counts in the current intersection
    //      for (int j = main.m_Project.m_Intersections[i].m_Counts.Count - 1; j >= 0; j--)
    //      {
    //        //Deletes count if found in internal project but not active in GUI, else count remains and date, site code, and intervals range (if different) are updated
    //        Count currentInternalCount = main.m_Project.m_Intersections[i].m_Counts[j];
    //        TimePeriod currentGUITimePeriod;

    //        switch(currentInternalCount.m_TimePeriod)
    //        {
    //          case TimePeriodName.AM:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).AM;
    //            break;
    //          case TimePeriodName.MID:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).MID;
    //            break;
    //          case TimePeriodName.PM:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).PM;
    //            break;
    //          case TimePeriodName.C1:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).C1;
    //            break;
    //          case TimePeriodName.C2:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).C2;
    //            break;
    //          case TimePeriodName.C3:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).C3;
    //            break;
    //          case TimePeriodName.C4:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).C4;
    //            break;
    //          case TimePeriodName.C5:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).C5;
    //            break;
    //          default:
    //            currentGUITimePeriod = ((CustomizeLocationModule)CustomizeLocationsList.Items.GetItemAt(i)).AM;
    //            break;
    //        }

    //        //internal count exists but unselected in GUI, delete
    //        if (currentGUITimePeriod.Visibility != Visibility.Visible)
    //        {
    //          main.m_Project.m_Intersections[i].m_Counts.RemoveAt(j);
    //          TPsContainedInCurrentInternalIntersection[j] = false;
    //        }
    //        //Count remains, updates date, site code, and time period range (if different)
    //        else
    //        {
    //          currentInternalCount.m_FilmDate = (DateTime)currentGUITimePeriod.CountDate.SelectedDate;
    //          currentInternalCount.m_Id = currentGUITimePeriod.OrderNumForSiteCode.Text + currentGUITimePeriod.SiteCode.Text;
    //          string fullTimePeriod = ((TimePeriodModule)main.TimePeriodList.Items.GetItemAt(((int)currentInternalCount.m_TimePeriod))).GetTimePeriod();
    //          currentInternalCount.m_StartTime = fullTimePeriod.Split('-')[0];
    //          currentInternalCount.m_EndTime = fullTimePeriod.Split('-')[1];
    //          currentInternalCount.m_NumIntervals = currentInternalCount.CalculateNumberOfIntervals();
    //          //Add_RemoveDataIntervals(currentInternalCount);

    //          TPsContainedInCurrentInternalIntersection[j] = true;
    //        }
    //      }
    //      //Adds counts that are active in GUI but don't yet exist in internal project
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).AM_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).AM_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[0], 0, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).AM, TimePeriodName.AM);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).MID_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).MID_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[1], 1, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).MID, TimePeriodName.MID);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).PM_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).PM_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[2], 2, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).PM, TimePeriodName.PM);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C1_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C1_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[3], 3, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).C1, TimePeriodName.C1);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C2_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C2_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[4], 4, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).C2, TimePeriodName.C2);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C3_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C3_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[5], 5, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).C3, TimePeriodName.C3);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C4_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C4_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[6], 6, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).C4, TimePeriodName.C4);
    //      AddInternalCountIfDoesntExist((bool)((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C5_isEnabled.IsChecked && ((LocationModule)main.LocationListBox.Items.GetItemAt(i)).C5_isEnabled.Visibility == Visibility.Visible, TPsContainedInCurrentInternalIntersection[7], 7, i, ((CustomizeLocationModule)(CustomizeLocationsList.Items.GetItemAt(i))).C5, TimePeriodName.C5);
    //    }
    //    //The remaining locations and their child counts in locationslistbox/customizelocationslist are new, add to internal project
    //    PopulateLoadedProjectIntersectionsAndCounts();

    //    main.ChangeDetailsTabState(DetailsTabState.Viewing);
    //    MessageBox.Show("Oh Snap!\n\nProject was successfully edited!", "JR's Thoughts", MessageBoxButton.OK);
    //    bypassClosingMessage = true;
    //    this.Close();
    //    bypassClosingMessage = false;
    //    main.m_CurrentState.m_DataTabState = DataTabState.Empty;

    //  }
    //  catch
    //  {
    //    MessageBox.Show("There was a problem editing the project.", "Error", MessageBoxButton.OK);
    //  }
    //}

    public void AddInternalCountIfDoesntExist(bool doesCountAlreadyExist, int tpIndex, int locIndex, TimePeriod GUItp)
    {
      if ((bool)GUItp.isActiveCheckBox.IsChecked && !doesCountAlreadyExist)
      {
        Count countToAdd = new Count(GUItp.OrderNumForSiteCode.Text + GUItp.SiteCode.Text, ((TimePeriodModule)main.TimePeriodList.Items.GetItemAt(tpIndex)).GetTimePeriod(), tpIndex, main.m_Project.m_Intersections[locIndex], (DateTime)GUItp.CountDate.SelectedDate);
        main.m_Project.m_Intersections[locIndex].m_Counts.Add(countToAdd);
        if(main.m_Project.m_Intersections[locIndex].m_Counts.Count(x => x.m_Id == countToAdd.m_Id) > 1)
        {
          ;
        }
      }
    }


    #endregion

    private void RegenerateSideCodes_Click(object sender, RoutedEventArgs e)
    {
      if(main.m_CurrentState.m_DetailsTabState != DetailsTabState.Viewing)
      {
        CalculateSiteCodes(main);
      }
    }

    private void ProjectListView_OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
      if(canBringIntoView)
      {
        canBringIntoView = false;
      }
      else
      {
        e.Handled = true;
      }
    }

    private void CustomizeLocationsList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Tab)
      {
        //causes tab to go to next control if the overall CustomizeLocationModule got selected in the list
        TraversalRequest tr = new TraversalRequest(FocusNavigationDirection.Next);
        UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;
        do
        {
          if (keyboardFocus != null)
          {
            keyboardFocus.MoveFocus(tr);
          }
          keyboardFocus = Keyboard.FocusedElement as UIElement;
          canBringIntoView = true;
          CustomizeLocationsList.BringIntoView();
        } while (keyboardFocus.GetType() == typeof(ListViewItem));
        e.Handled = true;
      }
    }
  }
}
