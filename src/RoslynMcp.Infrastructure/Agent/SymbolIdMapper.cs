using System.Collections.Concurrent;

namespace RoslynMcp.Infrastructure.Agent;

/// <summary>
/// Static mapper for converting between internal Roslyn symbol IDs and external short string IDs.
/// External IDs are short (e.g., "S0001"), token-friendly, and less error-prone for copy/paste.
/// </summary>
public static class SymbolIdMapper
{
    private static readonly ConcurrentDictionary<string, string> _internalToExternal = new();
    private static readonly ConcurrentDictionary<string, string> _externalToInternal = new();
    
    private static int _nextId = 0;
    private const int Padding = 4; // S0001, S0002, etc.

    /// <summary>
    /// Converts an internal Roslyn symbol ID to an external short ID (e.g., "S0001").
    /// </summary>
    public static string ToExternal(string internalId)
    {
        if (string.IsNullOrEmpty(internalId))
            return string.Empty;

        if (_internalToExternal.TryGetValue(internalId, out var existingId))
            return existingId;

        var newId = Interlocked.Increment(ref _nextId);
        var externalId = string.Format("S{0:D4}", newId);
        
        _internalToExternal[internalId] = externalId;
        _externalToInternal[externalId] = internalId;
        
        return externalId;
    }

    /// <summary>
    /// Attempts to convert an external ID back to an internal Roslyn symbol ID.
    /// </summary>
    /// <returns>True if the conversion was successful; otherwise, false.</returns>
    public static bool TryToInternal(string externalId, out string? internalId)
    {
        internalId = null;

        if (string.IsNullOrEmpty(externalId))
            return false;

        return _externalToInternal.TryGetValue(externalId, out internalId);
    }

    /// <summary>
    /// Converts an external ID back to an internal Roslyn symbol ID.
    /// Throws if the external ID is not found in the mapping.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the external ID is not found in the mapping.</exception>
    public static string ToInternal(string externalId)
    {
        if (!TryToInternal(externalId, out var internalId))
            throw new KeyNotFoundException($"External ID not found in mapping: {externalId}");

        return internalId;
    }

    /// <summary>
    /// Updates an internal ID mapping, keeping the external ID but pointing to a new internal ID.
    /// Useful when a symbol's internal ID changes (e.g., after rename) but the external ID should remain the same.
    /// </summary>
    /// <returns>True if the update was successful; false if the old internal ID was not found.</returns>
    public static bool Update(string oldInternalId, string newInternalId)
    {
        if (string.IsNullOrEmpty(oldInternalId) || string.IsNullOrEmpty(newInternalId))
            return false;

        if (!_internalToExternal.TryGetValue(oldInternalId, out var externalId))
            return false;

        // Remove old internal ID
        _internalToExternal.TryRemove(oldInternalId, out _);

        // Add new internal ID with same external ID
        _internalToExternal[newInternalId] = externalId;
        _externalToInternal[externalId] = newInternalId;

        return true;
    }
}
