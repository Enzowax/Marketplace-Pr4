SET XACT_ABORT ON;

CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    Login NVARCHAR(100) COLLATE Latin1_General_100_CI_AS NOT NULL UNIQUE,
    PasswordHash NVARCHAR(512) NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'User' CHECK (Role IN ('User', 'Admin'))
);

CREATE TABLE Categories (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) COLLATE Latin1_General_100_CI_AS NOT NULL UNIQUE
);

CREATE TABLE Products (
    Id INT IDENTITY PRIMARY KEY,
    CategoryId INT NOT NULL REFERENCES Categories(Id),
    Name NVARCHAR(512) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ImagePath NVARCHAR(512) NOT NULL,
    PriceKopecks INT NOT NULL CHECK (PriceKopecks >= 0),
    Stock INT NOT NULL CHECK (Stock >= 0),
    PackSize INT CHECK (PackSize > 0),
    IsAvailable INT NOT NULL DEFAULT 1 CHECK (IsAvailable IN (0, 1))
);

CREATE TABLE PickupPoints (
    Id INT IDENTITY PRIMARY KEY,
    Address NVARCHAR(512) NOT NULL,
    IsActive INT NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

CREATE TABLE CartItems (
    UserId INT NOT NULL REFERENCES Users(Id),
    ProductId INT NOT NULL REFERENCES Products(Id),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE Favorites (
    UserId INT NOT NULL REFERENCES Users(Id),
    ProductId INT NOT NULL REFERENCES Products(Id),
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE Reviews (
    UserId INT NOT NULL REFERENCES Users(Id),
    ProductId INT NOT NULL REFERENCES Products(Id),
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX) NOT NULL,
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE Orders (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalKopecks INT NOT NULL CHECK (TotalKopecks >= 0),
    PickupPointId INT REFERENCES PickupPoints(Id),
    DeliveryAddress NVARCHAR(512),
    Status VARCHAR(20) NOT NULL DEFAULT 'Placed'
        CHECK (Status IN ('Placed', 'Processing', 'Shipped', 'Ready', 'Received', 'Cancelled')),
    CHECK (
        (PickupPointId IS NOT NULL AND DeliveryAddress IS NULL)
        OR (PickupPointId IS NULL AND LEN(TRIM(COALESCE(DeliveryAddress, N''))) > 0
            AND TotalKopecks > 60000)
    )
);

CREATE TABLE OrderItems (
    OrderId INT NOT NULL REFERENCES Orders(Id),
    ProductId INT NOT NULL REFERENCES Products(Id),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPriceKopecks INT NOT NULL CHECK (UnitPriceKopecks >= 0),
    PRIMARY KEY (OrderId, ProductId)
);
