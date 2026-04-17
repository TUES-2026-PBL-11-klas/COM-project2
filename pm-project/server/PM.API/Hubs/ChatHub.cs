using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using PM.Core.Interfaces;

namespace PM.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(Guid conversationId, Guid senderId, string content)
        {
            var message = await _chatService.SendMessageAsync(conversationId, senderId, content);
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message, content);
        }
    }
}