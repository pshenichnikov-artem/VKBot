namespace VKBot.Features.Core.Domain.Models
{
    public class UserSession
    {
        public long UserId { get; init; }
        public string? CurrentStateName { get; set; }
    
        public Dictionary<string, object> Data { get; init; } = new();
        
        public Type? GetCurrentState() => string.IsNullOrEmpty(CurrentStateName) ? null : Type.GetType(CurrentStateName);
        public void SetCurrentState(Type? state) => CurrentStateName = state?.AssemblyQualifiedName;
    }
}