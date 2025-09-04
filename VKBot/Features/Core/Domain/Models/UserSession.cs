using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Domain.Models
{
    public class UserSession
    {
        public long UserId { get; set; }
        
        private Dictionary<string, object> _data = new();
        private string? _currentStateName;
        private string? _lastResponse;
        
        public Type? GetCurrentState() => string.IsNullOrEmpty(_currentStateName) ? null : Type.GetType(_currentStateName);
        public void SetCurrentState(Type? state) => _currentStateName = state?.AssemblyQualifiedName;
        
        public string? GetLastResponse() => _lastResponse;
        public void SetLastResponse(string? response) => _lastResponse = response;
        
        public T? GetData<T>(string key) => _data.ContainsKey(key) ? (T)_data[key] : default;
        public void SetData(string key, object value) => _data[key] = value;
        
        public Dictionary<string, object> GetAllData() => new(_data);
        public void LoadData(Dictionary<string, object> data) => _data = new(data);
    }
}