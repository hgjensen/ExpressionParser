using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionParser.Extensions;

public static class EnumerableExtensions {
  public static TOutput[] ToArray<TInput, TOutput>(this IEnumerable<TInput> source, Func<TInput, TOutput> select)
    => source?.Select(select).ToArray() ?? new TOutput[] { };
}