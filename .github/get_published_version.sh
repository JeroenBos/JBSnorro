#!/bin/bash

# NOTE: don't debug this in VS, but do in VSCode because:
# nuget list "id:JBSnorro" # doesn't hang
# $(nuget list "id:JBSnorro") # hangs 🤷‍ in VS wsl in Developer Powershell window (but not in VSCode terminal)

version=$(nuget list "id:JBSnorro"   \
        | grep '\bJBSnorro\s'        \
        | grep -v 'JBSnorro.Testing' \
        | sed 's/JBSnorro//'         \
        | xargs                      \
        | tr -d '\n'                 \
        | tr -d '\r'                 )
# grep tries to exclude other JBSnorro patters. sed strips JBSnorro. xargs trims. tr trims newlines

if [ -z "$version" ]; then
    echo "fatal: No version found";
    exit 1;
fi

if [ -z $(echo "$version" | grep -Pe '[0-9\.]+') ]; then
    echo "fatal: Invalid version found: $version";
    exit 1;
fi
echo "$version"
