using LiquidDocsData.Models;
using LiquidDocsSite.Data;
using LiquidDocsSite.Database;
using SixLabors.ImageSharp;
using System.Collections.Concurrent;

namespace LiquidDocsSite.State;

public class ApplicationStateManager : IApplicationStateManager
{
    public bool IsUseFakeData { get; set; } = false;
    public ConcurrentDictionary<string, object> ActiveSessions { get; set; } = new();

    private readonly ILogger<ApplicationStateManager> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private IMongoDatabaseRepo appDb;
    private ApplicationDbContext dbContext;
    private readonly IConfiguration config;
    private readonly IHttpContextAccessor http;

    public ApplicationStateManager(ILogger<ApplicationStateManager> logger, IServiceScopeFactory scopeFactory, IMongoDatabaseRepo appDb, IConfiguration config, IHttpContextAccessor http)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.config = config;
        this.appDb = appDb;
        this.http = http;

        IsUseFakeData = config.GetValue<bool>("IsUseFakeData");
    }

    public async Task<UserProfile> GetUserProfileByUserNameAsync(string userName)
    {
        UserProfile userProfile = null;

        try
        {
            userProfile = await appDb.GetRecordByUserNameAsync<UserProfile>(userName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return userProfile;
    }

    public bool UserIs(string role) => http.HttpContext?.User?.IsInRole(role) == true;

    public IEnumerable<string> GetRoles() => http.HttpContext?.User?
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
}