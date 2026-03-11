USE CarRentalDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Cars)
BEGIN
    INSERT INTO dbo.Cars (Make, Model, Year, PriceInUsd, Status) VALUES
    (N'Toyota',         N'Camry',       2022, 55.00,  0),  -- Available
    (N'Honda',          N'Civic',        2023, 49.99,  0),  -- Available
    (N'Ford',           N'Mustang',      2021, 89.99,  1),  -- Rented
    (N'BMW',            N'3 Series',     2023, 120.00, 0),  -- Available
    (N'Tesla',          N'Model 3',      2024, 149.99, 0),  -- Available
    (N'Chevrolet',      N'Silverado',    2022, 95.00,  2),  -- Maintenance
    (N'Audi',           N'A4',           2023, 110.00, 0),  -- Available
    (N'Mercedes-Benz',  N'C-Class',      2022, 130.00, 1),  -- Rented
    (N'Hyundai',        N'Sonata',       2023, 45.00,  0),  -- Available
    (N'Volkswagen',     N'Golf',         2022, 52.00,  0);  -- Available

    PRINT 'Seed data inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Cars table already has data — skipping seed.';
END
GO
