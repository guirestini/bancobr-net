using System.Net;
using BancoBr.API.Core.Errors;
using BancoBr.API.Santander.Cobranca.Boletos;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    public class CobrancaBoletoClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/collection_bill_management/v2");

        private const string Registrado = @"{
            ""bankNumber"": ""123"", ""barcode"": ""03396939700000001009356720600000000123450101"",
            ""digitableLine"": ""03399356782060000000201234501011693970000000100"", ""entryDate"": ""2022-12-12"",
            ""qrCodePix"": ""000201010212"", ""qrCodeUrl"": ""pix.santander.com.br/qr/v2/cobv/9fa03dbd""
        }";

        /// <summary>Resposta de GET /workspaces com uma única workspace de cobrança ativa que contém o convênio 1234567.</summary>
        private static string ListaComWorkspace(string status = "ACTIVE", string type = "BILLING", string convenio = "1234567") => $@"{{
            ""_pageable"": {{ ""_totalElements"": 1 }},
            ""_content"": [ {{
                ""workspace_id"": ""ws-cob-1"", ""status"": ""{status}"", ""type"": ""{type}"",
                ""covenants"": [ {{ ""code"": ""{convenio}"" }} ]
            }} ]
        }}";

        private static CobrancaBoletoClient CriarClient(HttpMessageHandler handler) =>
            new CobrancaBoletoClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);

        private static SequencedFakeHttpMessageHandler Sequencia(params (HttpStatusCode, string?)[] operacao) =>
            new SequencedFakeHttpMessageHandler(new (HttpStatusCode, string?)[] { (HttpStatusCode.OK, ListaComWorkspace()) }.Concat(operacao).ToArray());

        private static Correntista CriarBeneficiario() => new Correntista { Convenio = "1234567" };

        private static Movimento CriarMovimento() => new Movimento
        {
            ValorPagamento = 10.15m,
            NumeroDocumento = "NF10",
            Favorecido = new Favorecido
            {
                Nome = "João da Silva",
                CPF_CNPJ = "961.586.583-20",
                Endereco = "Rua XV de Maio",
                NumeroEndereco = "100",
                Bairro = "Vila Industrial",
                Cidade = "São Paulo",
                UF = "SP",
                CEP = 9761233,
            },
            MovimentoItem = new MovimentoItemBoletoCobranca
            {
                NossoNumero = "123",
                DataEmissao = new DateTime(2022, 12, 1),
                DataVencimento = new DateTime(2022, 12, 12),
            },
        };

        [Fact]
        public async Task EmitirBoletoAsync_EnviaCorpoEMapeiaResultado()
        {
            var handler = Sequencia((HttpStatusCode.Created, Registrado));
            var movimento = CriarMovimento();

            var resultado = await CriarClient(handler).EmitirBoletoAsync(movimento, CriarBeneficiario());

            Assert.Same(movimento, resultado);
            Assert.Equal(2, handler.Requests.Count);
            var post = handler.Requests[1];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-cob-1/bank_slips", post.RequestUri!.ToString());
            Assert.Equal("fake-client-id", post.Headers.GetValues("X-Application-Key").Single());
            Assert.Equal("Bearer", post.Headers.Authorization!.Scheme);

            var body = handler.Bodies[1]!;
            Assert.Contains(@"""environment"":""PRODUCAO""", body);
            Assert.Contains(@"""covenantCode"":""1234567""", body);
            Assert.Contains(@"""bankNumber"":""123""", body);
            Assert.Contains(@"""clientNumber"":""NF10""", body);
            Assert.Contains(@"""dueDate"":""2022-12-12""", body);
            Assert.Contains(@"""issueDate"":""2022-12-01""", body);
            Assert.Contains(@"""nominalValue"":""10.15""", body);
            Assert.Contains(@"""documentType"":""CPF""", body);
            Assert.Contains(@"""documentNumber"":""96158658320""", body);
            Assert.Contains(@"""address"":""Rua XV de Maio, 100""", body);
            Assert.Contains(@"""zipCode"":""09761-233""", body);
            Assert.Contains(@"""documentKind"":""DUPLICATA_MERCANTIL""", body);
            Assert.Contains(@"""discount"":{""type"":""ISENTO""}", body);
            Assert.Contains(@"""protestType"":""SEM_PROTESTO""", body);
            Assert.Contains(@"""paymentType"":""REGISTRO""", body);
            Assert.DoesNotContain("finePercentage", body);
            Assert.DoesNotContain("beneficiary", body);
            Assert.DoesNotContain(@"""key""", body);
            Assert.DoesNotContain("messages", body);

            Assert.Equal("123", resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Registrado, resultado.SituacaoBancoBr);
            Assert.Equal(new DateTime(2022, 12, 12), resultado.HorarioBancoBr);
            var item = (MovimentoItemBoletoCobranca)resultado.MovimentoItem;
            Assert.Equal("03396939700000001009356720600000000123450101", item.CodigoBarras);
            Assert.Equal("03399356782060000000201234501011693970000000100", item.LinhaDigitavel);
            Assert.Equal("000201010212", item.QrCodePix);
            Assert.Equal("pix.santander.com.br/qr/v2/cobv/9fa03dbd", item.UrlQrCodePix);
            Assert.Equal(new DateTime(2022, 12, 12), item.DataEntrada);
        }

        [Fact]
        public async Task EmitirBoletoAsync_CamposOpcionais_SaoEnviados()
        {
            var handler = Sequencia((HttpStatusCode.Created, Registrado));
            var movimento = CriarMovimento();
            var item = (MovimentoItemBoletoCobranca)movimento.MovimentoItem;
            item.TipoDesconto = TipoDescontoBoletoEnum.ValorDataFixa;
            item.Descontos = new List<DescontoBoleto> { new DescontoBoleto { Valor = 5.5m, DataLimite = new DateTime(2022, 12, 10) } };
            item.PercentualMulta = 2m;
            item.DiasMulta = 5;
            item.PercentualJuros = 1m;
            item.TipoProtesto = TipoProtestoBoletoEnum.DiasCorridos;
            item.DiasProtesto = 10;
            item.EspecieDocumento = EspecieDocumentoBoletoEnum.DuplicataServico;
            item.TipoChavePIX = FormaIniciacaoEnum.PIX_CPF_CNPJ;
            item.ChavePIX = "09463589723";
            item.Mensagens = new List<string> { "mensagem um" };
            item.Partilha = new List<PartilhaBoleto> { new PartilhaBoleto { Codigo = "12", Valor = 132.5m } };
            item.AmbienteTeste = true;

            await CriarClient(handler).EmitirBoletoAsync(movimento, CriarBeneficiario());

            var body = handler.Bodies[1]!;
            Assert.Contains(@"""environment"":""TESTE""", body);
            Assert.Contains(@"""discount"":{""type"":""VALOR_DATA_FIXA"",""discountOne"":{""value"":5.5,""limitDate"":""2022-12-10""}}", body);
            Assert.Contains(@"""finePercentage"":""2.00""", body);
            Assert.Contains(@"""fineQuantityDays"":""5""", body);
            Assert.Contains(@"""interestPercentage"":""1.00""", body);
            Assert.Contains(@"""protestType"":""DIAS_CORRIDOS""", body);
            Assert.Contains(@"""protestQuantityDays"":""10""", body);
            Assert.Contains(@"""documentKind"":""DUPLICATA_SERVICO""", body);
            Assert.Contains(@"""key"":{""type"":""CPF"",""dictKey"":""09463589723""}", body);
            Assert.Contains(@"""messages"":[""mensagem um""]", body);
            Assert.Contains(@"""sharing"":[{""code"":""12"",""value"":""132.50""}]", body);
        }

        [Fact]
        public async Task EmitirBoletoAsync_NsuGerado_ComoGravadoNoItemEReutilizadoNoRetry()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ListaComWorkspace()), (HttpStatusCode.Created, Registrado), (HttpStatusCode.Created, Registrado));
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            var item = (MovimentoItemBoletoCobranca)movimento.MovimentoItem;

            await client.EmitirBoletoAsync(movimento, CriarBeneficiario());

            Assert.Matches("^[A-Z0-9]{20}$", item.NsuCode);
            Assert.Equal(DateTime.Today, item.DataNsu);
            Assert.Contains($@"""nsuCode"":""{item.NsuCode}""", handler.Bodies[1]!);

            await client.EmitirBoletoAsync(movimento, CriarBeneficiario());

            Assert.Contains($@"""nsuCode"":""{item.NsuCode}""", handler.Bodies[2]!);
        }

        [Fact]
        public async Task EmitirBoletoAsync_AmbienteTeste_GeraNsuComPrefixoTst()
        {
            var handler = Sequencia((HttpStatusCode.Created, Registrado));
            var movimento = CriarMovimento();
            var item = (MovimentoItemBoletoCobranca)movimento.MovimentoItem;
            item.AmbienteTeste = true;

            await CriarClient(handler).EmitirBoletoAsync(movimento, CriarBeneficiario());

            Assert.Matches("^TST[A-Z0-9]{17}$", item.NsuCode);
        }

        [Fact]
        public async Task EmitirBoletoAsync_Workspace_FicaEmCache()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ListaComWorkspace()), (HttpStatusCode.Created, Registrado), (HttpStatusCode.Created, Registrado));
            var client = CriarClient(handler);

            await client.EmitirBoletoAsync(CriarMovimento(), CriarBeneficiario());
            await client.EmitirBoletoAsync(CriarMovimento(), CriarBeneficiario());

            Assert.Equal(3, handler.Requests.Count); // 1 listagem + 2 POSTs
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        }

        [Fact]
        public async Task EmitirBoletoAsync_WorkspacePaginada_ProcuraEmTodasAsPaginas()
        {
            var pagina1 = @"{ ""_pageable"": { ""_totalElements"": 2 }, ""_content"": [ { ""workspace_id"": ""ws-outra"", ""status"": ""ACTIVE"", ""type"": ""BILLING"", ""covenants"": [ { ""code"": ""999"" } ] } ] }";
            var pagina2 = @"{ ""_pageable"": { ""_totalElements"": 2 }, ""_content"": [ { ""workspace_id"": ""ws-cob-2"", ""status"": ""ACTIVE"", ""type"": ""BILLING"", ""covenants"": [ { ""code"": ""0001234567"" } ] } ] }";
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, pagina1), (HttpStatusCode.OK, pagina2), (HttpStatusCode.Created, Registrado));

            await CriarClient(handler).EmitirBoletoAsync(CriarMovimento(), CriarBeneficiario());

            Assert.Contains("_offset=1", handler.Requests[1].RequestUri!.Query);
            Assert.Equal($"{BaseUrl}/workspaces/ws-cob-2/bank_slips", handler.Requests[2].RequestUri!.ToString());
        }

        [Theory]
        [InlineData("INACTIVE", "BILLING", "1234567")]
        [InlineData("ACTIVE", "PAYMENTS", "1234567")]
        [InlineData("ACTIVE", "BILLING", "7654321")]
        public async Task EmitirBoletoAsync_SemWorkspaceDoConvenio_Lanca(string status, string type, string convenio)
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, ListaComWorkspace(status, type, convenio)));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CriarClient(handler).EmitirBoletoAsync(CriarMovimento(), CriarBeneficiario()));

            Assert.Contains("1234567", ex.Message);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task EmitirBoletoAsync_Erro400_LancaBancoApiException()
        {
            var handler = Sequencia((HttpStatusCode.BadRequest, @"{ ""_errorCode"": ""400"", ""_message"": ""Bad Request"", ""_errors"": [ { ""_code"": ""E1"", ""_field"": ""bankNumber"", ""_message"": ""já existe"" } ] }"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => CriarClient(handler).EmitirBoletoAsync(CriarMovimento(), CriarBeneficiario()));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Contains(ex.Mensagens, m => m.Codigo == "E1" && m.Mensagem == "bankNumber: já existe");
        }

        [Fact]
        public async Task EmitirBoletoAsync_ItemDeOutroTipo_Lanca()
        {
            var movimento = new Movimento { MovimentoItem = new MovimentoItemTransferenciaPIX() };

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarClient(new SequencedFakeHttpMessageHandler()).EmitirBoletoAsync(movimento, CriarBeneficiario()));
        }

        [Fact]
        public async Task EmitirBoletoAsync_SemBeneficiario_Lanca()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => CriarClient(new SequencedFakeHttpMessageHandler()).EmitirBoletoAsync(CriarMovimento(), null!));
        }

        [Theory]
        [InlineData("convenio-invalido")]
        [InlineData("")]
        [InlineData("1234567890")]
        public async Task EmitirBoletoAsync_ConvenioInvalido_Lanca(string convenio)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarClient(new SequencedFakeHttpMessageHandler()).EmitirBoletoAsync(CriarMovimento(), new Correntista { Convenio = convenio }));
        }

        [Theory]
        [InlineData("nossoNumero", "ABC")]
        [InlineData("nossoNumero", "12345678901234")]
        [InlineData("valor", "0")]
        [InlineData("vencimento", "anterior")]
        [InlineData("uf", "XX")]
        [InlineData("cep", "0")]
        [InlineData("nome", "Nome com mais de quarenta caracteres, sim senhor")]
        [InlineData("txid", "curto")]
        public async Task EmitirBoletoAsync_DadosInvalidos_LancaSemChamarOBanco(string campo, string valor)
        {
            var movimento = CriarMovimento();
            var item = (MovimentoItemBoletoCobranca)movimento.MovimentoItem;
            switch (campo)
            {
                case "nossoNumero": item.NossoNumero = valor; break;
                case "valor": movimento.ValorPagamento = decimal.Parse(valor); break;
                case "vencimento": item.DataVencimento = item.DataEmissao!.Value.AddDays(-1); break;
                case "uf": movimento.Favorecido.UF = valor; break;
                case "cep": movimento.Favorecido.CEP = int.Parse(valor); break;
                case "nome": movimento.Favorecido.Nome = valor; break;
                case "txid": item.TxId = valor; break;
            }
            var handler = new SequencedFakeHttpMessageHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarClient(handler).EmitirBoletoAsync(movimento, CriarBeneficiario()));

            Assert.Empty(handler.Requests);
        }

        private const string Liquidado = @"{
            ""nsuCode"": 12345678901234567000, ""nsuDate"": ""2022-12-12"", ""covenantCode"": ""1234567"",
            ""status"": ""LIQUIDADO"", ""statusComplement"": ""Baixa Operacional"",
            ""payer"": { ""name"": ""João da Silva"", ""documentType"": ""CPF"", ""documentNumber"": 9615865832, ""address"": ""Rua XV de Maio"",
                         ""neighborhood"": ""Vila Industrial"", ""city"": ""São Paulo"", ""state"": ""SP"", ""zipCode"": ""09761-233"" },
            ""beneficiary"": { ""name"": ""Maria"", ""documentType"": ""CNPJ"", ""documentNumber"": ""12345678000100"" },
            ""bankNumber"": ""123"", ""clientNumber"": ""NF10"", ""dueDate"": ""2022-12-12"", ""issueDate"": ""2022-12-01"", ""nominalValue"": ""10.15"",
            ""payment"": { ""paidValue"": 90, ""interestValue"": 1.2, ""fineValue"": 1.7, ""deductionValue"": ""10.00"", ""rebateValue"": 0, ""iofValue"": 0,
                           ""date"": ""2024-05-29"", ""type"": ""PIX"", ""bankCode"": 33, ""channel"": ""INTERNET BANKING"", ""kind"": ""DEBITO EM CONTA"",
                           ""txId"": ""1234567890abcdefghij123456"" },
            ""barcode"": ""03396939700000001009356720600000000123450101"", ""digitableLine"": ""03399356782060000000201234501011693970000000100"", ""entryDate"": ""2022-12-12""
        }";

        private static string Pagina(int total, params string[] boletos) =>
            $@"{{ ""_pageable"": {{ ""_totalElements"": {total} }}, ""_content"": [ {string.Join(",", boletos)} ] }}";

        [Fact]
        public async Task ConsultarRetornoAsync_Liquidacao_MapeiaMovimentoEItem()
        {
            var handler = Sequencia((HttpStatusCode.OK, Pagina(1, Liquidado)));

            var resultado = await CriarClient(handler).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Pagamento, nossoNumero: null, seuNumero: null);

            var get = handler.Requests[1];
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-cob-1/bank_slips?status=LIQUIDADO&paymentDateInitial=2024-05-01&paymentDateFinal=2024-05-31&_limit=50&_offset=0", get.RequestUri!.ToString());
            Assert.Equal("fake-client-id", get.Headers.GetValues("X-Application-Key").Single());

            var movimento = Assert.Single(resultado);
            Assert.Equal("123", movimento.NumeroDocumentoNoBanco);
            Assert.Equal("NF10", movimento.NumeroDocumento);
            Assert.Equal(90m, movimento.ValorPagamento);
            Assert.Equal(new DateTime(2024, 5, 29), movimento.DataPagamento);
            Assert.Equal(new DateTime(2024, 5, 29), movimento.HorarioBancoBr);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.SituacaoBancoBr);
            Assert.Equal("João da Silva", movimento.Favorecido.Nome);
            Assert.Equal("9615865832", movimento.Favorecido.CPF_CNPJ);
            Assert.Equal(9761233, movimento.Favorecido.CEP);

            var item = Assert.IsType<MovimentoItemRetornoCobranca>(movimento.MovimentoItem);
            Assert.Equal(TipoRetornoCobrancaEnum.Liquidacao, item.TipoRetorno);
            Assert.Equal("1234567", item.Convenio);
            Assert.Equal("12345678901234567000", item.NsuCode);
            Assert.Equal(new DateTime(2022, 12, 12), item.DataNsu);
            Assert.Equal("LIQUIDADO", item.StatusBanco);
            Assert.Equal("Baixa Operacional", item.ComplementoStatusBanco);
            Assert.Equal(10.15m, item.ValorNominal);
            Assert.Equal(new DateTime(2022, 12, 12), item.DataVencimento);
            Assert.Equal(new DateTime(2022, 12, 1), item.DataEmissao);
            Assert.Equal(1.2m, item.ValorJuros);
            Assert.Equal(1.7m, item.ValorMulta);
            Assert.Equal(10m, item.ValorAbatimento);
            Assert.Equal(0m, item.ValorDesconto);
            Assert.Equal("PIX", item.TipoPagamento);
            Assert.Equal("33", item.BancoPagamento);
            Assert.Equal("INTERNET BANKING", item.CanalPagamento);
            Assert.Equal("DEBITO EM CONTA", item.EspeciePagamento);
            Assert.Equal("1234567890abcdefghij123456", item.TxId);
            Assert.Equal("Maria", item.BeneficiarioFinal.Nome);
            Assert.Equal(TipoInscricaoCPFCNPJEnum.CNPJ, item.BeneficiarioFinal.TipoPessoa);
        }

        [Fact]
        public async Task ConsultarRetornoAsync_PorVencimentoComFiltros_MontaQuery()
        {
            var handler = Sequencia((HttpStatusCode.OK, Pagina(0)));

            var resultado = await CriarClient(handler).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Vencimento, nossoNumero: "123", seuNumero: "NF 10");

            Assert.Empty(resultado);
            Assert.Equal("status=LIQUIDADO&dueDateInitial=2024-05-01&dueDateFinal=2024-05-31&bankNumber=123&clientNumber=NF%2010&_limit=50&_offset=0",
                handler.Requests[1].RequestUri!.Query.TrimStart('?'));
        }

        [Fact]
        public async Task ConsultarRetornoAsync_VariasPaginas_LeTodas()
        {
            var handler = Sequencia((HttpStatusCode.OK, Pagina(2, Liquidado)), (HttpStatusCode.OK, Pagina(2, Liquidado)));

            var resultado = await CriarClient(handler).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Pagamento, null, null);

            Assert.Equal(2, resultado.Count);
            Assert.Contains("_offset=1", handler.Requests[2].RequestUri!.Query);
        }

        [Fact]
        public async Task ConsultarRetornoAsync_OutroConvenioDaWorkspace_EhFiltrado()
        {
            var deOutroConvenio = Liquidado.Replace(@"""covenantCode"": ""1234567""", @"""covenantCode"": ""7654321""");
            var handler = Sequencia((HttpStatusCode.OK, Pagina(2, Liquidado, deOutroConvenio)));

            var resultado = await CriarClient(handler).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Pagamento, null, null);

            Assert.Single(resultado);
        }

        [Fact]
        public async Task ConsultarRetornoAsync_Erro400_LancaBancoApiException()
        {
            var handler = Sequencia((HttpStatusCode.BadRequest, @"{ ""_errorCode"": ""400"", ""_message"": ""Bad Request"" }"));

            await Assert.ThrowsAsync<BancoApiException>(() => CriarClient(handler).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Pagamento, null, null));
        }

        [Fact]
        public async Task ConsultarRetornoAsync_PeriodoInvertido_Lanca()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => CriarClient(new SequencedFakeHttpMessageHandler()).ConsultarRetornoAsync(
                CriarBeneficiario(), TipoRetornoCobrancaEnum.Liquidacao, new DateTime(2024, 5, 31), new DateTime(2024, 5, 1),
                API.Base.Models.TipoDataRetornoCobrancaEnum.Pagamento, null, null));
        }

        [Fact]
        public async Task EmitirBoletoAsync_DescontoSemItens_Lanca()
        {
            var movimento = CriarMovimento();
            ((MovimentoItemBoletoCobranca)movimento.MovimentoItem).TipoDesconto = TipoDescontoBoletoEnum.ValorDiaUtil;

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarClient(new SequencedFakeHttpMessageHandler()).EmitirBoletoAsync(movimento, CriarBeneficiario()));
        }
    }
}
