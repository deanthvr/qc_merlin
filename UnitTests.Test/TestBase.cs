using System.Collections.Generic;
using AppMerlin;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;
using System.Linq;
using System.Windows;

namespace UnitTests.Test
{
  public abstract class TestBase
  {
    public TMCProject put;
    public int m_NumTimePeriods;
    public string m_OrderNumber;
    public Dictionary<string, string> m_TimePeriods;
    public XmlFile m_xmlFile;

    public void Setup()
    {
      m_xmlFile = new XmlFile("C:\\QCProjects\\135587\\135587.xml");
      m_xmlFile.TryFileLoad<TMCProject>();

      if (!m_xmlFile.TryFileLoad<TMCProject>() || !m_xmlFile.DeserializeProjectFile(ref put))
      {
        MessageBox.Show(m_xmlFile.GetErrorMessage(), m_xmlFile.GetHelperMessage(), MessageBoxButton.OK);
      }

      m_NumTimePeriods = 3;
      m_OrderNumber = "135587";
      m_TimePeriods = new Dictionary<string, string>()
      {
        {"AM", "06:00-09:00"},
        {"MID", "11:00-13:00"},
        {"PM", "16:00-19:00"}
      };
    }

  }
}
