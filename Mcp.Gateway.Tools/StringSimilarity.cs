namespace Mcp.Gateway.Tools;

/// <summary>
/// Helper class for calculating string similarity using Levenshtein distance.
/// Used for suggesting similar tool names when a tool is not found.
/// </summary>
internal static class StringSimilarity
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// Lower value = more similar strings.
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>Edit distance between strings</returns>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;
        
        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Create matrix of distances
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (var i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;
        
        for (var j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        // Calculate distances
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                
                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // deletion
                        distance[i, j - 1] + 1),     // insertion
                    distance[i - 1, j - 1] + cost);  // substitution
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Finds the most similar strings from a collection based on Levenshtein distance.
    /// </summary>
    /// <param name="target">String to find matches for</param>
    /// <param name="candidates">Collection of candidate strings</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="maxDistance">Maximum edit distance to consider (default: 3)</param>
    /// <returns>List of similar strings, ordered by similarity</returns>
    public static List<string> FindSimilarStrings(
        string target,
        IEnumerable<string> candidates,
        int maxResults = 3,
        int maxDistance = 3)
    {
        return candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Distance = LevenshteinDistance(target.ToLowerInvariant(), candidate.ToLowerInvariant())
            })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Candidate) // Secondary sort for determinism
            .Take(maxResults)
            .Select(x => x.Candidate)
            .ToList();
    }
}
