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
NewApplications AS (
    SELECT 
        fa.QuarterLabel,
        COUNT(DISTINCT fa.apprequestid) AS NewApps
    FROM FilteredApplications fa
    GROUP BY fa.QuarterLabel
),
AllApplications AS (
    SELECT 
        CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn)) AS QuarterLabel,
        COUNT(DISTINCT car.apprequestid) AS AllApps
    FROM CloudApplicationRequest car
    WHERE EXISTS (
        SELECT 1 
        FROM Components c
        WHERE c.apprequestid = car.apprequestid
          AND c.ApprovalStatusId > 30 AND c.ApprovalStatusId < 2284
    )
    GROUP BY CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn))
),
FilteredComponents AS (
    SELECT 
        c.id AS ComponentId,
        CONCAT(YEAR(c.CreatedOn), ' Q', DATEPART(QUARTER, c.CreatedOn)) AS QuarterLabel,
        c.ApprovalStatusId,
        c.apprequestid
    FROM Components c
    WHERE c.ApprovalStatusId > 30 AND c.ApprovalStatusId < 2284
),
NewComponents AS (
    SELECT 
        fc.QuarterLabel,
        COUNT(DISTINCT fc.ComponentId) AS NewComponents
    FROM FilteredComponents fc
    GROUP BY fc.QuarterLabel
),
AllComponents AS (
    SELECT 
        'All Time' AS QuarterLabel,
        COUNT(DISTINCT fc.ComponentId) AS AllComponents
    FROM FilteredComponents fc
)
SELECT 
    na.QuarterLabel,
    COALESCE(na.NewApps, 0) AS NewApps,
    COALESCE(aa.AllApps, 0) AS AllApps,
    COALESCE(nc.NewComponents, 0) AS NewComponents,
    COALESCE(ac.AllComponents, 0) AS AllComponents
FROM NewApplications na
LEFT JOIN AllApplications aa ON na.QuarterLabel = aa.QuarterLabel
LEFT JOIN NewComponents nc ON na.QuarterLabel = nc.QuarterLabel
CROSS JOIN AllComponents ac
ORDER BY na.QuarterLabel;