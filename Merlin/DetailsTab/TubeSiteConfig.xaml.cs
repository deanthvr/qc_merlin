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

namespace Merlin.DetailsTab
{
  /// <summary>
  /// Interaction logic for TubeSiteConfig.xaml
  /// </summary>
  public partial class TubeSiteConfig : UserControl
  {
    //intersection image file names
    private Dictionary<TubeLayouts, Uri> layoutImages;

    public TubeSiteConfig()
    {
      InitializeComponent();

      layoutImages = new Dictionary<TubeLayouts, Uri>()
      {
        {TubeLayouts.EB_WB, new Uri("../Resources/TubeEBWB.png", UriKind.Relative)},
        {TubeLayouts.NB_SB, new Uri("../Resources/TubeNBSB.png", UriKind.Relative)},
        {TubeLayouts.No_EB, new Uri("../Resources/TubeNoEB.png", UriKind.Relative)},
        {TubeLayouts.No_NB, new Uri("../Resources/TubeNoNB.png", UriKind.Relative)},
        {TubeLayouts.No_SB, new Uri("../Resources/TubeNoSB.png", UriKind.Relative)},
        {TubeLayouts.No_WB, new Uri("../Resources/TubeNoWB.png", UriKind.Relative)}
      };

      CurrentLayout = TubeLayouts.EB_WB;
    }

    #region Properties

    private TubeLayouts _CurrentLayout;
    public TubeLayouts CurrentLayout
    {
      get
      {
        return _CurrentLayout;
      }
      set
      {
        _CurrentLayout = value;
        ImageBackground.Source = new BitmapImage(layoutImages[_CurrentLayout]);
      }
    }

    #endregion

    private void ApproachRectangle_MouseDown(object sender, MouseButtonEventArgs e)
    {
      Rectangle clickedRect = sender as Rectangle;
      if (clickedRect.Name == "ebApproachRect" || clickedRect.Name == "wbApproachRect")
      {
        switch (CurrentLayout)
        {
          case TubeLayouts.EB_WB:
            CurrentLayout = TubeLayouts.No_WB;
            break;
          case TubeLayouts.No_WB:
            CurrentLayout = TubeLayouts.No_EB;
            break;
          case TubeLayouts.No_EB:
            CurrentLayout = TubeLayouts.NB_SB;
            break;
          default:
            CurrentLayout = TubeLayouts.EB_WB;
            break;
        }
      }
      else if (clickedRect.Name == "nbApproachRect" || clickedRect.Name == "sbApproachRect")
      {
        switch (CurrentLayout)
        {
          case TubeLayouts.NB_SB:
            CurrentLayout = TubeLayouts.No_SB;
            break;
          case TubeLayouts.No_SB:
            CurrentLayout = TubeLayouts.No_NB;
            break;
          case TubeLayouts.No_NB:
            CurrentLayout = TubeLayouts.EB_WB;
            break;
          default:
            CurrentLayout = TubeLayouts.NB_SB;
            break;
        }
      }
    }

    #region public methods

    #endregion

  }
}
