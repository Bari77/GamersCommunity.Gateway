using System.Net;
using GamersCommunity.Core.Exceptions;

namespace Gateway.Exceptions;

public class ServiceUnavailableException(string code = "UNAVAILABLE", string? message = "The microservice is unavailable.")
    : AppException(HttpStatusCode.ServiceUnavailable, code, message);
