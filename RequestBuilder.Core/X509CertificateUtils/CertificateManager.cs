using System;
using System.Security.Cryptography.X509Certificates;

namespace RequestBuilder.X509CertificateUtils
{
    public class CertificateManager
    {
        public byte[] ConvertCertificateToPfxBytes(X509Certificate2 certificate, string password)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("Certificate must include a private key.");

            var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
            return pfxBytes;
        }

        public bool IsCertificateSignedBy(X509Certificate2 leafCert, X509Certificate2 issuerCert)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.ExtraStore.Add(issuerCert);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            var isValid = chain.Build(leafCert);

            // Check if the chain ends with the expected issuer
            if (isValid && chain.ChainElements.Count > 1)
            {
                var actualIssuer = chain.ChainElements[1].Certificate;
                return actualIssuer.Thumbprint == issuerCert.Thumbprint;
            }

            return false;
        }

        public bool IsCertificateExpired(X509Certificate2 cert)
        {
            var now = DateTime.UtcNow;
            var utcNotBefore = cert.NotBefore.ToUniversalTime();
            var utcNotAfter = cert.NotAfter.ToUniversalTime();
            return now < utcNotBefore || now > utcNotAfter;
        }

        public  X509Certificate2 LoadCertificateFromPfx(string filePath, string password)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must be provided.", nameof(password));

            return new X509Certificate2(filePath, password,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet | 
                X509KeyStorageFlags.Exportable);
        }

    }
}
