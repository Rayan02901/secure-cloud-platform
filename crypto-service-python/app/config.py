# config.py
import os
from dotenv import load_dotenv
from cryptography.fernet import Fernet

print("[CONFIG] Loading environment variables...")

# Load environment variables from .env file
load_dotenv()

print(f"[CONFIG] Environment variables loaded. Checking required secrets...")

# Check FERNET_KEY
fernet_key = os.getenv("FERNET_KEY")
print(f"[CONFIG] FERNET_KEY exists: {fernet_key is not None}")
if fernet_key:
    print(f"[CONFIG] FERNET_KEY length: {len(fernet_key)}")
    print(f"[CONFIG] FERNET_KEY first 10 chars: {fernet_key[:10]}...")
else:
    print("[CONFIG] ERROR: FERNET_KEY not found in environment!")

# Check JWT_SECRET
jwt_secret = os.getenv("JWT_SECRET")
print(f"[CONFIG] JWT_SECRET exists: {jwt_secret is not None}")
if jwt_secret:
    print(f"[CONFIG] JWT_SECRET length: {len(jwt_secret)}")
    print(f"[CONFIG] JWT_SECRET first 10 chars: {jwt_secret[:10]}...")
else:
    print("[CONFIG] ERROR: JWT_SECRET not found in environment!")

# JWT Configuration
JWT_ISSUER = "SecureCloudPlatform"
JWT_AUDIENCE = "SecureCloudClients"
print(f"[CONFIG] JWT_ISSUER: {JWT_ISSUER}")
print(f"[CONFIG] JWT_AUDIENCE: {JWT_AUDIENCE}")

def get_cipher():
    """Initialize and return Fernet cipher instance."""
    key = os.getenv("FERNET_KEY")
    if not key:
        print("[CONFIG] ERROR: FERNET_KEY environment variable is not set")
        raise RuntimeError("FERNET_KEY environment variable is not set. "
                         "Please set it in your .env file or environment variables.")
    
    # Validate the key
    try:
        cipher_instance = Fernet(key.encode())
        print("[CONFIG] Cipher initialized successfully")
        return cipher_instance
    except ValueError as e:
        print(f"[CONFIG] ERROR: Invalid FERNET_KEY format: {e}")
        raise RuntimeError(f"Invalid FERNET_KEY: {e}. "
                         "Key must be 32 url-safe base64-encoded bytes.")
    except Exception as e:
        print(f"[CONFIG] ERROR: Failed to initialize cipher: {e}")
        raise RuntimeError(f"Failed to initialize cipher: {e}")

def validate_secrets():
    """Validate that all required secrets are present."""
    fernet_key = os.getenv("FERNET_KEY")
    jwt_secret = os.getenv("JWT_SECRET")
    
    missing_secrets = []
    if not fernet_key:
        missing_secrets.append("FERNET_KEY")
    if not jwt_secret:
        missing_secrets.append("JWT_SECRET")
    
    if missing_secrets:
        error_msg = f"Required secrets are missing: {', '.join(missing_secrets)}"
        print(f"[CONFIG] ERROR: {error_msg}")
        raise RuntimeError(error_msg)
    
    print("[CONFIG] All required secrets are present")
    return True

# Validate secrets and create cipher instance
try:
    print("[CONFIG] Validating required secrets...")
    validate_secrets()
    
    print("[CONFIG] Attempting to create cipher instance...")
    cipher = get_cipher()
    print("[CONFIG] SUCCESS: Cipher instance created")
    
    # Export JWT_SECRET for use in other modules
    JWT_SECRET = os.getenv("JWT_SECRET")
    print("[CONFIG] JWT_SECRET exported for use")
    
except RuntimeError as e:
    print(f"[CONFIG] FAILED: Configuration error: {e}")
    cipher = None
    JWT_SECRET = None