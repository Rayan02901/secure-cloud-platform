import { useState } from "react";
import api from "../api/axios";

export default function Dashboard() {
  const [text, setText] = useState("");
  const [result, setResult] = useState("");

  const encrypt = async () => {
    const res = await api.post("/crypto/encrypt", { plaintext: text });
    setResult(res.data);
  };

  return (
    <div>
      <h2>Secure Crypto Dashboard</h2>
      <input placeholder="Text" onChange={e => setText(e.target.value)} />
      <button onClick={encrypt}>Encrypt</button>
      <pre>{JSON.stringify(result, null, 2)}</pre>
    </div>
  );
}
