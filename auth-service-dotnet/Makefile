auth-run-background:
	@echo "🔐 Starting Auth Service on port 5000 (background)..."
	@cd auth-service-dotnet && start "Auth Service" cmd /c "run-background.bat"
	@timeout /t 7 /nobreak > nul
	@echo "Checking if auth service is ready..."
	@curl -s http://localhost:5000/health >nul 2>&1 && echo "✅ Auth service is ready!" || echo "⚠️  Auth service might be starting..."