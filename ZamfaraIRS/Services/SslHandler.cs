using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ZamfaraIRS.Services
{
    public static class SslHandler
    {
        public static void ConfigureSSL()
        {
            try
            {
                // Enable TLS 1.2 and TLS 1.1 protocols
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
                                                       SecurityProtocolType.Tls11 |
                                                       SecurityProtocolType.Tls;

                // Trust all SSL certificates globally for legacy HttpWebRequest / WebClient
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SSL Configuration Error]: {ex.Message}");
            }
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Bypass all SSL certificate chain / hostname validation errors
            return true;
        }

        /// <summary>
        /// Provides an HttpClientHandler configured to bypass SSL certificate validation.
        /// </summary>
        public static HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
            };

            return handler;
        }

        /// <summary>
        /// Returns a pre-configured HttpClient that ignores SSL certificate validation.
        /// </summary>
        public static HttpClient GetInsecureHttpClient(TimeSpan? timeout = null)
        {
            var client = new HttpClient(GetInsecureHandler())
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            };

            return client;
        }
    }
}