// ========================================================================
// Date: 25/10/2569 (BE) / 25/10/2026 (CE)
// File: protect.cedar
// Version: 1.0.0
// Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// Location: protect.cedar
// Description: Cedar authorization policy for agentic tool execution governance
// ========================================================================

// Allow read-only tools by default
permit (
    principal,
    action in [Action::"Read", Action::"Glob", Action::"Grep", Action::"WebFetch"],
    resource
);

// Require explicit allow for safe commands
permit (
    principal,
    action == Action::"Bash",
    resource
) when {
    context.command_pattern in ["git", "dotnet", "powershell", "python", "uv", "cmd", "ls", "cat", "echo", "pwd", "test"]
};

// Never allow recursive deletion of project root or critical directories
forbid (
    principal,
    action == Action::"Bash",
    resource
) when {
    context.command_pattern == "rm -rf"
};

// Require safe file writes within project scope
forbid (
    principal,
    action in [Action::"Edit", Action::"Write"],
    resource
) when {
    context.path_starts_with != "."
};
