using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace YourAppNamespace.Data.Specifications
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        Expression<Func<T, object>> OrderBy { get; }
        Expression<Func<T, object>> OrderByDescending { get; }
        int? Page { get; }
        int? PageSize { get; }
        bool IsPagingEnabled { get; }
    }
}