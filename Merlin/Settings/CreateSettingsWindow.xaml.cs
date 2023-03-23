using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppMerlin;
using QCCommon.QCData;
using System.Linq;
using System.Collections.Generic;
using QCCommon.Extension;

namespace Merlin.Settings
{
  /// <summary>
  /// Interaction logic for CreateSettingsWindow.xaml
  /// </summary>
  public partial class CreateSettingsWindow : Window
  {
    public Preferences savedPreferences;
    public XmlFile serializedPreferences;
    public bool allowClose = false;

    public CreateSettingsWindow()
    {
      InitializeComponent();
      savedPreferences = new Preferences();
      savedPreferences.LoadDefaultPreferences();
      LoadDefaults();

      directoryProjectButton.Tag = directoryProject;
      directoryDataFilesButton.Tag = directoryDataFiles;
      directoryASCIIButton.Tag = directoryASCII;
      directoryQCConversionButton.Tag = directoryQCConversion;
      ndirectoryProjectButton.Tag = ndirectoryProject;
      ndirectoryDataFilesButton.Tag = ndirectoryDataFiles;
      ndirectoryASCIIButton.Tag = ndirectoryASCII;
      ndirectoryQCConversionButton.Tag = ndirectoryQCConversion;
    }

    private void LoadDefaults()
    {
      directoryProject.Text = @"C:\QCProjects\";
      directoryDataFiles.Text = @"C:\QCProjects\";
      directoryASCII.Text = @"C:\QCProjects\";
      directoryQCConversion.Text = @"C:\QCProjects\";
      ndirectoryProject.Text = @"\\qcfs1\Office docs\(17) TPA_VRC\Projects";
      ndirectoryDataFiles.Text = @"\\tpanetwork\NFL";
      ndirectoryASCII.Text = @"\\qcfs1\Office docs\(17) TPA_VRC\Jamar\";
      ndirectoryQCConversion.Text = @"\\qcfs1\Office docs\(17) TPA_VRC\QC Conversion Tool For New Website\QC Conversion Tool v5.1";

      daysToSearch.Text = "14";
      lowIntervalPercent.Text = "0.25";
      highIntervalPercent.Text = "2.0";
      intervalMin.Text = "200";
      lowHeaviesPercent.Text = "0.05";
      highHeaviesPercent.Text = "0.50";
      heaviesMin.Text = "100";
      lowMoveVolPercent.Text = "0.80";
      highMoveVolPercent.Text = "1.25";
      timePeriodPercent.Text = "0.25";
      moveVolMin.Text = "200";
      balancingDiff.Text = ".02";

      fhwa1Threshold.Text = "5.0";
      fhwa2Threshold.Text = "5.0";
      fhwa3Threshold.Text = "5.0";
      fhwa4Threshold.Text = "5.0";
      fhwa5Threshold.Text = "5.0";
      fhwa6Threshold.Text = "5.0";
      fhwa7Threshold.Text = "5.0";
      fhwa8Threshold.Text = "5.0";
      fhwa9Threshold.Text = "5.0";
      fhwa10Threshold.Text = "5.0";
      fhwa11Threshold.Text = "5.0";
      fhwa12Threshold.Text = "5.0";
      fhwa13Threshold.Text = "5.0";
      fhwa6_13Threshold.Text = "5.0";

      Dictionary<QCDataSource, string> dataSources = new Dictionary<QCDataSource, string>();
      foreach (var source in Enum.GetValues(typeof(QCDataSource)).Cast<QCDataSource>())
      {
        if(source != QCDataSource.MySql)
        {
          dataSources.Add(source, source.GetDescription());
        }
      }
      selectedDataSourceComboBox.ItemsSource = dataSources;
      selectedDataSourceComboBox.SelectedItem = ((Dictionary<QCDataSource, string>)selectedDataSourceComboBox.ItemsSource).FirstOrDefault(x => x.Key == QCDataSource.SqlServer);
      userNameTextBox.Text = "";
      pwdPasswordBox.Password = "";
    }

    private bool SaveChoicesToPreferences()
    {
      try
      {
        savedPreferences.m_DaysToSearch = int.Parse(daysToSearch.Text);

        savedPreferences.m_LocalProjectDirectory = directoryProject.Text;
        savedPreferences.m_LocalDataDirectory = directoryDataFiles.Text;
        savedPreferences.m_LocalAsciiDirectory = directoryASCII.Text;
        savedPreferences.m_LocalQCConversionDirectory = directoryQCConversion.Text;
        savedPreferences.m_NetworkProjectDirectory = ndirectoryProject.Text;
        savedPreferences.m_NetworkDataDirectory = ndirectoryDataFiles.Text;
        savedPreferences.m_NetworkAsciiDirectory = ndirectoryASCII.Text;
        savedPreferences.m_NetworkQCConversionDirectory = ndirectoryQCConversion.Text;

        savedPreferences.m_LowInterval = double.Parse(lowIntervalPercent.Text);
        savedPreferences.m_HighInterval = double.Parse(highIntervalPercent.Text);
        savedPreferences.m_IntervalMin = int.Parse(intervalMin.Text);
        savedPreferences.m_LowHeavies = double.Parse(lowHeaviesPercent.Text);
        savedPreferences.m_HighHeavies = double.Parse(highHeaviesPercent.Text);
        savedPreferences.m_HeaviesMin = int.Parse(heaviesMin.Text);
        savedPreferences.m_TimePeriodLow = double.Parse(lowMoveVolPercent.Text);
        savedPreferences.m_TimePeriodHigh = double.Parse(highMoveVolPercent.Text);
        savedPreferences.m_TimePeriodMin = int.Parse(moveVolMin.Text);
        savedPreferences.m_CrossPeakDiff = double.Parse(timePeriodPercent.Text);

        savedPreferences.m_BalancingDiff = double.Parse(timePeriodPercent.Text);

        savedPreferences.m_fhwa1 = float.Parse(fhwa1Threshold.Text);
        savedPreferences.m_fhwa2 = float.Parse(fhwa2Threshold.Text);
        savedPreferences.m_fhwa3 = float.Parse(fhwa3Threshold.Text);
        savedPreferences.m_fhwa4 = float.Parse(fhwa4Threshold.Text);
        savedPreferences.m_fhwa5 = float.Parse(fhwa5Threshold.Text);
        savedPreferences.m_fhwa6 = float.Parse(fhwa6Threshold.Text);
        savedPreferences.m_fhwa7 = float.Parse(fhwa7Threshold.Text);
        savedPreferences.m_fhwa8 = float.Parse(fhwa8Threshold.Text);
        savedPreferences.m_fhwa9 = float.Parse(fhwa9Threshold.Text);
        savedPreferences.m_fhwa10 = float.Parse(fhwa10Threshold.Text);
        savedPreferences.m_fhwa11 = float.Parse(fhwa11Threshold.Text);
        savedPreferences.m_fhwa12 = float.Parse(fhwa12Threshold.Text);
        savedPreferences.m_fhwa13 = float.Parse(fhwa13Threshold.Text);
        savedPreferences.m_fhwa6_13 = float.Parse(fhwa6_13Threshold.Text);

        savedPreferences.m_dataSource = selectedDataSourceComboBox.SelectedItem == null ? QCDataSource.SqlServer : ((KeyValuePair<QCDataSource, string>)selectedDataSourceComboBox.SelectedItem).Key;
        savedPreferences.m_userName = userNameTextBox.Text;
        savedPreferences.m_password = pwdPasswordBox.SecurePassword;
      }
      catch(Exception ex)
      {
        if (ex is FormatException)
        {
          windowErrorMessage.Text = "One or more threshold fields has an invalid character or is empty.";
        } 
        else if( ex is ArgumentNullException)
        {
          windowErrorMessage.Text = "One or more threshold fields is null.";
        } 
        else if (ex is OverflowException)
        {
          windowErrorMessage.Text = "One or more threshold fields has a value too large for the type.";
        }
        return false;
      }
      return true;

    }
    
    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
      if (SaveChoicesToPreferences())
      {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Directory.CreateDirectory(appData + @"\Merlin");

        serializedPreferences = new XmlFile(appData + @"\Merlin\", "Settings.xml");
        serializedPreferences.SerializeToFile<Preferences>(savedPreferences);
        allowClose = true;
        DialogResult = true;
      }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!allowClose)
      {
        e.Cancel = true;
      }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
      ExtensionClasses.MerlinButton btn = (ExtensionClasses.MerlinButton)sender;
      TextBox pathText = (TextBox)btn.Tag;

      System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
      System.Windows.Forms.DialogResult result = browser.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        pathText.Text = browser.SelectedPath.ToString();
      }
    }

    private void Directory_Changed(object sender, TextChangedEventArgs e)
    {
      TextBox thisTextBox = (TextBox)sender;
      string path = thisTextBox.Text;
      bool isLocal = thisTextBox.Tag.ToString() == "local";
      Regex startsWithLetter = new Regex(@"^[A-Za-z]");
      Regex startsWithSlashes = new Regex(@"^[\\][\\]");
      Regex networkPath = new Regex(@"^\\{2}[\w-]+(\\{1}(([\w-][\w-\s]*[\w-]+[$$]?)|([\w-][$$]?$)))+");
      Regex localAbsolutePath = new Regex(@"^[a-zA-Z][:][\\].*");

      if (String.IsNullOrEmpty(path))
      {
        if (isLocal)
        {
          localError.Text = "Directories cannot be blank.";
        }
        else
        {
          networkError.Text = "Directories cannot be blank.";
        }
        return;
      }
      if (isLocal)
      {
        if (!startsWithLetter.IsMatch(path))
        {
          thisTextBox.Foreground = new SolidColorBrush(Colors.Red);
          localError.Text = "Local directory must start with a drive letter.";
          return;
        }
        else if (!localAbsolutePath.IsMatch(path))
        {
          thisTextBox.Foreground = new SolidColorBrush(Colors.Red);
          localError.Text = "Local directory is invalid or contains bad characters.";
          return;
        }
      }
      else
      {
        if (startsWithSlashes.IsMatch(path))
        {
          if (!networkPath.IsMatch(path))
          {
            thisTextBox.Foreground = new SolidColorBrush(Colors.Red);
            networkError.Text = "Network path is invalid";
            return;
          }
        }
        else if (startsWithLetter.IsMatch(path))
        {
          if (!localAbsolutePath.IsMatch(path))
          {
            thisTextBox.Foreground = new SolidColorBrush(Colors.Red);
            networkError.Text = "Directory is local path, but is invalid or contains bad characters.";
            return;
          }
        }
        else
        {
          thisTextBox.Foreground = new SolidColorBrush(Colors.Red);
          networkError.Text = "Directory must begin either with drive letter or \\\\";
          return;
        }
      }

      thisTextBox.Foreground = new SolidColorBrush(Colors.Black);
      if (isLocal)
      {
        localError.Text = "";
      }
      else
      {
        networkError.Text = "";
      }


    }

  }
}
