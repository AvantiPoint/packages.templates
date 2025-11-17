// Authentication helper for JWT token management
(function() {
    'use strict';

    let refreshTokenTimeout;

    // Initialize authentication on page load
    function initializeAuth() {
        const accessToken = localStorage.getItem('access_token');
        if (accessToken) {
            scheduleTokenRefresh();
        }
    }

    // Schedule automatic token refresh before expiration
    function scheduleTokenRefresh() {
        // Clear any existing timeout
        if (refreshTokenTimeout) {
            clearTimeout(refreshTokenTimeout);
        }

        const accessToken = localStorage.getItem('access_token');
        if (!accessToken) {
            return;
        }

        try {
            // Decode JWT to get expiration time
            const tokenData = parseJwt(accessToken);
            const expiresAt = tokenData.exp * 1000; // Convert to milliseconds
            const now = Date.now();
            const timeUntilExpiry = expiresAt - now;

            // Refresh 1 minute before expiration
            const refreshTime = Math.max(0, timeUntilExpiry - 60000);

            refreshTokenTimeout = setTimeout(() => {
                refreshAccessToken();
            }, refreshTime);

            console.log(`Token refresh scheduled in ${Math.floor(refreshTime / 1000)} seconds`);
        } catch (error) {
            console.error('Error scheduling token refresh:', error);
        }
    }

    // Parse JWT token
    function parseJwt(token) {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));

            return JSON.parse(jsonPayload);
        } catch (error) {
            console.error('Error parsing JWT:', error);
            return null;
        }
    }

    // Refresh the access token
    async function refreshAccessToken() {
        const refreshToken = localStorage.getItem('refresh_token');
        if (!refreshToken) {
            console.log('No refresh token available');
            return;
        }

        try {
            const response = await fetch('/api/authentication/refresh', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ refreshToken: refreshToken })
            });

            if (response.ok) {
                const data = await response.json();
                
                // Update stored tokens
                localStorage.setItem('access_token', data.accessToken);
                localStorage.setItem('refresh_token', data.refreshToken);
                
                // Update cookies
                document.cookie = `access_token=${data.accessToken}; path=/; max-age=900; SameSite=Lax`;
                document.cookie = `refresh_token=${data.refreshToken}; path=/; max-age=604800; SameSite=Lax`;

                console.log('Access token refreshed successfully');
                
                // Schedule next refresh
                scheduleTokenRefresh();
            } else {
                console.error('Failed to refresh token:', response.status);
                
                // If refresh fails, clear tokens and redirect to home
                localStorage.clear();
                window.location.href = '/';
            }
        } catch (error) {
            console.error('Error refreshing token:', error);
        }
    }

    // Add Authorization header to fetch requests
    const originalFetch = window.fetch;
    window.fetch = function(url, options) {
        options = options || {};
        const accessToken = localStorage.getItem('access_token');
        
        if (accessToken && !options.skipAuth) {
            options.headers = options.headers || {};
            if (!options.headers['Authorization']) {
                options.headers['Authorization'] = `Bearer ${accessToken}`;
            }
        }
        
        return originalFetch(url, options);
    };

    // Check if user is authenticated
    window.isAuthenticated = function() {
        const accessToken = localStorage.getItem('access_token');
        if (!accessToken) {
            return false;
        }

        try {
            const tokenData = parseJwt(accessToken);
            const expiresAt = tokenData.exp * 1000;
            return Date.now() < expiresAt;
        } catch {
            return false;
        }
    };

    // Get current user info from token
    window.getCurrentUser = function() {
        const accessToken = localStorage.getItem('access_token');
        if (!accessToken) {
            return null;
        }

        try {
            const tokenData = parseJwt(accessToken);
            return {
                email: tokenData.email || tokenData['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
                name: tokenData.name || tokenData['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
                isAdmin: localStorage.getItem('is_admin') === 'true'
            };
        } catch {
            return null;
        }
    };

    // Initialize on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAuth);
    } else {
        initializeAuth();
    }
})();
