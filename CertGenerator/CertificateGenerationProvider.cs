
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertGenerator
{
    public class CertificateGenerationProvider
    {
        public void GenerateRootCertificate(CertificateConfiguration settings)//1:36
        {
            SecureRandom secRand = new SecureRandom();
            RsaKeyPairGenerator keyGen = new RsaKeyPairGenerator();
            RsaKeyGenerationParameters prms = new RsaKeyGenerationParameters(new Org.BouncyCastle.Math.BigInteger("10001", 16), secRand, 1024, 80);
            keyGen.Init(prms);
            AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();

            string issuer = "CN=" + settings.CertName;

            //  Определим имена файлов
            string p12FileName = settings.OutFolder + @"\" + settings.CertName + ".p12";
            string crtFileName = settings.OutFolder + @"\" + settings.CertName + ".crt";

            //Серейный номер сертификата
            byte[] serialNumber = Guid.NewGuid().ToByteArray();
            serialNumber[0] = (byte)(serialNumber[0] & 0x7F);

            X509V3CertificateGenerator certGen = new X509V3CertificateGenerator();
            certGen.SetSerialNumber(new Org.BouncyCastle.Math.BigInteger(1, serialNumber));
            certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name(issuer));
            certGen.SetNotBefore(DateTime.Now.ToUniversalTime());
            certGen.SetNotAfter(DateTime.Now.ToUniversalTime() + new TimeSpan(settings.CertDuration * 365, 0, 0, 0));
            certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name(issuer));
            certGen.SetPublicKey(keyPair.Public);
            certGen.SetSignatureAlgorithm("MD5WITHRSA");
            certGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
                new AuthorityKeyIdentifierStructure(keyPair.Public));
            certGen.AddExtension(X509Extensions.SubjectKeyIdentifier, false,
                new SubjectKeyIdentifierStructure(keyPair.Public));
            certGen.AddExtension(X509Extensions.BasicConstraints, false,
                new BasicConstraints(true));

            Org.BouncyCastle.X509.X509Certificate rootCert = certGen.Generate(keyPair.Private);

            // Получим подписанный сертификат
            byte[] rawCert = rootCert.GetEncoded();

            // Сохраним закрытую часть сертификата
            try
            {
                using (FileStream fs = new FileStream(p12FileName, FileMode.Create))
                {
                    Pkcs12Store p12 = new Pkcs12Store();
                    X509CertificateEntry certEntry = new X509CertificateEntry(rootCert);
                    p12.SetKeyEntry(settings.CertName, new AsymmetricKeyEntry(keyPair.Private),
                        new X509CertificateEntry[] { certEntry });
                    p12.Save(fs, settings.Password.ToCharArray(), secRand);
                    fs.Close();
                }

            }
            catch (Exception exception)
            {
                // При сохранении сертификата произошла ошибка 
                throw new CertificateGenerationException("При сохранении закрытой части сертификата произошла ошибка.\r\n" +
                    exception.Message);

            }

            // Сохраним открытую часть  сертификата 

            try
            {
                using (FileStream fs = new FileStream(crtFileName, FileMode.Create))
                {
                    fs.Write(rawCert, 0, rawCert.Length);
                    fs.Close();
                }

            }
            catch (Exception exception)
            {
                // При сохранении сертификата произошла ошибка
                throw new CertificateGenerationException("При сохранении открытой части сертификата произошла ошибка.\r\n" +
                    exception.Message);
            }

        }

    }
}
