using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BancoBr.API.Base;
using BancoBr.API.Core;
using BancoBr.API.Core.Http;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.Common.Enums;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoBoletoApiTests
    {
        private static CertificateSource CriarCertificateSourceFake()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=fake", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificado = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            var pfxBytes = certificado.Export(X509ContentType.Pfx, "senha");
            return CertificateSource.FromPfxBytes(pfxBytes, "senha");
        }

        [Fact]
        public void Criar_BancoSicoob_RetornaPagamentoBoletoClient()
        {
            var resultado = BancoApi.Criar<PagamentoBoletoApiBase>(BancoEnum.Sicoob, "fake-client-id", CriarCertificateSourceFake(), new FakeOAuthTokenProvider());

            Assert.IsType<PagamentoBoletoClient>(resultado);
        }

        [Fact]
        public void Criar_BancoNaoImplementado_LancaException()
        {
            Assert.Throws<System.Exception>(() => BancoApi.Criar<PagamentoBoletoApiBase>(BancoEnum.BradescoSA, "fake-client-id", CriarCertificateSourceFake(), new FakeOAuthTokenProvider()));
        }
    }
}
