using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SGServer.Models;

namespace SGServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InviteController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, Invite> Invites = new ();
    
    /// <summary>
    ///     Get all active invites that a user received.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("{userId}")]
    public IActionResult Get(string userId)
    {
        var invites = Invites.Values.Where(invite => invite.ReceiverId == userId).ToList();
        
        return Ok(invites.Count == 0 ? null : invites.First());
    }

    /// <summary>
    ///     Create a new invite
    /// </summary>
    /// <param name="invite"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Post([FromBody] Invite invite)
    {
        string inviteId = invite.HostId + '-' + invite.ReceiverId;
        
        Invites[inviteId] = invite;
        
        return Ok();
    }
    
    /// <summary>
    ///     Delete a single invite
    /// </summary>
    /// <param name="hostId"></param>
    /// <param name="receiverId"></param>
    /// <returns></returns>
    [HttpDelete("{hostId}/{receiverId}")]
    public IActionResult Delete(string hostId, string receiverId)
    {
        string inviteId = hostId + '-' + receiverId;
        
        Invites.TryRemove(inviteId, out _);
        
        return Ok();
    }

    /// <summary>
    ///     Delete all invites for a host
    /// </summary>
    /// <param name="hostId"></param>
    /// <returns></returns>
    [HttpDelete("{hostId}")]
    public IActionResult DeleteAll(string hostId)
    {
        foreach (var inviteId in Invites.Keys)
        {
            if (inviteId.StartsWith(hostId))
                Invites.TryRemove(inviteId, out _);
        }
        
        return Ok();
    }
}