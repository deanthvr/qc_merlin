using QCCommon.Logging;
using QCCommon.Utilities;

namespace AppMerlin
{
  public class FileNameGenerator
  {
    private const string APP_FOLDER = "Merlin";
    private const string PROJ_FOLDER = "Projects";

    public string OrderNumber { get; }

    public IFileIoWrapper FileWrapper { get; set; } = new FileIoWrapper();

    public FileNameGenerator(string orderNumber)
    {
      OrderNumber = orderNumber;
    }

    public string CreateLogFileName()
    {
      return FileWrapper.CombinePaths(FileWrapper.GetAppDataRoamingFolder(), $"{APP_FOLDER}\\{PROJ_FOLDER}", OrderNumber, $"{OrderNumber}_log.txt");
    }
  }
}
