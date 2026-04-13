# MarketListings API Specifications

For use with the [`logmarketlistings`](/README.md#log-commands) command.  Configured by the [`MarketListingsAPI`](/README.md#marketlistingsapi) setting.

Alternatively, this same data can be obtained using the `/Api/BoosterManager/{botName}/MarketListings` [IPC API](/README.md#account-info).

## Request Details

 - **Method**: `POST`
 - **Content-Type**: `application/json`

## Request Body
  
Property | Type | Description
--- | --- | ---
`steamid`|`ulong`|SteamID of the bot that `data` belongs to
`source`|`string`|`https://steamcommunity.com/market/mylistings?norender=1`
`data`|`object`|The data taken directly from `source` with empty string values converted to `null`

> [!NOTE]
> Pagination here is not supported.  While `source` does support pagination for `data[listings]`, the plugin will automatically combine all pages into one such that `data[listings]` contains all active listings for the bot.

---

## Response Details

 - **Content-Type**: `application/json`

## Response Body

Property | Type | Required | Description
--- | --- | --- | ---
`success`|`bool`|Yes|Whether your operations succeeded or failed.
`message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
`show_message`|`bool`|No|Whether or not to show any message
