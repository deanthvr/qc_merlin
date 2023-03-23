using System;

namespace AppMerlin
{
  public class TimePeriod
  {
    public int ID;
    public DateTime StartTime;
    public DateTime EndTime;

    public string stringStartTime
    {
      get
      {
        return StartTime.TimeOfDay.ToString().Remove(5, 3);
      }
    }

    public string stringEndTime
    {
      get
      {
        return EndTime.TimeOfDay.ToString().Remove(5, 3);
      }
    }

    public TimePeriod()
    {
    }

    public TimePeriod(int ID, DateTime startTime, DateTime endTime)
    {
      this.ID = ID;
      StartTime = startTime;
      EndTime = endTime;
    }

    
    public override bool Equals(object obj)
    {
      return ID.Equals(((TimePeriod)obj).ID);
    }

    public override int GetHashCode()
    {
      return ID.GetHashCode();
    }
  }
}
