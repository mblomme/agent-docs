# MARKETPLACE

This prompt is a marketplace for a collection of agent plugins related to domain-driven design (DDD) and specification-driven development (SDD).

## GOAL

Install or update the requested agent plugins on a workspace level and provide a summary of the available plugings, skills, mcps, hooks, ... 

## PREREQUISITES

The following information should be provided. If not, the agent should ask for it in simple multiple-choice questions. One question at a time, until all information is collected.

- Run `python3 "<skill-directory>/scripts/install_marketplace.py" discover "<target-workspace>"` first to inspect the latest plugins in this repository and compare them against the target workspace without relying on a cached checkout.
- Requested plugins:
  - Iterate through the discovered plugins from the `plugins` folder in this repository and ask a confirmation for each plugin (unless already installed during an update/refresh). If the user confirms, add it to the list of requested plugins.


## INSTALLATION

The installation contains 2 phases that must be completed in order. Execute `python3 "<skill-directory>/scripts/install_marketplace.py" install "<target-workspace>" --plugins <requested-plugins...>` to perform Phase 1 and the deterministic Phase 2 steps (Antigravity, Codex, and Copilot `settings.json`), then complete the GitHub Copilot `.github/plugin/marketplace.json` step manually before running `verify`.

### Phase 1: install base
The target destination for the plugins is the `.agents` folder of the current workspace the agents is working in. The use of the `.agents` folder is recommended to avoid conflicts and make it vendor-agnostic.

If the folder does not exist, create it. 

Verify that the container folder for the plugins exists, if not create it. The container folder is `.agents/plugins` in the current workspace.

Make use of the marketplace.json file to register the plugins. If the file does not exist, create it according to the example in the next section. If the file exists, update it with the new plugins.

The folder structure below is an example on where to place the plugins and how to register them in the marketplace.json file.


```example target destination
my-repo/
└── .agents/
    └── plugins/                                   # Container folder for all plugins
        ├── marketplace.json                       # Registers local and/or remote Git plugins
        └── some-plugin-of-this-repository/       # Local plugin 1
```

### Phase 2: install clients

This repository strives to use the .agents folder and the agents-plugins specification as much as possible, however not every clients has full support for it. That's why when the installation is done, the following needs to happen per client.

Install ALL the clients listed below.

**client: ddd-sdd-dotnet plugin**

1. **antigravity**
  - On the root of this plugin create a `hooks.json` file that is a symlink to ´com.google.antigravity/hooks.json´.

2. **codex**
  - On the root of the git repository create a .codex/hooks.json that is a symlink to ´.agents/plugins/ddd-sdd-dotnet/com.openai/hooks/hooks.json´.
  - Replace the `${PLUGIN_ROOT}` variable in the hooks.json file with the relative path to the plugin root starting from the root of the git repository. Check if this creates a valid path to the plugin root from the root of the git repository.
  - create or update the folder .codex/agents and create a copy of each subagent you can find in com.openai extension folder.
  - Create a file .codex/config.toml on the root of the repository and add the requested plugins by adding a '[plugins."<name>@<marketplace>"]' and set the property enabled to true of each requested plugin.

3. **copilot**
  - Create a github copilot .github/plugin/marketplace.json file in the repository and add the requested plugins.
  - If the marketplace.json file already exists, update the file, if not create the file and directories as needed.
  - Create a file .github/copilot/settings.json on the root of the repository and add the requested plugins.
  - The settings file should enable the requested plugens on the local repository level
  - If the file already exists, update the file, if not create the file and directories as needed.
  - Example settings file
```example settings.json
{
  "extraKnownMarketplaces": {
    "workspace-plugins": {
      "source": {
        "source": "directory",
        "path": "."
      }
    }
  },
  "enabledPlugins": {
    "some-plugin@workspace-plugins": true,
    "some-plugin2@workspace-plugins": true
  }
}
```

### VERIFICATION

After completing Phase 1, the scripted Phase 2 steps, and `.github/plugin/marketplace.json`, run `python3 "<skill-directory>/scripts/install_marketplace.py" verify "<target-workspace>" --plugins <requested-plugins...>` and ensure all checks below pass:

- **Phase 1 (install base)**
  - Verify that `.agents/` and `.agents/plugins/` exist in the current workspace.
  - Verify that each requested plugin is present in `.agents/plugins/<plugin-name>/`.
  - Verify that `.agents/plugins/marketplace.json` exists and registers all requested plugins.
- **Phase 2 (install clients — all listed clients must be verified)**
  - **antigravity** (`ddd-sdd-dotnet` plugin):
    - Verify that `.agents/plugins/ddd-sdd-dotnet/hooks.json` exists and is a symlink to `com.google.antigravity/hooks.json`.
  - **codex**:
    - Verify that `.codex/hooks.json` at the git repository root exists and is a symlink to `.agents/plugins/ddd-sdd-dotnet/com.openai/hooks/hooks.json`.
    - Verify that `${PLUGIN_ROOT}` in `hooks.json` has been replaced with a relative path to the plugin root that resolves validly from the git repository root.
    - Verify that `.codex/agents/` exists and contains a copy for every subagent in the `com.openai` extension folder.
    - Verify that `.codex/config.toml` exists at the repository root and enables every requested plugin under `[plugins."<name>@<marketplace>"]` with `enabled = true`.
  - **copilot**:
    - Verify that `.github/plugin/marketplace.json` in the repository and contains all requested plugins.
    - Verify that `.github/plugin/marketplace.json` format is github copilot compatible
    - Verify that `.github/copilot/settings.json` exists at the repository root and enables all requested plugins at the local repository level.
 
## IDEMPOTENCY

This section is only relevant if the `.agents` folders already has some of the available plugins installed.

Executing this prompt again should not result in duplicate plugins or entries in the marketplace.json file. The agent should check for existing plugins and update them if necessary. 

Updating means overwrite the plugin folder of the target location with the plugin of this repository event if it contains changes. The other files in the `.agents` folder should remain untouched because they may be used by other agents or tools. 

If this repository yields new plugins, the agent should ask for confirmation before adding them to the marketplace.json file. 

The agent should not ask for confirmation if the plugin contains updated or new content, it should update the existing plugin.


## EXAMPLE .github/plugin/marketplace.json

When creating `.github/plugin/marketplace.json` for GitHub Copilot (matching the `"workspace-plugins"` marketplace in `.github/copilot/settings.json` and reading `name`, `description`, and `version` from each installed plugin's `plugin.json`):

```example copilot marketplace.json
{
  "name": "workspace-plugins",
  "owner": {
    "name": "your-org-or-user"
  },
  "metadata": {
    "description": "Workspace agent plugins."
  },
  "plugins": [
    {
      "name": "some-plugin",
      "source": "./.agents/plugins/some-plugin",
      "description": "Plugin description from plugin.json.",
      "version": "0.1.0"
    }
  ]
}
```

## EXAMPLE marketplace.json

If there is no marketplace.json file, in .agents/plugins, look at the following example:

```example local marketplace.json
{
  "name": "workspace-plugins",
  "interface": {
    "displayName": "Workspace Plugins"
  },
  "plugins": [
    {
      "name": "my-core-plugin",
      "source": {
        "source": "local",
        "path": "./.agents/plugins/my-core-plugin"
      },
      "policy": {
        "installation": "INSTALLED_BY_DEFAULT",
        "authentication": "ON_INSTALL"
      },
      "category": "Productivity"
    }
  ]
}
```
