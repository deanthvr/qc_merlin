using AppMerlin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merlin.TubeImport
{
  public class ImportDataFile
  {
    public string FileName { get; set; }
    public string SiteCode {get; set;}
    public string Approach { get; set; }
    public string Type { get; set; }

    public ImportDataFile(string file, string siteCode, string approach, string type)
    {
      FileName = file;
      SiteCode = siteCode;
      Approach = approach;
      Type = type;
    }
  }
}
