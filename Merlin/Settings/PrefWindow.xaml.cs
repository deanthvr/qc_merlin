using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppMerlin;
using System.Collections.Generic;
using QCCommon.QCData;
using System.Linq;
using QCCommon.Extension;

namespace Merlin.Settings
{
  /// <summary>
  /// Interaction logic for PrefWindow.xaml
  /// </summary>
  public partial class PrefWindow : Window
  {
    private TMCProject m_Project;
    private Preferences m_DefaultPreferences;

    public PrefWindow(TMCProject project, Preferences defaults)
    {
      InitializeComponent();
      m_Project = project;
      m_DefaultPreferences = defaults;
      LoadPreferences();

      directoryProjectButton.Tag = directoryProject;
      directoryDataFilesButton.Tag = directoryDataFiles;
      directoryASCIIButton.Tag = directoryASCII;
      directoryQCConversionButton.Tag = directoryQCConversion;
      ndirectoryProjectButton.Tag = ndirectoryProject;
      ndirectoryDataFilesButton.Tag = ndirectoryDataFiles;
      ndirectoryASCIIButton.Tag = ndirectoryASCII;
      ndirectoryQCConversionButton.Tag = ndirectoryQCConversion;

    }

    private void LoadPreferences()
    {
      //First populate the data source combobox with the possible options
      Dictionary<QCDataSource, string> dataSources = new Dictionary<QCDataSource, string>();
      foreach (var source in Enum.GetValues(typeof(QCDataSource)).Cast<QCDataSource>())
      {
        if (source != QCDataSource.MySql)
        {
          dataSources.Add(source, source.GetDescription());
        }
      }
      selectedDataSourceComboBox.ItemsSource = dataSources;

      if (m_Project == null)
      {
        LoadPreferencesNoProject();
      }
      else
      {
        LoadPreferencesProjectPresent();
      }
    }

    private void LoadPreferencesNoProject()
    {
      saveForProjectButton.IsEnabled = false;

      PreferencesMessage.Text = "No Project Loaded.  Viewing Default Preferences.";
      PreferencesMessage.FontSize = 16;
      PreferencesMessage.Foreground = new SolidColorBrush(Colors.Red);

      daysToSearch.Text = m_DefaultPreferences.m_DaysToSearch.ToString();

      directoryProject.Text = m_DefaultPreferences.m_LocalProjectDirectory;
      directoryDataFiles.Text = m_DefaultPreferences.m_LocalDataDirectory;
      directoryASCII.Text = m_DefaultPreferences.m_LocalAsciiDirectory;
      directoryQCConversion.Text = m_DefaultPreferences.m_LocalQCConversionDirectory;
      ndirectoryProject.Text = m_DefaultPreferences.m_NetworkProjectDirectory;
      ndirectoryDataFiles.Text = m_DefaultPreferences.m_NetworkDataDirectory;
      ndirectoryASCII.Text = m_DefaultPreferences.m_NetworkAsciiDirectory;
      ndirectoryQCConversion.Text = m_DefaultPreferences.m_NetworkQCConversionDirectory;

      lowIntervalPercent.Text = m_DefaultPreferences.m_LowInterval.ToString();
      highIntervalPercent.Text = m_DefaultPreferences.m_HighInterval.ToString();
      intervalMin.Text = m_DefaultPreferences.m_IntervalMin.ToString();
      lowHeaviesPercent.Text = m_DefaultPreferences.m_LowHeavies.ToString();
      highHeaviesPercent.Text = m_DefaultPreferences.m_HighHeavies.ToString();
      heaviesMin.Text = m_DefaultPreferences.m_HeaviesMin.ToString();
      lowMoveVolPercent.Text = m_DefaultPreferences.m_TimePeriodLow.ToString();
      highMoveVolPercent.Text = m_DefaultPreferences.m_TimePeriodHigh.ToString();
      moveVolMin.Text = m_DefaultPreferences.m_TimePeriodMin.ToString();
      timePeriodPercent.Text = m_DefaultPreferences.m_CrossPeakDiff.ToString();
      balancingDiff.Text = m_DefaultPreferences.m_BalancingDiff.ToString();

      fhwa1Threshold.Text = m_DefaultPreferences.m_fhwa1.ToString();
      fhwa2Threshold.Text = m_DefaultPreferences.m_fhwa2.ToString();
      fhwa3Threshold.Text = m_DefaultPreferences.m_fhwa3.ToString();
      fhwa4Threshold.Text = m_DefaultPreferences.m_fhwa4.ToString();
      fhwa5Threshold.Text = m_DefaultPreferences.m_fhwa5.ToString();
      fhwa6Threshold.Text = m_DefaultPreferences.m_fhwa6.ToString();
      fhwa7Threshold.Text = m_DefaultPreferences.m_fhwa7.ToString();
      fhwa8Threshold.Text = m_DefaultPreferences.m_fhwa8.ToString();
      fhwa9Threshold.Text = m_DefaultPreferences.m_fhwa9.ToString();
      fhwa10Threshold.Text = m_DefaultPreferences.m_fhwa10.ToString();
      fhwa11Threshold.Text = m_DefaultPreferences.m_fhwa11.ToString();
      fhwa12Threshold.Text = m_DefaultPreferences.m_fhwa12.ToString();
      fhwa13Threshold.Text = m_DefaultPreferences.m_fhwa13.ToString();
      fhwa6_13Threshold.Text = m_DefaultPreferences.m_fhwa6_13.ToString();

      selectedDataSourceComboBox.SelectedItem = ((Dictionary<QCDataSource, string>)selectedDataSourceComboBox.ItemsSource).FirstOrDefault(x => x.Key == m_DefaultPreferences.m_dataSource);
      userNameTextBox.Text = m_DefaultPreferences.m_userName;
      pwdPasswordBox.Password = "";
      updatePwdCheckBox.IsChecked = false;
      pwdTextBlock.Text = new string('•', m_DefaultPreferences.m_password.Length);
    }

    private void LoadPreferencesProjectPresent()
    {
      PreferencesMessage.Text = m_Project.m_OrderNumber + " - " + m_Project.m_ProjectName + " Preferences";
      PreferencesMessage.FontSize = 16;
      PreferencesMessage.Foreground = new SolidColorBrush(Colors.Green);

      daysToSearch.Text = m_Project.m_Prefs.m_DaysToSearch.ToString();

      directoryProject.Text = m_Project.m_Prefs.m_LocalProjectDirectory;
      directoryDataFiles.Text = m_Project.m_Prefs.m_LocalDataDirectory;
      directoryASCII.Text = m_Project.m_Prefs.m_LocalAsciiDirectory;
      directoryQCConversion.Text = m_Project.m_Prefs.m_LocalQCConversionDirectory;
      ndirectoryProject.Text = m_Project.m_Prefs.m_NetworkProjectDirectory;
      ndirectoryDataFiles.Text = m_Project.m_Prefs.m_NetworkDataDirectory;
      ndirectoryASCII.Text = m_Project.m_Prefs.m_NetworkAsciiDirectory;
      ndirectoryQCConversion.Text = m_Project.m_Prefs.m_NetworkQCConversionDirectory;

      lowIntervalPercent.Text = m_Project.m_Prefs.m_LowInterval.ToString();
      highIntervalPercent.Text = m_Project.m_Prefs.m_HighInterval.ToString();
      intervalMin.Text = m_Project.m_Prefs.m_IntervalMin.ToString();
      lowHeaviesPercent.Text = m_Project.m_Prefs.m_LowHeavies.ToString();
      highHeaviesPercent.Text = m_Project.m_Prefs.m_HighHeavies.ToString();
      heaviesMin.Text = m_Project.m_Prefs.m_HeaviesMin.ToString();
      lowMoveVolPercent.Text = m_Project.m_Prefs.m_TimePeriodLow.ToString();
      highMoveVolPercent.Text = m_Project.m_Prefs.m_TimePeriodHigh.ToString();
      moveVolMin.Text = m_Project.m_Prefs.m_TimePeriodMin.ToString();
      timePeriodPercent.Text = m_Project.m_Prefs.m_CrossPeakDiff.ToString();
      balancingDiff.Text = m_Project.m_Prefs.m_BalancingDiff.ToString();

      fhwa1Threshold.Text = m_Project.m_Prefs.m_fhwa1.ToString();
      fhwa2Threshold.Text = m_Project.m_Prefs.m_fhwa2.ToString();
      fhwa3Threshold.Text = m_Project.m_Prefs.m_fhwa3.ToString();
      fhwa4Threshold.Text = m_Project.m_Prefs.m_fhwa4.ToString();
      fhwa5Threshold.Text = m_Project.m_Prefs.m_fhwa5.ToString();
      fhwa6Threshold.Text = m_Project.m_Prefs.m_fhwa6.ToString();
      fhwa7Threshold.Text = m_Project.m_Prefs.m_fhwa7.ToString();
      fhwa8Threshold.Text = m_Project.m_Prefs.m_fhwa8.ToString();
      fhwa9Threshold.Text = m_Project.m_Prefs.m_fhwa9.ToString();
      fhwa10Threshold.Text = m_Project.m_Prefs.m_fhwa10.ToString();
      fhwa11Threshold.Text = m_Project.m_Prefs.m_fhwa11.ToString();
      fhwa12Threshold.Text = m_Project.m_Prefs.m_fhwa12.ToString();
      fhwa13Threshold.Text = m_Project.m_Prefs.m_fhwa13.ToString();
      fhwa6_13Threshold.Text = m_Project.m_Prefs.m_fhwa6_13.ToString();

      selectedDataSourceComboBox.SelectedItem = ((Dictionary<QCDataSource, string>)selectedDataSourceComboBox.ItemsSource).FirstOrDefault(x => x.Key == m_Project.m_Prefs.m_dataSource);
      userNameTextBox.Text = m_Project.m_Prefs.m_userName;
      pwdPasswordBox.Password = "";
      updatePwdCheckBox.IsChecked = false;
      pwdTextBlock.Text = new string('•', m_Project.m_Prefs.m_password.Length);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBox.Show("All changes will be lost", "Cancel",
        MessageBoxButton.OKCancel);
      switch (result)
      {
        case MessageBoxResult.OK:
          Close();
          break;
        case MessageBoxResult.Cancel:
          break;
      }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result =
        MessageBox.Show("After changing thresholds, user should re-process flags. \nContinue?", "Warning",
          MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.Cancel)
      {
        return;
      }
      try
      {
        m_Project.m_Prefs.m_LocalProjectDirectory = directoryProject.Text;
        m_Project.m_Prefs.m_LocalDataDirectory = directoryDataFiles.Text;
        m_Project.m_Prefs.m_LocalAsciiDirectory = directoryASCII.Text;
        m_Project.m_Prefs.m_LocalQCConversionDirectory = directoryQCConversion.Text;
        m_Project.m_Prefs.m_NetworkProjectDirectory = ndirectoryProject.Text;
        m_Project.m_Prefs.m_NetworkDataDirectory = ndirectoryDataFiles.Text;
        m_Project.m_Prefs.m_NetworkAsciiDirectory = ndirectoryASCII.Text;
        m_Project.m_Prefs.m_NetworkQCConversionDirectory = ndirectoryQCConversion.Text;

        m_Project.m_Prefs.m_DaysToSearch = int.Parse(daysToSearch.Text);

        m_Project.m_Prefs.m_LowInterval = double.Parse(lowIntervalPercent.Text);
        m_Project.m_Prefs.m_HighInterval = double.Parse(highIntervalPercent.Text);
        m_Project.m_Prefs.m_IntervalMin = int.Parse(intervalMin.Text);
        m_Project.m_Prefs.m_LowHeavies = double.Parse(lowHeaviesPercent.Text);
        m_Project.m_Prefs.m_HighHeavies = double.Parse(highHeaviesPercent.Text);
        m_Project.m_Prefs.m_HeaviesMin = int.Parse(heaviesMin.Text);
        m_Project.m_Prefs.m_TimePeriodLow = double.Parse(lowMoveVolPercent.Text);
        m_Project.m_Prefs.m_TimePeriodHigh = double.Parse(highMoveVolPercent.Text);
        m_Project.m_Prefs.m_TimePeriodMin = int.Parse(moveVolMin.Text);
        m_Project.m_Prefs.m_CrossPeakDiff = double.Parse(timePeriodPercent.Text);
        m_Project.m_Prefs.m_BalancingDiff = double.Parse(balancingDiff.Text);

        m_Project.m_Prefs.m_fhwa1 = float.Parse(fhwa1Threshold.Text);
        m_Project.m_Prefs.m_fhwa2 = float.Parse(fhwa2Threshold.Text);
        m_Project.m_Prefs.m_fhwa3 = float.Parse(fhwa3Threshold.Text);
        m_Project.m_Prefs.m_fhwa4 = float.Parse(fhwa4Threshold.Text);
        m_Project.m_Prefs.m_fhwa5 = float.Parse(fhwa5Threshold.Text);
        m_Project.m_Prefs.m_fhwa6 = float.Parse(fhwa6Threshold.Text);
        m_Project.m_Prefs.m_fhwa7 = float.Parse(fhwa7Threshold.Text);
        m_Project.m_Prefs.m_fhwa8 = float.Parse(fhwa8Threshold.Text);
        m_Project.m_Prefs.m_fhwa9 = float.Parse(fhwa9Threshold.Text);
        m_Project.m_Prefs.m_fhwa10 = float.Parse(fhwa10Threshold.Text);
        m_Project.m_Prefs.m_fhwa11 = float.Parse(fhwa11Threshold.Text);
        m_Project.m_Prefs.m_fhwa12 = float.Parse(fhwa12Threshold.Text);
        m_Project.m_Prefs.m_fhwa13 = float.Parse(fhwa13Threshold.Text);
        m_Project.m_Prefs.m_fhwa6_13 = float.Parse(fhwa6_13Threshold.Text);

        m_Project.m_Prefs.m_dataSource = selectedDataSourceComboBox.SelectedItem == null ? QCDataSource.SqlServer : ((KeyValuePair<QCDataSource, string>)selectedDataSourceComboBox.SelectedItem).Key;
        m_Project.m_Prefs.m_userName = userNameTextBox.Text;
        m_Project.m_Prefs.m_password = pwdPasswordBox.SecurePassword;
      }
      catch (Exception ex)
      {
        if (ex is FormatException)
        {
          windowErrorMessage.Text = "One or more threshold fields has an invalid character or is empty.";
        }
        else if (ex is ArgumentNullException)
        {
          windowErrorMessage.Text = "One or more threshold fields is null.";
        }
        else if (ex is OverflowException)
        {
          windowErrorMessage.Text = "One or more threshold fields has a value too large for the type.";
        }
        return;
      }
      Close();
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result =
        MessageBox.Show(
          "Make current settings permanent default. \nThis is permanent and cannot be undone.",
          "Set New Default", MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        try
        {
          m_DefaultPreferences.m_DaysToSearch = int.Parse(daysToSearch.Text);

          m_DefaultPreferences.m_LocalProjectDirectory = directoryProject.Text;
          m_DefaultPreferences.m_LocalDataDirectory = directoryDataFiles.Text;
          m_DefaultPreferences.m_LocalAsciiDirectory = directoryASCII.Text;
          m_DefaultPreferences.m_LocalQCConversionDirectory = directoryQCConversion.Text;
          m_DefaultPreferences.m_NetworkProjectDirectory = ndirectoryProject.Text;
          m_DefaultPreferences.m_NetworkDataDirectory = ndirectoryDataFiles.Text;
          m_DefaultPreferences.m_NetworkAsciiDirectory = ndirectoryASCII.Text;
          m_DefaultPreferences.m_NetworkQCConversionDirectory = ndirectoryQCConversion.Text;

          m_DefaultPreferences.m_LowInterval = double.Parse(lowIntervalPercent.Text);
          m_DefaultPreferences.m_HighInterval = double.Parse(highIntervalPercent.Text);
          m_DefaultPreferences.m_IntervalMin = int.Parse(intervalMin.Text);
          m_DefaultPreferences.m_LowHeavies = double.Parse(lowHeaviesPercent.Text);
          m_DefaultPreferences.m_HighHeavies = double.Parse(highHeaviesPercent.Text);
          m_DefaultPreferences.m_HeaviesMin = int.Parse(heaviesMin.Text);
          m_DefaultPreferences.m_TimePeriodLow = double.Parse(lowMoveVolPercent.Text);
          m_DefaultPreferences.m_TimePeriodHigh = double.Parse(highMoveVolPercent.Text);
          m_DefaultPreferences.m_TimePeriodMin = int.Parse(moveVolMin.Text);
          m_DefaultPreferences.m_CrossPeakDiff = double.Parse(timePeriodPercent.Text);
          m_DefaultPreferences.m_BalancingDiff = double.Parse(balancingDiff.Text);

          m_DefaultPreferences.m_fhwa1 = float.Parse(fhwa1Threshold.Text);
          m_DefaultPreferences.m_fhwa2 = float.Parse(fhwa2Threshold.Text);
          m_DefaultPreferences.m_fhwa3 = float.Parse(fhwa3Threshold.Text);
          m_DefaultPreferences.m_fhwa4 = float.Parse(fhwa4Threshold.Text);
          m_DefaultPreferences.m_fhwa5 = float.Parse(fhwa5Threshold.Text);
          m_DefaultPreferences.m_fhwa6 = float.Parse(fhwa6Threshold.Text);
          m_DefaultPreferences.m_fhwa7 = float.Parse(fhwa7Threshold.Text);
          m_DefaultPreferences.m_fhwa8 = float.Parse(fhwa8Threshold.Text);
          m_DefaultPreferences.m_fhwa9 = float.Parse(fhwa9Threshold.Text);
          m_DefaultPreferences.m_fhwa10 = float.Parse(fhwa10Threshold.Text);
          m_DefaultPreferences.m_fhwa11 = float.Parse(fhwa11Threshold.Text);
          m_DefaultPreferences.m_fhwa12 = float.Parse(fhwa12Threshold.Text);
          m_DefaultPreferences.m_fhwa13 = float.Parse(fhwa13Threshold.Text);
          m_DefaultPreferences.m_fhwa6_13 = float.Parse(fhwa6_13Threshold.Text);

          m_DefaultPreferences.m_dataSource = selectedDataSourceComboBox.SelectedItem == null ? QCDataSource.SqlServer : ((KeyValuePair<QCDataSource, string>)selectedDataSourceComboBox.SelectedItem).Key;
          m_DefaultPreferences.m_userName = userNameTextBox.Text;
          m_DefaultPreferences.m_password = pwdPasswordBox.SecurePassword;
        }
        catch (Exception)
        {
          MessageBoxResult exceptionResult = MessageBox.Show(
              "One or more of the values in the threshold fields has an issue. \nCheck for improper characters, empty fields, or values too large for their type and try again.",
              "Parsing Error", MessageBoxButton.OK);
          return;
        }

        XmlFile file = new XmlFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Merlin\", "Settings.xml");
        file.SerializeToFile(m_DefaultPreferences);
      }
      
    }

    private void RestoreDefault_Click(object sender, RoutedEventArgs e)
    {
      daysToSearch.Text = m_DefaultPreferences.m_DaysToSearch.ToString();

      directoryProject.Text = m_DefaultPreferences.m_LocalProjectDirectory;
      directoryDataFiles.Text = m_DefaultPreferences.m_LocalDataDirectory;
      directoryASCII.Text = m_DefaultPreferences.m_LocalAsciiDirectory;
      directoryQCConversion.Text = m_DefaultPreferences.m_LocalQCConversionDirectory;
      ndirectoryProject.Text = m_DefaultPreferences.m_NetworkProjectDirectory;
      ndirectoryDataFiles.Text = m_DefaultPreferences.m_NetworkDataDirectory;
      ndirectoryASCII.Text = m_DefaultPreferences.m_NetworkAsciiDirectory;
      ndirectoryQCConversion.Text = m_DefaultPreferences.m_NetworkQCConversionDirectory;

      lowIntervalPercent.Text = m_DefaultPreferences.m_LowInterval.ToString();
      highIntervalPercent.Text = m_DefaultPreferences.m_HighInterval.ToString();
      intervalMin.Text = m_DefaultPreferences.m_IntervalMin.ToString();
      lowHeaviesPercent.Text = m_DefaultPreferences.m_LowHeavies.ToString();
      highHeaviesPercent.Text = m_DefaultPreferences.m_HighHeavies.ToString();
      heaviesMin.Text = m_DefaultPreferences.m_HeaviesMin.ToString();
      lowMoveVolPercent.Text = m_DefaultPreferences.m_TimePeriodLow.ToString();
      highMoveVolPercent.Text = m_DefaultPreferences.m_TimePeriodHigh.ToString();
      moveVolMin.Text = m_DefaultPreferences.m_TimePeriodMin.ToString();
      timePeriodPercent.Text = m_DefaultPreferences.m_CrossPeakDiff.ToString();
      balancingDiff.Text = m_DefaultPreferences.m_BalancingDiff.ToString();

      fhwa1Threshold.Text = m_DefaultPreferences.m_fhwa1.ToString();
      fhwa2Threshold.Text = m_DefaultPreferences.m_fhwa2.ToString();
      fhwa3Threshold.Text = m_DefaultPreferences.m_fhwa3.ToString();
      fhwa4Threshold.Text = m_DefaultPreferences.m_fhwa4.ToString();
      fhwa5Threshold.Text = m_DefaultPreferences.m_fhwa5.ToString();
      fhwa6Threshold.Text = m_DefaultPreferences.m_fhwa6.ToString();
      fhwa7Threshold.Text = m_DefaultPreferences.m_fhwa7.ToString();
      fhwa8Threshold.Text = m_DefaultPreferences.m_fhwa8.ToString();
      fhwa9Threshold.Text = m_DefaultPreferences.m_fhwa9.ToString();
      fhwa10Threshold.Text = m_DefaultPreferences.m_fhwa10.ToString();
      fhwa11Threshold.Text = m_DefaultPreferences.m_fhwa11.ToString();
      fhwa12Threshold.Text = m_DefaultPreferences.m_fhwa12.ToString();
      fhwa13Threshold.Text = m_DefaultPreferences.m_fhwa13.ToString();
      fhwa6_13Threshold.Text = m_DefaultPreferences.m_fhwa6_13.ToString();

      selectedDataSourceComboBox.SelectedItem = ((Dictionary<QCDataSource, string>)selectedDataSourceComboBox.ItemsSource).FirstOrDefault(x => x.Key == m_DefaultPreferences.m_dataSource);
      userNameTextBox.Text = m_DefaultPreferences.m_userName;
      pwdPasswordBox.Password = "";
      updatePwdCheckBox.IsChecked = false;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
      ExtensionClasses.MerlinButton btn = (ExtensionClasses.MerlinButton)sender;
      TextBox pathText = (TextBox)btn.Tag;

      System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
      System.Windows.Forms.DialogResult result = browser.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        pathText.Text = browser.SelectedPath.ToString();
      }
    }

    private void Directory_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      TextBox thisTextBox = (TextBox) sender;
      string path = thisTextBox.Text;
      bool isLocal = thisTextBox.Tag.ToString() == "local";
      Regex startsWithLetter = new Regex(@"^[A-Za-z]");
      Regex startsWithSlashes = new Regex(@"^[\\][\\]");
      Regex networkPath = new Regex(@"^\\{2}[\w-]+(\\{1}(([\w-][\w-\s]*[\w-]+[$$]?)|([\w-][$$]?$)))+");
      Regex localAbsolutePath = new Regex(@"^[a-zA-Z][:][\\].*");

      if(String.IsNullOrEmpty(path))
      {
        if(isLocal)
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
