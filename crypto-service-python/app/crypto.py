import os
from cryptography.fernet import Fernet, InvalidToken
from fastapi import HTTPException

# Initialize cipher from environment variable
fernet_key = os.getenv("FERNET_KEY")
cipher = Fernet(fernet_key.encode()) if fernet_key else None


def encrypt_data(data: str) -> str:
    """
    Encrypt plaintext data using Fernet symmetric encryption.
    
    Args:
        data: The plaintext string to encrypt
        
    Returns:
        Base64-encoded ciphertext as a string
        
    Raises:
        HTTPException: If encryption fails or cipher is not configured
    """
    # Validate input
    if not data:
        raise HTTPException(
            status_code=400,
            detail="Data to encrypt cannot be empty"
        )
    
    # Check if cipher is configured
    if cipher is None:
        raise HTTPException(
            status_code=503,
            detail="Encryption service not properly configured"
        )
    
    try:
        encrypted = cipher.encrypt(data.encode())
        return encrypted.decode()
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Encryption failed: {str(e)}"
        )


def decrypt_data(token: str) -> str:
    """
    Decrypt ciphertext using Fernet symmetric encryption.
    
    Args:
        token: The base64-encoded ciphertext to decrypt
        
    Returns:
        Decrypted plaintext as a string
        
    Raises:
        HTTPException: If decryption fails or cipher is not configured
    """
    # Validate input
    if not token:
        raise HTTPException(
            status_code=400,
            detail="Token to decrypt cannot be empty"
        )
    
    # Check if cipher is configured
    if cipher is None:
        raise HTTPException(
            status_code=503,
            detail="Decryption service not properly configured"
        )
    
    try:
        decrypted = cipher.decrypt(token.encode())
        return decrypted.decode()
    except InvalidToken:
        raise HTTPException(
            status_code=400,
            detail="Invalid or tampered ciphertext"
        )
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Decryption failed: {str(e)}"
        )