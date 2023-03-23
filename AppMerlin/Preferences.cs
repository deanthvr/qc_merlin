using QCCommon.QCData;
using QCCommon.Extension;
using System.Security;
using System.Xml.Serialization;
using QCCommon.Utilities;

namespace AppMerlin
{
  public class Preferences
  {
    private static string m_EncryptionKey = "da0f6895-5622-4b5b-a593-c9e31bfdef46";
    private static ICrypto m_Crypto = new CryptoAes();

    public int m_DaysToSearch;

    public string m_LocalProjectDirectory;
    public string m_LocalDataDirectory;
    public string m_LocalAsciiDirectory;
    public string m_LocalQCConversionDirectory;

    public string m_NetworkProjectDirectory;
    public string m_NetworkDataDirectory;
    public string m_NetworkAsciiDirectory;
    public string m_NetworkQCConversionDirectory;

    public double m_LowInterval;
    public double m_HighInterval;
    public int m_IntervalMin;
    public double m_LowHeavies;
    public double m_HighHeavies;
    public int m_HeaviesMin;
    public double m_TimePeriodLow;
    public double m_TimePeriodHigh;
    public int m_TimePeriodMin;
    public double m_CrossPeakDiff;
    public double m_BalancingDiff;

    public float m_fhwa1;
    public float m_fhwa2;
    public float m_fhwa3;
    public float m_fhwa4;
    public float m_fhwa5;
    public float m_fhwa6;
    public float m_fhwa7;
    public float m_fhwa8;
    public float m_fhwa9;
    public float m_fhwa10;
    public float m_fhwa11;
    public float m_fhwa12;
    public float m_fhwa13;
    public float m_fhwa6_13;

    public QCDataSource m_dataSource;
    public string m_userName;
    [XmlIgnore]
    public SecureString m_password;
    [XmlElement("m_password")]
    public string PasswordForXml
    {
      get
      {
        return (m_password == null || m_password.Length == 0) ? "" : m_Crypto.EncryptString(m_password.GetStringValue(), m_EncryptionKey);
      }
      set
      {
        m_password = string.IsNullOrEmpty(value) ? new SecureString() : m_Crypto.DecryptString(value, m_EncryptionKey).GetSecureString();
      }
    }
    

    public Preferences()
    {
    }

    public Preferences CopyPreferences()
    {
      Preferences newPrefs = new Preferences();

      newPrefs.m_LocalProjectDirectory = this.m_LocalProjectDirectory;
      newPrefs.m_LocalDataDirectory = this.m_LocalDataDirectory;
      newPrefs.m_LocalAsciiDirectory = this.m_LocalAsciiDirectory;
      newPrefs.m_LocalQCConversionDirectory = this.m_LocalQCConversionDirectory;
      
      newPrefs.m_NetworkProjectDirectory = this.m_NetworkProjectDirectory;
      newPrefs.m_NetworkDataDirectory = this.m_NetworkDataDirectory;
      newPrefs.m_NetworkAsciiDirectory = this.m_NetworkAsciiDirectory;
      newPrefs.m_NetworkQCConversionDirectory = this.m_NetworkQCConversionDirectory;
      
      newPrefs.m_LowInterval = this.m_LowInterval;
      newPrefs.m_HighInterval = this.m_HighInterval;
      newPrefs.m_IntervalMin = this.m_IntervalMin;
      newPrefs.m_LowHeavies = this.m_LowHeavies;
      newPrefs.m_HighHeavies = this.m_HighHeavies;
      newPrefs.m_HeaviesMin = this.m_HeaviesMin;
      newPrefs.m_TimePeriodLow = this.m_TimePeriodLow;
      newPrefs.m_TimePeriodHigh = this.m_TimePeriodHigh;
      newPrefs.m_TimePeriodMin = this.m_TimePeriodMin;
      newPrefs.m_CrossPeakDiff = this.m_CrossPeakDiff;
      newPrefs.m_BalancingDiff = this.m_BalancingDiff;

      newPrefs.m_fhwa1 = this.m_fhwa1;
      newPrefs.m_fhwa2 = this.m_fhwa2;
      newPrefs.m_fhwa3 = this.m_fhwa3;
      newPrefs.m_fhwa4 = this.m_fhwa4;
      newPrefs.m_fhwa5 = this.m_fhwa5;
      newPrefs.m_fhwa6 = this.m_fhwa6;
      newPrefs.m_fhwa7 = this.m_fhwa7;
      newPrefs.m_fhwa8 = this.m_fhwa8;
      newPrefs.m_fhwa9 = this.m_fhwa9;
      newPrefs.m_fhwa10 = this.m_fhwa10;
      newPrefs.m_fhwa11 = this.m_fhwa11;
      newPrefs.m_fhwa12 = this.m_fhwa12;
      newPrefs.m_fhwa13 = this.m_fhwa13;
      newPrefs.m_fhwa6_13 = this.m_fhwa6_13;

      newPrefs.m_dataSource = this.m_dataSource;
      newPrefs.m_userName = this.m_userName;
      newPrefs.m_password = this.m_password;

      return newPrefs;
    }


    public void LoadDefaultPreferences()
    {
      m_DaysToSearch = 14;

      m_LocalProjectDirectory = @"C:\QCProjects\";
      m_LocalDataDirectory = @"C:\QCProjects\";
      m_LocalAsciiDirectory = @"C:\QCProjects\";
      m_LocalQCConversionDirectory = @"C:\QCProjects\";


      m_NetworkProjectDirectory = @"\\qcfs1\Office docs\(17) TPA_VRC\Projects";
      m_NetworkDataDirectory = @"\\tpanetwork\NFL";
      m_NetworkAsciiDirectory = @"\\qcfs1\Office docs\(17) TPA_VRC\Jamar\";
      m_NetworkQCConversionDirectory = @"\\qcfs1\Office docs\(17) TPA_VRC\QC Conversion Tool For New Website\QC Conversion Tool v5.1";
      m_LowInterval = 0.25;
      m_HighInterval = 2.00;
      m_IntervalMin = 200;
      m_LowHeavies = 0.05;
      m_HighHeavies = 0.50;
      m_HeaviesMin = 100;
      m_TimePeriodLow = 0.80;
      m_TimePeriodHigh = 1.25;
      m_TimePeriodMin = 200;
      m_CrossPeakDiff = 0.25;
      m_BalancingDiff = 0.02;

      m_fhwa1    = 5.0F;
      m_fhwa2    = 5.0F;
      m_fhwa3    = 5.0F;
      m_fhwa4    = 5.0F;
      m_fhwa5    = 5.0F;
      m_fhwa6    = 5.0F;
      m_fhwa7    = 5.0F;
      m_fhwa8    = 5.0F;
      m_fhwa9    = 5.0F;
      m_fhwa10   = 5.0F;
      m_fhwa11   = 5.0F;
      m_fhwa12   = 5.0F;
      m_fhwa13   = 5.0F;
      m_fhwa6_13 = 5.0F;

      m_dataSource = QCDataSource.SqlServer;
      m_userName = "";
      m_password = new SecureString();
    }
  }
}
