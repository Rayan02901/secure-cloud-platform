# Variables - Read from appsettings.json if available
APPSETTINGS := appsettings.json
PORT := 8000

# Try to read port from appsettings.json (fallback to 8000)
ifneq (,$(wildcard $(APPSETTINGS)))
    PORT := $(shell powershell -Command "$$json = Get-Content $(APPSETTINGS) | ConvertFrom-Json; if ($$json.Kestrel.Endpoints.Http.Url -match ':(\d+)') { $$matches[1] } else { '8000' }")
endif

.PHONY: help build run run-background stop clean test restore watch

# Default target
help:
	@echo "🌐 API Gateway Service - Make Commands"
	@echo "======================================"
	@echo ""
	@echo "  make build           - Build the service"
	@echo "  make run             - Run in foreground (port $(PORT))"
	@echo "  make run-background  - Run in background (port $(PORT))"
	@echo "  make stop            - Stop the service"
	@echo "  make test            - Run tests"
	@echo "  make restore         - Restore dependencies"
	@echo "  make clean           - Clean build artifacts"
	@echo "  make watch           - Run in watch mode"
	@echo "  make show-config     - Show current configuration"

# Show current configuration
show-config:
	@echo "📋 Current Configuration:"
	@echo "  Port: $(PORT)"
	@echo "  Config file: $(APPSETTINGS)"

# Restore dependencies
restore:
	@echo "📦 Restoring dependencies..."
	dotnet restore

# Build the project
build: restore
	@echo "🔨 Building API Gateway..."
	dotnet build --no-restore

# Run in foreground
run: build
	@echo "🌐 Starting API Gateway on port $(PORT) (foreground)..."
	@echo "Press Ctrl+C to stop"
	dotnet run

# Run in background
run-background:
	@echo "🌐 Starting API Gateway on port $(PORT) (background)..."
	@cmd /c start "API Gateway" run-background.bat
	@timeout /t 5 /nobreak > nul
	@echo "Checking if API Gateway is ready..."
	@curl -s http://localhost:$(PORT)/health >nul 2>&1 && echo "✅ API Gateway is ready!" || echo "⚠️  API Gateway might be starting..."

# Stop the service
stop:
	@echo "🛑 Stopping API Gateway..."
	@taskkill /F /FI "WINDOWTITLE eq API Gateway*" 2>nul || echo "No API Gateway process found"
	@echo "✅ API Gateway stopped"

# Run tests
test:
	@echo "🧪 Running API Gateway tests..."
	dotnet test --no-build --verbosity normal

# Clean build artifacts
clean:
	@echo "🧹 Cleaning API Gateway..."
	dotnet clean
	@-rmdir /S /Q bin 2>nul || echo ""
	@-rmdir /S /Q obj 2>nul || echo ""
	@-del /Q api-gateway.log 2>nul || echo ""
	@echo "✅ Cleanup complete!"

# Watch mode for development
watch:
	@echo "👀 Running API Gateway in watch mode on port $(PORT)..."
	dotnet watch run