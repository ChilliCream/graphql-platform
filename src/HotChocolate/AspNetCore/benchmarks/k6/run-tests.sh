#!/bin/bash

set -eo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Variable to track if we started the AppHost
APPHOST_STARTED=false
APPHOST_PID=""

# Cleanup function
cleanup() {
    if [ "$APPHOST_STARTED" = true ] && [ -n "$APPHOST_PID" ]; then
        echo ""
        echo -e "${YELLOW}Stopping AppHost (PID: $APPHOST_PID)...${NC}"
        kill $APPHOST_PID 2>/dev/null || true
        wait $APPHOST_PID 2>/dev/null || true
        echo -e "${GREEN}✓${NC} AppHost stopped"
    fi
}

# Register cleanup function to run on script exit
trap cleanup EXIT INT TERM

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}k6 Load Test Runner${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check if k6 is installed
check_k6() {
    if command_exists k6; then
        K6_VERSION=$(k6 version | head -n 1)
        echo -e "${GREEN}✓${NC} k6 is installed: $K6_VERSION"
        return 0
    else
        echo -e "${YELLOW}✗${NC} k6 is not installed"
        return 1
    fi
}

# Function to install k6 based on OS
install_k6() {
    echo -e "${YELLOW}Installing k6...${NC}"

    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        if command_exists brew; then
            echo "Installing k6 via Homebrew..."
            brew install k6
        else
            echo -e "${RED}Error: Homebrew is not installed. Please install Homebrew first:${NC}"
            echo "  /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            exit 1
        fi
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        if command_exists apt-get; then
            # Debian/Ubuntu
            echo "Installing k6 via apt..."
            sudo gpg -k
            sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
            echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
            sudo apt-get update
            sudo apt-get install k6
        elif command_exists yum; then
            # RHEL/CentOS
            echo "Installing k6 via yum..."
            sudo yum install -y https://dl.k6.io/rpm/repo.rpm
            sudo yum install k6
        else
            echo -e "${RED}Error: Unsupported Linux distribution${NC}"
            echo "Please install k6 manually: https://k6.io/docs/getting-started/installation/"
            exit 1
        fi
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
        # Windows
        if command_exists choco; then
            echo "Installing k6 via Chocolatey..."
            choco install k6
        elif command_exists winget; then
            echo "Installing k6 via winget..."
            winget install k6 --source winget
        else
            echo -e "${RED}Error: Neither Chocolatey nor winget found${NC}"
            echo "Please install k6 manually: https://k6.io/docs/getting-started/installation/"
            exit 1
        fi
    else
        echo -e "${RED}Error: Unsupported operating system${NC}"
        echo "Please install k6 manually: https://k6.io/docs/getting-started/installation/"
        exit 1
    fi

    echo -e "${GREEN}✓${NC} k6 installed successfully"
}

# Function to check if .NET is installed
check_dotnet() {
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        echo -e "${GREEN}✓${NC} .NET SDK is installed: $DOTNET_VERSION"
        return 0
    else
        echo -e "${RED}✗${NC} .NET SDK is not installed"
        echo "Please install .NET SDK: https://dotnet.microsoft.com/download"
        return 1
    fi
}

# Function to start the AppHost
start_apphost() {
    local apphost_dir="$1"

    echo -e "${BLUE}Starting Catalog.AppHost...${NC}"

    if [ ! -d "$apphost_dir" ]; then
        echo -e "${RED}✗${NC} AppHost directory not found: $apphost_dir"
        return 1
    fi

    # Start the AppHost in the background
    cd "$apphost_dir"
    dotnet run -c release > /tmp/apphost.log 2>&1 &
    APPHOST_PID=$!
    APPHOST_STARTED=true

    echo -e "${YELLOW}AppHost starting (PID: $APPHOST_PID)...${NC}"
    cd - > /dev/null

    return 0
}

# Function to wait for the GraphQL server to be ready
wait_for_server() {
    local max_wait=$1
    local elapsed=0
    local check_interval=2

    echo -e "${BLUE}Waiting for GraphQL server to be ready (max ${max_wait}s)...${NC}"

    while [ $elapsed -lt $max_wait ]; do
        if curl -s -o /dev/null -w "%{http_code}" http://localhost:5224/graphql -X POST \
            -H "Content-Type: application/json" \
            -d '{"query": "{ __typename }"}' | grep -q "200"; then
            echo -e "${GREEN}✓${NC} GraphQL server is ready (took ${elapsed}s)"
            return 0
        fi

        echo -n "."
        sleep $check_interval
        elapsed=$((elapsed + check_interval))
    done

    echo ""
    echo -e "${RED}✗${NC} GraphQL server did not become ready within ${max_wait}s"
    echo -e "${YELLOW}Check the logs: tail -f /tmp/apphost.log${NC}"
    return 1
}

# Function to check if the GraphQL server is running
check_server() {
    if curl -s -o /dev/null -w "%{http_code}" http://localhost:5224/graphql -X POST \
        -H "Content-Type: application/json" \
        -d '{"query": "{ __typename }"}' 2>/dev/null | grep -q "200"; then
        return 0
    else
        return 1
    fi
}

# Function to kill any existing processes on port 5224
kill_existing_server() {
    echo -e "${BLUE}Checking for existing server on port 5224...${NC}"

    # Find process using port 5224
    local PID=""
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        PID=$(lsof -ti:5224 2>/dev/null || true)
    else
        # Linux
        PID=$(lsof -ti:5224 2>/dev/null || fuser 5224/tcp 2>/dev/null || true)
    fi

    if [ -n "$PID" ]; then
        echo -e "${YELLOW}Found existing process on port 5224 (PID: $PID). Terminating...${NC}"
        kill $PID 2>/dev/null || true
        sleep 2

        # Force kill if still running
        if kill -0 $PID 2>/dev/null; then
            echo -e "${YELLOW}Force killing process...${NC}"
            kill -9 $PID 2>/dev/null || true
            sleep 1
        fi

        echo -e "${GREEN}✓${NC} Existing server terminated"
    else
        echo -e "${GREEN}✓${NC} No existing server found on port 5224"
    fi

    return 0
}

# Function to run a single test
run_test() {
    local test_file=$1
    local test_name=$2

    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}Running: ${test_name}${NC}"
    echo -e "${BLUE}========================================${NC}"

    if k6 run "$test_file"; then
        echo -e "${GREEN}✓${NC} ${test_name} completed successfully"
        return 0
    else
        echo -e "${RED}✗${NC} ${test_name} failed"
        return 1
    fi
}

# Main script execution
echo -e "${BLUE}Step 1: Checking dependencies${NC}"

# Check .NET SDK
if ! check_dotnet; then
    echo -e "${RED}Cannot proceed without .NET SDK. Exiting.${NC}"
    exit 1
fi

# Check k6
if ! check_k6; then
    read -p "Would you like to install k6 now? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        install_k6
    else
        echo -e "${RED}Cannot proceed without k6. Exiting.${NC}"
        exit 1
    fi
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
APPHOST_DIR="$SCRIPT_DIR/Catalog.AppHost"

echo ""
echo -e "${BLUE}Step 2: Preparing server for clean test run${NC}"

# Kill any existing server to ensure clean state
kill_existing_server

# Start the AppHost
if ! start_apphost "$APPHOST_DIR"; then
    echo -e "${RED}Failed to start AppHost. Exiting.${NC}"
    exit 1
fi

# Wait for the server to be ready (30 seconds for cold start)
if ! wait_for_server 30; then
    echo -e "${RED}Server did not become ready in time. Exiting.${NC}"
    exit 1
fi

# Run tests
echo ""
echo -e "${BLUE}Step 3: Running load tests${NC}"

FAILED_TESTS=0

if ! run_test "$SCRIPT_DIR/single-fetch.js" "Single Fetch Test"; then
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi

if ! run_test "$SCRIPT_DIR/dataloader.js" "DataLoader Test"; then
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi

# Summary
echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}✗ $FAILED_TESTS test(s) failed${NC}"
    exit 1
fi
