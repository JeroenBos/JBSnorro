#!/bin/bash
cr="$(printf "\r")"  # get \r
lf="$(printf "\n")"  # get \n

# Files (not deleted) in the index
files=$(git diff-index --name-status --cached HEAD | grep -v ^D | cut -c3-)
if [ "$files" != "" ]
then
  for f in $files
  do
    # Only examine text files
    if [[ "$f" =~ [.](py|pyi|sh|lock|toml|rst|md|cfg|ini|css|html|js|ts|jsx|tsx|json|log|txt|xml|yml|yaml|crt|key)$ ]]
    then
      # Replace CRLF with LF 
      # -U: prevents grep from stripping CR characters. By default it does this it if it decides it's a text file.
      
      # Add a linebreak to the file if it doesn't have one
      if [ "$(tail -c1 $f)" != "$lf" ]
      then
        echo >> $f
        git add $f
        # TODO: if the file was not fully in the index, raise an error, or only add the newline if possible
      fi

      if grep -U --color "$cr" "$f"
      then
        sed -i 's/\r$//' "$f"
        git add "$f"
        # TODO: if the file was not fully in the index, raise an error
      fi
      
    fi
  done
fi
