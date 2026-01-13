import axios from "axios";

const api = axios.create({
  baseURL: "", // Your API base URL
  withCredentials: true // CRITICAL: Enables cookie sending/receiving
});

// REMOVE the token interceptor completely - HttpOnly cookies can't be accessed by JavaScript
// The browser automatically handles the cookie with withCredentials: true

// Optional: Add response interceptor for authentication errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid
      // You might want to redirect to login or refresh token
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export default api;