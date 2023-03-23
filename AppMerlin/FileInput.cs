using System;
using System.Collections.Generic;
using System.IO;

namespace AppMerlin
{
  public class FileInput
  {
    protected string m_FileName;
    protected string m_ErrorMessage;
    protected string m_HelperMessage;
    protected StreamReader m_Reader;

    public FileInput(string file)
    {
      m_FileName = file;
    }

    public FileInput(string filePath, string fileName)
    {
      m_FileName = filePath + fileName;
    }

    #region Accessors
    
    public string GetErrorMessage()
    {
      return m_ErrorMessage;
    }

    public string GetHelperMessage()
    {
      return m_HelperMessage;
    }

    #endregion

    public virtual bool TryFileLoad<T>()
    {
      bool isValid = true;
      try
      {
        m_Reader = new StreamReader(m_FileName);
      }
      catch (FileNotFoundException ex)
      {
        m_HelperMessage = "File Not Found";
        m_ErrorMessage = ex.Message;
        isValid = false;
      }
      catch (InvalidOperationException ex)
      {
        m_HelperMessage = "Invalid Operation";
        m_ErrorMessage = ex.Message;
        isValid = false;
      }
      catch (Exception ex)
      {
        m_HelperMessage = "Exception!";
        m_ErrorMessage = ex.Message;
        isValid = false;
      }
      return isValid;
    }

    public bool GetDataFileLines(ref List<string> lines)
    {
      if (m_Reader == null)
      {
        return false;
      }
      string fileLine;
      while ((fileLine = m_Reader.ReadLine()) != null)
      {
        lines.Add(fileLine);
      }
      m_Reader.Close();
      if (lines.Count < 1)
      {
        return false;
      }
      return true;
    }

    public void CloseStream()
    {
      m_Reader.Close();
    }

    public static SurveyType GetFileType(List<string> fileLines)
    {
      if(fileLines.Count >= 1)
      {
        string token = fileLines[0].Split(',')[0];
        if (token == "Start Date" || token == "Job number")
        {
          return SurveyType.TMC;
        }
        if(fileLines.Count >= 4)
        {
          token = fileLines[3].Split(',')[1];
          if(token == "Vehicle Classification Data")
          {
            return SurveyType.TubeClass;
          }
          else if(token == "Volume Data")
          {
            return SurveyType.TubeVolumeOnly;
          }
        }
      }
      return (SurveyType)(-1);
    }
  }
}
