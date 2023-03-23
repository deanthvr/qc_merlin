using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Linq;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for StartingScreen.xaml
  /// </summary>
  public partial class StartingScreen : UserControl
  {
    public StartingScreen()
    {
      InitializeComponent();
      try
      {
        ApplySeasonalEffects();
      }
      catch
      {
        //How should we ensure any error with the seasonal effects doesn't interrupt Merlin?
      }
    }

    private void ApplySeasonalEffects()
    {
      if (IsHolidaySeason(DateTime.Now))
      {
        ShowSantaHat();
      }
      if(IsThanksgivingWeek(DateTime.Now))
      {
        ShowTurkey();
      }
    }

    private void ShowSantaHat()
    {
      Image santaHat = new Image()
      {
        Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/cap-147417_640.png")),
        Width = 350,
        Stretch = Stretch.Uniform,
        Margin = new Thickness(175, -156, -13, 156),
        RenderTransform = new RotateTransform(-11.056)
      };
      //ensures smooth rendering when image is displayed smaller than native resolution
      RenderOptions.SetBitmapScalingMode(santaHat, BitmapScalingMode.Fant);

      mainGrid.Children.Add(santaHat);
    }

    private void ShowTurkey()
    {
      Image turkey = new Image()
      {
        Source = new BitmapImage(new Uri("pack://application:,,,/Merlin;component/Resources/turkey.png")),
        Width = 510,
        Stretch = Stretch.Uniform,
        Margin = new Thickness(1, -27, 1, 27),
      };
      //ensures smooth rendering when image is displayed smaller than native resolution
      RenderOptions.SetBitmapScalingMode(turkey, BitmapScalingMode.Fant);

      mainGrid.Children.Add(turkey);
    }

    private bool IsThanksgivingWeek(DateTime currentTime)
    {
      //finds day of month that Thanksgiving falls on for the current year
      var thanksgiving = (from day in Enumerable.Range(1, 30)
                          where new DateTime(currentTime.Year, 11, day).DayOfWeek == DayOfWeek.Thursday
                          select day).ElementAt(3);
      //Thanksgiving day for the current year
      DateTime thanksgivingDay = new DateTime(currentTime.Year, 11, thanksgiving);
      int daysFromThanksgiving = (thanksgivingDay - currentTime).Days;
      if (daysFromThanksgiving >= 0 && daysFromThanksgiving < 6)
      {
        return true;
      }
      return false;
    }

    private bool IsHolidaySeason(DateTime currentTime)
    {
      return currentTime.Month == 12;
    }
  }
}
