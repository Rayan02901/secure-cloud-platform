# Root Makefile - secure-cloud-platform
.PHONY: help all run stop test \
        auth-test auth-run auth-stop auth-run-background \
        crypto-test crypto-run crypto-run-background crypto-stop \
        gateway-run gateway-stop gateway-run-background

# Default target
help:
	@echo "🚀 Secure Cloud Platform - Make Commands"
	@echo "=========================================="
	@echo ""
	@echo "🏁 START SERVICES:"
	@echo "  make all     - Start all services with tests"
	@echo "  make run     - Alias for 'all'"
	@echo "  make stop    - Stop all services"
	@echo "  make test    - Run all tests"
	@echo ""
	@echo "🔐 AUTH SERVICE (Port 5000):"
	@echo "  make auth-test - Test auth service"
	@echo "  make auth-run  - Start auth service (foreground)"
	@echo "  make auth-run-background - Start auth service (background)"
	@echo "  make auth-stop - Stop auth service"
	@echo ""
	@echo "🔒 CRYPTO SERVICE (Port 8002):"
	@echo "  make crypto-test - Test crypto service"
	@echo "  make crypto-run  - Start crypto service (foreground)"
	@echo "  make crypto-run-background - Start crypto service (background)"
	@echo "  make crypto-stop - Stop crypto service"
	@echo ""
	@echo "🌐 API GATEWAY (Port 8003):"
	@echo "  make gateway-run - Start API gateway (foreground)"
	@echo "  make gateway-run-background - Start API gateway (background)"
	@echo "  make gateway-stop - Stop API gateway"
	@echo ""
	@echo "🔄 COMBINATIONS:"
	@echo "  make core    - Start core services (auth + crypto + gateway)"
	@echo ""
	@echo "⚙️  UTILITIES:"
	@echo "  make status  - Check service status"
	@echo "  make get-token - Get auth token"
	@echo "  make clean   - Clean everything"

# ====================
# MAIN TARGETS
# ====================

# Run all tests first, then start services
all: auth-test crypto-test auth-run-background crypto-run-background gateway-run-background
	@echo ""
	@echo "✅ All services started and tested!"
	@echo "🌐 Services running:"
	@echo "  - Auth:    http://localhost:5000"
	@echo "  - Crypto:  http://localhost:8002"
	@echo "  - Gateway: http://localhost:8003"
	@echo ""
	@echo "🔑 To get auth token: make get-token"
	@echo "🛑 To stop all: make status"
	@echo "🛑 To stop all: make stop"

# Alias
run: all

# Run all tests
test: auth-test crypto-test
	@echo "✅ All tests completed!"

# Stop all services
stop: auth-stop crypto-stop gateway-stop
	@echo "✅ All services stopped!"

# Start core services
core: auth-run-background crypto-run-background gateway-run-background
	@echo "✅ Core services started (Auth + Crypto + Gateway)"

# ====================
# AUTH SERVICE (.NET)
# ====================

# Test auth service
auth-test:
	@echo "🔐 Testing Auth Service..."
	@cd auth-service-dotnet.Tests && $(MAKE) test

# Start auth service in foreground
auth-run:
	@echo "🔐 Starting Auth Service on port 5000 (foreground)..."
	@echo "Note: This will block. Press Ctrl+C to stop."
	@cd auth-service-dotnet && dotnet run --urls "http://localhost:5000"

# Start auth service in background
auth-run-background:
	@echo "🔐 Starting Auth Service on port 5000 (background)..."
	@cd auth-service-dotnet && cmd /c start "Auth Service" run-background.bat
	@timeout /t 7 /nobreak > nul
	@echo "Checking if auth service is ready..."
	@curl -s http://localhost:5000/health >nul 2>&1 && echo "✅ Auth service is ready!" || echo "⚠️  Auth service might be starting..."

# Stop auth service
auth-stop:
	@echo "🔐 Stopping Auth Service..."
	@taskkill /F /FI "WINDOWTITLE eq Auth Service*" 2>nul || echo "No auth service running"
	@echo "✅ Auth service stopped"

# ====================
# CRYPTO SERVICE (Python)
# ====================

# Test crypto service (requires auth)
crypto-test: auth-run-background
	@echo "🔒 Testing Crypto Service..."
	@timeout /t 3 /nobreak > nul
	@cd crypto-service-python && $(MAKE) test

# Start crypto service (foreground)
crypto-run:
	@echo "🔒 Starting Crypto Service on port 8002 (foreground)..."
	@cd crypto-service-python && $(MAKE) run

# Start crypto service (background)
crypto-run-background:
	@echo "🔒 Starting Crypto Service on port 8002 (background)..."
	@cd crypto-service-python && $(MAKE) run-background

# Stop crypto service
crypto-stop:
	@echo "🔒 Stopping Crypto Service..."
	@-cd crypto-service-python && $(MAKE) stop 2>nul || echo "No stop target or service not running"

# ====================
# API GATEWAY (.NET)
# ====================

# Start API gateway in foreground
gateway-run:
	@echo "🌐 Starting API Gateway on port 8003 (foreground)..."
	@cd api-gateway-dotnet && $(MAKE) run

# Start API gateway in background
gateway-run-background:
	@echo "🌐 Starting API Gateway on port 8003 (background)..."
	@cd api-gateway-dotnet && $(MAKE) run-background

# Stop API gateway
gateway-stop:
	@echo "🌐 Stopping API Gateway..."
	@cd api-gateway-dotnet && $(MAKE) stop

# ====================
# UTILITIES
# ====================

# Check service status
status:
	@echo "📊 Service Status:"
	@curl -s -o nul -w "Auth (5000):    %%{http_code}" http://localhost:5000/health 2>nul || echo "Auth (5000):    DOWN"
	@echo ""
	@curl -s -o nul -w "Crypto (8002):  %%{http_code}" http://localhost:8002/health 2>nul || echo "Crypto (8002):  DOWN"
	@echo ""
	@curl -s -o nul -w "Gateway (8003): %%{http_code}" http://localhost:8003/health 2>nul || echo "Gateway (8003): DOWN"

# Get auth token
get-token:
	@echo "🔑 Getting auth token..."
	@curl -s -X POST http://localhost:5000/api/auth/login \
		-H "Content-Type: application/json" \
		-d "{\"email\":\"admin@example.com\",\"password\":\"admin123\"}" 2>nul || \
		echo "⚠️  Auth service not running. Start with: make auth-run-background"

# Clean everything
clean: stop
	@echo "🧹 Cleaning up..."
	@-cd crypto-service-python && $(MAKE) clean 2>nul || echo "Crypto clean skipped"
	@-cd auth-service-dotnet.Tests && $(MAKE) clean 2>nul || echo "Auth test clean skipped"
	@-cd auth-service-dotnet && dotnet clean 2>nul || echo "Auth clean skipped"
	@-cd api-gateway-dotnet && $(MAKE) clean 2>nul || echo "Gateway clean skipped"
	@echo "✅ Cleanup complete!"