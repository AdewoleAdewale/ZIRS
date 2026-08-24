using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Text;

namespace ZamfaraIRS.Services
{
    public static class SslHandler
    {
        public static void ConfigureSSL()
        {
            // ============ CHANGE: Add this to handle SSL certificates ============
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true; // Certificate is valid
                    }

                    // Log certificate errors
                    Debug.WriteLine($"[SSL VALIDATION] Error: {sslPolicyErrors}");
                    Debug.WriteLine($"[SSL CERT] Subject: {certificate?.Subject}");
                    Debug.WriteLine($"[SSL CERT] Issuer: {certificate?.Issuer}");

                    // Return true to accept the certificate
                    // NOTE: For production, implement certificate pinning instead
                    return true;
                };

            // ============ CHANGE: Update TLS version ============
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // ============ End TLS version update ============
        }
    }
}
