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
    rel_path=$(git_repo_path "$1")
    repo_dir=$(get_repo_dir)
    tmp_dir="/c/temp"
    dir -p "$tmp_dir"
    
    cd "$repo_dir"
    # copy file from index to tmp_dir
    err="$(GIT_WORK_TREE="$tmp_dir" git checkout-index -- "$rel_path" 2>&1)"
    # --temp doesn't do what you think it does
    exit_code="$?"
    
    # select the first word in the map, which is the relative temp file name
    # tmp_rel_path="$(echo "$tmp_path_map" | sed -e 's/\s.*$//')"
    echo run "$(date +"%T")"                                  >  $HOME/test.txt
    echo prepare_create_diff_patch: "$PWD"                    >> $HOME/test.txt
    echo prepare_create_diff_patch: "rel_path:$rel_path"      >> $HOME/test.txt
    echo prepare_create_diff_patch: "repo_dir:$repo_dir"      >> $HOME/test.txt
    echo prepare_create_diff_patch: "$exit_code"              >> $HOME/test.txt
    echo prepare_create_diff_patch: "$err"                    >> $HOME/test.txt
    echo prepare_create_diff_patch: "$tmp_dir/$rel_path"      >> $HOME/test.txt
    
    if [[ "$exit_code" -ne 0 ]]; then
        >&2 echo "${err}"
        exit $exit_code
    fi

    echo "$tmp_dir/$rel_path"
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
    # not necessary to call if the condition create_diff_patch_adding_eof_lf_condition is not met
    ################################
    # parameters:
    # $1: the path of the temp file to add eof lf to
    tmp_path="$1"
    ################################
    echo "In create_diff_patch_adding_eof_lf '$1'"


    # do the manipulation
    echo hhhh >> "$tmp_path"
}
export -f create_diff_patch_adding_eof_lf

function create_diff_patch_adding_eof_lf_condition() {
    path="$1"
    [ "$(tail -c1 $path)" != "$lf" ] && echo true || echo false
}
export -f create_diff_patch_adding_eof_lf_condition


function conditionally_apply() {
    # parameters:
    # $1: the condition function echoing 'true' or 'false'
    # $2: the action function to be called on 'true'
    # $N: parameters to be passed on to both functions
    
    if [ "$#" -lt 2 ] ; then
        echo "Expected at least 2 arguments";
        exit 1;
    fi

    F_CONDITION="$1"
    F_ACTION="$2"
    shift
    shift

    condition=$(${F_CONDITION} "$@")
    if [[ "${condition}" == 'true' ]]; then
        echo 6
        "${F_ACTION}" "$@"
    else
        echo "7: ${condition}"
    fi
}
export -f conditionally_apply

function conditionally_apply_on_tmp_path() {
    # parameters:
    # $1: the condition function echoing 'true' or 'false'
    # $2: the action function to be called on 'true'
    # $3: the path to apply the action on
    # $N: extra parameters to be passed
    echo 1
    if [[ "$#" -lt 3 ]]; then echo "expected at least 3 arguments"; exit 1; fi

    F_CONDITION="$1"
    F_ACTION="$2"
    shift
    shift

    function CURRIED_F_ACTION() {
        echo 3
        path="$1"
        shift
        tmp_path=$(prepare_create_diff_patch "$path")

        echo 4
        "${F_ACTION}" "$tmp_file" "$@"

        finalize_create_diff_patch "$path" "$tmp_path"
    }

    echo 2
    x=$(conditionally_apply $F_CONDITION CURRIED_F_ACTION "$@")
    echo 5
    echo $x
}
export -f conditionally_apply_on_tmp_path

# Add a linebreak to the file if it doesn't have one
conditionally_apply_on_tmp_path create_diff_patch_adding_eof_lf_condition create_diff_patch_adding_eof_lf "$HOME/a.txt"


