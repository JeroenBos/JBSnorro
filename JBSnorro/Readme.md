# This package

is a collection of general-purpose constructs that I use in other projects.

# Notes to self:

## Publishing to nuget

Happens automatically in a GitHub workflow on the `master` branch whenever the version changes.

It would be best we just incremented at the beginning of a PR, like immediately after publishing the previous version.
Then dependent projects can prevent a caching issue (where for instance building against a specific version still builds against a local version rather than a published one).
If we increment immediately, then the local version will never match that specific version, but only the published one will match.

## Testing

I currently can't find out how to run tests from the Test Explorer that require a TestCategory. Via the commandline:
```bash
dotnet test --filter TestCategory=Integration --filter "FullyQualifiedName~xyz"
```
`--filter xyz` is supposed to be equivalent but doesn't work for me 
