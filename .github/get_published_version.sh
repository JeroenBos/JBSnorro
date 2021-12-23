#!/bin/bash

# NOTE: don't debug this in VS, but do in VSCode because:
# nuget list "id:JBSnorro" # doesn't hang
# $(nuget list "id:JBSnorro") # hangs 🤷‍ in VS wsl in Developer Powershell window (but not in VSCode terminal)

version=$(nuget list "id:JBSnorro" | sed 's/JBSnorro//' | xargs | tr -d '\n' | tr -d '\r')
# xargs trims. tr trims newlines

if [ -z $version ]; then
    echo "fatal: No version found"
    exit 1
fi

echo "$version"
