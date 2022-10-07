# Booster Manager Plugin for ArchiSteamFarm

## Introduction
This plugin is based off of the [Booster Creator Plugin](https://github.com/Ryzhehvost/BoosterCreator) by [Outzzz](https://github.com/Outzzz) and [Ryzhehvost](https://github.com/Ryzhehvost).  At its core it serves the same purpose: to provide an easy-to-use interface for turning gems into booster packs.  The major difference being: this plugin is more tailored for users wanting to craft **a lot** of booster packs.  To that end, the scope of this project is a bit wider, intending to encompass all manner of features that could, even indirectly, facilitate crafting boosters.  Although this plugin is designed for power users, casual booster crafters should find it just as useful.

## Installation

- Download the .zip file from the [latest release](https://github.com/Citrinate/BoosterManager/releases/latest)
- Unpack the downloaded .zip file to the `plugins` folder inside your ASF folder.
- (Re)start ASF, you should get a message indicating that the plugin loaded successfully. 

Please note, this plugin only works with ASF-generic.

## Usage

Note: Parameters in square brackets are sometimes `[Optional]`, parameters in angle brackets are always `<Required>`

### Booster Commands

Command | Access | Description
--- | --- | ---
`booster [Bots] <AppIDs>`|`Master`|Adds `AppIDs` to the given bot's booster queue.  `AppIDs` added to the booster queue this way will be crafted one time as soon as they become available.
`bstatus [Bots]`|`Master`|Prints the status of the given bot's booster queue.
`bstop [Bots] <AppIDs>`|`Master`|Removes `AppIDs` from the given bot's booster queue.
`bstoptime [Bots] <Hours>`|`Master`|Removes everything from the given bot's booster queue that will take more than the given `Hours` to craft.
`bstopall [Bots]`|`Master`|Removes everything from the given bot's booster queue.

### Inventory Commands

Command | Access | Description
--- | --- | ---
`gems [Bots]`|`Master`|Displays the number of gems owned by the given bot.
`keys [Bots]`|`Master`|Displays the number of Mann Co. Supply Crate Keys owned by the given bot.
`lootboosters [Bots]`|`Master`|Sends all marketable booster packs from the given bot to the `Master` user.
`lootcards [Bots]`|`Master`|Sends all marketable non-foil trading cards from the given bot to the `Master` user.
`lootfoils [Bots]`|`Master`|Sends all marketable foil trading cards from the given bot to the `Master` user.
`lootgems [Bots]`|`Master`|Sends all gems from the given bot to the `Master` user.
`lootitems [Bots] <AppID> <ContextID> <ClassID>`|`Master`|Sends all items with the matching `AppID`, `ContextID`, and `ClassID` from the given bot to the `Master` user.
`lootkeys [Bots]`|`Master`|Sends all Mann Co. Supply Crate Keys from the given bot to the `Master` user.
`lootsacks [Bots]`|`Master`|Sends all Sacks of Gems from the given bot to the `Master` user.
`transferboosters [Bots] <TargetBot>`|`Master`|Sends all marketable booster packs from the given bot to the given target bot.
`transfercards [Bots] <TargetBot>`|`Master`|Sends all marketable non-foil trading cards from the given bot to the given target bot.
`transferfoils [Bots] <TargetBot>`|`Master`|Sends all marketable foil trading cards from the given bot to the given target bot.
`transfergems [Bot] <TargetBots> <Amounts>`|`Master`|Sends the provided `Amounts` of gems from the given bot to the given target bots. The `Amounts` specified may be a single amount sent to all targets, or multiple amounts sent to each target respectively.|
`transferitems [Bots] <TargetBot> <AppID> <ContextID> <ClassID>`|`Master`|Sends all items with the matching `AppID`, `ContextID`, and `ClassID` from the given bot to the given target bot.
`transferkeys [Bot] <TargetBots> <Amounts>`|`Master`|Sends the provided `Amounts` of Mann Co. Supply Crate Keys from the given bot to the given target bots. The `Amounts` specified may be a single amount sent to all targets, or multiple amounts sent to each target respectively.|
`transfersacks [Bots] <TargetBot>`|`Master`|Sends all Sacks of Gems from the given bot to the given target bot.
`unpackgems [Bots]`|`Master`|Unpacks all Sacks of Gems owned by the given bot.


### Market Commands

Command | Access | Description
--- | --- | ---
`findlistings [Bots] <ItemNames>`|`Master`|Displays the `ListingID` of any listings belonging to the given bot with a name matching any of `ItemNames`.  Multiple item names may be provided, but must be separated with `&&`
`findandremovelistings [Bots] <ItemNames>`|`Master`|Removes any listings belonging to the given bot with a name matching any of `ItemNames`.  Multiple names may be provided, but must be separated with `&&`
`listings [Bots]`|`Master`|Displays the total value of all listings owned by the given bot.
`logdata [Bots] [Count] [Start]`|`Master`|Collects data (booster data, market listings, market history) from the given bot and sends it to the [APIs](#boosterdataapi-marketlistingsapi-markethistoryapi) specified in `ASF.json`. The number of pages of market history may be specified using `Count`, and may begin on the page specified by `Start`
`removelistings [Bot] <ListingIDs>`|`Master`|Removes `ListingIDs` belonging to the given bot.
`value [Bots] [BalanceLimit]`|`Master`|Displays the combined wallet balance and total value of all listings owned by the given bot.  The maximum allowed balance in your region may be provided as `BalanceLimit`, a whole number, and it will instead display how close the given bot is to reaching that limit.

---

### AllowCraftUntradableBoosters

`"AllowCraftUntradableBoosters": <true/false>,`

Example: `"AllowCraftUntradableBoosters": true,`

This `bool` type configuration setting can be added to your `ASF.json` config file.  If set to `false`, untradable gems will not be used to craft boosters, and the `unpackgems` command will not unpack untradable Sacks of Gems.

By default, this is set to `true`

---

### GamesToBooster

`"GamesToBooster": [<AppIDs>],`

Example: `"GamesToBooster": [730, 570],`

This `HashSet<uint>` type configuration setting can be added to your individual bot config files.  It will automatically add any of the `AppIDs` to that bot's booster queue, and will automatically re-queue them after they've been crafted.

Note: It's not possible to remove any of these `AppIDs` from the booster queue using any commands.  Any changes you want to make will need to be made in the configuration file.

---

### BoosterDelayBetweenBots

`"BoosterDelayBetweenBots": <Seconds>,`

Example: `"BoosterDelayBetweenBots": 60,`

This `uint` type configuration setting can be added to your `ASF.json` config file.  It will add a `Seconds` delay between each of your bot's booster crafts.  For example: when crafting a booster at 12:00 using a 60 second delay; Bot 1 will craft at 12:00, Bot 2 will  craft at 12:01, Bot 3 will craft at 12:02, and so on.

By default, this delay is set to `0`, and is not recommended to be used except in the most extreme cases.

---

### BoosterDataAPI, MarketListingsAPI, MarketHistoryAPI

```
"BoosterDataAPI": "<Url>",
"MarketListingsAPI": "<Url>",
"MarketHistoryAPI": "<Url>",
```

Example: 
```
"BoosterDataAPI": "http://localhost/api/boosters", 
"MarketListingsAPI": "http://localhost/api/listings", 
"MarketHistoryAPI": "http://localhost/api/history",
```

These `string` type configuration settings can be added to your `ASF.json` config file.  When the `logdata` command is used, data from each of three sources will be gathered and sent to API at the associated `Url`.  The `logdata` command will not function unless at least one of these are defined.  

You will need to design your API to accept requests and return responses per the following specifications:

#### Request

> **Method**: `POST`
>
> **Content-Type**: `application/json`
>
> Name | Type | Description
> --- | --- | ---
> `steamid`|`ulong`|SteamID of the bot that `data` belongs to
> `source`|`string`|The url used to fetch `data`.  See API-specific details below.
> `page`|`uint?`|Page number, for when `data` is paginated, `null` otherwise.  See API-specific details below.
> `data`|`JObject/JArray`|The data taken from `source`.  See API-specific details below.
>
> > **BoosterDataAPI-specific Details**:
> >
> > Name | Type | Description
> > --- | --- | ---
> > `source`|`string`|`https://steamcommunity.com/tradingcards/boostercreator/`
> > `data`|`JArray`|The data parsed from `source` and sent as an array of objects.  See array entry details below.
> >
> > > **BoosterDataAPI-specific `data` Array Entry Details**:
> > >
> > > Name | Type | Notes
> > > --- | --- | ---
> > > `appid`|`uint`|Booster game AppID
> > > `name`|`string`|Booster game name
> > > `series`|`uint`|Booster series number
> > > `price`|`uint`|Price of booster in gems
> > > `unavailable`|`bool`|Set to `true` when the booster is on a 24 hour cooldown
> > > `available_at_time`|`string?`|A date and time string in ISO 8601 format, if `unavailable = false` then this will be `null`|
> >
> > **MarketListingsAPI-specific Details**:
> >
> > Name | Type | Description
> > --- | --- | ---
> > `source`|`string`|`https://steamcommunity.com/market/mylistings?norender=1`
> > `data`|`JObject`|The data taken directly from `source`
> >
> > **MarketHistoryAPI-specific Details**:
> >
> > Name | Type | Description
> > --- | --- | ---
> > `source`|`string`|`https://steamcommunity.com/market/myhistory?norender=1&count=500`
> > `page`|`uint`|Page number, defined as `floor(data[start] / 500) + 1`
> > `data`|`JObject`|The data taken directly from `source`

#### Response

> **Content-Type**: `application/json`
>
> Name | Type | Required | Description
> --- | --- | --- | ---
> `success`|`bool`|Yes|Whether your operations succeeded or failed
> `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
> `show_message`|`bool`|No|Whether or not to show any message
> `get_next_page`|`bool`|No|Whether or not to fetch the next page (for when `data` is paginated)

---

### MarketHistoryDelay

`"MarketHistoryDelay": <Seconds>,`

Example: `"MarketHistoryDelay": 15,`

This `uint` type configuration setting can be added to your `ASF.json` config file.  When using the `logdata` command, it will add a `Seconds` delay between fetching market history pages.

By default, this is set to `15`
