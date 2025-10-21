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
    RoleName NVARCHAR(50) NOT NULL CHECK (RoleName IN ('Customer', 'Dealer'))
);

INSERT INTO Roles (RoleName) VALUES
(N'Customer'), (N'Dealer');

-- 3. Users (system accounts for authentication)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL DEFAULT 1,
    Username NVARCHAR(50) NOT NULL,
	Email NVARCHAR(100) UNIQUE NOT NULL,
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
    Phone NVARCHAR(20) UNIQUE NOT NULL,
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
		'CANCELLED',
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

-- ====================================
-- 🌱 TEST DATA SEED FOR PRN222ASM2
-- ====================================

-- 1️⃣ Vehicle Categories
INSERT INTO Vehicle_Category (Name) VALUES
(N'Sedan'),
(N'SUV'),
(N'Pickup'),
(N'Sports Car');

-- 2️⃣ Users
INSERT INTO Users (RoleId, Username, Email, PasswordHash, IsDeleted) VALUES
(2, N'dealer1', N'dealer1@dealers.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 0),
(2, N'dealer2', N'dealer2@dealers.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 0),
(1, N'user1', N'user1@gmail.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 0),
(1, N'user2', N'user2@gmail.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 0);

-- 3️⃣ Dealers
INSERT INTO Dealer (UserId, DealerName, Address, Quantity) VALUES
(1, N'Speed Motors', N'123 Nguyen Trai, District 5, HCMC', 20),
(2, N'Auto Galaxy', N'45 Le Loi, Hoan Kiem, Hanoi', 15);

-- 4️⃣ Customers
INSERT INTO Customer (UserId, Name, Phone, Address) VALUES
(3, N'Nguyen Van A', N'0901234567', N'12 Tran Hung Dao, HCMC'),
(4, N'Tran Thi B', N'0917654321', N'56 Pham Van Dong, Hanoi');

-- 5️⃣ Vehicles
INSERT INTO Vehicle (CategoryId, Color, Price, ManufactureDate, Model, Version, Image, IsDeleted) VALUES
(1, N'Red', 55000, '2023-05-12', N'Toyota Camry', N'2.5Q', N'/uploads/vehicles/01242521-60f3-436d-83b6-f6eea4a9bc1c.jpg', 0),
(2, N'Black', 72000, '2024-01-25', N'Hyundai Tucson', N'2024 Edition', N'/uploads/vehicles/503b5b8d-5c8b-4b41-a126-c72483215994.jpg', 0),
(3, N'White', 68000, '2023-07-15', N'Ford Ranger', N'Wildtrak', N'/uploads/vehicles/2b9f4973-846b-4d54-8482-c746858ba7f9.jpg', 0),
(4, N'Blue', 120000, '2024-03-01', N'BMW Z4', N'M40i', N'/uploads/vehicles/13ec6a3c-b940-4631-8b22-6d29320376b4.jpg', 0),
(1, N'Silver', 43000, '2022-11-18', N'Honda Civic', N'RS Turbo', N'/uploads/vehicles/d36bd81a-1089-4fe0-9c56-a103323f08d9.jpg', 0);

-- 6️⃣ Vehicle_Dealer (inventory by dealer)
INSERT INTO Vehicle_Dealer (VehicleId, DealerId, Quantity) VALUES
(1, 1, 5),
(2, 1, 3),
(3, 1, 4),
(4, 1, 2),
(5, 1, 6);

-- 7️⃣ Appointments (customers viewing/test-driving vehicles)
INSERT INTO Appointment (CustomerId, VehicleId, AppointmentDate, Status) VALUES
(1, 1, DATEADD(DAY, -3, GETDATE()), 'COMPLETED'),
(1, 5, DATEADD(DAY, 2, GETDATE()), 'PENDING'),
(2, 2, DATEADD(DAY, -1, GETDATE()), 'APPROVE'),
(2, 4, DATEADD(DAY, 5, GETDATE()), 'PENDING');

-- 8️⃣ Orders
INSERT INTO Orders (CustomerId, DealerId, OrderDate, TotalAmount, Status) VALUES
(1, 1, DATEADD(DAY, -7, GETDATE()), 55000, 'DONE'),
(2, 2, DATEADD(DAY, -1, GETDATE()), 72000, 'PENDING'),
(1, 1, DATEADD(DAY, -2, GETDATE()), 43000, 'PAID');

-- 9️⃣ Order_Vehicle
INSERT INTO Order_Vehicle (OrderId, VehicleId, Quantity, UnitPrice) VALUES
(1, 1, 1, 55000),
(2, 2, 1, 72000),
(3, 5, 1, 43000);
