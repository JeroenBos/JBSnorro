## Publishing to nuget

To prevent me from wasting time next time, be sure to use the command
```bash
dotnet build && dotnet pack
```
to pack, and don't use `nuget pack`.

For the actual publishing I manually upload the `JBSnorro.X.X.X.symbols.nupkg` to nuget.org.