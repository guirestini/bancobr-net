using BancoBr.API.Base;
using BancoBr.API.Core;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.Models;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.Common.Enums;

// Console standalone para validar a integração Sicoob (PagamentoBoletoClient) contra um
// ambiente real (sandbox/homologação), sem depender de um ERP. Nenhuma credencial é lida
// de arquivo de configuração commitado: tudo vem de variáveis de ambiente.

string? clientId = Environment.GetEnvironmentVariable("SICOOB_CLIENT_ID");
string? accessToken = Environment.GetEnvironmentVariable("SICOOB_ACCESS_TOKEN");
string? tokenEndpoint = Environment.GetEnvironmentVariable("SICOOB_TOKEN_ENDPOINT");
string? certPfxPath = Environment.GetEnvironmentVariable("SICOOB_CERT_PFX_PATH");
string? certPassword = Environment.GetEnvironmentVariable("SICOOB_CERT_PASSWORD");
string? numeroAgenciaText = Environment.GetEnvironmentVariable("SICOOB_NUMERO_COOPERATIVA");
string? numeroContaText = Environment.GetEnvironmentVariable("SICOOB_NUMERO_CONTA");
string? codigoBarras = Environment.GetEnvironmentVariable("SICOOB_CODIGO_BARRAS");
string? idLancamentoText = Environment.GetEnvironmentVariable("SICOOB_ID_LANCAMENTO");
Guid idLancamento = string.IsNullOrWhiteSpace(idLancamentoText) ? Guid.NewGuid() : Guid.Parse(idLancamentoText);
bool confirmarPagamento = Environment.GetEnvironmentVariable("SICOOB_CONFIRMAR_PAGAMENTO") == "true";

// O Sicoob expõe, no portal de sandbox, um "Access token (Bearer)" já emitido para teste
// rápido — sem precisar implementar o fluxo client_credentials completo. SICOOB_ACCESS_TOKEN
// usa esse token diretamente; sem ele, cai no fluxo OAuth2 real (SicoobApiOptions já traz o
// endpoint de token padrão — https://auth.sicoob.com.br/... — só sobrescrito se SICOOB_TOKEN_ENDPOINT
// for definido, para algum ambiente fora do padrão).
bool usarTokenFixo = !string.IsNullOrWhiteSpace(accessToken);

var faltando = new List<string>();
if (string.IsNullOrWhiteSpace(clientId)) faltando.Add("SICOOB_CLIENT_ID");
if (string.IsNullOrWhiteSpace(certPfxPath)) faltando.Add("SICOOB_CERT_PFX_PATH");
if (string.IsNullOrWhiteSpace(certPassword)) faltando.Add("SICOOB_CERT_PASSWORD");
if (string.IsNullOrWhiteSpace(numeroAgenciaText)) faltando.Add("SICOOB_NUMERO_COOPERATIVA");
if (string.IsNullOrWhiteSpace(numeroContaText)) faltando.Add("SICOOB_NUMERO_CONTA");
if (string.IsNullOrWhiteSpace(codigoBarras)) faltando.Add("SICOOB_CODIGO_BARRAS");

if (faltando.Count > 0)
{
    Console.WriteLine("Variáveis de ambiente obrigatórias ausentes:");
    foreach (var nome in faltando)
    {
        Console.WriteLine($"  - {nome}");
    }
    Console.WriteLine();
    Console.WriteLine("Opcional: SICOOB_ACCESS_TOKEN para usar um token Bearer já emitido pelo portal de sandbox, pulando o fluxo OAuth2.");
    Console.WriteLine("Opcional: SICOOB_TOKEN_ENDPOINT para sobrescrever o endpoint de token padrão do Sicoob.");
    Console.WriteLine("Opcional: SICOOB_CONFIRMAR_PAGAMENTO=true para também testar PagarBoletoAsync (cuidado: dispara um pagamento real, mesmo em sandbox).");
    Console.WriteLine("Opcional: SICOOB_ID_LANCAMENTO (UUID) para fixar o lançamento usado na idempotency key (default: UUID gerado a cada execução).");
    return 1;
}

var numeroAgencia = int.Parse(numeroAgenciaText!);
var numeroConta = long.Parse(numeroContaText!);

var certificateSource = CertificateSource.FromPfxFile(certPfxPath!, certPassword!);
var tokenEndpointOverride = string.IsNullOrWhiteSpace(tokenEndpoint) ? null : new Uri(tokenEndpoint);

var client = usarTokenFixo
    ? BancoApi.Criar<PagamentoBoletoApiBase>(BancoEnum.Sicoob, clientId!, certificateSource, new StaticAccessTokenProvider(accessToken!))
    : BancoApi.Criar<PagamentoBoletoApiBase>(BancoEnum.Sicoob, clientId!, clientSecret: null, certificateSource, tokenEndpointOverride);

try
{
    Console.WriteLine($"Consultando boleto {codigoBarras} na conta {numeroConta}...");
    var consulta = await client.ConsultarBoletoAsync(codigoBarras!, numeroConta);

    if (consulta == null)
    {
        Console.WriteLine("Boleto não encontrado (204 sem conteúdo).");
        return 0;
    }

    Console.WriteLine($"Beneficiário: {consulta.NomeRazaoSocialBeneficiario} ({consulta.NumeroCpfCnpjBeneficiario})");
    Console.WriteLine($"Valor do boleto: {consulta.ValorBoleto:C}");
    Console.WriteLine($"Valor de pagamento: {consulta.ValorPagamento:C}");
    Console.WriteLine($"Vencimento: {consulta.DataVencimentoBoleto:yyyy-MM-dd}");
    Console.WriteLine($"IdentificadorConsulta: {consulta.IdentificadorConsulta}");
    Console.WriteLine($"BloquearPagamento: {consulta.BloquearPagamento} ({consulta.MensagemBloqueioPagamento})");

    if (!confirmarPagamento)
    {
        Console.WriteLine();
        Console.WriteLine("PagarBoletoAsync não foi chamado (defina SICOOB_CONFIRMAR_PAGAMENTO=true para testar o pagamento).");
        return 0;
    }

    if (consulta.BloquearPagamento)
    {
        Console.WriteLine("Pagamento bloqueado pelo Sicoob para este boleto; abortando antes de chamar PagarBoletoAsync.");
        return 1;
    }

    Console.WriteLine();
    Console.WriteLine("SICOOB_CONFIRMAR_PAGAMENTO=true: chamando PagarBoletoAsync...");

    var request = new BancoBr.API.Base.Models.BoletoPagamentoRequest
    {
        IdentificadorConsulta = consulta.IdentificadorConsulta,
        ValorBoleto = consulta.ValorBoleto,
        ValorDescontoAbatimento = consulta.ValorAbatimentoDesconto,
        ValorMultaMora = consulta.ValorMultaMora,
        Amount = consulta.ValorPagamento,
        Date = consulta.DataPagamento,
        AceitaValorDivergente = false,
        NumeroCpfCnpjPortador = consulta.NumeroCpfCnpjPagador,
        NomePortador = consulta.NomeRazaoSocialPagador,
        DebtorAccount = new BancoBr.API.Base.Models.DebtorAccount
        {
            Issuer = numeroAgencia,
            Number = numeroConta,
            AccountType = 0,
            PersonType = 1,
        },
    };

    var idempotencyKey = IdempotencyKey.New(numeroAgencia, numeroConta, idLancamento);
    var resultado = await client.PagarBoletoAsync(codigoBarras!, request, idempotencyKey);

    if (resultado.PendenteAssinatura)
    {
        Console.WriteLine("Pagamento pendente de assinatura (202).");
    }
    else if (resultado.Comprovante == null)
    {
        Console.WriteLine("Pagamento processado sem conteúdo de retorno (204).");
    }
    else
    {
        Console.WriteLine($"Pagamento efetivado. IdPagamento={resultado.Comprovante.IdPagamento}, Situação={resultado.Comprovante.SituacaoPagamento}");
    }

    return 0;
}
catch (SicoobApiException ex)
{
    Console.WriteLine($"Erro de negócio do Sicoob (HTTP {ex.HttpStatusCode}):");
    foreach (var mensagem in ex.Mensagens)
    {
        Console.WriteLine($"  [{mensagem.Codigo}] {mensagem.Mensagem}");
    }
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine("Falha de transporte/autenticação (cert/mTLS/OAuth/rede), não um erro de negócio do Sicoob:");
    Console.WriteLine(ex);
    return 1;
}
