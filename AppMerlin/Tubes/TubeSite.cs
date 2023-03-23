using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AppMerlin
{
  public class TubeSite : Location
  {
    private TubeLayouts m_Layout;

    public string m_Location = "";

    public List<TubeCount> m_TubeCounts;

    #region Constructors

    public TubeSite()
    {
      m_TubeCounts = new List<TubeCount>();
    }

    public TubeSite(TMCProject parentProject, int ID, string lat, string lon, string location, TubeLayouts layout)
      : base(parentProject, ID, lat, lon)
    {
      m_Location = location;
      m_Layout = layout;
      m_TubeCounts = new List<TubeCount>();
    }

    #endregion

    #region Properties

    public TubeLayouts TubeLayout
    {
      get
      {
        return m_Layout;
      }
      set
      {
        if (m_Layout != value && m_Layout != TubeLayouts.Unknown)
        {
          //when changed, drop existing data for all children TubeCounts
          foreach (TubeCount tc in m_TubeCounts)
          {
            tc.m_Data = new DataSet();
          }
        }
        m_Layout = value;
      }
    }


    #endregion

    #region Private Methods

    private TubeCount GetTubeCountContainingInterval(DateTime interval)
    {
      foreach (TubeCount tc in m_TubeCounts)
      {
        if (tc.ContainsInterval(interval))
        {
          return tc;
        }
      }
      return null;
    }

    #endregion

    #region Public Methods

    public BitmapImage GetImage()
    {
      string path = "../Resources/";
      string file;
      List<BalancingInsOuts> connections = GetPossibleBalancingConnections();

      if(connections.Contains(BalancingInsOuts.EBEntering) && connections.Contains(BalancingInsOuts.WBEntering))
      {
        file = "TubeEBWB.png";
      }
      else if(connections.Contains(BalancingInsOuts.NBEntering) && connections.Contains(BalancingInsOuts.SBEntering))
      {
        file = "TubeNBSB.png";
      }
      else if(connections.Contains(BalancingInsOuts.EBEntering))
      {
        file = "TubeNoWB.png";
      }
      else if(connections.Contains(BalancingInsOuts.WBEntering))
      {
        file = "TubeNoEB.png";
      }
      else if(connections.Contains(BalancingInsOuts.NBEntering))
      {
        file = "TubeNoSB.png";
      }
      else if(connections.Contains(BalancingInsOuts.SBEntering))
      {
        file = "TubeNoNB.png";
      }
      else
      {
        file = "turkey.png";
      }

      return new BitmapImage(new Uri(path + file, UriKind.Relative));
    }

    public List<DateTime> GetTubeDates()
    {
      List<DateTime> dates = new List<DateTime>();
      foreach(TubeCount tc in m_TubeCounts)
      {
        foreach(DateTime date in tc.GetDatesContainingData())
        {
          if(!dates.Contains(date.Date))
          {
            dates.Add(date.Date);
          }
        }
      }
      return dates;
    }
    
    public List<BalancingInsOuts> GetPossibleBalancingConnections()
    {
      return GetPossibleConnectionsForCurrentLayout();
    }

    public int GetConnectionVolume(BalancingInsOuts connection, DateTime firstInterval, TimeSpan length, bool includeUnclassed, List<BankVehicleTypes> FHWAClasses = null)
    {
      TubeCount tc = GetTubeCountContainingInterval(firstInterval);
      if(tc == null)
      {
        return 0;
        //throw new NullReferenceException(string.Format("No tube count exists for this tube site that contains data in the {0} interval for the {1} connection.", firstInterval, connection.ToString()));
      }
      //If caller is asking for the volume for a connection that is not possible given the current layout, just return a total of 0
      if(!GetPossibleConnectionsForCurrentLayout().Contains(connection))
      {
        return 0;
      }
      return tc.GetVolume(connection, firstInterval, length, includeUnclassed, FHWAClasses);
    }

    public List<BalancingInsOuts> GetPossibleConnectionsForCurrentLayout()
    {
      List<BalancingInsOuts> connections = new List<BalancingInsOuts>();
      switch (TubeLayout)
      {
        case TubeLayouts.EB_WB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.EBEntering, BalancingInsOuts.EBExiting, BalancingInsOuts.WBEntering, BalancingInsOuts.WBExiting});
          break;
        case TubeLayouts.NB_SB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.NBEntering, BalancingInsOuts.NBExiting, BalancingInsOuts.SBEntering, BalancingInsOuts.SBExiting});
          break;
        case TubeLayouts.No_EB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.WBEntering, BalancingInsOuts.WBExiting});
          break;
        case TubeLayouts.No_NB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.SBEntering, BalancingInsOuts.SBExiting});
          break;
        case TubeLayouts.No_SB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.NBEntering, BalancingInsOuts.NBExiting});
          break;
        case TubeLayouts.No_WB:
          connections.AddRange(new BalancingInsOuts[] { BalancingInsOuts.EBEntering, BalancingInsOuts.EBExiting});
          break;
        default:
          break;
      }
      return connections;
    }

    public override PossibleConnectionFlows GetTrafficFlowTypeAtConnection(BalancingInsOuts myConnection)
    {
      if(!GetPossibleBalancingConnections().Contains(myConnection))
      {
        return PossibleConnectionFlows.None;
      }
      switch(myConnection)
      {
        case BalancingInsOuts.EBEntering:
        case BalancingInsOuts.NBEntering:
        case BalancingInsOuts.SBEntering:
        case BalancingInsOuts.WBEntering:
          return PossibleConnectionFlows.In;
        case BalancingInsOuts.EBExiting:
        case BalancingInsOuts.NBExiting:
        case BalancingInsOuts.SBExiting:
        case BalancingInsOuts.WBExiting:
          return PossibleConnectionFlows.Out;
        default:
          return PossibleConnectionFlows.None;
      }
    }

    public override string GetLocationName()
    {
      return m_Location;
    }

    public override string GetLocationNumber()
    {
      return "T" + (m_ParentProject.m_Tubes.IndexOf(this) + 1).ToString();
    }

    #endregion
  }
}
