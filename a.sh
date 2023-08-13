# a script that should succeed:
TempPackageSource='/tmp/tmp.eELq5HKfyV'
nuget locals all -clear
dotnet nuget enable source 'Package source 1' # with 'enable' it works. with 'disable' it doesn't

rm -rf "/tmp/tmp.eELq5HKfyV"
mkdir -p "/tmp/tmp.eELq5HKfyV"
rm -rf JBSnorro/bin
rm -rf JBSnorro/obj
rm -rf JBSnorro.Tests/bin
rm -rf JBSnorro.Tests/obj
rm -rf JBSnorro.Testing/bin
rm -rf JBSnorro.Testing/obj
rm -rf JBSnorro.Testing.Tests/bin
rm -rf JBSnorro.Testing.Tests/obj
# delete all bins and objs

dotnet publish --configuration Release --framework net7.0 --output "$TempPackageSource" JBSnorro/JBSnorro.csproj
dotnet restore JBSnorro.Testing/JBSnorro.Testing.csproj -p:Configuration=Release
