using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppMerlin
{
  static class DTHelper
  {
    /// <summary>
    /// Converts a DateTime to a string using the format "MM/dd/yyyy HH:mm"
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ConvertDT(DateTime dt)
    {
      return dt.ToString("MM/dd/yyyy HH:mm");
    }

    public static DateTime ConvertStr(string str)
    {
      DateTime dt;
      if(!DateTime.TryParse(str, out dt))
      {
        throw new FormatException("You must pass in a date & time represented as a string that is able to be parsed by the DateTime class");
      }
      return dt;
    }
  }
}
