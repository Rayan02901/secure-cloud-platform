from fastapi import FastAPI, HTTPException, Depends, Request
from fastapi.middleware.cors import CORSMiddleware
from datetime import datetime, timezone
import logging
from .schemas import (
    EncryptRequest,
    EncryptResponse,
    DecryptRequest,
    DecryptResponse,
    HealthResponse
)
from .crypto import encrypt_data, decrypt_data
from .security import verify_token

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] [%(levelname)s] [%(name)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)
logger = logging.getLogger(__name__)

print("[MAIN] Initializing FastAPI application...")

app = FastAPI(
    title="Crypto Service API",
    description="A secure encryption/decryption microservice using Fernet symmetric encryption with JWT authentication",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

print("[MAIN] FastAPI app created")

# Add CORS middleware (adjust origins as needed)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Restrict this in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.on_event("startup")
async def startup_event():
    """Run on application startup."""
    logger.info("Application startup event triggered")
    logger.info("Checking crypto module status...")
    from .crypto import cipher
    logger.info(f"Cipher available: {cipher is not None}")
    
    logger.info("Checking JWT configuration...")
    from .config import JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE
    logger.info(f"JWT_SECRET available: {JWT_SECRET is not None}")
    logger.info(f"JWT_ISSUER: {JWT_ISSUER}")
    logger.info(f"JWT_AUDIENCE: {JWT_AUDIENCE}")

@app.middleware("http")
async def log_requests(request: Request, call_next):
    """Middleware to log all requests."""
    logger.info(f"Request: {request.method} {request.url}")
    logger.debug(f"Headers: {dict(request.headers)}")
    
    response = await call_next(request)
    
    logger.info(f"Response: {response.status_code}")
    return response

@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint for service monitoring (no authentication required)."""
    logger.info("Health check endpoint called")
    try:
        # Test encryption/decryption as part of health check
        test_data = "health_check_test"
        encrypted = encrypt_data(test_data)
        decrypted = decrypt_data(encrypted)
        
        if decrypted != test_data:
            raise HTTPException(status_code=503, detail="Crypto self-test failed")
            
        return HealthResponse(
            status="healthy",
            service="crypto-service",
            timestamp=datetime.now(timezone.utc),
            version="1.0.0",
            details={"crypto_test": "passed"}
        )
    except Exception as e:
        logger.error(f"Health check failed: {str(e)}")
        return HealthResponse(
            status="unhealthy",
            service="crypto-service",
            timestamp=datetime.now(timezone.utc),
            version="1.0.0",
            error=str(e)
        )

@app.post("/encrypt", response_model=EncryptResponse)
async def encrypt(
    req: EncryptRequest,
    token: dict = Depends(verify_token)
):
    """
    Encrypt plaintext data.
    Requires valid JWT token in Authorization header.
    """
    logger.info(f"/encrypt endpoint called by user: {token.get('sub', 'unknown')}")
    logger.debug(f"Request plaintext length: {len(req.plaintext)}")
    
    try:
        ciphertext = encrypt_data(req.plaintext)
        logger.info("Encryption successful")
        return EncryptResponse(ciphertext=ciphertext)
    except HTTPException as e:
        logger.error(f"HTTPException in encrypt: {e.status_code} - {e.detail}")
        raise
    except Exception as e:
        logger.error(f"Unexpected error in encrypt: {type(e).__name__}: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal server error during encryption: {str(e)}"
        )

@app.post("/decrypt", response_model=DecryptResponse)
async def decrypt(
    req: DecryptRequest,
    token: dict = Depends(verify_token)
):
    """
    Decrypt ciphertext data.
    Requires valid JWT token in Authorization header.
    """
    logger.info(f"/decrypt endpoint called by user: {token.get('sub', 'unknown')}")
    logger.debug(f"Request ciphertext length: {len(req.ciphertext)}")
    
    try:
        plaintext = decrypt_data(req.ciphertext)
        logger.info("Decryption successful")
        return DecryptResponse(plaintext=plaintext)
    except HTTPException as e:
        logger.error(f"HTTPException in decrypt: {e.status_code} - {e.detail}")
        raise
    except Exception as e:
        logger.error(f"Unexpected error in decrypt: {type(e).__name__}: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal server error during decryption: {str(e)}"
        )

# Optional: Add metrics endpoint
@app.get("/metrics")
async def metrics():
    """Simple metrics endpoint."""
    return {
        "service": "crypto-service",
        "uptime": datetime.now(timezone.utc) - app.startup_time if hasattr(app, 'startup_time') else "unknown",
        "endpoints": ["/health", "/encrypt", "/decrypt", "/docs", "/redoc"]
    }