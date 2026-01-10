#test_api.py
import os
import requests
import pytest
import sys
from pathlib import Path
from fastapi.testclient import TestClient
from unittest.mock import patch
from cryptography.fernet import Fernet

# Add project root to Python path
sys.path.insert(0, str(Path(__file__).parent.parent))

# Import the app
from app.main import app

# Generate a test key
TEST_KEY = Fernet.generate_key()
TEST_CIPHER = Fernet(TEST_KEY)

client = TestClient(app)

# Get auth token from environment or make request
def get_auth_token():
    token = os.getenv("AUTH_TOKEN")
    if not token:
        # Try to get token from auth service
        try:
            response = requests.post(
                "http://localhost:5000/api/auth/login",
                json={"email": "admin@example.com", "password": "admin123"}
            )
            if response.status_code == 200:
                token = response.json()["token"]
        except:
            token = "mock_token_for_testing"
    return token

# Add auth header to your test requests
def auth_headers():
    return {"Authorization": f"Bearer {get_auth_token()}"}

class TestHealthEndpoint:
    def test_health_check(self):
        """Test health check endpoint."""
        response = client.get("/health")
        
        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["service"] == "crypto-service"
        assert "timestamp" in data

class TestEncryptEndpoint:
    def test_encrypt_valid_data(self):
        """Test encrypt endpoint with valid data."""
        test_data = {"plaintext": "Hello, World!"}
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            response = client.post("/encrypt", json=test_data)
            
            assert response.status_code == 200
            data = response.json()
            assert "ciphertext" in data
            
            # Verify the ciphertext can be decrypted
            decrypted = TEST_CIPHER.decrypt(data["ciphertext"].encode())
            assert decrypted.decode() == test_data["plaintext"]
    
    def test_encrypt_empty_data(self):
        """Test encrypt endpoint with empty data."""
        test_data = {"plaintext": ""}
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            response = client.post("/encrypt", json=test_data)
            
            assert response.status_code == 400
            data = response.json()
            assert "detail" in data
    
    def test_encrypt_missing_field(self):
        """Test encrypt endpoint with missing required field."""
        test_data = {}
        
        response = client.post("/encrypt", json=test_data)
        assert response.status_code == 422  # Validation error

class TestDecryptEndpoint:
    def test_decrypt_valid_ciphertext(self):
        """Test decrypt endpoint with valid ciphertext."""
        plaintext = "Hello, World!"
        ciphertext = TEST_CIPHER.encrypt(plaintext.encode()).decode()
        test_data = {"ciphertext": ciphertext}
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            response = client.post("/decrypt", json=test_data)
            
            assert response.status_code == 200
            data = response.json()
            assert data["plaintext"] == plaintext
    
    def test_decrypt_invalid_ciphertext(self):
        """Test decrypt endpoint with invalid ciphertext."""
        test_data = {"ciphertext": "invalid_ciphertext"}
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            response = client.post("/decrypt", json=test_data)
            
            assert response.status_code == 400
            data = response.json()
            assert "detail" in data
    
    def test_decrypt_empty_ciphertext(self):
        """Test decrypt endpoint with empty ciphertext."""
        test_data = {"ciphertext": ""}
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            response = client.post("/decrypt", json=test_data)
            
            assert response.status_code == 400
            data = response.json()
            assert "detail" in data
    
    def test_decrypt_missing_field(self):
        """Test decrypt endpoint with missing required field."""
        test_data = {}
        
        response = client.post("/decrypt", json=test_data)
        assert response.status_code == 422  # Validation error

class TestIntegration:
    def test_encrypt_decrypt_roundtrip(self):
        """Test complete encrypt-decrypt cycle."""
        original_text = "This is a secret message!"
        
        # FIXED: Use 'app.crypto.cipher' not 'crypto.cipher'
        with patch('app.crypto.cipher', TEST_CIPHER):
            # Encrypt
            encrypt_response = client.post("/encrypt", json={"plaintext": original_text})
            assert encrypt_response.status_code == 200
            ciphertext = encrypt_response.json()["ciphertext"]
            
            # Decrypt
            decrypt_response = client.post("/decrypt", json={"ciphertext": ciphertext})
            assert decrypt_response.status_code == 200
            decrypted_text = decrypt_response.json()["plaintext"]
            
            # Verify
            assert decrypted_text == original_text