using System.Collections.Generic;


namespace AppMerlin
{
  //global constants
  public static class Constants
  {
    public static readonly double MENU_ICON_HEIGHT = 20d;
    public static readonly double MENU_ICON_WIDTH = 20d;
    public static readonly int MAX_BANKS_ALLOWED = 14;
  }

  //types
  public enum FlagType
  { 
    EmptyInterval, DataBeyondEndTime, ImpossibleMovement, LowInterval, HighInterval, NoVehicleWarning,
    LowHeavies, SuspiciousTrafficFlow, ImpossibleUTurn, InappropriateRTOR, SuspiciousMovement, NoData,
    CountClassificationDiscrepancy, FileDataClassificationDiscrepancy, HourClassificationDiscrepancy
  };

  public enum PossibleIntersectionApproaches
  { SB, SWB, WB, NWB, NB, NEB, EB, SEB };

  public enum StandardIntersectionApproaches
  { SB, WB, NB, EB };

  public enum MovementNames
  { HardRight, Right, VeerRight, Through, VeerLeft, Left, HardLeft, UTurn };

  public enum PossibleApproachFlows
  { TwoWay, ExitingOnly, EnteringOnly, PedsOnly };

  public enum PossibleConnectionFlows
  { In, Out, None };

  public enum StandardMovements
  { Right, Through, Left, UTurn };

  //Because we did not make this a class, you must update FHWAMappings whenever adding a new type to update its FHWA mapping (needed for comparing tubes with TMCS on balancing tab).
  public enum BankVehicleTypes
  {
    Passenger, Heavies, Bicycles, E_Scooters, Buses, LightHeavies, MediumHeavies, HeavyHeavies, FHWA1_2_3, FHWA4_5_6_7, 
    FHWA8_9_10, FHWA11_12_13, SingleUnitHeavies, MultiUnitHeavies, 
    FHWA1, FHWA2, FHWA3, FHWA4, FHWA5, FHWA6, FHWA7, FHWA8, FHWA9, FHWA10, FHWA11, FHWA12, FHWA13,
    TEX_UTurnLanePassenger, TEX_UTurnLaneHeavy, FHWAPedsBikes, FHWA6through13,
    Unclassified
  };

  public static class FHWAMappings
  {
    public static Dictionary<BankVehicleTypes, List<BankVehicleTypes>> mappings = new Dictionary<BankVehicleTypes, List<BankVehicleTypes>>()
    {
      {BankVehicleTypes.Passenger, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA1, BankVehicleTypes.FHWA2, BankVehicleTypes.FHWA3}},
      {BankVehicleTypes.Heavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA5, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7, BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.Buses, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4}},
      {BankVehicleTypes.LightHeavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA5}},
      {BankVehicleTypes.MediumHeavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7}},
      {BankVehicleTypes.HeavyHeavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.FHWA1_2_3, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA1, BankVehicleTypes.FHWA2, BankVehicleTypes.FHWA3}},
      {BankVehicleTypes.FHWA4_5_6_7, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA5, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7}},
      {BankVehicleTypes.FHWA8_9_10, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10}},
      {BankVehicleTypes.FHWA11_12_13, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.SingleUnitHeavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA5, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7, BankVehicleTypes.FHWA8}},
      {BankVehicleTypes.MultiUnitHeavies, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.FHWA1, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA1}},
      {BankVehicleTypes.FHWA2, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA2}},
      {BankVehicleTypes.FHWA3, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA3}},
      {BankVehicleTypes.FHWA4, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4}},
      {BankVehicleTypes.FHWA5, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA5}},
      {BankVehicleTypes.FHWA6, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA6}},
      {BankVehicleTypes.FHWA7, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA7}},
      {BankVehicleTypes.FHWA8, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA8}},
      {BankVehicleTypes.FHWA9, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA9}},
      {BankVehicleTypes.FHWA10, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA10}},
      {BankVehicleTypes.FHWA11, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA11}},
      {BankVehicleTypes.FHWA12, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA12}},
      {BankVehicleTypes.FHWA13, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.TEX_UTurnLanePassenger, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA1, BankVehicleTypes.FHWA2, BankVehicleTypes.FHWA3}},
      {BankVehicleTypes.TEX_UTurnLaneHeavy, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA5, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7, BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.FHWA6through13, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7, BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
      {BankVehicleTypes.Unclassified, new List<BankVehicleTypes>() {BankVehicleTypes.FHWA1, BankVehicleTypes.FHWA2, BankVehicleTypes.FHWA3, BankVehicleTypes.FHWA4, BankVehicleTypes.FHWA5, BankVehicleTypes.FHWA6, BankVehicleTypes.FHWA7, BankVehicleTypes.FHWA8, BankVehicleTypes.FHWA9, BankVehicleTypes.FHWA10, BankVehicleTypes.FHWA11, BankVehicleTypes.FHWA12, BankVehicleTypes.FHWA13}},
    };

    public static List<BankVehicleTypes> GetFHWATypesRepresentedByGivenVehicleTypes(List<BankVehicleTypes> types)
    {
      List<BankVehicleTypes> FHWATypes = new List<BankVehicleTypes>();
      foreach(BankVehicleTypes type in types)
      {
        if(mappings.ContainsKey(type))
        {
          foreach(BankVehicleTypes fhwaType in mappings[type])
          {
            if(!FHWATypes.Contains(fhwaType))
            {
              FHWATypes.Add(fhwaType);
            }
          }
        }
      }
      return FHWATypes;
    }

    public static bool ContainsAll13FHWATypes(List<BankVehicleTypes> types)
    {
      return types.Contains(BankVehicleTypes.FHWA1) &&
             types.Contains(BankVehicleTypes.FHWA2) &&
             types.Contains(BankVehicleTypes.FHWA3) &&
             types.Contains(BankVehicleTypes.FHWA4) &&
             types.Contains(BankVehicleTypes.FHWA5) &&
             types.Contains(BankVehicleTypes.FHWA6) &&
             types.Contains(BankVehicleTypes.FHWA7) &&
             types.Contains(BankVehicleTypes.FHWA8) &&
             types.Contains(BankVehicleTypes.FHWA9) &&
             types.Contains(BankVehicleTypes.FHWA10) &&
             types.Contains(BankVehicleTypes.FHWA11) &&
             types.Contains(BankVehicleTypes.FHWA12) &&
             types.Contains(BankVehicleTypes.FHWA13);
    }
  }

  public enum PedColumnDataType
  {
    Pedestrian, RTOR, UTurn, PassengerRTOR, HeavyRTOR, PassengerUTurn, HeavyUTurn, NA
  };

  public enum TimePeriodName  //Only used to convert legacy projects from this old system of 8 possible time periods
  {
    AM, MID, PM, C1, C2, C3, C4, C5
  };

  public enum IntervalLength
  {
    Five = 5, Fifteen = 15, Thirty = 30, Sixty = 60
  };

  public enum RotationDirection
  {
    Clockwise, CounterClockwise
  };

  public enum ProgramState
  {
    Creating, Editing, Loaded
  };

  public enum DataState
  {
    Empty, Partial, Complete
  };

  public enum ProjectSource
  {
    Scratch, Web, Sync, ManualEdit
  };

  /// <summary>
  /// Entering and exiting connection names
  /// </summary>
  public enum BalancingInsOuts  //this specific order must be preserved for Intersection methods
  {
    SBEntering, NBExiting, WBEntering, EBExiting, SBExiting, NBEntering, WBExiting, EBEntering
  };

  public static class Descriptions
  {
    public static Dictionary<BankVehicleTypes, string> FhwaToClass = new Dictionary<BankVehicleTypes, string>()
    {
      { BankVehicleTypes.FHWA1_2_3, "Classes 1-3" },
      { BankVehicleTypes.FHWA4_5_6_7, "Classes 4-7" },
      { BankVehicleTypes.FHWA8_9_10, "Classes 8-10" },
      { BankVehicleTypes.FHWA11_12_13, "Classes 11-13" },
      { BankVehicleTypes.FHWA1, "Class 1" },
      { BankVehicleTypes.FHWA2, "Class 2" },
      { BankVehicleTypes.FHWA3, "Class 3" },
      { BankVehicleTypes.FHWA4, "Class 4" },
      { BankVehicleTypes.FHWA5, "Class 5" },
      { BankVehicleTypes.FHWA6, "Class 6" },
      { BankVehicleTypes.FHWA7, "Class 7" },
      { BankVehicleTypes.FHWA8, "Class 8" },
      { BankVehicleTypes.FHWA9, "Class 9" },
      { BankVehicleTypes.FHWA10, "Class 10" },
      { BankVehicleTypes.FHWA11, "Class 11" },
      { BankVehicleTypes.FHWA12, "Class 12" },
      { BankVehicleTypes.FHWA13, "Class 13" }
    };

    public static Dictionary<BankVehicleTypes, string> FhwaToDescription = new Dictionary<BankVehicleTypes, string>()
    {
      { BankVehicleTypes.FHWA1_2_3, "Passenger" },
      { BankVehicleTypes.FHWA4_5_6_7, "Single-Unit Heavy" },
      { BankVehicleTypes.FHWA8_9_10, "Single-Trailer Heavy" },
      { BankVehicleTypes.FHWA11_12_13, "Multi-Trailer Heavy" },
      { BankVehicleTypes.FHWA1, "Motorcycles" },
      { BankVehicleTypes.FHWA2, "Cars & Trailers" },
      { BankVehicleTypes.FHWA3, "2 Axle Long" },
      { BankVehicleTypes.FHWA4, "Buses" },
      { BankVehicleTypes.FHWA5, "2 Axle 6 Tire" },
      { BankVehicleTypes.FHWA6, "3 Axle Single" },
      { BankVehicleTypes.FHWA7, "4 Axle Single" },
      { BankVehicleTypes.FHWA8, "<5 Axle Double" },
      { BankVehicleTypes.FHWA9, "5 Axle Double" },
      { BankVehicleTypes.FHWA10, "> 6 Axle Double" },
      { BankVehicleTypes.FHWA11, "<6 Axle Multi" },
      { BankVehicleTypes.FHWA12, "6 Axle Multi" },
      { BankVehicleTypes.FHWA13, ">6 Axle Multi" }
    };

    public static Dictionary<string, string> ApproachDescription = new Dictionary<string, string>()
    {
      { "SB", "Southbound" },
      { "WB", "Westbound" },
      { "NB", "Northbound" },
      { "EB", "Eastbound" },
    };

    public static Dictionary<string, string> MovementDescription = new Dictionary<string, string>()
    {
      { "T", "Thru" },
      { "L", "Left" },
      { "R", "Right" },
      { "U", "U-Turn" },
    };

    public static Dictionary<string, string> PedApproachToSide = new Dictionary<string, string>()
    {
      { "SBP", "North Leg" },
      { "WBP", "East Leg" },
      { "NBP", "South Leg" },
      { "EBP", "West Leg" },
    };

  }

  public static class GlobalBankPresets
  {
    public const string PRESET_NAME_STANDARD = "Standard";
    public const string PRESET_NAME_NCDOT = "NCDOT";
    public const string PRESET_NAME_TPA_RTORS = "TPA RTORs";
    public const string PRESET_NAME_TEX_U_TURN_LANES = "TEX U-Turn Lanes";
    public const string PRESET_NAME_BUSES = "Buses";
    public const string PRESET_NAME_PORT_OF_PORTLAND = "Port Of Portland";
    public const string PRESET_NAME_FHWA_13_VEH_CLASS = "FHWA 13 Vehicle Classification";
    public const string PRESET_NAME_FHWA_13_VEH_CLASS_SIMPLE = "FHWA 13 Vehicle Classification (Simple)";

    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> Standard = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> NCDOT = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> TPA_RTOR = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> TEX_UTurns = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> Buses = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> PortOfPortland = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> FHWA13 = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);
    public static Dictionary<int, KeyValuePair<string, PedColumnDataType>> FHWA13_Simple = new Dictionary<int, KeyValuePair<string, PedColumnDataType>>(Constants.MAX_BANKS_ALLOWED);

    public static List<Dictionary<int, KeyValuePair<string, PedColumnDataType>>> presetList = new List<Dictionary<int, KeyValuePair<string, PedColumnDataType>>>
    {
      Standard, NCDOT, TPA_RTOR, TEX_UTurns, Buses, PortOfPortland, FHWA13, FHWA13_Simple
    };

    public static List<string> presetNames = new List<string>()
    { 
      PRESET_NAME_STANDARD, 
      PRESET_NAME_NCDOT, 
      PRESET_NAME_TPA_RTORS, 
      PRESET_NAME_TEX_U_TURN_LANES, 
      PRESET_NAME_BUSES,
      PRESET_NAME_PORT_OF_PORTLAND,
      PRESET_NAME_FHWA_13_VEH_CLASS,
      PRESET_NAME_FHWA_13_VEH_CLASS_SIMPLE
    };

    static GlobalBankPresets()
    {
      Standard[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Passenger.ToString(), PedColumnDataType.UTurn);
      Standard[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Heavies.ToString(), PedColumnDataType.NA);
      Standard[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.Pedestrian);
      Standard[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Buses.ToString(), PedColumnDataType.NA);
      Standard[4] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.E_Scooters.ToString(), PedColumnDataType.NA);

      NCDOT[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA1_2_3.ToString(), PedColumnDataType.PassengerUTurn);
      NCDOT[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA4_5_6_7.ToString(), PedColumnDataType.HeavyUTurn);
      NCDOT[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.Pedestrian);
      NCDOT[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA8_9_10.ToString(), PedColumnDataType.NA);
      NCDOT[4] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA11_12_13.ToString(), PedColumnDataType.NA);

      TPA_RTOR[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Passenger.ToString(), PedColumnDataType.Pedestrian);
      TPA_RTOR[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Heavies.ToString(), PedColumnDataType.RTOR);
      TPA_RTOR[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.UTurn);

      TEX_UTurns[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Passenger.ToString(), PedColumnDataType.Pedestrian);
      TEX_UTurns[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Heavies.ToString(), PedColumnDataType.NA);
      TEX_UTurns[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.NA);
      TEX_UTurns[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.TEX_UTurnLanePassenger.ToString(), PedColumnDataType.NA);
      TEX_UTurns[4] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.TEX_UTurnLaneHeavy.ToString(), PedColumnDataType.NA);

      Buses[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Passenger.ToString(), PedColumnDataType.Pedestrian);
      Buses[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Heavies.ToString(), PedColumnDataType.NA);
      Buses[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.UTurn);
      Buses[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Buses.ToString(), PedColumnDataType.NA);

      PortOfPortland[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Passenger.ToString(), PedColumnDataType.Pedestrian);
      PortOfPortland[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.SingleUnitHeavies.ToString(), PedColumnDataType.NA);
      PortOfPortland[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.Bicycles.ToString(), PedColumnDataType.UTurn);
      PortOfPortland[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.MultiUnitHeavies.ToString(), PedColumnDataType.NA);

      FHWA13[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA1.ToString(), PedColumnDataType.NA);
      FHWA13[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA2.ToString(), PedColumnDataType.NA);
      FHWA13[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA3.ToString(), PedColumnDataType.NA);
      FHWA13[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA4.ToString(), PedColumnDataType.NA);
      FHWA13[4] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA5.ToString(), PedColumnDataType.NA);
      FHWA13[5] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA6.ToString(), PedColumnDataType.NA);
      FHWA13[6] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA7.ToString(), PedColumnDataType.NA);
      FHWA13[7] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA8.ToString(), PedColumnDataType.NA);
      FHWA13[8] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA9.ToString(), PedColumnDataType.NA);
      FHWA13[9] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA10.ToString(), PedColumnDataType.NA);
      FHWA13[10] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA11.ToString(), PedColumnDataType.NA);
      FHWA13[11] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA12.ToString(), PedColumnDataType.NA);
      FHWA13[12] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA13.ToString(), PedColumnDataType.NA);
      FHWA13[13] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWAPedsBikes.ToString(), PedColumnDataType.Pedestrian);

      FHWA13_Simple[0] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA1.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[1] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA2.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[2] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA3.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[3] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA4.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[4] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA5.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[5] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWA6through13.ToString(), PedColumnDataType.NA);
      FHWA13_Simple[6] = new KeyValuePair<string, PedColumnDataType>(BankVehicleTypes.FHWAPedsBikes.ToString(), PedColumnDataType.Pedestrian);
      
      InitializeUnusedBanks();
    }

    public static List<string> presetNamesWithColumnSwapping = new List<string>()
    {
      PRESET_NAME_STANDARD,
      PRESET_NAME_NCDOT
    };

    public static List<string> presetNamesWithTccRules = new List<string>()
    {
      PRESET_NAME_FHWA_13_VEH_CLASS,
      PRESET_NAME_FHWA_13_VEH_CLASS_SIMPLE
    };

    private static void InitializeUnusedBanks()
    {
      foreach(Dictionary<int, KeyValuePair<string, PedColumnDataType>> preset in presetList)
      {
        for (int i = 0; i < Constants.MAX_BANKS_ALLOWED; i++)
        {
          if(!preset.ContainsKey(i))
          {
            preset[i] = new KeyValuePair<string,PedColumnDataType>("NOT USED", PedColumnDataType.NA);
          }
        }
      }
    }
  }

  public enum NoteType
  {
    ProjectLevel,
    IntersectionLevel,
    CountLevel_Conglomerate,
    CountLevel_Data,
    CountLevel_Balancing,
    CountLevel_Flag
  };

  public enum CompassBasic
  {
    North = 0,
    South = 180,
    East = 90,
    West = 270
  };

  public enum SurveyType
  {
    Unknown, TMC, TurnClass, TubeClass, TubeSpeed, TubeSpeedClass, TubeVolumeOnly
  };

  public enum TubeLayouts
  {
    Unknown, EB_WB, NB_SB, No_EB, No_NB, No_SB, No_WB
  }

}
