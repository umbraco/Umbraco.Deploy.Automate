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
    /// Specific settings field names to always ignore during deployment.
    /// BLOCKS: All values for these specific fields (most specific control).
    /// Use this for fine-grained control over individual fields.
    /// </summary>
    public string[] IgnoreSettings { get; set; } = [];
}
