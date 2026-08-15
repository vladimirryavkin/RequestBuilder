using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.X509CertificateUtils
{
    public class CertificateGenerator
    {
        public X509Certificate2 CreateSelfSignedCertificate(string commonName, int keySize = 2048, int validDays = 365)
        {
            using (var rsa = RSA.Create(keySize))
            {
                // Create a CertificateRequest object
                var request = new CertificateRequest(
                    $"CN={commonName}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add certificate extensions (optional)
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, false)); // Not a CA
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                        false));

                var notBefore = DateTimeOffset.UtcNow;
                var notAfter = DateTime.UtcNow.AddDays(validDays);

                // Create the self-signed certificate
                var certificate = request.CreateSelfSigned(notBefore, notAfter);
                return certificate;
            }
        }

        public X509Certificate2 CreateIntermediateCertificate(
            string subjectName,
            X509Certificate2 issuerCert,
            int keySize = 2048,
            int validDays = 365
        )
        {
            using (var rsa = RSA.Create(keySize))
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName($"CN={subjectName}"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Required extensions for an intermediate CA
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                var notBefore = DateTimeOffset.UtcNow;
                var notAfter = notBefore.AddDays(validDays);

                using var issuerKey = issuerCert.GetRSAPrivateKey();
                if (issuerKey == null)
                    throw new InvalidOperationException("Issuer certificate must have a private key.");

                var intermediateCert = request.Create(
                    issuerCert,
                    notBefore,
                    notAfter,
                    Guid.NewGuid().ToByteArray());

                return intermediateCert.CopyWithPrivateKey(rsa);
            }
        }


        public X509Certificate2 CreateChainedCertificate(string distinguishedName, X509Certificate2 rootCert, int keySize = 2048, int hours = 365)
        {
            using (var rsa = RSA.Create(keySize))
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName(distinguishedName),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add extensions
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // Extract root private key
                using var rootPrivateKey = rootCert.GetRSAPrivateKey();
                if (rootPrivateKey == null)
                    throw new InvalidOperationException("Root certificate does not have a private key.");

                // Define validity period
                var notBefore = DateTimeOffset.UtcNow;
                var notAfter = notBefore.AddHours(hours);

                // Sign the certificate with the root's private key
                var signedCert = request.Create(
                    rootCert,
                    notBefore,
                    notAfter,
                    Guid.NewGuid().ToByteArray());

                // Combine with private key
                return signedCert.CopyWithPrivateKey(rsa);
            }
        }

        public string ExportPrivateKey(X509Certificate2 cert)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            var sb = new StringBuilder();

            if (cert.HasPrivateKey)
            {
                // Export private key in PKCS#8 format
                var pkcs8PrivateKey = cert.GetRSAPrivateKey()?.ExportPkcs8PrivateKey();
                if (pkcs8PrivateKey != null)
                {
                    sb.AppendLine("-----BEGIN PRIVATE KEY-----");
                    sb.AppendLine(Convert.ToBase64String(pkcs8PrivateKey, Base64FormattingOptions.InsertLineBreaks));
                    sb.AppendLine("-----END PRIVATE KEY-----");
                }
            }
            else
            {
                throw new Errors.NotAllowedException("Cert doesn't have a private key");
            }
            return sb.ToString();
        }

        public string ExportPublicKey(X509Certificate2 cert)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            var sb = new StringBuilder();

            // ----- Certificate -----
            sb.AppendLine("-----BEGIN CERTIFICATE-----");
            sb.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END CERTIFICATE-----");

            return sb.ToString();
        }

        public string ExportToPem(X509Certificate2 cert)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            var sb = new StringBuilder();

            // ----- Certificate -----
            sb.AppendLine("-----BEGIN CERTIFICATE-----");
            sb.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END CERTIFICATE-----");

            // ----- Private Key (optional) -----
            if (cert.HasPrivateKey)
            {
                // Export private key in PKCS#8 format
                var pkcs8PrivateKey = cert.GetRSAPrivateKey()?.ExportPkcs8PrivateKey();
                if (pkcs8PrivateKey != null)
                {
                    sb.AppendLine("-----BEGIN PRIVATE KEY-----");
                    sb.AppendLine(Convert.ToBase64String(pkcs8PrivateKey, Base64FormattingOptions.InsertLineBreaks));
                    sb.AppendLine("-----END PRIVATE KEY-----");
                }
            }

            return sb.ToString();
        }
    }
}
