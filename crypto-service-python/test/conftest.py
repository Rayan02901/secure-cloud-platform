import os
import sys
from pathlib import Path
from dotenv import load_dotenv

# Add project root to Python path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

# Load environment variables
load_dotenv()

# If FERNET_KEY is not set, use a valid one for testing
if not os.getenv("FERNET_KEY"):
    # This is a valid Fernet key: 32 'A's encoded in base64
    os.environ["FERNET_KEY"] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="