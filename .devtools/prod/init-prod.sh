#!/bin/bash
set -e

/opt/mssql-tools18/bin/sqlcmd \
  -S mssql \
  -U sa \
  -P "YourStrong!Passw0rd" \
  -C \
  -i /init-prod.sql