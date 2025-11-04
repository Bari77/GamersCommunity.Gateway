using Gateway.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace Gateway.Validators
{
    /// <summary>
    /// Provides validation logic for <see cref="GatewayRoutingSettings"/> 
    /// to ensure that the Gateway configuration is consistent and unambiguous.
    /// </summary>
    /// <remarks>
    /// This validator runs automatically at startup when registered via 
    /// <c>services.AddSingleton&lt;IValidateOptions&lt;GatewayRoutingSettings&gt;, GatewayRoutingValidator&gt;()</c>.  
    /// It checks for:
    /// <list type="bullet">
    ///   <item><description>Duplicate microservice identifiers.</description></item>
    ///   <item><description>Missing or empty queue names.</description></item>
    ///   <item><description>Duplicate resource names per microservice.</description></item>
    ///   <item><description>Duplicate action names per resource.</description></item>
    ///   <item><description>Empty identifiers at any level (microservice, resource, action).</description></item>
    /// </list>
    /// If any inconsistency is found, an <see cref="OptionsValidationException"/> is thrown during startup.
    /// </remarks>
    public sealed class GatewayRoutingValidator : IValidateOptions<GatewayRoutingSettings>
    {
        /// <summary>
        /// Validates the provided <see cref="GatewayRoutingSettings"/> instance.
        /// </summary>
        /// <param name="name">The name of the options instance (ignored).</param>
        /// <param name="options">The current configuration instance.</param>
        /// <returns>A <see cref="ValidateOptionsResult"/> representing the validation outcome.</returns>
        public ValidateOptionsResult Validate(string? name, GatewayRoutingSettings options)
        {
            if (options is null)
                return ValidateOptionsResult.Fail("GatewayRouting configuration is missing.");

            var errors = new List<string>();

            var msIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ms in options.Microservices)
            {
                if (string.IsNullOrWhiteSpace(ms.Id))
                    errors.Add("A microservice has an empty or null Id.");

                if (!msIds.Add(ms.Id))
                    errors.Add($"Duplicate microservice Id detected: '{ms.Id}'.");

                if (string.IsNullOrWhiteSpace(ms.Queue))
                    errors.Add($"Microservice '{ms.Id}' has no defined queue.");

                var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var res in ms.Resources)
                {
                    if (string.IsNullOrWhiteSpace(res.Name))
                        errors.Add($"Microservice '{ms.Id}' contains a resource with an empty name.");

                    if (!resourceNames.Add(res.Name))
                        errors.Add($"Duplicate resource '{res.Name}' found in microservice '{ms.Id}'.");

                    var actionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var act in res.Actions)
                    {
                        if (string.IsNullOrWhiteSpace(act.Name))
                            errors.Add($"Resource '{res.Name}' in microservice '{ms.Id}' contains an unnamed action.");

                        if (!actionNames.Add(act.Name))
                            errors.Add($"Duplicate action '{act.Name}' found in resource '{res.Name}' (microservice '{ms.Id}').");
                    }
                }
            }

            if (errors.Count == 0)
                return ValidateOptionsResult.Success;

            var sb = new StringBuilder();
            sb.AppendLine("Invalid GatewayRouting configuration:");
            foreach (var err in errors)
                sb.AppendLine($" - {err}");

            return ValidateOptionsResult.Fail(sb.ToString());
        }
    }
}
