using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;


namespace Spectero.daemon.Libraries.Core.Crypto
{
    public interface ICryptoService
    {
        X509Certificate2 LoadCertificate(string issuerFileName, string password);
        X509Certificate2 IssueCertificate(string subjectName, X509Certificate2 issuerCertificate, string[] subjectAlternativeNames, KeyPurposeID[] usages);
        X509Certificate2 CreateCertificateAuthorityCertificate(string subjectName, string[] subjectAlternativeNames, KeyPurposeID[] usages);
        X509Certificate2 CreateSelfSignedCertificate(string subjectName, string[] subjectAlternativeNames, KeyPurposeID[] usages);
        SecureRandom GetSecureRandom();

        X509Certificate GenerateCertificate(SecureRandom random,
            string subjectName,
            AsymmetricCipherKeyPair subjectKeyPair,
            BigInteger subjectSerialNumber,
            string[] subjectAlternativeNames,
            string issuerName,
            AsymmetricCipherKeyPair issuerKeyPair,
            BigInteger issuerSerialNumber,
            bool isCertificateAuthority,
            KeyPurposeID[] usages);

        /// <summary>
        /// The certificate needs a serial number. This is used for revocation,
        /// and usually should be an incrementing index (which makes it easier to revoke a range of certificates).
        /// Since we don't have anywhere to store the incrementing index, we can just use a random number.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        BigInteger GenerateSerialNumber(SecureRandom random);

        /// <summary>
        /// Generate a key pair.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="strength">The key length in bits. For RSA, 2048 bits should be considered the minimum acceptable these days.</param>
        /// <returns></returns>
        AsymmetricCipherKeyPair GenerateKeyPair(SecureRandom random, int strength);

        /// <summary>
        /// Add the Authority Key Identifier. According to http://www.alvestrand.no/objectid/2.5.29.35.html, this
        /// identifies the public key to be used to verify the signature on this certificate.
        /// In a certificate chain, this corresponds to the "Subject Key Identifier" on the *issuer* certificate.
        /// The Bouncy Castle documentation, at http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation,
        /// shows how to create this from the issuing certificate. Since we're creating a self-signed certificate, we have to do this slightly differently.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="issuerDN"></param>
        /// <param name="issuerKeyPair"></param>
        /// <param name="issuerSerialNumber"></param>
        void AddAuthorityKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
            X509Name issuerDN,
            AsymmetricCipherKeyPair issuerKeyPair,
            BigInteger issuerSerialNumber);

        /// <summary>
        /// Add the "Subject Alternative Names" extension. Note that you have to repeat
        /// the value from the "Subject Name" property.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="subjectAlternativeNames"></param>
        void AddSubjectAlternativeNames(X509V3CertificateGenerator certificateGenerator,
            IEnumerable<string> subjectAlternativeNames);

        /// <summary>
        /// Add the "Extended Key Usage" extension, specifying (for example) "server authentication".
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="usages"></param>
        void AddExtendedKeyUsage(X509V3CertificateGenerator certificateGenerator, KeyPurposeID[] usages);

        /// <summary>
        /// Add the "Basic Constraints" extension.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="isCertificateAuthority"></param>
        void AddBasicConstraints(X509V3CertificateGenerator certificateGenerator,
            bool isCertificateAuthority);

        /// <summary>
        /// Add the Subject Key Identifier.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="subjectKeyPair"></param>
        void AddSubjectKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
            AsymmetricCipherKeyPair subjectKeyPair);

        X509Certificate2 ConvertCertificate(X509Certificate certificate,
            AsymmetricCipherKeyPair subjectKeyPair,
            SecureRandom random);

        void WriteCertificate(X509Certificate2 certificate, string outputFileName, string password = "password");

        X509Certificate2 LoadCertificate(byte[] certBytes, string password = "password");

        byte[] GetCertificateBytes(X509Certificate2 certificate, string password = "password");
    }
}