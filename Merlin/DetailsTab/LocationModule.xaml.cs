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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Merlin;
using AppMerlin;

namespace Merlin.DetailsTab
{
  /// <summary>
  /// Interaction logic for LocationModule.xaml
  /// </summary>
  public partial class LocationModule : UserControl
  {
    #region Class Variables

    //int numApproaches = 4;
    public string m_NumericTextBoxTracker; //tracks order number while entering in case of bad char

    public static readonly RoutedEvent LocationDeletedEvent =
    EventManager.RegisterRoutedEvent("LocationDeletedEvent", RoutingStrategy.Bubble,
    typeof(RoutedEventHandler), typeof(LocationModule));

    public List<string> m_SiteCodes;  //tracks the 8 site codes for each time period during edit mode
    public ListBox m_Parent;
    public int m_ID;
    public string m_Latitude;
    public string m_Longitude;
    private List<string> m_customizedMovements;  //will be null unless there are customized movements set for the intersection this LocationModule represents
    public List<string> m_CustomizedMovements
    {
      get
      {
        return m_customizedMovements;
      }
      set
      {
        m_customizedMovements = value;
        if(m_customizedMovements == null)
        {
          locationNumberTextBlock.Foreground = Brushes.Black;
          customizeMovementsButton.Foreground = Brushes.Black;
        }
        else
        {
          locationNumberTextBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#BE00ED");
          customizeMovementsButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#BE00ED");
        }
      }
    }

    #endregion

    #region Constructors

    public LocationModule(ListBox parent, int ID, string lat, string lon)
    {
      InitializeComponent();

      m_SiteCodes = new List<string> { "", "", "", "", "", "", "", ""};
      m_Parent = parent;
      m_ID = ID;
      m_Latitude = lat;
      m_Longitude = lon;
      m_CustomizedMovements = null;

      AddHandler(Merlin.CustomizeLocationsWindow.IntersectionConfig.LegChangedEvent, new RoutedEventHandler(Leg_Changed));
    }

    #endregion

    public void UpdateStateVisibility(DetailsTabState state)
    {
      if (state == DetailsTabState.Creating)
      {
        DeleteX.Visibility = Visibility.Visible;
      }
      else if (state == DetailsTabState.Editing)
      {
        DeleteX.Visibility = Visibility.Visible;
      }
      else if (state == DetailsTabState.Viewing)
      {
        DeleteX.Visibility = Visibility.Hidden;
      }
    }
    
    public event RoutedEventHandler LocationDeleted
    {
      add { AddHandler(LocationDeletedEvent, value); }
      remove { RemoveHandler(LocationDeletedEvent, value); }
    }

    //TODO: Andrew copied this code from MainWindow, need to define in one place.
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

    //TODO: Andrew copied this code from MainWindow, need to define in one place.
    //Numeric text boxes that use the tracker and NumericTextBox_TextChanged
    //to validate numeric entry only need to use this event handler
    public void NumericTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      m_NumericTextBoxTracker = "";
    }

    //mouse enters delete button area
    private void Grid_MouseEnter(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Bold;
      this.Cursor = Cursors.No;
    }

    //mouse leaves delete button area
    private void Grid_MouseLeave(object sender, MouseEventArgs e)
    {
      DeleteX.FontWeight = FontWeights.Normal;
      this.Cursor = Cursors.Arrow;
    }

    //Delete "x" clicked
    private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
    {
      RaiseEvent(new RoutedEventArgs(LocationModule.LocationDeletedEvent));
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Tab)
      {
        //pressing tab key cycles focus to the next street name text box
        TextBox tb = sender as TextBox;
        if(tb.Name == "NBSB")
        {
          EBWB.Focus();
        }
        else if(tb.Name == "EBWB")
        {
          int nextLocIndex = (m_Parent.Items.IndexOf(this) + 1) % (m_Parent.Items.OfType<LocationModule>().Count());
          ((LocationModule)m_Parent.Items.GetItemAt(nextLocIndex)).NBSB.Focus();
        }
        e.Handled = true;
        
        ////causes tab to go to next textbox only, cycling within the locations list box, old method
        //TraversalRequest tr = new TraversalRequest(FocusNavigationDirection.Next);
        //UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;
        //do
        //{
        //  if (keyboardFocus != null)
        //  {
        //    keyboardFocus.MoveFocus(tr);
        //  }
        //  keyboardFocus = Keyboard.FocusedElement as UIElement;
        //} while (keyboardFocus.GetType() != typeof(TextBox));// || ((TextBox)keyboardFocus).Name != "NBSB" || ((TextBox)keyboardFocus).Name != "EBWB");
        //e.Handled = true;
      }
    }

    public void ExpandModule()
    {
      mainGrid.RowDefinitions[1].Height = new GridLength(68);
      diagram.Visibility = Visibility.Visible;
    }

    public void CondenseModule()
    {
      mainGrid.RowDefinitions[1].Height = new GridLength(0);
      diagram.Visibility = Visibility.Hidden;
    }

    private void diagram_MouseEnter(object sender, MouseEventArgs e)
    {
      locationNumberTextBlock.Visibility = Visibility.Collapsed;
      customizeMovementsButton.Visibility = Visibility.Visible;
    }

    private void diagram_MouseLeave(object sender, MouseEventArgs e)
    {
      locationNumberTextBlock.Visibility = Visibility.Visible;
      customizeMovementsButton.Visibility = Visibility.Collapsed;
    }

    private void customizeMovementsButton_Click(object sender, RoutedEventArgs e)
    {
      Dictionary<StandardIntersectionApproaches, IntersectionApproach> uiApproaches = new Dictionary<StandardIntersectionApproaches, IntersectionApproach>();
      uiApproaches.Add(StandardIntersectionApproaches.SB, new IntersectionApproach("", PossibleIntersectionApproaches.SB, diagram.GetLegFlow(StandardIntersectionApproaches.SB)));
      uiApproaches.Add(StandardIntersectionApproaches.WB, new IntersectionApproach("", PossibleIntersectionApproaches.WB, diagram.GetLegFlow(StandardIntersectionApproaches.WB)));
      uiApproaches.Add(StandardIntersectionApproaches.NB, new IntersectionApproach("", PossibleIntersectionApproaches.NB, diagram.GetLegFlow(StandardIntersectionApproaches.NB)));
      uiApproaches.Add(StandardIntersectionApproaches.EB, new IntersectionApproach("", PossibleIntersectionApproaches.EB, diagram.GetLegFlow(StandardIntersectionApproaches.EB)));

      CustomizeMovementsWindow cmw;
      if(m_CustomizedMovements == null)
      {
        //no movements have been customized, let the window calculate standard possible movements
        cmw = new CustomizeMovementsWindow(uiApproaches);
      }
      else
      {
        //movements have been customized, pass the customized movements to the window
        cmw = new CustomizeMovementsWindow(uiApproaches, m_CustomizedMovements);
      }
      cmw.ShowDialog();
      if(cmw.movementsWereChanged)
      {
        m_CustomizedMovements = cmw.m_IncludedMovements;
      }

    }

    private void Leg_Changed(object sender, RoutedEventArgs e)
    {
      //this means user changed a leg in the intersection diagram, nullify customized movements if they existed
      m_CustomizedMovements = null;
    }
  }
}
