# main.py
from fastapi import FastAPI, HTTPException, Depends
from datetime import datetime
from .schemas import (
    EncryptRequest,
    EncryptResponse,
    DecryptRequest,
    DecryptResponse,
    HealthResponse
)
from .crypto import encrypt_data, decrypt_data
from .security import verify_token

print("[MAIN] Initializing FastAPI application...")

app = FastAPI(
    title="Crypto Service API",
    description="A secure encryption/decryption microservice using Fernet symmetric encryption with JWT authentication",
    version="1.0.0"
)

print("[MAIN] FastAPI app created")

@app.on_event("startup")
async def startup_event():
    """Run on application startup."""
    print("[MAIN] Application startup event triggered")
    print("[MAIN] Checking crypto module status...")
    from .crypto import cipher
    print(f"[MAIN] Cipher available: {cipher is not None}")
    
    print("[MAIN] Checking JWT configuration...")
    from .config import JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE
    print(f"[MAIN] JWT_SECRET available: {JWT_SECRET is not None}")
    print(f"[MAIN] JWT_ISSUER: {JWT_ISSUER}")
    print(f"[MAIN] JWT_AUDIENCE: {JWT_AUDIENCE}")

@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint for service monitoring (no authentication required)."""
    print("[MAIN] Health check endpoint called")
    return HealthResponse(
        status="healthy",
        service="crypto-service",
        timestamp=datetime.utcnow(),
        version="1.0.0"
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
    print(f"[MAIN] /encrypt endpoint called")
    print(f"[MAIN] Authenticated user: {token.get('sub', 'unknown')}")
    print(f"[MAIN] Request plaintext length: {len(req.plaintext)}")
    
    try:
        ciphertext = encrypt_data(req.plaintext)
        print(f"[MAIN] Encryption successful, returning ciphertext")
        return EncryptResponse(ciphertext=ciphertext)
    except HTTPException as e:
        print(f"[MAIN] HTTPException raised: {e.status_code} - {e.detail}")
        raise
    except Exception as e:
        print(f"[MAIN] Unexpected error: {type(e).__name__}: {str(e)}")
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
    print(f"[MAIN] /decrypt endpoint called")
    print(f"[MAIN] Authenticated user: {token.get('sub', 'unknown')}")
    print(f"[MAIN] Request ciphertext length: {len(req.ciphertext)}")
    
    try:
        plaintext = decrypt_data(req.ciphertext)
        print(f"[MAIN] Decryption successful, returning plaintext")
        return DecryptResponse(plaintext=plaintext)
    except HTTPException as e:
        print(f"[MAIN] HTTPException raised: {e.status_code} - {e.detail}")
        raise
    except Exception as e:
        print(f"[MAIN] Unexpected error: {type(e).__name__}: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal server error during decryption: {str(e)}"
        )