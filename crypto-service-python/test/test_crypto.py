import pytest
import sys
from pathlib import Path
from unittest.mock import patch, MagicMock
from cryptography.fernet import Fernet, InvalidToken
from fastapi import HTTPException

# Add project root to Python path so we can import app modules
sys.path.insert(0, str(Path(__file__).parent.parent))

# Now import from app.crypto
from app.crypto import encrypt_data, decrypt_data

# Generate a test key for unit tests
TEST_KEY = Fernet.generate_key()
TEST_CIPHER = Fernet(TEST_KEY)

class TestEncryptData:
    def test_encrypt_valid_data(self):
        """Test encrypting valid data."""
        plaintext = "Hello, World!"
        
        with patch('app.crypto.cipher', TEST_CIPHER):
            ciphertext = encrypt_data(plaintext)
            
            # Verify encryption
            decrypted = TEST_CIPHER.decrypt(ciphertext.encode())
            assert decrypted.decode() == plaintext
    
    def test_encrypt_empty_data(self):
        """Test encrypting empty data raises HTTPException."""
        with patch('app.crypto.cipher', TEST_CIPHER):
            with pytest.raises(HTTPException) as exc_info:
                encrypt_data("")
            
            assert exc_info.value.status_code == 400
            assert "cannot be empty" in exc_info.value.detail
    
    def test_encrypt_when_cipher_not_configured(self):
        """Test encryption when cipher is not configured."""
        with patch('app.crypto.cipher', None):
            with pytest.raises(HTTPException) as exc_info:
                encrypt_data("test")
            
            assert exc_info.value.status_code == 503
            assert "not properly configured" in exc_info.value.detail
    
    def test_encrypt_encryption_failure(self):
        """Test when encryption fails unexpectedly."""
        mock_cipher = MagicMock()
        mock_cipher.encrypt.side_effect = Exception("Encryption error")
        
        with patch('app.crypto.cipher', mock_cipher):
            with pytest.raises(HTTPException) as exc_info:
                encrypt_data("test")
            
            assert exc_info.value.status_code == 500
            assert "Encryption failed" in exc_info.value.detail

class TestDecryptData:
    def test_decrypt_valid_ciphertext(self):
        """Test decrypting valid ciphertext."""
        plaintext = "Hello, World!"
        ciphertext = TEST_CIPHER.encrypt(plaintext.encode()).decode()
        
        with patch('app.crypto.cipher', TEST_CIPHER):
            decrypted = decrypt_data(ciphertext)
            assert decrypted == plaintext
    
    def test_decrypt_empty_ciphertext(self):
        """Test decrypting empty ciphertext raises HTTPException."""
        with patch('app.crypto.cipher', TEST_CIPHER):
            with pytest.raises(HTTPException) as exc_info:
                decrypt_data("")
            
            assert exc_info.value.status_code == 400
            assert "cannot be empty" in exc_info.value.detail
    
    def test_decrypt_invalid_token(self):
        """Test decrypting invalid/tampered ciphertext."""
        invalid_ciphertext = "invalid_ciphertext"
        
        with patch('app.crypto.cipher', TEST_CIPHER):
            with pytest.raises(HTTPException) as exc_info:
                decrypt_data(invalid_ciphertext)
            
            assert exc_info.value.status_code == 400
            assert "Invalid or tampered" in exc_info.value.detail
    
    def test_decrypt_when_cipher_not_configured(self):
        """Test decryption when cipher is not configured."""
        with patch('app.crypto.cipher', None):
            with pytest.raises(HTTPException) as exc_info:
                decrypt_data("test")
            
            assert exc_info.value.status_code == 503
            assert "not properly configured" in exc_info.value.detail
    
    def test_decrypt_decryption_failure(self):
        """Test when decryption fails unexpectedly."""
        mock_cipher = MagicMock()
        mock_cipher.decrypt.side_effect = Exception("Decryption error")
        
        with patch('app.crypto.cipher', mock_cipher):
            with pytest.raises(HTTPException) as exc_info:
                decrypt_data("test")
            
            assert exc_info.value.status_code == 500
            assert "Decryption failed" in exc_info.value.detail