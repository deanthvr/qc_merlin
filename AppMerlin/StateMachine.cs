
namespace Merlin
{
  public enum ProjectState
  {
    Loaded, Empty
  };

  public enum DetailsWindowState
  {
    Creating, Editing, Viewing
  };

  public enum DataState
  {
    Empty, PartialData, FullData 
  };

  public enum FlagState
  {
    Empty, Generated
  };


  public class StateMachine
  {
    public ProjectState m_ProjectState;
    public DataState m_DataState;
    public FlagState m_FlagState;


    public StateMachine()
    {
      m_ProjectState = ProjectState.Empty;
      m_DataState = DataState.Empty;
      m_FlagState = FlagState.Empty;
    }
  }
}
