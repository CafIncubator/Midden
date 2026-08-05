using System.Text;

namespace Caf.Midden.Cli.Common;

/// <summary>
/// Console helpers for reading credentials without echoing them to the screen or leaving them in
/// shell history.
/// </summary>
public static class ConsolePrompt
{
    /// <summary>
    /// Environment variable used to supply the secret store password for unattended runs, so the
    /// password never has to be passed as a command line argument.
    /// </summary>
    public const string PasswordEnvironmentVariable = "MIDDEN_STORE_PASSWORD";

    /// <summary>
    /// Reads the secret store password from the environment when present, otherwise prompts for it
    /// with masked input.
    /// </summary>
    public static string ReadStorePassword(string prompt = "Secret store password: ")
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);

        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            return fromEnvironment;
        }

        return ReadHidden(prompt);
    }

    /// <summary>
    /// Prompts twice and requires the two entries to match. Returns null if they do not.
    /// </summary>
    public static string? ReadNewPassword()
    {
        var password = ReadHidden("New secret store password: ");

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("The password cannot be empty.");
            return null;
        }

        var confirmation = ReadHidden("Confirm password: ");

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("The passwords do not match.");
            return null;
        }

        return password;
    }

    /// <summary>
    /// Reads a line of input without echoing it. Falls back to a plain read when input is
    /// redirected, so the CLI remains scriptable.
    /// </summary>
    public static string ReadHidden(string prompt)
    {
        Console.Write(prompt);

        if (Console.IsInputRedirected)
        {
            var redirected = Console.ReadLine() ?? string.Empty;
            Console.WriteLine();
            return redirected;
        }

        var builder = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return builder.ToString();

                case ConsoleKey.Backspace when builder.Length > 0:
                    builder.Length--;
                    Console.Write("\b \b");
                    break;

                case ConsoleKey.Backspace:
                    break;

                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return string.Empty;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        builder.Append(key.KeyChar);
                        Console.Write('*');
                    }

                    break;
            }
        }
    }
}
