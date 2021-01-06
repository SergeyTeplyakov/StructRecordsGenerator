using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static StructGenerators.CollectionsBehavior;
//#nullable disable
namespace StructGenerators
{
    internal static class ToStringGenerationHelper
    {
        public static void PrintCollection<T>(this StringBuilder sb, IEnumerable<T> source, string name, CollectionsBehavior behavior, int limit)
        {
            int? count = null;
            if (source is ICollection collection)
            {
                count = collection.Count;
            }
            
            if (behavior is PrintTypeNameAndCount || source is null)
            {
                // Need to close the paren if the count is available.
                if (count != null)
                {
                    sb.Append($"{name} (Count: {count}) = {source}");
                }
                else
                {
                    sb.Append($"{name} = {source}");
                }
            }
            else // behavior is PrintContent and source is not null
            {
                string suffix = (count, limit) switch
                {
                    (null, _) => $"(Limit: {limit})",
                    (_, _) when count <= limit => $"(Count: {count})",
                    _ => $"(Count: {count}, Limit: {limit})",
                };

                var content = string.Join(", ", source.Take(limit).Select(e => e?.ToString()));
                sb.Append($"{name} {suffix} = [{content}]");
            }
        }
    }
}
