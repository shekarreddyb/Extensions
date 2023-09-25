CREATE TABLE DataProtectionKeys
(
    Id int IDENTITY(1,1) PRIMARY KEY,
    FriendlyName nvarchar(max),
    Xml nvarchar(max) NOT NULL
);


DECLARE @TableName NVARCHAR(128) = 'YourTableName'
DECLARE @ColumnName NVARCHAR(128) = 'YourColumnName'

SELECT 
    fk.name AS ForeignKey,
    OBJECT_NAME(fk.parent_object_id) AS ReferencingTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ReferencingColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM 
    sys.foreign_keys AS fk
INNER JOIN 
    sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
WHERE 
    OBJECT_NAME(fk.referenced_object_id) = @TableName 
    AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = @ColumnName
ORDER BY 
    ReferencingTable, ForeignKey
