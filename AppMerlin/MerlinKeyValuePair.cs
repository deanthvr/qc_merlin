
namespace AppMerlin
{
  /// <summary>
  /// Our own version of the KeyValuePair class that doesn't contain read-only members to enable XML serialization.
  /// </summary>
  /// <typeparam name="TKey">Key</typeparam>
  /// <typeparam name="TValue">Value</typeparam>
  public class MerlinKeyValuePair<TKey,TValue>
  {
    public TKey Key
    {get; set;}

    public TValue Value
    {get; set;}

    public MerlinKeyValuePair()
    {
      Key = default(TKey);
      Value = default(TValue);
    }

    public MerlinKeyValuePair(TKey key, TValue val)
    {
      Key = key;
      Value = val;
    }

  }
}
