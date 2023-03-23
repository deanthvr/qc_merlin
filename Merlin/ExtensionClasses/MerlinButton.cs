
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Merlin.ExtensionClasses
{
  public class MerlinButton : Button
  {
    static MerlinButton()
    {
      //DefaultStyleKeyProperty.OverrideMetadata(typeof(MerlinButton), new FrameworkPropertyMetadata(typeof(MerlinButton)));
    }

    public ImageSource ImageSource
    {
      get { return (ImageSource)GetValue(ImageSourceProperty); }
      set { SetValue(ImageSourceProperty, value); }
    }

    public int ButtonTextFontSize
    {
      get { return (int)GetValue(ButtonTextFontSizeProperty); }
      set { SetValue(ButtonTextFontSizeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MerlinButton), new UIPropertyMetadata(null));

    public static readonly DependencyProperty ButtonTextFontSizeProperty =
        DependencyProperty.Register("ButtonTextFontSize", typeof(int), typeof(MerlinButton), new UIPropertyMetadata(null));
  }
}
