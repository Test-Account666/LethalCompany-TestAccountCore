using System.Collections.Generic;
using System.Linq;

namespace TestAccountCore;

public static class LinqExtensions {
    public static bool None<TSource>(this IEnumerable<TSource> source) => !source.Any();
}