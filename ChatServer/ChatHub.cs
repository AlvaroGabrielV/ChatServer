using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VYNDRA.Classes;


namespace ChatServer
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<int, string> _usuarios = new ConcurrentDictionary<int, string>();

        public async Task RegistrarUsuario(int idUsuario)
        {
            Console.WriteLine("Tentando registrar ID: {0}", idUsuario);
            _usuarios[idUsuario] = Context.ConnectionId;

            await Clients.All.SendAsync("UsuarioConectado", idUsuario);
            Console.WriteLine($"Usuário registrado: {idUsuario} com ConnectionId: {Context.ConnectionId}");
        }

        public async Task EnviarMensagemPrivada(int paraIdUsuario, int deIdUsuario, string mensagem)
        {
            if (_usuarios.TryGetValue(paraIdUsuario, out string connIdDestino))
            {
                await Clients.Client(connIdDestino).SendAsync("ReceberMensagemPrivada", deIdUsuario, mensagem);
                await Clients.Caller.SendAsync("ReceberMensagemPrivada", deIdUsuario, mensagem);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceberMensagemErro", "Usuário não encontrado ou offline.");
            }
        }

        public async Task EnviarSolicitacaoAmizade(int idDestino, int meuId)
        {
            Console.WriteLine($"Enviando solicitação de amizade de {meuId} para {idDestino}");
            var dao = new PedidoAmizadeDAO();
            dao.SalvarPedido(new PedidoAmizade { DeUsuario = meuId, ParaUsuario = idDestino });

            if (_usuarios.TryGetValue(idDestino, out string connIdDestino))
            {
                Console.WriteLine($"Usuário destino conectado, enviando via SignalR");
                await Clients.Client(connIdDestino).SendAsync("ReceberSolicitacaoAmizade", meuId);
                await Clients.Caller.SendAsync("SolicitacaoEnviada", idDestino);
            }
            else
            {
                Console.WriteLine($"Usuário destino OFFLINE, enviando resposta offline");
                await Clients.Caller.SendAsync("SolicitacaoEnviadaOffline", idDestino);
            }
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