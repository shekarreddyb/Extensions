

WITH ParsedLoadBalancer AS (
    SELECT 
        v.url,
        a.appid, -- Pulling appid from the joined applications table
        CASE 
            WHEN CHARINDEX('svdca', lb.loadbalancer) > 0 THEN 
                CASE WHEN CHARINDEX('svdca-a', lb.loadbalancer) > 0 THEN 'active' ELSE 'passive' END
        END AS svdc_status,
        CASE 
            WHEN CHARINDEX('slsp', lb.loadbalancer) > 0 THEN 
                CASE WHEN CHARINDEX('slsp-a', lb.loadbalancer) > 0 THEN 'active' ELSE 'passive' END
        END AS sls_status,
        CASE 
            WHEN CHARINDEX('oxdca', lb.loadbalancer) > 0 THEN 
                CASE WHEN CHARINDEX('oxdca-a', lb.loadbalancer) > 0 THEN 'active' ELSE 'passive' END
        END AS oxdc_status,
        CASE 
            WHEN CHARINDEX('tmpe', lb.loadbalancer) > 0 THEN 
                CASE WHEN CHARINDEX('tmpe-a', lb.loadbalancer) > 0 THEN 'active' ELSE 'passive' END
        END AS tmpe_status,
        CASE 
            WHEN CHARINDEX('temr', lb.loadbalancer) > 0 THEN 
                CASE WHEN CHARINDEX('temr-a', lb.loadbalancer) > 0 THEN 'active' ELSE 'passive' END
        END AS temr_status
    FROM 
        your_loadbalancer_table lb
    JOIN 
        vanityurls v ON lb.guid = v.guid
    JOIN 
        applications a ON v.appid = a.appid -- Join with applications table
)
SELECT
    url, appid, 'svdc' AS key, svdc_status AS status
FROM 
    ParsedLoadBalancer
WHERE 
    svdc_status IS NOT NULL
UNION ALL
SELECT
    url, appid, 'sls' AS key, sls_status AS status
FROM 
    ParsedLoadBalancer
WHERE 
    sls_status IS NOT NULL
UNION ALL
SELECT
    url, appid, 'oxdc' AS key, oxdc_status AS status
FROM 
    ParsedLoadBalancer
WHERE 
    oxdc_status IS NOT NULL
UNION ALL
SELECT
    url, appid, 'tmpe' AS key, tmpe_status AS status
FROM 
    ParsedLoadBalancer
WHERE 
    tmpe_status IS NOT NULL
UNION ALL
SELECT
    url, appid, 'temr' AS key, temr_status AS status
FROM 
    ParsedLoadBalancer
WHERE 
    temr_status IS NOT NULL;


 
             
