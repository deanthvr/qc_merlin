
using System.Windows.Controls;
using AppMerlin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;


namespace Merlin.TubeImport
{
  public enum DataFileType
  {
    CSV,
    ASCII,
    Unknown
  }

  /// <summary>
  /// Interaction logic for FileDialog3.xaml
  /// </summary>
  public partial class TubeDataImporter : Window
  {
    List<ImportDataFile> filesToImport;
    TMCProject project;
    private delegate void UpdateProgressBarValueDelegate(
          DependencyProperty dp, Object value);
    private delegate void UpdateProgressBarTextDelegate(
          DependencyProperty dp, Object value);

    public List<KeyValuePair<string, string>> log;

    public TubeDataImporter(TMCProject proj, List<ImportDataFile> files)
    {
      InitializeComponent();
      log = new List<KeyValuePair<string, string>>();
      project = proj;
      filesToImport = files;
      progressBar.Maximum = files.Count;
    }

    private void windowContent_Rendered(object sender, EventArgs e)
    {
      ProcessFiles();
    }

    private void ProcessFiles()
    {
      UpdateProgressBarValueDelegate updatePBValueDelegate = (progressBar.SetValue);
      UpdateProgressBarTextDelegate updatePBTextDelegate = (fileBlock.SetValue);
      List<string> lines = new List<string>();

      var organizedFiles = OrganizeFiles();
      var filesToSkip = AnalyzeFiles(organizedFiles);

      double i = 1;
      foreach (var entry in organizedFiles)
      {
        ImportDataFile fileToImport = GetFileByHighestPriorityType(entry.Value);

        if (fileToImport == null)
        {
          MessageBox.Show("Something went wrong wtih importing the following files: \n" + entry.Value.Select(x => "\n" + x.FileName).ToList<string>(),
            "File Empty", MessageBoxButton.OK);
          foreach (var file in entry.Value)
          {
            log.Add(new KeyValuePair<string, string>(file.FileName, "There was a problem discerning the type for this file."));
          }
          continue;
        }

        Dispatcher.Invoke(updatePBValueDelegate,
          System.Windows.Threading.DispatcherPriority.Background,
          new object[] { ProgressBar.ValueProperty, i });

        lines.Clear();
        FileInput fileInput = new FileInput(fileToImport.FileName);
        if (!fileInput.TryFileLoad<string>())
        {
          MessageBox.Show("Could not open file. \n\n" + GetPresentableFileName(fileToImport.FileName) + "\n\n" + fileInput.GetErrorMessage(), "Data File Error", MessageBoxButton.OK);
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, "Could not open file."));
          fileInput.CloseStream();
          continue;
        }

        if (!fileInput.GetDataFileLines(ref lines))
        {
          MessageBox.Show("No data in file: \n\n" + GetPresentableFileName(fileToImport.FileName),
            "File Empty", MessageBoxButton.OK);
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, "No data in file."));
          continue;
        }

        if (filesToSkip.Contains(fileToImport.FileName))
        {
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, fileToImport.SiteCode + " " + fileToImport.Approach
            + " has a different interval length than other files for the same count.  Please review the data files."));
          continue;
        }

        TubeCount count = FindCount(fileToImport.SiteCode);
        if (count == null)
        {
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, "Site Code was not in set of tube sites."));
          continue;
        }

        if (CountHasNoDataOverlap(count, lines))
        {
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, fileToImport.SiteCode + " " + fileToImport.Approach
            + " contains no data that overlaps with the time period.  Please review the data file."));
        }


        if (CountWantsClassButNoClassFile(count, fileToImport))
        {
          log.Add(new KeyValuePair<string, string>(fileToImport.FileName, "Site Code " + count.m_SiteCode
            + " has survey type 'Class', but no Class data files were found. All Data will go into Unclassified for approach: "
            + fileToImport.Approach + "."));
        }

        Dispatcher.Invoke(updatePBTextDelegate,
          System.Windows.Threading.DispatcherPriority.Background,
          new object[] { TextBlock.TextProperty, fileToImport.FileName });


        ProcessFile(count, lines, fileToImport);

        foreach (var file in entry.Value)
        {
          if (file.FileName == fileToImport.FileName)
          {
            i++;
            continue;
          }
          log.Add(new KeyValuePair<string, string>(file.FileName, string.Format("This file was skipped because its data was redundant or unnecessary. (Used {0})", fileToImport.FileName)));

          Dispatcher.Invoke(updatePBValueDelegate,
            System.Windows.Threading.DispatcherPriority.Background,
            new object[] { ProgressBar.ValueProperty, i });
          i++;
        }
      }
      DialogResult = true;
    }

    private ImportDataFile GetFileByHighestPriorityType(List<ImportDataFile> list)
    {
      try
      {
        var classAttempt = list.FirstOrDefault(x => x.Type == "Class");
        if (classAttempt != null)
        {
          return classAttempt;
        }
        var volumeAttempt = list.FirstOrDefault(x => x.Type == "Volume");
        if (volumeAttempt != null)
        {
          return volumeAttempt;
        }
        var speedAttempt = list.FirstOrDefault(x => x.Type == "Speed");
        if (speedAttempt != null)
        {
          return speedAttempt;
        }
        else
        {
          return null;
        }
      }
      catch (Exception)
      {
        return null;
      }
    }

    private bool CountWantsClassButNoClassFile(TubeCount count, ImportDataFile file)
    {
      if ((count.m_Type == SurveyType.TubeClass || count.m_Type == SurveyType.TubeSpeedClass) && file.Type != "Class")
      {
        return true;
      }
      return false;
    }

    private bool CountHasNoDataOverlap(TubeCount count, List<string> lines)
    {
      DateTime fileStartTime = DateTime.MaxValue;
      DateTime fileEndTime = DateTime.MinValue;
      foreach (string fileLine in lines)
      {
        if (String.IsNullOrEmpty(fileLine))
        {
          continue;
        }
        var parsedLine = fileLine.Split(',');
        if (!IsFileLineData(parsedLine))
        {
          continue;
        }
        var intervalTime = MakeDateTime(parsedLine[0], parsedLine[1]);

        if (intervalTime < fileStartTime)
        {
          fileStartTime = intervalTime;
        }
        if (intervalTime > fileEndTime)
        {
          fileEndTime = intervalTime;
        }
      }

      if (fileEndTime < count.StartTime || fileStartTime > count.EndTime)
      {
        return true;
      }
      return false;
    }

    private Dictionary<string, List<ImportDataFile>> OrganizeFiles()
    {
      var returnDict = new Dictionary<string, List<ImportDataFile>>();
      foreach (var file in filesToImport)
      {
        var key = file.SiteCode + " " + file.Approach;
        if (!returnDict.ContainsKey(key))
        {
          returnDict.Add(key, new List<ImportDataFile>());
        }
        returnDict[key].Add(file);
      }
      return returnDict;
    }

    private List<string> AnalyzeFiles(Dictionary<string, List<ImportDataFile>> files)
    {
      var listOfMismatches = new List<string>();
      var fileSets = PutFilesIntoPairs(files);
      foreach (var set in fileSets)
      {
        if (IntervalLengthMismatched(set))
        {
          listOfMismatches.AddRange(set);
        }
      }

      return listOfMismatches;
    }

    private List<List<string>> PutFilesIntoPairs(Dictionary<string, List<ImportDataFile>> files)
    {
      var setList = new Dictionary<string, List<string>>();

      foreach (var item in files)
      {
        var siteCode = item.Key.Split(' ')[0];
        if (!setList.ContainsKey(siteCode))
        {
          setList.Add(siteCode, new List<string>());
        }
        setList[siteCode].Add(GetFileByHighestPriorityType(item.Value).FileName);
      }

      return setList.Values.ToList();
    }

    private bool IntervalLengthMismatched(List<string> set)
    {
      var setDictionary = new Dictionary<string, IntervalLength>();
      IntervalLength compareValue = IntervalLength.Fifteen;
      foreach (var file in set)
      {
        List<string> lines = new List<string>();
        FileInput fileInput = new FileInput(file);
        if (!fileInput.TryFileLoad<string>())
        {
          continue;
        }
        if (!fileInput.GetDataFileLines(ref lines))
        {
          continue;
        }
        compareValue = DetermineIntervalLength(lines);
        setDictionary.Add(file, compareValue);
      }
      return setDictionary.Any(x => x.Value != compareValue);
    }

    private IntervalLength DetermineIntervalLength(List<string> lines)
    {
      DateTime lineA = DateTime.MinValue;
      DateTime lineB = DateTime.MinValue;
      foreach (string fileLine in lines)
      {
        if (String.IsNullOrEmpty(fileLine))
        {
          continue;
        }
        var parsedLine = fileLine.Split(',');
        if (!IsFileLineData(parsedLine))
        {
          continue;
        }
        if (lineA == DateTime.MinValue)
        {
          lineA = MakeDateTime(parsedLine[0], parsedLine[1]);
        }
        else if (lineB == DateTime.MinValue && MakeDateTime(parsedLine[0], parsedLine[1]) != lineA)
        {
          lineB = MakeDateTime(parsedLine[0], parsedLine[1]);
        }
        if (lineA != DateTime.MinValue && lineB != DateTime.MinValue)
        {
          break;
        }
      }
      var spanInMinutes = (lineB - lineA).TotalMinutes;
      switch ((int)spanInMinutes)
      {
        case 5:
          return IntervalLength.Five;
        case 30:
          return IntervalLength.Thirty;
        case 60:
          return IntervalLength.Sixty;
        default:
          return IntervalLength.Fifteen;
      }
    }

    private bool IsFileLineData(string[] fileLine)
    {
      if (fileLine.Length <= 2)
      {
        return false;
      }
      DateTime trialDate;
      if (DateTime.TryParse(fileLine[0] + " " + fileLine[1], out trialDate))
      {
        return true;
      }
      return false;
    }

    private DateTime MakeDateTime(string date, string time)
    {
      try
      {
        return DateTime.Parse(date + " " + time);
      }
      catch (Exception)
      {
        return new DateTime();
      }
    }

    private void ProcessFile(TubeCount count, List<string> lines, ImportDataFile fileInfo)
    {
      string fileName = fileInfo.FileName;

      if (!IsFileTypeValid(fileName))
      {
        MessageBox.Show("File has unknown extension. \n\n" + GetPresentableFileName(fileName),
          "Data File Error", MessageBoxButton.OK);
        log.Add(new KeyValuePair<string, string>(fileName, "File had unknown extension."));
        return;
      }

      count.m_IntervalSize = DetermineIntervalLength(lines);
      count.ImportAsciiData(lines, fileInfo.Approach, fileInfo.Type);

    }

    private bool IsFileTypeValid(string fileName)
    {
      if (fileName.EndsWith("txt"))
      {
        return true;
      }
      return false;
    }

    private TubeCount FindCount(string key)
    {
      foreach (TubeSite tubeSite in project.m_Tubes)
      {
        foreach (TubeCount count in tubeSite.m_TubeCounts)
        {
          if (count.m_SiteCode == key)
          {
            return count;
          }
        }
      }
      return null;
    }

    private string GetPresentableFileName(string fullPathName)
    {
      return fullPathName.Split('\\')[fullPathName.Split('\\').Length - 1];
    }
  }
}
