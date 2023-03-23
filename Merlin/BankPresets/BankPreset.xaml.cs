using AppMerlin;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;

namespace Merlin.BankPresets
{
  /// <summary>
  /// Interaction logic for BankPreset.xaml
  /// </summary>
  public partial class BankPreset : UserControl
  {
    public Dictionary<int, KeyValuePair<string, PedColumnDataType>> preset;
    public BankPreset(Dictionary<int, KeyValuePair<string, PedColumnDataType>> preset)
    {
      InitializeComponent();
      SetBanks(preset);
      this.preset = preset;
    }

    public void SetTitle(string title)
    {
      presetName.Text = title;
    }

    private void SetBanks(Dictionary <int, KeyValuePair<string, PedColumnDataType>> banks)
    {
      TextBlock[] GUIvehBanks = GetGuiDataBanks();
      TextBlock[] GUIpedBanks = GetGuiPedBanks();
      TextBlock[] GUIbankHeaders = GetGuiBankHeaders();

      for(int i = 0; i < Constants.MAX_BANKS_ALLOWED; i++)
      {
        if(banks[i].Key == "NOT USED" && banks[i].Value == PedColumnDataType.NA)
        {
          GUIvehBanks[i].Text = "--";
          GUIpedBanks[i].Text = "--";
          GUIbankHeaders[i].FontWeight = FontWeights.Normal;

        }
        else
        {
          GUIvehBanks[i].Text = banks[i].Key;
          GUIpedBanks[i].Text = banks[i].Value.ToString();
          GUIpedBanks[i].Tag = banks[i].Value;
        }
      }
    }

    //We really should use data binding and not have to create arrays of all the controls so they can be easily iterated over

    private TextBlock[] GetGuiDataBanks()
    {
      TextBlock[] banks = new TextBlock[Constants.MAX_BANKS_ALLOWED];

      banks[0] = bank0VehType;
      banks[1] = bank1VehType;
      banks[2] = bank2VehType;
      banks[3] = bank3VehType;
      banks[4] = bank4VehType;
      banks[5] = bank5VehType;
      banks[6] = bank6VehType;
      banks[7] = bank7VehType;
      banks[8] = bank8VehType;
      banks[9] = bank9VehType;
      banks[10] = bank10VehType;
      banks[11] = bank11VehType;
      banks[12] = bank12VehType;
      banks[13] = bank13VehType;

      return banks;
    }

    private TextBlock[] GetGuiPedBanks()
    {
      TextBlock[] banks = new TextBlock[Constants.MAX_BANKS_ALLOWED];

      banks[0] = bank0PedData;
      banks[1] = bank1PedData;
      banks[2] = bank2PedData;
      banks[3] = bank3PedData;
      banks[4] = bank4PedData;
      banks[5] = bank5PedData;
      banks[6] = bank6PedData;
      banks[7] = bank7PedData;
      banks[8] = bank8PedData;
      banks[9] = bank9PedData;
      banks[10] = bank10PedData;
      banks[11] = bank11PedData;
      banks[12] = bank12PedData;
      banks[13] = bank13PedData;

      return banks;
    }

    private TextBlock[] GetGuiBankHeaders()
    {
      TextBlock[] banks = new TextBlock[Constants.MAX_BANKS_ALLOWED];

      banks[0] = Bank0Header;
      banks[1] = Bank1Header;
      banks[2] = Bank2Header;
      banks[3] = Bank3Header;
      banks[4] = Bank4Header;
      banks[5] = Bank5Header;
      banks[6] = Bank6Header;
      banks[7] = Bank7Header;
      banks[8] = Bank8Header;
      banks[9] = Bank9Header;
      banks[10] = Bank10Header;
      banks[11] = Bank11Header;
      banks[12] = Bank12Header;
      banks[13] = Bank13Header;

      return banks;
    }

  }
}
