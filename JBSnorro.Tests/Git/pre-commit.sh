#!/bin/bash
# set -e
echo realStart

cr="$(printf "\r")"  # get \r
lf="$(printf "\n")"  # get \n


function get_repo_dir() {
    # Gets the absolute path of the repo
    echo $(cd $(git rev-parse --show-cdup) .; pwd)
}
# https://stackoverflow.com/a/21619617/308451
function git_repo_path () {
    fullpath=$([[ $1 = /* ]] && echo "$1" || echo "$PWD/${1#./}")
    gitroot="$(get_repo_dir)" || return 1
    [[ "$fullpath" =~ "$gitroot" ]] && echo "${fullpath/$gitroot\//}"
}
export -f git_repo_path

function prepare_create_diff_patch() {
    datetime=$(date '+%Y-%m-%d__%H-%M-%S')
    tmp_path="/c/temp/$datetime.txt"
    cp "$1" "$tmp_path"
    if [[ $(stat -c %s "$1") != $(stat -c %s "$tmp_path") ]]; then
       >&2 echo "files not copied properly"
       exit 1
    fi
    echo "$tmp_path"
}
export -f prepare_create_diff_patch
function finalize_create_diff_patch() {
    path="$1"
    tmp_path="$2"

    path_repo=$(git_repo_path "$path")
    repo_dir=$(get_repo_dir)
    cd "$repo_dir"
    
    >&2 echo HHHHHHHHHH. tmp path = "$tmp_path". repo path = "$path_repo". repo_dir="$repo_dir"
    
    # errors after this....


    # -b preserves CR
    patch=$(git diff --no-index "$path_repo" "$tmp_path" | sed -b -e "s:C\::c:g" | sed  -b -e "s:$tmp_path:/$path_repo:g")

    >&2 echo "creating $HOME/test.patch"
    echo "$patch" > "$HOME/test.patch"
    >&2 echo applying
    git apply <<< "$patch" --index
    exitcode=$?
    >&2 echo done applying. exitcode=$?
    cd -
    # rm "$tmp_path"
}
export -f finalize_create_diff_patch

function create_diff_patch_adding_eof_lf() {
    ################################
    ######### Adds EOF LF ##########
    ################################
    echo "In create_diff_patch_adding_eof_lf '$1'"

    path="$1"
    
    if [ "$(tail -c1 $path)" != "$lf" ]
    then
        # create tmp file
        tmp_path=$(prepare_create_diff_patch "$path")
        >&2 echo prepared, tmp_path="$tmp_path"

        # do the manipulation
        echo >> "$tmp_path"

        >&2 echo finalizing
        # do the patch
        finalize_create_diff_patch "$path" "$tmp_path"
    fi
    echo Done
}
export -f create_diff_patch_adding_eof_lf


function create_diff_patch_replacing_crlf_with_lf() {
    ################################
    #### Replaces CRLF with LF #####
    ################################

    path="$1"

    # -U: prevents grep from stripping CR characters. By default it does this it if it decides it's a text file.
    if grep -U --color "$cr" "$path"
    then
        # create tmp file
        tmp_path=$(prepare_create_diff_patch "$path")

        # do the manipulation
        sed -i 's/\r$//' "$tmp_path"

        # do the patch
        finalize_create_diff_patch "$path" "$tmp_path"
    fi
}
export -f create_diff_patch_replacing_crlf_with_lf


# Files (not deleted) in the index
files=$(git diff-index --name-status --cached HEAD | grep -v ^D | cut -c3-)
if [ "$files" != "" ]
then
  for f in $files
  do
    # Only examine text files
    if [[ "$f" =~ [.](py|pyi|sh|lock|toml|rst|md|cfg|ini|css|html|js|ts|jsx|tsx|json|log|txt|xml|yml|yaml|crt|key)$ ]]
    then
      
      # Add a linebreak to the file if it doesn't have one
      create_diff_patch_adding_eof_lf "$f"

      # Replace CRLF with LF
      create_diff_patch_replacing_crlf_with_lf "$f"
      
    fi
  done
fi

