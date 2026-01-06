import os
from cryptography.fernet import Fernet

FERNET_KEY = os.getenv("FERNET_KEY")

if not FERNET_KEY:
    raise RuntimeError("FERNET_KEY is not set")

cipher = Fernet(FERNET_KEY.encode())
