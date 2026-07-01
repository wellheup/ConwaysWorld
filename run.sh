#!/bin/bash
set -e

# Prevent git-lfs from smudging (and zeroing) files whose LFS objects are not
# available from the remote. Model.cs is stored in LFS but served locally from
# the workspace; without this flag dotnet-format's internal git calls zero it.
export GIT_LFS_SKIP_SMUDGE=1

echo "==> Formatting ConwaysWorld.Simulation..."
dotnet format "ConwaysWorld.Simulation/ConwaysWorld.Simulation.csproj" --no-restore

echo "==> Formatting ConwaysWorld.Blazor..."
dotnet format "ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj" --no-restore

echo "==> Writing build timestamp..."
date +%s > ConwaysWorld.Blazor/wwwroot/_build.txt

echo "==> Starting application..."
exec dotnet run --project ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj --urls http://0.0.0.0:5000
