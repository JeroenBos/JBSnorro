#!/bin/bash

# NOTE: don't debug this in VS, but do in VSCode because:
# nuget list "id:JBSnorro" # doesn't hang
# $(nuget list "id:JBSnorro") # hangs 🤷‍ in VS wsl in Developer Powershell window (but not in VSCode terminal)

if [[ "$#" -ne 1 ]]; then
    echo "Expected exactly 1 argument, namely the nuget package ID, e.g. 'JBSnorro'";
    exit 1;
fi

version=$(dotnet nuget list "packageid:$1"  \
        | sed "s/$1//"                      \
        | xargs                             \
        | sed -e 's/^[[:space:]]*//'        \
        | tr -d '\n'                        \
        | tr -d '\r'                        )
# sed strips package name. xargs trims. sed -e trims leading spaces because xargs doesn't trim leading spaces 🤷‍, tr trims newlines

if [[ "$version" == "No packages found." ]]; then
    echo "fatal: No version of '$1' found"
    exit 1
fi

if [ -z $(echo "$version" | grep -Pe '[0-9\.]+') ]; then  # -Pe is Perl-regex format
    echo "fatal: Invalid version found: $version";
    exit 1;
fi
echo "$version"
