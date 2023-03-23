using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Merlin.SpecialWindows
{
  /// <summary>
  /// Interaction logic for AprilFoolsWindow.xaml
  /// </summary>
  public partial class AprilFoolsWindow : Window
  {
    public AprilFoolsWindow()
    {
      InitializeComponent();
    }

    

    private void RunWait()
    {
      Thread.Sleep(10000);
      MessageBox.Show("April Fools!  :)", "Merlin Gotcha?", MessageBoxButton.OK);
      this.Close();
    }

    private void OnContent_Rendered(object sender, EventArgs e)
    {
      RunWait();
    }
  }
}
