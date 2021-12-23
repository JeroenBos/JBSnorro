#!/bin/bash
version=$(cat ./JBSnorro/JBSnorro.csproj | grep "<Version>" | sed 's/<Version>//' | sed 's/<\/Version>//' | xargs)
# xargs trims

if [ -z $version ]; then
    echo "No version found"
    exit 1
fi

echo "$version"