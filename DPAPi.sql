CREATE TABLE DataProtectionKeys
(
    Id int IDENTITY(1,1) PRIMARY KEY,
    FriendlyName nvarchar(max),
    Xml nvarchar(max) NOT NULL
);
