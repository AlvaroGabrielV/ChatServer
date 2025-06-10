using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SeuProjetoChat.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> _connectedUsers = new ConcurrentDictionary<string, string>();

        public async Task EnviarMensagem(string usuario, string mensagem)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(mensagem))
            {
                await Clients.Caller.SendAsync("ReceberMensagemErro", "Nome de usuário ou mensagem não podem ser vazios.");
                return;
            }

            await Clients.All.SendAsync("ReceberMensagem", usuario, mensagem);
            Console.WriteLine($"Mensagem de {usuario}: {mensagem}");
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Cliente conectado: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Cliente desconectado: {Context.ConnectionId} (Erro: {exception?.Message})");
            await base.OnDisconnectedAsync(exception);
        }
    }
}