using System.Net;
using BancoBr.API.Core.Models;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoBoletoClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pagamentos/v3");

        private static PagamentoBoletoClient CriarClient(FakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoBoletoClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        private static PagamentoBoletoClient CriarClient(SequencedFakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoBoletoClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        [Fact]
        public async Task ConsultarBoletoAsync_200_RetornaIdentificadorConsulta()
        {
            var json = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""nomeInstituicaoEmissora"": ""Banco Cooperativo do Brasil"",
                ""codigoBarras"": ""00000000000000000000000000000000000000000000"",
                ""numeroLinhaDigitavel"": ""string"",
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 0,
                ""valorPagamento"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""permiteAlterarValor"": true,
                ""consultaEmContingencia"": false,
                ""identificadorConsulta"": ""hash-123"",
                ""descricaoInstrucaoValorMinMax"": ""string"",
                ""bloquearPagamento"": false
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletoAsync("00000000000000000000000000000000000000000000", 1234569);

            Assert.Equal("hash-123", resultado.IdentificadorConsulta);
            Assert.Equal(756, resultado.NumeroInstituicaoEmissora);
            Assert.Equal("fake-client-id", handler.LastRequest!.Headers.GetValues("client_id").Single());
        }

        [Fact]
        public async Task ConsultarBoletoAsync_204_RetornaNull()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletoAsync("00000000000000000000000000000000000000000000", 1234569);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task PagarBoletoAsync_200_RetornaComprovanteEfetivado()
        {
            var json = @"
            {
              ""resultado"": {
                ""numeroAgencia"": ""0001-9"",
                ""numeroConta"": 1234569,
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimento"": ""2018-09-20"",
                ""valorBoleto"": 100.36,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 60.36,
                ""valorPagamento"": 255.63,
                ""dataPagamento"": ""2019-10-20"",
                ""situacaoPagamento"": ""Efetivado"",
                ""idPagamento"": 1983450,
                ""numeroAutenticacaoPagamento"": ""89C3E9FD-1A37-40BE-A85B-69AF118D336A""
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var request = new BoletoPagamentoRequest
            {
                IdentificadorConsulta = "hash-123",
                ValorBoleto = 100.36m,
                Amount = 255.63m,
                Date = new DateTime(2019, 10, 20),
                NumeroCpfCnpjPortador = "12345678900",
                NomePortador = "Rosa Maria da Silva",
                AceitaValorDivergente = true,
                DebtorAccount = new DebtorAccount { Issuer = 1234, Number = 1234569, AccountType = 0, PersonType = 0 },
            };

            var resultado = await client.PagarBoletoAsync("00000000000000000000000000000000000000000000", request, IdempotencyKey.New("lancamento-1"));

            Assert.False(resultado.PendenteAssinatura);
            Assert.NotNull(resultado.Comprovante);
            Assert.Equal(1983450, resultado.Comprovante.IdPagamento);
            Assert.Equal("Efetivado", resultado.Comprovante.SituacaoPagamento);
            Assert.Contains("\"identificadorConsulta\":\"hash-123\"", handler.LastRequestBody);
            Assert.Contains("2019-10-20", handler.LastRequestBody);
        }

        [Fact]
        public async Task PagarBoletoAsync_202_RetornaPendenteAssinatura()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Accepted);
            var client = CriarClient(handler);
            var request = new BoletoPagamentoRequest { DebtorAccount = new DebtorAccount() };

            var resultado = await client.PagarBoletoAsync("00000000000000000000000000000000000000000000", request, IdempotencyKey.New("lancamento-1"));

            Assert.True(resultado.PendenteAssinatura);
            Assert.Null(resultado.Comprovante);
        }

        [Fact]
        public async Task PagarBoletoAsync_400_LancaSicoobApiException()
        {
            var json = @"
            {
              ""mensagens"": [ { ""mensagem"": ""Saldo insuficiente para o lançamento."", ""codigo"": ""10013"" } ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, json);
            var client = CriarClient(handler);
            var request = new BoletoPagamentoRequest { DebtorAccount = new DebtorAccount() };

            var ex = await Assert.ThrowsAsync<SicoobApiException>(() =>
                client.PagarBoletoAsync("00000000000000000000000000000000000000000000", request, IdempotencyKey.New("lancamento-1")));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("10013", ex.Mensagens.Single().Codigo);
        }

        [Fact]
        public async Task CancelarAgendamentoAsync_204_NaoLancaExcecao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
            var client = CriarClient(handler);

            await client.CancelarAgendamentoAsync(1983450, 1234569);

            Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        }

        [Fact]
        public async Task ConsultarBoletosDdaAsync_200_MapeiaSituacaoEListaDeItens()
        {
            var json = @"
            [
              {
                ""valorBoleto"": 100.0,
                ""dataVencimentoBoleto"": ""2026-06-27"",
                ""codigoTipoSituacaoBoleto"": 1,
                ""dataEmissao"": ""2026-06-01"",
                ""numeroCodigoBarras"": ""string""
              }
            ]";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletosDdaAsync(
                1234569,
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 30),
                SituacaoBoletoEnum.EmAberto,
                TipoDataConsultaEnum.Vencimento);

            Assert.Single(resultado);
            Assert.Equal(1, resultado[0].CodigoTipoSituacaoBoleto);
        }

        [Fact]
        public void IdempotencyKey_New_CombinaIdLancamentoComAcao()
        {
            Assert.Equal("lancamento-42-INCLUSAO", IdempotencyKey.New("lancamento-42"));
            Assert.Equal("lancamento-42-CANCELAMENTO", IdempotencyKey.New("lancamento-42", "CANCELAMENTO"));
        }

        [Fact]
        public void IdempotencyKey_New_MesmoLancamentoMesmaAcaoGeraMesmaKey()
        {
            var key1 = IdempotencyKey.New("lancamento-42");
            var key2 = IdempotencyKey.New("lancamento-42");

            Assert.Equal(key1, key2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IdempotencyKey_New_SemIdLancamento_LancaArgumentException(string? idLancamento)
        {
            Assert.Throws<ArgumentException>(() => IdempotencyKey.New(idLancamento!));
        }

        private const string ConsultaJsonNaoBloqueado = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 0,
                ""valorPagamento"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""identificadorConsulta"": ""hash-123"",
                ""bloquearPagamento"": false
              }
            }";

        private const string ConsultaJsonBloqueado = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""identificadorConsulta"": ""hash-123"",
                ""bloquearPagamento"": true,
                ""mensagemBloqueioPagamento"": ""Pagamento bloqueado""
              }
            }";

        private const string ComprovanteJson = @"
            {
              ""resultado"": {
                ""idPagamento"": 1983450,
                ""situacaoPagamento"": ""Efetivado""
              }
            }";

        [Fact]
        public async Task PagarBoletoComConsultaAsync_ConsultaOk_ConsultaEPagaEm2Chamadas()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado),
                (HttpStatusCode.OK, ComprovanteJson));
            var client = CriarClient(handler);

            var resultado = await client.PagarBoletoComConsultaAsync(
                "00000000000000000000000000000000000000000000",
                numeroConta: 1234569,
                numeroAgencia: 4342,
                idLancamento: "lancamento-1",
                numeroCpfCnpjPortador: "12345678900",
                nomePortador: "Rosa Maria da Silva");

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
            Assert.False(resultado.BoletoNaoEncontrado);
            Assert.False(resultado.PagamentoBloqueado);
            Assert.NotNull(resultado.Comprovante);
            Assert.Equal(1983450, resultado.Comprovante.IdPagamento);
        }

        [Fact]
        public async Task PagarBoletoComConsultaAsync_BoletoNaoEncontrado_NaoChamaPagamento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.NoContent, null));
            var client = CriarClient(handler);

            var resultado = await client.PagarBoletoComConsultaAsync(
                "00000000000000000000000000000000000000000000",
                numeroConta: 1234569,
                numeroAgencia: 4342,
                idLancamento: "lancamento-1",
                numeroCpfCnpjPortador: "12345678900",
                nomePortador: "Rosa Maria da Silva");

            Assert.Single(handler.Requests);
            Assert.True(resultado.BoletoNaoEncontrado);
            Assert.Null(resultado.Comprovante);
        }

        [Fact]
        public async Task PagarBoletoComConsultaAsync_PagamentoBloqueado_NaoChamaPagamento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, ConsultaJsonBloqueado));
            var client = CriarClient(handler);

            var resultado = await client.PagarBoletoComConsultaAsync(
                "00000000000000000000000000000000000000000000",
                numeroConta: 1234569,
                numeroAgencia: 4342,
                idLancamento: "lancamento-1",
                numeroCpfCnpjPortador: "12345678900",
                nomePortador: "Rosa Maria da Silva");

            Assert.Single(handler.Requests);
            Assert.True(resultado.PagamentoBloqueado);
            Assert.Equal("Pagamento bloqueado", resultado.MensagemBloqueio);
            Assert.Null(resultado.Comprovante);
        }

        [Fact]
        public async Task ConsultarBoletoAsync_401_RenovaTokenERepeteChamadaUmaVez()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.Unauthorized, null),
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado));
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletoAsync("00000000000000000000000000000000000000000000", 1234569);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("hash-123", resultado!.IdentificadorConsulta);
        }

        [Fact]
        public async Task PagarLoteBoletosAsync_ItemComErro_NaoInterrompeOsDemais()
        {
            const string erroJson = @"
                {
                  ""mensagens"": [ { ""mensagem"": ""Saldo insuficiente para o lançamento."", ""codigo"": ""10013"" } ]
                }";

            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado),  // item 1: consulta
                (HttpStatusCode.OK, ComprovanteJson),           // item 1: pagamento
                (HttpStatusCode.OK, ConsultaJsonBloqueado),     // item 2: consulta (bloqueado, sem 2ª chamada)
                (HttpStatusCode.BadRequest, erroJson));         // item 3: consulta falha

            var client = CriarClient(handler);
            var itens = new[]
            {
                new PagamentoBoletoLoteItem { CodigoBarras = "boleto-1", NumeroConta = 1234569, NumeroAgencia = 4342, IdLancamento = "lancamento-1", NumeroCpfCnpjPortador = "12345678900", NomePortador = "Item 1" },
                new PagamentoBoletoLoteItem { CodigoBarras = "boleto-2", NumeroConta = 1234569, NumeroAgencia = 4342, IdLancamento = "lancamento-2", NumeroCpfCnpjPortador = "12345678900", NomePortador = "Item 2" },
                new PagamentoBoletoLoteItem { CodigoBarras = "boleto-3", NumeroConta = 1234569, NumeroAgencia = 4342, IdLancamento = "lancamento-3", NumeroCpfCnpjPortador = "12345678900", NomePortador = "Item 3" },
            };

            var resultados = await client.PagarLoteBoletosAsync(itens);

            Assert.Equal(3, resultados.Count);

            Assert.True(resultados[0].Sucesso);
            Assert.NotNull(resultados[0].Resultado?.Comprovante);

            Assert.True(resultados[1].Sucesso);
            Assert.True(resultados[1].Resultado!.PagamentoBloqueado);

            Assert.False(resultados[2].Sucesso);
            Assert.IsType<SicoobApiException>(resultados[2].Erro);
            Assert.Equal("10013", ((SicoobApiException)resultados[2].Erro).Mensagens.Single().Codigo);
        }
    }
}
