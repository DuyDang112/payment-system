#!/bin/bash

# Kill all payment system services running on ports 5000-5004
# Usage: ./kill-services.sh

PORTS=(5000 5001 5002 5003 5004)
KILLED=0
NOT_FOUND=0

echo "🔍 Searching for payment system services on ports ${PORTS[*]}..."
echo ""

for port in "${PORTS[@]}"; do
    # Find PIDs listening on the specified port (both IPv4 and IPv6)
    PIDS=$(lsof -ti :"$port" 2>/dev/null)

    if [ -n "$PIDS" ]; then
        echo "📍 Port $port:"
        for pid in $PIDS; do
            # Get process name and command
            if command -v ps &> /dev/null; then
                PROCESS_INFO=$(ps -p "$pid" -o comm= 2>/dev/null || echo "unknown")
            else
                PROCESS_INFO="process"
            fi

            echo "   Killing $PROCESS_INFO (PID: $pid)"
            kill "$pid" 2>/dev/null && echo "   ✅ Killed" || echo "   ❌ Failed to kill"
            ((KILLED++))
        done
        echo ""
    else
        echo "📍 Port $port: No process found"
        ((NOT_FOUND++))
    fi
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Summary:"
echo "   ✅ Killed: $KILLED processes"
echo "   ℹ️  Not found: $NOT_FOUND ports"
echo ""

# Verify all ports are clear
STILL_RUNNING=0
for port in "${PORTS[@]}"; do
    if lsof -ti :"$port" &>/dev/null; then
        echo "⚠️  Warning: Port $port still has a process running"
        ((STILL_RUNNING++))
    fi
done

if [ $STILL_RUNNING -eq 0 ]; then
    echo "🎉 All payment system ports are clear!"
else
    echo "⚠️  Some ports still have processes. Try with: sudo kill -9 \$(lsof -ti :5000-5004)"
fi
