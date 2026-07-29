using System.Collections.Generic;

namespace Sukurini.Models;

public record StoreChange(
    IReadOnlyList<Screenshot> Inserted,
    IReadOnlySet<string> RemovedPaths,
    bool IsFullReload,
    IReadOnlyDictionary<string, string> Replacements)
{
    public static StoreChange Empty => new(
        System.Array.Empty<Screenshot>(),
        new HashSet<string>(),
        false,
        new Dictionary<string, string>());
}
