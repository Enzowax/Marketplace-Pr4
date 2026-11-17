:On Error exit
USE tempdb;
GO
BEGIN TRANSACTION;
:r Marketplace.Api/Data/schema.sql

INSERT Users (Login, PasswordHash) VALUES (N'buyer', N'test-hash');
INSERT Categories (Name) VALUES (N'Test');
INSERT Products (CategoryId, Name, Description, ImagePath, PriceKopecks, Stock)
VALUES (1, N'Item', N'Description', N'item.png', 60100, 2);
INSERT Orders (UserId, TotalKopecks, DeliveryAddress)
VALUES (1, 60100, N'Test address');
INSERT OrderItems (OrderId, ProductId, Quantity, UnitPriceKopecks)
VALUES (1, 1, 1, 60100);
UPDATE Products SET PriceKopecks = 70000 WHERE Id = 1;

IF NOT EXISTS (SELECT 1 FROM OrderItems WHERE OrderId = 1 AND UnitPriceKopecks = 60100)
    THROW 50000, 'Order price changed.', 1;

ROLLBACK;
PRINT 'SQL Server schema check passed.';
