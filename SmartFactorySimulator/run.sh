#!/bin/bash
# Run the Smart Factory Simulator with a specified number of parallel sessions
#
# Usage: ./run.sh [number_of_sessions]
# Example: ./run.sh 5    # Run 5 parallel factory sessions
#          ./run.sh       # Run 1 session (default)

cd "$(dirname "$0")"

if [ -z "$1" ]; then
    echo "Running with 1 session (default)"
    dotnet run
else
    echo "Running with $1 parallel sessions"
    dotnet run -- "$1"
fi
