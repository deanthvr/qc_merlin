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

namespace Merlin
{
  /// <summary>
  /// Interaction logic for MerlinProgressWindow.xaml
  /// </summary>
  public partial class MerlinProgressWindow : Window
  {
    private delegate void UpdateProgressBarValueDelegate(DependencyProperty dp, Object value);
    public MerlinProgressWindow(string title)
    {
      InitializeComponent();

      Title = title;
      progressBar.Maximum = 100;
      progressBar.Value = 0;
    }

    /// <summary>
    /// Set value of progress bar with values on the interval [0, 100].
    /// </summary>
    /// <param name="pct"></param>
    public void SetPct(int pct)
    {
      UpdateProgressBarValueDelegate updatePBValueDelegate = (progressBar.SetValue);
      int value;
      
      if(pct < 0)
      {
        value = 0;
      }
      else if(pct > 100)
      {
        value = 100;
      }
      else
      {
        value = pct;
      }

      Dispatcher.Invoke(updatePBValueDelegate,
                System.Windows.Threading.DispatcherPriority.Background, 
                new object[] { ProgressBar.ValueProperty, (double)value });
    }
  }
}
