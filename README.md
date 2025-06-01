# MultiObfuscator

A Windows Forms-based C# code obfuscator built with Roslyn (Microsoft.CodeAnalysis) to protect your source code by renaming identifiers, encrypting string literals, and providing step-by-step obfuscation insights.

![Demo Screenshot](screenshot.png) <!-- Replace with actual screenshot path if available -->

## Features

* **Identifier Renaming**

  * Variables (prefix: `V_`)
  * Parameters (prefix: `P_`)
  * Methods (prefix: `M_`)
  * Classes (prefix: `C_`)
* **String Literal Encryption**

  * Converts string literals to Base64-encoded values
  * Injects an automatic decryption method (`M_mgbkf`) into the primary class
  * Configurable string splitting (split long strings into multiple encrypted parts)
* **Control Flow Flattening**

  * Converts simple `switch`-based logic into a state-machine structure for integer-based switches (non-string/non-char cases)
* **Const Field Conversion**

  * Transforms `const` local or field declarations into `static readonly` fields to preserve runtime behavior and support obfuscation
* **Slow Mode (Step-by-Step)**

  * Enables a “Slow Mode” checkbox in the UI to step through each obfuscation transformation one at a time
  * Highlights the exact token spans being modified and logs details of each step
* **Real-Time Logging**

  * Colored log entries indicating informational, success, warning, error, and debug messages
  * Tracks all transformation steps and diagnostics (including compilation checks)
* **Settings Panel**

  * Configure obfuscator behavior, such as enabling/disabling string splitting
  * Easily update settings via a dedicated settings form
* **Compilation Validation**

  * Attempts to compile the obfuscated code in-memory to catch errors immediately
  * Logs any compilation errors or confirms a successful build
* **Smart Preservation**

  * Skips public methods and explicit interface implementations
  * Preserves `Main` method signature and entry-point behavior
  * Maintains common system identifiers (e.g., `System`, `Console`, `List`)

## Disclaimer

> **Note**: This obfuscator is primarily for educational or light-weight use. For production-grade protection, consider established commercial obfuscators such as Dotfuscator or Babel.

## Usage

1. **Input File Selection**

   * Click **Browse** to select a C# source file (single `.cs` file).
   * The original code appears in the left pane (`Original Code`).

2. **Configure Settings (Optional)**

   * Click **Settings** to open the `ObfuscatorSettings` form.
   * Enable or disable options like **String Splitting**. Click **OK** to save.

3. **Obfuscation Process**

   * Click **Obfuscate** to start the process:

     1. Reads and parses the selected `.cs` file using Roslyn.
     2. Performs syntax-tree rewrites to rename identifiers, encrypt string literals, flatten simple switch statements, and convert `const` fields.
     3. Injects a static `M_mgbkf` decryption method into the primary class.
     4. In **Fast Mode** (default), applies all transformations at once and writes the obfuscated code to the right pane (`Obfuscated Code`).
     5. In **Slow Mode** (check the **Slow Mode** checkbox before clicking Obfuscate):

        * Disables the **Obfuscate** button and enables **Next**.
        * Click **Next** to apply each recorded transformation step-by-step.
        * Highlights the exact span being modified in the code and updates the step counter at the top.
        * Once all steps are applied, the **Next** button becomes **Finish**; click to finalize and re-enable **Obfuscate**.
   * After transformations, the tool tries to compile the obfuscated code in-memory:

     * If there are compilation errors, they appear in the Log panel in red, and the obfuscated code is still displayed for inspection.
     * If compilation succeeds, a success message is logged in green.

4. **Log Monitoring**

   * The **Log** panel at the bottom displays color-coded messages:

     * **Blue**: Informational steps
     * **Green**: Successful operations
     * **Orange**: Warnings (e.g., skipping unsupported constructs)
     * **Red**: Errors (syntax or compilation errors)
     * **White**: Debug messages (detailed internal info)
   * Follow the log to track each renaming, encryption, or control-flow transformation.

## Installation

1. **Requirements**

   * Windows OS (Forms-based application)
   * .NET Framework 4.7.2 or higher (or .NET 5+/6+ with appropriate Windows Forms workload)
   * Visual Studio 2019 or later (recommended) or any IDE supporting WinForms and C# 8.0+
   * Roslyn NuGet packages:

     * `Microsoft.CodeAnalysis.CSharp`
     * `Microsoft.CodeAnalysis.CSharp.Workspaces`

2. **Build from Source**

   ```bash
   git clone https://github.com/yourusername/MultiObfuscator.git
   cd MultiObfuscator
   dotnet restore
   dotnet build
   ```

3. **Run the Application**

   * Launch `MultiObfuscator.exe` from the `bin/Debug` or `bin/Release` folder.

## Code Example

**Original Code:**

```csharp
class Sample {
    private const string secretConst = "confidential data";
    private string secret = "hidden info";
    
    private void ProcessData(int count) {
        var items = new List<string>();
        Console.WriteLine("Processing...");
    }
}
```

**Obfuscated Output (Fast Mode):**

```csharp
class C_AbcXyz {
    private static readonly string V_kj4Lmn = M_mgbkf("Y29uZmlkZW50aWFsIGRhdGE=");  // const → static readonly
    private string V_pqRstu = M_mgbkf("aGlkZGVuIGluZm8=");

    private void M_defGhi(int P_1q2w3e) {
        var V_2w3e4r = new List<string>();
        Console.WriteLine(M_mgbkf("UHJvY2Vzc2luZy4uLg=="));
    }

    private static string M_mgbkf(string V_homft) {
        if (string.IsNullOrEmpty(V_homft)) return string.Empty;
        byte[] V_xkgpd = Convert.FromBase64String(V_homft);
        return System.Text.Encoding.UTF8.GetString(V_xkgpd);
    }
}
```

## Limitations

* Basic obfuscation; not intended for high-assurance production security.
* Does **not** handle:

  * Reflection-based code or dynamic invocation
  * Advanced generics or LINQ expression trees
  * External assembly references beyond core .NET (e.g., third-party libraries)
* Preserves public API surface (external callers still use original method signatures).
* Not designed for:

  * ASP.NET Core applications
  * WPF/XAML projects
  * Class library projects with complex build pipelines

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the Repository**
2. **Create a Feature Branch**

   ```bash
   git checkout -b feature/my-improvement
   ```
3. **Commit Changes**

   * Write descriptive commit messages
4. **Push to Your Fork**

   ```bash
   git push origin feature/my-improvement
   ```
5. **Open a Pull Request**

   * Describe the change and its rationale

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE.md) for details.
