using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
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

            await Clients.Others.SendAsync("UsuarioConectado", idUsuario);
            Console.WriteLine($"Usuário registrado: {idUsuario} com ConnectionId: {Context.ConnectionId}");

            var listaOnline = _usuarios.Keys.Where(u => u != idUsuario).ToList();
            await Clients.Caller.SendAsync("ListaUsuariosOnline", listaOnline);
        }

        public async Task EnviarMensagemPrivada(int remetenteId, int destinatarioId, string mensagem)
        {
            Console.WriteLine($"[DEBUG] Enviando mensagem de {remetenteId} para {destinatarioId}: {mensagem}");

            try
            {
                var dao = new ChatDAO();
                dao.SalvarMensagem(remetenteId, destinatarioId, mensagem);

                var dataEnvio = DateTime.UtcNow;

                if (_usuarios.TryGetValue(destinatarioId, out string connIdDestino))
                {
                    await Clients.Client(connIdDestino).SendAsync("ReceberMensagemPrivada", remetenteId, destinatarioId, mensagem, dataEnvio);
                    
                }

                await Clients.Caller.SendAsync("ReceberMensagemPrivada", remetenteId, destinatarioId, mensagem, dataEnvio);

                Console.WriteLine("[DEBUG] Mensagem enviada com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao enviar mensagem: {ex.Message}");
                throw;
            }
        }

        public async Task CarregarMensagensPrivadas(int usuario1Id, int usuario2Id)
        {
            var dao = new ChatDAO();
            var mensagens = dao.BuscarMensagens(usuario1Id, usuario2Id);
            Console.WriteLine($"[ChatHub] Carregando {mensagens.Count} mensagens para {usuario1Id} e {usuario2Id}");
            await Clients.Caller.SendAsync("MensagensCarregadas", mensagens);
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

            var usuarioRemovido = _usuarios.FirstOrDefault(u => u.Value == Context.ConnectionId).Key;
            if (usuarioRemovido != 0)
            {
                _usuarios.TryRemove(usuarioRemovido, out _);
                await Clients.All.SendAsync("UsuarioDesconectado", usuarioRemovido);
                Console.WriteLine($"Usuário {usuarioRemovido} removido do dicionário");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}