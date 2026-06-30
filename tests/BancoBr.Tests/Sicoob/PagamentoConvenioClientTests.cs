using System.Net;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Convenios;
using BancoBr.API.Sicoob.Pagamentos.Convenios.Models;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoConvenioClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/convenios-pagamentos/v2");

        private static PagamentoConvenioClient CriarClient(FakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoConvenioClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        private static PagamentoConvenioClient CriarClient(SequencedFakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoConvenioClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        [Fact]
        public async Task ConsultarCodigoBarrasAsync_200_RetornaDadosDoConvenio()
        {
            var json = @"
            {
              ""resultado"": {
                ""convenio"": ""1"",
                ""siglaConvenio"": ""SiG"",
                ""valorDocumento"": 1171.23,
                ""valorDesconto"": 11.23,
                ""valorMulta"": 17.23,
                ""valorJuros"": 1.71,
                ""valorOutrosEncargos"": 0,
                ""valorTotal"": 1171.23,
                ""codigoConvenioFebraban"": ""0025"",
                ""nsu"": 183390172928
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarCodigoBarrasAsync("00000000000000000000000000000000000000000000", new DateTime(2026, 6, 29));

            Assert.Equal("SiG", resultado.SiglaConvenio);
            Assert.Equal(183390172928, resultado.Nsu);
            Assert.Equal("fake-client-id", handler.LastRequest!.Headers.GetValues("client_id").Single());
        }

        [Fact]
        public async Task PagarConvenioAsync_200_RetornaArrecadacaoEfetivada()
        {
            var json = @"
            {
              ""resultado"": {
                ""comprovante"": ""PCFbQ0RBVEFb"",
                ""arrecadacao"": {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""valorDesconto"": 11.23,
                  ""valorJuros"": 1.71,
                  ""valorMulta"": 17.23,
                  ""autenticacao"": ""71205202-2DBB-46C7-BA31-0DFB8DC64EBE"",
                  ""recebimentoViaCaixa"": false
                }
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var request = new ArrecadacaoPagamentoRequest
            {
                Identificacao = new Identificacao { Instituicao = 1234, Unidade = 0 },
                Pagamento = new PagamentoConvenio
                {
                    ValorPago = 1171.23m,
                    DataPagamento = new DateTime(2026, 6, 29),
                    ValorDocumento = 1171.23m,
                    ValorDesconto = 11.23m,
                    ValorJuros = 1.71m,
                    ValorMulta = 17.23m,
                },
                Transacao = 123456789,
            };

            var resultado = await client.PagarConvenioAsync("00000000000000000000000000000000000000000000", request);

            Assert.False(resultado.PendenteAssinatura);
            Assert.NotNull(resultado.Resultado);
            Assert.Equal("71205202-2DBB-46C7-BA31-0DFB8DC64EBE", resultado.Resultado.Arrecadacao.Autenticacao);
            Assert.Contains("\"instituicao\":1234", handler.LastRequestBody);
            Assert.Contains("2026-06-29", handler.LastRequestBody);
        }

        [Fact]
        public async Task PagarConvenioAsync_202_RetornaPendenteAssinatura()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Accepted);
            var client = CriarClient(handler);
            var request = new ArrecadacaoPagamentoRequest
            {
                Identificacao = new Identificacao { Instituicao = 1234, Unidade = 0 },
                Pagamento = new PagamentoConvenio { DataPagamento = new DateTime(2026, 6, 29) },
                Transacao = 123456789,
            };

            var resultado = await client.PagarConvenioAsync("00000000000000000000000000000000000000000000", request);

            Assert.True(resultado.PendenteAssinatura);
            Assert.Null(resultado.Resultado);
        }

        [Fact]
        public async Task PagarConvenioAsync_400_LancaSicoobApiException()
        {
            var json = @"
            {
              ""mensagens"": [ { ""mensagem"": ""Convênio não habilitado."", ""codigo"": ""20011"" } ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, json);
            var client = CriarClient(handler);
            var request = new ArrecadacaoPagamentoRequest
            {
                Identificacao = new Identificacao { Instituicao = 1234, Unidade = 0 },
                Pagamento = new PagamentoConvenio { DataPagamento = new DateTime(2026, 6, 29) },
                Transacao = 123456789,
            };

            var ex = await Assert.ThrowsAsync<SicoobApiException>(() =>
                client.PagarConvenioAsync("00000000000000000000000000000000000000000000", request));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("20011", ex.Mensagens.Single().Codigo);
        }

        [Fact]
        public async Task ConsultarPagamentosAsync_200_MapeiaSituacaoEListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""situacao"": { ""codigo"": 0, ""descricao"": ""Recebido"" },
                  ""convenio"": ""string"",
                  ""siglaConvenio"": ""string"",
                  ""transacao"": 1234569789
                }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarPagamentosAsync("00000000000000000000000000000000000000000000", 1234, new DateTime(2026, 6, 29));

            Assert.Single(resultado);
            Assert.Equal("Recebido", resultado[0].Situacao.Descricao);
        }

        [Fact]
        public async Task ConsultarComprovantePorNsuAsync_200_RetornaComprovante()
        {
            var json = @"
            {
              ""resultado"": {
                ""comprovante"": ""PCFbQ0RBVEFb"",
                ""pagamento"": {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""autenticacao"": ""71205202-2DBB-46C7-BA31-0DFB8DC64EBE""
                }
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarComprovantePorNsuAsync(183390172928, 1234);

            Assert.Equal("PCFbQ0RBVEFb", resultado.Comprovante);
            Assert.Equal("71205202-2DBB-46C7-BA31-0DFB8DC64EBE", resultado.Pagamento.Autenticacao);
        }

        [Fact]
        public async Task ConsultarConciliacoesAsync_200_RetornaListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                { ""situacao"": ""Aceito"", ""convenio"": ""1"", ""siglaConvenio"": ""SiG"", ""valorTotal"": 1171.23, ""quantidade"": 3 }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarConciliacoesAsync(1234, new DateTime(2026, 6, 29));

            Assert.Single(resultado);
            Assert.Equal("Aceito", resultado[0].Situacao);
            Assert.Equal(3, resultado[0].Quantidade);
        }

        [Fact]
        public async Task ConsultarConveniosHabilitadosAsync_200_RetornaListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                { ""identificador"": ""0183390172928"", ""sigla"": ""Sigla - 001"", ""codigoFebraban"": ""0006"", ""segmento"": 4 }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarConveniosHabilitadosAsync(123456789, 1234);

            Assert.Single(resultado);
            Assert.Equal("Sigla - 001", resultado[0].Sigla);
        }

        [Fact]
        public async Task ConsultarCodigoBarrasAsync_401_RenovaTokenERepeteChamadaUmaVez()
        {
            const string json = @"
            {
              ""resultado"": {
                ""convenio"": ""1"",
                ""siglaConvenio"": ""SiG"",
                ""valorDocumento"": 1171.23,
                ""valorTotal"": 1171.23
              }
            }";
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.Unauthorized, null),
                (HttpStatusCode.OK, json));
            var client = CriarClient(handler);

            var resultado = await client.ConsultarCodigoBarrasAsync("00000000000000000000000000000000000000000000", new DateTime(2026, 6, 29));

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("SiG", resultado!.SiglaConvenio);
        }
    }
}
