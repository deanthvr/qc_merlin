
namespace AppMerlin
{
  //Not currently used to store Intersection neighbor connections, used to serialize one
  public class Neighbor
  {
    public BalancingInsOuts myConnection;
    public Intersection neighbor;
    public BalancingInsOuts neighborConnection;
    
    public Neighbor(BalancingInsOuts myConnection, Intersection neighbor, BalancingInsOuts neighborConnection)
    {
      this.myConnection = myConnection;
      this.neighbor = neighbor;
      this.neighborConnection = neighborConnection;
    }

    public MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Intersection, BalancingInsOuts>> GetConnectionAsKVP()
    {
      return new MerlinKeyValuePair<BalancingInsOuts, MerlinKeyValuePair<Intersection, BalancingInsOuts>>(myConnection, new MerlinKeyValuePair<Intersection, BalancingInsOuts>(neighbor, neighborConnection));
    }
  }
}
