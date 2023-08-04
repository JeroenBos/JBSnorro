## Publishing to nuget

Happens automatically in a GitHub workflow on the `master` branch whenever the version changes.

## Testing

I currently can't find out how to run tests from the Test Explorer that require a TestCategory. Via the commandline:
```bash
dotnet test --filter TestCategory=Integration --filter "FullyQualifiedName~xyz"
```
`--filter xyz` is supposed to be equivalent but doesn't work for me 
