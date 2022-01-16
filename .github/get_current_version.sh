#!/bin/bash
if [[ "$#" -ne 1 ]]; then
    echo "Expected exactly 1 argument, namely a path to a csproj file e.g. './JBSnorro/JBSnorro.csproj'";
    exit 1;
fi

version=$(cat "$1" | grep "<Version>" | sed 's/<Version>//' | sed 's/<\/Version>//' | xargs | tr -d '\n' | tr -d '\r')
# xargs trims. tr trims newlines

if [ -z $version ]; then
    echo "No version found"
    exit 1
fi

echo "$version"
