# MarketHistory API Specifications

For use with the [`logmarkethistory`](/README.md#log-commands) command.  Configured by the [`MarketHistoryAPI`](/README.md#markethistoryapi) and [`LogDataPageDelay`](/README.md#logdatapagedelay) settings.

Alternatively, this same data can be obtained using the `/Api/BoosterManager/{botName}/MarketHistory` [IPC API](/README.md#account-info).

## Request Details

 - **Method**: `POST`
 - **Content-Type**: `application/json`

## Request Body
  
Property | Type | Description
--- | --- | ---
`steamid`|`ulong`|SteamID of the bot that `data` belongs to
`source`|`string`|`https://steamcommunity.com/market/myhistory?norender=1&count=500`
`page`|`uint`|Page number, defined as `floor(data[start] / 500) + 1`
`data`|`object`|The data taken directly from `source` with empty string values converted to `null`

> [!NOTE]
> Multiple pages of `data` will be requested sequentially, and not in parallel.

---

## Response Details

 - **Content-Type**: `application/json`

## Response Body

Property | Type | Required | Description
--- | --- | --- | ---
`success`|`bool`|Yes|Whether your operations succeeded or failed.  If there's more pages to fetch, the plugin will only continue when `success` is `true`
`message`|`string`|No|A custom message that will be displayed in place of the default succeed/fail message
`show_message`|`bool`|No|Whether or not to show any message
`get_next_page`|`bool`|No|Whether or not to fetch the next page.  If the plugin was already going to fetch the next page anyway, this does nothing.
`next_page`|`uint`|No|If `get_next_page` is set to `true`, the next page will be fetched using this page number
