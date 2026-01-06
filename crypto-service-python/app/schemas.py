from pydantic import BaseModel

class EncryptRequest(BaseModel):
    plaintext: str

class EncryptResponse(BaseModel):
    ciphertext: str

class DecryptRequest(BaseModel):
    ciphertext: str

class DecryptResponse(BaseModel):
    plaintext: str
