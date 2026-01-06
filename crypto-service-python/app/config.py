# app/config.py
import os
from dotenv import load_dotenv
from cryptography.fernet import Fernet

# Load environment variables from .env file
load_dotenv()

def get_cipher():
    key = os.getenv("FERNET_KEY")
    if not key:
        raise RuntimeError("FERNET_KEY environment variable is not set")
    
    # Validate the key
    try:
        return Fernet(key.encode())
    except Exception as e:
        raise RuntimeError(f"Invalid FERNET_KEY: {e}")

cipher = get_cipher()