# Aspire Migration Roadmap

## 🗓️ 6-Day Implementation Timeline

```
Day 1: AppHost Setup
├── Install prerequisites
├── Add NuGet packages
├── Configure user secrets
└── Update AppHost.cs

Day 2: Client Integration
├── Update Blazor app packages
├── Modify Program.cs
├── Update AzureOpenAIService
└── Update AzureSpeechService

Day 3: Configuration Cleanup
├── Update ConfigurationService
├── Delete appsettings.json files
├── Test local development
└── Verify all services work

Day 4: Documentation
├── Update QUICKSTART.md
├── Update ARCHITECTURE.md
├── Update DEPLOYMENT.md
└── Update root README.md

Day 5: VS Code Integration
├── Add Aspire CLI tasks
├── Add launch configuration
├── Test all tasks
└── Final local testing

Day 6: Production Testing
├── Install Azure Developer CLI
├── Run azd init
├── Test azd up deployment
└── Verify production deployment
```

---

## 📊 Progress Tracking

### Phase 1: AppHost Configuration ⬜

- [ ] Install Aspire workload (`dotnet workload install aspire`)
- [ ] Install Aspire CLI (`dotnet tool install -g aspire`)
- [ ] Update `AzureAIAvatarBlazor.AppHost.csproj` with packages
- [ ] Configure AppHost user secrets
- [ ] Rewrite `AppHost.cs` with AI resources
- [ ] Update `.gitignore` for AppHost secrets
- [ ] Test build: `dotnet build AzureAIAvatarBlazor.AppHost.csproj`

### Phase 2: Aspire Client Integration ⬜

- [ ] Update `AzureAIAvatarBlazor.csproj` packages
- [ ] Modify `Program.cs` to add `AddAzureOpenAIClient`
- [ ] Update `AzureOpenAIService.cs` to use injected client
- [ ] Update `AzureSpeechService.cs` to parse connection strings
- [ ] Test build: `dotnet build AzureAIAvatarBlazor.csproj`

### Phase 3: Remove appsettings.json ⬜

- [ ] Update `ConfigurationService.cs` to remove file parsing
- [ ] Delete `appsettings.json` from Blazor app
- [ ] Delete `appsettings.Development.json` from Blazor app
- [ ] Run AppHost: `dotnet run --project AzureAIAvatarBlazor.AppHost`
- [ ] Test Blazor app at `https://localhost:5001`
- [ ] Test avatar session connection
- [ ] Test chat functionality
- [ ] Verify configuration in `/config` page

### Phase 4: Documentation Updates ⬜

- [ ] Update `QUICKSTART.md` with AppHost instructions
- [ ] Add Aspire orchestration section to `ARCHITECTURE.md`
- [ ] Update `DEPLOYMENT.md` with `azd up` instructions
- [ ] Update root `README.md` prerequisites
- [ ] Search/replace all `appsettings.json` references
- [ ] Review `.github/copilot-instructions.md`

### Phase 5: VS Code Integration ⬜

- [ ] Add Aspire tasks to `.vscode/tasks.json`
- [ ] Add launch configuration to `.vscode/launch.json`
- [ ] Test "Aspire: Run with Dashboard" task
- [ ] Test "Aspire: Stop" task
- [ ] Test keyboard shortcut `Ctrl+Shift+B`
- [ ] Verify Aspire Dashboard opens automatically

### Phase 6: Production Deployment ⬜

- [ ] Install Azure Developer CLI (`winget install microsoft.azd`)
- [ ] Run `azd init` in AppHost directory
- [ ] Configure Azure subscription in user secrets
- [ ] Test `azd up` deployment
- [ ] Verify Azure OpenAI resource created
- [ ] Verify Speech Service resource created
- [ ] Verify model deployment
- [ ] Test deployed app functionality
- [ ] Run `azd down` to cleanup (optional)

---

## 🎯 Milestones

### Milestone 1: Local Development Works ✅

**Criteria**:

- AppHost starts without errors
- Aspire Dashboard accessible
- Blazor app loads at localhost:5001
- Avatar connects successfully
- Chat messages stream correctly

### Milestone 2: No Configuration Files ✅

**Criteria**:

- No `appsettings.json` in Blazor app
- All config from AppHost env vars
- Services read from `IConfiguration`
- No hardcoded credentials

### Milestone 3: Documentation Complete ✅

**Criteria**:

- All markdown files updated
- No references to deleted files
- Clear setup instructions
- Architecture diagrams updated

### Milestone 4: VS Code Integration ✅

**Criteria**:

- Tasks defined in `tasks.json`
- Tasks run successfully
- Keyboard shortcuts work
- Dashboard opens automatically

### Milestone 5: Production Ready ✅

**Criteria**:

- `azd up` provisions resources
- Model deploys automatically
- App runs in Azure
- Managed identity works
- No manual Portal steps

---

## 🔍 Quality Gates

Before proceeding to next phase, verify:

### Gate 1 (After Phase 1)

```powershell
# Build succeeds
dotnet build dotnet/AzureAIAvatarBlazor.AppHost

# Secrets configured
dotnet user-secrets list --project dotnet/AzureAIAvatarBlazor.AppHost

# Expected: Shows connection strings
```

### Gate 2 (After Phase 2)

```powershell
# Blazor app builds
dotnet build dotnet/AzureAIAvatarBlazor

# No package conflicts
dotnet list dotnet/AzureAIAvatarBlazor package

# Expected: Shows Aspire packages
```

### Gate 3 (After Phase 3)

```powershell
# AppHost runs
dotnet run --project dotnet/AzureAIAvatarBlazor.AppHost

# Expected:
# - Dashboard at https://localhost:15216
# - App at https://localhost:5001
# - No configuration errors
```

### Gate 4 (After Phase 4)

```bash
# No broken references
grep -r "appsettings.json" dotnet/docs/
grep -r "appsettings.Development.json" dotnet/docs/

# Expected: No matches (or only in migration docs)
```

### Gate 5 (After Phase 5)

**VS Code**: Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Aspire: Run with Dashboard"
**Expected**: App starts, dashboard opens

### Gate 6 (After Phase 6)

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost
azd up

# Expected:
# ✓ Resources provisioned
# ✓ App deployed
# ✓ Endpoint URL provided
```

---

## 🐛 Known Issues & Solutions

### Issue 1: Aspire CLI Not Found

**Symptom**: `'aspire' is not recognized as an internal or external command`

**Solution**:

```powershell
dotnet tool install -g aspire
dotnet tool update -g aspire
```

### Issue 2: Port Conflicts

**Symptom**: `Failed to bind to address https://localhost:15216`

**Solution**:

```powershell
# Stop existing processes
Get-Process -Id (Get-NetTCPConnection -LocalPort 15216 -ErrorAction SilentlyContinue).OwningProcess | Stop-Process -Force
```

### Issue 3: Missing Secrets

**Symptom**: `Azure OpenAI credentials not configured`

**Solution**:

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=...;Key=...;"
```

### Issue 4: Package Version Conflicts

**Symptom**: Build errors about incompatible package versions

**Solution**:

```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

### Issue 5: Dashboard Doesn't Open

**Symptom**: Task runs but browser doesn't open

**Solution**: Manually navigate to `https://localhost:15216`

---

## 📈 Success Metrics

Track these metrics before/after migration:

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Configuration files | 2 | 0 | 0 |
| Manual Azure steps | Many | 0 | 0 |
| Startup commands | 2+ | 1 | 1 |
| Secret locations | 3+ | 1 | 1 |
| Deployment time | 30+ min | 5 min | <10 min |
| Lines of config code | 200+ | <50 | <100 |

---

## 📞 Getting Help

If you encounter issues:

1. **Check logs**: Aspire Dashboard → Console Logs tab
2. **Review plan**: [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md)
3. **Rollback**: See "Rollback Strategy" in migration plan
4. **Documentation**:
   - [Aspire CLI Docs](https://learn.microsoft.com/dotnet/aspire/cli/overview)
   - [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview)
5. **Community**:
   - [.NET Aspire GitHub](https://github.com/dotnet/aspire/issues)
   - [.NET Discord](https://aka.ms/dotnet-discord)

---

## 🎓 Learning Resources

### Before Starting

- [ ] Read [.NET Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [ ] Watch [.NET Aspire Quickstart Video](https://www.youtube.com/watch?v=z1M-7Bms1Jg)
- [ ] Review [Aspire Samples](https://github.com/dotnet/aspire-samples)

### During Migration

- [ ] [Aspire Azure AI Integration](https://learn.microsoft.com/dotnet/aspire/azureai/azureai-openai-component)
- [ ] [Aspire Configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/configuration)
- [ ] [Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview)

### After Migration

- [ ] [Azure Deployment](https://learn.microsoft.com/dotnet/aspire/deployment/azure/azure-deployment)
- [ ] [Managed Identity](https://learn.microsoft.com/dotnet/aspire/deployment/azure/managed-identity)
- [ ] [Production Considerations](https://learn.microsoft.com/dotnet/aspire/deployment/overview)

---

## 🎉 Completion Checklist

### Technical Completion

- [ ] All phases implemented
- [ ] All quality gates passed
- [ ] All tests passing
- [ ] Production deployment verified
- [ ] Rollback plan tested

### Documentation Completion

- [ ] All markdown files updated
- [ ] Architecture diagrams current
- [ ] Setup instructions accurate
- [ ] Troubleshooting guide complete
- [ ] Migration plan reviewed

### Team Readiness

- [ ] Team trained on new workflow
- [ ] VS Code tasks documented
- [ ] Secrets management understood
- [ ] Deployment process tested
- [ ] Support documentation ready

---

**Status**: Planning Complete ✅  
**Next Action**: Install prerequisites (Day 1)  
**Estimated Completion**: 6 working days  
**Risk Level**: Medium (rollback available)

---

**Ready to start? Begin with [ASPIRE-MIGRATION-PLAN.md](./ASPIRE-MIGRATION-PLAN.md) Phase 1!**
