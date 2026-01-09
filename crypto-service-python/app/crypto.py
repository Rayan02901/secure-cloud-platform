# crypto.py
from cryptography.fernet import InvalidToken
from fastapi import HTTPException
from .config import cipher
from .logger import setup_logger

# Setup logger for this module
logger = setup_logger(__name__)

logger.info("Crypto module initialized")
logger.debug(f"Cipher instance available: {cipher is not None}")


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
    logger.debug(f"encrypt_data called with data length: {len(data)}")
    
    # Validate input
    if not data:
        logger.warning("Encryption attempted with empty data")
        raise HTTPException(
            status_code=400,
            detail="Data to encrypt cannot be empty"
        )
    
    # Check if cipher is configured
    if cipher is None:
        logger.error("Cipher not configured - encryption cannot proceed")
        raise HTTPException(
            status_code=503,
            detail="Encryption service not properly configured"
        )
    
    try:
        encrypted = cipher.encrypt(data.encode())
        result = encrypted.decode()
        logger.info(f"Successfully encrypted data (result length: {len(result)})")
        return result
    except Exception as e:
        logger.error(f"Encryption failed: {type(e).__name__}: {str(e)}", exc_info=True)
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
    logger.debug(f"decrypt_data called with token length: {len(token)}")
    
    # Validate input
    if not token:
        logger.warning("Decryption attempted with empty token")
        raise HTTPException(
            status_code=400,
            detail="Token to decrypt cannot be empty"
        )
    
    # Check if cipher is configured
    if cipher is None:
        logger.error("Cipher not configured - decryption cannot proceed")
        raise HTTPException(
            status_code=503,
            detail="Decryption service not properly configured"
        )
    
    try:
        decrypted = cipher.decrypt(token.encode())
        result = decrypted.decode()
        logger.info(f"Successfully decrypted data (result length: {len(result)})")
        return result
    except InvalidToken:
        logger.warning("Decryption failed due to invalid or tampered token")
        raise HTTPException(
            status_code=400,
            detail="Invalid or tampered ciphertext"
        )
    except Exception as e:
        logger.error(f"Decryption failed: {type(e).__name__}: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=500,
            detail=f"Decryption failed: {str(e)}"
        )