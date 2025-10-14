USE [master];
GO

IF DB_ID('PRN222ASM2') IS NOT NULL
    DROP DATABASE PRN222ASM2;
GO

CREATE DATABASE [PRN222ASM2];
GO

USE [PRN222ASM2];
GO

-- 1. Vehicle_Category
CREATE TABLE Vehicle_Category (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

-- 2. Roles
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL CHECK (RoleName IN ('User', 'Dealer'))
);

-- 3. Users (system accounts for authentication)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL DEFAULT 1,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- 4. Dealer (business entity linked to Users)
CREATE TABLE Dealer (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    DealerName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0 CHECK (Quantity >= 0), -- merged inventory quantity
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 5. Customer (end-users who buy vehicles)
CREATE TABLE Customer (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 6. Vehicle
CREATE TABLE Vehicle (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    Color NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    ManufactureDate DATE NOT NULL,
    Model NVARCHAR(100) NOT NULL,
    Version NVARCHAR(50),
    Image NVARCHAR(255),
    IsDeleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (CategoryId) REFERENCES Vehicle_Category(Id)
);

-- 7. Vehicle_Dealer (N–N between Vehicle and Dealer with Quantity)
CREATE TABLE Vehicle_Dealer (
    VehicleId INT NOT NULL,
    DealerId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity >= 0),
    PRIMARY KEY (VehicleId, DealerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicle(Id),
    FOREIGN KEY (DealerId) REFERENCES Dealer(Id)
);

-- 8. Appointment (customer booking for test drive or viewing)
CREATE TABLE Appointment (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    VehicleId INT NOT NULL,
    AppointmentDate DATETIME NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    FOREIGN KEY (CustomerId) REFERENCES Customer(Id),
    FOREIGN KEY (VehicleId) REFERENCES Vehicle(Id),
    CONSTRAINT CK_Appointment_Status CHECK (Status IN (
        'PENDING',
        'APPROVE',
        'RUNNING',
        'COMPLETED',
        'EXPIRED'
    ))
);

-- 9. Orders (sales orders created by a dealer for a customer)
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    DealerId INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    FOREIGN KEY (CustomerId) REFERENCES Customer(Id),
    FOREIGN KEY (DealerId) REFERENCES Dealer(Id),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (
        'PENDING',
        'CANCELLED',
        'APPROVE',
        'PAID',
        'DELIVERING',
        'DONE'
    ))
);

-- 10. Order_Vehicle (N–N between Orders and Vehicles)
CREATE TABLE Order_Vehicle (
    OrderId INT NOT NULL,
    VehicleId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (OrderId, VehicleId),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (VehicleId) REFERENCES Vehicle(Id)
);
GO
