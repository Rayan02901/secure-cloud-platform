from .config import cipher

def encrypt_data(data: str) -> str:
    encrypted = cipher.encrypt(data.encode())
    return encrypted.decode()

def decrypt_data(token: str) -> str:
    decrypted = cipher.decrypt(token.encode())
    return decrypted.decode()
