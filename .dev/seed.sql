USE CarRentalDB;
GO

BEGIN
    DECLARE @Today DATE = CONVERT(DATE, SYSDATETIME());

    -- Clear existing data and reseed identities
    IF OBJECT_ID('dbo.CarUnavailableDates', 'U') IS NOT NULL
        DELETE FROM dbo.CarUnavailableDates;
    IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL
        DELETE FROM dbo.Bookings;
    IF OBJECT_ID('dbo.Cars', 'U') IS NOT NULL
        DELETE FROM dbo.Cars;
    IF OBJECT_ID('dbo.CarUnavailableDates', 'U') IS NOT NULL
        DBCC CHECKIDENT ('dbo.CarUnavailableDates', RESEED, 0);
    IF OBJECT_ID('dbo.Cars', 'U') IS NOT NULL
        DBCC CHECKIDENT ('dbo.Cars', RESEED, 0);
    IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL
        DBCC CHECKIDENT ('dbo.Bookings', RESEED, 0);

    -- Seed cars: keep all cars Available; availability is now controlled by bookings
    INSERT INTO dbo.Cars (Make, Model, Year, PriceInUsd, Status)
    VALUES
    (N'Toyota',         N'Camry',            2022,  55.00, 0),
    (N'Honda',          N'Civic',            2023,  49.99, 0),
    (N'Ford',           N'Mustang',          2021,  89.99, 0),
    (N'BMW',            N'3 Series',         2023, 120.00, 0),
    (N'Tesla',          N'Model 3',          2024, 149.99, 0),
    (N'Chevrolet',      N'Silverado',        2022,  95.00, 0),
    (N'Audi',           N'A4',               2023, 110.00, 0),
    (N'Mercedes-Benz',  N'C-Class',          2022, 130.00, 0),
    (N'Hyundai',        N'Sonata',           2023,  45.00, 0),
    (N'Volkswagen',     N'Golf',             2022,  52.00, 0),
    (N'Kia',            N'Optima',           2021,  43.50, 0),
    (N'Nissan',         N'Altima',           2022,  47.25, 0),
    (N'Mazda',          N'Mazda6',           2021,  50.00, 0),
    (N'Subaru',         N'Legacy',           2023,  57.00, 0),
    (N'Lexus',          N'ES 350',           2022, 138.00, 0),
    (N'Volvo',          N'S60',              2023, 126.00, 0),
    (N'Jaguar',         N'XE',               2021, 142.00, 0),
    (N'Alfa Romeo',     N'Giulia',           2022, 134.99, 0),
    (N'Peugeot',        N'508',              2023,  61.00, 0),
    (N'Renault',        N'Talisman',         2021,  54.00, 0),
    (N'Skoda',          N'Octavia',          2023,  53.00, 0),
    (N'SEAT',           N'Leon',             2022,  51.00, 0),
    (N'Opel',           N'Insignia',         2021,  49.00, 0),
    (N'Fiat',           N'Tipo',             2023,  41.00, 0),
    (N'Mini',           N'Cooper',           2022,  78.00, 0),
    (N'Dacia',          N'Jogger',           2023,  39.99, 0),
    (N'Mitsubishi',     N'Lancer',           2021,  46.00, 0),
    (N'Jeep',           N'Cherokee',         2022,  96.50, 0),
    (N'Land Rover',     N'Discovery Sport',  2023, 168.00, 0),
    (N'Porsche',        N'Macan',            2022, 220.00, 0),
    (N'Infiniti',       N'Q50',              2021, 112.00, 0),
    (N'Acura',          N'TLX',              2023, 118.00, 0),
    (N'Genesis',        N'G70',              2022, 125.00, 0),
    (N'Chrysler',       N'300',              2021,  83.00, 0),
    (N'Dodge',          N'Charger',          2023,  92.00, 0),
    (N'Cadillac',       N'CT5',              2022, 141.00, 0),
    (N'Buick',          N'Regal',            2021,  68.00, 0),
    (N'Lincoln',        N'Corsair',          2023, 133.00, 0),
    (N'Suzuki',         N'Swace',            2022,  44.00, 0),
    (N'Citroen',        N'C5 X',             2023,  58.00, 0),
    (N'DS',             N'DS 7',             2022, 101.00, 0),
    (N'Polestar',       N'Polestar 2',       2024, 159.00, 0),
    (N'Cupra',          N'Formentor',        2023,  73.00, 0),
    (N'BYD',            N'Seal',             2024,  88.00, 0),
    (N'Smart',          N'#1',               2023,  66.00, 0),
    (N'Rivian',         N'R1S',              2024, 210.00, 0),
    (N'Lucid',          N'Air',              2024, 235.00, 0),
    (N'GMC',            N'Acadia',           2022,  97.00, 0),
    (N'Ram',            N'1500',             2023, 108.00, 0),
    (N'Lynk & Co',      N'01',               2022,  74.00, 0);

    PRINT 'Cars seed data inserted successfully.';

    -- Sample bookings with the denormalized car snapshot required by BookingService.
    INSERT INTO dbo.Bookings
    (
        CarId,
        Make,
        Model,
        CarYear,
        CarPriceInUsd,
        UserId,
        BookingDate,
        PickupDate,
        DropoffDate,
        TotalCostInUsd,
        Status
    )
    SELECT
        c.Id,
        c.Make,
        c.Model,
        c.Year,
        c.PriceInUsd,
        NEWID(),
        DATEADD(DAY, -1, CAST(@Today AS DATETIME2)),
        CAST(@Today AS DATETIME2),
        DATEADD(DAY, 2, CAST(@Today AS DATETIME2)),
        CAST(2 * c.PriceInUsd AS DECIMAL(18, 2)),
        0
    FROM dbo.Cars c
    WHERE c.Id = 3

    UNION ALL

    SELECT
        c.Id,
        c.Make,
        c.Model,
        c.Year,
        c.PriceInUsd,
        NEWID(),
        CAST(@Today AS DATETIME2),
        DATEADD(DAY, 5, CAST(@Today AS DATETIME2)),
        DATEADD(DAY, 8, CAST(@Today AS DATETIME2)),
        CAST(3 * c.PriceInUsd AS DECIMAL(18, 2)),
        0
    FROM dbo.Cars c
    WHERE c.Id = 7

    UNION ALL

    SELECT
        c.Id,
        c.Make,
        c.Model,
        c.Year,
        c.PriceInUsd,
        NEWID(),
        DATEADD(DAY, -12, CAST(@Today AS DATETIME2)),
        DATEADD(DAY, -10, CAST(@Today AS DATETIME2)),
        DATEADD(DAY, -8, CAST(@Today AS DATETIME2)),
        CAST(2 * c.PriceInUsd AS DECIMAL(18, 2)),
        1
    FROM dbo.Cars c
    WHERE c.Id = 10;

    PRINT 'Bookings seed data inserted successfully.';

    -- Keep CarService availability data in sync with blocking booking statuses.
    INSERT INTO dbo.CarUnavailableDates (CarId, BookingId, PickupDate, DropOffDate)
    SELECT CarId, Id, PickupDate, DropoffDate
    FROM dbo.Bookings
    WHERE Status NOT IN (1, 4);

    PRINT 'Car unavailable date seed data inserted successfully.';
END
GO
