using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppMerlin
{
  //Stores information about an approach
  public class IntersectionApproach
  {
    public string ApproachName;
    public PossibleIntersectionApproaches ApproachDirection;
    public PossibleApproachFlows TrafficFlowType;
    public double ApproachHeading;
    //public List<IntersectionApproach> Movements;  //List of approach destinations for this approach (ped movement is implied)

    //Constructors for this class

    public IntersectionApproach()
    {

    }

    public IntersectionApproach(string ApproachName, PossibleIntersectionApproaches ApproachDirection, PossibleApproachFlows TrafficFlowType)
    {
      this.ApproachName = ApproachName;
      this.ApproachDirection = ApproachDirection;
      this.TrafficFlowType = TrafficFlowType;
      //Movements = new List<IntersectionApproach>();

      ApproachHeading = (180.0 + (((int)ApproachDirection) * 45)) % 360.0;
    }
  }
}
