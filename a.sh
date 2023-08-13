# a script that should succeed:
TempPackageSource='%TMP%/tmp.eELq5HKfyV'
nuget locals all -clear

# delete all bins and objs

dotnet publish --configuration Release --framework net7.0 --output "$TempPackageSource" JBSnorro/JBSnorro.csproj

dotnet restore JBSnorro.Testing/JBSnorro.Testing.csproj -p:Configuration=Release