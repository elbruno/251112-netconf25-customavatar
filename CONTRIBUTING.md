# Contributing to Azure AI Avatar Demo

Thank you for your interest in contributing to the Azure AI Avatar Demo project! This document provides guidelines and instructions for contributing.

## 🤝 How to Contribute

We welcome contributions in many forms:

- 🐛 **Bug reports** - Help us identify and fix issues
- 💡 **Feature requests** - Suggest new capabilities
- 📝 **Documentation improvements** - Help make docs clearer
- 🔧 **Code contributions** - Submit bug fixes or new features
- 🎨 **UI/UX improvements** - Enhance the user experience
- 🧪 **Testing** - Help improve test coverage

## 🚀 Getting Started

### Prerequisites

Before contributing, ensure you have:

1. **Git** installed and configured
2. **GitHub account** with SSH or HTTPS access configured
3. **Development environment** set up for your chosen implementation:
   - **JavaScript**: Node.js 18+
   - **.NET**: .NET 9 SDK + Aspire workload
4. **Azure subscription** with Speech and OpenAI services

### Fork and Clone

```bash
# Fork the repository on GitHub (click Fork button)

# Clone your fork
git clone https://github.com/YOUR_USERNAME/customavatarlabs.git
cd customavatarlabs

# Add upstream remote
git remote add upstream https://github.com/elbruno/customavatarlabs.git

# Verify remotes
git remote -v
```

## 🔄 Development Workflow

### 1. Create a Branch

Always create a new branch for your changes:

```bash
# Update main branch
git checkout main
git pull upstream main

# Create feature branch
git checkout -b feature/your-feature-name
# OR for bug fixes
git checkout -b fix/issue-number-description
```

**Branch naming conventions:**
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation changes
- `refactor/` - Code refactoring
- `test/` - Test additions or changes

### 2. Make Changes

#### For JavaScript Implementation

```bash
# Navigate to python folder
cd python

# Create .env file
cp .env.example .env
# Edit .env with your credentials

# Start dev server
cd dev-server
npm install
npm start

# Test your changes
# Open http://localhost:5173 in browser
```

#### For .NET Implementation

```bash
# Navigate to dotnet folder
cd dotnet

# Install Aspire workload
dotnet workload install aspire

# Configure secrets
cd AzureAIAvatarBlazor.AppHost
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://...;Key=...;"
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://...;Key=...;"

# Run application
dotnet run

# Test your changes
# Open https://localhost:5001 in browser
```

### 3. Test Your Changes

Before submitting:

- ✅ **Test locally** - Verify changes work as expected
- ✅ **Test both implementations** if changes affect shared concepts
- ✅ **Test on multiple browsers** (Chrome, Firefox, Edge, Safari)
- ✅ **Check console logs** - No errors in browser/terminal
- ✅ **Verify documentation** - Update relevant docs

#### Specific Test Cases

**For Avatar Changes:**
1. Open avatar session successfully
2. Avatar video loads and displays
3. Avatar speaks with correct lip sync
4. Audio is clear and at appropriate volume

**For Chat Changes:**
1. Messages send and receive correctly
2. Streaming responses work
3. Conversation history displays properly
4. Special characters and formatting work

**For Configuration Changes:**
1. Settings save and persist
2. Settings load correctly on page refresh
3. Test connection buttons work
4. Validation messages are clear

### 4. Commit Your Changes

Write clear, descriptive commit messages:

```bash
# Stage your changes
git add .

# Commit with meaningful message
git commit -m "Add support for custom avatar styles

- Extend avatar catalog in config.js
- Update UI to show new style options
- Add validation for custom styles
- Update documentation with new styles"
```

**Commit message guidelines:**
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- First line: Brief summary (50 chars or less)
- Body: Detailed explanation with bullet points
- Reference issues: "Fixes #123" or "Related to #456"

### 5. Push and Create Pull Request

```bash
# Push to your fork
git push origin feature/your-feature-name

# Go to GitHub and create Pull Request
# Fill out the PR template completely
```

## 📋 Pull Request Guidelines

### PR Title

Use clear, descriptive titles:
- ✅ "Add support for custom background images"
- ✅ "Fix avatar connection timeout on Safari"
- ✅ "Update QUICKSTART.md with Aspire setup steps"
- ❌ "Update"
- ❌ "Fix bug"
- ❌ "Changes"

### PR Description

Include:
1. **Summary** - What does this PR do?
2. **Motivation** - Why is this change needed?
3. **Testing** - How was this tested?
4. **Screenshots** - For UI changes (before/after)
5. **Related Issues** - Link to issues this addresses
6. **Checklist** - Mark completed items

**Example PR Description:**

```markdown
## Summary
Adds support for custom background images in avatar video panel.

## Motivation
Users requested ability to customize the background behind the avatar to match their branding.

## Changes
- Added background image upload in config.html
- Implemented CSS overlay technique in chat.html
- Added configuration persistence in localStorage
- Updated documentation with new feature

## Testing
- ✅ Tested image upload and preview
- ✅ Verified image persists across sessions
- ✅ Tested on Chrome, Firefox, Edge
- ✅ Verified responsive behavior on mobile
- ✅ Checked performance with large images

## Screenshots
### Before
[Screenshot]

### After
[Screenshot]

## Related Issues
Fixes #123
Related to #456

## Checklist
- [x] Code follows project style guidelines
- [x] Documentation updated
- [x] Self-reviewed code
- [x] Tested locally
- [ ] Added tests (N/A for this change)
```

## 📝 Code Style Guidelines

### JavaScript

- Use **ES6+** features
- Use **const** for immutable values, **let** for mutable
- Use **arrow functions** for callbacks
- Use **async/await** for asynchronous code
- Add **JSDoc comments** for functions
- Keep functions small and focused

**Example:**
```javascript
/**
 * Connects to Azure Speech Service and initializes avatar session
 * @param {string} token - Speech service token
 * @param {string} region - Azure region
 * @returns {Promise<void>}
 */
async function connectAvatar(token, region) {
    try {
        const config = SpeechSDK.SpeechConfig.fromAuthorizationToken(token, region);
        // ... implementation
    } catch (error) {
        console.error('Failed to connect avatar:', error);
        throw error;
    }
}
```

### C# / .NET

- Follow **Microsoft C# Coding Conventions**
- Use **PascalCase** for classes and methods
- Use **camelCase** for local variables
- Add **XML documentation** for public APIs
- Use **async/await** consistently
- Implement **IDisposable** when needed

**Example:**
```csharp
/// <summary>
/// Sends a chat message to Azure OpenAI and streams the response.
/// </summary>
/// <param name="message">The user's message</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Async enumerable of response chunks</returns>
public async IAsyncEnumerable<string> SendMessageAsync(
    string message,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(message);
    
    // ... implementation
}
```

### Documentation

- Use **Markdown** for all documentation
- Include **code examples** where relevant
- Add **screenshots** for UI features
- Keep **line length** under 120 characters
- Use **headers** to organize content
- Include **table of contents** for long documents

## 🧪 Testing Guidelines

### Manual Testing

Before submitting, verify:

**JavaScript Implementation:**
1. Open `config.html` and verify all fields load
2. Test Speech connection button
3. Test OpenAI connection button
4. Save configuration and reload page
5. Open `chat.html` and start avatar session
6. Send messages via text and voice
7. Verify avatar speaks and lip syncs
8. Test stop, clear, and close buttons

**.NET Implementation:**
1. Run AppHost and verify Aspire Dashboard loads
2. Verify Blazor app loads at https://localhost:5001
3. Test configuration page and save settings
4. Test chat page and avatar connection
5. Send messages and verify responses
6. Check Aspire Dashboard for logs and metrics

### Browser Testing

Test on:
- ✅ Chrome (latest)
- ✅ Firefox (latest)
- ✅ Edge (latest)
- ✅ Safari (latest) if on macOS

### Responsive Testing

Test at different viewport sizes:
- 📱 Mobile (320px - 767px)
- 📱 Tablet (768px - 1023px)
- 💻 Desktop (1024px+)

## 🐛 Reporting Bugs

When reporting bugs, include:

1. **Description** - Clear summary of the issue
2. **Steps to Reproduce** - Detailed steps
3. **Expected Behavior** - What should happen
4. **Actual Behavior** - What actually happens
5. **Environment** - Browser, OS, implementation
6. **Screenshots** - If applicable
7. **Console Logs** - Browser console or terminal output

**Example Bug Report:**

```markdown
## Bug Description
Avatar video remains black after successful connection.

## Steps to Reproduce
1. Open chat.html in Firefox 115
2. Click "Open Avatar Session"
3. Wait for "Connected" status
4. Video panel remains black

## Expected Behavior
Avatar video should display after connection.

## Actual Behavior
Video panel shows black screen, but audio works.

## Environment
- Browser: Firefox 115.0
- OS: Windows 11
- Implementation: JavaScript
- Speech Region: westus2

## Console Logs
```
WebRTC connection established
Avatar token received
getUserMedia success
Video element: <video id="avatar-video">
```

## Screenshots
[Screenshot of black video panel]
```

## 💡 Suggesting Features

When suggesting features:

1. **Use Case** - Describe the scenario
2. **Proposed Solution** - How it should work
3. **Alternatives** - Other approaches considered
4. **Implementation** - Technical approach (if known)
5. **Impact** - Who benefits and how

## 🔒 Security

### Reporting Security Issues

**DO NOT** open public issues for security vulnerabilities.

Instead:
1. Email: [security contact]
2. Include detailed description
3. Steps to reproduce
4. Potential impact
5. Suggested fix (if known)

### Security Guidelines

When contributing:
- ❌ **Never commit** API keys, secrets, or credentials
- ❌ **Never hardcode** sensitive information
- ✅ **Use** environment variables for secrets
- ✅ **Use** .gitignore for credential files
- ✅ **Validate** all user input
- ✅ **Sanitize** output to prevent XSS

## 📜 Code of Conduct

### Our Standards

- ✅ Be respectful and inclusive
- ✅ Welcome newcomers
- ✅ Accept constructive criticism
- ✅ Focus on what's best for the community
- ✅ Show empathy towards others

### Unacceptable Behavior

- ❌ Harassment or discrimination
- ❌ Trolling or insulting comments
- ❌ Public or private harassment
- ❌ Publishing others' private information
- ❌ Inappropriate conduct

## 📞 Getting Help

Need help contributing?

1. **Documentation** - Check [README](README.md) and guides
2. **Issues** - Search existing issues
3. **Discussions** - Ask in GitHub Discussions
4. **Discord** - Join our Discord server [if available]

## 🎉 Recognition

Contributors will be:
- Listed in release notes
- Mentioned in CONTRIBUTORS.md
- Acknowledged in project documentation

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing!** 🙏

Your contributions help make this project better for everyone.
