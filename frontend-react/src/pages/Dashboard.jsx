import { useState, useEffect, useContext } from "react";
import api from "../api/axios";
import { AuthContext } from "../auth/AuthContext";
import { useNavigate } from "react-router-dom";

export default function Dashboard() {
  const [text, setText] = useState("");
  const [result, setResult] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  
  const { isAuthenticated, loading: authLoading } = useContext(AuthContext);
  const navigate = useNavigate();

  // Redirect if not authenticated
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate("/login");
    }
  }, [isAuthenticated, authLoading, navigate]);

  const encrypt = async () => {
    if (!text.trim()) {
      setError("Please enter text to encrypt");
      return;
    }

    setLoading(true);
    setError("");
    
    try {
      // Cookie is automatically sent with withCredentials: true
      const res = await api.post("/crypto/encrypt", { plaintext: text });
      setResult(res.data);
    } catch (err) {
      console.error("Encryption error:", err);
      
      if (err.response?.status === 401) {
        // Token expired or invalid - redirect to login
        setError("Session expired. Please login again.");
        setTimeout(() => navigate("/login"), 2000);
      } else if (err.response?.data?.error) {
        setError(`Encryption failed: ${err.response.data.error}`);
      } else {
        setError("Encryption failed. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  };

  // Show loading while checking auth
  if (authLoading) {
    return (
      <div style={{ textAlign: "center", marginTop: "50px" }}>
        <p>Checking authentication...</p>
      </div>
    );
  }

  // Should not reach here if not authenticated due to useEffect redirect
  if (!isAuthenticated) {
    return null; // Redirect will happen
  }

  return (
    <div style={{ maxWidth: "600px", margin: "30px auto", padding: "20px" }}>
      <h2>Secure Crypto Dashboard</h2>
      <p>Welcome! Your authentication cookie is automatically sent with each request.</p>
      
      {error && (
        <div style={{ 
          color: "red", 
          background: "#ffe6e6", 
          padding: "10px", 
          marginBottom: "15px",
          borderRadius: "4px"
        }}>
          {error}
        </div>
      )}
      
      <div style={{ marginBottom: "20px" }}>
        <input 
          placeholder="Enter text to encrypt" 
          value={text}
          onChange={e => setText(e.target.value)}
          style={{ 
            width: "100%", 
            padding: "10px", 
            boxSizing: "border-box",
            marginBottom: "10px"
          }}
          disabled={loading}
        />
        
        <button 
          onClick={encrypt} 
          disabled={loading || !text.trim()}
          style={{ 
            padding: "10px 20px", 
            background: loading ? "#ccc" : "#28a745",
            color: "white",
            border: "none",
            borderRadius: "4px",
            cursor: loading ? "not-allowed" : "pointer"
          }}
        >
          {loading ? "Encrypting..." : "Encrypt"}
        </button>
      </div>
      
      {result && (
        <div>
          <h3>Encryption Result:</h3>
          <pre style={{ 
            background: "#f8f9fa", 
            padding: "15px", 
            borderRadius: "4px",
            overflow: "auto",
            whiteSpace: "pre-wrap"
          }}>
            {JSON.stringify(result, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
}