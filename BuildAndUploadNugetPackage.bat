echo ### BUILDING ###
dotnet build --configuration Release --force

echo ### PACKAGING ###
dotnet pack --configuration Release --force

echo ### UPLOADING ###
dotnet nuget push "VaultCoreAPI\bin\Release\*.nupkg" --source "github" --skip-duplicate
