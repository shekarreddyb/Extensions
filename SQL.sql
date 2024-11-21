-- Sample temporary table with ID and Script
CREATE TABLE #TempTable (
    id INT,
    script NVARCHAR(MAX)
);

-- Example data
INSERT INTO #TempTable (id, script)
VALUES 
(1, 'SELECT 1 AS Result'),
(2, 'SELECT 2 AS Result');

-- Temporary table to store IDs that successfully executed
CREATE TABLE #ExecutedIDs (id INT);

-- Declare variables
DECLARE @id INT;
DECLARE @script NVARCHAR(MAX);
DECLARE @sql NVARCHAR(MAX);

-- Cursor to iterate over the #TempTable rows
DECLARE cur CURSOR FOR 
SELECT id, script
FROM #TempTable;

OPEN cur;

FETCH NEXT FROM cur INTO @id, @script;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        -- Execute the dynamic SQL
        SET @sql = @script;
        EXEC(@sql);

        -- If successful, insert the ID into #ExecutedIDs
        INSERT INTO #ExecutedIDs (id)
        VALUES (@id);
    END TRY
    BEGIN CATCH
        -- Handle errors if needed
        PRINT 'Error executing script with ID: ' + CAST(@id AS NVARCHAR(10));
    END CATCH;

    FETCH NEXT FROM cur INTO @id, @script;
END;

CLOSE cur;
DEALLOCATE cur;

-- Perform the UPDATE query where ID is in the executed list
UPDATE YourTargetTable
SET YourColumn = 'Updated'
WHERE ID IN (SELECT id FROM #ExecutedIDs);

-- Clean up
DROP TABLE #TempTable;
DROP TABLE #ExecutedIDs;