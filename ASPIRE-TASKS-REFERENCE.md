# Aspire CLI Tasks - Quick Reference

## Available Tasks

After migration, you'll have these new VS Code tasks available:

### 🚀 Primary Tasks

#### 1. **Aspire: Run with Dashboard** (Default Build Task)

**Shortcut**: `Ctrl+Shift+B`

Starts the AppHost with Aspire Dashboard in the browser.

```
Tasks: Run Task → Aspire: Run with Dashboard
```

**What it does**:

- ✅ Builds the AppHost and Blazor app
- ✅ Starts Aspire Dashboard at `https://localhost:15216`
- ✅ Starts Blazor app at `https://localhost:5001`
- ✅ Automatically opens dashboard in browser
- ✅ Shows real-time logs, metrics, and traces

**Use when**: Starting a new development session

---

#### 2. **Aspire: Run (CLI)**

Same as above but doesn't open the dashboard automatically.

```
Tasks: Run Task → Aspire: Run (CLI)
```

**Use when**: You want to run the app without opening the dashboard

---

### 🛑 Control Tasks

#### 3. **Aspire: Stop**

Stops all running Aspire processes.

```
Tasks: Run Task → Aspire: Stop
```

**What it does**:

- ✅ Terminates AppHost process
- ✅ Stops Blazor app
- ✅ Closes Aspire Dashboard

**Use when**: Stopping the development session

---

#### 4. **Aspire: Build**

Builds the AppHost project without running it.

```
Tasks: Run Task → Aspire: Build
```

**Use when**: You want to verify the project compiles

---

### 📜 Python Tasks (Unchanged)

These tasks remain for the JavaScript/HTML implementation:

#### 5. **Dev Server (HTTP)**

Starts the Python dev server on `http://localhost:5173`

#### 6. **Dev Server (HTTPS)**

Starts the Python dev server on `https://localhost:5173`

---

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Run default task (Aspire with Dashboard) | `Ctrl+Shift+B` |
| Open task menu | `Ctrl+Shift+P` → "Tasks: Run Task" |
| Stop running task | `Ctrl+C` in terminal |

---

## Command Line Equivalents

If you prefer the command line:

### Run AppHost

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet run
```

### Run with Aspire CLI

```powershell
aspire run --project dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj --dashboard
```

### Build AppHost

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet build
```

### Stop All

```powershell
# Windows
Get-Process -Name 'dotnet' -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*aspire*' } | Stop-Process -Force

# macOS/Linux
pkill -f aspire
```

---

## Troubleshooting

### Task Won't Start

**Error**: "Command 'aspire' not found"

**Solution**: Install Aspire CLI

```powershell
dotnet tool install -g aspire
```

---

### Port Already in Use

**Error**: "Failed to bind to address"

**Solution**: Stop existing processes

```powershell
# Run the "Aspire: Stop" task
# OR manually:
Get-Process -Id (Get-NetTCPConnection -LocalPort 15216 -ErrorAction SilentlyContinue).OwningProcess | Stop-Process -Force
Get-Process -Id (Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue).OwningProcess | Stop-Process -Force
```

---

### Dashboard Won't Open

**Error**: Dashboard doesn't open automatically

**Solution**: Manually navigate to:

- Aspire Dashboard: `https://localhost:15216`
- Blazor App: `https://localhost:5001`

---

## Tips & Tricks

### 1. Use Aspire Dashboard for Debugging

The dashboard shows:

- **Console Logs**: Real-time app output
- **Traces**: HTTP request traces
- **Metrics**: Performance metrics
- **Environment**: Configured variables

### 2. Check Connection Strings

In the Aspire Dashboard:

1. Go to **Resources** tab
2. Click on "azureaiavatarblazor"
3. See injected environment variables

### 3. View Logs

In the Aspire Dashboard:

1. Go to **Console Logs** tab
2. Select "azureaiavatarblazor"
3. See real-time logs

### 4. Background Execution

Tasks marked as `isBackground: true` run in the background and don't block the terminal.

---

## References

- **Aspire CLI Documentation**: <https://learn.microsoft.com/dotnet/aspire/cli/overview>
- **VS Code Tasks**: <https://code.visualstudio.com/docs/editor/tasks>
- **Migration Plan**: [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md)
