import os
from dotenv import load_dotenv
from cryptography.fernet import Fernet

# Load environment variables from .env file
load_dotenv()

def get_cipher():
    key = os.getenv("FERNET_KEY")
    if not key:
        raise RuntimeError("FERNET_KEY environment variable is not set. "
                          "Please set it in your .env file or environment variables.")
    
    # Validate the key
    try:
        return Fernet(key.encode())
    except ValueError as e:
        raise RuntimeError(f"Invalid FERNET_KEY: {e}. "
                          "Key must be 32 url-safe base64-encoded bytes.")
    except Exception as e:
        raise RuntimeError(f"Failed to initialize cipher: {e}")

# Create cipher instance (will fail early if key is invalid)
try:
    cipher = get_cipher()
except RuntimeError as e:
    print(f"Configuration error: {e}")
    cipher = None