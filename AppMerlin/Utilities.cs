using System;

namespace AppMerlin
{
  public static class Utilities
  {
    public static string GetCurrentUser()
    {
      string user;
      try
      {
        //It was suggested in some online documentation that this might not always work if the user's full name isn't assigned in a computer
        user = System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName;
        if (user == null)
        {
          throw new Exception();
        }
      }
      catch
      {
        user = Environment.UserName;
      }
      if (user == "David Crisman")
      {
        user = System.Environment.MachineName + "\\" + user;
      }
      return user;
    }

  }
}
