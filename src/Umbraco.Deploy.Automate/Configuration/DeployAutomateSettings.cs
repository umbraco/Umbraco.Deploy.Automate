namespace Umbraco.Deploy.Automate.Configuration;

/// <summary>
/// Configuration settings for Umbraco Deploy Automate.
/// </summary>
public class DeployAutomateSettings
{
    /// <summary>
    /// Connection deployment settings (filtering sensitive data).
    /// </summary>
    public DeployAutomateConnectionSettings Connections { get; set; } = new();
}

/// <summary>
/// Configuration settings for Automate Connection deployment.
/// Controls how sensitive settings are filtered during deployment.
/// Filters are applied in this precedence order:
/// <list type="number">
/// <item><see cref="IgnoreSettings"/> — explicit field-name blocklist.</item>
/// <item><see cref="IgnoreSensitive"/> — schema-driven; strips every field marked <c>[Field(IsSensitive = true)]</c>, regardless of value.</item>
/// <item><see cref="IgnoreEncrypted"/> — value-driven; strips <c>ENC:</c> values but passes <c>$</c> configuration references through.</item>
/// </list>
/// </summary>
public class DeployAutomateConnectionSettings
{
    /// <summary>
    /// If true, ignore fields with encrypted values (values starting with "ENC:").
    /// ALLOWS: $ configuration references (e.g., "$MyService:ApiKey").
    /// BLOCKS: Encrypted values (ENC:...).
    /// </summary>
    public bool IgnoreEncrypted { get; set; } = true;

    /// <summary>
    /// If true, ignore every field marked <c>[Field(IsSensitive = true)]</c> on the
    /// connection type's settings POCO — regardless of its current value. This blocks
    /// plaintext, <c>ENC:</c> values, and <c>$</c> configuration references alike.
    /// Defaults to false; with <see cref="IgnoreEncrypted"/> on (default), encrypted
    /// secrets are already filtered, so this only matters if you also want to strip
    /// non-encrypted values from sensitive fields.
    /// </summary>
    public bool IgnoreSensitive { get; set; }

    /// <summary>
    /// Specific settings field names to always ignore during deployment.
    /// BLOCKS: All values for these specific fields (most specific control).
    /// Use this for fine-grained control over individual fields.
    /// </summary>
    public string[] IgnoreSettings { get; set; } = [];
}
