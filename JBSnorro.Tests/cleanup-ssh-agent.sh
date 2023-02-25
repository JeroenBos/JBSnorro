#!/usr/bin/env bash
function kill_debug_ssh_agent() {
    env=$HOME/.ssh/jbsnorro_debug_agent.env
    
    agent_load_env () { test -f "$env" && . "$env" >| /dev/null ; }
    
    agent_load_env
    
    echo "$SSH_AGENT_PID"
    if [[ -z "$SSH_AGENT_PID" ]]; then
        >&2 echo "No ssh agent pid found to shutdown"
        exit 1
    else
        echo "Killing ssh-agent (pid=$SSH_AGENT_PID)"
        kill "$SSH_AGENT_PID"
    fi
}

kill_debug_ssh_agent
SSH_AUTH_SOCK=""
export SSH_AUTH_SOCK
