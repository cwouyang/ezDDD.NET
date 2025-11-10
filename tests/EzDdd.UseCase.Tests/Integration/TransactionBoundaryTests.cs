namespace EzDdd.UseCase.Tests.Integration;

public sealed class TransactionBoundaryTests
{
    /// <summary>
    ///     Finds the project root directory by searching upward for .sln file.
    ///     This approach is more reliable than counting directory levels.
    /// </summary>
    private static string _GetProjectRootDirectory()
    {
        string directory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            // Look for solution file as project root indicator
            if (Directory.GetFiles(directory, "*.sln").Length > 0)
            {
                return directory;
            }

            // Move up one directory
            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new InvalidOperationException("Could not find project root directory (no .sln file found)");
    }

    [Fact]
    public void OutboxRepository_DoesNotContainTransactionLogic()
    {
        string projectRoot = _GetProjectRootDirectory();
        string sourceFile = Path.Combine(projectRoot, "src", "EzDdd.UseCase", "Port", "Out", "OutboxRepository.cs");

        Assert.True(File.Exists(sourceFile), $"Source file not found: {sourceFile}");

        // Read source code
        string content = File.ReadAllText(sourceFile);

        Assert.DoesNotContain("BeginTransaction", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TransactionScope", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CommitAsync", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RollbackAsync", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EsRepository_DoesNotContainTransactionLogic()
    {
        string projectRoot = _GetProjectRootDirectory();
        string sourceFile = Path.Combine(projectRoot, "src", "EzDdd.UseCase", "Port", "Out", "EsRepository.cs");

        Assert.True(File.Exists(sourceFile), $"Source file not found: {sourceFile}");

        // Read source code
        string content = File.ReadAllText(sourceFile);

        Assert.DoesNotContain("BeginTransaction", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TransactionScope", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CommitAsync", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RollbackAsync", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IRepositoryPeer_Documentation_MustMentionTransactionRequirement()
    {
        string projectRoot = _GetProjectRootDirectory();
        string sourceFile = Path.Combine(projectRoot, "src", "EzDdd.UseCase", "Port", "Out", "IRepositoryPeer.cs");

        Assert.True(File.Exists(sourceFile), $"Source file not found: {sourceFile}");

        // Read source code
        string content = File.ReadAllText(sourceFile);

        Assert.Contains("Transaction", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CRITICAL ARCHITECTURE RULE", content);
        Assert.Contains("CORRECT", content);
        Assert.Contains("WRONG", content);
        Assert.Contains("TRANSACTION_BOUNDARY_GUIDE.md", content);
    }

    [Fact]
    public void IRepository_Documentation_MustWarnAgainstTransactions()
    {
        string projectRoot = _GetProjectRootDirectory();
        string sourceFile = Path.Combine(projectRoot, "src", "EzDdd.UseCase", "Port", "Out", "IRepository.cs");

        Assert.True(File.Exists(sourceFile), $"Source file not found: {sourceFile}");

        // Read source code
        string content = File.ReadAllText(sourceFile);

        Assert.Contains("Transaction", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CRITICAL ARCHITECTURE RULE", content);
        Assert.Contains("WRONG", content);
        Assert.Contains("MUST NOT", content);
        Assert.Contains("TRANSACTION_BOUNDARY_GUIDE.md", content);
    }

    [Fact]
    public void TransactionBoundaryGuide_Exists_WithCorrectContent()
    {
        string projectRoot = _GetProjectRootDirectory();
        string guidePath = Path.Combine(projectRoot, "docs", "TRANSACTION_BOUNDARY_GUIDE.md");

        Assert.True(File.Exists(guidePath), $"TRANSACTION_BOUNDARY_GUIDE.md not found at: {guidePath}");

        // Read content
        string content = File.ReadAllText(guidePath);

        // Verify key sections exist
        Assert.Contains("CRITICAL ARCHITECTURE RULE", content);
        Assert.Contains("✅ CORRECT Implementation", content);
        Assert.Contains("❌ WRONG Implementation", content);
        Assert.Contains("Why This Rule Exists", content);
        Assert.Contains("Clean Architecture", content);
        Assert.Contains("IRepositoryPeer", content);
        Assert.Contains("IRepository", content);
        Assert.Contains("Transaction", content);
        Assert.Contains("EF Core", content);
        Assert.Contains("TransactionScope", content);
    }

    [Fact]
    public void TransactionBoundaryGuide_ContainsCorrectExamples()
    {
        string projectRoot = _GetProjectRootDirectory();
        string guidePath = Path.Combine(projectRoot, "docs", "TRANSACTION_BOUNDARY_GUIDE.md");

        Assert.True(File.Exists(guidePath), $"TRANSACTION_BOUNDARY_GUIDE.md not found at: {guidePath}");

        // Read content
        string content = File.ReadAllText(guidePath);

        // Verify examples show correct patterns
        Assert.Contains("BeginTransactionAsync", content); // EF Core transaction example
        Assert.Contains("CommitAsync", content); // Transaction commit
        Assert.Contains("RollbackAsync", content); // Transaction rollback
        Assert.Contains("DbUpdateConcurrencyException", content); // Optimistic locking
        Assert.Contains("Transactional Outbox", content); // Pattern name
    }

    [Fact]
    public void TransactionBoundaryGuide_ExplainsWhyRuleExists()
    {
        string projectRoot = _GetProjectRootDirectory();
        string guidePath = Path.Combine(projectRoot, "docs", "TRANSACTION_BOUNDARY_GUIDE.md");

        Assert.True(File.Exists(guidePath), $"TRANSACTION_BOUNDARY_GUIDE.md not found at: {guidePath}");

        // Read content
        string content = File.ReadAllText(guidePath);

        // Verify rationale sections
        Assert.Contains("Clean Architecture Enforcement", content);
        Assert.Contains("Testability", content);
        Assert.Contains("Technology Independence", content);
        Assert.Contains("layer separation", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("infrastructure concern", content, StringComparison.OrdinalIgnoreCase);
    }
}