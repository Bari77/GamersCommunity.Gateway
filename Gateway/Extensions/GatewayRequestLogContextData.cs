namespace Gateway.Extensions;

internal readonly record struct GatewayRequestLogContextData(
    string Method,
    string Path,
    string? Microservice,
    string? Resource,
    string? Action,
    string? Id,
    string? PublicId);
