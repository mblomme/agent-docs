# MARKETPLACE

This prompt is a marketplace for a collection of agent plugins related to domain-driven design (DDD) and specification-driven development (SDD).

## GOAL

Install or update the requested agent plugins on a workspace level and provide a summary of the available plugings, skills, mcps, hooks, ... 

## PREREQUISITES

The following information should be provided. If not, the agent should ask for it in simple multiple-choice questions. One question at a time, until all information is collected.

- Requested plugins: 
  - Iterate through the plugins folder in this repository and ask a confirmation for each plugin. If the user confirms, add it to the list of requested plugins.


## INSTALLATION

The target destination for the plugins is the `.agents` folder of the current workspace the agents is working in.
The use of the `.agents` folder is recommended to avoid conflicts and make it vendor-agnostic.

If the folder does not exist, create it. Make use of the marketplace.json file to register the plugins. If the file does not exist, create it according to the example in the next section. If the file exists, update it with the new plugins.

The folder structure below is an example on where to place the plugins and how to register them in the marketplace.json file.


```example target destination
my-repo/
└── .agents/
    └── plugins/
        ├── marketplace.json                       # Registers local and/or remote Git plugins
        └── some-pluging-of-this-repository/       # Local plugin
```

## IDEMPOTENCY

This section is only relevant if the `.agents` folders already has some of the available plugins installed.

Executing this prompt again should not result in duplicate plugins or entries in the marketplace.json file. The agent should check for existing plugins and update them if necessary. 

Updating means overwrite the plugin folder of the target location with the plugin of this repository event if it contains changes. The other files in the `.agents` folder should remain untouched because they may be used by other agents or tools. 

If this repository yields new plugins, the agent should ask for confirmation before adding them to the marketplace.json file. 

The agent should not ask for confirmation if the plugin contains updated or new content, it should update the existing plugin.


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