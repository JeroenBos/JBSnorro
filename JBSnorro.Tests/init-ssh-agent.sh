#!/usr/bin/env bash
function start_ssh_agent_if_necessary() {
    env="$1"

    agent_load_env () { test -f "$env" && . "$env" >| /dev/null ; }

    agent_start () {
        (umask 077; ssh-agent >| "$env")
        . "$env" >| /dev/null ; }

    agent_load_env  # I've verified it works even with this commented out (meaning a new ssh-agent per command).
    # TODO: technically, even though I'm only creating one and reusing it between test runs, I should terminate the ssh-agent after testing

    # agent_run_state: 0=agent running w/ key; 1=agent w/o key; 2= agent not running
    agent_run_state=$(ssh-add -l >| /dev/null 2>&1; echo $?)

    if [ ! "$SSH_AUTH_SOCK" ]; then
        # echo "starting ssh-agent: SSH_AUTH_SOCK = '$SSH_AUTH_SOCK'";
        agent_start
    elif [ $agent_run_state = 2 ]; then
        # echo "starting ssh-agent: wasn't running";
        agent_start
    elif [ $agent_run_state = 1 ]; then
        : # echo "ssh-agent already up without key";
    else
        : # echo "ssh-agent already up with key";
    fi

    unset env
}

env="${1:-"$HOME/.ssh/jbsnorro_debug_agent.env"}"
start_ssh_agent_if_necessary "$env"
export SSH_AUTH_SOCK
