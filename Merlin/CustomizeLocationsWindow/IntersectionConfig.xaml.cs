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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Merlin.CustomizeLocationsWindow
{
  /// <summary>
  /// Interaction logic for IntersectionConfig.xaml
  /// </summary>
  public partial class IntersectionConfig : UserControl
  {
    //intersection image file names

    //arrows
    Uri nbsb;
    Uri ebwb;
    Uri nbOnly;
    Uri sbOnly;
    Uri ebOnly;
    Uri wbOnly;
    //background intersections
    Uri fourLegs;
    Uri noNB;
    Uri noSB;
    Uri noEB;
    Uri noWB;
    Uri noEBWB;
    Uri noEBWBNoLines;
    Uri noNBEB;
    Uri noNBEBNoLines;
    Uri noNBSB;
    Uri noNBSBNoLines;
    Uri noNBWB;
    Uri noNBWBNoLines;
    Uri noSBEB;
    Uri noSBEBNoLines;
    Uri noSBWB;
    Uri noSBWBNoLines;

    
    //these store the order of arrow images to cycle when legs clicked
    Uri[] topFileNames;
    Uri[] bottomFileNames;
    Uri[] leftFileNames;
    Uri[] rightFileNames;

    //current states of each leg, 0-2 are arrows on an a present leg, 3 means no leg present
    PossibleApproachFlows topState;
    PossibleApproachFlows bottomState;
    PossibleApproachFlows leftState;
    PossibleApproachFlows rightState;

    ////tracks whether each leg has an in or out
    //bool topIn { get; set { updateOneRemainingInOut();} }
    //bool topOut { get; set { updateOneRemainingInOut(); } }
    //bool bottomIn { get; set { updateOneRemainingInOut(); } }
    //bool bottomOut { get; set { updateOneRemainingInOut(); } }
    //bool leftIn { get; set { updateOneRemainingInOut(); } }
    //bool leftOut { get; set { updateOneRemainingInOut(); } }
    //bool rightIn { get; set { updateOneRemainingInOut(); } }
    //bool rightOut { get; set { updateOneRemainingInOut(); } }

    ////tracks if there is only one in or out left
    //bool onlyOneInLeft = false;
    //bool onlyOneOutLeft = false;

    public static readonly RoutedEvent LegChangedEvent =
      EventManager.RegisterRoutedEvent("LegChangedEvent", RoutingStrategy.Bubble,
      typeof(RoutedEventHandler), typeof(IntersectionConfig));
    
    public IntersectionConfig()
    {
      InitializeComponent();

      nbsb = new Uri("../Resources/NBSB_Arrows.png", UriKind.Relative);
      ebwb = new Uri("../Resources/EBWB_Arrows.png", UriKind.Relative);
      nbOnly = new Uri("../Resources/NBOnly_Arrow.png", UriKind.Relative);
      sbOnly = new Uri("../Resources/SBOnly_Arrow.png", UriKind.Relative);
      ebOnly = new Uri("../Resources/EBOnly_Arrow.png", UriKind.Relative);
      wbOnly = new Uri("../Resources/WBOnly_Arrow.png", UriKind.Relative);

      fourLegs = new Uri("../Resources/BlankIntersection.png", UriKind.Relative);
      noNB = new Uri("../Resources/BlankIntersectionNoNB.png", UriKind.Relative);
      noSB = new Uri("../Resources/BlankIntersectionNoSB.png", UriKind.Relative);
      noEB = new Uri("../Resources/BlankIntersectionNoEB.png", UriKind.Relative);
      noWB = new Uri("../Resources/BlankIntersectionNoWB.png", UriKind.Relative);
      noEBWB = new Uri("../Resources/BlankIntersectionNoEBWB.png", UriKind.Relative);
      noEBWBNoLines = new Uri("../Resources/BlankIntersectionNoEBWBNoLines.png", UriKind.Relative);
      noNBEB = new Uri("../Resources/BlankIntersectionNoNBEB.png", UriKind.Relative);
      noNBEBNoLines = new Uri("../Resources/BlankIntersectionNoNBEBNoLines.png", UriKind.Relative);
      noNBSB = new Uri("../Resources/BlankIntersectionNoNBSB.png", UriKind.Relative);
      noNBSBNoLines = new Uri("../Resources/BlankIntersectionNoNBSBNoLines.png", UriKind.Relative);
      noNBWB = new Uri("../Resources/BlankIntersectionNoNBWB.png", UriKind.Relative);
      noNBWBNoLines = new Uri("../Resources/BlankIntersectionNoNBWBNoLines.png", UriKind.Relative);
      noSBEB = new Uri("../Resources/BlankIntersectionNoSBEB.png", UriKind.Relative);
      noSBEBNoLines = new Uri("../Resources/BlankIntersectionNoSBEBNoLines.png", UriKind.Relative);
      noSBWB = new Uri("../Resources/BlankIntersectionNoSBWB.png", UriKind.Relative);
      noSBWBNoLines = new Uri("../Resources/BlankIntersectionNoSBWBNoLines.png", UriKind.Relative);

      topFileNames = new Uri[] {
        nbsb, sbOnly, nbOnly };
      bottomFileNames = new Uri[] {
        nbsb, nbOnly, sbOnly };
      leftFileNames = new Uri[] {
        ebwb, ebOnly, wbOnly };
      rightFileNames = new Uri[] {
        ebwb, wbOnly, ebOnly };

      ImageBackground.Source = new BitmapImage(fourLegs);

      topState = PossibleApproachFlows.TwoWay;
      bottomState = PossibleApproachFlows.TwoWay;
      leftState = PossibleApproachFlows.TwoWay;
      rightState = PossibleApproachFlows.TwoWay;

      ////tracks whether each leg has an in or out
      //topIn = true;
      //topOut = true;
      //bottomIn = true;
      //bottomOut = true;
      //leftIn = true;
      //leftOut = true;
      //rightIn = true;
      //rightOut = true;


    }

    public event RoutedEventHandler LegChanged
    {
      add { AddHandler(LegChangedEvent, value); }
      remove { RemoveHandler(LegChangedEvent, value); }
    }

    //Mouse hovers over a leg in the diagram, changes to a hand
    private void Leg_MouseEnter(object sender, MouseEventArgs e)
    {
      this.Cursor = Cursors.Hand;
      
      ////old code from when I used different cursors depending on leg
      //switch (((Image)sender).Name)
      //{
      //  case "Top":
      //    this.Cursor = Cursors.ScrollS;
      //    break;
      //  case "Bottom":
      //    this.Cursor = Cursors.ScrollN;
      //    break;
      //  case "Left":
      //    this.Cursor = Cursors.ScrollE;
      //    break;
      //  case "Right":
      //    this.Cursor = Cursors.ScrollW;
      //    break;
      //}
    }

    //hovering mouse leaves leg area, changes back to arrow
    private void Leg_MouseLeave(object sender, MouseEventArgs e)
    {
      this.Cursor = Cursors.Arrow;
    }

    //A leg is clicked, initiates change in state of that leg
    private void Leg_MouseDown(object sender, MouseButtonEventArgs e)
    {
      Image sendingLeg = (Image)sender;

      //determines which leg was clicked in order to call ChangeLegState function
      switch (((Image)sender).Name)
      {
        case "Top":
          ChangeLegState(sendingLeg, ref topState, topFileNames);
          break;
        case "Bottom":
          ChangeLegState(sendingLeg, ref bottomState, bottomFileNames);
          break;
        case "Left":
          ChangeLegState(sendingLeg, ref leftState, leftFileNames);
          break;
        case "Right":
          ChangeLegState(sendingLeg, ref rightState, rightFileNames);
          break;
      }
      RaiseEvent(new RoutedEventArgs(IntersectionConfig.LegChangedEvent));
    }

    //Changed a leg state to next state
    private void ChangeLegState(Image arrowImage, ref PossibleApproachFlows currentLegState, Uri[] orderedFileNamesForThisLeg)
    {
      PossibleApproachFlows originalLegState = currentLegState;

      currentLegState = NextStateAfter(currentLegState);
      while(currentLegState != originalLegState)
      {
        if(NumRemainingInsAndOuts()[0] > 0 && NumRemainingInsAndOuts()[1] > 0 && NumPedApproaches() <= 2)
        {
          break;
        }
        currentLegState = NextStateAfter(currentLegState);
      }
      if(originalLegState != currentLegState)
      {
        ChangeLegArrows(arrowImage, currentLegState, orderedFileNamesForThisLeg);
        UpdateBGImage();
      }
    }

    private void ChangeLegArrows(Image arrowImage, PossibleApproachFlows changeTo, Uri[] orderedFileNamesForThisLeg)
    {
      switch(changeTo)
      {
        case PossibleApproachFlows.TwoWay:
          arrowImage.Source = new BitmapImage(orderedFileNamesForThisLeg[0]);
          arrowImage.Opacity = 1;
          break;
        case PossibleApproachFlows.EnteringOnly:
          arrowImage.Source = new BitmapImage(orderedFileNamesForThisLeg[1]);
          arrowImage.Opacity = 1;
          break;
        case PossibleApproachFlows.ExitingOnly:
          arrowImage.Source = new BitmapImage(orderedFileNamesForThisLeg[2]);
          arrowImage.Opacity = 1;
          break;
        case PossibleApproachFlows.PedsOnly:
          arrowImage.Source = new BitmapImage(orderedFileNamesForThisLeg[0]);
          arrowImage.Opacity = 0;
          break;
      }
    }

    private PossibleApproachFlows NextStateAfter(PossibleApproachFlows currentState)
    {
      switch (currentState)
      {
        case PossibleApproachFlows.TwoWay:
          return PossibleApproachFlows.EnteringOnly;
        case PossibleApproachFlows.EnteringOnly:
          return PossibleApproachFlows.ExitingOnly;
        case PossibleApproachFlows.ExitingOnly:
          return PossibleApproachFlows.PedsOnly;
        case PossibleApproachFlows.PedsOnly:
          return PossibleApproachFlows.TwoWay;
        default:
          return PossibleApproachFlows.TwoWay;
      }
    }

    private int NumPedApproaches()
    {
      int total = 0;
      if (topState == PossibleApproachFlows.PedsOnly)
      {
        total++;
      }
      if (leftState == PossibleApproachFlows.PedsOnly)
      {
        total++;
      }
      if (bottomState == PossibleApproachFlows.PedsOnly)
      {
        total++;
      }
      if (rightState == PossibleApproachFlows.PedsOnly)
      {
        total++;
      }
      return total;
    }

    //Sets the background intersection image based on the flow type for all legs
    private void UpdateBGImage()
    {
      //no SB leg
      if (topState == PossibleApproachFlows.PedsOnly)
      {
        //and no EB leg
        if(leftState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(rightState, bottomState)) ? new BitmapImage(noSBEBNoLines) : new BitmapImage(noSBEB);
        }
        //and no NB leg
        else if(bottomState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(leftState, rightState)) ? new BitmapImage(noNBSBNoLines) : new BitmapImage(noNBSB);
        }
        //and no WB leg
        else if(rightState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(bottomState, leftState)) ? new BitmapImage(noSBWBNoLines) : new BitmapImage(noSBWB);
        }
        else
        {
          ImageBackground.Source = new BitmapImage(noSB);
        }
      }
      //no EB leg
      else if (leftState == PossibleApproachFlows.PedsOnly)
      {
        //and no NB leg
        if (bottomState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(rightState, topState)) ? new BitmapImage(noNBEBNoLines) : new BitmapImage(noNBEB);
        }
        //and no WB leg
        else if (rightState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(topState, bottomState)) ? new BitmapImage(noEBWBNoLines) : new BitmapImage(noEBWB);
        }
        else
        {
          ImageBackground.Source = new BitmapImage(noEB);
        }
      }
      //no NB leg
      else if (bottomState == PossibleApproachFlows.PedsOnly)
      {
        //and no WB leg
        if (rightState == PossibleApproachFlows.PedsOnly)
        {
          ImageBackground.Source = (IsOneWayMidBlock(topState, leftState)) ? new BitmapImage(noNBWBNoLines) : new BitmapImage(noNBWB);
        }
        else
        {
          ImageBackground.Source = new BitmapImage(noNB);
        }
      }
      //no WB leg
      else if(rightState == PossibleApproachFlows.PedsOnly)
      {
        ImageBackground.Source = new BitmapImage(noWB);
      }
      //has all 4 legs
      else
      {
        ImageBackground.Source = new BitmapImage(fourLegs);
      }
    }

    /// <summary>
    /// Determines if a midblock intersection has one-way traffic in the intersection based on the two approach flows
    /// </summary>
    /// <param name="sideA">One of the existing (not missing) sides</param>
    /// <param name="sideB">The other existing (not missing) side</param>
    /// <returns></returns>
    private bool IsOneWayMidBlock(PossibleApproachFlows sideA, PossibleApproachFlows sideB)
    {
      return (sideA != PossibleApproachFlows.TwoWay || sideB != PossibleApproachFlows.TwoWay);
    }

    //Changes a leg state to next state
    //private void ChangeLegState(Uri[] orderedFileNames, ref int state, ref Image leg, Uri noApproachImage, ref bool hasIn, ref bool hasOut)
    //{
    //  int numOuts = 0;
    //  int numIns = 0;
    //  if(bottomIn)
    //    numIns++;
    //  if(topIn)
    //    numIns++;
    //  if(leftIn)
    //    numIns++;
    //  if(rightIn)
    //    numIns++;
    //  if(bottomOut)
    //    numOuts++;
    //  if(topOut)
    //    numOuts++;
    //  if(leftOut)
    //    numOuts++;
    //  if(rightOut)
    //    numOuts++;
    //  if (state == 0)
    //  {
    //    if(!hasOut || (hasOut && numOuts > 1))
    //    {
    //      state = 1;
    //      leg.Source = new BitmapImage(orderedFileNames[state]);
    //      hasIn = true;
    //      hasOut = false;
    //      return;
    //    }
    //    else
    //    {
    //      state = 2;
    //      leg.Source = new BitmapImage(orderedFileNames[state]);
    //      hasIn = false;
    //      hasOut = true;
    //      return;
    //    }
    //  }
    //  if (state == 1)
    //  {
    //    if (!hasIn || (hasIn && numIns > 1))
    //    {
    //      state = 2;
    //      leg.Source = new BitmapImage(orderedFileNames[state]);
    //      hasIn = false;
    //      hasOut = true;
    //      return;
    //    }
    //    else
    //    {
    //      if (((BitmapImage)ImageBackground.Source).UriSource == fourLegs && (!hasIn || numIns > 1))
    //      {
    //        state = 3;
    //        ImageBackground.Source = new BitmapImage(noApproachImage);
    //        leg.Opacity = 0.0;
    //        hasIn = false;
    //        hasOut = false;
    //        return;
    //      }
    //      else
    //      {
    //        state = 0;
    //        leg.Opacity = 1.0;
    //        leg.Source = new BitmapImage(orderedFileNames[state]);
    //        if (((BitmapImage)ImageBackground.Source).UriSource == noApproachImage)
    //          ImageBackground.Source = new BitmapImage(fourLegs);
    //        hasIn = true;
    //        hasOut = true;
    //        return;
    //      }
    //    }
    //  }
    //  if (state == 2)
    //  {
    //    if (((BitmapImage)ImageBackground.Source).UriSource == fourLegs && (!hasOut || numOuts > 1))
    //    {
    //      state = 3;
    //      ImageBackground.Source = new BitmapImage(noApproachImage);
    //      leg.Opacity = 0.0;
    //      hasIn = false;
    //      hasOut = false;
    //      return;
    //    }
    //  }
    //  state = 0;
    //  leg.Opacity = 1.0;
    //  leg.Source = new BitmapImage(orderedFileNames[state]);
    //  if (((BitmapImage)ImageBackground.Source).UriSource == noApproachImage)
    //    ImageBackground.Source = new BitmapImage(fourLegs);
    //  hasIn = true;
    //  hasOut = true;
    //  return;
    //}

    /// <summary>
    /// Returns number of in our out currently in the intersection
    /// </summary>
    /// <returns></returns>
    private List<int> NumRemainingInsAndOuts()
    {
      int inCount = 0;
      int outCount = 0;

      foreach (PossibleApproachFlows approach in new List<PossibleApproachFlows>() { topState, leftState, bottomState, rightState})
      {
        switch (approach)
        {
          case PossibleApproachFlows.TwoWay:
            inCount++;
            outCount++;
            break;
          case PossibleApproachFlows.EnteringOnly:
            inCount++;
            break;
          case PossibleApproachFlows.ExitingOnly:
            outCount++;
            break;
          default:
            //This means it was ped only
            break;
        }
      }
      return new List<int>() { inCount, outCount };
    }

    //private void UpdateInOutBools(PossibleApproachFlows flow, ref bool inBool, ref bool outBool)
    //{
    //  switch (flow)
    //  {
    //    case PossibleApproachFlows.TwoWay:
    //      inBool = true;
    //      outBool = true;
    //      break;
    //    case PossibleApproachFlows.EnteringOnly:
    //      inBool = true;
    //      outBool = false;
    //      break;
    //    case PossibleApproachFlows.ExitingOnly:
    //      inBool = false;
    //      outBool = true;
    //      break;
    //    case PossibleApproachFlows.PedsOnly:
    //      inBool = false;
    //      outBool = false;
    //      break;
    //  }
    //}

    #region public methods

    //returns flow type of leg given in the argument
    public PossibleApproachFlows GetLegFlow(StandardIntersectionApproaches approach)
    {
      //int state = -1;

      //determine which leg state to look at
      switch(approach)
      {
        case StandardIntersectionApproaches.NB:
          return bottomState;
        case StandardIntersectionApproaches.SB:
          return topState;
        case StandardIntersectionApproaches.EB:
          return leftState;
        case StandardIntersectionApproaches.WB:
          return rightState;
        default:
          return rightState;
      }

      ////translate state as int (0 = two way, 1 = in only, 2 = out only, 3 = none/ped only) to PossibleApproachFlows type
      //switch(state)
      //{
      //  case 0:
      //    return PossibleApproachFlows.TwoWay;
      //  case 1:
      //    return PossibleApproachFlows.EnteringOnly;
      //  case 2:
      //    return PossibleApproachFlows.ExitingOnly;
      //  case 3:
      //    return PossibleApproachFlows.PedsOnly;
      //  default:
      //    return PossibleApproachFlows.TwoWay;
      //}
    }

    //Configures the diagram legs based on the PossibleApproachFlows passed in, CURRENTLY RELIES ON CALLER TO NOT PASS IMPOSSIBLE CONFIGURATIONS!
    public void SetLegFlows(PossibleApproachFlows SB, PossibleApproachFlows WB, PossibleApproachFlows NB, PossibleApproachFlows EB)
    {
      SetALeg(SB, ref topState, Top, topFileNames, noSB);
      SetALeg(WB, ref rightState, Right, rightFileNames, noWB);
      SetALeg(NB, ref bottomState, Bottom, bottomFileNames, noNB);
      SetALeg(EB, ref leftState, Left, leftFileNames, noEB);
    }

    //Helper function of SetLegFlows to set a leg
    private void SetALeg(PossibleApproachFlows desiredFlow, ref PossibleApproachFlows legState, Image legImage, Uri[] orderedFileNames, Uri noApproachImage)
    {
      legState = desiredFlow;

      switch (desiredFlow)
      {
        case PossibleApproachFlows.TwoWay:
          legImage.Opacity = 1.0;
          legImage.Source = new BitmapImage(orderedFileNames[0]);
          break;
        case PossibleApproachFlows.EnteringOnly:
          legImage.Opacity = 1.0;
          legImage.Source = new BitmapImage(orderedFileNames[1]);
          break;
        case PossibleApproachFlows.ExitingOnly:
          legImage.Opacity = 1.0;
          legImage.Source = new BitmapImage(orderedFileNames[2]);
          break;
        case PossibleApproachFlows.PedsOnly:
          legImage.Opacity = 0.0;
          ImageBackground.Source = new BitmapImage(noApproachImage);
          break;
      }

      UpdateBGImage();
    }

    #endregion

  }
}
