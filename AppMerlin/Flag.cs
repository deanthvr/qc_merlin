using System;
using System.Text;
using System.Xml.Serialization;

namespace AppMerlin
{
  public class Flag
  {
    public string m_Key;
    public bool m_IsAcceptable;
    public string m_UserNote;
    public FlagType m_Type;
    public Note m_Note;
    public string m_Information;
    public string m_Movement;
    public string m_IntervalContainingError;
    public string m_Bank;
    [XmlIgnore]
    public Count m_ParentCount;
    
    #region Constructors

    public Flag()
    {
      m_IsAcceptable = false;
      m_UserNote = "";
      m_Information = "";
      m_IntervalContainingError = "";
      m_Movement = "";
      m_ParentCount = new Count();
    }

    //constructor
    public Flag(FlagType errorType, Count parentCount, string bank, string information = "", string movement =
      "", string intervalContainingError = "")
    {
      m_Type = errorType;
      m_Bank = bank;
      m_IntervalContainingError = intervalContainingError;
      m_Information = information;
      m_Movement = movement;
      m_IsAcceptable = false;
      m_UserNote = "";
      m_ParentCount = parentCount;
      m_Key = m_ParentCount.m_Id.Substring(6, 2) + m_Type + m_Information + m_IntervalContainingError;
    }

    #endregion

    public string CreateFlagKey(string id, string type, string information, string interval = "")
    {
      return id.Substring(6, 2) + type + information + interval;
    }

    public string MakeListMessage()
    {
      StringBuilder message = new StringBuilder();

      message.Append("[");
      message.Append(m_ParentCount.m_Id);
      message.Append("]  [");
      message.Append(m_Type);
      message.Append("]");
      if (!String.IsNullOrEmpty(m_IntervalContainingError))
      {
        message.Append("  [");
        message.Append(m_IntervalContainingError);
        message.Append("]");
      }
      message.Append("  ");
      message.Append(m_Information);

      return message.ToString();
    }

  }
}
