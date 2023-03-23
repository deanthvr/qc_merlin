using NUnit.Framework;
using AppMerlin;
using System.Collections.Generic;

namespace UnitTests.Test
{
  [TestFixture]
  public class TProject : TestBase
  {

    [TestFixtureSetUp]
    public void TestSetup()
    {
      Setup();
    }


    [Test]
    public void LoadTest()
    {
      Assert.That(put.m_OrderNumber, Is.EqualTo(m_OrderNumber), "Verify that Order Number is " + m_OrderNumber);
      Assert.That(put.m_TimePeriods.Count, Is.EqualTo(m_NumTimePeriods), "Verify that NumTimePeriods is " + m_NumTimePeriods);
      foreach (KeyValuePair<string, string> timePeriod in m_TimePeriods) 
      { 
        //Assert.That(put.m_TimePeriods[timePeriod.Key], Is.EqualTo(timePeriod.Value));
      }
    }

  }

}
