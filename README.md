# Booster Manager Plugin for ArchiSteamFarm

[![Check out my other ArchiSteamFarm plugins](https://img.shields.io/badge/Check%20out%20my%20other%20ArchiSteamFarm%20plugins-blue?logo=github)](https://github.com/stars/Citrinate/lists/archisteamfarm-plugins) [![Help with translations](https://img.shields.io/badge/Help%20with%20translations-purple?logo=crowdin)](https://github.com/Citrinate/BoosterManager/tree/master/BoosterManager/Localization) ![GitHub all releases](https://img.shields.io/github/downloads/Citrinate/BoosterManager/total?logo=github&label=Downloads)

## Introduction
This plugin provides an easy-to-use interface for turning gems into booster packs, and includes tools for managing trading cards and other related items in your inventories and on the marketplace.

This project was originally based off of the [Booster Creator Plugin](https://github.com/Rudokhvist/BoosterCreator) by [Outzzz](https://github.com/Outzzz) and [Rudokhvist](https://github.com/Rudokhvist)

## Installation

- Download the .zip file from the [latest release](https://github.com/Citrinate/BoosterManager/releases/latest)
- Locate the `plugins` folder inside your ASF folder.  Create a new folder here and unpack the downloaded .zip file to that folder.
- (Re)start ASF, you should get a message indicating that the plugin loaded successfully.

> [!NOTE]
> This plugin is only tested to work with ASF-generic.  It may or may not work with other ASF variants, but feel free to report any issues you may encounter.

## Usage

Parameters in square brackets are sometimes `[Optional]`, parameters in angle brackets are always `<Required>`. When providing a command with an optional parameter, you must also provide it with all other optional parameters that come before it. Plural parameters such as `[Bots]` can accept multiple values separated by `,` such as `A,B,C` unless otherwise specified.

### Booster Commands

Command | Access | Description
--- | --- | ---
`booster [Bots] <AppIDs>`|`Master`|Adds `AppIDs` to the given bot's booster queue.
`booster^ [Bots] <AppIDs> <Amounts>`|`Master`|Adds `AppIDs` to some or all of given bot's booster queues, selected in a way to minimize the time it takes to craft a total `Amount` of boosters.  The `Amounts` specified may be a single amount for all `AppIDs`, or multiple amounts for each `AppID` respectively.
`bstatus [Bots]`|`Master`|Prints the status of the given bot's booster queue.
`bstatus^ [Bots]`|`Master`|Prints a shortened status of the given bot's booster queue.
`bstop [Bots] <AppIDs>`|`Master`|Removes `AppIDs` from the given bot's booster queue.
`bstoptime [Bots] <Hours>`|`Master`|Removes everything from the given bot's booster queue that will take more than the given `Hours` to craft.
`bstopall [Bots]`|`Master`|Removes everything from the given bot's booster queue.
`brate [Level]`|`Master`|Prints the optimal booster drop rate for an account at `Level`
`bdrops [Bots]`|`Master`|Prints the number of booster eligible games for the given bots

> [!NOTE]
> Any `booster` commands that haven't completed when ASF is closed will automatically resume the next time ASF is ran.

### Inventory Commands

#### Gems

Command | Access | Description
--- | --- | ---
`gems [Bots]`|`Master`|Displays the number of gems owned by the given bot.
`lootgems [Bots]`|`Master`|Sends all gems from the given bot to the `Master` user.
`lootsacks [Bots]`|`Master`|Sends all "Sack of Gems" from the given bot to the `Master` user.
`packgems [Bots]`|`Master`|Packs all "Gems" owned by the given bot into "Sack of Gems".
`transfergems [Bot] <TargetBots> <Amounts>`|`Master`|Sends the provided `Amounts` of unpacked gems from the given bot to the given target bot. The `Amounts` specified may be a single amount sent to all target bots, or multiple amounts sent to each target bot respectively.  You may also use `queue` or `q` as an amount to represent the number of gems needed to complete the target bot's booster queue.
`transfergems^ [Bots] <TargetBot>`|`Master`|Sends all gems from the given bot to the given target bot.
`transfersacks [Bots] <TargetBot>`|`Master`|Sends all "Sack of Gems" from the given bot to the given target bot.
`unpackgems [Bots]`|`Master`|Unpacks all "Sack of Gems" owned by the given bot into "Gems".

#### Boosters

The below commands only aplly to marketable boosters.  To have them apply to only unmarketable boosters, add `u` to the start of the command (ex: `ulootboosters`).  To have them apply to all boosters, add `a` to the start of the command (ex: `alootboosters`)

Command | Access | Description
--- | --- | ---
`boosters [Bots]`|`Master`|Displays the number of marketable boosters owned by the given bot.
`lootboosters [Bots]`|`Master`|Sends all marketable booster packs from the given bot to the `Master` user.
`transferboosters [Bots] <TargetBot>`|`Master`|Sends all marketable booster packs from the given bot to the given target bot.

#### Cards

The bellow commands only apply to marketable cards.  To have them apply to only unmarketable cards, add `u` to the start of the command (ex: `ulootcards`).  To have them apply to all cards, add `a` to the start of the command (ex: `alootcards`)

Command | Access | Description
--- | --- | ---
`cards [Bots]`|`Master`|Displays the number of marketable non-foil trading cards owned by the given bot.
`foils [Bots]`|`Master`|Displays the number of marketable foil trading cards owned by the given bot.
`lootcards [Bots]`|`Master`|Sends all marketable non-foil trading cards from the given bot to the `Master` user.
`lootfoils [Bots]`|`Master`|Sends all marketable foil trading cards from the given bot to the `Master` user.
`transfercards [Bots] <TargetBot>`|`Master`|Sends all marketable non-foil trading cards from the given bot to the given target bot.
`transferfoils [Bots] <TargetBot>`|`Master`|Sends all marketable foil trading cards from the given bot to the given target bot.

#### TF2 Keys

Command | Access | Description
--- | --- | ---
`keys [Bots]`|`Master`|Displays the number of "Mann Co. Supply Crate Key" owned by the given bot.
`lootkeys [Bots]`|`Master`|Sends all "Mann Co. Supply Crate Key" from the given bot to the `Master` user.
`transferkeys [Bot] <TargetBots> <Amounts>`|`Master`|Sends the provided `Amounts` of "Mann Co. Supply Crate Key" from the given bot to the given target bot. The `Amounts` specified may be a single amount sent to all target bots, or multiple amounts sent to each target bot respectively.
`transferkeys^ [Bots] <TargetBot>`|`Master`|Sends all "Mann Co. Supply Crate Key" from the given bot to the given target bot.

#### Other Items

The below commands apply to all items.  To have them apply to only marketable items, add `m` to the start of the command (ex: `mlootitems`).  To have them apply to only unmarketable items, add `u` to the start of the command (ex: `ulootitems`).

Command | Access | Description
--- | --- | ---
`countitems <Bots> <AppID> <ContextID> <ItemIdentifier>`|`Master`|Displays the number of items owned by the given bot with the matching `AppID`, `ContextID`, and [`ItemIdentifier`](#itemidentifiers).
`lootitems <Bots> <AppID> <ContextID> <ItemIdentifiers>`|`Master`|Sends all items with the matching `AppID`, `ContextID`, and any of [`ItemIdentifiers`](#itemidentifiers) from the given bot to the `Master` user.
`transferitems <Bots> <TargetBot> <AppID> <ContextID> <ItemIdentifiers>`|`Master`|Sends all items with the matching `AppID`, `ContextID`, and any of [`ItemIdentifiers`](#itemidentifiers) from the given bot to the given target bot.
`transferitems^ <Bot> <TargetBots> <Amounts> <AppID> <ContextID> <ItemIdentifiers>`|`Master`|Sends an amount of items with the matching `AppID`, `ContextID`, and any of [`ItemIdentifiers`](#itemidentifiers) from the given bot to the given target bot. The `Amounts` specified may be a single amount of each item sent to all target bots, or differing amounts of each item, respectively, sent to all target bots.
`transferitems% <Bot> <TargetBots> <Amounts> <AppID> <ContextID> <ItemIdentifier>`|`Master`|Sends an amount of an item with the matching `AppID`, `ContextID`, and [`ItemIdentifier`](#itemidentifiers) from the given bot to the given target bot. The `Amounts` specified may be a single amount sent to all target bots, or differing amounts sent to each target bot respectively.

#### Miscellaneous

Command | Access | Description
--- | --- | ---
`trade2faok [Bot] [Minutes]`|`Master`|Accepts all pending 2FA trade confirmations for given bot instances.  Optionally repeat this action once every `Minutes`.  To cancel any repetition, set `Minutes` to 0.
`tradecheck [Bot]`|`Master`|Attempt to handle any incoming trades for the given bot using ASF's [trading logic](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Trading#logic).
`tradesincoming [Bot] [From]`|`Master`|Displays the number of incoming trades for the given bot, optionally filtered to only count trades `From` the given bot names or 64-bit SteamIDs.

### Market Commands

Command | Access | Description
--- | --- | ---
`alert [Bot] <AppID> <HashName> <Buy/Sell> <Above/Below/AboveOrAt/BelowOrAt> <Amount>`|`Master`|The given bot will check the price (in that bot's wallet currency) of the item identified by [`AppID`](/BoosterManager/Docs/ItemIDs.md#example-for-a-market-item) and [`HashName`](/BoosterManager/Docs/ItemIDs.md#example-for-a-market-item) every 15 minutes.  A message will be sent when the `Buy/Sell` now price is `Above/Below/AboveOrAt/BelowOrAt` the given `Amount`. `HashName` here should be taken directly from the URL of the item's market listing page and not include any whitespace characters.
`buylimit <Bots>`|`Master`|Displays the value of the given bot's active buy orders, and how close the bot is to hitting the buy order limit.
`cancelalert <Bots> <AppID> [HashName] [Buy/Sell] [Above/Below/AboveOrAt/BelowOrAt] [Amount]`|`Master`|Cancels market alerts specified by [`AppID`](/BoosterManager/Docs/ItemIDs.md#example-for-a-market-item), [`HashName`](/BoosterManager/Docs/ItemIDs.md#example-for-a-market-item), `Buy/Sell`, `Above/Below/AboveOrAt/BelowOrAt`, and `Amount` for the given bots. `HashName` here should be taken directly from the URL of the item's market listing page and not include any whitespace characters.
`cancelallalerts [Bots]`|`Master`|Cancels all market alerts for the given bots.
`findlistings <Bots> <ItemIdentifiers>`|`Master`|Displays the `ListingIDs` of any market listing belonging to the given bot and matching any of the [`ItemIdentifiers`](#itemidentifiers).
`findandremovelistings <Bots> <ItemIdentifiers>`|`Master`|Removes any market listing belonging to the given bot and matching any of the [`ItemIdentifiers`](#itemidentifiers).
`listings [Bots]`|`Master`|Displays the total value of all active market listings owned by the given bot.
`removelistings [Bot] <ListingIDs>`|`Master`|Removes market `ListingIDs` belonging to the given bot.
`removepending <Bots>`|`Master`|Removes all pending market listings belonging to the given bot.
`market2faok [Bot] [Minutes]`|`Master`|Accepts all pending 2FA market confirmations for given bot instances.  Optionally repeat this action once every `Minutes`.  To cancel any repetition, set `Minutes` to 0.
`value [Bots] [BalanceLimit]`|`Master`|Displays the combined wallet balance and total value of all active market listings owned by the given bot.  The maximum allowed balance in your region may be provided as `BalanceLimit`, a whole number, and it will instead display how close the given bot is to reaching that limit.
`viewalerts [Bots]`|`Master`|Lists all market alerts for the given bots.

### Log Commands

Command | Access | Description
--- | --- | ---
`logdata [Bots]`|`Master`|A combination of the `logboosterdata`, `loginventoryhistory`, `logmarketlistings` and `logmarkethistory` commands.
`logboosterdata [Bots]`|`Master`|Collects booster data from the given bot and sends it to [`BoosterDataAPI`](#boosterdataapi)
`loginventoryhistory [Bots] [Count] [StartTime] [TimeFrac] [S]`|`Master`|Collects inventory history data from the given bot and sends it to [`InventoryHistoryAPI`](#inventoryhistoryapi).  The number of pages of inventory history may be specified using `Count`, and may begin on the page specified by either `StartTime` alone or by the combination of `StartTime`, `TimeFrac`, and `S`
`logmarketlistings [Bots]`|`Master`|Collects market listings data from the given bot and sends it to [`MarketListingsAPI`](#marketlistingsapi)
`logmarkethistory [Bots] [Count] [Start]`|`Master`|Collects market history data from the given bot and sends it to [`MarketHistoryAPI`](#markethistoryapi).  The number of pages of market history may be specified using `Count`, and may begin on the page specified by `Start`
`logstop [Bots]`|`Master`|Stops any actively running `loginventoryhistory` or `logmarkethistory` commands.

### Other Commands

Command | Access | Description
--- | --- | ---
`boostermanager`|`Master`|Prints version of plugin.

---

### ItemIdentifiers

An item identifier is an input used in certain commands which allows you target specific items or groups of items.  If a command allows for multiple item identifiers, each identifier must be separated with `&&` instead of a comma.  The valid formats for an item identifier are as follows:

Format | Example |
--- | --- |
`ItemName`|The identifier `Gems` will match the all "Gems" items
`ItemType`|The identifier `Steam Gems` will match all "Sack of Gems" and "Gems" items
`HashName`|The identifiers `753-Sack of Gems` or `753-Sack%20of%20Gems` will match all "Sack of Gems" items
`AppID::ContextID`|The identifier `753::6` will match with all Steam Community items
`AppID::ContextID::ClassID`|The identifier `753::6::667933237` will match all "Sack of Gems" items

> [!NOTE]
> Information on how to determine an item's `AppID`, `ContextID`, `ClassID`, `ItemName`, `ItemType`, and `HashName` may be found [here](/BoosterManager/Docs/ItemIDs.md).

---

### Command Aliases

Most pluralized commands also have a non-pluralized alias; ex: `lootboosters` has the alias `lootbooster`

Command | Alias |
--- | --- |
`alert`|`a`, `ma`
`buylimit`|`bl`
`cancelalert`|`cma`
`findlistings`|`fl`
`findandremovelistings`|`frl`
`removelistings`|`rlistings`, `removel`
`removepending`|`rp`
`logboosterdata`|`logbd`
`loginventoryhistory`|`logih`
`logmarketlistings`|`logml`
`logmarkethistory`|`logmh`
`market2faok`|`m2faok`
`trade2faok`|`t2faok`
`tradecheck`|`tc`
`tradesincoming`|`ti`

Command | Alias |
--- | --- |
`bstatus ASF`|`bsa`
`bstatus^ ASF`|`bsa^`
`boosters asf`|`ba`
`buylimit ASF`|`bla`
`cards asf`|`ca`
`foils asf`|`fa`
`gems ASF`|`ga`
`keys ASF`|`ka`
`listings ASF`|`lia`
`logdata ASF`|`lda`, `loga`
`lootboosters ASF`|`lba`
`lootcards ASF`|`lca`
`lootfoils ASF`|`lfa`
`lootgems ASF`|`lga`
`lootkeys ASF`|`lka`
`lootsacks ASF`|`lsa`
`market2faok ASF [Minutes]`|`m2faoka [Minutes]`
`trade2faok ASF [Minutes]`|`t2faoka [Minutes]`
`tradecheck ASF`|`tca`
`tradesincoming ASF [From]`|`tia [From]`
`tradesincoming ASF ASF`|`tiaa`
`transferboosters ASF <TargetBot>`|`tba <TargetBot>`
`transfercards ASF <TargetBot>`|`tca <TargetBot>`
`transferfoils ASF <TargetBot>`|`tfa <TargetBot>`
`value ASF [BalanceLimit]`|`va [BalanceLimit]`
`viewalerts ASF`|`vaa`

---

### AllowCraftUntradableBoosters

`bool` type with default value of `true`.  This configuration setting can be added to your `ASF.json` config file.  If set to `false`, untradable gems will not be used to craft boosters, and the `unpackgems` and `packgems` command will not unpack untradable "Sack of Gems".

```json
"AllowCraftUntradableBoosters": false,
```

---

### AllowCraftUnmarketableBoosters

`bool` type with default value of `true`.  This configuration setting can be added to your `ASF.json` config file.  If set to `false`, the plugin will not craft unmarketable boosters.

```json
"AllowCraftUnmarketableBoosters": false,
```

> [!NOTE]
> The plugin cannot immediately detect when a game's boosters switch from being marketable to unmarketable.  It will usually take ~4 hours to detect this change.

---

### GamesToBooster

`HashSet<uint>` type with default value of `[]`.  This configuration setting can be added to your individual bot config files.  It will automatically add all of the `AppIDs` to that bot's booster queue, and will automatically re-queue them after they've been crafted.

Example:

```json
"GamesToBooster": [730, 570],
```

> [!NOTE]
> It's not possible to remove any of these `AppIDs` from the booster queue using any commands.  Any changes you want to make will need to be made in the configuration file.

---

### BoosterDataAPI

`string` type with no default value.  This configuration setting can be added to your `ASF.json` config file.  When the `logboosterdata` command is used, booster data will be gathered and sent to the API located at the specified url.

Example:

```json
"BoosterDataAPI": "http://localhost/api/boosters",
```

You will need to design your API to accept requests and return responses per the following specifications:

<details>
  <summary>Request</summary>
  
  **Method**: `POST`
  
  **Content-Type**: `application/json`
  
  Name | Type | Description
  --- | --- | ---
  `steamid`|`ulong`|SteamID of the bot that `data` belongs to
  `source`|`string`|`https://steamcommunity.com/tradingcards/boostercreator/`
  `data`|`JArray`|The data parsed from `source` and sent as an array of objects.  Detailed below.
  `data[][appid]`|`uint`|Booster game AppID
  `data[][name]`|`string`|Booster game name
  `data[][series]`|`uint`|Booster series number
  `data[][price]`|`uint`|Price of booster in gems
  `data[][unavailable]`|`bool`|Set to `true` when the booster is on a 24 hour cooldown
  `data[][available_at_time]`|`string?`|A date and time string in ISO 8601 format, if `unavailable` is `false` then this will be `null`|
</details>

<details>
  <summary>Response</summary>
  
  **Content-Type**: `application/json`
  
  Name | Type | Required | Description
  --- | --- | --- | ---
  `success`|`bool`|Yes|Whether your operations succeeded or failed.
  `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
  `show_message`|`bool`|No|Whether or not to show any message
</details>

---

### MarketListingsAPI

`string` type with no default value.  This configuration setting can be added to your `ASF.json` config file.  When the `logmarketlistings` command is used, market listing data will be gathered and sent to the API located at the specified url.

Example:

```json
"MarketListingsAPI": "http://localhost/api/listings",
```

You will need to design your API to accept requests and return responses per the following specifications:

<details>
  <summary>Request</summary>

  **Method**: `POST`
  
  **Content-Type**: `application/json`
  
  Name | Type | Description
  --- | --- | ---
  `steamid`|`ulong`|SteamID of the bot that `data` belongs to
  `source`|`string`|`https://steamcommunity.com/market/mylistings?norender=1`
  `data`|`JObject`|The data taken directly from `source` with empty string values converted to `null`

  > **Note**
  > Pagination here is not supported.  While `source` does support pagination for `data[listings]`, the plugin will automatically combine all pages into one such that `data[listings]` contains all active listings for the bot.
</details>

<details>
  <summary>Response</summary>
  
  **Content-Type**: `application/json`
   
  Name | Type | Required | Description
  --- | --- | --- | ---
  `success`|`bool`|Yes|Whether your operations succeeded or failed.
  `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
  `show_message`|`bool`|No|Whether or not to show any message
</details>

---

### MarketHistoryAPI

`string` type with no default value.  This configuration setting can be added to your `ASF.json` config file.  When the `logmarkethistory` command is used, market history data will be gathered and sent to the API located at the specified url.

Example:

```json
"MarketHistoryAPI": "http://localhost/api/markethistory",
```

You will need to design your API to accept requests and return responses per the following specifications:

<details>
  <summary>Request</summary>
  
  **Method**: `POST`
  
  **Content-Type**: `application/json`
  
  Name | Type | Description
  --- | --- | ---
  `steamid`|`ulong`|SteamID of the bot that `data` belongs to
  `source`|`string`|`https://steamcommunity.com/market/myhistory?norender=1&count=500`
  `page`|`uint`|Page number, defined as `floor(data[start] / 500) + 1`
  `data`|`JObject`|The data taken directly from `source` with empty string values converted to `null`

  > **Note**
  > Multiple pages of `data` will be requested sequentially, and not in parallel.
</details>

<details>
  <summary>Response</summary>
  
  **Content-Type**: `application/json`
  
  Name | Type | Required | Description
  --- | --- | --- | ---
  `success`|`bool`|Yes|Whether your operations succeeded or failed.  If there's more pages to fetch, the plugin will only continue when `success` is `true`
  `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
  `show_message`|`bool`|No|Whether or not to show any message
  `get_next_page`|`bool`|No|Whether or not to fetch the next page.  If the plugin was already going to fetch the next page anyway, this does nothing.
  `next_page`|`uint`|No|If `get_next_page` is set to `true`, the next page will be fetched using this page number
</details>

---

### InventoryHistoryAPI

`string` type with no default value.  This configuration setting can be added to your `ASF.json` config file.  When the `loginventoryhistory` command is used, inventory history data will be gathered and sent to the API located at the specified url.

Example:

```json
"InventoryHistoryAPI": "http://localhost/api/inventoryhistory",
```

You will need to design your API to accept requests and return responses per the following specifications:

<details>
  <summary>Request</summary>
  
  **Method**: `POST`
  
  **Content-Type**: `application/json`
  
  Name | Type | Description
  --- | --- | ---
  `steamid`|`ulong`|SteamID of the bot that `data` belongs to
  `source`|`string`|`https://steamcommunity.com/my/inventoryhistory/?ajax=1`
  `page`|`uint`|The value of the `start_time` query parameter used to request `source`.  If a cursor object was used to request `source` instead, this will be equal to `cursor[time]`
  `cursor`|`JObject`|The value of the `cursor` object query parameter used to request `source`
  `data`|`JObject`|The data taken directly from `source` with empty string values converted to `null`
  
  > **Note**
  > Documentation of Steam's Inventory History API can be found [here](/BoosterManager/Docs/InventoryHistory.md)
  
  > **Note**
  > Multiple pages of `data` will be requested sequentially, and not in parallel.
</details>

<details>
  <summary>Response</summary>
  
  **Content-Type**: `application/json`
  
  Name | Type | Required | Description
  --- | --- | --- | ---
  `success`|`bool`|Yes|Whether your operations succeeded or failed.  If there's more pages to fetch, the plugin will only continue when `success` is `true`
  `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
  `show_message`|`bool`|No|Whether or not to show any message
  `get_next_page`|`bool`|No|Whether or not to fetch the next page.  If the plugin was already going to fetch the next page anyway, this does nothing.
  `next_page`|`uint`|No|If `get_next_page` is set to `true`, the next page will be fetched using this page number
  `next_cursor`|`JObject`|No|If `get_next_page` is set to `true`, the next page will be fetched using this cursor object
</details>

---

### InventoryHistoryAppFilter

`HashSet<uint>` type with defalt value of `[]`.  This configuration setting can be added to your `ASF.json` config file.  When using the `loginventoryhistory` command or `InventoryHistory` IPC interface API endpoint, the results will be filtered to only show inventory history events from these `AppIDs`

Example:

```json
"InventoryHistoryAppFilter": [730, 570],
```

---

### LogDataPageDelay

`uint` type with default value of `15`.  This configuration setting can be added to your `ASF.json` config file.  When using the `loginventoryhistory` or `logmarkethistory` commands to fetch multiple pages, it will add a `Seconds` delay between each page fetch.

Example:

```json
"LogDataPageDelay": 15,
```

---

### IPC Interface

API | Method | Parameters | Description
--- | --- | --- | ---
`/API/BoosterManager/{botName}/BoosterData`|`GET`||Retrieves booster data for given bot
`/API/BoosterManager/{botName}/MarketListings`|`GET`||Retrieves market listings data for given bot
`/API/BoosterManager/{botName}/MarketHistory`|`GET`|`page`|Retrieves market history data for given bot
`/API/BoosterManager/{botName}/InventoryHistory`|`GET`|`startTime`, `timeFrac`, `s`|Retrieves inventory history data for given bot
`/API/BoosterManager/{botName}/GetBadgeInfo/{appID}`|`GET`|`border`|Retrieves badge info for given bot
`/API/BoosterManager/{botNames}/GetPriceHistory/{appID}/{hashName}`|`GET`||Retrieves price history for market items [^1]

[^1]: Responses are not dependent on the account used to make these requests.  You may provide multiple `botNames`, and the first available bot will be used to make the request.
