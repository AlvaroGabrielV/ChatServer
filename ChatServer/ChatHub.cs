using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VYNDRA.Classes;


namespace ChatServer
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> _usuarios = new ConcurrentDictionary<string, string>();

        public async Task RegistrarUsuario(string idUsuario)
        {
            _usuarios[idUsuario] = Context.ConnectionId;

            await Clients.All.SendAsync("UsuarioConectado", idUsuario);
            Console.WriteLine($"Usuário registrado: {idUsuario} com ConnectionId: {Context.ConnectionId}");
        }


        public async Task EnviarMensagemPrivada(string paraIdUsuario, string deIdUsuario, string mensagem)
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


        public async Task EnviarSolicitacaoAmizade(string paraIdUsuario, string deIdUsuario)
        {
            var dao = new PedidoAmizadeDAO();
            dao.SalvarPedido(new PedidoAmizade { DeUsuario = deIdUsuario, ParaUsuario = paraIdUsuario });

            if (_usuarios.TryGetValue(paraIdUsuario, out string connIdDestino))
            {
                await Clients.Client(connIdDestino).SendAsync("ReceberSolicitacaoAmizade", deIdUsuario);
                await Clients.Caller.SendAsync("SolicitacaoEnviada", paraIdUsuario);
            }
            else
            {
                await Clients.Caller.SendAsync("SolicitacaoEnviadaOffline", paraIdUsuario);
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