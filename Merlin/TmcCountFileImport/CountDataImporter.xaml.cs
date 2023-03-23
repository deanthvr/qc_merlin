using System.Data;
using System.Windows.Controls;
using AppMerlin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;

namespace Merlin.TmcCountFileImport
{
  public enum DataFileType
  {
    CSV,
    ASCII,
    TCC,
    Unknown
  }

  /// <summary>
  /// Interaction logic for FileDialog3.xaml
  /// </summary>
  public partial class CountDataImporter : Window
  {
    Dictionary<string, Tuple<string, string>> filesToImport;
    TMCProject project;
    private int option;
    private bool disjointDataError;
    private bool dataPastEndError;
    private delegate void UpdateProgressBarValueDelegate(
          DependencyProperty dp, Object value);
    private delegate void UpdateProgressBarTextDelegate(
          DependencyProperty dp, Object value);

    public List<KeyValuePair<string, string>> log;

    private delegate void importDelegate(List<string> lines, string fileName, string approach = null);
    private delegate bool conflictDelegate(Count count, List<string> lines, ref Count fileCount, string approach = null);

    public CountDataImporter(TMCProject proj, Dictionary<string, Tuple<string, string>> files, int opt)
    {
      InitializeComponent();
      log = new List<KeyValuePair<string, string>>();
      project = proj;
      filesToImport = files;
      progressBar.Maximum = files.Count;
      option = opt;
    }

    private void windowContent_Rendered(object sender, EventArgs e)
    {
      ProcessFiles();

      project.PopulateDataCellMaps();
    }

    private void ProcessFiles()
    {
      UpdateProgressBarValueDelegate updatePBValueDelegate = (progressBar.SetValue);
      UpdateProgressBarTextDelegate updatePBTextDelegate = (fileBlock.SetValue);
      List<string> lines = new List<string>();

      double i = 1;
      foreach (var file in filesToImport)
      {
        Dispatcher.Invoke(updatePBValueDelegate,
          System.Windows.Threading.DispatcherPriority.Background,
          new object[] { ProgressBar.ValueProperty, i });
        lines.Clear();
        FileInput fileInput = new FileInput(file.Key);
        if (!fileInput.TryFileLoad<string>())
        {
          MessageBox.Show("Could not open file. \n\n" + file.Key.Split('\\')[file.Key.Split('\\').Length - 1] 
            + "\n\n" + fileInput.GetErrorMessage(), "Data File Error", MessageBoxButton.OK);
          log.Add(new KeyValuePair<string, string>(file.Key, "Could not open file."));
          fileInput.CloseStream();
          continue;
        }
        if (!fileInput.GetDataFileLines(ref lines))
        {
          MessageBox.Show("No data in file: \n\n" + file.Key.Split('\\')[file.Key.Split('\\').Length - 1],
            "File Empty", MessageBoxButton.OK);
          log.Add(new KeyValuePair<string, string>(file.Key, "No data in file."));
          fileInput.CloseStream();
          continue;
        }

        Count count = FindCount(file.Value.Item1);
        if (count == null)
        {
          MessageBox.Show("Site Code was not found in Project.  Something went wrong. \n\n" + file.Value.Item1,
                      "Site Code Error", MessageBoxButton.OK);
          continue;
        }

        Dispatcher.Invoke(updatePBTextDelegate,
          System.Windows.Threading.DispatcherPriority.Background,
          new object[] { TextBlock.TextProperty, file.Key });

        ProcessFile(count, lines, file);

        if (count.m_ParentIntersection.m_ParentProject.m_NCDOTColSwappingEnabled)
        {
          log.Add(new KeyValuePair<string, string>(file.Key, "Ped column data swapped between bank 0 and bank 2."));
        }
        i++;
      }
      DialogResult = true;
    }

    private void ProcessFile(Count count, List<string> lines, KeyValuePair<string, Tuple<string, string>> fileInfo)
    {
      Count fileCount = count.CopyLite();
      bool conflictResult;
      importDelegate dataImportFunction;
      conflictDelegate conflictFunction;
      string fileName = fileInfo.Key;

      DataFileType fileType;
      if (!GetFileType(out fileType, fileName))
      {
        MessageBox.Show("File has unknown extension. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
          "Data File Error", MessageBoxButton.OK);
        log.Add(new KeyValuePair<string, string>(fileName, "File had unknown extension."));
        return;
      }
      switch (fileType)
      {
        case DataFileType.CSV:
          conflictFunction = HasDataFileConflictCSV;
          dataImportFunction = count.ImportCSVData;
          break;
        case DataFileType.ASCII:
          conflictFunction = HasDataFileConflictASCII;
          dataImportFunction = count.ImportASCIIData;
          break;
        case DataFileType.TCC:
          conflictFunction = HasDataFileConflictTCC_CSV;
          dataImportFunction = count.ImportTCC_CSVData;
          break;
        default:
          throw new Exception("Unknown file type made it into the processing method.");
      }
      conflictResult = conflictFunction(count, lines, ref fileCount, fileInfo.Value.Item2);
      if (disjointDataError)
      {
        MessageBox.Show("File did not contain data for any intervals in this count. \n\n" + fileName.Split('\\')[fileName.Split('\\').Length - 1],
            "Data File Error", MessageBoxButton.OK);
        log.Add(new KeyValuePair<string, string>(fileName, "File did not contain data for any intervals in this count."));
        return;
      }
      if (dataPastEndError)
      {
        log.Add(new KeyValuePair<string, string>(fileName, "Data found past final interval."));
      }

      DataFile newFile = new DataFile(fileName);
      if (!count.HasDataFile(fileName))
      {
        count.AddFileNameToFileList(newFile);
      }

      if (option == 1 || (option == 2 && conflictResult))
      {
        var timeBoundaries = GetTimeBoundaries(count, lines);
        MergeResolutionDialog fd4 = new MergeResolutionDialog(count, fileCount, fileName, timeBoundaries);
        fd4.Owner = Owner;
        bool? result = fd4.ShowDialog();
        if (result == true && !fd4.useCurrentCount)
        {
          if (fd4.applyAllData)
          {

            string endTime = DateTime.Parse(fileCount.m_EndTime).AddMinutes(-5).TimeOfDay.ToString().Remove(5, 3);
            dataImportFunction(lines, fileName, fileInfo.Value.Item2);
            //if (count.m_Id == "13558705")
            //{
            //  count.PrintOutFileAssociation();
            //}
            //if (count.CopyData(fileName, ref fileCount.m_Data, true, timeBoundaries.Item1, timeBoundaries.Item2))
            //{
              log.Add(new KeyValuePair<string, string>(fileName, "Merge Conflict Resolution initiated for this file.  The user chose Apply ALL Data (left-side)."));  
            //}
            //else
            //{
            //  log.Add(new KeyValuePair<string, string>(fileName, "Something went wrong importing the data from this file.  Review the data for the associated count."));
            //}
          }
          else
          {
            bool foundBadTime = false;
            Dictionary<string, string> badTimes = new Dictionary<string, string>();
            List<string> banksToInclude = new List<string>();
            if (fd4.allBanksChecked == true)
            {
              banksToInclude = project.m_Banks.Where(x => x != "NOT USED").ToList<string>();
            }
            else
            {
              foreach (var box in fd4.bankPanel.Children)
              {
                if (box.GetType() == (typeof(CheckBox)))
                {
                  if (((CheckBox)box).IsChecked == true)
                  {
                    banksToInclude.Add(((CheckBox)box).Content.ToString());
                  }
                }
              }
            }
            foreach (var item in fd4.fileDataGrid.SelectedCells)
            {
              if (item.Column.DisplayIndex == 0)
              {
                continue;
              }
              DataRowView row = item.Item as DataRowView;
              string startTime = row.Row.ItemArray[0].ToString();
              string endTime = DateTime.Parse(startTime).AddMinutes(5).TimeOfDay.ToString().Remove(5, 3);
              if (!count.CopyData(fileName, ref fileCount.m_Data, true, startTime, startTime, new List<int> { item.Column.DisplayIndex }, banksToInclude))
              {
                foundBadTime = true;
                badTimes.Add(row.Row.ItemArray[0].ToString(), item.Column.Header.ToString());
              }
            }
            if (foundBadTime)
            {
              log.Add(new KeyValuePair<string, string>(fileName, "Some cells did not overlap with associated count time period.  Results may not have been as expected."));
            }

            //if (count.m_Id == "13558705")
            //{
            //  count.PrintOutFileAssociation();
            //}
            log.Add(new KeyValuePair<string, string>(fileName, "Merge Conflict Resolution initiated for this file.  The user chose Apply SELECTED Data (left-side)."));
          }
        }
        else
        {
          log.Add(new KeyValuePair<string, string>(fileName, "Merge Conflict Resolution initiated for this file.  The user chose to use existing data (right-side)."));
        }
      }
      else
      {
        dataImportFunction(lines, fileName, fileInfo.Value.Item2);
      }
    }


    private bool HasDataFileConflictCSV(Count count, List<string> lines, ref Count fileCount, string approach = null)
    {
      bool conflict = false;
      disjointDataError = true;
      dataPastEndError = false;
      Dictionary<int, List<string>> columnHeaders = project.m_ColumnHeaders;
      int bank = 0;
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
              break;
            case "Bank number":
              bank = int.Parse(parsedLine[1]);
              if (bank >= count.m_ParentIntersection.m_ParentProject.GetNumberOfBanksInUse())
              {
                return conflict;
              }
              break;
            default:
              int thisRow = 0;
              string interval = parsedLine[0];
              DateTime testTime;
              if (!DateTime.TryParse(interval, out testTime))
              {
                break;
              }
              if (interval.Length != 5)
              {
                interval = "0" + interval;
              }
              foreach (DataRow r in fileCount.m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == interval)
                {
                  disjointDataError = false;
                  break;
                }
                thisRow++;
              }
              if (thisRow >= count.m_NumIntervals)
              {
                int quickSum = 0;
                for (int v = 1; v < parsedLine.Count; v++)
                {
                  quickSum += int.Parse(parsedLine[v]);
                }
                if (quickSum > 0)
                {
                  dataPastEndError = true;
                }
                break;
              }
              for (int i = 1; i < columnHeaders[bank].Count(); i++)
              {
                int tempBank = bank;
                //NCDOT Column Swapping - bank 0 and bank 2 ped columns swap
                if ((bank == 0 || bank == 2) && project.m_NCDOTColSwappingEnabled && (i % 4) == 0)
                {
                  tempBank = bank == 0 ? 2 : 0;
                }
                if (int.Parse(count.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]].ToString()) > 0
                  && int.Parse(parsedLine[i]) != int.Parse(count.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]].ToString()))
                {
                  conflict = true;
                }
                fileCount.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]] =
                  int.Parse(parsedLine[i]);

              }
              break;
          }
        }
      }
      return conflict;
    }

    private bool HasDataFileConflictASCII(Count count, List<string> lines, ref Count fileCount, string approach = null)
    {
      bool conflict = false;
      disjointDataError = true;
      dataPastEndError = false;
      Dictionary<int, List<string>> columnHeaders = project.m_ColumnHeaders;
      int bank = 0;
      string interval = "";
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Start Date":
            case "Start Time":
            case "Site Code":
            case "Street Name":
              break;
            default:
              DateTime testTime;
              if (!DateTime.TryParse(parsedLine[0], out testTime))
              {
                break;
              }
              if (parsedLine[0] == interval)
              {
                bank++;
              }
              else
              {
                bank = 0;
                interval = parsedLine[0];
              }
              int sum = 0;
              for (int s = 1; s < parsedLine.Count; s++)
              {
                sum += int.Parse(parsedLine[s]);
              }
              if (sum < 1)
              {
                break;
              }
              int thisRow = 0;
              string adjustedInterval;
              string[] time = parsedLine[0].Split(' ');
              if (time[1] == "AM")
              {
                adjustedInterval = time[0];
              }
              else
              {
                int hour = int.Parse(time[0].Split(':')[0]);
                hour = hour + 12;
                adjustedInterval = hour + ":" + time[0].Split(':')[1];
              }

              foreach (DataRow r in count.m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == adjustedInterval)
                {
                  disjointDataError = false;
                  //TODO: The interval was not in the time period.
                  break;
                }
                thisRow++;
              }
              if (thisRow >= count.m_NumIntervals)
              {
                break;
              }
              for (int i = 1; i < columnHeaders[bank].Count(); i++)
              {
                int tempBank = bank;
                //NCDOT Column Swapping - bank 0 and bank 2 ped columns swap
                if ((bank == 0 || bank == 2) && project.m_NCDOTColSwappingEnabled && (i % 4) == 0)
                {
                  tempBank = bank == 0 ? 2 : 0;
                }
                if (int.Parse(count.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]]
                        .ToString()) > 0 && int.Parse(parsedLine[i]) != int.Parse(
                      count.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]].ToString()))
                {
                  conflict = true;
                }
                fileCount.m_Data.Tables["Bank " + tempBank].Rows[thisRow][columnHeaders[tempBank][i]] =
                  int.Parse(parsedLine[i]);
              }
              break;
          }
        }
      }
      return conflict;
    }

    private bool HasDataFileConflictTCC_CSV(Count count, List<string> lines, ref Count fileCount, string approach = null)
    {
      bool conflict = false;
      disjointDataError = true;
      dataPastEndError = false;
      Dictionary<int, List<string>> columnHeaders = project.m_ColumnHeaders;
      var approaches = ParseApproach(approach);
      if (approaches.Length == 0)
      {
        return true;
      }
      int bank = 0;
      foreach (string fileLine in lines)
      {
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
              break;
            case "Bank number":
              bank = int.Parse(parsedLine[1]);
              //Check to see if we are looking at a bank that we won't use.
              if (bank >= approaches.Length)
              {
                return conflict;
              }

              break;
            default:
              DateTime testTime;
              if (!DateTime.TryParse(parsedLine[0], out testTime))
              {
                break;
              }
              // If sum of interval is 0, don't do anything.  This could be an extra interval, or a time the counter intentionally didn't count.
              int sum = 0;
              for (int s = 1; s < parsedLine.Count; s++)
              {
                sum += int.Parse(parsedLine[s]);
              }
              if (sum < 1)
              {
                break;
              }
              int thisRow = 0;
              // If the time is before 10am and after midnight, we need to add the leading 0.
              string interval = parsedLine[0];
              if (interval.Length != 5)
              {
                interval = "0" + interval;
              }

              // Line up the intervals to enter the data in the right spot
              foreach (DataRow r in count.m_Data.Tables["Bank " + bank].Rows)
              {
                if (r.ItemArray[0].ToString() == interval)
                {
                  disjointDataError = false;
                  break;
                }
                thisRow++;
              }
              // Can't go past the number of intervals in the count
              if (thisRow >= count.m_NumIntervals)
              {
                int quickSum = 0;
                for (int v = 1; v < parsedLine.Count; v++)
                {
                  quickSum += int.Parse(parsedLine[v]);
                }
                if (quickSum > 0)
                {
                  dataPastEndError = true;
                }
                break;
              }

              for (int i = 0; i < project.m_Banks.Count - 1; i++)
              {
                if (project.m_Banks[i] == "NOT USED")
                {
                  continue;
                }
                if (int.Parse(count.m_Data.Tables[i].Rows[thisRow][approaches[bank]].ToString()) > 0 &&
                  count.m_Data.Tables[i].Rows[thisRow][approaches[bank]].ToString() != parsedLine[i + 1])
                {
                  conflict = true;
                }
                fileCount.m_Data.Tables[i].Rows[thisRow][approaches[bank]] = int.Parse(parsedLine[i + 1]);
              }
              if (approaches[bank][2] == 'T' && approach.Length == 2)
              {
                int pedBank = project.m_PedBanks.IndexOf(PedColumnDataType.Pedestrian);
                if (pedBank >= 0)
                {
                  if (int.Parse(count.m_Data.Tables[pedBank].Rows[thisRow][approach + "P"].ToString()) > 0 &&
                    count.m_Data.Tables[pedBank].Rows[thisRow][approach + "P"].ToString() != parsedLine[project.m_Banks.Count])
                  {
                    conflict = true;
                  }
                  fileCount.m_Data.Tables[pedBank].Rows[thisRow][approach + "P"] = parsedLine[project.m_Banks.Count + 1];
                }
              }
              if (approaches[bank][2] != 'U')
              {
                var bikeCheck = project.m_Banks.FirstOrDefault(x => x == "Bicycles" || x == "FHWAPedsBikes");

                int bikeBank = -1;
                if (bikeCheck != null)
                {
                  bikeBank = project.m_Banks.IndexOf(project.m_Banks.FirstOrDefault(x => x == "Bicycles" || x == "FHWAPedsBikes"));
                }
                if (bikeBank >= 0)
                {
                  if (int.Parse(count.m_Data.Tables[bikeBank].Rows[thisRow][approaches[bank]].ToString()) > 0 &&
                    count.m_Data.Tables[bikeBank].Rows[thisRow][approaches[bank]].ToString() != parsedLine[project.m_Banks.Count + 1])
                  {
                    conflict = true;
                  }
                  fileCount.m_Data.Tables[bikeBank].Rows[thisRow][approaches[bank]] = parsedLine[project.m_Banks.Count + 2];
                }
              }
              break;
          }
        }

      }
      return conflict;
    }

    private string[] ParseApproach(string approach)
    {
      if (approach.Length == 2)
      {
        return new string[] { approach + "T", approach + "R", approach + "L", approach + "U" };
      }
      else if (approach.Length == 3)
      {
        return new string[] { approach };
      }
      return new string[0];
    }

    private bool GetFileType(out DataFileType fileType, string fileName)
    {
      if (fileName.EndsWith("csv"))
      {
        if (project.m_TCCDataFileRules)
        {
          fileType = DataFileType.TCC;
          return true;
        }
        else
        {
          fileType = DataFileType.CSV;
          return true;
        }
      }
      else if (fileName.EndsWith("txt"))
      {
        fileType = DataFileType.ASCII;
        return true;
      }
      fileType = DataFileType.Unknown;
      return false;
    }
    
    private Count FindCount(string key)
    {
      foreach (Intersection intersection in project.m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          if (count.m_Id == key)
          {
            return count;
          }
        }
      }
      return null;
    }

    private Tuple<DateTime, DateTime> GetTimeBoundaries(Count count, List<string> lines)
    {
      bool finished = false;
      DateTime startTime = DateTime.Parse(count.m_EndTime);
      DateTime endTime = DateTime.Parse(count.m_StartTime);
      if (endTime <= startTime)
      {
        startTime = startTime.AddDays(1);
      }
      foreach (string fileLine in lines)
      {
        if(finished)
        {
          break;
        }
        if (fileLine != "" || fileLine != String.Empty)
        {
          List<string> parsedLine = fileLine.Split(',').ToList();
          switch (parsedLine[0])
          {
            case "Job number":
            case "Employee ID":
            case "Time":
              break;
            case "Bank number":
              if (int.Parse(parsedLine[1]) > 0)
              {
                finished = true;
              }
              break;
            default:
              string interval = parsedLine[0];
              DateTime testTime;
              if (!DateTime.TryParse(interval, out testTime))
              {
                break;
              }
              if (testTime < startTime)
              {
                startTime = testTime;
              }
              if (testTime > endTime)
              {
                endTime = testTime;
              }
              break;
          }
        }
      }

      return new Tuple<DateTime, DateTime>(startTime, endTime);
    }
  }
}
