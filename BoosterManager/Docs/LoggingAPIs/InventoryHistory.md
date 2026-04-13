# InventoryHistory API Specifications

For use with the [`loginventoryhistory`](/README.md#log-commands) command.  Configured by the [`InventoryHistoryAPI`](/README.md#inventoryhistoryapi), [`InventoryHistoryAppFilter`](/README.md#inventoryhistoryappfilter), and [`LogDataPageDelay`](/README.md#logdatapagedelay) settings.

Alternatively, this same data can be obtained using the `/Api/BoosterManager/{botName}/InventoryHistory` [IPC API](/README.md#account-info).

## Request Details

 - **Method**: `POST`
 - **Content-Type**: `application/json`

## Request Body
  
Property | Type | Description
--- | --- | ---
`steamid`|`ulong`|SteamID of the bot that `data` belongs to
`source`|`string`|`https://steamcommunity.com/my/inventoryhistory/?ajax=1`
`page`|`uint`|The value of the `start_time` query parameter used to request `source`.  If a cursor object was used to request `source` instead, this will be equal to `cursor[time]`
`cursor`|`object`|The value of the `cursor` object query parameter used to request `source`
`data`|`object`|The data taken directly from `source` with empty string values converted to `null`

> [!NOTE]
> Documentation of Steam's Inventory History API can be found [here](/BoosterManager/Docs/InventoryHistory.md)

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
`next_cursor`|`object`|No|If `get_next_page` is set to `true`, the next page will be fetched using this cursor object
