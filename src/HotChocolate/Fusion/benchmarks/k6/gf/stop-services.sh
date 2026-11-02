#!/usr/bin/env bash
set -euo pipefail

echo "============================================"
echo "Stopping All Services"
echo "============================================"
echo ""

# Function to kill process on a specific port
kill_on_port() {
  local port=$1
  local name=$2

  if lsof -ti:$port > /dev/null 2>&1; then
    echo "Stopping $name on port $port..."
    lsof -ti:$port | xargs kill -9 2>/dev/null || true
    echo "  ✓ Stopped"
  else
    echo "$name (port $port): not running"
  fi
}

# Stop all services
kill_on_port 5220 "Gateway"
kill_on_port 5221 "Accounts"
kill_on_port 5222 "Inventory"
kill_on_port 5223 "Products"
kill_on_port 5224 "Reviews"

echo ""
echo "✓ All services stopped"
