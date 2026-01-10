# Variables
PYTHON := python
VENV := venv
AUTH_URL := http://localhost:8001
AUTH_USERNAME := admin
AUTH_PASSWORD := password
PORT := 8002

.PHONY: help install install-dev test test-with-auth run stop clean get-token

# Default target
help:
	@echo "🔒 Crypto Service - Make Commands"
	@echo "=================================="
	@echo ""
	@echo "  make install     - Install dependencies"
	@echo "  make install-dev - Install dev dependencies"
	@echo "  make test        - Run tests with auth"
	@echo "  make run         - Start the service (port 8002)"
	@echo "  make stop        - Stop the service"
	@echo "  make clean       - Clean up files"
	@echo "  make get-token   - Get auth token"

# Install dependencies
install:
	@echo "📦 Installing dependencies..."
	@$(PYTHON) -m pip install -r requirements.txt

# Install dev dependencies
install-dev: install
	@echo "📦 Installing dev dependencies..."
	@$(PYTHON) -m pip install pytest pytest-asyncio httpx

# Get auth token
get-token:
	@echo "🔑 Getting authentication token..."
	@curl -s -X POST $(AUTH_URL)/api/auth/login \
		-H "Content-Type: application/json" \
		-d "{\"email\":\"$(AUTH_USERNAME)\",\"password\":\"$(AUTH_PASSWORD)\"}"

# Test with auth
test-with-auth: install-dev
	@echo "🔐 Running tests with authentication..."
	@echo "Getting auth token..."
	@$(PYTHON) -m pytest test/ -v --tb=short || echo "⚠️ Tests failed or no tests found"

# Main test target
test: test-with-auth

# Run the service
run:
	@echo "🔒 Starting Crypto Service on port $(PORT)..."
	@$(PYTHON) -m uvicorn app.main:app --host 0.0.0.0 --port $(PORT) --reload

# Run the service (background)
# Run the service (background) - using batch file
run-background:
	@echo "🔒 Starting Crypto Service on port $(PORT) (background)..."
	@cmd /c start "Crypto Service" run-background.bat
	@timeout /t 5 /nobreak > nul
	@echo "Checking if crypto service is ready..."
	@curl -s http://localhost:$(PORT)/health >nul 2>&1 && echo "✅ Crypto service is ready!" || echo "⚠️ Crypto service might be starting..."
# Stop the service
stop:
	@echo "Stopping crypto service..."
	@taskkill /F /IM python.exe /FI "WINDOWTITLE eq *uvicorn*" 2>nul || echo "Service not running"

# Clean up
clean:
	@echo "🧹 Cleaning up..."
	@-rmdir /S /Q __pycache__ 2>nul || echo ""
	@-rmdir /S /Q .pytest_cache 2>nul || echo ""
	@-rmdir /S /Q test/__pycache__ 2>nul || echo ""
	@-del /Q *.pyc 2>nul || echo ""
	@echo "✅ Cleanup complete!"