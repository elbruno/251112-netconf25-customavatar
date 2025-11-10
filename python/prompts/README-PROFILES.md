# Prompt Profiles

This folder stores prompt profiles and templates used by the config UI.

- `index.json` lists available profiles.
- `*.md` files are templates with `{{vars}}` placeholders.

To always apply a profile on load, add an env var in your `.env`:

```
PROMPT_PROFILE=pablito-piova-es
PROMPT_ENFORCE_PROFILE=true
# Optional variable overrides (merged with profile defaults)
PROMPT_VAR_max_frases=4
```

Aliases for the enforcement flag: `FORCE_PROMPT_PROFILE=true` or `PROMPT_PROFILE_FORCE=true`.

When enforcement is enabled, the UI will replace the System Prompt with the selected profile on every load, even if `SYSTEM_PROMPT` is set in `.env` or persisted in localStorage.
