using AppMerlin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for BalancingModule.xaml
  /// </summary>
  public partial class BalancingModule : UserControl
  {
    public static Dictionary<TubeLayouts, string> LayoutToFileNameMapping = new Dictionary<TubeLayouts, string>()
    {
      {TubeLayouts.EB_WB, "../Resources/TubeEBWB.png"},
      {TubeLayouts.NB_SB, "../Resources/TubeNBSB.png"},
      {TubeLayouts.No_EB, "../Resources/TubeNoEB.png"},
      {TubeLayouts.No_NB, "../Resources/TubeNoNB.png"},
      {TubeLayouts.No_SB, "../Resources/TubeNoSB.png"},
      {TubeLayouts.No_WB, "../Resources/TubeNoWB.png"},
    };

    public Location internalLocation;

    //stores the current totals for each of the 8 connections
    Dictionary<BalancingInsOuts, int> currentTotals;

    public string currentNBEnteringDiff = "0%";
    public string currentSBExitingDiff = "0%";
    public string currentSBEnteringDiff = "0%";
    public string currentNBExitingDiff = "0%";
    public string currentEBEnteringDiff = "0%";
    public string currentWBExitingDiff = "0%";
    public string currentWBEnteringDiff = "0%";
    public string currentEBExitingDiff = "0%";

    public Dictionary<BalancingInsOuts, TextBlock> balancingTextBlocks;
    public Dictionary<BalancingInsOuts, TextBlock> connectionDateTextBlocks;
    private MainWindow main;
    public bool areBalancingParametersNull;
    public ContextMenu neighborMenu;

    public BalancingModule(int row, int col, Location location, MainWindow parent)
    {
      InitializeComponent();

      currentTotals = new Dictionary<BalancingInsOuts, int>();
      foreach (BalancingInsOuts conn in Enum.GetValues(typeof(BalancingInsOuts)))
      {
        currentTotals.Add(conn, 0);
      }

      internalLocation = location;
      main = parent;

      areBalancingParametersNull = false;
      LocationTitle.Text = internalLocation.GetLocationName();
      internalLocation.m_BalancingLocation = new MerlinKeyValuePair<int, int>(row, col);
      CreateDiagram();

      locationNumber.Text = "#" + internalLocation.GetLocationNumber();

      balancingTextBlocks = new Dictionary<BalancingInsOuts, TextBlock>()
      { 
        {BalancingInsOuts.SBEntering, SB_Entering_Numbers},
        {BalancingInsOuts.NBExiting, NB_Exiting_Numbers},
        {BalancingInsOuts.NBEntering, NB_Entering_Numbers},
        {BalancingInsOuts.SBExiting, SB_Exiting_Numbers},
        {BalancingInsOuts.EBEntering, EB_Entering_Numbers},
        {BalancingInsOuts.WBExiting, WB_Exiting_Numbers},
        {BalancingInsOuts.WBEntering, WB_Entering_Numbers},
        {BalancingInsOuts.EBExiting, EB_Exiting_Numbers},
      };

      connectionDateTextBlocks = new Dictionary<BalancingInsOuts, TextBlock>()
      { 
        {BalancingInsOuts.SBEntering, SB_Entering_Date},
        {BalancingInsOuts.NBExiting, NB_Exiting_Date},
        {BalancingInsOuts.NBEntering, NB_Entering_Date},
        {BalancingInsOuts.SBExiting, SB_Exiting_Date},
        {BalancingInsOuts.EBEntering, EB_Entering_Date},
        {BalancingInsOuts.WBExiting, WB_Exiting_Date},
        {BalancingInsOuts.WBEntering, WB_Entering_Date},
        {BalancingInsOuts.EBExiting, EB_Exiting_Date},
      };

      foreach (TextBlock tb in balancingTextBlocks.Values)
      {
        tb.ContextMenu = neighborMenu;
        tb.Tag = "neighborTextBlock";
      }

      neighborMenu = new ContextMenu();
      DatesShowing = true;
    }

    #region Properties

    private bool _DatesShowing;
    public bool DatesShowing
    {
      get
      {
        return _DatesShowing;
      }
      set
      {
        _DatesShowing = value;
        if (_DatesShowing == true)
        {
          ShowDates();
        }
        else
        {
          HideDates();
        }
      }
    }

    public Count InternalCount
    {
      get
      {
        return ((Intersection)internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)((RadioButton)main.GetSelectedBalancingRadioButton(false)).Tag);
      }
    }

    #endregion

    #region Public Methods

    public void SetLayoutMode(BalancingTabState mode)
    {
      if (mode == BalancingTabState.ViewNeighbors)
      {
        foreach (TextBlock tb in balancingTextBlocks.Values)
        {
          tb.MouseEnter += BalancingTextBlock_MouseEnter;
          tb.MouseLeave += BalancingTextBlock_MouseLeave;
          tb.PreviewMouseDown += BalancingTextBlock_PreviewMouseDown;
        }

        DisplayNeighbors();
      }
      else if (mode == BalancingTabState.ViewTotals)
      {
        foreach (TextBlock tb in balancingTextBlocks.Values)
        {
          tb.MouseEnter -= BalancingTextBlock_MouseEnter;
          tb.MouseLeave -= BalancingTextBlock_MouseLeave;
          tb.PreviewMouseDown -= BalancingTextBlock_PreviewMouseDown;
        }

        DisplayTotals();
      }
      else if (mode == BalancingTabState.ViewDifference)
      {
        foreach (TextBlock tb in balancingTextBlocks.Values)
        {
          tb.MouseEnter -= BalancingTextBlock_MouseEnter;
          tb.MouseLeave -= BalancingTextBlock_MouseLeave;
          tb.PreviewMouseDown -= BalancingTextBlock_PreviewMouseDown;
        }

        DisplayPercentages();
      }
      DatesShowing = (bool)main.showDatesCheckBox.IsChecked;
    }

    public void UpdateNumbers(int timePeriodIndex, int[] banks, int[] uTurnBanks, int[] RTORBanks, string firstInterval, int numIntervals, DateTime? tubeDate, bool autoTubeDates, bool includeUnclassedTubes)
    {
      if (internalLocation is Intersection)
      {
        UpdateTMCNumbers(timePeriodIndex, banks, uTurnBanks, RTORBanks, firstInterval, numIntervals);
      }
      else if (internalLocation is TubeSite)
      {
        DateTime? dtFirstInterval;
        if (tubeDate == null)
        {
          if(autoTubeDates)
          {
            dtFirstInterval = (DateTime.MinValue).Date.Add(DateTime.Parse(firstInterval).TimeOfDay);
          }
          else
          {
          dtFirstInterval = null;
          }
        }
        else
        {
          dtFirstInterval = ((DateTime)tubeDate).Date.Add(DateTime.Parse(firstInterval).TimeOfDay);
        }

        //get list of BankVehicleType enum from the project bank numbers passed in
        List<BankVehicleTypes> enumBanks = new List<BankVehicleTypes>();
        foreach (int bankNum in banks)
        {
          enumBanks.Add((BankVehicleTypes)Enum.Parse(typeof(BankVehicleTypes), internalLocation.m_ParentProject.m_BankDictionary[bankNum]));
        }
        //take list of BankVehicleType and retrieve the list of (mostly) equivalent FHWA types
        List<BankVehicleTypes> FHWABanks = FHWAMappings.GetFHWATypesRepresentedByGivenVehicleTypes(enumBanks);

        UpdateTubeNumbers(includeUnclassedTubes, autoTubeDates, dtFirstInterval, numIntervals, FHWAMappings.ContainsAll13FHWATypes(FHWABanks) ? null : FHWABanks);
      }
    }

    //helper for UpdateTubeNumbers
    private DateTime? GetTMCNeighborDate(BalancingInsOuts myConnection)
    {
      MerlinKeyValuePair<Location, BalancingInsOuts> otherConnection = internalLocation.GetNeighbor(myConnection);
      if (otherConnection.Key == null || otherConnection.Key is TubeSite)
      {
        return null;
      }
      else
      {
        //Location on other end of connection must be a TMC, return its film date or null if the neighbor TMC doesn't have a count for the selected time period
        
        Count nbr = ((Intersection)otherConnection.Key).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)((RadioButton)main.GetSelectedBalancingRadioButton(false)).Tag);
        return nbr == null ? null : ((DateTime?)nbr.m_FilmDate.Date);
      }

    }

    private DateTime? GetPredominantTMCFilmDate()
    {
      Dictionary<DateTime, int> dateCounts = new Dictionary<DateTime, int>();
      foreach (Count cnt in internalLocation.m_ParentProject.GetCountsInTimePeriod((int)main.GetSelectedBalancingRadioButton(false).Tag))
      {
        if (dateCounts.Keys.Contains(cnt.m_FilmDate))
        {
          dateCounts[cnt.m_FilmDate]++;
        }
        else
        {
          dateCounts[cnt.m_FilmDate] = 1;
        }
      }
      int maxOccurrences = dateCounts.Values.Max();
      return dateCounts.FirstOrDefault(x => x.Value == maxOccurrences).Key;
    }

    private DateTime GetPredominantTubeSurveyDate()
    {
      Dictionary<DateTime, int> dateCounts = new Dictionary<DateTime, int>();
      foreach (TubeSite ts in main.m_Project.m_Tubes)
      {
        TubeCount tc = ts.m_TubeCounts[0];
        foreach (DateTime dt in tc.GetDatesContainingData())
        {
          if (dateCounts.Keys.Contains(dt))
          {
            dateCounts[dt]++;
          }
          else
          {
            dateCounts[dt] = 1;
          }
        }
      }
      int maxOccurrences = dateCounts.Values.Max();
      return dateCounts.FirstOrDefault(x => x.Value == maxOccurrences).Key;
    }

    public void SetPercentages()
    {
      currentNBEnteringDiff = CalculateAPercentage(BalancingInsOuts.NBEntering);
      currentSBExitingDiff = CalculateAPercentage(BalancingInsOuts.SBExiting);
      currentSBEnteringDiff = CalculateAPercentage(BalancingInsOuts.SBEntering);
      currentNBExitingDiff = CalculateAPercentage(BalancingInsOuts.NBExiting);
      currentEBEnteringDiff = CalculateAPercentage(BalancingInsOuts.EBEntering);
      currentWBExitingDiff = CalculateAPercentage(BalancingInsOuts.WBExiting);
      currentWBEnteringDiff = CalculateAPercentage(BalancingInsOuts.WBEntering);
      currentEBExitingDiff = CalculateAPercentage(BalancingInsOuts.EBExiting);
    }

    //helper for SetPercentages
    private string CalculateAPercentage(BalancingInsOuts myConnection)
    {
      Location neighbor = internalLocation.GetNeighbor(myConnection).Key;
      int myTotal = currentTotals[myConnection];

      if (neighbor == null)
      {
        //no neighbor connection, display blank
        return "";
      }

      IEnumerable<BalancingModule> bmList = main.GetCurrentBalancingModules();

      BalancingModule neighborBM = bmList.First(x => neighbor.m_BalancingLocation.Key
        == Grid.GetRow(x) && neighbor.m_BalancingLocation.Value == Grid.GetColumn(x));

      int NeighborTotal = neighborBM.currentTotals[internalLocation.GetNeighbor(myConnection).Value];

      string plus_Minus;
      if (NeighborTotal > myTotal && myTotal > 0)
      {
        plus_Minus = "-";
        return plus_Minus + ((float)(NeighborTotal - myTotal) / (float)NeighborTotal).ToString("P1");
      }
      else if (NeighborTotal <= myTotal && NeighborTotal > 0)
      {
        if (NeighborTotal == myTotal)
        {
          plus_Minus = " ";
        }
        else
        {
          plus_Minus = "+";
        }
        return plus_Minus + ((float)(myTotal - NeighborTotal) / (float)myTotal).ToString("P1");
      }
      else
      {
        //there is a neighbor connection, but one of the totals is 0 and/or different dates
        return "N/A";
      }
    }

    public void DisplayTotals()
    {
      bool doIHaveCountInThisTimePeriod = internalLocation is TubeSite || InternalCount != null;
      if (areBalancingParametersNull || !doIHaveCountInThisTimePeriod)
      {
        DisplayNull();
      }
      else
      {
        NB_Entering_Numbers.Text = currentTotals[BalancingInsOuts.NBEntering].ToString();
        SB_Exiting_Numbers.Text = currentTotals[BalancingInsOuts.SBExiting].ToString();
        SB_Entering_Numbers.Text = currentTotals[BalancingInsOuts.SBEntering].ToString();
        NB_Exiting_Numbers.Text = currentTotals[BalancingInsOuts.NBExiting].ToString();
        EB_Entering_Numbers.Text = currentTotals[BalancingInsOuts.EBEntering].ToString();
        WB_Exiting_Numbers.Text = currentTotals[BalancingInsOuts.WBExiting].ToString();
        WB_Entering_Numbers.Text = currentTotals[BalancingInsOuts.WBEntering].ToString();
        EB_Exiting_Numbers.Text = currentTotals[BalancingInsOuts.EBExiting].ToString();
      }
      SetTextBlockColors();
    }

    public void DisplayPercentages()
    {
      if (areBalancingParametersNull)
      {
        DisplayNull();
      }
      else
      {
        NB_Entering_Numbers.Text = currentNBEnteringDiff;
        SB_Exiting_Numbers.Text = currentSBExitingDiff;
        SB_Entering_Numbers.Text = currentSBEnteringDiff;
        NB_Exiting_Numbers.Text = currentNBExitingDiff;
        EB_Entering_Numbers.Text = currentEBEnteringDiff;
        WB_Exiting_Numbers.Text = currentWBExitingDiff;
        WB_Entering_Numbers.Text = currentWBEnteringDiff;
        EB_Exiting_Numbers.Text = currentEBExitingDiff;
      }
      SetTextBlockColors();
    }

    public void DisplayNeighbors()
    {
      //neighbors unfortunately not stored as dict in internalLocation, so make a dict here to make code simpler
      Dictionary<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>> neighbors = new Dictionary<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>>();
      foreach (BalancingInsOuts conn in Enum.GetValues(typeof(BalancingInsOuts)))
      {
        if (internalLocation.m_Neighbors.Exists(x => x.Key == conn))
        {
          neighbors.Add(conn, internalLocation.m_Neighbors.FirstOrDefault(x => x.Key == conn).Value);
        }
      }

      foreach (BalancingInsOuts conn in Enum.GetValues(typeof(BalancingInsOuts)))
      {
        if (neighbors.ContainsKey(conn))
        {
          balancingTextBlocks[conn].Text = String.Format("#{0} {1}", neighbors[conn].Key.GetLocationNumber(), neighbors[conn].Value.ToString().Substring(0, 2));
        }
        else
        {
          balancingTextBlocks[conn].Text = "";
        }
      }
      SetTextBlockColors();
    }

    public void DisplayNull()
    {
      string nullDisplay = "";

      foreach (TextBlock tb in balancingTextBlocks.Values)
      {
        tb.Text = nullDisplay;
      }
      SetTextBlockColors();
    }



    #endregion

    /// <summary>
    /// Updates balancing totals and neighbor difference percentages based on given time and data bank parameters.
    /// </summary>
    /// <param name="timePeriod">Time period to use for balancing</param>
    /// <param name="banks">Banks to include for balancing. Pass in an array with the integer bank numbers to include.</param>
    /// <param name="firstInterval">First interval to include.</param>
    /// <param name="numIntervals">Number of intervals to include.</param>
    private void UpdateTMCNumbers(int timePeriodIndex, int[] banks, int[] uTurnBanks, int[] RTORBanks, string firstInterval, int numIntervals)
    {
      //hack
      if (!(internalLocation is Intersection))
      {
        throw new NotSupportedException("Can't invoke UpdateTMCNumbers method on a BalancingModule that does not represent an Intersection");
      }
      //try
      //{
      foreach (BalancingInsOuts conn in Enum.GetValues(typeof(BalancingInsOuts)))
      {
        Count cnt = ((Intersection)internalLocation).m_Counts.Find(x => x.m_TimePeriodIndex == timePeriodIndex);
        if (cnt == null)
        {
          currentTotals[conn] = 0;
          connectionDateTextBlocks[conn].Tag = null;
        }
        else
        {
          currentTotals[conn] = cnt.GetBalancingSumByDirection(conn.ToString(), banks, uTurnBanks, RTORBanks, firstInterval, numIntervals);
          connectionDateTextBlocks[conn].Tag = cnt.m_FilmDate.Date;
        }
      }
      //}
      //catch
      //{
      //foreach (BalancingInsOuts conn in Enum.GetValues(typeof(BalancingInsOuts)))
      //{
      //  currentTotals[conn] = 0;
      //}
      //}

    }

    /// <summary>
    /// Updates balancing totals and neighbor difference percentages.
    /// </summary>
    /// <param name="autoTubeDates">When true, each connection will attempt to display data for the time period from the date that corresponds to its neighbor's date if neighbor is TMC.</param>
    /// <param name="firstInterval">Time of the first interval. When autoTubeDates is true, connections may use a different date component to match their TMC neighbor's date.</param>
    /// <param name="numFiveMinuteIntervals">Number of FIVE-minute intervals to display tube data for.</param>
    /// <param name="classes">FHWA classes to include.</param>
    private void UpdateTubeNumbers(bool includeUnclassed, bool autoTubeDates, DateTime? firstInterval, int numFiveMinuteIntervals, List<BankVehicleTypes> classes = null)
    {
      //hack
      if (!(internalLocation is TubeSite))
      {
        throw new NotSupportedException("Can't invoke UpdateTubeNumbers method on a BalancingModule that does not represent a TubeSite");
      }

      //first check for conditions where it's impossible to get tube data that would match with the TMC parameters selected
      if (!autoTubeDates && firstInterval == null)
      {
        foreach (var key in currentTotals.Keys.ToArray())
        {
          currentTotals[key] = 0;
        }

        foreach (TextBlock tb in connectionDateTextBlocks.Values)
        {
          tb.Tag = null;
        }
      }
      else if (autoTubeDates == true)
      {
        //auto tube dates set to true, meaning each connection will attempt to display totals from the date of their TMC neighbors, 
        //    otherwise will display totals from the most common film date for TMCs in the selected time period
        DateTime? predominantTMCFilmDate = GetPredominantTMCFilmDate();
        foreach (var key in currentTotals.Keys.ToArray())
        {
          DateTime startingInterval;
          DateTime? tmcNeighborDate = GetTMCNeighborDate(key);
          if (tmcNeighborDate == null)
          {
            //neighbor either non-existent or is another tube, use predominant TMC film date
            //if for some reason there are no TMCs in project, get predominant tube date (first date where the most tube coverage happens)
            startingInterval = predominantTMCFilmDate == null ? GetPredominantTubeSurveyDate().Date.Add(((DateTime)firstInterval).TimeOfDay) : ((DateTime)predominantTMCFilmDate).Date.Add(((DateTime)firstInterval).TimeOfDay);
          }
          else
          {
            //TMC neighbor found, use date component from that date and time of day from firstInterval passed in
            startingInterval = ((DateTime)tmcNeighborDate).Date.Add(((DateTime)firstInterval).TimeOfDay);
          }
          try
          {
            currentTotals[key] = ((TubeSite)internalLocation).GetConnectionVolume(key, startingInterval, new TimeSpan(0, numFiveMinuteIntervals * 5, 0), includeUnclassed, classes);
            connectionDateTextBlocks[key].Tag = startingInterval.Date;
          }
          catch (Exception ex)
          {
            MessageBox.Show(ex.Message);
            currentTotals[key] = 0;
            connectionDateTextBlocks[key].Tag = null;
          }
        }
      }
      else
      {
        //auto tube dates set to false, meaning each connection will attempt to display totals from not only the time, but also the date component passed in via the firstInterval parameter
        foreach (var key in currentTotals.Keys.ToArray())
        {
          try
          {
            currentTotals[key] = ((TubeSite)internalLocation).GetConnectionVolume(key, ((DateTime)firstInterval), new TimeSpan(0, numFiveMinuteIntervals * 5, 0), includeUnclassed, classes);
            connectionDateTextBlocks[key].Tag = ((DateTime)firstInterval).Date;
          }
          catch (Exception ex)
          {
            MessageBox.Show(ex.Message);
            currentTotals[key] = 0;
            connectionDateTextBlocks[key].Tag = DateTime.MinValue;
          }
        }
      }
    }

    private void ShowDates()
    {
      List<DateTime> connectionDates = new List<DateTime>();
      if (internalLocation is TubeSite)
      {
        foreach (TextBlock tb in connectionDateTextBlocks.Values.Where(x => x.Tag != null))
        {
          DateTime connDate = ((DateTime)tb.Tag).Date;
          if (!connectionDates.Contains(connDate))
          {
            connectionDates.Add(connDate);
          }
        }
      }
      else
      {
        Count thisCount = ((Intersection)internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)main.GetSelectedBalancingRadioButton(false).Tag);
        //internal location is an Intersection
        foreach(TextBlock tb in connectionDateTextBlocks.Values)
        {
          tb.Tag = thisCount == null ? null : (DateTime?)thisCount.m_FilmDate;
        }
        if(thisCount != null)
        {
          connectionDates.Add(thisCount.m_FilmDate);
        }
      }
      if (connectionDates.Count == 0)
      {
        HideDates();
      }
      else if (connectionDates.Count == 1)
      {
        HideDates();
        locationDate.Text = connectionDates[0].ToString("M/d");
        locationDate.Visibility = Visibility.Visible;
      }
      else
      {
        //more than 1 unique connection date; elect to show individual connection dates instead of single location date
        locationDate.Visibility = Visibility.Collapsed;
        foreach (TextBlock tb in connectionDateTextBlocks.Values)
        {
          if (tb.Tag == null)
          {
            tb.Visibility = Visibility.Collapsed;
          }
          else
          {
            tb.Text = ((DateTime)tb.Tag).ToString("M/d");
            tb.Visibility = Visibility.Visible;
          }
        }
      }
    }

    private void HideDates()
    {
      List<TextBlock> allDateTextBlocks = new List<TextBlock>();
      allDateTextBlocks.AddRange(connectionDateTextBlocks.Values);
      allDateTextBlocks.Add(locationDate);

      foreach (TextBlock tb in allDateTextBlocks)
      {
        tb.Visibility = Visibility.Collapsed;
      }
    }

    private bool CreateDiagram()
    {
      if (internalLocation == null)
      {
        return false;
      }
      if (internalLocation is Intersection)
      {
        Intersection internalIntersection = internalLocation as Intersection;
        CustomizeLocationsWindow.IntersectionConfig diagram = new CustomizeLocationsWindow.IntersectionConfig();
        IntersectionApproach sbApproach = internalIntersection.m_ApproachesInThisIntersection[1];
        IntersectionApproach wbApproach = internalIntersection.m_ApproachesInThisIntersection[3];
        IntersectionApproach nbApproach = internalIntersection.m_ApproachesInThisIntersection[0];
        IntersectionApproach ebApproach = internalIntersection.m_ApproachesInThisIntersection[2];

        diagram.SetLegFlows(sbApproach.TrafficFlowType, wbApproach.TrafficFlowType, nbApproach.TrafficFlowType, ebApproach.TrafficFlowType);
        mainGrid.Children.Add(diagram);
        Grid.SetRow(diagram, 1);
        Grid.SetColumn(diagram, 1);
        diagram.IsEnabled = false;
        Panel.SetZIndex(diagram, 0);
        return true;
      }
      else if (internalLocation is TubeSite)
      {
        Image tubeImage = new Image();
        tubeImage.Source = new BitmapImage(new Uri(LayoutToFileNameMapping[((TubeSite)internalLocation).TubeLayout], UriKind.Relative));
        mainGrid.Children.Add(tubeImage);
        Grid.SetRow(tubeImage, 1);
        Grid.SetColumn(tubeImage, 1);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Sets textbox colors to either default or purple if different date or red if difference threshold exceeded
    /// </summary>
    public void SetTextBlockColors()
    {
      if(main.m_IsBalancingTabCurrentlyPopulating)
      {
        return;
      }
      Location neighborLocation;
      BalancingModule neighborBM;
      Count thisCount = null;

      foreach (TextBlock tb in balancingTextBlocks.Values)
      {
        neighborLocation = internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(tb)).Key;
        neighborBM = main.GetCurrentBalancingModules().FirstOrDefault(x => x.internalLocation == neighborLocation);
        this.Opacity = 1;
        if (internalLocation is Intersection)
        {
          thisCount = ((Intersection)internalLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)main.GetSelectedBalancingRadioButton(false).Tag);
          if (thisCount == null)
          {
            //no neighbor or this location doesn't have a count for the currently selected time period (or is a tube), default to black
            tb.Foreground = new SolidColorBrush(Colors.Black);
            this.Opacity = .3;
          }
        }
        if(neighborLocation == null)
        {
          continue;
        }

        // Count neighborCount = null;
        //if (neighborLocation is Intersection)
        //{
        //  neighborCount = ((Intersection)neighborLocation).m_Counts.FirstOrDefault(x => x.m_TimePeriodIndex == (int)main.GetSelectedBalancingRadioButton(false).Tag);
        //}
        //if (neighborCount == null)
        //{
        //  //neighbor  doesn't have a count for the currently selected time period (or is a tube), default to black
        //  tb.Foreground = new SolidColorBrush(Colors.Black);
        //  continue;
        //}
        TextBlock neighborsConnTextBlock = GetNeighborTextBlock(tb);
        TextBlock neighborsDateTextBlock = neighborBM.GetDateTBFromConnTB(neighborsConnTextBlock);
        if(neighborsDateTextBlock.Tag == null)
        {
          //neighbor doesn't have data for the currently selected parameters; default this textblock to black
          tb.Foreground = new SolidColorBrush(Colors.Black);
        }

        double diff;

        //TODO: Need to update logic determining if the two counts/tubecounts are comparing the same date
        //if (thisCount.m_FilmDate.Date != neighborCount.m_FilmDate.Date)
        TextBlock myDateTB = GetDateTBFromConnTB(tb);
        TextBlock neighborConnTB = GetNeighborTextBlock(tb);
        TextBlock neighborDateTB = neighborBM.GetDateTBFromConnTB(neighborConnTB);
        if (myDateTB.Tag != null && neighborDateTB.Tag != null && (DateTime)myDateTB.Tag != (DateTime)neighborDateTB.Tag)
        {
          //indicate to user the dates are different by coloring textblock purple
          tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BE00ED"));
        }
        else if (double.TryParse(GetDiffStringFromConnectionTextBlock(tb).Substring(1,
          GetDiffStringFromConnectionTextBlock(tb).Length - 2), out diff) && (Math.Abs(diff) / 100) > main.m_Project.m_Prefs.m_BalancingDiff)
        {
          //percentage difference exceeds value set in preferences
          tb.Foreground = new SolidColorBrush(Colors.Red);
        }
        else
        {
          //nothing abnormal, color default/black
          tb.Foreground = new SolidColorBrush(Colors.Black);
        }
      }
    }

    private string GetDiffStringFromConnectionTextBlock(TextBlock connection)
    {
      switch (connection.Name)
      {
        case "SB_Entering_Numbers":
          return currentSBEnteringDiff;
        case "NB_Exiting_Numbers":
          return currentNBExitingDiff;
        case "NB_Entering_Numbers":
          return currentNBEnteringDiff;
        case "SB_Exiting_Numbers":
          return currentSBExitingDiff;
        case "EB_Entering_Numbers":
          return currentEBEnteringDiff;
        case "WB_Exiting_Numbers":
          return currentWBExitingDiff;
        case "WB_Entering_Numbers":
          return currentWBEnteringDiff;
        case "EB_Exiting_Numbers":
          return currentEBExitingDiff;
        default:
          return currentEBEnteringDiff;
      }
    }

    private BalancingInsOuts GetBalancingInsOutsEnumFromConnectionTextBlock(TextBlock connection)
    {
      switch (connection.Name)
      {
        case "SB_Entering_Numbers":
          return BalancingInsOuts.SBEntering;
        case "NB_Exiting_Numbers":
          return BalancingInsOuts.NBExiting;
        case "NB_Entering_Numbers":
          return BalancingInsOuts.NBEntering;
        case "SB_Exiting_Numbers":
          return BalancingInsOuts.SBExiting;
        case "EB_Entering_Numbers":
          return BalancingInsOuts.EBEntering;
        case "WB_Exiting_Numbers":
          return BalancingInsOuts.WBExiting;
        case "WB_Entering_Numbers":
          return BalancingInsOuts.WBEntering;
        case "EB_Exiting_Numbers":
          return BalancingInsOuts.EBExiting;
        default:
          //had to return something, this should never happen
          MessageBox.Show("There is a problem with the balancing neighbors feature.");
          return BalancingInsOuts.EBEntering;
      }
    }

    private TextBlock GetConnectionTextBlockFromBalancingInsOutsEnum(BalancingInsOuts connection)
    {
      switch (connection)
      {
        case BalancingInsOuts.SBEntering:
          return SB_Entering_Numbers;
        case BalancingInsOuts.NBExiting:
          return NB_Exiting_Numbers;
        case BalancingInsOuts.NBEntering:
          return NB_Entering_Numbers;
        case BalancingInsOuts.SBExiting:
          return SB_Exiting_Numbers;
        case BalancingInsOuts.EBEntering:
          return EB_Entering_Numbers;
        case BalancingInsOuts.WBExiting:
          return WB_Exiting_Numbers;
        case BalancingInsOuts.WBEntering:
          return WB_Entering_Numbers;
        case BalancingInsOuts.EBExiting:
          return EB_Exiting_Numbers;
        default:
          //had to return something, this should never happen
          MessageBox.Show("There is a problem with the balancing neighbors feature.");
          return EB_Exiting_Numbers;
      }
    }

    private TextBlock GetNeighborTextBlock(TextBlock origin)
    {
      MerlinKeyValuePair<Location, BalancingInsOuts> otherConnection = internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(origin));
      if (otherConnection.Key == null)
      {
        //neighbor doesn't exist, return null
        return null;
      }
      TextBlock neighbor = main.GetBalancingModule(otherConnection.Key).GetConnectionTextBlockFromBalancingInsOutsEnum(otherConnection.Value);
      return neighbor;
    }

    private bool CanThisHaveANeighbor(TextBlock tb)
    {
      switch (tb.Name)
      {
        case "SB_Entering_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.SBEntering) != PossibleConnectionFlows.In)
          {
            return false;
          }
          break;
        case "NB_Exiting_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.NBExiting) != PossibleConnectionFlows.Out)
          {
            return false;
          }
          break;
        case "NB_Entering_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.NBEntering) != PossibleConnectionFlows.In)
          {
            return false;
          }
          break;
        case "SB_Exiting_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.SBExiting) != PossibleConnectionFlows.Out)
          {
            return false;
          }
          break;
        case "EB_Entering_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.EBEntering) != PossibleConnectionFlows.In)
          {
            return false;
          }
          break;
        case "WB_Exiting_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.WBExiting) != PossibleConnectionFlows.Out)
          {
            return false;
          }
          break;
        case "WB_Entering_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.WBEntering) != PossibleConnectionFlows.In)
          {
            return false;
          }
          break;
        case "EB_Exiting_Numbers":
          if (internalLocation.GetTrafficFlowTypeAtConnection(BalancingInsOuts.EBExiting) != PossibleConnectionFlows.Out)
          {
            return false;
          }
          break;
        default:
          return true;
      }
      return true;
    }

    private void OpenContextMenu(FrameworkElement element)
    {
      if (element.ContextMenu != null)
      {
        element.ContextMenu.PlacementTarget = element;
        element.ContextMenu.IsOpen = true;
      }
    }

    private void PopulateNeighborContextMenu(TextBlock clickedConnection)
    {
      MenuItem title1 = new MenuItem();
      string fromOrTo;
      //Image checkedIcon = new System.Windows.Controls.Image
      //{
      //  Source = new BitmapImage(new Uri("/Merlin;component/Resources/Icons/cancel_2-48.png", UriKind.Relative))
      //};

      neighborMenu.Items.Clear();
      if (GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection).ToString().Substring(3, 1).Equals("x"))
      {
        fromOrTo = " to...";
      }
      else
      {
        fromOrTo = " from...";
      }
      title1.Header = string.Format("#{0} - {1}\n{2} {3} {4}",
                                    internalLocation.GetLocationNumber(),
                                    internalLocation.GetLocationName(),
                                    GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection).ToString().Substring(0, 2),
                                    GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection).ToString().Substring(2),
                                    fromOrTo);
      title1.FontWeight = FontWeights.Bold;
      title1.StaysOpenOnClick = true;
      title1.IsHitTestVisible = false;
      neighborMenu.Items.Add(title1);
      neighborMenu.Items.Add(new Separator());

      foreach (BalancingModule bm in main.GetCurrentBalancingModules().Where(x => x != this))
      {
        //Adds each other location to list
        MenuItem location = new MenuItem();
        location.Header = string.Format("#{0} - {1}", bm.internalLocation.GetLocationNumber(), bm.internalLocation.GetLocationName());
        location.Tag = bm.internalLocation;
        neighborMenu.Items.Add(location);

        //For each other intersection, adds sublist of possible connections
        foreach (BalancingInsOuts inOut in Enum.GetValues(typeof(BalancingInsOuts)))
        {
          if (internalLocation.IsConnectionPossible(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection), bm.internalLocation, inOut))
          {
            MenuItem connection = new MenuItem();
            connection.Header = inOut.ToString().Substring(0, 2) + " " + inOut.ToString().Substring(2);
            connection.Tag = new MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>>
              (GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection), new MerlinKeyValuePair<Location, BalancingInsOuts>
              (bm.internalLocation, inOut));
            connection.PreviewMouseLeftButtonDown += PossibleNeighborConnection_PreviewMouseLeftButtonDown;
            if (internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection)).Key == bm.internalLocation
              && internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection)).Value == inOut)
            {
              connection.Icon = new Image
              {
                Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/Icons/checked_2-48.png")),
                Width = Constants.MENU_ICON_WIDTH,
                Height = Constants.MENU_ICON_HEIGHT
              };
              location.Icon = new Image
              {
                Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/Icons/checked_2-48.png")),
                Width = Constants.MENU_ICON_WIDTH,
                Height = Constants.MENU_ICON_HEIGHT
              };
            }
            location.Items.Add(connection);
          }
        }
      }
      //After list of intersections, add entry for no connection
      MenuItem noConnection = new MenuItem();
      noConnection.Tag = new MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>>(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection), internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection)));
      noConnection.Header = "No Connection";
      noConnection.Icon = new Image
      {
        Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/Icons/delete_link-48.png")),
        Width = Constants.MENU_ICON_WIDTH,
        Height = Constants.MENU_ICON_HEIGHT
      };
      noConnection.PreviewMouseLeftButtonDown += NoNeighborConnection_PreviewMouseLeftButtonDown;
      if (internalLocation.GetNeighbor(GetBalancingInsOutsEnumFromConnectionTextBlock(clickedConnection)).Key == null)
      {
        noConnection.Icon = new Image
        {
          Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/Icons/checked_2-48.png")),
          Width = Constants.MENU_ICON_WIDTH,
          Height = Constants.MENU_ICON_HEIGHT
        };
      }
      neighborMenu.Items.Add(noConnection);
    }

    private void BalancingTextBlock_MouseEnter(object sender, MouseEventArgs e)
    {
      TextBlock thisTB = (TextBlock)sender;
      TextBlock neighborTB = GetNeighborTextBlock(thisTB);
      if (!CanThisHaveANeighbor(thisTB) || main.m_CurrentState.m_BalancingTabState != BalancingTabState.ViewNeighbors)
      {
        return;
      }

      thisTB.FontWeight = FontWeights.Bold;
      thisTB.Cursor = Cursors.Hand;
      if (neighborTB != null)
      {
        neighborTB.FontWeight = FontWeights.Bold;
      }
    }

    private void BalancingTextBlock_MouseLeave(object sender, MouseEventArgs e)
    {
      TextBlock thisTB = (TextBlock)sender;
      TextBlock neighborTB = GetNeighborTextBlock(thisTB);

      thisTB.FontWeight = FontWeights.Normal;
      thisTB.Cursor = Cursors.Arrow;
      if (neighborTB != null)
      {
        neighborTB.FontWeight = FontWeights.Normal;
      }
    }

    private void BalancingTextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      TextBlock tb = (TextBlock)sender;
      if (!CanThisHaveANeighbor(tb) || main.m_CurrentState.m_BalancingTabState != BalancingTabState.ViewNeighbors)
      {
        return;
      }

      PopulateNeighborContextMenu(tb);
      neighborMenu.IsOpen = true;

      //OpenContextMenu(tb);

      e.Handled = true;
    }

    private void ConnectionTextBlock_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
      TextBlock clickedConnection = balancingTextBlocks.Values.First(x => x == (UIElement)e.Source);

      //PopulateNeighborContextMenu(clickedConnection);

      e.Handled = true;
    }

    private void PossibleNeighborConnection_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      MenuItem item = (MenuItem)sender;
      MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>> connection;
      connection = ((MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>>)item.Tag);

      internalLocation.RemoveNeighborConnection(connection.Key);
      internalLocation.SetNeighbor(connection.Key, connection.Value.Key, connection.Value.Value);
      connection.Value.Key.RemoveNeighborConnection(connection.Value.Value);
      connection.Value.Key.SetNeighbor(connection.Value.Value, internalLocation, connection.Key);

      main.RefreshBalancingTotals();
    }

    private void NoNeighborConnection_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      MenuItem item = (MenuItem)sender;

      MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>> connection;
      connection = (MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Location, BalancingInsOuts>>)item.Tag;

      if (connection.Value.Key != null)
      {
        //remove connection if not already null
        internalLocation.RemoveNeighborConnection(connection.Key);
        connection.Value.Key.RemoveNeighborConnection(connection.Value.Value);
      }

      main.RefreshBalancingTotals();
    }

    public TextBlock GetDateTBFromConnTB(TextBlock connTB)
    {
      return connectionDateTextBlocks[GetBalancingInsOutsEnumFromConnectionTextBlock(connTB)];
    }

  }
}