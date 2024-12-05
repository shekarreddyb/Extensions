WITH FilteredApplications AS (
    SELECT 
        car.apprequestid,
        car.CreatedOn,
        CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn)) AS QuarterLabel
    FROM CloudApplicationRequest car
    WHERE EXISTS (
        SELECT 1 
        FROM Components c
        WHERE c.apprequestid = car.apprequestid
          AND c.ApprovalStatusId > 30 AND c.ApprovalStatusId < 2284
    )
),
FilteredComponents AS (
    SELECT 
        c.id AS ComponentId,
        c.CreatedOn,
        CONCAT(YEAR(c.CreatedOn), ' Q', DATEPART(QUARTER, c.CreatedOn)) AS QuarterLabel,
        c.apprequestid
    FROM Components c
    WHERE c.ApprovalStatusId > 30 AND c.ApprovalStatusId < 2284
),
Quarters AS (
    SELECT DISTINCT 
        CONCAT(YEAR(CreatedOn), ' Q', DATEPART(QUARTER, CreatedOn)) AS QuarterLabel,
        YEAR(CreatedOn) AS Year,
        DATEPART(QUARTER, CreatedOn) AS Quarter
    FROM CloudApplicationRequest
    UNION
    SELECT DISTINCT 
        CONCAT(YEAR(CreatedOn), ' Q', DATEPART(QUARTER, CreatedOn)) AS QuarterLabel,
        YEAR(CreatedOn) AS Year,
        DATEPART(QUARTER, CreatedOn) AS Quarter
    FROM Components
),
NewApplications AS (
    SELECT 
        q.QuarterLabel,
        COUNT(DISTINCT fa.apprequestid) AS NewApps
    FROM Quarters q
    LEFT JOIN FilteredApplications fa 
        ON CONCAT(YEAR(fa.CreatedOn), ' Q', DATEPART(QUARTER, fa.CreatedOn)) = q.QuarterLabel
    GROUP BY q.QuarterLabel
),
AllApplications AS (
    SELECT 
        q.QuarterLabel,
        COUNT(DISTINCT fa.apprequestid) AS AllApps
    FROM Quarters q
    LEFT JOIN FilteredApplications fa 
        ON fa.CreatedOn <= DATEADD(QUARTER, 1, DATEFROMPARTS(q.Year, (q.Quarter - 1) * 3 + 1, 1)) 
    GROUP BY q.QuarterLabel
),
NewComponents AS (
    SELECT 
        q.QuarterLabel,
        COUNT(DISTINCT fc.ComponentId) AS NewComponents
    FROM Quarters q
    LEFT JOIN FilteredComponents fc 
        ON CONCAT(YEAR(fc.CreatedOn), ' Q', DATEPART(QUARTER, fc.CreatedOn)) = q.QuarterLabel
    GROUP BY q.QuarterLabel
),
AllComponents AS (
    SELECT 
        q.QuarterLabel,
        COUNT(DISTINCT fc.ComponentId) AS AllComponents
    FROM Quarters q
    LEFT JOIN FilteredComponents fc 
        ON fc.CreatedOn <= DATEADD(QUARTER, 1, DATEFROMPARTS(q.Year, (q.Quarter - 1) * 3 + 1, 1)) 
    GROUP BY q.QuarterLabel
)
SELECT 
    q.QuarterLabel,
    COALESCE(na.NewApps, 0) AS NewApps,
    COALESCE(aa.AllApps, 0) AS AllApps,
    COALESCE(nc.NewComponents, 0) AS NewComponents,
    COALESCE(ac.AllComponents, 0) AS AllComponents
FROM Quarters q
LEFT JOIN NewApplications na ON q.QuarterLabel = na.QuarterLabel
LEFT JOIN AllApplications aa ON q.QuarterLabel = aa.QuarterLabel
LEFT JOIN NewComponents nc ON q.QuarterLabel = nc.QuarterLabel
LEFT JOIN AllComponents ac ON q.QuarterLabel = ac.QuarterLabel
ORDER BY q.Year, q.Quarter;