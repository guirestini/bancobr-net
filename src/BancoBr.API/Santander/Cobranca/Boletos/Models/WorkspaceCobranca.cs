using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Cobranca.Boletos.Models
{
    /// <summary>
    /// Resposta paginada de GET /workspaces do Hub de Cobrança. O formato é suposto a partir do
    /// Hub de Pagamentos (a documentação recebida cobre só o POST bank_slips) e fica isolado
    /// aqui e em <c>CobrancaBoletoClient.ResolverWorkspaceAsync</c>.
    /// </summary>
    internal class WorkspaceCobrancaListResponse
    {
        [JsonProperty("_pageable")]
        public PaginacaoCobranca Pageable { get; set; }

        [JsonProperty("_content")]
        public List<WorkspaceCobranca> Content { get; set; }
    }

    internal class PaginacaoCobranca
    {
        [JsonProperty("_totalElements")]
        public int TotalElements { get; set; }
    }

    internal class WorkspaceCobranca
    {
        // A documentação do Hub de Pagamentos usa "id" na criação/consulta por ID e "workspace_id" na listagem.
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("workspace_id")]
        public string WorkspaceId { get; set; }

        [JsonIgnore]
        public string IdResolvido => Id ?? WorkspaceId;

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("covenants")]
        public List<ConvenioWorkspaceCobranca> Covenants { get; set; }
    }

    internal class ConvenioWorkspaceCobranca
    {
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
