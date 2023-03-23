using AppMerlin;
using NUnit.Framework;
using System;

namespace UnitTests.Test
{
  [TestFixture]
  public class TSystem : TestBase
  {

    [SetUp]
    public void TestSetup()
    {
      Setup();
    }

    [Test]
    public void PassEmptyFile()
    {
      XmlFile xmlFile = new XmlFile("");
      Assert.That(xmlFile.TryFileLoad<TMCProject>(), Is.False, "Verify that an exception is thrown when given an empty string for a file name.");
      Console.WriteLine(m_xmlFile.GetErrorMessage());
    }

    [Test]
    public void PassBadDirectory()
    {
      XmlFile xmlFile = new XmlFile("C:\\QCProjects\\135586\\135587.xml");
      Assert.That(xmlFile.TryFileLoad<TMCProject>(), Is.False, "Verify that an exception is thrown when given a bad directory.");
      Console.WriteLine(m_xmlFile.GetErrorMessage());
    }

    [Test]
    public void PassCSVFile()
    {
      XmlFile xmlFile = new XmlFile("C:\\QCProjects\\135587\\Data Files\\13558701\\13558701annik_count.csv");
      Assert.That(xmlFile.TryFileLoad<TMCProject>(), Is.False, "Verify that an exception is thrown when given an CSV file.");
      Console.WriteLine(m_xmlFile.GetErrorMessage());
    }
  }
}
