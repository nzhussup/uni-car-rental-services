#!/usr/bin/env bash
set -euo pipefail

for i in $(seq 1 30); do
  if /opt/mssql-tools18/bin/sqlcmd -S mssql,1433 -U sa -P 'YourStrong!Passw0rd' -C -Q "SELECT TOP 1 c.Id FROM CarRentalDB.dbo.Cars c; SELECT TOP 1 b.Id FROM CarRentalDB.dbo.Bookings b; SELECT TOP 1 d.Id FROM CarRentalDB.dbo.CarUnavailableDates d;" >/dev/null 2>&1; then
    /opt/mssql-tools18/bin/sqlcmd -S mssql,1433 -U sa -P 'YourStrong!Passw0rd' -C -i /seed.sql
    exit 0
  fi

  echo "Waiting for migrations... (${i}/30)"
  sleep 5
done

echo "Seed timed out."
exit 1
