var query = from ce in dbContext.ComponentEnvironments
            join e1 in dbContext.Environments on ce.EnvironmentId equals e1.EnvironmentId
            where ce.ComponentId == inputComponentId
            let initialLocation = (from env in dbContext.Environments
                                   where env.EnvironmentId == inputEnvironmentId
                                   select env.Location).FirstOrDefault()
            from dcp in dbContext.DatacenterPairs
            where dcp.F1 == initialLocation
            let remainingEnvironments = (from ce2 in dbContext.ComponentEnvironments
                                         join e2 in dbContext.Environments on ce2.EnvironmentId equals e2.EnvironmentId
                                         where ce2.ComponentId == inputComponentId && ce2.EnvironmentId != inputEnvironmentId
                                         select e2.Location)
            where remainingEnvironments.Contains(dcp.F2) && (dcp.F3 == null || remainingEnvironments.Contains(dcp.F3))
            select new
            {
                ce.ComponentEnvironmentId,
                ce.EnvironmentId,
                ce.ComponentId,
                DatacenterPair = dcp
            };

var result = query.ToList();