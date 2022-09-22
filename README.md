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
`booster [Bots] <AppIDs>`|`Master`|Adds `AppIDs` to the given bot's booster queue.  `AppIDs` added to the booster queue this way will be crafted one time as soon as they become available.
`bstatus [Bots]`|`Master`|Prints the status of the given bot's booster queue.
`bstop [Bots] <AppIDs>`|`Master`|Removes `AppIDs` from the given bot's booster queue.
`bstoptime [Bots] <Hours>`|`Master`|Removes everything from the given bot's booster queue that will take more than the given `Hours` to craft.
`bstopall [Bots]`|`Master`|Removes everything from the given bot's booster queue.
`gems [Bots]`|`Master`|Displays the number of gems owned by the given bot.|
`logdata [Bots] [Count] [Start]`|`Master`|Collects data (booster data, market listings, market history) from the given bot and sends it to the [APIs](#boosterdataapi-marketlistingsapi-markethistoryapi) specified in `ASF.json`. The number of pages of market history may be specified using `Count`, and may begin on the page specified by `Start`|
`transfergems [Bot] <TargetBots> <Amounts>`|`Master`|Sends the provided `Amounts` of gems from the given bot to the given target bots. The `Amounts` specified may be a single amount sent to all targets, or multiple amounts sent to each target respectively.|

Note: Parameters in square brackets are `[Optional]`, parameters in angle brackets are `<Required>`

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
> `source`|`string`|The url used to fetch `data`
> `page`|`uint?`|Page number, for when `data` is paginated (only used for `MarketHistoryAPI`, else this is set to `null`)
> `data`|`JObject`|The data taken from `source`, more details below
>
> > `MarketListingsAPI` `data` comes directly from `https://steamcommunity.com/market/mylistings?norender=1`
> >
> > `MarketHistoryAPI` `data` comes directly from `https://steamcommunity.com/market/myhistory?norender=1`
> > 
> > `BoosterDataAPI` `data` is parsed from `https://steamcommunity.com/tradingcards/boostercreator/` and sent as an array of objects:
> >
> > Name | Type | Notes
> > --- | --- | ---
> > `appid`|`uint`|Booster game AppID
> > `name`|`string`|Booster game name
> > `series`|`uint`|Booster series number
> > `price`|`uint`|Price of booster in gems
> > `unavailable`|`bool`|Set to `true` when the booster is on a 24 hour cooldown
> > `available_at_time`|`string?`|A date and time string in ISO 8601 format, if `unavailable` is `false` then this will be `null`|

#### Response

> **Content-Type**: `application/json`
>
> Name | Type | Required | Description
> --- | --- | --- | ---
> `success`|`bool`|Yes|Whether your operations succeeded or failed
> `message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
> `show_message`|`bool`|No|Whether or not to show any message

---

### MarketHistoryDelay

`"BoosterDataAPI": <Seconds>,`

Example: `"BoosterDataAPI": 15,`

This `uint` type configuration setting can be added to your `ASF.json` config file.  When using the `logdata` command, it will add a `Seconds` delay between fetching market history pages.

By default, this is set to `15`
