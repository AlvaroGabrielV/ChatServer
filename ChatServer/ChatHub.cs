using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    public async Task EnviarMensagem(string usuario, string mensagem)
    {
        await Clients.All.SendAsync("ReceberMensagem", usuario, mensagem);
    }
}
