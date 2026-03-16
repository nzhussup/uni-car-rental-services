USE CarRentalDB;
GO

BEGIN
    DELETE FROM dbo.Cars;

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
    (N'Volkswagen',     N'Golf',         2022, 52.00,  0),  -- Available
    (N'Kia',            N'Optima',       2021, 43.50,  0),  -- Available
    (N'Nissan',         N'Altima',       2022, 47.25,  0),  -- Available
    (N'Mazda',          N'Mazda6',       2021, 50.00,  1),  -- Rented
    (N'Subaru',         N'Legacy',       2023, 57.00,  0),  -- Available
    (N'Lexus',          N'ES 350',       2022, 138.00, 2),  -- Maintenance
    (N'Volvo',          N'S60',          2023, 126.00, 0),  -- Available
    (N'Jaguar',         N'XE',           2021, 142.00, 1),  -- Rented
    (N'Alfa Romeo',     N'Giulia',       2022, 134.99, 0),  -- Available
    (N'Peugeot',        N'508',          2023, 61.00,  0),  -- Available
    (N'Renault',        N'Talisman',     2021, 54.00,  2),  -- Maintenance
    (N'Skoda',          N'Octavia',      2023, 53.00,  0),  -- Available
    (N'SEAT',           N'Leon',         2022, 51.00,  0),  -- Available
    (N'Opel',           N'Insignia',     2021, 49.00,  1),  -- Rented
    (N'Fiat',           N'Tipo',         2023, 41.00,  0),  -- Available
    (N'Mini',           N'Cooper',       2022, 78.00,  0),  -- Available
    (N'Dacia',          N'Jogger',       2023, 39.99,  0),  -- Available
    (N'Mitsubishi',     N'Lancer',       2021, 46.00,  2),  -- Maintenance
    (N'Jeep',           N'Cherokee',     2022, 96.50,  1),  -- Rented
    (N'Land Rover',     N'Discovery Sport', 2023, 168.00, 0),  -- Available
    (N'Porsche',        N'Macan',        2022, 220.00, 0),  -- Available
    (N'Infiniti',       N'Q50',          2021, 112.00, 0),  -- Available
    (N'Acura',          N'TLX',          2023, 118.00, 1),  -- Rented
    (N'Genesis',        N'G70',          2022, 125.00, 0),  -- Available
    (N'Chrysler',       N'300',          2021, 83.00,  2),  -- Maintenance
    (N'Dodge',          N'Charger',      2023, 92.00,  0),  -- Available
    (N'Cadillac',       N'CT5',          2022, 141.00, 1),  -- Rented
    (N'Buick',          N'Regal',        2021, 68.00,  0),  -- Available
    (N'Lincoln',        N'Corsair',      2023, 133.00, 0),  -- Available
    (N'Suzuki',         N'Swace',        2022, 44.00,  0),  -- Available
    (N'Citroen',        N'C5 X',         2023, 58.00,  1),  -- Rented
    (N'DS',             N'DS 7',         2022, 101.00, 0),  -- Available
    (N'Polestar',       N'Polestar 2',   2024, 159.00, 0),  -- Available
    (N'Cupra',          N'Formentor',    2023, 73.00,  2),  -- Maintenance
    (N'BYD',            N'Seal',         2024, 88.00,  0),  -- Available
    (N'Smart',          N'#1',           2023, 66.00,  0),  -- Available
    (N'Rivian',         N'R1S',          2024, 210.00, 1),  -- Rented
    (N'Lucid',          N'Air',          2024, 235.00, 0),  -- Available
    (N'GMC',            N'Acadia',       2022, 97.00,  0),  -- Available
    (N'Ram',            N'1500',         2023, 108.00, 2),  -- Maintenance
    (N'Lynk & Co',      N'01',           2022, 74.00,  1);  -- Rented

    PRINT 'Cars table overwritten and seed data inserted successfully.';
END
GO
