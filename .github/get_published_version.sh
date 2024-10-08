#!/bin/bash

# NOTE: don't debug this in VS, but do in VSCode because:
# nuget list "id:JBSnorro" # doesn't hang
# $(nuget list "id:JBSnorro") # hangs 🤷‍ in VS wsl in Developer Powershell window (but not in VSCode terminal)

if [[ "$#" -ne 1 ]]; then
    echo "Expected exactly 1 argument, namely the nuget package ID, e.g. 'JBSnorro'";
    exit 1;
fi

output="$(nuget search "packageid:$1" -Verbosity quiet -Source nuget.org)"
if [[ $? -ne 0 ]]; then
    echo $output
    exit 1
fi

version="$(echo "$output"               \
         | grep '>'                     \
         | cut -d '|' -f2               \
         | tr -d '[:space:]'            )"

if [[ "$version" == "No packages found." ]]; then
    echo "fatal: No version of '$1' found"
    exit 1
fi

if [ -z "$(echo "$version" | grep -Pe '[0-9\.]+')" ]; then  # -Pe is Perl-regex format
    echo "fatal: Invalid version found: $version";
    exit 1;
fi
echo "$version"
