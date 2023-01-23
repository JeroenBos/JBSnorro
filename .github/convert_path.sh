#!/bin/usr/bash
############################################################
# Coerces paths to certain shapes.                         #
#                                                          #
#      convert_path (--windows|--linux) <path>             #
#                                                          #
############################################################

# argument validation:
os="$1"
path="$2"
if [[ "$os" != '--windows' && "$os" != '--linux' ]]; then
	>&2 echo "Invalid first argument. Expected --windows|--linux; got '$os'"
	exit 1
fi
if [[ -z "$path" ]]; then
    >&2 echo "No path specified."
    exit 1
fi


function to_windows_path() {
    local replace_drive='s/\/c/C:/'
    local replace_dir_seps='s/\//\\/g'
    echo "$1" | sed "$replace_drive" | sed "$replace_dir_seps"
}
function to_linux_path() {
    local replace_drive='s/^C:/\/c/'
    local replace_dir_seps='s/\\/\//g'
    echo "$1" | sed "$replace_drive" | sed "$replace_dir_seps"
}


# main
if [[ "$1" == "--windows" ]]; then
    to_windows_path "$path"
else
    to_linux_path "$path"
fi
