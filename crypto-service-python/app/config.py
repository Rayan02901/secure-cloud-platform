# config.py
import os
from dotenv import load_dotenv
from cryptography.fernet import Fernet
from .logger import setup_logger

# Setup logger for this module
logger = setup_logger(__name__)

logger.info("Loading environment variables...")

# Load environment variables from .env file
load_dotenv()

logger.info("Environment variables loaded. Checking required secrets...")

# Check FERNET_KEY
fernet_key = os.getenv("FERNET_KEY")
logger.debug(f"FERNET_KEY exists: {fernet_key is not None}")
if fernet_key:
    logger.debug(f"FERNET_KEY length: {len(fernet_key)}")
    logger.debug(f"FERNET_KEY first 10 chars: {fernet_key[:10]}...")
else:
    logger.error("FERNET_KEY not found in environment!")

# Check JWT_SECRET
jwt_secret = os.getenv("JWT_SECRET")
logger.debug(f"JWT_SECRET exists: {jwt_secret is not None}")
if jwt_secret:
    logger.debug(f"JWT_SECRET length: {len(jwt_secret)}")
    logger.debug(f"JWT_SECRET first 10 chars: {jwt_secret[:10]}...")
else:
    logger.error("JWT_SECRET not found in environment!")

# JWT Configuration
JWT_ISSUER = "SecureCloudPlatform"
JWT_AUDIENCE = "SecureCloudClients"
logger.info(f"JWT_ISSUER: {JWT_ISSUER}")
logger.info(f"JWT_AUDIENCE: {JWT_AUDIENCE}")

def get_cipher():
    """Initialize and return Fernet cipher instance."""
    key = os.getenv("FERNET_KEY")
    if not key:
        logger.error("FERNET_KEY environment variable is not set")
        raise RuntimeError("FERNET_KEY environment variable is not set. "
                         "Please set it in your .env file or environment variables.")
    
    # Validate the key
    try:
        cipher_instance = Fernet(key.encode())
        logger.info("Cipher initialized successfully")
        return cipher_instance
    except ValueError as e:
        logger.error(f"Invalid FERNET_KEY format: {e}")
        raise RuntimeError(f"Invalid FERNET_KEY: {e}. "
                         "Key must be 32 url-safe base64-encoded bytes.")
    except Exception as e:
        logger.error(f"Failed to initialize cipher: {e}")
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
        logger.error(error_msg)
        raise RuntimeError(error_msg)
    
    logger.info("All required secrets are present")
    return True

# Validate secrets and create cipher instance
try:
    logger.info("Validating required secrets...")
    validate_secrets()
    
    logger.info("Attempting to create cipher instance...")
    cipher = get_cipher()
    logger.info("SUCCESS: Cipher instance created")
    
    # Export JWT_SECRET for use in other modules
    JWT_SECRET = os.getenv("JWT_SECRET")
    logger.info("JWT_SECRET exported for use")
    
except RuntimeError as e:
    logger.critical(f"Configuration error: {e}")
    cipher = None
    JWT_SECRET = None