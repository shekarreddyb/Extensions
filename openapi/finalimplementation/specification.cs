using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace YourAppNamespace.Data.Specifications
{
    public abstract class BaseSpecification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public Expression<Func<T, object>> OrderBy { get; private set; }
        public Expression<Func<T, object>> OrderByDescending { get; private set; }
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        public void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        public void ApplySorting(Expression<Func<T, object>> orderBy, bool descending = false)
        {
            if (descending)
                OrderByDescending = orderBy;
            else
                OrderBy = orderBy;
        }
    }
}