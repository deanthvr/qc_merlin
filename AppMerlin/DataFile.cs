using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppMerlin
{
  public class DataFile : ICloneable
  {
    public string Name;
    public string Counter;
    public List<string> DataCellMap;
    public DateTime ImportDate;
    public DateTime LastModified;

    public string TidyName
    {
      get { return Name.Split('\\')[Name.Split('\\').Length - 1]; }
    }

    public object Clone()
    {
      DataFile newFile = new DataFile(Name, LastModified, Counter);
      newFile.ImportDate = this.ImportDate;
      newFile.DataCellMap = new List<string>(DataCellMap.Select(x => x));

      return newFile;
    }

    public DataFile()
    {
      Name = "";
      DataCellMap = new List<string>();
      ImportDate = DateTime.Now;
    }

    public DataFile(string name)
    {
      Name = name;
      DataCellMap = new List<string>();
      ImportDate = DateTime.Now;
      Counter = GetCounterFromDataFile(name);
      LastModified = File.GetLastWriteTime(name);
    }

    public DataFile(string name, DateTime lastModifed, string counter)
    {
      Name = name;
      DataCellMap = new List<string>();
      ImportDate = DateTime.Now;
      Counter = counter;
      LastModified = lastModifed;
    }

    public void AddDataCell(string cell)
    {
      DataCellMap.Add(cell);
    }

    public void RemoveDataCell(string cell)
    {
      DataCellMap.Remove(cell);
    }

    private string GetCounterFromDataFile(string file)
    {
      if (Path.GetExtension(file) == ".csv")
      {
        List<string> lines = new List<string>();
        FileInput fileInput = new FileInput(file);
        fileInput.TryFileLoad<string>();
        fileInput.GetDataFileLines(ref lines);
        if (lines.Count > 0)
        {
          foreach (string line in lines)
          {
            if (line.Split(',')[0] == "Employee")
            {
              return line.Split(',')[1];
            }
          }
        }
      }
      return "";
    }

    public string MakeDataCellKey(string tableName, string columnHeader, string intervalTime)
    {
      return tableName + "," + columnHeader + "," + intervalTime;
    }

  }
}
