USE MarketplacePr4;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Посуда')
    INSERT Categories(Name) VALUES (N'Посуда');
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Канцелярия')
    INSERT Categories(Name) VALUES (N'Канцелярия');
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = N'Чай')
    INSERT Categories(Name) VALUES (N'Чай');

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = N'Кружка белая')
    INSERT Products(CategoryId, Name, Description, ImagePath, PriceKopecks, Stock)
    SELECT Id, N'Кружка белая', N'Белая керамическая кружка для горячих напитков.', N'mug.png', 59900, 20
    FROM Categories WHERE Name = N'Посуда';
IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = N'Блокнот')
    INSERT Products(CategoryId, Name, Description, ImagePath, PriceKopecks, Stock)
    SELECT Id, N'Блокнот', N'Блокнот с крафтовой обложкой для заметок.', N'notebook.png', 60000, 15
    FROM Categories WHERE Name = N'Канцелярия';
IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = N'Чёрный чай')
    INSERT Products(CategoryId, Name, Description, ImagePath, PriceKopecks, Stock, PackSize)
    SELECT Id, N'Чёрный чай', N'Упаковка чёрного чая, 20 пакетиков.', N'tea.png', 60100, 10, 20
    FROM Categories WHERE Name = N'Чай';

IF NOT EXISTS (SELECT 1 FROM PickupPoints WHERE Address = N'Учебный ПВЗ — ул. Центральная, 10')
    INSERT PickupPoints(Address) VALUES (N'Учебный ПВЗ — ул. Центральная, 10');
IF NOT EXISTS (SELECT 1 FROM PickupPoints WHERE Address = N'Учебный ПВЗ — ул. Парковая, 5')
    INSERT PickupPoints(Address) VALUES (N'Учебный ПВЗ — ул. Парковая, 5');

COMMIT;
