using LiquidDocsData.Models;
using System.Collections.Concurrent;

namespace LiquidDocsSite.State;

public interface IApplicationStateManager
{
    ConcurrentDictionary<string, object> ActiveSessions { get; set; }
    bool IsUseFakeData { get; set; }

    IEnumerable<string> GetRoles();

    Task<UserProfile> GetUserProfileByUserNameAsync(string userName);

    bool UserIs(string role);
}