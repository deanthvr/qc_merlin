using QCCommon.Logging;
using QCCommon.QCData;
using QCCommon.QCData.DataModels;
using System.Linq;
using System.Security;

namespace AppMerlin
{
  public class DataImporter
  {
    private IQCData QCData { get; set; }
    private QCDataSource DataSource { get; set; }

    public DataImporter(QCDataSource dataSource)
    {
      DataSource = dataSource;
    }

    public IQCData GetDataConnection(string user = null, SecureString password = null)
    {
      switch (DataSource)
      {
        case QCDataSource.SqlServer:
          QCData = new QCCommon.QCData.SqlConnector(Utilities.GetCurrentUser(), new Logger(new FileNameGenerator("No_Project").CreateLogFileName()));
          break;
        case QCDataSource.MySql:
        case QCDataSource.Api:
        default:
          if (string.IsNullOrEmpty(user) || password == null || password.Length == 0)
          {
            //nothing happens
          }
          else
          {
            QCData = new QCApi(user, password, new Logger(new FileNameGenerator("No_Project").CreateLogFileName()));
          }
          break;
      }
      return QCData;
    }

    public TubeSite GenerateTubeSite(QCCommon.QCData.DataModels.Location location, SurveyType type, int orderNumber, TMCProject parent = null)
    {
      var tubeSite = new TubeSite(parent, location.OrderLocationID, location.Latitude.ToString(), location.Longitude.ToString(), location.Street.FirstOrDefault(x => x.StreetDirectionCode == "MAIN")?.StreetName ?? "", AppMerlin.TubeLayouts.EB_WB);

      var id = location.OrderLocationID;
      var locationTimes =
         from c in QCData.GetCountDetails(orderNumber).TubeCount
         join tp in QCData.GetTimePeriodDetails(orderNumber).GetAllTubeCountTimePeriodsFromAllGroups()
         on c.ServiceTimePeriodID equals tp.ServiceTimePeriodID
         where c.OrderLocationID == id
         select new
         {
           StartTime = tp.StartTime,
           EndTime = tp.EndTime,
           Duration = tp.TotalHours,
           SiteCode = c.SiteNumber,
         };

      foreach (var locationTime in locationTimes)
      {
        var tubeCount = new TubeCount(locationTime.SiteCode.ToString(), type, locationTime.StartTime, locationTime.Duration, tubeSite);
        tubeSite.m_TubeCounts.Add(tubeCount);
      }
      return tubeSite;
    }
  }
}
