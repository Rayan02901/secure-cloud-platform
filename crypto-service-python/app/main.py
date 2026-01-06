from fastapi import FastAPI
from .schemas import (
    EncryptRequest,
    EncryptResponse,
    DecryptRequest,
    DecryptResponse
)
from .crypto import encrypt_data, decrypt_data

app = FastAPI(title="Crypto Service")

@app.post("/encrypt", response_model=EncryptResponse)
def encrypt(req: EncryptRequest):
    return {"ciphertext": encrypt_data(req.plaintext)}

@app.post("/decrypt", response_model=DecryptResponse)
def decrypt(req: DecryptRequest):
    return {"plaintext": decrypt_data(req.ciphertext)}
