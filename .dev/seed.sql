USE CarRentalDB;
GO

BEGIN
    DECLARE @Today DATE = CONVERT(DATE, SYSDATETIME());
    DECLARE @Users TABLE (
        UserId BIGINT NOT NULL,
        Email NVARCHAR(100) NOT NULL
    );
    DECLARE @Cars TABLE (
        CarId INT NOT NULL,
        Make NVARCHAR(100) NOT NULL,
        Model NVARCHAR(100) NOT NULL,
        [Year] INT NOT NULL,
        PriceInUsd DECIMAL(18, 2) NOT NULL,
        Status INT NOT NULL
    );

    DELETE FROM dbo.Bookings;
    DELETE FROM dbo.Users;
    DELETE FROM dbo.Cars;

    INSERT INTO dbo.Users (FirstName, LastName, Email, RegistrationDate)
    OUTPUT inserted.Id, inserted.Email INTO @Users (UserId, Email)
    VALUES
    (N'Anna',  N'Berger',  N'anna.berger@leiwandcars.test',  DATEADD(DAY, -240, CAST(@Today AS DATETIME2(0)))),
    (N'Lukas', N'Huber',   N'lukas.huber@leiwandcars.test',  DATEADD(DAY, -210, CAST(@Today AS DATETIME2(0)))),
    (N'Sofia', N'Novak',   N'sofia.novak@leiwandcars.test',  DATEADD(DAY, -180, CAST(@Today AS DATETIME2(0)))),
    (N'Noah',  N'Gruber',  N'noah.gruber@leiwandcars.test',  DATEADD(DAY, -150, CAST(@Today AS DATETIME2(0)))),
    (N'Emma',  N'Bauer',   N'emma.bauer@leiwandcars.test',   DATEADD(DAY, -120, CAST(@Today AS DATETIME2(0)))),
    (N'Leon',  N'Wagner',  N'leon.wagner@leiwandcars.test',  DATEADD(DAY, -90,  CAST(@Today AS DATETIME2(0)))),
    (N'Mia',   N'Steiner', N'mia.steiner@leiwandcars.test',  DATEADD(DAY, -60,  CAST(@Today AS DATETIME2(0)))),
    (N'Paul',  N'Hofer',   N'paul.hofer@leiwandcars.test',   DATEADD(DAY, -30,  CAST(@Today AS DATETIME2(0))));

    INSERT INTO dbo.Cars (Make, Model, Year, PriceInUsd, Status)
    OUTPUT inserted.Id, inserted.Make, inserted.Model, inserted.Year, inserted.PriceInUsd, inserted.Status
      INTO @Cars (CarId, Make, Model, [Year], PriceInUsd, Status)
    VALUES
    (N'Toyota',         N'Camry',            2022,  55.00, 0),  -- Available
    (N'Honda',          N'Civic',            2023,  49.99, 0),  -- Available
    (N'Ford',           N'Mustang',          2021,  89.99, 1),  -- Rented
    (N'BMW',            N'3 Series',         2023, 120.00, 0),  -- Available
    (N'Tesla',          N'Model 3',          2024, 149.99, 0),  -- Available
    (N'Chevrolet',      N'Silverado',        2022,  95.00, 2),  -- Maintenance
    (N'Audi',           N'A4',               2023, 110.00, 0),  -- Available
    (N'Mercedes-Benz',  N'C-Class',          2022, 130.00, 1),  -- Rented
    (N'Hyundai',        N'Sonata',           2023,  45.00, 0),  -- Available
    (N'Volkswagen',     N'Golf',             2022,  52.00, 0),  -- Available
    (N'Kia',            N'Optima',           2021,  43.50, 0),  -- Available
    (N'Nissan',         N'Altima',           2022,  47.25, 0),  -- Available
    (N'Mazda',          N'Mazda6',           2021,  50.00, 1),  -- Rented
    (N'Subaru',         N'Legacy',           2023,  57.00, 0),  -- Available
    (N'Lexus',          N'ES 350',           2022, 138.00, 2),  -- Maintenance
    (N'Volvo',          N'S60',              2023, 126.00, 0),  -- Available
    (N'Jaguar',         N'XE',               2021, 142.00, 1),  -- Rented
    (N'Alfa Romeo',     N'Giulia',           2022, 134.99, 0),  -- Available
    (N'Peugeot',        N'508',              2023,  61.00, 0),  -- Available
    (N'Renault',        N'Talisman',         2021,  54.00, 2),  -- Maintenance
    (N'Skoda',          N'Octavia',          2023,  53.00, 0),  -- Available
    (N'SEAT',           N'Leon',             2022,  51.00, 0),  -- Available
    (N'Opel',           N'Insignia',         2021,  49.00, 1),  -- Rented
    (N'Fiat',           N'Tipo',             2023,  41.00, 0),  -- Available
    (N'Mini',           N'Cooper',           2022,  78.00, 0),  -- Available
    (N'Dacia',          N'Jogger',           2023,  39.99, 0),  -- Available
    (N'Mitsubishi',     N'Lancer',           2021,  46.00, 2),  -- Maintenance
    (N'Jeep',           N'Cherokee',         2022,  96.50, 1),  -- Rented
    (N'Land Rover',     N'Discovery Sport',  2023, 168.00, 0),  -- Available
    (N'Porsche',        N'Macan',            2022, 220.00, 0),  -- Available
    (N'Infiniti',       N'Q50',              2021, 112.00, 0),  -- Available
    (N'Acura',          N'TLX',              2023, 118.00, 1),  -- Rented
    (N'Genesis',        N'G70',              2022, 125.00, 0),  -- Available
    (N'Chrysler',       N'300',              2021,  83.00, 2),  -- Maintenance
    (N'Dodge',          N'Charger',          2023,  92.00, 0),  -- Available
    (N'Cadillac',       N'CT5',              2022, 141.00, 1),  -- Rented
    (N'Buick',          N'Regal',            2021,  68.00, 0),  -- Available
    (N'Lincoln',        N'Corsair',          2023, 133.00, 0),  -- Available
    (N'Suzuki',         N'Swace',            2022,  44.00, 0),  -- Available
    (N'Citroen',        N'C5 X',             2023,  58.00, 1),  -- Rented
    (N'DS',             N'DS 7',             2022, 101.00, 0),  -- Available
    (N'Polestar',       N'Polestar 2',       2024, 159.00, 0),  -- Available
    (N'Cupra',          N'Formentor',        2023,  73.00, 2),  -- Maintenance
    (N'BYD',            N'Seal',             2024,  88.00, 0),  -- Available
    (N'Smart',          N'#1',               2023,  66.00, 0),  -- Available
    (N'Rivian',         N'R1S',              2024, 210.00, 1),  -- Rented
    (N'Lucid',          N'Air',              2024, 235.00, 0),  -- Available
    (N'GMC',            N'Acadia',           2022,  97.00, 0),  -- Available
    (N'Ram',            N'1500',             2023, 108.00, 2),  -- Maintenance
    (N'Lynk & Co',      N'01',               2022,  74.00, 1);  -- Rented

    ;WITH RankedUsers AS (
        SELECT
            UserId,
            ROW_NUMBER() OVER (ORDER BY UserId) AS UserRank
        FROM @Users
    ),
    RankedRentedCars AS (
        SELECT
            CarId,
            PriceInUsd,
            ROW_NUMBER() OVER (ORDER BY CarId) AS CarRank
        FROM @Cars
        WHERE Status = 1
    )
    INSERT INTO dbo.Bookings (CarId, UserId, BookingDate, PickupDate, DropoffDate, TotalCostInUsd, Status)
    SELECT
        car.CarId,
        usr.UserId,
        DATEADD(DAY, -2, CAST(@Today AS DATETIME2(0))) AS BookingDate,
        DATEADD(HOUR, 9, CAST(@Today AS DATETIME2(0))) AS PickupDate,
        DATEADD(HOUR, 9, CAST(DATEADD(DAY, 2, @Today) AS DATETIME2(0))) AS DropoffDate,
        CAST(car.PriceInUsd * 2 AS DECIMAL(18, 2)) AS TotalCostInUsd,
        CASE WHEN car.CarRank % 4 = 0 THEN 3 ELSE 2 END AS Status -- PickedUp / ReturnLate
    FROM RankedRentedCars car
    JOIN RankedUsers usr
      ON usr.UserRank = ((car.CarRank - 1) % (SELECT COUNT(*) FROM @Users)) + 1;

    ;WITH RankedUsers AS (
        SELECT
            UserId,
            ROW_NUMBER() OVER (ORDER BY UserId) AS UserRank
        FROM @Users
    ),
    RankedHistoricalCars AS (
        SELECT
            CarId,
            PriceInUsd,
            ROW_NUMBER() OVER (ORDER BY CarId) AS CarRank
        FROM @Cars
        WHERE Status IN (0, 2)
    )
    INSERT INTO dbo.Bookings (CarId, UserId, BookingDate, PickupDate, DropoffDate, TotalCostInUsd, Status)
    SELECT
        car.CarId,
        usr.UserId,
        DATEADD(DAY, -(20 + car.CarRank), CAST(@Today AS DATETIME2(0))) AS BookingDate,
        DATEADD(HOUR, 10, CAST(DATEADD(DAY, -(14 + car.CarRank), @Today) AS DATETIME2(0))) AS PickupDate,
        DATEADD(HOUR, 10, CAST(DATEADD(DAY, -(11 + car.CarRank), @Today) AS DATETIME2(0))) AS DropoffDate,
        CAST(car.PriceInUsd * 3 AS DECIMAL(18, 2)) AS TotalCostInUsd,
        CASE WHEN car.CarRank % 2 = 0 THEN 4 ELSE 1 END AS Status -- Completed / Canceled
    FROM RankedHistoricalCars car
    JOIN RankedUsers usr
      ON usr.UserRank = ((car.CarRank + 2) % (SELECT COUNT(*) FROM @Users)) + 1
    WHERE car.CarRank <= 8;

    PRINT 'Cars, users, and bookings seed data inserted successfully.';
END
GO
