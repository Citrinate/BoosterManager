# ASF Booster Creator Plugin

# Introduction
This plugin was made by [Outzzz](https://github.com/Outzzz), but after some time it got removed from github. Luckily I forked it before that, and as it was published under Apache 2.0 license I can continue development of this plugin. I tried to improve it a little, you can check what changes I made to it in git commits history.<br/>
As title says, aim of this plugin is giving a user an easy way to create booster packs, both by command and automatically.

## Installation
- download .zip file from [latest release](https://github.com/Ryzhehvost/BoosterCreator/releases/latest), in most cases you need `BoosterCreator.zip`, but if you use ASF-generic-netf.zip (you really need a strong reason to do that) download `BoosterCreator-netf.zip`.
- unpack downloaded .zip file to `plugins` folder inside your ASF folder.
- (re)start ASF, you should get a message indicating that plugin loaded successfully. 

## Usage
There is two ways to create boosters with this plugin: manual and automatic.
To manually create booster just send ASF command `booster <bots> <appids>`, and ASF will try to create boosters from specified games on selected bots.<br/>
Example: `booster bot1 730`<br/>
To automatically create boosters you can add to config of your bot(s) parameter `GamesToBooster`, of type "array of uint". ASF will create boosters from specified games as long as there is enough gems, automatically waiting for cooldowns.<br/>
Example: `"GamesToBooster": [730, 570],`<br/>

# Плагин для создания наборов карточек в ASF

# Введение
Этот плагин изначально был разработан [Outzzz](https://github.com/Outzzz), однако спустя некоторое время он был удалён с github. 
К счастью перед этим я сделал fork, и поскольку плагин был опубликован под лицензией Apache 2.0 я могу продолжить его разработку. 
Я попытался его несколько улучшить, вы можете ознакомиться с изменениями в истории коммитов на github.<br/>
Как ясно из заголовка, цель этого плагина - дать пользователю возможность создавать наборы карточек, как по команде, так и автоматически.

## Установка
- скачайте файл .zip из [последнего релиза](https://github.com/Ryzhehvost/BoosterCreator/releases/latest), в большинстве случаев вам нужен файл `BoosterCreator.zip`, не если вы по какой-то причине пользуетесь ASF-generic-netf.zip (а для этого нужны веские причины) - скачайте `BoosterCreator-netf.zip`.
- распакуйте скачанный файл .zip в папку `plugins` внутри вашей папки с ASF.
- (пере)запустите ASF, вы должны получить сообщение что плагин успешно загружен. 

## Использование
Есть два способа создания наборов карточек с помощью этого плагина: ручной и автоматический.
Для ручного создания набора карточек просто дайте ASF команду `booster <bots> <appids>`, и ASF попытается создать наборы карточек из всех указанных игр на заданных ботах.<br/>
Пример: `booster bot1 730`<br/>
Для автоматического создания набора карточек вам нужно добавить в конфигурационный файл бота(ов) параметр `GamesToBooster`, типа "массив uint". ASF будет создавать наборы карточек из указаных игр пока у него не закончатся самоцветы, автоматически учитывая задержки после создания.<br/>
Пример: `"GamesToBooster": [730, 570],`<br/>

![downloads](https://img.shields.io/github/downloads/Ryzhehvost/BoosterCreator/total.svg?style=social)
