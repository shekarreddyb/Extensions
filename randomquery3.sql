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
        CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn)) AS QuarterLabel,
        COUNT(DISTINCT car.apprequestid) AS NewApps
    FROM CloudApplicationRequest car
    WHERE EXISTS (
        SELECT 1 
        FROM Components c
        WHERE c.apprequestid = car.apprequestid
          AND c.ApprovalStatusId > 30 AND c.ApprovalStatusId < 2284
    )
    GROUP BY CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn))
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
    AND DATEPART(YEAR, car.CreatedOn) * 10 + DATEPART(QUARTER, car.CreatedOn) <=
        (SELECT MAX(DATEPART(YEAR, CreatedOn) * 10 + DATEPART(QUARTER, CreatedOn)) FROM CloudApplicationRequest)
    GROUP BY CONCAT(YEAR(car.CreatedOn), ' Q', DATEPART(QUARTER, car.CreatedOn))
),
FilteredComponents AS (
    SELECT 
        c.id AS ComponentId,
        c.CreatedOn,
        CONCAT(YEAR(c.CreatedOn), ' Q', DATEPART(QUARTER, c.CreatedOn)) AS QuarterLabel
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
        CONCAT(YEAR(fc.CreatedOn), ' Q', DATEPART(QUARTER, fc.CreatedOn)) AS QuarterLabel,
        COUNT(DISTINCT fc.ComponentId) AS AllComponents
    FROM FilteredComponents fc
    WHERE DATEPART(YEAR, fc.CreatedOn) * 10 + DATEPART(QUARTER, fc.CreatedOn) <=
          (SELECT MAX(DATEPART(YEAR, CreatedOn) * 10 + DATEPART(QUARTER, CreatedOn)) FROM Components)
    GROUP BY CONCAT(YEAR(fc.CreatedOn), ' Q', DATEPART(QUARTER, fc.CreatedOn))
)
SELECT 
    na.QuarterLabel,
    COALESCE(na.NewApps, 0) AS NewApps,
    COALESCE(aa.AllApps, 0) AS AllApps,
    COALESCE(nc.NewComponents, 0) AS NewComponents,
    COALESCE(ac.AllComponents, 0) AS AllComponents
FROM NewApplications na
FULL OUTER JOIN AllApplications aa ON na.QuarterLabel = aa.QuarterLabel
FULL OUTER JOIN NewComponents nc ON na.QuarterLabel = nc.QuarterLabel
FULL OUTER JOIN AllComponents ac ON na.QuarterLabel = ac.QuarterLabel
ORDER BY na.QuarterLabel;