#!/bin/bash
set -e

echo "==> Formatting ConwaysWorld.Simulation..."
dotnet format "ConwaysWorld.Simulation/ConwaysWorld.Simulation.csproj" --no-restore

echo "==> Formatting ConwaysWorld.Blazor..."
dotnet format "ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj" --no-restore

echo "==> Starting application..."
exec dotnet run --project ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj --urls http://0.0.0.0:5000
