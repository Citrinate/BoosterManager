# BoosterData API Specifications

For use with the [`logboosterdata`](/README.md#log-commands) command.  Configured by the [`BoosterDataAPI`](/README.md#boosterdataapi) setting.

Alternatively, this same data can be obtained using the `/Api/BoosterManager/{botName}/BoosterData` [IPC API](/README.md#account-info).

## Request Details

 - **Method**: `POST`
 - **Content-Type**: `application/json`

## Request Body

Property | Type | Description
--- | --- | ---
`steamid`|`ulong`|SteamID of the bot that `data` belongs to
`source`|`string`|`https://steamcommunity.com/tradingcards/boostercreator/`
`data`|`array`|The data parsed from `source` and sent as an array of objects.  Detailed below.
`data[][appid]`|`uint`|Booster game AppID
`data[][name]`|`string`|Booster game name
`data[][series]`|`uint`|Booster series number
`data[][price]`|`uint`|Price of booster in gems
`data[][unavailable]`|`bool`|Set to `true` when the booster is on a 24 hour cooldown
`data[][available_at_time]`|`string?`|A date and time string in ISO 8601 format, if `unavailable` is `false` then this will be `null`|

---

## Response Details

 - **Content-Type**: `application/json`

## Response Body

Property | Type | Required | Description
--- | --- | --- | ---
`success`|`bool`|Yes|Whether your operations succeeded or failed.
`message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
`show_message`|`bool`|No|Whether or not to show any message
