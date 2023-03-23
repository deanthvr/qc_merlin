using AppMerlin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Merlin.DetailsTab
{
  /// <summary>
  /// Interaction logic for CustomizeMovementsWindow.xaml
  /// </summary>
  public partial class CustomizeMovementsWindow : Window
  {
    private Dictionary<StandardIntersectionApproaches, IntersectionApproach> m_ApproachTypes;
    public List<string> m_IncludedMovements;
    private List<CheckBox> uiMovementCheckBoxes = new List<CheckBox>();
    private List<StackPanel> uiMovementStackPanels = new List<StackPanel>();
    public bool movementsWereChanged = false;
    
    #region Constructors

    public CustomizeMovementsWindow(Dictionary<StandardIntersectionApproaches, IntersectionApproach> approachTypes, List<string> includedMovements)
    {
      Setup(approachTypes, includedMovements);
    }

    //This constructor assumes default movements based on the approach types, meant for locations that haven't had their movements customized before
    public CustomizeMovementsWindow(Dictionary<StandardIntersectionApproaches, IntersectionApproach> approachTypes)
    {
      Setup(approachTypes, Intersection.CalculateStandardMovements(approachTypes.Values.ToList()));
    }

    private void Setup(Dictionary<StandardIntersectionApproaches, IntersectionApproach> approachTypes, List<string> includedMovements)
    {
      InitializeComponent();

      m_ApproachTypes = approachTypes;
      m_IncludedMovements = includedMovements;
      uiMovementCheckBoxes = new List<CheckBox>()
      {
        checkBoxSBR, checkBoxSBT, checkBoxSBL, checkBoxSBU,
        checkBoxWBR, checkBoxWBT, checkBoxWBL, checkBoxWBU,
        checkBoxNBR, checkBoxNBT, checkBoxNBL, checkBoxNBU,
        checkBoxEBR, checkBoxEBT, checkBoxEBL, checkBoxEBU
      };
      uiMovementStackPanels = new List<StackPanel>()
      {
        SBR, SBT, SBL, SBU,
        WBR, WBT, WBL, WBU,
        NBR, NBT, NBL, NBU,
        EBR, EBT, EBL, EBU
      };

      DisplayMovementsThatCouldPossiblyHappen(approachTypes);
      SelectIncludedMovements(includedMovements);
      ConfigureApproaches();
    }

    #endregion

    #region Functions

    /// <summary>
    /// Makes visible the movements that could possibly (but not necessarily) happen based on the approach types.
    /// </summary>
    /// <param name="approachTypes">LIst of approaches from which the possible movements will be calculated</param>
    private void DisplayMovementsThatCouldPossiblyHappen(Dictionary<StandardIntersectionApproaches, IntersectionApproach> approachTypes)
    {
      //possible movements here means they could possibly be included in permitted intersection movements based on approach types
      List<string> possibleMovements = Intersection.CalculateStandardMovements(approachTypes.Values.ToList());

      foreach(StackPanel sp in uiMovementStackPanels)
      {
        if(possibleMovements.Contains(sp.Name))
        {
          sp.Visibility = Visibility.Visible;
        }
        else
        {
          sp.Visibility = Visibility.Collapsed;
        }
      }
    }

    /// <summary>
    /// Checks or unchecks all movement checkboxes based on list of movements passed in.
    /// </summary>
    /// <param name="includedMovements">List of movements that will be checked</param>
    private void SelectIncludedMovements(List<string> includedMovements)
    {
      foreach(CheckBox cb in uiMovementCheckBoxes)
      {
        //don't love that this is hardcoded, will break if we ever change the name of the checkboxes...
        if(includedMovements.Contains(cb.Name.Substring(8)))
        {
          cb.IsChecked = true;
        }
        else
        {
          cb.IsChecked = false;
        }
      }
    }

    private void ConfigureApproaches()
    {
      diagram.SetLegFlows(m_ApproachTypes[StandardIntersectionApproaches.SB].TrafficFlowType,
                          m_ApproachTypes[StandardIntersectionApproaches.WB].TrafficFlowType,
                          m_ApproachTypes[StandardIntersectionApproaches.NB].TrafficFlowType,
                          m_ApproachTypes[StandardIntersectionApproaches.EB].TrafficFlowType);
    }

    private List<string> GetSelectedMovements()
    {
      List<string> selectedMovements = new List<string>();

      foreach(CheckBox cb in uiMovementCheckBoxes)
      {
        if((bool)cb.IsChecked)
        {
          selectedMovements.Add(cb.Name.Substring(8));
        }
      }
      return selectedMovements;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      List<string> currentlyIncludedMovements = GetSelectedMovements();

      //Get set differences between currently selected and originally selected movements to determine if the user made changes
      List<string> a = currentlyIncludedMovements.Except(m_IncludedMovements).ToList<string>();
      List<string> b = m_IncludedMovements.Except(currentlyIncludedMovements).ToList<string>();

      if(a.Count == 0 && b.Count == 0)
      {
        movementsWereChanged = false;
      }
      else
      {
        movementsWereChanged = true;
        m_IncludedMovements = currentlyIncludedMovements;
      }
    }

    #endregion

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
      this.Closing -= Window_Closing;
      this.Close();
      this.Closing += Window_Closing;
    }

    private void acceptButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
