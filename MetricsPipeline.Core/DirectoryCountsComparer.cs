using System.Collections.Generic;
using System.Linq;

namespace MetricsPipeline.Core;

/// <summary>
/// Compares two maps of directory counts and yields entries that differ.
/// </summary>
public sealed class DirectoryCountsComparer
{
    /// <summary>
    /// Joins the two maps and streams entries where the counts do not match.
    /// </summary>
    /// <param name="left">The first map.</param>
    /// <param name="right">The second map.</param>
    /// <returns>Sequence of differences.</returns>
    public IEnumerable<CountsDifference> Compare(
        IReadOnlyDictionary<string, DirectoryCounts> left,
        IReadOnlyDictionary<string, DirectoryCounts> right)
    {
        var allKeys = left.Keys.Union(right.Keys);
        foreach (var key in allKeys)
        {
            left.TryGetValue(key, out var l);
            right.TryGetValue(key, out var r);
            if (!Equals(l, r))
            {
                yield return new CountsDifference(key, l, r);
            }
        }
    }
}

/// <summary>
/// Represents a difference in directory counts between two maps.
/// </summary>
/// <param name="Path">The directory path.</param>
/// <param name="Left">Counts from the first map or null if missing.</param>
/// <param name="Right">Counts from the second map or null if missing.</param>
public record CountsDifference(string Path, DirectoryCounts? Left, DirectoryCounts? Right);
