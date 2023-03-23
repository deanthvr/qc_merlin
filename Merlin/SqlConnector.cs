using AppMerlin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace Merlin
{
  public class SqlConnector
  {
    private string SQLConnectionString;
    private SqlConnection connection;
    //private string SQLDBConnection;

    private string[] connStrings =  
    {
      @"Data Source=LEGOLAS\MAVEHOME;Initial Catalog=QCApp_Prod;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False",
      @"Data Source=DESKTOP-D1BGTCP;Initial Catalog=QCApp_Prod;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False",
      @"Initial Catalog=QCApp_Staging;Data Source=QCFS1.qualitycounts.net,1433;Packet Size=4096;Persist Security Info=False;Network Library=DBMSSOCN;User Id=qcappuser;Password=catfoxwolf101",
      @"Initial Catalog=QCApp_Prod;Data Source=QCFS1.qualitycounts.net,1433;Packet Size=4096;Persist Security Info=False;Network Library=DBMSSOCN;User Id=qcappuser;Password=catfoxwolf101",
      @"Initial Catalog=QCApp_Prod;Data Source=QCSF1.qualitycounts.net,1433;Packet Size=4096;Persist Security Info=False;Network Library=DBMSSOCN;user=qcuser;password=q2a3z4x5",
      @"Database=QCApp_Prod;Server=QCFS1.QUALITYCOUNTS.NET,1433;user=qcuser;password=q2a3z4x5",
      @"Database=QCApp_Staging;Server=QCFS1.QUALITYCOUNTS.NET,1433;user=qcuser;password=q2a3z4x5",
      @"Data Source=SHIRE\MAVEHOME;Initial Catalog=QCApp_Prod;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False",
    };

    //private string[] dbStrings =
    //{
    //  "Dev Backup (DGC)",
    //  "Choice 1",
    //  "Choice 2",
    //  "Choice 3",
    //  "Choice 4",
    //  "Choice 5",
    //  "Choice 6"
    //};

    public SqlConnector(int choice = 0)
    {
      SQLConnectionString = connStrings[choice];
      //SQLDBConnection = dbStrings[choice];
      connection = new SqlConnection(SQLConnectionString);
    }

    public bool Connect()
    {
      try
      {
        connection.Open();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Could not establish connection to QC SQL Server (QCSS). \n\nAttempted: \n" + SQLConnectionString + "\n\nMessage: \n" + ex.Message,
          "Connection Error", MessageBoxButton.OK);
        return false;
      }
      return true;
    }

    public void Disconnect()
    {
      connection.Close();
    }

    public Dictionary<string, string> GetOrderDetails(int orderNumber)
    {
      Dictionary<string, string> details = new Dictionary<string, string>();
      SqlCommand command = new SqlCommand("Order_MerlinOrderDetails", connection);
      //string query = "SELECT OrderID, OrderDate, ProjectName "
      //               + "FROM [dbo].[Order] "
      //               + "WHERE OrderID = " + orderNumber;
      command.CommandType = System.Data.CommandType.StoredProcedure;
      command.Parameters.Add("@orderNumber", System.Data.SqlDbType.Int, 4);
      command.Parameters["@orderNumber"].Value = orderNumber;
      command.CommandTimeout = 15;

      try
      {
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
          details.Add("OrderID", reader["OrderID"].ToString());
          details.Add("OrderDate", reader["OrderDate"].ToString());
          details.Add("ProjectName", reader["ProjectName"].ToString());
        }
        reader.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message,
          "SQL Query Error on Order Details", MessageBoxButton.OK);
      }

      return details;
    }

    public List<Dictionary<string, string>> GetLocationDetails(int orderNumber)
    {
      List<Dictionary<string, string>> details = new List<Dictionary<string, string>>();
      SqlCommand command = new SqlCommand("Order_MerlinLocationDetails", connection);
      //string query = "SELECT OL.OrderLocationID, OL.Latitude, OL.Longitude, OL.SpecialRequirements, OL.LocationIndex "
      //              + "FROM [OrderLocation] OL "
      //              + "INNER JOIN OrderService OS ON OS.OrderServiceID = OL.OrderServiceID "
      //              + "WHERE OS.OrderID = " + orderNumber + " AND OS.ServiceID = 35"
      //              + "ORDER BY OL.LocationIndex ";
      command.CommandType = System.Data.CommandType.StoredProcedure;
      command.Parameters.Add("@orderNumber", System.Data.SqlDbType.Int, 4);
      command.Parameters["@orderNumber"].Value = orderNumber;
      command.CommandTimeout = 15;

      try
      {
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
          details.Add(new Dictionary<string, string> 
          {
            {"LocationID", reader["OrderLocationID"].ToString()},
            {"Latitude", reader["Latitude"].ToString()},
            {"Longitude", reader["Longitude"].ToString()},
            {"SpecialRequirements", reader["SpecialRequirements"].ToString()},
            {"Index", reader["LocationIndex"].ToString()},
            {"NSStreet", ""},
            {"EWStreet", ""},
            {"TubeLocation", ""},
            {"ServiceID", reader["ServiceID"].ToString()}
          });
        }
        reader.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message,
          "SQL Query Error on Location", MessageBoxButton.OK);
      }

      foreach (Dictionary<string, string> entry in details)
      {
        SqlCommand command2 = new SqlCommand("Order_MerlinStreetNameByLocationId", connection);
        //query = "SELECT OLS.StreetName, OLS.StreetDirectionCode "
        //              + "FROM [OrderLocationStreet] OLS "
        //              + "WHERE OLS.OrderLocationID = " + entry["LocationID"].ToString() 
        //              + "AND (OLS.StreetDirectionCode = 'NS' OR OLS.StreetDirectionCode ='EW')";

        command2.CommandType = CommandType.StoredProcedure;
        command2.Parameters.Add("@locationNumber", System.Data.SqlDbType.Int, 4);
        command2.Parameters["@locationNumber"].Value = int.Parse(entry["LocationID"]);
        command2.CommandTimeout = 15;

        try
        {
          SqlDataReader reader = command2.ExecuteReader();

          while (reader.Read())
          {
            switch (reader["StreetDirectionCode"].ToString())
            {
              case "NS":
                entry["NSStreet"] = reader["StreetName"].ToString();
                break;
              case "EW":
                entry["EWStreet"] = reader["StreetName"].ToString();
                break;
              case "MAIN":
                entry["TubeLocation"] = reader["StreetName"].ToString();
                break;
            }
          }
          reader.Close();
        }
        catch (Exception ex)
        {
          MessageBox.Show(ex.Message,
            "SQL Query Error on Street Names", MessageBoxButton.OK);
        }
      }
      return details;
    }

    public List<Dictionary<string, string>> GetTimePeriodDetails(int orderNumber)
    {
      List<Dictionary<string, string>> details = new List<Dictionary<string, string>>();
      SqlCommand command = new SqlCommand("Order_MerlinTimePeriodDetails", connection);

      //string query = "SELECT OLT.SiteNumber, OLT.OrderLocationID, STP.StartTime, STP.EndTime, STP.SpecialRequirements "
      //              + "FROM [OrderLocationTime] OLT "
      //              + "INNER JOIN [ServiceTimePeriod] STP ON STP.ServiceTimePeriodID = OLT.ServiceTimePeriodID "
      //              + "INNER JOIN [OrderLocation] OL ON OL.OrderLocationID = OLT.OrderLocationID "
      //              + "INNER JOIN [OrderService] OS ON OS.OrderServiceID = OL.OrderServiceID "
      //              + "WHERE OS.OrderID = " + orderNumber 
      //              + "ORDER BY OLT.OrderLocationID ";
      command.CommandType = System.Data.CommandType.StoredProcedure;
      command.Parameters.Add("@orderNumber", System.Data.SqlDbType.Int, 4);
      command.Parameters["@orderNumber"].Value = orderNumber;
      command.CommandTimeout = 15;

      try
      {
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
          details.Add(new Dictionary<string, string> 
          {
            {"LocationID", reader["OrderLocationID"].ToString()},
            {"SiteNumber", reader["SiteNumber"].ToString()},
            {"StartTime", reader["StartTime"].ToString()},
            {"EndTime", reader["EndTime"].ToString()},
            {"SpecialRequirements", reader["SpecialRequirements"].ToString()},
            {"TimePeriodID", reader["ServiceTimePeriodID"].ToString()},
            {"DurationHours", reader["DurationHours"].ToString()},
            {"DayToCount", reader["DayToCount"].ToString()}
          });
        }
        reader.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message,
          "SQL Query Error on Time Period", MessageBoxButton.OK);
      }

      return details;
    }

    public DataTable GetTubeData(int siteCode, SurveyType type)
    {
      string procedure = type == SurveyType.TubeClass ? "Data_MerlinGetTubeVehicleClass" : "Data_MerlinGetTubeVolume";
      try
      {
        using (SqlDataAdapter da = new SqlDataAdapter())
        {
          da.SelectCommand = new SqlCommand(procedure, connection);
          da.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
          da.SelectCommand.Parameters.Add("@SiteNumber", System.Data.SqlDbType.Int, 4);
          da.SelectCommand.Parameters["@SiteNumber"].Value = siteCode;

          DataSet ds = new DataSet();
          da.Fill(ds, "result_name");

          return ds.Tables["result_name"];
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message,
          "SQL Query On Tube Data", MessageBoxButton.OK);
        return null;
      }
    }
  }
}
