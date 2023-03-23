
namespace Merlin
{
  public enum ProjectState
  {
    Loaded, Empty
  };

  public enum DetailsTabState
  {
    Creating, Editing, Viewing
  };

  public enum DataTabState
  {
    Empty, Loaded
  };

  public enum BalancingTabState
  {
    ViewNeighbors, ViewTotals, ViewDifference
  };

  public enum FlagTabState
  {
    Empty, Generated, Accepted
  };

  public enum ExportState
  {
    None, Some, All
  }


  public class StateMachine
  {
    public ProjectState m_ProjectState;
    public DetailsTabState m_DetailsTabState;
    public bool m_CreatingWithImportedDetails;
    public DataTabState m_DataTabState;
    public BalancingTabState m_BalancingTabState;
    public FlagTabState m_FlagTabState;
    public bool m_DataTabVisited;
    public bool m_FlagTabVisited;
    public bool m_NotesTabVisited;
    public ExportState m_ExportState;


    public StateMachine()
    {
      m_ProjectState = ProjectState.Empty;
      m_DetailsTabState = DetailsTabState.Creating;
      m_CreatingWithImportedDetails = false;
      m_DataTabState = DataTabState.Empty;
      m_BalancingTabState = BalancingTabState.ViewTotals;
      m_FlagTabState = FlagTabState.Empty;
      m_ExportState = ExportState.None;
      m_DataTabVisited = false;
      m_FlagTabVisited = false;
      m_NotesTabVisited = false;
    }

    public void ResetStates()
    {
      m_ProjectState = ProjectState.Empty;
      m_DetailsTabState = DetailsTabState.Creating;
      m_DataTabState = DataTabState.Empty;
      m_BalancingTabState = BalancingTabState.ViewTotals;
      //m_FlagTabState = FlagTabState.Empty;
      m_ExportState = ExportState.None;
      m_DataTabVisited = false;
      m_NotesTabVisited = false;
      m_FlagTabVisited = false;
    }
  }
}
