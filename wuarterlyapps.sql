WITH QuarterlyApps AS (
    SELECT 
        DATEPART(YEAR, CreatedDate) AS Year,
        DATEPART(QUARTER, CreatedDate) AS Quarter,
        CONCAT(DATEPART(YEAR, CreatedDate), ' Q', DATEPART(QUARTER, CreatedDate)) AS QuarterLabel,
        apprequestid,
        status
    FROM Cloudapplicationrequest
),
NewApps AS (
    SELECT 
        qa.QuarterLabel,
        COUNT(DISTINCT qa.apprequestid) AS NewAppsCount
    FROM QuarterlyApps qa
    LEFT JOIN QuarterlyApps prev ON qa.apprequestid = prev.apprequestid 
        AND (prev.Year < qa.Year OR (prev.Year = qa.Year AND prev.Quarter < qa.Quarter))
    WHERE (qa.status LIKE 'Dev Approved%' OR qa.status LIKE 'Test Approved%' OR qa.status LIKE 'Prod Approved%')
      AND (prev.apprequestid IS NULL OR prev.status LIKE 'Draft%')
    GROUP BY qa.QuarterLabel
),
AllApps AS (
    SELECT 
        qa.QuarterLabel,
        COUNT(DISTINCT qa.apprequestid) AS AllAppsCount
    FROM QuarterlyApps qa
    WHERE qa.status LIKE 'Dev Approved%' OR qa.status LIKE 'Test Approved%' OR qa.status LIKE 'Prod Approved%'
    GROUP BY qa.QuarterLabel
),
NewComponents AS (
    SELECT 
        qa.QuarterLabel,
        COUNT(DISTINCT c.id) AS NewComponentsCount
    FROM QuarterlyComponents c
    INNER JOIN QuarterlyApps qa ON c.apprequestid = qa.apprequestid
    WHERE c.status LIKE 'Approved%' AND qa.status LIKE 'Dev Approved%'
    GROUP BY qa.QuarterLabel
),
AllComponents AS (
    SELECT 
        qa.QuarterLabel,
        COUNT(DISTINCT c.id) AS AllComponentsCount
    FROM QuarterlyComponents c
    INNER JOIN QuarterlyApps qa ON c.apprequestid = qa.apprequestid
    WHERE c.status LIKE 'Approved%' OR c.status LIKE 'Completed%'
    GROUP BY qa.QuarterLabel
)
SELECT 
    na.QuarterLabel,
    na.NewAppsCount,
    aa.AllAppsCount,
    nc.NewComponentsCount,
    ac.AllComponentsCount
FROM NewApps na
LEFT JOIN AllApps aa ON na.QuarterLabel = aa.QuarterLabel
LEFT JOIN NewComponents nc ON na.QuarterLabel = nc.QuarterLabel
LEFT JOIN AllComponents ac ON na.QuarterLabel = ac.QuarterLabel
ORDER BY na.QuarterLabel;