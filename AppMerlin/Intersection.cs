using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

namespace AppMerlin
{

  //Builds intersection geometry
  public class Intersection : Location
  {
    public bool m_HasGreaterThanFourLegs;
    public int Index;
    public List<IntersectionApproach> m_ApproachesInThisIntersection;
    public List<string> m_MovementsInThisIntersection;
    public List<Count> m_Counts;

    #region Constructors

    //default constructor
    public Intersection()
    {
      m_HasGreaterThanFourLegs = false;
      m_ApproachesInThisIntersection = new List<IntersectionApproach>();
      m_MovementsInThisIntersection = new List<string>();
      m_Counts = new List<Count>();
      Index = -1;
    }

    //Constructor for standard <=4-approach intersection.
    public Intersection(string SB, PossibleApproachFlows SBflow,
                        string NB, PossibleApproachFlows NBflow,
                        string WB, PossibleApproachFlows WBflow,
                        string EB, PossibleApproachFlows EBflow, 
                        TMCProject parentProject, bool hasFiveOrMoreLegs, int ID, string lat, string lon) : base(parentProject, ID, lat, lon)
    {
      m_HasGreaterThanFourLegs = hasFiveOrMoreLegs;
      m_ApproachesInThisIntersection = new List<IntersectionApproach>();
      m_MovementsInThisIntersection = new List<string>();
      m_Counts = new List<Count>();
      Index = -1;

      //note: other functions currently rely on approaches being added to the list in this order (IntersectionConfig.SetLegFlows())
      m_ApproachesInThisIntersection.Add(new IntersectionApproach(NB, PossibleIntersectionApproaches.NB, NBflow));
      m_ApproachesInThisIntersection.Add(new IntersectionApproach(SB, PossibleIntersectionApproaches.SB, SBflow));
      m_ApproachesInThisIntersection.Add(new IntersectionApproach(EB, PossibleIntersectionApproaches.EB, EBflow));
      m_ApproachesInThisIntersection.Add(new IntersectionApproach(WB, PossibleIntersectionApproaches.WB, WBflow));

      GenerateIntersectionMovements();

    }

    ////Constructor for any other geometry
    //public Intersection()
    //{
    //  //Will write this method if/when QC orders 5+ leg module
    //}

    #endregion

    #region Accessors

    public string GetSBNBApproach()
    {
      string streetName = "";

      foreach (var approach in m_ApproachesInThisIntersection)
      {
        if (approach.ApproachDirection == PossibleIntersectionApproaches.SB || approach.ApproachDirection == PossibleIntersectionApproaches.NB)
        {
          streetName = approach.ApproachName;
        }
      }
      return streetName;
    }

    public string GetWBEBApproach()
    {
      string streetName = "";

      foreach (var approach in m_ApproachesInThisIntersection)
      {
        if (approach.ApproachDirection == PossibleIntersectionApproaches.WB || approach.ApproachDirection == PossibleIntersectionApproaches.EB)
        {
          streetName = approach.ApproachName;
        }
      }
      return streetName;
    }

    private void GetCrossPeakCounts(List<int> amTimes, List<int> pmTimes, out Count am, out Count pm)
    {
      am = new Count();
      pm = new Count();
      if (amTimes.Count > 1)
      {
        List<KeyValuePair<int, int>> ams = new List<KeyValuePair<int, int>>();
        for (int i = 0; i < amTimes.Count; i++)
        {
          for (int j = 0; j < m_Counts.Count; j++)
          {
            if (amTimes[i] == m_Counts[j].m_TimePeriodIndex)
            {
              ams.Add(new KeyValuePair<int, int>(m_Counts[j].GetCountDataTotal(), j));
            }
          }
        }
        int currentHigh = 0;
        for (int c = 0; c < ams.Count; c++)
        {
          if (ams[c].Key > currentHigh)
          {
            am = m_Counts[ams[c].Value];
          }
        }
      }
      else
      {
        for (int j = 0; j < m_Counts.Count; j++)
        {
          if (amTimes[0] == m_Counts[j].m_TimePeriodIndex)
          {
            am = m_Counts[j];
          }
        }
      }

      if (pmTimes.Count > 1)
      {
        List<KeyValuePair<int, int>> pms = new List<KeyValuePair<int, int>>();
        for (int i = 0; i < pmTimes.Count; i++)
        {
          for (int j = 0; j < m_Counts.Count; j++)
          {
            if (pmTimes[i] == m_Counts[j].m_TimePeriodIndex)
            {
              pms.Add(new KeyValuePair<int, int>(m_Counts[j].GetCountDataTotal(), j));
            }
          }
        }
        int currentHigh = 0;
        for (int c = 0; c < pms.Count; c++)
        {
          if (pms[c].Key > currentHigh)
          {
            pm = m_Counts[pms[c].Value];
          }
        }
      }
      else
      {
        for (int j = 0; j < m_Counts.Count; j++)
        {
          if (pmTimes[0] == m_Counts[j].m_TimePeriodIndex)
          {
            pm = m_Counts[j];
          }
        }
      }
    }

    public override PossibleConnectionFlows GetTrafficFlowTypeAtConnection(BalancingInsOuts myConnection)
    {
      bool myConnIsEntering = Location.IsConnectionEntering(myConnection);
      //Had to initialize bogus value to these two to avoid Visual Studio error, will get assigned later
      IntersectionApproach myLegObject = GetApproachObjContainingConnection(myConnection);

      if(myConnIsEntering)
      {
        if(myLegObject.TrafficFlowType == PossibleApproachFlows.TwoWay || myLegObject.TrafficFlowType == PossibleApproachFlows.EnteringOnly)
        {
          return PossibleConnectionFlows.In;
        }
      }
      else
      {
        if (myLegObject.TrafficFlowType == PossibleApproachFlows.TwoWay || myLegObject.TrafficFlowType == PossibleApproachFlows.ExitingOnly)
        {
          return PossibleConnectionFlows.Out;
        }
      }
      return PossibleConnectionFlows.None;
    }

    public override string GetLocationName()
    {
      return GetSBNBApproach() + " & " + GetWBEBApproach();
    }

    public override string GetLocationNumber()
    {
      return (m_ParentProject.m_Intersections.IndexOf(this) + 1).ToString();
    }

    #endregion

    #region Helper Functions

    //Generates intersection movements by adding list of approaches which are the destinations
    //of each approach and also generating string list of all movements in intersection
    public void GenerateIntersectionMovements()
    {
      m_MovementsInThisIntersection.Clear();
      m_MovementsInThisIntersection.AddRange(CalculateStandardMovements(m_ApproachesInThisIntersection));
    }

    public void SetIntersectionMovements(List<string> movements)
    {
      m_MovementsInThisIntersection = movements;
    }

    //returns a list of standard movements that for the passed in list of approaches
    static public List<string> CalculateStandardMovements(List<IntersectionApproach> approaches)
    {
      List<string> movements = new List<string>();
      
      //loop through each approach to evaluate its vehicle movements
      foreach (IntersectionApproach approach in approaches)
      {
        if (approach.TrafficFlowType != PossibleApproachFlows.ExitingOnly && approach.TrafficFlowType != PossibleApproachFlows.PedsOnly)
        {
          //for each approach, check each approach to see if it's a destination
          foreach (IntersectionApproach possibleDestination in approaches)
          {
            //for each leg, see if it is a destination for the current approach
            if (possibleDestination.TrafficFlowType != PossibleApproachFlows.EnteringOnly && possibleDestination.TrafficFlowType != PossibleApproachFlows.PedsOnly)
            {
              //add movement to overall string list of intersection movements
              string fullMovementName = approach.ApproachDirection.ToString() + CalculateMovementName(approach, possibleDestination).ToString()[0];

              movements.Add(fullMovementName);
            }
          }
        }
      }
      return movements;
    }

    //Currently only works for <= 4-approach intersection using NB SB EB WB approaches
    static private MovementNames CalculateMovementName(IntersectionApproach origin, IntersectionApproach destination)
    {
      double origMinusDest = origin.ApproachHeading - destination.ApproachHeading;

      if (origMinusDest == -180.0 || origMinusDest == 180.0)
        return MovementNames.Through;
      else if (origMinusDest == -270.0 || origMinusDest == 90.0)
        return MovementNames.Right;
      else if (origMinusDest == -90.0 || origMinusDest == 270.0)
        return MovementNames.Left;
      else if (origMinusDest == 0.0)
        return MovementNames.UTurn;
      else
        //had to return a movement name, but this shouldn't happen. If it does, will return unexpected
        //"HardLeft" name.
        return MovementNames.HardLeft;

    }

    /// <summary>
    /// Used to convert neighbor connection array (for XML serialization) for versions before 4.0.5 where other intersections were referred to by their index in m_Project's list of intersections to now be referenced by their location IDs.
    /// </summary>
    /// <param name="proj">Need to pass parent project since this function gets called before non-serializable members are set and m_ParentProject is currently not serialized</param>
    public void UpdateArrayNeighborReferenceID(TMCProject proj)
    {
      for (int i = 0; i < xmlNeighbors.Length / 3; i++)
      {
        //Get the intersection by index which the way this old project version stores references to other intersections.
        Intersection intersection = proj.m_Intersections[xmlNeighbors[i * 3 + 1]];
        xmlNeighbors[i * 3 + 1] = intersection.Id;
      }
    }

    private IntersectionApproach GetApproachObjContainingConnection(BalancingInsOuts connection)
    {
      PossibleIntersectionApproaches myLegName;

      switch ((int)connection / 2)
      {
        case 0:
          myLegName = PossibleIntersectionApproaches.SB;
          break;
        case 1:
          myLegName = PossibleIntersectionApproaches.WB;
          break;
        case 2:
          myLegName = PossibleIntersectionApproaches.NB;
          break;
        case 3:
          myLegName = PossibleIntersectionApproaches.EB;
          break;
        default:
          myLegName = PossibleIntersectionApproaches.EB;
          break;
      }
      return m_ApproachesInThisIntersection.Find(x => x.ApproachDirection == myLegName);
    }

    #endregion

    #region Public Methods

    //Add an approach. Does NOT currently prevent more than one of the same approach being added
    public void AddApproach(string name, PossibleIntersectionApproaches direction, PossibleApproachFlows flowType)
    {
      m_ApproachesInThisIntersection.Add(new IntersectionApproach(name, direction, flowType));

      //re-generate movements now that approach has been added
      GenerateIntersectionMovements();
    }

    public void CrossPeakSuspiciousFlowTest(List<int> amTimes, List<int> pmTimes)
    {
      if (amTimes == null || pmTimes == null || amTimes.Count < 1 || pmTimes.Count < 1 || m_Counts.Count < 2)
      {
        return;
      }

      double crossPeakRatioThreshold = m_ParentProject.m_Prefs.m_CrossPeakDiff;
      Count amCount;
      Count pmCount;
      GetCrossPeakCounts(amTimes, pmTimes, out amCount, out pmCount);
      if (amCount.m_Id == null || pmCount.m_Id == null)
      {
        // If internal logging is created, write something out here.
        return;
      }
      List<KeyValuePair<string, string>> crossMovements = new List<KeyValuePair<string,string>>
      {
        {new KeyValuePair<string, string>("SBEntering", "NBExiting")},
        {new KeyValuePair<string, string>("WBEntering", "EBExiting")},
        {new KeyValuePair<string, string>("NBEntering", "SBExiting")},
        {new KeyValuePair<string, string>("EBEntering", "WBExiting")}
      };

      foreach(KeyValuePair<string,string> cm in crossMovements)
      {
        double amKey = amCount.GetBalancingSumByDirection(cm.Key);
        double amValue = amCount.GetBalancingSumByDirection(cm.Value);
        double pmKey = pmCount.GetBalancingSumByDirection(cm.Key);
        double pmValue = pmCount.GetBalancingSumByDirection(cm.Value);
        if (amKey < 1 || amValue < 1 || pmKey < 1 || pmValue < 1)
        {
          continue;
        }
        double amRatio = amKey / amValue;
        double pmRatio = pmValue / pmKey;

        if (System.Math.Abs(amRatio - pmRatio) > crossPeakRatioThreshold)
        {
          //string information = "Suspicious of cross peak traffic flow for " + cm.Key + " and " + cm.Value +". Inverse ratios are different by more than " + crossPeakRatioThreshold;
          string information = cm.Key + "/" + cm.Value + " Ratio discrepancy between AM/PM. AM: " + amRatio.ToString("n2") + " PM: " + pmRatio.ToString("n2") + " (PM Inverted).";

          if (amCount.m_Flags.All(f => f.m_Key != (amCount.m_Id.Substring(6, 2) + FlagType.SuspiciousTrafficFlow + information)))
          {
            amCount.m_Flags.Add(new Flag(FlagType.SuspiciousTrafficFlow, amCount,
              "Combined Banks", information));
          }
        }
      }
    }

    public void MovementSuspiciousFlowTest()
    {
      if (m_Counts.Count < 2)
      {
        return;
      }
      int averageMinimum = m_ParentProject.m_Prefs.m_TimePeriodMin;
      double lowerRatioThreshold = m_ParentProject.m_Prefs.m_TimePeriodLow;
      double upperRatioThreshold = m_ParentProject.m_Prefs.m_TimePeriodHigh;
      List<string> columns = m_ParentProject.m_ColumnHeaders[0];

      for (int i = 1; i < columns.Count; i++)
      {
        bool isPedColumn = i == 4 || i == 8 || i == 12 || i == 16;
        List<double> movementAverages = new List<double>();
        double carsPerHourAverage = 0;
        if (!isPedColumn)
        {
          for (int j = 0; j < m_Counts.Count; j++)
          {
            movementAverages.Add((m_Counts[j].GetMovementTotal(i) / m_Counts[j].m_NumIntervals) * 12);
            carsPerHourAverage += movementAverages[j];
          }
          carsPerHourAverage = carsPerHourAverage / m_Counts.Count;
          if (carsPerHourAverage < averageMinimum)
          {
            continue;
          }
          for (int k = 0; k < movementAverages.Count; k++)
          {
            double ratio = movementAverages[k] / carsPerHourAverage;
            if (ratio < lowerRatioThreshold)
            {
              string percent = (lowerRatioThreshold * 100).ToString("n2").Split('.')[0] + "%";
              string information = columns[i] + " Volume per hour (" + movementAverages[k] + ") less than " + percent
                + " of average for this location. (" + carsPerHourAverage + ")";

              if (m_Counts[k].m_Flags.All(f => f.m_Key != (m_Counts[k].m_Id.Substring(6, 2) + FlagType.SuspiciousMovement + information)))
              {
                m_Counts[k].m_Flags.Add(new Flag(FlagType.SuspiciousMovement, m_Counts[k],
                  m_ParentProject.m_BankDictionary[0], information, columns[i]));
              }
            }
            else if (ratio > upperRatioThreshold)
            {
              string percent2 = (upperRatioThreshold * 100).ToString("n2").Split('.')[0] + "%";
              string information2 = columns[i] + " Volume per hour more (" + movementAverages[k] + ") than " + percent2
                + " of average for this location. (" + carsPerHourAverage + ")";

              if (m_Counts[k].m_Flags.All(f => f.m_Key != (m_Counts[k].m_Id.Substring(6, 2) + FlagType.SuspiciousMovement + information2)))
              {
                m_Counts[k].m_Flags.Add(new Flag(FlagType.SuspiciousMovement, m_Counts[k],
                  "Combined Banks", information2, columns[i]));
              }
            }
          }
        }
        else
        {
          for (int b = 0; b < m_ParentProject.m_Banks.Count; b++)
          {
            if (m_ParentProject.m_Banks[b] == "NOT USED" && m_ParentProject.m_PedBanks[b] == PedColumnDataType.NA)
            {
              continue;
            }
            movementAverages = new List<double>();
            carsPerHourAverage = 0;
            columns = m_ParentProject.m_ColumnHeaders[b];
            for (int j = 0; j < m_Counts.Count; j++)
            {
              movementAverages.Add((m_Counts[j].GetMovementTotal(i, new List<int> { b }) / m_Counts[j].m_NumIntervals) * 12);
              carsPerHourAverage += movementAverages[j];
            }
            carsPerHourAverage = carsPerHourAverage / m_Counts.Count;
            if (carsPerHourAverage < averageMinimum)
            {
              continue;
            }
            for (int k = 0; k < movementAverages.Count; k++)
            {
              double ratio = movementAverages[k] / carsPerHourAverage;
              if (ratio < lowerRatioThreshold)
              {
                string percent = (lowerRatioThreshold * 100).ToString("n2").Split('.')[0] + "%";
                string information = columns[i] + " Volume per hour less (" + movementAverages[k] + ") than " + percent
                  + " of average for this location. (" + carsPerHourAverage + ")";

                if (m_Counts[k].m_Flags.All(f => f.m_Key != (m_Counts[k].m_Id.Substring(6, 2) + FlagType.SuspiciousMovement + information)))
                {
                  m_Counts[k].m_Flags.Add(new Flag(FlagType.SuspiciousMovement, m_Counts[k],
                    "Combined Banks", information, columns[i]));
                }
              }
              else if (ratio > upperRatioThreshold)
              {
                string percent2 = (upperRatioThreshold * 100).ToString("n2").Split('.')[0] + "%";
                string information2 = columns[i] + " Volume per hour more (" + movementAverages[k] + ") than " + percent2
                  + " of average for this location. (" + carsPerHourAverage + ")";

                if (m_Counts[k].m_Flags.All(f => f.m_Key != (m_Counts[k].m_Id.Substring(6, 2) + FlagType.SuspiciousMovement + information2)))
                {
                  m_Counts[k].m_Flags.Add(new Flag(FlagType.SuspiciousMovement, m_Counts[k],
                    "Combined Banks", information2, columns[i]));
                }
              }
            }
          }
        }
      }
    }


    #endregion

  }
}