using System;
using System.Collections.Generic;

namespace HealthyPlant.Grpc.Infrastructure
{
    public interface IDiagnosticContext
    {
        void Set(string name, object? value);
        object Get(string name);
        void Remove(string name);
        void Pop(string name, out object value);
        Dictionary<string, object> GetData();
    }

    public class DiagnosticContext : IDiagnosticContext
    {
        private readonly Dictionary<string, object> _store;

        public DiagnosticContext()
        {
            _store = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public void Set(string name, object? value)
        {
            value ??= "null";
            if (_store.ContainsKey(name))
            {
                _store[name] = value;
                return;
            }

            _store.Add(name, value);
        }

        public object Get(string name) => _store.TryGetValue(name, out var value)
            ? value
            : throw new ArgumentException(nameof(name));

        public void Remove(string name) => _store.Remove(name);
        public void Pop(string name, out object value)
        {
            value = Get(name);
            Remove(name);
        }

        public Dictionary<string, object> GetData() => new Dictionary<string, object>(_store, StringComparer.OrdinalIgnoreCase);
    }
}