#!/bin/bash
version=$(cat ./JBSnorro/JBSnorro.csproj | grep "<Version>" | sed 's/<Version>//' | sed 's/<\/Version>//' | xargs | tr -d '\n')
# xargs trims. tr trims newlines

if [ -z $version ]; then
    echo "No version found"
    exit 1
fi

echo "$version"
