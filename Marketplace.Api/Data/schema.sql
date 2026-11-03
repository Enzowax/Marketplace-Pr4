PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY,
    Login TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'User' CHECK (Role IN ('User', 'Admin'))
);

CREATE TABLE IF NOT EXISTS Categories (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE
);

CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY,
    CategoryId INTEGER NOT NULL REFERENCES Categories(Id),
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    ImagePath TEXT NOT NULL,
    PriceKopecks INTEGER NOT NULL CHECK (PriceKopecks >= 0),
    Stock INTEGER NOT NULL CHECK (Stock >= 0),
    PackSize INTEGER CHECK (PackSize > 0),
    IsAvailable INTEGER NOT NULL DEFAULT 1 CHECK (IsAvailable IN (0, 1))
);

CREATE TABLE IF NOT EXISTS PickupPoints (
    Id INTEGER PRIMARY KEY,
    Address TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

CREATE TABLE IF NOT EXISTS CartItems (
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    ProductId INTEGER NOT NULL REFERENCES Products(Id),
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE IF NOT EXISTS Favorites (
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    ProductId INTEGER NOT NULL REFERENCES Products(Id),
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE IF NOT EXISTS Reviews (
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    ProductId INTEGER NOT NULL REFERENCES Products(Id),
    Rating INTEGER NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment TEXT NOT NULL,
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE IF NOT EXISTS Orders (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    CreatedAt TEXT NOT NULL,
    TotalKopecks INTEGER NOT NULL CHECK (TotalKopecks >= 0),
    PickupPointId INTEGER REFERENCES PickupPoints(Id),
    DeliveryAddress TEXT,
    Status TEXT NOT NULL DEFAULT 'Placed'
        CHECK (Status IN ('Placed', 'Processing', 'Shipped', 'Ready', 'Received', 'Cancelled')),
    CHECK (
        (PickupPointId IS NOT NULL AND DeliveryAddress IS NULL)
        OR (PickupPointId IS NULL AND length(trim(coalesce(DeliveryAddress, ''))) > 0
            AND TotalKopecks > 60000)
    )
);

CREATE TABLE IF NOT EXISTS OrderItems (
    OrderId INTEGER NOT NULL REFERENCES Orders(Id),
    ProductId INTEGER NOT NULL REFERENCES Products(Id),
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPriceKopecks INTEGER NOT NULL CHECK (UnitPriceKopecks >= 0),
    PRIMARY KEY (OrderId, ProductId)
);
