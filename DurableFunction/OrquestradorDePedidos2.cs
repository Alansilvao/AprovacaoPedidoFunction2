using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunction
{
    public static class OrquestradorDePedidos2
    {
        [Function(nameof(OrquestradorDePedidos2))]
        public static async Task<List<Pedido>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
       
            var pedidos = new List<Pedido>
        {
            new Pedido { Id = "1", Status = "Em Análise" },
            new Pedido { Id = "2", Status = "Em Análise" },
            new Pedido { Id = "3", Status = "Em Análise" }
        };

            foreach (var pedido in pedidos)
            {
                // Chamando a função de atividade para cada pedido
                pedidos.Add(await context.CallActivityAsync<Pedido>(nameof(AtualizaStatusPedido), pedido));
            }

            return pedidos;
        }

        [Function(nameof(AtualizaStatusPedido))]
        public static string AtualizaStatusPedido([ActivityTrigger] Pedido pedido, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("AtualizaStatusPedido");

            logger.LogInformation("Status antes = '{instanceId}'.", pedido.Status);
            pedido.Status = "Aprovado"; // Atualizando o status do pedido para Aprovado

            logger.LogInformation("Status depois = '{instanceId}'.", pedido.Status);
            return $"Pedido {pedido.Id} atualizado para {pedido.Status}.";
        }

        [Function("OrquestradorDePedidos_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("OrquestradorDePedidos_HttpStart");

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(OrquestradorDePedidos2));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
            
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
