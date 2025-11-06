# HotChocolate Fusion v-next Benchmarks

Performance benchmarks for HotChocolate Fusion gateway with CPU affinity isolation, comparing AOT vs Release compilation modes.

## Architecture

This benchmark suite tests a federated GraphQL gateway composed of:

- **Gateway** (Port 5220) - HotChocolate Fusion gateway composing all source schemas
- **Source Schemas:**
  - **Accounts** (Port 5221) - User data and authentication
  - **Inventory** (Port 5222) - Product stock and shipping estimates
  - **Products** (Port 5223) - Product catalog
  - **Reviews** (Port 5224) - Product reviews and ratings

All services use in-memory data (no database) for consistent, reproducible results.

## CPU Allocation Strategy

Inspired by [The Guild's GraphQL Gateway Benchmark](https://github.com/graphql-hive/graphql-gateways-benchmark), this suite uses CPU pinning via `taskset` to isolate load generation from the system under test:

```
Cores 0-1:   K6 load generator (isolated)
Cores 2-7:   Gateway (6 cores for main SUT)
Cores 8-9:   Accounts source schema
Cores 10-11: Inventory source schema
Cores 12-13: Products source schema
Cores 14-15: Reviews source schema
```

**Why CPU pinning?**
- Prevents CPU migration between cores (better cache efficiency)
- Isolates load generator from services under test
- Reduces performance variance between runs
- Provides more accurate, reproducible benchmarks

## Prerequisites

- **k6** - Load testing tool ([installation guide](https://k6.io/docs/getting-started/installation/))
- **.NET 9 SDK** - For building and running services
- **taskset** - CPU affinity tool (Linux/macOS, usually pre-installed)
- **jq** - JSON parsing for result analysis (optional but recommended)
- **16 CPU cores** - Recommended for full CPU pinning strategy

### Quick Install

**macOS:**
```bash
brew install k6 jq
```

**Ubuntu/Debian:**
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6 jq
```

## Quick Start

Run the complete benchmark suite:

```bash
chmod +x *.sh
./run-benchmarks.sh
```

This will:
1. Start all 4 source schemas with CPU pinning
2. Test gateway in **Release** mode (both k6 tests)
3. Test gateway in **AOT** mode (both k6 tests)
4. Save results to `results/` directory
5. Clean up all services

View performance comparison:

```bash
./compare-results.sh
```

## Test Scenarios

### 1. Single Fetch (simple-fetch.js)

Tests basic query performance with minimal federation overhead:

```graphql
query GetTopProduct {
  topProducts(first: 1) {
    name
  }
}
```

**Load Pattern:**
- Warmup: Ramp from 50 to 500 RPS over 30 seconds
- Measurement: Constant 500 RPS for 60 seconds

**Thresholds:**
- p95 < 500ms (measurement phase)
- p99 < 1000ms (measurement phase)
- Error rate < 1%

### 2. Federation Stress (federation-stress.js)

Tests complex federated query with nested joins across all source schemas:

```graphql
query TestQuery {
  users {
    id
    username
    name
    reviews {
      id
      body
      product {
        inStock          # From Inventory
        name             # From Products
        price            # From Products
        shippingEstimate # From Inventory (computed)
        reviews {
          author {
            reviews {
              product { ... }
            }
          }
        }
      }
    }
  }
  topProducts { ... }
}
```

This query exercises:
- Cross-schema type resolution
- N+1 query prevention (batching)
- Computed fields from multiple sources
- Deep nesting (4+ levels)

**Same load pattern and thresholds as Single Fetch.**

## Manual Usage

### Start Individual Components

**Start source schemas:**
```bash
./start-source-schemas.sh
```

**Start gateway (Release mode):**
```bash
./start-gateway.sh release
```

**Start gateway (AOT mode):**
```bash
./start-gateway.sh aot
```

**Stop all services:**
```bash
./stop-services.sh
```

### Run Individual Tests

**Run single k6 test:**
```bash
k6 run single-fetch.js
```

**Run with CPU pinning:**
```bash
taskset -c 0-1 k6 run single-fetch.js
```

**Export results:**
```bash
k6 run single-fetch.js \
  --out json=results/my-test.json \
  --summary-export=results/my-summary.json
```

## Customizing CPU Allocation

Override CPU sets via environment variables:

```bash
# K6 on cores 0-3
export K6_CPUSET="0-3"

# Gateway on cores 4-11
export GATEWAY_CPUSET="4-11"

# Source schemas
export ACCOUNTS_CPUSET="12-13"
export INVENTORY_CPUSET="14-15"
export PRODUCTS_CPUSET="16-17"
export REVIEWS_CPUSET="18-19"

./run-benchmarks.sh
```

**For systems with fewer cores:**

The scripts gracefully degrade if `taskset` is unavailable. Services will run normally without CPU pinning.

## Understanding Results

### K6 Metrics

- **http_req_duration** - Request latency (p50, p95, p99)
- **http_reqs** - Total requests and rate (RPS)
- **http_req_failed** - Error rate
- **iteration_duration** - Total iteration time including checks

### AOT vs Release Comparison

The `compare-results.sh` script shows:

```
Test: Single Fetch (Top Product)
────────────────────────────────────────────
Metric               AOT             Release         Δ
────────────────── ───────────────── ─────────────── ────────
p50 latency (ms)   45.00           52.00           +13%
p95 latency (ms)   120.00          145.00          +17%
p99 latency (ms)   200.00          250.00          +20%
Throughput (RPS)   2100.00         1850.00         -13%
```

**Interpreting Δ (Delta):**
- **Green (+%)** - AOT improvement (faster/lower latency)
- **Red (-%)** - AOT regression (slower/higher latency)
- **For RPS**: Higher is better, so positive Δ means AOT handles more requests

**Expected AOT Improvements:**
- Faster startup time
- Lower memory footprint
- Reduced p95/p99 latency (less GC pressure)
- Higher throughput under sustained load

## Troubleshooting

### Port Already in Use

```bash
./stop-services.sh
# Or manually:
lsof -ti:5220 | xargs kill -9  # Gateway
lsof -ti:5221 | xargs kill -9  # Accounts
# ... etc
```

### Gateway Fails to Start

Check that `gateway.far` exists in `eShop.Gateway/` directory. This file contains the composed schema configuration.

### AOT Build Fails

Ensure you're using .NET 9 or later:
```bash
dotnet --version
```

Check for trimming warnings during publish. Some dependencies may not be AOT-compatible.

### k6 Tests Fail Thresholds

If tests consistently fail thresholds:
1. Check system load (close other applications)
2. Verify CPU pinning is working (`taskset -cp <PID>`)
3. Adjust thresholds in test files if needed
4. Try fewer VUs or lower RPS targets

### No taskset on macOS

macOS doesn't have `taskset` by default. The scripts will work but without CPU pinning. For better isolation:
- Close other applications
- Use Activity Monitor to check CPU usage
- Consider running on Linux for production benchmarks

## Benchmark Best Practices

1. **Close other applications** - Minimize background CPU/memory usage
2. **Run multiple iterations** - Single runs can have variance
3. **Let system cool down** - Wait 30s between runs to clear caches
4. **Monitor system metrics** - Use `htop` or Activity Monitor during tests
5. **Check for thermal throttling** - Especially on laptops
6. **Use consistent environment** - Same OS, same load, same config

## Files

- `single-fetch.js` - Simple query k6 test
- `federation-stress.js` - Complex federated query k6 test
- `start-source-schemas.sh` - Start all source schemas with CPU pinning
- `start-gateway.sh` - Start gateway (supports `aot`/`release` mode)
- `stop-services.sh` - Stop all services
- `run-benchmarks.sh` - Main orchestration script
- `compare-results.sh` - Compare AOT vs Release results
- `results/` - Output directory for k6 results (created automatically)

## Contributing

When modifying benchmarks:
- Keep queries realistic (avoid artificial micro-benchmarks)
- Document any schema changes in this README
- Update thresholds if baseline performance changes significantly
- Test on both Linux and macOS

## References

- [The Guild's GraphQL Gateway Benchmark](https://github.com/graphql-hive/graphql-gateways-benchmark)
- [k6 Documentation](https://k6.io/docs/)
- [HotChocolate Fusion](https://chillicream.com/docs/hotchocolate/v13/distributed-schemas/fusion)
- [.NET Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
