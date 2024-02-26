# How to find Steam Item IDs

## AppID

An AppID refers to a specific Steam App.  This can be found in the url of an App's Steam store page.

---

#### Example

`https://store.steampowered.com/app/730/CounterStrike_2/`

AppID of `730`

---

## ContextID

Apps can have multiple inventories, and the ContextID refers to the inventory an item exists in.  The ContextID can be found in the url you'd get by right clicking any item in your inventory, and selecting "Copy link address".

---

#### Example

`https://steamcommunity.com/id/████/inventory/#753_6_22000101010` 

AppID of `753`

ContextID of `6` 

AssetID of `22000101010`

---

## ClassID

The ClassID refers to all copies of an item.  This can be found in the source code of an item's market listing page.  Alternatively, or if an item doesn't have a market listing page, this can be found at `https://steamcommunity.com/my/inventory/json/AppID/ContextID`.  There also exists another location which can be used similarly at `https://steamcommunity.com/inventory/SteamID/AppID/ContextID`, which has the query parameters `count` and `start_assetid`.

---

#### Example using a Market Listing Page

The source code for [:TheMessenger:](https://steamcommunity.com/market/listings/753/764790-%3ATheMessenger%3A) contains the text:

```javascript
var g_rgAssets = {"753":{"6":{"28191259516":{"currency":0,"appid":753,"contextid":"6","id":"28191259516","classid":"2994832731","instanceid":"0"
```

AppID of `753`

ContextID of `6` 

ClassID of `2994832731`

---

#### Example using JSON inventory

It will be necessary to first get the AppID, ContextID, and AssetID of the item.  The AssetID refers to a specific copy of an item, and all three can be found using the above method to find the ContextID.

When navigating to https://steamcommunity.com/my/inventory/json/753/6 and searching for the AssetID of `22000101010`, we may find something like this:

```json
{"22000101010":{"id":"22000101010","classid":"2994832731","instanceid":"0","amount":"1","hide_in_china":0,"pos":42},
```

ClassID of `2994832731`
