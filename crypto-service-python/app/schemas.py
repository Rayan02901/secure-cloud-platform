from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class EncryptRequest(BaseModel):
    plaintext: str

    class Config:
        json_schema_extra = {
            "example": {
                "plaintext": "Hello, World!"
            }
        }

class EncryptResponse(BaseModel):
    ciphertext: str

    class Config:
        json_schema_extra = {
            "example": {
                "ciphertext": "gAAAAABl..."
            }
        }

class DecryptRequest(BaseModel):
    ciphertext: str

    class Config:
        json_schema_extra = {
            "example": {
                "ciphertext": "gAAAAABl..."
            }
        }

class DecryptResponse(BaseModel):
    plaintext: str

    class Config:
        json_schema_extra = {
            "example": {
                "plaintext": "Hello, World!"
            }
        }

class HealthResponse(BaseModel):
    status: str
    service: str
    timestamp: datetime
    version: str
    details: Optional[str] = None

    class Config:
        json_schema_extra = {
            "example": {
                "status": "healthy",
                "service": "crypto-service",
                "timestamp": "2024-01-01T12:00:00Z",
                "version": "1.0.0",
                "details": "Service is running normally"
            }
        }