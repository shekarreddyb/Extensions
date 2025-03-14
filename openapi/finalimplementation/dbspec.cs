using System;
using System.Linq.Expressions;

namespace YourAppNamespace.Data.Specifications
{
    public class DatabaseSpecification : BaseSpecification<Database>
    {
        public DatabaseSpecification(string environment, string name, int? page = null, int? pageSize = null)
            : base(d => 
                (string.IsNullOrEmpty(environment) || d.Environment == environment) &&
                (string.IsNullOrEmpty(name) || d.Name.Contains(name)))
        {
            ApplySorting(d => d.CreatedTime, descending: true);
            
            if (page.HasValue && pageSize.HasValue)
            {
                ApplyPaging((page.Value - 1) * pageSize.Value, pageSize.Value);
            }
        }
    }
}