# Booster Manager Plugin for ArchiSteamFarm

## Introduction
This plugin is based off of the [Booster Creator Plugin](https://github.com/Ryzhehvost/BoosterCreator) by [Outzzz](https://github.com/Outzzz) and [Ryzhehvost](https://github.com/Ryzhehvost).  At its core it serves the same purpose: to provide an easy-to-use interface for turning gems into booster packs.  The major difference being: this plugin is more tailored for users wanting to craft **a lot** of booster packs.  To that end, the scope of this project is a bit wider, intending to encompass all manner of features that could, even indirectly, facilitate crafting boosters.  Although this plugin is designed for power users, casual booster crafters should find it just as useful.

## Installation

- Download the .zip file from the [latest release](https://github.com/Citrinate/BoosterManager/releases/latest)
- Unpack the downloaded .zip file to the `plugins` folder inside your ASF folder.
- (Re)start ASF, you should get a message indicating that the plugin loaded successfully. 

Please note, this plugin only works with ASF-generic.

## Usage

### Commands

Command | Access | Description
--- | --- | ---
`booster [Bots] <AppIDs>`|`Operator`|Adds `AppIDs` to the given bot's booster queue.  `AppIDs` added to the booster queue this way will be crafted one time as soon as they become available.
`bstatus [Bots]`|`Operator`|Prints the status of the given bot's booster queue.
`bstop [Bots] <AppIDs>`|`Operator`|Removes `AppIDs` from the given bot's booster queue.
`bstoptime [Bots] <#>`|`Operator`|Removes everything from the given bot's booster queue that will take more than `#` hours to craft.
`bstopall [Bots]`|`Operator`|Removes everything from the given bot's booster queue.

---

### GamesToBooster

`"GamesToBooster": [<AppIDs>],`

Example: `"GamesToBooster": [730, 570],`

This `HashSet<uint>` type configuration setting can be added to your individual bot config files.  It will automatically add any of the `AppIDs` to that bot's booster queue, and will automatically re-queue them after they've been crafted.

Note: It's not possible to remove any of these `AppIDs` from the booster queue using any commands.  Any changes you want to make will need to be made in the configuration file.

---

### BoosterDelayBetweenBots

`"BoosterDelayBetweenBots": #,`

Example: `"BoosterDelayBetweenBots": 60,`

This `uint` type configuration setting can be added to your `ASF.json` config file.  It will add a `#` second delay between each of your bot's booster crafts.  For example: when crafting a booster at 12:00 using a 60 second delay; Bot 1 will craft at 12:00, Bot 2 will  craft at 12:01, Bot 3 will craft at 12:02, and so on.

By default, this delay is set to `0`, and is not recommended to be used except in the most extreme cases.