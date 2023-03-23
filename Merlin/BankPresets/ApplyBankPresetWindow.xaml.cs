using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using AppMerlin;
using System.Windows.Controls;

namespace Merlin.BankPresets
{
  /// <summary>
  /// Interaction logic for ApplyBankPresetWindow.xaml
  /// </summary>
  public partial class ApplyBankPresetWindow : Window
  {
    public BankPreset GUIclickedPreset;
    public Dictionary<int, KeyValuePair<string, PedColumnDataType>> clickedPresetDic;
    public bool colSwap;
    public bool tccRules;
        
    public ApplyBankPresetWindow()
    {
      InitializeComponent();
      colSwap = false;
      tccRules = false;
      foreach (Dictionary<int, KeyValuePair<string, PedColumnDataType>> presetDic in GlobalBankPresets.presetList)
      {
        BankPreset preset = new BankPreset(presetDic);
        preset.Cursor = Cursors.Hand;
        preset.PreviewMouseDown += bankPresetListItem_PreviewMouseDown;

        preset.SetTitle(GlobalBankPresets.presetNames[GlobalBankPresets.presetList.IndexOf(presetDic)]);

        bankPresetList.Items.Add(preset);
      }
    }

    private void bankPresetListItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if(e.Source.GetType() == typeof(BankPreset))
      {
        GUIclickedPreset = (BankPreset)e.Source;
        clickedPresetDic = ((BankPreset)e.Source).preset;
        colSwap = GlobalBankPresets.presetNamesWithColumnSwapping.Contains(GUIclickedPreset.presetName.Text);
        tccRules = GlobalBankPresets.presetNamesWithTccRules.Contains(GUIclickedPreset.presetName.Text);

      }
      this.Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      MaxHeight = SystemParameters.PrimaryScreenHeight * 0.9;
      //Height = SystemParameters.PrimaryScreenHeight * 0.8;
    }

    //Allows scroll wheel to work 
    private void bankPresetList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (!e.Handled)
      {
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
        eventArg.RoutedEvent = UIElement.MouseWheelEvent;
        eventArg.Source = sender;
        var parent = ((Control)sender).Parent as UIElement;
        parent.RaiseEvent(eventArg);
      }
    }
  }
}
