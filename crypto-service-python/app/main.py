from fastapi import FastAPI, HTTPException
from datetime import datetime
from .schemas import (
    EncryptRequest,
    EncryptResponse,
    DecryptRequest,
    DecryptResponse,
    HealthResponse
)
from .crypto import encrypt_data, decrypt_data

app = FastAPI(
    title="Crypto Service API",
    description="A secure encryption/decryption microservice using Fernet symmetric encryption",
    version="1.0.0"
)

@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint for service monitoring."""
    return HealthResponse(
        status="healthy",
        service="crypto-service",
        timestamp=datetime.utcnow(),
        version="1.0.0"
    )

@app.post("/encrypt", response_model=EncryptResponse)
async def encrypt(req: EncryptRequest):
    """Encrypt plaintext data."""
    ciphertext = encrypt_data(req.plaintext)
    return EncryptResponse(ciphertext=ciphertext)

@app.post("/decrypt", response_model=DecryptResponse)
async def decrypt(req: DecryptRequest):
    """Decrypt ciphertext data."""
    plaintext = decrypt_data(req.ciphertext)
    return DecryptResponse(plaintext=plaintext)