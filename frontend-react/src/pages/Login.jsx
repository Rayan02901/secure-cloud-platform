import { useState, useContext } from "react";
import api from "../api/axios";
import { AuthContext } from "../auth/AuthContext";
import { useNavigate } from "react-router-dom";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  
  const { login, checkAuth } = useContext(AuthContext);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      // Step 1: Make login request
      const res = await api.post("/auth/login", { username, password });
      
      if (res.data.success) {
        // Step 2: Wait a moment for cookie to be set
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Step 3: Verify authentication
        try {
          await login(); // This calls checkAuth internally
          // Step 4: Navigate to home/dashboard
          navigate("/");
        } catch (authError) {
          setError("Login succeeded but authentication check failed");
        }
      } else {
        setError(res.data.message || "Login failed");
      }
    } catch (err) {
      console.error("Login error:", err);
      
      if (err.response?.status === 401) {
        setError("Invalid username or password");
      } else if (err.response?.data?.error) {
        setError(err.response.data.error);
      } else {
        setError("Login failed. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  };

  // Alternative: Direct form submission
  const handleSubmit = async (e) => {
    e.preventDefault();
    await handleLogin(e);
  };

  return (
    <div style={{ maxWidth: "400px", margin: "50px auto", padding: "20px" }}>
      <h2>Login</h2>
      
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
      
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: "15px" }}>
          <input 
            placeholder="Username" 
            value={username}
            onChange={e => setUsername(e.target.value)}
            style={{ width: "100%", padding: "8px", boxSizing: "border-box" }}
            required
            disabled={loading}
          />
        </div>
        
        <div style={{ marginBottom: "20px" }}>
          <input 
            type="password" 
            placeholder="Password" 
            value={password}
            onChange={e => setPassword(e.target.value)}
            style={{ width: "100%", padding: "8px", boxSizing: "border-box" }}
            required
            disabled={loading}
          />
        </div>
        
        <button 
          onClick={handleLogin} 
          disabled={loading || !username || !password}
          style={{ 
            width: "100%", 
            padding: "10px", 
            background: loading ? "#ccc" : "#007bff",
            color: "white",
            border: "none",
            borderRadius: "4px",
            cursor: loading ? "not-allowed" : "pointer"
          }}
        >
          {loading ? "Logging in..." : "Login"}
        </button>
      </form>
    </div>
  );
}